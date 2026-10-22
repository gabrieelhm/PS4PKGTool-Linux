#nullable enable
using DarkUI.Controls;
using DarkUI.Forms;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PS4PKGTool.Utilities.Ffpfsc;

namespace PS4PKGTool.Forms.Main
{
    /// <summary>
    /// Progress dialog for a single-PKG FFPFSC conversion. Mirrors the shape
    /// and cancellation behavior of <see cref="PkgMergeProgressForm"/> but
    /// runs <see cref="Ps4FfpfscConverterService"/> on a worker thread.
    /// Two progress bars: overall pipeline steps (1/6) and current sub-
    /// progress (extraction n/n files, PFSC MB, verify MB).
    /// </summary>
    internal sealed partial class FfpfscConvertForm : DarkForm
    {
        private readonly FfpfscConvertOptions _options;
        private readonly CancellationTokenSource _cancellation = new();
        private bool _finished;

        public FfpfscConvertResult? Result { get; private set; }
        public string ErrorMessage { get; private set; } = "";
        public bool WasCancelled { get; private set; }

        public FfpfscConvertForm(FfpfscConvertOptions options)
        {
            _options = options;
            InitializeComponent();
        }

        private void FfpfscConvertForm_Shown(object sender, EventArgs e) => _ = RunConversionAsync();

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cancellation.Cancel();
            btnCancel.Enabled = false;
            lblStage.Text = "Cancelling conversion...";
        }

        private async Task RunConversionAsync()
        {
            var converter = new Ps4FfpfscConverterService();
            var progress = new Progress<FfpfscConvertProgress>(UpdateProgress);
            var (succeeded, message, result) = await Task.Run(
                () => converter.ConvertAsync(_options, progress, _cancellation.Token)).ConfigureAwait(true);
            _finished = true;
            if (succeeded)
            {
                Result = result;
                DialogResult = DialogResult.OK;
            }
            else if (message == "Cancelled")
            {
                WasCancelled = true;
                DialogResult = DialogResult.Cancel;
            }
            else
            {
                ErrorMessage = message;
                DialogResult = DialogResult.Abort;
            }
            Close();
        }

        private void UpdateProgress(FfpfscConvertProgress progress)
        {
            // Overall bar: steps 1..TotalSteps. XOfN renders Value+1/Maximum+1
            // (0-based semantics), so feed it step-1 of totalSteps-1.
            _overallProgress.Marquee = false;
            _overallProgress.TextMode = DarkProgressBarMode.XOfN;
            _overallProgress.Maximum = Math.Max(1, progress.TotalSteps - 1);
            _overallProgress.Value = Math.Clamp(progress.CurrentStep - 1, 0, _overallProgress.Maximum);

            lblStage.Text = progress.Stage;
            lblDetail.Text = progress.CurrentFile ?? "";

            // Current bar: item counts (extraction n/n) or byte counts
            // (PFSC build/verify). Marquee when a step has no sub-progress.
            if (progress.ItemsTotal > 0)
            {
                _currentProgress.Marquee = false;
                _currentProgress.TextMode = DarkProgressBarMode.XOfN;
                _currentProgress.Maximum = Math.Max(1, progress.ItemsTotal - 1);
                _currentProgress.Value = Math.Clamp(progress.ItemsProcessed - 1, 0, _currentProgress.Maximum);
                lblBytes.Text = $"Extracting {progress.ItemsProcessed} of {progress.ItemsTotal} files";
            }
            else if (progress.TotalBytes > 0)
            {
                _currentProgress.Marquee = false;
                _currentProgress.TextMode = DarkProgressBarMode.Percentage;
                _currentProgress.Maximum = 100;
                int percent = (int)Math.Clamp(progress.BytesProcessed * 100 / progress.TotalBytes, 0, 100);
                _currentProgress.Value = percent;
                lblBytes.Text = $"{progress.BytesProcessed / 1048576.0:F1} MB / {progress.TotalBytes / 1048576.0:F1} MB";
            }
            else
            {
                _currentProgress.Marquee = true;
                _currentProgress.TextMode = DarkProgressBarMode.NoText;
                lblBytes.Text = "";
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
