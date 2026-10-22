using System;
using System.Windows.Forms;
using PS4PKGTool.Utilities;
using PS4PKGTool.Utilities.PS4PKGToolHelper;

namespace PS4PKGTool
{
    /// <summary>
    /// Read-only look at the Markdown that Copy Markdown would put on the
    /// clipboard. The text is pre-selected so Ctrl+C copies it directly.
    /// </summary>
    public partial class ReportPreviewForm : DarkUI.Forms.DarkForm
    {
        public ReportPreviewForm(string markdown)
        {
            InitializeComponent();
            txtPreviewMarkdown.Text = markdown;
        }

        private void ReportPreviewForm_Shown(object sender, EventArgs e)
        {
            txtPreviewMarkdown.SelectAll();
        }

        private void btnPreviewClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
