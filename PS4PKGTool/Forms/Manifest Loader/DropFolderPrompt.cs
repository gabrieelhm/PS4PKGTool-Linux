using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PS4PKGTool.Utilities.PS4PKGToolHelper;

namespace PS4PKGTool
{
    public partial class DropFolderPrompt : DarkUI.Forms.DarkForm
    {
        public bool ScanRecursively { get; private set; } = true;
        public bool AddToDirectories { get; private set; } = false;
        public bool Confirmed { get; private set; } = false;

        public List<string> FolderPaths { get; } = new List<string>();

        public DropFolderPrompt(List<string> folderPaths)
        {
            InitializeComponent();
            FolderPaths = folderPaths;

            darkLabelTitle.Text = folderPaths.Count == 1 ? "Folder Dropped" : $"{folderPaths.Count} Folders Dropped";

            string first = folderPaths[0];
            if (first.Length > 70)
                first = "..." + first.Substring(first.Length - 67);
            darkLabelPath.Text = first;
            darkLabelPath2.Text = folderPaths.Count == 1
                ? Path.GetDirectoryName(first)
                : $"+ {folderPaths.Count - 1} more folder{(folderPaths.Count == 2 ? "" : "s")}";

            chkRecursive.Checked = true;
            chkAddToDirectories.Checked = true;
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            ScanRecursively = chkRecursive.Checked;
            AddToDirectories = chkAddToDirectories.Checked;
            Confirmed = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Confirmed = false;
            this.Close();
        }
    }
}
