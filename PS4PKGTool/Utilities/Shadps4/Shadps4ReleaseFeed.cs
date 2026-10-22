using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PS4PKGTool.Utilities.Shadps4
{
    public enum Shadps4Component
    {
        Core,
        QtLauncher,
    }

    public enum Shadps4FeedKind
    {
        CoreStable,
        CoreNightly,
        QtLauncher,
    }

    public sealed record Shadps4ReleaseInfo(
        Shadps4FeedKind Feed,
        string Repository,
        string Tag,
        DateTime PublishedUtc,
        string Commit,
        string AssetName,
        string AssetUrl,
        long SizeBytes,
        string? AssetDigestSha256,
        bool IsPrerelease)
    {
        public string BuildId => string.IsNullOrWhiteSpace(Commit) ? Sanitize(Tag) : Commit;

        private static string Sanitize(string s)
        {
            char[] bad = Path.GetInvalidFileNameChars();
            var chars = s.Select(c => bad.Contains(c) ? '_' : c).ToArray();
            return new string(chars);
        }
    }

    public sealed record Shadps4FeedResult(IReadOnlyList<Shadps4ReleaseInfo>? Releases, string? Error)
    {
        public bool Succeeded => Error == null;
    }

    public interface IShadps4FeedClient
    {
        Task<Shadps4FeedResult> GetReleasesAsync(Shadps4FeedKind feed, CancellationToken ct = default);
    }

    public sealed class Shadps4ReleaseFeed : IShadps4FeedClient
    {
        public const string CoreStableRepository = "shadps4-emu/shadPS4";
        public static string CoreNightlyRepository => OperatingSystem.IsWindows()
            ? "shadps4-emu/shadps4-binaries-Windows"
            : "shadps4-emu/shadPS4";
        public const string QtLauncherRepository = "shadps4-emu/shadps4-qtlauncher";

        private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromHours(1);
        private const string CacheFileName = "shadps4_releases.json";

        private readonly HttpClient _http;
        private readonly string _cachePath;
        private readonly IClock _clock;
        private readonly TimeSpan _cacheTtl;

        public Shadps4ReleaseFeed(string? cachePath = null, IClock? clock = null, HttpClient? http = null, TimeSpan? cacheTtl = null)
        {
            _cachePath = cachePath ?? Path.Combine(PS4PKGToolHelper.Helper.AppDataDirectory, CacheFileName);
            _clock = clock ?? new SystemClock();
            _cacheTtl = cacheTtl ?? DefaultCacheTtl;
            _http = http ?? new HttpClient();
            if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
                _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PS4-PKG-Tool", "2.0"));
        }

        public async Task<Shadps4FeedResult> GetReleasesAsync(Shadps4FeedKind feed, CancellationToken ct = default)
        {
            var cached = LoadCached(feed);
            if (cached != null) return new Shadps4FeedResult(cached, null);

            string repository = RepositoryFor(feed);
            string url = $"https://api.github.com/repos/{repository}/releases?per_page=20";

            HttpResponseMessage response;
            try
            {
                response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                return new Shadps4FeedResult(null, $"Could not reach GitHub: {ex.Message}");
            }

            using (response)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    return new Shadps4FeedResult(null,
                        "GitHub temporarily refused the request. Try again later.");
                }
                if (!response.IsSuccessStatusCode)
                {
                    return new Shadps4FeedResult(null,
                        $"GitHub returned {(int)response.StatusCode} for {repository}.");
                }

                string json;
                try
                {
                    json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return new Shadps4FeedResult(null, $"Could not read the GitHub response: {ex.Message}");
                }

                var releases = ParseReleases(json, feed, repository, _clock.UtcNow);
                SaveCache(feed, json);
                return new Shadps4FeedResult(releases, null);
            }
        }

        public static IReadOnlyList<Shadps4ReleaseInfo> ParseReleases(string json, Shadps4FeedKind feed, string repository, DateTime? now = null)
        {
            var result = new List<Shadps4ReleaseInfo>();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;

            bool isLinux = OperatingSystem.IsLinux();
            string osToken = isLinux ? "linux" : "win64";

            foreach (var release in doc.RootElement.EnumerateArray())
            {
                string tag = Str(release, "tag_name");
                if (string.IsNullOrWhiteSpace(tag)) continue;

                bool isPrerelease = release.TryGetProperty("prerelease", out var pre) && pre.GetBoolean();
                if (isLinux && feed == Shadps4FeedKind.CoreStable && isPrerelease) continue;
                if (isLinux && feed == Shadps4FeedKind.CoreNightly && !isPrerelease) continue;

                DateTime published = release.TryGetProperty("published_at", out var pub)
                    && DateTime.TryParse(pub.GetString(), out var parsed)
                        ? parsed.ToUniversalTime()
                        : DateTime.MinValue;

                string commit = ExtractCommit(tag, feed);

                if (!release.TryGetProperty("assets", out var assets)) continue;
                foreach (var asset in assets.EnumerateArray())
                {
                    string name = Str(asset, "name");
                    if (!name.Contains(osToken, StringComparison.OrdinalIgnoreCase)) continue;

                    string url = Str(asset, "browser_download_url");
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    long size = asset.TryGetProperty("size", out var sz) && sz.TryGetInt64(out var s) ? s : 0;
                    string? digest = asset.TryGetProperty("digest", out var dg) ? dg.GetString() : null;
                    if (digest != null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                        digest = digest["sha256:".Length..].Trim();

                    result.Add(new Shadps4ReleaseInfo(
                        feed, repository, tag, published, commit,
                        name, url, size, digest, isPrerelease));
                }
            }
            return result;
        }

        public static string ExtractCommit(string tag, Shadps4FeedKind feed)
        {
            int dash = tag.LastIndexOf('-');
            if (dash > 0 && dash < tag.Length - 1)
            {
                string tail = tag[(dash + 1)..];
                if (tail.Length >= 7 && tail.All(Uri.IsHexDigit))
                    return tail.ToLowerInvariant();
            }
            return "";
        }

        private static string Str(JsonElement e, string name)
            => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

        private static string RepositoryFor(Shadps4FeedKind feed) => feed switch
        {
            Shadps4FeedKind.CoreStable => CoreStableRepository,
            Shadps4FeedKind.CoreNightly => CoreNightlyRepository,
            Shadps4FeedKind.QtLauncher => QtLauncherRepository,
            _ => CoreStableRepository,
        };

        internal IReadOnlyList<Shadps4ReleaseInfo>? LoadCached(Shadps4FeedKind feed)
        {
            try
            {
                if (!File.Exists(_cachePath)) return null;
                using var doc = JsonDocument.Parse(File.ReadAllText(_cachePath));
                if (!doc.RootElement.TryGetProperty("fetchedUtc", out var fetched)
                    || !DateTime.TryParse(fetched.GetString(), out var fetchedAt))
                    return null;
                if (_clock.UtcNow - fetchedAt.ToUniversalTime() > _cacheTtl) return null;
                if (!doc.RootElement.TryGetProperty(feed.ToString(), out var entry)
                    || entry.ValueKind != JsonValueKind.Array)
                    return null;
                return ParseReleases(entry.GetRawText(), feed, RepositoryFor(feed));
            }
            catch
            {
                return null;
            }
        }

        private void SaveCache(Shadps4FeedKind feed, string rawJson)
        {
            try
            {
                var payload = new Dictionary<string, object>();
                if (File.Exists(_cachePath))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(File.ReadAllText(_cachePath));
                        foreach (var prop in doc.RootElement.EnumerateObject())
                            payload[prop.Name] = prop.Value.Clone();
                    }
                    catch { }
                }
                payload["fetchedUtc"] = _clock.UtcNow.ToString("O");
                payload[feed.ToString()] = JsonDocument.Parse(rawJson).RootElement.Clone();
                string? dir = Path.GetDirectoryName(_cachePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_cachePath, JsonSerializer.Serialize(payload));
            }
            catch { }
        }
    }
}
