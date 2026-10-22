namespace PS4PKGTool.Assets.Abstractions;

/// <summary>
/// A virtual, stream-based view of an asset's bytes. Never materialized as a
/// full byte[] — containers expose children as further IAssetSource slices so
/// multi-gigabyte packages can be browsed without loading them into RAM.
///
/// A source may represent: a PKG entry, a filesystem file, a Unity bundle
/// member, an Unreal PAK member, a PSARC member, a slice of another source,
/// or any future container member.
/// </summary>
public interface IAssetSource
{
    string Name { get; }

    long Length { get; }

    /// <summary>Human-readable provenance, e.g. "PKG entry", "PAK member", "PSARC member".</summary>
    string? SourceDescription { get; }

    /// <summary>Full seekable stream over the entire source.</summary>
    Stream OpenRead();

    /// <summary>
    /// Bounded seekable stream over a slice. Implementations must not read the
    /// whole source; only the requested range should be touched.
    /// </summary>
    Stream OpenRead(long offset, long length);
}
