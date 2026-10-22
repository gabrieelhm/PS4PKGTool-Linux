namespace PS4PKGTool
{
    partial class GameFeedbackViewer
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameFeedbackViewer));
            darkLabelViewerHeader = new DarkUI.Controls.DarkLabel();
            lstFeedbackEntries = new DarkUI.Controls.DarkListBox(components);
            lblFeedbackViewerDetails = new DarkUI.Controls.DarkLabel();
            btnFeedbackCopyReport = new DarkUI.Controls.DarkButton();
            btnFeedbackSetBest = new DarkUI.Controls.DarkButton();
            btnFeedbackClose = new DarkUI.Controls.DarkButton();
            SuspendLayout();
            // 
            // darkLabelViewerHeader
            // 
            darkLabelViewerHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            darkLabelViewerHeader.Location = new System.Drawing.Point(12, 10);
            darkLabelViewerHeader.Name = "darkLabelViewerHeader";
            darkLabelViewerHeader.Size = new System.Drawing.Size(300, 22);
            darkLabelViewerHeader.TabIndex = 0;
            darkLabelViewerHeader.Text = "Test History";
            //
            // lstFeedbackEntries
            //
            lstFeedbackEntries.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lstFeedbackEntries.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            lstFeedbackEntries.ItemHeight = 18;
            lstFeedbackEntries.Location = new System.Drawing.Point(12, 36);
            lstFeedbackEntries.Name = "lstFeedbackEntries";
            lstFeedbackEntries.Size = new System.Drawing.Size(436, 118);
            lstFeedbackEntries.TabIndex = 1;
            lstFeedbackEntries.SelectedIndexChanged += lstFeedbackEntries_SelectedIndexChanged;
            //
            // lblFeedbackViewerDetails
            //
            lblFeedbackViewerDetails.Location = new System.Drawing.Point(12, 162);
            lblFeedbackViewerDetails.Name = "lblFeedbackViewerDetails";
            lblFeedbackViewerDetails.Size = new System.Drawing.Size(436, 166);
            lblFeedbackViewerDetails.TabIndex = 2;
            //
            // btnFeedbackCopyReport
            //
            btnFeedbackCopyReport.Location = new System.Drawing.Point(64, 340);
            btnFeedbackCopyReport.Name = "btnFeedbackCopyReport";
            btnFeedbackCopyReport.Size = new System.Drawing.Size(180, 30);
            btnFeedbackCopyReport.TabIndex = 5;
            btnFeedbackCopyReport.Text = "Create Compatibility Report";
            btnFeedbackCopyReport.Click += btnFeedbackCopyReport_Click;
            //
            // btnFeedbackSetBest
            //
            btnFeedbackSetBest.Location = new System.Drawing.Point(252, 340);
            btnFeedbackSetBest.Name = "btnFeedbackSetBest";
            btnFeedbackSetBest.Size = new System.Drawing.Size(108, 30);
            btnFeedbackSetBest.TabIndex = 3;
            btnFeedbackSetBest.Text = "Set as Pinned";
            btnFeedbackSetBest.Click += btnFeedbackSetBest_Click;
            //
            // btnFeedbackClose
            //
            btnFeedbackClose.Location = new System.Drawing.Point(370, 340);
            btnFeedbackClose.Name = "btnFeedbackClose";
            btnFeedbackClose.Size = new System.Drawing.Size(70, 30);
            btnFeedbackClose.TabIndex = 4;
            btnFeedbackClose.Text = "Close";
            btnFeedbackClose.Click += btnFeedbackClose_Click;
            //
            // GameFeedbackViewer
            //
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(460, 382);
            Controls.Add(btnFeedbackClose);
            Controls.Add(btnFeedbackSetBest);
            Controls.Add(btnFeedbackCopyReport);
            Controls.Add(lblFeedbackViewerDetails);
            Controls.Add(lstFeedbackEntries);
            Controls.Add(darkLabelViewerHeader);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = global::PS4PKGTool.Properties.Resources.PackageIcon;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "GameFeedbackViewer";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Test History";
            ResumeLayout(false);
        }

        private DarkUI.Controls.DarkLabel darkLabelViewerHeader;
        private DarkUI.Controls.DarkListBox lstFeedbackEntries;
        private DarkUI.Controls.DarkLabel lblFeedbackViewerDetails;
        private DarkUI.Controls.DarkButton btnFeedbackCopyReport;
        private DarkUI.Controls.DarkButton btnFeedbackSetBest;
        private DarkUI.Controls.DarkButton btnFeedbackClose;
    }
}
