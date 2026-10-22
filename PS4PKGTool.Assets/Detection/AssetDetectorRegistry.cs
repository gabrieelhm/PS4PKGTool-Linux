using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Detection;

/// <summary>
/// Runs all registered detectors and returns the best-scoring result.
/// Ties break by Order (lower first). A detector throwing is treated as
/// "no opinion" for that detector — one broken detector must not break
/// detection for everyone.
/// </summary>
public sealed class AssetDetectorRegistry
{
    private readonly List<IAssetDetector> _detectors = new();

    public void Register(IAssetDetector detector) => _detectors.Add(detector);

    public void RegisterRange(IEnumerable<IAssetDetector> detectors) => _detectors.AddRange(detectors);

    public IReadOnlyList<IAssetDetector> Detectors => _detectors;

    public AssetDetectionResult? Detect(IAssetSource source, CancellationToken ct = default)
    {
        AssetDetectionResult? best = null;
        foreach (var detector in _detectors.OrderBy(d => d.Order))
        {
            ct.ThrowIfCancellationRequested();
            AssetDetectionResult? result;
            try
            {
                result = detector.Detect(source, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                continue; // a broken detector yields no opinion
            }

            if (result == null) continue;
            if (best == null || result.Confidence > best.Confidence)
                best = result;
        }
        return best;
    }
}
