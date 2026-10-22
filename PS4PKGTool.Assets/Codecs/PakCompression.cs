using System.IO.Compression;
using K4os.Compression.LZ4;
using PS4PKGTool.Assets.Errors;

namespace PS4PKGTool.Assets.Codecs;

/// <summary>
/// PAK entry decompression: None (raw), Zlib (RFC 1950 wrapper over raw
/// deflate), Gzip, LZ4 block (UE4's FLZ4). Oodle is intentionally absent -
/// it surfaces as a missing-dependency error (never auto-downloaded).
/// </summary>
public static class PakCompression
{
    public const long MaxDecompressed = 256L * 1024 * 1024; // guard against decompression bombs

    public static byte[] Decompress(string method, ReadOnlySpan<byte> data, long uncompressedSize)
    {
        if (uncompressedSize < 0)
            throw new CorruptAssetException($"Entry declares a negative uncompressed size ({uncompressedSize}).");
        if (uncompressedSize > MaxDecompressed)
            throw new UnsupportedAssetException(
                $"Entry decompresses to {uncompressedSize} bytes - over the {MaxDecompressed} safety cap.");

        switch (method)
        {
            case "None":
                return data.ToArray();
            case "Zlib":
                return DecompressZlib(data, uncompressedSize);
            case "Gzip":
                return DecompressGzip(data, uncompressedSize);
            case "LZ4":
                return DecompressLz4(data, uncompressedSize);
            case "Oodle":
                throw new MissingDependencyException(
                    "Entry uses Oodle compression - the Oodle DLL is not bundled (see ASSET_FRAMEWORK.md). Raw export of the compressed bytes remains available.");
            default:
                throw new UnsupportedAssetException($"Unknown compression method '{method}'.");
        }
    }

    /// <summary>RFC 1950 zlib: 2-byte header (CMF/FLG), raw deflate stream, optional adler32 tail.</summary>
    private static byte[] DecompressZlib(ReadOnlySpan<byte> data, long expected)
    {
        if (data.Length < 6 || (data[0] & 0x0F) != 8)
            throw new CorruptAssetException("Invalid zlib header.");
        using var input = new MemoryStream(data[2..].ToArray());
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        return ReadAll(deflate, expected);
    }

    private static byte[] DecompressGzip(ReadOnlySpan<byte> data, long expected)
    {
        using var input = new MemoryStream(data.ToArray());
        using var gz = new GZipStream(input, CompressionMode.Decompress);
        return ReadAll(gz, expected);
    }

    private static byte[] DecompressLz4(ReadOnlySpan<byte> data, long expected)
    {
        var output = new byte[expected];
        int written = LZ4Codec.Decode(data.ToArray(), 0, data.Length, output, 0, output.Length);
        if (written != expected)
            throw new CorruptAssetException($"LZ4 decode wrote {written} bytes, expected {expected}.");
        return output;
    }

    private static byte[] ReadAll(Stream s, long expected)
    {
        // Never trust the declared size: stream through a bounded loop and
        // abort the instant the output exceeds the promised (or safety-capped)
        // length, so a small declared size cannot be used to inflate a
        // decompression bomb into memory.
        long limit = expected > 0 ? expected : MaxDecompressed;
        using var ms = new MemoryStream(limit <= int.MaxValue ? (int)limit : 0);
        byte[] buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > limit)
                throw new CorruptAssetException(
                    $"Decompressed data exceeds the declared size of {expected} bytes.");
            ms.Write(buffer, 0, read);
        }
        if (ms.Length != expected)
            throw new CorruptAssetException($"Decompressed size mismatch: got {ms.Length}, expected {expected}.");
        return ms.ToArray();
    }
}
