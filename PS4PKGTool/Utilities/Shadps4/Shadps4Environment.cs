using System;
using System.Collections.Generic;

namespace PS4PKGTool.Utilities.Shadps4
{
    public enum Shadps4UserDirectoryMode
    {
        /// <summary>No shadPS4 user directory found.</summary>
        Unknown,
        /// <summary>Portable layout: user\ next to the emulator executable.</summary>
        Portable,
        /// <summary>Roaming AppData layout: %APPDATA%\shadPS4.</summary>
        AppData,
        /// <summary>A user folder exists but its config is missing or unreadable - layout defaults only.</summary>
        Custom,
        /// <summary>Multiple plausible user directories exist and none was selected silently.</summary>
        Ambiguous,
    }

    public enum Shadps4DetectionConfidence
    {
        None,
        Low,
        High,
    }

    /// <summary>Which executables the selected shadPS4 distribution provides.</summary>
    public enum Shadps4DistributionType
    {
        Unknown,
        /// <summary>Only shadPS4.exe (the emulator core) was found.</summary>
        CoreOnly,
        /// <summary>Only shadPS4QtLauncher.exe was found.</summary>
        QtLauncherOnly,
        /// <summary>Both the core and the Qt launcher exist in the selected folder.</summary>
        CoreAndQtLauncher,
        /// <summary>Pre-v0.12 builds with the GUI integrated into the core (not reliably detectable).</summary>
        LegacyIntegrated,
    }

    /// <summary>The format of the configuration file the environment was resolved from.</summary>
    public enum Shadps4ConfigFormat
    {
        Unknown,
        Json,
        LegacyToml,
    }

    /// <summary>
    /// Resolved view of a shadPS4 installation. Nothing here is persisted -
    /// it is re-derived from the configured executable and the filesystem
    /// every time it is needed. PS4PKGTool reads shadPS4's config; it never
    /// rewrites it. The executable location and the shared configuration
    /// are SEPARATE concepts: a freshly extracted launcher still reads the
    /// shared %APPDATA%\shadPS4\config.json.
    /// </summary>
    public sealed class Shadps4Environment
    {
        /// <summary>The executable the user selected in settings.</summary>
        public string? SelectedExecutablePath { get; init; }

        /// <summary>The emulator core (shadPS4.exe), when the distribution provides one.</summary>
        public string? CoreExePath { get; init; }

        /// <summary>The Qt launcher (shadPS4QtLauncher.exe), when the distribution provides one.</summary>
        public string? LauncherExePath { get; init; }

        public Shadps4DistributionType DistributionType { get; init; } = Shadps4DistributionType.Unknown;

        /// <summary>The user directory that was selected as authoritative, or null.</summary>
        public string? UserDirectory { get; init; }

        /// <summary>The config file that produced the values (config.json or config.toml), or null.</summary>
        public string? ConfigPath { get; init; }

        public Shadps4ConfigFormat ConfigFormat { get; init; } = Shadps4ConfigFormat.Unknown;

        public Shadps4UserDirectoryMode UserDirectoryMode { get; init; } = Shadps4UserDirectoryMode.Unknown;

        /// <summary>Enabled game libraries from the config (possibly empty).</summary>
        public IReadOnlyList<string> InstallDirectories { get; init; } = Array.Empty<string>();

        /// <summary>Save-data root (General.home_dir, default &lt;user&gt;\home).</summary>
        public string? HomeDirectory { get; init; }

        /// <summary>Font directory (General.font_dir, default &lt;user&gt;\fonts).</summary>
        public string? FontDirectory { get; init; }

        /// <summary>System-modules directory (General.sys_modules_dir, default &lt;user&gt;\sys_modules).</summary>
        public string? SysModulesDirectory { get; init; }

        /// <summary>Add-on/DLC directory (General.addon_install_dir, default &lt;user&gt;\addcont).</summary>
        public string? AddonInstallDirectory { get; init; }

        public Shadps4DetectionConfidence DetectionConfidence { get; init; } = Shadps4DetectionConfidence.None;

        /// <summary>All candidate config files that were considered (for the Ambiguous display).</summary>
        public IReadOnlyList<string> CandidateConfigPaths { get; init; } = Array.Empty<string>();

        /// <summary>Human-readable detection notes (stale folders, missing dirs, malformed configs...).</summary>
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

        /// <summary>True when a known configuration was resolved (mode is Portable/AppData/Custom).</summary>
        public bool IsValid =>
            UserDirectoryMode is Shadps4UserDirectoryMode.Portable
                or Shadps4UserDirectoryMode.AppData
                or Shadps4UserDirectoryMode.Custom;

        /// <summary>True when the environment can run launch operations (core exe available).</summary>
        public bool IsUsable =>
            IsValid
            && !string.IsNullOrEmpty(CoreExePath)
            && System.IO.File.Exists(CoreExePath);
    }
}
