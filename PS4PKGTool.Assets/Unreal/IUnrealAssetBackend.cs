using PS4PKGTool.Assets.Abstractions;

namespace PS4PKGTool.Assets.Unreal;

/// <summary>
/// Neutral reference to one PAK entry. Offsets are relative to the pak file.
/// Compressed entries are self-describing: their data starts with a copy of
/// the record (including the block list); the block ranges locate the
/// compressed payloads relative to the entry offset (v5+) or absolute (v4).
/// </summary>
public sealed record PakEntryRef(
    string Path,
    long Offset,
    long Size,
    long UncompressedSize,
    string Compression,   // "None", "Zlib", "Gzip", "LZ4", "Oodle", "Unknown"
    bool Encrypted,
    IReadOnlyList<(long Start, long End)>? Blocks = null);

/// <summary>
/// Unreal parsing seam. The framework depends only on this interface; the
/// concrete implementation can be swapped (current: self-written PAK parser
/// verified against a real UE4 pak; a future CUE4Parse adapter would implement
/// the same contract). Oodle-compressed or encrypted entries surface as
/// graceful "requires ..." errors, never crashes.
/// </summary>
public interface IUnrealAssetBackend
{
    /// <summary>Human-readable parser identity, shown in metadata.</summary>
    string Name { get; }

    /// <summary>Enumerates the entries of a PAK file.</summary>
    IReadOnlyList<PakEntryRef> ListEntries(IAssetSource source);

    /// <summary>
    /// Opens a decompressed read stream for an entry. Throws
    /// UnsupportedAssetException for Oodle/encrypted entries (missing
    /// dependency) or entries over the decompression size cap.
    /// </summary>
    Stream OpenEntryStream(IAssetSource pakSource, PakEntryRef entry);
}
