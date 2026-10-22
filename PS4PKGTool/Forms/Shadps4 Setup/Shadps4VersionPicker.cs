using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PS4PKGTool.Utilities.PS4PKGToolHelper;
using PS4PKGTool.Utilities.Shadps4;
using DarkUI.Forms;

namespace PS4PKGTool
{
    /// <summary>
    /// Lists real upstream release metadata (date, tag/commit, asset) for the
    /// user to pick a specific shadPS4 core version. Version numbers are the
    /// actual upstream ones - never invented.
    /// </summary>
    public partial class Shadps4VersionPicker : DarkForm
    {
        private readonly List<Shadps4ReleaseInfo> _releases;

        public Shadps4VersionPicker(IReadOnlyList<Shadps4ReleaseInfo> releases)
        {
            InitializeComponent();
            _releases = new List<Shadps4ReleaseInfo>(releases);
            foreach (var r in _releases)
            {
                string channel = r.Feed == Shadps4FeedKind.CoreStable ? "Stable" : "Nightly";
                string commit = string.IsNullOrWhiteSpace(r.Commit) ? r.Tag : r.Commit;
                lstVersions.Items.Add($"{channel}  {r.PublishedUtc:yyyy-MM-dd}  {commit}  ({r.AssetName})");
            }
            if (lstVersions.Items.Count > 0) lstVersions.SelectedIndex = 0;
        }

        /// <summary>The release the user picked, or null.</summary>
        public Shadps4ReleaseInfo? Selected { get; private set; }

        private void btnOk_Click(object sender, EventArgs e)
        {
            int idx = lstVersions.SelectedIndex;
            if (idx < 0 || idx >= _releases.Count) return;
            Selected = _releases[idx];
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
