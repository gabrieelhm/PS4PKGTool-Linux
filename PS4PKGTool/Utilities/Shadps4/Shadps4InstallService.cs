using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PS4PKGTool.Utilities.PS4PKGToolHelper;

namespace PS4PKGTool.Utilities.Shadps4
{
    public enum Shadps4InstallStatus
    {
        Success,
        /// <summary>The destination CUSA folder already exists and the user did not approve a replacement.</summary>
        ExistingInstall,
        InsufficientSpace,
        PkgMissing,
        LibraryMissing,
        ExtractionFailed,
        ValidationFailed,
        Cancelled,
        Failed,
    }

    public sealed record Shadps4InstallResult(
        Shadps4InstallStatus Status,
        string Message,
        string? InstalledPath = null);

    /// <summary>
    /// Transactional "Install Game to shadPS4 Library": extracts a base-game
    /// PKG into a staging folder inside the destination library, validates the
    /// extracted layout, then atomically renames it to the CUSA folder.
    ///
    /// Never extracts directly into the final directory: on cancellation,
    /// extraction failure, validation failure or disk-full the staging folder
    /// is removed and any existing installation is left untouched.
    /// The extractor is OrbisPkgTool.PkgReader in-process (read-only on the
    /// PKG, writes straight into the staging folder - Unicode paths work).
    /// </summary>
    public sealed class Shadps4InstallService
    {
        /// <summary>Same passcode the app uses everywhere else.</summary>
        public const string DefaultPasscode = "00000000000000000000000000000000";

        /// <summary>Extra margin on top of the estimated extracted size.</summary>
        public const long SpaceMarginBytes = 1L * 1024 * 1024 * 1024; // 1 GB

        /// <summary>
        /// The passcode used to decrypt the package. Defaults to the standard
        /// all-zero code; callers that already know a package uses a custom
        /// one (e.g. a shell passcode prompt) set it before Install.
        /// </summary>
        public string Passcode { get; set; } = DefaultPasscode;

        /// <summary>Test seam: replaces the orbis-based extraction. (pkg, destDir, ct) -> success.</summary>
        public Func<string, string, CancellationToken, bool>? ExtractOverride { get; set; }

        /// <summary>Test seam: free bytes on the destination volume. (libraryDir) -> bytes.</summary>
        public Func<string, long>? FreeSpaceOverride { get; set; }

        /// <summary>
        /// Installs the PKG into the given shadPS4 library as
        /// &lt;library&gt;\&lt;CUSAxxxxx&gt;. replaceExisting approves overwriting an
        /// existing installation (the UI must confirm first).
        /// mergeIntoExisting is the update/patch path: instead of rejecting or
        /// replacing an existing folder, the extracted (flattened) patch files
        /// are merged over it - same-named files are overwritten, base-only
        /// files remain. Without an existing folder it behaves like a fresh
        /// install, so a patch can also be installed before its base game.
        /// </summary>
        public Shadps4InstallResult Install(
            string pkgPath, string titleId, string libraryDir, bool replaceExisting,
            IProgress<string>? progress = null, CancellationToken ct = default,
            bool mergeIntoExisting = false,
            IProgress<(int Current, int Total, string CurrentFile)>? fileProgress = null)
        {
            Logger.LogInformation($"Shadps4Install: {pkgPath}");
            Logger.LogInformation($"Shadps4Install: target = {Path.Combine(libraryDir, titleId)} (replace={replaceExisting}, merge={mergeIntoExisting})");
            if (string.IsNullOrWhiteSpace(pkgPath) || !File.Exists(pkgPath))
                return Fail(Shadps4InstallStatus.PkgMissing, $"PKG not found: {pkgPath}");
            if (string.IsNullOrWhiteSpace(libraryDir) || !Directory.Exists(libraryDir))
                return Fail(Shadps4InstallStatus.LibraryMissing, $"shadPS4 library not found: {libraryDir}");

            string finalDir = Path.Combine(libraryDir, titleId);
            if (Directory.Exists(finalDir) && !replaceExisting && !mergeIntoExisting)
                return Fail(Shadps4InstallStatus.ExistingInstall,
                    $"Game {titleId} is already installed in {libraryDir}.");

            // Free-space check BEFORE writing anything.
            long free = FreeSpaceOverride != null
                ? FreeSpaceOverride(libraryDir)
                : new DriveInfo(Path.GetPathRoot(Path.GetFullPath(libraryDir))!).AvailableFreeSpace;
            long estimate = EstimatedExtractedSize(pkgPath);
            Logger.LogInformation($"Shadps4Install: free={HelperBytes(free)}, estimate={HelperBytes(estimate)}");
            if (free < estimate + SpaceMarginBytes)
                return Fail(Shadps4InstallStatus.InsufficientSpace,
                    $"Not enough free space in {libraryDir}: needs ~{HelperBytes(estimate + SpaceMarginBytes)}, has {HelperBytes(free)}.");

            string staging = Path.Combine(libraryDir, $".ps4pkgtool-{titleId}.tmp");
            try
            {
                if (Directory.Exists(staging))
                {
                    Logger.LogWarning($"Shadps4Install: removing leftover staging {staging}");
                    Directory.Delete(staging, true); // leftovers from a previous interrupted run
                }
                Directory.CreateDirectory(staging);

                if (ct.IsCancellationRequested)
                    return Fail(Shadps4InstallStatus.Cancelled, "Installation cancelled.");

                progress?.Report("Extracting PKG...");
                Logger.LogInformation("Shadps4Install: extracting via OrbisPkgTool.PkgReader...");
                string extractDetail = "";
                bool extracted;
                if (ExtractOverride != null)
                {
                    extracted = ExtractOverride(pkgPath, staging, ct);
                }
                else
                {
                    (extracted, extractDetail) = ExtractInProcess(pkgPath, staging, ct, fileProgress);
                }
                if (!extracted)
                {
                    string detail = string.IsNullOrWhiteSpace(extractDetail)
                        ? "extraction failed (see log)."
                        : extractDetail;
                    return Fail(Shadps4InstallStatus.ExtractionFailed,
                        "PKG extraction failed: " + detail);
                }

                if (ct.IsCancellationRequested)
                    return Fail(Shadps4InstallStatus.Cancelled, "Installation cancelled.");

                // shadPS4 libraries hold DUMP-LAYOUT game folders (eboot.bin and
                // sce_sys at the folder root), not the PKG Image0/Sc0 tree the
                // extractor produces - flatten before validating/finalizing.
                progress?.Report("Arranging game files...");
                Logger.LogInformation("Shadps4Install: flattening to dump layout...");
                FlattenToDumpLayout(staging);

                progress?.Report("Validating extracted game...");
                string? eboot = Shadps4Launcher.FindEboot(staging, depth: 3);
                if (eboot == null || !Directory.Exists(Path.Combine(staging, "sce_sys")))
                    return Fail(Shadps4InstallStatus.ValidationFailed,
                        "The extracted PKG does not contain a valid game layout (eboot.bin / sce_sys missing).");
                Logger.LogInformation($"Shadps4Install: validated eboot at {eboot}");

                progress?.Report("Finalizing installation...");
                if (mergeIntoExisting && Directory.Exists(finalDir))
                {
                    // Update path: merge the patch over the existing dump.
                    Logger.LogInformation($"Shadps4Install: merging patch into {finalDir}");
                    ApplyPatchTransactionally(staging, finalDir);
                    return Success($"Updated {titleId} in {finalDir}.", finalDir);
                }
                Logger.LogInformation($"Shadps4Install: replacing existing install {finalDir}");
                ReplaceInstall(staging, finalDir);

                return Success($"Installed {titleId} into {finalDir}.", finalDir);
            }
            catch (OperationCanceledException)
            {
                Cleanup(staging);
                Logger.LogWarning("Shadps4Install: cancelled - staging cleaned.");
                return new Shadps4InstallResult(Shadps4InstallStatus.Cancelled, "Installation cancelled.");
            }
            catch (IOException ex) when ((uint)ex.HResult == 0x80070070) // ERROR_DISK_FULL
            {
                Cleanup(staging);
                Logger.LogError("Shadps4Install: disk full - " + ex.Message);
                return new Shadps4InstallResult(Shadps4InstallStatus.InsufficientSpace,
                    $"Disk full during extraction: {ex.Message}");
            }
            catch (Exception ex)
            {
                Cleanup(staging);
                Logger.LogError("Shadps4Install failed: " + ex);
                return new Shadps4InstallResult(Shadps4InstallStatus.Failed, ex.Message);
            }
            finally
            {
                // Safety net: the staging folder must never survive failure.
                if (Directory.Exists(staging))
                {
                    try { Directory.Delete(staging, true); } catch { /* best-effort: next install overwrites it */ }
                }
            }
        }

        /// <summary>
        /// Extracts a PKG to a user-chosen folder (Extract &amp; Launch flow) and
        /// returns the eboot.bin path, or null when extraction/validation failed.
        /// The game root must follow the dump layout, so the extraction is
        /// flattened before the eboot is located.
        /// </summary>
        public string? ExtractToFolder(string pkgPath, string destinationDir,
            IProgress<string>? progress = null, CancellationToken ct = default)
        {
            if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);
            progress?.Report("Extracting PKG...");
            bool ok;
            if (ExtractOverride != null)
            {
                ok = ExtractOverride(pkgPath, destinationDir, ct);
            }
            else
            {
                var (extracted, detail) = ExtractInProcess(pkgPath, destinationDir, ct);
                ok = extracted;
                if (!ok)
                    Logger.LogWarning("Shadps4Install: ExtractToFolder failed: " + detail);
            }
            if (!ok || ct.IsCancellationRequested) return null;
            FlattenToDumpLayout(destinationDir);
            return Shadps4Launcher.FindEboot(destinationDir, depth: 3);
        }

        /// <summary>
        /// Dedicated "extract as PS4 game dump" layout transformation.
        ///
        /// The raw PKG extraction tree (Image0/, Sc0/) is NOT the layout
        /// shadPS4 libraries use. The final game folder must resemble a
        /// decrypted dump from a jailbroken PS4:
        ///
        ///   CUSAxxxxx\
        ///   ├── eboot.bin
        ///   ├── sce_module\
        ///   ├── data\
        ///   └── sce_sys\          (param.sfo, icon0.png, pic0.png, ...)
        ///
        /// - Image0\ contents become the ROOT of the game folder.
        /// - Sc0\ contents are system metadata and merge into sce_sys\
        ///   (a Sc0\sce_sys\ subtree merges its contents directly).
        /// - Image0 copies win on conflicts.
        /// A no-op when the tree is already flat (a future extractor may
        /// emit the dump layout directly).
        /// </summary>
        public static void FlattenToDumpLayout(string folder)
        {
            // Shared authority for the raw -> dump mapping (Image0 wins, Sc0 ->
            // sce_sys, nested Sc0 paths preserved). keepConflicts:false so an
            // installed game never carries _flatten_conflicts diagnostics.
            PS4PKGTool.Utilities.PkgInspection.DumpLayout.Flatten(folder, keepConflicts: false);
        }

        /// <summary>
        /// Moves the source tree over an existing destination tree: same-named
        /// files are OVERWRITTEN by the source (patch wins), files only in the
        /// destination remain. Used for update/patch installs - the base-game
        /// merge inside FlattenToDumpLayout uses the opposite rule.
        /// </summary>
        private static void MergeOverwrite(string source, string dest)
        {
            foreach (string entry in Directory.GetFileSystemEntries(source))
            {
                string target = Path.Combine(dest, Path.GetFileName(entry));
                if (Directory.Exists(entry))
                {
                    if (!Directory.Exists(target)) Directory.Move(entry, target);
                    else MergeOverwrite(entry, target);
                }
                else
                {
                    File.Move(entry, target, overwrite: true);
                }
            }
            Directory.Delete(source, true);
        }

        /// <summary>
        /// Replaces <paramref name="finalDir"/> with <paramref name="staging"/>
        /// using two cheap directory renames. The previous install is renamed
        /// aside first and restored if the swap fails, so a locked or partially
        /// written staging tree can never destroy a working installation.
        /// </summary>
        private static void ReplaceInstall(string staging, string finalDir)
        {
            string? backup = null;
            if (Directory.Exists(finalDir))
            {
                backup = finalDir + $".previous-{Guid.NewGuid():N}";
                Logger.LogInformation($"Shadps4Install: moving existing install aside to {backup}");
                Directory.Move(finalDir, backup);
            }
            try
            {
                Directory.Move(staging, finalDir);
            }
            catch
            {
                if (backup != null && !Directory.Exists(finalDir) && Directory.Exists(backup))
                {
                    Logger.LogWarning("Shadps4Install: swap failed - restoring previous install");
                    Directory.Move(backup, finalDir);
                }
                throw;
            }
            if (backup != null) Cleanup(backup);
        }

        /// <summary>
        /// Applies a patch over an existing install. Files the patch overwrites
        /// are snapshotted first and entries the patch adds are journaled, so a
        /// failure midway restores the previous install instead of leaving it
        /// half-updated. Disk cost is proportional to the patch size, not the
        /// size of the installed game.
        /// </summary>
        private static void ApplyPatchTransactionally(string staging, string finalDir)
        {
            string backupRoot = finalDir + $".backup-{Guid.NewGuid():N}";
            var overwritten = new List<(string Backup, string Original)>();
            var created = new List<string>();
            try
            {
                MergeOverwriteWithRollback(staging, finalDir, finalDir, backupRoot, overwritten, created);
            }
            catch
            {
                RollbackPatch(overwritten, created);
                throw;
            }
            finally
            {
                Cleanup(backupRoot);
            }
        }

        private static void MergeOverwriteWithRollback(string source, string dest, string destRoot,
            string backupRoot, List<(string Backup, string Original)> overwritten, List<string> created)
        {
            foreach (string entry in Directory.GetFileSystemEntries(source))
            {
                string target = Path.Combine(dest, Path.GetFileName(entry));
                if (Directory.Exists(entry))
                {
                    if (!Directory.Exists(target))
                    {
                        Directory.Move(entry, target);
                        created.Add(target);
                    }
                    else
                    {
                        MergeOverwriteWithRollback(entry, target, destRoot, backupRoot, overwritten, created);
                    }
                }
                else
                {
                    if (File.Exists(target))
                    {
                        string relative = Path.GetRelativePath(destRoot, target);
                        string backupPath = Path.Combine(backupRoot, relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                        File.Copy(target, backupPath, overwrite: true);
                        overwritten.Add((backupPath, target));
                    }
                    else
                    {
                        created.Add(target);
                    }
                    File.Move(entry, target, overwrite: true);
                }
            }
        }

        private static void RollbackPatch(List<(string Backup, string Original)> overwritten, List<string> created)
        {
            // Remove what the patch added first, then restore overwritten files.
            foreach (string path in created)
            {
                try
                {
                    if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
                    else if (File.Exists(path)) File.Delete(path);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Shadps4Install: rollback could not remove {path}: {ex.Message}");
                }
            }
            foreach ((string backup, string original) in overwritten)
            {
                try
                {
                    if (File.Exists(backup))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(original)!);
                        File.Copy(backup, original, overwrite: true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Shadps4Install: rollback could not restore {original}: {ex.Message}");
                }
            }
        }

        /// <summary>Available free space on the volume that hosts the given directory.</summary>
        public static long FreeSpace(string directory)
            => new DriveInfo(Path.GetPathRoot(Path.GetFullPath(directory))!).AvailableFreeSpace;

        /// <summary>Estimated extracted size: 1.5x the PKG size (very rough lower bound).</summary>
        public static long EstimatedExtractedSize(string pkgPath)
        {
            try
            {
                var fi = new FileInfo(pkgPath);
                return (long)(fi.Length * 1.5);
            }
            catch
            {
                return 20L * 1024 * 1024 * 1024;
            }
        }

        private Shadps4InstallResult Fail(Shadps4InstallStatus status, string message)
        {
            Logger.LogWarning($"Shadps4Install: {message}");
            return new Shadps4InstallResult(status, message);
        }

        private static Shadps4InstallResult Success(string message, string finalDir)
        {
            Logger.LogInformation($"Shadps4Install: {message}");
            return new Shadps4InstallResult(Shadps4InstallStatus.Success, message, finalDir);
        }

        /// <summary>
        /// Real extraction: OrbisPkgTool.PkgReader in-process (the same
        /// pipeline as PkgExtractionService). The PKG is opened read-only -
        /// never renamed, never moved - and every Sc0 + Image0 entry is
        /// decrypted straight into the staging folder. Unicode paths work.
        /// Cancellation throws OperationCanceledException (caught by Install).
        /// </summary>
        private (bool Ok, string Detail) ExtractInProcess(
            string pkgPath, string destinationDir, CancellationToken ct,
            IProgress<(int Current, int Total, string CurrentFile)>? fileProgress = null)
        {
            try
            {
                using var reader = new OrbisPkgTool.PkgReader(pkgPath, Passcode);
                var failures = reader.ExtractAll(destinationDir, fileProgress,
                    new OrbisPkgTool.ExtractAllOptions { CancellationToken = ct });
                if (failures.Count > 0)
                {
                    string summary = string.Join("; ",
                        failures.Take(5).Select(f => $"{f.Path}: {f.Exception.Message}"));
                    Logger.LogWarning($"Shadps4Install: {failures.Count} entries failed: {summary}");
                    return (false, $"{failures.Count} {(failures.Count == 1 ? "entry" : "entries")} failed to extract: {summary}");
                }
                Logger.LogInformation("Shadps4Install: in-process extraction completed.");
                return (true, "");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                // "Passcode mismatch." lands here with 'passcode' in the
                // message - the shell flow keys its re-prompt on exactly that.
                Logger.LogError("Shadps4Install: extraction threw: " + ex);
                return (false, ex.Message);
            }
        }

        private static void Cleanup(string dir)
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { /* best-effort: next install overwrites it */ }
        }

        private static string HelperBytes(long bytes)
            => bytes >= 1024 * 1024 * 1024 ? $"{bytes / 1024.0 / 1024.0 / 1024.0:0.0} GB" : $"{bytes / 1024.0 / 1024.0:0.0} MB";
    }

    /// <summary>
    /// Minimal PSF (param.sfo) reader for TITLE and APP_VER, used to show the
    /// installed version and to pre-fill the feedback form from the game's
    /// own param.sfo. Deterministic format: header (magic/version/key/data
    /// offsets/count) + entry table.
    /// </summary>
    public static class ParamSfoReader
    {
        private const int HeaderSize = 20;

        /// <summary>Legacy: APP_VER only; null when absent or unreadable.</summary>
        public static string? ReadAppVersion(string paramSfoPath)
        {
            var info = ReadGameInfo(paramSfoPath);
            return string.IsNullOrEmpty(info?.AppVersion) ? null : info.Value.AppVersion;
        }

        /// <summary>Reads TITLE and APP_VER from a game's param.sfo; null when unreadable.</summary>
        public static (string Title, string AppVersion)? ReadGameInfo(string paramSfoPath)
        {
            try
            {
                var data = File.ReadAllBytes(paramSfoPath);
                if (data.Length < HeaderSize) return null;
                if (data[0] != 0x00 || data[1] != 0x50 || data[2] != 0x53 || data[3] != 0x46) return null; // "\0PSF"

                uint keyOffset = BitConverter.ToUInt32(data, 8);
                uint dataOffset = BitConverter.ToUInt32(data, 12);
                int count = BitConverter.ToInt32(data, 16);
                if (count <= 0 || count > 1000) return null;

                string title = "", appVersion = "";
                // Real layout: header (20) -> entry table -> key table (at
                // the keyOffset field) -> data table (at the dataOffset
                // field). Entry offsets are relative to their table starts.
                int entryBase = HeaderSize;
                for (int i = 0; i < count; i++)
                {
                    int entry = entryBase + i * 16;
                    if (entry + 16 > data.Length) break;
                    ushort keyOff = BitConverter.ToUInt16(data, entry);
                    ushort fmt = BitConverter.ToUInt16(data, entry + 2);
                    int length = BitConverter.ToInt32(data, entry + 4);
                    int valueOff = BitConverter.ToInt32(data, entry + 12);

                    int keyStart = (int)keyOffset + keyOff;
                    int keyEnd = keyStart;
                    while (keyEnd < data.Length && data[keyEnd] != 0) keyEnd++;
                    string key = Encoding.ASCII.GetString(data, keyStart, keyEnd - keyStart);

                    if (fmt != 0x0204) continue; // PSF string
                    if (length <= 0 || dataOffset + valueOff + length > data.Length) return null;
                    string value = Encoding.UTF8.GetString(data, (int)dataOffset + valueOff, length).TrimEnd('\0');

                    if (key == "APP_VER") appVersion = value;
                    else if (key == "TITLE") title = value;
                }
                return (title, appVersion);
            }
            catch
            {
                return null;
            }
        }
    }
}
