namespace MCEBuddy.GUI
{
    partial class MediaInfoForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MediaInfoForm));
            this.mediaSpace = new System.Windows.Forms.RichTextBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // mediaSpace
            // 
            this.mediaSpace.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.mediaSpace.Location = new System.Drawing.Point(12, 12);
            this.mediaSpace.Margin = new System.Windows.Forms.Padding(3, 3, 10, 10);
            this.mediaSpace.Name = "mediaSpace";
            this.mediaSpace.ReadOnly = true;
            this.mediaSpace.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.mediaSpace.Size = new System.Drawing.Size(239, 68);
            this.mediaSpace.TabIndex = 0;
            this.mediaSpace.Text = "";
            // 
            // MediaInfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(270, 100);
            this.Controls.Add(this.mediaSpace);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MediaInfoForm";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.ShowInTaskbar = false;
            this.Text = "Video / Audio Information";
            this.Load += new System.EventHandler(this.MediaInfoForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.RichTextBox mediaSpace;
        private System.Windows.Forms.ToolTip toolTip;
    }
}