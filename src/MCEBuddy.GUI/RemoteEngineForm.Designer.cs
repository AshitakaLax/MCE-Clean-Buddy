namespace MCEBuddy.GUI
{
    partial class RemoteEngineForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RemoteEngineForm));
            this.okCmd = new System.Windows.Forms.Button();
            this.cancelCmd = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.portNoTxt = new System.Windows.Forms.TextBox();
            this.engineNameTxt = new System.Windows.Forms.TextBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.defaultCmd = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // okCmd
            // 
            this.okCmd.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okCmd.Location = new System.Drawing.Point(28, 99);
            this.okCmd.Name = "okCmd";
            this.okCmd.Size = new System.Drawing.Size(75, 23);
            this.okCmd.TabIndex = 3;
            this.okCmd.Text = "OK";
            this.okCmd.UseVisualStyleBackColor = true;
            this.okCmd.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // cancelCmd
            // 
            this.cancelCmd.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelCmd.Location = new System.Drawing.Point(255, 99);
            this.cancelCmd.Name = "cancelCmd";
            this.cancelCmd.Size = new System.Drawing.Size(75, 23);
            this.cancelCmd.TabIndex = 4;
            this.cancelCmd.Text = "Cancel";
            this.cancelCmd.UseVisualStyleBackColor = true;
            this.cancelCmd.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(25, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Engine Name";
            this.toolTip.SetToolTip(this.label1, "Enter the Name or IP Address of the MCEBuddy engine you want to connect to");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(25, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Engine Port";
            this.toolTip.SetToolTip(this.label2, "Enter the TCP Port number of the MCEBuddy Engine you want to connect to (1 to 633" +
        "55)");
            // 
            // portNoTxt
            // 
            this.portNoTxt.Location = new System.Drawing.Point(173, 58);
            this.portNoTxt.MaxLength = 5;
            this.portNoTxt.Name = "portNoTxt";
            this.portNoTxt.Size = new System.Drawing.Size(157, 20);
            this.portNoTxt.TabIndex = 1;
            this.portNoTxt.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.portNoTxt_KeyPress);
            // 
            // engineNameTxt
            // 
            this.engineNameTxt.Location = new System.Drawing.Point(173, 25);
            this.engineNameTxt.Name = "engineNameTxt";
            this.engineNameTxt.Size = new System.Drawing.Size(157, 20);
            this.engineNameTxt.TabIndex = 0;
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 200;
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 40;
            this.toolTip.ReshowDelay = 40;
            // 
            // defaultCmd
            // 
            this.defaultCmd.Location = new System.Drawing.Point(143, 99);
            this.defaultCmd.Name = "defaultCmd";
            this.defaultCmd.Size = new System.Drawing.Size(75, 23);
            this.defaultCmd.TabIndex = 3;
            this.defaultCmd.Text = "Default";
            this.toolTip.SetToolTip(this.defaultCmd, "Click here to reset to the Default MCEBuddy Engine Name and Port Number");
            this.defaultCmd.UseVisualStyleBackColor = true;
            this.defaultCmd.Click += new System.EventHandler(this.DefaultButton_Click);
            // 
            // RemoteEngineForm
            // 
            this.AcceptButton = this.okCmd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cancelCmd;
            this.ClientSize = new System.Drawing.Size(361, 145);
            this.Controls.Add(this.engineNameTxt);
            this.Controls.Add(this.portNoTxt);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cancelCmd);
            this.Controls.Add(this.defaultCmd);
            this.Controls.Add(this.okCmd);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "RemoteEngineForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 25, 20);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MCEBuddy Remote Engine Connection";
            this.Load += new System.EventHandler(this.RemoteEngine_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okCmd;
        private System.Windows.Forms.Button cancelCmd;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox portNoTxt;
        private System.Windows.Forms.TextBox engineNameTxt;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Button defaultCmd;
    }
}