using System;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>
    /// Structured outcome of a launched shadPS4 process, produced on the
    /// watcher thread and delivered to UI subscribers (which marshal to the
    /// UI thread themselves). Carries everything the UI needs to decide
    /// whether a dialog is warranted and what evidence exists.
    /// </summary>
    public sealed record Shadps4TerminationReport(
        /// <summary>The launched Title ID (CUSAxxxxx) or booted executable path, or null.</summary>
        string? Target,
        /// <summary>The core executable that ran.</summary>
        string Executable,
        string[] Arguments,
        /// <summary>Core file version ("0.17.0"), or null.</summary>
        string? CoreVersion,
        DateTime StartTimeUtc,
        TimeSpan Runtime,
        /// <summary>Process exit code as an unsigned value (sign of the signed int is not meaningful).</summary>
        uint ExitCode,
        Shadps4ExitCategory Category,
        /// <summary>Windows STATUS_ name for the exit code, or null when unmapped.</summary>
        string? StatusName,
        /// <summary>Tail of the emulator's own log, or null when none was found or readable.</summary>
        string? LogTail,
        /// <summary>Folder of a fresh WER report (or LocalDumps dump folder), or null when none found.</summary>
        string? WerReportFolder,
        /// <summary>An actual dump artifact (.dmp/.hdmp) inside the WER folder, or null.</summary>
        string? DumpFile,
        /// <summary>Path of the emulator log this session used, or null when none was found.</summary>
        string? LogPath)
    {
        /// <summary>"00:00:04.2" style runtime for logs and dialogs (no hyphens).</summary>
        public string RuntimeText
            => $"{(int)Runtime.TotalHours:00}:{Runtime.Minutes:00}:{Runtime.Seconds:00}.{Runtime.Milliseconds / 100}";

        /// <summary>"0xC0000005" style exit code, or with its STATUS_ name when mapped.</summary>
        public string ExitCodeText
            => string.IsNullOrEmpty(StatusName)
                ? $"0x{ExitCode:X8}"
                : $"0x{ExitCode:X8} ({StatusName})";
    }
}
