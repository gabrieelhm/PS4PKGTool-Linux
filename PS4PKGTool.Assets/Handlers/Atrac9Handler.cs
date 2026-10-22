using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Errors;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Handlers;

/// <summary>
/// Sony ATRAC9 (.at9) audio header. Phase 2A: container/config metadata only —
/// PCM decode (LibAtrac9 port) is part of the audio phase. Capability model:
/// Inspect + Preview(metadata) + ExportRaw; no Decode, no ExportConverted.
/// </summary>
public sealed class Atrac9Handler : IAssetHandler, IAssetPreviewProvider
{
    public const string FormatId = "atrac9";
    private static readonly byte[] Magic = { (byte)'A', (byte)'T', (byte)'9' };

    // ATRAC9 sample-rate index table (kHz).
    private static readonly int[] SampleRates = { 8000, 11025, 12000, 16000, 22050, 24000, 32000, 44100, 48000 };

    public string Format => FormatId;

    public AssetCapabilities GetCapabilities(AssetDetectionResult detection)
        => AssetCapabilities.Inspect | AssetCapabilities.Preview | AssetCapabilities.ExportRaw;

    public bool IsContainer(AssetDetectionResult detection) => false;

    public Task<AssetDescriptor> InspectAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        using var stream = source.OpenRead(0, Math.Min(source.Length, 64));
        var head = new byte[stream.Length];
        stream.ReadExactly(head);

        if (head.Length < 14 || head[0] != 'A' || head[1] != 'T' || head[2] != '9')
            throw new CorruptAssetException("Missing ATRAC9 magic.");

        // Sony AT9 header: 3-byte magic, version, flags, config (channels,
        // sample-rate index, bitrate index, superframe/frame sizes).
        var meta = new Dictionary<string, string>
        {
            ["Version"] = head[3].ToString(),
        };

        if (head.Length >= 14)
        {
            byte channels = head[6];
            byte rateIndex = head[7];
            byte bitrateIndex = head[8];
            ushort superframeSize = BitConverter.ToUInt16(head, 10);
            ushort frameSize = BitConverter.ToUInt16(head, 12);

            meta["Channels"] = channels.ToString();
            meta["Sample Rate"] = rateIndex < SampleRates.Length ? $"{SampleRates[rateIndex]} Hz" : $"index {rateIndex}";
            meta["Bitrate Index"] = bitrateIndex.ToString();
            meta["Superframe Size"] = superframeSize.ToString();
            meta["Frame Size"] = frameSize.ToString();
            meta["Duration"] = EstimateDuration(source.Length, superframeSize, channels);
        }

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
        var descriptor = InspectAsync(source, detection, ct).GetAwaiter().GetResult();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"ATRAC9 Audio: {descriptor.Metadata.GetValueOrDefault("Channels")} ch, {descriptor.Metadata.GetValueOrDefault("Sample Rate")}");
        sb.AppendLine($"Superframe: {descriptor.Metadata.GetValueOrDefault("Superframe Size")} bytes");
        sb.AppendLine($"Frame: {descriptor.Metadata.GetValueOrDefault("Frame Size")} bytes");
        sb.AppendLine($"Duration (est): {descriptor.Metadata.GetValueOrDefault("Duration")}");
        sb.AppendLine();
        sb.AppendLine("ATRAC9 PCM decode (LibAtrac9 port) arrives in the audio phase.");
        return Task.FromResult<AssetPreview?>(AssetPreview.ForText(sb.ToString()));
    }

    public Task<IReadOnlyList<IAssetSource>> GetChildrenAsync(IAssetSource source, AssetDetectionResult detection, int depth, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<IAssetSource>>(Array.Empty<IAssetSource>());

    private static string EstimateDuration(long fileBytes, ushort superframeSize, byte channels)
    {
        if (superframeSize == 0 || channels == 0) return "?";
        // ATRAC9 superframe = 2048 samples per channel; ~64 superframes/sec at 48 kHz is
        // a rough constant — duration is an estimate for display purposes.
        double superframes = fileBytes / (double)superframeSize;
        double seconds = superframes * 2048 / 48000.0;
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
    }
}
