using DarkUI.Controls;
using DarkUI.Forms;
using OrbisPkgTool;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PS4PKGTool
{
    internal sealed partial class PkgMergeProgressForm : DarkForm
    {
        private readonly PkgMergeRequest _request;
        private readonly CancellationTokenSource _cancellation = new();
        private bool _finished;

        public PkgMergeResult? Result { get; private set; }
        public Exception? Error { get; private set; }
        public bool WasCancelled { get; private set; }

        public PkgMergeProgressForm(PkgMergeRequest request)
        {
            _request = request;
            InitializeComponent();
        }

        private async void PkgMergeProgressForm_Shown(object sender, EventArgs e) => await RunMergeAsync();

        private void cancel_Click(object sender, EventArgs e)
        {
            _cancellation.Cancel();
            _cancel.Enabled = false;
            _stage.Text = "Cancelling merge...";
        }

        private async Task RunMergeAsync()
        {
            using var progress = new UiProgress<PkgMergeProgress>(UpdateProgress);
            try
            {
                Result = await Task.Run(() => new PkgMergeService().Merge(_request with
                {
                    Progress = progress,
                    CancellationToken = _cancellation.Token
                }));
                _finished = true;
                DialogResult = DialogResult.OK;
            }
            catch (OperationCanceledException)
            {
                WasCancelled = true;
                _finished = true;
                DialogResult = DialogResult.Cancel;
            }
            catch (Exception ex)
            {
                Error = ex;
                _finished = true;
                DialogResult = DialogResult.Abort;
            }
            finally
            {
                _finished = true;
                Close();
            }
        }

        private void UpdateProgress(PkgMergeProgress progress)
        {
            _stage.Text = progress.Stage;
            _detail.Text = progress.CurrentFile ?? "";

            // Overall bar: pipeline steps 1..TotalSteps (Step/TotalSteps come
            // from PkgMergeService). XOfN renders Value+1 / Maximum+1
            // (0-based semantics), so feed step-1 of steps-1.
            if (progress.TotalSteps > 0 && progress.Step > 0)
            {
                _overallProgress.Marquee = false;
                _overallProgress.TextMode = DarkProgressBarMode.XOfN;
                _overallProgress.Maximum = Math.Max(1, progress.TotalSteps - 1);
                _overallProgress.Value = Math.Clamp(progress.Step - 1, 0, _overallProgress.Maximum);
            }
            else
            {
                _overallProgress.Marquee = true;
                _overallProgress.TextMode = DarkProgressBarMode.NoText;
            }

            // Current bar: extraction/validation item counts (XOfN) or build
            // byte counts (Percentage). Marquee when a step has no sub-progress.
            if (progress.TotalItems > 0)
            {
                _currentProgress.Marquee = false;
                _currentProgress.TextMode = DarkProgressBarMode.XOfN;
                _currentProgress.Maximum = Math.Max(1, progress.TotalItems - 1);
                _currentProgress.Value = Math.Clamp(progress.CurrentItem, 0, _currentProgress.Maximum);
                _bytesLabel.Text = progress.Stage.StartsWith("Validating", StringComparison.Ordinal)
                    ? ""
                    : $"Extracting {progress.CurrentItem + 1} of {progress.TotalItems} files";
            }
            else if (progress.TotalBytes > 0)
            {
                _currentProgress.Marquee = false;
                _currentProgress.TextMode = DarkProgressBarMode.Percentage;
                _currentProgress.Maximum = 100;
                _currentProgress.Value = (int)Math.Clamp(progress.CurrentBytes * 100 / progress.TotalBytes, 0, 100);
                _bytesLabel.Text = $"{progress.CurrentBytes / 1048576.0:F1} MB / {progress.TotalBytes / 1048576.0:F1} MB";
            }
            else
            {
                _currentProgress.Marquee = true;
                _currentProgress.TextMode = DarkProgressBarMode.NoText;
                _bytesLabel.Text = "";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_finished)
            {
                _cancellation.Cancel();
                e.Cancel = true;
                return;
            }

            _cancellation.Dispose();
            base.OnFormClosing(e);
        }
    }
}
