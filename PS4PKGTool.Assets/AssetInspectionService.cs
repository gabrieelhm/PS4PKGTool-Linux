using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Detection;
using PS4PKGTool.Assets.Models;
using PS4PKGTool.Assets.Registry;

namespace PS4PKGTool.Assets;

/// <summary>
/// Top-level entry point: detect → resolve handler → inspect, and enumerate
/// container children with a configurable depth limit (guards against
/// recursion/cycles/pathological nesting).
/// </summary>
public sealed class AssetInspectionService
{
    public const int DefaultMaxDepth = 8;

    private readonly AssetDetectorRegistry _detectors;
    private readonly AssetHandlerRegistry _handlers;

    public AssetInspectionService(AssetDetectorRegistry detectors, AssetHandlerRegistry handlers, int maxDepth = DefaultMaxDepth)
    {
        _detectors = detectors;
        _handlers = handlers;
        MaxDepth = maxDepth;
    }

    public int MaxDepth { get; }

    /// <summary>Runs detection only. Null when nothing recognizes the source.</summary>
    public AssetDetectionResult? Detect(IAssetSource source, CancellationToken ct = default)
        => _detectors.Detect(source, ct);

    /// <summary>Detect and inspect. Returns null when no detector recognizes the source.</summary>
    public async Task<AssetDescriptor?> InspectAsync(IAssetSource source, CancellationToken ct = default)
    {
        var detection = _detectors.Detect(source, ct);
        if (detection == null) return null;
        return await InspectAsync(source, detection, ct).ConfigureAwait(false);
    }

    public async Task<AssetDescriptor> InspectAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        var handler = _handlers.Get(detection.Format);
        if (handler == null)
        {
            // Unknown format: universal fallback — inspect + raw export.
            return new AssetDescriptor
            {
                Name = source.Name,
                Format = detection.Format,
                Engine = detection.Engine,
                Size = source.Length,
                Capabilities = AssetCapabilities.Inspect | AssetCapabilities.ExportRaw,
                SourceDescription = source.SourceDescription,
                Metadata = new Dictionary<string, string>
                {
                    ["Detection"] = string.Join("; ", detection.Evidence),
                },
            };
        }
        return await handler.InspectAsync(source, detection, ct).ConfigureAwait(false);
    }

    /// <summary>True when the detected format has a handler that exposes children.</summary>
    public bool IsContainer(AssetDetectionResult detection)
    {
        var handler = _handlers.Get(detection.Format);
        return handler != null && handler.IsContainer(detection);
    }

    public async Task<IReadOnlyList<IAssetSource>> GetChildrenAsync(
        IAssetSource source,
        AssetDetectionResult detection,
        int depth,
        CancellationToken ct = default)
    {
        if (depth >= MaxDepth)
            throw new Errors.AssetException($"Container nesting exceeds the depth limit of {MaxDepth}.");

        var handler = _handlers.Get(detection.Format);
        if (handler == null || !handler.IsContainer(detection))
            return Array.Empty<IAssetSource>();
        return await handler.GetChildrenAsync(source, detection, depth, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Asks the handler for a preview. Returns null when the format has no
    /// preview provider; unsupported sub-formats surface as
    /// UnsupportedAssetException (callers fall back to metadata + raw export).
    /// </summary>
    public Task<AssetPreview?> TryPreviewAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        var handler = _handlers.Get(detection.Format);
        if (handler is IAssetPreviewProvider provider)
            return provider.PreviewAsync(source, detection, ct);
        return Task.FromResult<AssetPreview?>(null);
    }
}
