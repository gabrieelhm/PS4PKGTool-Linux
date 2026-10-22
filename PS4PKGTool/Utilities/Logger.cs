using Microsoft.Extensions.Logging;
using PS4PKGTool.Utilities.PS4PKGToolHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PS4PKGTool.Utilities
{
    class Logger
    {
        public static string LogFilename { get; set; }
        public static Action<string> OnLog;

        /// <summary>
        /// Normalizes mixed line endings to CRLF: multi-line messages (e.g.
        /// the emulator log tail) arrive with LF-only breaks, exception text
        /// with CRLF. The log file and the Log tab must render every break.
        /// </summary>
        public static string NormalizeNewlines(string text)
            => text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");

        public static void FlushLog()
        {
            File.WriteAllText(Helper.PS4PKGToolLogFile, string.Empty);
        }

        private static object lockObject = new object(); // For thread safety

        // Fallback flag: set once when the portable log file can no longer
        // be written (read-only install dir, locked file, full disk...). The
        // logger then retries every write in %TEMP% and tells the Log tab
        // once where the log lives now - logging must never fail silently.
        private static string _fallbackLogPath;
        private static bool _fallbackAnnounced;

        /// <summary>
        /// Overrides the %TEMP% fallback location. The test process runs from
        /// a bin dir without an AppData\ folder, so EVERY log write fails and
        /// engages the fallback - without this hook the whole suite would
        /// dump its output into the real %TEMP%. Never set in production.
        /// </summary>
        internal static string Test_FallbackDirectory
        {
            set => _fallbackDirectoryOverride = value;
        }
        private static string _fallbackDirectoryOverride;

        public static void LogInformation(string msg)
        {
            Log(LogLevel.Information, msg);
        }

        public static void LogWarning(string msg)
        {
            Log(LogLevel.Warning, msg);
        }

        public static void LogError(string msg, Exception ex = null)
        {
            Log(LogLevel.Error, msg, ex);
        }

        private static void Log(LogLevel level, string msg, Exception ex = null)
        {
            if (string.IsNullOrEmpty(msg))
                return;

            try
            {
                DateTime now = DateTime.Now;
                string shortLabel = level.ToString().Replace("Information", "INFO").Replace("Warning","WARN").Replace("Error","ERR");
                string logMessage = $"{now:G} : [{shortLabel}] {msg}";
                string displayMessage = $"{now:HH:mm:ss}  [{shortLabel}] {msg}";

                if (ex != null)
                {
                    logMessage += Environment.NewLine + ex.ToString();
                    displayMessage += " " + ex.Message;
                }

                bool fileWriteOk = true;
                lock (lockObject)
                {
                    if (_fallbackLogPath == null)
                    {
                        try
                        {
                            using (var sw = new StreamWriter(Helper.PS4PKGToolLogFile, true))
                            {
                                sw.WriteLine(NormalizeNewlines(logMessage));
                            }
                        }
                        catch
                        {
                            fileWriteOk = false;
                            _fallbackLogPath = Path.Combine(
                                _fallbackDirectoryOverride ?? Path.GetTempPath(),
                                "PS4PKGTool-fallback.log");
                        }
                    }

                    if (_fallbackLogPath != null)
                    {
                        // Best-effort: if even %TEMP% is unwritable there is
                        // nowhere left to write - the Log tab is the only sink.
                        try
                        {
                            using (var sw = new StreamWriter(_fallbackLogPath, true))
                            {
                                sw.WriteLine(NormalizeNewlines(logMessage));
                            }
                        }
                        catch { }
                    }
                }

                if (!fileWriteOk && !_fallbackAnnounced)
                {
                    _fallbackAnnounced = true;
                    // Announce on the Log tab exactly once: where the log went.
                    try { OnLog?.Invoke($"[WARN] Log file unavailable - falling back to {_fallbackLogPath}"); }
                    catch { }
                }

                OnLog?.Invoke(displayMessage);
            }
            catch (Exception)
            {
                // Last resort: never let logging throw. The display pipeline
                // or a broken fallback would otherwise take the app down.
            }
        }

        // ── test seam ────────────────────────────────────────────────────
        // The fallback state is process-static. Tests reset it between runs
        // and redirect the fallback path so they never touch the real %TEMP%
        // fallback. Guarded by InternalsVisibleTo - never called in production.
        internal static void Test_ResetFallback()
        {
            _fallbackLogPath = null;
            _fallbackAnnounced = false;
        }

        internal static string Test_FallbackLogPath => _fallbackLogPath;
        internal static bool Test_FallbackAnnounced => _fallbackAnnounced;
    }
}
