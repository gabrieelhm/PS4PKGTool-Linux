using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Containers;
using PS4PKGTool.Assets.Errors;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Handlers;

/// <summary>
/// Unity asset bundle container handler. Lists bundle members as child
/// IAssetSources (members are then inspected by the generic handlers — a
/// member PNG previews as PNG, etc.). Phase 3a: container + member listing;
/// Texture2D/Sprite class-level decoding lands with the AssetStudio adapter.
/// </summary>
public sealed class UnityBundleHandler : IAssetHandler, IAssetPreviewProvider
{
    public const string FormatId = "unity-bundle";

    public string Format => FormatId;

    public AssetCapabilities GetCapabilities(AssetDetectionResult detection)
        => AssetCapabilities.Inspect | AssetCapabilities.Browse | AssetCapabilities.Preview | AssetCapabilities.ExportRaw;

    public bool IsContainer(AssetDetectionResult detection) => true;

    public Task<AssetDescriptor> InspectAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        var bundle = UnityBundle.Parse(source);
        var meta = new Dictionary<string, string>
        {
            ["Version"] = bundle.Version.ToString(),
            ["Files"] = bundle.Files.Count.ToString(),
        };
        if (!string.IsNullOrEmpty(bundle.UnityVersion)) meta["Unity Version"] = bundle.UnityVersion!;
        if (bundle.AnyCompressedBlocks) meta["Compression"] = "LZMA/LZ4 (unsupported yet)";

        return Task.FromResult(new AssetDescriptor
        {
            Name = source.Name,
            Format = FormatId,
            Engine = "unity",
            Size = source.Length,
            Capabilities = GetCapabilities(detection),
            SourceDescription = source.SourceDescription,
            Metadata = meta,
        });
    }

    public Task<AssetPreview?> PreviewAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        // Container listing as a text preview (nested browsing data).
        var bundle = UnityBundle.Parse(source);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Unity Asset Bundle: {bundle.Files.Count} files");
        sb.AppendLine($"Bundle version: {bundle.Version}{(string.IsNullOrEmpty(bundle.UnityVersion) ? "" : $" (Unity {bundle.UnityVersion})")}");
        sb.AppendLine();
        foreach (var f in bundle.Files)
            sb.AppendLine($"  {f.Name} ({f.Size} bytes)");
        return Task.FromResult<AssetPreview?>(AssetPreview.ForText(sb.ToString()));
    }

    public Task<IReadOnlyList<IAssetSource>> GetChildrenAsync(IAssetSource source, AssetDetectionResult detection, int depth, CancellationToken ct = default)
    {
        var bundle = UnityBundle.Parse(source);
        var children = new List<IAssetSource>(bundle.Files.Count);
        foreach (var member in bundle.Files)
            children.Add(bundle.OpenEntry(source, member));
        return Task.FromResult<IReadOnlyList<IAssetSource>>(children);
    }
}
