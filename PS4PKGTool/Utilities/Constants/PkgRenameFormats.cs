using System.Collections.Generic;
using PS4PKGTool.Util.Constants;

namespace PS4PKGTool.Utilities.Constants
{
    /// <summary>
    /// THE single source of truth for PKG rename formats. Main, the Mini
    /// Viewer and the Explorer shell integration all build their rename
    /// menus and execute renames from this list - there is no per-UI copy.
    /// Formats 1..10 are predefined; 11 is the user's custom format.
    /// </summary>
    public static class PkgRenameFormats
    {
        /// <summary>The predefined formats, in menu order (1-based, 11 = custom).</summary>
        public static IReadOnlyList<(int Id, string Format)> Predefined { get; } = new (int, string)[]
        {
            (1, NamingFormat.TITLE),
            (2, $"{NamingFormat.TITLE} [{NamingFormat.TITLE_ID}]"),
            (3, $"{NamingFormat.TITLE} [{NamingFormat.TITLE_ID}] [{NamingFormat.APP_VERSION}]"),
            (4, $"{NamingFormat.TITLE} [{NamingFormat.CATEGORY}]"),
            (5, NamingFormat.TITLE_ID),
            (6, $"{NamingFormat.TITLE_ID} [{NamingFormat.TITLE}]"),
            (7, $"[{NamingFormat.TITLE_ID}] [{NamingFormat.CATEGORY}] [{NamingFormat.APP_VERSION}] {NamingFormat.TITLE}"),
            (8, $"{NamingFormat.TITLE} [{NamingFormat.CATEGORY}] [{NamingFormat.VERSION}]"),
            (9, NamingFormat.CONTENT_ID),
            (10, NamingFormat.CONTENT_ID2),
        };

        public const int CustomFormatId = 11;

        /// <summary>
        /// The format string for a format id: 1..10 predefined, 11 the
        /// caller-supplied custom format. Returns null for unknown ids.
        /// </summary>
        public static string? GetFormat(int formatId, string? customFormat = null)
        {
            if (formatId == CustomFormatId) return customFormat;
            foreach (var (id, format) in Predefined)
                if (id == formatId) return format;
            return null;
        }

        /// <summary>
        /// Menu label for a predefined format: the format itself is the
        /// clearest label ("{TITLE} [{TITLE_ID}]"), kept short.
        /// </summary>
        public static string DisplayLabel(string format)
            => format.Replace("{", "").Replace("}", "");
    }
}
