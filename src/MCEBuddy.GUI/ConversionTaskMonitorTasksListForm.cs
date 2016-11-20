using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using MCEBuddy.Configuration;
using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.GUI
{
    public partial class ConversionTaskMonitorTasksListForm : Form
    {
        private ConversionJobOptions _cjo;
        private List<MonitorJobOptions> _monitorTasks;

        public ConversionTaskMonitorTasksListForm(List<MonitorJobOptions> monitorTasks, ConversionJobOptions cjo)
        {
            InitializeComponent();

            _monitorTasks = monitorTasks;
            _cjo = cjo;
        }

        private void MonitorTasksListForm_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            int count = 1;
            foreach (MonitorJobOptions mjo in _monitorTasks)
            {
                CheckBox checkBox = new System.Windows.Forms.CheckBox();
                checkBox.AutoSize = true;
                checkBox.Location = new System.Drawing.Point(43, 25 * count);
                checkBox.Name = "mjoBox" + count.ToString();
                checkBox.Size = new System.Drawing.Size(134, 17);
                checkBox.TabIndex = count;
                checkBox.Text = mjo.taskName;
                checkBox.UseVisualStyleBackColor = true;
                
                // If nothing is there then we assume all are checked
                if (_cjo.monitorTaskNames != null)
                {
                    if (_cjo.monitorTaskNames.Contains(mjo.taskName))
                        checkBox.Checked = true;
                    else
                        checkBox.Checked = false;
                }
                else
                    checkBox.Checked = true;
                
                Controls.Add(checkBox);

                count++;
            }

            // Move the other button accordingly
            okCmd.Location = new Point(okCmd.Location.X, okCmd.Location.Y  + (25 * count));
            cancelCmd.Location = new Point(cancelCmd.Location.X, cancelCmd.Location.Y + (25 * count));

            // Localize the text
            LocaliseForms.LocaliseForm(this, toolTip);
        }

        private void okCmd_Click(object sender, EventArgs e)
        {
            List<string> mjoTaskList = new List<string>();

            foreach (Control ctl in Controls)
            {
                if (ctl.GetType() == typeof(CheckBox))
                {
                    if (((CheckBox)ctl).Checked)
                        mjoTaskList.Add(((CheckBox)ctl).Text);
                }
            }

            if (mjoTaskList.Count < 1)
            {
                MessageBox.Show(Localise.GetPhrase("Select atleast one Monitor Location name"), Localise.GetPhrase("Invalid selection"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            // If all are selected then we set to null (process all monitor tasks
            // This is important since each time we add a new monitor task we should not need to update conversion tasks who have NOT selected ANY monitor tasks (null is interpreted as process all monitor tasks, so new ones added are automatically processed)
            if (mjoTaskList.Count == _monitorTasks.Count) // i.e. all tasks are selected, so we write a null
            {
                if (MessageBox.Show(Localise.GetPhrase("You have selected all the current Monitor Locations.\r\nDo you want this conversion task to automatically process any new Monitor Locations added in future?"), Localise.GetPhrase("New monitor locations"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes)
                    _cjo.monitorTaskNames = null;
                else
                    _cjo.monitorTaskNames = mjoTaskList.ToArray();
            }
            else
                _cjo.monitorTaskNames = mjoTaskList.ToArray();
        }
    }
}
