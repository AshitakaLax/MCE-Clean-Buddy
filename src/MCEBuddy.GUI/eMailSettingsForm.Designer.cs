namespace MCEBuddy.GUI
{
    partial class eMailSettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(eMailSettingsForm));
            this.smtpAddressLbl = new System.Windows.Forms.Label();
            this.smtpServer = new System.Windows.Forms.TextBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.fromLbl = new System.Windows.Forms.Label();
            this.toLbl = new System.Windows.Forms.Label();
            this.portLbl = new System.Windows.Forms.Label();
            this.sslChk = new System.Windows.Forms.CheckBox();
            this.usernameLbl = new System.Windows.Forms.Label();
            this.passwordLbl = new System.Windows.Forms.Label();
            this.testSettings = new System.Windows.Forms.Button();
            this.fromAddress = new System.Windows.Forms.TextBox();
            this.toAddress = new System.Windows.Forms.TextBox();
            this.portNo = new System.Windows.Forms.TextBox();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.oKcmd = new System.Windows.Forms.Button();
            this.sendSuccessChk = new System.Windows.Forms.CheckBox();
            this.sendFailedChk = new System.Windows.Forms.CheckBox();
            this.sendCancelledChk = new System.Windows.Forms.CheckBox();
            this.userName = new System.Windows.Forms.TextBox();
            this.passWord = new System.Windows.Forms.TextBox();
            this.sendStartChk = new System.Windows.Forms.CheckBox();
            this.confirmPasswordLbl = new System.Windows.Forms.Label();
            this.confirmPassword = new System.Windows.Forms.TextBox();
            this.sendDownloadErrorChk = new System.Windows.Forms.CheckBox();
            this.sendQueueChk = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // smtpAddressLbl
            // 
            this.smtpAddressLbl.AutoSize = true;
            this.smtpAddressLbl.Location = new System.Drawing.Point(23, 23);
            this.smtpAddressLbl.Name = "smtpAddressLbl";
            this.smtpAddressLbl.Size = new System.Drawing.Size(102, 13);
            this.smtpAddressLbl.TabIndex = 0;
            this.smtpAddressLbl.Text = "SMTP Server Name";
            this.toolTip.SetToolTip(this.smtpAddressLbl, "Enter the outgoing eMail (SMTP) server address here\r\ne.g. smtp.gmail.com");
            // 
            // smtpServer
            // 
            this.smtpServer.Location = new System.Drawing.Point(131, 20);
            this.smtpServer.Name = "smtpServer";
            this.smtpServer.Size = new System.Drawing.Size(294, 20);
            this.smtpServer.TabIndex = 1;
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 200;
            this.toolTip.AutoPopDelay = 10000;
            this.toolTip.InitialDelay = 40;
            this.toolTip.ReshowDelay = 100;
            // 
            // fromLbl
            // 
            this.fromLbl.AutoSize = true;
            this.fromLbl.Location = new System.Drawing.Point(23, 175);
            this.fromLbl.Name = "fromLbl";
            this.fromLbl.Size = new System.Drawing.Size(73, 13);
            this.fromLbl.TabIndex = 0;
            this.fromLbl.Text = "From <e-Mail>";
            this.toolTip.SetToolTip(this.fromLbl, "Enter the senders (From) eMail address here");
            // 
            // toLbl
            // 
            this.toLbl.AutoSize = true;
            this.toLbl.Location = new System.Drawing.Point(23, 208);
            this.toLbl.Name = "toLbl";
            this.toLbl.Size = new System.Drawing.Size(68, 13);
            this.toLbl.TabIndex = 0;
            this.toLbl.Text = "To <e-Mails>";
            this.toolTip.SetToolTip(this.toLbl, "Enter the recipients (To) eMail address here.\r\nSeparate multiple recipients with " +
        "a semicolon (;)");
            // 
            // portLbl
            // 
            this.portLbl.AutoSize = true;
            this.portLbl.Location = new System.Drawing.Point(258, 50);
            this.portLbl.Name = "portLbl";
            this.portLbl.Size = new System.Drawing.Size(26, 13);
            this.portLbl.TabIndex = 2;
            this.portLbl.Text = "Port";
            this.toolTip.SetToolTip(this.portLbl, "Enter the SMTP server port");
            // 
            // sslChk
            // 
            this.sslChk.AutoSize = true;
            this.sslChk.Location = new System.Drawing.Point(357, 49);
            this.sslChk.Name = "sslChk";
            this.sslChk.Size = new System.Drawing.Size(68, 17);
            this.sslChk.TabIndex = 4;
            this.sslChk.Text = "Use SSL";
            this.toolTip.SetToolTip(this.sslChk, "Check this if you SMTP server uses SSL");
            this.sslChk.UseVisualStyleBackColor = true;
            // 
            // usernameLbl
            // 
            this.usernameLbl.AutoSize = true;
            this.usernameLbl.Location = new System.Drawing.Point(23, 76);
            this.usernameLbl.Name = "usernameLbl";
            this.usernameLbl.Size = new System.Drawing.Size(55, 13);
            this.usernameLbl.TabIndex = 0;
            this.usernameLbl.Text = "Username";
            this.toolTip.SetToolTip(this.usernameLbl, "<Optional> Enter the username for the eMail server authentication");
            // 
            // passwordLbl
            // 
            this.passwordLbl.AutoSize = true;
            this.passwordLbl.Location = new System.Drawing.Point(23, 110);
            this.passwordLbl.Name = "passwordLbl";
            this.passwordLbl.Size = new System.Drawing.Size(53, 13);
            this.passwordLbl.TabIndex = 0;
            this.passwordLbl.Text = "Password";
            this.toolTip.SetToolTip(this.passwordLbl, "<Optional> Enter the password for the eMail server authentication");
            // 
            // testSettings
            // 
            this.testSettings.Location = new System.Drawing.Point(186, 391);
            this.testSettings.Name = "testSettings";
            this.testSettings.Size = new System.Drawing.Size(75, 23);
            this.testSettings.TabIndex = 205;
            this.testSettings.Text = "Test";
            this.toolTip.SetToolTip(this.testSettings, "Click this button to test the eMail server settings");
            this.testSettings.UseVisualStyleBackColor = true;
            this.testSettings.Click += new System.EventHandler(this.testSettings_Click);
            // 
            // fromAddress
            // 
            this.fromAddress.Location = new System.Drawing.Point(131, 172);
            this.fromAddress.Name = "fromAddress";
            this.fromAddress.Size = new System.Drawing.Size(294, 20);
            this.fromAddress.TabIndex = 40;
            // 
            // toAddress
            // 
            this.toAddress.Location = new System.Drawing.Point(131, 205);
            this.toAddress.Name = "toAddress";
            this.toAddress.Size = new System.Drawing.Size(294, 20);
            this.toAddress.TabIndex = 50;
            // 
            // portNo
            // 
            this.portNo.Location = new System.Drawing.Point(290, 47);
            this.portNo.Name = "portNo";
            this.portNo.Size = new System.Drawing.Size(38, 20);
            this.portNo.TabIndex = 3;
            this.portNo.Text = "25";
            this.portNo.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.portNo_TextChanged);
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.cmdCancel.Location = new System.Drawing.Point(350, 391);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 210;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // oKcmd
            // 
            this.oKcmd.Location = new System.Drawing.Point(26, 391);
            this.oKcmd.Name = "oKcmd";
            this.oKcmd.Size = new System.Drawing.Size(75, 23);
            this.oKcmd.TabIndex = 200;
            this.oKcmd.Text = "OK";
            this.oKcmd.UseVisualStyleBackColor = true;
            this.oKcmd.Click += new System.EventHandler(this.oKcmd_Click);
            // 
            // sendSuccessChk
            // 
            this.sendSuccessChk.AutoSize = true;
            this.sendSuccessChk.Checked = true;
            this.sendSuccessChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.sendSuccessChk.Location = new System.Drawing.Point(131, 285);
            this.sendSuccessChk.Name = "sendSuccessChk";
            this.sendSuccessChk.Size = new System.Drawing.Size(202, 17);
            this.sendSuccessChk.TabIndex = 70;
            this.sendSuccessChk.Text = "Send eMail on successful conversion";
            this.sendSuccessChk.UseVisualStyleBackColor = true;
            // 
            // sendFailedChk
            // 
            this.sendFailedChk.AutoSize = true;
            this.sendFailedChk.Checked = true;
            this.sendFailedChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.sendFailedChk.Location = new System.Drawing.Point(131, 308);
            this.sendFailedChk.Name = "sendFailedChk";
            this.sendFailedChk.Size = new System.Drawing.Size(177, 17);
            this.sendFailedChk.TabIndex = 80;
            this.sendFailedChk.Text = "Send eMail on failed conversion";
            this.sendFailedChk.UseVisualStyleBackColor = true;
            // 
            // sendCancelledChk
            // 
            this.sendCancelledChk.AutoSize = true;
            this.sendCancelledChk.Checked = true;
            this.sendCancelledChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.sendCancelledChk.Location = new System.Drawing.Point(131, 331);
            this.sendCancelledChk.Name = "sendCancelledChk";
            this.sendCancelledChk.Size = new System.Drawing.Size(198, 17);
            this.sendCancelledChk.TabIndex = 90;
            this.sendCancelledChk.Text = "Send eMail on cancelled conversion";
            this.sendCancelledChk.UseVisualStyleBackColor = true;
            // 
            // userName
            // 
            this.userName.Location = new System.Drawing.Point(131, 73);
            this.userName.Name = "userName";
            this.userName.Size = new System.Drawing.Size(197, 20);
            this.userName.TabIndex = 10;
            // 
            // passWord
            // 
            this.passWord.Location = new System.Drawing.Point(131, 107);
            this.passWord.Name = "passWord";
            this.passWord.PasswordChar = '*';
            this.passWord.Size = new System.Drawing.Size(197, 20);
            this.passWord.TabIndex = 20;
            // 
            // sendStartChk
            // 
            this.sendStartChk.AutoSize = true;
            this.sendStartChk.Checked = true;
            this.sendStartChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.sendStartChk.Location = new System.Drawing.Point(131, 262);
            this.sendStartChk.Name = "sendStartChk";
            this.sendStartChk.Size = new System.Drawing.Size(184, 17);
            this.sendStartChk.TabIndex = 60;
            this.sendStartChk.Text = "Send eMail on start of conversion";
            this.sendStartChk.UseVisualStyleBackColor = true;
            // 
            // confirmPasswordLbl
            // 
            this.confirmPasswordLbl.AutoSize = true;
            this.confirmPasswordLbl.Location = new System.Drawing.Point(23, 142);
            this.confirmPasswordLbl.Name = "confirmPasswordLbl";
            this.confirmPasswordLbl.Size = new System.Drawing.Size(91, 13);
            this.confirmPasswordLbl.TabIndex = 0;
            this.confirmPasswordLbl.Text = "Confirm Password";
            // 
            // confirmPassword
            // 
            this.confirmPassword.Location = new System.Drawing.Point(131, 139);
            this.confirmPassword.Name = "confirmPassword";
            this.confirmPassword.PasswordChar = '*';
            this.confirmPassword.Size = new System.Drawing.Size(197, 20);
            this.confirmPassword.TabIndex = 30;
            // 
            // sendDownloadErrorChk
            // 
            this.sendDownloadErrorChk.AutoSize = true;
            this.sendDownloadErrorChk.Checked = true;
            this.sendDownloadErrorChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.sendDownloadErrorChk.Location = new System.Drawing.Point(131, 354);
            this.sendDownloadErrorChk.Name = "sendDownloadErrorChk";
            this.sendDownloadErrorChk.Size = new System.Drawing.Size(267, 17);
            this.sendDownloadErrorChk.TabIndex = 100;
            this.sendDownloadErrorChk.Text = "Send eMail if unable to download series information";
            this.sendDownloadErrorChk.UseVisualStyleBackColor = true;
            // 
            // sendQueueChk
            // 
            this.sendQueueChk.AutoSize = true;
            this.sendQueueChk.Checked = true;
            this.sendQueueChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.sendQueueChk.Location = new System.Drawing.Point(131, 239);
            this.sendQueueChk.Name = "sendQueueChk";
            this.sendQueueChk.Size = new System.Drawing.Size(256, 17);
            this.sendQueueChk.TabIndex = 55;
            this.sendQueueChk.Text = "Send eMail on adding a conversion to the queue";
            this.sendQueueChk.UseVisualStyleBackColor = true;
            // 
            // eMailSettingsForm
            // 
            this.AcceptButton = this.oKcmd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(448, 441);
            this.Controls.Add(this.sendQueueChk);
            this.Controls.Add(this.sendStartChk);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.testSettings);
            this.Controls.Add(this.oKcmd);
            this.Controls.Add(this.sendDownloadErrorChk);
            this.Controls.Add(this.sendCancelledChk);
            this.Controls.Add(this.sendFailedChk);
            this.Controls.Add(this.sendSuccessChk);
            this.Controls.Add(this.sslChk);
            this.Controls.Add(this.portNo);
            this.Controls.Add(this.portLbl);
            this.Controls.Add(this.toAddress);
            this.Controls.Add(this.toLbl);
            this.Controls.Add(this.confirmPassword);
            this.Controls.Add(this.confirmPasswordLbl);
            this.Controls.Add(this.passWord);
            this.Controls.Add(this.passwordLbl);
            this.Controls.Add(this.userName);
            this.Controls.Add(this.usernameLbl);
            this.Controls.Add(this.fromAddress);
            this.Controls.Add(this.fromLbl);
            this.Controls.Add(this.smtpServer);
            this.Controls.Add(this.smtpAddressLbl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "eMailSettingsForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 25, 20);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "e-Mail Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.eMailSettingsForm_FormClosing);
            this.Load += new System.EventHandler(this.eMailSettingsForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label smtpAddressLbl;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.TextBox smtpServer;
        private System.Windows.Forms.Label fromLbl;
        private System.Windows.Forms.TextBox fromAddress;
        private System.Windows.Forms.Label toLbl;
        private System.Windows.Forms.TextBox toAddress;
        private System.Windows.Forms.Label portLbl;
        private System.Windows.Forms.TextBox portNo;
        private System.Windows.Forms.CheckBox sslChk;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Button oKcmd;
        private System.Windows.Forms.CheckBox sendSuccessChk;
        private System.Windows.Forms.CheckBox sendFailedChk;
        private System.Windows.Forms.CheckBox sendCancelledChk;
        private System.Windows.Forms.Label usernameLbl;
        private System.Windows.Forms.TextBox userName;
        private System.Windows.Forms.Label passwordLbl;
        private System.Windows.Forms.TextBox passWord;
        private System.Windows.Forms.Button testSettings;
        private System.Windows.Forms.CheckBox sendStartChk;
        private System.Windows.Forms.Label confirmPasswordLbl;
        private System.Windows.Forms.TextBox confirmPassword;
        private System.Windows.Forms.CheckBox sendDownloadErrorChk;
        private System.Windows.Forms.CheckBox sendQueueChk;
    }
}