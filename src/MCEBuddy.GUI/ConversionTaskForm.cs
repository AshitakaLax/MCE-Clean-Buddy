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
using MCEBuddy.Util;
using MCEBuddy.Configuration;

namespace MCEBuddy.GUI
{
    public partial class ConversionTaskForm : Form
    {
        private int[] _resolutions = new int[] {320, 480, 576, 640, 720, 1280, 1440, 1920, 7680};
        private int _advancedBoxCollapsedSize = 14;
        private bool _netWarningDone = false;
        private Size _advGrpSize;
        private ConversionJobOptions _cjo;
        private MCEBuddyConf _mceOptions;
        private bool _loading = false;
        private bool _newTask = false; // Are we creating a new task

        public ConversionTaskForm(MCEBuddyConf mceOptions, string taskName)
        {
            InitializeComponent();

            maxWidthBar.Maximum = _resolutions.Length - 1;
            _advGrpSize = advancedSettings.Size; // Store the value
            _mceOptions = mceOptions;
            _cjo = _mceOptions.GetConversionTaskByName(taskName);

            // First get the new scale
            using (Graphics g = this.CreateGraphics())
            {
                float _scale = g.DpiX / 96; // Get the system DPI (font scaling)

                _advancedBoxCollapsedSize = (int)(_advancedBoxCollapsedSize * _scale);
            }
        }

        private bool ShowAnalyzerInstalled()
        {
            return StatusForm.showAnalyzerInstalled;
        }

        private void ConversionTaskForm_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            _loading = true;

            LocaliseForms.LocaliseForm(this, toolTip); // Localize form
            ReadSettings(ref _cjo); // Read settings

            showAdvControls_Click(sender, e); // close the advanced settings group
            _loading = false;
        }

        /// <summary>
        /// Reads the settings from the conversion job options and populates the form
        /// </summary>
        /// <param name="cjo">Refernce to a cjo object, will create a new object if it is null</param>
        private void ReadSettings(ref ConversionJobOptions cjo)
        {
            // Load the languages list
            for (int i = 0; i < ISO639_3.ISOLanguageCodesLength; i++)
            {
                if (!langBox.Items.Contains(ISO639_3.ISOLanguageCodes[i, 2]))
                    langBox.Items.Add(ISO639_3.ISOLanguageCodes[i, 2]);
            }

            profileCbo.Items.Clear(); // We need to clear the list and start over other the "-----" getting messed up
            foreach (string[] profileSummary in StatusForm.profilesSummary)
            {
                profileCbo.Items.Add(profileSummary[0]); // Name of profile
            }

            if (cjo != null)
            {

                taskNameTxt.Text = cjo.taskName;
                if (!_newTask)
                    taskNameTxt.ReadOnly = true; // Mark read only for an existing task

                string profile = cjo.profile;
                if (profileCbo.Items.Contains(profile))
                {
                    profileCbo.SelectedItem = profile;                   
                }
                else
                {
                    MessageBox.Show(Localise.GetPhrase("Cannot find selected profile in profiles.conf"), Localise.GetPhrase("Profile Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    profileCbo.SelectedIndex = 0;
                }
                
                destinationPathTxt.Text = cjo.destinationPath;
                string newPath = Util.Net.GetUNCPath(destinationPathTxt.Text);
                if (newPath != destinationPathTxt.Text) destinationPathTxt.Text = newPath;
                setConnectionCredentials.Enabled = (Net.IsUNCPath(newPath));

                maxWidthTxt.Text = cjo.maxWidth.ToString(System.Globalization.CultureInfo.InvariantCulture);
                
                double qualityMultiplier = cjo.qualityMultiplier;
                qualityBar.Value = QualityToRange(qualityMultiplier);
                UpdateQualityLabel();

                double volumeMultiplier = cjo.volumeMultiplier;
                volumeBar.Value = VolumeToRange(volumeMultiplier);
                UpdateVolumeLabel();

                singleAudioTrackChk.Checked = cjo.encoderSelectBestAudioTrack;

                detectAdsCbo.SelectedIndex = (int) cjo.commercialRemoval;
                if (cjo.commercialRemoval != CommercialRemovalOptions.None) // Only if commercial detection is enabled, we check for commercial skip cutting
                {
                    if (!cjo.renameOnly)
                        commercialSkipCutChk.Enabled = true;
                    else
                        commercialSkipCutChk.Enabled = false;
                    commercialSkipCutChk.Checked = cjo.commercialSkipCut;
                }
                else
                {
                    commercialSkipCutChk.Enabled = commercialSkipCutChk.Checked = false;
                }

                fileMatchTxt.Text = cjo.fileSelection;
                metaShowMatchTxt.Text = cjo.metaShowSelection;
                metaNetworkMatchTxt.Text = cjo.metaNetworkSelection;
                switch (cjo.metaShowTypeSelection)
                {
                    case ShowType.Movie:
                        metaShowTypeCbo.SelectedIndex = 1;
                        break;

                    case ShowType.Series:
                        metaShowTypeCbo.SelectedIndex = 2;
                        break;

                    case ShowType.Sports:
                        metaShowTypeCbo.SelectedIndex = 3;
                        break;

                    case ShowType.Default:
                    default:
                        metaShowTypeCbo.SelectedIndex = 0;
                        break; // No show type meta selection
                }

                langBox.Text = ISO639_3.GetLanguageName(cjo.audioLanguage);

                string extractCCOpts = cjo.extractCC;
                if (String.IsNullOrEmpty(extractCCOpts))
                    extractCC.Checked = false;
                else
                    extractCC.Checked = true;

                multiChannelAudioChk.Checked = !cjo.stereoAudio;
                autoDeinterlaceChk.Checked = cjo.autoDeInterlace;

                addiTunesChk.Checked = cjo.addToiTunes;
                addToWMPChk.Checked = cjo.addToWMP;

                // Do these in the end since they control other check boxes
                renameOnlyChk.Checked = cjo.renameOnly;
                altRenameBySeriesChk.Checked = cjo.altRenameBySeries;
                string renamePattern = cjo.customRenameBySeries;
                if (renamePattern != "")
                {
                    customReNamingChk.Checked = true;
                    customFileRenamePattern.Text = renamePattern;
                }

                renameBySeriesChk.Checked = cjo.renameBySeries; // Last one to be set
            }
            else
            {
                cjo = new ConversionJobOptions();
                _newTask = true; // this is a new task and will remain until we hit OK

                profileCbo.SelectedIndex = 0; // profile
                detectAdsCbo.SelectedIndex = (int)CommercialRemovalOptions.Comskip; // Comskip
                langBox.Text = ISO639_3.GetLanguageName("");
                UpdateVolumeLabel();
                UpdateQualityLabel();
                cjo.altRenameBySeries = cjo.preferHardwareEncoding = cjo.downloadSeriesDetails = cjo.downloadBanner = cjo.writeMetadata = cjo.encoderSelectBestAudioTrack = cjo.drc = true;
                double.TryParse(GlobalDefs.DEFAULT_CC_OFFSET, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out cjo.ccOffset);
                metaShowTypeCbo.SelectedIndex = 0; // All shows
            }

            CheckNetDrive(false); // No pop up while reading
        }

        /// <summary>
        /// Writes the settings to the cjo object
        /// </summary>
        /// <param name="save">True if you want to save the settings to the Global MCE object</param>
        private void WriteSettings(ConversionJobOptions cjo, bool save)
        {
            cjo.taskName = taskNameTxt.Text.Trim();
            cjo.profile = profileCbo.SelectedItem.ToString();
            cjo.destinationPath = destinationPathTxt.Text;
            cjo.addToiTunes = addiTunesChk.Checked;
            cjo.addToWMP = addToWMPChk.Checked;
            int.TryParse(maxWidthTxt.Text.ToString(), out cjo.maxWidth);
            cjo.encoderSelectBestAudioTrack = singleAudioTrackChk.Checked;
            cjo.volumeMultiplier = RangeValToVolume(volumeBar.Value);
            cjo.qualityMultiplier = RangeValToQuality(qualityBar.Value);
            cjo.commercialRemoval = (CommercialRemovalOptions)detectAdsCbo.SelectedIndex;
            cjo.commercialSkipCut = commercialSkipCutChk.Checked;
            cjo.renameBySeries = renameBySeriesChk.Checked;
            cjo.altRenameBySeries = altRenameBySeriesChk.Checked;
            cjo.renameOnly = renameOnlyChk.Checked;
            cjo.fileSelection = fileMatchTxt.Text;
            cjo.metaShowSelection = metaShowMatchTxt.Text;
            cjo.metaNetworkSelection = metaNetworkMatchTxt.Text;
            cjo.audioLanguage = ISO639_3.GetLanguageCode(langBox.Text);
            cjo.stereoAudio = !multiChannelAudioChk.Checked;
            cjo.autoDeInterlace = autoDeinterlaceChk.Checked;
            cjo.enabled = true; // By default tasks are enabled

            switch (metaShowTypeCbo.SelectedIndex)
            {
                case 1:
                    cjo.metaShowTypeSelection = ShowType.Movie;
                    break;

                case 2:
                    cjo.metaShowTypeSelection = ShowType.Series;
                    break;

                case 3:
                    cjo.metaShowTypeSelection = ShowType.Sports;
                    break;

                case 0:
                default:
                    cjo.metaShowTypeSelection = ShowType.Default;
                    break;
            }

            if (detectAdsCbo.SelectedIndex == (int)CommercialRemovalOptions.None) // Write this only if commercial detection is enabled, default is false
                cjo.commercialSkipCut = false;

            if (customReNamingChk.Checked && customFileRenamePattern.Text != "")
                cjo.customRenameBySeries = customFileRenamePattern.Text;
            else
                cjo.customRenameBySeries = "";

            if (extractCC.Checked == false)
                cjo.extractCC = "";

            // SANITY CHECKS:
            if ((detectAdsCbo.SelectedIndex != (int)CommercialRemovalOptions.None) && renameOnlyChk.Checked) // If we are naming only, then we need to ensure CommercialSkipCut is enabled if commercial detection is enabled
                cjo.commercialSkipCut = true;

            if (save) // Are we asked to save them
                _mceOptions.AddOrUpdateConversionTask(cjo, false);
        }

        private void oKcmd_Click(object sender, EventArgs e)
        {
            //Not required, since it interfers with UNC paths with Username and Passwords, either way paths can be invalidated on the fly
            /*if (!System.IO.Directory.Exists(destinationPathTxt.Text))
            {
                MessageBox.Show("Path " + destinationPathTxt.Text + " is invalid", "Invalid Path", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return;
            }*/

            string taskName = taskNameTxt.Text.Trim();
            if (String.IsNullOrWhiteSpace(taskName))
            {
                MessageBox.Show(Localise.GetPhrase("Please supply a task name"), Localise.GetPhrase("No name supplied"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            destinationPathTxt.Text = destinationPathTxt.Text.Trim();
            if (String.IsNullOrWhiteSpace(destinationPathTxt.Text))
            {
                if (MessageBox.Show(Localise.GetPhrase("No destination directory provided, converted file will be placed in original video directory"),
                    Localise.GetPhrase("No name supplied"),
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Cancel)
                    return;
            }

            if (_newTask)
            {
                if ((_mceOptions.AllConversionTasks.FindIndex(item => item.taskName.ToLower() == taskName.ToLower()) >= 0) || (_mceOptions.AllMonitorTasks.FindIndex(item => item.taskName.ToLower() == taskName.ToLower()) >= 0))// Check if the name already exists (check both monitor and conversion task otherwise they might overright the same section in the INI file)
                {
                    MessageBox.Show(Localise.GetPhrase("A task name or section") + " " + taskNameTxt.Text + " " + Localise.GetPhrase("already exists\n\rPlease use another name"), Localise.GetPhrase("Duplicate Name"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            CheckNetDrive(true);

            // Quick validate the remaining settings with the ExpertForm
            ConversionTaskExpertSettingsForm expertSettings = new ConversionTaskExpertSettingsForm(_mceOptions.AllMonitorTasks, _cjo);
            expertSettings.WriteAndValidateSettings(); // Clean up expert settings to avoid conflicts

            // Now write the settings
            WriteSettings(_cjo, true);

            this.Close();
        }

        private void CheckNetDrive(bool popup)
        {
            string newPath = Util.Net.GetUNCPath(destinationPathTxt.Text);
            if (newPath != destinationPathTxt.Text) destinationPathTxt.Text = newPath;
            setConnectionCredentials.Enabled = (Net.IsUNCPath(newPath));
            if ((Net.IsUNCPath(newPath)) && (!_netWarningDone) && (popup))
            {
                MessageBox.Show( Localise.GetPhrase("MCEBuddy does NOT use the currently logged in user to connect to network shares, it uses its own service account.  Be sure to specify the connection user information [MCEBuddy with a key icon]"), Localise.GetPhrase("Connection Reminder"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                CredentialsForm connectionCredentialsForm = new CredentialsForm(_cjo); // Force the user to enter atleast default credentials
                connectionCredentialsForm.ShowDialog();
                
                _netWarningDone = true;
            }
        }

        private void SelectFolderCmd_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = destinationPathTxt.Text;
            folderBrowserDialog.ShowDialog();
            if (folderBrowserDialog.SelectedPath != "")
            {
                destinationPathTxt.Text = folderBrowserDialog.SelectedPath;
                CheckNetDrive(true);               
            }
        }

        private void conversionCbo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (profileCbo.SelectedItem.ToString().Contains("-----"))
            {
                MessageBox.Show(Localise.GetPhrase("Please select a valid profile"), Localise.GetPhrase("Invalid Profile"));
                profileCbo.SelectedIndex = 0;
                return;
            }

            foreach (string[] profileSummary in StatusForm.profilesSummary)
            {
                if (profileCbo.SelectedItem.ToString() == profileSummary[0])
                {
                    conversionDescriptionTxt.Text = profileSummary[1];

                    // Check if we need scroll bars
                    Size tS = TextRenderer.MeasureText(conversionDescriptionTxt.Text, conversionDescriptionTxt.Font, new Size(conversionDescriptionTxt.Width, int.MaxValue), (TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl));

                    if (tS.Height > conversionDescriptionTxt.Height)
                        conversionDescriptionTxt.ScrollBars = ScrollBars.Vertical;
                    else
                        conversionDescriptionTxt.ScrollBars = ScrollBars.None;
 
                    return;
                }
            }
        }

        private int VolumeToRange(double volume)
        {
            int range = 0;
            if (volume < 0)
                range = (int)(volume * 20);
            else
                range = (int)(volume * 10);
            return range;
        }

        private double RangeValToVolume(double rangeVal)
        {
            // The acceptable range in dB for the volume gain or reduction is
            // -15db to +30db (3% to 1000%) (100% representing normal volume)
            // For display purposes the bar is from -300 to +300 (keeping for one decimal point by dividing by 10)
            double dbValue = 0;
            if (rangeVal < 0)
                dbValue = rangeVal / 20; // mapping 0 to -15db to 0 to -300 on the bar scale
            else
                dbValue = rangeVal / 10; // from 0 to 30dB we map to the bar 0 to 300, 0dB = 0 bar value

            return dbValue;
        }

        private int QualityToRange(double quality)
        {
            return (int)(quality*20);
        }

        private double RangeValToQuality( int rangeVal)
        {
            return (((double)rangeVal)/20);
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void destinationPathTxt_Leave(object sender, EventArgs e)
        {
            CheckNetDrive(true);
        }

        private void maxWidthTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            const char Delete = (char)8;
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != Delete;
        }

        private void taskNameTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = (e.KeyChar == '[') || (e.KeyChar == ']') || (e.KeyChar == '=') || (e.KeyChar == ',');
        }

        private void customFileRenamePattern_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = (e.KeyChar == '[') || (e.KeyChar == ']') || (e.KeyChar == '='); // INI file sections use [] - avoid messing up history/conf files
        }

        private void UpdateVolumeLabel()
        {
            // The acceptable range in dB for the volume gain or reduction is
            // -15db to +30db (3% to 1000%) (100% representing normal volume)
            // For display purposes the bar is from -300 to +300 (to keep for 1 decimal point)
            // We then display the number as a % increase or reduction (100% being base)
            // % = 10 ^ (dB/10)
            double dbValue = 0;
            if (volumeBar.Value < 0)
                dbValue = (double) volumeBar.Value / 20; // mapping 0 to -15db to 0 to -300 on the bar scale
            else
                dbValue = (double) volumeBar.Value / 10; // from 0 to 30dB we map to the bar 0 to 300, 0dB = 0 bar value
            
            double percentageValue = Math.Pow(10, (dbValue / 10)); // convert dB to %
            string displayValue = "";
            if (percentageValue < 10)
                displayValue = percentageValue.ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture);
            else if (percentageValue < 100)
                displayValue = percentageValue.ToString("#0.0", System.Globalization.CultureInfo.InvariantCulture);
            else
                displayValue = percentageValue.ToString("#0", System.Globalization.CultureInfo.InvariantCulture);

            volumeTxt.Text = displayValue;
        }

        private void volumeBar_Scroll(object sender, EventArgs e)
        {
            UpdateVolumeLabel();
        }

        private void UpdateQualityLabel()
        {
            int value = (qualityBar.Value - 20) * 5;
            string displayValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value >= 0) displayValue = "+" + displayValue + " %";
            if (value < 0) displayValue = displayValue + " %";
            qualityTxt.Text = displayValue;
        }

        private void qualityBar_Scroll(object sender, EventArgs e)
        {
            UpdateQualityLabel();
        }

        private void maxWidthBar_Scroll(object sender, EventArgs e)
        {
            maxWidthTxt.Text = _resolutions[maxWidthBar.Value].ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private void maxWidthTxt_TextChanged(object sender, EventArgs e)
        {
            int width = 720;
            int.TryParse(maxWidthTxt.Text, out width);
            if (width > _resolutions[_resolutions.Length-1])
            {
                maxWidthBar.Value = maxWidthBar.Maximum;
            }
            else
            {
                for (int i = 0; i < _resolutions.Length; i++)
                {
                    if (width <= _resolutions[i])
                    {
                        maxWidthBar.Value = i;
                        break;
                    }
                }
                
            }
        }

        private void customFileRenamePattern_TextChanged(object sender, EventArgs e)
        {
            string newFileName = "";
            string destinationPath = "";
            VideoTags metaData = new VideoTags() { Title = "Title", SubTitle = "Subtitle", Description = "Meta description", Network = "Network", Rating = "PG", MediaCredits = "Credits", Genres = new string[1] { "Genre" }, Season = 1, Episode = 2, BannerFile = "Banner File Path", BannerURL = "Banner URL", imdbId = "IMDBID", tmdbId = "TMDBID", tvdbId = "TVDBID", IsMovie = true, IsSports = true, OriginalBroadcastDateTime = new DateTime(2010, 1, 1), RecordedDateTime = DateTime.Now, SeriesPremiereDate = new DateTime(2000, 1, 1), CopyProtected = true, sageTV = new VideoTags.SageTV { airingDbId = "airingDb", mediaFileDbId = "mediaFileDb" } };

            try
            {
                MetaData.CustomRename.CustomRenameFilename(customFileRenamePattern.Text, ref newFileName, ref destinationPath, "Original Filename.wtv", metaData, new Log(Log.LogDestination.Null));
                customRenamePreview.Text = Path.Combine(destinationPath, newFileName); // Show the Text
            }
            catch
            {
                customRenamePreview.Text = Localise.GetPhrase("Invalid Filename Pattern");
            }
        }

        private void setConnectionCredentials_Click(object sender, EventArgs e)
        {
            CredentialsForm connectionCredentialsForm = new CredentialsForm(_cjo);
            connectionCredentialsForm.ShowDialog();
        }

        private void renameBySeriesChk_CheckedChanged(object sender, EventArgs e)
        {
            //If the rename by series box is unchecked, then uncheck/disable the Atl naming series box
            if (renameBySeriesChk.Checked == false)
            {
                altRenameBySeriesChk.Checked = altRenameBySeriesChk.Enabled = false;
                customReNamingChk.Checked = customReNamingChk.Enabled = false;
            }

            //If Rename by series is checked then enable altRenamebySeries
            if (renameBySeriesChk.Checked == true)
            {
                altRenameBySeriesChk.Enabled = true;
                customReNamingChk.Enabled = true;
            }
        }

        private void langBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            langCode.Text = ISO639_3.GetLanguageCode(langBox.Text);
        }

        private void showAdvControls_Click(object sender, EventArgs e)
        {
            if (advancedSettings.Size == _advGrpSize)
            {
                expertSettingsBtn.Visible = false; // Hide the button
                advancedSettings.Size = new Size(_advGrpSize.Width, _advancedBoxCollapsedSize);
                oKcmd.Location = new Point(oKcmd.Location.X, oKcmd.Location.Y - _advGrpSize.Height + _advancedBoxCollapsedSize);
                cmdCancel.Location = new Point(cmdCancel.Location.X, cmdCancel.Location.Y - _advGrpSize.Height + _advancedBoxCollapsedSize);
                expertSettingsBtn.Location = new Point(expertSettingsBtn.Location.X, expertSettingsBtn.Location.Y - _advGrpSize.Height + _advancedBoxCollapsedSize);
                this.Height = this.Height - _advGrpSize.Height + _advancedBoxCollapsedSize;
            }
            else
            {
                expertSettingsBtn.Visible = true; // Show the button
                advancedSettings.Size = _advGrpSize;
                oKcmd.Location = new Point(oKcmd.Location.X, oKcmd.Location.Y + _advGrpSize.Height - _advancedBoxCollapsedSize);
                cmdCancel.Location = new Point(cmdCancel.Location.X, cmdCancel.Location.Y + _advGrpSize.Height - _advancedBoxCollapsedSize);
                expertSettingsBtn.Location = new Point(expertSettingsBtn.Location.X, expertSettingsBtn.Location.Y + _advGrpSize.Height - _advancedBoxCollapsedSize);
                this.Height = this.Height + _advGrpSize.Height - _advancedBoxCollapsedSize;
            }
        }

        private void customReNaming_CheckedChanged(object sender, EventArgs e)
        {
            if (customReNamingChk.Checked == false)
            {
                customFileRenamePattern.Enabled = customRenamePreview.Enabled = false;
                if (renameBySeriesChk.Checked == true)
                    altRenameBySeriesChk.Enabled = true;
            }
            else
            {
                customFileRenamePattern.Enabled = customRenamePreview.Enabled = true;
                altRenameBySeriesChk.Checked = false;
                altRenameBySeriesChk.Enabled = false;
            }
        }

        private void removeAdsCbo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((!ShowAnalyzerInstalled()) && (detectAdsCbo.SelectedIndex == (int)CommercialRemovalOptions.ShowAnalyzer)) // ShowAnalyzer
            {
                MessageBox.Show(Localise.GetPhrase("ShowAnalyzer not found.\nPlease download and install ShowAnalyzer."), Localise.GetPhrase("ShowAnalyzer Missing"));
                detectAdsCbo.SelectedIndex = (int)CommercialRemovalOptions.Comskip;
            }

            if (detectAdsCbo.SelectedIndex == (int)CommercialRemovalOptions.None)
                commercialSkipCutChk.Checked = commercialSkipCutChk.Enabled = false; // Disable commercial skip cut
            else if (!renameOnlyChk.Checked) // Enabled commercial skip cut of we aren't only renaming (if renaming then commercial skip cut is always true when commercial removal is set)
                commercialSkipCutChk.Enabled = true;
            else if (renameOnlyChk.Checked)
            {
                commercialSkipCutChk.Enabled = false;
                commercialSkipCutChk.Checked = true; // If we are only renaming and commercials cuts are enabled then we must always skip cuts
            }
        }

        private void langCode_TextChanged(object sender, EventArgs e)
        {
            if (langCode.TextLength == 3)
            {
                try
                {
                    langBox.Text = ISO639_3.GetLanguageName(langCode.Text);
                }
                catch (Exception)
                {
                    MessageBox.Show(Localise.GetPhrase("Invalid language code, reverting to <Default> language"), Localise.GetPhrase("Invalid Language Code"));
                    langBox.Text = ISO639_3.GetLanguageName("");
                }
            }
            else if (langCode.Text == "")
                langBox.Text = ISO639_3.GetLanguageName("");
        }

        private void extractCC1_CheckedChanged(object sender, EventArgs e)
        {
            if (extractCC.Checked)
            {
                if (String.IsNullOrEmpty(_cjo.extractCC))
                    _cjo.extractCC = "default"; // Set default if not set
            }
            else // Disabled, reset
                _cjo.extractCC = ""; // Clear it
        }

        private void renameOnlyChk_CheckedChanged(object sender, EventArgs e)
        {
            if (!_loading)
            {
                if (renameOnlyChk.Checked)
                {
                    detectAdsCbo.SelectedIndex = (int)CommercialRemovalOptions.None; // Default turn off Commercial detection if we are renaming and moving only
                    extractCC.Checked = false; // Default turn off Closed caption extraction if we are renaming and moving only
                }
                else
                {
                    if (detectAdsCbo.SelectedIndex != (int)CommercialRemovalOptions.None)
                        commercialSkipCutChk.Enabled = true; // Enable commercial skip cut if we are cutting commercials
                    _cjo.commercialSkipCut = false; // Reset skip cutting when unchecking
                }
            }

            // Disable or enable as required when using rename only
            autoDeinterlaceChk.Enabled = maxWidthBar.Enabled = qualityBar.Enabled = volumeBar.Enabled = multiChannelAudioChk.Enabled = singleAudioTrackChk.Enabled = langBox.Enabled = langCode.Enabled = maxWidthTxt.Enabled = qualityTxt.Enabled = volumeTxt.Enabled
                = !renameOnlyChk.Checked;
        }

        private void expertSettingsBtn_Click(object sender, EventArgs e)
        {
            // First get the latest settings
            WriteSettings(_cjo, false); // Only temp

            ConversionTaskExpertSettingsForm expertSettings = new ConversionTaskExpertSettingsForm(_mceOptions.AllMonitorTasks, _cjo);
            expertSettings.ShowDialog();

            // Now read the new settings and update
            ReadSettings(ref _cjo);
        }

        private void commercialSkipCutChk_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading)
                return;

            if (commercialSkipCutChk.Checked)
            {
                if (!renameOnlyChk.Checked) // If we are in rename only mode then we have to skip cuts
                {
                    if (MessageBox.Show(Localise.GetPhrase("Are you sure you DO NOT want to remove commercials from the converted video and only keep the EDL file?"), Localise.GetPhrase("Skip cutting ads"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.No)
                        commercialSkipCutChk.Checked = false;
                    else
                        MessageBox.Show(Localise.GetPhrase("If you want to convert the EDL file into chapters, make sure you check the 'Add Subtitles and Chapters' option in Expert Settings."), Localise.GetPhrase("EDL to Chapters"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
