using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>
    /// Official RELEASE versions for compatibility reporting. The shadPS4
    /// template only accepts major released versions, so nightlies (commit
    /// shas, date tagged prerelease builds) are never candidates here.
    /// Sources, both read-only and offline-friendly:
    ///   - the stable feed cache (populated by Check for Updates and the
    ///     install wizard; a stale cache is still useful - releases do not
    ///     disappear, so the TTL is widened for this read)
    ///   - installed managed core builds whose manifest carries a release
    ///     tag (v0.17.0, ...)
    /// </summary>
    public static class Shadps4ReleaseVersions
    {
        // Upstream stable tags are "v.0.17.0" (dot after the v) - accept
        // both that shape and the plain "v0.17.0". "Pre-release-shadPS4-..."
        // nightly ids never match.
        private static readonly Regex ReleaseTagPattern = new(@"^v\.?\d+\.\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ReleaseVersionPattern = new(@"^\d+\.\d+", RegexOptions.Compiled);

        /// <summary>True when a manifest/feed tag is an official release tag (v0.17.0), not a nightly id.</summary>
        public static bool IsReleaseTag(string? tag)
            => !string.IsNullOrWhiteSpace(tag) && ReleaseTagPattern.IsMatch(tag);

        /// <summary>True when a version string (v stripped) is release shaped (0.17.0).</summary>
        public static bool IsReleaseVersion(string? version)
            => !string.IsNullOrWhiteSpace(version) && ReleaseVersionPattern.IsMatch(version);

        /// <summary>
        /// Strips a single leading v so feed tags and typed versions compare
        /// equal. shadPS4 tags are "v.0.17.0" (a dot after the v), so the
        /// dot is removed too: "v.0.17.0" and "v0.17.0" both give "0.17.0".
        /// </summary>
        public static string Normalize(string? tagOrVersion)
        {
            string s = (tagOrVersion ?? "").Trim();
            if (!s.StartsWith("v", StringComparison.OrdinalIgnoreCase)) return s;
            s = s.Substring(1);
            return s.StartsWith(".") ? s.Substring(1) : s;
        }

        /// <summary>
        /// Known official release versions, newest first. Never throws - an
        /// unreadable cache or store simply contributes nothing.
        /// </summary>
        public static List<string> Collect(string? managedRoot = null, string? cachePath = null)
        {
            var versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Stable feed cache. Releases never disappear, so a stale cache
            // is fine for this read - widen the TTL instead of dropping it.
            try
            {
                var feed = new Shadps4ReleaseFeed(cachePath, cacheTtl: TimeSpan.FromDays(7));
                var cached = feed.LoadCached(Shadps4FeedKind.CoreStable);
                if (cached != null)
                {
                    foreach (var r in cached)
                        if (IsReleaseTag(r.Tag)) versions.Add(Normalize(r.Tag));
                }
            }
            catch (Exception ex)
            {
                // Never throws by contract - but a version list that is empty
                // for an invisible reason reads as "no versions exist".
                Logger.LogWarning("Release versions: stable feed cache unreadable: " + ex.Message);
            }

            // Installed managed core builds with a release manifest tag.
            try
            {
                var store = new Shadps4ManagedBuilds(managedRoot);
                foreach (var b in store.ListBuilds(Shadps4Component.Core))
                    if (IsReleaseTag(b.Manifest.Release)) versions.Add(Normalize(b.Manifest.Release));
            }
            catch (Exception ex)
            {
                // Managed root may simply not be configured yet (first run).
                Logger.LogWarning("Release versions: managed builds unreadable: " + ex.Message);
            }

            return versions.OrderByDescending(v => v, VersionComparer.Instance).ToList();
        }

        private sealed class VersionComparer : IComparer<string>
        {
            public static readonly VersionComparer Instance = new();

            public int Compare(string? a, string? b)
            {
                string[] pa = (a ?? "").Split('.');
                string[] pb = (b ?? "").Split('.');
                int n = Math.Min(pa.Length, pb.Length);
                for (int i = 0; i < n; i++)
                {
                    if (!int.TryParse(pa[i], out int x) || !int.TryParse(pb[i], out int y))
                        return string.Compare(pa[i], pb[i], StringComparison.OrdinalIgnoreCase);
                    if (x != y) return x.CompareTo(y);
                }
                return pa.Length.CompareTo(pb.Length);
            }
        }
    }
}
