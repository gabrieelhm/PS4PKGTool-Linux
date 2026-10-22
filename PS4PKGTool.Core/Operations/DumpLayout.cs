namespace PS4PKGTool.Utilities.PkgInspection;

/// <summary>
/// Output layout for a full PKG extraction.
/// <see cref="Raw"/> keeps the engine's two-layer tree (Image0/ + Sc0/);
/// <see cref="Merged"/> produces the single game-dump folder the app shows.
/// </summary>
public enum ExtractLayout
{
    Raw = 0,
    Merged = 1,
}

/// <summary>Summary of a merge/flatten operation.</summary>
public sealed class FlattenResult
{
    public int Merged { get; init; }
    public int Conflicts { get; init; }
    public int Quarantined { get; init; }
}

/// <summary>
/// The single source of truth for the PS4 "dump" layout:
///
///   Image0/X        -> X            (app root)
///   Sc0/sce_sys/X   -> sce_sys/X    (already nested)
///   Sc0/X           -> sce_sys/X    (everything else lands under sce_sys)
///
/// On a path collision the Image0 (PFS) copy wins. A differing Sc0 copy is
/// never silently dropped: it is moved to <c>_flatten_conflicts/</c> (unless
/// <c>keepConflicts</c> is false, used by install flows that must not carry
/// diagnostics into the game). Nested Sc0 paths (trophy/, changeinfo/,
/// app/playgo-*) are preserved.
///
/// Iteration is sorted so precedence never depends on filesystem enumeration
/// order: the canonical <c>Sc0/sce_sys/</c> subtree is applied first.
/// </summary>
public static class DumpLayout
{
    public const string ConflictsDirectoryName = "_flatten_conflicts";

    /// <summary>Maps a raw PKG entry path to its logical (flattened) path.</summary>
    public static string MapToLogical(string realPath)
    {
        string p = realPath.Replace('\\', '/').TrimStart('/');
        if (p.Equals("Image0", StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        if (p.StartsWith("Image0/", StringComparison.OrdinalIgnoreCase))
            return p["Image0/".Length..];
        if (p.Equals("Sc0", StringComparison.OrdinalIgnoreCase))
            return "sce_sys";
        if (p.StartsWith("Sc0/", StringComparison.OrdinalIgnoreCase))
        {
            string rest = p["Sc0/".Length..];
            return IsUnderSceSys(rest) ? rest : "sce_sys/" + rest;
        }
        return p;
    }

    private static bool IsUnderSceSys(string rest)
        => rest.Equals("sce_sys", StringComparison.OrdinalIgnoreCase)
           || rest.StartsWith("sce_sys/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when <paramref name="logical"/> is the selection prefix itself or a
    /// descendant of it. An empty prefix is the synthetic game root and matches
    /// the whole package.
    /// </summary>
    public static bool IsUnderLogical(string logical, string prefix)
    {
        if (prefix.Length == 0) return true;
        if (logical.Equals(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        return logical.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The destination-relative path for a selected logical entry.
    /// With structure: the full logical path. Without: a selected file keeps
    /// just its name; a selected folder keeps its name with the subtree below.
    /// </summary>
    public static string DestinationRelative(string logical, string selection, bool preserveStructure)
    {
        if (preserveStructure)
            return logical;
        if (logical.Equals(selection, StringComparison.OrdinalIgnoreCase))
            return Path.GetFileName(selection);
        string leaf = Path.GetFileName(selection);
        string relative = logical.Length > selection.Length ? logical[(selection.Length + 1)..] : string.Empty;
        return relative.Length == 0 ? leaf : leaf + "/" + relative;
    }

    /// <summary>
    /// Resolves a logical destination under <paramref name="root"/> and rejects
    /// anything that would escape it (path traversal / absolute paths / mixed
    /// separators). Mirrors the engine's own extraction guard.
    /// </summary>
    public static string SafeDestination(string root, string relative)
    {
        string normalized = relative.Replace('/', Path.DirectorySeparatorChar);
        string full = Path.GetFullPath(Path.Combine(root, normalized));
        string rootFull = Path.GetFullPath(root);
        if (!rootFull.EndsWith(Path.DirectorySeparatorChar))
            rootFull += Path.DirectorySeparatorChar;
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Path traversal detected: '{relative}' resolves outside the extraction directory.");
        return full;
    }

    /// <summary>
    /// Flattens an already-extracted raw tree in place: Image0 contents move to
    /// <paramref name="folder"/>'s root, Sc0 contents move under sce_sys/, and
    /// both layer directories are removed when empty. Idempotent when already
    /// flat (neither layer present).
    /// </summary>
    /// <param name="keepConflicts">
    /// When true (default) a differing losing copy is preserved under
    /// <c>_flatten_conflicts/</c>. When false the loser is removed instead, so
    /// install flows do not carry diagnostic files into the game.
    /// </param>
    public static FlattenResult Flatten(string folder, bool keepConflicts = true)
    {
        int merged = 0, conflicts = 0, quarantined = 0;
        if (!Directory.Exists(folder))
            return new FlattenResult();

        string conflictsRoot = Path.Combine(folder, ConflictsDirectoryName);
        string image0 = Path.Combine(folder, "Image0");
        string sc0 = Path.Combine(folder, "Sc0");

        // ── Step 1: Image0/* -> root (Image0 wins over any pre-existing root file) ──
        if (Directory.Exists(image0))
        {
            foreach (string entry in SortedEntries(image0))
            {
                string name = Path.GetFileName(entry);
                MergeMove(entry, Path.Combine(folder, name), preferSource: true,
                    conflictsRoot, name, keepConflicts, ref merged, ref conflicts, ref quarantined);
            }
            TryDeleteEmptyDirectory(image0);
        }

        // ── Step 2: Sc0/* -> sce_sys/* (Image0 already present wins) ──
        if (Directory.Exists(sc0))
        {
            string sceSys = Path.Combine(folder, "sce_sys");
            Directory.CreateDirectory(sceSys);

            // The canonical Sc0/sce_sys subtree is applied first so a stray
            // Sc0/<name> duplicate (Sc0/param.sfo) can never win over it.
            string sc0SceSys = Path.Combine(sc0, "sce_sys");
            if (Directory.Exists(sc0SceSys))
            {
                foreach (string sub in SortedEntries(sc0SceSys))
                {
                    string subName = Path.GetFileName(sub);
                    MergeMove(sub, Path.Combine(sceSys, subName), preferSource: false,
                        conflictsRoot, "sce_sys/" + subName, keepConflicts, ref merged, ref conflicts, ref quarantined);
                }
                TryDeleteEmptyDirectory(sc0SceSys);
            }

            foreach (string entry in SortedEntries(sc0))
            {
                string name = Path.GetFileName(entry);
                if (name.Equals("sce_sys", StringComparison.OrdinalIgnoreCase))
                    continue; // handled above
                MergeMove(entry, Path.Combine(sceSys, name), preferSource: false,
                    conflictsRoot, "sce_sys/" + name, keepConflicts, ref merged, ref conflicts, ref quarantined);
            }
            TryDeleteEmptyDirectory(sc0);
        }

        if (!keepConflicts)
            TryDeleteDirectory(conflictsRoot);

        return new FlattenResult { Merged = merged, Conflicts = conflicts, Quarantined = quarantined };
    }

    private static string[] SortedEntries(string dir)
    {
        string[] entries = Directory.GetFileSystemEntries(dir);
        Array.Sort(entries, StringComparer.OrdinalIgnoreCase);
        return entries;
    }

    private static void MergeMove(string source, string dest, bool preferSource, string conflictsRoot,
        string logicalRelative, bool keepConflicts, ref int merged, ref int conflicts, ref int quarantined)
    {
        if (Directory.Exists(source))
        {
            if (Directory.Exists(dest))
            {
                foreach (string child in SortedEntries(source))
                {
                    string childName = Path.GetFileName(child);
                    MergeMove(child, Path.Combine(dest, childName), preferSource, conflictsRoot,
                        logicalRelative + "/" + childName, keepConflicts, ref merged, ref conflicts, ref quarantined);
                }
                TryDeleteEmptyDirectory(source);
                return;
            }
            if (File.Exists(dest))
            {
                // file/dir clash: existing wins, keep or drop the loser.
                conflicts++;
                if (keepConflicts) { Quarantine(source, Path.Combine(conflictsRoot, logicalRelative)); quarantined++; }
                else DropSource(source);
                return;
            }
            Directory.Move(source, dest);
            merged++;
            return;
        }

        // source is a file
        if (Directory.Exists(dest))
        {
            conflicts++;
            if (keepConflicts) { Quarantine(source, Path.Combine(conflictsRoot, logicalRelative)); quarantined++; }
            else DropSource(source);
            return;
        }
        if (!File.Exists(dest))
        {
            File.Move(source, dest);
            merged++;
            return;
        }
        if (preferSource)
        {
            File.Move(source, dest, overwrite: true);
            merged++;
            return;
        }
        // Existing (Image0) copy wins. Dedupe identical content; otherwise keep/drop the loser.
        if (FilesIdentical(source, dest))
        {
            File.Delete(source);
            return;
        }
        conflicts++;
        if (keepConflicts) { Quarantine(source, Path.Combine(conflictsRoot, logicalRelative)); quarantined++; }
        else DropSource(source);
    }

    private static void Quarantine(string source, string dest)
    {
        // Two losers can map to the same logical path (e.g. Sc0/param.sfo and
        // Sc0/sce_sys/param.sfo). Never skip: pick a unique destination so no
        // source is left behind and the layer directory can still be removed.
        string unique = dest;
        int suffix = 2;
        while (File.Exists(unique) || Directory.Exists(unique))
            unique = dest + "." + suffix++;

        string? parent = Path.GetDirectoryName(unique);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);

        if (Directory.Exists(source))
            Directory.Move(source, unique);
        else
            File.Move(source, unique);
    }

    private static void DropSource(string source)
    {
        if (Directory.Exists(source)) Directory.Delete(source, true);
        else if (File.Exists(source)) File.Delete(source);
    }

    private static bool FilesIdentical(string a, string b)
    {
        var fa = new FileInfo(a);
        var fb = new FileInfo(b);
        if (fa.Length != fb.Length)
            return false;
        using var sa = fa.OpenRead();
        using var sb = fb.OpenRead();
        byte[] ba = new byte[81920];
        byte[] bb = new byte[81920];
        while (true)
        {
            int ra = sa.Read(ba, 0, ba.Length);
            int rb = sb.Read(bb, 0, bb.Length);
            if (ra != rb)
                return false;
            if (ra == 0)
                return true;
            if (!ba.AsSpan(0, ra).SequenceEqual(bb.AsSpan(0, rb)))
                return false;
        }
    }

    private static void TryDeleteEmptyDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir);
        }
        catch
        {
            // best effort: a locked/leftover layer dir is harmless
        }
    }

    private static void TryDeleteDirectory(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        catch { /* best effort */ }
    }
}
