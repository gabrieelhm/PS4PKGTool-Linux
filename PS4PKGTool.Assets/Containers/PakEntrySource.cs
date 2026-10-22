using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Unreal;

namespace PS4PKGTool.Assets.Containers;

/// <summary>
/// One PAK entry as an IAssetSource. OpenRead() serves the DECOMPRESSED bytes
/// (decompression is bounded by the safety cap in PakCompression). Bounded
/// reads decompress once and slice the result - fine for browsing, revisit if
/// streaming becomes necessary.
/// </summary>
public sealed class PakEntrySource : IAssetSource
{
    private readonly IAssetSource _parent;
    private readonly IUnrealAssetBackend _backend;

    public PakEntryRef Entry { get; }

    public PakEntrySource(IAssetSource parent, IUnrealAssetBackend backend, PakEntryRef entry)
    {
        _parent = parent;
        _backend = backend;
        Entry = entry;
    }

    public string Name => Path.GetFileName(Entry.Path);

    public long Length => Entry.UncompressedSize;

    public string? SourceDescription => $"PAK entry ({Entry.Compression}) in {_parent.Name}";

    public Stream OpenRead() => _backend.OpenEntryStream(_parent, Entry);

    public Stream OpenRead(long offset, long length)
    {
        if (offset < 0 || length < 0 || offset + length > Length)
            throw new ArgumentOutOfRangeException(nameof(length));
        using var all = OpenRead();
        all.Position = offset;
        var buf = new byte[length];
        all.ReadExactly(buf);
        return new MemoryStream(buf, writable: false);
    }
}
