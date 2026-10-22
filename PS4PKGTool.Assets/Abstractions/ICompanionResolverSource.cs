namespace PS4PKGTool.Assets.Abstractions;

/// <summary>
/// Optional capability of an IAssetSource: resolve a companion file whose name
/// is relative to this source (e.g. Unity's ".assets.resS" stream data next to
/// ".assets"). The app sets this up when it extracts companions alongside the
/// main entry; handlers use it to read streamed payloads without filesystem
/// knowledge.
/// </summary>
public interface ICompanionResolverSource
{
    /// <summary>Resolves a relative companion name to a source, or null when unavailable.</summary>
    IAssetSource? ResolveCompanion(string relativePath);
}
