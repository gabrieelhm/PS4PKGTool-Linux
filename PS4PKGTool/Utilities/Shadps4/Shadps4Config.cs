using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Model;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>The subset of shadPS4's configuration that PS4PKGTool needs.</summary>
    public sealed class Shadps4Config
    {
        public IReadOnlyList<Shadps4InstallDir> InstallDirs { get; init; } = Array.Empty<Shadps4InstallDir>();
        public string? AddonInstallDir { get; init; }
        public string? HomeDir { get; init; }
        public string? FontDir { get; init; }
        public string? SysModulesDir { get; init; }
    }

    /// <summary>One configured game library entry (shadPS4 General.install_dirs).</summary>
    public sealed record Shadps4InstallDir(string Path, bool Enabled);

    /// <summary>
    /// Reads the values PS4PKGTool needs from a shadPS4 config file.
    /// Isolated behind an interface so the parser can be swapped.
    /// </summary>
    public interface IShadps4ConfigReader
    {
        /// <summary>True when this reader handles the given file (by name).</summary>
        bool Handles(string filePath);

        /// <summary>Parses the config, or null when it is missing/unreadable/malformed.</summary>
        Shadps4Config? Read(string filePath);
    }

    // ── JSON DTOs (System.Text.Json, explicit mappings, unknown properties ignored) ──

    internal sealed class Shadps4ConfigRoot
    {
        [JsonPropertyName("General")]
        public Shadps4GeneralConfig? General { get; set; }
    }

    internal sealed class Shadps4GeneralConfig
    {
        [JsonPropertyName("install_dirs")]
        public List<Shadps4InstallDirectory>? InstallDirs { get; set; }

        [JsonPropertyName("addon_install_dir")]
        public string? AddonInstallDir { get; set; }

        [JsonPropertyName("home_dir")]
        public string? HomeDir { get; set; }

        [JsonPropertyName("font_dir")]
        public string? FontDir { get; set; }

        [JsonPropertyName("sys_modules_dir")]
        public string? SysModulesDir { get; set; }
    }

    internal sealed class Shadps4InstallDirectory
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }

    /// <summary>
    /// Current shadPS4 JSON config backend. Verified against the real
    /// %APPDATA%\shadPS4\config.json on this machine and the shadPS4 source
    /// (src/core/emulator_settings.h):
    ///   General.install_dirs      = [{ "enabled": bool, "path": "..." }, ...]
    ///   General.addon_install_dir = "..."   (empty = not configured)
    ///   General.home_dir          = "..."   (empty = default &lt;user&gt;\home)
    ///   General.font_dir          = "..."   (empty = default &lt;user&gt;\fonts)
    ///   General.sys_modules_dir   = "..."   (empty = default &lt;user&gt;\sys_modules)
    /// Unknown properties are ignored; missing General/install_dirs are valid
    /// (empty) states, not parse failures. Section-name casing is matched
    /// case-insensitively ("General" in launcher-managed configs, "general"
    /// in core-written ones).
    /// </summary>
    public sealed class Shadps4JsonConfigReader : IShadps4ConfigReader
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public bool Handles(string filePath)
            => Path.GetFileName(filePath).Equals("config.json", StringComparison.OrdinalIgnoreCase);

        public Shadps4Config? Read(string filePath)
        {
            try
            {
                var root = JsonSerializer.Deserialize<Shadps4ConfigRoot>(File.ReadAllText(filePath), Options);
                if (root?.General == null) return new Shadps4Config();

                var dirs = new List<Shadps4InstallDir>();
                if (root.General.InstallDirs != null)
                {
                    foreach (var item in root.General.InstallDirs)
                    {
                        if (string.IsNullOrWhiteSpace(item.Path)) continue;
                        dirs.Add(new Shadps4InstallDir(item.Path.Trim(), item.Enabled));
                    }
                }

                return new Shadps4Config
                {
                    InstallDirs = dirs,
                    AddonInstallDir = NormalizeOptional(root.General.AddonInstallDir),
                    HomeDir = NormalizeOptional(root.General.HomeDir),
                    FontDir = NormalizeOptional(root.General.FontDir),
                    SysModulesDir = NormalizeOptional(root.General.SysModulesDir),
                };
            }
            catch
            {
                return null; // missing / unreadable / malformed
            }
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Legacy shadPS4 config.toml (verified against shadPS4 main,
    /// src/core/emulator_settings.cpp, TransferSettings): the current
    /// emulator migrates this to config.json on launch, but a config.toml
    /// can still be the only file present when the emulator has not been
    /// started since the backend change.
    ///   [GUI]
    ///   installDirs         = ["...", ...]
    ///   installDirsEnabled  = [true, ...]   (parallel, optional)
    ///   addonInstallDir     = "..."
    /// </summary>
    public sealed class Shadps4TomlConfigReader : IShadps4ConfigReader
    {
        public bool Handles(string filePath)
            => Path.GetFileName(filePath).Equals("config.toml", StringComparison.OrdinalIgnoreCase);

        public Shadps4Config? Read(string filePath)
        {
            try
            {
                var model = Toml.ToModel(File.ReadAllText(filePath));
                if (model is not TomlTable root) return null;
                // Tomlyn's indexer THROWS on absent keys - use TryGetValue.
                if (!root.TryGetValue("GUI", out var guiValue) || guiValue is not TomlTable gui)
                    return new Shadps4Config();

                var dirs = new List<Shadps4InstallDir>();
                var enabled = new List<bool>();
                if (gui.TryGetValue("installDirs", out var dirsValue) && dirsValue is TomlArray dirArray)
                {
                    foreach (var item in dirArray)
                        if (item is string s && !string.IsNullOrWhiteSpace(s))
                            dirs.Add(new Shadps4InstallDir(s.Trim(), true));
                }
                if (gui.TryGetValue("installDirsEnabled", out var enabledValue) && enabledValue is TomlArray enabledArray)
                {
                    foreach (var item in enabledArray)
                        enabled.Add(item is bool b && b);
                }
                for (int i = 0; i < dirs.Count; i++)
                {
                    bool isEnabled = i < enabled.Count ? enabled[i] : true;
                    if (dirs[i].Enabled != isEnabled)
                        dirs[i] = new Shadps4InstallDir(dirs[i].Path, isEnabled);
                }

                string? addonDir = gui.TryGetValue("addonInstallDir", out var addonValue) && addonValue is string addonStr
                    ? addonStr
                    : null;
                if (string.IsNullOrWhiteSpace(addonDir)) addonDir = null;

                return new Shadps4Config { InstallDirs = dirs, AddonInstallDir = addonDir?.Trim(), HomeDir = null };
            }
            catch
            {
                return null; // missing / unreadable / malformed
            }
        }
    }
}
