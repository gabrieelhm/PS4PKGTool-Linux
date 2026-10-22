using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Errors;
using PS4PKGTool.Assets.Models;
using StbImageSharp;

namespace PS4PKGTool.Assets.Handlers;

/// <summary>
/// PNG / JPEG / BMP / GIF raster images. Decodes via StbImage (MIT, managed)
/// into RGBA8 for preview; PNG/BMP/TGA export via StbImageWrite.
/// </summary>
public sealed class RasterImageHandler : IAssetHandler, IAssetPreviewProvider
{
    public const string PngFormat = "png";
    public const string JpegFormat = "jpeg";
    public const string BmpFormat = "bmp";
    public const string GifFormat = "gif";

    // Upper bound on decoded pixels: a tiny, highly-compressed image can
    // otherwise declare a colossal canvas and allocate gigabytes on decode.
    private const long MaxPixels = 64_000_000; // ~64 MP (~256 MB RGBA)

    private readonly string _format;

    public RasterImageHandler(string format) => _format = format;

    public string Format => _format;

    public AssetCapabilities GetCapabilities(AssetDetectionResult detection)
        => AssetCapabilities.Inspect | AssetCapabilities.Preview | AssetCapabilities.Decode
           | AssetCapabilities.ExportRaw | AssetCapabilities.ExportConverted;

    public bool IsContainer(AssetDetectionResult detection) => false;

    public Task<AssetDescriptor> InspectAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        using var stream = source.OpenRead(0, Math.Min(source.Length, 128));
        var head = new byte[stream.Length];
        stream.ReadExactly(head);

        int width = 0, height = 0;
        switch (_format)
        {
            case PngFormat when head.Length >= 24 && head[0] == 0x89 && head[1] == (byte)'P':
                width = (head[16] << 24) | (head[17] << 16) | (head[18] << 8) | head[19];
                height = (head[20] << 24) | (head[21] << 16) | (head[22] << 8) | head[23];
                break;
            case JpegFormat when head.Length >= 4 && head[0] == 0xFF && head[1] == 0xD8:
                break; // dimensions require scanning markers; skip for metadata
            case BmpFormat when head.Length >= 26 && head[0] == (byte)'B' && head[1] == (byte)'M':
                // BitConverter.ToInt32 can be int.MinValue, whose Math.Abs throws.
                width = ToAbsDimension(BitConverter.ToInt32(head, 18));
                height = ToAbsDimension(BitConverter.ToInt32(head, 22));
                break;
            case GifFormat when head.Length >= 10 && head[0] == (byte)'G' && head[1] == (byte)'I':
                width = BitConverter.ToInt16(head, 6);
                height = BitConverter.ToInt16(head, 8);
                break;
        }

        var meta = new Dictionary<string, string>();
        if (width > 0) meta["Width"] = width.ToString();
        if (height > 0) meta["Height"] = height.ToString();

        return Task.FromResult(new AssetDescriptor
        {
            Name = source.Name,
            Format = _format,
            Size = source.Length,
            Capabilities = GetCapabilities(detection),
            SourceDescription = source.SourceDescription,
            Metadata = meta,
        });
    }

    public Task<AssetPreview?> PreviewAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        // Probe the dimensions from the header before decoding: a tiny file can
        // declare a huge canvas, and StbImage would allocate it all at once.
        try
        {
            using var probe = source.OpenRead();
            var info = ImageInfo.FromStream(probe);
            if (info is { } meta && (long)meta.Width * meta.Height > MaxPixels)
                throw new UnsupportedAssetException(
                    $"Image {meta.Width}x{meta.Height} exceeds the {MaxPixels}-pixel preview limit.");
        }
        catch (UnsupportedAssetException) { throw; }
        catch { /* probe is best-effort; fall through to the normal decode */ }

        using var stream = source.OpenRead();
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        ct.ThrowIfCancellationRequested();
        var texture = TextureData.FromRgba(image.Data, image.Width, image.Height, hasAlpha: true);
        return Task.FromResult<AssetPreview?>(AssetPreview.ForTexture(texture));
    }

    private static int ToAbsDimension(int value)
        => value == int.MinValue ? 0 : Math.Abs(value);

    public Task<IReadOnlyList<IAssetSource>> GetChildrenAsync(IAssetSource source, AssetDetectionResult detection, int depth, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<IAssetSource>>(Array.Empty<IAssetSource>());
}
