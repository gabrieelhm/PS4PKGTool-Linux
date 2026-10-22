using System;
using System.Collections.Generic;
using System.Text;

namespace PS4PKGTool.Utilities.PS4PKGToolHelper
{
    /// <summary>
    /// Markdown builder for the shadPS4 compatibility issue template.
    /// Kept fully UI-free so the template can be reworded or reordered
    /// without touching any form. The checklist statements mirror the
    /// upstream issue template; every item is OPT-IN: the UI must not
    /// tick them unless the user confirmed them.
    /// </summary>
    public static class CompatibilityReport
    {
        /// <summary>The checklist statements, in template order.</summary>
        public static readonly string[] ChecklistItems =
        {
            "Tested on required release build",
            "Checked for an existing report",
            "Own and unmodified game dump",
            "Required firmware modules installed",
            "Sync logging enabled",
            "Default emulation settings used",
        };

        /// <summary>
        /// Builds the full Markdown report. checklist[i] maps to
        /// <see cref="ChecklistItems"/>[i] (default false). logFileName is
        /// the session log's file name when known - the Log File section
        /// keeps the GitHub placeholder either way, GitHub generates its own
        /// attachment URL on paste.
        /// </summary>
        public static string BuildMarkdown(GameFeedbackEntry entry, IReadOnlyList<bool>? checklist = null, string? logFileName = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine("### Checklist");
            for (int i = 0; i < ChecklistItems.Length; i++)
            {
                bool done = checklist != null && i < checklist.Count && checklist[i];
                sb.AppendLine($"- [{(done ? "x" : " ")}] {ChecklistItems[i]}");
            }

            AppendSection(sb, "Game Name", entry.Title);
            AppendSection(sb, "Game serial", entry.TitleId);
            AppendSection(sb, "Game version", VersionDisplay(entry.GameVersion));
            AppendSection(sb, "Used emulator's version", entry.EmulatorVersion);
            AppendSection(sb, "Current status", NormalizeStatus(entry.Status));
            AppendSection(sb, "Operating System", entry.Os);
            AppendSection(sb, "Processor", entry.Processor);
            AppendSection(sb, "Graphics Card", entry.GraphicsCard);
            AppendSection(sb, "Error", entry.Error);
            AppendSection(sb, "Description", entry.Comment);
            sb.AppendLine("### Screenshots");
            sb.AppendLine();
            sb.AppendLine("<!-- Add screenshots here -->");
            sb.AppendLine();
            sb.AppendLine("### Log File");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrWhiteSpace(logFileName)
                ? "<!-- Attach the log file here -->"
                : $"<!-- Attach {logFileName} here -->");

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// One mapping for status spelling: UI/history uses "In-Game", the
        /// upstream template expects "Ingame". Everything else passes through.
        /// </summary>
        public static string NormalizeStatus(string? status) => (status ?? "").Trim() switch
        {
            "In-Game" => "Ingame",
            "In Game" => "Ingame",
            _ => (status ?? "").Trim(),
        };

        /// <summary>
        /// Game version in the upstream "v01.09" shape (a leading "v" is
        /// added only when absent; an empty version becomes a clear
        /// unknown marker).
        /// </summary>
        public static string VersionDisplay(string gameVersion)
        {
            string v = (gameVersion ?? "").Trim();
            if (string.IsNullOrEmpty(v)) return "(unknown)";
            return v.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? v : "v" + v;
        }

        private static void AppendSection(StringBuilder sb, string heading, string value)
        {
            sb.AppendLine();
            sb.AppendLine("### " + heading);
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrWhiteSpace(value) ? "(unknown)" : value.TrimEnd());
        }
    }
}
