namespace PS4PKGTool
{
    partial class GameFeedbackForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameFeedbackForm));
            lblFeedbackGameName = new DarkUI.Controls.DarkLabel();
            lblFeedbackGameMeta = new DarkUI.Controls.DarkLabel();
            lblFeedbackSession = new DarkUI.Controls.DarkLabel();
            btnStatusNothing = new DarkUI.Controls.DarkButton();
            btnStatusBoots = new DarkUI.Controls.DarkButton();
            btnStatusMenus = new DarkUI.Controls.DarkButton();
            btnStatusInGame = new DarkUI.Controls.DarkButton();
            btnStatusPlayable = new DarkUI.Controls.DarkButton();
            lblFeedbackNotesLabel = new DarkUI.Controls.DarkLabel();
            txtFeedbackNotes = new DarkUI.Controls.DarkTextBox();
            btnFeedbackSave = new DarkUI.Controls.DarkButton();
            btnFeedbackCreateReport = new DarkUI.Controls.DarkButton();
            btnFeedbackSkip = new DarkUI.Controls.DarkButton();
            SuspendLayout();
            // 
            // lblFeedbackGameName
            // 
            lblFeedbackGameName.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblFeedbackGameName.Location = new System.Drawing.Point(12, 12);
            lblFeedbackGameName.Name = "lblFeedbackGameName";
            lblFeedbackGameName.Size = new System.Drawing.Size(436, 20);
            lblFeedbackGameName.TabIndex = 1;
            lblFeedbackGameName.Text = "-";
            // 
            // lblFeedbackGameMeta
            // 
            lblFeedbackGameMeta.Location = new System.Drawing.Point(12, 34);
            lblFeedbackGameMeta.Name = "lblFeedbackGameMeta";
            lblFeedbackGameMeta.Size = new System.Drawing.Size(436, 16);
            lblFeedbackGameMeta.TabIndex = 2;
            // 
            // lblFeedbackSession
            //
            lblFeedbackSession.Location = new System.Drawing.Point(12, 52);
            lblFeedbackSession.Name = "lblFeedbackSession";
            lblFeedbackSession.Size = new System.Drawing.Size(436, 16);
            lblFeedbackSession.TabIndex = 13;
            //
            // btnStatusNothing
            //
            btnStatusNothing.Location = new System.Drawing.Point(12, 72);
            btnStatusNothing.Name = "btnStatusNothing";
            btnStatusNothing.Size = new System.Drawing.Size(80, 26);
            btnStatusNothing.TabIndex = 3;
            btnStatusNothing.Text = "Nothing";
            btnStatusNothing.Click += btnStatus_Click;
            //
            // btnStatusBoots
            //
            btnStatusBoots.Location = new System.Drawing.Point(96, 72);
            btnStatusBoots.Name = "btnStatusBoots";
            btnStatusBoots.Size = new System.Drawing.Size(80, 26);
            btnStatusBoots.TabIndex = 4;
            btnStatusBoots.Text = "Boots";
            btnStatusBoots.Click += btnStatus_Click;
            //
            // btnStatusMenus
            //
            btnStatusMenus.Location = new System.Drawing.Point(180, 72);
            btnStatusMenus.Name = "btnStatusMenus";
            btnStatusMenus.Size = new System.Drawing.Size(80, 26);
            btnStatusMenus.TabIndex = 5;
            btnStatusMenus.Text = "Menus";
            btnStatusMenus.Click += btnStatus_Click;
            //
            // btnStatusInGame
            //
            btnStatusInGame.Location = new System.Drawing.Point(264, 72);
            btnStatusInGame.Name = "btnStatusInGame";
            btnStatusInGame.Size = new System.Drawing.Size(80, 26);
            btnStatusInGame.TabIndex = 6;
            btnStatusInGame.Text = "In-Game";
            btnStatusInGame.Click += btnStatus_Click;
            //
            // btnStatusPlayable
            //
            btnStatusPlayable.Location = new System.Drawing.Point(348, 72);
            btnStatusPlayable.Name = "btnStatusPlayable";
            btnStatusPlayable.Size = new System.Drawing.Size(88, 26);
            btnStatusPlayable.TabIndex = 7;
            btnStatusPlayable.Text = "Playable";
            btnStatusPlayable.Click += btnStatus_Click;
            //
            // lblFeedbackNotesLabel
            //
            lblFeedbackNotesLabel.Location = new System.Drawing.Point(12, 108);
            lblFeedbackNotesLabel.Name = "lblFeedbackNotesLabel";
            lblFeedbackNotesLabel.Size = new System.Drawing.Size(120, 16);
            lblFeedbackNotesLabel.TabIndex = 8;
            lblFeedbackNotesLabel.Text = "Notes";
            //
            // txtFeedbackNotes
            //
            txtFeedbackNotes.Location = new System.Drawing.Point(12, 128);
            txtFeedbackNotes.Multiline = true;
            txtFeedbackNotes.Name = "txtFeedbackNotes";
            txtFeedbackNotes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtFeedbackNotes.Size = new System.Drawing.Size(436, 96);
            txtFeedbackNotes.TabIndex = 9;
            // 
            // btnFeedbackSave
            //
            btnFeedbackSave.Location = new System.Drawing.Point(84, 236);
            btnFeedbackSave.Name = "btnFeedbackSave";
            btnFeedbackSave.Size = new System.Drawing.Size(76, 30);
            btnFeedbackSave.TabIndex = 10;
            btnFeedbackSave.Text = "Save Result";
            btnFeedbackSave.Click += btnFeedbackSave_Click;
            //
            // btnFeedbackCreateReport
            //
            btnFeedbackCreateReport.Location = new System.Drawing.Point(172, 236);
            btnFeedbackCreateReport.Name = "btnFeedbackCreateReport";
            btnFeedbackCreateReport.Size = new System.Drawing.Size(178, 30);
            btnFeedbackCreateReport.TabIndex = 11;
            btnFeedbackCreateReport.Text = "Create Compatibility Report";
            btnFeedbackCreateReport.Click += btnFeedbackCreateReport_Click;
            //
            // btnFeedbackSkip
            //
            btnFeedbackSkip.Location = new System.Drawing.Point(360, 236);
            btnFeedbackSkip.Name = "btnFeedbackSkip";
            btnFeedbackSkip.Size = new System.Drawing.Size(88, 30);
            btnFeedbackSkip.TabIndex = 12;
            btnFeedbackSkip.Text = "Skip";
            btnFeedbackSkip.Click += btnFeedbackSkip_Click;
            //
            // GameFeedbackForm
            //
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(460, 280);
            Controls.Add(btnFeedbackSkip);
            Controls.Add(btnFeedbackCreateReport);
            Controls.Add(btnFeedbackSave);
            Controls.Add(txtFeedbackNotes);
            Controls.Add(lblFeedbackNotesLabel);
            Controls.Add(btnStatusPlayable);
            Controls.Add(btnStatusInGame);
            Controls.Add(btnStatusMenus);
            Controls.Add(btnStatusBoots);
            Controls.Add(btnStatusNothing);
            Controls.Add(lblFeedbackSession);
            Controls.Add(lblFeedbackGameMeta);
            Controls.Add(lblFeedbackGameName);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = global::PS4PKGTool.Properties.Resources.PackageIcon;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "GameFeedbackForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Add Test Result";
            ResumeLayout(false);
            PerformLayout();
        }
        private DarkUI.Controls.DarkLabel lblFeedbackGameName;
        private DarkUI.Controls.DarkLabel lblFeedbackGameMeta;
        private DarkUI.Controls.DarkLabel lblFeedbackSession;
        private DarkUI.Controls.DarkButton btnStatusNothing;
        private DarkUI.Controls.DarkButton btnStatusBoots;
        private DarkUI.Controls.DarkButton btnStatusMenus;
        private DarkUI.Controls.DarkButton btnStatusInGame;
        private DarkUI.Controls.DarkButton btnStatusPlayable;
        private DarkUI.Controls.DarkLabel lblFeedbackNotesLabel;
        private DarkUI.Controls.DarkTextBox txtFeedbackNotes;
        private DarkUI.Controls.DarkButton btnFeedbackSave;
        private DarkUI.Controls.DarkButton btnFeedbackCreateReport;
        private DarkUI.Controls.DarkButton btnFeedbackSkip;
    }
}
