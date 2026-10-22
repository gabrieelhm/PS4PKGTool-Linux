using System;
using System.IO;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>Size and slot count of a managed save backup.</summary>
    public sealed record Shadps4BackupMeasurements(long TotalSize, int SlotCount);

    /// <summary>
    /// Read-only measurements of a managed save backup's data folder
    /// (snapshot\data is the copy of the live save folder). Kept separate
    /// from Shadps4SaveStore so its test seams (IClock / IFileSystemOps)
    /// are not dragged into display-only code. Never throws: a missing or
    /// unreadable backup measures as (0, 0).
    /// </summary>
    public static class Shadps4BackupMetrics
    {
        public static Shadps4BackupMeasurements Measure(string backupPath)
        {
            if (string.IsNullOrWhiteSpace(backupPath)) return new Shadps4BackupMeasurements(0, 0);

            string dataDir = Path.Combine(backupPath, "data");
            if (!Directory.Exists(dataDir)) return new Shadps4BackupMeasurements(0, 0);

            long total = 0;
            int slots = 0;
            foreach (string entry in SafeEnumerateEntries(dataDir))
            {
                // Mirrors the live-save slot semantics: every direct entry of
                // the save folder counts as a slot, dirs and loose files alike.
                slots++;
                total += Directory.Exists(entry) ? SafeDirectorySize(entry) : SafeFileSize(entry);
            }
            return new Shadps4BackupMeasurements(total, slots);
        }

        private static string[] SafeEnumerateEntries(string dir)
        {
            try { return Directory.GetFileSystemEntries(dir); }
            catch { return Array.Empty<string>(); }
        }

        private static long SafeDirectorySize(string dir)
        {
            long total = 0;
            try
            {
                foreach (string f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                    total += SafeFileSize(f);
            }
            catch { /* one unreadable entry never hides the rest */ }
            return total;
        }

        private static long SafeFileSize(string file)
        {
            try { return new FileInfo(file).Length; }
            catch { return 0; }
        }
    }
}
