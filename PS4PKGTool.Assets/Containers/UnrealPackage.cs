using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Errors;

namespace PS4PKGTool.Assets.Containers;

/// <summary>
/// Unreal Engine package (.uasset / .uexp) reader, written against real bytes
/// and verified with a standard UE4 package from Bee Simulator (magic at
/// offset 0, unversioned, uexp-split export data).
///
/// Layout (little-endian):
///   - FPackageFileSummary (may follow a game-specific preamble - the magic is
///     located by scanning the first 256 bytes).
///   - Name table: {int32 byteLen incl null, UTF-8 + null, uint32 hash}.
///   - Export table: 40-byte records {Class, Super, Template, Outer, Name(8),
///     Flags, SerialSize(8), SerialOffset(8)}; SerialOffset is relative to
///     TotalHeaderSize and addresses the .uexp companion.
///   - Export object data: unversioned properties followed by the cooked
///     platform data (FTexturePlatformData / FTexture2DMipMap / FByteBulkData).
/// </summary>
public sealed class UnrealPackage
{
    public const uint PackageMagic = 0x9E2A83C1;

    private const int MaxNames = 5_000_000;
    private const int MaxExports = 5_000_000;

    public sealed class Summary
    {
        public required int TotalHeaderSize { get; init; }
        public required int NameCount { get; init; }
        public required int ExportCount { get; init; }
        public required int ExportOffset { get; init; }
        public required int ImportCount { get; init; }
        public required int ImportOffset { get; init; }
        public int NameOffset { get; init; }
        public required bool HasPreamble { get; init; }
    }

    public sealed class ObjectExport
    {
        public required int ClassIndex { get; init; }
        public required int SuperIndex { get; init; }
        public required int TemplateIndex { get; init; }
        public required int OuterIndex { get; init; }
        public required int NameIndex { get; init; }
        public required long SerialSize { get; init; }
        public required long SerialOffset { get; init; }
    }

    public sealed class Texture2DInfo
    {
        public required string Name { get; init; }
        public required int SizeX { get; init; }
        public required int SizeY { get; init; }
        public required string PixelFormat { get; init; }
        public required int MipCount { get; init; }
        public required int Mip0SizeX { get; init; }
        public required int Mip0SizeY { get; init; }
        public byte[]? Mip0Data { get; init; }
    }

    public static bool IsLikelyPackage(ReadOnlySpan<byte> head)
    {
        if (head.Length < 64) return false;
        // The magic can sit at offset 0 (standard) or after a preamble (game-specific).
        for (int i = 0; i + 4 <= Math.Min(head.Length, 256); i++)
        {
            if (BitConverter.ToUInt32(head.Slice(i, 4)) == PackageMagic) return true;
        }
        return false;
    }

    /// <summary>Parses the summary; the magic is located by scanning the first 256 bytes.</summary>
    public static Summary ParseSummary(IAssetSource source)
    {
        try
        {
            return ParseSummaryCore(source);
        }
        catch (CorruptAssetException) { throw; }
        catch (UnsupportedAssetException) { throw; }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            // Raw BitConverter reads on a truncated/short file land here; surface
            // a structured error instead of an unhandled framework exception.
            throw new CorruptAssetException("Package summary is malformed or truncated.");
        }
    }

    private static Summary ParseSummaryCore(IAssetSource source)
    {
        byte[] head;
        using (var s = source.OpenRead(0, Math.Min(source.Length, 512)))
        {
            head = new byte[s.Length];
            s.ReadExactly(head);
        }

        int magicPos = -1;
        for (int i = 0; i + 4 <= head.Length; i++)
        {
            if (BitConverter.ToUInt32(head, i) == PackageMagic) { magicPos = i; break; }
        }
        if (magicPos < 0) throw new CorruptAssetException("Missing package magic.");

        int pos = magicPos + 4;
        int legacy = ReadI32(head, ref pos);          // -7 = modern, unversioned
        if (legacy >= 0) throw new UnsupportedAssetException($"Legacy UE3 package (version {legacy}) is not supported.");
        _ = ReadI32(head, ref pos);                   // UE3 version
        int ue4ver = ReadI32(head, ref pos);          // UE4 version (0 = unversioned)
        _ = ReadI32(head, ref pos);                   // licensee version

        int customVersionCount = ReadI32(head, ref pos);
        if (customVersionCount > 0 && customVersionCount < 128
            && pos + (long)customVersionCount * 20 <= head.Length)
            pos += customVersionCount * 20;           // per version: FGuid(16) + int32

        int totalHeaderSize = ReadI32(head, ref pos);
        int pkgNameLen = ReadI32(head, ref pos);
        if (pkgNameLen > 0 && pkgNameLen < 1024
            && pos + ((pkgNameLen + 3) & ~3) <= head.Length)
            pos += ((pkgNameLen + 3) & ~3);
        _ = ReadI32(head, ref pos);                   // PackageFlags
        int nameCount = ReadI32(head, ref pos);
        int nameOffset = 0;
        // Some writers insert an extra byte after the flags (observed when the
        // cooked flag is set) - the name count then reads one byte late and is
        // absurd. Realign by one byte and re-read.
        if (nameCount > 10000)
        {
            pos -= 3;
            nameCount = ReadI32(head, ref pos);
        }
        if (nameCount is < 0 or > MaxNames) nameCount = 0; // never pre-allocate an absurd count
        nameOffset = ReadI32(head, ref pos);          // 0 on this format - located empirically in ReadNames
        _ = ReadI32(head, ref pos);                   // gatherable text data count
        _ = ReadI32(head, ref pos);                   // gatherable text data offset
        int exportOffset = ReadI32(head, ref pos);
        int exportCount = ReadI32(head, ref pos);
        if (exportCount is < 0 or > MaxExports) exportCount = 0; // never pre-allocate an absurd count
        int importOffset = ReadI32(head, ref pos);
        _ = ReadI32(head, ref pos);                   // (unknown tail field - not the import count)

        // The import count is not reliably present; derive it from the table
        // size: the imports end where the export table begins, 28 bytes each
        // (cooked imports omit the package name).
        int importCount = exportOffset > importOffset ? (exportOffset - importOffset) / 28 : 0;
        if (importCount is < 0 or > 100000) importCount = 0;

        return new Summary
        {
            TotalHeaderSize = totalHeaderSize,
            NameCount = nameCount,
            ExportCount = exportCount,
            ExportOffset = exportOffset,
            ImportCount = importCount,
            ImportOffset = importOffset,
            NameOffset = nameOffset,
            HasPreamble = magicPos > 0,
        };
    }

    /// <summary>
    /// Reads the name table. Each entry: {int32 byteLen incl null, UTF-8 + null,
    /// uint32 hash}. The table position is located empirically (some writers
    /// omit it from the summary): scan forward for the first position where a
    /// length-prefixed printable string is followed by another valid entry.
    /// </summary>
    public static List<string> ReadNames(IAssetSource source, Summary summary)
    {
        int offset = summary.NameOffset;
        if (offset <= 0 || offset >= source.Length)
        {
            // Empirical finder: scan from the summary end for the entry pattern.
            offset = FindNameTable(source);
            if (offset < 0) return new List<string>();
        }
        byte[] data;
        using (var s = source.OpenRead(offset, Math.Min(source.Length - offset, 4 * 1024 * 1024)))
        {
            data = new byte[s.Length];
            s.ReadExactly(data);
        }
        int count = Math.Clamp(summary.NameCount, 0, MaxNames);
        var names = new List<string>(count);
        int pos = 0;
        for (int i = 0; i < count && pos + 8 < data.Length; i++)
        {
            int len = BitConverter.ToInt32(data, pos);
            if (len <= 0 || len > 1 << 20 || pos + 4 + len + 4 > data.Length) break;
            names.Add(System.Text.Encoding.UTF8.GetString(data, pos + 4, len - 1));
            pos += 4 + len + 4; // len + string + hash
        }
        return names;
    }

    private static int FindNameTable(IAssetSource source)
    {
        int limit = (int)Math.Min(source.Length, 4096);
        byte[] data;
        using (var s = source.OpenRead(0, limit))
        {
            data = new byte[s.Length];
            s.ReadExactly(data);
        }
        for (int p = 32; p + 16 < data.Length; p++)
        {
            int len = BitConverter.ToInt32(data, p);
            if (len is <= 2 or > 1024) continue; // real names have at least 1 char + null
            if (p + 4 + len + 4 + 4 > data.Length) continue;
            bool printable = true;
            for (int i = 0; i < len - 1; i++)
            {
                byte c = data[p + 4 + i];
                if (c is not (>= 32 and < 127) and not 0)
                {
                    if (c < 32 || c >= 127) { printable = false; break; }
                }
            }
            if (!printable) continue;
            // The next entry's length must also be sane (string + hash follow).
            int nextLen = BitConverter.ToInt32(data, p + 4 + len + 4);
            if (nextLen is > 0 and <= 1024) return p;
        }
        return -1;
    }

    /// <summary>Reads the export table (40-byte records).</summary>
    public static List<ObjectExport> ReadExports(IAssetSource source, Summary summary)
    {
        if (summary.ExportCount <= 0) return new List<ObjectExport>();
        if (summary.ExportOffset < 0) throw new CorruptAssetException("Export table offset is invalid.");
        long length = (long)summary.ExportCount * 40;
        long need = summary.ExportOffset + length;
        if (need > source.Length) throw new CorruptAssetException("Export table exceeds the file.");
        byte[] data;
        using (var s = source.OpenRead(summary.ExportOffset, length))
        {
            data = new byte[s.Length];
            s.ReadExactly(data);
        }
        var exports = new List<ObjectExport>(summary.ExportCount);
        for (int i = 0; i < summary.ExportCount; i++)
        {
            int o = i * 40;
            exports.Add(new ObjectExport
            {
                ClassIndex = BitConverter.ToInt32(data, o),
                SuperIndex = BitConverter.ToInt32(data, o + 4),
                TemplateIndex = BitConverter.ToInt32(data, o + 8),
                OuterIndex = BitConverter.ToInt32(data, o + 12),
                NameIndex = BitConverter.ToInt32(data, o + 16),
                // 32-bit serial size/offset in the 40-byte record:
                // {Class, Super, Template, Outer, Name(8), Flags, Size(4),
                //  ???(4), Offset(4)}.
                SerialSize = BitConverter.ToInt32(data, o + 28),
                SerialOffset = BitConverter.ToInt32(data, o + 36),
            });
        }
        return exports;
    }

    // ── UTexture2D ────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads a Texture2D export from the .uexp companion. SerialOffset is
    /// relative to TotalHeaderSize and addresses the uexp start.
    /// </summary>
    public static Texture2DInfo? ReadTexture2D(IAssetSource uexpSource, ObjectExport export, List<string> names, int totalHeaderSize)
    {
        long dataOffset = export.SerialOffset - totalHeaderSize;
        if (dataOffset < 0 || export.SerialSize <= 0 || export.SerialSize > 256L * 1024 * 1024)
            return null;
        byte[] data;
        using (var s = uexpSource.OpenRead(dataOffset, export.SerialSize))
        {
            data = new byte[s.Length];
            s.ReadExactly(data);
        }

        // Unversioned properties (skipped via the presence header), then the
        // cooked platform data. The property reader returns the position right
        // after the property values.
        int pos = 0;
        if (!TrySkipUnversionedProperties(data, ref pos))
            return null;

        // FTexturePlatformData: {SizeX, SizeY, PackedData, PixelFormat FString,
        //   FirstMip, mipCount, mips...}
        if (pos + 32 > data.Length) return null;
        int sizeX = BitConverter.ToInt32(data, pos); pos += 4;
        int sizeY = BitConverter.ToInt32(data, pos); pos += 4;
        _ = BitConverter.ToUInt32(data, pos); pos += 4;       // PackedData
        int fmtLen = BitConverter.ToInt32(data, pos); pos += 4;
        if (fmtLen <= 0 || fmtLen > 128 || pos + fmtLen > data.Length) return null;
        string pixelFormat = System.Text.Encoding.UTF8.GetString(data, pos, fmtLen - 1);
        pos += ((fmtLen + 3) & ~3);
        _ = BitConverter.ToInt32(data, pos); pos += 4;        // FirstMipToSerialize
        int mipCount = BitConverter.ToInt32(data, pos); pos += 4;
        if (mipCount is < 0 or > 64) return null;

        int mip0X = 0, mip0Y = 0;
        byte[]? mip0Data = null;
        for (int m = 0; m < mipCount; m++)
        {
            if (!TryReadMip(data, ref pos, out int mx, out int my, out byte[]? bulk))
                return null;
            if (m == 0)
            {
                mip0X = mx; mip0Y = my;
                mip0Data = bulk;
            }
        }

        return new Texture2DInfo
        {
            Name = export.NameIndex >= 0 && export.NameIndex < names.Count ? names[export.NameIndex] : "texture",
            SizeX = sizeX,
            SizeY = sizeY,
            PixelFormat = pixelFormat,
            MipCount = mipCount,
            Mip0SizeX = mip0X,
            Mip0SizeY = mip0Y,
            Mip0Data = mip0Data,
        };
    }

    /// <summary>
    /// Skips the unversioned property values using the presence header.
    /// UE4.22-24 format: {uint32 mask, zero-mask bits for absent properties},
    /// then the values of the present properties (sized by the UTexture2D
    /// schema). Returns the position after the values.
    /// </summary>
    private static bool TrySkipUnversionedProperties(byte[] data, ref int pos)
    {
        if (pos + 4 > data.Length) return false;
        uint mask = BitConverter.ToUInt32(data, pos); pos += 4;
        if (mask == 0) return false; // no present properties - unexpected for a texture
        if ((mask & 0x80000000) != 0) return false; // chained masks not supported yet

        // Zero-mask bits: one per absent property in the first 32.
        int absent = 32 - System.Numerics.BitOperations.PopCount(mask);
        if (absent > 0)
        {
            int zmBytes = (absent + 7) / 8;
            if (pos + zmBytes > data.Length) return false;
            pos += zmBytes;
        }

        // The UTexture2D property values, in class order, sized by schema.
        // The texture's platform data (PF_ string anchor) follows the values.
        // To stay robust across engine versions we do not decode the values -
        // instead we scan forward for the platform data anchor: the pixel
        // format FString "PF_" preceded by SizeX/SizeY/PackedData int32s.
        return FindPlatformDataAnchor(data, ref pos);
    }

    /// <summary>
    /// Scans for the cooked platform data: {int32 SizeX, int32 SizeY, uint32
    /// PackedData, FString "PF_..."}. The pixel-format string is the anchor.
    /// </summary>
    private static bool FindPlatformDataAnchor(byte[] data, ref int pos)
    {
        int searchFrom = pos;
        int searchTo = Math.Min(data.Length, searchFrom + 512);
        for (int i = searchFrom; i + 8 <= searchTo; i++)
        {
            if (data[i] != (byte)'P' || data[i + 1] != (byte)'F' || data[i + 2] != (byte)'_') continue;
            // The FString length (incl null) precedes the string, and the
            // platform data begins 16 bytes before the string: SizeX + SizeY +
            // PackedData + the FString length.
            int len = BitConverter.ToInt32(data, i - 4);
            if (len <= 0 || len > 128 || i + len > data.Length) continue;
            if (data[i + len - 1] != 0) continue;
            pos = i - 16;
            return true;
        }
        return false;
    }

    /// <summary>
    /// FTexture2DMipMap: {bool cooked(1B)+pad, FByteBulkData, SizeX, SizeY, SizeZ}.
    /// FByteBulkData: {flags, elementCount, sizeOnDisk, offsetInFile(8),
    ///   filename FString, cookedIndex}. Inline payload follows the header.
    /// </summary>
    private static bool TryReadMip(byte[] data, ref int pos, out int mx, out int my, out byte[]? bulk)
    {
        mx = 0; my = 0; bulk = null;
        if (pos + 1 > data.Length) return false;
        pos += 4; // cooked bool + pad

        if (pos + 4 * 4 + 8 > data.Length) return false;
        int flags = BitConverter.ToInt32(data, pos); pos += 4;
        int elementCount = BitConverter.ToInt32(data, pos); pos += 4;
        int sizeOnDisk = BitConverter.ToInt32(data, pos); pos += 4;
        long offsetInFile = BitConverter.ToInt64(data, pos); pos += 8;
        int fnLen = BitConverter.ToInt32(data, pos); pos += 4;
        if (fnLen > 0 && fnLen < 1024) pos += ((fnLen + 3) & ~3);
        else if (fnLen < 0) pos += 0;
        else pos += 0; // empty filename
        _ = BitConverter.ToInt32(data, pos); pos += 4; // cooked index

        // Inline payload when the flags say so; otherwise the data sits in the
        // .ubulk / .uexp at offsetInFile.
        bool inline = (flags & 0x01000000) != 0 || (flags & 0x0100) != 0;
        if (inline && sizeOnDisk > 0 && pos + sizeOnDisk <= data.Length)
        {
            bulk = data.AsSpan(pos, sizeOnDisk).ToArray();
            pos += sizeOnDisk;
        }
        else if (sizeOnDisk > 0 && offsetInFile >= 0 && offsetInFile + sizeOnDisk <= data.Length)
        {
            bulk = data.AsSpan((int)offsetInFile, sizeOnDisk).ToArray();
        }

        if (pos + 12 > data.Length) return false;
        mx = BitConverter.ToInt32(data, pos); pos += 4;
        my = BitConverter.ToInt32(data, pos); pos += 4;
        _ = BitConverter.ToInt32(data, pos); pos += 4; // SizeZ
        return true;
    }

    private static int ReadI32(byte[] d, ref int p)
    {
        int v = BitConverter.ToInt32(d, p); p += 4; return v;
    }
}
