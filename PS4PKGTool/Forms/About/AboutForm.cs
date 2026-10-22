using PS4PKGTool.Utilities.PS4PKGToolHelper;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PS4PKGTool
{
    public partial class AboutForm : DarkUI.Forms.DarkForm
    {
        public AboutForm(string version)
        {
            InitializeComponent();
            lblVersion.Text = "Version " + version;
        }

        private void btnGitHub_Click(object sender, EventArgs e)
        {
            Helper.Tool.OpenWebLink("https://github.com/pearlxcore/PS4-PKG-Tool");
        }

        private void btnKofi_Click(object sender, EventArgs e)
        {
            Helper.Tool.OpenWebLink("https://ko-fi.com/pearlxcore");
        }

        private void btnBug_Click(object sender, EventArgs e)
        {
            Helper.Tool.OpenWebLink("https://github.com/pearlxcore/PS4-PKG-Tool/issues");
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
