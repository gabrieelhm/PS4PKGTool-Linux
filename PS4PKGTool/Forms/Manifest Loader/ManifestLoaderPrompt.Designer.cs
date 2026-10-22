using System;

namespace PS4PKGTool
{
    partial class ManifestLoaderPrompt
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ManifestLoaderPrompt));
            darkLabelInfo = new DarkUI.Controls.DarkLabel();
            darkLabelTitle = new DarkUI.Controls.DarkLabel();
            btnLoadFromManifest = new DarkUI.Controls.DarkButton();
            btnScanFromDirectory = new DarkUI.Controls.DarkButton();
            btnLaunchEmpty = new DarkUI.Controls.DarkButton();
            SuspendLayout();
            // 
            // darkLabelInfo
            // 
            darkLabelInfo.Font = new System.Drawing.Font("Segoe UI", 9F);
            darkLabelInfo.ForeColor = System.Drawing.Color.Gainsboro;
            darkLabelInfo.Location = new System.Drawing.Point(20, 40);
            darkLabelInfo.Name = "darkLabelInfo";
            darkLabelInfo.Size = new System.Drawing.Size(380, 55);
            darkLabelInfo.TabIndex = 1;
            // 
            // darkLabelTitle
            // 
            darkLabelTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            darkLabelTitle.ForeColor = System.Drawing.Color.Gainsboro;
            darkLabelTitle.Location = new System.Drawing.Point(20, 15);
            darkLabelTitle.Name = "darkLabelTitle";
            darkLabelTitle.Size = new System.Drawing.Size(380, 22);
            darkLabelTitle.TabIndex = 0;
            darkLabelTitle.Text = "Welcome to PS4 PKG Tool";
            // 
            // btnLoadFromManifest
            // 
            btnLoadFromManifest.Font = new System.Drawing.Font("Segoe UI", 9F);
            btnLoadFromManifest.Location = new System.Drawing.Point(23, 103);
            btnLoadFromManifest.Name = "btnLoadFromManifest";
            btnLoadFromManifest.Size = new System.Drawing.Size(115, 32);
            btnLoadFromManifest.TabIndex = 2;
            btnLoadFromManifest.Text = "Load Manifest";
            btnLoadFromManifest.Click += btnLoadFromManifest_Click;
            // 
            // btnScanFromDirectory
            // 
            btnScanFromDirectory.Font = new System.Drawing.Font("Segoe UI", 9F);
            btnScanFromDirectory.Location = new System.Drawing.Point(152, 103);
            btnScanFromDirectory.Name = "btnScanFromDirectory";
            btnScanFromDirectory.Size = new System.Drawing.Size(115, 32);
            btnScanFromDirectory.TabIndex = 3;
            btnScanFromDirectory.Text = "Scan Directory";
            btnScanFromDirectory.Click += btnScanFromDirectory_Click;
            // 
            // btnLaunchEmpty
            // 
            btnLaunchEmpty.Font = new System.Drawing.Font("Segoe UI", 9F);
            btnLaunchEmpty.Location = new System.Drawing.Point(281, 103);
            btnLaunchEmpty.Name = "btnLaunchEmpty";
            btnLaunchEmpty.Size = new System.Drawing.Size(115, 32);
            btnLaunchEmpty.TabIndex = 4;
            btnLaunchEmpty.Text = "Launch Empty";
            btnLaunchEmpty.Click += btnLaunchEmpty_Click;
            // 
            // ManifestLoaderPrompt
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(420, 155);
            Controls.Add(btnLaunchEmpty);
            Controls.Add(btnScanFromDirectory);
            Controls.Add(btnLoadFromManifest);
            Controls.Add(darkLabelInfo);
            Controls.Add(darkLabelTitle);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = global::PS4PKGTool.Properties.Resources.PackageIcon;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ManifestLoaderPrompt";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Welcome";
            ResumeLayout(false);
        }

        #endregion

        private DarkUI.Controls.DarkLabel darkLabelTitle;
        private DarkUI.Controls.DarkLabel darkLabelInfo;
        private DarkUI.Controls.DarkButton btnLoadFromManifest;
        private DarkUI.Controls.DarkButton btnScanFromDirectory;
        private DarkUI.Controls.DarkButton btnLaunchEmpty;
    }
}
