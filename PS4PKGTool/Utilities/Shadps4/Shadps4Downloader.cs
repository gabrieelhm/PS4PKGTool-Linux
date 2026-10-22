using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PS4PKGTool.Utilities.Shadps4
{
    /// <summary>
    /// Streaming binary downloader: writes to "&lt;dest&gt;.part", streams
    /// (never buffers the archive in memory), reports byte progress, verifies
    /// the finished size and optionally the sha256 (GitHub's own asset digest
    /// - no invented hashes), then renames to the final file. Cancellation or
    /// any failure removes the .part file.
    /// </summary>
    public sealed class Shadps4Downloader
    {
        /// <summary>Test seam: replaces the HTTP source with a canned stream.</summary>
        public Func<string, Task<Stream>>? StreamFactory { get; set; }

        private readonly HttpClient _http;

        public Shadps4Downloader(HttpClient? http = null)
        {
            _http = http ?? new HttpClient();
        }

        /// <summary>
        /// Downloads url to destinationFile. Returns an error message, or null on success.
        /// </summary>
        public async Task<string?> DownloadAsync(
            string url,
            string destinationFile,
            long? expectedSize,
            string? expectedSha256Hex,
            IProgress<(long Done, long? Total)>? progress,
            CancellationToken ct)
        {
            string partFile = destinationFile + ".part";
            try
            {
                Stream source;
                try
                {
                    source = StreamFactory != null
                        ? await StreamFactory(url).ConfigureAwait(false)
                        : await _http.GetStreamAsync(url, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    return $"Download failed: {ex.Message}";
                }

                long done = 0;
                string actualHash = "";
                using (source)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(partFile)!);
                    using var output = new FileStream(partFile, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true);
                    using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    var buffer = new byte[256 * 1024];
                    int read;
                    while ((read = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                    {
                        ct.ThrowIfCancellationRequested();
                        await output.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                        hasher.AppendData(buffer, 0, read);
                        done += read;
                        progress?.Report((done, expectedSize));
                    }
                    await output.FlushAsync(ct).ConfigureAwait(false);
                    actualHash = Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
                }

                // Verification happens AFTER the .part stream is closed, so a
                // failed check can delete the file (FileShare.None would block it).
                if (expectedSize.HasValue && done != expectedSize.Value)
                {
                    TryDelete(partFile);
                    return $"Download incomplete: expected {expectedSize.Value} bytes, received {done}.";
                }

                if (!string.IsNullOrWhiteSpace(expectedSha256Hex)
                    && !string.Equals(actualHash, expectedSha256Hex.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    TryDelete(partFile);
                    return $"Checksum mismatch: expected {expectedSha256Hex}, got {actualHash}.";
                }

                File.Move(partFile, destinationFile, overwrite: true);
                return null;
            }
            catch (OperationCanceledException)
            {
                TryDelete(partFile);
                throw;
            }
            catch (Exception ex)
            {
                TryDelete(partFile);
                return $"Download failed: {ex.Message}";
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort: leftover .part is overwritten next run */ }
        }
    }
}
