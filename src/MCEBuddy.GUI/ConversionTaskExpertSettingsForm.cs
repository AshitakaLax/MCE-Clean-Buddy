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
    public partial class ConversionTaskExpertSettingsForm : Form
    {
        private ConversionJobOptions _cjo = null;
        private bool _loading = false;
        List<MonitorJobOptions> _monitorTasks;

        public ConversionTaskExpertSettingsForm(List<MonitorJobOptions> monitorTasks, ConversionJobOptions cjo)
        {
            InitializeComponent();
            _cjo = cjo;
            _monitorTasks = monitorTasks;
        }

        private void ConversionTaskExpertSettingsForm_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            _loading = true;

            LocaliseForms.LocaliseForm(this, toolTip); // Localize form
            ReadSettings(_cjo); // Read settings

            _loading = false;
        }

        /// <summary>
        /// This function essentially reads the settings and writes it back after validating them (and setting/unsettings options as required)
        /// It emulates a load function followed by an OK click
        /// </summary>
        public void WriteAndValidateSettings()
        {
            _loading = true;
            ReadSettings(_cjo); // Read settings
            WriteSettings(_cjo); // Validate and write settings
            _loading = false;
        }

        /// <summary>
        /// Reads the settings from the conversion job options and populates the form
        /// </summary>
        private void ReadSettings(ConversionJobOptions cjo)
        {
            monitorTaskNameMatchChk.Checked = !(cjo.monitorTaskNames == null); // If we have a list then check the box

            startTrim.Text = cjo.startTrim.ToString();
            startTrimChk.Checked = (cjo.startTrim != 0);
            endTrim.Text = cjo.endTrim.ToString();
            endTrimChk.Checked = (cjo.endTrim != 0);

            makKey.Text = cjo.tivoMAKKey;
            insertTopChk.Checked = cjo.insertQueueTop;

            string extractCCOpts = cjo.extractCC;
            if (String.IsNullOrEmpty(extractCCOpts))
            {
                extractCCAdvOpts.Enabled = extractCCAdvOpts.Checked = false;
                ccField.Enabled = ccChannel.Enabled = false;
                ccOffset.Enabled = false;
                ccField.Text = ccChannel.Text = "1"; // Populate some default values
            }
            else if (extractCCOpts == "default")
            {
                extractCCAdvOpts.Enabled = true;
                extractCCAdvOpts.Checked = false;
                ccOffset.Enabled = true;
                ccField.Enabled = ccChannel.Enabled = false;
                ccField.Text = ccChannel.Text = "1"; // Populate some default values
            }
            else
            {
                string[] ccOpts = extractCCOpts.Split(','); // Field,Channel
                extractCCAdvOpts.Enabled = extractCCAdvOpts.Checked = true;
                ccOffset.Enabled = ccField.Enabled = ccChannel.Enabled = true;
                ccField.Text = ccOpts[0]; // MCEBuddy doesn't support 12/both configuration
                ccChannel.Text = ccOpts[1];
            }
            
            ccOffset.Text = cjo.ccOffset.ToString(CultureInfo.InvariantCulture);

            if (cjo.audioOffset == 0)
            {
                audioOffsetChk.Checked = audioOffset.Enabled = false;
                audioOffset.Text = "";
            }
            else
            {
                audioOffsetChk.Checked = audioOffset.Enabled = true;
                audioOffset.Text = cjo.audioOffset.ToString(CultureInfo.InvariantCulture);
            }

            if (String.IsNullOrWhiteSpace(cjo.FPS))
            {
                frameRateChk.Checked = frameRate.Enabled = false;
                frameRate.Text = "";
            }
            else
            {
                frameRateChk.Checked = frameRate.Enabled = true;
                frameRate.Text = cjo.FPS;
            }

            embedSrtChaptersChk.Checked = cjo.embedSubtitlesChapters;

            skipHistoryChk.Checked = cjo.checkReprocessingHistory; // First
            skipReprocessChk.Checked = cjo.skipReprocessing; // Second
            autoIncFilenameChk.Checked = cjo.autoIncrementFilename;

            xmlChk.Checked = cjo.extractXML;
            writeMetadataChk.Checked = cjo.writeMetadata;
            disableCroppingChk.Checked = cjo.disableCropping;
            downloadSeriesChk.Checked = cjo.downloadSeriesDetails; // First
            seriesButton.Enabled = downloadSeriesChk.Checked; // Second
            skipCopyChk.Checked = cjo.skipCopyBackup;
            skipRemuxChk.Checked = cjo.skipRemuxing;
            forceShowTypeCbo.Text = cjo.forceShowType.ToString();
            drmFilterCbo.Text = cjo.metaDRMSelection.ToString();
            ignoreCopyProtectionChk.Checked = cjo.ignoreCopyProtection;
            drcChk.Checked = cjo.drc;
            hardwareEncodingChk.Checked = cjo.preferHardwareEncoding;
            tempFldrPath.Text = cjo.workingPath; //Temp folder path

            comskipINIPath.Text = cjo.comskipIni;

            if (cjo.commercialRemoval == CommercialRemovalOptions.Comskip) // Comskip
                comskipINIPath.Enabled = comskipIniCmd.Enabled = true;
            else
                comskipINIPath.Enabled = comskipIniCmd.Enabled = false;

            // RENAME ONLY CHECK - DISABLE OTHER CONTROLS: Do these in the end since they control other check boxes
            if (cjo.renameOnly)
                frameRateChk.Enabled = audioOffsetChk.Enabled = audioOffset.Enabled = startTrimChk.Enabled = startTrim.Enabled = endTrimChk.Enabled = endTrim.Enabled = disableCroppingChk.Enabled = drcChk.Enabled = embedSrtChaptersChk.Enabled = writeMetadataChk.Enabled = hardwareEncodingChk.Enabled = ignoreCopyProtectionChk.Enabled
                    = false;
            else
                drmFilterCbo.Enabled
                    = false;
        }

        /// <summary>
        /// Writes the settings from the form to the Conversion Job Options
        /// </summary>
        private void WriteSettings(ConversionJobOptions cjo)
        {
            cjo.ignoreCopyProtection = ignoreCopyProtectionChk.Checked;
            cjo.skipCopyBackup = skipCopyChk.Checked;
            cjo.skipRemuxing = skipRemuxChk.Checked;
            cjo.skipReprocessing = skipReprocessChk.Checked;
            cjo.autoIncrementFilename = autoIncFilenameChk.Checked;
            cjo.checkReprocessingHistory = skipHistoryChk.Checked;
            cjo.comskipIni = comskipINIPath.Text;
            cjo.downloadSeriesDetails = downloadSeriesChk.Checked;
            cjo.forceShowType = (ShowType)forceShowTypeCbo.SelectedIndex;
            cjo.metaDRMSelection = (DRMType)drmFilterCbo.SelectedIndex;
            cjo.extractXML = xmlChk.Checked;
            cjo.writeMetadata = writeMetadataChk.Checked;
            cjo.insertQueueTop = insertTopChk.Checked;
            cjo.disableCropping = disableCroppingChk.Checked;
            cjo.drc = drcChk.Checked;
            cjo.preferHardwareEncoding = hardwareEncodingChk.Checked;
            cjo.embedSubtitlesChapters = embedSrtChaptersChk.Checked;
            cjo.tivoMAKKey = makKey.Text;
            cjo.workingPath = tempFldrPath.Text.Trim();
            cjo.enabled = true; // By default tasks are enabled

            if (!monitorTaskNameMatchChk.Checked)
                cjo.monitorTaskNames = null; // clear it if the box is unchecked

            if (startTrimChk.Checked && startTrim.Text != "")
                int.TryParse(startTrim.Text, out cjo.startTrim);
            else
                cjo.startTrim = 0;

            if (endTrimChk.Checked && endTrim.Text != "")
                int.TryParse(endTrim.Text, out cjo.endTrim);
            else
                cjo.endTrim = 0;

            if (extractCCAdvOpts.Enabled == false)
                cjo.extractCC = "";
            else if (extractCCAdvOpts.Checked == false)
                cjo.extractCC = "default";
            else
                cjo.extractCC = ccField.Text + "," + ccChannel.Text; // Field,Channel

            double.TryParse(ccOffset.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out cjo.ccOffset);

            if (audioOffsetChk.Checked == false)
                cjo.audioOffset = 0;
            else
                double.TryParse(audioOffset.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out cjo.audioOffset);

            if (frameRateChk.Checked == false)
                cjo.FPS = "";
            else
                cjo.FPS = frameRate.Text.Trim();
        }

        private void oKcmd_Click(object sender, EventArgs e)
        {
            if (makKey.Text.Length > 0)
            {
                if (makKey.Text.Length != 10)
                {
                    MessageBox.Show(Localise.GetPhrase("The TiVO MAK key should be 10 digits"), Localise.GetPhrase("Invalid TiVO MAK Key"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            WriteSettings(_cjo);

            this.Close();
        }

        private void startTrimChk_CheckedChanged(object sender, EventArgs e)
        {
            startTrim.Enabled = startTrimChk.Checked;
        }

        private void audioOffsetChk_CheckedChanged(object sender, EventArgs e)
        {
            audioOffset.Enabled = audioOffsetChk.Checked;
        }

        private void frameRateChk_CheckedChanged(object sender, EventArgs e)
        {
            frameRate.Enabled = frameRateChk.Checked;
        }

        private void endTrimChk_CheckedChanged(object sender, EventArgs e)
        {
            endTrim.Enabled = endTrimChk.Checked;
        }

        private void extractCCAdvOpts_CheckedChanged(object sender, EventArgs e)
        {
            if (extractCCAdvOpts.Checked == true)
            {
                ccChannel.Enabled = true;
                ccField.Enabled = true;
            }
            else
            {
                ccChannel.Enabled = false;
                ccField.Enabled = false;
            }
        }

        private void comskipIniCmd_Click(object sender, EventArgs e)
        {
            openFileDialog.AutoUpgradeEnabled = true;
            openFileDialog.Multiselect = false;
            openFileDialog.FileName = "";
            openFileDialog.Filter = "INI Files|*.ini|All Files|*.*";
            if (File.Exists(comskipINIPath.Text))
                openFileDialog.InitialDirectory = Path.GetDirectoryName(comskipINIPath.Text);
            openFileDialog.ShowDialog();
            if (openFileDialog.FileName != "")
                comskipINIPath.Text = openFileDialog.FileName;
        }

        private void tempFldrCmd_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = tempFldrPath.Text;
            folderBrowserDialog.ShowDialog();
            if (folderBrowserDialog.SelectedPath != "")
                tempFldrPath.Text = folderBrowserDialog.SelectedPath;
        }

        private void downloadSeriesChk_CheckedChanged(object sender, EventArgs e)
        {
            seriesButton.Enabled = downloadSeriesChk.Checked;
        }

        private void monitorTaskNameMatchChk_CheckedChanged(object sender, EventArgs e)
        {
            if (!_loading)
            {
                if (monitorTaskNameMatchChk.Checked)
                {
                    ConversionTaskMonitorTasksListForm mList = new ConversionTaskMonitorTasksListForm(_monitorTasks, _cjo);
                    mList.ShowDialog();

                    // Reload the settings
                    _loading = true; // Needed so we don't go into infinite loop
                    ReadSettings(_cjo);
                    _loading = false;
                }
            }
        }

        private void skipReprocessChk_CheckedChanged(object sender, EventArgs e)
        {
            skipHistoryChk.Enabled = skipReprocessChk.Checked;
            autoIncFilenameChk.Enabled = !skipReprocessChk.Checked;
            if (!skipReprocessChk.Checked)
                skipHistoryChk.Checked = false;
            else
                autoIncFilenameChk.Checked = false;
        }

        private void seriesButton_Click(object sender, EventArgs e)
        {
            ConversionTaskSetSeriesIDForm seriesId = new ConversionTaskSetSeriesIDForm(_cjo);
            seriesId.ShowDialog();
        }

        private void makKey_TextChanged(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;

            if (char.IsDigit(e.KeyChar) && makKey.Text.Length >= 10)
            {
                MessageBox.Show(Localise.GetPhrase("The TiVO MAK key should be 10 digits"), Localise.GetPhrase("Invalid TiVO MAK Key"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Handled = true;
            }
        }

        private void trim_TextChanged(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void offset_TextChanged(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && !(e.KeyChar == '.') && !(e.KeyChar == '-'))
                e.Handled = true;
        }

        private void framerate_TextChanged(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && !(e.KeyChar == '.') && !(e.KeyChar == '/'))
                e.Handled = true;
        }

        private void skipCopyChk_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) // nothing to do if the form is loading
                return;

            if (skipCopyChk.Checked)
            {
                DialogResult result =
                    MessageBox.Show(
                        Localise.GetPhrase("CHECK THIS BOX AT YOUR OWN RISK.\r\nDO NOT CHECK THIS BOX UNLESS YOU KNOW WHAT YOU ARE DOING.\r\nCheck this box to skip making a backup of original file (copying) while converting."),
                        Localise.GetPhrase("Are you sure you want to do this?"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                    skipCopyChk.Checked = true;
                else
                    skipCopyChk.Checked = false;
            }
        }

        private void skipRemuxChk_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) // nothing to do if the form is loading
                return;

            if (skipRemuxChk.Checked)
            {
                DialogResult result =
                    MessageBox.Show(
                        Localise.GetPhrase("CHECK THIS BOX AT YOUR OWN RISK.\r\nDO NOT CHECK THIS BOX UNLESS YOU KNOW WHAT YOU ARE DOING.\r\nCheck this box to skip remuxing the original file to a TS file while converting."),
                        Localise.GetPhrase("Are you sure you want to do this?"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                    skipRemuxChk.Checked = true;
                else
                    skipRemuxChk.Checked = false;
            }
        }

        private void autoIncFilenameChk_CheckedChanged(object sender, EventArgs e)
        {
            skipReprocessChk.Enabled = !autoIncFilenameChk.Checked;
            if (autoIncFilenameChk.Checked)
                skipReprocessChk.Checked = false;
        }
    }
}
