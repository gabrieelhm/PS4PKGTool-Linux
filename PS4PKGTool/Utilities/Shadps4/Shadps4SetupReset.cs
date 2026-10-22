using System;
using System.IO;
using System.Linq;
using PS4PKGTool.Utilities.Settings;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>
    /// Full reset of the PS4PKGTool-owned shadPS4 state, so the setup can be
    /// redone from scratch. Removes ONLY what PS4PKGTool manages:
    ///  - the managed builds store (builds\ and .work\ under the managed root;
    ///    a custom managed root itself is kept - it may hold other things; the
    ///    default exe-adjacent AppData\shadPS4 root is removed too when it
    ///    ends up empty)
    ///  - the tool's shadPS4 settings (active core/launcher, managed root,
    ///    install directory, the legacy executable anchor - cleared so the
    ///    one-time migration cannot resurrect them)
    /// shadPS4's OWN state is never touched: %APPDATA%\shadPS4 config, saves,
    /// installed game libraries, and adopted installations stay intact.
    /// </summary>
    public static class Shadps4SetupReset
    {
        public static void Reset(Shadps4ManagedBuilds store, AppSettings settings)
        {
            TryDeleteDir(Path.Combine(store.RootPath, "builds"));
            TryDeleteDir(Path.Combine(store.RootPath, ".work"));

            // The default root is PS4PKGTool's alone - remove it when empty.
            string root = store.RootPath.TrimEnd('\\', '/');
            string defaultRoot = Shadps4ManagedBuilds.DefaultRootPath().TrimEnd('\\', '/');
            if (string.Equals(root, defaultRoot, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (Directory.Exists(store.RootPath)
                        && !Directory.EnumerateFileSystemEntries(store.RootPath).Any())
                    {
                        Directory.Delete(store.RootPath);
                    }
                }
                catch { /* best-effort: reset never fails on a non-empty or locked root */ }
            }

            settings.Shadps4ActiveCore = "";
            settings.Shadps4ActiveLauncher = "";
            settings.Shadps4ManagedRoot = "";
            settings.Shadps4InstallDirectory = "";
            settings.Shadps4ExecutablePath = "";
            settings.Shadps4CoreExePath = "";
            settings.Shadps4LauncherExePath = "";
        }

        private static void TryDeleteDir(string dir)
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { /* best-effort: reset never fails on leftovers */ }
        }
    }
}
