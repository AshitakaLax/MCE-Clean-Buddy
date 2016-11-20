namespace MCEBuddy.GUI
{
    partial class MonitorTaskExpertSettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MonitorTaskExpertSettingsForm));
            this.cmdCancel = new System.Windows.Forms.Button();
            this.oKcmd = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.grpBoxFiles = new System.Windows.Forms.GroupBox();
            this.archiveOriginalChk = new System.Windows.Forms.CheckBox();
            this.deleteOriginalChk = new System.Windows.Forms.CheckBox();
            this.archiveFolderCmd = new System.Windows.Forms.Button();
            this.archivePathTxt = new System.Windows.Forms.TextBox();
            this.archiveLbl = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.reMonitorRecordedChk = new System.Windows.Forms.CheckBox();
            this.monitorConvertedChk = new System.Windows.Forms.CheckBox();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.grpBoxFiles.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.cmdCancel.Location = new System.Drawing.Point(307, 215);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 1100;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // oKcmd
            // 
            this.oKcmd.Location = new System.Drawing.Point(19, 215);
            this.oKcmd.Name = "oKcmd";
            this.oKcmd.Size = new System.Drawing.Size(75, 23);
            this.oKcmd.TabIndex = 1000;
            this.oKcmd.Text = "OK";
            this.oKcmd.UseVisualStyleBackColor = true;
            this.oKcmd.Click += new System.EventHandler(this.oKcmd_Click);
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 200;
            this.toolTip.AutoPopDelay = 9000;
            this.toolTip.InitialDelay = 40;
            this.toolTip.ReshowDelay = 40;
            // 
            // grpBoxFiles
            // 
            this.grpBoxFiles.Controls.Add(this.archiveFolderCmd);
            this.grpBoxFiles.Controls.Add(this.archivePathTxt);
            this.grpBoxFiles.Controls.Add(this.archiveLbl);
            this.grpBoxFiles.Controls.Add(this.archiveOriginalChk);
            this.grpBoxFiles.Controls.Add(this.deleteOriginalChk);
            this.grpBoxFiles.Location = new System.Drawing.Point(19, 13);
            this.grpBoxFiles.Name = "grpBoxFiles";
            this.grpBoxFiles.Size = new System.Drawing.Size(363, 94);
            this.grpBoxFiles.TabIndex = 100;
            this.grpBoxFiles.TabStop = false;
            this.grpBoxFiles.Text = "Files";
            // 
            // archiveOriginalChk
            // 
            this.archiveOriginalChk.AutoSize = true;
            this.archiveOriginalChk.Location = new System.Drawing.Point(15, 28);
            this.archiveOriginalChk.Name = "archiveOriginalChk";
            this.archiveOriginalChk.Size = new System.Drawing.Size(114, 17);
            this.archiveOriginalChk.TabIndex = 110;
            this.archiveOriginalChk.Text = "Archive original file";
            this.toolTip.SetToolTip(this.archiveOriginalChk, resources.GetString("archiveOriginalChk.ToolTip"));
            this.archiveOriginalChk.UseVisualStyleBackColor = true;
            this.archiveOriginalChk.CheckedChanged += new System.EventHandler(this.archiveOriginalChk_CheckedChanged);
            // 
            // deleteOriginalChk
            // 
            this.deleteOriginalChk.AutoSize = true;
            this.deleteOriginalChk.Location = new System.Drawing.Point(192, 28);
            this.deleteOriginalChk.Name = "deleteOriginalChk";
            this.deleteOriginalChk.Size = new System.Drawing.Size(109, 17);
            this.deleteOriginalChk.TabIndex = 111;
            this.deleteOriginalChk.Text = "Delete original file";
            this.toolTip.SetToolTip(this.deleteOriginalChk, "Deletes the original recording after a successful conversion");
            this.deleteOriginalChk.UseVisualStyleBackColor = true;
            this.deleteOriginalChk.CheckedChanged += new System.EventHandler(this.deleteOriginalChk_CheckedChanged);
            // 
            // archiveFolderCmd
            // 
            this.archiveFolderCmd.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.archiveFolderCmd.Location = new System.Drawing.Point(328, 54);
            this.archiveFolderCmd.Name = "archiveFolderCmd";
            this.archiveFolderCmd.Size = new System.Drawing.Size(29, 20);
            this.archiveFolderCmd.TabIndex = 121;
            this.archiveFolderCmd.Text = "...";
            this.archiveFolderCmd.UseVisualStyleBackColor = true;
            this.archiveFolderCmd.Click += new System.EventHandler(this.archiveFolderCmd_Click);
            // 
            // archivePathTxt
            // 
            this.archivePathTxt.Location = new System.Drawing.Point(92, 54);
            this.archivePathTxt.Name = "archivePathTxt";
            this.archivePathTxt.Size = new System.Drawing.Size(230, 20);
            this.archivePathTxt.TabIndex = 120;
            // 
            // archiveLbl
            // 
            this.archiveLbl.AutoSize = true;
            this.archiveLbl.Location = new System.Drawing.Point(12, 57);
            this.archiveLbl.Name = "archiveLbl";
            this.archiveLbl.Size = new System.Drawing.Size(72, 13);
            this.archiveLbl.TabIndex = 115;
            this.archiveLbl.Text = "Archive folder";
            this.toolTip.SetToolTip(this.archiveLbl, "Change the location of the archive folder.\r\nLeave empty to use the default \'MCEBu" +
        "ddyArchive\' folder in the original directory.");
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.reMonitorRecordedChk);
            this.groupBox1.Controls.Add(this.monitorConvertedChk);
            this.groupBox1.Location = new System.Drawing.Point(19, 122);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(363, 66);
            this.groupBox1.TabIndex = 116;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Monitoring";
            // 
            // reMonitorRecordedChk
            // 
            this.reMonitorRecordedChk.AutoSize = true;
            this.reMonitorRecordedChk.ForeColor = System.Drawing.Color.Firebrick;
            this.reMonitorRecordedChk.Location = new System.Drawing.Point(192, 28);
            this.reMonitorRecordedChk.Name = "reMonitorRecordedChk";
            this.reMonitorRecordedChk.Size = new System.Drawing.Size(156, 17);
            this.reMonitorRecordedChk.TabIndex = 211;
            this.reMonitorRecordedChk.Text = "Re-monitor recorded videos";
            this.toolTip.SetToolTip(this.reMonitorRecordedChk, resources.GetString("reMonitorRecordedChk.ToolTip"));
            this.reMonitorRecordedChk.UseVisualStyleBackColor = true;
            // 
            // monitorConvertedChk
            // 
            this.monitorConvertedChk.AutoSize = true;
            this.monitorConvertedChk.ForeColor = System.Drawing.Color.Firebrick;
            this.monitorConvertedChk.Location = new System.Drawing.Point(15, 28);
            this.monitorConvertedChk.Name = "monitorConvertedChk";
            this.monitorConvertedChk.Size = new System.Drawing.Size(146, 17);
            this.monitorConvertedChk.TabIndex = 210;
            this.monitorConvertedChk.Text = "Monitor converted videos";
            this.toolTip.SetToolTip(this.monitorConvertedChk, resources.GetString("monitorConvertedChk.ToolTip"));
            this.monitorConvertedChk.UseVisualStyleBackColor = true;
            // 
            // MonitorTaskExpertSettingsForm
            // 
            this.AcceptButton = this.oKcmd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(410, 269);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.grpBoxFiles);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.oKcmd);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MonitorTaskExpertSettingsForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 25, 20);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Expert Settings";
            this.Load += new System.EventHandler(this.SourceForm_Load);
            this.grpBoxFiles.ResumeLayout(false);
            this.grpBoxFiles.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Button oKcmd;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.GroupBox grpBoxFiles;
        private System.Windows.Forms.CheckBox archiveOriginalChk;
        private System.Windows.Forms.CheckBox deleteOriginalChk;
        private System.Windows.Forms.Button archiveFolderCmd;
        private System.Windows.Forms.TextBox archivePathTxt;
        private System.Windows.Forms.Label archiveLbl;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox reMonitorRecordedChk;
        private System.Windows.Forms.CheckBox monitorConvertedChk;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
    }
}