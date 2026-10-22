using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Errors;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Containers;

/// <summary>
/// Unity serialized file (.assets / globalgamemanagers). Parses the header,
/// type tree (blob with hashed common strings) and object table for
/// serialized-file format version 17 (Unity 5.5-2018.4 - the PS4-era range).
///
/// Texture2D objects are read with AssetStudio's hardcoded class layout
/// (verified against AssetStudio master), which works even when the type tree
/// is stripped - the PS4 build norm. See ReadTexture2D.
/// </summary>
public sealed class UnitySerializedFile
{
    public sealed class ObjectInfo
    {
        public long PathId { get; init; }
        public int ClassId { get; init; }
        public long Offset { get; init; }
        public long Size { get; init; }
        public string? TypeName { get; init; }
    }

    public required int Version { get; init; }
    public required int DataOffset { get; init; }
    public required bool EnableTypeTree { get; init; }
    public required bool BigEndian { get; init; }
    public required string UnityVersion { get; init; }
    public required IReadOnlyList<ObjectInfo> Objects { get; init; }
    public IReadOnlyDictionary<long, List<FieldLeaf>> TypeTreeLeavesByClass { get; init; } = new Dictionary<long, List<FieldLeaf>>();

    public sealed class FieldLeaf
    {
        public required string TypeName { get; init; }
        public required string Name { get; init; }
    }

    public static bool IsLikelySerializedFile(IAssetSource source)
    {
        try
        {
            using var s = source.OpenRead(0, 20);
            var head = new byte[s.Length];
            s.ReadExactly(head);
            return IsLikelySerializedFile(head);
        }
        catch { return false; }
    }

    /// <summary>
    /// Head-buffer probe: the version field at [8..12) must be a known
    /// serialized-file version in either byte order. Cheap, bounded, safe.
    /// </summary>
    public static bool IsLikelySerializedFile(ReadOnlySpan<byte> head)
    {
        if (head.Length < 20) return false;
        uint be = (uint)((head[8] << 24) | (head[9] << 16) | (head[10] << 8) | head[11]);
        uint le = BitConverter.ToUInt32(head[8..]);
        return be is >= 5 and <= 22 || le is >= 5 and <= 22;
    }

    public static UnitySerializedFile Parse(IAssetSource source)
    {
        var data = ReadAll(source);
        int pos = 0;
        bool bigEndian = true;

        uint metaSize = ReadU32(data, ref pos, bigEndian);
        _ = ReadU32(data, ref pos, bigEndian); // file size
        uint version = ReadU32(data, ref pos, bigEndian);
        uint dataOffset = ReadU32(data, ref pos, bigEndian);

        if (version < 5 || version > 22) throw new CorruptAssetException($"Unsupported serialized file version {version}.");

        if (version >= 9)
        {
            if (pos >= data.Length) throw new CorruptAssetException("Serialized file header truncated.");
            byte endianness = data[pos++];
            pos += 3; // reserved
            // AssetStudio: the first four header fields are read with the reader's
            // default (big-endian); endianness == 0 then switches to LITTLE-endian
            // for everything after (metadata, type tree, object table).
            bigEndian = endianness != 0;
        }

        string unityVersion = "";
        if (version >= 7) unityVersion = ReadNullString(data, ref pos) ?? "";
        if (version >= 8) pos += 4; // target platform

        bool enableTypeTree = true;
        if (version >= 13)
        {
            if (pos >= data.Length) throw new CorruptAssetException("Serialized file header truncated.");
            enableTypeTree = data[pos++] != 0;
        }

        int typeCount = ReadI32(data, ref pos, bigEndian);
        if (typeCount < 0 || typeCount > 4096) throw new CorruptAssetException($"Invalid type count {typeCount}.");

        var leavesByClass = new Dictionary<long, List<FieldLeaf>>();
        var orderedClassIds = new List<long>(typeCount);
        for (int i = 0; i < typeCount; i++)
        {
            long classId = ReadI32(data, ref pos, bigEndian);
            orderedClassIds.Add(classId);
            // AssetStudio SerializedFile.ReadSerializedType: type records were
            // refactored twice.  v16 adds isStrippedType; v17 adds
            // scriptTypeIndex.  Older records instead carry a script ID when
            // their class ID is negative.
            if (version >= 16) pos += 1;      // isStrippedType
            if (version >= 17) pos += 2;      // scriptTypeIndex
            if (version >= 13)
            {
                if ((version < 16 && classId < 0) || (version >= 16 && classId == 114))
                    pos += 16;                 // scriptID
                pos += 16;                     // oldTypeHash
            }
            if (enableTypeTree)
            {
                if (version >= 12) leavesByClass[classId] = ReadTypeTreeBlob(data, ref pos, bigEndian);
                else throw new UnsupportedAssetException($"Type tree format for version {version} is not supported yet.");
            }
        }

        // v7 <= version < 14: bigIDEnabled
        if (version >= 7 && version < 14) pos += 4;

        int objectCount = ReadI32(data, ref pos, bigEndian);

        if (!IsPlausibleObjectCount(objectCount))
            throw new CorruptAssetException($"Invalid object count {objectCount}.");

        var objects = new List<ObjectInfo>(objectCount);
        for (int i = 0; i < objectCount; i++)
        {
            long pathId;
            if (version >= 14)
            {
                Align4(ref pos);
                pathId = BitConverter.ToInt64(SliceBigEndian(data, pos, 8, bigEndian), 0);
                pos += 8;
            }
            else
            {
                pathId = ReadU32(data, ref pos, bigEndian);
            }

            uint byteStart = version >= 22 ? checked((uint)ReadU64(data, ref pos, bigEndian)) : ReadU32(data, ref pos, bigEndian);
            uint byteSize = ReadU32(data, ref pos, bigEndian);
            int typeId = ReadI32(data, ref pos, bigEndian);

            int classId;
            if (version < 16)
            {
                // v15 and older store the class directly in every object
                // record; it is not an index into the serialized-type list.
                classId = ReadU16(data, ref pos, bigEndian);
            }
            else
            {
                classId = typeId >= 0 && typeId < orderedClassIds.Count ? (int)orderedClassIds[typeId] : 0;
            }

            if (version < 11) pos += 2;                 // isDestroyed
            if (version >= 11 && version < 17) pos += 2; // scriptTypeIndex
            if (version is 15 or 16) pos += 1;           // stripped

            objects.Add(new ObjectInfo
            {
                PathId = pathId,
                ClassId = classId,
                Offset = byteStart + dataOffset,
                Size = byteSize,
                TypeName = ClassIdToName(classId),
            });
        }

        return new UnitySerializedFile
        {
            Version = (int)version,
            DataOffset = (int)dataOffset,
            EnableTypeTree = enableTypeTree,
            BigEndian = bigEndian,
            UnityVersion = unityVersion,
            Objects = objects,
            TypeTreeLeavesByClass = leavesByClass,
        };
    }

    /// <summary>
    /// Reads a Texture2D's fields using a Unity-version-specific layout. Works with STRIPPED type
    /// trees (the PS4 build norm) - no type tree required.
    ///
    /// Field order (verified against AssetStudio master, Texture2D.cs / Texture.cs):
    ///   m_Name, Texture base (2017.3+: m_ForcedFallbackFormat, m_DownscaleFallback),
    ///   m_Width, m_Height, m_CompleteImageSize, m_TextureFormat, m_MipCount,
    ///   m_IsReadable, [Unity 3.0-5.4: m_ReadAllowed], m_ImageCount,
    ///   m_TextureDimension, GLTextureSettings (2017:
    ///   FilterMode, Aniso, MipBias, WrapU, WrapV, WrapW), m_LightmapFormat,
    ///   m_ColorSpace, image-data size, [StreamingInfo], then inline image bytes
    ///   OR an external .resS stream reference.
    /// </summary>
    public static Texture2DInfo? ReadTexture2D(
        IAssetSource source,
        ObjectInfo obj,
        bool bigEndian,
        bool hasFallbackFields = true,
        bool hasSeparateWrapModes = true,
        bool hasReadAllowed = false,
        bool hasMipCount = true)
        => ReadTexture2D(source, obj, bigEndian, new Texture2DLayout
        {
            HasFallbackFields = hasFallbackFields,
            HasSeparateWrapModes = hasSeparateWrapModes,
            HasReadAllowed = hasReadAllowed,
            HasMipCount = hasMipCount,
        });

    public static Texture2DInfo? ReadTexture2D(
        IAssetSource source,
        ObjectInfo obj,
        bool bigEndian,
        Texture2DLayout layout)
    {
        if (obj.ClassId != 28) return null; // Texture2D

        var data = ReadRange(source, obj.Offset, obj.Size);
        int pos = 0;

        string name = ReadAlignedString(data, ref pos, bigEndian) ?? "";
        if (layout.HasFallbackFields)
        {
            _ = ReadI32(data, ref pos, bigEndian); // Texture.m_ForcedFallbackFormat
            pos += 1;                              // Texture.m_DownscaleFallback (bool)
            Align4(ref pos);
        }

        int width = ReadI32(data, ref pos, bigEndian);
        int height = ReadI32(data, ref pos, bigEndian);
        _ = ReadU32(data, ref pos, bigEndian);    // m_CompleteImageSize
        if (layout.HasMipsStripped)
            _ = ReadI32(data, ref pos, bigEndian); // m_MipsStripped (2020.1+)
        int format = ReadI32(data, ref pos, bigEndian);
        int mipCount;
        if (layout.HasMipCount)
        {
            mipCount = ReadI32(data, ref pos, bigEndian);
        }
        else
        {
            if (pos >= data.Length) throw new CorruptAssetException("Serialized Texture2D data is truncated.");
            mipCount = data[pos++] != 0 ? 1 : 0; // m_MipMap (Unity 5.0-5.1)
        }
        pos += 1;                                 // m_IsReadable (bool)
        if (layout.HasPreProcessed)
            pos += 1;                             // m_IsPreProcessed (2020.1+)
        if (layout.HasIgnoreMasterTextureLimit)
            pos += 1;                             // m_IgnoreMasterTextureLimit (2019.3+)
        if (layout.HasReadAllowed)
            pos += 1;                             // m_ReadAllowed (Unity 3.0-5.4)
        if (layout.HasStreamingMipmaps)
            pos += 1;                             // m_StreamingMipmaps (2018.2+)
        Align4(ref pos);
        if (layout.HasStreamingMipmaps)
            _ = ReadI32(data, ref pos, bigEndian); // m_StreamingMipmapsPriority
        _ = ReadI32(data, ref pos, bigEndian);    // m_ImageCount
        _ = ReadI32(data, ref pos, bigEndian);    // m_TextureDimension
        _ = ReadI32(data, ref pos, bigEndian);    // GLTextureSettings.m_FilterMode
        _ = ReadI32(data, ref pos, bigEndian);    // GLTextureSettings.m_Aniso
        _ = ReadSingle(data, ref pos, bigEndian); // GLTextureSettings.m_MipBias
        _ = ReadI32(data, ref pos, bigEndian);    // GLTextureSettings.m_WrapMode / m_WrapU
        // Unity 5.x stored one wrap mode. m_WrapV and m_WrapW were added
        // with the newer Texture2D layout, alongside the fallback fields.
        if (layout.HasSeparateWrapModes)
        {
            _ = ReadI32(data, ref pos, bigEndian); // GLTextureSettings.m_WrapV
            _ = ReadI32(data, ref pos, bigEndian); // GLTextureSettings.m_WrapW
        }
        _ = ReadI32(data, ref pos, bigEndian);    // m_LightmapFormat
        _ = ReadI32(data, ref pos, bigEndian);    // m_ColorSpace

        int imageDataSize = ReadI32(data, ref pos, bigEndian);
        byte[]? image = null;
        string? streamPath = null;
        long streamOffset = 0;
        uint streamSize = 0;

        if (imageDataSize == 0 && layout.HasStreamingInfo)
        {
            // StreamingInfo (version < 2020): external .resS reference.
            streamOffset = ReadU32(data, ref pos, bigEndian);
            streamSize = ReadU32(data, ref pos, bigEndian);
            streamPath = ReadAlignedString(data, ref pos, bigEndian);
        }
        else if (imageDataSize > 0 && pos + imageDataSize <= data.Length)
        {
            image = data.AsSpan(pos, imageDataSize).ToArray();
        }

        if (width <= 0 || height <= 0 || format < 0)
            throw new CorruptAssetException($"Texture2D fields are out of range (layout mismatch: {width}x{height}, format {format}).");

        return new Texture2DInfo
        {
            Name = name,
            Width = width,
            Height = height,
            Format = format,
            MipCount = mipCount,
            ImageBytes = image,
            StreamDataPath = streamPath,
            StreamOffset = streamOffset,
            StreamSize = streamSize,
        };
    }

    public sealed class Texture2DInfo
    {
        public required string Name { get; init; }
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required int Format { get; init; }
        public int MipCount { get; init; }
        /// <summary>Inline image bytes (m_ImageContents). Null when the data streams externally.</summary>
        public byte[]? ImageBytes { get; init; }
        /// <summary>External .resS reference (m_StreamData) - relative to the .assets file.</summary>
        public string? StreamDataPath { get; init; }
        public long StreamOffset { get; init; }
        public uint StreamSize { get; init; }
    }

    public static string ClassIdToName(int classId) => classId switch
    {
        28 => "Texture2D",
        48 => "Shader",
        49 => "TextAsset",
        83 => "AudioClip",
        114 => "MonoBehaviour",
        43 => "Mesh",
        18 => "Material",
        _ => $"class {classId}",
    };

    // ── helpers ───────────────────────────────────────────────────────────

    private const long MaxSerializedFileBytes = 1L << 30; // 1 GiB

    private static byte[] ReadAll(IAssetSource source)
    {
        using var s = source.OpenRead();
        if (s.CanSeek && s.Length > MaxSerializedFileBytes)
            throw new UnsupportedAssetException($"Serialized file is too large ({s.Length} bytes).");
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        if (ms.Length > MaxSerializedFileBytes)
            throw new UnsupportedAssetException("Serialized file is too large.");
        return ms.ToArray();
    }

    private static byte[] ReadRange(IAssetSource source, long offset, long length)
    {
        if (length < 0 || length > int.MaxValue)
            throw new CorruptAssetException($"Invalid serialized object size {length}.");
        using var s = source.OpenRead(offset, length);
        var b = new byte[s.Length];
        s.ReadExactly(b);
        return b;
    }

    private static uint ReadU32(byte[] d, ref int p, bool be)
    {
        if (p < 0 || p + 4 > d.Length)
            throw new CorruptAssetException("Serialized Texture2D data is truncated or uses an unsupported layout.");
        uint v = be ? (uint)((d[p] << 24) | (d[p + 1] << 16) | (d[p + 2] << 8) | d[p + 3]) : BitConverter.ToUInt32(d, p);
        p += 4;
        return v;
    }

    /// <summary>Texture2D field switches for a Unity editor version.</summary>
    public sealed class Texture2DLayout
    {
        public bool HasFallbackFields { get; init; }
        public bool HasSeparateWrapModes { get; init; }
        public bool HasReadAllowed { get; init; }
        public bool HasMipCount { get; init; } = true;
        public bool HasStreamingMipmaps { get; init; }
        public bool HasMipsStripped { get; init; }
        public bool HasPreProcessed { get; init; }
        public bool HasIgnoreMasterTextureLimit { get; init; }
        public bool HasStreamingInfo { get; init; } = true;

        public static Texture2DLayout FromUnityVersion(string unityVersion)
        {
            var parts = unityVersion.Split('.', 3);
            if (parts.Length < 2 || !int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor))
                return new Texture2DLayout(); // compatible fallback for common 2017.3-2018 files

            bool AtLeast(int requiredMajor, int requiredMinor = 0)
                => major > requiredMajor || (major == requiredMajor && minor >= requiredMinor);

            return new Texture2DLayout
            {
                HasFallbackFields = AtLeast(2017, 3),
                HasSeparateWrapModes = AtLeast(2017),
                HasReadAllowed = major >= 3 && (major < 5 || (major == 5 && minor <= 4)),
                HasMipCount = AtLeast(5, 2),
                HasStreamingMipmaps = AtLeast(2018, 2),
                HasMipsStripped = AtLeast(2020),
                HasPreProcessed = AtLeast(2020),
                HasIgnoreMasterTextureLimit = AtLeast(2019, 3),
                HasStreamingInfo = AtLeast(5, 3),
            };
        }
    }

    private static ushort ReadU16(byte[] d, ref int p, bool be)
    {
        if (p + 2 > d.Length) throw new CorruptAssetException("Serialized file is truncated.");
        ushort v = be ? (ushort)((d[p] << 8) | d[p + 1]) : BitConverter.ToUInt16(d, p);
        p += 2;
        return v;
    }

    private static float ReadSingle(byte[] d, ref int p, bool be)
    {
        // Assemble the 4 bytes in file order; the resulting bit pattern IS the float.
        int v = ReadI32(d, ref p, be);
        return BitConverter.Int32BitsToSingle(v);
    }

    /// <summary>Unity aligned string: int32 length (negative = null), UTF8 bytes, padded to 4.</summary>
    private static string? ReadAlignedString(byte[] d, ref int p, bool be)
    {
        if (p + 4 > d.Length) throw new CorruptAssetException("Aligned string truncated.");
        int len = ReadI32(d, ref p, be);
        if (len < 0) return null;
        if (len > 1 << 24 || p + len > d.Length)
            throw new CorruptAssetException("Aligned string length out of range.");
        string s = System.Text.Encoding.UTF8.GetString(d, p, len);
        p += len;
        Align4(ref p);
        return s;
    }

    private static int ReadI32(byte[] d, ref int p, bool be)
        => unchecked((int)ReadU32(d, ref p, be));

    private static long ReadU64(byte[] d, ref int p, bool be)
    {
        var b = SliceBigEndian(d, p, 8, be);
        p += 8;
        return BitConverter.ToInt64(b, 0);
    }

    private static string? ReadNullString(byte[] d, ref int p)
    {
        int start = p;
        while (p < d.Length && d[p] != 0) p++;
        if (p >= d.Length) return null;
        var s = System.Text.Encoding.UTF8.GetString(d, start, p - start);
        p++; // null
        return s;
    }

    private static List<FieldLeaf> ReadTypeTreeBlob(byte[] d, ref int p, bool be)
    {
        int nodeCount = ReadI32(d, ref p, be);
        int stringBufferSize = ReadI32(d, ref p, be);
        if (nodeCount < 0 || nodeCount > 1_000_000 || stringBufferSize < 0 || stringBufferSize > 64 * 1024 * 1024)
            throw new CorruptAssetException("Invalid type tree blob sizes.");

        var nodes = new (uint typeOff, uint nameOff)[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            p += 2; // version
            p += 2; // level + typeFlags
            uint typeOff = ReadU32(d, ref p, be);
            uint nameOff = ReadU32(d, ref p, be);
            p += 4 + 4 + 4; // byteSize, index, metaFlag
            nodes[i] = (typeOff, nameOff);
        }

        int bufferStart = p;
        string Resolve(uint offset)
        {
            if ((offset & 0x80000000) == 0)
            {
                int o = (int)offset;
                if (o >= 0 && o + 1 < bufferStart + stringBufferSize && o < d.Length)
                {
                    int end = o;
                    while (end < d.Length && d[end] != 0) end++;
                    return System.Text.Encoding.UTF8.GetString(d, o, end - o);
                }
                return "";
            }
            uint common = offset & 0x7FFFFFFF;
            return CommonStrings.Table.TryGetValue(common, out var cs) ? cs : common.ToString();
        }

        var leaves = new List<FieldLeaf>(nodeCount);
        foreach (var (t, n) in nodes)
        {
            string type = Resolve(t);
            string name = Resolve(n);
            // Keep only leaf-ish fields (skip container nodes with children later);
            leaves.Add(new FieldLeaf { TypeName = type, Name = name });
        }
        p = bufferStart + stringBufferSize;
        return leaves;
    }

    private static byte[] SliceBigEndian(byte[] d, int p, int len, bool be)
    {
        if (p < 0 || len < 0 || p > d.Length - len)
            throw new CorruptAssetException("Serialized file is truncated.");
        var b = new byte[len];
        Array.Copy(d, p, b, 0, len);
        if (be) Array.Reverse(b);
        return b;
    }


    private static void Align4(ref int p) => p = (p + 3) & ~3;
    private static void Align8(ref int p) => p = (p + 7) & ~7;

    private static bool IsPlausibleObjectCount(int count) => count >= 0 && count <= 10_000_000;

}
