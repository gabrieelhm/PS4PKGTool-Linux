namespace PS4PKGTool.Utilities.PkgInspection;

/// <summary>
/// Maps between the raw PKG entry paths (<c>Image0/…</c>, <c>Sc0/…</c>) and the
/// logical flattened paths the file browser shows. One logical path can have
/// several real sources (Image0 and Sc0); Image0 is always listed first so it
/// resolves as the winning copy, matching <see cref="DumpLayout"/>.
/// </summary>
public sealed class PkgPathMap
{
    private readonly Dictionary<string, List<string>> _byLogical =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _realToLogical =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers one raw entry against its logical path.</summary>
    public void Add(string realPath, string logicalPath)
    {
        if (string.IsNullOrEmpty(realPath))
            return;

        _realToLogical[realPath] = logicalPath;

        if (!_byLogical.TryGetValue(logicalPath, out List<string>? list))
        {
            list = new List<string>();
            _byLogical[logicalPath] = list;
        }
        // Image0 (PFS) wins: keep it ahead of any Sc0 source.
        if (realPath.StartsWith("Image0/", StringComparison.OrdinalIgnoreCase))
            list.Insert(0, realPath);
        else
            list.Add(realPath);
    }

    /// <summary>Converts a raw entry path back to its logical path.</summary>
    public string ToLogical(string realPath)
        => _realToLogical.TryGetValue(realPath, out string? logical) ? logical : realPath;

    /// <summary>The winning real path for a logical file (Image0 preferred).</summary>
    public bool TryResolveFile(string logicalPath, out string realPath)
    {
        if (_byLogical.TryGetValue(logicalPath, out List<string>? list) && list.Count > 0)
        {
            realPath = list[0];
            return true;
        }
        realPath = logicalPath;
        return false;
    }

    /// <summary>Every real source that maps to a logical path.</summary>
    public IReadOnlyList<string> RealSources(string logicalPath)
        => _byLogical.TryGetValue(logicalPath, out List<string>? list)
            ? list
            : Array.Empty<string>();

    /// <summary>All logical path → real sources pairs.</summary>
    public IEnumerable<KeyValuePair<string, List<string>>> Entries => _byLogical;

    /// <summary>True when the logical path (or a real source of it) is present.</summary>
    public bool Contains(string logicalPath) => _byLogical.ContainsKey(logicalPath);
}
