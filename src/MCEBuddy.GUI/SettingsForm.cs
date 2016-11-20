using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.Configuration;

namespace MCEBuddy.GUI
{
    public partial class SettingsForm : Form
    {
        private int _advancedBoxCollapsedSize = 14;
        private Size advGrpSize;
        private bool loading;
        private ICore _pipeProxy;
        MCEBuddyConf _mceOptions; // Config options
        
        public SettingsForm(ICore pipeProxy, MCEBuddyConf mceOptions)
        {
            InitializeComponent();

            advGrpSize = advancedSettings.Size; // Store the value
            _pipeProxy = pipeProxy; // Store for use by other forms
            _mceOptions = mceOptions; // We will use this mcebuddy config object everywhere, not work directly on the GlobalMCEbuddyConf object

            // First get the new scale
            using (Graphics g = this.CreateGraphics())
            {
                float _scale = g.DpiX / 96; // Get the system DPI (font scaling)

                // Rescale the columns - for some reason Windows does not rescale the columns in high DPI mode
                this.monitorName.Width = (int)(this.monitorName.Width * _scale);
                this.monitorPath.Width = (int)(this.monitorPath.Width * _scale);
                this.taskName.Width = (int)(this.taskName.Width * _scale);
                this.taskPath.Width = (int)(this.taskPath.Width * _scale);

                _advancedBoxCollapsedSize = (int)(_advancedBoxCollapsedSize * _scale);
            }
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations
            
            LocaliseForms.LocaliseForm(this, toolTip, taskContextMenuStrip);

            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                string culture = "";
                try
                {
                    culture = CultureInfo.CreateSpecificCulture(ci.Name).DisplayName;
                    if (!localeCbo.Items.Contains(culture))
                        localeCbo.Items.Add(culture);
                }
                catch { }
            }

            ReadSettings();
            showAdvControls_Click(sender, e); // close the advanced settings section
        }

        private void CheckButtons()
        {
            deleteSourcePathCmd.Enabled = (sourcePathsLv.SelectedItems.Count > 0);
            changeSourcePathCmd.Enabled = (sourcePathsLv.SelectedItems.Count > 0);
            deleteTaskCmd.Enabled = (taskListLv.SelectedItems.Count > 0);
            changeTaskCmd.Enabled = (taskListLv.SelectedItems.Count > 0);

            wakeAMPMCbo.Enabled = wakeMinuteCbo.Enabled = wakeHourCbo.Enabled = (startCheck.Checked || wakeCheck.Checked);
            sunChk.Enabled = monChk.Enabled = tueChk.Enabled = wedChk.Enabled = thuChk.Enabled = friChk.Enabled = satChk.Enabled = (startCheck.Checked || wakeCheck.Checked);
            stopChk.Checked = startCheck.Checked; //Stop and Start have to work in tandem
            stopChk.Enabled = startCheck.Checked;
            stopAMPMCbo.Enabled = stopMinuteCbo.Enabled = stopHourCbo.Enabled = startCheck.Checked && stopChk.Checked;
            if (!startCheck.Enabled) stopChk.Checked = false;
        }

        /// <summary>
        /// Returns the hour set (converts AM and PM to Military style hour)
        /// </summary>
        /// <param name="hourCbo">Hour Combo Box</param>
        /// <param name="ampm">AM/PM Box</param>
        /// <returns>Military style hour, -1 if invalid</returns>
        private int GetHour(ComboBox hourCbo, ComboBox ampm)
        {
            if ((hourCbo.SelectedItem.ToString() == "") || (ampm.SelectedItem.ToString() == ""))
                return -1;

            if ((hourCbo.SelectedItem.ToString() == "12") && (ampm.SelectedItem.ToString() == "AM")) // Midnight
                return 0;
            else if ((int.Parse(hourCbo.SelectedItem.ToString()) >= 1) && (int.Parse(hourCbo.SelectedItem.ToString()) <= 11) && (ampm.SelectedItem.ToString() == "AM")) // AM
                return int.Parse(hourCbo.SelectedItem.ToString());
            else if ((hourCbo.SelectedItem.ToString() == "12") && (ampm.SelectedItem.ToString() == "PM")) // Noon
                return 12;
            else if ((int.Parse(hourCbo.SelectedItem.ToString()) >= 1) && (int.Parse(hourCbo.SelectedItem.ToString()) <= 11) && (ampm.SelectedItem.ToString() == "PM")) // PM
                return (int.Parse(hourCbo.SelectedItem.ToString()) + 12);
            else
                return -1;
        }

        /// <summary>
        /// Sets the time in AMPM based on the military style hour and minutes input
        /// </summary>
        private void SetTime(CheckBox chkBox, ComboBox hourCbo, ComboBox minuteCbo, ComboBox ampm, int hour, int minute)
        {
            if ((hour < 0) || (hour > 24)) hour = -1;
            if ((minute < 0) || (minute > 59)) minute = -1;

            if ((hour == 00) && (minute >= 0)) // Midnight
            {
                chkBox.Checked = true;
                hourCbo.SelectedItem = "12";
                minuteCbo.SelectedItem = minute.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                ampm.SelectedItem = "AM";
                hourCbo.Enabled = chkBox.Checked;
                minuteCbo.Enabled = chkBox.Checked;
            }
            else if ((hour >= 1) && (hour <= 11) && (minute >= 0)) // AM
            {
                chkBox.Checked = true;
                hourCbo.SelectedItem = hour.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                minuteCbo.SelectedItem = minute.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                ampm.SelectedItem = "AM";
                hourCbo.Enabled = chkBox.Checked;
                minuteCbo.Enabled = chkBox.Checked;
            }
            else if ((hour == 12) && (minute >= 0)) // Noon
            {
                chkBox.Checked = true;
                hourCbo.SelectedItem = "12";
                minuteCbo.SelectedItem = minute.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                ampm.SelectedItem = "PM";
                hourCbo.Enabled = chkBox.Checked;
                minuteCbo.Enabled = chkBox.Checked;
            }
            else if ((hour >= 13) && (hour <= 23) && (minute >= 0)) // PM
            {
                chkBox.Checked = true;
                hourCbo.SelectedItem = (hour-12).ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                minuteCbo.SelectedItem = minute.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                ampm.SelectedItem = "PM";
                hourCbo.Enabled = chkBox.Checked;
                minuteCbo.Enabled = chkBox.Checked;
            }
            else
            {
                chkBox.Checked = false;
            }
        }

        private void ReadMonitorTaskSettings()
        {
            // Monitor search tasks
            sourcePathsLv.Items.Clear();
            foreach (MonitorJobOptions mjo in _mceOptions.AllMonitorTasks)
            {
                string searchRecord = mjo.taskName;
                string[] srItem = new string[sourcePathsLv.Columns.Count];
                srItem[sourcePathsLv.Columns["monitorName"].Index] = searchRecord;
                srItem[sourcePathsLv.Columns["monitorPath"].Index] = mjo.searchPath;
                sourcePathsLv.Items.Add(new ListViewItem(srItem));
            }

            CheckButtons();
        }

        private void ReadConversionTaskSettings()
        {
            // Conversions Tasks
            int loc = 0;
            taskListLv.Items.Clear();
            foreach (ConversionJobOptions cjo in _mceOptions.AllConversionTasks)
            {
                string task = cjo.taskName;
                string[] srItem = new string[taskListLv.Columns.Count];
                srItem[taskListLv.Columns["taskName"].Index] = task;
                srItem[taskListLv.Columns["taskPath"].Index] = cjo.destinationPath;
                taskListLv.Items.Add(new ListViewItem(srItem));

                // Check if the task is disabled
                if (!cjo.enabled)
                    taskListLv.Items[loc].ForeColor = Color.LightGray;
                loc++;
            }

            CheckButtons();
        }

        private void ReadSettings()
        {
            loading = true;

            GeneralOptions go = _mceOptions.GeneralOptions;

            ReadMonitorTaskSettings(); // Monitor Task Settings
            ReadConversionTaskSettings(); // Conversion Task Settings

            // Start/Wake and Stop times
            SetTime(wakeCheck, wakeHourCbo, wakeMinuteCbo, wakeAMPMCbo, go.wakeHour, go.wakeMinute); 
            SetTime(startCheck, wakeHourCbo, wakeMinuteCbo, wakeAMPMCbo, go.startHour, go.startMinute);
            SetTime(stopChk, stopHourCbo, stopMinuteCbo, stopAMPMCbo, go.stopHour, go.stopMinute);
            stopChk.Enabled = startCheck.Checked;

            // Days of Week
            string[] daysOfWeek = go.daysOfWeek.Split(',');
            foreach (string dayOfWeek in daysOfWeek)
            {
                switch (dayOfWeek)
                {
                    case "Sunday":
                        sunChk.Checked = true;
                        break;

                    case "Monday":
                        monChk.Checked = true;
                        break;

                    case "Tuesday":
                        tueChk.Checked = true;
                        break;

                    case "Wednesday":
                        wedChk.Checked = true;
                        break;

                    case "Thursday":
                        thuChk.Checked = true;
                        break;

                    case "Friday":
                        friChk.Checked = true;
                        break;

                    case "Saturday":
                        satChk.Checked = true;
                        break;

                    default:
                        MessageBox.Show(Localise.GetPhrase("Invalid day of week found in configuration file"), Localise.GetPhrase("Invalid Days of Week"));
                        break;
                }
            }

            // Max concurrent
            maxConcurrentJobsCbo.SelectedItem = go.maxConcurrentJobs.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Logging
            logJobsChk.Checked = go.logJobs;

            // Delete original
            deleteOriginalChk.Checked = go.deleteOriginal;

            // Archive original
            archiveOriginalChk.Checked = go.archiveOriginal;

            // Delete converted
            deleteConvertedChk.Checked = go.deleteConverted;

            // Allow system to sleep
            allowSleepChk.Checked = go.allowSleep;

            // Suspend on Battery
            suspendOnBatteryChk.Checked = go.suspendOnBattery;

            // Minimum age
            minAgeCbo.SelectedItem = go.minimumAge.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Send eMail
            sendEmailChk.Checked = go.sendEmail;

            // Locale 
            CultureInfo ci = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                ci = new CultureInfo(go.locale);
            }
            catch (Exception)
            {
            }          

            localeCbo.SelectedIndex = 0;
            for (int i = 0; i < localeCbo.Items.Count; i++)
            {
                if (localeCbo.Items[i].ToString() == ci.DisplayName)
                {
                    localeCbo.SelectedIndex = i;
                    break;
                }
            }
            
            //Check the manip buttons
            CheckButtons();

            loading = false; // done loading the settings
        }

        private void WriteGeneralSettings()
        {
            GeneralOptions go = _mceOptions.GeneralOptions; // Get the current GO

            if (wakeCheck.Checked)
            {
                go.wakeHour = GetHour(wakeHourCbo, wakeAMPMCbo);
                go.wakeMinute = int.Parse(wakeMinuteCbo.SelectedItem.ToString());
            }
            else
            {
                go.wakeHour = go.wakeMinute = -1;
            }

            if (startCheck.Checked)
            {
                go.startHour = GetHour(wakeHourCbo, wakeAMPMCbo);
                go.startMinute = int.Parse(wakeMinuteCbo.SelectedItem.ToString());
            }
            else
            {
                go.startHour = go.startMinute = -1;
            }

            if (stopChk.Checked)
            {
                go.stopHour = GetHour(stopHourCbo, stopAMPMCbo);
                go.stopMinute = int.Parse(stopMinuteCbo.SelectedItem.ToString());
            }
            else
            {
                go.stopHour = go.stopMinute = -1;
            }

            string daysOfWeek = "";
            if (wakeCheck.Checked || startCheck.Checked)
            {
                if (sunChk.Checked)
                    daysOfWeek += (daysOfWeek == "" ? "Sunday" : ",Sunday");
                if (monChk.Checked)
                    daysOfWeek += (daysOfWeek == "" ? "Monday" : ",Monday");
                if (tueChk.Checked)
                    daysOfWeek += (daysOfWeek == "" ? "Tuesday" : ",Tuesday");
                if (wedChk.Checked)
                    daysOfWeek += (daysOfWeek == "" ? "Wednesday" : ",Wednesday");
                if (thuChk.Checked)
                    daysOfWeek += (daysOfWeek == "" ? "Thursday" : ",Thursday");
                if (friChk.Checked)
                    daysOfWeek += (daysOfWeek == "" ? "Friday" : ",Friday");
                if (satChk.Checked)
                    daysOfWeek += (daysOfWeek == "" ? "Saturday" : ",Saturday");
            }
            else
                daysOfWeek = "Sunday,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday"; // Default is all days of week
                    
            go.daysOfWeek = daysOfWeek;

            go.maxConcurrentJobs = int.Parse(maxConcurrentJobsCbo.SelectedItem.ToString());
            go.logJobs = logJobsChk.Checked;
            go.deleteOriginal = deleteOriginalChk.Checked;
            go.archiveOriginal = archiveOriginalChk.Checked;
            go.deleteConverted = deleteConvertedChk.Checked;
            go.allowSleep = allowSleepChk.Checked;
            go.suspendOnBattery = suspendOnBatteryChk.Checked;
            go.minimumAge = int.Parse(minAgeCbo.SelectedItem.ToString());
            go.sendEmail = sendEmailChk.Checked;

            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (ci.DisplayName == localeCbo.Items[localeCbo.SelectedIndex].ToString())
                {
                    go.locale = ci.Name;
                    break;
                }
            }

            // Update the settings
            _mceOptions.UpdateGeneralOptions(go, false);
        }

        private void taskList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void startCheck_CheckedChanged(object sender, EventArgs e)
        {
            CheckButtons();

            if (!loading)
                if (startCheck.Checked || wakeCheck.Checked)
                    MessageBox.Show(Localise.GetPhrase("Make sure you press 'Start' after closing this window, otherwise Scheduling and Wake will not work."), Localise.GetPhrase("Enabling Scheduling and Wake"), MessageBoxButtons.OK);
        }

        private void wakeUpCheck_CheckedChanged(object sender, EventArgs e)
        {
            CheckButtons();
        }

        private bool CheckValidTimes()
        {
            // Check if atleast one day of the week is enabled
            if (!(sunChk.Checked || monChk.Checked || tueChk.Checked || wedChk.Checked || thuChk.Checked || friChk.Checked || satChk.Checked))
            {
                MessageBox.Show(Localise.GetPhrase("Atleast one day of the week must be enabled"), Localise.GetPhrase("Invalid Days of Week"));
                return false;
            }

            // check if any of the times aren't entered
            if (((wakeCheck.Checked || startCheck.Checked) && ("" == wakeHourCbo.Text || "" == wakeMinuteCbo.Text || "" == wakeAMPMCbo.Text)) || ((stopChk.Checked) && ("" == stopHourCbo.Text || "" == stopMinuteCbo.Text || "" == stopAMPMCbo.Text)))
            {
                MessageBox.Show(Localise.GetPhrase("Please enter a valid time"), Localise.GetPhrase("Invalid Time"));
                return false;
            }

            if (stopChk.Enabled) // check if end time > start time
            {
                DateTime rn = DateTime.Now;
                DateTime startTime = new DateTime(rn.Year, rn.Month, rn.Day, GetHour(wakeHourCbo, wakeAMPMCbo), int.Parse(wakeMinuteCbo.Text), 0);
                DateTime endTime = new DateTime(rn.Year, rn.Month, rn.Day, GetHour(stopHourCbo, stopAMPMCbo), int.Parse(stopMinuteCbo.Text), 0);

                if (startTime >= endTime)
                {
                    if (MessageBox.Show(Localise.GetPhrase("Stop time is after Start time. Assuming the Stop time is during the next day, is that correct?"),
                        Localise.GetPhrase("Time Check"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.No)
                        return false;
                }
            }

            return true;
        }

        private void oKcmd_Click(object sender, EventArgs e)
        {
            if (!CheckValidTimes()) return;

            WriteGeneralSettings(); // Write the latest settings

            GeneralOptions go = _mceOptions.GeneralOptions; // Get the current copy of GO

            // Validate and update the expert form settings
            SettingsSystemSettingsForm esf = new SettingsSystemSettingsForm(go);
            esf.WriteAndValidateSettings();

            _mceOptions.UpdateGeneralOptions(go, false); // Save the updated settings

            this.Close(); // locale not changed, we're good
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void sourcePathsLv_SelectedIndexChanged(object sender, EventArgs e)
        {
            CheckButtons();
        }

        private void taskListLv_Click(object sender, EventArgs e)
        {

        }

        private void taskListLv_SelectedIndexChanged(object sender, EventArgs e)
        {
            CheckButtons();
        }

        private void addConversionTaskCmd_Click(object sender, EventArgs e)
        {

            ConversionTaskForm conversionTaskForm=new ConversionTaskForm(_mceOptions, "");
            conversionTaskForm.ShowDialog();
            ReadConversionTaskSettings();
        }

        private void changeMonitorTaskCmd_Click(object sender, EventArgs e)
        {
            string selectedSourceName = sourcePathsLv.SelectedItems[0].SubItems[0].Text;
            MonitorTaskForm sourceForm = new MonitorTaskForm(_mceOptions, selectedSourceName);
            sourceForm.ShowDialog();
            ReadMonitorTaskSettings();
        }

        private void addMonitorTaskCmd_Click(object sender, EventArgs e)
        {
            MonitorTaskForm sourceForm = new MonitorTaskForm(_mceOptions, "");
            sourceForm.ShowDialog();
            ReadMonitorTaskSettings();
        }

        private void changeConversionTaskCmd_Click(object sender, EventArgs e)
        {
            string selectedTaskName =  taskListLv.SelectedItems[0].SubItems[0].Text;
            ConversionTaskForm conversionTaskForm = new ConversionTaskForm(_mceOptions, selectedTaskName);
            conversionTaskForm.ShowDialog();
            ReadConversionTaskSettings();
        }

        private void deleteMonitorTaskCmd_Click(object sender, EventArgs e)
        {
            if (sourcePathsLv.SelectedItems.Count > 0)
            {
                if (MessageBox.Show(
                    Localise.GetPhrase("Are you sure you want to delete this task?"),
                    Localise.GetPhrase("Delete Task"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        string selectedSourceName = sourcePathsLv.SelectedItems[0].SubItems[0].Text;
                        _mceOptions.DeleteMonitorTask(selectedSourceName, false);
                        ReadMonitorTaskSettings();
                    }
            }
        }

        private void deleteConversionTaskCmd_Click(object sender, EventArgs e)
        {
            if (taskListLv.SelectedItems.Count > 0)
            {
                int enabledCount = 0;

                foreach (ListViewItem item in taskListLv.Items)
                {
                    if (_mceOptions.GetConversionTaskByName(item.SubItems[0].Text).enabled)
                        enabledCount++;
                }

                // Cannot disable/delete all conversion tasks, if we are deleting a disabled task no issues (disable click handler will handle not allowing all tasks to be disabled)
                if (enabledCount <= 1 && _mceOptions.GetConversionTaskByName(taskListLv.SelectedItems[0].SubItems[0].Text).enabled)
                {
                    MessageBox.Show(Localise.GetPhrase("Atleast one Conversion Task must be enabled"), Localise.GetPhrase("Delete Task"));
                    return;
                }

                if (MessageBox.Show(
                    Localise.GetPhrase("Are you sure you want to delete this task?"),
                    Localise.GetPhrase("Delete Task"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    string selectedTaskName = taskListLv.SelectedItems[0].SubItems[0].Text;
                    _mceOptions.DeleteConversionTask(selectedTaskName, false);
                    ReadConversionTaskSettings();
                }
            }
        }

        private void sourcePathsLv_DoubleClick(object sender, EventArgs e)
        {
            if (sourcePathsLv.SelectedItems.Count > 0) changeMonitorTaskCmd_Click(null, null);
        }

        private void taskListLv_DoubleClick(object sender, EventArgs e)
        {
            if (taskListLv.SelectedItems.Count > 0) changeConversionTaskCmd_Click(null, null);
        }

        private void stopChk_CheckedChanged(object sender, EventArgs e)
        {
            CheckButtons();
        }

        private void deleteOriginalCheckedChanged(object sender, EventArgs e)
        {
            if (loading) // nothing to do if the form is loading
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
                    deleteConvertedChk.Checked = archiveOriginalChk.Checked = false; //only delete or archive can be checked at a time
                }
                else
                    deleteOriginalChk.Checked = false;
            }
        }

        private void deleteConvertedChk_CheckedChanged(object sender, EventArgs e)
        {
            if (loading) // nothing to do if the form is loading
                return;

            if (deleteConvertedChk.Checked)
            {
                DialogResult result =
                    MessageBox.Show(
                        Localise.GetPhrase("Do you wish MCEBuddy to delete each converted file when the original file has been deleted?"),
                        Localise.GetPhrase("Confirm sync conversions?"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    deleteConvertedChk.Checked = true;
                    deleteOriginalChk.Checked = archiveOriginalChk.Checked = false; //only delete or archive can be checked at a time
                }
                else
                    deleteConvertedChk.Checked = false;
            }
        }

        private void archiveOriginalCheckedChanged(object sender, EventArgs e)
        {
            if (archiveOriginalChk.Checked) // only delete or archive can be checked at a time
                deleteConvertedChk.Checked = deleteOriginalChk.Checked = false;
        }

        private void showAdvControls_Click(object sender, EventArgs e)
        {
            if (advancedSettings.Size == advGrpSize) // Now collapse the settings
            {
                expertSettingsBtn.Visible = false; // Hide expert settings button
                advancedSettings.Size = new Size(advGrpSize.Width, _advancedBoxCollapsedSize);
                oKcmd.Location = new Point(oKcmd.Location.X, oKcmd.Location.Y - advGrpSize.Height + _advancedBoxCollapsedSize);
                cmdCancel.Location = new Point(cmdCancel.Location.X, cmdCancel.Location.Y - advGrpSize.Height + _advancedBoxCollapsedSize);
                expertSettingsBtn.Location = new Point(expertSettingsBtn.Location.X, expertSettingsBtn.Location.Y - advGrpSize.Height + _advancedBoxCollapsedSize);
                this.Height = this.Height - advGrpSize.Height + _advancedBoxCollapsedSize;
            }
            else // Now expand the settings
            {
                advancedSettings.Size = advGrpSize;
                oKcmd.Location = new Point(oKcmd.Location.X, oKcmd.Location.Y + advGrpSize.Height - _advancedBoxCollapsedSize);
                cmdCancel.Location = new Point(cmdCancel.Location.X, cmdCancel.Location.Y + advGrpSize.Height - _advancedBoxCollapsedSize);
                expertSettingsBtn.Location = new Point(expertSettingsBtn.Location.X, expertSettingsBtn.Location.Y + advGrpSize.Height - _advancedBoxCollapsedSize);
                this.Height = this.Height + advGrpSize.Height - _advancedBoxCollapsedSize;
                expertSettingsBtn.Visible = true; // Show expert settings button
            }
        }

        private void sendEmailChk_CheckedChanged(object sender, EventArgs e)
        {
            if (sendEmailChk.Checked && !loading)
            {
                GeneralOptions go = _mceOptions.GeneralOptions; // Get the current copy of GO

                eMailSettingsForm eMailForm = new eMailSettingsForm(_pipeProxy, go);
                if (eMailForm.ShowDialog() == System.Windows.Forms.DialogResult.Abort)
                    sendEmailChk.Checked = false; // the user did not enter any server address
                else
                    _mceOptions.UpdateGeneralOptions(go, false); // Save the updated settings
            }
        }

        private void taskNameTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = (e.KeyChar == '[') || (e.KeyChar == ']') || (e.KeyChar == '=') || (e.KeyChar == ',');
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InputBox.InputBoxResult renameText = InputBox.Show(Localise.GetPhrase("Enter the new task name"), Localise.GetPhrase("Rename Task"), "", null, new KeyPressEventHandler(taskNameTxt_KeyPress));

            if (!renameText.OK || String.IsNullOrWhiteSpace(renameText.Text))
                return;

            // Get the existing conversion job
            string selectedTaskName = taskListLv.SelectedItems[0].SubItems[0].Text;
            ConversionJobOptions cjo = _mceOptions.GetConversionTaskByName(selectedTaskName);

            // Delete the conversion job
            _mceOptions.DeleteConversionTask(selectedTaskName, false);

            // Update the taskname and create a new task with it
            cjo.taskName = renameText.Text.Trim();
            _mceOptions.AddOrUpdateConversionTask(cjo, false);

            // Reload the tasks
            ReadConversionTaskSettings();
        }

        private void disableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int enabledCount = 0;

            foreach (ListViewItem item in taskListLv.Items)
            {
                if (_mceOptions.GetConversionTaskByName(item.SubItems[0].Text).enabled)
                    enabledCount++;
            }

            // Cannot disable all conversion tasks
            if (enabledCount <= 1)
            {
                MessageBox.Show(Localise.GetPhrase("Atleast one Conversion Task must be enabled"), Localise.GetPhrase("Conversion Task"));
                return;
            }

            string selectedTaskName = taskListLv.SelectedItems[0].SubItems[0].Text;
            ConversionJobOptions cjo = _mceOptions.GetConversionTaskByName(selectedTaskName);

            // Set it to disable in the config file
            cjo.enabled = false;
            _mceOptions.AddOrUpdateConversionTask(cjo, false);

            // Change the color
            taskListLv.SelectedItems[0].ForeColor = Color.LightGray;
        }

        private void enableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string selectedTaskName = taskListLv.SelectedItems[0].SubItems[0].Text;

            // Set it to enable in the config file
            ConversionJobOptions cjo = _mceOptions.GetConversionTaskByName(selectedTaskName);
            cjo.enabled = true;
            _mceOptions.AddOrUpdateConversionTask(cjo, false);

            // Change the color
            taskListLv.SelectedItems[0].ForeColor = Color.Black;
        }

        private void expertSettingsBtn_Click(object sender, EventArgs e)
        {
            if (!CheckValidTimes()) return;

            WriteGeneralSettings(); // Get the changes to the general options object before getting into expert settings so it is reflected (don't save it)

            GeneralOptions go = _mceOptions.GeneralOptions; // Get a copy of the GO
            SettingsSystemSettingsForm esf = new SettingsSystemSettingsForm(go);
            esf.ShowDialog();

            _mceOptions.UpdateGeneralOptions(go, false); // Update it
        }

        private void upToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfSettings config = _mceOptions.ConfigSettings; // Get the entire configuration
            
            string selectedTaskName = taskListLv.SelectedItems[0].SubItems[0].Text;
            int index = config.conversionTasks.FindIndex(item => item.taskName == selectedTaskName);
            if (index == 0)
                return; // Already at top, nothing to do
            else
            {
                ConversionJobOptions cjo = config.conversionTasks[index].Clone(); // Save it first
                config.conversionTasks.RemoveAt(index); // Remove it
                config.conversionTasks.Insert(index - 1, cjo); // Insert it one up
            }
            
            _mceOptions.UpdateConfigOptions(config, false); // Update with new config

            // Reload the tasks
            ReadConversionTaskSettings();
        }

        private void downToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfSettings config = _mceOptions.ConfigSettings; // Get the entire configuration

            string selectedTaskName = taskListLv.SelectedItems[0].SubItems[0].Text;
            int index = config.conversionTasks.FindIndex(item => item.taskName == selectedTaskName);
            if (index == taskListLv.Items.Count - 1)
                return; // Already at bottom, nothing to do
            else
            {
                ConversionJobOptions cjo = config.conversionTasks[index].Clone(); // Save it first
                config.conversionTasks.RemoveAt(index); // Remove it
                config.conversionTasks.Insert(index + 1, cjo); // Insert it one down
            }
            
            _mceOptions.UpdateConfigOptions(config, false); // Reinitialize it with new config

            // Reload the tasks
            ReadConversionTaskSettings();
        }
    }
}
