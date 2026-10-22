using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Abstractions;

/// <summary>
/// Multi-stage detector: extension + path context + magic + container probe.
/// Returns null when it has no opinion; otherwise a confidence-scored result.
/// </summary>
public interface IAssetDetector
{
    /// <summary>Order in the registry; lower runs first. Equal confidence → lower order wins.</summary>
    int Order { get; }

    AssetDetectionResult? Detect(IAssetSource source, CancellationToken ct = default);
}
