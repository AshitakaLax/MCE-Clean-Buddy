using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Diagnostics;

using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.Configuration;

namespace MCEBuddy.GUI
{
    public partial class eMailSettingsForm : Form
    {
        private bool invalid = false;
        private GeneralOptions _go;
        private ICore _pipeProxy = null;
        private Thread _testThread = null;

        public eMailSettingsForm(ICore pipeProxy, GeneralOptions go)
        {
            InitializeComponent();

            _pipeProxy = pipeProxy;
            _go = go;
        }

        private void eMailSettingsForm_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        public bool IsValidEmail(string strIn)
        {
            invalid = false;
            if (String.IsNullOrEmpty(strIn))
                return false;

            // Use IdnMapping class to convert Unicode domain names.
            strIn = Regex.Replace(strIn, @"(@)(.+)$", DomainMapper);
            if (invalid)
                return false;

            // Return true if strIn is in valid e-mail format.
            return Regex.IsMatch(strIn,
                   @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                   @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
                   RegexOptions.IgnoreCase);
        }

        private string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            IdnMapping idn = new IdnMapping();

            string domainName = match.Groups[2].Value;
            try
            {
                domainName = idn.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                invalid = true;
            }
            return match.Groups[1].Value + domainName;
        }

        /// <summary>
        /// Capture the eMail Settings from the GUI page and optionally write to the config file
        /// </summary>
        /// <param name="go">General Options object to write to</param>
        private void WriteSettings(GeneralOptions go)
        {
            go.eMailSettings.eMailBasicSettings.smtpServer = smtpServer.Text;
            go.eMailSettings.eMailBasicSettings.port = int.Parse(portNo.Text);
            go.eMailSettings.eMailBasicSettings.ssl = sslChk.Checked;
            go.eMailSettings.eMailBasicSettings.fromAddress = fromAddress.Text;
            go.eMailSettings.eMailBasicSettings.toAddresses = toAddress.Text;
            go.eMailSettings.successEvent = sendSuccessChk.Checked;
            go.eMailSettings.failedEvent = sendFailedChk.Checked;
            go.eMailSettings.cancelledEvent = sendCancelledChk.Checked;
            go.eMailSettings.startEvent = sendStartChk.Checked;
            go.eMailSettings.downloadFailedEvent = sendDownloadErrorChk.Checked;
            go.eMailSettings.queueEvent = sendQueueChk.Checked;
            go.eMailSettings.eMailBasicSettings.userName = userName.Text;
            go.eMailSettings.eMailBasicSettings.password = passWord.Text;
        }

        /// <summary>
        /// Read eMail settings from the General Options memory object into the GUI page
        /// </summary>
        /// <param name="go">General Options to read settings from</param>
        private void ReadSettings(GeneralOptions go)
        {
            smtpServer.Text = go.eMailSettings.eMailBasicSettings.smtpServer;
            portNo.Text = go.eMailSettings.eMailBasicSettings.port.ToString();
            sslChk.Checked = go.eMailSettings.eMailBasicSettings.ssl;
            fromAddress.Text = go.eMailSettings.eMailBasicSettings.fromAddress;
            toAddress.Text = go.eMailSettings.eMailBasicSettings.toAddresses;
            sendSuccessChk.Checked = go.eMailSettings.successEvent;
            sendFailedChk.Checked = go.eMailSettings.failedEvent;
            sendCancelledChk.Checked = go.eMailSettings.cancelledEvent;
            sendStartChk.Checked = go.eMailSettings.startEvent;
            sendDownloadErrorChk.Checked = go.eMailSettings.downloadFailedEvent;
            sendQueueChk.Checked = go.eMailSettings.queueEvent;
            userName.Text = go.eMailSettings.eMailBasicSettings.userName;
            passWord.Text = go.eMailSettings.eMailBasicSettings.password;
            confirmPassword.Text = go.eMailSettings.eMailBasicSettings.password;
        }

        private void eMailSettingsForm_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            LocaliseForms.LocaliseForm(this, toolTip);

            ReadSettings(_go);
        }

        private bool CheckSettings()
        {
            // Check server address
            if (smtpServer.Text == "")
            {
                MessageBox.Show(Localise.GetPhrase("Please enter a valid server address"), Localise.GetPhrase("Invalid SMTP Server"));
                return false;
            }

            // Check port
            if (portNo.Text == "")
            {
                MessageBox.Show(Localise.GetPhrase("Please enter a valid server port"), Localise.GetPhrase("Invalid SMTP Port"));
                return false;
            }

            // Check from address
            if (!IsValidEmail(fromAddress.Text))
            {
                MessageBox.Show(Localise.GetPhrase("Please enter a valid From eMail address"), Localise.GetPhrase("Invalid eMail Address"));
                return false;
            }

            // Check to addresses
            string[] toAddresses = toAddress.Text.Split(';');
            foreach (string address in toAddresses)
            {
                if (!IsValidEmail(address))
                {
                    MessageBox.Show(Localise.GetPhrase("Please enter valid To eMail addresses"), Localise.GetPhrase("Invalid eMail Address"));
                    return false;
                }
            }

            // Check passwords
            if (passWord.Text != confirmPassword.Text)
            {
                MessageBox.Show(Localise.GetPhrase("Passwords do not match"), Localise.GetPhrase("Password error"));
                return false;
            }

            return true;
        }
        private void oKcmd_Click(object sender, EventArgs e)
        {
            // Validate settings
            if (CheckSettings() == false)
                return;

            // Write the settings
            WriteSettings(_go);

            Close();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void portNo_TextChanged(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void testSettings_Click(object sender, EventArgs e)
        {
            // Validate settings
            if (CheckSettings() == false)
                return;

            GeneralOptions go = _go.Clone(); // Create a temporary object to test with

            WriteSettings(go); // Update the temp GO object with the test settings, only testing it so we don't need to write main object

            if (MessageBox.Show(Localise.GetPhrase("MCEBuddy will send an eMail to test the server settings.\r\nThis can take upto 60 seconds.\r\nMCEBuddy may be unresponsive during this period."),
                Localise.GetPhrase("Test eMail"),
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
                return;


            // Disable the control except for Ok and Cancel
            foreach (Control ctl in this.Controls)
                ctl.Enabled = false;
            oKcmd.Enabled = cmdCancel.Enabled = true;

            testSettings.Text = Localise.GetPhrase("Testing");
            _testThread = new Thread(() => eMail_SendTest(go)); // Send the eMail through a thead
            _testThread.IsBackground = true; // Kill thread when object terminates
            _testThread.Start();
        }

        void eMail_SendTest(GeneralOptions go)
        {
            bool success = false;

            try
            {
                success = _pipeProxy.TestEmailSettings(go.eMailSettings.eMailBasicSettings);

                if (!success)
                    MessageBox.Show(Localise.GetPhrase("Send eMail test failed! Please check your settings and internet connectivity"), Localise.GetPhrase("Test eMail Failed"));
                else
                    MessageBox.Show(Localise.GetPhrase("Send eMail test successful"), Localise.GetPhrase("Test eMail Success"));
            }
            catch (Exception e)
            {
                MessageBox.Show(Localise.GetPhrase("Unable to communicate with engine."), Localise.GetPhrase("Communication Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to get pipe active status"), EventLogEntryType.Warning);
                Log.WriteSystemEventLog(e.ToString(), EventLogEntryType.Warning);
            }

            // Enable the controls
            foreach (Control ctl in this.Controls)
                ctl.Enabled = true;

            testSettings.Text = Localise.GetPhrase("Test");

            _testThread = null; // This is done
        }
    }
}
