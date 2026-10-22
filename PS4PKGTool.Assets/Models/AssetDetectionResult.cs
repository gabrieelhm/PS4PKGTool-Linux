namespace PS4PKGTool.Assets.Models;

/// <summary>
/// Outcome of multi-stage detection. Confidence is 0..1; the registry picks the
/// best-scoring detector. Evidence lists why the detector believes its answer.
/// </summary>
public sealed class AssetDetectionResult
{
    public required string Format { get; init; }

    public string? Engine { get; init; }

    /// <summary>0..1 — how sure the detector is.</summary>
    public double Confidence { get; init; }

    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();
}
