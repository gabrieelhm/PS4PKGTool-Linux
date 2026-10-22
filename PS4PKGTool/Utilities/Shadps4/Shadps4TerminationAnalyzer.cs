using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PS4PKGTool.Utilities.Shadps4
{
    public sealed class Shadps4TerminationAnalyzer
    {
        private const int MaxLogBytes = 64 * 1024;
        private const int MaxLogLines = 200;
        private const int PollAttempts = 2;
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(300);

        private static readonly TimeSpan FreshBeforeExit = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan FreshAfterExit = TimeSpan.FromSeconds(30);

        private readonly IClock _clock;
        private readonly IDelay _delay;

        public Shadps4TerminationAnalyzer(IClock clock, IDelay delay)
        {
            _clock = clock;
            _delay = delay;
        }

        public Func<string?> LocalDumpsFolderResolver { get; set; } = ReadConfiguredLocalDumpsFolder;
        public Func<IReadOnlyList<string>> WerRootsResolver { get; set; } = DefaultWerRoots;
        public Func<string?, string?> LogPathResolver { get; set; } = ResolveLogPathBestEffort;

        public async Task<Shadps4TerminationReport> AnalyzeAsync(
            string? target, string executable, string[] arguments, string? coreVersion,
            string? userDirectory, DateTime startTimeUtc, DateTime exitTimeUtc,
            int exitCode, CancellationToken cancellationToken = default)
        {
            uint code = unchecked((uint)exitCode);
            var category = Shadps4ExitStatus.Classify(code);

            string? logPath = LogPathResolver(userDirectory);
            string? logTail = ReadLogTail(logPath);
            var wer = await FindWerEvidenceAsync(exitTimeUtc, cancellationToken).ConfigureAwait(false);

            return new Shadps4TerminationReport(
                target, executable, arguments, coreVersion,
                startTimeUtc, exitTimeUtc - startTimeUtc,
                code, category, Shadps4ExitStatus.StatusName(code),
                logTail, wer?.Folder, wer?.DumpFile, logPath);
        }

        private async Task<(string Folder, string? DumpFile)?> FindWerEvidenceAsync(
            DateTime exitTimeUtc, CancellationToken cancellationToken)
        {
            for (int attempt = 0; attempt < PollAttempts; attempt++)
            {
                if (attempt > 0)
                    await _delay.DelayAsync(PollInterval, cancellationToken).ConfigureAwait(false);
                var found = FindWerEvidenceOnce(exitTimeUtc);
                if (found != null) return found;
            }
            return null;
        }

        private (string Folder, string? DumpFile)? FindWerEvidenceOnce(DateTime exitTimeUtc)
        {
            DateTime now = _clock.UtcNow;

            string? localDumps = null;
            try { localDumps = LocalDumpsFolderResolver(); } catch { }
            if (!string.IsNullOrWhiteSpace(localDumps) && Directory.Exists(localDumps))
            {
                try
                {
                    foreach (string file in Directory.EnumerateFiles(localDumps, "*shad*"))
                    {
                        if (IsFresh(LastWriteUtc(file), exitTimeUtc, now))
                            return (localDumps, file);
                    }
                }
                catch { }
            }

            foreach (string root in WerRootsResolver())
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) continue;
                try
                {
                    foreach (string dir in Directory.EnumerateDirectories(root, "AppCrash_shadPS4*"))
                    {
                        if (!IsFresh(LastWriteUtc(dir), exitTimeUtc, now)) continue;
                        string? dump = FindDumpArtifact(dir);
                        return (dir, dump);
                    }
                }
                catch { }
            }

            return null;
        }

        private static bool IsFresh(DateTime lastWriteUtc, DateTime exitTimeUtc, DateTime now)
            => lastWriteUtc >= exitTimeUtc - FreshBeforeExit && lastWriteUtc <= now + FreshAfterExit;

        private static string? FindDumpArtifact(string reportFolder)
        {
            try
            {
                foreach (string pattern in new[] { "*.hdmp", "*.dmp", "core.*" })
                {
                    string? first = Directory.EnumerateFiles(reportFolder, pattern)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();
                    if (first != null) return first;
                }
            }
            catch { }
            return null;
        }

        private static DateTime LastWriteUtc(string path)
        {
            try { return Directory.GetLastWriteTimeUtc(path); }
            catch { return DateTime.MinValue; }
        }

        internal static string? ReadConfiguredLocalDumpsFolder()
        {
            if (Directory.Exists("/var/lib/systemd/coredump"))
                return "/var/lib/systemd/coredump";
            return null;
        }

        private static IReadOnlyList<string> DefaultWerRoots()
        {
            return Array.Empty<string>();
        }

        internal static string? ResolveLogPathBestEffort(string? userDirectory)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(userDirectory))
            {
                candidates.Add(Path.Combine(userDirectory, "log", "shad_log.txt"));
                candidates.Add(Path.Combine(userDirectory, "shad_log.txt"));
            }
            string localShare = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            candidates.Add(Path.Combine(localShare, "shadPS4", "log", "shad_log.txt"));
            candidates.Add(Path.Combine(localShare, "shadPS4", "shad_log.txt"));
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            candidates.Add(Path.Combine(appData, "shadPS4", "log", "shad_log.txt"));
            candidates.Add(Path.Combine(appData, "shadPS4", "shad_log.txt"));

            foreach (string candidate in candidates)
            {
                try
                {
                    if (File.Exists(candidate)) return candidate;
                }
                catch { }
            }
            return null;
        }

        private static string? ReadLogTail(string? logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath)) return null;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete);
                    long length = fs.Length;
                    long start = Math.Max(0, length - MaxLogBytes);
                    fs.Position = start;
                    using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    if (start > 0)
                    {
                        int c = sr.Read();
                        while (c >= 0 && c != '\n') c = sr.Read();
                    }
                    string tail = sr.ReadToEnd();
                    if (tail.Length == 0) return "";
                    var lines = tail.Split('\n')
                        .Select(l => l.TrimEnd('\r'))
                        .Where(l => l.Length > 0)
                        .ToList();
                    if (lines.Count > MaxLogLines)
                        lines = lines.GetRange(lines.Count - MaxLogLines, MaxLogLines);
                    return string.Join("\n", lines);
                }
                catch { }
            }
            return null;
        }
    }
}
