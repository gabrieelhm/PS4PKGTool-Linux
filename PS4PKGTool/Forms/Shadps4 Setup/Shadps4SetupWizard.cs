using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PS4PKGTool.Utilities.PS4PKGToolHelper;
using PS4PKGTool.Utilities.Settings;
using PS4PKGTool.Utilities.Shadps4;
using DarkUI.Forms;

namespace PS4PKGTool
{
    /// <summary>
    /// First-run shadPS4 setup wizard: install the recommended setup (stable
    /// core + QtLauncher), choose a specific core version, or adopt an
    /// existing installation. Managed builds go into versioned folders and
    /// the wizard NEVER writes shadPS4's own config - it only sets the tool's
    /// active core/launcher after a successful install. Existing-install
    /// users never see this flow (they use Settings).
    /// </summary>
    public partial class Shadps4SetupWizard : DarkForm
    {
        private readonly AppSettings _settings;
        private Shadps4ManagedBuilds _store;
        private CancellationTokenSource? _installCts;
        private Shadps4InstalledBuild? _installedCore;
        private Shadps4InstalledBuild? _installedLauncher;

        public Shadps4SetupWizard(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            _store = new Shadps4ManagedBuilds(_settings.Shadps4ManagedRoot);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            lblWizardIntro.Text = "shadPS4 is not configured.\n\n" +
                "PS4 PKG Tool can install shadPS4 for you - the core (shadPS4.exe) launches " +
                "games directly from this tool, and the optional QtLauncher is where you " +
                "configure graphics, controllers, audio, cheats and patches.\n\n" +
                "If you already have shadPS4, choose Use Existing Installation.";
        }

        private void ShowStep(int step)
        {
            panelStep1.Visible = step == 1;
            panelStep2.Visible = step == 2;
            panelStep3.Visible = step == 3;
            panelStep4.Visible = step == 4;
            if (step == 2) PrefillStep2();
        }

        /// <summary>Whether the user already made an install-directory choice in step 2.</summary>
        private bool _installDirApplied;

        /// <summary>Prefills the install directory with the managed-root default when nothing is set yet.</summary>
        private void PrefillStep2()
        {
            if (string.IsNullOrWhiteSpace(tbInstallDir.Text)
                && !string.IsNullOrWhiteSpace(tbManagedRoot.Text))
            {
                tbInstallDir.Text = Path.Combine(tbManagedRoot.Text.Trim(), "Data");
            }
            _installDirApplied = false;
        }

        // ── step 1 ──

        private async void btnWizardRecommended_Click(object sender, EventArgs e)
        {
            btnWizardRecommended.Enabled = false;
            try
            {
                var feed = new Shadps4ReleaseFeed();
                var rec = await new Shadps4RecommendationService(feed).GetRecommendedAsync();
                if (rec.Core == null)
                {
                    var choice = AppMessageBox.Show("shadPS4 Setup",
                        "The recommended shadPS4 core could not be determined right now.\n\n" +
                        "Open the official download page instead?",
                        AppMessageType.Warning, AppMessageButtons.YesNo);
                    if (choice == DialogResult.Yes) Helper.Tool.OpenWebLink("https://github.com/shadps4-emu/shadPS4/releases");
                    return;
                }
                tbManagedRoot.Text = ManagedRootOrDefault();
                ShowStep(2);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowWarning("Could not load the recommended shadPS4 setup: " + ex.Message, false);
            }
            finally
            {
                btnWizardRecommended.Enabled = true;
            }
        }

        private async void btnWizardChooseVersion_Click(object sender, EventArgs e)
        {
            btnWizardChooseVersion.Enabled = false;
            try
            {
                var feed = new Shadps4ReleaseFeed();
                var stable = await feed.GetReleasesAsync(Shadps4FeedKind.CoreStable);
                var nightly = await feed.GetReleasesAsync(Shadps4FeedKind.CoreNightly);
                var releases = new List<Shadps4ReleaseInfo>();
                if (stable.Releases != null) releases.AddRange(stable.Releases);
                if (nightly.Releases != null) releases.AddRange(nightly.Releases);
                releases = releases.OrderByDescending(r => r.PublishedUtc).ToList();

                if (releases.Count == 0)
                {
                    MessageBoxHelper.ShowWarning("No core versions could be loaded.\n\n" + (stable.Error ?? nightly.Error ?? ""), false);
                    return;
                }

                using var picker = new Shadps4VersionPicker(releases);
                if (picker.ShowDialog(this) != DialogResult.OK || picker.Selected == null) return;
                _pendingRelease = picker.Selected;
                tbManagedRoot.Text = ManagedRootOrDefault();
                ShowStep(2);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowWarning("Could not load core versions: " + ex.Message, false);
            }
            finally
            {
                btnWizardChooseVersion.Enabled = true;
            }
        }

        private Shadps4ReleaseInfo? _pendingRelease;

        private void btnWizardUseExisting_Click(object sender, EventArgs e)
        {
            // Close with a marker: the caller opens the shadPS4 Manager's
            // Settings tab (adoption and detection live there now).
            Tag = "use-existing";
            Close();
        }

        private void btnWizardCancel1_Click(object sender, EventArgs e)
        {
            Close();
        }

        // ── step 2 ──

        private string ManagedRootOrDefault()
        {
            string configured = _settings.Shadps4ManagedRoot?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(configured)) return configured;
            return Shadps4ManagedBuilds.DefaultRootPath();
        }

        private void btnBrowseRoot_Click(object sender, EventArgs e)
        {
            string previous = tbManagedRoot.Text.Trim();
            using var fbd = new FolderBrowserDialog
            {
                Description = "Choose the managed shadPS4 builds folder",
                ShowNewFolderButton = true,
            };
            if (Directory.Exists(previous)) fbd.SelectedPath = previous;
            if (fbd.ShowDialog() != DialogResult.OK) return;
            tbManagedRoot.Text = fbd.SelectedPath;

            // Follow the root with the default install dir, but only while
            // the field still holds the previous default (or is empty) - a
            // manual choice is never overwritten.
            string current = tbInstallDir.Text.Trim();
            string oldDefault = Path.Combine(previous, "Data");
            if (string.IsNullOrWhiteSpace(current)
                || string.Equals(current, oldDefault, StringComparison.OrdinalIgnoreCase))
            {
                tbInstallDir.Text = Path.Combine(tbManagedRoot.Text.Trim(), "Data");
            }
        }

        private void btnBrowseInstallDir_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Choose where PS4 PKG Tool installs games for shadPS4",
                ShowNewFolderButton = true,
            };
            if (Directory.Exists(tbInstallDir.Text.Trim())) fbd.SelectedPath = tbInstallDir.Text.Trim();
            if (fbd.ShowDialog() != DialogResult.OK) return;
            tbInstallDir.Text = fbd.SelectedPath;
        }

        private void btnBack2_Click(object sender, EventArgs e)
        {
            ShowStep(1);
        }

        private async void btnNext2_Click(object sender, EventArgs e)
        {
            string root = tbManagedRoot.Text.Trim();
            if (string.IsNullOrWhiteSpace(root))
            {
                MessageBoxHelper.ShowWarning("Choose a managed builds folder first.", false);
                return;
            }
            try
            {
                Directory.CreateDirectory(root);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowWarning("The folder is not usable: " + ex.Message, false);
                return;
            }

            try
            {
                // Managed root is chosen once - persist it.
                _settings.Shadps4ManagedRoot = root;
                // The install directory choice from this step (may be empty to
                // leave it unset - the post-install default is then skipped).
                _installDirApplied = true;
                string installDir = tbInstallDir.Text.Trim();
                _settings.Shadps4InstallDirectory = installDir;
                Logger.LogInformation($"Shadps4Wizard: install dir chosen = '{installDir}'");
                if (!string.IsNullOrWhiteSpace(installDir))
                {
                    try { Directory.CreateDirectory(installDir); }
                    catch (Exception ex) { Logger.LogWarning("Wizard: install dir could not be created: " + ex.Message); }
                }
                SettingsManager.SaveSettings(_settings, SettingsManager.SettingFilePath);
                _store = new Shadps4ManagedBuilds(root);

                btnNext2.Enabled = false;
                ShowStep(3);
                await RunInstallAsync();
            }
            catch (Exception ex)
            {
                // RunInstallAsync handles its own failures; this guards the
                // settings/save path so a bad disk state can never crash the app.
                Logger.LogError("shadPS4 wizard could not start the install", ex);
                MessageBoxHelper.ShowWarning("The setup could not continue: " + ex.Message, false);
                btnNext2.Enabled = true;
                ShowStep(2);
            }
        }

        // ── step 3: install ──

        private async Task RunInstallAsync()
        {
            _installCts?.Dispose();
            _installCts = new CancellationTokenSource();
            pbInstall.Style = ProgressBarStyle.Marquee;
            pbInstall.Visible = true;
            lblProgress.Text = "Preparing...";

            try
            {
                Shadps4ReleaseInfo? coreRelease = null;
                Shadps4ReleaseInfo? launcherRelease = null;

                if (_pendingRelease != null)
                {
                    coreRelease = _pendingRelease;
                }
                else
                {
                    var rec = await new Shadps4RecommendationService(new Shadps4ReleaseFeed()).GetRecommendedAsync();
                    coreRelease = rec.Core;
                    if (chkInstallLauncher.Checked) launcherRelease = rec.QtLauncher;
                }

                var svc = new Shadps4SetupService();
                var progress = new Progress<string>(s => lblProgress.Text = s);

                if (coreRelease != null)
                {
                    var coreResult = await svc.InstallBuildAsync(coreRelease, _store, progress, _installCts.Token);
                    if (coreResult.Status == Shadps4SetupStatus.Cancelled) { ShowStep(2); return; }
                    if (coreResult.Status != Shadps4SetupStatus.Success)
                    {
                        lblProgress.Text = coreResult.Message;
                        pbInstall.Visible = false;
                        MessageBoxHelper.ShowWarning(coreResult.Message, false);
                        ShowStep(2);
                        return;
                    }
                    _installedCore = coreResult.Build;
                }

                if (launcherRelease != null)
                {
                    var launcherResult = await svc.InstallBuildAsync(launcherRelease, _store, progress, _installCts.Token);
                    if (launcherResult.Status == Shadps4SetupStatus.Cancelled) { ShowStep(2); return; }
                    if (launcherResult.Status != Shadps4SetupStatus.Success)
                    {
                        lblProgress.Text = launcherResult.Message;
                        MessageBoxHelper.ShowWarning(
                            "The core installed, but the QtLauncher failed:\n\n" + launcherResult.Message, false);
                    }
                    else
                    {
                        _installedLauncher = launcherResult.Build;
                    }
                }

                // Only now - after success - touch the active settings.
                if (_installedCore != null)
                {
                    _settings.Shadps4ActiveCore = Shadps4ActiveCore.ForManaged(_installedCore.BuildId);
                }
                if (_installedLauncher != null)
                {
                    _settings.Shadps4ActiveLauncher = Shadps4ActiveCore.ForManaged(_installedLauncher.BuildId);
                }
                // First-run default install target (only when step 2 did not
                // already apply a choice): a fresh shadPS4 has no config.json
                // yet (created on its first launch), so nothing can auto-fill
                // it. shadPS4's config is never written; the user can add the
                // folder in QtLauncher later.
                if (_installedCore != null && !_installDirApplied
                    && string.IsNullOrWhiteSpace(_settings.Shadps4InstallDirectory))
                {
                    string defaultInstallDir = Path.Combine(_settings.Shadps4ManagedRoot, "Data");
                    try { Directory.CreateDirectory(defaultInstallDir); }
                    catch (Exception ex) { Logger.LogWarning("Wizard: default install dir could not be created: " + ex.Message); }
                    _settings.Shadps4InstallDirectory = defaultInstallDir;
                }
                SettingsManager.SaveSettings(_settings, SettingsManager.SettingFilePath);

                pbInstall.Visible = false;
                FillCompletion();
                ShowStep(4);
            }
            catch (OperationCanceledException)
            {
                ShowStep(2);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowWarning("Installation failed: " + ex.Message, false);
                ShowStep(2);
            }
            finally
            {
                btnNext2.Enabled = true;
            }
        }

        private void btnCancelInstall_Click(object sender, EventArgs e)
        {
            _installCts?.Cancel();
            lblProgress.Text = "Cancelling...";
        }

        // ── step 4 ──

        private void FillCompletion()
        {
            var env = Shadps4EnvironmentResolver.Resolve(_settings.Shadps4ExecutablePath);
            string configState = env.IsValid
                ? (env.ConfigPath ?? "configuration detected")
                : "Not initialized (start shadPS4 once and it is created)";

            string libs = env.InstallDirectories.Count == 0
                ? "Not configured - set up game folders in QtLauncher"
                : string.Join(" | ", env.InstallDirectories);

            string installTarget = string.IsNullOrWhiteSpace(_settings.Shadps4InstallDirectory)
                ? "(not set)"
                : _settings.Shadps4InstallDirectory;

            string compatibility = "Unknown";
            string compNote = "The upstream project does not publish a compatibility mapping between core and launcher builds.";

            lblComplete.Text =
                "shadPS4 Setup Complete\n\n" +
                $"Active Core:\n{(_installedCore?.ExecutablePath ?? "(none)")}\n" +
                $"Build: {(_installedCore?.DisplayName ?? "-")}\n\n" +
                $"QtLauncher:\n{(_installedLauncher?.ExecutablePath ?? "Not installed")}\n" +
                $"Build: {(_installedLauncher?.DisplayName ?? "-")}\n\n" +
                $"Configuration: {configState}\n" +
                $"Game libraries: {libs}\n" +
                $"PS4 PKG Tool install target: {installTarget}\n\n" +
                $"Direct Play: {(_installedCore != null ? "Ready" : "Not ready - a core is required")}\n" +
                $"Core/Launcher compatibility: {compatibility} ({compNote})\n\n" +
                "Emulator settings, cheats and patches: configure using QtLauncher.";
        }

        private void btnOpenLauncher4_Click(object sender, EventArgs e)
        {
            var store = new Shadps4ManagedBuilds(_settings.Shadps4ManagedRoot);
            string? path = Shadps4ActiveCore.ResolveExecutable(
                _settings.Shadps4ActiveLauncher, store.ResolveManagedExecutable, out _);
            if (path != null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(path) ?? "",
                    });
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowWarning("Failed to open the QtLauncher: " + ex.Message, false);
                }
            }
        }

        private void btnDone_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
