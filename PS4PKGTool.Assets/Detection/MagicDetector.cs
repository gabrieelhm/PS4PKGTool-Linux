using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Detection;

/// <summary>
/// Detects by leading magic bytes with high confidence. Reads only a bounded
/// head (never the full source).
/// </summary>
public sealed class MagicDetector : IAssetDetector
{
    private static readonly (byte[] magic, string format, string? engine)[] Table =
    {
        (new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "png", null),
        (new byte[] { 0xFF, 0xD8, 0xFF }, "jpeg", null),
        (new byte[] { 0x42, 0x4D }, "bmp", null),                       // "BM"
        (new byte[] { 0x47, 0x49, 0x46, 0x38 }, "gif", null),           // "GIF8"
        (new byte[] { 0x44, 0x44, 0x53, 0x20 }, "dds", null),           // "DDS "
        (new byte[] { 0x52, 0x49, 0x46, 0x46 }, "riff-container", null),// RIFF — disambiguated below
        (new byte[] { 0x4F, 0x67, 0x67, 0x53 }, "ogg", null),           // "OggS"
        (new byte[] { 0x47, 0x4E, 0x46, 0x00 }, "gnf", null),           // "GNF\0" — PS4 texture
        (new byte[] { 0x41, 0x54, 0x39 }, "atrac9", null),              // "AT9" — ATRAC9 audio
        (new byte[] { 0x55, 0x6E, 0x69, 0x74, 0x79, 0x46, 0x53, 0x00 }, "unity-bundle", "unity"), // "UnityFS\0"
        (new byte[] { 0x55, 0x6E, 0x69, 0x74, 0x79, 0x52, 0x61, 0x77, 0x00 }, "unity-bundle", "unity"), // "UnityRaw\0"
        (new byte[] { 0x55, 0x6E, 0x69, 0x74, 0x79, 0x57, 0x65, 0x62, 0x00 }, "unity-bundle", "unity"), // "UnityWeb\0"
    };

    public int Order => 0;

    public AssetDetectionResult? Detect(IAssetSource source, CancellationToken ct = default)
    {
        using var stream = source.OpenRead(0, Math.Min(source.Length, 12));
        var head = new byte[stream.Length];
        if (stream.Length == 0) return null;
        stream.ReadExactly(head);

        foreach (var (magic, format, engine) in Table)
        {
            ct.ThrowIfCancellationRequested();
            if (head.Length < magic.Length || !head.AsSpan(0, magic.Length).SequenceEqual(magic))
                continue;

            // RIFF is a container: check the sub-format fourCC (WAVE etc.).
            if (format == "riff-container")
            {
                if (head.Length >= 12 && head[8] == 'W' && head[9] == 'A' && head[10] == 'V' && head[11] == 'E')
                    return new AssetDetectionResult { Format = "wav", Confidence = 0.95, Evidence = new[] { "RIFF/WAVE magic" } };
                return new AssetDetectionResult { Format = "riff", Confidence = 0.5, Evidence = new[] { "RIFF magic (unknown sub-format)" } };
            }

            return new AssetDetectionResult { Format = format, Engine = engine, Confidence = 0.95, Evidence = new[] { $"{format} magic bytes" } };
        }

        return null;
    }
}
