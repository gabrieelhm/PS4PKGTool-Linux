using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PS4PKGTool.Utilities.Shadps4;

namespace PS4PKGTool.Utilities.PS4PKGToolHelper
{
    /// <summary>
    /// One local test result, saved after a game session (or added manually).
    /// New session fields are OPTIONAL - old JSONL records without them
    /// deserialize fine (missing properties keep their defaults) and the UI
    /// shows "Unknown" / "Not recorded" instead of failing.
    /// </summary>
    public sealed class GameFeedbackEntry
    {
        public string TimestampUtc { get; set; } = "";
        public string TitleId { get; set; } = "";
        public string Title { get; set; } = "";
        public string GameVersion { get; set; } = "";
        public string Core { get; set; } = "";
        public string Launcher { get; set; } = "";
        public string Status { get; set; } = "";
        public string Os { get; set; } = "";
        public string EmulatorVersion { get; set; } = "";
        public string Processor { get; set; } = "";
        public string GraphicsCard { get; set; } = "";
        public string Error { get; set; } = "";
        public string Comment { get; set; } = "";
        /// <summary>User-selected best profile for this game (at most one per game).</summary>
        public bool IsBestProfile { get; set; } = false;

        // ── structured session information (optional, additive) ──────────

        /// <summary>Stable identity of this result; the name of its preserved log snapshot.</summary>
        public string ResultId { get; set; } = "";
        /// <summary>"LaunchSession" or "Manual" - where the result came from.</summary>
        public string ResultSource { get; set; } = "";
        /// <summary>Session runtime in whole seconds (machine-stable; format in the UI).</summary>
        public long? RuntimeSeconds { get; set; }
        public string? SessionStartedAtUtc { get; set; }
        public string? SessionEndedAtUtc { get; set; }
        /// <summary>Shadps4ExitCategory name ("NormalExit", "AccessViolation", ...). Empty for manual results.</summary>
        public string TerminationKind { get; set; } = "";
        /// <summary>Process exit code (unsigned reinterpretation preserved as int).</summary>
        public int? ExitCode { get; set; }
        /// <summary>Windows STATUS_ name, e.g. "STATUS_ACCESS_VIOLATION".</summary>
        public string? ExitStatusName { get; set; }
        /// <summary>Managed build id (commit sha or release tag) of the active core, when managed.</summary>
        public string CoreId { get; set; } = "";
        /// <summary>Short commit of the managed core build, when the id is a full sha.</summary>
        public string? CoreCommit { get; set; }
        /// <summary>Core executable version (e.g. "0.17.1.0") when known.</summary>
        public string? CoreVersion { get; set; }
        /// <summary>Path to the PS4PKGTool-owned snapshot of the session log, or the session log path itself.</summary>
        public string? LogReference { get; set; }

        /// <summary>
        /// True when the session's termination was an error status (reuses the
        /// termination subsystem's own classification - never derived from
        /// exit code != 0). Independent of the compatibility Status.
        /// </summary>
        [JsonIgnore]
        public bool Crashed
        {
            get
            {
                if (string.IsNullOrEmpty(TerminationKind)) return false;
                return Enum.TryParse(TerminationKind, out Shadps4ExitCategory category)
                    && Shadps4ExitStatus.IsErrorStatus(category);
            }
        }
    }

    /// <summary>
    /// Display helpers for test results (runtime, exit, timestamps). Values
    /// are formatted from machine-stable primitives; missing data shows
    /// "Unknown" / "Not recorded" instead of failing.
    /// </summary>
    public static class TestResultDisplay
    {
        /// <summary>"18s", "47m 12s", "2h 13m" - or "Unknown".</summary>
        public static string FormatRuntime(long? runtimeSeconds)
        {
            if (runtimeSeconds == null || runtimeSeconds < 0) return "Unknown";
            long seconds = runtimeSeconds.Value;
            if (seconds < 60) return seconds + "s";
            long minutes = seconds / 60;
            long hours = minutes / 60;
            if (hours > 0) return $"{hours}h {minutes % 60}m";
            return minutes < 60
                ? (seconds % 60 == 0 ? $"{minutes}m" : $"{minutes}m {seconds % 60}s")
                : $"{minutes}m";
        }

        /// <summary>"0xC0000005" - or "Not recorded".</summary>
        public static string FormatExitCode(int? exitCode)
            => exitCode == null ? "Not recorded" : $"0x{unchecked((uint)exitCode.Value):X8}";

        /// <summary>Local "yyyy-MM-dd HH:mm" or "-" for unparseable timestamps.</summary>
        public static string FormatWhen(string? timestampUtc)
        {
            if (DateTime.TryParse(timestampUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime when))
                return when.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return "-";
        }
    }

    /// <summary>
    /// The session log a test result may use. Historical results reference
    /// their own preserved snapshot (or the original session log path); a
    /// missing reference means "not preserved" - the CURRENT shadPS4 log is
    /// never substituted for an old or manual result.
    /// </summary>
    public static class TestResultLogs
    {
        /// <summary>
        /// The log file belonging to this result, or null when it was not
        /// preserved (or its snapshot no longer exists). Never falls back to
        /// today's shadPS4 log.
        /// </summary>
        public static string? Resolve(GameFeedbackEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.LogReference)) return null;
            try { return File.Exists(entry.LogReference) ? entry.LogReference : null; }
            catch { return null; }
        }
    }

    /// <summary>
    /// What the result form pre-fills, resolved by the caller (Main/Manager)
    /// from the game grid, the active core/launcher settings and - for
    /// launch sessions - the termination report. Manual results leave the
    /// session fields at their defaults (null / ""), so nothing is invented.
    /// </summary>
    public sealed record GameFeedbackContext(
        string TitleId,
        string Title,
        string GameVersion,
        string KnownStatus,
        string CoreDisplay,
        string LauncherDisplay,
        string OsDisplay = "",
        string EmulatorVersion = "",
        string ErrorPrefill = "",
        string? SessionLogPath = null,
        long? RuntimeSeconds = null,
        string? TerminationKind = null,
        int? ExitCode = null,
        string? ExitStatusName = null,
        string CoreId = "",
        string? CoreCommit = null,
        string? CoreVersion = null,
        string ResultSource = "Manual");

    /// <summary>
    /// Formats an entry as the shadPS4 compatibility submission template
    /// (the same sections the upstream project's issue template uses):
    /// Game Name / Game serial / Game version / Used emulator's version /
    /// Current status / Operating System / Processor / Graphics Card /
    /// Error / Description. Blank-line separated, ready to paste.
    /// </summary>
    public static class Shadps4SubmissionFormatter
    {
        public static string FormatSubmissionText(GameFeedbackEntry e)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Game Name");
            sb.AppendLine(e.Title);
            sb.AppendLine();
            sb.AppendLine("Game serial");
            sb.AppendLine(e.TitleId);
            sb.AppendLine();
            sb.AppendLine("Game version");
            sb.AppendLine(e.GameVersion);
            sb.AppendLine();
            sb.AppendLine("Used emulator's version (only major released versions are acceptable)");
            sb.AppendLine(e.EmulatorVersion);
            sb.AppendLine();
            sb.AppendLine("Current status");
            sb.AppendLine(CompatibilityReport.NormalizeStatus(e.Status));
            sb.AppendLine();
            sb.AppendLine("Operating System");
            sb.AppendLine(e.Os);
            sb.AppendLine();
            sb.AppendLine("Processor");
            sb.AppendLine(e.Processor);
            sb.AppendLine();
            sb.AppendLine("Graphics Card");
            sb.AppendLine(e.GraphicsCard);
            sb.AppendLine();
            sb.AppendLine("Error");
            sb.AppendLine(e.Error);
            sb.AppendLine();
            sb.AppendLine("Description");
            sb.Append(e.Comment);
            return sb.ToString().TrimEnd();
        }
    }

    /// <summary>
    /// Appends one feedback entry per game session as JSONL next to the exe
    /// (AppData\game-feedback.jsonl) - the same portable folder as the
    /// settings file, so reports travel with the app folder.
    /// filePath is injectable for tests.
    /// </summary>
    public static class GameFeedbackStore
    {
        public static string FilePath => Path.Combine(Helper.UserDataDirectory, "game-feedback.jsonl");

        /// <summary>PS4PKGTool-owned session log snapshots, one &lt;ResultId&gt;.log per result.</summary>
        public static string LogSnapshotsDirectory => Path.Combine(Helper.UserDataDirectory, "Shadps4Reports", "Logs");

        private static readonly object Sync = new();

        public static void Append(GameFeedbackEntry entry, string? filePath = null)
        {
            try
            {
                string path = filePath ?? FilePath;
                string dir = Path.GetDirectoryName(path) ?? "";
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                lock (Sync)
                {
                    File.AppendAllText(path, JsonSerializer.Serialize(entry) + Environment.NewLine);
                }
            }
            catch
            {
                // Feedback must never break the session flow.
            }
        }

        /// <summary>
        /// Marks one entry as the game's best profile and clears the flag on
        /// the game's other entries (a game has at most one best). Rewrites
        /// the JSONL file; no-op when the entry is not found.
        /// </summary>
        public static void SetBestProfile(string titleId, string timestampUtc, string? filePath = null)
        {
            try
            {
                string path = filePath ?? FilePath;
                if (!File.Exists(path)) return;
                lock (Sync)
                {
                    var all = ReadAll(path);
                    bool matched = false;
                    foreach (var e in all)
                    {
                        bool isTarget = string.Equals(e.TitleId, titleId, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(e.TimestampUtc, timestampUtc, StringComparison.OrdinalIgnoreCase);
                        if (isTarget) { e.IsBestProfile = true; matched = true; }
                        else if (string.Equals(e.TitleId, titleId, StringComparison.OrdinalIgnoreCase))
                            e.IsBestProfile = false;
                    }
                    if (!matched) return;

                    using (var sw = new StreamWriter(path, false))
                        foreach (var e in all)
                            sw.WriteLine(JsonSerializer.Serialize(e));
                }
            }
            catch
            {
                // Selection must never break the session flow.
            }
        }

        /// <summary>All entries in file order (test helper and future export).</summary>
        public static List<GameFeedbackEntry> ReadAll(string? filePath = null)
        {
            var list = new List<GameFeedbackEntry>();
            string path = filePath ?? FilePath;
            if (!File.Exists(path)) return list;

            foreach (string line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var entry = JsonSerializer.Deserialize<GameFeedbackEntry>(line);
                    if (entry != null) list.Add(entry);
                }
                catch { }
            }
            return list;
        }
    }
}
