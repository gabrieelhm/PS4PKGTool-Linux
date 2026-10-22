namespace PS4PKGTool
{
    internal sealed partial class PkgMergeProgressForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel progressLayout;
        private System.Windows.Forms.FlowLayoutPanel footerPanel;
        private DarkUI.Controls.DarkLabel _stage;
        private DarkUI.Controls.DarkLabel _detail;
        private DarkUI.Controls.DarkLabel _lblCurrent;
        private DarkUI.Controls.DarkLabel _lblOverall;
        private DarkUI.Controls.DarkProgressBar _overallProgress;
        private DarkUI.Controls.DarkProgressBar _currentProgress;
        private DarkUI.Controls.DarkLabel _bytesLabel;
        private DarkUI.Controls.DarkButton _cancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            progressLayout = new System.Windows.Forms.TableLayoutPanel();
            footerPanel = new System.Windows.Forms.FlowLayoutPanel();
            _cancel = new DarkUI.Controls.DarkButton();
            _overallProgress = new DarkUI.Controls.DarkProgressBar();
            _currentProgress = new DarkUI.Controls.DarkProgressBar();
            _detail = new DarkUI.Controls.DarkLabel();
            _stage = new DarkUI.Controls.DarkLabel();
            _lblCurrent = new DarkUI.Controls.DarkLabel();
            _lblOverall = new DarkUI.Controls.DarkLabel();
            _bytesLabel = new DarkUI.Controls.DarkLabel();
            progressLayout.SuspendLayout();
            footerPanel.SuspendLayout();
            SuspendLayout();
            //
            // progressLayout
            //
            progressLayout.ColumnCount = 1;
            progressLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            progressLayout.Location = new System.Drawing.Point(0, 0);
            progressLayout.Name = "progressLayout";
            progressLayout.Padding = new System.Windows.Forms.Padding(12);
            progressLayout.RowCount = 7;
            progressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            progressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            progressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            progressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            progressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            progressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            progressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            progressLayout.Size = new System.Drawing.Size(520, 182);
            progressLayout.TabIndex = 0;
            progressLayout.Controls.Add(_stage, 0, 0);
            progressLayout.Controls.Add(_detail, 0, 1);
            progressLayout.Controls.Add(_lblCurrent, 0, 2);
            progressLayout.Controls.Add(_currentProgress, 0, 3);
            progressLayout.Controls.Add(_lblOverall, 0, 4);
            progressLayout.Controls.Add(_overallProgress, 0, 5);
            progressLayout.Controls.Add(_bytesLabel, 0, 6);
            //
            // _stage
            //
            _stage.Dock = System.Windows.Forms.DockStyle.Fill;
            _stage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            _stage.Location = new System.Drawing.Point(12, 12);
            _stage.Name = "_stage";
            _stage.Size = new System.Drawing.Size(496, 26);
            _stage.TabIndex = 0;
            _stage.Text = "Preparing...";
            _stage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _detail
            //
            _detail.AutoEllipsis = true;
            _detail.Dock = System.Windows.Forms.DockStyle.Fill;
            _detail.ForeColor = System.Drawing.Color.Silver;
            _detail.Location = new System.Drawing.Point(12, 38);
            _detail.Name = "_detail";
            _detail.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
            _detail.Size = new System.Drawing.Size(496, 22);
            _detail.TabIndex = 1;
            //
            // _lblCurrent
            //
            _lblCurrent.Dock = System.Windows.Forms.DockStyle.Fill;
            _lblCurrent.ForeColor = System.Drawing.Color.Silver;
            _lblCurrent.Location = new System.Drawing.Point(12, 60);
            _lblCurrent.Name = "_lblCurrent";
            _lblCurrent.Size = new System.Drawing.Size(496, 26);
            _lblCurrent.TabIndex = 2;
            _lblCurrent.Text = "Current progress";
            _lblCurrent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _lblOverall
            //
            _lblOverall.Dock = System.Windows.Forms.DockStyle.Fill;
            _lblOverall.ForeColor = System.Drawing.Color.Silver;
            _lblOverall.Location = new System.Drawing.Point(12, 108);
            _lblOverall.Name = "_lblOverall";
            _lblOverall.Size = new System.Drawing.Size(496, 26);
            _lblOverall.TabIndex = 4;
            _lblOverall.Text = "Overall progress";
            _lblOverall.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _overallProgress
            //
            _overallProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            _overallProgress.Location = new System.Drawing.Point(12, 134);
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
            // _currentProgress
            //
            _currentProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            _currentProgress.Location = new System.Drawing.Point(12, 86);
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
            // _bytesLabel
            //
            _bytesLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            _bytesLabel.ForeColor = System.Drawing.Color.Silver;
            _bytesLabel.Location = new System.Drawing.Point(12, 156);
            _bytesLabel.Name = "_bytesLabel";
            _bytesLabel.Size = new System.Drawing.Size(496, 14);
            _bytesLabel.TabIndex = 6;
            _bytesLabel.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            //
            // footerPanel
            //
            footerPanel.Controls.Add(_cancel);
            footerPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            footerPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            footerPanel.Location = new System.Drawing.Point(0, 182);
            footerPanel.Name = "footerPanel";
            footerPanel.Padding = new System.Windows.Forms.Padding(12, 7, 12, 7);
            footerPanel.Size = new System.Drawing.Size(520, 44);
            footerPanel.TabIndex = 1;
            //
            // _cancel
            //
            _cancel.Location = new System.Drawing.Point(409, 10);
            _cancel.Name = "_cancel";
            _cancel.Size = new System.Drawing.Size(84, 30);
            _cancel.TabIndex = 0;
            _cancel.Text = "Cancel";
            _cancel.Click += cancel_Click;
            //
            // PkgMergeProgressForm
            //
            ClientSize = new System.Drawing.Size(520, 226);
            ControlBox = false;
            Controls.Add(progressLayout);
            Controls.Add(footerPanel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = Properties.Resources.PackageIcon;
            Name = "PkgMergeProgressForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Merging PKGs";
            Shown += PkgMergeProgressForm_Shown;
            progressLayout.ResumeLayout(false);
            footerPanel.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
