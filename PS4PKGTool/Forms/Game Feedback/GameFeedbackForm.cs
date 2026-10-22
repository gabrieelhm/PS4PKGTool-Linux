using System;
using System.Drawing;
using System.Windows.Forms;
using DarkUI.Controls;
using PS4PKGTool.Utilities;
using PS4PKGTool.Utilities.PS4PKGToolHelper;
using PS4PKGTool.Utilities.Shadps4;

namespace PS4PKGTool
{
    /// <summary>
    /// Post-session result capture: how far the game got, notes, then Save
    /// (local test history) or Create Compatibility Report (shadPS4 Markdown).
    /// Launch-session results carry their runtime, exit classification and a
    /// preserved session log; manual results truthfully have none.
    /// Compatibility status and session termination stay independent - a
    /// crash never auto-classifies the game as Nothing.
    /// </summary>
    public partial class GameFeedbackForm : DarkUI.Forms.DarkForm
    {
        /// <summary>Community status wording, casual order (Nothing to Playable).</summary>
        private static readonly string[] StatusOrder =
            { "Nothing", "Boots", "Menus", "In-Game", "Playable" };

        private static readonly Color SelectedFill = Color.FromArgb(100, 160, 220);

        private readonly GameFeedbackContext _context;
        private readonly Color _defaultButtonForeColor;
        private string _selectedStatus = "";
        private bool _saved;
        private GameFeedbackEntry? _savedEntry;

        public GameFeedbackForm(GameFeedbackContext context)
        {
            InitializeComponent();
            _context = context;
            _defaultButtonForeColor = btnStatusNothing.ForeColor;

            lblFeedbackGameName.Text = string.IsNullOrEmpty(context.Title) ? "(unknown)" : context.Title;
            lblFeedbackGameMeta.Text = BuildMetaLine(context.TitleId, context.GameVersion);
            lblFeedbackSession.Text = BuildSessionLine(context);

            var buttons = new[] { btnStatusNothing, btnStatusBoots, btnStatusMenus, btnStatusInGame, btnStatusPlayable };
            int known = Array.IndexOf(StatusOrder, context.KnownStatus);
            if (known >= 0) SelectStatus(buttons[known]);
            UpdateActionButtons();
        }

        private static string BuildMetaLine(string titleId, string gameVersion)
        {
            string line = string.IsNullOrEmpty(titleId) ? "" : titleId;
            if (!string.IsNullOrEmpty(gameVersion))
            {
                string v = CompatibilityReport.VersionDisplay(gameVersion);
                if (!v.StartsWith("(unknown)", StringComparison.Ordinal))
                    line = line.Length == 0 ? v : line + "  ·  " + v;
            }
            return line;
        }

        /// <summary>
        /// Session context line: runtime + termination for launch sessions
        /// ("Runtime: 47m 12s   Crash 0xC0000005"), an explicit no-session
        /// note for manual results. Nothing is invented for either.
        /// </summary>
        private static string BuildSessionLine(GameFeedbackContext context)
        {
            if (context.ResultSource == "LaunchSession")
            {
                string runtime = TestResultDisplay.FormatRuntime(context.RuntimeSeconds);
                string termination = IsCrashed(context)
                    ? "Crash " + TestResultDisplay.FormatExitCode(context.ExitCode)
                    : "Normal exit";
                return "Runtime: " + runtime + "   " + termination;
            }
            return "Manual test result  ·  No launch session is associated with this entry.";
        }

        private static bool IsCrashed(GameFeedbackContext context)
            => Enum.TryParse(context.TerminationKind, out Shadps4ExitCategory category)
                && Shadps4ExitStatus.IsErrorStatus(category);

        private void btnStatus_Click(object sender, EventArgs e)
        {
            SelectStatus((DarkButton)sender);
        }

        private void SelectStatus(DarkButton selected)
        {
            foreach (var btn in new[] { btnStatusNothing, btnStatusBoots, btnStatusMenus, btnStatusInGame, btnStatusPlayable })
            {
                if (ReferenceEquals(btn, selected))
                {
                    btn.BackColorUseGeneric = false;
                    btn.BackColor = SelectedFill;
                    btn.ForeColor = Color.White;
                }
                else
                {
                    btn.BackColorUseGeneric = true;
                    btn.ForeColor = _defaultButtonForeColor;
                }
            }
            _selectedStatus = selected.Text;
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            bool ready = _selectedStatus.Length > 0;
            btnFeedbackSave.Enabled = ready;
            btnFeedbackCreateReport.Enabled = ready;
        }

        private GameFeedbackEntry CollectEntry()
            => new()
            {
                TitleId = _context.TitleId,
                Title = _context.Title,
                GameVersion = _context.GameVersion,
                Core = _context.CoreDisplay,
                Launcher = _context.LauncherDisplay,
                Status = _selectedStatus,
                Os = _context.OsDisplay,
                EmulatorVersion = _context.EmulatorVersion,
                Processor = HostInfo.CpuName,
                GraphicsCard = HostInfo.GpuName,
                Error = BuildErrorSummary(_context),
                Comment = txtFeedbackNotes.Text.Trim(),
                RuntimeSeconds = _context.RuntimeSeconds,
                TerminationKind = _context.TerminationKind ?? "",
                ExitCode = _context.ExitCode,
                ExitStatusName = _context.ExitStatusName,
                CoreId = _context.CoreId,
                CoreCommit = _context.CoreCommit,
                CoreVersion = _context.CoreVersion,
            };

        /// <summary>
        /// Crash sessions persist a concise structured summary ("Crash
        /// 0xC0000005 (STATUS_ACCESS_VIOLATION)") - the full log tail lives
        /// in the preserved session log. The bulk tail is kept inline only
        /// when that log could not be preserved (see SaveResult).
        /// </summary>
        private static string BuildErrorSummary(GameFeedbackContext context)
        {
            if (!IsCrashed(context)) return context.ErrorPrefill ?? "";
            string exit = TestResultDisplay.FormatExitCode(context.ExitCode);
            string name = string.IsNullOrEmpty(context.ExitStatusName) ? "" : " (" + context.ExitStatusName + ")";
            return "Crash " + exit + name;
        }

        private void btnFeedbackSave_Click(object sender, EventArgs e)
        {
            SaveResult();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnFeedbackCreateReport_Click(object sender, EventArgs e)
        {
            // The report is built from the saved result (which carries its
            // log reference). Save once - repeated actions never create
            // duplicate history entries.
            SaveResult();
            if (_savedEntry != null)
            {
                using (var report = new ReportBuilderForm(_savedEntry))
                    report.ShowDialog(this);
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnFeedbackSkip_Click(object sender, EventArgs e)
        {
            // Skip (and closing the form via X) saves nothing.
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void SaveResult()
        {
            if (_saved) return;
            _saved = true;

            var entry = CollectEntry();
            entry.TimestampUtc = DateTime.UtcNow.ToString("O");
            entry.ResultId = Guid.NewGuid().ToString("N");
            entry.ResultSource = _context.ResultSource;
            entry.LogReference = TestResultLogSnapshot.Preserve(entry.ResultId, _context.SessionLogPath);

            if (entry.LogReference == null && !string.IsNullOrWhiteSpace(_context.ErrorPrefill))
            {
                // The session log could not be preserved - keep the tail
                // inline so the diagnostic text is not lost. The result is
                // still saved either way.
                entry.Error = (entry.Error + "\n\n" + _context.ErrorPrefill).Trim();
                Logger.LogWarning("Could not preserve the session log; the log tail was kept in the result instead.");
            }

            GameFeedbackStore.Append(entry);
            _savedEntry = entry;
            Logger.LogInformation($"Test result saved for {_context.TitleId} ({entry.Status}).");
        }
    }
}
