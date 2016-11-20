namespace MCEBuddy.GUI
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.oKcmd = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.sourcePathsLv = new System.Windows.Forms.ListView();
            this.monitorName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.monitorPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.taskListLv = new System.Windows.Forms.ListView();
            this.taskName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.taskPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.taskContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.disableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.moveUpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.upToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.downToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteOriginalChk = new System.Windows.Forms.CheckBox();
            this.minAgeCbo = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.advancedSettings = new System.Windows.Forms.GroupBox();
            this.deleteConvertedChk = new System.Windows.Forms.CheckBox();
            this.localeCbo = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.maxConcurrentJobsCbo = new System.Windows.Forms.ComboBox();
            this.archiveOriginalChk = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.logJobsChk = new System.Windows.Forms.CheckBox();
            this.sendEmailChk = new System.Windows.Forms.CheckBox();
            this.suspendOnBatteryChk = new System.Windows.Forms.CheckBox();
            this.allowSleepChk = new System.Windows.Forms.CheckBox();
            this.schedulingGrp = new System.Windows.Forms.GroupBox();
            this.wakeHourCbo = new System.Windows.Forms.ComboBox();
            this.startCheck = new System.Windows.Forms.CheckBox();
            this.wakeCheck = new System.Windows.Forms.CheckBox();
            this.monChk = new System.Windows.Forms.CheckBox();
            this.sunChk = new System.Windows.Forms.CheckBox();
            this.tueChk = new System.Windows.Forms.CheckBox();
            this.wedChk = new System.Windows.Forms.CheckBox();
            this.thuChk = new System.Windows.Forms.CheckBox();
            this.friChk = new System.Windows.Forms.CheckBox();
            this.satChk = new System.Windows.Forms.CheckBox();
            this.stopHourCbo = new System.Windows.Forms.ComboBox();
            this.stopAMPMCbo = new System.Windows.Forms.ComboBox();
            this.wakeAMPMCbo = new System.Windows.Forms.ComboBox();
            this.wakeMinuteCbo = new System.Windows.Forms.ComboBox();
            this.stopMinuteCbo = new System.Windows.Forms.ComboBox();
            this.stopChk = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.expertSettingsBtn = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.showAdvControls = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.deleteTaskCmd = new System.Windows.Forms.Button();
            this.addTaskCmd = new System.Windows.Forms.Button();
            this.changeTaskCmd = new System.Windows.Forms.Button();
            this.changeSourcePathCmd = new System.Windows.Forms.Button();
            this.deleteSourcePathCmd = new System.Windows.Forms.Button();
            this.addSourcePathCmd = new System.Windows.Forms.Button();
            this.taskContextMenuStrip.SuspendLayout();
            this.advancedSettings.SuspendLayout();
            this.schedulingGrp.SuspendLayout();
            this.SuspendLayout();
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(23, 10);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(87, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Monitor locations";
            this.toolTip.SetToolTip(this.label4, "MCEBuddy will monitor these directories for video files to convert automatically");
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(23, 130);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(88, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Conversion tasks";
            this.toolTip.SetToolTip(this.label5, resources.GetString("label5.ToolTip"));
            // 
            // oKcmd
            // 
            this.oKcmd.Location = new System.Drawing.Point(23, 608);
            this.oKcmd.Name = "oKcmd";
            this.oKcmd.Size = new System.Drawing.Size(75, 23);
            this.oKcmd.TabIndex = 200;
            this.oKcmd.Text = "OK";
            this.oKcmd.UseVisualStyleBackColor = true;
            this.oKcmd.Click += new System.EventHandler(this.oKcmd_Click);
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.cmdCancel.Location = new System.Drawing.Point(321, 608);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 201;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // sourcePathsLv
            // 
            this.sourcePathsLv.Activation = System.Windows.Forms.ItemActivation.TwoClick;
            this.sourcePathsLv.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.monitorName,
            this.monitorPath});
            this.sourcePathsLv.FullRowSelect = true;
            this.sourcePathsLv.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.sourcePathsLv.Location = new System.Drawing.Point(26, 26);
            this.sourcePathsLv.MultiSelect = false;
            this.sourcePathsLv.Name = "sourcePathsLv";
            this.sourcePathsLv.ShowItemToolTips = true;
            this.sourcePathsLv.Size = new System.Drawing.Size(371, 56);
            this.sourcePathsLv.TabIndex = 1;
            this.sourcePathsLv.UseCompatibleStateImageBehavior = false;
            this.sourcePathsLv.View = System.Windows.Forms.View.Details;
            this.sourcePathsLv.SelectedIndexChanged += new System.EventHandler(this.sourcePathsLv_SelectedIndexChanged);
            this.sourcePathsLv.DoubleClick += new System.EventHandler(this.sourcePathsLv_DoubleClick);
            // 
            // monitorName
            // 
            this.monitorName.Name = "monitorName";
            this.monitorName.Text = "";
            this.monitorName.Width = 130;
            // 
            // monitorPath
            // 
            this.monitorPath.Name = "monitorPath";
            this.monitorPath.Text = "";
            this.monitorPath.Width = 220;
            // 
            // taskListLv
            // 
            this.taskListLv.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.taskName,
            this.taskPath});
            this.taskListLv.ContextMenuStrip = this.taskContextMenuStrip;
            this.taskListLv.FullRowSelect = true;
            this.taskListLv.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.taskListLv.Location = new System.Drawing.Point(26, 146);
            this.taskListLv.MultiSelect = false;
            this.taskListLv.Name = "taskListLv";
            this.taskListLv.ShowItemToolTips = true;
            this.taskListLv.Size = new System.Drawing.Size(371, 91);
            this.taskListLv.TabIndex = 5;
            this.taskListLv.UseCompatibleStateImageBehavior = false;
            this.taskListLv.View = System.Windows.Forms.View.Details;
            this.taskListLv.SelectedIndexChanged += new System.EventHandler(this.taskListLv_SelectedIndexChanged);
            this.taskListLv.Click += new System.EventHandler(this.taskListLv_Click);
            this.taskListLv.DoubleClick += new System.EventHandler(this.taskListLv_DoubleClick);
            // 
            // taskName
            // 
            this.taskName.Name = "taskName";
            this.taskName.Text = "";
            this.taskName.Width = 130;
            // 
            // taskPath
            // 
            this.taskPath.Name = "taskPath";
            this.taskPath.Text = "";
            this.taskPath.Width = 220;
            // 
            // taskContextMenuStrip
            // 
            this.taskContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.disableToolStripMenuItem,
            this.enableToolStripMenuItem,
            this.renameToolStripMenuItem,
            this.moveUpToolStripMenuItem});
            this.taskContextMenuStrip.Name = "taskContextMenuStrip";
            this.taskContextMenuStrip.Size = new System.Drawing.Size(118, 92);
            // 
            // disableToolStripMenuItem
            // 
            this.disableToolStripMenuItem.Name = "disableToolStripMenuItem";
            this.disableToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.disableToolStripMenuItem.Text = "Disable";
            this.disableToolStripMenuItem.Click += new System.EventHandler(this.disableToolStripMenuItem_Click);
            // 
            // enableToolStripMenuItem
            // 
            this.enableToolStripMenuItem.Name = "enableToolStripMenuItem";
            this.enableToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.enableToolStripMenuItem.Text = "Enable";
            this.enableToolStripMenuItem.Click += new System.EventHandler(this.enableToolStripMenuItem_Click);
            // 
            // renameToolStripMenuItem
            // 
            this.renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            this.renameToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.renameToolStripMenuItem.Text = "Rename";
            this.renameToolStripMenuItem.Click += new System.EventHandler(this.renameToolStripMenuItem_Click);
            // 
            // moveUpToolStripMenuItem
            // 
            this.moveUpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.upToolStripMenuItem,
            this.downToolStripMenuItem});
            this.moveUpToolStripMenuItem.Name = "moveUpToolStripMenuItem";
            this.moveUpToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.moveUpToolStripMenuItem.Text = "Move";
            // 
            // upToolStripMenuItem
            // 
            this.upToolStripMenuItem.Name = "upToolStripMenuItem";
            this.upToolStripMenuItem.Size = new System.Drawing.Size(105, 22);
            this.upToolStripMenuItem.Text = "Up";
            this.upToolStripMenuItem.Click += new System.EventHandler(this.upToolStripMenuItem_Click);
            // 
            // downToolStripMenuItem
            // 
            this.downToolStripMenuItem.Name = "downToolStripMenuItem";
            this.downToolStripMenuItem.Size = new System.Drawing.Size(105, 22);
            this.downToolStripMenuItem.Text = "Down";
            this.downToolStripMenuItem.Click += new System.EventHandler(this.downToolStripMenuItem_Click);
            // 
            // deleteOriginalChk
            // 
            this.deleteOriginalChk.AutoSize = true;
            this.deleteOriginalChk.Location = new System.Drawing.Point(224, 22);
            this.deleteOriginalChk.Name = "deleteOriginalChk";
            this.deleteOriginalChk.Size = new System.Drawing.Size(109, 17);
            this.deleteOriginalChk.TabIndex = 21;
            this.deleteOriginalChk.Text = "Delete original file";
            this.toolTip.SetToolTip(this.deleteOriginalChk, "Deletes the original recording after a successful conversion");
            this.deleteOriginalChk.UseVisualStyleBackColor = true;
            this.deleteOriginalChk.CheckedChanged += new System.EventHandler(this.deleteOriginalCheckedChanged);
            // 
            // minAgeCbo
            // 
            this.minAgeCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.minAgeCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.minAgeCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.minAgeCbo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.minAgeCbo.FormattingEnabled = true;
            this.minAgeCbo.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "48",
            "72",
            "96",
            "120",
            "144",
            "168",
            "336",
            "720",
            "1440",
            "2160"});
            this.minAgeCbo.Location = new System.Drawing.Point(26, 43);
            this.minAgeCbo.Name = "minAgeCbo";
            this.minAgeCbo.Size = new System.Drawing.Size(48, 21);
            this.minAgeCbo.TabIndex = 20;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(11, 25);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(168, 13);
            this.label9.TabIndex = 18;
            this.label9.Text = "Min age before conversion (hours)";
            this.toolTip.SetToolTip(this.label9, "Minimum hours to wait before converting each file after the file is added to the " +
        "queue");
            // 
            // advancedSettings
            // 
            this.advancedSettings.Controls.Add(this.deleteConvertedChk);
            this.advancedSettings.Controls.Add(this.localeCbo);
            this.advancedSettings.Controls.Add(this.label9);
            this.advancedSettings.Controls.Add(this.label10);
            this.advancedSettings.Controls.Add(this.minAgeCbo);
            this.advancedSettings.Controls.Add(this.maxConcurrentJobsCbo);
            this.advancedSettings.Controls.Add(this.archiveOriginalChk);
            this.advancedSettings.Controls.Add(this.label6);
            this.advancedSettings.Controls.Add(this.deleteOriginalChk);
            this.advancedSettings.Controls.Add(this.logJobsChk);
            this.advancedSettings.Controls.Add(this.sendEmailChk);
            this.advancedSettings.Controls.Add(this.suspendOnBatteryChk);
            this.advancedSettings.Controls.Add(this.allowSleepChk);
            this.advancedSettings.Controls.Add(this.schedulingGrp);
            this.advancedSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.advancedSettings.Location = new System.Drawing.Point(24, 291);
            this.advancedSettings.Name = "advancedSettings";
            this.advancedSettings.Size = new System.Drawing.Size(371, 298);
            this.advancedSettings.TabIndex = 11;
            this.advancedSettings.TabStop = false;
            this.advancedSettings.Text = "General Settings";
            // 
            // deleteConvertedChk
            // 
            this.deleteConvertedChk.AutoSize = true;
            this.deleteConvertedChk.Location = new System.Drawing.Point(224, 68);
            this.deleteConvertedChk.Name = "deleteConvertedChk";
            this.deleteConvertedChk.Size = new System.Drawing.Size(122, 17);
            this.deleteConvertedChk.TabIndex = 23;
            this.deleteConvertedChk.Text = "Sync converted files";
            this.toolTip.SetToolTip(this.deleteConvertedChk, resources.GetString("deleteConvertedChk.ToolTip"));
            this.deleteConvertedChk.UseVisualStyleBackColor = true;
            this.deleteConvertedChk.CheckedChanged += new System.EventHandler(this.deleteConvertedChk_CheckedChanged);
            // 
            // localeCbo
            // 
            this.localeCbo.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.localeCbo.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.localeCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.localeCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.localeCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.localeCbo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.localeCbo.FormattingEnabled = true;
            this.localeCbo.Location = new System.Drawing.Point(65, 261);
            this.localeCbo.Name = "localeCbo";
            this.localeCbo.Size = new System.Drawing.Size(143, 21);
            this.localeCbo.Sorted = true;
            this.localeCbo.TabIndex = 71;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(11, 264);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(55, 13);
            this.label10.TabIndex = 32;
            this.label10.Text = "Language";
            this.toolTip.SetToolTip(this.label10, "Use this to change the language of the text on the screen");
            // 
            // maxConcurrentJobsCbo
            // 
            this.maxConcurrentJobsCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.maxConcurrentJobsCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.maxConcurrentJobsCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.maxConcurrentJobsCbo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.maxConcurrentJobsCbo.FormattingEnabled = true;
            this.maxConcurrentJobsCbo.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8"});
            this.maxConcurrentJobsCbo.Location = new System.Drawing.Point(147, 232);
            this.maxConcurrentJobsCbo.Name = "maxConcurrentJobsCbo";
            this.maxConcurrentJobsCbo.Size = new System.Drawing.Size(61, 21);
            this.maxConcurrentJobsCbo.TabIndex = 62;
            // 
            // archiveOriginalChk
            // 
            this.archiveOriginalChk.AutoSize = true;
            this.archiveOriginalChk.Location = new System.Drawing.Point(224, 45);
            this.archiveOriginalChk.Name = "archiveOriginalChk";
            this.archiveOriginalChk.Size = new System.Drawing.Size(114, 17);
            this.archiveOriginalChk.TabIndex = 22;
            this.archiveOriginalChk.Text = "Archive original file";
            this.toolTip.SetToolTip(this.archiveOriginalChk, resources.GetString("archiveOriginalChk.ToolTip"));
            this.archiveOriginalChk.UseVisualStyleBackColor = true;
            this.archiveOriginalChk.CheckedChanged += new System.EventHandler(this.archiveOriginalCheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 235);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(119, 13);
            this.label6.TabIndex = 61;
            this.label6.Text = "Concurrent conversions";
            this.toolTip.SetToolTip(this.label6, "Number of simultaneous conversions MCEBuddy can start\r\nNOTE: You need large amoun" +
        "ts of free disk space and processor time for more than 2 simultaneous conversion" +
        "s");
            // 
            // logJobsChk
            // 
            this.logJobsChk.AutoSize = true;
            this.logJobsChk.Location = new System.Drawing.Point(224, 234);
            this.logJobsChk.Name = "logJobsChk";
            this.logJobsChk.Size = new System.Drawing.Size(121, 17);
            this.logJobsChk.TabIndex = 63;
            this.logJobsChk.Text = "Log conversion jobs";
            this.toolTip.SetToolTip(this.logJobsChk, "Save a log file for each conversion in the \'logs\' directory where where MCEBuddy " +
        "is installed\r\n(e.g. C:\\Program Files\\MCEBuddy2x\\logs)");
            this.logJobsChk.UseVisualStyleBackColor = true;
            // 
            // sendEmailChk
            // 
            this.sendEmailChk.AutoSize = true;
            this.sendEmailChk.Location = new System.Drawing.Point(14, 205);
            this.sendEmailChk.Name = "sendEmailChk";
            this.sendEmailChk.Size = new System.Drawing.Size(79, 17);
            this.sendEmailChk.TabIndex = 51;
            this.sendEmailChk.Text = "Send eMail";
            this.toolTip.SetToolTip(this.sendEmailChk, "Send an eMail after each conversion.");
            this.sendEmailChk.UseVisualStyleBackColor = true;
            this.sendEmailChk.CheckedChanged += new System.EventHandler(this.sendEmailChk_CheckedChanged);
            // 
            // suspendOnBatteryChk
            // 
            this.suspendOnBatteryChk.AutoSize = true;
            this.suspendOnBatteryChk.Location = new System.Drawing.Point(224, 205);
            this.suspendOnBatteryChk.Name = "suspendOnBatteryChk";
            this.suspendOnBatteryChk.Size = new System.Drawing.Size(118, 17);
            this.suspendOnBatteryChk.TabIndex = 53;
            this.suspendOnBatteryChk.Text = "Suspend on battery";
            this.toolTip.SetToolTip(this.suspendOnBatteryChk, "Suspend conversions when the computer switches to battery power");
            this.suspendOnBatteryChk.UseVisualStyleBackColor = true;
            this.suspendOnBatteryChk.CheckedChanged += new System.EventHandler(this.stopChk_CheckedChanged);
            // 
            // allowSleepChk
            // 
            this.allowSleepChk.AutoSize = true;
            this.allowSleepChk.Location = new System.Drawing.Point(118, 205);
            this.allowSleepChk.Name = "allowSleepChk";
            this.allowSleepChk.Size = new System.Drawing.Size(79, 17);
            this.allowSleepChk.TabIndex = 52;
            this.allowSleepChk.Text = "Allow sleep";
            this.toolTip.SetToolTip(this.allowSleepChk, "If unchecked, it prevents the system from entering sleep while converting");
            this.allowSleepChk.UseVisualStyleBackColor = true;
            this.allowSleepChk.CheckedChanged += new System.EventHandler(this.stopChk_CheckedChanged);
            // 
            // schedulingGrp
            // 
            this.schedulingGrp.Controls.Add(this.wakeHourCbo);
            this.schedulingGrp.Controls.Add(this.startCheck);
            this.schedulingGrp.Controls.Add(this.wakeCheck);
            this.schedulingGrp.Controls.Add(this.monChk);
            this.schedulingGrp.Controls.Add(this.sunChk);
            this.schedulingGrp.Controls.Add(this.tueChk);
            this.schedulingGrp.Controls.Add(this.wedChk);
            this.schedulingGrp.Controls.Add(this.thuChk);
            this.schedulingGrp.Controls.Add(this.friChk);
            this.schedulingGrp.Controls.Add(this.satChk);
            this.schedulingGrp.Controls.Add(this.stopHourCbo);
            this.schedulingGrp.Controls.Add(this.stopAMPMCbo);
            this.schedulingGrp.Controls.Add(this.wakeAMPMCbo);
            this.schedulingGrp.Controls.Add(this.wakeMinuteCbo);
            this.schedulingGrp.Controls.Add(this.stopMinuteCbo);
            this.schedulingGrp.Controls.Add(this.stopChk);
            this.schedulingGrp.Controls.Add(this.label2);
            this.schedulingGrp.Controls.Add(this.label8);
            this.schedulingGrp.Controls.Add(this.label7);
            this.schedulingGrp.Controls.Add(this.label3);
            this.schedulingGrp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.schedulingGrp.Location = new System.Drawing.Point(6, 85);
            this.schedulingGrp.Name = "schedulingGrp";
            this.schedulingGrp.Size = new System.Drawing.Size(359, 105);
            this.schedulingGrp.TabIndex = 30;
            this.schedulingGrp.TabStop = false;
            this.schedulingGrp.Text = "Scheduling";
            // 
            // wakeHourCbo
            // 
            this.wakeHourCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.wakeHourCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.wakeHourCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.wakeHourCbo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.wakeHourCbo.FormattingEnabled = true;
            this.wakeHourCbo.Items.AddRange(new object[] {
            "01",
            "02",
            "03",
            "04",
            "05",
            "06",
            "07",
            "08",
            "09",
            "10",
            "11",
            "12"});
            this.wakeHourCbo.Location = new System.Drawing.Point(124, 19);
            this.wakeHourCbo.Name = "wakeHourCbo";
            this.wakeHourCbo.Size = new System.Drawing.Size(40, 21);
            this.wakeHourCbo.TabIndex = 33;
            // 
            // startCheck
            // 
            this.startCheck.AutoSize = true;
            this.startCheck.Location = new System.Drawing.Point(8, 21);
            this.startCheck.Name = "startCheck";
            this.startCheck.Size = new System.Drawing.Size(60, 17);
            this.startCheck.TabIndex = 31;
            this.startCheck.Text = "Start at";
            this.toolTip.SetToolTip(this.startCheck, resources.GetString("startCheck.ToolTip"));
            this.startCheck.UseVisualStyleBackColor = true;
            this.startCheck.CheckedChanged += new System.EventHandler(this.startCheck_CheckedChanged);
            // 
            // wakeCheck
            // 
            this.wakeCheck.AutoSize = true;
            this.wakeCheck.Location = new System.Drawing.Point(70, 21);
            this.wakeCheck.Name = "wakeCheck";
            this.wakeCheck.Size = new System.Drawing.Size(55, 17);
            this.wakeCheck.TabIndex = 32;
            this.wakeCheck.Text = "Wake";
            this.toolTip.SetToolTip(this.wakeCheck, "If checked MCEBuddy will wake the system from sleep\r\nat the specified Start time " +
        "and enabled days of the week.");
            this.wakeCheck.UseVisualStyleBackColor = true;
            this.wakeCheck.CheckedChanged += new System.EventHandler(this.startCheck_CheckedChanged);
            // 
            // monChk
            // 
            this.monChk.AutoSize = true;
            this.monChk.Enabled = false;
            this.monChk.Location = new System.Drawing.Point(59, 83);
            this.monChk.Name = "monChk";
            this.monChk.Size = new System.Drawing.Size(47, 17);
            this.monChk.TabIndex = 45;
            this.monChk.Text = "Mon";
            this.monChk.UseVisualStyleBackColor = true;
            this.monChk.CheckedChanged += new System.EventHandler(this.wakeUpCheck_CheckedChanged);
            // 
            // sunChk
            // 
            this.sunChk.AutoSize = true;
            this.sunChk.Enabled = false;
            this.sunChk.Location = new System.Drawing.Point(8, 83);
            this.sunChk.Name = "sunChk";
            this.sunChk.Size = new System.Drawing.Size(45, 17);
            this.sunChk.TabIndex = 44;
            this.sunChk.Text = "Sun";
            this.sunChk.UseVisualStyleBackColor = true;
            this.sunChk.CheckedChanged += new System.EventHandler(this.wakeUpCheck_CheckedChanged);
            // 
            // tueChk
            // 
            this.tueChk.AutoSize = true;
            this.tueChk.Enabled = false;
            this.tueChk.Location = new System.Drawing.Point(112, 83);
            this.tueChk.Name = "tueChk";
            this.tueChk.Size = new System.Drawing.Size(45, 17);
            this.tueChk.TabIndex = 46;
            this.tueChk.Text = "Tue";
            this.tueChk.UseVisualStyleBackColor = true;
            this.tueChk.CheckedChanged += new System.EventHandler(this.wakeUpCheck_CheckedChanged);
            // 
            // wedChk
            // 
            this.wedChk.AutoSize = true;
            this.wedChk.Enabled = false;
            this.wedChk.Location = new System.Drawing.Point(163, 83);
            this.wedChk.Name = "wedChk";
            this.wedChk.Size = new System.Drawing.Size(49, 17);
            this.wedChk.TabIndex = 47;
            this.wedChk.Text = "Wed";
            this.wedChk.UseVisualStyleBackColor = true;
            this.wedChk.CheckedChanged += new System.EventHandler(this.wakeUpCheck_CheckedChanged);
            // 
            // thuChk
            // 
            this.thuChk.AutoSize = true;
            this.thuChk.Enabled = false;
            this.thuChk.Location = new System.Drawing.Point(218, 83);
            this.thuChk.Name = "thuChk";
            this.thuChk.Size = new System.Drawing.Size(45, 17);
            this.thuChk.TabIndex = 48;
            this.thuChk.Text = "Thu";
            this.thuChk.UseVisualStyleBackColor = true;
            this.thuChk.CheckedChanged += new System.EventHandler(this.wakeUpCheck_CheckedChanged);
            // 
            // friChk
            // 
            this.friChk.AutoSize = true;
            this.friChk.Enabled = false;
            this.friChk.Location = new System.Drawing.Point(269, 83);
            this.friChk.Name = "friChk";
            this.friChk.Size = new System.Drawing.Size(37, 17);
            this.friChk.TabIndex = 49;
            this.friChk.Text = "Fri";
            this.friChk.UseVisualStyleBackColor = true;
            this.friChk.CheckedChanged += new System.EventHandler(this.wakeUpCheck_CheckedChanged);
            // 
            // satChk
            // 
            this.satChk.AutoSize = true;
            this.satChk.Enabled = false;
            this.satChk.Location = new System.Drawing.Point(312, 83);
            this.satChk.Name = "satChk";
            this.satChk.Size = new System.Drawing.Size(42, 17);
            this.satChk.TabIndex = 50;
            this.satChk.Text = "Sat";
            this.satChk.UseVisualStyleBackColor = true;
            this.satChk.CheckedChanged += new System.EventHandler(this.wakeUpCheck_CheckedChanged);
            // 
            // stopHourCbo
            // 
            this.stopHourCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.stopHourCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.stopHourCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.stopHourCbo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.stopHourCbo.FormattingEnabled = true;
            this.stopHourCbo.Items.AddRange(new object[] {
            "01",
            "02",
            "03",
            "04",
            "05",
            "06",
            "07",
            "08",
            "09",
            "10",
            "11",
            "12"});
            this.stopHourCbo.Location = new System.Drawing.Point(124, 50);
            this.stopHourCbo.Name = "stopHourCbo";
            this.stopHourCbo.Size = new System.Drawing.Size(40, 21);
            this.stopHourCbo.TabIndex = 41;
            // 
            // stopAMPMCbo
            // 
            this.stopAMPMCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.stopAMPMCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.stopAMPMCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.stopAMPMCbo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.stopAMPMCbo.FormattingEnabled = true;
            this.stopAMPMCbo.Items.AddRange(new object[] {
            "AM",
            "PM"});
            this.stopAMPMCbo.Location = new System.Drawing.Point(289, 50);
            this.stopAMPMCbo.Name = "stopAMPMCbo";
            this.stopAMPMCbo.Size = new System.Drawing.Size(40, 21);
            this.stopAMPMCbo.TabIndex = 43;
            // 
            // wakeAMPMCbo
            // 
            this.wakeAMPMCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.wakeAMPMCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.wakeAMPMCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.wakeAMPMCbo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.wakeAMPMCbo.FormattingEnabled = true;
            this.wakeAMPMCbo.Items.AddRange(new object[] {
            "AM",
            "PM"});
            this.wakeAMPMCbo.Location = new System.Drawing.Point(289, 19);
            this.wakeAMPMCbo.Name = "wakeAMPMCbo";
            this.wakeAMPMCbo.Size = new System.Drawing.Size(40, 21);
            this.wakeAMPMCbo.TabIndex = 35;
            // 
            // wakeMinuteCbo
            // 
            this.wakeMinuteCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.wakeMinuteCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.wakeMinuteCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.wakeMinuteCbo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.wakeMinuteCbo.FormattingEnabled = true;
            this.wakeMinuteCbo.Items.AddRange(new object[] {
            "00",
            "01",
            "02",
            "03",
            "04",
            "05",
            "06",
            "07",
            "08",
            "09",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59"});
            this.wakeMinuteCbo.Location = new System.Drawing.Point(198, 19);
            this.wakeMinuteCbo.Name = "wakeMinuteCbo";
            this.wakeMinuteCbo.Size = new System.Drawing.Size(40, 21);
            this.wakeMinuteCbo.TabIndex = 34;
            // 
            // stopMinuteCbo
            // 
            this.stopMinuteCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.stopMinuteCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.stopMinuteCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.stopMinuteCbo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.stopMinuteCbo.FormattingEnabled = true;
            this.stopMinuteCbo.Items.AddRange(new object[] {
            "00",
            "01",
            "02",
            "03",
            "04",
            "05",
            "06",
            "07",
            "08",
            "09",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59"});
            this.stopMinuteCbo.Location = new System.Drawing.Point(198, 50);
            this.stopMinuteCbo.Name = "stopMinuteCbo";
            this.stopMinuteCbo.Size = new System.Drawing.Size(40, 21);
            this.stopMinuteCbo.TabIndex = 42;
            // 
            // stopChk
            // 
            this.stopChk.AutoSize = true;
            this.stopChk.Location = new System.Drawing.Point(8, 52);
            this.stopChk.Name = "stopChk";
            this.stopChk.Size = new System.Drawing.Size(60, 17);
            this.stopChk.TabIndex = 40;
            this.stopChk.Text = "Stop at";
            this.toolTip.SetToolTip(this.stopChk, "No new conversions would be started after this time");
            this.stopChk.UseVisualStyleBackColor = true;
            this.stopChk.CheckedChanged += new System.EventHandler(this.stopChk_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(167, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(28, 13);
            this.label2.TabIndex = 24;
            this.label2.Text = "hour";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(245, 53);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(38, 13);
            this.label8.TabIndex = 23;
            this.label8.Text = "minute";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(167, 51);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(28, 13);
            this.label7.TabIndex = 25;
            this.label7.Text = "hour";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(245, 24);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 22;
            this.label3.Text = "minute";
            // 
            // expertSettingsBtn
            // 
            this.expertSettingsBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.expertSettingsBtn.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.expertSettingsBtn.Location = new System.Drawing.Point(162, 608);
            this.expertSettingsBtn.Name = "expertSettingsBtn";
            this.expertSettingsBtn.Size = new System.Drawing.Size(95, 23);
            this.expertSettingsBtn.TabIndex = 301;
            this.expertSettingsBtn.Text = "System Settings";
            this.toolTip.SetToolTip(this.expertSettingsBtn, "Click here to change additional settings (for experts only)");
            this.expertSettingsBtn.UseVisualStyleBackColor = true;
            this.expertSettingsBtn.Click += new System.EventHandler(this.expertSettingsBtn_Click);
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 7000;
            this.toolTip.InitialDelay = 40;
            this.toolTip.ReshowDelay = 100;
            // 
            // showAdvControls
            // 
            this.showAdvControls.BackgroundImage = global::MCEBuddy.GUI.Properties.Resources._3d_up_and_down_arrow1;
            this.showAdvControls.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.showAdvControls.Location = new System.Drawing.Point(366, 290);
            this.showAdvControls.Name = "showAdvControls";
            this.showAdvControls.Size = new System.Drawing.Size(20, 20);
            this.showAdvControls.TabIndex = 10;
            this.toolTip.SetToolTip(this.showAdvControls, "Click this button to open or close the Advanced Settings section");
            this.showAdvControls.UseVisualStyleBackColor = true;
            this.showAdvControls.Click += new System.EventHandler(this.showAdvControls_Click);
            // 
            // deleteTaskCmd
            // 
            this.deleteTaskCmd.Image = global::MCEBuddy.GUI.Properties.Resources.delete;
            this.deleteTaskCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.deleteTaskCmd.Location = new System.Drawing.Point(171, 243);
            this.deleteTaskCmd.Name = "deleteTaskCmd";
            this.deleteTaskCmd.Size = new System.Drawing.Size(85, 23);
            this.deleteTaskCmd.TabIndex = 7;
            this.deleteTaskCmd.Text = "Delete";
            this.deleteTaskCmd.UseVisualStyleBackColor = true;
            this.deleteTaskCmd.Click += new System.EventHandler(this.deleteConversionTaskCmd_Click);
            // 
            // addTaskCmd
            // 
            this.addTaskCmd.Image = global::MCEBuddy.GUI.Properties.Resources.add;
            this.addTaskCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.addTaskCmd.Location = new System.Drawing.Point(26, 243);
            this.addTaskCmd.Name = "addTaskCmd";
            this.addTaskCmd.Size = new System.Drawing.Size(85, 23);
            this.addTaskCmd.TabIndex = 6;
            this.addTaskCmd.Text = "Add";
            this.addTaskCmd.UseVisualStyleBackColor = true;
            this.addTaskCmd.Click += new System.EventHandler(this.addConversionTaskCmd_Click);
            // 
            // changeTaskCmd
            // 
            this.changeTaskCmd.Image = global::MCEBuddy.GUI.Properties.Resources.control_panel;
            this.changeTaskCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.changeTaskCmd.Location = new System.Drawing.Point(312, 243);
            this.changeTaskCmd.Name = "changeTaskCmd";
            this.changeTaskCmd.Size = new System.Drawing.Size(85, 23);
            this.changeTaskCmd.TabIndex = 8;
            this.changeTaskCmd.Text = "Change";
            this.changeTaskCmd.UseVisualStyleBackColor = true;
            this.changeTaskCmd.Click += new System.EventHandler(this.changeConversionTaskCmd_Click);
            // 
            // changeSourcePathCmd
            // 
            this.changeSourcePathCmd.Image = global::MCEBuddy.GUI.Properties.Resources.control_panel;
            this.changeSourcePathCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.changeSourcePathCmd.Location = new System.Drawing.Point(312, 88);
            this.changeSourcePathCmd.Name = "changeSourcePathCmd";
            this.changeSourcePathCmd.Size = new System.Drawing.Size(85, 23);
            this.changeSourcePathCmd.TabIndex = 4;
            this.changeSourcePathCmd.Text = "Change";
            this.changeSourcePathCmd.UseVisualStyleBackColor = true;
            this.changeSourcePathCmd.Click += new System.EventHandler(this.changeMonitorTaskCmd_Click);
            // 
            // deleteSourcePathCmd
            // 
            this.deleteSourcePathCmd.Image = global::MCEBuddy.GUI.Properties.Resources.delete;
            this.deleteSourcePathCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.deleteSourcePathCmd.Location = new System.Drawing.Point(171, 88);
            this.deleteSourcePathCmd.Name = "deleteSourcePathCmd";
            this.deleteSourcePathCmd.Size = new System.Drawing.Size(85, 23);
            this.deleteSourcePathCmd.TabIndex = 3;
            this.deleteSourcePathCmd.Text = "Delete";
            this.deleteSourcePathCmd.UseVisualStyleBackColor = true;
            this.deleteSourcePathCmd.Click += new System.EventHandler(this.deleteMonitorTaskCmd_Click);
            // 
            // addSourcePathCmd
            // 
            this.addSourcePathCmd.Image = global::MCEBuddy.GUI.Properties.Resources.add;
            this.addSourcePathCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.addSourcePathCmd.Location = new System.Drawing.Point(26, 88);
            this.addSourcePathCmd.Name = "addSourcePathCmd";
            this.addSourcePathCmd.Size = new System.Drawing.Size(85, 23);
            this.addSourcePathCmd.TabIndex = 2;
            this.addSourcePathCmd.Text = "Add";
            this.addSourcePathCmd.UseVisualStyleBackColor = true;
            this.addSourcePathCmd.Click += new System.EventHandler(this.addMonitorTaskCmd_Click);
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.oKcmd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(426, 646);
            this.Controls.Add(this.expertSettingsBtn);
            this.Controls.Add(this.showAdvControls);
            this.Controls.Add(this.advancedSettings);
            this.Controls.Add(this.taskListLv);
            this.Controls.Add(this.sourcePathsLv);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.deleteTaskCmd);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.addTaskCmd);
            this.Controls.Add(this.changeTaskCmd);
            this.Controls.Add(this.oKcmd);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.addSourcePathCmd);
            this.Controls.Add(this.deleteSourcePathCmd);
            this.Controls.Add(this.changeSourcePathCmd);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SettingsForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 25, 20);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MCEBuddy Settings";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.taskContextMenuStrip.ResumeLayout(false);
            this.advancedSettings.ResumeLayout(false);
            this.advancedSettings.PerformLayout();
            this.schedulingGrp.ResumeLayout(false);
            this.schedulingGrp.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button addSourcePathCmd;
        private System.Windows.Forms.Button deleteSourcePathCmd;
        private System.Windows.Forms.Button addTaskCmd;
        private System.Windows.Forms.Button deleteTaskCmd;
        private System.Windows.Forms.Button changeSourcePathCmd;
        private System.Windows.Forms.Button changeTaskCmd;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button oKcmd;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.ListView sourcePathsLv;
        private System.Windows.Forms.ListView taskListLv;
        private System.Windows.Forms.CheckBox deleteOriginalChk;
        private System.Windows.Forms.ComboBox minAgeCbo;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.GroupBox advancedSettings;
        private System.Windows.Forms.CheckBox stopChk;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox stopMinuteCbo;
        private System.Windows.Forms.ComboBox wakeMinuteCbo;
        private System.Windows.Forms.ComboBox stopHourCbo;
        private System.Windows.Forms.ComboBox wakeHourCbo;
        private System.Windows.Forms.CheckBox startCheck;
        private System.Windows.Forms.ComboBox maxConcurrentJobsCbo;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox logJobsChk;
        private System.Windows.Forms.ComboBox localeCbo;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.CheckBox archiveOriginalChk;
        private System.Windows.Forms.CheckBox wakeCheck;
        private System.Windows.Forms.CheckBox allowSleepChk;
        private System.Windows.Forms.Button showAdvControls;
        private System.Windows.Forms.CheckBox sendEmailChk;
        private System.Windows.Forms.ContextMenuStrip taskContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem disableToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enableToolStripMenuItem;
        private System.Windows.Forms.CheckBox satChk;
        private System.Windows.Forms.CheckBox friChk;
        private System.Windows.Forms.CheckBox thuChk;
        private System.Windows.Forms.CheckBox wedChk;
        private System.Windows.Forms.CheckBox tueChk;
        private System.Windows.Forms.CheckBox sunChk;
        private System.Windows.Forms.CheckBox monChk;
        private System.Windows.Forms.GroupBox schedulingGrp;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.CheckBox deleteConvertedChk;
        private System.Windows.Forms.Button expertSettingsBtn;
        private System.Windows.Forms.CheckBox suspendOnBatteryChk;
        private System.Windows.Forms.ColumnHeader monitorName;
        private System.Windows.Forms.ColumnHeader monitorPath;
        private System.Windows.Forms.ColumnHeader taskName;
        private System.Windows.Forms.ColumnHeader taskPath;
        private System.Windows.Forms.ComboBox stopAMPMCbo;
        private System.Windows.Forms.ComboBox wakeAMPMCbo;
        private System.Windows.Forms.ToolStripMenuItem renameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem moveUpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem upToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem downToolStripMenuItem;
    }
}