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
    public partial class MonitorTaskForm : Form
    {
        private bool _netWarningDone = false;
        private MonitorJobOptions _mjo;
        private MCEBuddyConf _mceOptions;
        private bool _newTask = false; // Are we creating a new task
        private bool _loading;

        public MonitorTaskForm(MCEBuddyConf mceOptions, string SourceName)
        {
            InitializeComponent();

            _mceOptions = mceOptions;
            _mjo = mceOptions.GetMonitorTaskByName(SourceName);
        }

        private void SourceForm_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            _loading = true;
            LocaliseForms.LocaliseForm(this, toolTip);
            ReadSettings(ref _mjo);
            _loading = false;
        }

        /// <summary>
        /// Reads the settings from the montior job options and populates the form
        /// </summary>
        /// <param name="mjo">Refernce to a mjo object, will create a new object if it is null</param>
        private void ReadSettings(ref MonitorJobOptions mjo)
        {
            if (mjo != null)
            {
                sourceNameTxt.Text = mjo.taskName;
                if (!_newTask)
                    sourceNameTxt.ReadOnly = true;
                searchPathTxt.Text = mjo.searchPath;
                searchPatternTxt.Text = mjo.searchPattern;
                monitorSubdirChk.Checked = mjo.monitorSubdirectories;
            }
            else
            {
                mjo = new MonitorJobOptions();
                _newTask = true; // this is a new task and will remain until we hit OK

                searchPatternTxt.Text = GlobalDefs.DEFAULT_VIDEO_STRING;
            }

            CheckNetDrive(false); // No pop up while reading
        }

        /// <summary>
        /// Writes the settings to the mjo object
        /// </summary>
        /// <param name="save">True if you want to save the settings to the Global MCE object</param>
        private void WriteSettings(MonitorJobOptions mjo, bool save)
        {
            _mjo.taskName = sourceNameTxt.Text.Trim();
            _mjo.searchPath = searchPathTxt.Text;
            _mjo.monitorSubdirectories = monitorSubdirChk.Checked;

            if (searchPatternTxt.Text == "")
                _mjo.searchPattern = GlobalDefs.DEFAULT_VIDEO_STRING;
            else
                _mjo.searchPattern = searchPatternTxt.Text;

            if (save) // Are we asked to save them
                _mceOptions.AddOrUpdateMonitorTask(mjo, false);
        }

        private void CheckNetDrive(bool popup)
        {
            string newPath = Util.Net.GetUNCPath(searchPathTxt.Text);
            if (newPath != searchPathTxt.Text) searchPathTxt.Text = newPath;
            setConnectionCredentials.Enabled = (Net.IsUNCPath(newPath));
            if ((Net.IsUNCPath(newPath)) && (!_netWarningDone) && popup)
            {
                MessageBox.Show(
                    Localise.GetPhrase("MCEBuddy does NOT use the currently logged in user to connect to network shares, it uses its own service account.  Be sure to specify the connection user information [MCEBuddy with a key icon]"),
                    Localise.GetPhrase("Connection Reminder"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

                CredentialsForm connectionCredentialsForm = new CredentialsForm(_mjo); // Force the user to enter atleast default credentials
                connectionCredentialsForm.ShowDialog(); 
                
                _netWarningDone = true;
            }
        }

        private void SelectFolderCmd_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = searchPathTxt.Text;
            folderBrowserDialog.ShowDialog();
            if (folderBrowserDialog.SelectedPath != "")
            {
                searchPathTxt.Text = folderBrowserDialog.SelectedPath;
                CheckNetDrive(true);
            }
        }

        private void searchPathTxt_Leave(object sender, EventArgs e)
        {
            CheckNetDrive(true);
        }

        private void sourceNameTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = (e.KeyChar == '[') || (e.KeyChar == ']') || (e.KeyChar == '=') || (e.KeyChar == ',');
        }

        private void setConnectionCredentials_Click(object sender, EventArgs e)
        {
            CredentialsForm connectionCredentialsForm = new CredentialsForm(_mjo);
            connectionCredentialsForm.ShowDialog();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void oKcmd_Click(object sender, EventArgs e)
        {
            //Not required, since it interfers with UNC paths with Username and Passwords, either way paths can be invalidated on the fly
            /*if (!System.IO.Directory.Exists(searchPathTxt.Text))
            {
                MessageBox.Show("Path " + searchPathTxt.Text + " is invalid", "Invalid Path", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return;
            }*/

            string sourceName = sourceNameTxt.Text.Trim();
            if (String.IsNullOrWhiteSpace(sourceName))
            {
                MessageBox.Show(Localise.GetPhrase("Please supply a source name"), Localise.GetPhrase("No name supplied"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            searchPathTxt.Text = searchPathTxt.Text.Trim();
            if (String.IsNullOrWhiteSpace(searchPathTxt.Text))
            {
                MessageBox.Show(Localise.GetPhrase("Please supply a search path name"), Localise.GetPhrase("No name supplied"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_newTask)
            {
                if ((_mceOptions.AllMonitorTasks.FindIndex(item => item.taskName.ToLower() == sourceName.ToLower()) >= 0) || (_mceOptions.AllConversionTasks.FindIndex(item => item.taskName.ToLower() == sourceName.ToLower()) >= 0)) // check both monitor and conversion task name otherwise they may overwrite the same section in the ini file
                {
                    MessageBox.Show(Localise.GetPhrase("A source name or section") + " " + sourceNameTxt.Text + " " + Localise.GetPhrase("already exists. Please use another name"), Localise.GetPhrase("Duplicate Name"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            CheckNetDrive(true);

            // Quick validate the remaining settings with the ExpertForm
            MonitorTaskExpertSettingsForm expertSettings = new MonitorTaskExpertSettingsForm(_mjo);
            expertSettings.WriteAndValidateSettings(); // Clean up expert settings to avoid conflicts

            // Now write the settings
            WriteSettings(_mjo, true);

            this.Close();
        }

        private void expertSettingsBtn_Click(object sender, EventArgs e)
        {
            // First get the latest settings
            WriteSettings(_mjo, false); // Only temp

            MonitorTaskExpertSettingsForm expertSettings = new MonitorTaskExpertSettingsForm(_mjo);
            expertSettings.ShowDialog();

            // Now read the new settings and update
            ReadSettings(ref _mjo);
        }

    }
}
