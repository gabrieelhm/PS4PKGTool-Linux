#nullable enable
using PS4PKGTool.Utilities.TrophyMetadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace TRPViewer
{
    // Compatibility facade retained for the existing UI and TRP creator.
    // Binary parsing and bounds validation live in the maintained TrpReader service.
    public class TRPReader
    {
        private readonly TrpReader _reader = new();
        private TrpArchive? _archive;
        private List<Archiver> _trophyList = new();
        private bool _readBytes;
        private bool _throwError = true;
        private bool _isError;
        private string _error = string.Empty;
        private string? _calculatedSha1;

        public void Load(string filename)
        {
            try
            {
                _isError = false;
                _error = string.Empty;
                _archive = _reader.Read(filename);
                _trophyList = _archive.Entries.Select(entry => new Archiver(
                    entry.Index,
                    entry.Name,
                    entry.Offset,
                    entry.Size,
                    _readBytes ? _reader.ReadEntry(_archive, entry) : null)).ToList();
                _calculatedSha1 = Version > 1 ? CalculateSHA1Hash() : null;
            }
            catch (Exception ex)
            {
                _isError = true;
                _error = ex.Message;
                _archive = null;
                _trophyList = new List<Archiver>();
                if (_throwError) throw;
            }
        }

        public bool ReadBytes { get => _readBytes; set => _readBytes = value; }
        public List<Archiver> TrophyList => _trophyList;
        public int FileSize => checked((int)(_archive?.DeclaredFileSize ?? 0));
        public int FileCount => _trophyList.Count;
        public int Version => checked((int)(_archive?.Version ?? 0));
        public string? SHA1 => _archive == null || _archive.Sha1.Length == 0 ? null : Convert.ToHexString(_archive.Sha1);
        public string? CalculatedSHA1 => _calculatedSha1;
        public bool IsError => _isError;
        public string Error => _error;
        public bool ThrowError { get => _throwError; set => _throwError = value; }
        public string? TitleName { get; set; }
        public string? NPCommId { get; set; }

        public void Extract(string outputPath)
        {
            EnsureLoaded();
            Directory.CreateDirectory(outputPath);
            foreach (TrpEntry entry in _archive!.Entries)
            {
                string target = SafeOutputPath(outputPath, entry.Name);
                using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None);
                _reader.CopyEntryTo(_archive, entry, output);
            }
        }

        public void ExtractFile(string filename, string outputPath, string? customename = null)
        {
            TrpEntry? entry = Find(filename);
            if (entry == null) return;
            Directory.CreateDirectory(outputPath);
            string target = SafeOutputPath(outputPath, customename ?? entry.Name);
            using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None);
            _reader.CopyEntryTo(_archive!, entry, output);
        }

        public void ExtractFileToMemory(string filename, ref byte[] outputBytes)
        {
            byte[]? bytes = ExtractFileToMemory(filename);
            if (bytes != null) outputBytes = bytes;
        }

        public byte[]? ExtractFileToMemory(string filename)
        {
            TrpEntry? entry = Find(filename);
            return entry == null ? null : _reader.ReadEntry(_archive!, entry);
        }

        public string? CalculateSHA1Hash()
        {
            if (_archive == null || Version <= 1) return null;
            using var stream = new FileStream(_archive.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
            byte[] prefix = new byte[28];
            stream.ReadExactly(prefix);
            sha1.AppendData(prefix);
            sha1.AppendData(new byte[20]);
            stream.Position = 48;
            byte[] buffer = new byte[81920];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                sha1.AppendData(buffer, 0, read);
            return Convert.ToHexString(sha1.GetHashAndReset());
        }

        private TrpEntry? Find(string filename)
        {
            EnsureLoaded();
            return _archive!.Entries.FirstOrDefault(entry => entry.Name.Equals(filename, StringComparison.OrdinalIgnoreCase));
        }

        private void EnsureLoaded()
        {
            if (_archive == null) throw new InvalidOperationException("No TRP file is loaded.");
        }

        private static string SafeOutputPath(string directory, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name))
                throw new InvalidDataException($"TRP entry name '{name}' is not a safe file name.");
            string root = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
            string target = Path.GetFullPath(Path.Combine(root, name));
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"TRP entry name '{name}' escapes the extraction directory.");
            return target;
        }

        public struct TRPHeader
        {
            public byte[] magic;
            public byte[] version;
            public byte[] file_size;
            public byte[] files_count;
            public byte[] element_size;
            public byte[] dev_flag;
            public byte[] sha1;
            public byte[] padding;
        }
    }
}
