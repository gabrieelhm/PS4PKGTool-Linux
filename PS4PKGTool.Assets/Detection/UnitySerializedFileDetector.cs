using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Containers;
using PS4PKGTool.Assets.Handlers;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Detection;

/// <summary>
/// Detects Unity serialized files (.assets, globalgamemanagers, level*)
/// by probing the header's version field (5-22, either byte order). Sits
/// between magic-based and extension-fallback detection; a positive probe
/// outranks the extension fallback.
/// </summary>
public sealed class UnitySerializedFileDetector : IAssetDetector
{
    public int Order => 5;

    public AssetDetectionResult? Detect(IAssetSource source, CancellationToken ct = default)
    {
        try
        {
            using var s = source.OpenRead(0, 20);
            var head = new byte[s.Length];
            s.ReadExactly(head);
            if (!UnitySerializedFile.IsLikelySerializedFile(head)) return null;
            return new AssetDetectionResult
            {
                Format = UnitySerializedFileHandler.FormatId,
                Engine = "unity",
                Confidence = 0.8,
                Evidence = new[] { "serialized file header probe (version 5-22)" },
            };
        }
        catch { return null; }
    }
}
