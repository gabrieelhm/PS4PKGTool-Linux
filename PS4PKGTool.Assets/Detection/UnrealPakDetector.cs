using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Containers;
using PS4PKGTool.Assets.Handlers;
using PS4PKGTool.Assets.Models;

namespace PS4PKGTool.Assets.Detection;

/// <summary>
/// Detects Unreal PAK files by probing the trailer magic (the file-start
/// header is often stripped in shipping builds, so the trailer is the reliable
/// probe). Bounded read of the last 60 bytes only.
/// </summary>
public sealed class UnrealPakDetector : IAssetDetector
{
    // Runs BEFORE content-magic detection: a pak's first bytes are its first
    // entry's data, which can carry any content magic (e.g. a leading PNG).
    // The trailer probe (magic at a precise position + version + index bounds
    // inside the file) is authoritative evidence for the file as a whole, so
    // it outranks content magic in both order and confidence.
    public int Order => 0;

    public AssetDetectionResult? Detect(IAssetSource source, CancellationToken ct = default)
    {
        if (!UnrealPak.IsLikelyPak(source)) return null;
        return new AssetDetectionResult
        {
            Format = UnrealPakHandler.FormatId,
            Engine = "unreal",
            Confidence = 1.0,
            Evidence = new[] { "PAK trailer magic probe (0x5A6F12E1/E2)" },
        };
    }
}
