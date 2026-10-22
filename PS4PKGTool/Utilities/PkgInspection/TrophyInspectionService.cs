using PS4PKGTool.Utilities.PS4PKGToolHelper;
using PS4PKGTool.Utilities.TrophyMetadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PS4PKGTool.Utilities.PkgInspection
{
    public enum TrophyInspectionStatus
    {
        Loaded,
        NoTrophyResource,
        Inaccessible,
        Failed
    }

    public sealed class TrophyInspectionItem : IDisposable
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Grade { get; init; } = string.Empty;
        public bool Hidden { get; init; }
        public byte[] IconBytes { get; init; }

        public void Dispose() { }
    }

    public sealed class TrophyInspectionResult : IDisposable
    {
        private bool _disposed;

        public TrophyInspectionStatus Status { get; init; }
        public IReadOnlyList<TrophyInspectionItem> Trophies { get; init; } =
            Array.Empty<TrophyInspectionItem>();
        public string Message { get; init; } = string.Empty;
        public string MetadataEntryName { get; init; } = string.Empty;
        public string NpCommunicationId { get; init; } = string.Empty;
        public int IconDecodeFailures { get; init; }
        public bool IsDisposed => _disposed;

        public void Dispose()
        {
            _disposed = true;
        }
    }

    internal enum PkgTrophyResourceStatus
    {
        Extracted,
        NotFound,
        Inaccessible
    }

    internal sealed class PkgTrophyResourceResult
    {
        public PkgTrophyResourceStatus Status { get; init; }
        public string TrpPath { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    internal interface IPkgTrophyResourceExtractor
    {
        PkgTrophyResourceResult Extract(
            string packagePath, string temporaryDirectory, CancellationToken cancellationToken);
    }

    internal sealed class OrbisPkgTrophyResourceExtractor : IPkgTrophyResourceExtractor
    {
        private const string TrophyEntryPath = "Sc0/trophy/trophy00.trp";

        public PkgTrophyResourceResult Extract(
            string packagePath, string temporaryDirectory, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var reader = new OrbisPkgTool.PkgReader(packagePath);
                cancellationToken.ThrowIfCancellationRequested();
                var metadata = reader.Entries.FirstOrDefault(entry =>
                    entry.Id == OrbisPkgTool.Pkg.PkgEntryIds.Trophy00Trp);
                if (metadata == null)
                {
                    return new PkgTrophyResourceResult
                    {
                        Status = PkgTrophyResourceStatus.NotFound,
                        Message = "No trophy data was found in this package."
                    };
                }

                if (metadata.IsEncrypted)
                {
                    return new PkgTrophyResourceResult
                    {
                        Status = PkgTrophyResourceStatus.Inaccessible,
                        Message = "The package contains trophy data, but its TRP entry is encrypted and cannot be accessed."
                    };
                }

                string trpPath = Path.Combine(temporaryDirectory, "TROPHY00.TRP");
                byte[] trpBytes = reader.ExtractEntryBytes(TrophyEntryPath);
                File.WriteAllBytes(trpPath, trpBytes);

                return new PkgTrophyResourceResult
                {
                    Status = PkgTrophyResourceStatus.Extracted,
                    TrpPath = trpPath
                };
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }

    internal interface ITrophyMetadataReader
    {
        TrophyMetadataResult Read(string trpPath, string npCommunicationId);
    }

    internal sealed class SharedTrophyMetadataReader : ITrophyMetadataReader
    {
        private readonly TrophyMetadataService _service = new TrophyMetadataService();

        public TrophyMetadataResult Read(string trpPath, string npCommunicationId) =>
            _service.Read(trpPath, string.IsNullOrWhiteSpace(npCommunicationId) ? null : npCommunicationId);
    }

    internal interface INpCommunicationIdProvider
    {
        string Find(string contentId);
    }

    internal sealed class CachedNpCommunicationIdProvider : INpCommunicationIdProvider
    {
        private readonly string _cachePath;

        public CachedNpCommunicationIdProvider()
        {
            _cachePath = Path.Combine(
                Helper.AppDataDirectory, "TrophyMetadata", "np-communication-ids.json");
        }

        public string Find(string contentId)
        {
            if (string.IsNullOrWhiteSpace(contentId))
                return string.Empty;
            return new NpCommunicationIdCache(_cachePath)
                .TryGet(contentId, out string value) ? value : string.Empty;
        }
    }

    public interface ITrophyInspectionLoader
    {
        Task<TrophyInspectionResult> LoadAsync(
            string packagePath, string contentId, CancellationToken cancellationToken);
    }

    public sealed class TrophyInspectionService : ITrophyInspectionLoader
    {
        private readonly IPkgTrophyResourceExtractor _extractor;
        private readonly ITrophyMetadataReader _metadataReader;
        private readonly INpCommunicationIdProvider _idProvider;
        private readonly string _temporaryRoot;

        public TrophyInspectionService()
            : this(
                new OrbisPkgTrophyResourceExtractor(),
                new SharedTrophyMetadataReader(),
                new CachedNpCommunicationIdProvider(),
                Path.Combine(Path.GetTempPath(), "PS4PKGTool", "MiniPkgViewerTrophy"))
        {
        }

        internal TrophyInspectionService(
            IPkgTrophyResourceExtractor extractor,
            ITrophyMetadataReader metadataReader,
            INpCommunicationIdProvider idProvider,
            string temporaryRoot)
        {
            _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
            _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
            _idProvider = idProvider ?? throw new ArgumentNullException(nameof(idProvider));
            _temporaryRoot = Path.GetFullPath(
                temporaryRoot ?? throw new ArgumentNullException(nameof(temporaryRoot)));
        }

        public async Task<TrophyInspectionResult> LoadAsync(
            string packagePath, string contentId, CancellationToken cancellationToken)
        {
            string temporaryDirectory = Path.Combine(
                _temporaryRoot, "viewer_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);

            try
            {
                PkgTrophyResourceResult resource = await Task.Run(
                    () => _extractor.Extract(packagePath, temporaryDirectory, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (resource.Status == PkgTrophyResourceStatus.NotFound)
                    return State(TrophyInspectionStatus.NoTrophyResource, resource.Message);
                if (resource.Status == PkgTrophyResourceStatus.Inaccessible)
                    return State(TrophyInspectionStatus.Inaccessible, resource.Message);

                string npCommunicationId = _idProvider.Find(contentId);
                if (string.IsNullOrWhiteSpace(npCommunicationId))
                {
                    try
                    {
                        var npbindResult = await new NpbindExtractor().ExtractAsync(packagePath, cancellationToken).ConfigureAwait(false);
                        if (npbindResult.Succeeded && !string.IsNullOrWhiteSpace(npbindResult.NpCommunicationId))
                        {
                            npCommunicationId = npbindResult.NpCommunicationId;
                        }
                    }
                    catch { }
                }

                TrophyMetadataResult metadata = await Task.Run(
                    () => _metadataReader.Read(resource.TrpPath, npCommunicationId),
                    cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (metadata.Trophies.Count == 0)
                {
                    return new TrophyInspectionResult
                    {
                        Status = metadata.FailureKind is
                            TrophyMetadataFailureKind.MissingNpCommunicationId or
                            TrophyMetadataFailureKind.DecryptionFailed
                                ? TrophyInspectionStatus.Inaccessible
                                : TrophyInspectionStatus.Failed,
                        Message = SafeFailureMessage(metadata.StatusMessage),
                        MetadataEntryName = metadata.MetadataEntryName,
                        NpCommunicationId = metadata.NpCommunicationId ?? string.Empty
                    };
                }

                var items = new List<TrophyInspectionItem>(metadata.Trophies.Count);
                foreach (TrophyInfo trophy in metadata.Trophies.OrderBy(item => item.Id))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    items.Add(new TrophyInspectionItem
                    {
                        Id = trophy.Id,
                        Name = trophy.Name,
                        Description = trophy.Description,
                        Grade = trophy.Grade.ToString(),
                        Hidden = trophy.IsHidden,
                        IconBytes = trophy.IconData
                    });
                }

                return new TrophyInspectionResult
                {
                    Status = TrophyInspectionStatus.Loaded,
                    Trophies = items,
                    Message = metadata.StatusMessage,
                    MetadataEntryName = metadata.MetadataEntryName,
                    NpCommunicationId = metadata.NpCommunicationId ?? string.Empty,
                    IconDecodeFailures = 0
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return State(
                    TrophyInspectionStatus.Failed,
                    "Trophy information could not be read. " + ex.Message);
            }
            finally
            {
                TryDeleteTemporaryDirectory(temporaryDirectory);
            }
        }

        private static TrophyInspectionResult State(TrophyInspectionStatus status, string message) =>
            new TrophyInspectionResult { Status = status, Message = message ?? string.Empty };

        private static string SafeFailureMessage(string message) =>
            string.IsNullOrWhiteSpace(message)
                ? "Trophy information could not be read."
                : message;

        private static void TryDeleteTemporaryDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
            catch { }
        }
    }
}
