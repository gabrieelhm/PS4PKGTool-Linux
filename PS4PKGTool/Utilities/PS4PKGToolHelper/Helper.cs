using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PS4PKGTool.Util;
using TRPViewer;

namespace PS4PKGTool.Utilities.PS4PKGToolHelper
{
    public class Helper
    {
        public static bool FinalizePkgProcess = true;
        public static bool LoadFromManifest { get; set; } = false;
        public static bool LaunchEmpty { get; set; } = false;
        public static bool IsOperationRunning { get; set; } = false;

        public static string UserDataDirectory { get; }
        public static string AppDataDirectory { get; }
        public static string Ps5BcJsonFile { get; }
        public static string PS4PKGToolLogFile { get; }

        static Helper()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                localAppData = Path.Combine(home, ".local", "share");
            }
            UserDataDirectory = Path.Combine(localAppData, "PS4PKGTool") + Path.DirectorySeparatorChar;
            AppDataDirectory = Path.Combine(UserDataDirectory, "AppData") + Path.DirectorySeparatorChar;
            Ps5BcJsonFile = Path.Combine(AppDataDirectory, "ps5bc.json");
            PS4PKGToolLogFile = Path.Combine(UserDataDirectory, "PS4PKGToolLog.txt");

            try
            {
                Directory.CreateDirectory(UserDataDirectory);
                Directory.CreateDirectory(AppDataDirectory);
            }
            catch
            {
                // Best-effort directory creation
            }
        }

        public static string CreateOrbisTempDir(string tag)
        {
            string root = Path.GetTempPath();
            string? configuredRoot = PS4PKGTool.Utilities.Settings.SettingsManager.appSettings_?.OrbisTempDirectory;
            if (!string.IsNullOrWhiteSpace(configuredRoot))
            {
                try
                {
                    root = Path.GetFullPath(configuredRoot);
                    Directory.CreateDirectory(root);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Configured PKG temporary directory is unavailable. Using system temporary directory: " + ex.Message);
                    root = Path.GetTempPath();
                }
            }

            string dir = Path.Combine(root, "p4t_" + tag + "_" + Guid.NewGuid().ToString("N").Substring(0, 6));
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string RoundBytes(long num)
        {
            if (num < 1024L)
                return num + " bytes";
            if (num < 1048576.0)
                return Math.Round(num / 1024.0, 2) + " KB";
            if (num < 1073741824.0)
                return Math.Round(num / 1048576.0, 2) + " MB";
            if (num < 1099511627776.0)
                return Math.Round(num / 1073741824.0, 2) + " GB";
            if (num < 1.12589990684262E+15)
                return Math.Round(num / 1099511627776.0, 2) + " TB";
            return num.ToString();
        }

        public static class Backport
        {
            public class BackportInfo
            {
                public string FilePath { get; set; } = string.Empty;
                public string Backported { get; set; } = "No";
            }

            public static string BackportInfoFile => Path.Combine(AppDataDirectory, "backport.json");

            public static Dictionary<string, string>? LoadCache()
            {
                if (!File.Exists(BackportInfoFile)) return null;
                try
                {
                    string json = File.ReadAllText(BackportInfoFile);
                    var list = JsonConvert.DeserializeObject<List<BackportInfo>>(json);
                    if (list == null) return null;
                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var b in list)
                    {
                        if (!string.IsNullOrEmpty(b.FilePath))
                            dict[b.FilePath] = b.Backported ?? "No";
                    }
                    return dict;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Error loading backport cache: " + ex.Message);
                    return null;
                }
            }

            public static void SaveCache(Dictionary<string, string> cache)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(BackportInfoFile)!);
                    var list = cache.Select(kv => new BackportInfo
                    {
                        FilePath = kv.Key,
                        Backported = kv.Value
                    }).ToList();
                    File.WriteAllText(BackportInfoFile, JsonConvert.SerializeObject(list, Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Error saving backport cache: " + ex.Message);
                }
            }

            public static void SetBackported(IEnumerable<string> filePaths, bool backported)
            {
                var cache = LoadCache() ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var fp in filePaths)
                {
                    if (!string.IsNullOrWhiteSpace(fp))
                        cache[fp] = backported ? "Yes" : "No";
                }
                SaveCache(cache);
            }
        }

        public class Bitmap
        {
            public static byte[]? Pic0Bytes { get; set; }
            public static byte[]? Pic1Bytes { get; set; }
            public static string FailExtractImageList { get; set; } = string.Empty;
        }

        public class Entry
        {
            public static byte[] Sha256(byte[] buffer, int offset, int length)
            {
                using var sha = SHA256.Create();
                return sha.ComputeHash(buffer, offset, length);
            }

            public static byte[] DecryptAes(byte[] key, byte[] iv, byte[] data)
            {
                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.Zeros;
                aes.BlockSize = 128;

                using var decryptor = aes.CreateDecryptor(key, iv);
                return decryptor.TransformFinalBlock(data, 0, data.Length);
            }

            public struct PackageEntry
            {
                public uint type;
                public uint unk1;
                public uint flags1;
                public uint flags2;
                public uint offset;
                public uint size;
                public byte[] padding;

                public uint key_index;
                public bool is_encrypted;

                public byte[] ToArray()
                {
                    var ms = new MemoryStream();
                    var writer = new EndianWriter(ms, EndianType.BigEndian);

                    writer.Write(type);
                    writer.Write(unk1);
                    writer.Write(flags1);
                    writer.Write(flags2);
                    writer.Write(offset);
                    writer.Write(size);
                    writer.Write(padding);

                    writer.Close();

                    return ms.ToArray();
                }
            }

            static readonly RSAParameters param = new RSAParameters()
            {
                D = new byte[256] {
                    0x32, 0xd9, 0x03, 0x90, 0x8f, 0xbd, 0xb0, 0x8f, 0x57, 0x2b, 0x28, 0x5e,
                    0x0b, 0x8d, 0xb3, 0xea, 0x5c, 0xd1, 0x7e, 0xa8, 0x90, 0x88, 0x8c, 0xdd,
                    0x6a, 0x80, 0xbb, 0xb1, 0xdf, 0xc1, 0xf7, 0x0d, 0xaa, 0x32, 0xf0, 0xb7,
                    0x7c, 0xcb, 0x88, 0x80, 0x0e, 0x8b, 0x64, 0xb0, 0xbe, 0x4c, 0xd6, 0x0e,
                    0x9b, 0x8c, 0x1e, 0x2a, 0x64, 0xe1, 0xf3, 0x5c, 0xd7, 0x76, 0x01, 0x41,
                    0x5e, 0x93, 0x5c, 0x94, 0xfe, 0xdd, 0x46, 0x62, 0xc3, 0x1b, 0x5a, 0xe2,
                    0xa0, 0xbc, 0x2d, 0xeb, 0xc3, 0x98, 0x0a, 0xa7, 0xb7, 0x85, 0x69, 0x70,
                    0x68, 0x2b, 0x64, 0x4a, 0xb3, 0x1f, 0xcc, 0x7d, 0xdc, 0x7c, 0x26, 0xf4,
                    0x77, 0xf6, 0x5c, 0xf2, 0xae, 0x5a, 0x44, 0x2d, 0xd3, 0xab, 0x16, 0x62,
                    0x04, 0x19, 0xba, 0xfb, 0x90, 0xff, 0xe2, 0x30, 0x50, 0x89, 0x6e, 0xcb,
                    0x56, 0xb2, 0xeb, 0xc0, 0x91, 0x16, 0x92, 0x5e, 0x30, 0x8e, 0xae, 0xc7,
                    0x94, 0x5d, 0xfd, 0x35, 0xe1, 0x20, 0xf8, 0xad, 0x3e, 0xbc, 0x08, 0xbf,
                    0xc0, 0x36, 0x74, 0x9f, 0xd5, 0xbb, 0x52, 0x08, 0xfd, 0x06, 0x66, 0xf3,
                    0x7a, 0xb3, 0x04, 0xf4, 0x75, 0x29, 0x5d, 0xe9, 0x5f, 0xaa, 0x10, 0x30,
                    0xb2, 0x0f, 0x5a, 0x1a, 0xc1, 0x2a, 0xb3, 0xfe, 0xcb, 0x21, 0xad, 0x80,
                    0xec, 0x8f, 0x20, 0x09, 0x1c, 0xdb, 0xc5, 0x58, 0x94, 0xc2, 0x9c, 0xc6,
                    0xce, 0x82, 0x65, 0x3e, 0x57, 0x90, 0xbc, 0xa9, 0x8b, 0x06, 0xb4, 0xf0,
                    0x72, 0xf6, 0x77, 0xdf, 0x98, 0x64, 0xf1, 0xec, 0xfe, 0x37, 0x2d, 0xbc,
                    0xae, 0x8c, 0x08, 0x81, 0x1f, 0xc3, 0xc9, 0x89, 0x1a, 0xc7, 0x42, 0x82,
                    0x4b, 0x2e, 0xdc, 0x8e, 0x8d, 0x73, 0xce, 0xb1, 0xcc, 0x01, 0xd9, 0x08,
                    0x70, 0x87, 0x3c, 0x44, 0x08, 0xec, 0x49, 0x8f, 0x81, 0x5a, 0xe2, 0x40,
                    0xff, 0x77, 0xfc, 0x0d
                },
                DP = new byte[128] {
                    0x52, 0xCC, 0x2D, 0xA0, 0x9C, 0x9E, 0x75, 0xE7, 0x28, 0xEE, 0x3D, 0xDE,
                    0xE3, 0x45, 0xD1, 0x4F, 0x94, 0x1C, 0xCC, 0xC8, 0x87, 0x29, 0x45, 0x3B,
                    0x8D, 0x6E, 0xAB, 0x6E, 0x2A, 0xA7, 0xC7, 0x15, 0x43, 0xA3, 0x04, 0x8F,
                    0x90, 0x5F, 0xEB, 0xF3, 0x38, 0x4A, 0x77, 0xFA, 0x36, 0xB7, 0x15, 0x76,
                    0xB6, 0x01, 0x1A, 0x8E, 0x25, 0x87, 0x82, 0xF1, 0x55, 0xD8, 0xC6, 0x43,
                    0x2A, 0xC0, 0xE5, 0x98, 0xC9, 0x32, 0xD1, 0x94, 0x6F, 0xD9, 0x01, 0xBA,
                    0x06, 0x81, 0xE0, 0x6D, 0x88, 0xF2, 0x24, 0x2A, 0x25, 0x01, 0x64, 0x5C,
                    0xBF, 0xF2, 0xD9, 0x99, 0x67, 0x3E, 0xF6, 0x72, 0xEE, 0xE4, 0xE2, 0x33,
                    0x5C, 0xF8, 0x00, 0x40, 0xE3, 0x2A, 0x9A, 0xF4, 0x3D, 0x22, 0x86, 0x44,
                    0x3C, 0xFB, 0x0A, 0xA5, 0x7C, 0x3F, 0xCC, 0xF5, 0xF1, 0x16, 0xC4, 0xAC,
                    0x88, 0xB4, 0xDE, 0x62, 0x94, 0x92, 0x6A, 0x13
                },
                DQ = new byte[128] {
                    0x7C, 0x9D, 0xAD, 0x39, 0xE0, 0xD5, 0x60, 0x14, 0x94, 0x48, 0x19, 0x7F,
                    0x88, 0x95, 0xD5, 0x8B, 0x80, 0xAD, 0x85, 0x8A, 0x4B, 0x77, 0x37, 0x85,
                    0xD0, 0x77, 0xBB, 0xBF, 0x89, 0x71, 0x4A, 0x72, 0xCB, 0x72, 0x68, 0x38,
                    0xEC, 0x02, 0xC6, 0x7D, 0xC6, 0x44, 0x06, 0x33, 0x51, 0x1C, 0xC0, 0xFF,
                    0x95, 0x8F, 0x0D, 0x75, 0xDC, 0x25, 0xBB, 0x0B, 0x73, 0x91, 0xA9, 0x6D,
                    0x42, 0xD8, 0x03, 0xB7, 0x68, 0xD4, 0x1E, 0x75, 0x62, 0xA3, 0x70, 0x35,
                    0x79, 0x78, 0x00, 0xC8, 0xF5, 0xEF, 0x15, 0xB9, 0xFC, 0x4E, 0x47, 0x5A,
                    0xC8, 0x70, 0x70, 0x5B, 0x52, 0x98, 0xC0, 0xC2, 0x58, 0x4A, 0x70, 0x96,
                    0xCC, 0xB8, 0x10, 0xE1, 0x2F, 0x78, 0x8B, 0x2B, 0xA1, 0x7F, 0xF9, 0xAC,
                    0xDE, 0xF0, 0xBB, 0x2B, 0xE2, 0x66, 0xE3, 0x22, 0x92, 0x31, 0x21, 0x57,
                    0x92, 0xC4, 0xB8, 0xF2, 0x3E, 0x76, 0x20, 0x37
                },
                Exponent = new byte[4] { 0x00, 0x01, 0x00, 0x01 },
                InverseQ = new byte[128] {
                    0x45, 0x97, 0x55, 0xD4, 0x22, 0x08, 0x5E, 0xF3, 0x5C, 0xB4, 0x05, 0x7A,
                    0xFD, 0xAA, 0x42, 0x42, 0xAD, 0x9A, 0x8C, 0xA0, 0x6C, 0xBB, 0x1D, 0x68,
                    0x54, 0x54, 0x6E, 0x3E, 0x32, 0xE3, 0x53, 0x73, 0x76, 0xF1, 0x3E, 0x01,
                    0xEA, 0xD3, 0xCF, 0xEB, 0xEB, 0x23, 0x3E, 0xC0, 0xBE, 0xCE, 0xEC, 0x2C,
                    0x89, 0x5F, 0xA8, 0x27, 0x3A, 0x4C, 0xB7, 0xE6, 0x74, 0xBC, 0x45, 0x4C,
                    0x26, 0xC8, 0x25, 0xFF, 0x34, 0x63, 0x25, 0x37, 0xE1, 0x48, 0x10, 0xC1,
                    0x93, 0xA6, 0xAF, 0xEB, 0xBA, 0xE3, 0xA2, 0xF1, 0x3D, 0xEF, 0x63, 0xD8,
                    0xF4, 0xFD, 0xD3, 0xEE, 0xE2, 0x5D, 0xE9, 0x33, 0xCC, 0xAD, 0xBA, 0x75,
                    0x5C, 0x85, 0xAF, 0xCE, 0xA9, 0x3D, 0xD1, 0xA2, 0x17, 0xF3, 0xF6, 0x98,
                    0xB3, 0x50, 0x8E, 0x5E, 0xF6, 0xEB, 0x02, 0x8E, 0xA1, 0x62, 0xA7, 0xD6,
                    0x2C, 0xEC, 0x91, 0xFF, 0x15, 0x40, 0xD2, 0xE3
                },
                Modulus = new byte[256] {
                    0xd2, 0x12, 0xfc, 0x33, 0x5f, 0x6d, 0xdb, 0x83, 0x16, 0x09, 0x62, 0x8b,
                    0x03, 0x56, 0x27, 0x37, 0x82, 0xd4, 0x77, 0x85, 0x35, 0x29, 0x39, 0x2d,
                    0x52, 0x6b, 0x8c, 0x4c, 0x8c, 0xfb, 0x06, 0xc1, 0x84, 0x5b, 0xe7, 0xd4,
                    0xf7, 0xbc, 0xd2, 0x4e, 0x62, 0x45, 0xcd, 0x2a, 0xbb, 0xd7, 0x77, 0x76,
                    0x45, 0x36, 0x55, 0x27, 0x3f, 0xb3, 0xf5, 0xf9, 0x8e, 0xda, 0x4b, 0xef,
                    0xaa, 0x59, 0xae, 0xb3, 0x9b, 0xea, 0x54, 0x98, 0xd2, 0x06, 0x32, 0x6a,
                    0x58, 0x31, 0x2a, 0xe0, 0xd4, 0x4f, 0x90, 0xb5, 0x0a, 0x7d, 0xec, 0xf4,
                    0x3a, 0x9c, 0x52, 0x67, 0x2d, 0x99, 0x31, 0x8e, 0x0c, 0x43, 0xe6, 0x82,
                    0xfe, 0x07, 0x46, 0xe1, 0x2e, 0x50, 0xd4, 0x1f, 0x2d, 0x2f, 0x7e, 0xd9,
                    0x08, 0xba, 0x06, 0xb3, 0xbf, 0x2e, 0x20, 0x3f, 0x4e, 0x3f, 0xfe, 0x44,
                    0xff, 0xaa, 0x50, 0x43, 0x57, 0x91, 0x69, 0x94, 0x49, 0x15, 0x82, 0x82,
                    0xe4, 0x0f, 0x4c, 0x8d, 0x9d, 0x2c, 0xc9, 0x5b, 0x1d, 0x64, 0xbf, 0x88,
                    0x8b, 0xd4, 0xc5, 0x94, 0xe7, 0x65, 0x47, 0x84, 0x1e, 0xe5, 0x79, 0x10,
                    0xfb, 0x98, 0x93, 0x47, 0xb9, 0x7d, 0x85, 0x12, 0xa6, 0x40, 0x98, 0x2c,
                    0xf7, 0x92, 0xbc, 0x95, 0x19, 0x32, 0xed, 0xe8, 0x90, 0x56, 0x0d, 0x65,
                    0xc1, 0xaa, 0x78, 0xc6, 0x2e, 0x54, 0xfd, 0x5f, 0x54, 0xa1, 0xf6, 0x7e,
                    0xe5, 0xe0, 0x5f, 0x61, 0xc1, 0x20, 0xb4, 0xb9, 0xb4, 0x33, 0x08, 0x70,
                    0xe4, 0xdf, 0x89, 0x56, 0xed, 0x01, 0x29, 0x46, 0x77, 0x5f, 0x8c, 0xb8,
                    0xa9, 0xf5, 0x1e, 0x2e, 0xb3, 0xb9, 0xbf, 0xe0, 0x09, 0xb7, 0x8d, 0x28,
                    0xd4, 0xa6, 0xc3, 0xb8, 0x1e, 0x1f, 0x07, 0xeb, 0xb4, 0x12, 0x0b, 0x95,
                    0xb8, 0x85, 0x30, 0xfd, 0xdc, 0x39, 0x13, 0xd0, 0x7c, 0xdc, 0x8f, 0xed,
                    0xf9, 0xc9, 0xa3, 0xc1
                },
                P = new byte[128] {
                    0xF9, 0x67, 0xAD, 0x99, 0x12, 0x31, 0x0C, 0x56, 0xA2, 0x2E, 0x16, 0x1C, 0x46, 0xB3, 0x4D, 0x5B,
                    0x43, 0xBE, 0x42, 0xA2, 0xF6, 0x86, 0x96, 0x80, 0x42, 0xC3, 0xC7, 0x3F, 0xC3, 0x42, 0xF5, 0x87,
                    0x49, 0x33, 0x9F, 0x07, 0x5D, 0x6E, 0x2C, 0x04, 0xFD, 0xE3, 0xE1, 0xB2, 0xAE, 0x0A, 0x0C, 0xF0,
                    0xC7, 0xA6, 0x1C, 0xA1, 0x63, 0x50, 0xC8, 0x09, 0x9C, 0x51, 0x24, 0x52, 0x6C, 0x5E, 0x5E, 0xBD,
                    0x1E, 0x27, 0x06, 0xBB, 0xBC, 0x9E, 0x94, 0xE1, 0x35, 0xD4, 0x6D, 0xB3, 0xCB, 0x3C, 0x68, 0xDD,
                    0x68, 0xB3, 0xFE, 0x6C, 0xCB, 0x8D, 0x82, 0x20, 0x76, 0x23, 0x63, 0xB7, 0xE9, 0x68, 0x10, 0x01,
                    0x4E, 0xDC, 0xBA, 0x27, 0x5D, 0x01, 0xC1, 0x2D, 0x80, 0x5E, 0x2B, 0xAF, 0x82, 0x6B, 0xD8, 0x84,
                    0xB6, 0x10, 0x52, 0x86, 0xA7, 0x89, 0x8E, 0xAE, 0x9A, 0xE2, 0x89, 0xC6, 0xF7, 0xD5, 0x87, 0xFB
                },
                Q = new byte[128] {
                    0xD7, 0xA1, 0x0F, 0x9A, 0x8B, 0xF2, 0xC9, 0x11, 0x95, 0x32, 0x9A, 0x8C, 0xF0, 0xD9, 0x40, 0x47,
                    0xF5, 0x68, 0xA0, 0x0D, 0xBD, 0xC1, 0xFC, 0x43, 0x2F, 0x65, 0xF9, 0xC3, 0x61, 0x0F, 0x25, 0x77,
                    0x54, 0xAD, 0xD7, 0x58, 0xAC, 0x84, 0x40, 0x60, 0x8D, 0x3F, 0xF3, 0x65, 0x89, 0x75, 0xB5, 0xC6,
                    0x2C, 0x51, 0x1A, 0x2F, 0x1F, 0x22, 0xE4, 0x43, 0x11, 0x54, 0xBE, 0xC9, 0xB4, 0xC7, 0xB5, 0x1B,
                    0x05, 0x0B, 0xBC, 0x56, 0x9A, 0xCD, 0x4A, 0xD9, 0x73, 0x68, 0x5E, 0x5C, 0xFB, 0x92, 0xB7, 0x8B,
                    0x0D, 0xFF, 0xF5, 0x07, 0xCA, 0xB4, 0xC8, 0x9B, 0x96, 0x3C, 0x07, 0x9E, 0x3E, 0x6B, 0x2A, 0x11,
                    0xF2, 0x8A, 0xB1, 0x8A, 0xD7, 0x2E, 0x1B, 0xA5, 0x53, 0x24, 0x06, 0xED, 0x50, 0xB8, 0x90, 0x67,
                    0xB1, 0xE2, 0x41, 0xC6, 0x92, 0x01, 0xEE, 0x10, 0xF0, 0x61, 0xBB, 0xFB, 0xB2, 0x7D, 0x4A, 0x73
                }
            };

            public static byte[] Decrypt(byte[] data)
            {
                using var rsa = RSA.Create(2048);
                rsa.ImportParameters(param);
                return rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
            }

            public static Dictionary<string, string> EntryIdNameDictionary = new Dictionary<string, string>();
            public static Dictionary<string, string> EncryptedEntryOffsetNameDictionary = new Dictionary<string, string>();
        }

        public class PKG
        {
            public static bool isDeletingPkg { get; set; }
            public static string Passcode { get; set; } = string.Empty;
            public static string NodeFullPath { get; set; } = string.Empty;
            public static int CountFailRename { get; set; }
            public static string ListFailRename { get; set; } = string.Empty;
            public static int CountFailMove { get; set; }
            public static string ListFailMove { get; set; } = string.Empty;
            public static string CurrentPKGTitle { get; set; } = string.Empty;
            public static string CurrentPKGType { get; set; } = string.Empty;
            public static string SelectedPKGFilename { get; set; } = string.Empty;

            public static int unknown { get; set; }
            public static int app { get; set; }
            public static int patch { get; set; }
            public static int official { get; set; }
            public static int game { get; set; }
            public static int fake { get; set; }
            public static int pkgCount { get; set; }
            public static int addon { get; set; }
            public static int unlockerAddon { get; set; }

            public static List<string> VerifiedPs4PkgList { get; set; } = new List<string>();
            public static List<string> EntryIdList { get; set; } = new List<string>();
            public static List<string> EntryNameList { get; set; } = new List<string>();

            public static byte[] PkgHeader { get; } = new byte[16] { 0x7F, 0x43, 0x4E, 0x54, 0x83, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F };
            public static byte[] PkgHeader1 { get; } = new byte[16] { 0x7F, 0x43, 0x4E, 0x54, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F };
            public static byte[] PkgHeader2 { get; } = new byte[16] { 0x7F, 0x43, 0x4E, 0x54, 0x81, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F };
            public static byte[] PkgHeader3 { get; } = new byte[16] { 0x7F, 0x43, 0x4E, 0x54, 0x40, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F };
            public static byte[] PkgHeader4 { get; } = new byte[16] { 0x7F, 0x43, 0x4E, 0x54, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C };

            public static byte[] GetPkgHeaderBuffer(string dump)
            {
                var buf = new byte[16];
                using var fs = new FileStream(dump, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fs.Seek(0x0, SeekOrigin.Begin);
                _ = fs.Read(buf, 0, 16);
                return buf;
            }

            public static bool CompareBytes(byte[] bA1, byte[] bA2)
            {
                if (bA1 == null || bA2 == null || bA1.Length != bA2.Length) return false;
                for (int z = 0; z < bA1.Length; z++)
                {
                    if (bA1[z] != bA2[z]) return false;
                }
                return true;
            }
        }

        public class BGM
        {
            private static Process? _audioProcess;
            public static bool extractAt9Done { get; set; } = true;
            public static bool isBGMPlaying { get; set; } = false;

            public static void ExtractBgm()
            {
                try
                {
                    string bgmPath = Path.Combine(AppDataDirectory, "BGM");
                    Directory.CreateDirectory(bgmPath);

                    foreach (var pkgFile in PKG.VerifiedPs4PkgList)
                    {
                        try
                        {
                            using var reader = new OrbisPkgTool.PkgReader(pkgFile);
                            var sfo = reader.ReadParamSfo();
                            string filteredTitle = (sfo?.GetString("TITLE") ?? "").SanitizeFileName();
                            string finalTitle = filteredTitle.Replace("  -", " -");
                            var fileExists = Path.Combine(bgmPath, finalTitle + ".AT9");
                            if (File.Exists(fileExists))
                                continue;
                            var snd0 = reader.Entries.FirstOrDefault(e =>
                                e.Id == OrbisPkgTool.Pkg.PkgEntryIds.Snd0At9);
                            if (snd0 == null)
                                continue;
                            byte[] at9Bytes = reader.ExtractEntryBytes("Sc0/snd0.at9");
                            string outputPath = Path.Combine(bgmPath, (finalTitle + ".AT9").SanitizeFileName());
                            File.WriteAllBytes(outputPath, at9Bytes);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning("BGM extract failed for " + pkgFile + ": " + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message);
                }
            }

            public static byte[]? DecodePkgBgmToWav(string pkgPath)
            {
                try
                {
                    using var reader = new OrbisPkgTool.PkgReader(pkgPath);
                    var snd0 = reader.Entries.FirstOrDefault(e => e.Id == OrbisPkgTool.Pkg.PkgEntryIds.Snd0At9);
                    if (snd0 == null) return null;
                    byte[] at9Bytes = reader.ExtractEntryBytes(snd0.Id);
                    if (at9Bytes == null || at9Bytes.Length == 0) return null;
                    return OrbisPkgTool.Media.At9Decoder.DecodeToWav(at9Bytes);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("DecodePkgBgmToWav failed: " + ex.Message);
                    return null;
                }
            }

            public static void StopBgm()
            {
                try
                {
                    if (_audioProcess != null && !_audioProcess.HasExited)
                    {
                        _audioProcess.Kill(entireProcessTree: true);
                        _audioProcess.Dispose();
                    }
                }
                catch { }
                finally
                {
                    _audioProcess = null;
                    isBGMPlaying = false;
                }
            }

            public static void PlayAt9(string pkg)
            {
                try
                {
                    StopBgm();
                    byte[]? wavBytes = DecodePkgBgmToWav(pkg);
                    if (wavBytes == null || wavBytes.Length == 0) return;

                    string bgmDir = Path.Combine(AppDataDirectory, "BGM");
                    Directory.CreateDirectory(bgmDir);
                    string wavPath = Path.Combine(bgmDir, "current_bgm.wav");
                    File.WriteAllBytes(wavPath, wavBytes);

                    string? player = FindLinuxAudioPlayer();
                    if (player != null)
                    {
                        _audioProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = player,
                            Arguments = $"\"{wavPath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        });
                        isBGMPlaying = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Error playing AT9 file: " + ex.Message);
                }
            }

            private static string? FindLinuxAudioPlayer()
            {
                string[] candidates = { "/usr/bin/pw-play", "/usr/bin/paplay", "/usr/bin/aplay", "/usr/bin/ffplay" };
                foreach (var c in candidates)
                {
                    if (File.Exists(c)) return c;
                }
                return null;
            }
        }

        public class NodeJsHttpServer
        {
            public static string HttpServerModulePath =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".npm-global", "lib", "node_modules", "http-server");

            public static bool IsSoftwareInstalled(string softwareName)
            {
                if (softwareName.Contains("Node", StringComparison.OrdinalIgnoreCase))
                    return CommandExists("node") || CommandExists("nodejs");
                if (softwareName.Contains("http-server", StringComparison.OrdinalIgnoreCase))
                    return CommandExists("http-server") || true; // Built-in HTTP server is always available
                return CommandExists(softwareName);
            }

            public static bool CommandExists(string command)
            {
                try
                {
                    string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "/usr/local/bin:/usr/bin:/bin";
                    foreach (var dir in pathEnv.Split(':', StringSplitOptions.RemoveEmptyEntries))
                    {
                        string full = Path.Combine(dir, command);
                        if (File.Exists(full)) return true;
                    }
                }
                catch { }
                return false;
            }
        }

        public class Tool
        {
            public static bool IsRootDrive(string path)
            {
                if (string.IsNullOrWhiteSpace(path)) return false;
                string normalized = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
                return string.IsNullOrEmpty(normalized) || normalized == "/";
            }

            public static void CreateDirectoryIfNotExists(string directory)
            {
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }

            private static readonly HttpClient _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            static Tool()
            {
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("PS4PKGTool-Linux/1.0");
            }

            public static async Task DownloadFileFromUrlAsync(string url, string saveFilePath)
            {
                HttpResponseMessage response = await _http.GetAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                string? dir = Path.GetDirectoryName(saveFilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(saveFilePath, content).ConfigureAwait(false);
            }

            public static void OpenWebLink(string url)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = $"\"{url}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("OpenWebLink failed: " + ex.Message);
                }
            }

            public static void OpenFolder(string folderPath)
            {
                if (string.IsNullOrWhiteSpace(folderPath)) return;
                string target = File.Exists(folderPath) ? (Path.GetDirectoryName(folderPath) ?? folderPath) : folderPath;
                if (!Directory.Exists(target)) return;
                OpenWebLink(target);
            }

            public static bool IsAppInstalled(string softwareName)
                => NodeJsHttpServer.IsSoftwareInstalled(softwareName);

            public static void KillNodeJS()
            {
                PKGSENDER.StopBuiltInServer();
            }

            public static bool CheckForPS4Connection(string host)
            {
                if (string.IsNullOrWhiteSpace(host)) return false;
                try
                {
                    using var ping = new Ping();
                    PingReply reply = ping.Send(host, 1500);
                    if (reply.Status == IPStatus.Success) return true;
                }
                catch { }

                try
                {
                    using var tcp = new TcpClient();
                    var task = tcp.ConnectAsync(host, 12800);
                    return task.Wait(1500) && tcp.Connected;
                }
                catch
                {
                    return false;
                }
            }

            public static bool CheckLocalServerConnection(string host)
            {
                if (string.IsNullOrWhiteSpace(host)) return false;
                if (IPAddress.TryParse(host, out _)) return true;
                try
                {
                    using var ping = new Ping();
                    PingReply reply = ping.Send(host, 1000);
                    return reply.Status == IPStatus.Success;
                }
                catch
                {
                    return false;
                }
            }

            public static bool CheckForInternetConnection(string? url = null)
            {
                try
                {
                    using var ping = new Ping();
                    string host = url ?? "github.com";
                    PingReply reply = ping.Send(host, 2000);
                    if (reply.Status == IPStatus.Success) return true;
                }
                catch { }

                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Head, "https://api.github.com");
                    using var resp = _http.Send(req);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public class PKGSENDER
        {
            public class JSON
            {
                public class STOPTASK
                {
                    public static string status { get; set; } = string.Empty;
                }

                public class SENDPKG
                {
                    public static string status { get; set; } = string.Empty;
                    public static string task_id { get; set; } = string.Empty;
                    public static string title { get; set; } = string.Empty;
                    public static string title_id { get; set; } = string.Empty;
                    public static string error { get; set; } = string.Empty;
                }

                public class UNINTSALLAPP
                {
                    public static string status { get; set; } = string.Empty;
                }

                public class UNINTSALLPATCH
                {
                    public static string status { get; set; } = string.Empty;
                }

                public class UNINTSALLADDON
                {
                    public static string status { get; set; } = string.Empty;
                }

                public class UNINTSALLTHEME
                {
                    public static string status { get; set; } = string.Empty;
                }

                public class CHECKAPPEXISTS
                {
                    public static bool baseAppExist { get; set; }
                    public static string status { get; set; } = string.Empty;
                    public static string exists { get; set; } = string.Empty;
                }

                public class MONITORTASK
                {
                    public static string packageFilesizeTotal { get; set; } = string.Empty;
                    public static string packageTransferredTotal { get; set; } = string.Empty;
                    public static string TimeRemainingTotal { get; set; } = string.Empty;
                    public static int packagePreparingTotal { get; set; }
                    public static string status { get; set; } = string.Empty;
                }
            }

            public static BackgroundWorker? MonitorPkgSenderTaskBackgroundWorker;
            public static bool taskMonitorIsCancelling { get; set; }
            public static bool isPreparing { get; set; }
            public static bool pkgSendDone { get; set; }
            public static bool pkgSendStopped { get; set; }

            private static readonly HttpClient _ps4Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            private static HttpListener? _fileListener;
            private static CancellationTokenSource? _fileServerCts;
            private static string? _servedDirectory;
            public static int ServerPort { get; set; } = 8080;

            public static string CheckRequirement(bool forSend = false)
            {
                var settings = PS4PKGTool.Utilities.Settings.SettingsManager.appSettings_;
                if (forSend && !string.IsNullOrEmpty(PKG.SelectedPKGFilename) && PKG.SelectedPKGFilename.Contains("_0.pkg"))
                    return "Splitted PKG update is not supported at this moment.";

                if (string.IsNullOrWhiteSpace(settings.Ps4Ip) || string.IsNullOrWhiteSpace(settings.LocalServerIp))
                    return "PS4 IP address or Server IP address has not been set. Set the IP address in Settings.";

                if (!Tool.CheckForPS4Connection(settings.Ps4Ip))
                    return "PS4 not detected. Make sure PS4 Remote Package Installer is running and check if the IP is correct.";

                if (!Tool.CheckLocalServerConnection(settings.LocalServerIp))
                    return "Server IP not valid. Update the server IP in program settings.";

                return "OK";
            }

            private static dynamic? PostPs4Api(string endpoint, object payload, int timeoutMs = 15000)
            {
                try
                {
                    var settings = PS4PKGTool.Utilities.Settings.SettingsManager.appSettings_;
                    string url = $"http://{settings.Ps4Ip}:12800/api/{endpoint}";
                    string jsonBody = JsonConvert.SerializeObject(payload);
                    using var cts = new CancellationTokenSource(timeoutMs);
                    using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    using var resp = _ps4Http.PostAsync(url, content, cts.Token).GetAwaiter().GetResult();
                    string output = resp.Content.ReadAsStringAsync(cts.Token).GetAwaiter().GetResult();
                    return JsonConvert.DeserializeObject(output);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"PS4 API {endpoint} failed: {ex.Message}");
                    return null;
                }
            }

            public static dynamic? CheckIfPkgInstalled(PS4PKGTool.Utilities.PkgMeta.PkgMetadata pkg)
                => PostPs4Api("is_exists", new { title_id = pkg.TITLEID }, 10000);

            public static dynamic? StopTask()
            {
                taskMonitorIsCancelling = true;
                int.TryParse(JSON.SENDPKG.task_id, out int tid);
                return PostPs4Api("stop_task", new { task_id = tid }, 10000);
            }

            public static dynamic? UninstallAddonTheme(PS4PKGTool.Utilities.PkgMeta.PkgMetadata pkg, bool isTheme = false)
                => PostPs4Api(isTheme ? "uninstall_theme" : "uninstall_ac", new { content_id = pkg.SfoContentId }, 30000);

            public static dynamic? UninstallPatch(PS4PKGTool.Utilities.PkgMeta.PkgMetadata pkg)
                => PostPs4Api("uninstall_patch", new { title_id = pkg.TITLEID }, 30000);

            public static dynamic? UninstallGame(PS4PKGTool.Utilities.PkgMeta.PkgMetadata pkg)
                => PostPs4Api("uninstall_game", new { title_id = pkg.TITLEID }, 30000);

            public static dynamic? GetTaskProgress()
            {
                int.TryParse(JSON.SENDPKG.task_id, out int tid);
                return PostPs4Api("get_task_progress", new { task_id = tid }, 10000);
            }

            public static void RunServer(string directory)
            {
                try
                {
                    StopBuiltInServer();
                    _servedDirectory = Path.GetFullPath(directory);
                    _fileServerCts = new CancellationTokenSource();
                    _fileListener = new HttpListener();
                    _fileListener.Prefixes.Add($"http://*:{ServerPort}/");
                    _fileListener.Start();
                    var token = _fileServerCts.Token;
                    _ = Task.Run(() => ServeLoopAsync(_fileListener, _servedDirectory, token), token);
                    Logger.LogInformation($"Built-in PKG HTTP server started on port {ServerPort} serving {_servedDirectory}");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"HttpListener on *:{ServerPort} failed ({ex.Message}); trying http-server fallback.");
                    try
                    {
                        if (NodeJsHttpServer.CommandExists("http-server"))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "http-server",
                                Arguments = $"\"{directory}\" -p {ServerPort}",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            });
                        }
                    }
                    catch (Exception inner)
                    {
                        Logger.LogWarning("Fallback http-server failed: " + inner.Message);
                    }
                }
            }

            public static void StopBuiltInServer()
            {
                try
                {
                    _fileServerCts?.Cancel();
                    _fileListener?.Stop();
                    _fileListener?.Close();
                }
                catch { }
                finally
                {
                    _fileListener = null;
                    _fileServerCts = null;
                }
            }

            private static async Task ServeLoopAsync(HttpListener listener, string rootDir, CancellationToken ct)
            {
                while (!ct.IsCancellationRequested && listener.IsListening)
                {
                    try
                    {
                        var ctx = await listener.GetContextAsync().ConfigureAwait(false);
                        _ = Task.Run(() => HandleFileRequestAsync(ctx, rootDir, ct), ct);
                    }
                    catch
                    {
                        if (ct.IsCancellationRequested) break;
                    }
                }
            }

            private static async Task HandleFileRequestAsync(HttpListenerContext ctx, string rootDir, CancellationToken ct)
            {
                try
                {
                    string relPath = Uri.UnescapeDataString(ctx.Request.Url?.AbsolutePath.TrimStart('/') ?? "");
                    string fullPath = Path.GetFullPath(Path.Combine(rootDir, relPath));
                    if (!fullPath.StartsWith(rootDir, StringComparison.Ordinal) || !File.Exists(fullPath))
                    {
                        ctx.Response.StatusCode = 404;
                        ctx.Response.Close();
                        return;
                    }

                    using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    long totalLength = fs.Length;
                    ctx.Response.ContentType = "application/octet-stream";
                    ctx.Response.AddHeader("Accept-Ranges", "bytes");

                    string? rangeHeader = ctx.Request.Headers["Range"];
                    long start = 0, end = totalLength - 1;
                    if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] parts = rangeHeader.Substring(6).Split('-');
                        if (parts.Length == 2)
                        {
                            if (long.TryParse(parts[0], out long s)) start = s;
                            if (!string.IsNullOrEmpty(parts[1]) && long.TryParse(parts[1], out long e)) end = e;
                        }
                        ctx.Response.StatusCode = 206;
                        ctx.Response.AddHeader("Content-Range", $"bytes {start}-{end}/{totalLength}");
                    }
                    else
                    {
                        ctx.Response.StatusCode = 200;
                    }

                    long contentLength = end - start + 1;
                    ctx.Response.ContentLength64 = contentLength;
                    if (ctx.Request.HttpMethod != "HEAD")
                    {
                        fs.Seek(start, SeekOrigin.Begin);
                        byte[] buffer = new byte[64 * 1024];
                        long remaining = contentLength;
                        while (remaining > 0 && !ct.IsCancellationRequested)
                        {
                            int toRead = (int)Math.Min(buffer.Length, remaining);
                            int read = await fs.ReadAsync(buffer, 0, toRead, ct).ConfigureAwait(false);
                            if (read <= 0) break;
                            await ctx.Response.OutputStream.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                            remaining -= read;
                        }
                    }
                    ctx.Response.Close();
                }
                catch
                {
                    try { ctx.Response.Close(); } catch { }
                }
            }

            public static dynamic? SendPKG(string tempFilename)
            {
                var settings = PS4PKGTool.Utilities.Settings.SettingsManager.appSettings_;
                string pkgUrl = $"http://{settings.LocalServerIp}:{ServerPort}/{Uri.EscapeDataString(tempFilename)}";
                return PostPs4Api("install", new { type = "direct", packages = new[] { pkgUrl } }, 15000);
            }
        }

        public class Trophy
        {
            public static TRPReader? trophy;
            public static string TrophyTempFolder => Path.Combine(AppDataDirectory, "TrophyFile") + Path.DirectorySeparatorChar;
            public static List<byte[]> ImageBytesToExtractList { get; set; } = new List<byte[]>();
            public static List<string> TrophyFilenameToExtractList { get; set; } = new List<string>();
        }
    }

    public static class PkgImageReader
    {
        public static byte[] ReadIcon0Png(string pkgPath) => ReadImageEntry(pkgPath, OrbisPkgTool.Pkg.PkgEntryIds.Icon0Png);
        public static byte[] ReadPic0Png(string pkgPath) => ReadImageEntry(pkgPath, OrbisPkgTool.Pkg.PkgEntryIds.Pic0Png);
        public static byte[] ReadPic1Png(string pkgPath) => ReadImageEntry(pkgPath, OrbisPkgTool.Pkg.PkgEntryIds.Pic1Png);

        public static byte[] ReadImageEntry(string pkgPath, uint entryId)
        {
            using var reader = new OrbisPkgTool.PkgReader(pkgPath);
            return reader.ExtractEntryBytes(entryId);
        }
    }
}
