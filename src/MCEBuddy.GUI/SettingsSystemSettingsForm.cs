using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.IO;

using MCEBuddy.Globals;
using MCEBuddy.Configuration;
using MCEBuddy.Util;

namespace MCEBuddy.GUI
{
    public partial class SettingsSystemSettingsForm : Form
    {
        GeneralOptions _go;

        public SettingsSystemSettingsForm(GeneralOptions go)
        {
            InitializeComponent();

            // Read Settings
            _go = go;
        }

        private void ExpertSettingsForm_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            LocaliseForms.LocaliseForm(this, toolTip);

            ReadSettings();
        }

        /// <summary>
        /// This function essentially reads the settings and writes it back after validating them (and setting/unsettings options as required)
        /// It emulates a load function followed by an OK click
        /// </summary>
        public void WriteAndValidateSettings()
        {
            ReadSettings(); // Read settings
            WriteSettings(); // Validate and write settings
        }

        private void WriteSettings()
        {
            // Logging
            _go.logLevel = logLevelCbo.SelectedIndex;
            if (maxLogDays.Enabled)
                int.TryParse(maxLogDays.SelectedItem.ToString(), out _go.logKeepDays);
            else
                _go.logKeepDays = 0; // No deleting if logs are disabled

            // Folder Management
            _go.tempWorkingPath = tempPathTxt.Text.Trim();
            _go.archivePath = archivePathTxt.Text.Trim();
            _go.spaceCheck = spaceChk.Checked;
            _go.failedPath = (failedMoveChk.Checked ? failedPathTxt.Text.Trim() : "");
            _go.comskipPath = comskipPathTxt.Text.Trim();
            _go.customProfilePath = customProfileTxt.Text.Trim();

            // Customized eMail Subjects
            _go.eMailSettings.successSubject = successEMailTxt.Text.Trim();
            _go.eMailSettings.failedSubject = failedEMailTxt.Text.Trim();
            _go.eMailSettings.cancelledSubject = cancelledEMailTxt.Text.Trim();
            _go.eMailSettings.startSubject = startEMailTxt.Text.Trim();
            _go.eMailSettings.downloadFailedSubject = downloadFailEMailTxt.Text.Trim();
            _go.eMailSettings.queueSubject = queueEMailTxt.Text.Trim();
            _go.eMailSettings.skipBody = skipeMailBodyChk.Checked;

            // Miscellaneous
            _go.useRecycleBin = useRecycleBinChk.Checked;
            double.TryParse(subtitleSegmentOffsetTxt.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _go.subtitleSegmentOffset);
            int.TryParse(pollPeriodTxt.Text, out _go.pollPeriod);
            int.TryParse(serverPortTxt.Text, out _go.localServerPort);
            int.TryParse(hangPeriodTxt.Text, out _go.hangTimeout);
            _go.uPnPEnable = uPnPChk.Checked;
            _go.firewallExceptionEnabled = enableFirewallExpChk.Checked;

            // Processor Management
            _go.CPUAffinity = GetProcessorAffinityMask();
        }

        private void ReadSettings()
        {
            // Logging
            logLevelCbo.SelectedIndex = _go.logLevel;
            maxLogDays.SelectedItem = _go.logKeepDays.ToString(System.Globalization.CultureInfo.InvariantCulture);
            logLevelCbo.Enabled = maxLogDays.Enabled = _go.logJobs;

            // Folder Management
            tempPathTxt.Text = _go.tempWorkingPath; // Temp working folder
            archivePathTxt.Enabled = archiveFolderCmd.Enabled = _go.archiveOriginal; // Archiving enabled
            archivePathTxt.Text = _go.archivePath; // Archive folder
            spaceChk.Checked = _go.spaceCheck; // Check for enough free space
            failedPathTxt.Text = _go.failedPath; // Failed conversion move original path
            failedPathTxt.Enabled = failedFolderCmd.Enabled = failedMoveChk.Checked = !String.IsNullOrWhiteSpace(_go.failedPath); // Failed move original enable
            comskipPathTxt.Text = _go.comskipPath;
            customProfileTxt.Text = _go.customProfilePath;

            // Customized eMail Subjects
            successEMailTxt.Text = _go.eMailSettings.successSubject;
            failedEMailTxt.Text = _go.eMailSettings.failedSubject;
            cancelledEMailTxt.Text = _go.eMailSettings.cancelledSubject;
            startEMailTxt.Text = _go.eMailSettings.startSubject;
            downloadFailEMailTxt.Text = _go.eMailSettings.downloadFailedSubject;
            queueEMailTxt.Text = _go.eMailSettings.queueSubject;
            skipeMailBodyChk.Checked = _go.eMailSettings.skipBody;
            successEMailTxt.Enabled = failedEMailTxt.Enabled = cancelledEMailTxt.Enabled = startEMailTxt.Enabled = downloadFailEMailTxt.Enabled = queueEMailTxt.Enabled = skipeMailBodyChk.Enabled = _go.sendEmail; // Enable only if send eMail is enabled

            // Miscellaneous
            useRecycleBinChk.Checked = _go.useRecycleBin;
            subtitleSegmentOffsetTxt.Text = _go.subtitleSegmentOffset.ToString(CultureInfo.InvariantCulture);
            pollPeriodTxt.Text = _go.pollPeriod.ToString();
            serverPortTxt.Text = _go.localServerPort.ToString();
            hangPeriodTxt.Text = _go.hangTimeout.ToString();
            uPnPChk.Checked = _go.uPnPEnable;
            enableFirewallExpChk.Checked = _go.firewallExceptionEnabled;

            // Processor Management
            ProcessorBankSet(processorLimitChk.Checked);
            SetProcessorAffinityMask(_go.CPUAffinity);
        }

        private void ProcessorBankSet(bool enableBank)
        {
            // Find the number of logical processors
            int procCount = StatusForm.engineProcessorCount;

            // Enable or disable each processor as per the processor count
            for (int i = 1; i <= 32; i++)
            {
                CheckBox processorBox = (CheckBox)this.Controls.Find("p" + i.ToString(), true)[0];
                processorBox.Enabled = (i <= procCount) && enableBank;
                if (i > procCount) // Non existent processors
                {
                    processorBox.Checked = false;
                    processorBox.Hide();
                }
            }
        }

        /// <summary>
        /// Return 0 if all processors are enabled, -1 if no processor is enabled otherwise Bitmask of upto 32 processors
        /// </summary>
        private IntPtr GetProcessorAffinityMask()
        {
            // Find the number of logical processors
            int procCount = StatusForm.engineProcessorCount;

            int cpuMask = 0; // Default all are enabled (nothing is checked)

            if (!processorLimitChk.Checked)
                return (IntPtr)cpuMask; // Enable all processors

            // Create a bit mask based on then processors checked (max 32 processors since return type is int)
            for (int i = 1; i <= 32; i++)
            {
                CheckBox processorBox = (CheckBox)this.Controls.Find("p" + i.ToString(), true)[0];
                if (processorBox.Checked && processorBox.Enabled)
                    cpuMask |= (1 << (i-1)); // Each bit represents a processor
            }

            // Check if nothing was checked
            if (cpuMask == 0)
                cpuMask = -1; // Set to -1 if nothing is checked

            // Check if all enabled processors are checked, then we use 0
            if (cpuMask == (Math.Pow(2, procCount) - 1))
                cpuMask = 0; // All processors enabled

            return (IntPtr)cpuMask;
        }

        /// <summary>
        /// Sets the checkboxes to reflect the CPU Affinity mask
        /// </summary>
        /// <param name="cpuMask">CPU Affininty Mask</param>
        private void SetProcessorAffinityMask(IntPtr cpuMask)
        {
            // Find the number of logical processors
            int procCount = StatusForm.engineProcessorCount;

            // If processor management is disabled, nothing to do here
            processorLimitChk.Checked = (cpuMask == (IntPtr)0 ? false : true); // Enable processor limitation
            if (!processorLimitChk.Checked)
                return;

            // Create a bit mask based on then processors checked (max 32 processors since return type is int)
            for (int i = 1; i <= 32; i++)
            {
                CheckBox processorBox = (CheckBox)this.Controls.Find("p" + i.ToString(), true)[0];
                if (processorBox.Enabled) // Only work on processors that are enabled
                {
                    if (((int)cpuMask & (1 << (i-1))) != 0) // Each bit represents a processor
                        processorBox.Checked = true;
                    else
                        processorBox.Checked = false;
                }
            }
        }

        private void processorLimitChk_CheckedChanged(object sender, EventArgs e)
        {
            ProcessorBankSet(processorLimitChk.Checked); // Enable or disable bank
        }

        private void failedMoveChk_CheckedChanged(object sender, EventArgs e)
        {
            failedPathTxt.Enabled = failedFolderCmd.Enabled = failedMoveChk.Checked;
        }

        private void tempFolderCmd_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = tempPathTxt.Text;
            folderBrowserDialog.ShowDialog();
            if (folderBrowserDialog.SelectedPath != "")
                tempPathTxt.Text = folderBrowserDialog.SelectedPath;
        }

        private void archiveFolderCmd_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = archivePathTxt.Text;
            folderBrowserDialog.ShowDialog();
            if (folderBrowserDialog.SelectedPath != "")
                archivePathTxt.Text = folderBrowserDialog.SelectedPath;
        }

        private void failedFolderCmd_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = failedPathTxt.Text;
            folderBrowserDialog.ShowDialog();
            if (folderBrowserDialog.SelectedPath != "")
                failedPathTxt.Text = folderBrowserDialog.SelectedPath;
        }

        private void comskipFolderCmd_Click(object sender, EventArgs e)
        {
            openFileDialog.AutoUpgradeEnabled = true;
            openFileDialog.Multiselect = false;
            openFileDialog.FileName = "";
            openFileDialog.Filter = "Comskip|comskip.exe|Exe Files|*.exe|All Files|*.*";
            if (File.Exists(comskipPathTxt.Text))
                openFileDialog.InitialDirectory = Path.GetDirectoryName(comskipPathTxt.Text);
            openFileDialog.ShowDialog();
            if (openFileDialog.FileName != "")
                comskipPathTxt.Text = openFileDialog.FileName;
        }

        private void profileFileCmd_Click(object sender, EventArgs e)
        {
            openFileDialog.AutoUpgradeEnabled = true;
            openFileDialog.Multiselect = false;
            openFileDialog.FileName = "";
            openFileDialog.Filter = "Profiles|profiles.conf|Config Files|*.conf|All Files|*.*";
            if (File.Exists(customProfileTxt.Text))
                openFileDialog.InitialDirectory = Path.GetDirectoryName(customProfileTxt.Text);
            openFileDialog.ShowDialog();
            if (openFileDialog.FileName != "")
                customProfileTxt.Text = openFileDialog.FileName;
        }

        private bool ValidateSettings()
        {
            if (GetProcessorAffinityMask() == (IntPtr)(-1))
            {
                MessageBox.Show(Localise.GetPhrase("Select atleast one processor to use"), Localise.GetPhrase("Invalid Configuration"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
            }

            if (String.IsNullOrWhiteSpace(pollPeriodTxt.Text) || (pollPeriodTxt.Text == "0"))
            {
                MessageBox.Show(Localise.GetPhrase("Invalid poll period"), Localise.GetPhrase("Invalid Configuration"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            if (String.IsNullOrWhiteSpace(serverPortTxt.Text) || (serverPortTxt.Text == "0"))
            {
                MessageBox.Show(Localise.GetPhrase("Invalid server port"), Localise.GetPhrase("Invalid Configuration"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            if (!String.IsNullOrWhiteSpace(customProfileTxt.Text) && !File.Exists(customProfileTxt.Text))
            {
                MessageBox.Show(Localise.GetPhrase("Invalid custom profiles.conf"), Localise.GetPhrase("Invalid Configuration"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            return true;
        }

        private void oKcmd_Click(object sender, EventArgs e)
        {
            if (!ValidateSettings())
                return;

            WriteSettings();

            this.Close();
        }

        private void double_TextChanged(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && !(e.KeyChar == '.') && !(e.KeyChar == '-'))
                e.Handled = true;
        }

        private void int_TextChanged(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void serverPort_TextChanged(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;

            if (char.IsDigit(e.KeyChar) && (serverPortTxt.Text.Length >= 5)) // 5 digit port maximum
                e.Handled = true;
        }

        private void defaultCmd_Click(object sender, EventArgs e)
        {
            subtitleSegmentOffsetTxt.Text = GlobalDefs.SEGMENT_CUT_OFFSET_GOP_COMPENSATE;
            serverPortTxt.Text = GlobalDefs.MCEBUDDY_SERVER_PORT;
            hangPeriodTxt.Text = GlobalDefs.HANG_PERIOD_DETECT.ToString();
            pollPeriodTxt.Text = GlobalDefs.MONITOR_POLL_PERIOD.ToString();
        }

        private void SelectFolderCmd_Click(object sender, EventArgs e)
        {
            CredentialsForm connectionCredentialsForm = new CredentialsForm(_go); // Enter network credentials
            connectionCredentialsForm.ShowDialog();
        }

        private void uPnPChk_CheckedChanged(object sender, EventArgs e)
        {
            if (uPnPChk.Checked)
                enableFirewallExpChk.Checked = true; // We need firewall for uPnP
        }
    }
}
