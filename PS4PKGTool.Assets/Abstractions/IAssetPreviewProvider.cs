using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Abstractions;

/// <summary>
/// Optional interface implemented by handlers that can render a preview.
/// The app calls this when the descriptor's capabilities include Preview.
/// Unsupported sub-formats throw UnsupportedAssetException — the app then
/// falls back to metadata + raw export (capability model, not format-wide
/// failure).
/// </summary>
public interface IAssetPreviewProvider
{
    Task<AssetPreview?> PreviewAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default);
}
