using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace PS4PKGTool.Utilities.Shadps4
{
    public static class SafeZipExtractor
    {
        private const uint FileTypeMask = 0xF000;
        private const uint SymlinkType = 0xA000;
        private const uint SocketType = 0xC000;
        private const uint CharDeviceType = 0x2000;
        private const uint BlockDeviceType = 0x6000;
        private const uint FifoType = 0x1000;

        public static void Extract(Stream zipStream, string stagingRoot)
        {
            string root = Path.GetFullPath(stagingRoot);
            Directory.CreateDirectory(root);

            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
            var seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in archive.Entries)
            {
                string entryName = entry.FullName.Replace('\\', '/');
                if (IsSuspiciousEntry(entryName, entry))
                    throw new InvalidDataException(
                        $"Archive entry rejected as unsafe: {entry.FullName}");

                string target = Path.GetFullPath(Path.Combine(root, entryName));
                if (!IsWithin(target, root))
                    throw new InvalidDataException(
                        $"Archive entry escapes the extraction folder: {entry.FullName}");
                if (!seenTargets.Add(target))
                    throw new InvalidDataException(
                        $"Archive contains duplicate output path: {entry.FullName}");

                if (entryName.EndsWith("/") || string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(target);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                using (var input = entry.Open())
                using (var output = File.Create(target))
                {
                    input.CopyTo(output);
                }

                if (!OperatingSystem.IsWindows())
                {
                    TryApplyUnixExecutableBit(target, entry);
                }
            }
        }

        private static void TryApplyUnixExecutableBit(string target, ZipArchiveEntry entry)
        {
            try
            {
                uint mode = (uint)(entry.ExternalAttributes >> 16);
                bool hasExecBit = (mode & 0x49) != 0; // 0111 octal
                bool isExecName = target.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase)
                    || target.EndsWith(".sh", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFileName(target), "shadps4", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFileName(target), "shadPS4QtLauncher", StringComparison.OrdinalIgnoreCase);

                bool isElf = false;
                if (!hasExecBit && !isExecName)
                {
                    using var fs = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (fs.Length >= 4)
                    {
                        Span<byte> magic = stackalloc byte[4];
                        if (fs.Read(magic) == 4 && magic[0] == 0x7F && magic[1] == (byte)'E' && magic[2] == (byte)'L' && magic[3] == (byte)'F')
                            isElf = true;
                    }
                }

                if (hasExecBit || isExecName || isElf)
                {
                    var current = File.GetUnixFileMode(target);
                    File.SetUnixFileMode(target, current | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
                }
            }
            catch
            {
                // Best-effort permission setting
            }
        }

        public static bool IsSuspiciousEntry(string entryName, ZipArchiveEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entryName)) return false;

            if (entryName.Contains("..")) return true;
            if (entryName.Length >= 2 && entryName[1] == ':') return true;
            if (entryName.StartsWith("//") || entryName.StartsWith(@"\\")) return true;
            if (entryName.StartsWith("/")) return true;

            uint mode = (uint)(entry.ExternalAttributes >> 16);
            uint type = mode & FileTypeMask;
            if (type != 0)
            {
                if (type == SymlinkType || type == SocketType
                    || type == CharDeviceType || type == BlockDeviceType || type == FifoType)
                    return true;
            }
            return false;
        }

        public static bool IsWithin(string child, string root)
        {
            string c = Path.GetFullPath(child);
            string r = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            return string.Equals(c, r, StringComparison.OrdinalIgnoreCase)
                || c.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        public static long TotalUncompressedSize(Stream zipStream)
        {
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
            long total = 0;
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                total += entry.Length;
            }
            return total;
        }
    }
}
