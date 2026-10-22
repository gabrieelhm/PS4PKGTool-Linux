using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Detection;

/// <summary>
/// Last-resort detector: claims any unrecognized source by its file extension
/// at very low confidence, so unknown formats still resolve to a descriptor
/// with Inspect + ExportRaw rather than "no detection at all".
/// </summary>
public sealed class ExtensionFallbackDetector : IAssetDetector
{
    public int Order => 1000; // always last

    public AssetDetectionResult? Detect(IAssetSource source, CancellationToken ct = default)
    {
        string ext = Path.GetExtension(source.Name);
        if (string.IsNullOrEmpty(ext)) return null;
        return new AssetDetectionResult
        {
            Format = ext.TrimStart('.').ToLowerInvariant(),
            Confidence = 0.1,
            Evidence = new[] { "extension fallback" },
        };
    }
}
