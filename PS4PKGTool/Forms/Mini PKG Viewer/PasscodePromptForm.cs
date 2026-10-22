using PS4PKGTool.Utilities.PS4PKGToolHelper;
using System;
using System.Windows.Forms;

namespace PS4PKGTool
{
    /// <summary>
    /// Asks the user for a package passcode when the default one is rejected
    /// by orbis-pub-cmd. The passcode is a 32 character hex value; the default
    /// all-zero passcode is prefilled because most packages use it.
    /// The "No passcode" option selects orbis-pub-cmd's --no_passcode mode,
    /// which works for official PKGs whose key is unknown.
    /// </summary>
    public partial class PasscodePromptForm : DarkUI.Forms.DarkForm
    {
        /// <summary>orbis-pub-cmd --no_passcode sentinel: run without any
        /// passcode (official packages with unknown keys).</summary>
        public const string NoPasscode = "";

        public PasscodePromptForm()
        {
            InitializeComponent();
            AcceptButton = btnPasscodeOk;
            CancelButton = btnPasscodeCancel;
        }

        /// <summary>The entered passcode, "" for the no-passcode mode, or
        /// null when nothing valid was chosen.</summary>
        public string? Passcode => chkNoPasscode.Checked ? NoPasscode : tbPasscodeInput.Text.Trim();

        /// <summary>True when the user chose the --no_passcode mode.</summary>
        public bool IsNoPasscode => chkNoPasscode.Checked;

        private void btnPasscodeOk_Click(object sender, EventArgs e)
        {
            if (chkNoPasscode.Checked)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            if (string.IsNullOrWhiteSpace(tbPasscodeInput.Text))
            {
                ShowWarning("Enter the PKG passcode, or press Cancel.", false);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void chkNoPasscode_CheckedChanged(object sender, EventArgs e)
        {
            tbPasscodeInput.Enabled = !chkNoPasscode.Checked;
        }
    }
}
