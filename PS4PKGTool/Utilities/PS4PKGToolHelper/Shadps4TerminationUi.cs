using System;
using System.Diagnostics;
using System.IO;
using PS4PKGTool.Utilities.Shadps4;

namespace PS4PKGTool.Utilities.PS4PKGToolHelper
{
    public static class Shadps4TerminationUi
    {
        private const int DialogLogTailLines = 12;

        public static event Action<Shadps4TerminationReport, GameFeedbackContext?>? TerminationReported;

        public static void ShowIfNeeded(Shadps4TerminationReport report, GameFeedbackContext? feedback = null)
        {
            TerminationReported?.Invoke(report, feedback);
        }

        public static (AppMessageType Type, string Message, bool OfferWerFolder) BuildDialog(Shadps4TerminationReport report)
        {
            if (Shadps4ExitStatus.IsErrorStatus(report.Category))
            {
                string msg = $"shadPS4 terminated with error status {report.ExitCodeText} after {report.RuntimeText}.\n\n";
                if (!string.IsNullOrEmpty(report.Target))
                    msg += $"Target: {report.Target}\n";
                msg += $"Core: {report.Executable}\n";
                if (!string.IsNullOrEmpty(report.CoreVersion))
                    msg += $"Core version: {report.CoreVersion}\n";

                string? logTail = TrimTailForDialog(report.LogTail);
                if (logTail != null)
                    msg += $"\nLast emulator log lines:\n{logTail}\n";

                if (string.IsNullOrWhiteSpace(report.WerReportFolder))
                    return (AppMessageType.Warning, msg, false);
                msg += "\nA crash report was found for this termination.";
                if (!string.IsNullOrEmpty(report.DumpFile))
                    msg += $"\nIt contains a crash dump ({Path.GetFileName(report.DumpFile)}).";
                msg += "\n\nOpen the crash report folder?";
                return (AppMessageType.Warning, msg, true);
            }

            string closed = $"shadPS4 closed after {report.RuntimeText}.\n";
            if (!string.IsNullOrEmpty(report.Target))
                closed += $"Target: {report.Target}\n";
            if (report.Category == Shadps4ExitCategory.Unknown)
                closed += $"Exit code: {report.ExitCodeText}\n";
            return (AppMessageType.Info, closed, false);
        }

        private static string? TrimTailForDialog(string? logTail)
        {
            if (string.IsNullOrWhiteSpace(logTail)) return null;
            var lines = logTail.Split('\n');
            if (lines.Length <= DialogLogTailLines) return logTail;
            return string.Join("\n", lines, lines.Length - DialogLogTailLines, DialogLogTailLines)
                + "\n...";
        }
    }
}
