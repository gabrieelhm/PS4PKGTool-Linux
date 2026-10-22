namespace PS4PKGTool
{
    internal sealed partial class MissingBasePkgResultsForm
    {
        private System.ComponentModel.IContainer components;
        private DarkUI.Controls.DarkLabel lblSummary;
        private DarkUI.Controls.DarkDataGridView dgvMissingPatches;
        private DarkUI.Controls.DarkFooterBar footerBar;
        private DarkUI.Controls.DarkButton btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lblSummary = new DarkUI.Controls.DarkLabel();
            dgvMissingPatches = new DarkUI.Controls.DarkDataGridView();
            footerBar = new DarkUI.Controls.DarkFooterBar();
            btnClose = new DarkUI.Controls.DarkButton();
            ((System.ComponentModel.ISupportInitialize)dgvMissingPatches).BeginInit();
            footerBar.SuspendLayout();
            SuspendLayout();
            // 
            // lblSummary
            // 
            lblSummary.Dock = System.Windows.Forms.DockStyle.Top;
            lblSummary.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblSummary.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            lblSummary.Location = new System.Drawing.Point(0, 0);
            lblSummary.Name = "lblSummary";
            lblSummary.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            lblSummary.Size = new System.Drawing.Size(1034, 42);
            lblSummary.TabIndex = 0;
            lblSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dgvMissingPatches
            // 
            dgvMissingPatches.AllowUserToAddRows = false;
            dgvMissingPatches.AllowUserToDeleteRows = false;
            dgvMissingPatches.AllowUserToResizeRows = false;
            dgvMissingPatches.AutoGenerateColumns = true;
            dgvMissingPatches.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvMissingPatches.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvMissingPatches.Location = new System.Drawing.Point(0, 42);
            dgvMissingPatches.MultiSelect = false;
            dgvMissingPatches.Name = "dgvMissingPatches";
            dgvMissingPatches.ReadOnly = true;
            dgvMissingPatches.RowHeadersVisible = false;
            dgvMissingPatches.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvMissingPatches.Size = new System.Drawing.Size(1034, 397);
            dgvMissingPatches.TabIndex = 1;
            // 
            // footerBar
            // 
            footerBar.Controls.Add(btnClose);
            footerBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            footerBar.Location = new System.Drawing.Point(0, 439);
            footerBar.Name = "footerBar";
            footerBar.Size = new System.Drawing.Size(1034, 42);
            footerBar.TabIndex = 2;
            // 
            // btnClose
            // 
            btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnClose.Location = new System.Drawing.Point(942, 7);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(80, 30);
            btnClose.TabIndex = 0;
            btnClose.Text = "Close";
            btnClose.Click += btnClose_Click;
            // 
            // MissingBasePkgResultsForm
            // 
            ClientSize = new System.Drawing.Size(1034, 481);
            Controls.Add(dgvMissingPatches);
            Controls.Add(footerBar);
            Controls.Add(lblSummary);
            Icon = global::PS4PKGTool.Properties.Resources.PackageIcon;
            MinimumSize = new System.Drawing.Size(760, 360);
            Name = "MissingBasePkgResultsForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Patches Missing Base PKG";
            ((System.ComponentModel.ISupportInitialize)dgvMissingPatches).EndInit();
            footerBar.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
