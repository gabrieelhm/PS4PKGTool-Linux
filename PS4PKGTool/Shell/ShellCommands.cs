using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PS4PKGTool.Startup;
using PS4PKGTool.Util.Constants;
using PS4PKGTool.Utilities;
using PS4PKGTool.Utilities.Constants;
using PS4PKGTool.Utilities.PkgInspection;
using PS4PKGTool.Utilities.PS4PKGToolHelper;
using PS4PKGTool.Utilities.Settings;
using PS4PKGTool.Utilities.Shadps4;

namespace PS4PKGTool.Shell
{
    public static class ShellCommands
    {
        public static void Run(ShellRequest request)
        {
            string? pathError = ShellCommandRouter.ValidatePaths(request.PackagePaths);
            if (pathError != null)
            {
                NotifyUser("PS4 PKG Tool", pathError, isError: true);
                return;
            }

            switch (request.Kind)
            {
                case ShellCommandKind.Copy:
                    RunCopy(request.Field, request.PackagePaths);
                    break;
                case ShellCommandKind.Rename:
                    RunRename(request.FormatId, request.PackagePaths);
                    break;
                case ShellCommandKind.Validate:
                    RunValidate(request.PackagePaths);
                    break;
                case ShellCommandKind.Extract:
                    RunExtract(request.PackagePaths);
                    break;
                case ShellCommandKind.InstallShadps4:
                    RunInstallShadps4(request.PackagePaths);
                    break;
            }
        }

        private static bool GuardSinglePackage(IReadOnlyList<string> paths)
        {
            if (paths.Count <= 1) return true;
            NotifyUser("PS4 PKG Tool", "This action supports one PKG at a time. The other selected files were not touched.");
            return false;
        }

        private static PkgInspectionSnapshot? ReadMetadata(string path)
        {
            try
            {
                return new PkgInspectionService()
                    .InspectAsync(path, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Shell: could not read PKG metadata for {path}: {ex.Message}");
                return null;
            }
        }

        private static void RunCopy(string field, IReadOnlyList<string> paths)
        {
            var parts = new List<string>();
            foreach (string path in paths)
            {
                var meta = ReadMetadata(path);
                if (meta == null)
                {
                    NotifyUser("PS4 PKG Tool", "Could not read PKG metadata:\n" + path, isError: true);
                    return;
                }
                parts.Add(ExtractField(field, meta));
            }

            string text = field == "info" && parts.Count == 1 ? parts[0] : string.Join(Environment.NewLine, parts);
            if (!TrySetLinuxClipboard(text))
            {
                Console.WriteLine(text);
                NotifyUser("PS4 PKG Tool", text);
                return;
            }

            string label = parts.Count == 1 ? parts[0] : parts.Count + " values";
            NotifyUser("PS4 PKG Tool", (field == "info" ? "PKG info copied" : CopyFieldLabel(field) + " copied") + ":\n" + Truncate(label, 80));
        }

        public static bool TrySetLinuxClipboard(string text)
        {
            string[][] candidates =
            {
                new[] { "wl-copy", "" },
                new[] { "xclip", "-selection clipboard" },
                new[] { "xsel", "--clipboard --input" }
            };

            foreach (var c in candidates)
            {
                try
                {
                    if (!Helper.NodeJsHttpServer.CommandExists(c[0])) continue;
                    using var p = Process.Start(new ProcessStartInfo
                    {
                        FileName = c[0],
                        Arguments = c[1],
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        CreateNoWindow = true
                    });
                    if (p != null)
                    {
                        p.StandardInput.Write(text);
                        p.StandardInput.Close();
                        p.WaitForExit(2000);
                        if (p.ExitCode == 0) return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public static void NotifyUser(string title, string message, bool isError = false)
        {
            Console.WriteLine($"[{title}] {message}");
            try
            {
                if (Helper.NodeJsHttpServer.CommandExists("notify-send"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "notify-send",
                        Arguments = $"-a \"PS4 PKG Tool\" -u {(isError ? "critical" : "normal")} \"{title.Replace("\"", "\\\"")}\" \"{message.Replace("\"", "\\\"")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
            }
            catch { }
        }

        internal static string ExtractField(string field, PkgInspectionSnapshot meta) => field switch
        {
            "info" => BuildPackageInfo(meta),
            "title" => meta.Title,
            "titleid" => meta.TitleId,
            "contentid" => meta.ContentId,
            "version" => meta.PackageVersion,
            "appversion" => meta.ApplicationVersion,
            "fullpath" => meta.PackagePath,
            _ => meta.PackagePath,
        };

        internal static string BuildPackageInfo(PkgInspectionSnapshot m)
        {
            var lines = new List<string>
            {
                "Title: " + m.Title,
                "Title ID: " + m.TitleId,
                "Content ID: " + m.ContentId,
                "Version: " + m.PackageVersion,
                "App Version: " + m.ApplicationVersion,
                "Type: " + m.PackageCategory,
                "Size: " + m.PackageSize,
                "Path: " + m.PackagePath,
            };
            return string.Join(Environment.NewLine, lines);
        }

        private static string CopyFieldLabel(string field) => field switch
        {
            "title" => "Title",
            "titleid" => "Title ID",
            "contentid" => "Content ID",
            "version" => "Version",
            "appversion" => "App Version",
            "fullpath" => "Full path",
            _ => "Value",
        };

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value.Substring(0, max) + "...";

        private static void RunRename(int formatId, IReadOnlyList<string> paths)
        {
            string? format = PkgRenameFormats.GetFormat(formatId, SettingsManager.appSettings_.RenameCustomName);
            if (string.IsNullOrWhiteSpace(format))
            {
                NotifyUser("PS4 PKG Tool", "The rename format is not available.", isError: true);
                return;
            }

            string? lastRenamed = null;
            foreach (string path in paths)
            {
                string? renamed = RenameOne(path, format);
                if (renamed == null) return;
                lastRenamed = renamed;
            }

            if (lastRenamed != null)
            {
                NotifyUser("PS4 PKG Tool",
                    (paths.Count > 1 ? paths.Count + " PKGs renamed" : "Renamed to") + ":\n" + Path.GetFileName(lastRenamed));
            }
        }

        private static string? RenameOne(string sourcePath, string format)
        {
            try
            {
                string destinationFolder = (Path.GetDirectoryName(sourcePath) ?? "") + Path.DirectorySeparatorChar;
                (_, _, string targetPkg) =
                    PS4PKGTool.Utilities.PkgRename.PkgRenameService.GetNewPKGName(sourcePath, destinationFolder, format);

                if (File.Exists(targetPkg))
                {
                    NotifyUser("PS4 PKG Tool",
                        "A file with the target name already exists. The PKG was not renamed:\n" + Path.GetFileName(targetPkg),
                        isError: true);
                    return null;
                }

                File.Move(sourcePath, targetPkg);
                Logger.LogInformation($"Shell renamed: {Path.GetFileName(sourcePath)} -> {Path.GetFileName(targetPkg)}");
                return targetPkg;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Shell rename failed: " + ex.Message);
                NotifyUser("PS4 PKG Tool", "The PKG could not be renamed: " + ex.Message, isError: true);
                return null;
            }
        }

        private static void RunValidate(IReadOnlyList<string> paths)
        {
            if (!GuardSinglePackage(paths)) return;
            string path = paths[0];
            string fileName = Path.GetFileName(path);

            try
            {
                var meta = new PkgInspectionService().InspectAsync(path, CancellationToken.None).GetAwaiter().GetResult();
                var listing = new PkgFileListingService().ListAsync(path, PkgFileListingService.DefaultPasscode, CancellationToken.None).GetAwaiter().GetResult();
                if (listing.Succeeded)
                {
                    NotifyUser("PKG Validated", $"{fileName}\nTitle: {meta.Title} ({meta.TitleId})\nEntries: {listing.Entries.Count}\nWarnings: 0, Errors: 0");
                }
                else
                {
                    NotifyUser("PKG Validation Failed", $"{fileName}\n{listing.ErrorMessage}", isError: true);
                }
            }
            catch (Exception ex)
            {
                NotifyUser("PKG Validation Failed", ex.Message, isError: true);
            }
        }

        internal static bool IsPasscodeFailure(string errorMessage) =>
            !string.IsNullOrWhiteSpace(errorMessage) &&
            errorMessage.IndexOf("passcode", StringComparison.OrdinalIgnoreCase) >= 0;

        private static void RunExtract(IReadOnlyList<string> paths)
        {
            if (!GuardSinglePackage(paths)) return;
            string path = paths[0];
            var meta = ReadMetadata(path);
            string title = meta?.Title ?? Path.GetFileNameWithoutExtension(path);
            string parentDir = Path.GetDirectoryName(path) ?? Environment.CurrentDirectory;
            string extractLocation = Path.Combine(parentDir, PkgExtractionService.SanitizeFolderName(title));

            NotifyUser("PS4 PKG Tool", $"Extracting {title} to:\n{extractLocation}");
            var (succeeded, message) = new PkgExtractionService(passcode: PkgFileListingService.DefaultPasscode)
                .ExtractFullAsync(path, extractLocation, null, CancellationToken.None, null, logCompletion: true, layout: ExtractLayout.Merged)
                .GetAwaiter().GetResult();

            if (succeeded)
                NotifyUser("Extraction Complete", $"{title}\nSaved to: {extractLocation}");
            else
                NotifyUser("Extraction Failed", message, isError: true);
        }

        private static void RunInstallShadps4(IReadOnlyList<string> paths)
        {
            if (!GuardSinglePackage(paths)) return;
            string path = paths[0];
            var meta = ReadMetadata(path);
            if (meta == null)
            {
                NotifyUser("PS4 PKG Tool", "Could not read PKG metadata.", isError: true);
                return;
            }

            string titleId = meta.TitleId;
            string title = meta.Title;
            bool isGame = string.Equals(meta.PackageCategory, PKGCategory.GAME, StringComparison.OrdinalIgnoreCase);
            bool isPatch = string.Equals(meta.PackageCategory, PKGCategory.PATCH, StringComparison.OrdinalIgnoreCase);
            if (!isGame && !isPatch)
            {
                NotifyUser("PS4 PKG Tool", "Select a base game or update (patch) PKG. DLC/addon installation is not supported yet.", isError: true);
                return;
            }

            string library = SettingsManager.appSettings_.Shadps4InstallDirectory?.Trim() ?? "";
            if (string.IsNullOrEmpty(library) || !Directory.Exists(library))
            {
                var store = new Shadps4ManagedBuilds(SettingsManager.appSettings_.Shadps4ManagedRoot);
                library = Path.Combine(store.RootPath, "games");
                Directory.CreateDirectory(library);
            }

            NotifyUser("shadPS4 Install", $"Installing {title} ({titleId}) into {library}...");
            var svc = new Shadps4InstallService { Passcode = PkgFileListingService.DefaultPasscode };
            var result = svc.Install(path, titleId, library, replaceExisting: true, progress: null, ct: CancellationToken.None, mergeIntoExisting: isPatch);

            if (result.Status == Shadps4InstallStatus.Success)
                NotifyUser("shadPS4 Install Complete", $"{title} ({titleId}) installed to {result.InstalledPath ?? Path.Combine(library, titleId)}");
            else
                NotifyUser("shadPS4 Install Failed", result.Message ?? "Installation failed.", isError: true);
        }
    }
}
