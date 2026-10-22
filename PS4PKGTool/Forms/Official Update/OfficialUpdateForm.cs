using PS4PKGTool.Utilities.PS4PKGToolHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using ByteSizeLib;

namespace PS4PKGTool
{
    public partial class OfficialUpdateForm : DarkUI.Forms.DarkForm
    {
        private string _currentTitleId;
        private string _downloadDir;
        private BackgroundWorker _downloadWorker;
        private WebRequest _activeRequest;
        private Action<string> _logCallback;
        private int _lookupGeneration;
        private bool _closeWhenDownloadStops;

        public OfficialUpdateForm()
        {
            InitializeComponent();
        }

        public void SetLogCallback(Action<string> logCallback)
        {
            _logCallback = logCallback;
        }

        public void LoadUpdate(string titleId, string pkgType, string downloadDirectory)
        {
            if (_downloadWorker?.IsBusy == true)
            {
                lblStatus.Text = "Finish or stop the current download before loading another game.";
                return;
            }
            _ = Handle;
            int lookupGeneration = Interlocked.Increment(ref _lookupGeneration);
            _currentTitleId = titleId;
            _downloadDir = downloadDirectory;

            if (pkgType != "Game" && pkgType != "Patch")
            {
                lblSummary.Text = "Updates are only available for Game and Patch PKGs.";
                dgvParts.DataSource = null;
                return;
            }

            lblSummary.Text = $"Loading updates for {titleId}...";
            dgvParts.DataSource = null;
            btnDownloadSelected.Enabled = false;
            btnDownloadAll.Enabled = false;

            var bg = new BackgroundWorker();
            bg.DoWork += (_, _) =>
            {
                try
                {
                    var updateInfo = OrbisPkgTool.Psn.UpdateCheck.CheckForUpdate(titleId);
                    if (updateInfo?.Tag?.Package?.ManifestItem?.Pieces != null)
                    {
                        var dt = new DataTable();
                        dt.Columns.Add("Part");
                        dt.Columns.Add("File Size");
                        dt.Columns.Add("SHA256");
                        dt.Columns.Add("URL");
                        dt.Columns.Add("RawSize", typeof(long));

                        string version = updateInfo.Tag.Package.Version ?? "?";
                        string sysVer = FormatSystemVersion(updateInfo.Tag.Package.SystemVer);
                        string type = ToTitleCase(updateInfo.Tag.Package.Type ?? "?");
                        string mandatory = ToTitleCase(updateInfo.Tag.Mandatory ?? "?");
                        string remaster = ToTitleCase(updateInfo.Tag.Package.Remaster ?? "?");
                        int fileCount = updateInfo.Tag.Package.ManifestItem.Pieces.Count;
                        long totalBytes = 0;

                        int partNum = 0;
                        foreach (var piece in updateInfo.Tag.Package.ManifestItem.Pieces)
                        {
                            partNum++;
                            long size = piece.FileSize;
                            totalBytes += size;
                            dt.Rows.Add(
                                $"Part {partNum}",
                                ByteSize.FromBytes(size).ToString(),
                                piece.HashValue ?? "",
                                piece.Url ?? "",
                                size
                            );
                        }

                        string sizeStr = ByteSize.FromBytes(totalBytes).ToString();

                        ApplyLookupResult(lookupGeneration, () =>
                        {
                            lblSummary.Text = $"Version: {version}  |  System: {sysVer}  |  Type: {type}  |  Mandatory: {mandatory}  |  Remaster: {remaster}  |  Files: {fileCount}  |  Size: {sizeStr}";
                            dgvParts.DataSource = dt;
                            if (dgvParts.Columns.Count > 4)
                                dgvParts.Columns[4].Visible = false;
                            btnDownloadSelected.Enabled = dt.Rows.Count > 0;
                            btnDownloadAll.Enabled = dt.Rows.Count > 0;
                        });
                    }
                    else
                    {
                        ApplyLookupResult(lookupGeneration, () =>
                        {
                            lblSummary.Text = $"No updates available for {titleId}.";
                            dgvParts.DataSource = null;
                            btnDownloadSelected.Enabled = false;
                            btnDownloadAll.Enabled = false;
                        });
                    }
                }
                catch (Exception ex)
                {
                    ApplyLookupResult(lookupGeneration, () =>
                    {
                        lblSummary.Text = $"Failed to check updates: {ex.Message}";
                        btnDownloadSelected.Enabled = false;
                        btnDownloadAll.Enabled = false;
                    });
                }
            };
            bg.RunWorkerAsync();
        }

        private void DownloadParts(IEnumerable<DataGridViewRow> rows)
        {
            string titleId = _currentTitleId;
            string downloadDir = _downloadDir;
            var downloads = new List<(string url, string filename, long size, string sha256)>();
            foreach (var row in rows)
            {
                string url = row.Cells[3].Value?.ToString();
                string part = row.Cells[0].Value?.ToString() ?? "part";
                if (!string.IsNullOrEmpty(url))
                {
                    long size = 0;
                    long.TryParse(row.Cells[4].Value?.ToString(), out size);
                    string sha256 = row.Cells[2].Value?.ToString() ?? "";
                    downloads.Add((url, $"{titleId}_{part}.pkg", size, sha256));
                }
            }

            if (downloads.Count == 0) { lblStatus.Text = "No URLs to download."; return; }
            if (string.IsNullOrEmpty(downloadDir)) { lblStatus.Text = "No download directory configured."; return; }
            try { Directory.CreateDirectory(downloadDir); } catch (Exception ex) { lblStatus.Text = "Failed to create download directory: " + ex.Message; return; }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13 | SecurityProtocolType.Tls11;

            toolStripProgress.Visible = true;
            toolStripProgress.Style = ProgressBarStyle.Continuous;
            toolStripProgress.Maximum = 10000;
            toolStripProgress.Value = 0;
            btnDownloadSelected.Enabled = false;
            btnDownloadAll.Enabled = false;

            var bg = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _downloadWorker = bg;

            bg.DoWork += (_, _) =>
            {
                int total = downloads.Count;
                int done = 0, failed = 0;
                _logCallback?.Invoke($"Download official update: {total} part(s) for {titleId}");

                for (int i = 0; i < total; i++)
                {
                    if (bg.CancellationPending) break;

                    var (url, filename, expectedSize, expectedSha256) = downloads[i];
                    string outPath = Path.Combine(downloadDir, filename);
                    string partialPath = outPath + "." + Guid.NewGuid().ToString("N") + ".part";

                    bg.ReportProgress((done + failed) * 10000 / total, $"Downloading {i + 1}/{total}...");

                    try
                    {
                        var request = (HttpWebRequest)WebRequest.Create(url);
                        request.UserAgent = "PS4PKGTool/1.0";
                        request.Timeout = 1800000;
                        request.ReadWriteTimeout = 300000;
                        _activeRequest = request;

                        using (var response = (HttpWebResponse)request.GetResponse())
                        using (var stream = response.GetResponseStream())
                        {
                            _activeRequest = null;
                            long totalBytes = response.ContentLength;
                            byte[] buffer = new byte[65536];
                            long bytesWritten = 0;
                            int lastPct = -1;

                            using (var fs = new FileStream(partialPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                            {
                                while (true)
                                {
                                    if (bg.CancellationPending)
                                    {
                                        request.Abort();
                                        break;
                                    }

                                    int read = stream.Read(buffer, 0, buffer.Length);
                                    if (read == 0) break;
                                    fs.Write(buffer, 0, read);
                                    bytesWritten += read;

                                    int pct = totalBytes > 0 ? (int)(bytesWritten * 100 / totalBytes) : -1;
                                    if (pct != lastPct)
                                    {
                                        lastPct = pct;
                                        bg.ReportProgress(
                                            (int)Math.Min((done + failed) * 10000 / total + pct * 100 / total, 10000),
                                            $"Downloading {i + 1}/{total} ({pct}%)..."
                                        );
                                    }
                                }
                            }

                            if (bg.CancellationPending)
                            {
                                TryDeletePartial(partialPath);
                                break;
                            }

                            PS4PKGTool.Utilities.WorkflowGuards.VerifyDownloadedPiece(
                                partialPath, bytesWritten, expectedSize, expectedSha256);
                            File.Move(partialPath, outPath, true);
                            done++;
                            _logCallback?.Invoke($"Download part {i + 1}/{total}: {filename}");
                            bg.ReportProgress(Math.Min((done + failed) * 10000 / total, 10000));
                        }
                    }
                    catch (WebException wex) when (wex.Status == WebExceptionStatus.RequestCanceled)
                    {
                        TryDeletePartial(partialPath);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (bg.CancellationPending)
                        {
                            TryDeletePartial(partialPath);
                            break;
                        }
                        failed++;
                        _logCallback?.Invoke($"Download failed for {filename}: {ex.Message}");
                        TryDeletePartial(partialPath);
                    }
                    finally
                    {
                        _activeRequest = null;
                    }
                }

                string finalText;
                if (bg.CancellationPending)
                {
                    finalText = "Download stopped.";
                    _logCallback?.Invoke($"Download official update for {titleId}: stopped by user.");
                }
                else if (failed > 0)
                {
                    finalText = $"Downloaded {done} file(s). {failed} failed.";
                    _logCallback?.Invoke($"Download official update for {titleId}: {done} OK, {failed} failed.");
                }
                else
                {
                    finalText = $"Downloaded {done} file(s).";
                    _logCallback?.Invoke($"Download official update for {titleId}: {done} part(s) downloaded.");
                }

                bg.ReportProgress(10000, finalText);
            };

            bg.ProgressChanged += (_, e) =>
            {
                if (e.ProgressPercentage >= 0 && e.ProgressPercentage <= 10000)
                    toolStripProgress.Value = e.ProgressPercentage;
                lblStatus.Text = e.UserState?.ToString() ?? "";
            };

            bg.RunWorkerCompleted += (_, _) =>
            {
                _downloadWorker = null;
                toolStripProgress.Value = 10000;
                toolStripProgress.Visible = false;
                btnDownloadSelected.Enabled = true;
                btnDownloadAll.Enabled = true;
                if (_closeWhenDownloadStops)
                {
                    _closeWhenDownloadStops = false;
                    Close();
                }
            };

            bg.RunWorkerAsync();
        }

        private void ApplyLookupResult(int generation, Action action)
        {
            if (generation != Volatile.Read(ref _lookupGeneration) || IsDisposed || Disposing || !IsHandleCreated)
                return;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    if (generation == Volatile.Read(ref _lookupGeneration) && !IsDisposed && !Disposing)
                        action();
                }));
            }
            catch (InvalidOperationException) { }
        }

        private static void TryDeletePartial(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { Logger.LogWarning("Failed to delete partial download: " + ex.Message); }
        }

        private void btnDownloadSelected_Click(object sender, EventArgs e)
        {
            if (dgvParts.SelectedRows.Count == 0)
            {
                lblStatus.Text = "No rows selected.";
                return;
            }
            DownloadParts(dgvParts.SelectedRows.Cast<DataGridViewRow>());
        }

        private void btnDownloadAll_Click(object sender, EventArgs e)
        {
            DownloadParts(dgvParts.Rows.Cast<DataGridViewRow>());
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ctxCopyUrl_Click(object sender, EventArgs e)
        {
            var urls = new List<string>();
            foreach (DataGridViewRow row in dgvParts.SelectedRows)
            {
                string url = row.Cells[3].Value?.ToString();
                if (!string.IsNullOrEmpty(url)) urls.Add(url);
            }
            if (urls.Count > 0)
            {
                Clipboard.SetText(string.Join("\n", urls));
                lblStatus.Text = $"{urls.Count} URL(s) copied.";
            }
        }

        private void ctxDownload_Click(object sender, EventArgs e)
        {
            btnDownloadSelected_Click(sender, e);
        }

        private static string ToTitleCase(string str)
        {
            if (string.IsNullOrEmpty(str)) return "?";
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
        }

        private static string FormatSystemVersion(string sysVer)
        {
            if (string.IsNullOrEmpty(sysVer) || sysVer == "0")
                return sysVer ?? "?";

            if (sysVer.Contains("."))
                return sysVer;

            try
            {
                int value = Convert.ToInt32(sysVer);
                string hex = string.Format("{0:X}", value);
                if (hex.Length >= 3)
                {
                    string f3 = hex.Substring(0, 3);
                    return f3.Insert(1, ".");
                }
                return hex;
            }
            catch
            {
                return sysVer;
            }
        }

        private void OfficialUpdateForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Interlocked.Increment(ref _lookupGeneration);
            var bg = _downloadWorker;
            if (bg == null || !bg.IsBusy) return;

            var result = MessageBoxHelper.DialogResultYesNo("A download is in progress. Stop and close?");

            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            bg.CancelAsync();
            _activeRequest?.Abort();
            _closeWhenDownloadStops = true;
            e.Cancel = true;
        }
    }
}
