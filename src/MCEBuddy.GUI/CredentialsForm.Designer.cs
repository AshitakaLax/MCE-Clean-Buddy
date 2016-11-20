namespace MCEBuddy.GUI
{
    partial class CredentialsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CredentialsForm));
            this.okCmd = new System.Windows.Forms.Button();
            this.cancelCmd = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.userNameTxt = new System.Windows.Forms.TextBox();
            this.domainNameTxt = new System.Windows.Forms.TextBox();
            this.passwordTxt = new System.Windows.Forms.TextBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.fallbackToSourceChk = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.confirmPasswordTxt = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // okCmd
            // 
            this.okCmd.Location = new System.Drawing.Point(28, 174);
            this.okCmd.Name = "okCmd";
            this.okCmd.Size = new System.Drawing.Size(75, 23);
            this.okCmd.TabIndex = 100;
            this.okCmd.Text = "OK";
            this.okCmd.UseVisualStyleBackColor = true;
            this.okCmd.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // cancelCmd
            // 
            this.cancelCmd.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.cancelCmd.Location = new System.Drawing.Point(255, 174);
            this.cancelCmd.Name = "cancelCmd";
            this.cancelCmd.Size = new System.Drawing.Size(75, 23);
            this.cancelCmd.TabIndex = 110;
            this.cancelCmd.Text = "Cancel";
            this.cancelCmd.UseVisualStyleBackColor = true;
            this.cancelCmd.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(25, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Domain name";
            this.toolTip.SetToolTip(this.label1, "<Optional> Enter if remote computer or network drive uses a Domain for authentica" +
        "tion.\r\nDo NOT enter your workgroup here. Leave this blank if you\'re using a work" +
        "group.");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(25, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "User name";
            this.toolTip.SetToolTip(this.label2, "Enter the username to access the remote computer or network drive");
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(25, 92);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Password";
            this.toolTip.SetToolTip(this.label3, "Enter the password to access the remote computer or network drive");
            // 
            // userNameTxt
            // 
            this.userNameTxt.Location = new System.Drawing.Point(173, 58);
            this.userNameTxt.Name = "userNameTxt";
            this.userNameTxt.Size = new System.Drawing.Size(157, 20);
            this.userNameTxt.TabIndex = 20;
            this.userNameTxt.Text = "Guest";
            // 
            // domainNameTxt
            // 
            this.domainNameTxt.Location = new System.Drawing.Point(173, 25);
            this.domainNameTxt.Name = "domainNameTxt";
            this.domainNameTxt.Size = new System.Drawing.Size(157, 20);
            this.domainNameTxt.TabIndex = 10;
            this.domainNameTxt.Leave += new System.EventHandler(this.domainNameTxt_Leave);
            // 
            // passwordTxt
            // 
            this.passwordTxt.Location = new System.Drawing.Point(173, 89);
            this.passwordTxt.Name = "passwordTxt";
            this.passwordTxt.PasswordChar = '*';
            this.passwordTxt.Size = new System.Drawing.Size(157, 20);
            this.passwordTxt.TabIndex = 30;
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 200;
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 40;
            this.toolTip.ReshowDelay = 40;
            // 
            // fallbackToSourceChk
            // 
            this.fallbackToSourceChk.AutoSize = true;
            this.fallbackToSourceChk.Location = new System.Drawing.Point(173, 151);
            this.fallbackToSourceChk.Name = "fallbackToSourceChk";
            this.fallbackToSourceChk.Size = new System.Drawing.Size(137, 17);
            this.fallbackToSourceChk.TabIndex = 90;
            this.fallbackToSourceChk.Text = "Fallback to source path";
            this.toolTip.SetToolTip(this.fallbackToSourceChk, "Enable this if you want MCEBuddy to put the converted file in the source director" +
        "y\r\nif the destination path is unavailable. (e.g. Network failure or mapping fail" +
        "ure)");
            this.fallbackToSourceChk.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(25, 123);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(91, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Confirm Password";
            // 
            // confirmPasswordTxt
            // 
            this.confirmPasswordTxt.Location = new System.Drawing.Point(173, 120);
            this.confirmPasswordTxt.Name = "confirmPasswordTxt";
            this.confirmPasswordTxt.PasswordChar = '*';
            this.confirmPasswordTxt.Size = new System.Drawing.Size(157, 20);
            this.confirmPasswordTxt.TabIndex = 40;
            // 
            // CredentialsForm
            // 
            this.AcceptButton = this.okCmd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cancelCmd;
            this.ClientSize = new System.Drawing.Size(361, 220);
            this.Controls.Add(this.fallbackToSourceChk);
            this.Controls.Add(this.confirmPasswordTxt);
            this.Controls.Add(this.passwordTxt);
            this.Controls.Add(this.domainNameTxt);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.userNameTxt);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cancelCmd);
            this.Controls.Add(this.okCmd);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "CredentialsForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 25, 20);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Network Connection User Credentials";
            this.Load += new System.EventHandler(this.ConnectionCredentials_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okCmd;
        private System.Windows.Forms.Button cancelCmd;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox userNameTxt;
        private System.Windows.Forms.TextBox domainNameTxt;
        private System.Windows.Forms.TextBox passwordTxt;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.CheckBox fallbackToSourceChk;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox confirmPasswordTxt;
    }
}