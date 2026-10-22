using OrbisPkgTool;
using OrbisPkgTool.Pkg;
using OrbisPkgTool.Trp;

namespace PS4PKGTool.Utilities.PkgMeta;

/// <summary>The production PKG parser, extracted unchanged in behavior from the WinForms composition project.</summary>
public static class PkgMetadataReader
{
    public static PkgMetadata Read(string pkgPath) { using var reader = new PkgReader(pkgPath); return Read(reader, true, true, true); }
    public static PkgMetadata ReadQuick(string pkgPath) { using var reader = new PkgReader(pkgPath); return Read(reader, true, false, false); }
    public static PkgMetadata ReadMetadataOnly(string pkgPath) { using var reader = new PkgReader(pkgPath); return Read(reader, false, false, false); }
    public static PkgMetadata ReadArtwork(string pkgPath) { using var reader = new PkgReader(pkgPath); return Read(reader, true, true, false); }
    public static PkgMetadata Read(PkgReader reader) => Read(reader, true, true, true);

    private static PkgMetadata Read(PkgReader reader, bool includeIcon, bool includeLargeAssets, bool includeTrophy)
    {
        var header = reader.Header;
        var sfo = reader.ReadParamSfo();
        var buildState = DetectBuildState(header);
        var kind = PkgMetadata.GetPkgType(sfo?.GetString("CATEGORY") ?? "");
        byte[]? icon = null, pic0 = null, pic1 = null, trp = null;
        if (includeIcon) FindEntry(reader, PkgEntryIds.Icon0Png, ref icon);
        if (includeLargeAssets)
        {
            FindEntry(reader, PkgEntryIds.Pic0Png, ref pic0);
            FindEntry(reader, PkgEntryIds.Pic1Png, ref pic1);
            if (includeTrophy) FindEntry(reader, PkgEntryIds.Trophy00Trp, ref trp);
        }
        if (includeTrophy && (icon is null || icon.Length == 0) && trp is { Length: > 0 })
        {
            try { icon = Trp.Read(trp).FirstOrDefault(entry => entry.Name.Equals("ICON0.PNG", StringComparison.OrdinalIgnoreCase))?.Data; if (icon is { Length: 0 }) icon = null; }
            catch { }
        }
        return new PkgMetadata { Header = header, BuildState = buildState, Kind = kind, ContentId = header.ContentId, RawTitle = sfo?["TITLE"]?.StringValue, ParamSfo = sfo, SfoTables = sfo?.Tables, Icon = icon, Pic0 = pic0, Pic1 = pic1, TrpData = trp };
    }
    private static void FindEntry(PkgReader reader, uint entryId, ref byte[]? slot)
    {
        try { var data = reader.ExtractEntryBytes(entryId); if (data.Length > 0) slot = data; }
        catch (FileNotFoundException) { }
    }
    internal static PkgBuildState DetectBuildState(PkgHeader header)
    {
        ushort dpProbe = (ushort)(((header.ContentType & 0xFF) << 8) | (header.ContentFlags >> 24));
        if (dpProbe == 0x1E43) return PkgBuildState.Official_DP;
        ushort typeProbe = (ushort)(header.Flags >> 16);
        return typeProbe is 0x8300 or 0x8100 ? PkgBuildState.Official : PkgBuildState.Fake;
    }
}
