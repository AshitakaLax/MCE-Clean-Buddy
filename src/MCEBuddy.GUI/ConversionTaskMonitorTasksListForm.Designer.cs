namespace MCEBuddy.GUI
{
    partial class ConversionTaskMonitorTasksListForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConversionTaskMonitorTasksListForm));
            this.okCmd = new System.Windows.Forms.Button();
            this.cancelCmd = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // okCmd
            // 
            this.okCmd.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okCmd.Location = new System.Drawing.Point(26, 28);
            this.okCmd.Name = "okCmd";
            this.okCmd.Size = new System.Drawing.Size(75, 23);
            this.okCmd.TabIndex = 100;
            this.okCmd.Text = "OK";
            this.okCmd.UseVisualStyleBackColor = true;
            this.okCmd.Click += new System.EventHandler(this.okCmd_Click);
            // 
            // cancelCmd
            // 
            this.cancelCmd.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelCmd.Location = new System.Drawing.Point(162, 28);
            this.cancelCmd.Name = "cancelCmd";
            this.cancelCmd.Size = new System.Drawing.Size(75, 23);
            this.cancelCmd.TabIndex = 110;
            this.cancelCmd.Text = "Cancel";
            this.cancelCmd.UseVisualStyleBackColor = true;
            // 
            // ConversionTaskMonitorTasksListForm
            // 
            this.AcceptButton = this.okCmd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cancelCmd;
            this.ClientSize = new System.Drawing.Size(265, 86);
            this.Controls.Add(this.cancelCmd);
            this.Controls.Add(this.okCmd);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ConversionTaskMonitorTasksListForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 25, 20);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Monitor Locations";
            this.Load += new System.EventHandler(this.MonitorTasksListForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button okCmd;
        private System.Windows.Forms.Button cancelCmd;
        private System.Windows.Forms.ToolTip toolTip;
    }
}