using System;
using System.IO;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>
    /// Filesystem mutation boundary for save backup/restore. The store drives
    /// every destructive operation through this seam so tests can fail any
    /// single operation and verify the transactional invariants.
    /// </summary>
    public interface IFileSystemOps
    {
        bool DirectoryExists(string path);
        bool FileExists(string path);
        /// <summary>Recursive copy. Reparse points (links, junction mounts) are skipped, never traversed.</summary>
        void CopyDirectory(string source, string destination);
        /// <summary>Directory rename. Callers guarantee same volume (staging lives beside the target).</summary>
        void MoveDirectory(string source, string destination);
        /// <summary>Recursive delete.</summary>
        void DeleteDirectory(string path);
        /// <summary>Free bytes on the volume containing the path.</summary>
        long GetFreeSpace(string path);
        /// <summary>Recursive size; unreadable files are skipped.</summary>
        long GetDirectorySize(string path);
    }

    /// <summary>The real filesystem.</summary>
    public sealed class RealFileSystemOps : IFileSystemOps
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);
        public bool FileExists(string path) => File.Exists(path);

        public void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string entry in Directory.GetFileSystemEntries(source))
            {
                if ((File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0)
                    continue; // never traverse external reparse targets
                if (Directory.Exists(entry))
                    CopyDirectory(entry, Path.Combine(destination, Path.GetFileName(entry)));
                else
                    File.Copy(entry, Path.Combine(destination, Path.GetFileName(entry)), false);
            }
        }

        public void MoveDirectory(string source, string destination)
            => Directory.Move(source, destination);

        public void DeleteDirectory(string path)
            => Directory.Delete(path, true);

        public long GetFreeSpace(string path)
        {
            string root = Path.GetPathRoot(Path.GetFullPath(path)) ?? "";
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return 0;
            return new DriveInfo(root).AvailableFreeSpace;
        }

        public long GetDirectorySize(string path)
        {
            long total = 0;
            foreach (string f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { total += new FileInfo(f).Length; } catch { /* best-effort: file vanished mid-scan */ }
            }
            return total;
        }
    }
}
