using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PS4PKGTool.Utilities.PS4PKGToolHelper
{
    /// <summary>
    /// shadPS4 compatibility lookup, per operating system. The database is
    /// the official shadps4-compatibility/shadps4-game-compatibility GitHub
    /// repo - each game/OS combo is a GitHub Issue titled
    /// "CUSAxxxxx - Title", labeled with its status
    /// (status-playable/ingame/menus/boots/nothing) and its operating
    /// system (os-windows/os-linux/os-macOS). Issues without an os-* label
    /// are ignored (no guessing). This class downloads the issues once
    /// (manual fetch), caches a { CUSA -> { os -> status } } map in
    /// AppData\shadps4.json, and looks up statuses by Title ID + OS.
    /// </summary>
    public static class Shadps4Compat
    {
        private const string RepoApi = "https://api.github.com/repos/shadps4-compatibility/shadps4-game-compatibility/issues";
        /// <summary>
        /// The upstream project publishes an aggregated JSON dump as a release
        /// asset (one CDN download instead of many rate-limited API calls).
        /// "latest" always resolves to the newest release.
        /// </summary>
        private const string ReleaseAssetUrl =
            "https://github.com/shadps4-compatibility/shadps4-game-compatibility/releases/latest/download/compatibility_data.json";
        private static readonly string CachePath = Helper.AppDataDirectory + "shadps4.json";
        private static readonly Regex CusaRegex = new(@"^(CUSA\d{5})", RegexOptions.Compiled);
        private static readonly string[] Labels =
            { "status-playable", "status-ingame", "status-menus", "status-boots", "status-nothing" };

        private static Dictionary<string, Dictionary<string, string>> _cache;

        public static bool CacheExists => File.Exists(CachePath);

        public static DateTime? LastDownload
        {
            get
            {
                try { return File.Exists(CachePath) ? (DateTime?)File.GetLastWriteTime(CachePath) : null; }
                catch { return null; }
            }
        }

        public static int CacheCount
        {
            get
            {
                if (_cache == null) LoadCache();
                return _cache?.Count ?? 0;
            }
        }

        /// <summary>Display name of an OS key ("windows" -> "Windows").</summary>
        public static string OsDisplay(string os) => NormalizeOs(os) switch
        {
            "linux" => "Linux",
            "macos" => "macOS",
            _ => "Windows",
        };

        /// <summary>Returns the status ("Playable", "In-Game", ...) for a Title ID on the given OS, or "" if unknown.</summary>
        public static string Lookup(string titleId, string os)
        {
            if (string.IsNullOrEmpty(titleId)) return "";
            if (_cache == null) LoadCache();
            if (_cache != null && _cache.TryGetValue(titleId, out var byOs)
                && byOs.TryGetValue(NormalizeOs(os), out var status))
                return status;
            return "";
        }

        public static void LoadCache()
        {
            _cache = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(CachePath)) return;
                var entries = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(CachePath));
                if (entries == null) return;

                foreach (var kv in entries)
                {
                    var byOs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (kv.Value is string legacyStatus)
                    {
                        // Flat pre-OS cache ({CUSA -> status}): migrate as the
                        // Windows layer - those entries were Windows reports.
                        if (legacyStatus.Length > 0) byOs["windows"] = legacyStatus;
                    }
                    else if (kv.Value is JObject jo)
                    {
                        foreach (var osKv in jo)
                            byOs[NormalizeOs(osKv.Key)] = osKv.Value?.ToString() ?? "";
                    }
                    _cache[kv.Key] = byOs;
                }
            }
            catch (Exception ex)
            {
                // A corrupt/unreadable cache used to be swallowed silently,
                // which left _cache as an empty dict - Lookup then returned
                // "" forever until the app restarted. Log it so the user can
                // tell a broken cache from a genuinely unknown title.
                Logger.LogWarning("shadPS4 compat cache load failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Downloads the compatibility database and writes the cache file.
        /// Primary source: the aggregated release asset (one CDN download).
        /// Fallback: scrape the source GitHub issues as before. Returns
        /// (count, error) - error is null on success. The cache shape is
        /// unchanged ({CUSA -> {os -> status}}), so LoadCache is untouched.
        /// </summary>
        public static async Task<(int count, string error)> DownloadAsync(IProgress<string> progress = null)
        {
            progress?.Report("Downloading the compatibility database...");
            var (result, error) = await TryDownloadReleaseAssetAsync();
            if (error == null && result.Count > 0)
            {
                _cache = result;
                File.WriteAllText(CachePath, JsonConvert.SerializeObject(result, Formatting.Indented));
                progress?.Report($"Compatibility database updated: {result.Count} games");
                return (result.Count, null);
            }

            // The aggregated release is unavailable (network, HTTP, parse or
            // empty) - fall back to scraping the source issues.
            return await DownloadFromIssuesAsync(progress);
        }

        /// <summary>Downloads the release asset once; null error on success.</summary>
        private static async Task<(Dictionary<string, Dictionary<string, string>> result, string? error)>
            TryDownloadReleaseAssetAsync()
        {
            var empty = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "PS4-PKG-Tool");
                http.Timeout = TimeSpan.FromSeconds(90);

                using var resp = await http.GetAsync(ReleaseAssetUrl);
                if (!resp.IsSuccessStatusCode)
                    return (empty, $"GitHub download error: {(int)resp.StatusCode}");
                string json = await resp.Content.ReadAsStringAsync();
                return ParseReleaseAsset(json);
            }
            catch (Exception ex)
            {
                return (empty, ex.Message);
            }
        }

        /// <summary>
        /// Parses the aggregated release asset:
        /// {CUSA -> {"os-windows" -> {status, name, version, ...}}} into the
        /// cache shape {CUSA -> {os -> status}}. Entries with unknown OS keys
        /// or unknown status labels are skipped (never guessed).
        /// </summary>
        internal static (Dictionary<string, Dictionary<string, string>> result, string? error) ParseReleaseAsset(string json)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var db = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, JObject>>>(json);
                if (db == null || db.Count == 0)
                    return (result, "The compatibility database is empty or unreadable.");

                foreach (var game in db)
                {
                    var byOs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var osEntry in game.Value)
                    {
                        string os = OsFromLabel(osEntry.Key);
                        if (os == null) continue; // not an os-* key - ignore

                        string statusLabel = osEntry.Value?["status"]?.ToString() ?? "";
                        string status = LabelToStatus(statusLabel);
                        if (string.IsNullOrEmpty(status) || status == statusLabel) continue; // unknown status
                        byOs[os] = status;
                    }
                    if (byOs.Count > 0) result[game.Key] = byOs;
                }
                return (result, null);
            }
            catch (Exception ex)
            {
                return (result, ex.Message);
            }
        }

        /// <summary>Legacy source: scrape the repository's status-labeled issues.</summary>
        private static async Task<(int count, string error)> DownloadFromIssuesAsync(IProgress<string> progress = null)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "PS4-PKG-Tool");
                http.Timeout = TimeSpan.FromSeconds(90);

                foreach (var label in Labels)
                {
                    int page = 1;
                    while (true)
                    {
                        string url = $"{RepoApi}?state=all&labels={label}&per_page=100&page={page}";
                        string json;
                        using (var resp = await http.GetAsync(url))
                        {
                            if (!resp.IsSuccessStatusCode)
                                return (result.Count, $"GitHub API error: {(int)resp.StatusCode}");
                            json = await resp.Content.ReadAsStringAsync();
                        }

                        var issues = JsonConvert.DeserializeObject<List<dynamic>>(json);
                        if (issues == null || issues.Count == 0) break;

                        foreach (var issue in issues)
                        {
                            // Collect the OS labels; issues without an os-*
                            // label are ignored (don't guess the OS).
                            var osTags = new List<string>();
                            foreach (var lbl in issue.labels)
                            {
                                string os = OsFromLabel((string)(lbl.name ?? ""));
                                if (os != null && !osTags.Contains(os)) osTags.Add(os);
                            }
                            if (osTags.Count == 0) continue;

                            string title = (string)(issue.title ?? "");
                            var m = CusaRegex.Match(title);
                            if (!m.Success) continue;

                            string status = LabelToStatus(label);
                            if (!result.TryGetValue(m.Groups[1].Value, out var byOs))
                            {
                                byOs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                result[m.Groups[1].Value] = byOs;
                            }
                            foreach (string os in osTags) byOs[os] = status;
                        }

                        if (issues.Count < 100) break;
                        page++;
                    }
                    progress?.Report($"{label.Replace("status-", "")}: {result.Count} games so far");
                }

                _cache = result;
                File.WriteAllText(CachePath, JsonConvert.SerializeObject(result, Formatting.Indented));
                return (result.Count, null);
            }
            catch (Exception ex)
            {
                return (result.Count, ex.Message);
            }
        }

        private static string LabelToStatus(string label) => label switch
        {
            "status-playable" => "Playable",
            "status-ingame" => "In-Game",
            "status-menus" => "Menus",
            "status-boots" => "Boots",
            "status-nothing" => "Nothing",
            _ => label
        };

        /// <summary>Maps an os-* label to a normalized OS key, or null when the label is not an OS label.</summary>
        private static string OsFromLabel(string label) => (label ?? "").ToLowerInvariant() switch
        {
            "os-windows" => "windows",
            "os-linux" => "linux",
            "os-macos" => "macos",
            _ => null
        };

        /// <summary>Normalizes an OS key ("mac"/"macos"/anything unknown -&gt; defaults).</summary>
        private static string NormalizeOs(string os) => (os ?? "").Trim().ToLowerInvariant() switch
        {
            "linux" => "linux",
            "macos" => "macos",
            "mac" => "macos",
            _ => "windows",
        };

        /// <summary>
        /// Semantic sort rank for the compatibility column (Playable best,
        /// unknown worst). Alphabetical order would put "Boots" above
        /// "In-Game" - this fixes that.
        /// </summary>
        public static int StatusRank(string status) => status switch
        {
            "Playable" => 5,
            "In-Game" => 4,
            "Menus" => 3,
            "Boots" => 2,
            "Nothing" => 1,
            _ => 0,
        };

        /// <summary>Status color for the grid cell (readable on the dark theme).</summary>
        public static Color StatusColor(string status) => status switch
        {
            "Playable" => Color.FromArgb(102, 187, 106),  // green
            "In-Game" => Color.FromArgb(255, 213, 79),    // yellow
            "Menus" => Color.FromArgb(255, 167, 38),      // orange
            "Boots" => Color.FromArgb(239, 83, 80),       // red
            "Nothing" => Color.FromArgb(158, 158, 158),   // gray
            _ => Color.FromArgb(158, 158, 158)
        };
    }
}
