using System;
using System.IO;
using System.Linq;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>
    /// Display helpers shared by the main window and the shadPS4 Manager so
    /// active-component rendering stays consistent everywhere.
    /// </summary>
    public static class Shadps4SetupDisplay
    {
        /// <summary>
        /// Readable name of an active component setting for dialogs and
        /// lists: managed builds show DisplayName (date + short sha), adopted
        /// installations show the file name.
        /// </summary>
        public static string ComponentDisplayName(string? setting, Shadps4Component component, string? managedRoot = null)
        {
            var parsed = Shadps4ActiveCore.Parse(setting ?? "");
            if (parsed.Source == Shadps4ComponentSource.Managed)
            {
                var store = new Shadps4ManagedBuilds(managedRoot);
                var build = store.ListBuilds(component)
                    .FirstOrDefault(b => string.Equals(b.BuildId, parsed.Value, StringComparison.OrdinalIgnoreCase));
                if (build != null) return build.DisplayName;
                return parsed.Value;
            }
            if (parsed.Source == Shadps4ComponentSource.Adopted)
                return Path.GetFileName(parsed.Value);
            return "(not set)";
        }

        /// <summary>
        /// Released emulator version of the active managed core for the
        /// feedback submission ("v.0.17.0" -&gt; "0.17.0"). Nightly builds and
        /// adopted installations carry no released version - empty, the user
        /// types it (the template only accepts major released versions).
        /// </summary>
        /// <summary>
        /// Raw active-component setting for settings-style textboxes:
        /// managed builds show "Build &lt;id&gt;", adopted installations show
        /// the path itself, unset shows "(not set)".
        /// </summary>
        public static string DescribeActiveSetting(string? setting)
        {
            var r = Shadps4ActiveCore.Parse(setting);
            return r.Source switch
            {
                Shadps4ComponentSource.Managed => "Build " + r.Value,
                Shadps4ComponentSource.Adopted => r.Value,
                _ => "(not set)",
            };
        }

        /// <summary>Path shortened for detection-style reports (tail kept).</summary>
        public static string ShortenPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "(not found)";
            return path.Length <= 70 ? path : "..." + path.Substring(path.Length - 67);
        }

        /// <summary>First 8 chars of a full 40-hex build id (a commit), or null.</summary>
        public static string? ShortBuildCommit(string? buildId)
        {
            if (string.IsNullOrEmpty(buildId) || buildId.Length != 40) return null;
            foreach (char c in buildId)
                if (!Uri.IsHexDigit(c)) return null;
            return buildId.Substring(0, 8);
        }

        public static string EmulatorVersion(string? coreSetting, string? managedRoot = null)
        {
            var parsed = Shadps4ActiveCore.Parse(coreSetting ?? "");
            if (parsed.Source != Shadps4ComponentSource.Managed) return "";

            var store = new Shadps4ManagedBuilds(managedRoot);
            var build = store.ListBuilds(Shadps4Component.Core)
                .FirstOrDefault(b => string.Equals(b.BuildId, parsed.Value, StringComparison.OrdinalIgnoreCase));
            if (build == null) return "";

            string version = Shadps4ReleaseVersions.Normalize(build.DisplayName);
            return version;
        }
    }
}
