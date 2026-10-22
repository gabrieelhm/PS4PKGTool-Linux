using PS4PKGTool.Assets.Abstractions;

namespace PS4PKGTool.Assets.Models;

/// <summary>Neutral description of an asset — engine/library types never leak into this.</summary>
public sealed class AssetDescriptor
{
    public required string Name { get; init; }

    /// <summary>Machine format id, e.g. "unity-serialized-file", "unreal-uasset", "dds", "gnf", "wem".</summary>
    public required string Format { get; init; }

    /// <summary>Optional engine id: "unity", "unreal", "ps4", null for generic.</summary>
    public string? Engine { get; init; }

    public long Size { get; init; }

    public AssetCapabilities Capabilities { get; init; } = AssetCapabilities.Inspect;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public string? SourceDescription { get; init; }

    public override string ToString() => $"{Name} [{Format}]{(Engine is null ? "" : $" ({Engine})")}";
}
