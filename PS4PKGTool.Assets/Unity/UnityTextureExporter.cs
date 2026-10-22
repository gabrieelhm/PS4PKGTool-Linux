using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Containers;
using PS4PKGTool.Assets.Errors;
using PS4PKGTool.Assets.Export;
using PS4PKGTool.Assets.Handlers;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Unity;

/// <summary>
/// Exports Unity textures as PNG (converted). A texture child exports a single
/// file; a whole serialized file exports every decodeable texture into a
/// folder. Raw export (byte copy) is the universal fallback. Streamed textures
/// resolve their .resS companion through ICompanionResolverSource like the
/// preview path does.
/// </summary>
public sealed class UnityTextureExporter : IAssetExporter
{
    private readonly IUnityAssetBackend _backend;

    public UnityTextureExporter() : this(new AssetStudioBackend()) { }

    public UnityTextureExporter(IUnityAssetBackend backend) => _backend = backend;

    public bool CanExport(string format, AssetCapabilities capability)
        => format == UnitySerializedFileHandler.FormatId && (capability & AssetCapabilities.ExportConverted) != 0;

    public Task ExportRawAsync(IAssetSource source, AssetDescriptor descriptor, string destinationPath, CancellationToken ct = default)
    {
        using var src = source.OpenRead();
        using var dst = File.Create(destinationPath);
        src.CopyTo(dst);
        return Task.CompletedTask;
    }

    public Task ExportConvertedAsync(IAssetSource source, AssetDescriptor descriptor, string destinationPath, CancellationToken ct = default)
    {
        if (source is UnityObjectAssetSource objectSource)
        {
            var tex = _backend.ReadTexture2D(objectSource.Parent, objectSource.Object);
            if (tex == null) throw new UnsupportedAssetException("Not a Texture2D object.");
            var withBytes = UnitySerializedFileHandler.LoadTextureBytes(objectSource.Parent, tex)
                ?? throw new UnsupportedAssetException($"Texture '{tex.Name}' streams its data to '{tex.StreamPath}' - the .resS companion is not available.");
            var decoded = _backend.DecodeTexture(withBytes)
                ?? throw new UnsupportedAssetException($"Texture format {tex.Format} is not decodeable yet.");
            PngExport.Write(decoded, destinationPath);
            return Task.CompletedTask;
        }

        // Whole serialized file: every decodeable texture into the target folder.
        Directory.CreateDirectory(destinationPath);
        int exported = 0, failed = 0;
        foreach (var obj in _backend.ListObjects(source).Where(o => o.ClassId == 28))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var info = _backend.ReadTexture2D(source, obj);
                if (info == null) continue;
                var withBytes = UnitySerializedFileHandler.LoadTextureBytes(source, info);
                if (withBytes == null) { failed++; continue; }
                var decoded = _backend.DecodeTexture(withBytes);
                if (decoded == null) { failed++; continue; }

                string safe = SanitizeFileName(info.Name);
                string path = Path.Combine(destinationPath, $"{safe}_{info.Width}x{info.Height}.png");
                PngExport.Write(decoded, path);
                exported++;
            }
            catch (AssetException) { failed++; }
        }
        if (exported == 0)
            throw new UnsupportedAssetException($"No decodeable textures in this file (skipped {failed}).");
        return Task.CompletedTask;
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "texture";
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        string s = new string(chars).Trim();
        return s.Length == 0 ? "texture" : s;
    }
}
