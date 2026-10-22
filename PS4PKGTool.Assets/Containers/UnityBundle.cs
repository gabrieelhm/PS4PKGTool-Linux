using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Errors;
using PS4PKGTool.Assets.IO;

namespace PS4PKGTool.Assets.Containers;

/// <summary>
/// Unity asset bundle container (UnityFS / UnityRaw / UnityWeb). Parses the
/// header + blocks info (start or end of file), lists member files, and serves
/// each member as a raw IAssetSource slice.
///
/// Phase 3a scope: uncompressed bundles are fully supported. Compressed blocks
/// (LZMA/LZ4) surface as UnsupportedAssetException on member open — the LZ4
/// path lands with the AssetStudio adapter spike.
/// </summary>
public sealed class UnityBundle
{
    public const string MagicFs = "UnityFS\0";
    public const string MagicRaw = "UnityRaw\0";
    public const string MagicWeb = "UnityWeb\0";
    private const int BlocksInfoCap = 16 * 1024 * 1024;

    public sealed class Member
    {
        public required string Name { get; init; }
        public long Offset { get; init; }
        public long Size { get; init; }
        public uint Flags { get; init; }
    }

    public required uint Version { get; init; }
    public string? UnityVersion { get; init; }
    public required IReadOnlyList<Member> Files { get; init; }
    public bool AnyCompressedBlocks { get; init; }

    /// <summary>File offset where the data section begins. Member offsets are
    /// relative to this (not to the file start).</summary>
    public long DataOffset { get; init; }

    public static bool HasUnityMagic(ReadOnlySpan<byte> head)
        => head.Length >= 8
           && head[0] == 'U' && head[1] == 'n' && head[2] == 'i' && head[3] == 't'
           && head[4] == 'y' && (head[5] == 'F' || head[5] == 'R' || head[5] == 'W');

    public static UnityBundle Parse(IAssetSource source)
    {
        long probe = Math.Min(source.Length, 256);
        byte[] head;
        using (var s = source.OpenRead(0, probe)) { head = new byte[s.Length]; s.ReadExactly(head); }

        if (!HasUnityMagic(head)) throw new CorruptAssetException("Missing Unity bundle magic.");
        if (head.Length < 16) throw new CorruptAssetException("Unity bundle header truncated.");

        int pos = 8;
        uint version = BitConverter.ToUInt32(head, pos); pos += 4;

        string? unityVersion = null;
        if (version >= 6)
        {
            int len = BitConverter.ToInt32(head, pos); pos += 4;
            if (len > 0 && pos + len <= head.Length)
            {
                unityVersion = System.Text.Encoding.UTF8.GetString(head, pos, len);
                pos += len;
            }
        }

        uint flags;
        if (version >= 7)
        {
            if (pos + 24 > head.Length) throw new CorruptAssetException("Unity bundle header truncated.");
            pos += 4;                          // header size
            pos += 8;                          // file size
            pos += 8;                          // compressed + uncompressed block sizes
            flags = BitConverter.ToUInt32(head, pos); pos += 4;
        }
        else if (version >= 6)
        {
            if (pos + 20 > head.Length) throw new CorruptAssetException("Unity bundle header truncated.");
            pos += 4; pos += 4; pos += 8;
            flags = BitConverter.ToUInt32(head, pos); pos += 4;
        }
        else
        {
            if (pos + 16 > head.Length) throw new CorruptAssetException("Unity bundle header truncated.");
            pos += 4; pos += 8;
            flags = BitConverter.ToUInt32(head, pos); pos += 4;
        }

        bool blocksAtEnd = (flags & 0x20) != 0;
        byte[] info;
        if (!blocksAtEnd)
        {
            long infoLen = Math.Min(source.Length - pos, BlocksInfoCap);
            using var s = source.OpenRead(pos, infoLen);
            info = new byte[s.Length]; s.ReadExactly(info);
        }
        else
        {
            // Blocks info sits at the END: the last 4 bytes are the file count;
            // walk the file table backward from there.
            long tailLen = Math.Min(source.Length, BlocksInfoCap);
            using (var s = source.OpenRead(source.Length - tailLen, tailLen))
            {
                var tail = new byte[s.Length];
                s.ReadExactly(tail);
                info = tail;
            }
        }

        return ParseBlocksInfo(info, blocksAtEnd, version);
    }

    private static UnityBundle ParseBlocksInfo(byte[] info, bool blocksAtEnd, uint version)
    {
        if (info.Length < 8) throw new CorruptAssetException("Unity bundle blocks info truncated.");

        int p = 0;
        long dataOffset = BitConverter.ToUInt32(info, p); p += 4;
        int blockCount = BitConverter.ToInt32(info, p); p += 4;
        if (blockCount < 0 || blockCount > 4096) throw new CorruptAssetException("Unity bundle has an invalid block count.");
        if (p + blockCount * 10 > info.Length) throw new CorruptAssetException("Unity bundle block table truncated.");

        bool anyCompressed = false;
        for (int i = 0; i < blockCount; i++)
        {
            p += 8; // uncompressed size + compressed size
            ushort blockFlags = BitConverter.ToUInt16(info, p); p += 2;
            if ((blockFlags & 0x3F) != 0) anyCompressed = true; // compression type bits
        }

        int fileCount;
        int fileTableStart;
        if (!blocksAtEnd)
        {
            if (p + 4 > info.Length) throw new CorruptAssetException("Unity bundle file count missing.");
            fileCount = BitConverter.ToInt32(info, p); p += 4;
            fileTableStart = p;
        }
        else
        {
            // Last 4 bytes = file count; walk each record backward.
            if (info.Length < 4) throw new CorruptAssetException("Unity bundle blocks info truncated.");
            fileCount = BitConverter.ToInt32(info, info.Length - 4);
            if (fileCount < 0 || fileCount > 65536) throw new CorruptAssetException("Unity bundle has an invalid file count.");

            int q = info.Length - 4;
            var entries = new (int nameStart, int nameLen, long off, long size, uint fileFlags)[fileCount];
            for (int i = fileCount - 1; i >= 0; i--)
            {
                if (version >= 7)
                {
                    if (q - 4 < 0) throw new CorruptAssetException("Unity bundle file entry truncated.");
                    uint fileFlags = BitConverter.ToUInt32(info, q - 4); q -= 4;
                    entries[i] = entries[i] with { fileFlags = fileFlags };
                }
                if (q - 8 < 0) throw new CorruptAssetException("Unity bundle file entry truncated.");
                long size = BitConverter.ToUInt32(info, q - 4); q -= 4;
                long off = BitConverter.ToUInt32(info, q - 4); q -= 4;
                if (q - 4 < 0) throw new CorruptAssetException("Unity bundle file name truncated.");
                int nameLen = BitConverter.ToInt32(info, q - 4); q -= 4;
                if (nameLen < 0 || nameLen > 4096 || q - nameLen < 0) throw new CorruptAssetException("Unity bundle file name truncated.");
                entries[i] = (q - nameLen, nameLen, off, size, entries[i].fileFlags);
                q -= nameLen;
            }

            var files = new List<Member>(fileCount);
            foreach (var (nameStart, nameLen, off, size, fileFlags) in entries)
            {
                files.Add(new Member
                {
                    Name = System.Text.Encoding.UTF8.GetString(info, nameStart, nameLen),
                    Offset = off,
                    Size = size,
                    Flags = fileFlags,
                });
            }
            return new UnityBundle { Version = version, UnityVersion = null, Files = files, AnyCompressedBlocks = anyCompressed, DataOffset = dataOffset };
        }

        // Forward layout (blocks at start): fixed-size file table.
        int entrySize = version >= 7 ? 16 : 12; // nameLen+name + off + size (+flags)
        if (fileTableStart + (long)fileCount * entrySize > info.Length)
            throw new CorruptAssetException("Unity bundle file table truncated.");

        var fwd = new List<Member>(fileCount);
        int r = fileTableStart;
        for (int i = 0; i < fileCount; i++)
        {
            if (r + 4 > info.Length) throw new CorruptAssetException("Unity bundle file table truncated.");
            int nameLen = BitConverter.ToInt32(info, r); r += 4;
            if (nameLen < 0 || nameLen > 4096 || r + nameLen > info.Length) throw new CorruptAssetException("Unity bundle file name truncated.");
            string name = System.Text.Encoding.UTF8.GetString(info, r, nameLen); r += nameLen;
            int tail = version >= 7 ? 16 : 12; // off + size (+ flags)
            if (r + tail > info.Length) throw new CorruptAssetException("Unity bundle file table truncated.");
            uint off = BitConverter.ToUInt32(info, r); r += 4;
            uint size = BitConverter.ToUInt32(info, r); r += 4;
            uint fileFlags = version >= 7 ? BitConverter.ToUInt32(info, r) : 0;
            if (version >= 7) r += 4;
            fwd.Add(new Member { Name = name, Offset = off, Size = size, Flags = fileFlags });
        }
        return new UnityBundle { Version = version, UnityVersion = null, Files = fwd, AnyCompressedBlocks = anyCompressed, DataOffset = dataOffset };
    }

    public IAssetSource OpenEntry(IAssetSource containerSource, Member member)
    {
        if ((long)member.Offset + member.Size > containerSource.Length)
            throw new CorruptAssetException($"Unity bundle member '{member.Name}' lies outside the container.");
        if (AnyCompressedBlocks)
            throw new UnsupportedAssetException("This Unity bundle uses compressed blocks (LZMA/LZ4) - compression support is not in yet.");
        return new UnityBundleEntrySource(containerSource, member, DataOffset);
    }
}

/// <summary>A Unity bundle member served as a raw IAssetSource slice.</summary>
public sealed class UnityBundleEntrySource : IAssetSource
{
    private readonly IAssetSource _container;
    private readonly UnityBundle.Member _member;
    private readonly long _dataOffset;

    internal UnityBundleEntrySource(IAssetSource container, UnityBundle.Member member, long dataOffset)
    {
        _container = container;
        _member = member;
        _dataOffset = dataOffset;
        Name = member.Name;
        Length = member.Size;
        SourceDescription = "Unity bundle member";
    }

    public string Name { get; }
    public long Length { get; }
    public string? SourceDescription { get; }

    public Stream OpenRead() => _container.OpenRead(_dataOffset + _member.Offset, _member.Size);

    public Stream OpenRead(long offset, long length)
    {
        if (offset < 0 || length < 0 || offset + length > Length)
            throw new ArgumentOutOfRangeException(nameof(length));
        return _container.OpenRead(_dataOffset + _member.Offset + offset, length);
    }
}
