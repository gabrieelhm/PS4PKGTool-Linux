using System.Runtime.CompilerServices;
using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Codecs;
using PS4PKGTool.Assets.Containers;
using PS4PKGTool.Assets.Errors;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Unity;

/// <summary>
/// IUnityAssetBackend implemented with a self-written parser that matches
/// AssetStudio's verified class layouts (serialized-file v17, hardcoded
/// Texture2D fields, stripped-type-tree safe). A drop-in replacement for
/// embedding the real AssetStudio core if Unity version coverage ever needs
/// extending beyond 5.5-2018.4.
/// </summary>
public sealed class AssetStudioBackend : IUnityAssetBackend
{
    public string Name => "AssetStudio-derived layout (self-written)";

    /// <summary>Parse cache per source (a parse reads the whole file - do it once per source lifetime).</summary>
    private static readonly ConditionalWeakTable<IAssetSource, UnitySerializedFile> ParseCache = new();

    private static UnitySerializedFile GetParse(IAssetSource source)
        => ParseCache.GetValue(source, UnitySerializedFile.Parse);

    public IReadOnlyList<UnityObjectRef> ListObjects(IAssetSource source)
    {
        var sf = GetParse(source);
        return sf.Objects
            // Some Unity files include zero-sized Texture2D placeholders. They
            // have no pixels to preview and a few use a private stripped layout;
            // omit them from the browse list rather than presenting a preview
            // action that is guaranteed to fail.
            .Where(o => o.ClassId != 28 || sf.Version < 16 || IsPreviewableTexture(source, sf, o))
            .Select(o =>
            {
                // Textures carry their real name in the object data - read it so
                // the asset list shows e.g. "UI_LoadingPage", not "Texture2D".
                string name = o.ClassId == 28
                    ? ReadTextureName(source, sf, o) ?? UnitySerializedFile.ClassIdToName(o.ClassId)
                    : UnitySerializedFile.ClassIdToName(o.ClassId);
                return new UnityObjectRef(name, o.ClassId, o.PathId, o.Offset, o.Size);
            })
            .ToList();
    }

    private static bool IsPreviewableTexture(IAssetSource source, UnitySerializedFile sf, UnitySerializedFile.ObjectInfo obj)
    {
        try
        {
            var info = UnitySerializedFile.ReadTexture2D(source, obj, sf.BigEndian,
                UnitySerializedFile.Texture2DLayout.FromUnityVersion(sf.UnityVersion));
            return info is { Width: > 0, Height: > 0 };
        }
        catch (AssetException) { return false; }
    }

    private static string? ReadTextureName(IAssetSource source, UnitySerializedFile sf, UnitySerializedFile.ObjectInfo obj)
    {
        try
        {
            var info = UnitySerializedFile.ReadTexture2D(source, obj, sf.BigEndian,
                UnitySerializedFile.Texture2DLayout.FromUnityVersion(sf.UnityVersion));
            return string.IsNullOrEmpty(info.Name) ? null : info.Name;
        }
        catch { return null; }
    }

    public UnityTextureInfo? ReadTexture2D(IAssetSource source, UnityObjectRef obj)
    {
        if (obj.ClassId != 28) return null;
        var sf = GetParse(source);
        var match = sf.Objects.FirstOrDefault(o => o.PathId == obj.PathId);
        if (match == null) return null;

        var info = UnitySerializedFile.ReadTexture2D(source, match, sf.BigEndian,
            UnitySerializedFile.Texture2DLayout.FromUnityVersion(sf.UnityVersion));
        return new UnityTextureInfo
        {
            Name = info.Name,
            Width = info.Width,
            Height = info.Height,
            Format = info.Format,
            MipCount = info.MipCount,
            InlineBytes = info.ImageBytes,
            StreamPath = info.StreamDataPath,
            StreamOffset = info.StreamOffset,
            StreamSize = info.StreamSize,
        };
    }

    private const long MaxPixels = 64_000_000;

    public TextureData? DecodeTexture(UnityTextureInfo info)
    {
        if (info.Width <= 0 || info.Height <= 0) return null;
        if ((long)info.Width * info.Height > MaxPixels) return null;
        if (info.InlineBytes is not { Length: > 0 } bytes) return null;

        byte[]? rgba = info.Format switch
        {
            // Unity TextureFormat enum values (verified against AssetStudio TextureFormat.cs).
            1  => ExpandLum(bytes, info.Width, info.Height),          // Alpha8
            3  => ExpandRgb24(bytes, info.Width, info.Height),        // RGB24
            4  => Swizzle(bytes, info.Width, info.Height, 0, 1, 2, 3),// RGBA32
            5  => Swizzle(bytes, info.Width, info.Height, 3, 0, 1, 2),// ARGB32
            7  => ExpandRgb565(bytes, info.Width, info.Height),       // RGB565
            10 => DdsDecoder.DecodeBcPayload(info.Width, info.Height, "DXT1", bytes),
            11 => DdsDecoder.DecodeBcPayload(info.Width, info.Height, "DXT3", bytes),
            12 => DdsDecoder.DecodeBcPayload(info.Width, info.Height, "DXT5", bytes),
            13 => ExpandRgba4444(bytes, info.Width, info.Height),     // RGBA4444
            14 => Swizzle(bytes, info.Width, info.Height, 2, 1, 0, 3),// BGRA32
            _  => null,
        };
        return rgba == null ? null : new TextureData { Width = info.Width, Height = info.Height, Rgba8 = rgba };
    }

    /// <summary>Channel reorder (copies the first mip only; extra bytes ignored).</summary>
    private static byte[] Swizzle(byte[] src, int w, int h, int r, int g, int b, int a)
    {
        int pixels = w * h;
        var dst = new byte[pixels * 4];
        int n = Math.Min(src.Length, pixels * 4);
        for (int i = 0; i < n; i += 4)
        {
            dst[i] = src[i + r];
            dst[i + 1] = src[i + g];
            dst[i + 2] = src[i + b];
            dst[i + 3] = src[i + a];
        }
        return dst;
    }

    private static byte[] ExpandRgb24(byte[] src, int w, int h)
    {
        int pixels = w * h;
        var dst = new byte[pixels * 4];
        int n = Math.Min(src.Length, pixels * 3);
        for (int i = 0, j = 0; i < n; i += 3, j += 4)
        {
            dst[j] = src[i];
            dst[j + 1] = src[i + 1];
            dst[j + 2] = src[i + 2];
            dst[j + 3] = 255;
        }
        return dst;
    }

    private static byte[] ExpandLum(byte[] src, int w, int h)
    {
        int pixels = w * h;
        var dst = new byte[pixels * 4];
        int n = Math.Min(src.Length, pixels);
        for (int i = 0, j = 0; i < n; i++, j += 4)
        {
            dst[j] = src[i];
            dst[j + 1] = src[i];
            dst[j + 2] = src[i];
            dst[j + 3] = 255;
        }
        return dst;
    }

    private static byte[] ExpandRgb565(byte[] src, int w, int h)
    {
        int pixels = w * h;
        var dst = new byte[pixels * 4];
        int n = Math.Min(src.Length, pixels * 2);
        for (int i = 0, j = 0; i + 1 < n; i += 2, j += 4)
        {
            ushort c = (ushort)(src[i] | (src[i + 1] << 8));
            dst[j] = (byte)(((c >> 11) * 255 + 15) / 31);
            dst[j + 1] = (byte)((((c >> 5) & 0x3F) * 255 + 31) / 63);
            dst[j + 2] = (byte)(((c & 0x1F) * 255 + 15) / 31);
            dst[j + 3] = 255;
        }
        return dst;
    }

    private static byte[] ExpandRgba4444(byte[] src, int w, int h)
    {
        int pixels = w * h;
        var dst = new byte[pixels * 4];
        int n = Math.Min(src.Length, pixels * 2);
        for (int i = 0, j = 0; i + 1 < n; i += 2, j += 4)
        {
            ushort c = (ushort)(src[i] | (src[i + 1] << 8));
            dst[j] = (byte)(((c >> 12) & 0xF) * 17);
            dst[j + 1] = (byte)(((c >> 8) & 0xF) * 17);
            dst[j + 2] = (byte)(((c >> 4) & 0xF) * 17);
            dst[j + 3] = (byte)((c & 0xF) * 17);
        }
        return dst;
    }

}
