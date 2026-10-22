namespace PS4PKGTool.Assets.Models;

/// <summary>Platform-neutral decoded texture: RGBA8, bottom-up optional. The app
/// layer converts this to whatever the UI needs (Bitmap, etc.).</summary>
public sealed class TextureData
{
    public required int Width { get; init; }
    public required int Height { get; init; }

    /// <summary>4 bytes per pixel: R, G, B, A. Length must equal Width * Height * 4.</summary>
    public required byte[] Rgba8 { get; init; }

    public bool HasAlpha { get; init; }

    public static TextureData FromRgba(byte[] rgba8, int width, int height, bool hasAlpha = true)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (rgba8.Length != width * height * 4) throw new ArgumentException("RGBA buffer size mismatch.");
        return new TextureData { Width = width, Height = height, Rgba8 = rgba8, HasAlpha = hasAlpha };
    }
}
