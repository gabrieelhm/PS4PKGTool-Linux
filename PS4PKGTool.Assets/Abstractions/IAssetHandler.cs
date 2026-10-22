using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Abstractions;

/// <summary>
/// Parses and inspects one format family. Handlers are untrusted parsing code:
/// all failures must surface as structured <see cref="Errors.AssetException"/>
/// subtypes — never raw exceptions.
/// </summary>
public interface IAssetHandler
{
    /// <summary>Format id this handler owns, e.g. "dds".</summary>
    string Format { get; }

    /// <summary>What this handler can do for a given detected result.</summary>
    AssetCapabilities GetCapabilities(AssetDetectionResult detection);

    /// <summary>
    /// Build a descriptor for the source. MUST NOT load the whole source into
    /// memory; metadata reads go through bounded slices.
    /// </summary>
    Task<AssetDescriptor> InspectAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default);

    /// <summary>True when the source is a container exposing children.</summary>
    bool IsContainer(AssetDetectionResult detection);

    /// <summary>Enumerate children (only when IsContainer). Depth limit enforced by the caller.</summary>
    Task<IReadOnlyList<IAssetSource>> GetChildrenAsync(IAssetSource source, AssetDetectionResult detection, int depth, CancellationToken ct = default);
}
