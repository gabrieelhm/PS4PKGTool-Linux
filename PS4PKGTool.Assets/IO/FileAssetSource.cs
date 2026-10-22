using PS4PKGTool.Assets.Abstractions;

namespace PS4PKGTool.Assets.IO;

/// <summary>
/// IAssetSource over a physical filesystem file. Optionally resolves companion
/// files (e.g. Unity .resS stream data extracted next to a .assets file).
/// </summary>
public sealed class FileAssetSource : IAssetSource, ICompanionResolverSource
{
    private readonly string _path;
    private readonly Func<string, IAssetSource?>? _companionResolver;

    public FileAssetSource(string path, string? sourceDescription = "filesystem file",
        Func<string, IAssetSource?>? companionResolver = null)
    {
        _path = path;
        Name = Path.GetFileName(path);
        Length = new FileInfo(path).Length;
        SourceDescription = sourceDescription;
        _companionResolver = companionResolver;
    }

    /// <summary>Resolves the companion via the configured delegate, or null.</summary>
    public IAssetSource? ResolveCompanion(string relativePath)
        => _companionResolver?.Invoke(relativePath);

    public string Name { get; }
    public long Length { get; }
    public string? SourceDescription { get; }

    public Stream OpenRead() => new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);

    public Stream OpenRead(long offset, long length)
    {
        var fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            fs.Seek(offset, SeekOrigin.Begin);
            return new BoundedReadStream(fs, length, leaveOpen: false);
        }
        catch
        {
            fs.Dispose();
            throw;
        }
    }
}
