using PS4PKGTool.Assets.Abstractions;
using PS4PKGTool.Assets.Detection;
using PS4PKGTool.Assets.Handlers;
using PS4PKGTool.Assets.Registry;

namespace PS4PKGTool.Assets;

/// <summary>
/// Builds the Phase 1 generic-format service: magic + text + extension-fallback
/// detection, handlers for PNG/JPEG/BMP/GIF/DDS/WAV/OGG/text.
/// </summary>
public static class GenericAssetRegistryBuilder
{
    public static AssetInspectionService Build()
    {
        var detectors = new AssetDetectorRegistry();
        detectors.Register(new UnrealPakDetector());   // before MagicDetector: paks start with entry data
        detectors.Register(new MagicDetector());
        detectors.Register(new UnitySerializedFileDetector());
        detectors.Register(new TextDetector());
        detectors.Register(new ExtensionFallbackDetector());

        var handlers = new AssetHandlerRegistry();
        handlers.Register(new RasterImageHandler(RasterImageHandler.PngFormat), RasterImageHandler.PngFormat);
        handlers.Register(new RasterImageHandler(RasterImageHandler.JpegFormat), RasterImageHandler.JpegFormat);
        handlers.Register(new RasterImageHandler(RasterImageHandler.BmpFormat), RasterImageHandler.BmpFormat);
        handlers.Register(new RasterImageHandler(RasterImageHandler.GifFormat), RasterImageHandler.GifFormat);
        handlers.Register(new DdsHandler(), DdsHandler.FormatId);
        handlers.Register(new GnfHandler(), GnfHandler.FormatId);
        handlers.Register(new Atrac9Handler(), Atrac9Handler.FormatId);
        handlers.Register(new UnityBundleHandler(), UnityBundleHandler.FormatId);
        handlers.Register(new UnitySerializedFileHandler(), UnitySerializedFileHandler.FormatId);
        handlers.Register(new UnrealPakHandler(), UnrealPakHandler.FormatId);
        handlers.Register(new AudioMetadataHandler(AudioMetadataHandler.WavFormat), AudioMetadataHandler.WavFormat);
        handlers.Register(new AudioMetadataHandler(AudioMetadataHandler.OggFormat), AudioMetadataHandler.OggFormat);
        handlers.Register(new TextFileHandler(), TextFileHandler.FormatId);

        return new AssetInspectionService(detectors, handlers);
    }
}
