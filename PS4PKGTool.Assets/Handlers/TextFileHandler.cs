using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Handlers;

/// <summary>
/// Plain text files (XML/JSON/TXT/INI/LOG/...). Preview reads a bounded head;
/// the full file is available via ExportRaw. Encoding is detected permissively.
/// </summary>
public sealed class TextFileHandler : IAssetHandler, IAssetPreviewProvider
{
    public const string FormatId = "text";

    private const int PreviewHeadBytes = 256 * 1024;

    public string Format => FormatId;

    public AssetCapabilities GetCapabilities(AssetDetectionResult detection)
        => AssetCapabilities.Inspect | AssetCapabilities.Preview | AssetCapabilities.ExportRaw;

    public bool IsContainer(AssetDetectionResult detection) => false;

    public Task<AssetDescriptor> InspectAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        // Estimate whether the head looks like UTF-8/ASCII text.
        bool looksText = LooksLikeText(source);
        var meta = new Dictionary<string, string>
        {
            ["Encoding"] = looksText ? "text" : "binary",
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
        long head = Math.Min(source.Length, PreviewHeadBytes);
        using var stream = source.OpenRead(0, head);
        var buf = new byte[head];
        stream.ReadExactly(buf);

        // Strip UTF-8 BOM if present.
        int start = buf.Length >= 3 && buf[0] == 0xEF && buf[1] == 0xBB && buf[2] == 0xBF ? 3 : 0;
        string text = System.Text.Encoding.UTF8.GetString(buf, start, buf.Length - start);

        // Defense in depth: a leading NUL makes native textboxes render empty
        // (the string is truncated at the first NUL) - strip a leading run of
        // them so previews always show the real text.
        int nul = 0;
        while (nul < text.Length && text[nul] == '\0') nul++;
        if (nul > 0) text = text.Substring(nul);

        string info = source.Length > PreviewHeadBytes
            ? $"Showing first {HelperFormatBytes(PreviewHeadBytes)} of {HelperFormatBytes(source.Length)}"
            : $"{HelperFormatBytes(source.Length)}";

        return Task.FromResult<AssetPreview?>(AssetPreview.ForText(text, info));
    }

    public Task<IReadOnlyList<IAssetSource>> GetChildrenAsync(IAssetSource source, AssetDetectionResult detection, int depth, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<IAssetSource>>(Array.Empty<IAssetSource>());

    private static bool LooksLikeText(IAssetSource source)
    {
        try
        {
            using var stream = source.OpenRead(0, Math.Min(source.Length, 512));
            var buf = new byte[stream.Length];
            stream.ReadExactly(buf);
            int printable = 0;
            foreach (byte b in buf)
            {
                if (b == 0) return false; // NUL is a strong binary signal
                if (b == 9 || b == 10 || b == 13 || (b >= 32 && b < 127) || b >= 0x80) printable++;
            }
            return printable >= buf.Length * 0.95;
        }
        catch
        {
            return false;
        }
    }

    private static string HelperFormatBytes(long bytes)
        => bytes >= 1024 * 1024 ? $"{bytes / 1024.0 / 1024.0:0.##} MB"
         : bytes >= 1024 ? $"{bytes / 1024.0:0.##} KB"
         : $"{bytes} bytes";
}
