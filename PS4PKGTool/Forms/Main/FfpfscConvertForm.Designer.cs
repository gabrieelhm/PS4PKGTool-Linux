namespace PS4PKGTool.Forms.Main
{
    internal sealed partial class FfpfscConvertForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel _layout;
        private System.Windows.Forms.FlowLayoutPanel _buttonsPanel;
        private DarkUI.Controls.DarkLabel lblStage;
        private DarkUI.Controls.DarkLabel lblDetail;
        private DarkUI.Controls.DarkLabel lblCurrent;
        private DarkUI.Controls.DarkLabel lblOverall;
        private DarkUI.Controls.DarkProgressBar _overallProgress;
        private DarkUI.Controls.DarkProgressBar _currentProgress;
        private DarkUI.Controls.DarkLabel lblBytes;
        private DarkUI.Controls.DarkButton btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            _layout = new System.Windows.Forms.TableLayoutPanel();
            lblStage = new DarkUI.Controls.DarkLabel();
            lblDetail = new DarkUI.Controls.DarkLabel();
            lblCurrent = new DarkUI.Controls.DarkLabel();
            lblOverall = new DarkUI.Controls.DarkLabel();
            _overallProgress = new DarkUI.Controls.DarkProgressBar();
            _currentProgress = new DarkUI.Controls.DarkProgressBar();
            lblBytes = new DarkUI.Controls.DarkLabel();
            _buttonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            btnCancel = new DarkUI.Controls.DarkButton();
            _layout.SuspendLayout();
            _buttonsPanel.SuspendLayout();
            SuspendLayout();
            //
            // _layout
            //
            _layout.ColumnCount = 1;
            _layout.Controls.Add(lblStage, 0, 0);
            _layout.Controls.Add(lblDetail, 0, 1);
            _layout.Controls.Add(lblCurrent, 0, 2);
            _layout.Controls.Add(_currentProgress, 0, 3);
            _layout.Controls.Add(lblOverall, 0, 4);
            _layout.Controls.Add(_overallProgress, 0, 5);
            _layout.Controls.Add(lblBytes, 0, 6);
            _layout.Dock = System.Windows.Forms.DockStyle.Fill;
            _layout.Name = "_layout";
            _layout.Padding = new System.Windows.Forms.Padding(12);
            _layout.RowCount = 7;
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            _layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _layout.TabIndex = 0;
            //
            // lblStage
            //
            lblStage.Dock = System.Windows.Forms.DockStyle.Fill;
            lblStage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblStage.Name = "lblStage";
            lblStage.Size = new System.Drawing.Size(496, 26);
            lblStage.TabIndex = 0;
            lblStage.Text = "Preparing...";
            lblStage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblDetail
            //
            lblDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            lblDetail.ForeColor = System.Drawing.Color.Silver;
            lblDetail.Name = "lblDetail";
            lblDetail.Size = new System.Drawing.Size(496, 22);
            lblDetail.TabIndex = 1;
            lblDetail.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblCurrent
            //
            lblCurrent.Dock = System.Windows.Forms.DockStyle.Fill;
            lblCurrent.ForeColor = System.Drawing.Color.Silver;
            lblCurrent.Name = "lblCurrent";
            lblCurrent.Size = new System.Drawing.Size(496, 26);
            lblCurrent.TabIndex = 2;
            lblCurrent.Text = "Current progress";
            lblCurrent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _currentProgress
            //
            _currentProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            _currentProgress.Marquee = true;
            _currentProgress.MarqueeAnimationSpeed = 25;
            _currentProgress.Maximum = 1;
            _currentProgress.Minimum = 0;
            _currentProgress.Name = "_currentProgress";
            _currentProgress.Size = new System.Drawing.Size(496, 22);
            _currentProgress.TabIndex = 3;
            _currentProgress.TextMode = DarkUI.Controls.DarkProgressBarMode.NoText;
            _currentProgress.Value = 0;
            //
            // lblOverall
            //
            lblOverall.Dock = System.Windows.Forms.DockStyle.Fill;
            lblOverall.ForeColor = System.Drawing.Color.Silver;
            lblOverall.Name = "lblOverall";
            lblOverall.Size = new System.Drawing.Size(496, 26);
            lblOverall.TabIndex = 4;
            lblOverall.Text = "Overall progress";
            lblOverall.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _overallProgress
            //
            _overallProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            _overallProgress.Marquee = true;
            _overallProgress.MarqueeAnimationSpeed = 25;
            _overallProgress.Maximum = 1;
            _overallProgress.Minimum = 0;
            _overallProgress.Name = "_overallProgress";
            _overallProgress.Size = new System.Drawing.Size(496, 22);
            _overallProgress.TabIndex = 5;
            _overallProgress.TextMode = DarkUI.Controls.DarkProgressBarMode.NoText;
            _overallProgress.Value = 0;
            //
            // lblBytes
            //
            lblBytes.Dock = System.Windows.Forms.DockStyle.Fill;
            lblBytes.ForeColor = System.Drawing.Color.Silver;
            lblBytes.Name = "lblBytes";
            lblBytes.Size = new System.Drawing.Size(496, 14);
            lblBytes.TabIndex = 6;
            lblBytes.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            //
            // _buttonsPanel
            //
            _buttonsPanel.Controls.Add(btnCancel);
            _buttonsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            _buttonsPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _buttonsPanel.Name = "_buttonsPanel";
            _buttonsPanel.Padding = new System.Windows.Forms.Padding(12, 7, 12, 7);
            _buttonsPanel.Size = new System.Drawing.Size(520, 44);
            _buttonsPanel.TabIndex = 1;
            //
            // btnCancel
            //
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(82, 30);
            btnCancel.TabIndex = 0;
            btnCancel.Text = "Cancel";
            btnCancel.Click += btnCancel_Click;
            //
            // FfpfscConvertForm
            //
            AcceptButton = null;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(520, 226);
            Controls.Add(_layout);
            Controls.Add(_buttonsPanel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = global::PS4PKGTool.Properties.Resources.PackageIcon;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FfpfscConvertForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Converting to FFPFSC";
            Shown += FfpfscConvertForm_Shown;
            _layout.ResumeLayout(false);
            _layout.PerformLayout();
            _buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
