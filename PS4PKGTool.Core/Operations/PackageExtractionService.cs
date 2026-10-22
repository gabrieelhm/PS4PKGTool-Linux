using System.Text;

namespace PS4PKGTool.Utilities.PkgInspection;

/// <summary>Platform-neutral, in-process full PKG extraction service.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established production extraction API.")]
public interface IPackageExtractionService
{
    Task<(bool Succeeded, string Message)> ExtractFullAsync(string sourcePath, string destination,
        IProgress<string>? progress = null, CancellationToken ct = default,
        IProgress<(int Current, int Total, string CurrentFile)>? fileProgress = null,
        bool logCompletion = true, ExtractLayout layout = ExtractLayout.Raw);
}

public sealed class PkgExtractionService : IPackageExtractionService
{
    public const string DefaultPasscode = "00000000000000000000000000000000";
    public const string NoPasscode = "\x1";
    private readonly string _passcode;

    public PkgExtractionService(string? passcode = null) => _passcode = string.IsNullOrWhiteSpace(passcode) || passcode == NoPasscode
        ? DefaultPasscode : passcode;

    public async Task<(bool Succeeded, string Message)> ExtractFullAsync(string sourcePath, string destination,
        IProgress<string>? progress = null, CancellationToken ct = default,
        IProgress<(int Current, int Total, string CurrentFile)>? fileProgress = null, bool logCompletion = true,
        ExtractLayout layout = ExtractLayout.Raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        try
        {
            return layout == ExtractLayout.Merged
                ? await ExtractMergedAsync(sourcePath, destination, progress, fileProgress, ct).ConfigureAwait(false)
                : await ExtractRawAsync(sourcePath, destination, progress, fileProgress, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return (false, "Cancelled"); }
        catch (Exception ex) { return (false, "Extraction failed: " + ex.Message); }
    }

    /// <summary>Extracts the raw Image0/Sc0 tree in place (internal workflows).</summary>
    private async Task<(bool Succeeded, string Message)> ExtractRawAsync(string sourcePath, string destination,
        IProgress<string>? progress,
        IProgress<(int Current, int Total, string CurrentFile)>? fileProgress, CancellationToken ct)
    {
        progress?.Report("Preparing package...");
        Directory.CreateDirectory(destination);
        progress?.Report("Extracting game files...");
        var failures = await Task.Run(() =>
        {
            using var reader = new OrbisPkgTool.PkgReader(sourcePath, _passcode);
            return reader.ExtractAll(destination, MakeProgress(progress, ExtractLayout.Raw, fileProgress),
                new OrbisPkgTool.ExtractAllOptions { CancellationToken = ct });
        }, ct).ConfigureAwait(false);

        return failures.Count == 0 ? (true, string.Empty) : (false, FormatFailures(failures, ExtractLayout.Raw));
    }

    /// <summary>
    /// Extracts straight into the flattened dump layout (Image0 -> root, Sc0 -> sce_sys)
    /// without ever materialising the raw Image0/Sc0 folders. On a logical collision the
    /// Image0 copy wins. A file that already exists in the destination is never
    /// overwritten: the package copy is preserved under <c>_flatten_conflicts/</c>.
    /// </summary>
    private async Task<(bool Succeeded, string Message)> ExtractMergedAsync(string sourcePath, string destination,
        IProgress<string>? progress,
        IProgress<(int Current, int Total, string CurrentFile)>? fileProgress, CancellationToken ct)
    {
        progress?.Report("Preparing package...");
        Directory.CreateDirectory(destination);
        progress?.Report("Extracting game files...");

        var report = MakeProgress(progress, ExtractLayout.Merged, fileProgress);
        (int Extracted, int Preserved, List<OrbisPkgTool.ExtractionFailure> Failures) result = await Task.Run(() =>
        {
            using var reader = new OrbisPkgTool.PkgReader(sourcePath, _passcode);
            List<OrbisPkgTool.PkgFileEntry> entries = reader.ListFiles();

            // Flatten the listing: Image0 -> root, Sc0 -> sce_sys, Image0 wins collisions.
            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // logical -> real
            var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (OrbisPkgTool.PkgFileEntry entry in entries)
            {
                string real = entry.Path.Replace('\\', '/').TrimStart('/');
                string logical = DumpLayout.MapToLogical(real);
                if (logical.Length == 0) continue; // bare Image0 maps onto the root
                if (entry.IsDirectory) { dirs.Add(logical); continue; }
                bool image0 = real.StartsWith("Image0/", StringComparison.OrdinalIgnoreCase);
                if (!files.TryGetValue(logical, out string? existing)
                    || (image0 && !existing.StartsWith("Image0/", StringComparison.OrdinalIgnoreCase)))
                    files[logical] = real;
            }

            foreach (string dir in dirs)
                Directory.CreateDirectory(DumpLayout.SafeDestination(destination, dir));

            var failures = new List<OrbisPkgTool.ExtractionFailure>();
            int extracted = 0, preserved = 0, index = 0;
            List<string> logicalPaths = files.Keys.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (string logical in logicalPaths)
            {
                ct.ThrowIfCancellationRequested();
                report?.Report((index, logicalPaths.Count, logical));
                index++;
                string dest = DumpLayout.SafeDestination(destination, logical);
                try
                {
                    if (File.Exists(dest) || Directory.Exists(dest))
                    {
                        // Pre-existing destination item: never overwrite; keep the package copy.
                        reader.ExtractFileTo(files[logical], UniqueConflictPath(destination, logical));
                        preserved++;
                    }
                    else
                    {
                        reader.ExtractFileTo(files[logical], dest);
                        extracted++;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { failures.Add(new OrbisPkgTool.ExtractionFailure(logical, ex)); }
            }
            return (extracted, preserved, failures);
        }, ct).ConfigureAwait(false);

        string message = result.Preserved > 0
            ? $"{result.Preserved} existing file(s) preserved under {DumpLayout.ConflictsDirectoryName}/."
            : string.Empty;
        if (result.Failures.Count > 0)
        {
            string failure = FormatFailures(result.Failures, ExtractLayout.Merged);
            return (false, string.IsNullOrEmpty(message) ? failure : failure + "\n" + message);
        }
        return (true, message);
    }

    /// <summary>Unique destination under the destination's <c>_flatten_conflicts/</c> folder.</summary>
    private static string UniqueConflictPath(string destination, string logical)
    {
        string conflictsRoot = Path.Combine(destination, DumpLayout.ConflictsDirectoryName);
        string path = DumpLayout.SafeDestination(conflictsRoot, logical);
        if (!File.Exists(path) && !Directory.Exists(path)) return path;
        int suffix = 2;
        string candidate;
        do { candidate = path + "." + suffix++; } while (File.Exists(candidate) || Directory.Exists(candidate));
        return candidate;
    }

    private static IProgress<(int Current, int Total, string CurrentFile)>? MakeProgress(
        IProgress<string>? progress, ExtractLayout layout,
        IProgress<(int Current, int Total, string CurrentFile)>? fileProgress)
    {
        if (fileProgress != null)
            return fileProgress;
        if (progress == null)
            return null;
        return new Progress<(int Current, int Total, string File)>(p =>
        {
            string label = layout == ExtractLayout.Merged ? DumpLayout.MapToLogical(p.File) : p.File;
            progress.Report(p.Total > 0 ? $"Extracting {p.Current + 1}/{p.Total}: {label}" : $"Extracting {label}");
        });
    }

    private static string FormatFailures(IReadOnlyList<OrbisPkgTool.ExtractionFailure> failures, ExtractLayout layout)
    {
        string Label(string path) => layout == ExtractLayout.Merged ? DumpLayout.MapToLogical(path) : path;
        string summary = string.Join("\n", failures.Take(5).Select(f => $"{Label(f.Path)}: {f.Exception.Message}"));
        if (failures.Count > 5) summary += $"\n... and {failures.Count - 5} more";
        return $"{failures.Count} {(failures.Count == 1 ? "entry" : "entries")} failed to extract:\n{summary}";
    }

    public static string SanitizeFolderName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "_";
        char[] invalid = Path.GetInvalidFileNameChars();
        var result = new StringBuilder(name.Length);
        foreach (char c in name)
            result.Append(SafeSubstitutes.TryGetValue(c, out char substitute) ? substitute : char.IsControl(c) || Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        string sanitized = result.ToString().TrimEnd('.', ' ');
        string upper = sanitized.ToUpperInvariant();
        if (upper is "CON" or "PRN" or "AUX" or "NUL" || (upper.Length == 4 && (upper.StartsWith("COM", StringComparison.Ordinal) || upper.StartsWith("LPT", StringComparison.Ordinal)) && upper[3] is >= '1' and <= '9')) sanitized = "_" + sanitized;
        return string.IsNullOrEmpty(sanitized) ? "_" : sanitized;
    }

    private static readonly Dictionary<char, char> SafeSubstitutes = new()
    {
        ['<'] = '＜', ['>'] = '＞', [':'] = '：', ['\"'] = '＂', ['/'] = '／', ['\\'] = '＼', ['|'] = '｜', ['?'] = '？', ['*'] = '＊'
    };
}
