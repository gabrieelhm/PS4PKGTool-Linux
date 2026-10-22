using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets;

/// <summary>Universal raw exporter — copies source bytes to a destination file.
/// Works for ANY format, including unknown ones (Inspect + ExportRaw fallback).</summary>
public sealed class RawAssetExporter : IAssetExporter
{
    public static readonly RawAssetExporter Instance = new();

    public bool CanExport(string format, AssetCapabilities capability)
        => capability == AssetCapabilities.ExportRaw;

    public async Task ExportRawAsync(IAssetSource source, AssetDescriptor descriptor, string destinationPath, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        await using var src = source.OpenRead();
        await using var dst = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await src.CopyToAsync(dst, 81920, ct).ConfigureAwait(false);
    }

    public Task ExportConvertedAsync(IAssetSource source, AssetDescriptor descriptor, string destinationPath, CancellationToken ct = default)
        => throw new Errors.UnsupportedAssetException($"No converted export available for format '{descriptor.Format}'.");
}
