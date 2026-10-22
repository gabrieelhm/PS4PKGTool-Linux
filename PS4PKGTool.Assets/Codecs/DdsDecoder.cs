using PS4PKGTool.Assets.Errors;

namespace PS4PKGTool.Assets.Codecs;

/// <summary>
/// Minimal DDS texture decoder. Header is DDS_PIXELFORMAT (magic "DDS " +
/// 124-byte header + 32-byte pixelformat). Decodes:
///   DXT1 (BC1), DXT3 (BC2), DXT5 (BC3)  → RGBA8
/// Unsupported fourCCs (BC4/5/6/7, DX10, uncompressed variants not covered)
/// throw UnsupportedAssetException — callers keep metadata + raw export.
/// </summary>
public static class DdsDecoder
{
    public const string Magic = "DDS ";

    public static bool HasDdsMagic(ReadOnlySpan<byte> head) => head.Length >= 4 && head[0] == 'D' && head[1] == 'D' && head[2] == 'S' && head[3] == ' ';

    /// <summary>
    /// Decodes a raw BC1/BC2/BC3 (DXT1/3/5) payload with NO DDS header - e.g.
    /// image data inside a Unity texture object. Synthesizes a minimal header
    /// and decodes the first mip level.
    /// </summary>
    public static byte[] DecodeBcPayload(int width, int height, string fourCc, ReadOnlySpan<byte> payload)
    {
        if (payload.Length > int.MaxValue - 128)
            throw new UnsupportedAssetException($"Texture payload is too large ({payload.Length} bytes).");
        var dds = new byte[128 + payload.Length];
        System.Text.Encoding.ASCII.GetBytes("DDS ").CopyTo(dds, 0);
        BitConverter.GetBytes(124).CopyTo(dds, 4);                          // dwSize
        BitConverter.GetBytes(height).CopyTo(dds, 12);
        BitConverter.GetBytes(width).CopyTo(dds, 16);
        // Pitch (bytes per compressed row) computed in 64-bit to avoid an
        // overflow on attacker-controlled dimensions; the decoder ignores it.
        long stride = fourCc is "DXT1" or "BC1" ? 8 : 16;
        long pitch = ((long)(width + 3) / 4) * ((height + 3) / 4) * stride;
        BitConverter.GetBytes((int)Math.Min(int.MaxValue, pitch)).CopyTo(dds, 20);
        BitConverter.GetBytes(32).CopyTo(dds, 76);                          // DDS_PIXELFORMAT.dwSize
        System.Text.Encoding.ASCII.GetBytes(fourCc).CopyTo(dds, 84);
        payload.CopyTo(dds.AsSpan(128));
        var info = ReadHeader(dds);
        return Decode(dds, info).rgba;
    }

    public sealed class DdsInfo
    {
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required string FourCc { get; init; }
        public int MipMapCount { get; init; }
        public bool IsDx10 { get; init; }
        public string? Dx10Format { get; init; }
    }

    public static DdsInfo ReadHeader(ReadOnlySpan<byte> data)
    {
        if (data.Length < 128) throw new CorruptAssetException("DDS header too short.");
        if (!HasDdsMagic(data)) throw new CorruptAssetException("Missing DDS magic.");

        int height = BitConverter.ToInt32(data[12..16]);
        int width = BitConverter.ToInt32(data[16..20]);
        int mipMaps = BitConverter.ToInt32(data[28..32]);
        string fourCc = System.Text.Encoding.ASCII.GetString(data[84..88]).TrimEnd('\0');
        bool dx10 = fourCc == "DX10";

        string? dx10Format = null;
        if (dx10 && data.Length >= 132)
        {
            int dxgi = BitConverter.ToInt32(data[128..132]);
            dx10Format = DxgiToString(dxgi);
        }

        return new DdsInfo
        {
            Width = width,
            Height = height,
            FourCc = fourCc,
            MipMapCount = mipMaps,
            IsDx10 = dx10,
            Dx10Format = dx10Format,
        };
    }

    private static string DxgiToString(int dxgi) => dxgi switch
    {
        71 => "BC1 (DXT1)",
        74 => "BC2 (DXT3)",
        77 => "BC3 (DXT5)",
        80 => "BC7",
        95 => "R8G8B8A8_UNORM",
        28 => "R8G8B8A8_UNORM",
        _ => $"DXGI {dxgi}",
    };

    /// <summary>Decodes the base mip level to RGBA8. Only BC1/BC2/BC3 are supported.</summary>
    public static (byte[] rgba, bool hasAlpha) Decode(ReadOnlySpan<byte> data, DdsInfo info)
    {
        if (info.Width <= 0 || info.Height <= 0) throw new CorruptAssetException("DDS has invalid dimensions.");

        string fourCc = info.IsDx10 ? (info.Dx10Format ?? info.FourCc) : info.FourCc;

        return fourCc switch
        {
            "DXT1" or "BC1 (DXT1)" or "BC1" => (DecodeBc1(data, info.Width, info.Height), true),
            "DXT3" or "BC2 (DXT3)" or "BC2" => (DecodeBc2(data, info.Width, info.Height), true),
            "DXT5" or "BC3 (DXT5)" or "BC3" => (DecodeBc3(data, info.Width, info.Height), true),
            _ => throw new UnsupportedAssetException($"DDS pixel format '{fourCc}' is not supported yet (BC1/BC2/BC3 only)."),
        };
    }

    /// <summary>Upper bound on either texture dimension, to bound RGBA allocation.</summary>
    public const int MaxDimension = 8192;

    private enum BcFormat { Bc1, Bc2, Bc3 }

    private static byte[] DecodeBc1(ReadOnlySpan<byte> data, int width, int height) => DecodeBc(BcFormat.Bc1, data, width, height);
    private static byte[] DecodeBc2(ReadOnlySpan<byte> data, int width, int height) => DecodeBc(BcFormat.Bc2, data, width, height);
    private static byte[] DecodeBc3(ReadOnlySpan<byte> data, int width, int height) => DecodeBc(BcFormat.Bc3, data, width, height);

    private static byte[] DecodeBc(
        BcFormat format, ReadOnlySpan<byte> data, int width, int height)
    {
        if (width > MaxDimension || height > MaxDimension)
            throw new UnsupportedAssetException(
                $"Texture {width}x{height} exceeds the maximum supported {MaxDimension}px dimension.");

        int stride = format == BcFormat.Bc1 ? 8 : 16;
        int blocksW = (width + 3) / 4;
        int blocksH = (height + 3) / 4;
        // 64-bit math: attacker-controlled dimensions must not wrap the size
        // check and bypass the truncation guard below.
        long needed = (long)blocksW * blocksH * stride;
        if (data.Length < 128 + needed)
            throw new CorruptAssetException("DDS data is truncated for the declared dimensions.");

        bool hasAlphaThreshold = format == BcFormat.Bc1;
        var rgba = new byte[width * height * 4];
        ReadOnlySpan<byte> payload = data[128..];

        // BC2/BC3 store the alpha block first (bytes 0-7), color block second (8-15).
        int colorOffset = stride == 16 ? 8 : 0;

        for (int by = 0; by < blocksH; by++)
        {
            for (int bx = 0; bx < blocksW; bx++)
            {
                int blockStart = (by * blocksW + bx) * stride;
                ReadOnlySpan<byte> block = payload.Slice(blockStart, stride);
                ReadOnlySpan<byte> color = block.Slice(colorOffset, 8);

                ushort c0 = (ushort)(color[0] | (color[1] << 8));
                ushort c1 = (ushort)(color[2] | (color[3] << 8));
                uint indices = (uint)(color[4] | (color[5] << 8) | (color[6] << 16) | (color[7] << 24));

                (int r0, int g0, int b0) = Rgb565(c0);
                (int r1, int g1, int b1) = Rgb565(c1);

                bool punchThrough = hasAlphaThreshold && c0 <= c1;

                int[]? paletteA = null;
                if (format == BcFormat.Bc2) // BC2: explicit 4-bit alpha
                {
                    ulong alphaBits = 0;
                    for (int i = 0; i < 8; i++) alphaBits |= (ulong)block[i] << (i * 8);
                    paletteA = new int[16];
                    for (int i = 0; i < 16; i++) paletteA[i] = (int)((alphaBits >> (i * 4)) & 0xF) * 17;
                }
                else if (format == BcFormat.Bc3) // BC3: alpha block (2 refs + 6 bytes indices)
                {
                    int a0 = block[0], a1 = block[1];
                    ulong aIdx = 0;
                    for (int i = 0; i < 6; i++) aIdx |= (ulong)block[2 + i] << (i * 8);
                    int[] ap = BuildAlphaPalette(a0, a1);
                    paletteA = new int[16];
                    for (int i = 0; i < 16; i++)
                    {
                        int shift = i * 3;
                        paletteA[i] = ap[(int)((aIdx >> shift) & 0x7)];
                    }
                }

                for (int py = 0; py < 4; py++)
                {
                    for (int px = 0; px < 4; px++)
                    {
                        int xi = bx * 4 + px;
                        int yi = by * 4 + py;
                        if (xi >= width || yi >= height) continue;

                        int idx = (int)((indices >> ((py * 4 + px) * 2)) & 0x3);
                        int r, g, b, a = 255;

                        if (hasAlphaThreshold && punchThrough && idx == 3)
                        {
                            r = g = b = 0; a = 0;
                        }
                        else
                        {
                            (r, g, b) = Interpolate(c0, c1, idx, hasAlphaThreshold && c0 <= c1);
                            if (paletteA != null) a = paletteA[py * 4 + px];
                        }

                        int dst = (yi * width + xi) * 4;
                        rgba[dst] = (byte)r;
                        rgba[dst + 1] = (byte)g;
                        rgba[dst + 2] = (byte)b;
                        rgba[dst + 3] = (byte)a;
                    }
                }
            }
        }

        return rgba;
    }

    private static (int, int, int) Rgb565(ushort c)
        => (((c >> 11) * 255 + 15) / 31, (((c >> 5) & 0x3F) * 255 + 31) / 63, ((c & 0x1F) * 255 + 15) / 31);

    private static (int, int, int) Interpolate(ushort c0, ushort c1, int idx, bool fourColorMode)
    {
        var (r0, g0, b0) = Rgb565(c0);
        var (r1, g1, b1) = Rgb565(c1);
        return idx switch
        {
            0 => (r0, g0, b0),
            1 => (r1, g1, b1),
            2 when fourColorMode => ((r0 + r1) / 2, (g0 + g1) / 2, (b0 + b1) / 2),
            2 => ((2 * r0 + r1) / 3, (2 * g0 + g1) / 3, (2 * b0 + b1) / 3),
            _ => ((r0 + 2 * r1) / 3, (g0 + 2 * g1) / 3, (b0 + 2 * b1) / 3),
        };
    }

    private static int[] BuildAlphaPalette(int a0, int a1)
    {
        var p = new int[8];
        p[0] = a0; p[1] = a1;
        if (a0 > a1)
        {
            for (int i = 2; i < 8; i++) p[i] = ((8 - i) * a0 + (i - 1) * a1) / 7;
        }
        else
        {
            for (int i = 2; i < 6; i++) p[i] = ((6 - i) * a0 + (i - 1) * a1) / 5;
            p[6] = 0; p[7] = 255;
        }
        return p;
    }
}
