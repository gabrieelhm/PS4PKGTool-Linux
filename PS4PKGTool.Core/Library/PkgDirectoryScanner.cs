namespace PS4PKGTool.Utilities;

public sealed record PkgDirectoryScanIssue(string Path, string Message);
public sealed record PkgDirectoryScanResult(IReadOnlyList<string> Files, IReadOnlyList<PkgDirectoryScanIssue> Issues);

/// <summary>Production library discovery, extracted from the WinForms project without UI dependencies.</summary>
public static class PkgDirectoryScanner
{
    public static PkgDirectoryScanResult Scan(string root, bool recursive, IReadOnlyCollection<string>? excludedDirectoryNames = null, bool includeImmediateChildrenWhenNonRecursive = true, CancellationToken cancellationToken = default)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var issues = new List<PkgDirectoryScanIssue>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var pending = new Stack<(string Path, int Depth)>();
        var excluded = new HashSet<string>(excludedDirectoryNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        try { pending.Push((Path.GetFullPath(root), 0)); }
        catch (Exception ex) { issues.Add(new PkgDirectoryScanIssue(root, ex.Message)); return new PkgDirectoryScanResult([], issues); }
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (directory, depth) = pending.Pop(); if (!visited.Add(directory)) continue;
            try { foreach (var file in Directory.EnumerateFiles(directory, "*.PKG", SearchOption.TopDirectoryOnly)) files.Add(file); }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { issues.Add(new PkgDirectoryScanIssue(directory, ex.Message)); }
            if (!recursive && (depth >= 1 || !includeImmediateChildrenWhenNonRecursive)) continue;
            try { foreach (var child in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly)) { if (excluded.Contains(Path.GetFileName(child))) continue; try { if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0) pending.Push((Path.GetFullPath(child), depth + 1)); } catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { issues.Add(new PkgDirectoryScanIssue(child, ex.Message)); } } }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { issues.Add(new PkgDirectoryScanIssue(directory, ex.Message)); }
        }
        return new PkgDirectoryScanResult(files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray(), issues);
    }
}
