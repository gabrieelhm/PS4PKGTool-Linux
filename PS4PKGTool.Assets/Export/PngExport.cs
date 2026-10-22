using PS4PKGTool.Assets.Models;
using StbImageWriteSharp;

namespace PS4PKGTool.Assets.Export;

/// <summary>
/// Writes neutral RGBA8 texture data to PNG (StbImageWriteSharp, MIT, managed).
/// </summary>
public static class PngExport
{
    public static void Write(TextureData tex, string path)
    {
        byte[] png = Encode(tex);
        File.WriteAllBytes(path, png);
    }

    public static byte[] Encode(TextureData tex)
    {
        using var ms = new MemoryStream();
        var writer = new ImageWriter();
        writer.WritePng(tex.Rgba8, tex.Width, tex.Height, ColorComponents.RedGreenBlueAlpha, ms);
        return ms.ToArray();
    }
}
