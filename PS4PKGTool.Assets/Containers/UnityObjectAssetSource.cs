using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Unity;

namespace PS4PKGTool.Assets.Containers;

/// <summary>
/// One object of a Unity serialized file as an IAssetSource — a virtual slice
/// into the parent file, never materialized. Carries the object reference so
/// handlers can re-read it with full class knowledge (e.g. decode a Texture2D).
/// </summary>
public sealed class UnityObjectAssetSource : IAssetSource
{
    private readonly IAssetSource _parent;

    public UnityObjectRef Object { get; }

    /// <summary>The serialized file this object lives in (needed to re-read with class knowledge).</summary>
    public IAssetSource Parent => _parent;

    public UnityObjectAssetSource(IAssetSource parent, UnityObjectRef obj)
    {
        _parent = parent;
        Object = obj;
    }

    public string Name => Object.Name;

    public long Length => Object.Size;

    public string? SourceDescription => $"Unity object (class {Object.ClassId}) in {_parent.Name}";

    public Stream OpenRead() => _parent.OpenRead(Object.Offset, Object.Size);

    public Stream OpenRead(long offset, long length)
    {
        if (offset < 0 || length < 0 || offset + length > Length)
            throw new ArgumentOutOfRangeException(nameof(length));
        return _parent.OpenRead(Object.Offset + offset, length);
    }
}
