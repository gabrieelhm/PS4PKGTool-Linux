using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using PS4PKGTool.Utilities;
using PS4PKGTool.Utilities.PS4PKGToolHelper;
using PS4PKGTool.Utilities.Settings;
using PS4PKGTool.Utilities.Shadps4;

namespace PS4PKGTool
{
    /// <summary>
    /// One straightforward report form (no wizard): everything the app knows
    /// about the game, session and host is pre-filled; the user confirms the
    /// checklist and copies the Markdown. The checklist is opt-in by design -
    /// nothing is ticked because the app assumes it.
    /// </summary>
    public partial class ReportBuilderForm : DarkUI.Forms.DarkForm
    {
        private readonly GameFeedbackEntry _entry;
        private readonly string? _logFilePath;

        public ReportBuilderForm(GameFeedbackEntry entry)
        {
            InitializeComponent();
            _entry = entry;

            // The game line also names the test result this report is based
            // on ("19 Aug 2026 11:42 · In Game") - a historical report never
            // looks like it was made from today's environment.
            string resultContext = BuildResultContext(entry);
            lblReportGame.Text = string.IsNullOrEmpty(resultContext)
                ? BuildGameLine(entry)
                : BuildGameLine(entry) + "   ·   " + resultContext;
            txtReportDescription.Text = entry.Comment;
            txtReportError.Text = entry.Error;

            int known = Array.IndexOf(PkgFilter.CompatOptions, entry.Status);
            if (known >= 0) cboReportStatus.SelectedIndex = known;

            // shadPS4 version: OFFICIAL RELEASE VERSIONS ONLY - the template
            // does not accept nightly/dev build results. Known releases come
            // from the stable feed cache and release-tagged managed builds;
            // an old entry's own version is kept only when it is release
            // shaped. Nothing is invented.
            foreach (string v in Shadps4ReleaseVersions.Collect(SettingsManager.appSettings_.Shadps4ManagedRoot))
                cboReportShadps4.Items.Add(v);
            string entryVersion = Shadps4ReleaseVersions.Normalize(entry.EmulatorVersion);
            if (Shadps4ReleaseVersions.IsReleaseVersion(entryVersion))
            {
                int idx = cboReportShadps4.Items.IndexOf(entryVersion);
                if (idx < 0)
                {
                    cboReportShadps4.Items.Add(entryVersion);
                    idx = cboReportShadps4.Items.Count - 1;
                }
                cboReportShadps4.SelectedIndex = idx;
            }
            lblReportShadps4Hint.Visible = cboReportShadps4.Items.Count == 0;
            UpdateActionStates();

            // Session log: ONLY the log belonging to this test result (its
            // preserved snapshot, or the session log path itself). An old or
            // manual result without one explicitly says so - today's shadPS4
            // log is never substituted for a historical result.
            _logFilePath = TestResultLogs.Resolve(entry);
            if (_logFilePath != null)
            {
                lblReportLogFile.Text = Path.GetFileName(_logFilePath);
                btnReportOpenLog.Enabled = true;
            }
            else
            {
                lblReportLogFile.Text = "No session log was preserved.";
                btnReportOpenLog.Enabled = false;
            }
        }

        /// <summary>
        /// Which test result this report is based on ("19 Aug 2026 11:42 ·
        /// In Game"), so historical reports never look like they were made
        /// from today's environment.
        /// </summary>
        private static string BuildResultContext(GameFeedbackEntry entry)
        {
            string when = TestResultDisplay.FormatWhen(entry.TimestampUtc);
            if (when == "-") return "";
            string status = string.IsNullOrWhiteSpace(entry.Status) ? "" : " · " + entry.Status;
            return when + status;
        }

        private static string BuildGameLine(GameFeedbackEntry entry)
        {
            string line = string.IsNullOrEmpty(entry.Title) ? "" : entry.Title;
            if (!string.IsNullOrEmpty(entry.TitleId))
                line = line.Length == 0 ? entry.TitleId : line + "  ·  " + entry.TitleId;
            string v = CompatibilityReport.VersionDisplay(entry.GameVersion);
            if (!v.StartsWith("(unknown)", StringComparison.Ordinal))
                line = line.Length == 0 ? v : line + "  ·  " + v;
            return line.Length == 0 ? "(unknown)" : line;
        }

        private string CurrentMarkdown()
        {
            var current = new GameFeedbackEntry
            {
                Title = _entry.Title,
                TitleId = _entry.TitleId,
                GameVersion = _entry.GameVersion,
                EmulatorVersion = cboReportShadps4.SelectedItem?.ToString() ?? "",
                Status = cboReportStatus.SelectedItem?.ToString() ?? "",
                Os = _entry.Os,
                Processor = _entry.Processor,
                GraphicsCard = _entry.GraphicsCard,
                Error = txtReportError.Text.Trim(),
                Comment = txtReportDescription.Text.Trim(),
            };
            bool[] checklist =
            {
                chkReportRelease.Checked,
                chkReportExisting.Checked,
                chkReportOwnDump.Checked,
                chkReportFirmware.Checked,
                chkReportSyncLog.Checked,
                chkReportDefaultSettings.Checked,
            };
            return CompatibilityReport.BuildMarkdown(current, checklist,
                _logFilePath != null ? Path.GetFileName(_logFilePath) : null);
        }

        /// <summary>Copy/Preview require an official release version - a report cannot be filed without one.</summary>
        private void cboReportShadps4_SelectedIndexChanged(object sender, EventArgs e) => UpdateActionStates();

        private void UpdateActionStates()
        {
            bool ready = cboReportShadps4.SelectedIndex >= 0;
            btnReportPreview.Enabled = ready;
            btnReportCopy.Enabled = ready;
        }

        private void btnReportPreview_Click(object sender, EventArgs e)
        {
            using (var preview = new ReportPreviewForm(CurrentMarkdown()))
                preview.ShowDialog(this);
        }

        private void btnReportCopy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(CurrentMarkdown());
                lblReportCopied.Text = "Report copied to clipboard.";
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Copy report failed: " + ex.Message);
                MessageBoxHelper.ShowWarning("Could not copy the report to the clipboard.", true);
            }
        }

        private void btnReportOpenLog_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_logFilePath)) return;
            try
            {
                Process.Start(new ProcessStartInfo(_logFilePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Open log failed: " + ex.Message);
                MessageBoxHelper.ShowWarning("Could not open the log file.", true);
            }
        }

        private void btnReportCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
