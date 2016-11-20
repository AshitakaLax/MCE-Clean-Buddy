namespace MCEBuddy.GUI
{
    partial class MonitorTaskForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MonitorTaskForm));
            this.label1 = new System.Windows.Forms.Label();
            this.sourceNameTxt = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.searchPathTxt = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.SelectFolderCmd = new System.Windows.Forms.Button();
            this.searchPatternTxt = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.oKcmd = new System.Windows.Forms.Button();
            this.setConnectionCredentials = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.monitorSubdirChk = new System.Windows.Forms.CheckBox();
            this.expertSettingsBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Monitor name";
            this.toolTip.SetToolTip(this.label1, "Give this a location a unique name");
            // 
            // sourceNameTxt
            // 
            this.sourceNameTxt.Location = new System.Drawing.Point(106, 20);
            this.sourceNameTxt.Name = "sourceNameTxt";
            this.sourceNameTxt.Size = new System.Drawing.Size(276, 20);
            this.sourceNameTxt.TabIndex = 10;
            this.sourceNameTxt.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.sourceNameTxt_KeyPress);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 55);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Monitor path";
            this.toolTip.SetToolTip(this.label2, "Select the directory that MCEBuddy will monitor in the background for new video f" +
        "iles to convert automatically");
            // 
            // searchPathTxt
            // 
            this.searchPathTxt.Location = new System.Drawing.Point(106, 52);
            this.searchPathTxt.Name = "searchPathTxt";
            this.searchPathTxt.Size = new System.Drawing.Size(206, 20);
            this.searchPathTxt.TabIndex = 20;
            this.searchPathTxt.Leave += new System.EventHandler(this.searchPathTxt_Leave);
            // 
            // SelectFolderCmd
            // 
            this.SelectFolderCmd.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.SelectFolderCmd.Location = new System.Drawing.Point(320, 52);
            this.SelectFolderCmd.Name = "SelectFolderCmd";
            this.SelectFolderCmd.Size = new System.Drawing.Size(29, 20);
            this.SelectFolderCmd.TabIndex = 25;
            this.SelectFolderCmd.Text = "...";
            this.toolTip.SetToolTip(this.SelectFolderCmd, "Click to select the folder to monitor");
            this.SelectFolderCmd.UseVisualStyleBackColor = true;
            this.SelectFolderCmd.Click += new System.EventHandler(this.SelectFolderCmd_Click);
            // 
            // searchPatternTxt
            // 
            this.searchPatternTxt.Location = new System.Drawing.Point(106, 103);
            this.searchPatternTxt.Name = "searchPatternTxt";
            this.searchPatternTxt.Size = new System.Drawing.Size(276, 20);
            this.searchPatternTxt.TabIndex = 40;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(23, 106);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Search pattern";
            this.toolTip.SetToolTip(this.label3, resources.GetString("label3.ToolTip"));
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.cmdCancel.Location = new System.Drawing.Point(307, 140);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 200;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // oKcmd
            // 
            this.oKcmd.Location = new System.Drawing.Point(19, 140);
            this.oKcmd.Name = "oKcmd";
            this.oKcmd.Size = new System.Drawing.Size(75, 23);
            this.oKcmd.TabIndex = 100;
            this.oKcmd.Text = "OK";
            this.oKcmd.UseVisualStyleBackColor = true;
            this.oKcmd.Click += new System.EventHandler(this.oKcmd_Click);
            // 
            // setConnectionCredentials
            // 
            this.setConnectionCredentials.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.setConnectionCredentials.Image = global::MCEBuddy.GUI.Properties.Resources.UserCredentials;
            this.setConnectionCredentials.Location = new System.Drawing.Point(356, 52);
            this.setConnectionCredentials.Name = "setConnectionCredentials";
            this.setConnectionCredentials.Size = new System.Drawing.Size(26, 21);
            this.setConnectionCredentials.TabIndex = 28;
            this.toolTip.SetToolTip(this.setConnectionCredentials, "Enter the username and password to access folders that are on a network drive or " +
        "a different computer");
            this.setConnectionCredentials.UseVisualStyleBackColor = true;
            this.setConnectionCredentials.Click += new System.EventHandler(this.setConnectionCredentials_Click);
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 200;
            this.toolTip.AutoPopDelay = 9000;
            this.toolTip.InitialDelay = 40;
            this.toolTip.ReshowDelay = 40;
            // 
            // monitorSubdirChk
            // 
            this.monitorSubdirChk.AutoSize = true;
            this.monitorSubdirChk.Checked = true;
            this.monitorSubdirChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.monitorSubdirChk.Location = new System.Drawing.Point(137, 78);
            this.monitorSubdirChk.Name = "monitorSubdirChk";
            this.monitorSubdirChk.Size = new System.Drawing.Size(129, 17);
            this.monitorSubdirChk.TabIndex = 30;
            this.monitorSubdirChk.Text = "Monitor subdirectories";
            this.toolTip.SetToolTip(this.monitorSubdirChk, "Check this box to also monitor all directories within the Monitor path");
            this.monitorSubdirChk.UseVisualStyleBackColor = true;
            // 
            // expertSettingsBtn
            // 
            this.expertSettingsBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.expertSettingsBtn.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.expertSettingsBtn.Location = new System.Drawing.Point(154, 140);
            this.expertSettingsBtn.Name = "expertSettingsBtn";
            this.expertSettingsBtn.Size = new System.Drawing.Size(93, 23);
            this.expertSettingsBtn.TabIndex = 300;
            this.expertSettingsBtn.Text = "Expert Settings";
            this.toolTip.SetToolTip(this.expertSettingsBtn, "Click here to change additional settings (for experts only)");
            this.expertSettingsBtn.UseVisualStyleBackColor = true;
            this.expertSettingsBtn.Click += new System.EventHandler(this.expertSettingsBtn_Click);
            // 
            // MonitorTaskForm
            // 
            this.AcceptButton = this.oKcmd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(410, 186);
            this.Controls.Add(this.expertSettingsBtn);
            this.Controls.Add(this.monitorSubdirChk);
            this.Controls.Add(this.setConnectionCredentials);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.oKcmd);
            this.Controls.Add(this.SelectFolderCmd);
            this.Controls.Add(this.searchPathTxt);
            this.Controls.Add(this.searchPatternTxt);
            this.Controls.Add(this.sourceNameTxt);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MonitorTaskForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 25, 20);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Monitor Location";
            this.Load += new System.EventHandler(this.SourceForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox sourceNameTxt;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox searchPathTxt;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.Button SelectFolderCmd;
        private System.Windows.Forms.TextBox searchPatternTxt;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Button oKcmd;
        private System.Windows.Forms.Button setConnectionCredentials;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.CheckBox monitorSubdirChk;
        private System.Windows.Forms.Button expertSettingsBtn;
    }
}