using System.Globalization;

namespace PS4PKGTool.Utilities.PkgMeta;

public static class PkgSystemVersion
{
    public static string Format(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0" || value == "NA") return value ?? string.Empty;
        if (value.Contains('.') && value.Length <= 5) return value;
        string text = value.Trim(); var style = NumberStyles.Integer;
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) { text = text[2..]; style = NumberStyles.AllowHexSpecifier; }
        if (!uint.TryParse(text, style, CultureInfo.InvariantCulture, out var encoded)) return value;
        uint compact = encoded <= ushort.MaxValue ? encoded : encoded >> 16;
        return $"{(compact >> 8) & 0xFF}.{compact & 0xFF:X2}";
    }
    public static string FormatSdkVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        string text = value.Trim(); if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text[2..];
        if (!uint.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var encoded)) return string.Empty;
        uint compact = encoded >> 16;
        return $"{(compact >> 8) & 0xFF}.{compact & 0xFF:X2}";
    }
}
