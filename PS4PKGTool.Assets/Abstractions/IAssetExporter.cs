using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Abstractions;

/// <summary>
/// Exports an asset. ExportRaw is the universal fallback (copy the source
/// bytes); ExportConverted produces PNG/OBJ/WAV/glTF depending on the handler.
/// </summary>
public interface IAssetExporter
{
    /// <summary>True when this exporter can produce the given capability for the format.</summary>
    bool CanExport(string format, AssetCapabilities capability);

    /// <summary>Copies the source bytes to the destination path (universal fallback).</summary>
    Task ExportRawAsync(IAssetSource source, AssetDescriptor descriptor, string destinationPath, CancellationToken ct = default);

    /// <summary>Converts and writes (PNG/OBJ/WAV/glTF...). Throws UnsupportedAssetException when not possible.</summary>
    Task ExportConvertedAsync(IAssetSource source, AssetDescriptor descriptor, string destinationPath, CancellationToken ct = default);
}
