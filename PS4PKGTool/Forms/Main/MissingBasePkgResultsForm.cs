using DarkUI.Forms;
using System;
using System.Collections.Generic;
using System.Data;

namespace PS4PKGTool
{
    internal sealed record MissingBasePatch(
        string Filename,
        string Title,
        string TitleId,
        string Version,
        string Directory);

    /// <summary>Read-only report of loaded patch PKGs whose base-game PKG is absent.</summary>
    internal sealed partial class MissingBasePkgResultsForm : DarkForm
    {
        public MissingBasePkgResultsForm(int checkedPatchCount, IReadOnlyList<MissingBasePatch> missingPatches)
        {
            InitializeComponent();
            lblSummary.Text = $"Checked {checkedPatchCount} patch PKG(s). {missingPatches.Count} base PKG(s) missing.";

            var table = new DataTable();
            table.Columns.Add("Status");
            table.Columns.Add("Patch PKG");
            table.Columns.Add("Title");
            table.Columns.Add("Title ID");
            table.Columns.Add("Patch Version");
            table.Columns.Add("Location");
            foreach (var patch in missingPatches)
                table.Rows.Add("Missing base", patch.Filename, patch.Title, patch.TitleId, patch.Version, patch.Directory);

            dgvMissingPatches.DataSource = table;
            dgvMissingPatches.Columns["Status"].FillWeight = 75;
            dgvMissingPatches.Columns["Patch PKG"].FillWeight = 160;
            dgvMissingPatches.Columns["Title"].FillWeight = 150;
            dgvMissingPatches.Columns["Title ID"].FillWeight = 75;
            dgvMissingPatches.Columns["Patch Version"].FillWeight = 85;
            dgvMissingPatches.Columns["Location"].FillWeight = 220;
        }

        private void btnClose_Click(object sender, EventArgs e) => Close();
    }
}
