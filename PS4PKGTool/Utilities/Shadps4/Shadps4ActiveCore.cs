using System;
using System.IO;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>How an Active Core / Active Launcher setting points at its component.</summary>
    public enum Shadps4ComponentSource
    {
        /// <summary>Not set.</summary>
        None,
        /// <summary>A PS4PKGTool-managed build, referenced by build id: managed:&lt;buildId&gt;.</summary>
        Managed,
        /// <summary>A user-chosen existing installation, referenced by absolute path: adopted:&lt;path&gt;.</summary>
        Adopted,
    }

    /// <summary>Parsed form of an active-component setting value ("managed:&lt;id&gt;" / "adopted:&lt;path&gt;" / "").</summary>
    public readonly record struct Shadps4ComponentRef(Shadps4ComponentSource Source, string Value)
    {
        public bool IsSet => Source != Shadps4ComponentSource.None;
    }

    /// <summary>
    /// The Active Core / Active Launcher model.
    ///
    /// PS4PKGTool never derives the launch executable from whichever binary
    /// is nearest on disk - that caused a real incident (an old parent-folder
    /// core was auto-selected and the game crashed). The launch path comes
    /// ONLY from this explicit setting:
    ///   ""                  - not configured
    ///   "managed:&lt;id&gt;"   - a PS4PKGTool-managed build (build store)
    ///   "adopted:&lt;path&gt;" - a user-selected existing installation (reference
    ///                          only - adopted installations are never written to)
    /// </summary>
    public static class Shadps4ActiveCore
    {
        public const string ManagedPrefix = "managed:";
        public const string AdoptedPrefix = "adopted:";

        /// <summary>Parses a setting value into its source and reference.</summary>
        public static Shadps4ComponentRef Parse(string? setting)
        {
            string s = setting?.Trim() ?? "";
            if (s.StartsWith(ManagedPrefix, StringComparison.OrdinalIgnoreCase))
                return new Shadps4ComponentRef(Shadps4ComponentSource.Managed, s[ManagedPrefix.Length..].Trim());
            if (s.StartsWith(AdoptedPrefix, StringComparison.OrdinalIgnoreCase))
                return new Shadps4ComponentRef(Shadps4ComponentSource.Adopted, s[AdoptedPrefix.Length..].Trim());
            return new Shadps4ComponentRef(Shadps4ComponentSource.None, s);
        }

        public static string ForAdopted(string path) => AdoptedPrefix + path;

        public static string ForManaged(string buildId) => ManagedPrefix + buildId;

        /// <summary>
        /// Resolves an active component to a concrete executable path, or
        /// returns null with an error reason. NEVER performs opportunistic
        /// disk searching: adopted paths must still exist (no silent
        /// replacement), managed ids resolve through the build-store hook.
        /// </summary>
        public static string? ResolveExecutable(string? setting, Func<string, string?>? resolveManagedBuild, out string? error)
        {
            var r = Parse(setting);
            switch (r.Source)
            {
                case Shadps4ComponentSource.Adopted:
                    string path = r.Value;
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        error = "The adopted shadPS4 path is empty.";
                        return null;
                    }
                    if (!File.Exists(path))
                    {
                        error = $"The configured shadPS4 executable no longer exists:\n{path}";
                        return null;
                    }
                    error = null;
                    return path;

                case Shadps4ComponentSource.Managed:
                    if (resolveManagedBuild == null)
                    {
                        error = "The managed shadPS4 build store is not available.";
                        return null;
                    }
                    string? managedPath = resolveManagedBuild(r.Value);
                    if (string.IsNullOrWhiteSpace(managedPath) || !File.Exists(managedPath))
                    {
                        error = $"The managed shadPS4 build was not found on disk: {r.Value}";
                        return null;
                    }
                    error = null;
                    return managedPath;

                default:
                    error = "No shadPS4 core is active. Open Program Settings to select or install one.";
                    return null;
            }
        }
    }
}
