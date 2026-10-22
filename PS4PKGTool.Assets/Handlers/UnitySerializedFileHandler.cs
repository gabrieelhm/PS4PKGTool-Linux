using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Containers;
using PS4PKGTool.Assets.Errors;
using PS4PKGTool.Assets.Models;
using PS4PKGTool.Assets.Unity;

namespace PS4PKGTool.Assets.Handlers;

/// <summary>
/// Unity serialized file (.assets) handler. Detects, inspects, enumerates
/// objects as child sources (Browse), and previews: the whole file renders as
/// a texture contact sheet when any texture decodes (falling back to an object
/// listing), a Texture2D child decodes to a texture preview. Streamed textures
/// (.resS companions) are resolved through ICompanionResolverSource when the
/// app provides one. The parsing implementation is isolated behind
/// IUnityAssetBackend - this handler never sees parser types.
/// </summary>
public sealed class UnitySerializedFileHandler : IAssetHandler, IAssetPreviewProvider
{
    public const string FormatId = "unity-serialized-file";

    private const int ContactSheetMaxTextures = 16;
    private const int ContactCellMax = 240;   // cells are scaled to fit, never upscaled
    private const int ContactPadding = 8;
    private const int ContactMargin = 8;

    // Class ids that carry user-visible content worth browsing.
    private static readonly int[] BrowseClasses = { 28, 43, 49, 83, 213 }; // Texture2D, Mesh, TextAsset, AudioClip, Sprite

    private readonly IUnityAssetBackend _backend;

    public UnitySerializedFileHandler() : this(new AssetStudioBackend()) { }

    public UnitySerializedFileHandler(IUnityAssetBackend backend) => _backend = backend;

    public string Format => FormatId;

    public AssetCapabilities GetCapabilities(AssetDetectionResult detection)
        => AssetCapabilities.Inspect | AssetCapabilities.Browse | AssetCapabilities.Preview | AssetCapabilities.ExportRaw;

    public bool IsContainer(AssetDetectionResult detection) => true;

    public Task<AssetDescriptor> InspectAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        var objects = _backend.ListObjects(source);
        var byClass = objects.GroupBy(o => o.ClassId).OrderByDescending(g => g.Count()).Take(6);

        var meta = new Dictionary<string, string>
        {
            ["Objects"] = objects.Count.ToString(),
            ["Backend"] = _backend.Name,
        };
        foreach (var g in byClass)
            meta[UnitySerializedFile.ClassIdToName(g.Key)] = g.Count().ToString();

        return Task.FromResult(new AssetDescriptor
        {
            Name = source.Name,
            Format = FormatId,
            Engine = "unity",
            Size = source.Length,
            Capabilities = GetCapabilities(detection),
            SourceDescription = source.SourceDescription,
            Metadata = meta,
        });
    }

    public Task<AssetPreview?> PreviewAsync(IAssetSource source, AssetDetectionResult detection, CancellationToken ct = default)
    {
        if (source is UnityObjectAssetSource objectSource)
            return PreviewTextureChildAsync(objectSource);
        return Task.FromResult(BuildFilePreview(source));
    }

    // ── whole-file preview: contact sheet, falling back to a text listing ──

    private AssetPreview? BuildFilePreview(IAssetSource source)
    {
        IReadOnlyList<UnityObjectRef> objects;
        try { objects = _backend.ListObjects(source); }
        catch (AssetException) { return null; } // unreadable header - caller falls back to hex

        var decoded = new List<TextureData>();
        int failed = 0;
        foreach (var obj in objects.Where(o => o.ClassId == 28).Take(ContactSheetMaxTextures))
        {
            try
            {
                var info = _backend.ReadTexture2D(source, obj);
                if (info == null) continue;
                var withBytes = LoadTextureBytes(source, info);
                if (withBytes == null) { failed++; continue; }
                var tex = _backend.DecodeTexture(withBytes);
                if (tex == null) { failed++; continue; }
                decoded.Add(tex);
            }
            catch (AssetException) { failed++; }
        }

        if (decoded.Count > 0)
        {
            string info = $"{source.Name}: {decoded.Count} texture{(decoded.Count == 1 ? "" : "s")}";
            if (failed > 0) info += $", {failed} skipped";
            return new AssetPreview { Texture = BuildContactSheet(decoded), Info = info };
        }

        // Nothing decodeable - fall back to the object listing.
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Unity serialized file: {objects.Count} objects");
        sb.AppendLine($"Backend: {_backend.Name}");
        sb.AppendLine();
        foreach (var g in objects.GroupBy(o => o.ClassId).OrderByDescending(g => g.Count()))
            sb.AppendLine($"  {UnitySerializedFile.ClassIdToName(g.Key)} x{g.Count()}");
        sb.AppendLine();
        int shown = 0;
        foreach (var o in objects.Where(o => BrowseClasses.Contains(o.ClassId)))
        {
            if (shown++ >= 200) { sb.AppendLine("  ..."); break; }
            sb.AppendLine($"  {o.Name} (pathId {o.PathId}, {o.Size} bytes)");
        }
        return AssetPreview.ForText(sb.ToString());
    }

    /// <summary>
    /// Supplies image bytes for a texture: inline data as-is, streamed data
    /// read from the .resS companion (resolved through ICompanionResolverSource).
    /// Null when the bytes are unavailable.
    /// </summary>
    internal static UnityTextureInfo? LoadTextureBytes(IAssetSource source, UnityTextureInfo info)
    {
        if (info.InlineBytes is { Length: > 0 }) return info;
        if (info.StreamSize == 0) return null;
        if (source is not ICompanionResolverSource resolver) return null;

        // PS4 Unity builds sometimes leave StreamingInfo.path empty while
        // still storing the payload beside the serialized file as
        // "<assets-file>.resS". Use that conventional sibling as a fallback.
        string companionPath = string.IsNullOrEmpty(info.StreamPath)
            ? source.Name + ".resS"
            : info.StreamPath;
        var companion = resolver.ResolveCompanion(companionPath);
        if (companion == null) return null;
        try
        {
            using var s = companion.OpenRead(info.StreamOffset, info.StreamSize);
            var bytes = new byte[s.Length];
            s.ReadExactly(bytes);
            return info.WithInlineBytes(bytes);
        }
        catch { return null; }
    }

    private Task<AssetPreview?> PreviewTextureChildAsync(UnityObjectAssetSource objectSource)
    {
        var tex = _backend.ReadTexture2D(objectSource.Parent, objectSource.Object);
        if (tex == null) return Task.FromResult<AssetPreview?>(null);

        var withBytes = LoadTextureBytes(objectSource.Parent, tex);
        if (withBytes == null)
            throw new UnsupportedAssetException(
                $"Texture '{tex.Name}' streams its data to '{tex.StreamPath}' - the .resS companion is not available.");

        var decoded = _backend.DecodeTexture(withBytes);
        if (decoded == null)
            throw new UnsupportedAssetException(
                $"Texture format {tex.Format} is not decodeable yet - metadata only, raw export available.");

        var preview = new AssetPreview
        {
            Texture = decoded,
            Info = $"{tex.Name} ({tex.Width}x{tex.Height}, {FormatName(tex.Format)}, {tex.MipCount} mip{(tex.MipCount == 1 ? "" : "s")})",
        };
        return Task.FromResult<AssetPreview?>(preview);
    }

    /// <summary>Composes decoded textures into one grid image (scaled to fit, dark background).</summary>
    private static TextureData BuildContactSheet(IReadOnlyList<TextureData> textures)
    {
        int count = Math.Min(textures.Count, ContactSheetMaxTextures);
        int cols = (int)Math.Ceiling(Math.Sqrt(count));
        int rows = (count + cols - 1) / cols;

        int sheetW = ContactMargin * 2 + cols * ContactCellMax + (cols - 1) * ContactPadding;
        int sheetH = ContactMargin * 2 + rows * ContactCellMax + (rows - 1) * ContactPadding;
        var sheet = new byte[sheetW * sheetH * 4];
        for (int i = 0; i < sheet.Length; i += 4)
        {
            sheet[i] = 30; sheet[i + 1] = 30; sheet[i + 2] = 30; sheet[i + 3] = 255;
        }

        for (int i = 0; i < count; i++)
        {
            var tex = textures[i];
            float scale = Math.Min((float)ContactCellMax / tex.Width, (float)ContactCellMax / tex.Height);
            scale = Math.Min(scale, 1f); // never upscale
            int dw = Math.Max(1, (int)(tex.Width * scale));
            int dh = Math.Max(1, (int)(tex.Height * scale));
            int ox = ContactMargin + (i % cols) * (ContactCellMax + ContactPadding) + (ContactCellMax - dw) / 2;
            int oy = ContactMargin + (i / cols) * (ContactCellMax + ContactPadding) + (ContactCellMax - dh) / 2;

            for (int y = 0; y < dh; y++)
            {
                int sy = Math.Min(tex.Height - 1, (int)(y / scale));
                for (int x = 0; x < dw; x++)
                {
                    int sx = Math.Min(tex.Width - 1, (int)(x / scale));
                    int si = (sy * tex.Width + sx) * 4;
                    int di = ((oy + y) * sheetW + ox + x) * 4;
                    sheet[di] = tex.Rgba8[si];
                    sheet[di + 1] = tex.Rgba8[si + 1];
                    sheet[di + 2] = tex.Rgba8[si + 2];
                    sheet[di + 3] = 255;
                }
            }
        }
        return new TextureData { Width = sheetW, Height = sheetH, Rgba8 = sheet };
    }

    public Task<IReadOnlyList<IAssetSource>> GetChildrenAsync(IAssetSource source, AssetDetectionResult detection, int depth, CancellationToken ct = default)
    {
        var objects = _backend.ListObjects(source);
        var children = objects
            .Where(o => BrowseClasses.Contains(o.ClassId))
            .Select(o => (IAssetSource)new UnityObjectAssetSource(source, o))
            .ToList();
        return Task.FromResult<IReadOnlyList<IAssetSource>>(children);
    }

    private static string FormatName(int format) => format switch
    {
        1 => "Alpha8", 2 => "ARGB4444", 3 => "RGB24", 4 => "RGBA32", 5 => "ARGB32",
        7 => "RGB565", 8 => "BGR24", 9 => "R16", 10 => "DXT1", 11 => "DXT3",
        12 => "DXT5", 13 => "RGBA4444", 14 => "BGRA32",
        _ => $"format {format}",
    };
}
