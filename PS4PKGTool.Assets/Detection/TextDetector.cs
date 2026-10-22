using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Detection;

/// <summary>
/// Detects plain text by extension + printable-ratio probe, at medium-low
/// confidence (below magic-based detectors).
/// </summary>
public sealed class TextDetector : IAssetDetector
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xml", ".json", ".txt", ".ini", ".cfg", ".conf", ".log", ".lst", ".map",
        ".yml", ".yaml", ".md", ".csv", ".html", ".htm", ".css", ".js", ".shader",
    };

    public int Order => 10;

    public AssetDetectionResult? Detect(IAssetSource source, CancellationToken ct = default)
    {
        string ext = Path.GetExtension(source.Name);
        if (string.IsNullOrEmpty(ext)) return null;

        if (!TextExtensions.Contains(ext)) return null;

        double confidence = 0.5;
        var evidence = new List<string> { $"text extension '{ext}'" };

        if (LooksLikeText(source))
        {
            confidence = 0.8;
            evidence.Add("printable content");
        }

        return new AssetDetectionResult { Format = "text", Confidence = confidence, Evidence = evidence };
    }

    private static bool LooksLikeText(IAssetSource source)
    {
        try
        {
            using var stream = source.OpenRead(0, Math.Min(source.Length, 512));
            var buf = new byte[stream.Length];
            stream.ReadExactly(buf);
            if (buf.Length == 0) return false;
            int printable = 0;
            foreach (byte b in buf)
            {
                if (b == 0) return false;
                if (b == 9 || b == 10 || b == 13 || (b >= 32 && b < 127) || b >= 0x80) printable++;
            }
            return printable >= buf.Length * 0.9;
        }
        catch
        {
            return false;
        }
    }
}
