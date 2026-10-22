namespace PS4PKGTool.Assets.Models;

/// <summary>Neutral preview result. Exactly one of Texture/Text is set (or neither,
/// when the handler cannot produce a preview).</summary>
public sealed class AssetPreview
{
    public TextureData? Texture { get; init; }
    public string? Text { get; init; }
    public string? Info { get; init; }

    public static AssetPreview ForTexture(TextureData texture) => new() { Texture = texture };

    public static AssetPreview ForText(string text, string? info = null) => new() { Text = text, Info = info };
}
