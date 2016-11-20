using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.Configuration;

namespace MCEBuddy.GUI
{
    public partial class MonitorTaskExpertSettingsForm : Form
    {
        private MonitorJobOptions _mjo;
        private bool _loading = false;

        public MonitorTaskExpertSettingsForm(MonitorJobOptions mjo)
        {
            InitializeComponent();

            _mjo = mjo;
        }

        private void SourceForm_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            _loading = true;
            LocaliseForms.LocaliseForm(this, toolTip);
            ReadSettings(_mjo);
            _loading = false;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void oKcmd_Click(object sender, EventArgs e)
        {
            WriteSettings(_mjo);

            this.Close();
        }

        /// <summary>
        /// This function essentially reads the settings and writes it back after validating them (and setting/unsettings options as required)
        /// It emulates a load function followed by an OK click
        /// </summary>
        public void WriteAndValidateSettings()
        {
            _loading = true;
            ReadSettings(_mjo); // Read settings
            WriteSettings(_mjo); // Validate and write settings
            _loading = false;
        }

        /// <summary>
        /// Reads the settings from the monitor job options and populates the form
        /// </summary>
        private void ReadSettings(MonitorJobOptions mjo)
        {
            deleteOriginalChk.Checked = mjo.deleteMonitorOriginal;
            archiveOriginalChk.Checked = mjo.archiveMonitorOriginal;
            archivePathTxt.Text = mjo.archiveMonitorPath;
            if (archiveOriginalChk.Checked)
                archivePathTxt.Enabled = archiveFolderCmd.Enabled = true;
            else
                archivePathTxt.Enabled = archiveFolderCmd.Enabled = false;
            monitorConvertedChk.Checked = mjo.monitorConvertedFiles;
            reMonitorRecordedChk.Checked = mjo.reMonitorRecordedFiles;
        }

        /// <summary>
        /// Validates and Writes the settings from the form to the Monitor Job Options
        /// </summary>
        private void WriteSettings(MonitorJobOptions mjo)
        {
            mjo.deleteMonitorOriginal = deleteOriginalChk.Checked;
            mjo.archiveMonitorOriginal = archiveOriginalChk.Checked;
            mjo.archiveMonitorPath = archivePathTxt.Text.Trim();
            mjo.monitorConvertedFiles = monitorConvertedChk.Checked;
            mjo.reMonitorRecordedFiles = reMonitorRecordedChk.Checked;
        }

        private void monitorConvertedChk_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) // nothing to do if the form is loading
                return;

            if (monitorConvertedChk.Checked)
            {
                DialogResult result =
                    MessageBox.Show(
                        Localise.GetPhrase("CHECK THIS BOX AT YOUR OWN RISK.\r\nDO NOT CHECK THIS BOX UNLESS YOU KNOW WHAT YOU ARE DOING.\r\nBy default Monitor Locations will ignore files that have already been converted.\r\nCheck this box if you also want to monitor converted files in the Monitor Path."),
                        Localise.GetPhrase("Are you sure you want to do this?"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    MessageBox.Show(
                        Localise.GetPhrase("To avoid an infinite conversion loop, it is recommended to setup your Monitor search pattern in a way such as to ensure that the converted files are not being monitored by this Monitor task."),
                        Localise.GetPhrase("Recommendation"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    monitorConvertedChk.Checked = true;
                }
                else
                    monitorConvertedChk.Checked = false;
            }
        }

        private void reMonitorChk_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) // nothing to do if the form is loading
                return;

            if (reMonitorRecordedChk.Checked)
            {
                DialogResult result =
                    MessageBox.Show(
                        Localise.GetPhrase("CHECK THIS BOX AT YOUR OWN RISK.\r\nDO NOT CHECK THIS BOX UNLESS YOU KNOW WHAT YOU ARE DOING.\r\nBy default Monitor Locations will not remonitor recorded videos that have been processed.\r\nCheck this box if you want to remonitor and reconvert the recorded videos."),
                        Localise.GetPhrase("Are you sure you want to do this?"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    MessageBox.Show(
                        Localise.GetPhrase("To avoid an infinite conversion loop, it is recommended that you enable Delete original file option in the Settings page."),
                        Localise.GetPhrase("Recommendation"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    reMonitorRecordedChk.Checked = true;
                }
                else
                    reMonitorRecordedChk.Checked = false;
            }
        }

        private void deleteOriginalChk_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) // nothing to do if the form is loading
                return;

            if (deleteOriginalChk.Checked)
            {
                DialogResult result =
                    MessageBox.Show(
                        Localise.GetPhrase("Only select this option if MCEBuddy has been converting your files reliably for an acceptable period of time. Do you wish MCEBuddy to delete each original file once it has been converted?"),
                        Localise.GetPhrase("Confirm delete originals?"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    deleteOriginalChk.Checked = true;
                    archiveOriginalChk.Checked = false; //only delete or archive can be checked at a time
                }
                else
                    deleteOriginalChk.Checked = false;
            }
        }

        private void archiveOriginalChk_CheckedChanged(object sender, EventArgs e)
        {
            if (archiveOriginalChk.Checked) // only delete or archive can be checked at a time
            {
                deleteOriginalChk.Checked = false;
                archivePathTxt.Enabled = archiveFolderCmd.Enabled = true; // Enable archive folder path
            }
            else
                archivePathTxt.Enabled = archiveFolderCmd.Enabled = false; // disable archive folder path
        }

        private void archiveFolderCmd_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = archivePathTxt.Text;
            folderBrowserDialog.ShowDialog();
            if (folderBrowserDialog.SelectedPath != "")
                archivePathTxt.Text = folderBrowserDialog.SelectedPath;
        }
    }
}
