using OrbisPkgTool.Pkg;
using OrbisPkgTool.Sfo;

namespace PS4PKGTool.Utilities.PkgMeta;

public enum PkgKind { Game, App, Addon, Patch, Unknown }
public enum PkgBuildState { Fake = 0, Official = 1, Official_DP = 2, Unknown = 99 }

/// <summary>Fully materialized, platform-neutral package metadata used by both desktop front ends.</summary>
public sealed class PkgMetadata
{
    public required PkgHeader Header { get; init; }
    public required PkgBuildState BuildState { get; init; }
    public required PkgKind Kind { get; init; }
    public required string ContentId { get; init; }
    public string? RawTitle { get; init; }
    public ParamSfo? ParamSfo { get; init; }
    public IReadOnlyList<SfoTable>? SfoTables { get; init; }
    public byte[]? Icon { get; init; }
    public byte[]? Pic0 { get; init; }
    public byte[]? Pic1 { get; init; }
    public byte[]? TrpData { get; init; }
    public string PS4_Title => RawTitle ?? ParamSfo?.GetString("TITLE") ?? "";
    public string Content_ID => ContentId;
    public PkgKind PKG_Type => Kind;
    public PkgBuildState PKGState => BuildState;
    public string Region => RegionFromContentId(ContentId);
    public string TITLEID => ParamSfo?.GetString("TITLE_ID") ?? "";
    public string APP_VER => ParamSfo?.GetString("APP_VER") ?? "";
    public string SfoContentId => ParamSfo?.GetString("CONTENT_ID") ?? "";
    public string Category => ParamSfo?.GetString("CATEGORY") ?? "";
    public static string RegionFromContentId(string contentId) => contentId.Length == 0 ? "" : contentId[0] switch
    {
        'E' => "EU", 'U' or 'I' => "US", 'J' => "JAPAN", 'H' => "HONG KONG", 'A' => "ASIA", 'K' => "KOREA", _ => ""
    };
    public static PkgKind GetPkgType(string category) => category switch
    {
        "gde" or "gdk" => PkgKind.App, "gd" => PkgKind.Game, "ac" => PkgKind.Addon, "gp" => PkgKind.Patch, _ => PkgKind.Unknown
    };
}
