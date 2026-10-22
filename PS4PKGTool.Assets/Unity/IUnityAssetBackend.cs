using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Unity;

/// <summary>
/// Neutral reference to one Unity object inside a serialized file. Parser
/// implementations must never leak their own types past this record.
/// </summary>
public sealed record UnityObjectRef(string Name, int ClassId, long PathId, long Offset, long Size);

/// <summary>
/// Neutral texture descriptor, independent of the Unity parser implementation.
/// Either InlineBytes is set (data embedded in the object) or the Stream*
/// fields reference the external .resS file.
/// </summary>
public sealed class UnityTextureInfo
{
    public required string Name { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int Format { get; init; }   // Unity TextureFormat enum value
    public int MipCount { get; init; }
    public byte[]? InlineBytes { get; init; }
    public string? StreamPath { get; init; }
    public long StreamOffset { get; init; }
    public uint StreamSize { get; init; }

    /// <summary>Copy with the image bytes supplied (e.g. read from the .resS companion).</summary>
    public UnityTextureInfo WithInlineBytes(byte[] bytes) => new()
    {
        Name = Name,
        Width = Width,
        Height = Height,
        Format = Format,
        MipCount = MipCount,
        InlineBytes = bytes,
        StreamPath = StreamPath,
        StreamOffset = StreamOffset,
        StreamSize = StreamSize,
    };
}

/// <summary>
/// Unity parsing seam. The framework (and the app) depend only on this
/// interface; the concrete parser behind it can be swapped without touching
/// callers. Current implementation: AssetStudioBackend (self-written parser
/// matching AssetStudio's verified class layouts). A future AssetRipperBackend
/// or a full AssetStudio-core embedding would implement the same contract.
/// </summary>
public interface IUnityAssetBackend
{
    /// <summary>Human-readable parser identity, shown in metadata.</summary>
    string Name { get; }

    /// <summary>Enumerates the objects of a Unity serialized file (.assets).</summary>
    IReadOnlyList<UnityObjectRef> ListObjects(IAssetSource source);

    /// <summary>Reads a Texture2D object's fields. Null when the object is not a texture.</summary>
    UnityTextureInfo? ReadTexture2D(IAssetSource source, UnityObjectRef obj);

    /// <summary>
    /// Decodes a texture to neutral RGBA8. Null when the format is not
    /// decodeable yet (capability model - callers keep metadata + raw export).
    /// </summary>
    TextureData? DecodeTexture(UnityTextureInfo info);
}
