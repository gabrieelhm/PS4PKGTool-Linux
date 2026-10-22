using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Registry;

/// <summary>Maps detected formats to handlers. Unknown formats resolve to null —
/// callers fall back to Inspect + ExportRaw.</summary>
public sealed class AssetHandlerRegistry
{
    private readonly Dictionary<string, IAssetHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAssetHandler handler) => _handlers[handler.Format] = handler;

    /// <summary>Registers one handler under multiple format ids (e.g. one raster
    /// handler for png/jpeg/bmp/gif).</summary>
    public void Register(IAssetHandler handler, params string[] formats)
    {
        if (formats.Length == 0) { Register(handler); return; }
        foreach (var f in formats) _handlers[f] = handler;
    }

    public IAssetHandler? Get(string format) => _handlers.TryGetValue(format, out var h) ? h : null;

    public bool TryGet(string format, out IAssetHandler? handler) => _handlers.TryGetValue(format, out handler);

    public IReadOnlyCollection<string> Formats => _handlers.Keys;
}
