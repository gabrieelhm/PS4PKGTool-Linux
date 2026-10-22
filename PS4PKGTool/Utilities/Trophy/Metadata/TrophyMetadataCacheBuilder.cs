#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PS4PKGTool.Utilities.PkgMeta;
using PS4PKGTool.Utilities.PS4PKGToolHelper;

namespace PS4PKGTool.Utilities.TrophyMetadata
{
    public sealed class TrophyCacheProgress
    {
        public int Processed { get; init; }
        public int Total { get; init; }
        public string CurrentFile { get; init; } = string.Empty;
    }

    public sealed class TrophyCacheBuildResult
    {
        public int TotalPackages { get; internal set; }
        public int Added { get; internal set; }
        public int AlreadyCached { get; internal set; }
        public int DuplicateContentIds { get; internal set; }
        public int WithoutTrophies { get; internal set; }
        public int Failed { get; internal set; }
        public bool Cancelled { get; internal set; }
        public List<string> Errors { get; } = new();
    }

    public sealed class TrophyMetadataCacheBuilder
    {
        private readonly NpbindExtractor _extractor = new();

        public async Task<TrophyCacheBuildResult> BuildAsync(
            IEnumerable<string> directories,
            bool recursive,
            string cachePath,
            IProgress<TrophyCacheProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new TrophyCacheBuildResult();
            List<string> packagePaths = EnumeratePackages(directories, recursive).ToList();
            result.TotalPackages = packagePaths.Count;
            var cache = new NpCommunicationIdCache(cachePath);
            var seenContentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < packagePaths.Count; index++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.Cancelled = true;
                    break;
                }

                string pkgPath = packagePaths[index];
                progress?.Report(new TrophyCacheProgress
                {
                    Processed = index,
                    Total = packagePaths.Count,
                    CurrentFile = Path.GetFileName(pkgPath)
                });

                try
                {
                    PkgMetadata pkg = PkgMetadataReader.Read(pkgPath);
                    if (!HasTrophyEntry(pkgPath))
                    {
                        result.WithoutTrophies++;
                        continue;
                    }

                    string contentId = pkg.Content_ID;
                    if (!seenContentIds.Add(contentId))
                    {
                        result.DuplicateContentIds++;
                        continue;
                    }

                    if (cache.TryGet(contentId, out _))
                    {
                        result.AlreadyCached++;
                        continue;
                    }

                    NpbindExtractionResult extraction = await _extractor.ExtractAsync(
                        pkgPath, cancellationToken).ConfigureAwait(false);
                    if (extraction.Succeeded && extraction.NpCommunicationId != null)
                    {
                        cache.Set(contentId, extraction.NpCommunicationId);
                        result.Added++;
                    }
                    else if (cancellationToken.IsCancellationRequested)
                    {
                        result.Cancelled = true;
                        break;
                    }
                    else
                    {
                        result.Failed++;
                        AddError(result, Path.GetFileName(pkgPath) + ": " + extraction.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    AddError(result, Path.GetFileName(pkgPath) + ": " + ex.Message);
                }
            }

            progress?.Report(new TrophyCacheProgress
            {
                Processed = result.Cancelled ? Math.Min(result.TotalPackages, result.Added + result.AlreadyCached + result.DuplicateContentIds + result.WithoutTrophies + result.Failed) : result.TotalPackages,
                Total = result.TotalPackages,
                CurrentFile = result.Cancelled ? "Cancelled" : "Complete"
            });
            return result;
        }

        private static IEnumerable<string> EnumeratePackages(IEnumerable<string> directories, bool recursive)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var packages = new List<string>();
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = recursive,
                IgnoreInaccessible = true,
                ReturnSpecialDirectories = false,
                MatchCasing = MatchCasing.CaseInsensitive
            };

            foreach (string directory in directories.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                string fullDirectory;
                try { fullDirectory = Path.GetFullPath(directory); }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { continue; }
                if (!Directory.Exists(fullDirectory)) continue;

                try
                {
                    foreach (string path in Directory.EnumerateFiles(fullDirectory, "*.pkg", options))
                    {
                        string fullPath = Path.GetFullPath(path);
                        if (seen.Add(fullPath)) packages.Add(fullPath);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
            return packages;
        }

        private static bool HasTrophyEntry(string pkgPath)
        {
            using var reader = new OrbisPkgTool.PkgReader(pkgPath);
            return reader.Entries.Any(meta => meta.Id == OrbisPkgTool.Pkg.PkgEntryIds.Trophy00Trp);
        }

        private static void AddError(TrophyCacheBuildResult result, string error)
        {
            if (result.Errors.Count < 20) result.Errors.Add(error);
        }
    }
}
