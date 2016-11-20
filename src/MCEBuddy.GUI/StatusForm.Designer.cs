namespace MCEBuddy.GUI
{
    partial class StatusForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StatusForm));
            this.MajorTitle = new System.Windows.Forms.Label();
            this.backgroundUpdate = new System.ComponentModel.BackgroundWorker();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.errorMsgLbl = new System.Windows.Forms.Label();
            this.BetaTitle = new System.Windows.Forms.Label();
            this.announcementLabel = new System.Windows.Forms.Label();
            this.fileContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mediaInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.priorityBox = new System.Windows.Forms.ComboBox();
            this.PriorityLabel = new System.Windows.Forms.Label();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.gettingStarted = new System.Windows.Forms.Label();
            this.logLabel = new System.Windows.Forms.Label();
            this.eventLogLbl = new System.Windows.Forms.Label();
            this.showHistoryLbl = new System.Windows.Forms.Label();
            this.closeCmd = new System.Windows.Forms.Button();
            this.facebookBtn = new System.Windows.Forms.PictureBox();
            this.remoteEngineCmd = new System.Windows.Forms.Button();
            this.paypalButton = new System.Windows.Forms.PictureBox();
            this.mcebuddyBox = new System.Windows.Forms.PictureBox();
            this.rescanCmd = new System.Windows.Forms.Button();
            this.stopCmd = new System.Windows.Forms.Button();
            this.startCmd = new System.Windows.Forms.Button();
            this.cancelFileCmd = new System.Windows.Forms.Button();
            this.addFileCmd = new System.Windows.Forms.Button();
            this.settingsCmd = new System.Windows.Forms.Button();
            this.engineBox = new System.Windows.Forms.GroupBox();
            this.fileBox = new System.Windows.Forms.GroupBox();
            this.serverInfoLbl = new System.Windows.Forms.Label();
            this.plainBackgroundPainter = new MCEBuddy.ProgressODoom.PlainBackgroundPainter();
            this.plainBorderPainter = new MCEBuddy.ProgressODoom.PlainBorderPainter();
            this.plainProgressPainter = new MCEBuddy.ProgressODoom.PlainProgressPainter();
            this.gradientGlossPainter = new MCEBuddy.ProgressODoom.GradientGlossPainter();
            this.currentQueue = new MCEBuddy.GUI.ListViewEx();
            this.fileName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.taskName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.fileContextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.facebookBtn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.paypalButton)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mcebuddyBox)).BeginInit();
            this.engineBox.SuspendLayout();
            this.fileBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // MajorTitle
            // 
            this.MajorTitle.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.MajorTitle.AutoSize = true;
            this.MajorTitle.Font = new System.Drawing.Font("Tahoma", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MajorTitle.ForeColor = System.Drawing.Color.OrangeRed;
            this.MajorTitle.Location = new System.Drawing.Point(109, 32);
            this.MajorTitle.Name = "MajorTitle";
            this.MajorTitle.Size = new System.Drawing.Size(209, 35);
            this.MajorTitle.TabIndex = 18;
            this.MajorTitle.Text = "MCEBuddy XXX";
            // 
            // backgroundUpdate
            // 
            this.backgroundUpdate.WorkerReportsProgress = true;
            this.backgroundUpdate.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundUpdate_DoWork);
            this.backgroundUpdate.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundUpdate_ProgressChanged);
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = resources.GetString("openFileDialog.Filter");
            this.openFileDialog.Multiselect = true;
            // 
            // errorMsgLbl
            // 
            this.errorMsgLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.errorMsgLbl.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.errorMsgLbl.ForeColor = System.Drawing.Color.OrangeRed;
            this.errorMsgLbl.Location = new System.Drawing.Point(25, 281);
            this.errorMsgLbl.Name = "errorMsgLbl";
            this.errorMsgLbl.Size = new System.Drawing.Size(368, 40);
            this.errorMsgLbl.TabIndex = 20;
            this.errorMsgLbl.Text = "Message";
            this.errorMsgLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.errorMsgLbl.Visible = false;
            // 
            // BetaTitle
            // 
            this.BetaTitle.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.BetaTitle.AutoSize = true;
            this.BetaTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BetaTitle.ForeColor = System.Drawing.Color.OrangeRed;
            this.BetaTitle.Location = new System.Drawing.Point(180, 67);
            this.BetaTitle.Name = "BetaTitle";
            this.BetaTitle.Size = new System.Drawing.Size(67, 13);
            this.BetaTitle.TabIndex = 21;
            this.BetaTitle.Text = "BETA XXX";
            // 
            // announcementLabel
            // 
            this.announcementLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.announcementLabel.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.announcementLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.announcementLabel.Location = new System.Drawing.Point(96, 7);
            this.announcementLabel.Margin = new System.Windows.Forms.Padding(0);
            this.announcementLabel.Name = "announcementLabel";
            this.announcementLabel.Size = new System.Drawing.Size(244, 25);
            this.announcementLabel.TabIndex = 10;
            this.announcementLabel.Text = "There is a new version!";
            this.announcementLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.announcementLabel, "Click here to download new version of MCEBuddy");
            this.announcementLabel.Visible = false;
            this.announcementLabel.Click += new System.EventHandler(this.announcementLabel_Click);
            // 
            // fileContextMenuStrip
            // 
            this.fileContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mediaInfoToolStripMenuItem});
            this.fileContextMenuStrip.Name = "fileContextMenuStrip";
            this.fileContextMenuStrip.Size = new System.Drawing.Size(132, 26);
            // 
            // mediaInfoToolStripMenuItem
            // 
            this.mediaInfoToolStripMenuItem.AutoToolTip = true;
            this.mediaInfoToolStripMenuItem.Name = "mediaInfoToolStripMenuItem";
            this.mediaInfoToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.mediaInfoToolStripMenuItem.Text = "Media info";
            this.mediaInfoToolStripMenuItem.ToolTipText = "Click here to get media properties for selected files";
            this.mediaInfoToolStripMenuItem.Click += new System.EventHandler(this.mediaInfoToolStripMenuItem_Click);
            // 
            // priorityBox
            // 
            this.priorityBox.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.priorityBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.priorityBox.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.priorityBox.ForeColor = System.Drawing.SystemColors.GrayText;
            this.priorityBox.Items.AddRange(new object[] {
            "High",
            "Normal",
            "Low"});
            this.priorityBox.Location = new System.Drawing.Point(329, 368);
            this.priorityBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
            this.priorityBox.MaxDropDownItems = 3;
            this.priorityBox.Name = "priorityBox";
            this.priorityBox.Size = new System.Drawing.Size(62, 21);
            this.priorityBox.TabIndex = 9;
            this.priorityBox.SelectedIndexChanged += new System.EventHandler(this.PriorityBox_SelectedIndexChanged);
            this.priorityBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.priorityBox_KeyPress);
            // 
            // PriorityLabel
            // 
            this.PriorityLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.PriorityLabel.AutoSize = true;
            this.PriorityLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.PriorityLabel.Location = new System.Drawing.Point(285, 371);
            this.PriorityLabel.Name = "PriorityLabel";
            this.PriorityLabel.Size = new System.Drawing.Size(38, 13);
            this.PriorityLabel.TabIndex = 27;
            this.PriorityLabel.Text = "Priority";
            this.toolTip.SetToolTip(this.PriorityLabel, "High priority - convert faster but may make other running applications unresponsi" +
        "ve\r\nLow priority - convert in the background but reduced interference with other" +
        " running applications");
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 200;
            this.toolTip.AutoPopDelay = 10000;
            this.toolTip.InitialDelay = 40;
            this.toolTip.ReshowDelay = 40;
            // 
            // gettingStarted
            // 
            this.gettingStarted.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gettingStarted.AutoSize = true;
            this.gettingStarted.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gettingStarted.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.gettingStarted.Location = new System.Drawing.Point(314, 67);
            this.gettingStarted.Name = "gettingStarted";
            this.gettingStarted.Size = new System.Drawing.Size(91, 13);
            this.gettingStarted.TabIndex = 13;
            this.gettingStarted.Text = "Getting started";
            this.gettingStarted.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.gettingStarted, "Click here for MCEBuddy guide");
            this.gettingStarted.Click += new System.EventHandler(this.gettingStarted_Click);
            // 
            // logLabel
            // 
            this.logLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.logLabel.AutoSize = true;
            this.logLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.logLabel.Location = new System.Drawing.Point(375, 46);
            this.logLabel.Name = "logLabel";
            this.logLabel.Size = new System.Drawing.Size(30, 13);
            this.logLabel.TabIndex = 12;
            this.logLabel.Text = "Logs";
            this.logLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.toolTip.SetToolTip(this.logLabel, "Click to open log folder");
            this.logLabel.Click += new System.EventHandler(this.logLabel_Click);
            // 
            // eventLogLbl
            // 
            this.eventLogLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.eventLogLbl.AutoSize = true;
            this.eventLogLbl.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.eventLogLbl.Location = new System.Drawing.Point(314, 46);
            this.eventLogLbl.Name = "eventLogLbl";
            this.eventLogLbl.Size = new System.Drawing.Size(40, 13);
            this.eventLogLbl.TabIndex = 11;
            this.eventLogLbl.Text = "Events";
            this.eventLogLbl.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.toolTip.SetToolTip(this.eventLogLbl, "Click here to open the MCEBuddy Windows Event Logs");
            this.eventLogLbl.Click += new System.EventHandler(this.eventLogLbl_Click);
            // 
            // showHistoryLbl
            // 
            this.showHistoryLbl.AutoSize = true;
            this.showHistoryLbl.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.showHistoryLbl.Location = new System.Drawing.Point(26, 67);
            this.showHistoryLbl.Name = "showHistoryLbl";
            this.showHistoryLbl.Size = new System.Drawing.Size(67, 13);
            this.showHistoryLbl.TabIndex = 14;
            this.showHistoryLbl.Text = "Show history";
            this.showHistoryLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.showHistoryLbl, "Click this button to show the history of all converted files.");
            this.showHistoryLbl.Click += new System.EventHandler(this.showHistoryLbl_Click);
            // 
            // closeCmd
            // 
            this.closeCmd.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.closeCmd.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.closeCmd.Image = global::MCEBuddy.GUI.Properties.Resources.mcebuddy_close;
            this.closeCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.closeCmd.Location = new System.Drawing.Point(301, 327);
            this.closeCmd.Name = "closeCmd";
            this.closeCmd.Size = new System.Drawing.Size(92, 23);
            this.closeCmd.TabIndex = 7;
            this.closeCmd.Text = "Close";
            this.toolTip.SetToolTip(this.closeCmd, "Close this window, MCEBuddy will keep running in the background");
            this.closeCmd.UseVisualStyleBackColor = true;
            this.closeCmd.Click += new System.EventHandler(this.closeCmd_Click);
            // 
            // facebookBtn
            // 
            this.facebookBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.facebookBtn.Image = ((System.Drawing.Image)(resources.GetObject("facebookBtn.Image")));
            this.facebookBtn.Location = new System.Drawing.Point(317, 8);
            this.facebookBtn.Name = "facebookBtn";
            this.facebookBtn.Size = new System.Drawing.Size(23, 30);
            this.facebookBtn.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.facebookBtn.TabIndex = 33;
            this.facebookBtn.TabStop = false;
            this.toolTip.SetToolTip(this.facebookBtn, "Click here to like MCEBuddy on facebook");
            this.facebookBtn.Click += new System.EventHandler(this.facebookBtn_Click);
            // 
            // remoteEngineCmd
            // 
            this.remoteEngineCmd.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.remoteEngineCmd.BackgroundImage = global::MCEBuddy.GUI.Properties.Resources.MCEBuddyUpdate_20x20;
            this.remoteEngineCmd.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.remoteEngineCmd.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.remoteEngineCmd.Location = new System.Drawing.Point(254, 365);
            this.remoteEngineCmd.Name = "remoteEngineCmd";
            this.remoteEngineCmd.Size = new System.Drawing.Size(25, 25);
            this.remoteEngineCmd.TabIndex = 8;
            this.toolTip.SetToolTip(this.remoteEngineCmd, "Click here to connect to another MCEBuddy engine");
            this.remoteEngineCmd.UseVisualStyleBackColor = true;
            this.remoteEngineCmd.Click += new System.EventHandler(this.remoteEngineCmd_Click);
            // 
            // paypalButton
            // 
            this.paypalButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.paypalButton.Image = global::MCEBuddy.GUI.Properties.Resources.paypal_donate;
            this.paypalButton.Location = new System.Drawing.Point(343, 7);
            this.paypalButton.Name = "paypalButton";
            this.paypalButton.Size = new System.Drawing.Size(62, 31);
            this.paypalButton.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.paypalButton.TabIndex = 22;
            this.paypalButton.TabStop = false;
            this.toolTip.SetToolTip(this.paypalButton, "If you like MCEBuddy, please click here to Donate - THANKS");
            this.paypalButton.Click += new System.EventHandler(this.paypalButton_Click);
            // 
            // mcebuddyBox
            // 
            this.mcebuddyBox.Image = global::MCEBuddy.GUI.Properties.Resources.MCEBuddy50High;
            this.mcebuddyBox.Location = new System.Drawing.Point(25, 7);
            this.mcebuddyBox.Name = "mcebuddyBox";
            this.mcebuddyBox.Size = new System.Drawing.Size(71, 55);
            this.mcebuddyBox.TabIndex = 19;
            this.mcebuddyBox.TabStop = false;
            this.toolTip.SetToolTip(this.mcebuddyBox, "Click here to open MCEBuddy website");
            this.mcebuddyBox.Click += new System.EventHandler(this.mceBuddyBox_Click);
            // 
            // rescanCmd
            // 
            this.rescanCmd.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rescanCmd.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.rescanCmd.Image = global::MCEBuddy.GUI.Properties.Resources.nav_refresh_blue;
            this.rescanCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.rescanCmd.Location = new System.Drawing.Point(165, 327);
            this.rescanCmd.Name = "rescanCmd";
            this.rescanCmd.Size = new System.Drawing.Size(92, 23);
            this.rescanCmd.TabIndex = 6;
            this.rescanCmd.Text = "Rescan";
            this.toolTip.SetToolTip(this.rescanCmd, "Forces the MCEBuddy to recheck the Monitor locations for new video files to conve" +
        "rt");
            this.rescanCmd.UseVisualStyleBackColor = true;
            this.rescanCmd.Click += new System.EventHandler(this.reScanCmd_Click);
            // 
            // stopCmd
            // 
            this.stopCmd.Enabled = false;
            this.stopCmd.ForeColor = System.Drawing.SystemColors.ControlText;
            this.stopCmd.Image = global::MCEBuddy.GUI.Properties.Resources.stop;
            this.stopCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.stopCmd.Location = new System.Drawing.Point(96, 15);
            this.stopCmd.Name = "stopCmd";
            this.stopCmd.Size = new System.Drawing.Size(75, 23);
            this.stopCmd.TabIndex = 2;
            this.stopCmd.Text = "Stop";
            this.toolTip.SetToolTip(this.stopCmd, "Stop the MCEBuddy monitor and conversion tasks");
            this.stopCmd.UseVisualStyleBackColor = true;
            this.stopCmd.Click += new System.EventHandler(this.stopCmd_Click);
            // 
            // startCmd
            // 
            this.startCmd.Enabled = false;
            this.startCmd.ForeColor = System.Drawing.SystemColors.ControlText;
            this.startCmd.Image = global::MCEBuddy.GUI.Properties.Resources.media_play_green;
            this.startCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.startCmd.Location = new System.Drawing.Point(8, 15);
            this.startCmd.Name = "startCmd";
            this.startCmd.Size = new System.Drawing.Size(75, 23);
            this.startCmd.TabIndex = 1;
            this.startCmd.Text = "Start";
            this.toolTip.SetToolTip(this.startCmd, "Start the MCEBuddy monitor and conversion tasks");
            this.startCmd.UseVisualStyleBackColor = true;
            this.startCmd.Click += new System.EventHandler(this.startCmd_Click);
            // 
            // cancelFileCmd
            // 
            this.cancelFileCmd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelFileCmd.Enabled = false;
            this.cancelFileCmd.ForeColor = System.Drawing.SystemColors.ControlText;
            this.cancelFileCmd.Image = global::MCEBuddy.GUI.Properties.Resources.delete;
            this.cancelFileCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.cancelFileCmd.Location = new System.Drawing.Point(8, 15);
            this.cancelFileCmd.Name = "cancelFileCmd";
            this.cancelFileCmd.Size = new System.Drawing.Size(75, 23);
            this.cancelFileCmd.TabIndex = 3;
            this.cancelFileCmd.Text = "Delete";
            this.toolTip.SetToolTip(this.cancelFileCmd, "Select the file(s) in the queue to delete");
            this.cancelFileCmd.UseVisualStyleBackColor = true;
            this.cancelFileCmd.Click += new System.EventHandler(this.cancelFileCmd_Click);
            // 
            // addFileCmd
            // 
            this.addFileCmd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.addFileCmd.Enabled = false;
            this.addFileCmd.ForeColor = System.Drawing.SystemColors.ControlText;
            this.addFileCmd.Image = global::MCEBuddy.GUI.Properties.Resources.add;
            this.addFileCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.addFileCmd.Location = new System.Drawing.Point(96, 15);
            this.addFileCmd.Name = "addFileCmd";
            this.addFileCmd.Size = new System.Drawing.Size(75, 23);
            this.addFileCmd.TabIndex = 4;
            this.addFileCmd.Text = "Add";
            this.toolTip.SetToolTip(this.addFileCmd, "Manually add video file(s) to the queue to convert");
            this.addFileCmd.UseVisualStyleBackColor = true;
            this.addFileCmd.Click += new System.EventHandler(this.addFileCmd_Click);
            // 
            // settingsCmd
            // 
            this.settingsCmd.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.settingsCmd.Image = global::MCEBuddy.GUI.Properties.Resources.control_panel;
            this.settingsCmd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.settingsCmd.Location = new System.Drawing.Point(29, 327);
            this.settingsCmd.Name = "settingsCmd";
            this.settingsCmd.Size = new System.Drawing.Size(92, 23);
            this.settingsCmd.TabIndex = 5;
            this.settingsCmd.Text = "Settings";
            this.toolTip.SetToolTip(this.settingsCmd, "Stops the monitor and conversion tasks and opens a page to configure MCEBuddy");
            this.settingsCmd.UseVisualStyleBackColor = true;
            this.settingsCmd.Click += new System.EventHandler(this.settingsCmd_Click);
            // 
            // engineBox
            // 
            this.engineBox.Controls.Add(this.stopCmd);
            this.engineBox.Controls.Add(this.startCmd);
            this.engineBox.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.engineBox.Location = new System.Drawing.Point(25, 103);
            this.engineBox.Name = "engineBox";
            this.engineBox.Size = new System.Drawing.Size(179, 44);
            this.engineBox.TabIndex = 30;
            this.engineBox.TabStop = false;
            this.engineBox.Text = "Conversion";
            // 
            // fileBox
            // 
            this.fileBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.fileBox.Controls.Add(this.cancelFileCmd);
            this.fileBox.Controls.Add(this.addFileCmd);
            this.fileBox.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.fileBox.Location = new System.Drawing.Point(214, 103);
            this.fileBox.Name = "fileBox";
            this.fileBox.Size = new System.Drawing.Size(179, 44);
            this.fileBox.TabIndex = 30;
            this.fileBox.TabStop = false;
            this.fileBox.Text = "Files";
            // 
            // serverInfoLbl
            // 
            this.serverInfoLbl.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.serverInfoLbl.AutoSize = true;
            this.serverInfoLbl.Font = new System.Drawing.Font("Footlight MT Light", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.serverInfoLbl.ForeColor = System.Drawing.SystemColors.GrayText;
            this.serverInfoLbl.Location = new System.Drawing.Point(31, 372);
            this.serverInfoLbl.Name = "serverInfoLbl";
            this.serverInfoLbl.Size = new System.Drawing.Size(62, 12);
            this.serverInfoLbl.TabIndex = 31;
            this.serverInfoLbl.Text = "Engine: Port:";
            // 
            // plainBackgroundPainter
            // 
            this.plainBackgroundPainter.Color = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.plainBackgroundPainter.GlossPainter = null;
            // 
            // plainBorderPainter
            // 
            this.plainBorderPainter.Color = System.Drawing.Color.Gainsboro;
            this.plainBorderPainter.RoundedCorners = true;
            this.plainBorderPainter.Style = MCEBuddy.ProgressODoom.PlainBorderPainter.PlainBorderStyle.Flat;
            // 
            // plainProgressPainter
            // 
            this.plainProgressPainter.Color = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(210)))), ((int)(((byte)(80)))));
            this.plainProgressPainter.GlossPainter = this.gradientGlossPainter;
            this.plainProgressPainter.LeadingEdge = System.Drawing.Color.LimeGreen;
            this.plainProgressPainter.ProgressBorderPainter = null;
            // 
            // gradientGlossPainter
            // 
            this.gradientGlossPainter.AlphaHigh = 192;
            this.gradientGlossPainter.AlphaLow = 0;
            this.gradientGlossPainter.Angle = 90F;
            this.gradientGlossPainter.Color = System.Drawing.Color.White;
            this.gradientGlossPainter.PercentageCovered = 55;
            this.gradientGlossPainter.Style = MCEBuddy.ProgressODoom.GlossStyle.Top;
            this.gradientGlossPainter.Successor = null;
            // 
            // currentQueue
            // 
            this.currentQueue.AllowDrop = true;
            this.currentQueue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.currentQueue.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.fileName,
            this.taskName});
            this.currentQueue.ContextMenuStrip = this.fileContextMenuStrip;
            this.currentQueue.FullRowSelect = true;
            this.currentQueue.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.currentQueue.LineAfter = -1;
            this.currentQueue.LineBefore = -1;
            this.currentQueue.Location = new System.Drawing.Point(25, 155);
            this.currentQueue.MinimumSize = new System.Drawing.Size(368, 120);
            this.currentQueue.Name = "currentQueue";
            this.currentQueue.ShowItemToolTips = true;
            this.currentQueue.Size = new System.Drawing.Size(368, 120);
            this.currentQueue.TabIndex = 9;
            this.currentQueue.UseCompatibleStateImageBehavior = false;
            this.currentQueue.View = System.Windows.Forms.View.Details;
            this.currentQueue.DragDrop += new System.Windows.Forms.DragEventHandler(this.currentQueue_DragDrop);
            this.currentQueue.DragEnter += new System.Windows.Forms.DragEventHandler(this.currentQueue_DragEnter);
            this.currentQueue.KeyDown += new System.Windows.Forms.KeyEventHandler(this.currentQueue_KeyDown);
            this.currentQueue.MouseDown += new System.Windows.Forms.MouseEventHandler(this.currentQueue_MouseDown);
            this.currentQueue.MouseMove += new System.Windows.Forms.MouseEventHandler(this.currentQueue_MouseMove);
            this.currentQueue.MouseUp += new System.Windows.Forms.MouseEventHandler(this.currentQueue_MouseUp);
            // 
            // fileName
            // 
            this.fileName.Name = "fileName";
            this.fileName.Text = "File";
            this.fileName.Width = 240;
            // 
            // taskName
            // 
            this.taskName.Name = "taskName";
            this.taskName.Text = "Task";
            this.taskName.Width = 100;
            // 
            // StatusForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.CancelButton = this.closeCmd;
            this.ClientSize = new System.Drawing.Size(431, 412);
            this.Controls.Add(this.showHistoryLbl);
            this.Controls.Add(this.facebookBtn);
            this.Controls.Add(this.remoteEngineCmd);
            this.Controls.Add(this.eventLogLbl);
            this.Controls.Add(this.logLabel);
            this.Controls.Add(this.PriorityLabel);
            this.Controls.Add(this.priorityBox);
            this.Controls.Add(this.announcementLabel);
            this.Controls.Add(this.currentQueue);
            this.Controls.Add(this.serverInfoLbl);
            this.Controls.Add(this.paypalButton);
            this.Controls.Add(this.BetaTitle);
            this.Controls.Add(this.gettingStarted);
            this.Controls.Add(this.MajorTitle);
            this.Controls.Add(this.mcebuddyBox);
            this.Controls.Add(this.rescanCmd);
            this.Controls.Add(this.settingsCmd);
            this.Controls.Add(this.closeCmd);
            this.Controls.Add(this.errorMsgLbl);
            this.Controls.Add(this.engineBox);
            this.Controls.Add(this.fileBox);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(439, 439);
            this.Name = "StatusForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MCEBuddy Status";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.StatusForm_Closed);
            this.Load += new System.EventHandler(this.StatusForm_Load);
            this.Shown += new System.EventHandler(this.StatusForm_Shown);
            this.ResizeEnd += new System.EventHandler(this.StatusForm_ResizeEnd);
            this.Resize += new System.EventHandler(this.StatusForm_Resize);
            this.fileContextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.facebookBtn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.paypalButton)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.mcebuddyBox)).EndInit();
            this.engineBox.ResumeLayout(false);
            this.fileBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button startCmd;
        private System.Windows.Forms.Button stopCmd;
        private System.Windows.Forms.Button addFileCmd;
        private System.Windows.Forms.Button cancelFileCmd;
        private System.Windows.Forms.Button closeCmd;
        private System.Windows.Forms.Button settingsCmd;
        private System.Windows.Forms.PictureBox mcebuddyBox;
        private System.Windows.Forms.Label MajorTitle;
        private ProgressODoom.GradientGlossPainter gradientGlossPainter;
        private ProgressODoom.PlainProgressPainter plainProgressPainter;
        private ProgressODoom.PlainBackgroundPainter plainBackgroundPainter;
        private ProgressODoom.PlainBorderPainter plainBorderPainter;
        private System.ComponentModel.BackgroundWorker backgroundUpdate;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Label errorMsgLbl;
        private System.Windows.Forms.Label BetaTitle;
        private System.Windows.Forms.Button rescanCmd;
        private System.Windows.Forms.PictureBox paypalButton;
        private System.Windows.Forms.Label announcementLabel;
        private ListViewEx currentQueue;
        private System.Windows.Forms.ColumnHeader fileName;
        private System.Windows.Forms.ColumnHeader taskName;
        private System.Windows.Forms.ComboBox priorityBox;
        private System.Windows.Forms.Label PriorityLabel;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label gettingStarted;
        private System.Windows.Forms.Label logLabel;
        private System.Windows.Forms.GroupBox engineBox;
        private System.Windows.Forms.GroupBox fileBox;
        private System.Windows.Forms.Label eventLogLbl;
        private System.Windows.Forms.Button remoteEngineCmd;
        private System.Windows.Forms.Label serverInfoLbl;
        private System.Windows.Forms.ContextMenuStrip fileContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem mediaInfoToolStripMenuItem;
        private System.Windows.Forms.Label showHistoryLbl;
        private System.Windows.Forms.PictureBox facebookBtn;
    }
}

