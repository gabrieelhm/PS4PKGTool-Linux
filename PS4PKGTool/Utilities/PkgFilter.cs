using System.Collections.Generic;
using System.Linq;

namespace PS4PKGTool.Utilities
{
    /// <summary>
    /// The complete filter state for the PKG grid. Multi-select aspects are
    /// OR-ed within the aspect and AND-ed across aspects. One state object
    /// feeds the single effective RowFilter, so the grid AND the grouped
    /// view always show the same filtered set.
    /// </summary>
    public sealed class PkgFilterState
    {
        /// <summary>Selected categories (Game/Patch/Addon/App/...); empty = no category filter.</summary>
        public List<string> Categories { get; } = new();

        /// <summary>Selected region names (EU/US/JAPAN/...); empty = no region filter.</summary>
        public List<string> Regions { get; } = new();

        /// <summary>Minimum firmware threshold; null = no system-version filter.</summary>
        public double? MinSystemVersion { get; set; }

        /// <summary>Selected PKG types (Official/Fake/...); empty = no type filter.</summary>
        public List<string> PkgTypes { get; } = new();

        /// <summary>Selected shadPS4 statuses (incl. CompatUnknown); empty = no compat filter.</summary>
        public List<string> CompatStatuses { get; } = new();

        /// <summary>When true, show only Title IDs installed in the local shadPS4 library.</summary>
        public bool Shadps4InstalledOnly { get; set; }

        /// <summary>Free-text search over filename/title/title id/content id.</summary>
        public string SearchText { get; set; } = "";

        public bool IsEmpty =>
            Categories.Count == 0 && Regions.Count == 0 && MinSystemVersion == null
            && PkgTypes.Count == 0 && CompatStatuses.Count == 0 && !Shadps4InstalledOnly
            && string.IsNullOrWhiteSpace(SearchText);
    }

    /// <summary>
    /// Builds the single effective RowFilter for the PKG grid. All filter
    /// controls (aspects, compatibility status, search) feed this one method
    /// so filters COMPOSE (AND) instead of overwriting each other.
    /// Column visibility does not affect filtering - the DataTable columns
    /// exist regardless of the DataGridView presentation state.
    /// </summary>
    public static class PkgFilter
    {
        /// <summary>Sentinel compat filter: games with no compatibility entry.</summary>
        public const string CompatUnknown = "Unknown";

        public static readonly string[] CompatOptions =
            { "Playable", "In-Game", "Menus", "Boots", "Nothing", CompatUnknown };

        public static readonly string[] RegionOptions =
            { "EU", "US", "JAPAN", "HONG_KONG", "ASIA", "KOREA" };

        /// <summary>
        /// Builds the RowFilter expression. Empty string = no filter.
        /// (Legacy single-value overload - kept for compatibility.)
        /// </summary>
        public static string BuildExpression(string? typeCategory, string? compatStatus, string? searchText)
        {
            var state = new PkgFilterState { SearchText = searchText ?? "" };
            if (!string.IsNullOrWhiteSpace(typeCategory)) state.Categories.Add(typeCategory);
            if (!string.IsNullOrWhiteSpace(compatStatus)) state.CompatStatuses.Add(compatStatus);
            return BuildExpression(state);
        }

        /// <summary>Builds the RowFilter expression from the full filter state.</summary>
        public static string BuildExpression(PkgFilterState state)
        {
            if (state == null || state.IsEmpty) return "";
            var parts = new List<string>();

            if (state.Categories.Count > 0)
            {
                var ors = state.Categories
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => $"[{PkgColumns.Category}] LIKE '%{Escape(c)}%'")
                    .ToList();
                parts.Add(JoinOr(ors));
            }

            if (state.Regions.Count > 0)
            {
                var ors = state.Regions
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => $"[{PkgColumns.RegionName}] = '{Escape(r)}'")
                    .ToList();
                parts.Add(JoinOr(ors));
            }

            if (state.MinSystemVersion is double min)
                parts.Add($"[{PkgColumns.SystemVersionNum}] >= {min.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

            if (state.PkgTypes.Count > 0)
            {
                var ors = state.PkgTypes
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => $"[{PkgColumns.PkgType}] = '{Escape(t)}'")
                    .ToList();
                parts.Add(JoinOr(ors));
            }

            if (state.CompatStatuses.Count > 0)
            {
                var ors = state.CompatStatuses
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c == CompatUnknown
                        ? $"([{PkgColumns.Shadps4}] IS NULL OR [{PkgColumns.Shadps4}] = '')"
                        : $"[{PkgColumns.Shadps4}] = '{Escape(c)}'")
                    .ToList();
                parts.Add(JoinOr(ors));
            }

            if (state.Shadps4InstalledOnly)
                parts.Add($"[{PkgColumns.Shadps4Installed}] = true");

            if (!string.IsNullOrWhiteSpace(state.SearchText))
            {
                string t = Escape(state.SearchText.Trim());
                parts.Add($"([{PkgColumns.Filename}] LIKE '%{t}%' OR [{PkgColumns.Title}] LIKE '%{t}%' " +
                          $"OR [{PkgColumns.TitleId}] LIKE '%{t}%' OR [{PkgColumns.ContentId}] LIKE '%{t}%')");
            }

            return parts.Count == 0 ? "" : string.Join(" AND ", parts);
        }

        private static string Escape(string value) => value.Replace("'", "''");

        /// <summary>Single value stays bare; 2+ values are OR-ed inside parens.</summary>
        private static string JoinOr(List<string> ors)
            => ors.Count == 1 ? ors[0] : "(" + string.Join(" OR ", ors) + ")";
    }
}
