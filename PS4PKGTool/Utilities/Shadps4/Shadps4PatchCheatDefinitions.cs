using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PS4PKGTool.Utilities.Shadps4
{
    public enum Shadps4DefinitionMatch { ExactMatch, VersionMismatch, Unknown }

    public sealed record Shadps4GamePatchContext(string Title, string TitleId, string AppVersion,
        string InstalledPath, string UserDirectory);
    public sealed record Shadps4CheatMod(string Name, string Hint, string Type, int OperationCount);
    public sealed record Shadps4CheatDefinition(string FilePath, string Source, string Game, string TitleId,
        string Version, string Process, IReadOnlyList<string> Authors, IReadOnlyList<Shadps4CheatMod> Mods);
    public sealed record Shadps4PatchEntry(int MetadataIndex, string Name, string Author, string PatchVersion, string AppVersion,
        string AppElf, bool? Enabled, int OperationCount, string OperationSummary);
    public sealed record Shadps4PatchStateChange(string FilePath, int MetadataIndex, bool Enabled);
    public sealed record Shadps4PatchDefinition(string FilePath, string Source, IReadOnlyList<string> TitleIds,
        IReadOnlyList<Shadps4PatchEntry> Entries);
    public sealed record Shadps4DefinitionDiagnostic(string FilePath, string Message);
    public sealed record Shadps4LocalDefinitions(IReadOnlyList<Shadps4CheatDefinition> Cheats,
        IReadOnlyList<Shadps4PatchDefinition> Patches, IReadOnlyList<Shadps4DefinitionDiagnostic> Diagnostics);
    public sealed record Shadps4DownloadResult(bool Succeeded, string Message, int DownloadedCount);

    /// <summary>
    /// Verified against shadps4-qtlauncher main (2026-08-22): definitions live in
    /// &lt;user&gt;\cheats and &lt;user&gt;\patches\shadPS4. QtLauncher applies cheats through IPC
    /// and its Save action rewrites selected patch XML. Cheats are runtime IPC only and have
    /// no persisted enabled state. Patches persist state as Metadata@isEnabled; this store
    /// changes only that attribute and never changes config.json.
    /// </summary>
    public sealed class Shadps4PatchCheatStore
    {
        public const string OfficialRepository = "shadPS4";
        public static string CheatsDirectory(string userDirectory) => Path.Combine(userDirectory, "cheats");
        public static string PatchesDirectory(string userDirectory) => Path.Combine(userDirectory, "patches", OfficialRepository);

        public Shadps4LocalDefinitions ListLocal(Shadps4GamePatchContext game)
        {
            var cheats = new List<Shadps4CheatDefinition>();
            var patches = new List<Shadps4PatchDefinition>();
            var diagnostics = new List<Shadps4DefinitionDiagnostic>();
            if (string.IsNullOrWhiteSpace(game.UserDirectory)) return new(cheats, patches, diagnostics);

            ReadFiles(CheatsDirectory(game.UserDirectory), "*.json", path =>
            {
                if (Shadps4DefinitionParser.TryParseCheat(File.ReadAllText(path), path, OfficialRepository, out var definition, out var error))
                {
                    if (string.Equals(definition.TitleId, game.TitleId, StringComparison.OrdinalIgnoreCase)) cheats.Add(definition);
                }
                else diagnostics.Add(new(path, error));
            }, diagnostics);
            ReadFiles(PatchesDirectory(game.UserDirectory), "*.xml", path =>
            {
                if (Shadps4DefinitionParser.TryParsePatch(File.ReadAllText(path), path, OfficialRepository, out var definition, out var error))
                {
                    if (definition.TitleIds.Any(id => string.Equals(id, game.TitleId, StringComparison.OrdinalIgnoreCase))) patches.Add(definition);
                }
                else diagnostics.Add(new(path, error));
            }, diagnostics);
            return new(cheats, patches, diagnostics);
        }

        public void InstallCheat(string userDirectory, string fileName, string contents)
            => InstallValidated(CheatsDirectory(userDirectory), fileName, contents, true);
        public void InstallPatch(string userDirectory, string fileName, string contents)
            => InstallValidated(PatchesDirectory(userDirectory), fileName, contents, false);

        /// <summary>Builds the index the current shadPS4 core uses to choose an XML per Title ID.</summary>
        public void RebuildPatchIndex(string userDirectory)
        {
            string directory = PatchesDirectory(userDirectory);
            if (!Directory.Exists(directory)) return;
            var map = new SortedDictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (string file in Directory.EnumerateFiles(directory, "*.xml"))
                if (Shadps4DefinitionParser.TryParsePatch(File.ReadAllText(file), file, OfficialRepository, out var patch, out _))
                    map[Path.GetFileName(file)] = patch.TitleIds.ToArray();
            string json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
            AtomicWrite(Path.Combine(directory, "files.json"), json);
        }

        /// <summary>Atomically changes only Metadata@isEnabled in verified local patch XML files.</summary>
        public void SetPatchEnabledStates(IEnumerable<Shadps4PatchStateChange> changes)
        {
            foreach (var group in changes.GroupBy(change => change.FilePath, StringComparer.OrdinalIgnoreCase))
            {
                string original = File.ReadAllText(group.Key);
                var document = XDocument.Parse(original, LoadOptions.PreserveWhitespace);
                var metadata = document.Descendants("Metadata").ToList();
                foreach (var change in group)
                {
                    if (change.MetadataIndex < 0 || change.MetadataIndex >= metadata.Count)
                        throw new InvalidDataException("Patch metadata changed before Apply.");
                    metadata[change.MetadataIndex].SetAttributeValue("isEnabled", change.Enabled ? "true" : "false");
                }
                using var writer = new StringWriter();
                document.Save(writer, SaveOptions.DisableFormatting);
                AtomicWrite(group.Key, writer.ToString());
            }
        }

        private static void InstallValidated(string directory, string fileName, string contents, bool cheat)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
                throw new InvalidOperationException("The definition file name is invalid.");
            bool valid = cheat
                ? Shadps4DefinitionParser.TryParseCheat(contents, fileName, OfficialRepository, out _, out var error)
                : Shadps4DefinitionParser.TryParsePatch(contents, fileName, OfficialRepository, out _, out error);
            if (!valid) throw new InvalidDataException(error);
            Directory.CreateDirectory(directory);
            string destination = Path.Combine(directory, fileName);
            string temp = Path.Combine(directory, "." + fileName + ".tmp");
            AtomicWrite(destination, contents);
        }

        private static void AtomicWrite(string destination, string contents)
        {
            string directory = Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("The destination directory is invalid.");
            Directory.CreateDirectory(directory);
            string temp = Path.Combine(directory, "." + Path.GetFileName(destination) + ".tmp");
            try { File.WriteAllText(temp, contents); File.Move(temp, destination, true); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        private static void ReadFiles(string directory, string filter, Action<string> read, List<Shadps4DefinitionDiagnostic> diagnostics)
        {
            if (!Directory.Exists(directory)) return;
            foreach (string path in Directory.EnumerateFiles(directory, filter))
            {
                try { read(path); }
                catch (Exception ex) { diagnostics.Add(new(path, ex.Message)); }
            }
        }
    }

    public static class Shadps4DefinitionParser
    {
        public static bool TryParseCheat(string json, string filePath, string source, out Shadps4CheatDefinition definition, out string error)
        {
            definition = null!; error = "";
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string Get(string name) => root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.String ? e.GetString()?.Trim() ?? "" : "";
                var mods = new List<Shadps4CheatMod>();
                if (root.TryGetProperty("mods", out var modsElement) && modsElement.ValueKind == JsonValueKind.Array)
                    foreach (var mod in modsElement.EnumerateArray())
                    {
                        string value(string n) => mod.TryGetProperty(n, out var e) && e.ValueKind == JsonValueKind.String ? e.GetString()?.Trim() ?? "" : "";
                        int count = mod.TryGetProperty("memory", out var memory) && memory.ValueKind == JsonValueKind.Array ? memory.GetArrayLength() : 0;
                        mods.Add(new(value("name"), value("hint"), value("type"), count));
                    }
                var authors = root.TryGetProperty("credits", out var credits) && credits.ValueKind == JsonValueKind.Array
                    ? credits.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()?.Trim() ?? "").Where(s => s.Length > 0).ToArray() : Array.Empty<string>();
                definition = new(filePath, source, Get("name"), Get("id"), Get("version"), Get("process"), authors, mods);
                if (string.IsNullOrWhiteSpace(definition.TitleId)) { error = "Cheat JSON has no id."; return false; }
                return true;
            }
            catch (Exception ex) { error = "Invalid cheat JSON: " + ex.Message; return false; }
        }

        public static bool TryParsePatch(string xml, string filePath, string source, out Shadps4PatchDefinition definition, out string error)
        {
            definition = null!; error = "";
            try
            {
                var document = XDocument.Parse(xml, LoadOptions.None);
                string[] ids = document.Descendants("TitleID").Descendants("ID").Select(e => e.Value.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var entries = new List<Shadps4PatchEntry>();
                int metadataIndex = 0;
                foreach (var metadata in document.Descendants("Metadata"))
                {
                    string A(string name) => metadata.Attribute(name)?.Value.Trim() ?? "";
                    var types = metadata.Descendants("Line").Select(e => e.Attribute("Type")?.Value.Trim() ?? "").Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                    bool? enabled = bool.TryParse(A("isEnabled"), out bool parsed) ? parsed : null;
                    entries.Add(new(metadataIndex++, A("Name"), A("Author"), A("PatchVer"), A("AppVer"), A("AppElf"), enabled,
                        metadata.Descendants("Line").Count(), string.Join(", ", types)));
                }
                definition = new(filePath, source, ids, entries);
                if (ids.Length == 0) { error = "Patch XML has no TitleID."; return false; }
                return true;
            }
            catch (Exception ex) { error = "Invalid patch XML: " + ex.Message; return false; }
        }
    }

    public static class Shadps4DefinitionMatcher
    {
        public static Shadps4DefinitionMatch Version(string installed, string definition)
        {
            if (string.IsNullOrWhiteSpace(installed) || string.IsNullOrWhiteSpace(definition)) return Shadps4DefinitionMatch.Unknown;
            return string.Equals(installed.Trim(), definition.Trim(), StringComparison.Ordinal)
                ? Shadps4DefinitionMatch.ExactMatch : Shadps4DefinitionMatch.VersionMismatch;
        }
        public static string Display(Shadps4DefinitionMatch match) => match switch
        {
            Shadps4DefinitionMatch.ExactMatch => "Exact match",
            Shadps4DefinitionMatch.VersionMismatch => "Version mismatch",
            _ => "Version unknown"
        };
    }

    public sealed class Shadps4OfficialDefinitionRepository
    {
        private const string IndexUrl = "https://raw.githubusercontent.com/shadps4-emu/ps4_cheats/main/CHEATS_JSON.txt";
        private const string CheatsUrl = "https://raw.githubusercontent.com/shadps4-emu/ps4_cheats/main/CHEATS/";
        private const string PatchesUrl = "https://api.github.com/repos/shadps4-emu/ps4_cheats/contents/PATCHES";
        private readonly HttpClient _http;
        public Shadps4OfficialDefinitionRepository(HttpClient? http = null)
        {
            _http = http ?? new HttpClient();
            if (_http.DefaultRequestHeaders.UserAgent.Count == 0) _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PS4-PKG-Tool", "1.7"));
        }
        public async Task<Shadps4DownloadResult> DownloadForGameAsync(Shadps4GamePatchContext game, Shadps4PatchCheatStore store, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(game.UserDirectory)) return new(false, "shadPS4 user directory is unavailable.", 0);
            try
            {
                string index = await GetText(IndexUrl, ct).ConfigureAwait(false);
                var files = index.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Split('=')[0].Trim()).Where(name => name.StartsWith(game.TitleId + "_" + game.AppVersion, StringComparison.OrdinalIgnoreCase) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                int downloaded = 0;
                foreach (string file in files)
                {
                    string content = await GetText(CheatsUrl + Uri.EscapeDataString(file), ct).ConfigureAwait(false);
                    store.InstallCheat(game.UserDirectory, file.Insert(file.Length - 5, "_shadPS4"), content); downloaded++;
                }
                using var patchDoc = JsonDocument.Parse(await GetText(PatchesUrl, ct).ConfigureAwait(false));
                foreach (var item in patchDoc.RootElement.EnumerateArray())
                {
                    string name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    string url = item.TryGetProperty("download_url", out var u) ? u.GetString() ?? "" : "";
                    if (!name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(url)) continue;
                    string content = await GetText(url, ct).ConfigureAwait(false);
                    if (Shadps4DefinitionParser.TryParsePatch(content, name, Shadps4PatchCheatStore.OfficialRepository, out var patch, out _)
                        && patch.TitleIds.Any(id => string.Equals(id, game.TitleId, StringComparison.OrdinalIgnoreCase))) { store.InstallPatch(game.UserDirectory, name, content); downloaded++; }
                }
                store.RebuildPatchIndex(game.UserDirectory);
                return new(true, downloaded == 0 ? "No official definitions match this installed game." : $"Downloaded {downloaded} definition(s).", downloaded);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { return new(false, "Could not update official definitions: " + ex.Message, 0); }
        }
        private async Task<string> GetText(string url, CancellationToken ct)
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode(); return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
    }
}
