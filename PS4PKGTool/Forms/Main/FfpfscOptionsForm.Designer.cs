namespace PS4PKGTool.Forms.Main
{
    internal sealed partial class FfpfscOptionsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel _layout;
        private System.Windows.Forms.FlowLayoutPanel _buttonsPanel;
        private DarkUI.Controls.DarkLabel _lblSource;
        private DarkUI.Controls.DarkTextBox _txtSource;
        private DarkUI.Controls.DarkLabel _lblTitle;
        private DarkUI.Controls.DarkTextBox _txtTitle;
        private DarkUI.Controls.DarkLabel _lblTitleId;
        private DarkUI.Controls.DarkTextBox _txtTitleId;
        private DarkUI.Controls.DarkLabel _lblVersion;
        private DarkUI.Controls.DarkTextBox _txtVersion;
        private DarkUI.Controls.DarkLabel _lblOutput;
        private DarkUI.Controls.DarkTextBox _outputPath;
        private DarkUI.Controls.DarkButton _btnBrowseOutput;
        private DarkUI.Controls.DarkLabel _lblWork;
        private DarkUI.Controls.DarkTextBox _workParent;
        private DarkUI.Controls.DarkButton _btnBrowseWork;
        private DarkUI.Controls.DarkCheckBox _validate;
        private DarkUI.Controls.DarkLabel _lblNote;
        private DarkUI.Controls.DarkLabel _lblSpaceWarning;
        private DarkUI.Controls.DarkButton _btnCancel;
        private DarkUI.Controls.DarkButton _btnConvert;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FfpfscOptionsForm));
            _layout = new System.Windows.Forms.TableLayoutPanel();
            _lblSource = new DarkUI.Controls.DarkLabel();
            _txtSource = new DarkUI.Controls.DarkTextBox();
            _lblTitle = new DarkUI.Controls.DarkLabel();
            _txtTitle = new DarkUI.Controls.DarkTextBox();
            _lblTitleId = new DarkUI.Controls.DarkLabel();
            _txtTitleId = new DarkUI.Controls.DarkTextBox();
            _lblVersion = new DarkUI.Controls.DarkLabel();
            _txtVersion = new DarkUI.Controls.DarkTextBox();
            _lblOutput = new DarkUI.Controls.DarkLabel();
            _outputPath = new DarkUI.Controls.DarkTextBox();
            _btnBrowseOutput = new DarkUI.Controls.DarkButton();
            _lblWork = new DarkUI.Controls.DarkLabel();
            _workParent = new DarkUI.Controls.DarkTextBox();
            _btnBrowseWork = new DarkUI.Controls.DarkButton();
            _validate = new DarkUI.Controls.DarkCheckBox();
            _lblSpaceWarning = new DarkUI.Controls.DarkLabel();
            _lblNote = new DarkUI.Controls.DarkLabel();
            _buttonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            _btnCancel = new DarkUI.Controls.DarkButton();
            _btnConvert = new DarkUI.Controls.DarkButton();
            _layout.SuspendLayout();
            _buttonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _layout
            // 
            _layout.ColumnCount = 3;
            _layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 105F));
            _layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            _layout.Controls.Add(_lblSource, 0, 0);
            _layout.Controls.Add(_txtSource, 1, 0);
            _layout.Controls.Add(_lblTitle, 0, 1);
            _layout.Controls.Add(_txtTitle, 1, 1);
            _layout.Controls.Add(_lblTitleId, 0, 2);
            _layout.Controls.Add(_txtTitleId, 1, 2);
            _layout.Controls.Add(_lblVersion, 0, 3);
            _layout.Controls.Add(_txtVersion, 1, 3);
            _layout.Controls.Add(_lblOutput, 0, 4);
            _layout.Controls.Add(_outputPath, 1, 4);
            _layout.Controls.Add(_btnBrowseOutput, 2, 4);
            _layout.Controls.Add(_lblWork, 0, 5);
            _layout.Controls.Add(_workParent, 1, 5);
            _layout.Controls.Add(_btnBrowseWork, 2, 5);
            _layout.Controls.Add(_validate, 1, 6);
            _layout.Controls.Add(_lblSpaceWarning, 0, 7);
            _layout.Controls.Add(_lblNote, 0, 8);
            _layout.Dock = System.Windows.Forms.DockStyle.Fill;
            _layout.Location = new System.Drawing.Point(0, 0);
            _layout.Name = "_layout";
            _layout.Padding = new System.Windows.Forms.Padding(12);
            _layout.RowCount = 9;
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            _layout.Size = new System.Drawing.Size(760, 344);
            _layout.TabIndex = 0;
            // 
            // _lblSource
            // 
            _lblSource.Location = new System.Drawing.Point(12, 19);
            _lblSource.Margin = new System.Windows.Forms.Padding(0, 7, 0, 0);
            _lblSource.Name = "_lblSource";
            _lblSource.Size = new System.Drawing.Size(67, 15);
            _lblSource.TabIndex = 0;
            _lblSource.Text = "Source PKG";
            // 
            // _txtSource
            // 
            _layout.SetColumnSpan(_txtSource, 2);
            _txtSource.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtSource.Location = new System.Drawing.Point(120, 15);
            _txtSource.Name = "_txtSource";
            _txtSource.ReadOnly = true;
            _txtSource.Size = new System.Drawing.Size(625, 23);
            _txtSource.TabIndex = 1;
            // 
            // _lblTitle
            // 
            _lblTitle.Location = new System.Drawing.Point(12, 53);
            _lblTitle.Margin = new System.Windows.Forms.Padding(0, 7, 0, 0);
            _lblTitle.Name = "_lblTitle";
            _lblTitle.Size = new System.Drawing.Size(30, 15);
            _lblTitle.TabIndex = 2;
            _lblTitle.Text = "Title";
            // 
            // _txtTitle
            // 
            _layout.SetColumnSpan(_txtTitle, 2);
            _txtTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtTitle.Location = new System.Drawing.Point(120, 49);
            _txtTitle.Name = "_txtTitle";
            _txtTitle.ReadOnly = true;
            _txtTitle.Size = new System.Drawing.Size(625, 23);
            _txtTitle.TabIndex = 3;
            // 
            // _lblTitleId
            // 
            _lblTitleId.Location = new System.Drawing.Point(12, 87);
            _lblTitleId.Margin = new System.Windows.Forms.Padding(0, 7, 0, 0);
            _lblTitleId.Name = "_lblTitleId";
            _lblTitleId.Size = new System.Drawing.Size(44, 15);
            _lblTitleId.TabIndex = 4;
            _lblTitleId.Text = "Title ID";
            // 
            // _txtTitleId
            // 
            _layout.SetColumnSpan(_txtTitleId, 2);
            _txtTitleId.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtTitleId.Location = new System.Drawing.Point(120, 83);
            _txtTitleId.Name = "_txtTitleId";
            _txtTitleId.ReadOnly = true;
            _txtTitleId.Size = new System.Drawing.Size(625, 23);
            _txtTitleId.TabIndex = 5;
            // 
            // _lblVersion
            // 
            _lblVersion.Location = new System.Drawing.Point(12, 121);
            _lblVersion.Margin = new System.Windows.Forms.Padding(0, 7, 0, 0);
            _lblVersion.Name = "_lblVersion";
            _lblVersion.Size = new System.Drawing.Size(45, 15);
            _lblVersion.TabIndex = 6;
            _lblVersion.Text = "Version";
            // 
            // _txtVersion
            // 
            _layout.SetColumnSpan(_txtVersion, 2);
            _txtVersion.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtVersion.Location = new System.Drawing.Point(120, 117);
            _txtVersion.Name = "_txtVersion";
            _txtVersion.ReadOnly = true;
            _txtVersion.Size = new System.Drawing.Size(625, 23);
            _txtVersion.TabIndex = 7;
            // 
            // _lblOutput
            // 
            _lblOutput.Location = new System.Drawing.Point(12, 155);
            _lblOutput.Margin = new System.Windows.Forms.Padding(0, 7, 0, 0);
            _lblOutput.Name = "_lblOutput";
            _lblOutput.Size = new System.Drawing.Size(45, 15);
            _lblOutput.TabIndex = 8;
            _lblOutput.Text = "Output";
            // 
            // _outputPath
            // 
            _outputPath.Dock = System.Windows.Forms.DockStyle.Fill;
            _outputPath.Location = new System.Drawing.Point(120, 151);
            _outputPath.Name = "_outputPath";
            _outputPath.Size = new System.Drawing.Size(537, 23);
            _outputPath.TabIndex = 0;
            _outputPath.TextChanged += UpdateSpaceWarning;
            // 
            // _btnBrowseOutput
            // 
            _btnBrowseOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            _btnBrowseOutput.Location = new System.Drawing.Point(663, 151);
            _btnBrowseOutput.Name = "_btnBrowseOutput";
            _btnBrowseOutput.Size = new System.Drawing.Size(82, 28);
            _btnBrowseOutput.TabIndex = 1;
            _btnBrowseOutput.Text = "Browse...";
            _btnBrowseOutput.Click += btnBrowseOutput_Click;
            // 
            // _lblWork
            // 
            _lblWork.Location = new System.Drawing.Point(12, 189);
            _lblWork.Margin = new System.Windows.Forms.Padding(0, 7, 0, 0);
            _lblWork.Name = "_lblWork";
            _lblWork.Size = new System.Drawing.Size(81, 15);
            _lblWork.TabIndex = 9;
            _lblWork.Text = "Work location";
            // 
            // _workParent
            // 
            _workParent.Dock = System.Windows.Forms.DockStyle.Fill;
            _workParent.Location = new System.Drawing.Point(120, 185);
            _workParent.Name = "_workParent";
            _workParent.Size = new System.Drawing.Size(537, 23);
            _workParent.TabIndex = 2;
            _workParent.TextChanged += UpdateSpaceWarning;
            // 
            // _btnBrowseWork
            // 
            _btnBrowseWork.Dock = System.Windows.Forms.DockStyle.Fill;
            _btnBrowseWork.Location = new System.Drawing.Point(663, 185);
            _btnBrowseWork.Name = "_btnBrowseWork";
            _btnBrowseWork.Size = new System.Drawing.Size(82, 28);
            _btnBrowseWork.TabIndex = 3;
            _btnBrowseWork.Text = "Browse...";
            _btnBrowseWork.Click += btnBrowseWork_Click;
            // 
            // _validate
            // 
            _validate.AutoSize = true;
            _validate.Checked = true;
            _validate.CheckState = System.Windows.Forms.CheckState.Checked;
            _validate.Location = new System.Drawing.Point(120, 219);
            _validate.Name = "_validate";
            _validate.Size = new System.Drawing.Size(154, 19);
            _validate.TabIndex = 10;
            _validate.Text = "Verify FFPFSC after build";
            // 
            // _lblSpaceWarning
            // 
            _lblSpaceWarning.AutoEllipsis = true;
            _layout.SetColumnSpan(_lblSpaceWarning, 3);
            _lblSpaceWarning.Dock = System.Windows.Forms.DockStyle.Fill;
            _lblSpaceWarning.Location = new System.Drawing.Point(15, 250);
            _lblSpaceWarning.Name = "_lblSpaceWarning";
            _lblSpaceWarning.Size = new System.Drawing.Size(730, 34);
            _lblSpaceWarning.TabIndex = 11;
            _lblSpaceWarning.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblNote
            // 
            _layout.SetColumnSpan(_lblNote, 3);
            _lblNote.Dock = System.Windows.Forms.DockStyle.Fill;
            _lblNote.Location = new System.Drawing.Point(15, 284);
            _lblNote.MaximumSize = new System.Drawing.Size(712, 0);
            _lblNote.Name = "_lblNote";
            _lblNote.Size = new System.Drawing.Size(712, 48);
            _lblNote.TabIndex = 12;
            _lblNote.Text = resources.GetString("_lblNote.Text");
            _lblNote.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _buttonsPanel
            // 
            _buttonsPanel.Controls.Add(_btnCancel);
            _buttonsPanel.Controls.Add(_btnConvert);
            _buttonsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            _buttonsPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _buttonsPanel.Location = new System.Drawing.Point(0, 344);
            _buttonsPanel.Name = "_buttonsPanel";
            _buttonsPanel.Padding = new System.Windows.Forms.Padding(12, 7, 12, 7);
            _buttonsPanel.Size = new System.Drawing.Size(760, 44);
            _buttonsPanel.TabIndex = 1;
            // 
            // _btnCancel
            // 
            _btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            _btnCancel.Location = new System.Drawing.Point(651, 10);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new System.Drawing.Size(82, 30);
            _btnCancel.TabIndex = 0;
            _btnCancel.Text = "Cancel";
            // 
            // _btnConvert
            // 
            _btnConvert.Location = new System.Drawing.Point(549, 10);
            _btnConvert.Name = "_btnConvert";
            _btnConvert.Size = new System.Drawing.Size(96, 30);
            _btnConvert.TabIndex = 1;
            _btnConvert.Text = "Convert";
            _btnConvert.Click += btnConvert_Click;
            // 
            // FfpfscOptionsForm
            // 
            AcceptButton = _btnConvert;
            CancelButton = _btnCancel;
            ClientSize = new System.Drawing.Size(760, 388);
            Controls.Add(_layout);
            Controls.Add(_buttonsPanel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = Properties.Resources.PackageIcon;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FfpfscOptionsForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Convert PS4 PKG to FFPFSC";
            _layout.ResumeLayout(false);
            _layout.PerformLayout();
            _buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

    }
}
