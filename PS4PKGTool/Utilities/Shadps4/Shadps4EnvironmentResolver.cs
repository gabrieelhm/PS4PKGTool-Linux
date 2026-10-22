using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PS4PKGTool.Utilities.Shadps4
{
    public static class Shadps4EnvironmentResolver
    {
        public const string PortableDirName = "user";
        public const string AppDataDirName = "shadPS4";

        public static string CoreExeFileName => OperatingSystem.IsWindows() ? "shadPS4.exe" : "shadps4";
        public static string QtLauncherFileName => OperatingSystem.IsWindows() ? "shadPS4QtLauncher.exe" : "shadPS4QtLauncher";

        private static readonly string[] CoreCandidateNames =
        {
            "shadps4", "shadPS4", "Shadps4-sdl.AppImage", "shadPS4-sdl.AppImage", "shadps4.AppImage", "shadPS4.exe"
        };

        private static readonly string[] LauncherCandidateNames =
        {
            "shadPS4QtLauncher", "shadps4-qtlauncher", "Shadps4-qt.AppImage", "shadPS4QtLauncher.AppImage", "shadPS4QtLauncher.exe"
        };

        private static readonly IShadps4ConfigReader[] Readers =
        {
            new Shadps4JsonConfigReader(),
            new Shadps4TomlConfigReader(),
        };

        public static (bool IsValid, string? Error) ValidateSelectedExecutablePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return (false, "No executable selected.");
            if (!File.Exists(path))
                return (false, "The selected file does not exist.");
            if (OperatingSystem.IsWindows() && !Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                return (false, "The selected file is not an executable (.exe).");
            return (true, null);
        }

        public static string? FindExecutableInDirectory(string dir, Shadps4Component component)
        {
            if (!Directory.Exists(dir)) return null;
            var candidates = component == Shadps4Component.Core ? CoreCandidateNames : LauncherCandidateNames;
            foreach (var name in candidates)
            {
                string p = Path.Combine(dir, name);
                if (File.Exists(p)) return p;
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    string fn = Path.GetFileName(file);
                    if (fn.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                        || fn.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                        || fn.EndsWith(".so", StringComparison.OrdinalIgnoreCase)
                        || fn.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (component == Shadps4Component.QtLauncher &&
                        (fn.Contains("Launcher", StringComparison.OrdinalIgnoreCase) ||
                         fn.Contains("-qt", StringComparison.OrdinalIgnoreCase)))
                        return file;

                    if (component == Shadps4Component.Core &&
                        !fn.Contains("Launcher", StringComparison.OrdinalIgnoreCase) &&
                        (fn.Contains("shadps4", StringComparison.OrdinalIgnoreCase) ||
                         fn.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase)))
                        return file;
                }
            }
            catch { }
            return null;
        }

        public static Shadps4Environment Resolve(string? selectedExecutablePath, string? appDataRoot = null)
        {
            string localShare = appDataRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roamingConfig = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var warnings = new List<string>();

            string? selectedDir = null;
            if (!string.IsNullOrWhiteSpace(selectedExecutablePath))
            {
                if (!File.Exists(selectedExecutablePath))
                    warnings.Add($"Selected shadPS4 executable does not exist: {selectedExecutablePath}");
                selectedDir = Path.GetDirectoryName(selectedExecutablePath);
            }

            string? core = null, launcher = null;
            string? coreDir = null;
            if (!string.IsNullOrWhiteSpace(selectedDir) && Directory.Exists(selectedDir))
            {
                var probeDirs = new List<string> { selectedDir };
                string? parent = Directory.GetParent(selectedDir)?.FullName;
                if (!string.IsNullOrWhiteSpace(parent)) probeDirs.Add(parent);
                string? grandparent = Directory.GetParent(parent ?? "")?.FullName;
                if (!string.IsNullOrWhiteSpace(grandparent)) probeDirs.Add(grandparent);

                foreach (var dir in probeDirs)
                {
                    if (core == null)
                    {
                        var foundCore = FindExecutableInDirectory(dir, Shadps4Component.Core);
                        if (foundCore != null)
                        {
                            core = foundCore;
                            coreDir = dir;
                        }
                    }
                    if (launcher == null)
                    {
                        var foundLauncher = FindExecutableInDirectory(dir, Shadps4Component.QtLauncher);
                        if (foundLauncher != null)
                            launcher = foundLauncher;
                    }
                }
                if (core == null && !string.IsNullOrWhiteSpace(selectedExecutablePath) && File.Exists(selectedExecutablePath))
                {
                    string fn = Path.GetFileName(selectedExecutablePath);
                    if (fn.Contains("Launcher", StringComparison.OrdinalIgnoreCase) || fn.Contains("-qt", StringComparison.OrdinalIgnoreCase))
                        launcher ??= selectedExecutablePath;
                    else
                        core = selectedExecutablePath;
                }
            }

            var distribution = (core != null, launcher != null) switch
            {
                (true, true) => Shadps4DistributionType.CoreAndQtLauncher,
                (true, false) => Shadps4DistributionType.CoreOnly,
                (false, true) => Shadps4DistributionType.QtLauncherOnly,
                _ => Shadps4DistributionType.Unknown,
            };
            if (distribution == Shadps4DistributionType.Unknown)
                warnings.Add("Neither shadPS4 core nor shadPS4QtLauncher was found beside the selected executable.");

            var appDataUserDir = Path.Combine(localShare, AppDataDirName);
            var configUserDir = Path.Combine(roamingConfig, AppDataDirName);
            if (!Directory.Exists(appDataUserDir) && Directory.Exists(configUserDir))
                appDataUserDir = configUserDir;

            string? portableUserDir = !string.IsNullOrWhiteSpace(selectedDir)
                ? Path.Combine(selectedDir, PortableDirName)
                : null;

            (string Config, Shadps4Config Values, Shadps4ConfigFormat Format)? appDataFound = null;
            (string Config, Shadps4Config Values, Shadps4ConfigFormat Format)? portableFound = null;
            bool appDataFolderPresent = false, portableFolderPresent = false;

            appDataFound = InspectCandidate(appDataUserDir, warnings, out appDataFolderPresent);
            if (appDataFound == null && !string.Equals(appDataUserDir, configUserDir, StringComparison.Ordinal))
            {
                var cfgFound = InspectCandidate(configUserDir, warnings, out bool cfgPresent);
                if (cfgFound != null)
                {
                    appDataFound = cfgFound;
                    appDataUserDir = configUserDir;
                    appDataFolderPresent = cfgPresent;
                }
            }
            if (portableUserDir != null)
                portableFound = InspectCandidate(portableUserDir, warnings, out portableFolderPresent);

            var candidatePaths = new List<string>();
            if (appDataFound != null) candidatePaths.Add(appDataFound.Value.Config);
            if (portableFound != null) candidatePaths.Add(portableFound.Value.Config);

            if (appDataFound != null && portableFound != null)
            {
                warnings.Add("Multiple shadPS4 configurations were found:");
                warnings.Add($"  AppData:  {appDataFound.Value.Config}");
                warnings.Add($"  Portable: {portableFound.Value.Config}");
                return Build(selectedExecutablePath, core, launcher, distribution,
                    null, null, Shadps4ConfigFormat.Unknown, Shadps4UserDirectoryMode.Ambiguous,
                    Shadps4DetectionConfidence.Low, Array.Empty<string>(), null, null, null, null,
                    candidatePaths, warnings);
            }

            var found = appDataFound ?? portableFound;
            if (found != null)
            {
                bool isPortable = appDataFound == null;
                var mode = isPortable ? Shadps4UserDirectoryMode.Portable : Shadps4UserDirectoryMode.AppData;
                string userDir = isPortable ? portableUserDir! : appDataUserDir;
                var values = found.Value.Values;

                if (!isPortable && portableFolderPresent)
                    warnings.Add($"Ignored stale portable user folder (no readable config): {portableUserDir}");

                bool hasCore = core != null && File.Exists(core);
                var dirs = ResolveInstallDirectories(values, userDir, warnings);
                string home = ResolveDefaulted(values.HomeDir, Path.Combine(userDir, "home"), userDir, warnings);
                string fonts = ResolveDefaulted(values.FontDir, Path.Combine(userDir, "fonts"), userDir, warnings);
                string sysModules = ResolveDefaulted(values.SysModulesDir, Path.Combine(userDir, "sys_modules"), userDir, warnings);
                string addon = ResolveDefaulted(values.AddonInstallDir, Path.Combine(userDir, "addcont"), userDir, warnings);

                return Build(selectedExecutablePath, core, launcher, distribution,
                    userDir, found.Value.Config, found.Value.Format, mode,
                    hasCore ? Shadps4DetectionConfidence.High : Shadps4DetectionConfidence.Low,
                    dirs, home, fonts, sysModules, addon, candidatePaths, warnings);
            }

            foreach (var (dir, present) in new[] { (appDataUserDir, appDataFolderPresent), (portableUserDir, portableFolderPresent) })
            {
                if (dir == null || !present) continue;
                warnings.Add($"shadPS4 user folder found without a readable config: {dir} (layout defaults assumed)");
                return Build(selectedExecutablePath, core, launcher, distribution,
                    dir, null, Shadps4ConfigFormat.Unknown, Shadps4UserDirectoryMode.Custom,
                    Shadps4DetectionConfidence.Low, Array.Empty<string>(),
                    Path.Combine(dir, "home"), Path.Combine(dir, "fonts"),
                    Path.Combine(dir, "sys_modules"), Path.Combine(dir, "addcont"),
                    candidatePaths, warnings);
            }

            return Build(selectedExecutablePath, core, launcher, distribution,
                appDataUserDir, null, Shadps4ConfigFormat.Unknown, Shadps4UserDirectoryMode.Unknown,
                Shadps4DetectionConfidence.None, Array.Empty<string>(),
                Path.Combine(appDataUserDir, "home"), Path.Combine(appDataUserDir, "fonts"),
                Path.Combine(appDataUserDir, "sys_modules"), Path.Combine(appDataUserDir, "addcont"),
                candidatePaths, warnings);
        }

        private static (string Config, Shadps4Config Values, Shadps4ConfigFormat Format)? InspectCandidate(
            string userDir, List<string> warnings, out bool folderPresent)
        {
            folderPresent = Directory.Exists(userDir);
            if (!folderPresent) return null;

            try
            {
                foreach (var name in new[] { "config.json", "config.toml" })
                {
                    string path = Path.Combine(userDir, name);
                    if (!File.Exists(path)) continue;
                    foreach (var reader in Readers)
                    {
                        if (!reader.Handles(path)) continue;
                        var values = reader.Read(path);
                        if (values == null)
                        {
                            warnings.Add($"Unreadable shadPS4 config ignored: {path}");
                            continue;
                        }
                        var format = name == "config.json"
                            ? Shadps4ConfigFormat.Json
                            : Shadps4ConfigFormat.LegacyToml;
                        return (path, values, format);
                    }
                }
            }
            catch { }
            return null;
        }

        private static IReadOnlyList<string> ResolveInstallDirectories(
            Shadps4Config values, string userDir, List<string> warnings)
        {
            var dirs = new List<string>();
            foreach (var d in values.InstallDirs)
            {
                if (!d.Enabled || string.IsNullOrWhiteSpace(d.Path)) continue;
                string normalized = NormalizeConfiguredPath(d.Path, userDir, warnings);
                dirs.Add(normalized);
                if (!Directory.Exists(normalized))
                    warnings.Add($"Configured game directory does not currently exist: {normalized}");
            }
            if (dirs.Count == 0)
                warnings.Add("shadPS4 has no enabled game libraries configured.");
            return dirs;
        }

        private static string ResolveDefaulted(string? configured, string fallback, string userDir, List<string> warnings)
            => NormalizeConfiguredPath(string.IsNullOrWhiteSpace(configured) ? fallback : configured!, userDir, warnings);

        private static string NormalizeConfiguredPath(string path, string userDir, List<string> warnings)
        {
            string p = path.Trim();
            if (p.Length >= 2 && ((p[0] == '"' && p[^1] == '"') || (p[0] == '\'' && p[^1] == '\'')))
                p = p[1..^1];
            if (p.Contains('%') || p.Contains("${"))
            {
                try { p = Environment.ExpandEnvironmentVariables(p); }
                catch { warnings.Add($"Could not expand environment variables in configured path: {path}"); }
            }
            p = p.Trim();
            if (OperatingSystem.IsWindows())
            {
                p = p.Replace('/', '\\');
                p = p.TrimEnd('\\', '/');
                if (p.Length >= 2 && p[1] == ':' && p.Length == 2)
                    p += '\\';
            }
            else
            {
                if (p.StartsWith("~/"))
                    p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), p[2..]);
                if (p.Length > 1)
                    p = p.TrimEnd('/');
            }
            if (!Path.IsPathRooted(p))
            {
                warnings.Add($"Relative configured path resolved against the user directory: {path}");
                p = Path.GetFullPath(Path.Combine(userDir, p));
            }
            return p;
        }

        private static Shadps4Environment Build(
            string? selected, string? core, string? launcher, Shadps4DistributionType distribution,
            string? userDir, string? configPath, Shadps4ConfigFormat format, Shadps4UserDirectoryMode mode,
            Shadps4DetectionConfidence confidence, IReadOnlyList<string> installDirs,
            string? home, string? fonts, string? sysModules, string? addon,
            IReadOnlyList<string> candidates, IReadOnlyList<string> warnings)
            => new()
            {
                SelectedExecutablePath = selected,
                CoreExePath = core,
                LauncherExePath = launcher,
                DistributionType = distribution,
                UserDirectory = userDir,
                ConfigPath = configPath,
                ConfigFormat = format,
                UserDirectoryMode = mode,
                DetectionConfidence = confidence,
                InstallDirectories = installDirs,
                HomeDirectory = home,
                FontDirectory = fonts,
                SysModulesDirectory = sysModules,
                AddonInstallDirectory = addon,
                CandidateConfigPaths = candidates,
                Warnings = warnings,
            };
    }
}
