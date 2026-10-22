using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Errors;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Handlers;

/// <summary>
/// WAV / OGG metadata + text preview. Phase 1 does not decode audio — the
/// capability model expresses this (Inspect + Preview(metadata) + ExportRaw;
/// no Decode, no ExportConverted). Full decode lands with the audio phase.
/// </summary>
public sealed class AudioMetadataHandler : IAssetHandler, IAssetPreviewProvider
{
    public const string WavFormat = "wav";
    public const string OggFormat = "ogg";

    private readonly string _format;

    public AudioMetadataHandler(string format) => _format = format;

    public string Format => _format;

    public AssetCapabilities GetCapabilities(AssetDetectionResult detection)
        => AssetCapabilities.Inspect | AssetCapabilities.Preview | AssetCapabilities.ExportRaw;

    public bool IsContainer(AssetDetectionResult detection) => false;

    public Task<AssetDescriptor> InspectAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        using var stream = source.OpenRead(0, Math.Min(source.Length, 256));
        var head = new byte[stream.Length];
        stream.ReadExactly(head);

        var meta = new Dictionary<string, string>();
        if (_format == WavFormat && head.Length >= 44
            && head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F'
            && head[8] == 'W' && head[9] == 'A' && head[10] == 'V' && head[11] == 'E')
        {
            short channels = BitConverter.ToInt16(head, 22);
            int sampleRate = BitConverter.ToInt32(head, 24);
            short bits = BitConverter.ToInt16(head, 34);
            meta["Channels"] = channels.ToString();
            meta["Sample Rate"] = $"{sampleRate} Hz";
            meta["Bit Depth"] = $"{bits}-bit";
            meta["Duration"] = FormatDuration(source.Length, sampleRate, channels, bits);
        }
        else if (_format == OggFormat && head.Length >= 28
                 && head[0] == 'O' && head[1] == 'g' && head[2] == 'g' && head[3] == 'S')
        {
            meta["Ogg Version"] = head[4].ToString();
            meta["Channels"] = head[27].ToString();
            meta["Sample Rate"] = $"{BitConverter.ToUInt32(head, 24)} Hz";
        }
        else
        {
            throw new CorruptAssetException($"File is not a valid {_format.ToUpperInvariant()} file.");
        }

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
        // Text preview of the metadata (no audio decode in Phase 1).
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(source.Name);
        sb.AppendLine($"Size: {source.Length} bytes");
        using var stream = source.OpenRead(0, Math.Min(source.Length, 256));
        var head = new byte[stream.Length];
        stream.ReadExactly(head);

        if (_format == WavFormat && head.Length >= 44)
        {
            sb.AppendLine($"Channels: {BitConverter.ToInt16(head, 22)}");
            sb.AppendLine($"Sample Rate: {BitConverter.ToInt32(head, 24)} Hz");
            sb.AppendLine($"Bit Depth: {BitConverter.ToInt16(head, 34)}-bit");
        }
        else if (_format == OggFormat && head.Length >= 28)
        {
            sb.AppendLine($"Sample Rate: {BitConverter.ToUInt32(head, 24)} Hz");
            sb.AppendLine($"Channels: {head[27]}");
        }

        return Task.FromResult<AssetPreview?>(AssetPreview.ForText(sb.ToString()));
    }

    public Task<IReadOnlyList<IAssetSource>> GetChildrenAsync(IAssetSource source, AssetDetectionResult detection, int depth, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<IAssetSource>>(Array.Empty<IAssetSource>());

    private static string FormatDuration(long byteLength, int sampleRate, short channels, short bits)
    {
        if (sampleRate <= 0 || channels <= 0 || bits <= 0) return "?";
        long bytesPerSecond = (long)sampleRate * channels * (bits / 8);
        if (bytesPerSecond <= 0) return "?";
        double seconds = byteLength / (double)bytesPerSecond;
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
    }
}
