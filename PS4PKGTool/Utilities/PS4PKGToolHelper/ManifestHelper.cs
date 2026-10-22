using Newtonsoft.Json;
using PS4PKGTool.Util.Constants;
using PS4PKGTool.Utilities.PkgMeta;
using PS4PKGTool.Utilities.Settings;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace PS4PKGTool.Utilities.PS4PKGToolHelper
{
    public class ManifestEntry
    {
        public string FilePath { get; set; }
        public string Filename { get; set; }
        public string Title { get; set; }
        public string TitleId { get; set; }
        public string ContentId { get; set; }
        public string Region { get; set; }
        public string SystemVersion { get; set; }
        public string Version { get; set; }
        public string PkgType { get; set; }
        public string Category { get; set; }
        public string Size { get; set; }
        public string Psvr { get; set; }
        public string Ps4ProEnhanced { get; set; }
        public string Ps5Bc { get; set; }
        public string Directory { get; set; }
        public string Backported { get; set; }
        public string LatestUpdate { get; set; }
        public DateTime FileLastWriteTimeUtc { get; set; }
    }

    public class ManifestData
    {
        public int SchemaVersion { get; set; } = 1;
        public DateTime CreatedAt { get; set; }
        public List<string> PkgDirectories { get; set; }
        public bool ScanRecursive { get; set; }
        public List<ManifestEntry> Entries { get; set; }
    }

    public static class ManifestHelper
    {
        public static string ManifestFilePath =>
            Path.Combine(Helper.UserDataDirectory, "manifest.json");

        public static bool ManifestExists() => File.Exists(ManifestFilePath);

        public static ManifestData LoadManifest()
        {
            return LoadManifest(ManifestFilePath);
        }

        public static ManifestData LoadManifest(string manifestFilePath)
        {
            try
            {
                string json = File.ReadAllText(manifestFilePath);
                var manifest = JsonConvert.DeserializeObject<ManifestData>(json);
                return manifest;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load manifest: {ex.Message}");
                return null;
            }
        }

        public static (bool IsValid, string Reason) ValidateManifest(ManifestData manifest, AppSettings currentSettings)
        {
            if (manifest == null)
                return (false, "Manifest is null.");

            if (manifest.SchemaVersion != 1)
                return (false, $"Unsupported manifest schema version: {manifest.SchemaVersion}.");

            if (manifest.PkgDirectories == null || manifest.PkgDirectories.Count == 0)
                return (false, "Manifest contains no PKG directories.");

            if (currentSettings.PkgDirectories == null || currentSettings.PkgDirectories.Count == 0)
                return (false, "No PKG directories configured in settings.");

            var manifestDirs = new HashSet<string>(
                manifest.PkgDirectories.Where(d => !string.IsNullOrEmpty(d)),
                StringComparer.OrdinalIgnoreCase);
            var settingsDirs = new HashSet<string>(
                currentSettings.PkgDirectories.Where(d => !string.IsNullOrEmpty(d)),
                StringComparer.OrdinalIgnoreCase);

            if (!manifestDirs.SetEquals(settingsDirs))
                return (false, "PKG directories have changed since the manifest was created.");

            if (manifest.ScanRecursive != currentSettings.ScanRecursive)
                return (false, "Scan recursive setting has changed.");

            return (true, string.Empty);
        }

        public static (List<ManifestEntry> Valid, int Removed) FilterValidEntries(List<ManifestEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return (new List<ManifestEntry>(), 0);

            var valid = new List<ManifestEntry>();
            int removed = 0;

            foreach (var entry in entries)
            {
                string fullPath = string.IsNullOrEmpty(entry.FilePath)
                    ? Path.Combine(entry.Directory ?? "", entry.Filename ?? "")
                    : entry.FilePath;

                if (!File.Exists(fullPath))
                {
                    removed++;
                    continue;
                }

                if (File.GetLastWriteTimeUtc(fullPath) > entry.FileLastWriteTimeUtc.AddSeconds(2))
                {
                    removed++;
                    continue;
                }

                valid.Add(entry);
            }

            if (removed > 0)
                Logger.LogWarning($"{removed} PKG entries removed from manifest (file missing or modified).");

            return (valid, removed);
        }

        public static DataTable BuildDataTableFromManifest(List<ManifestEntry> entries)
        {
            var dt = PkgColumns.CreateSchema();

            if (entries == null || entries.Count == 0)
                return dt;

            foreach (var entry in entries)
            {
                byte[] regionIcon = GetRegionIconBytes(entry.Region);

                var row = dt.NewRow();
                row[PkgColumns.Filename] = entry.Filename ?? "";
                row[PkgColumns.Title] = entry.Title ?? "";
                row[PkgColumns.TitleId] = entry.TitleId ?? "";
                row[PkgColumns.ContentId] = entry.ContentId ?? "";
                row[PkgColumns.Region] = regionIcon;
                row[PkgColumns.SystemVersion] = PkgSystemVersion.Format(entry.SystemVersion);
                row[PkgColumns.AppVersion] = entry.Version ?? "";
                row[PkgColumns.PkgType] = entry.PkgType ?? "";
                row[PkgColumns.Category] = entry.Category ?? "";
                row[PkgColumns.Size] = entry.Size ?? "";
                row[PkgColumns.Psvr] = entry.Psvr ?? "";
                row[PkgColumns.Ps4ProEnhanced] = entry.Ps4ProEnhanced ?? "";
                row[PkgColumns.Ps5Bc] = entry.Ps5Bc ?? "";
                row[PkgColumns.Directory] = entry.Directory ?? "";
                row[PkgColumns.Backported] = entry.Backported ?? "";
                row[PkgColumns.LatestUpdate] = entry.LatestUpdate ?? "";
                row[PkgColumns.Shadps4] = "";
                row[PkgColumns.RegionName] = entry.Region ?? "";
                row[PkgColumns.SystemVersionNum] = PkgColumns.ParseSystemVersionNum(entry.SystemVersion);
                dt.Rows.Add(row);
            }

            return dt;
        }

        public static List<string> BuildPkgPathList(List<ManifestEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return new List<string>();

            return entries
                .Select(e => string.IsNullOrEmpty(e.FilePath)
                    ? Path.Combine(e.Directory ?? "", e.Filename ?? "")
                    : e.FilePath)
                .ToList();
        }

        public static void SaveManifest(DataTable gridViewData, List<string> verifiedPkgList)
        {
            SaveManifest(gridViewData, verifiedPkgList, ManifestFilePath);
        }

        public static void SaveManifest(DataTable gridViewData, List<string> verifiedPkgList, string manifestFilePath)
        {
            if (gridViewData == null || gridViewData.Rows.Count == 0)
                return;

            var entries = new List<ManifestEntry>();

            foreach (DataRow row in gridViewData.Rows)
            {
                string filename = row["Filename"]?.ToString() ?? "";
                string directory = row["Directory"]?.ToString() ?? "";
                string fullPath = Path.Combine(directory, filename);

                string region = SafeCell(row, PkgColumns.RegionName);
                if (string.IsNullOrEmpty(region))
                    region = GetRegionStringFromIcon(row["Region"] as byte[]);

                DateTime lastWriteTime = File.Exists(fullPath)
                    ? File.GetLastWriteTimeUtc(fullPath)
                    : DateTime.UtcNow;

                entries.Add(new ManifestEntry
                {
                    FilePath = fullPath,
                    Filename = filename,
                    Title = row["Title"]?.ToString() ?? "",
                    TitleId = row["Title ID"]?.ToString() ?? "",
                    ContentId = row["Content ID"]?.ToString() ?? "",
                    Region = region,
                    SystemVersion = row[PkgColumns.SystemVersion]?.ToString() ?? "",
                    Version = row["Version [App Version]"]?.ToString() ?? "",
                    PkgType = row["PKG Type"]?.ToString() ?? "",
                    Category = row["Category"]?.ToString() ?? "",
                    Size = row["Size"]?.ToString() ?? "",
                    Psvr = row["PSVR"]?.ToString() ?? "",
                    Ps4ProEnhanced = row["PS4 Pro Enhanced"]?.ToString() ?? "",
                    Ps5Bc = row["PS5 BC"]?.ToString() ?? "",
                    Directory = directory,
                    Backported = row["Backported"]?.ToString() ?? "",
                    LatestUpdate = SafeCell(row, "Latest Update"),
                    FileLastWriteTimeUtc = lastWriteTime
                });
            }

            var manifest = new ManifestData
            {
                SchemaVersion = 1,
                CreatedAt = DateTime.Now,
                PkgDirectories = SettingsManager.appSettings_?.PkgDirectories
                    ?.Where(d => !string.IsNullOrEmpty(d)).ToList() ?? new List<string>(),
                ScanRecursive = SettingsManager.appSettings_?.ScanRecursive ?? false,
                Entries = entries
            };

            string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            string manifestDirectory = Path.GetDirectoryName(manifestFilePath);
            if (!string.IsNullOrEmpty(manifestDirectory))
                Directory.CreateDirectory(manifestDirectory);

            File.WriteAllText(manifestFilePath, json);
            Logger.LogInformation($"Manifest saved: {entries.Count} entries to {manifestFilePath}");
        }

        public static void DeleteManifest()
        {
            if (File.Exists(ManifestFilePath))
            {
                File.Delete(ManifestFilePath);
                Logger.LogInformation("Manifest deleted.");
            }
        }

        private static string SafeCell(DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName)
                ? (row[columnName]?.ToString() ?? "")
                : "";
        }

        public static byte[] GetRegionIconBytes(string region)
        {
            if (string.IsNullOrEmpty(region))
                return null;

            return region switch
            {
                PKGRegion.EU => Properties.Resources.eu,
                PKGRegion.US => Properties.Resources.us,
                PKGRegion.UK => Properties.Resources.us,
                PKGRegion.JAPAN => Properties.Resources.jp,
                PKGRegion.HONG_KONG => Properties.Resources.hk,
                PKGRegion.ASIA => Properties.Resources.asia,
                PKGRegion.KOREA => Properties.Resources.kr,
                _ => null
            };
        }

        public static string GetRegionStringFromIcon(byte[] iconBytes)
        {
            if (iconBytes == null || iconBytes.Length == 0)
                return "";

            var knownRegions = new (string RegionName, byte[] Icon)[]
            {
                (PKGRegion.EU, Properties.Resources.eu),
                (PKGRegion.US, Properties.Resources.us),
                (PKGRegion.JAPAN, Properties.Resources.jp),
                (PKGRegion.HONG_KONG, Properties.Resources.hk),
                (PKGRegion.ASIA, Properties.Resources.asia),
                (PKGRegion.KOREA, Properties.Resources.kr),
            };

            foreach (var (regionName, knownIcon) in knownRegions)
            {
                if (ByteArraysEqual(iconBytes, knownIcon))
                    return regionName;
            }

            return "";
        }

        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
