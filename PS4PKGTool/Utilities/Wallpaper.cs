using PS4PKGTool.Utilities;
using PS4PKGTool.Utilities.PS4PKGToolHelper;
using System;
using System.Diagnostics;
using System.IO;

namespace PS4PKGTool.Util
{
    public sealed class Wallpaper
    {
        Wallpaper() { }

        public enum Style : int
        {
            Tiled,
            Centered,
            Stretched
        }

        public static void Set(Style style)
        {
            string savedDir = Path.Combine(Helper.AppDataDirectory, "Saved image");
            string path = Path.Combine(savedDir, "wallpaper.png");
            if (!File.Exists(path))
                path = Path.Combine(savedDir, "wallpaper.JPG");
            SetFromFile(path, style);
        }

        public static bool SetFromBytes(byte[] imageBytes, Style style, out string error)
        {
            error = null;
            if (imageBytes == null || imageBytes.Length == 0)
            {
                error = "No artwork bytes available.";
                return false;
            }
            try
            {
                string savedDir = Path.Combine(Helper.AppDataDirectory, "Saved image");
                Directory.CreateDirectory(savedDir);
                string path = Path.Combine(savedDir, "wallpaper.png");
                File.WriteAllBytes(path, imageBytes);
                return SetFromFile(path, style, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool SetFromFile(string path, Style style)
            => SetFromFile(path, style, out _);

        public static bool SetFromFile(string path, Style style, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                error = "Wallpaper image file does not exist.";
                return false;
            }

            string fullPath = Path.GetFullPath(path);
            string fileUri = new Uri(fullPath).AbsoluteUri;
            string gnomeMode = style switch
            {
                Style.Tiled => "wallpaper",
                Style.Centered => "centered",
                _ => "stretched"
            };

            try
            {
                string desktop = (Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "").ToLowerInvariant();

                // 1) GNOME / Ubuntu / Budgie / Pantheon
                if (desktop.Contains("gnome") || desktop.Contains("ubuntu") || desktop.Contains("pantheon") || desktop.Contains("budgie"))
                {
                    RunProcess("gsettings", "set", "org.gnome.desktop.background", "picture-uri", fileUri);
                    RunProcess("gsettings", "set", "org.gnome.desktop.background", "picture-uri-dark", fileUri);
                    RunProcess("gsettings", "set", "org.gnome.desktop.background", "picture-options", gnomeMode);
                    Logger.LogInformation($"Wallpaper set via gsettings (GNOME): {fullPath} ({style})");
                    return true;
                }

                // 2) KDE Plasma
                if (desktop.Contains("kde") || desktop.Contains("plasma"))
                {
                    if (RunProcess("plasma-apply-wallpaperimage", fullPath))
                    {
                        Logger.LogInformation($"Wallpaper set via plasma-apply-wallpaperimage: {fullPath}");
                        return true;
                    }
                }

                // 3) Cinnamon
                if (desktop.Contains("cinnamon"))
                {
                    RunProcess("gsettings", "set", "org.cinnamon.desktop.background", "picture-uri", fileUri);
                    RunProcess("gsettings", "set", "org.cinnamon.desktop.background", "picture-options", gnomeMode);
                    return true;
                }

                // 4) MATE
                if (desktop.Contains("mate"))
                {
                    RunProcess("gsettings", "set", "org.mate.background", "picture-filename", fullPath);
                    RunProcess("gsettings", "set", "org.mate.background", "picture-options", gnomeMode);
                    return true;
                }

                // 5) Fallback chain across common Linux tools
                if (RunProcess("gsettings", "set", "org.gnome.desktop.background", "picture-uri", fileUri))
                {
                    RunProcess("gsettings", "set", "org.gnome.desktop.background", "picture-uri-dark", fileUri);
                    RunProcess("gsettings", "set", "org.gnome.desktop.background", "picture-options", gnomeMode);
                    return true;
                }
                if (RunProcess("plasma-apply-wallpaperimage", fullPath))
                    return true;

                string fehFlag = style switch
                {
                    Style.Tiled => "--bg-tile",
                    Style.Centered => "--bg-center",
                    _ => "--bg-scale"
                };
                if (RunProcess("feh", fehFlag, fullPath))
                    return true;

                error = "Could not detect a supported desktop wallpaper tool (tried gsettings, plasma-apply-wallpaperimage, feh).";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Logger.LogWarning("Wallpaper.Set failed: " + ex.Message);
                return false;
            }
        }

        private static bool RunProcess(string fileName, params string[] args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                foreach (var a in args) psi.ArgumentList.Add(a);
                using var proc = Process.Start(psi);
                if (proc == null) return false;
                proc.WaitForExit(4000);
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
