using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PS4PKGTool
{
    public static class IllegalNameCheck
    {
        private static readonly Dictionary<char, char> _safeSubstitutes = new Dictionary<char, char>
        {
            ['<'] = '＜',   // U+FF1C Fullwidth Less-Than
            ['>'] = '＞',   // U+FF1E Fullwidth Greater-Than
            [':'] = '：',   // U+FF1A Fullwidth Colon
            ['"'] = '＂',   // U+FF02 Fullwidth Quotation Mark
            ['/'] = '／',   // U+FF0F Fullwidth Solidus
            ['\\'] = '＼',  // U+FF3C Fullwidth Reverse Solidus
            ['|'] = '｜',   // U+FF5C Fullwidth Vertical Bar
            ['?'] = '？',   // U+FF1F Fullwidth Question Mark
            ['*'] = '＊',   // U+FF0A Fullwidth Asterisk
        };

        public static bool IsValidFileName(this string expression, bool platformIndependent)
        {
            string sPattern = @"^(?!^(PRN|AUX|CLOCK\$|NUL|CON|COM\d|LPT\d|\..*)(\..+)?$)[^\x00-\x1f\\?*:\"";|/]+$";
            if (platformIndependent)
            {
                sPattern = @"^(([a-zA-Z]:|\\)\\)?(((\.)|(\.\.)|([^\\/:\*\?""\|<>\. ](([^\\/:\*\?""\|<>\. ])|([^\\/:\*\?""\|<>]*[^\\/:\*\?""\|<>\. ]))?))\\)*[^\\/:\*\?""\|<>\. ](([^\\/:\*\?""\|<>\. ])|([^\\/:\*\?""\|<>]*[^\\/:\*\?""\|<>\. ]))?$";
            }
            return (Regex.IsMatch(expression, sPattern, RegexOptions.CultureInvariant));
        }

        /// <summary>Replaces NTFS-invalid characters with visually-similar Unicode alternatives.</summary>
        public static string SanitizeFileName(this string name)
        {
            if (string.IsNullOrEmpty(name)) return "_";

            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (_safeSubstitutes.TryGetValue(c, out char sub))
                    sb.Append(sub);
                else if (char.IsControl(c))
                    sb.Append('_');
                else if (Array.IndexOf(invalid, c) >= 0)
                    sb.Append('_');
                else
                    sb.Append(c);
            }

            string result = sb.ToString().TrimEnd('.', ' ');

            string upper = result.ToUpperInvariant();
            if (upper == "CON" || upper == "PRN" || upper == "AUX" || upper == "NUL" ||
                (upper.Length == 4 && upper.StartsWith("COM") && upper[3] >= '1' && upper[3] <= '9') ||
                (upper.Length == 4 && upper.StartsWith("LPT") && upper[3] >= '1' && upper[3] <= '9'))
                result = "_" + result;

            return string.IsNullOrEmpty(result) ? "_" : result;
        }
    }
}
