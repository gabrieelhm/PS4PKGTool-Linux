namespace PS4PKGTool
{
    partial class ReportPreviewForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReportPreviewForm));
            txtPreviewMarkdown = new DarkUI.Controls.DarkTextBox();
            btnPreviewClose = new DarkUI.Controls.DarkButton();
            SuspendLayout();
            // 
            // txtPreviewMarkdown
            // 
            txtPreviewMarkdown.Location = new System.Drawing.Point(12, 12);
            txtPreviewMarkdown.Multiline = true;
            txtPreviewMarkdown.Name = "txtPreviewMarkdown";
            txtPreviewMarkdown.ReadOnly = true;
            txtPreviewMarkdown.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            txtPreviewMarkdown.Size = new System.Drawing.Size(616, 424);
            txtPreviewMarkdown.TabIndex = 0;
            // 
            // btnPreviewClose
            // 
            btnPreviewClose.Location = new System.Drawing.Point(530, 444);
            btnPreviewClose.Name = "btnPreviewClose";
            btnPreviewClose.Size = new System.Drawing.Size(98, 30);
            btnPreviewClose.TabIndex = 1;
            btnPreviewClose.Text = "Close";
            btnPreviewClose.Click += btnPreviewClose_Click;
            // 
            // ReportPreviewForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(640, 486);
            Controls.Add(btnPreviewClose);
            Controls.Add(txtPreviewMarkdown);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = global::PS4PKGTool.Properties.Resources.PackageIcon;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ReportPreviewForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Report Preview";
            Shown += ReportPreviewForm_Shown;
            ResumeLayout(false);
            PerformLayout();
        }

        private DarkUI.Controls.DarkTextBox txtPreviewMarkdown;
        private DarkUI.Controls.DarkButton btnPreviewClose;
    }
}
