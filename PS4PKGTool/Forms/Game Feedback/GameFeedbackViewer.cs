using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using PS4PKGTool.Utilities.PS4PKGToolHelper;

namespace PS4PKGTool
{
    /// <summary>
    /// Test History for one game: every local test result (each is a profile
    /// candidate: status, core, launcher, session details). Newest first so
    /// runs are easy to compare; the user can pin one entry as the game's
    /// representative result - at most one per game, persisted in the JSONL.
    /// </summary>
    public partial class GameFeedbackViewer : DarkUI.Forms.DarkForm
    {
        private readonly string _titleId;
        private readonly List<GameFeedbackEntry> _entries;

        public GameFeedbackViewer(string titleId, List<GameFeedbackEntry> entries)
        {
            InitializeComponent();
            _titleId = titleId;
            // Newest first for chronological regression reading.
            _entries = new List<GameFeedbackEntry>(entries);
            _entries.Sort((a, b) => ParseTime(b.TimestampUtc).CompareTo(ParseTime(a.TimestampUtc)));
            this.Text = "Test History  ·  " + titleId;
            RefreshList();
        }

        private static DateTime ParseTime(string timestampUtc)
            => DateTime.TryParse(timestampUtc, null, DateTimeStyles.RoundtripKind, out DateTime when)
                ? when : DateTime.MinValue;

        private void RefreshList()
        {
            lstFeedbackEntries.Items.Clear();
            foreach (var e in _entries)
            {
                string prefix = e.IsBestProfile ? "✓ " : "  ";
                string runtime = TestResultDisplay.FormatRuntime(e.RuntimeSeconds);
                string crash = e.Crashed ? "  ·  Crash" : "";
                lstFeedbackEntries.Items.Add(
                    $"{prefix}{TestResultDisplay.FormatWhen(e.TimestampUtc)}  {e.Status}  {runtime}{crash}");
            }
            if (lstFeedbackEntries.Items.Count == 0)
                lstFeedbackEntries.Items.Add("(no test results yet)");
            btnFeedbackSetBest.Enabled = false;
            btnFeedbackCopyReport.Enabled = false;
            lblFeedbackViewerDetails.Text = "";
        }

        private void lstFeedbackEntries_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = lstFeedbackEntries.SelectedIndex;
            var entry = idx >= 0 && idx < _entries.Count ? _entries[idx] : null;
            btnFeedbackSetBest.Enabled = entry != null && !entry.IsBestProfile;
            btnFeedbackCopyReport.Enabled = entry != null;
            lblFeedbackViewerDetails.Text = entry == null ? "" : BuildDetails(entry);
        }

        private void btnFeedbackSetBest_Click(object sender, EventArgs e)
        {
            int idx = lstFeedbackEntries.SelectedIndex;
            if (idx < 0 || idx >= _entries.Count) return;

            var entry = _entries[idx];
            GameFeedbackStore.SetBestProfile(_titleId, entry.TimestampUtc);
            foreach (var other in _entries)
                other.IsBestProfile = string.Equals(other.TimestampUtc, entry.TimestampUtc, StringComparison.OrdinalIgnoreCase);

            RefreshList();
            lstFeedbackEntries.SelectedIndex = idx;
        }

        private void btnFeedbackCopyReport_Click(object sender, EventArgs e)
        {
            int idx = lstFeedbackEntries.SelectedIndex;
            if (idx < 0 || idx >= _entries.Count) return;

            // Same report builder as the post-session flow, pre-filled from
            // the saved result (with ITS session log) - no need to replay
            // the session and never a newer unrelated log.
            using (var report = new ReportBuilderForm(_entries[idx]))
                report.ShowDialog(this);
        }

        private void btnFeedbackClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private static string BuildDetails(GameFeedbackEntry e)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Status: " + e.Status);
            if (!string.IsNullOrWhiteSpace(e.Comment)) sb.AppendLine("Notes: " + e.Comment);

            sb.AppendLine();
            sb.AppendLine("SESSION");
            sb.AppendLine("Runtime: " + TestResultDisplay.FormatRuntime(e.RuntimeSeconds));
            sb.AppendLine("Termination: " + TerminationText(e));
            if (e.ExitCode != null) sb.AppendLine("Exit code: " + TestResultDisplay.FormatExitCode(e.ExitCode));
            if (!string.IsNullOrWhiteSpace(e.ExitStatusName)) sb.AppendLine("Status: " + e.ExitStatusName);
            if (!string.IsNullOrWhiteSpace(e.Error)) sb.AppendLine("Error: " + TrimForDetails(e.Error, 200));

            sb.AppendLine();
            sb.AppendLine("GAME");
            if (!string.IsNullOrWhiteSpace(e.Title)) sb.AppendLine("Title: " + e.Title);
            if (!string.IsNullOrWhiteSpace(e.TitleId)) sb.AppendLine("Title ID: " + e.TitleId);
            if (!string.IsNullOrWhiteSpace(e.GameVersion)) sb.AppendLine("App version: " + e.GameVersion);

            sb.AppendLine();
            sb.AppendLine("SHADPS4");
            if (!string.IsNullOrWhiteSpace(e.EmulatorVersion)) sb.AppendLine("Version: " + e.EmulatorVersion);
            if (!string.IsNullOrWhiteSpace(e.CoreCommit)) sb.AppendLine("Commit: " + e.CoreCommit);
            if (!string.IsNullOrWhiteSpace(e.Core)) sb.AppendLine("Core: " + e.Core);
            if (!string.IsNullOrWhiteSpace(e.Launcher)) sb.AppendLine("Launcher: " + e.Launcher);

            sb.AppendLine();
            sb.AppendLine("SYSTEM");
            if (!string.IsNullOrWhiteSpace(e.Os)) sb.AppendLine("OS: " + e.Os);
            if (!string.IsNullOrWhiteSpace(e.Processor)) sb.AppendLine("CPU: " + e.Processor);
            if (!string.IsNullOrWhiteSpace(e.GraphicsCard)) sb.AppendLine("GPU: " + e.GraphicsCard);

            sb.AppendLine();
            sb.AppendLine("LOG");
            sb.AppendLine(TestResultLogs.Resolve(e) != null ? "Session log preserved" : "Not preserved");
            return sb.ToString().TrimEnd();
        }

        private static string TerminationText(GameFeedbackEntry e)
        {
            if (e.Crashed) return "Crash";
            return string.IsNullOrEmpty(e.TerminationKind) ? "Not recorded" : "Normal exit";
        }

        private static string TrimForDetails(string text, int max)
        {
            string oneLine = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return oneLine.Length <= max ? oneLine : oneLine.Substring(0, max) + "...";
        }
    }
}
