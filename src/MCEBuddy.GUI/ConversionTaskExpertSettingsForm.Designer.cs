namespace MCEBuddy.GUI
{
    partial class ConversionTaskExpertSettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConversionTaskExpertSettingsForm));
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.comskipIniCmd = new System.Windows.Forms.Button();
            this.comskipIniLbl = new System.Windows.Forms.Label();
            this.ccAdjustLbl = new System.Windows.Forms.Label();
            this.makKeyLbl = new System.Windows.Forms.Label();
            this.skipHistoryChk = new System.Windows.Forms.CheckBox();
            this.skipReprocessChk = new System.Windows.Forms.CheckBox();
            this.monitorTaskNameMatchChk = new System.Windows.Forms.CheckBox();
            this.seriesButton = new System.Windows.Forms.Button();
            this.downloadSeriesChk = new System.Windows.Forms.CheckBox();
            this.xmlChk = new System.Windows.Forms.CheckBox();
            this.disableCroppingChk = new System.Windows.Forms.CheckBox();
            this.endTrimChk = new System.Windows.Forms.CheckBox();
            this.audioOffsetChk = new System.Windows.Forms.CheckBox();
            this.startTrimChk = new System.Windows.Forms.CheckBox();
            this.extractCCAdvOpts = new System.Windows.Forms.CheckBox();
            this.skipCopyChk = new System.Windows.Forms.CheckBox();
            this.forceShowTypeLbl = new System.Windows.Forms.Label();
            this.ignoreCopyProtectionChk = new System.Windows.Forms.CheckBox();
            this.insertTopChk = new System.Windows.Forms.CheckBox();
            this.embedSrtChaptersChk = new System.Windows.Forms.CheckBox();
            this.drcChk = new System.Windows.Forms.CheckBox();
            this.writeMetadataChk = new System.Windows.Forms.CheckBox();
            this.frameRateChk = new System.Windows.Forms.CheckBox();
            this.skipRemuxChk = new System.Windows.Forms.CheckBox();
            this.hardwareEncodingChk = new System.Windows.Forms.CheckBox();
            this.autoIncFilenameChk = new System.Windows.Forms.CheckBox();
            this.drmFilterLbl = new System.Windows.Forms.Label();
            this.tempFldrCmd = new System.Windows.Forms.Button();
            this.tempFldrLbl = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.oKcmd = new System.Windows.Forms.Button();
            this.comskipINIPath = new System.Windows.Forms.TextBox();
            this.ccChannel = new System.Windows.Forms.ComboBox();
            this.ccField = new System.Windows.Forms.ComboBox();
            this.ccOffset = new System.Windows.Forms.TextBox();
            this.ccChannelLbl = new System.Windows.Forms.Label();
            this.ccFieldLbl = new System.Windows.Forms.Label();
            this.makKey = new System.Windows.Forms.TextBox();
            this.audioOffset = new System.Windows.Forms.TextBox();
            this.endTrim = new System.Windows.Forms.TextBox();
            this.startTrim = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.forceShowTypeCbo = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.frameRate = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.drmFilterCbo = new System.Windows.Forms.ComboBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.tempFldrPath = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 200;
            this.toolTip.AutoPopDelay = 9000;
            this.toolTip.InitialDelay = 40;
            this.toolTip.ReshowDelay = 40;
            // 
            // comskipIniCmd
            // 
            this.comskipIniCmd.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.comskipIniCmd.Location = new System.Drawing.Point(358, 22);
            this.comskipIniCmd.Name = "comskipIniCmd";
            this.comskipIniCmd.Size = new System.Drawing.Size(29, 20);
            this.comskipIniCmd.TabIndex = 61;
            this.comskipIniCmd.Text = "...";
            this.toolTip.SetToolTip(this.comskipIniCmd, "Click to select custom Comskip INI file");
            this.comskipIniCmd.UseVisualStyleBackColor = true;
            this.comskipIniCmd.Click += new System.EventHandler(this.comskipIniCmd_Click);
            // 
            // comskipIniLbl
            // 
            this.comskipIniLbl.AutoSize = true;
            this.comskipIniLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.comskipIniLbl.Location = new System.Drawing.Point(12, 25);
            this.comskipIniLbl.Name = "comskipIniLbl";
            this.comskipIniLbl.Size = new System.Drawing.Size(64, 13);
            this.comskipIniLbl.TabIndex = 204;
            this.comskipIniLbl.Text = "Comskip INI";
            this.toolTip.SetToolTip(this.comskipIniLbl, "Select the custom Comskip INI file to use for this conversion task\r\nLeave this bl" +
        "ank to use the default Comskip INI file for advertisement removal");
            // 
            // ccAdjustLbl
            // 
            this.ccAdjustLbl.AutoSize = true;
            this.ccAdjustLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.ccAdjustLbl.Location = new System.Drawing.Point(12, 25);
            this.ccAdjustLbl.Name = "ccAdjustLbl";
            this.ccAdjustLbl.Size = new System.Drawing.Size(71, 13);
            this.ccAdjustLbl.TabIndex = 209;
            this.ccAdjustLbl.Text = "Subtitle offset";
            this.toolTip.SetToolTip(this.ccAdjustLbl, "Enter the number of seconds (+ve / -ve) to adjust the subtitles sync with video");
            // 
            // makKeyLbl
            // 
            this.makKeyLbl.AutoSize = true;
            this.makKeyLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.makKeyLbl.Location = new System.Drawing.Point(12, 82);
            this.makKeyLbl.Name = "makKeyLbl";
            this.makKeyLbl.Size = new System.Drawing.Size(57, 13);
            this.makKeyLbl.TabIndex = 214;
            this.makKeyLbl.Text = "TiVO MAK";
            this.toolTip.SetToolTip(this.makKeyLbl, "Enter your 10 digit TiVO MAK key to convert and process TiVO recordings");
            // 
            // skipHistoryChk
            // 
            this.skipHistoryChk.AutoSize = true;
            this.skipHistoryChk.Enabled = false;
            this.skipHistoryChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.skipHistoryChk.Location = new System.Drawing.Point(282, 29);
            this.skipHistoryChk.Name = "skipHistoryChk";
            this.skipHistoryChk.Size = new System.Drawing.Size(90, 17);
            this.skipHistoryChk.TabIndex = 22;
            this.skipHistoryChk.Text = "Check history";
            this.toolTip.SetToolTip(this.skipHistoryChk, resources.GetString("skipHistoryChk.ToolTip"));
            this.skipHistoryChk.UseVisualStyleBackColor = true;
            // 
            // skipReprocessChk
            // 
            this.skipReprocessChk.AutoSize = true;
            this.skipReprocessChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.skipReprocessChk.Location = new System.Drawing.Point(156, 29);
            this.skipReprocessChk.Name = "skipReprocessChk";
            this.skipReprocessChk.Size = new System.Drawing.Size(111, 17);
            this.skipReprocessChk.TabIndex = 21;
            this.skipReprocessChk.Text = "Skip reconversion";
            this.toolTip.SetToolTip(this.skipReprocessChk, resources.GetString("skipReprocessChk.ToolTip"));
            this.skipReprocessChk.UseVisualStyleBackColor = true;
            this.skipReprocessChk.CheckedChanged += new System.EventHandler(this.skipReprocessChk_CheckedChanged);
            // 
            // monitorTaskNameMatchChk
            // 
            this.monitorTaskNameMatchChk.AutoSize = true;
            this.monitorTaskNameMatchChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.monitorTaskNameMatchChk.Location = new System.Drawing.Point(15, 29);
            this.monitorTaskNameMatchChk.Name = "monitorTaskNameMatchChk";
            this.monitorTaskNameMatchChk.Size = new System.Drawing.Size(138, 17);
            this.monitorTaskNameMatchChk.TabIndex = 20;
            this.monitorTaskNameMatchChk.Text = "Select monitor locations";
            this.toolTip.SetToolTip(this.monitorTaskNameMatchChk, resources.GetString("monitorTaskNameMatchChk.ToolTip"));
            this.monitorTaskNameMatchChk.UseVisualStyleBackColor = true;
            this.monitorTaskNameMatchChk.CheckedChanged += new System.EventHandler(this.monitorTaskNameMatchChk_CheckedChanged);
            // 
            // seriesButton
            // 
            this.seriesButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.seriesButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.seriesButton.Location = new System.Drawing.Point(149, 24);
            this.seriesButton.Name = "seriesButton";
            this.seriesButton.Size = new System.Drawing.Size(71, 22);
            this.seriesButton.TabIndex = 11;
            this.seriesButton.Text = "Correction";
            this.toolTip.SetToolTip(this.seriesButton, "Click here to correct the Show title and force use of a specific TVDB series id o" +
        "r IMDB movie id\r\nto lookup additional metadata from the internet.");
            this.seriesButton.UseVisualStyleBackColor = true;
            this.seriesButton.Click += new System.EventHandler(this.seriesButton_Click);
            // 
            // downloadSeriesChk
            // 
            this.downloadSeriesChk.AutoSize = true;
            this.downloadSeriesChk.Checked = true;
            this.downloadSeriesChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.downloadSeriesChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.downloadSeriesChk.Location = new System.Drawing.Point(15, 28);
            this.downloadSeriesChk.Name = "downloadSeriesChk";
            this.downloadSeriesChk.Size = new System.Drawing.Size(128, 17);
            this.downloadSeriesChk.TabIndex = 10;
            this.downloadSeriesChk.Text = "Download information";
            this.toolTip.SetToolTip(this.downloadSeriesChk, "Check this to download additional information from the internet about the show li" +
        "ke a banner file, episode, season etc");
            this.downloadSeriesChk.UseVisualStyleBackColor = true;
            this.downloadSeriesChk.CheckedChanged += new System.EventHandler(this.downloadSeriesChk_CheckedChanged);
            // 
            // xmlChk
            // 
            this.xmlChk.AutoSize = true;
            this.xmlChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.xmlChk.Location = new System.Drawing.Point(15, 58);
            this.xmlChk.Name = "xmlChk";
            this.xmlChk.Size = new System.Drawing.Size(105, 17);
            this.xmlChk.TabIndex = 15;
            this.xmlChk.Text = "Save information";
            this.toolTip.SetToolTip(this.xmlChk, "Save the poster and show information (metadata) in a MC/XBMC compatible NFO file " +
        "(XML format)");
            this.xmlChk.UseVisualStyleBackColor = true;
            // 
            // disableCroppingChk
            // 
            this.disableCroppingChk.AutoSize = true;
            this.disableCroppingChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.disableCroppingChk.Location = new System.Drawing.Point(156, 57);
            this.disableCroppingChk.Name = "disableCroppingChk";
            this.disableCroppingChk.Size = new System.Drawing.Size(105, 17);
            this.disableCroppingChk.TabIndex = 42;
            this.disableCroppingChk.Text = "Disable cropping";
            this.toolTip.SetToolTip(this.disableCroppingChk, resources.GetString("disableCroppingChk.ToolTip"));
            this.disableCroppingChk.UseVisualStyleBackColor = true;
            // 
            // endTrimChk
            // 
            this.endTrimChk.AutoSize = true;
            this.endTrimChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.endTrimChk.Location = new System.Drawing.Point(282, 28);
            this.endTrimChk.Name = "endTrimChk";
            this.endTrimChk.Size = new System.Drawing.Size(63, 17);
            this.endTrimChk.TabIndex = 34;
            this.endTrimChk.Text = "Cut end";
            this.toolTip.SetToolTip(this.endTrimChk, "This specifies the number of seconds to cut from the end of the recording (BEFORE" +
        " removing the ads)");
            this.endTrimChk.UseVisualStyleBackColor = true;
            this.endTrimChk.CheckedChanged += new System.EventHandler(this.endTrimChk_CheckedChanged);
            // 
            // audioOffsetChk
            // 
            this.audioOffsetChk.AutoSize = true;
            this.audioOffsetChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.audioOffsetChk.Location = new System.Drawing.Point(15, 28);
            this.audioOffsetChk.Name = "audioOffsetChk";
            this.audioOffsetChk.Size = new System.Drawing.Size(81, 17);
            this.audioOffsetChk.TabIndex = 30;
            this.audioOffsetChk.Text = "Audio delay";
            this.toolTip.SetToolTip(this.audioOffsetChk, resources.GetString("audioOffsetChk.ToolTip"));
            this.audioOffsetChk.UseVisualStyleBackColor = true;
            this.audioOffsetChk.CheckedChanged += new System.EventHandler(this.audioOffsetChk_CheckedChanged);
            // 
            // startTrimChk
            // 
            this.startTrimChk.AutoSize = true;
            this.startTrimChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.startTrimChk.Location = new System.Drawing.Point(156, 28);
            this.startTrimChk.Name = "startTrimChk";
            this.startTrimChk.Size = new System.Drawing.Size(65, 17);
            this.startTrimChk.TabIndex = 32;
            this.startTrimChk.Text = "Cut start";
            this.toolTip.SetToolTip(this.startTrimChk, "This specifies the number of seconds to cut from the start of the recording (BEFO" +
        "RE removing the ads)");
            this.startTrimChk.UseVisualStyleBackColor = true;
            this.startTrimChk.CheckedChanged += new System.EventHandler(this.startTrimChk_CheckedChanged);
            // 
            // extractCCAdvOpts
            // 
            this.extractCCAdvOpts.AutoSize = true;
            this.extractCCAdvOpts.Enabled = false;
            this.extractCCAdvOpts.Location = new System.Drawing.Point(225, 25);
            this.extractCCAdvOpts.Name = "extractCCAdvOpts";
            this.extractCCAdvOpts.Size = new System.Drawing.Size(15, 14);
            this.extractCCAdvOpts.TabIndex = 51;
            this.toolTip.SetToolTip(this.extractCCAdvOpts, "Enable this to set advanced options for Closed Captions extraction\r\nField - 1 or " +
        "2\r\nChannel - 1 or 2\r\n\r\nDefault: Field 1, Channel 1");
            this.extractCCAdvOpts.UseVisualStyleBackColor = true;
            this.extractCCAdvOpts.CheckedChanged += new System.EventHandler(this.extractCCAdvOpts_CheckedChanged);
            // 
            // skipCopyChk
            // 
            this.skipCopyChk.AutoSize = true;
            this.skipCopyChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.skipCopyChk.Location = new System.Drawing.Point(229, 107);
            this.skipCopyChk.Name = "skipCopyChk";
            this.skipCopyChk.Size = new System.Drawing.Size(144, 17);
            this.skipCopyChk.TabIndex = 81;
            this.skipCopyChk.Text = "Skip copying original files";
            this.toolTip.SetToolTip(this.skipCopyChk, resources.GetString("skipCopyChk.ToolTip"));
            this.skipCopyChk.UseVisualStyleBackColor = true;
            this.skipCopyChk.CheckedChanged += new System.EventHandler(this.skipCopyChk_CheckedChanged);
            // 
            // forceShowTypeLbl
            // 
            this.forceShowTypeLbl.AutoSize = true;
            this.forceShowTypeLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.forceShowTypeLbl.Location = new System.Drawing.Point(226, 29);
            this.forceShowTypeLbl.Name = "forceShowTypeLbl";
            this.forceShowTypeLbl.Size = new System.Drawing.Size(85, 13);
            this.forceShowTypeLbl.TabIndex = 14;
            this.forceShowTypeLbl.Text = "Force show type";
            this.toolTip.SetToolTip(this.forceShowTypeLbl, resources.GetString("forceShowTypeLbl.ToolTip"));
            // 
            // ignoreCopyProtectionChk
            // 
            this.ignoreCopyProtectionChk.AutoSize = true;
            this.ignoreCopyProtectionChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.ignoreCopyProtectionChk.Location = new System.Drawing.Point(15, 107);
            this.ignoreCopyProtectionChk.Name = "ignoreCopyProtectionChk";
            this.ignoreCopyProtectionChk.Size = new System.Drawing.Size(132, 17);
            this.ignoreCopyProtectionChk.TabIndex = 80;
            this.ignoreCopyProtectionChk.Text = "Ignore copy protection";
            this.toolTip.SetToolTip(this.ignoreCopyProtectionChk, resources.GetString("ignoreCopyProtectionChk.ToolTip"));
            this.ignoreCopyProtectionChk.UseVisualStyleBackColor = true;
            // 
            // insertTopChk
            // 
            this.insertTopChk.AutoSize = true;
            this.insertTopChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.insertTopChk.Location = new System.Drawing.Point(229, 81);
            this.insertTopChk.Name = "insertTopChk";
            this.insertTopChk.Size = new System.Drawing.Size(127, 17);
            this.insertTopChk.TabIndex = 71;
            this.insertTopChk.Text = "Insert at top of queue";
            this.toolTip.SetToolTip(this.insertTopChk, resources.GetString("insertTopChk.ToolTip"));
            this.insertTopChk.UseVisualStyleBackColor = true;
            // 
            // embedSrtChaptersChk
            // 
            this.embedSrtChaptersChk.AutoSize = true;
            this.embedSrtChaptersChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.embedSrtChaptersChk.Location = new System.Drawing.Point(15, 51);
            this.embedSrtChaptersChk.Name = "embedSrtChaptersChk";
            this.embedSrtChaptersChk.Size = new System.Drawing.Size(151, 17);
            this.embedSrtChaptersChk.TabIndex = 55;
            this.embedSrtChaptersChk.Text = "Add subtitles and chapters";
            this.toolTip.SetToolTip(this.embedSrtChaptersChk, resources.GetString("embedSrtChaptersChk.ToolTip"));
            this.embedSrtChaptersChk.UseVisualStyleBackColor = true;
            // 
            // drcChk
            // 
            this.drcChk.AutoSize = true;
            this.drcChk.Checked = true;
            this.drcChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.drcChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.drcChk.Location = new System.Drawing.Point(282, 57);
            this.drcChk.Name = "drcChk";
            this.drcChk.Size = new System.Drawing.Size(49, 17);
            this.drcChk.TabIndex = 43;
            this.drcChk.Text = "DRC";
            this.toolTip.SetToolTip(this.drcChk, resources.GetString("drcChk.ToolTip"));
            this.drcChk.UseVisualStyleBackColor = true;
            // 
            // writeMetadataChk
            // 
            this.writeMetadataChk.AutoSize = true;
            this.writeMetadataChk.Checked = true;
            this.writeMetadataChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.writeMetadataChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.writeMetadataChk.Location = new System.Drawing.Point(156, 58);
            this.writeMetadataChk.Name = "writeMetadataChk";
            this.writeMetadataChk.Size = new System.Drawing.Size(99, 17);
            this.writeMetadataChk.TabIndex = 16;
            this.writeMetadataChk.Text = "Add information";
            this.toolTip.SetToolTip(this.writeMetadataChk, resources.GetString("writeMetadataChk.ToolTip"));
            this.writeMetadataChk.UseVisualStyleBackColor = true;
            // 
            // frameRateChk
            // 
            this.frameRateChk.AutoSize = true;
            this.frameRateChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.frameRateChk.Location = new System.Drawing.Point(15, 57);
            this.frameRateChk.Name = "frameRateChk";
            this.frameRateChk.Size = new System.Drawing.Size(76, 17);
            this.frameRateChk.TabIndex = 40;
            this.frameRateChk.Text = "Frame rate";
            this.toolTip.SetToolTip(this.frameRateChk, resources.GetString("frameRateChk.ToolTip"));
            this.frameRateChk.UseVisualStyleBackColor = true;
            this.frameRateChk.CheckedChanged += new System.EventHandler(this.frameRateChk_CheckedChanged);
            // 
            // skipRemuxChk
            // 
            this.skipRemuxChk.AutoSize = true;
            this.skipRemuxChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.skipRemuxChk.Location = new System.Drawing.Point(229, 133);
            this.skipRemuxChk.Name = "skipRemuxChk";
            this.skipRemuxChk.Size = new System.Drawing.Size(113, 17);
            this.skipRemuxChk.TabIndex = 91;
            this.skipRemuxChk.Text = "Skip remuxing files";
            this.toolTip.SetToolTip(this.skipRemuxChk, resources.GetString("skipRemuxChk.ToolTip"));
            this.skipRemuxChk.UseVisualStyleBackColor = true;
            this.skipRemuxChk.CheckedChanged += new System.EventHandler(this.skipRemuxChk_CheckedChanged);
            // 
            // hardwareEncodingChk
            // 
            this.hardwareEncodingChk.AutoSize = true;
            this.hardwareEncodingChk.Checked = true;
            this.hardwareEncodingChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.hardwareEncodingChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.hardwareEncodingChk.Location = new System.Drawing.Point(15, 86);
            this.hardwareEncodingChk.Name = "hardwareEncodingChk";
            this.hardwareEncodingChk.Size = new System.Drawing.Size(119, 17);
            this.hardwareEncodingChk.TabIndex = 44;
            this.hardwareEncodingChk.Text = "Hardware encoding";
            this.toolTip.SetToolTip(this.hardwareEncodingChk, resources.GetString("hardwareEncodingChk.ToolTip"));
            this.hardwareEncodingChk.UseVisualStyleBackColor = true;
            // 
            // autoIncFilenameChk
            // 
            this.autoIncFilenameChk.AutoSize = true;
            this.autoIncFilenameChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.autoIncFilenameChk.Location = new System.Drawing.Point(15, 133);
            this.autoIncFilenameChk.Name = "autoIncFilenameChk";
            this.autoIncFilenameChk.Size = new System.Drawing.Size(125, 17);
            this.autoIncFilenameChk.TabIndex = 90;
            this.autoIncFilenameChk.Text = "Do not overwrite files";
            this.toolTip.SetToolTip(this.autoIncFilenameChk, "When this box is checked, the destination file will not be overwritten if it alre" +
        "ady exists.\r\nInstead a new file will be created with a unique number added to th" +
        "e end of the filename.");
            this.autoIncFilenameChk.UseVisualStyleBackColor = true;
            this.autoIncFilenameChk.CheckedChanged += new System.EventHandler(this.autoIncFilenameChk_CheckedChanged);
            // 
            // drmFilterLbl
            // 
            this.drmFilterLbl.AutoSize = true;
            this.drmFilterLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.drmFilterLbl.Location = new System.Drawing.Point(12, 60);
            this.drmFilterLbl.Name = "drmFilterLbl";
            this.drmFilterLbl.Size = new System.Drawing.Size(32, 13);
            this.drmFilterLbl.TabIndex = 14;
            this.drmFilterLbl.Text = "DRM";
            this.toolTip.SetToolTip(this.drmFilterLbl, resources.GetString("drmFilterLbl.ToolTip"));
            // 
            // tempFldrCmd
            // 
            this.tempFldrCmd.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.tempFldrCmd.Location = new System.Drawing.Point(358, 49);
            this.tempFldrCmd.Name = "tempFldrCmd";
            this.tempFldrCmd.Size = new System.Drawing.Size(29, 20);
            this.tempFldrCmd.TabIndex = 66;
            this.tempFldrCmd.Text = "...";
            this.toolTip.SetToolTip(this.tempFldrCmd, "Click to select temp working folder");
            this.tempFldrCmd.UseVisualStyleBackColor = true;
            this.tempFldrCmd.Click += new System.EventHandler(this.tempFldrCmd_Click);
            // 
            // tempFldrLbl
            // 
            this.tempFldrLbl.AutoSize = true;
            this.tempFldrLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.tempFldrLbl.Location = new System.Drawing.Point(12, 52);
            this.tempFldrLbl.Name = "tempFldrLbl";
            this.tempFldrLbl.Size = new System.Drawing.Size(63, 13);
            this.tempFldrLbl.TabIndex = 204;
            this.tempFldrLbl.Text = "Temp folder";
            this.toolTip.SetToolTip(this.tempFldrLbl, "Select the folder for working with temporary files. Make sure there is enough emp" +
        "ty space on the drive.\r\nThis overrides the temp folder specified in the Settings" +
        " -> Expert Settings page.");
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog1";
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.cmdCancel.Location = new System.Drawing.Point(352, 632);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 203;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            // 
            // oKcmd
            // 
            this.oKcmd.Location = new System.Drawing.Point(25, 632);
            this.oKcmd.Name = "oKcmd";
            this.oKcmd.Size = new System.Drawing.Size(75, 23);
            this.oKcmd.TabIndex = 202;
            this.oKcmd.Text = "OK";
            this.oKcmd.UseVisualStyleBackColor = true;
            this.oKcmd.Click += new System.EventHandler(this.oKcmd_Click);
            // 
            // comskipINIPath
            // 
            this.comskipINIPath.Location = new System.Drawing.Point(84, 22);
            this.comskipINIPath.Name = "comskipINIPath";
            this.comskipINIPath.Size = new System.Drawing.Size(268, 20);
            this.comskipINIPath.TabIndex = 60;
            // 
            // ccChannel
            // 
            this.ccChannel.BackColor = System.Drawing.Color.Gainsboro;
            this.ccChannel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ccChannel.Enabled = false;
            this.ccChannel.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.ccChannel.FormattingEnabled = true;
            this.ccChannel.Items.AddRange(new object[] {
            "1",
            "2"});
            this.ccChannel.Location = new System.Drawing.Point(361, 22);
            this.ccChannel.Name = "ccChannel";
            this.ccChannel.Size = new System.Drawing.Size(30, 21);
            this.ccChannel.TabIndex = 53;
            // 
            // ccField
            // 
            this.ccField.BackColor = System.Drawing.Color.Gainsboro;
            this.ccField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ccField.Enabled = false;
            this.ccField.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.ccField.FormattingEnabled = true;
            this.ccField.Items.AddRange(new object[] {
            "1",
            "2"});
            this.ccField.Location = new System.Drawing.Point(277, 22);
            this.ccField.Name = "ccField";
            this.ccField.Size = new System.Drawing.Size(30, 21);
            this.ccField.TabIndex = 52;
            // 
            // ccOffset
            // 
            this.ccOffset.Enabled = false;
            this.ccOffset.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.ccOffset.Location = new System.Drawing.Point(96, 22);
            this.ccOffset.MaxLength = 5;
            this.ccOffset.Name = "ccOffset";
            this.ccOffset.Size = new System.Drawing.Size(38, 20);
            this.ccOffset.TabIndex = 50;
            this.ccOffset.Text = "3.1";
            this.ccOffset.WordWrap = false;
            this.ccOffset.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.offset_TextChanged);
            // 
            // ccChannelLbl
            // 
            this.ccChannelLbl.AutoSize = true;
            this.ccChannelLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.ccChannelLbl.Location = new System.Drawing.Point(313, 25);
            this.ccChannelLbl.Name = "ccChannelLbl";
            this.ccChannelLbl.Size = new System.Drawing.Size(46, 13);
            this.ccChannelLbl.TabIndex = 207;
            this.ccChannelLbl.Text = "Channel";
            // 
            // ccFieldLbl
            // 
            this.ccFieldLbl.AutoSize = true;
            this.ccFieldLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.ccFieldLbl.Location = new System.Drawing.Point(246, 25);
            this.ccFieldLbl.Name = "ccFieldLbl";
            this.ccFieldLbl.Size = new System.Drawing.Size(29, 13);
            this.ccFieldLbl.TabIndex = 208;
            this.ccFieldLbl.Text = "Field";
            // 
            // makKey
            // 
            this.makKey.Location = new System.Drawing.Point(84, 79);
            this.makKey.MaxLength = 10;
            this.makKey.Name = "makKey";
            this.makKey.Size = new System.Drawing.Size(69, 20);
            this.makKey.TabIndex = 70;
            this.makKey.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.makKey_TextChanged);
            // 
            // audioOffset
            // 
            this.audioOffset.Enabled = false;
            this.audioOffset.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.audioOffset.Location = new System.Drawing.Point(96, 26);
            this.audioOffset.MaxLength = 9;
            this.audioOffset.Name = "audioOffset";
            this.audioOffset.Size = new System.Drawing.Size(35, 20);
            this.audioOffset.TabIndex = 31;
            this.audioOffset.WordWrap = false;
            this.audioOffset.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.offset_TextChanged);
            // 
            // endTrim
            // 
            this.endTrim.Enabled = false;
            this.endTrim.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.endTrim.Location = new System.Drawing.Point(352, 26);
            this.endTrim.MaxLength = 9;
            this.endTrim.Name = "endTrim";
            this.endTrim.Size = new System.Drawing.Size(35, 20);
            this.endTrim.TabIndex = 35;
            this.endTrim.WordWrap = false;
            this.endTrim.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.trim_TextChanged);
            // 
            // startTrim
            // 
            this.startTrim.Enabled = false;
            this.startTrim.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.startTrim.Location = new System.Drawing.Point(229, 26);
            this.startTrim.MaxLength = 9;
            this.startTrim.Name = "startTrim";
            this.startTrim.Size = new System.Drawing.Size(35, 20);
            this.startTrim.TabIndex = 33;
            this.startTrim.WordWrap = false;
            this.startTrim.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.trim_TextChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.forceShowTypeLbl);
            this.groupBox1.Controls.Add(this.forceShowTypeCbo);
            this.groupBox1.Controls.Add(this.downloadSeriesChk);
            this.groupBox1.Controls.Add(this.writeMetadataChk);
            this.groupBox1.Controls.Add(this.xmlChk);
            this.groupBox1.Controls.Add(this.seriesButton);
            this.groupBox1.Location = new System.Drawing.Point(25, 19);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(402, 87);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Media Information Management";
            // 
            // forceShowTypeCbo
            // 
            this.forceShowTypeCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.forceShowTypeCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.forceShowTypeCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.forceShowTypeCbo.FormattingEnabled = true;
            this.forceShowTypeCbo.Items.AddRange(new object[] {
            "Default",
            "Series",
            "Movie",
            "Sports"});
            this.forceShowTypeCbo.Location = new System.Drawing.Point(317, 26);
            this.forceShowTypeCbo.Name = "forceShowTypeCbo";
            this.forceShowTypeCbo.Size = new System.Drawing.Size(70, 21);
            this.forceShowTypeCbo.TabIndex = 13;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.drcChk);
            this.groupBox2.Controls.Add(this.frameRateChk);
            this.groupBox2.Controls.Add(this.audioOffsetChk);
            this.groupBox2.Controls.Add(this.hardwareEncodingChk);
            this.groupBox2.Controls.Add(this.disableCroppingChk);
            this.groupBox2.Controls.Add(this.startTrim);
            this.groupBox2.Controls.Add(this.endTrimChk);
            this.groupBox2.Controls.Add(this.endTrim);
            this.groupBox2.Controls.Add(this.frameRate);
            this.groupBox2.Controls.Add(this.audioOffset);
            this.groupBox2.Controls.Add(this.startTrimChk);
            this.groupBox2.Location = new System.Drawing.Point(25, 225);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(402, 115);
            this.groupBox2.TabIndex = 30;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Audio and Video";
            // 
            // frameRate
            // 
            this.frameRate.Enabled = false;
            this.frameRate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.frameRate.Location = new System.Drawing.Point(96, 55);
            this.frameRate.MaxLength = 15;
            this.frameRate.Name = "frameRate";
            this.frameRate.Size = new System.Drawing.Size(51, 20);
            this.frameRate.TabIndex = 41;
            this.frameRate.WordWrap = false;
            this.frameRate.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.framerate_TextChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.drmFilterLbl);
            this.groupBox3.Controls.Add(this.drmFilterCbo);
            this.groupBox3.Controls.Add(this.monitorTaskNameMatchChk);
            this.groupBox3.Controls.Add(this.skipReprocessChk);
            this.groupBox3.Controls.Add(this.skipHistoryChk);
            this.groupBox3.Location = new System.Drawing.Point(25, 121);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(402, 87);
            this.groupBox3.TabIndex = 20;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Selection Filters";
            // 
            // drmFilterCbo
            // 
            this.drmFilterCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.drmFilterCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.drmFilterCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.drmFilterCbo.FormattingEnabled = true;
            this.drmFilterCbo.Items.AddRange(new object[] {
            "All",
            "Protected",
            "Unprotected"});
            this.drmFilterCbo.Location = new System.Drawing.Point(50, 57);
            this.drmFilterCbo.Name = "drmFilterCbo";
            this.drmFilterCbo.Size = new System.Drawing.Size(93, 21);
            this.drmFilterCbo.TabIndex = 23;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.embedSrtChaptersChk);
            this.groupBox4.Controls.Add(this.ccAdjustLbl);
            this.groupBox4.Controls.Add(this.ccFieldLbl);
            this.groupBox4.Controls.Add(this.ccChannelLbl);
            this.groupBox4.Controls.Add(this.ccOffset);
            this.groupBox4.Controls.Add(this.extractCCAdvOpts);
            this.groupBox4.Controls.Add(this.ccField);
            this.groupBox4.Controls.Add(this.ccChannel);
            this.groupBox4.Location = new System.Drawing.Point(25, 358);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(402, 81);
            this.groupBox4.TabIndex = 40;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Subtitles and Chapters";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.insertTopChk);
            this.groupBox5.Controls.Add(this.ignoreCopyProtectionChk);
            this.groupBox5.Controls.Add(this.skipRemuxChk);
            this.groupBox5.Controls.Add(this.autoIncFilenameChk);
            this.groupBox5.Controls.Add(this.skipCopyChk);
            this.groupBox5.Controls.Add(this.tempFldrLbl);
            this.groupBox5.Controls.Add(this.tempFldrPath);
            this.groupBox5.Controls.Add(this.comskipIniLbl);
            this.groupBox5.Controls.Add(this.tempFldrCmd);
            this.groupBox5.Controls.Add(this.comskipINIPath);
            this.groupBox5.Controls.Add(this.comskipIniCmd);
            this.groupBox5.Controls.Add(this.makKeyLbl);
            this.groupBox5.Controls.Add(this.makKey);
            this.groupBox5.Location = new System.Drawing.Point(25, 454);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(402, 161);
            this.groupBox5.TabIndex = 50;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Miscellaneous";
            // 
            // tempFldrPath
            // 
            this.tempFldrPath.Location = new System.Drawing.Point(84, 49);
            this.tempFldrPath.Name = "tempFldrPath";
            this.tempFldrPath.Size = new System.Drawing.Size(268, 20);
            this.tempFldrPath.TabIndex = 65;
            // 
            // ConversionTaskExpertSettingsForm
            // 
            this.AcceptButton = this.oKcmd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(459, 676);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.oKcmd);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "ConversionTaskExpertSettingsForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Expert Settings";
            this.Load += new System.EventHandler(this.ConversionTaskExpertSettingsForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Button oKcmd;
        private System.Windows.Forms.Button comskipIniCmd;
        private System.Windows.Forms.TextBox comskipINIPath;
        private System.Windows.Forms.Label comskipIniLbl;
        private System.Windows.Forms.ComboBox ccChannel;
        private System.Windows.Forms.ComboBox ccField;
        private System.Windows.Forms.TextBox ccOffset;
        private System.Windows.Forms.Label ccChannelLbl;
        private System.Windows.Forms.Label ccFieldLbl;
        private System.Windows.Forms.Label ccAdjustLbl;
        private System.Windows.Forms.TextBox makKey;
        private System.Windows.Forms.Label makKeyLbl;
        private System.Windows.Forms.CheckBox skipHistoryChk;
        private System.Windows.Forms.CheckBox skipReprocessChk;
        private System.Windows.Forms.CheckBox monitorTaskNameMatchChk;
        private System.Windows.Forms.Button seriesButton;
        private System.Windows.Forms.CheckBox downloadSeriesChk;
        private System.Windows.Forms.CheckBox xmlChk;
        private System.Windows.Forms.CheckBox disableCroppingChk;
        private System.Windows.Forms.CheckBox endTrimChk;
        private System.Windows.Forms.CheckBox audioOffsetChk;
        private System.Windows.Forms.CheckBox startTrimChk;
        private System.Windows.Forms.TextBox audioOffset;
        private System.Windows.Forms.TextBox endTrim;
        private System.Windows.Forms.TextBox startTrim;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.CheckBox extractCCAdvOpts;
        private System.Windows.Forms.CheckBox skipCopyChk;
        private System.Windows.Forms.ComboBox forceShowTypeCbo;
        private System.Windows.Forms.Label forceShowTypeLbl;
        private System.Windows.Forms.CheckBox ignoreCopyProtectionChk;
        private System.Windows.Forms.CheckBox insertTopChk;
        private System.Windows.Forms.CheckBox embedSrtChaptersChk;
        private System.Windows.Forms.CheckBox drcChk;
        private System.Windows.Forms.CheckBox writeMetadataChk;
        private System.Windows.Forms.CheckBox frameRateChk;
        private System.Windows.Forms.TextBox frameRate;
        private System.Windows.Forms.CheckBox skipRemuxChk;
        private System.Windows.Forms.CheckBox hardwareEncodingChk;
        private System.Windows.Forms.CheckBox autoIncFilenameChk;
        private System.Windows.Forms.Label drmFilterLbl;
        private System.Windows.Forms.ComboBox drmFilterCbo;
        private System.Windows.Forms.Label tempFldrLbl;
        private System.Windows.Forms.TextBox tempFldrPath;
        private System.Windows.Forms.Button tempFldrCmd;

    }
}