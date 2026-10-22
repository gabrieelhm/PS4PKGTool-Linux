using System;

namespace PS4PKGTool
{
    partial class DropFolderPrompt
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DropFolderPrompt));
            darkLabelTitle = new DarkUI.Controls.DarkLabel();
            darkLabelPath = new DarkUI.Controls.DarkLabel();
            darkLabelPath2 = new DarkUI.Controls.DarkLabel();
            chkRecursive = new DarkUI.Controls.DarkCheckBox();
            chkAddToDirectories = new DarkUI.Controls.DarkCheckBox();
            btnScan = new DarkUI.Controls.DarkButton();
            btnCancel = new DarkUI.Controls.DarkButton();
            SuspendLayout();
            // 
            // darkLabelTitle
            // 
            darkLabelTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            darkLabelTitle.ForeColor = System.Drawing.Color.Gainsboro;
            darkLabelTitle.Location = new System.Drawing.Point(20, 15);
            darkLabelTitle.Name = "darkLabelTitle";
            darkLabelTitle.Size = new System.Drawing.Size(380, 22);
            darkLabelTitle.TabIndex = 0;
            darkLabelTitle.Text = "Folder Dropped";
            // 
            // darkLabelPath
            // 
            darkLabelPath.Font = new System.Drawing.Font("Segoe UI", 9F);
            darkLabelPath.ForeColor = System.Drawing.Color.Gainsboro;
            darkLabelPath.Location = new System.Drawing.Point(20, 42);
            darkLabelPath.Name = "darkLabelPath";
            darkLabelPath.Size = new System.Drawing.Size(380, 18);
            darkLabelPath.TabIndex = 1;
            // 
            // darkLabelPath2
            // 
            darkLabelPath2.Font = new System.Drawing.Font("Segoe UI", 8F);
            darkLabelPath2.ForeColor = System.Drawing.Color.FromArgb(140, 140, 140);
            darkLabelPath2.Location = new System.Drawing.Point(20, 60);
            darkLabelPath2.Name = "darkLabelPath2";
            darkLabelPath2.Size = new System.Drawing.Size(380, 16);
            darkLabelPath2.TabIndex = 2;
            // 
            // chkRecursive
            // 
            chkRecursive.Font = new System.Drawing.Font("Segoe UI", 9F);
            chkRecursive.ForeColor = System.Drawing.Color.Gainsboro;
            chkRecursive.Location = new System.Drawing.Point(23, 88);
            chkRecursive.Name = "chkRecursive";
            chkRecursive.Size = new System.Drawing.Size(250, 22);
            chkRecursive.TabIndex = 3;
            chkRecursive.Text = "Scan subfolders recursively";
            // 
            // chkAddToDirectories
            // 
            chkAddToDirectories.Font = new System.Drawing.Font("Segoe UI", 9F);
            chkAddToDirectories.ForeColor = System.Drawing.Color.Gainsboro;
            chkAddToDirectories.Location = new System.Drawing.Point(23, 113);
            chkAddToDirectories.Name = "chkAddToDirectories";
            chkAddToDirectories.Size = new System.Drawing.Size(380, 22);
            chkAddToDirectories.TabIndex = 4;
            chkAddToDirectories.Text = "Add to saved directories";
            // 
            // btnScan
            // 
            btnScan.Font = new System.Drawing.Font("Segoe UI", 9F);
            btnScan.Location = new System.Drawing.Point(198, 148);
            btnScan.Name = "btnScan";
            btnScan.Size = new System.Drawing.Size(100, 30);
            btnScan.TabIndex = 5;
            btnScan.Text = "Scan";
            btnScan.Click += btnScan_Click;
            // 
            // btnCancel
            // 
            btnCancel.Font = new System.Drawing.Font("Segoe UI", 9F);
            btnCancel.Location = new System.Drawing.Point(303, 148);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(100, 30);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Cancel";
            btnCancel.Click += btnCancel_Click;
            // 
            // DropFolderPrompt
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(420, 195);
            Controls.Add(btnCancel);
            Controls.Add(btnScan);
            Controls.Add(chkAddToDirectories);
            Controls.Add(chkRecursive);
            Controls.Add(darkLabelPath2);
            Controls.Add(darkLabelPath);
            Controls.Add(darkLabelTitle);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = global::PS4PKGTool.Properties.Resources.PackageIcon;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "DropFolderPrompt";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Add PKG Folder";
            ResumeLayout(false);
        }

        private DarkUI.Controls.DarkLabel darkLabelTitle;
        private DarkUI.Controls.DarkLabel darkLabelPath;
        private DarkUI.Controls.DarkLabel darkLabelPath2;
        private DarkUI.Controls.DarkCheckBox chkRecursive;
        private DarkUI.Controls.DarkCheckBox chkAddToDirectories;
        private DarkUI.Controls.DarkButton btnScan;
        private DarkUI.Controls.DarkButton btnCancel;
    }
}
