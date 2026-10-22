using System;
using System.IO;
using System.Text.RegularExpressions;
using PS4PKGTool.Utilities.PkgMeta;

namespace PS4PKGTool.Utilities.PkgRename;

/// <summary>
/// PKG rename-token engine, ported from PS4_Tools.PKG.SceneRelated.GetNewPKGName.
/// Expands {TITLE} {TITLE_ID} {APP_VERSION} {VERSION} {CATEGORY} {CONTENT_ID}
/// {CONTENT_ID2} {REGION} {SYSTEM_VERSION} from the package's param.sfo.
/// </summary>
public static class PkgRenameService
{
    public static (string newPkgName, string sourcePkg, string targetPkg) GetNewPKGName(
        string sourcePkg, string destinationFolder, string namingFormat)
    {
        var pkg = PkgMetadataReader.Read(sourcePkg);
        string firmware = GetFirmware(pkg);
        string version = GetVersion(pkg);
        string appVer = Regex.Replace(pkg.APP_VER, "^0+(?=\\d+\\.)", "");

        namingFormat = Regex.Replace(namingFormat, "\\{TITLE\\}", IllegalNameCheck.SanitizeFileName(pkg.PS4_Title));
        namingFormat = Regex.Replace(namingFormat, "\\{TITLE_ID\\}", pkg.TITLEID);
        namingFormat = Regex.Replace(namingFormat, "\\{APP_VERSION\\}", appVer);
        namingFormat = Regex.Replace(namingFormat, "\\{VERSION\\}", version);
        namingFormat = Regex.Replace(namingFormat, "\\{CATEGORY\\}", pkg.PKG_Type.ToString());
        namingFormat = Regex.Replace(namingFormat, "\\{CONTENT_ID\\}", pkg.SfoContentId);
        namingFormat = Regex.Replace(namingFormat, "\\{CONTENT_ID2\\}", NewNameContentId2(pkg));
        namingFormat = Regex.Replace(namingFormat, "\\{REGION\\}", pkg.Region);
        namingFormat = Regex.Replace(namingFormat, "\\{SYSTEM_VERSION\\}", firmware);

        return (newPkgName: namingFormat, sourcePkg: sourcePkg,
            targetPkg: destinationFolder + namingFormat + ".pkg");
    }

    /// <summary>Legacy GetFirmware: SYSTEM_VER row → "0" stays "0", anything
    /// else is hex(formatted as XX.YYZ from the first 3 hex digits).</summary>
    internal static string GetFirmware(PkgMetadata pkg)
    {
        if (pkg.SfoTables == null) return string.Empty;
        foreach (var item in pkg.SfoTables)
        {
            if (item.Name == "SYSTEM_VER")
            {
                if (item.Value == "0") return item.Value;
                string hex = Convert.ToInt32(item.Value).ToString("X");
                return hex.Substring(0, 3).Insert(1, ".");
            }
        }
        return string.Empty;
    }

    /// <summary>Legacy GetVersion: VERSION row with leading zeros stripped.</summary>
    internal static string GetVersion(PkgMetadata pkg)
    {
        if (pkg.SfoTables == null) return string.Empty;
        foreach (var item in pkg.SfoTables)
        {
            if (item.Name == "VERSION")
                return Regex.Replace(item.Value, "^0+(?=\\d+\\.)", "");
        }
        return string.Empty;
    }

    /// <summary>
    /// Legacy NewNameContentId2: non-Addon packages append
    /// "-A&lt;app-ver&gt;-V&lt;version&gt;", Addons append "-A0000-V&lt;version&gt;"
    /// (dots stripped).
    /// </summary>
    internal static string NewNameContentId2(PkgMetadata pkg)
    {
        string version = "";
        if (pkg.SfoTables != null)
        {
            foreach (var item in pkg.SfoTables)
            {
                if (item.Name == "VERSION")
                    version = item.Value.Replace(".", "");
            }
        }

        if (pkg.PKG_Type.ToString() != "Addon")
            return pkg.SfoContentId + "-A" + pkg.APP_VER.Replace(".", "") + "-V" + version;
        return pkg.SfoContentId + "-A0000-V" + version;
    }
}
