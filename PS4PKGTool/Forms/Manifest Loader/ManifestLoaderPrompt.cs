using PS4PKGTool.Utilities.PS4PKGToolHelper;
using System;
using System.Windows.Forms;

namespace PS4PKGTool
{
    public enum StartupChoice
    {
        Manifest,
        Directory,
        Empty
    }

    public partial class ManifestLoaderPrompt : DarkUI.Forms.DarkForm
    {
        public StartupChoice Choice { get; private set; } = StartupChoice.Empty;

        public ManifestLoaderPrompt(bool manifestAvailable, int manifestEntryCount,
            bool directoriesAvailable, int directoryCount)
        {
            InitializeComponent();

            // Build description text
            string info = "";
            if (manifestAvailable)
                info += $"• Manifest available ({manifestEntryCount} PKGs cached)\r\n";
            else
                info += "• No manifest found\r\n";

            if (directoriesAvailable)
                info += $"• {directoryCount} director{(directoryCount == 1 ? "y" : "ies")} configured\r\n";
            else
                info += "• No directories configured\r\n";

            darkLabelInfo.Text = info.TrimEnd();

            btnLoadFromManifest.Enabled = manifestAvailable;
            btnScanFromDirectory.Enabled = directoriesAvailable;
            btnLaunchEmpty.Enabled = true;
        }

        private void btnLoadFromManifest_Click(object sender, EventArgs e)
        {
            Choice = StartupChoice.Manifest;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnScanFromDirectory_Click(object sender, EventArgs e)
        {
            Choice = StartupChoice.Directory;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnLaunchEmpty_Click(object sender, EventArgs e)
        {
            Choice = StartupChoice.Empty;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
