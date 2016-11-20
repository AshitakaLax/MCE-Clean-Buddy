namespace MCEBuddy.GUI
{
    partial class ConversionTaskForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any destinationLabel being used.
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConversionTaskForm));
            this.taskNameTxt = new System.Windows.Forms.TextBox();
            this.taskNameLabel = new System.Windows.Forms.Label();
            this.profileCbo = new System.Windows.Forms.ComboBox();
            this.conversionDescriptionTxt = new System.Windows.Forms.TextBox();
            this.conversionLabel = new System.Windows.Forms.Label();
            this.SelectFolderCmd = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.destinationPathTxt = new System.Windows.Forms.TextBox();
            this.destinationLabel = new System.Windows.Forms.Label();
            this.removeAdsLabel = new System.Windows.Forms.Label();
            this.detectAdsCbo = new System.Windows.Forms.ComboBox();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.oKcmd = new System.Windows.Forms.Button();
            this.renameBySeriesChk = new System.Windows.Forms.CheckBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.altRenameBySeriesChk = new System.Windows.Forms.CheckBox();
            this.qualityLabel = new System.Windows.Forms.Label();
            this.maxWidthLabel = new System.Windows.Forms.Label();
            this.metaShowMatchLabel = new System.Windows.Forms.Label();
            this.fileMatchLabel = new System.Windows.Forms.Label();
            this.langCode = new System.Windows.Forms.TextBox();
            this.langLbl = new System.Windows.Forms.Label();
            this.volumeLbl = new System.Windows.Forms.Label();
            this.customReNamingChk = new System.Windows.Forms.CheckBox();
            this.extractCC = new System.Windows.Forms.CheckBox();
            this.multiChannelAudioChk = new System.Windows.Forms.CheckBox();
            this.customRenamePreview = new System.Windows.Forms.TextBox();
            this.renameOnlyChk = new System.Windows.Forms.CheckBox();
            this.metaNetworkMatchLbl = new System.Windows.Forms.Label();
            this.addiTunesChk = new System.Windows.Forms.CheckBox();
            this.setConnectionCredentials = new System.Windows.Forms.Button();
            this.showAdvControls = new System.Windows.Forms.Button();
            this.addToWMPChk = new System.Windows.Forms.CheckBox();
            this.expertSettingsBtn = new System.Windows.Forms.Button();
            this.metadataShowTypeMatchLbl = new System.Windows.Forms.Label();
            this.singleAudioTrackChk = new System.Windows.Forms.CheckBox();
            this.autoDeinterlaceChk = new System.Windows.Forms.CheckBox();
            this.commercialSkipCutChk = new System.Windows.Forms.CheckBox();
            this.advancedSettings = new System.Windows.Forms.GroupBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.customFileRenamePattern = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.metaShowTypeCbo = new System.Windows.Forms.ComboBox();
            this.metaNetworkMatchTxt = new System.Windows.Forms.TextBox();
            this.fileMatchTxt = new System.Windows.Forms.TextBox();
            this.metaShowMatchTxt = new System.Windows.Forms.TextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.maxWidthTxt = new System.Windows.Forms.TextBox();
            this.qualityTxt = new System.Windows.Forms.TextBox();
            this.qualityBar = new System.Windows.Forms.TrackBar();
            this.volumeTxt = new System.Windows.Forms.TextBox();
            this.volumeBar = new System.Windows.Forms.TrackBar();
            this.maxWidthBar = new System.Windows.Forms.TrackBar();
            this.langBox = new System.Windows.Forms.ComboBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.advancedSettings.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.qualityBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.volumeBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxWidthBar)).BeginInit();
            this.SuspendLayout();
            // 
            // taskNameTxt
            // 
            this.taskNameTxt.Location = new System.Drawing.Point(96, 10);
            this.taskNameTxt.Name = "taskNameTxt";
            this.taskNameTxt.Size = new System.Drawing.Size(336, 20);
            this.taskNameTxt.TabIndex = 1;
            this.taskNameTxt.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.taskNameTxt_KeyPress);
            // 
            // taskNameLabel
            // 
            this.taskNameLabel.AutoSize = true;
            this.taskNameLabel.Location = new System.Drawing.Point(26, 13);
            this.taskNameLabel.Name = "taskNameLabel";
            this.taskNameLabel.Size = new System.Drawing.Size(60, 13);
            this.taskNameLabel.TabIndex = 2;
            this.taskNameLabel.Text = "Task name";
            this.toolTip.SetToolTip(this.taskNameLabel, "Give a unique name to this task");
            // 
            // profileCbo
            // 
            this.profileCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.profileCbo.FormattingEnabled = true;
            this.profileCbo.Location = new System.Drawing.Point(96, 41);
            this.profileCbo.Name = "profileCbo";
            this.profileCbo.Size = new System.Drawing.Size(336, 21);
            this.profileCbo.TabIndex = 2;
            this.profileCbo.SelectedIndexChanged += new System.EventHandler(this.conversionCbo_SelectedIndexChanged);
            // 
            // conversionDescriptionTxt
            // 
            this.conversionDescriptionTxt.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.conversionDescriptionTxt.Location = new System.Drawing.Point(33, 68);
            this.conversionDescriptionTxt.Multiline = true;
            this.conversionDescriptionTxt.Name = "conversionDescriptionTxt";
            this.conversionDescriptionTxt.ReadOnly = true;
            this.conversionDescriptionTxt.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.conversionDescriptionTxt.Size = new System.Drawing.Size(399, 47);
            this.conversionDescriptionTxt.TabIndex = 3;
            this.conversionDescriptionTxt.TabStop = false;
            this.toolTip.SetToolTip(this.conversionDescriptionTxt, "Gives a short description for the selection conversion profile");
            // 
            // conversionLabel
            // 
            this.conversionLabel.AutoSize = true;
            this.conversionLabel.Location = new System.Drawing.Point(26, 44);
            this.conversionLabel.Name = "conversionLabel";
            this.conversionLabel.Size = new System.Drawing.Size(36, 13);
            this.conversionLabel.TabIndex = 2;
            this.conversionLabel.Text = "Profile";
            this.toolTip.SetToolTip(this.conversionLabel, resources.GetString("conversionLabel.ToolTip"));
            // 
            // SelectFolderCmd
            // 
            this.SelectFolderCmd.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.SelectFolderCmd.Location = new System.Drawing.Point(371, 118);
            this.SelectFolderCmd.Name = "SelectFolderCmd";
            this.SelectFolderCmd.Size = new System.Drawing.Size(29, 20);
            this.SelectFolderCmd.TabIndex = 5;
            this.SelectFolderCmd.Text = "...";
            this.toolTip.SetToolTip(this.SelectFolderCmd, "Click to select destination folder");
            this.SelectFolderCmd.UseVisualStyleBackColor = true;
            this.SelectFolderCmd.Click += new System.EventHandler(this.SelectFolderCmd_Click);
            // 
            // destinationPathTxt
            // 
            this.destinationPathTxt.Location = new System.Drawing.Point(96, 118);
            this.destinationPathTxt.Name = "destinationPathTxt";
            this.destinationPathTxt.Size = new System.Drawing.Size(269, 20);
            this.destinationPathTxt.TabIndex = 4;
            this.destinationPathTxt.Leave += new System.EventHandler(this.destinationPathTxt_Leave);
            // 
            // destinationLabel
            // 
            this.destinationLabel.AutoSize = true;
            this.destinationLabel.Location = new System.Drawing.Point(26, 121);
            this.destinationLabel.Name = "destinationLabel";
            this.destinationLabel.Size = new System.Drawing.Size(60, 13);
            this.destinationLabel.TabIndex = 6;
            this.destinationLabel.Text = "Destination";
            this.toolTip.SetToolTip(this.destinationLabel, "Select the directory where the converted file will be placed.\r\nLeave this blank t" +
        "o keep the converted file in the same directory as the original video.");
            // 
            // removeAdsLabel
            // 
            this.removeAdsLabel.AutoSize = true;
            this.removeAdsLabel.Location = new System.Drawing.Point(26, 153);
            this.removeAdsLabel.Name = "removeAdsLabel";
            this.removeAdsLabel.Size = new System.Drawing.Size(61, 13);
            this.removeAdsLabel.TabIndex = 10;
            this.removeAdsLabel.Text = "Ad remover";
            this.toolTip.SetToolTip(this.removeAdsLabel, resources.GetString("removeAdsLabel.ToolTip"));
            // 
            // detectAdsCbo
            // 
            this.detectAdsCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.detectAdsCbo.FormattingEnabled = true;
            this.detectAdsCbo.Items.AddRange(new object[] {
            "None",
            "Yes (Comskip)",
            "Yes (ShowAnalyzer)"});
            this.detectAdsCbo.Location = new System.Drawing.Point(95, 150);
            this.detectAdsCbo.Name = "detectAdsCbo";
            this.detectAdsCbo.Size = new System.Drawing.Size(121, 21);
            this.detectAdsCbo.TabIndex = 7;
            this.detectAdsCbo.SelectedIndexChanged += new System.EventHandler(this.removeAdsCbo_SelectedIndexChanged);
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.cmdCancel.Location = new System.Drawing.Point(357, 834);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 202;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // oKcmd
            // 
            this.oKcmd.Location = new System.Drawing.Point(26, 834);
            this.oKcmd.Name = "oKcmd";
            this.oKcmd.Size = new System.Drawing.Size(75, 23);
            this.oKcmd.TabIndex = 201;
            this.oKcmd.Text = "OK";
            this.oKcmd.UseVisualStyleBackColor = true;
            this.oKcmd.Click += new System.EventHandler(this.oKcmd_Click);
            // 
            // renameBySeriesChk
            // 
            this.renameBySeriesChk.AutoSize = true;
            this.renameBySeriesChk.Checked = true;
            this.renameBySeriesChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.renameBySeriesChk.Location = new System.Drawing.Point(10, 23);
            this.renameBySeriesChk.Name = "renameBySeriesChk";
            this.renameBySeriesChk.Size = new System.Drawing.Size(204, 17);
            this.renameBySeriesChk.TabIndex = 30;
            this.renameBySeriesChk.Text = "Rename and sort by video information";
            this.toolTip.SetToolTip(this.renameBySeriesChk, "Renames the converted file and places it in a new directory based on the show/ser" +
        "ies information when available\r\n(e.g. ShowName\\ShowName-SXXEYY-EpisodeName-Recor" +
        "dDate)");
            this.renameBySeriesChk.UseVisualStyleBackColor = true;
            this.renameBySeriesChk.CheckedChanged += new System.EventHandler(this.renameBySeriesChk_CheckedChanged);
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 200;
            this.toolTip.AutoPopDelay = 15000;
            this.toolTip.InitialDelay = 40;
            this.toolTip.ReshowDelay = 40;
            // 
            // altRenameBySeriesChk
            // 
            this.altRenameBySeriesChk.AutoSize = true;
            this.altRenameBySeriesChk.Checked = true;
            this.altRenameBySeriesChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.altRenameBySeriesChk.Location = new System.Drawing.Point(221, 46);
            this.altRenameBySeriesChk.Name = "altRenameBySeriesChk";
            this.altRenameBySeriesChk.Size = new System.Drawing.Size(124, 17);
            this.altRenameBySeriesChk.TabIndex = 41;
            this.altRenameBySeriesChk.Text = "Organize by seasons";
            this.toolTip.SetToolTip(this.altRenameBySeriesChk, "Organize folders by Showname and Season when available (Media Center/XBMC/Plex co" +
        "mpatible naming convention)\r\n(e.g. Showname\\Season XX\\SXXEYY-Episode Name)");
            this.altRenameBySeriesChk.UseVisualStyleBackColor = true;
            // 
            // qualityLabel
            // 
            this.qualityLabel.AutoSize = true;
            this.qualityLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.qualityLabel.Location = new System.Drawing.Point(7, 54);
            this.qualityLabel.Name = "qualityLabel";
            this.qualityLabel.Size = new System.Drawing.Size(39, 13);
            this.qualityLabel.TabIndex = 21;
            this.qualityLabel.Text = "Quality";
            this.toolTip.SetToolTip(this.qualityLabel, resources.GetString("qualityLabel.ToolTip"));
            // 
            // maxWidthLabel
            // 
            this.maxWidthLabel.AutoSize = true;
            this.maxWidthLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.maxWidthLabel.Location = new System.Drawing.Point(7, 25);
            this.maxWidthLabel.Name = "maxWidthLabel";
            this.maxWidthLabel.Size = new System.Drawing.Size(55, 13);
            this.maxWidthLabel.TabIndex = 19;
            this.maxWidthLabel.Text = "Max width";
            this.toolTip.SetToolTip(this.maxWidthLabel, resources.GetString("maxWidthLabel.ToolTip"));
            // 
            // metaShowMatchLabel
            // 
            this.metaShowMatchLabel.AutoSize = true;
            this.metaShowMatchLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.metaShowMatchLabel.Location = new System.Drawing.Point(7, 55);
            this.metaShowMatchLabel.Name = "metaShowMatchLabel";
            this.metaShowMatchLabel.Size = new System.Drawing.Size(107, 13);
            this.metaShowMatchLabel.TabIndex = 15;
            this.metaShowMatchLabel.Text = "Recording title match";
            this.toolTip.SetToolTip(this.metaShowMatchLabel, resources.GetString("metaShowMatchLabel.ToolTip"));
            // 
            // fileMatchLabel
            // 
            this.fileMatchLabel.AutoSize = true;
            this.fileMatchLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.fileMatchLabel.Location = new System.Drawing.Point(7, 25);
            this.fileMatchLabel.Name = "fileMatchLabel";
            this.fileMatchLabel.Size = new System.Drawing.Size(84, 13);
            this.fileMatchLabel.TabIndex = 16;
            this.fileMatchLabel.Text = "File name match";
            this.toolTip.SetToolTip(this.fileMatchLabel, resources.GetString("fileMatchLabel.ToolTip"));
            // 
            // langCode
            // 
            this.langCode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.langCode.Location = new System.Drawing.Point(338, 163);
            this.langCode.MaxLength = 3;
            this.langCode.Name = "langCode";
            this.langCode.Size = new System.Drawing.Size(39, 20);
            this.langCode.TabIndex = 111;
            this.langCode.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip.SetToolTip(this.langCode, resources.GetString("langCode.ToolTip"));
            this.langCode.WordWrap = false;
            this.langCode.TextChanged += new System.EventHandler(this.langCode_TextChanged);
            // 
            // langLbl
            // 
            this.langLbl.AutoSize = true;
            this.langLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.langLbl.Location = new System.Drawing.Point(7, 166);
            this.langLbl.Name = "langLbl";
            this.langLbl.Size = new System.Drawing.Size(81, 13);
            this.langLbl.TabIndex = 26;
            this.langLbl.Text = "Audio language";
            this.toolTip.SetToolTip(this.langLbl, resources.GetString("langLbl.ToolTip"));
            // 
            // volumeLbl
            // 
            this.volumeLbl.AutoSize = true;
            this.volumeLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.volumeLbl.Location = new System.Drawing.Point(7, 109);
            this.volumeLbl.Name = "volumeLbl";
            this.volumeLbl.Size = new System.Drawing.Size(42, 13);
            this.volumeLbl.TabIndex = 21;
            this.volumeLbl.Text = "Volume";
            this.toolTip.SetToolTip(this.volumeLbl, "Used to increase or decrease the volume of the\r\nrecording while converting.\r\n1 is" +
        " normal, anything below will reduce it, anything\r\nhigher will increase it.");
            // 
            // customReNamingChk
            // 
            this.customReNamingChk.AutoSize = true;
            this.customReNamingChk.Location = new System.Drawing.Point(10, 46);
            this.customReNamingChk.Name = "customReNamingChk";
            this.customReNamingChk.Size = new System.Drawing.Size(158, 17);
            this.customReNamingChk.TabIndex = 40;
            this.customReNamingChk.Text = "Enable custom file renaming";
            this.toolTip.SetToolTip(this.customReNamingChk, resources.GetString("customReNamingChk.ToolTip"));
            this.customReNamingChk.UseVisualStyleBackColor = true;
            this.customReNamingChk.CheckedChanged += new System.EventHandler(this.customReNaming_CheckedChanged);
            // 
            // extractCC
            // 
            this.extractCC.AutoSize = true;
            this.extractCC.Location = new System.Drawing.Point(11, 21);
            this.extractCC.Name = "extractCC";
            this.extractCC.Size = new System.Drawing.Size(198, 17);
            this.extractCC.TabIndex = 130;
            this.extractCC.Text = "Extract subtitles and closed captions";
            this.toolTip.SetToolTip(this.extractCC, "Enable this to extract Closed Captions, Subtitles and Teletext from\r\nWTV, DVR-MS," +
        " TS, MP4, MKV and other supported files.\r\n\r\nIt is saved as a Subtitle (SRT) file" +
        ".");
            this.extractCC.UseVisualStyleBackColor = true;
            this.extractCC.CheckedChanged += new System.EventHandler(this.extractCC1_CheckedChanged);
            // 
            // multiChannelAudioChk
            // 
            this.multiChannelAudioChk.AutoSize = true;
            this.multiChannelAudioChk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.multiChannelAudioChk.Location = new System.Drawing.Point(214, 135);
            this.multiChannelAudioChk.Name = "multiChannelAudioChk";
            this.multiChannelAudioChk.Size = new System.Drawing.Size(115, 17);
            this.multiChannelAudioChk.TabIndex = 105;
            this.multiChannelAudioChk.Text = "Multichannel audio";
            this.toolTip.SetToolTip(this.multiChannelAudioChk, "Check to allow multi-channel audio if the source video supports it.\r\nBy default t" +
        "he audio is limited to 2 channels (stereo).");
            this.multiChannelAudioChk.UseVisualStyleBackColor = true;
            // 
            // customRenamePreview
            // 
            this.customRenamePreview.BackColor = System.Drawing.SystemColors.Control;
            this.customRenamePreview.Enabled = false;
            this.customRenamePreview.ForeColor = System.Drawing.SystemColors.WindowText;
            this.customRenamePreview.Location = new System.Drawing.Point(10, 95);
            this.customRenamePreview.Name = "customRenamePreview";
            this.customRenamePreview.ReadOnly = true;
            this.customRenamePreview.Size = new System.Drawing.Size(367, 20);
            this.customRenamePreview.TabIndex = 55;
            this.toolTip.SetToolTip(this.customRenamePreview, "Shows a preview of the custom filename and path");
            // 
            // renameOnlyChk
            // 
            this.renameOnlyChk.AutoSize = true;
            this.renameOnlyChk.Location = new System.Drawing.Point(221, 23);
            this.renameOnlyChk.Name = "renameOnlyChk";
            this.renameOnlyChk.Size = new System.Drawing.Size(156, 17);
            this.renameOnlyChk.TabIndex = 31;
            this.renameOnlyChk.Text = "Rename without converting";
            this.toolTip.SetToolTip(this.renameOnlyChk, resources.GetString("renameOnlyChk.ToolTip"));
            this.renameOnlyChk.UseVisualStyleBackColor = true;
            this.renameOnlyChk.CheckedChanged += new System.EventHandler(this.renameOnlyChk_CheckedChanged);
            // 
            // metaNetworkMatchLbl
            // 
            this.metaNetworkMatchLbl.AutoSize = true;
            this.metaNetworkMatchLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.metaNetworkMatchLbl.Location = new System.Drawing.Point(7, 85);
            this.metaNetworkMatchLbl.Name = "metaNetworkMatchLbl";
            this.metaNetworkMatchLbl.Size = new System.Drawing.Size(107, 13);
            this.metaNetworkMatchLbl.TabIndex = 15;
            this.metaNetworkMatchLbl.Text = "Channel name match";
            this.toolTip.SetToolTip(this.metaNetworkMatchLbl, resources.GetString("metaNetworkMatchLbl.ToolTip"));
            // 
            // addiTunesChk
            // 
            this.addiTunesChk.AutoSize = true;
            this.addiTunesChk.Location = new System.Drawing.Point(221, 23);
            this.addiTunesChk.Name = "addiTunesChk";
            this.addiTunesChk.Size = new System.Drawing.Size(92, 17);
            this.addiTunesChk.TabIndex = 21;
            this.addiTunesChk.Text = "Add to iTunes";
            this.toolTip.SetToolTip(this.addiTunesChk, "Check this box to automatically add the converted files to the iTunes library.\r\nT" +
        "his only works with iTunes supported files (e.g. MP4, M4V, MP3 etc)");
            this.addiTunesChk.UseVisualStyleBackColor = true;
            // 
            // setConnectionCredentials
            // 
            this.setConnectionCredentials.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.setConnectionCredentials.Image = global::MCEBuddy.GUI.Properties.Resources.UserCredentials;
            this.setConnectionCredentials.Location = new System.Drawing.Point(406, 117);
            this.setConnectionCredentials.Name = "setConnectionCredentials";
            this.setConnectionCredentials.Size = new System.Drawing.Size(26, 21);
            this.setConnectionCredentials.TabIndex = 6;
            this.toolTip.SetToolTip(this.setConnectionCredentials, "Enter the username and password to access folders that are on a network drive or " +
        "a different computer");
            this.setConnectionCredentials.UseVisualStyleBackColor = true;
            this.setConnectionCredentials.Click += new System.EventHandler(this.setConnectionCredentials_Click);
            // 
            // showAdvControls
            // 
            this.showAdvControls.BackgroundImage = global::MCEBuddy.GUI.Properties.Resources._3d_up_and_down_arrow1;
            this.showAdvControls.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.showAdvControls.Location = new System.Drawing.Point(402, 180);
            this.showAdvControls.Name = "showAdvControls";
            this.showAdvControls.Size = new System.Drawing.Size(20, 20);
            this.showAdvControls.TabIndex = 10;
            this.toolTip.SetToolTip(this.showAdvControls, "Click this button to open or close the Advanced Settings section");
            this.showAdvControls.UseVisualStyleBackColor = true;
            this.showAdvControls.Click += new System.EventHandler(this.showAdvControls_Click);
            // 
            // addToWMPChk
            // 
            this.addToWMPChk.AutoSize = true;
            this.addToWMPChk.Location = new System.Drawing.Point(11, 23);
            this.addToWMPChk.Name = "addToWMPChk";
            this.addToWMPChk.Size = new System.Drawing.Size(168, 17);
            this.addToWMPChk.TabIndex = 20;
            this.addToWMPChk.Text = "Add to Windows Media Player";
            this.toolTip.SetToolTip(this.addToWMPChk, "Check this box to automatically add the converted files to the WMP library.");
            this.addToWMPChk.UseVisualStyleBackColor = true;
            // 
            // expertSettingsBtn
            // 
            this.expertSettingsBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.expertSettingsBtn.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.expertSettingsBtn.Location = new System.Drawing.Point(178, 834);
            this.expertSettingsBtn.Name = "expertSettingsBtn";
            this.expertSettingsBtn.Size = new System.Drawing.Size(102, 23);
            this.expertSettingsBtn.TabIndex = 200;
            this.expertSettingsBtn.Text = "Expert Settings";
            this.toolTip.SetToolTip(this.expertSettingsBtn, "Click here to change additional settings (for experts only)");
            this.expertSettingsBtn.UseVisualStyleBackColor = true;
            this.expertSettingsBtn.Click += new System.EventHandler(this.expertSettingsBtn_Click);
            // 
            // metadataShowTypeMatchLbl
            // 
            this.metadataShowTypeMatchLbl.AutoSize = true;
            this.metadataShowTypeMatchLbl.Location = new System.Drawing.Point(7, 115);
            this.metadataShowTypeMatchLbl.Name = "metadataShowTypeMatchLbl";
            this.metadataShowTypeMatchLbl.Size = new System.Drawing.Size(57, 13);
            this.metadataShowTypeMatchLbl.TabIndex = 71;
            this.metadataShowTypeMatchLbl.Text = "Show type";
            this.toolTip.SetToolTip(this.metadataShowTypeMatchLbl, resources.GetString("metadataShowTypeMatchLbl.ToolTip"));
            // 
            // singleAudioTrackChk
            // 
            this.singleAudioTrackChk.AutoSize = true;
            this.singleAudioTrackChk.Checked = true;
            this.singleAudioTrackChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.singleAudioTrackChk.Location = new System.Drawing.Point(73, 135);
            this.singleAudioTrackChk.Name = "singleAudioTrackChk";
            this.singleAudioTrackChk.Size = new System.Drawing.Size(135, 17);
            this.singleAudioTrackChk.TabIndex = 101;
            this.singleAudioTrackChk.Text = "Select best soundtrack";
            this.toolTip.SetToolTip(this.singleAudioTrackChk, resources.GetString("singleAudioTrackChk.ToolTip"));
            this.singleAudioTrackChk.UseVisualStyleBackColor = true;
            // 
            // autoDeinterlaceChk
            // 
            this.autoDeinterlaceChk.AutoSize = true;
            this.autoDeinterlaceChk.Checked = true;
            this.autoDeinterlaceChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoDeinterlaceChk.Location = new System.Drawing.Point(73, 80);
            this.autoDeinterlaceChk.Name = "autoDeinterlaceChk";
            this.autoDeinterlaceChk.Size = new System.Drawing.Size(182, 17);
            this.autoDeinterlaceChk.TabIndex = 85;
            this.autoDeinterlaceChk.Text = "Detect and optimize video quality";
            this.toolTip.SetToolTip(this.autoDeinterlaceChk, resources.GetString("autoDeinterlaceChk.ToolTip"));
            this.autoDeinterlaceChk.UseVisualStyleBackColor = true;
            // 
            // commercialSkipCutChk
            // 
            this.commercialSkipCutChk.AutoSize = true;
            this.commercialSkipCutChk.ForeColor = System.Drawing.Color.Black;
            this.commercialSkipCutChk.Location = new System.Drawing.Point(257, 152);
            this.commercialSkipCutChk.Name = "commercialSkipCutChk";
            this.commercialSkipCutChk.Size = new System.Drawing.Size(101, 17);
            this.commercialSkipCutChk.TabIndex = 203;
            this.commercialSkipCutChk.Text = "Only detect Ads";
            this.toolTip.SetToolTip(this.commercialSkipCutChk, resources.GetString("commercialSkipCutChk.ToolTip"));
            this.commercialSkipCutChk.UseVisualStyleBackColor = true;
            this.commercialSkipCutChk.CheckedChanged += new System.EventHandler(this.commercialSkipCutChk_CheckedChanged);
            // 
            // advancedSettings
            // 
            this.advancedSettings.Controls.Add(this.groupBox5);
            this.advancedSettings.Controls.Add(this.groupBox1);
            this.advancedSettings.Controls.Add(this.groupBox2);
            this.advancedSettings.Controls.Add(this.groupBox3);
            this.advancedSettings.Controls.Add(this.groupBox4);
            this.advancedSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.advancedSettings.Location = new System.Drawing.Point(26, 181);
            this.advancedSettings.Name = "advancedSettings";
            this.advancedSettings.Size = new System.Drawing.Size(406, 636);
            this.advancedSettings.TabIndex = 16;
            this.advancedSettings.TabStop = false;
            this.advancedSettings.Text = "Advanced Settings";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.extractCC);
            this.groupBox5.Location = new System.Drawing.Point(10, 580);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(386, 47);
            this.groupBox5.TabIndex = 145;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Subtitles";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.addToWMPChk);
            this.groupBox1.Controls.Add(this.addiTunesChk);
            this.groupBox1.Location = new System.Drawing.Point(10, 21);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(386, 50);
            this.groupBox1.TabIndex = 141;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Library Management";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.renameBySeriesChk);
            this.groupBox2.Controls.Add(this.customFileRenamePattern);
            this.groupBox2.Controls.Add(this.altRenameBySeriesChk);
            this.groupBox2.Controls.Add(this.renameOnlyChk);
            this.groupBox2.Controls.Add(this.customReNamingChk);
            this.groupBox2.Controls.Add(this.customRenamePreview);
            this.groupBox2.Location = new System.Drawing.Point(10, 82);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(386, 127);
            this.groupBox2.TabIndex = 142;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Renaming and Sorting";
            // 
            // customFileRenamePattern
            // 
            this.customFileRenamePattern.Enabled = false;
            this.customFileRenamePattern.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.customFileRenamePattern.Location = new System.Drawing.Point(10, 69);
            this.customFileRenamePattern.Name = "customFileRenamePattern";
            this.customFileRenamePattern.Size = new System.Drawing.Size(367, 20);
            this.customFileRenamePattern.TabIndex = 50;
            this.customFileRenamePattern.TextChanged += new System.EventHandler(this.customFileRenamePattern_TextChanged);
            this.customFileRenamePattern.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.customFileRenamePattern_KeyPress);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.metaShowTypeCbo);
            this.groupBox3.Controls.Add(this.metadataShowTypeMatchLbl);
            this.groupBox3.Controls.Add(this.fileMatchLabel);
            this.groupBox3.Controls.Add(this.metaShowMatchLabel);
            this.groupBox3.Controls.Add(this.metaNetworkMatchTxt);
            this.groupBox3.Controls.Add(this.fileMatchTxt);
            this.groupBox3.Controls.Add(this.metaShowMatchTxt);
            this.groupBox3.Controls.Add(this.metaNetworkMatchLbl);
            this.groupBox3.Location = new System.Drawing.Point(10, 219);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(386, 143);
            this.groupBox3.TabIndex = 143;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Selection Filters";
            // 
            // metaShowTypeCbo
            // 
            this.metaShowTypeCbo.BackColor = System.Drawing.Color.Gainsboro;
            this.metaShowTypeCbo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.metaShowTypeCbo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.metaShowTypeCbo.FormattingEnabled = true;
            this.metaShowTypeCbo.Items.AddRange(new object[] {
            "All",
            "Movies",
            "Shows",
            "Sports"});
            this.metaShowTypeCbo.Location = new System.Drawing.Point(120, 112);
            this.metaShowTypeCbo.Name = "metaShowTypeCbo";
            this.metaShowTypeCbo.Size = new System.Drawing.Size(60, 21);
            this.metaShowTypeCbo.TabIndex = 68;
            // 
            // metaNetworkMatchTxt
            // 
            this.metaNetworkMatchTxt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.metaNetworkMatchTxt.Location = new System.Drawing.Point(120, 82);
            this.metaNetworkMatchTxt.Name = "metaNetworkMatchTxt";
            this.metaNetworkMatchTxt.Size = new System.Drawing.Size(257, 20);
            this.metaNetworkMatchTxt.TabIndex = 65;
            // 
            // fileMatchTxt
            // 
            this.fileMatchTxt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.fileMatchTxt.Location = new System.Drawing.Point(120, 22);
            this.fileMatchTxt.Name = "fileMatchTxt";
            this.fileMatchTxt.Size = new System.Drawing.Size(257, 20);
            this.fileMatchTxt.TabIndex = 60;
            // 
            // metaShowMatchTxt
            // 
            this.metaShowMatchTxt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.metaShowMatchTxt.Location = new System.Drawing.Point(120, 52);
            this.metaShowMatchTxt.Name = "metaShowMatchTxt";
            this.metaShowMatchTxt.Size = new System.Drawing.Size(257, 20);
            this.metaShowMatchTxt.TabIndex = 63;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.autoDeinterlaceChk);
            this.groupBox4.Controls.Add(this.singleAudioTrackChk);
            this.groupBox4.Controls.Add(this.maxWidthLabel);
            this.groupBox4.Controls.Add(this.qualityLabel);
            this.groupBox4.Controls.Add(this.maxWidthTxt);
            this.groupBox4.Controls.Add(this.volumeLbl);
            this.groupBox4.Controls.Add(this.qualityTxt);
            this.groupBox4.Controls.Add(this.qualityBar);
            this.groupBox4.Controls.Add(this.volumeTxt);
            this.groupBox4.Controls.Add(this.multiChannelAudioChk);
            this.groupBox4.Controls.Add(this.volumeBar);
            this.groupBox4.Controls.Add(this.maxWidthBar);
            this.groupBox4.Controls.Add(this.langLbl);
            this.groupBox4.Controls.Add(this.langBox);
            this.groupBox4.Controls.Add(this.langCode);
            this.groupBox4.Location = new System.Drawing.Point(10, 375);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(386, 195);
            this.groupBox4.TabIndex = 144;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Audio and Video";
            // 
            // maxWidthTxt
            // 
            this.maxWidthTxt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.maxWidthTxt.Location = new System.Drawing.Point(338, 25);
            this.maxWidthTxt.Name = "maxWidthTxt";
            this.maxWidthTxt.Size = new System.Drawing.Size(39, 20);
            this.maxWidthTxt.TabIndex = 71;
            this.maxWidthTxt.Text = "720";
            this.maxWidthTxt.TextChanged += new System.EventHandler(this.maxWidthTxt_TextChanged);
            this.maxWidthTxt.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.maxWidthTxt_KeyPress);
            // 
            // qualityTxt
            // 
            this.qualityTxt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.qualityTxt.Location = new System.Drawing.Point(338, 54);
            this.qualityTxt.Name = "qualityTxt";
            this.qualityTxt.ReadOnly = true;
            this.qualityTxt.Size = new System.Drawing.Size(39, 20);
            this.qualityTxt.TabIndex = 81;
            this.qualityTxt.TabStop = false;
            this.qualityTxt.Text = "1.0";
            // 
            // qualityBar
            // 
            this.qualityBar.AutoSize = false;
            this.qualityBar.Location = new System.Drawing.Point(73, 51);
            this.qualityBar.Maximum = 40;
            this.qualityBar.Minimum = 1;
            this.qualityBar.Name = "qualityBar";
            this.qualityBar.Size = new System.Drawing.Size(261, 23);
            this.qualityBar.TabIndex = 80;
            this.qualityBar.TickFrequency = 2;
            this.qualityBar.Value = 20;
            this.qualityBar.Scroll += new System.EventHandler(this.qualityBar_Scroll);
            // 
            // volumeTxt
            // 
            this.volumeTxt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.volumeTxt.Location = new System.Drawing.Point(338, 109);
            this.volumeTxt.Name = "volumeTxt";
            this.volumeTxt.ReadOnly = true;
            this.volumeTxt.Size = new System.Drawing.Size(39, 20);
            this.volumeTxt.TabIndex = 91;
            this.volumeTxt.TabStop = false;
            this.volumeTxt.Text = "1.0";
            // 
            // volumeBar
            // 
            this.volumeBar.AutoSize = false;
            this.volumeBar.LargeChange = 50;
            this.volumeBar.Location = new System.Drawing.Point(73, 106);
            this.volumeBar.Maximum = 300;
            this.volumeBar.Minimum = -300;
            this.volumeBar.Name = "volumeBar";
            this.volumeBar.Size = new System.Drawing.Size(261, 23);
            this.volumeBar.TabIndex = 90;
            this.volumeBar.TickFrequency = 30;
            this.volumeBar.Scroll += new System.EventHandler(this.volumeBar_Scroll);
            // 
            // maxWidthBar
            // 
            this.maxWidthBar.AutoSize = false;
            this.maxWidthBar.Location = new System.Drawing.Point(73, 25);
            this.maxWidthBar.Maximum = 8;
            this.maxWidthBar.Name = "maxWidthBar";
            this.maxWidthBar.Size = new System.Drawing.Size(261, 20);
            this.maxWidthBar.TabIndex = 70;
            this.maxWidthBar.Value = 4;
            this.maxWidthBar.Scroll += new System.EventHandler(this.maxWidthBar_Scroll);
            // 
            // langBox
            // 
            this.langBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.langBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.langBox.BackColor = System.Drawing.Color.Gainsboro;
            this.langBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.langBox.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.langBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.langBox.FormattingEnabled = true;
            this.langBox.Location = new System.Drawing.Point(94, 163);
            this.langBox.Name = "langBox";
            this.langBox.Size = new System.Drawing.Size(232, 21);
            this.langBox.TabIndex = 110;
            this.langBox.SelectedIndexChanged += new System.EventHandler(this.langBox_SelectedIndexChanged);
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog1";
            // 
            // ConversionTaskForm
            // 
            this.AcceptButton = this.oKcmd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(462, 880);
            this.Controls.Add(this.commercialSkipCutChk);
            this.Controls.Add(this.showAdvControls);
            this.Controls.Add(this.expertSettingsBtn);
            this.Controls.Add(this.setConnectionCredentials);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.oKcmd);
            this.Controls.Add(this.detectAdsCbo);
            this.Controls.Add(this.removeAdsLabel);
            this.Controls.Add(this.SelectFolderCmd);
            this.Controls.Add(this.destinationPathTxt);
            this.Controls.Add(this.destinationLabel);
            this.Controls.Add(this.conversionDescriptionTxt);
            this.Controls.Add(this.profileCbo);
            this.Controls.Add(this.taskNameTxt);
            this.Controls.Add(this.conversionLabel);
            this.Controls.Add(this.taskNameLabel);
            this.Controls.Add(this.advancedSettings);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ConversionTaskForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 25, 20);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Conversion Task";
            this.Load += new System.EventHandler(this.ConversionTaskForm_Load);
            this.advancedSettings.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.qualityBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.volumeBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxWidthBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox taskNameTxt;
        private System.Windows.Forms.Label taskNameLabel;
        private System.Windows.Forms.ComboBox profileCbo;
        private System.Windows.Forms.TextBox conversionDescriptionTxt;
        private System.Windows.Forms.Label conversionLabel;
        private System.Windows.Forms.Button SelectFolderCmd;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.TextBox destinationPathTxt;
        private System.Windows.Forms.Label destinationLabel;
        private System.Windows.Forms.Label removeAdsLabel;
        private System.Windows.Forms.ComboBox detectAdsCbo;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Button oKcmd;
        private System.Windows.Forms.CheckBox renameBySeriesChk;
        private System.Windows.Forms.Button setConnectionCredentials;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.CheckBox altRenameBySeriesChk;
        private System.Windows.Forms.GroupBox advancedSettings;
        private System.Windows.Forms.TextBox langCode;
        private System.Windows.Forms.ComboBox langBox;
        private System.Windows.Forms.Label langLbl;
        private System.Windows.Forms.TrackBar maxWidthBar;
        private System.Windows.Forms.TrackBar qualityBar;
        private System.Windows.Forms.TextBox qualityTxt;
        private System.Windows.Forms.TextBox maxWidthTxt;
        private System.Windows.Forms.Label qualityLabel;
        private System.Windows.Forms.Label maxWidthLabel;
        private System.Windows.Forms.TextBox metaShowMatchTxt;
        private System.Windows.Forms.TextBox fileMatchTxt;
        private System.Windows.Forms.Label metaShowMatchLabel;
        private System.Windows.Forms.Label fileMatchLabel;
        private System.Windows.Forms.TrackBar volumeBar;
        private System.Windows.Forms.TextBox volumeTxt;
        private System.Windows.Forms.Label volumeLbl;
        private System.Windows.Forms.Button showAdvControls;
        private System.Windows.Forms.CheckBox customReNamingChk;
        private System.Windows.Forms.TextBox customFileRenamePattern;
        private System.Windows.Forms.CheckBox extractCC;
        private System.Windows.Forms.CheckBox multiChannelAudioChk;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.TextBox customRenamePreview;
        private System.Windows.Forms.CheckBox renameOnlyChk;
        private System.Windows.Forms.TextBox metaNetworkMatchTxt;
        private System.Windows.Forms.Label metaNetworkMatchLbl;
        private System.Windows.Forms.CheckBox addiTunesChk;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox addToWMPChk;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button expertSettingsBtn;
        private System.Windows.Forms.Label metadataShowTypeMatchLbl;
        private System.Windows.Forms.ComboBox metaShowTypeCbo;
        private System.Windows.Forms.CheckBox singleAudioTrackChk;
        private System.Windows.Forms.CheckBox autoDeinterlaceChk;
        private System.Windows.Forms.CheckBox commercialSkipCutChk;
    }
}