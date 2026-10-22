using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Codecs;
using PS4PKGTool.Assets.Errors;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Handlers;

/// <summary>
/// DDS texture handler. Header inspection always works; BC1/BC2/BC3 decode for
/// preview; other pixel formats keep Inspect + ExportRaw (capability model).
/// </summary>
public sealed class DdsHandler : IAssetHandler, IAssetPreviewProvider
{
    public const string FormatId = "dds";

    public string Format => FormatId;

    public AssetCapabilities GetCapabilities(AssetDetectionResult detection)
        => AssetCapabilities.Inspect | AssetCapabilities.Preview | AssetCapabilities.Decode
           | AssetCapabilities.ExportRaw | AssetCapabilities.ExportConverted;

    public bool IsContainer(AssetDetectionResult detection) => false;

    public Task<AssetDescriptor> InspectAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        using var stream = source.OpenRead(0, Math.Min(source.Length, 132));
        var head = new byte[stream.Length];
        stream.ReadExactly(head);

        if (!DdsDecoder.HasDdsMagic(head)) throw new CorruptAssetException("Missing DDS magic.");
        var info = DdsDecoder.ReadHeader(head);

        var meta = new Dictionary<string, string>
        {
            ["Width"] = info.Width.ToString(),
            ["Height"] = info.Height.ToString(),
            ["Pixel Format"] = info.IsDx10 ? info.Dx10Format ?? "DX10" : info.FourCc,
            ["Mip Maps"] = info.MipMapCount.ToString(),
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
        using var stream = source.OpenRead();
        var data = new byte[stream.Length];
        stream.ReadExactly(data);

        var info = DdsDecoder.ReadHeader(data);
        var (rgba, hasAlpha) = DdsDecoder.Decode(data, info);
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<AssetPreview?>(AssetPreview.ForTexture(TextureData.FromRgba(rgba, info.Width, info.Height, hasAlpha)));
    }

    public Task<IReadOnlyList<IAssetSource>> GetChildrenAsync(IAssetSource source, AssetDetectionResult detection, int depth, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<IAssetSource>>(Array.Empty<IAssetSource>());
}
