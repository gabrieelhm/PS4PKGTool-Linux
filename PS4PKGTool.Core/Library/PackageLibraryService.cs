using PS4PKGTool.Utilities.PkgMeta;
using PS4PKGTool.Utilities;

namespace PS4PKGTool.Core.Library;

public sealed record PackageLibraryItem(
    string Title, string TitleId, string Version, string Category, string ContentId, string Region,
    long SizeBytes, string Path, string Status, byte[]? IconPng = null)
{
    public string Size => SizeBytes >= 1_073_741_824 ? $"{SizeBytes / 1_073_741_824d:0.00} GB" : $"{SizeBytes / 1_048_576d:0.0} MB";
}

public sealed record PackageScanIssue(string Path, string Message);
public sealed record PackageLibraryProgress(int Discovered, int Processed, string? CurrentPath);
public sealed record PackageLibraryScanResult(IReadOnlyList<PackageLibraryItem> Packages, IReadOnlyList<PackageScanIssue> Issues, bool IsCancelled);

public interface IPackageMetadataService
{
    Task<PackageLibraryItem> ReadAsync(string packagePath, bool includeIcon, CancellationToken cancellationToken = default);
}
public interface IPackageLibraryService
{
    Task<PackageLibraryScanResult> ScanAsync(IEnumerable<string> directories, bool recursive, IProgress<PackageLibraryProgress>? progress = null, CancellationToken cancellationToken = default);
}

/// <summary>Typed, UI-free adapter over the production PkgMetadataReader.</summary>
public sealed class PackageMetadataService : IPackageMetadataService
{
    public Task<PackageLibraryItem> ReadAsync(string packagePath, bool includeIcon, CancellationToken cancellationToken = default) =>
        Task.Run(() => Read(packagePath, includeIcon, cancellationToken), cancellationToken);

    private static PackageLibraryItem Read(string packagePath, bool includeIcon, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string fullPath = Path.GetFullPath(packagePath);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Package file was not found.", fullPath);
        var metadata = includeIcon ? PkgMetadataReader.ReadQuick(fullPath) : PkgMetadataReader.ReadMetadataOnly(fullPath);
        cancellationToken.ThrowIfCancellationRequested();
        string contentId = string.IsNullOrWhiteSpace(metadata.SfoContentId) ? metadata.Content_ID : metadata.SfoContentId;
        return new PackageLibraryItem(metadata.PS4_Title, metadata.TITLEID, metadata.APP_VER, metadata.PKG_Type.ToString(),
            contentId, metadata.Region, new FileInfo(fullPath).Length, fullPath, "Ready", metadata.Icon is null ? null : (byte[])metadata.Icon.Clone());
    }
}

/// <summary>Resilient directory scan that preserves the production one-directory-at-a-time behavior.</summary>
public sealed class PackageLibraryService : IPackageLibraryService
{
    private readonly IPackageMetadataService _metadata;
    public PackageLibraryService(IPackageMetadataService metadata) => _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

    public async Task<PackageLibraryScanResult> ScanAsync(IEnumerable<string> directories, bool recursive, IProgress<PackageLibraryProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(directories);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var issues = new List<PackageScanIssue>();
        try
        {
            foreach (var directory in directories.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var discovered = PkgDirectoryScanner.Scan(directory, recursive, cancellationToken: cancellationToken);
                foreach (var path in discovered.Files) paths.Add(path);
                foreach (var issue in discovered.Issues) issues.Add(new PackageScanIssue(issue.Path, issue.Message));
            }
            var packages = new List<PackageLibraryItem>();
            var ordered = paths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
            for (var index = 0; index < ordered.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string path = ordered[index];
                progress?.Report(new PackageLibraryProgress(ordered.Length, index, path));
                try { packages.Add(await _metadata.ReadAsync(path, includeIcon: false, cancellationToken).ConfigureAwait(false)); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
                { issues.Add(new PackageScanIssue(path, ex.Message)); }
            }
            progress?.Report(new PackageLibraryProgress(ordered.Length, ordered.Length, null));
            return new PackageLibraryScanResult(packages.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase).ToArray(), issues, false);
        }
        catch (OperationCanceledException) { return new PackageLibraryScanResult([], issues, true); }
    }

}

public static class PackageLibraryFilter
{
    public static IReadOnlyList<PackageLibraryItem> Apply(IEnumerable<PackageLibraryItem> items, string? query, string? category)
    {
        var filtered = items;
        if (!string.IsNullOrWhiteSpace(query)) { var needle = query.Trim(); filtered = filtered.Where(item => item.Title.Contains(needle, StringComparison.OrdinalIgnoreCase) || item.TitleId.Contains(needle, StringComparison.OrdinalIgnoreCase) || item.ContentId.Contains(needle, StringComparison.OrdinalIgnoreCase)); }
        if (!string.IsNullOrWhiteSpace(category) && category != "All") filtered = filtered.Where(item => string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase));
        return filtered.ToArray();
    }
}
