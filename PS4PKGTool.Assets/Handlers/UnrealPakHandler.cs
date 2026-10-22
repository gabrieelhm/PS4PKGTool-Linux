using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Containers;
using PS4PKGTool.Assets.Models;
using PS4PKGTool.Assets.Unreal;

namespace PS4PKGTool.Assets.Handlers;

/// <summary>
/// Unreal Engine PAK container handler. Detects via the trailer magic, inspects
/// (version/engine/entry count/compression mix), previews as an entry listing,
/// and exposes entries as child sources (decompressed). Oodle/encrypted entries
/// surface as structured errors through PakCompression/UnrealPakBackend - the
/// capability model, not failures.
/// </summary>
public sealed class UnrealPakHandler : IAssetHandler, IAssetPreviewProvider
{
    public const string FormatId = "unreal-pak";

    private const int ListingCap = 300;

    private readonly IUnrealAssetBackend _backend;

    public UnrealPakHandler() : this(new UnrealPakBackend()) { }

    public UnrealPakHandler(IUnrealAssetBackend backend) => _backend = backend;

    public string Format => FormatId;

    public AssetCapabilities GetCapabilities(AssetDetectionResult detection)
        => AssetCapabilities.Inspect | AssetCapabilities.Browse | AssetCapabilities.Preview | AssetCapabilities.ExportRaw;

    public bool IsContainer(AssetDetectionResult detection) => true;

    public Task<AssetDescriptor> InspectAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        var (header, _) = UnrealPak.Parse(source);
        var entries = _backend.ListEntries(source);

        var meta = new Dictionary<string, string>
        {
            ["Version"] = header.Version.ToString(),
            ["Engine"] = header.Engine,
            ["Entries"] = entries.Count.ToString(),
            ["Header"] = header.HeaderStripped ? "stripped" : "present",
            ["Backend"] = _backend.Name,
        };
        if (header.SkippedEntries > 0) meta["Skipped (deleted/duplicate)"] = header.SkippedEntries.ToString();
        foreach (var g in entries.GroupBy(e => e.Compression).OrderByDescending(g => g.Count()))
            meta[$"Compression {g.Key}"] = g.Count().ToString();
        int encrypted = entries.Count(e => e.Encrypted);
        if (encrypted > 0) meta["Encrypted entries"] = encrypted.ToString();

        return Task.FromResult(new AssetDescriptor
        {
            Name = source.Name,
            Format = FormatId,
            Engine = "unreal",
            Size = source.Length,
            Capabilities = GetCapabilities(detection),
            SourceDescription = source.SourceDescription,
            Metadata = meta,
        });
    }

    public Task<AssetPreview?> PreviewAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        var (header, _) = UnrealPak.Parse(source);
        var entries = _backend.ListEntries(source);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Unreal PAK ({header.Engine} v{header.Version}): {entries.Count} entries");
        sb.AppendLine($"Header: {(header.HeaderStripped ? "stripped" : "present")}");
        if (header.SkippedEntries > 0) sb.AppendLine($"Skipped: {header.SkippedEntries} deleted/duplicate records");
        sb.AppendLine();
        foreach (var g in entries.GroupBy(e => e.Compression).OrderByDescending(g => g.Count()))
            sb.AppendLine($"  {g.Key}: {g.Count()}");

        sb.AppendLine();
        int shown = 0;
        foreach (var e in entries)
        {
            if (shown++ >= ListingCap) { sb.AppendLine("  ..."); break; }
            sb.AppendLine($"  {e.Path} ({e.Size} stored, {e.UncompressedSize} raw, {e.Compression}{(e.Encrypted ? ", encrypted" : "")})");
        }
        return Task.FromResult<AssetPreview?>(AssetPreview.ForText(sb.ToString()));
    }

    public Task<IReadOnlyList<IAssetSource>> GetChildrenAsync(IAssetSource source, AssetDetectionResult detection, int depth, CancellationToken ct = default)
    {
        var entries = _backend.ListEntries(source);
        var children = entries
            .Select(e => (IAssetSource)new PakEntrySource(source, _backend, e))
            .ToList();
        return Task.FromResult<IReadOnlyList<IAssetSource>>(children);
    }
}
