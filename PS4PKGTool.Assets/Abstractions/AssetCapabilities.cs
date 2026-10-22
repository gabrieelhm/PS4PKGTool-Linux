namespace PS4PKGTool.Assets.Abstractions;

/// <summary>
/// What a handler can do with an asset. Formats are never a single binary
/// capability: a GNF with an unsupported pixel format is Inspect + ExportRaw,
/// while a plain texture is also Preview + Decode + ExportConverted.
/// </summary>
[Flags]
public enum AssetCapabilities
{
    None = 0,
    Inspect = 1 << 0,          // metadata available
    Browse = 1 << 1,           // exposes child IAssetSource (container)
    Preview = 1 << 2,          // can render a preview
    Decode = 1 << 3,           // can decode payload (e.g. texture -> pixels)
    ExportRaw = 1 << 4,        // can export the original bytes
    ExportConverted = 1 << 5,  // can export a converted form (PNG/OBJ/WAV/glTF)
}
