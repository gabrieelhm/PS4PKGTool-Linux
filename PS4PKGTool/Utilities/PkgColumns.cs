using System.Data;

namespace PS4PKGTool.Utilities
{
    /// <summary>
    /// Single source of truth for the main PKG grid's DataTable schema.
    /// The grid table is built independently by the manifest loader, the
    /// directory scanner and the drag-drop scanner - they all call
    /// CreateSchema() so the column set can never drift apart (the
    /// "ShadPS4" vs "ShadPS4 (Windows)" mismatch was exactly such a drift).
    /// </summary>
    public static class PkgColumns
    {
        public const string Filename = "Filename";
        public const string Title = "Title";
        public const string TitleId = "Title ID";
        public const string ContentId = "Content ID";
        public const string Region = "Region";
        public const string SystemVersion = "Required Firmware";
        public const string AppVersion = "Version [App Version]";
        public const string PkgType = "PKG Type";
        public const string Category = "Category";
        public const string Size = "Size";
        public const string Psvr = "PSVR";
        public const string Ps4ProEnhanced = "PS4 Pro Enhanced";
        public const string Ps5Bc = "PS5 BC";
        public const string Directory = "Directory";
        public const string Backported = "Backported";
        public const string LatestUpdate = "Latest Update";
        public const string Shadps4 = "ShadPS4";
        /// <summary>Hidden local-library flag used by the shadPS4 Installed filter.</summary>
        public const string Shadps4Installed = "ShadPS4 Installed";
        /// <summary>Hidden string column (region NAME) used by the row filter - Region itself is a byte[] icon.</summary>
        public const string RegionName = "Region Name";
        /// <summary>Hidden numeric column (parsed required firmware) used by the "&gt;= firmware" filter.</summary>
        public const string SystemVersionNum = "Required Firmware (Num)";

        /// <summary>Creates the main PKG grid schema. Region is byte[]; Region Name, Required Firmware (Num), and ShadPS4 Installed are hidden filter columns.</summary>
        public static DataTable CreateSchema()
        {
            var dt = new DataTable();
            dt.Columns.Add(Filename);
            dt.Columns.Add(Title);
            dt.Columns.Add(TitleId);
            dt.Columns.Add(ContentId);
            dt.Columns.Add(Region, typeof(byte[]));
            dt.Columns.Add(SystemVersion);
            dt.Columns.Add(AppVersion);
            dt.Columns.Add(PkgType);
            dt.Columns.Add(Category);
            dt.Columns.Add(Size);
            dt.Columns.Add(Psvr);
            dt.Columns.Add(Ps4ProEnhanced);
            dt.Columns.Add(Ps5Bc);
            dt.Columns.Add(Directory);
            dt.Columns.Add(Backported);
            dt.Columns.Add(LatestUpdate);
            dt.Columns.Add(Shadps4);
            dt.Columns.Add(Shadps4Installed, typeof(bool));
            dt.Columns.Add(RegionName);
            dt.Columns.Add(SystemVersionNum, typeof(double));
            return dt;
        }

        /// <summary>Parses a firmware string ("5.05", "NA", ...) into a number for the hidden filter column (0 when unparseable).</summary>
        public static double ParseSystemVersionNum(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            return double.TryParse(value.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : 0;
        }
    }
}
