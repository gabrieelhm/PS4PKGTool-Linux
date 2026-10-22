using ByteSizeLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PS4PKGTool.Utilities.PkgInspection
{
    internal sealed class PkgEntryInfo
    {
        public string Name { get; init; } = string.Empty;
        public string Offset { get; init; } = string.Empty;
        public string Size { get; init; } = string.Empty;
        public string Flags1 { get; init; } = string.Empty;
        public string Flags2 { get; init; } = string.Empty;
        public string Encrypted { get; init; } = string.Empty;
    }

    internal sealed class PkgEntryLoadResult
    {
        private PkgEntryLoadResult(IReadOnlyList<PkgEntryInfo> entries, string errorMessage)
        {
            Entries = entries ?? Array.Empty<PkgEntryInfo>();
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public IReadOnlyList<PkgEntryInfo> Entries { get; }
        public string ErrorMessage { get; }
        public bool Succeeded => ErrorMessage.Length == 0;

        public static PkgEntryLoadResult Success(IReadOnlyList<PkgEntryInfo> entries) =>
            new PkgEntryLoadResult(entries, null);

        public static PkgEntryLoadResult Failure(string errorMessage) =>
            new PkgEntryLoadResult(Array.Empty<PkgEntryInfo>(), errorMessage);
    }

    internal interface IPkgEntryReader
    {
        IReadOnlyList<PkgEntryInfo> Read(string packagePath, CancellationToken cancellationToken);
    }

    internal sealed class LibOrbisPkgEntryReader : IPkgEntryReader
    {
        public IReadOnlyList<PkgEntryInfo> Read(string packagePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var reader = new OrbisPkgTool.PkgReader(packagePath);
                cancellationToken.ThrowIfCancellationRequested();

                var entries = new List<PkgEntryInfo>();
                foreach (var entry in reader.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entries.Add(new PkgEntryInfo
                    {
                        Name = entry.Name ?? OrbisPkgTool.Pkg.PkgEntryNames.TryGetName(entry.Id) ?? $"0x{entry.Id:X8}",
                        Offset = $"0x{entry.DataOffset:X}",
                        Size = ByteSize.FromBytes(Convert.ToDouble(entry.DataSize)).ToString(),
                        Flags1 = $"0x{entry.Flags1:X}",
                        Flags2 = $"0x{entry.Flags2:X}",
                        Encrypted = $"{(entry.IsEncrypted ? 1 : 0):X}"
                    });
                }

                return entries;
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }

    internal interface IPkgEntryLoader
    {
        Task<PkgEntryLoadResult> LoadAsync(string packagePath, CancellationToken cancellationToken);
    }

    internal sealed class PkgEntryInspectionService : IPkgEntryLoader
    {
        private readonly IPkgEntryReader _reader;

        public PkgEntryInspectionService()
            : this(new LibOrbisPkgEntryReader())
        {
        }

        internal PkgEntryInspectionService(IPkgEntryReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public async Task<PkgEntryLoadResult> LoadAsync(
            string packagePath, CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<PkgEntryInfo> entries = await Task.Run(
                    () => _reader.Read(packagePath, cancellationToken), cancellationToken);
                return PkgEntryLoadResult.Success(entries.ToArray());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return PkgEntryLoadResult.Failure(ex.Message);
            }
        }
    }

    internal sealed class PkgEntryInspectionSession : IDisposable
    {
        private readonly string _packagePath;
        private readonly IPkgEntryLoader _loader;
        private readonly CancellationTokenSource _lifetimeCancellation = new CancellationTokenSource();
        private readonly object _sync = new object();
        private Task<PkgEntryLoadResult> _loadTask;
        private bool _disposed;

        public PkgEntryInspectionSession(string packagePath, IPkgEntryLoader loader)
        {
            _packagePath = packagePath ?? throw new ArgumentNullException(nameof(packagePath));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public Task<PkgEntryLoadResult> LoadAsync(CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_loadTask == null)
                    _loadTask = LoadCoreAsync(cancellationToken);
                return _loadTask;
            }
        }

        private async Task<PkgEntryLoadResult> LoadCoreAsync(CancellationToken cancellationToken)
        {
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCancellation.Token, cancellationToken);
            return await _loader.LoadAsync(_packagePath, linkedCancellation.Token);
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;
                _disposed = true;
                _lifetimeCancellation.Cancel();
            }

            _lifetimeCancellation.Dispose();
        }
    }
}
