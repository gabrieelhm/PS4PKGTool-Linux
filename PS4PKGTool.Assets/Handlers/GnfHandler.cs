using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Errors;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Handlers;

/// <summary>
/// Sony GNF texture container (PS4-native). Phase 2A: header parsing + metadata
/// only — decode (swizzle + BC) is Phase 5. Capability model: Inspect +
/// Preview(metadata) + ExportRaw.
/// </summary>
public sealed class GnfHandler : IAssetHandler, IAssetPreviewProvider
{
    public const string FormatId = "gnf";
    public const string Magic = "GNF\0";

    private static readonly Dictionary<uint, string> KnownFormats = new()
    {
        [0x06] = "R8G8B8A8_UNORM",
        [0x09] = "R8G8B8A8_UNORM_SRGB",
        [0x10] = "R32G32B32A32_FLOAT",
        [0x1B] = "BC1_UNORM",
        [0x1C] = "BC1_UNORM_SRGB",
        [0x1D] = "BC2_UNORM",
        [0x1F] = "BC3_UNORM",
        [0x23] = "BC5_UNORM",
        [0x24] = "BC6_UNORM",
        [0x25] = "BC7_UNORM",
        [0x26] = "BC7_UNORM_SRGB",
    };

    public string Format => FormatId;

    public AssetCapabilities GetCapabilities(AssetDetectionResult detection)
        => AssetCapabilities.Inspect | AssetCapabilities.Preview | AssetCapabilities.ExportRaw;

    public bool IsContainer(AssetDetectionResult detection) => false;

    public Task<AssetDescriptor> InspectAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        using var stream = source.OpenRead(0, Math.Min(source.Length, 128));
        var head = new byte[stream.Length];
        stream.ReadExactly(head);

        if (head.Length < 32 || head[0] != 'G' || head[1] != 'N' || head[2] != 'F' || head[3] != 0)
            throw new CorruptAssetException("Missing GNF magic.");

        uint version = BitConverter.ToUInt32(head, 4);
        uint textureType = BitConverter.ToUInt32(head, 8);
        uint formatRaw = BitConverter.ToUInt32(head, 12);
        uint width = BitConverter.ToUInt32(head, 16);
        uint height = BitConverter.ToUInt32(head, 20);
        uint depth = BitConverter.ToUInt32(head, 24);
        uint mips = BitConverter.ToUInt32(head, 28);

        var meta = new Dictionary<string, string>
        {
            ["Version"] = $"0x{version:X8}",
            ["Texture Type"] = textureType.ToString(),
            ["Pixel Format"] = KnownFormats.TryGetValue(formatRaw, out var name) ? name : $"0x{formatRaw:X2}",
            ["Width"] = width.ToString(),
            ["Height"] = height.ToString(),
            ["Depth"] = depth.ToString(),
            ["Mip Maps"] = mips.ToString(),
        };

        return Task.FromResult(new AssetDescriptor
        {
            Name = source.Name,
            Format = FormatId,
            Size = source.Length,
            Capabilities = GetCapabilities(detection),
            SourceDescription = source.SourceDescription,
            Metadata = meta,
        });
    }

    public Task<AssetPreview?> PreviewAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        // Metadata text preview (no texture decode in Phase 2A).
        var descriptor = InspectAsync(source, detection, ct).GetAwaiter().GetResult();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"GNF Texture: {descriptor.Metadata["Pixel Format"]}");
        sb.AppendLine($"Size: {descriptor.Metadata["Width"]} x {descriptor.Metadata["Height"]} x {descriptor.Metadata["Depth"]}");
        sb.AppendLine($"Mip Maps: {descriptor.Metadata["Mip Maps"]}");
        sb.AppendLine($"Version: {descriptor.Metadata["Version"]}");
        sb.AppendLine();
        sb.AppendLine("Texture decode (deswizzle + BC) arrives in a later phase.");
        return Task.FromResult<AssetPreview?>(AssetPreview.ForText(sb.ToString()));
    }

    public Task<IReadOnlyList<IAssetSource>> GetChildrenAsync(IAssetSource source, AssetDetectionResult detection, int depth, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<IAssetSource>>(Array.Empty<IAssetSource>());
}
