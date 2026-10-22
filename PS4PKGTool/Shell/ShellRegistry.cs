using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PS4PKGTool.Utilities;
using PS4PKGTool.Utilities.PS4PKGToolHelper;

namespace PS4PKGTool.Shell
{
    public sealed record RegistryEntry(string KeyPath, string? ValueName, string? Value)
    {
        public const string DefaultValueName = "";
        public static RegistryEntry KeyOnly(string keyPath) => new(keyPath, null, null);
        public static RegistryEntry DWord(string keyPath, string valueName, int value)
            => new(keyPath, valueName, null) { DWordValue = value };
        public int? DWordValue { get; init; }
        public bool IsDWord => DWordValue.HasValue;
    }

    /// <summary>
    /// Per-user Linux desktop &amp; File Manager (.pkg) integration (XDG .desktop,
    /// MIME type application/x-ps4-pkg, GNOME Files/Nautilus scripts, and KDE
    /// Dolphin service menu). Requires no root privileges.
    /// </summary>
    public static class ShellRegistry
    {
        public const string PkgVerbKeySystemFileAssociations =
            @"Software\Classes\SystemFileAssociations\.pkg\shell\PS4PKGTool";

        internal const string DisabledStateHandlerClsid = "{5B6D1451-B1E1-4372-90F5-88E541B4DAB9}";
        internal const int NeverMatchingAttributeMask = 0x2000;
        internal const int NeverMatchingAttributeValue = 0x4000;

        public static string ExecutablePath
        {
            get
            {
                string? appImage = Environment.GetEnvironmentVariable("APPIMAGE");
                if (!string.IsNullOrWhiteSpace(appImage) && File.Exists(appImage))
                    return appImage;
                string? procPath = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(procPath) && File.Exists(procPath))
                    return procPath;
                return Path.Combine(AppContext.BaseDirectory, "PS4PKGTool");
            }
        }

        public static string IconPath => Path.Combine(Helper.UserDataDirectory, "ps4pkgtool.png");

        internal static string? VerbRootOverride = null;

        private static string HomeDir => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static string DesktopFilePath => Path.Combine(HomeDir, ".local", "share", "applications", "ps4pkgtool.desktop");
        private static string MimeFilePath => Path.Combine(HomeDir, ".local", "share", "mime", "packages", "ps4pkgtool-mime.xml");
        private static string KdeServiceMenuPath => Path.Combine(HomeDir, ".local", "share", "kio", "servicemenus", "ps4pkgtool.desktop");
        private static string NautilusScriptsDir => Path.Combine(HomeDir, ".local", "share", "nautilus", "scripts");

        public static bool IsInstalled()
        {
            try
            {
                return File.Exists(DesktopFilePath);
            }
            catch { return false; }
        }

        public static void Install(string? executablePath = null)
        {
            Remove();
            string exe = executablePath ?? ExecutablePath;
            try
            {
                // 1) Save icon to durable user directory
                Directory.CreateDirectory(Helper.UserDataDirectory);
                byte[] iconBytes = Properties.Resources.PackageIcon;
                if (iconBytes != null && iconBytes.Length > 0)
                    File.WriteAllBytes(IconPath, iconBytes);

                // 2) Install XDG MIME definition for *.pkg
                string mimeDir = Path.GetDirectoryName(MimeFilePath)!;
                Directory.CreateDirectory(mimeDir);
                File.WriteAllText(MimeFilePath,
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<mime-info xmlns=""http://www.freedesktop.org/standards/shared-mime-info"">
  <mime-type type=""application/x-ps4-pkg"">
    <comment>PlayStation 4 Package (.pkg)</comment>
    <glob pattern=""*.pkg""/>
    <glob pattern=""*.PKG""/>
  </mime-type>
</mime-info>
");

                // 3) Install XDG .desktop entry with Actions
                string appsDir = Path.GetDirectoryName(DesktopFilePath)!;
                Directory.CreateDirectory(appsDir);
                File.WriteAllText(DesktopFilePath,
$@"[Desktop Entry]
Type=Application
Name=PS4 PKG Tool
Comment=PlayStation 4 PKG Manager, Asset Viewer, and shadPS4 Helper
Exec=""{exe}"" %f
Icon={IconPath}
Terminal=false
Categories=Game;Utility;Emulator;
MimeType=application/x-ps4-pkg;
Actions=Validate;Extract;Rename;InstallShadps4;CopyTitleId;

[Desktop Action Validate]
Name=Validate PKG
Exec=""{exe}"" --shell validate %F

[Desktop Action Extract]
Name=Extract PKG
Exec=""{exe}"" --shell extract %F

[Desktop Action Rename]
Name=Rename PKG...
Exec=""{exe}"" --shell rename 11 %F

[Desktop Action InstallShadps4]
Name=Install to shadPS4
Exec=""{exe}"" --shell install-shadps4 %F

[Desktop Action CopyTitleId]
Name=Copy Title ID
Exec=""{exe}"" --shell copy titleid %F
");

                // 4) Install KDE Dolphin Service Menu
                string kdeDir = Path.GetDirectoryName(KdeServiceMenuPath)!;
                Directory.CreateDirectory(kdeDir);
                File.WriteAllText(KdeServiceMenuPath,
$@"[Desktop Entry]
Type=Service
MimeType=application/x-ps4-pkg;application/octet-stream;
Actions=OpenViewer;ValidatePkg;ExtractPkg;RenamePkg;InstallShadps4;CopyTitle;CopyTitleId;CopyContentId;
X-KDE-Submenu=PS4 PKG Tool
Icon={IconPath}

[Desktop Action OpenViewer]
Name=Open in Mini PKG Viewer
Icon={IconPath}
Exec=""{exe}"" %f

[Desktop Action ValidatePkg]
Name=Validate PKG
Icon={IconPath}
Exec=""{exe}"" --shell validate %F

[Desktop Action ExtractPkg]
Name=Extract PKG
Icon={IconPath}
Exec=""{exe}"" --shell extract %F

[Desktop Action RenamePkg]
Name=Rename PKG...
Icon={IconPath}
Exec=""{exe}"" --shell rename 11 %F

[Desktop Action InstallShadps4]
Name=Install to shadPS4
Icon={IconPath}
Exec=""{exe}"" --shell install-shadps4 %F

[Desktop Action CopyTitle]
Name=Copy Title
Icon={IconPath}
Exec=""{exe}"" --shell copy title %F

[Desktop Action CopyTitleId]
Name=Copy Title ID
Icon={IconPath}
Exec=""{exe}"" --shell copy titleid %F

[Desktop Action CopyContentId]
Name=Copy Content ID
Icon={IconPath}
Exec=""{exe}"" --shell copy contentid %F
");
                TryChmodExecutable(KdeServiceMenuPath);

                // 5) Install GNOME Files (Nautilus) scripts
                Directory.CreateDirectory(NautilusScriptsDir);
                WriteNautilusScript("PS4 PKG Tool - Validate PKG", $"\"{exe}\" --shell validate $NAUTILUS_SCRIPT_SELECTED_FILE_PATHS");
                WriteNautilusScript("PS4 PKG Tool - Extract PKG", $"\"{exe}\" --shell extract $NAUTILUS_SCRIPT_SELECTED_FILE_PATHS");
                WriteNautilusScript("PS4 PKG Tool - Rename PKG", $"\"{exe}\" --shell rename 11 $NAUTILUS_SCRIPT_SELECTED_FILE_PATHS");
                WriteNautilusScript("PS4 PKG Tool - Install to shadPS4", $"\"{exe}\" --shell install-shadps4 $NAUTILUS_SCRIPT_SELECTED_FILE_PATHS");

                // Refresh XDG databases best-effort
                TryRun("update-mime-database", Path.Combine(HomeDir, ".local", "share", "mime"));
                TryRun("update-desktop-database", appsDir);
                TryRun("xdg-mime", "default", "ps4pkgtool.desktop", "application/x-ps4-pkg");

                Logger.LogInformation("Linux File Manager (.pkg) integration installed.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("ShellRegistry.Install failed: " + ex.Message);
            }
        }

        public static void Remove()
        {
            try
            {
                TryDeleteFile(DesktopFilePath);
                TryDeleteFile(MimeFilePath);
                TryDeleteFile(KdeServiceMenuPath);
                if (Directory.Exists(NautilusScriptsDir))
                {
                    foreach (var f in Directory.GetFiles(NautilusScriptsDir, "PS4 PKG Tool - *"))
                        TryDeleteFile(f);
                }
                TryRun("update-mime-database", Path.Combine(HomeDir, ".local", "share", "mime"));
                TryRun("update-desktop-database", Path.Combine(HomeDir, ".local", "share", "applications"));
                Logger.LogInformation("Linux File Manager (.pkg) integration removed.");
            }
            catch { }
        }

        public static string BuildCommandLine(string executablePath, string verb, string? argument = null)
        {
            string exe = "\"" + executablePath + "\"";
            string action = argument == null ? $"{exe} --shell {verb}" : $"{exe} --shell {verb} {argument}";
            return action + " \"%1\"";
        }

        private static readonly HashSet<int> ShellRenameFormatIds = new() { 1, 2, 3, 4, 5 };

        public static IReadOnlyList<RegistryEntry> BuildEntries(string executablePath)
        {
            var entries = new List<RegistryEntry>();
            string root = VerbRootOverride ?? PkgVerbKeySystemFileAssociations;
            entries.Add(new RegistryEntry(root, "MUIVerb", "PS4 PKG Tool"));
            return entries;
        }

        private static void WriteNautilusScript(string scriptName, string cmdLine)
        {
            string path = Path.Combine(NautilusScriptsDir, scriptName);
            File.WriteAllText(path, "#!/bin/sh\nIFS='\n'\n" + cmdLine + "\n");
            TryChmodExecutable(path);
        }

        private static void TryChmodExecutable(string path)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(path,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                }
            }
            catch { }
        }

        private static void TryDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static void TryRun(string file, params string[] args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                foreach (var a in args) psi.ArgumentList.Add(a);
                using var p = Process.Start(psi);
                p?.WaitForExit(3000);
            }
            catch { }
        }
    }
}
