#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PS4PKGTool.Utilities.TrophyMetadata
{
    public sealed class NpCommunicationIdResolver
    {
        private static readonly Regex IdRegex = new(@"\bNPWR\d{5}_\d{2}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly string[] CandidateNames = { "npbind.dat", "nptitle.dat", "param.sfo" };

        public bool IsValid(string? value) => value != null && IdRegex.IsMatch(value) && IdRegex.Match(value).Value.Length == value.Length;

        public string? Resolve(string? explicitValue, string? trpPath, IEnumerable<string>? additionalPaths = null)
        {
            if (!string.IsNullOrWhiteSpace(explicitValue))
            {
                string normalized = explicitValue.Trim().ToUpperInvariant();
                if (!IsValid(normalized))
                    throw new ArgumentException("NP Communication ID must match NPWRxxxxx_00.", nameof(explicitValue));
                return normalized;
            }

            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (additionalPaths != null)
                foreach (string path in additionalPaths)
                    AddCandidate(candidates, path);

            string? directory = string.IsNullOrWhiteSpace(trpPath) ? null : Path.GetDirectoryName(Path.GetFullPath(trpPath));
            for (int depth = 0; directory != null && depth < 5; depth++, directory = Directory.GetParent(directory)?.FullName)
            {
                foreach (string name in CandidateNames)
                    AddCandidate(candidates, Path.Combine(directory, name));
            }

            string? found = null;
            foreach (string path in candidates)
            {
                string? candidate = ResolveFromFile(path);
                if (candidate == null) continue;
                if (found != null && !string.Equals(found, candidate, StringComparison.Ordinal))
                    throw new InvalidDataException($"Conflicting NP Communication IDs were found: {found} and {candidate}.");
                found = candidate;
            }
            return found;
        }

        public string? ResolveFromFile(string path)
        {
            if (!File.Exists(path)) return null;
            var info = new FileInfo(path);
            if (info.Length > 16 * 1024 * 1024)
                return null;
            byte[] bytes = File.ReadAllBytes(path);
            return ResolveFromBytes(bytes);
        }

        /// <summary>Scans raw npbind.dat / nptitle.dat / param.sfo bytes for
        /// the NPWRxxxxx_00 token (no temp file needed).</summary>
        public string? ResolveFromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;
            string? ascii = FindId(Encoding.ASCII.GetString(bytes));
            string? unicode = bytes.Length >= 2 ? FindId(Encoding.Unicode.GetString(bytes)) : null;
            if (ascii != null && unicode != null && ascii != unicode)
                throw new InvalidDataException($"Conflicting NP Communication IDs were found inside the data.");
            return ascii ?? unicode;
        }

        private static string? FindId(string text)
        {
            MatchCollection matches = IdRegex.Matches(text);
            if (matches.Count == 0) return null;
            string first = matches[0].Value;
            for (int index = 1; index < matches.Count; index++)
                if (!string.Equals(first, matches[index].Value, StringComparison.Ordinal))
                    throw new InvalidDataException("More than one NP Communication ID was found in a candidate file.");
            return first;
        }

        private static void AddCandidate(HashSet<string> candidates, string? path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                candidates.Add(Path.GetFullPath(path));
        }
    }
}
