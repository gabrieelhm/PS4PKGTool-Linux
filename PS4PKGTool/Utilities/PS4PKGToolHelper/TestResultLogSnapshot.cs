using System;
using System.IO;
using System.Linq;

namespace PS4PKGTool.Utilities.PS4PKGToolHelper
{
    /// <summary>
    /// Copies a session log into PS4PKGTool-owned storage so a historical
    /// result keeps the log of THAT run even though shadPS4 reuses one
    /// shad_log.txt across sessions. shadPS4's own log is read-only: we copy,
    /// never modify, move or delete the original. Logs up to the snapshot cap
    /// are copied whole; larger ones keep the last 2 MB so a future report
    /// stays valid without unbounded storage.
    /// </summary>
    public static class TestResultLogSnapshot
    {
        private const long MaxSnapshotBytes = 2 * 1024 * 1024;

        /// <summary>
        /// Preserves the session log as &lt;resultId&gt;.log in the snapshots
        /// directory. Returns the snapshot path, or null when there is no
        /// source log / it cannot be read (the result can still be saved -
        /// the log is auxiliary, never mandatory).
        /// </summary>
        public static string? Preserve(string resultId, string? sourceLogPath, string? snapshotsDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(resultId) || string.IsNullOrWhiteSpace(sourceLogPath))
                return null;
            try
            {
                if (!File.Exists(sourceLogPath)) return null;

                string dir = snapshotsDirectory ?? GameFeedbackStore.LogSnapshotsDirectory;
                Directory.CreateDirectory(dir);
                string target = Path.Combine(dir, SanitizeFileName(resultId) + ".log");

                long length = new FileInfo(sourceLogPath).Length;
                if (length <= MaxSnapshotBytes)
                {
                    File.Copy(sourceLogPath, target, overwrite: true);
                    return target;
                }

                // Oversized log: keep the last 2 MB (tail is what a report
                // submission uses; the head is boot noise).
                using (var src = new FileStream(sourceLogPath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                using (var dst = new FileStream(target, FileMode.Create, FileAccess.Write))
                {
                    src.Position = length - MaxSnapshotBytes;
                    var buffer = new byte[64 * 1024];
                    int read;
                    while ((read = src.Read(buffer, 0, buffer.Length)) > 0)
                        dst.Write(buffer, 0, read);
                }
                return target;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Result ids are GUIDs, but never trust raw input as a file name.</summary>
        private static string SanitizeFileName(string value)
        {
            char[] bad = Path.GetInvalidFileNameChars();
            var chars = value.Select(c => bad.Contains(c) ? '_' : c).ToArray();
            return new string(chars);
        }
    }
}
