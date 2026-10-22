namespace PS4PKGTool
{
    partial class ReportBuilderForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReportBuilderForm));
            lblReportGameLabel = new DarkUI.Controls.DarkLabel();
            lblReportGame = new DarkUI.Controls.DarkLabel();
            lblReportShadps4Label = new DarkUI.Controls.DarkLabel();
            cboReportShadps4 = new DarkUI.Controls.DarkComboBox();
            lblReportShadps4Hint = new DarkUI.Controls.DarkLabel();
            lblReportStatusLabel = new DarkUI.Controls.DarkLabel();
            cboReportStatus = new DarkUI.Controls.DarkComboBox();
            lblReportDescriptionLabel = new DarkUI.Controls.DarkLabel();
            txtReportDescription = new DarkUI.Controls.DarkTextBox();
            lblReportErrorLabel = new DarkUI.Controls.DarkLabel();
            txtReportError = new DarkUI.Controls.DarkTextBox();
            lblReportChecklistLabel = new DarkUI.Controls.DarkLabel();
            chkReportRelease = new DarkUI.Controls.DarkCheckBox();
            chkReportExisting = new DarkUI.Controls.DarkCheckBox();
            chkReportOwnDump = new DarkUI.Controls.DarkCheckBox();
            chkReportFirmware = new DarkUI.Controls.DarkCheckBox();
            chkReportSyncLog = new DarkUI.Controls.DarkCheckBox();
            chkReportDefaultSettings = new DarkUI.Controls.DarkCheckBox();
            lblReportLogLabel = new DarkUI.Controls.DarkLabel();
            lblReportLogFile = new DarkUI.Controls.DarkLabel();
            btnReportOpenLog = new DarkUI.Controls.DarkButton();
            lblReportCopied = new DarkUI.Controls.DarkLabel();
            btnReportPreview = new DarkUI.Controls.DarkButton();
            btnReportCopy = new DarkUI.Controls.DarkButton();
            btnReportCancel = new DarkUI.Controls.DarkButton();
            SuspendLayout();
            // 
            // lblReportGameLabel
            // 
            lblReportGameLabel.Location = new System.Drawing.Point(12, 13);
            lblReportGameLabel.Name = "lblReportGameLabel";
            lblReportGameLabel.Size = new System.Drawing.Size(100, 20);
            lblReportGameLabel.TabIndex = 1;
            lblReportGameLabel.Text = "Game";
            // 
            // lblReportGame
            // 
            lblReportGame.AutoEllipsis = true;
            lblReportGame.Location = new System.Drawing.Point(120, 13);
            lblReportGame.Name = "lblReportGame";
            lblReportGame.Size = new System.Drawing.Size(348, 20);
            lblReportGame.TabIndex = 2;
            lblReportGame.Text = "-";
            // 
            // lblReportShadps4Label
            // 
            lblReportShadps4Label.Location = new System.Drawing.Point(12, 41);
            lblReportShadps4Label.Name = "lblReportShadps4Label";
            lblReportShadps4Label.Size = new System.Drawing.Size(100, 20);
            lblReportShadps4Label.TabIndex = 3;
            lblReportShadps4Label.Text = "shadPS4";
            // 
            // cboReportShadps4
            // 
            cboReportShadps4.Location = new System.Drawing.Point(120, 38);
            cboReportShadps4.Name = "cboReportShadps4";
            cboReportShadps4.Size = new System.Drawing.Size(200, 24);
            cboReportShadps4.TabIndex = 4;
            cboReportShadps4.SelectedIndexChanged += cboReportShadps4_SelectedIndexChanged;
            // 
            // lblReportShadps4Hint
            // 
            lblReportShadps4Hint.AutoEllipsis = true;
            lblReportShadps4Hint.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            lblReportShadps4Hint.Location = new System.Drawing.Point(120, 63);
            lblReportShadps4Hint.Name = "lblReportShadps4Hint";
            lblReportShadps4Hint.Size = new System.Drawing.Size(348, 14);
            lblReportShadps4Hint.TabIndex = 5;
            lblReportShadps4Hint.Text = "Release versions appear after Check for Updates or installing a release build.";
            // 
            // lblReportStatusLabel
            // 
            lblReportStatusLabel.Location = new System.Drawing.Point(12, 85);
            lblReportStatusLabel.Name = "lblReportStatusLabel";
            lblReportStatusLabel.Size = new System.Drawing.Size(100, 20);
            lblReportStatusLabel.TabIndex = 6;
            lblReportStatusLabel.Text = "Status";
            // 
            // cboReportStatus
            // 
            cboReportStatus.Location = new System.Drawing.Point(120, 82);
            cboReportStatus.Items.AddRange(new object[] { "Playable", "In-Game", "Menus", "Boots", "Nothing", "Unknown" });
            cboReportStatus.Name = "cboReportStatus";
            cboReportStatus.Size = new System.Drawing.Size(200, 24);
            cboReportStatus.TabIndex = 7;
            // 
            // lblReportDescriptionLabel
            // 
            lblReportDescriptionLabel.Location = new System.Drawing.Point(12, 113);
            lblReportDescriptionLabel.Name = "lblReportDescriptionLabel";
            lblReportDescriptionLabel.Size = new System.Drawing.Size(200, 16);
            lblReportDescriptionLabel.TabIndex = 8;
            lblReportDescriptionLabel.Text = "Description";
            // 
            // txtReportDescription
            // 
            txtReportDescription.Location = new System.Drawing.Point(12, 133);
            txtReportDescription.Multiline = true;
            txtReportDescription.Name = "txtReportDescription";
            txtReportDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtReportDescription.Size = new System.Drawing.Size(456, 64);
            txtReportDescription.TabIndex = 9;
            // 
            // lblReportErrorLabel
            // 
            lblReportErrorLabel.Location = new System.Drawing.Point(12, 203);
            lblReportErrorLabel.Name = "lblReportErrorLabel";
            lblReportErrorLabel.Size = new System.Drawing.Size(300, 16);
            lblReportErrorLabel.TabIndex = 10;
            lblReportErrorLabel.Text = "Error / relevant log";
            // 
            // txtReportError
            // 
            txtReportError.Location = new System.Drawing.Point(12, 223);
            txtReportError.Multiline = true;
            txtReportError.Name = "txtReportError";
            txtReportError.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtReportError.Size = new System.Drawing.Size(456, 64);
            txtReportError.TabIndex = 11;
            // 
            // lblReportChecklistLabel
            // 
            lblReportChecklistLabel.Location = new System.Drawing.Point(12, 293);
            lblReportChecklistLabel.Name = "lblReportChecklistLabel";
            lblReportChecklistLabel.Size = new System.Drawing.Size(200, 16);
            lblReportChecklistLabel.TabIndex = 12;
            lblReportChecklistLabel.Text = "Checklist";
            // 
            // chkReportRelease
            // 
            chkReportRelease.Checked = true;
            chkReportRelease.CheckState = System.Windows.Forms.CheckState.Checked;
            chkReportRelease.Location = new System.Drawing.Point(24, 315);
            chkReportRelease.Name = "chkReportRelease";
            chkReportRelease.Size = new System.Drawing.Size(444, 20);
            chkReportRelease.TabIndex = 13;
            chkReportRelease.Text = "Tested on required release build";
            // 
            // chkReportExisting
            // 
            chkReportExisting.Checked = true;
            chkReportExisting.CheckState = System.Windows.Forms.CheckState.Checked;
            chkReportExisting.Location = new System.Drawing.Point(24, 337);
            chkReportExisting.Name = "chkReportExisting";
            chkReportExisting.Size = new System.Drawing.Size(444, 20);
            chkReportExisting.TabIndex = 14;
            chkReportExisting.Text = "Checked for an existing report";
            // 
            // chkReportOwnDump
            // 
            chkReportOwnDump.Checked = true;
            chkReportOwnDump.CheckState = System.Windows.Forms.CheckState.Checked;
            chkReportOwnDump.Location = new System.Drawing.Point(24, 359);
            chkReportOwnDump.Name = "chkReportOwnDump";
            chkReportOwnDump.Size = new System.Drawing.Size(444, 20);
            chkReportOwnDump.TabIndex = 15;
            chkReportOwnDump.Text = "Own and unmodified game dump";
            // 
            // chkReportFirmware
            // 
            chkReportFirmware.Checked = true;
            chkReportFirmware.CheckState = System.Windows.Forms.CheckState.Checked;
            chkReportFirmware.Location = new System.Drawing.Point(24, 381);
            chkReportFirmware.Name = "chkReportFirmware";
            chkReportFirmware.Size = new System.Drawing.Size(444, 20);
            chkReportFirmware.TabIndex = 16;
            chkReportFirmware.Text = "Required firmware modules installed";
            // 
            // chkReportSyncLog
            // 
            chkReportSyncLog.Checked = true;
            chkReportSyncLog.CheckState = System.Windows.Forms.CheckState.Checked;
            chkReportSyncLog.Location = new System.Drawing.Point(24, 403);
            chkReportSyncLog.Name = "chkReportSyncLog";
            chkReportSyncLog.Size = new System.Drawing.Size(444, 20);
            chkReportSyncLog.TabIndex = 17;
            chkReportSyncLog.Text = "Sync logging enabled";
            // 
            // chkReportDefaultSettings
            // 
            chkReportDefaultSettings.Checked = true;
            chkReportDefaultSettings.CheckState = System.Windows.Forms.CheckState.Checked;
            chkReportDefaultSettings.Location = new System.Drawing.Point(24, 425);
            chkReportDefaultSettings.Name = "chkReportDefaultSettings";
            chkReportDefaultSettings.Size = new System.Drawing.Size(444, 20);
            chkReportDefaultSettings.TabIndex = 18;
            chkReportDefaultSettings.Text = "Default emulation settings used";
            // 
            // lblReportLogLabel
            // 
            lblReportLogLabel.Location = new System.Drawing.Point(12, 453);
            lblReportLogLabel.Name = "lblReportLogLabel";
            lblReportLogLabel.Size = new System.Drawing.Size(100, 20);
            lblReportLogLabel.TabIndex = 19;
            lblReportLogLabel.Text = "Log";
            // 
            // lblReportLogFile
            // 
            lblReportLogFile.AutoEllipsis = true;
            lblReportLogFile.Location = new System.Drawing.Point(120, 457);
            lblReportLogFile.Name = "lblReportLogFile";
            lblReportLogFile.Size = new System.Drawing.Size(260, 16);
            lblReportLogFile.TabIndex = 20;
            // 
            // btnReportOpenLog
            // 
            btnReportOpenLog.Location = new System.Drawing.Point(388, 452);
            btnReportOpenLog.Name = "btnReportOpenLog";
            btnReportOpenLog.Size = new System.Drawing.Size(80, 26);
            btnReportOpenLog.TabIndex = 21;
            btnReportOpenLog.Text = "Open";
            btnReportOpenLog.Click += btnReportOpenLog_Click;
            // 
            // lblReportCopied
            // 
            lblReportCopied.Location = new System.Drawing.Point(12, 489);
            lblReportCopied.Name = "lblReportCopied";
            lblReportCopied.Size = new System.Drawing.Size(456, 16);
            lblReportCopied.TabIndex = 22;
            // 
            // btnReportPreview
            // 
            btnReportPreview.Location = new System.Drawing.Point(148, 513);
            btnReportPreview.Name = "btnReportPreview";
            btnReportPreview.Size = new System.Drawing.Size(90, 26);
            btnReportPreview.TabIndex = 23;
            btnReportPreview.Text = "Preview";
            btnReportPreview.Click += btnReportPreview_Click;
            // 
            // btnReportCopy
            // 
            btnReportCopy.Location = new System.Drawing.Point(244, 513);
            btnReportCopy.Name = "btnReportCopy";
            btnReportCopy.Size = new System.Drawing.Size(112, 26);
            btnReportCopy.TabIndex = 24;
            btnReportCopy.Text = "Copy Markdown";
            btnReportCopy.Click += btnReportCopy_Click;
            // 
            // btnReportCancel
            // 
            btnReportCancel.Location = new System.Drawing.Point(362, 513);
            btnReportCancel.Name = "btnReportCancel";
            btnReportCancel.Size = new System.Drawing.Size(106, 26);
            btnReportCancel.TabIndex = 25;
            btnReportCancel.Text = "Cancel";
            btnReportCancel.Click += btnReportCancel_Click;
            // 
            // ReportBuilderForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(480, 552);
            Controls.Add(btnReportCancel);
            Controls.Add(btnReportCopy);
            Controls.Add(btnReportPreview);
            Controls.Add(lblReportCopied);
            Controls.Add(btnReportOpenLog);
            Controls.Add(lblReportLogFile);
            Controls.Add(lblReportLogLabel);
            Controls.Add(chkReportDefaultSettings);
            Controls.Add(chkReportSyncLog);
            Controls.Add(chkReportFirmware);
            Controls.Add(chkReportOwnDump);
            Controls.Add(chkReportExisting);
            Controls.Add(chkReportRelease);
            Controls.Add(lblReportChecklistLabel);
            Controls.Add(txtReportError);
            Controls.Add(lblReportErrorLabel);
            Controls.Add(txtReportDescription);
            Controls.Add(lblReportDescriptionLabel);
            Controls.Add(cboReportStatus);
            Controls.Add(lblReportStatusLabel);
            Controls.Add(lblReportShadps4Hint);
            Controls.Add(cboReportShadps4);
            Controls.Add(lblReportShadps4Label);
            Controls.Add(lblReportGame);
            Controls.Add(lblReportGameLabel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = global::PS4PKGTool.Properties.Resources.PackageIcon;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ReportBuilderForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Create Compatibility Report";
            ResumeLayout(false);
            PerformLayout();
        }
        private DarkUI.Controls.DarkLabel lblReportGameLabel;
        private DarkUI.Controls.DarkLabel lblReportGame;
        private DarkUI.Controls.DarkLabel lblReportShadps4Label;
        private DarkUI.Controls.DarkComboBox cboReportShadps4;
        private DarkUI.Controls.DarkLabel lblReportShadps4Hint;
        private DarkUI.Controls.DarkLabel lblReportStatusLabel;
        private DarkUI.Controls.DarkComboBox cboReportStatus;
        private DarkUI.Controls.DarkLabel lblReportDescriptionLabel;
        private DarkUI.Controls.DarkTextBox txtReportDescription;
        private DarkUI.Controls.DarkLabel lblReportErrorLabel;
        private DarkUI.Controls.DarkTextBox txtReportError;
        private DarkUI.Controls.DarkLabel lblReportChecklistLabel;
        private DarkUI.Controls.DarkCheckBox chkReportRelease;
        private DarkUI.Controls.DarkCheckBox chkReportExisting;
        private DarkUI.Controls.DarkCheckBox chkReportOwnDump;
        private DarkUI.Controls.DarkCheckBox chkReportFirmware;
        private DarkUI.Controls.DarkCheckBox chkReportSyncLog;
        private DarkUI.Controls.DarkCheckBox chkReportDefaultSettings;
        private DarkUI.Controls.DarkLabel lblReportLogLabel;
        private DarkUI.Controls.DarkLabel lblReportLogFile;
        private DarkUI.Controls.DarkButton btnReportOpenLog;
        private DarkUI.Controls.DarkLabel lblReportCopied;
        private DarkUI.Controls.DarkButton btnReportPreview;
        private DarkUI.Controls.DarkButton btnReportCopy;
        private DarkUI.Controls.DarkButton btnReportCancel;
    }
}
