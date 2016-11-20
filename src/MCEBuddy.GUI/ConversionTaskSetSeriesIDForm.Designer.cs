namespace MCEBuddy.GUI
{
    partial class ConversionTaskSetSeriesIDForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConversionTaskSetSeriesIDForm));
            this.okCmd = new System.Windows.Forms.Button();
            this.cancelCmd = new System.Windows.Forms.Button();
            this.tvdbLbl0 = new System.Windows.Forms.Label();
            this.imdbLbl0 = new System.Windows.Forms.Label();
            this.imdbSeriesId0 = new System.Windows.Forms.TextBox();
            this.tvdbSeriesId0 = new System.Windows.Forms.TextBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.addRowBtn = new System.Windows.Forms.Button();
            this.originalTitleLbl0 = new System.Windows.Forms.Label();
            this.correctedTitleLbl0 = new System.Windows.Forms.Label();
            this.airDateMatchChk = new System.Windows.Forms.CheckBox();
            this.originalTitleTxt0 = new System.Windows.Forms.TextBox();
            this.correctedTitleTxt0 = new System.Windows.Forms.TextBox();
            this.or0 = new System.Windows.Forms.Label();
            this.arrow0 = new System.Windows.Forms.Label();
            this.deleteRow0 = new System.Windows.Forms.Button();
            this.downloadBannerChk = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // okCmd
            // 
            this.okCmd.Location = new System.Drawing.Point(28, 120);
            this.okCmd.Name = "okCmd";
            this.okCmd.Size = new System.Drawing.Size(75, 23);
            this.okCmd.TabIndex = 1000;
            this.okCmd.Text = "OK";
            this.okCmd.UseVisualStyleBackColor = true;
            this.okCmd.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // cancelCmd
            // 
            this.cancelCmd.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.cancelCmd.Location = new System.Drawing.Point(959, 120);
            this.cancelCmd.Name = "cancelCmd";
            this.cancelCmd.Size = new System.Drawing.Size(75, 23);
            this.cancelCmd.TabIndex = 1010;
            this.cancelCmd.Text = "Cancel";
            this.cancelCmd.UseVisualStyleBackColor = true;
            this.cancelCmd.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // tvdbLbl0
            // 
            this.tvdbLbl0.AutoSize = true;
            this.tvdbLbl0.Location = new System.Drawing.Point(718, 74);
            this.tvdbLbl0.Name = "tvdbLbl0";
            this.tvdbLbl0.Size = new System.Drawing.Size(82, 13);
            this.tvdbLbl0.TabIndex = 16;
            this.tvdbLbl0.Text = "TVDB Series ID";
            this.toolTip.SetToolTip(this.tvdbLbl0, "Enter the TVDB Series ID you want to use for downloading show information.\r\nIf th" +
        "e Original title box is empty, this will be applied to all shows selected by thi" +
        "s conversion task.");
            // 
            // imdbLbl0
            // 
            this.imdbLbl0.AutoSize = true;
            this.imdbLbl0.Location = new System.Drawing.Point(879, 74);
            this.imdbLbl0.Name = "imdbLbl0";
            this.imdbLbl0.Size = new System.Drawing.Size(80, 13);
            this.imdbLbl0.TabIndex = 18;
            this.imdbLbl0.Text = "IMDB Series ID";
            this.toolTip.SetToolTip(this.imdbLbl0, "Enter the IMDB Movie ID you want to use for downloading movie information.\r\nIf th" +
        "e Original title box is empty, this will be applied to all shows selected by thi" +
        "s conversion task.");
            // 
            // imdbSeriesId0
            // 
            this.imdbSeriesId0.Location = new System.Drawing.Point(965, 70);
            this.imdbSeriesId0.Name = "imdbSeriesId0";
            this.imdbSeriesId0.Size = new System.Drawing.Size(69, 20);
            this.imdbSeriesId0.TabIndex = 19;
            // 
            // tvdbSeriesId0
            // 
            this.tvdbSeriesId0.Location = new System.Drawing.Point(804, 70);
            this.tvdbSeriesId0.Name = "tvdbSeriesId0";
            this.tvdbSeriesId0.Size = new System.Drawing.Size(69, 20);
            this.tvdbSeriesId0.TabIndex = 17;
            this.tvdbSeriesId0.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tvdbSeriesId_KeyPress);
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 200;
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 40;
            this.toolTip.ReshowDelay = 40;
            // 
            // addRowBtn
            // 
            this.addRowBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.addRowBtn.Image = global::MCEBuddy.GUI.Properties.Resources.add;
            this.addRowBtn.Location = new System.Drawing.Point(28, 25);
            this.addRowBtn.Name = "addRowBtn";
            this.addRowBtn.Size = new System.Drawing.Size(53, 28);
            this.addRowBtn.TabIndex = 1;
            this.toolTip.SetToolTip(this.addRowBtn, resources.GetString("addRowBtn.ToolTip"));
            this.addRowBtn.UseVisualStyleBackColor = true;
            this.addRowBtn.Click += new System.EventHandler(this.addRowBtn_Click);
            // 
            // originalTitleLbl0
            // 
            this.originalTitleLbl0.AutoSize = true;
            this.originalTitleLbl0.Location = new System.Drawing.Point(56, 74);
            this.originalTitleLbl0.Name = "originalTitleLbl0";
            this.originalTitleLbl0.Size = new System.Drawing.Size(61, 13);
            this.originalTitleLbl0.TabIndex = 10;
            this.originalTitleLbl0.Text = "Original title";
            this.toolTip.SetToolTip(this.originalTitleLbl0, resources.GetString("originalTitleLbl0.ToolTip"));
            // 
            // correctedTitleLbl0
            // 
            this.correctedTitleLbl0.AutoSize = true;
            this.correctedTitleLbl0.Location = new System.Drawing.Point(401, 74);
            this.correctedTitleLbl0.Name = "correctedTitleLbl0";
            this.correctedTitleLbl0.Size = new System.Drawing.Size(72, 13);
            this.correctedTitleLbl0.TabIndex = 13;
            this.correctedTitleLbl0.Text = "Corrected title";
            this.toolTip.SetToolTip(this.correctedTitleLbl0, resources.GetString("correctedTitleLbl0.ToolTip"));
            // 
            // airDateMatchChk
            // 
            this.airDateMatchChk.AutoSize = true;
            this.airDateMatchChk.Location = new System.Drawing.Point(123, 32);
            this.airDateMatchChk.Name = "airDateMatchChk";
            this.airDateMatchChk.Size = new System.Drawing.Size(149, 17);
            this.airDateMatchChk.TabIndex = 1011;
            this.airDateMatchChk.Text = "Prioritize air date matching";
            this.toolTip.SetToolTip(this.airDateMatchChk, resources.GetString("airDateMatchChk.ToolTip"));
            this.airDateMatchChk.UseVisualStyleBackColor = true;
            // 
            // originalTitleTxt0
            // 
            this.originalTitleTxt0.Location = new System.Drawing.Point(123, 70);
            this.originalTitleTxt0.Name = "originalTitleTxt0";
            this.originalTitleTxt0.Size = new System.Drawing.Size(196, 20);
            this.originalTitleTxt0.TabIndex = 11;
            // 
            // correctedTitleTxt0
            // 
            this.correctedTitleTxt0.Location = new System.Drawing.Point(478, 70);
            this.correctedTitleTxt0.Name = "correctedTitleTxt0";
            this.correctedTitleTxt0.Size = new System.Drawing.Size(196, 20);
            this.correctedTitleTxt0.TabIndex = 14;
            // 
            // or0
            // 
            this.or0.AutoSize = true;
            this.or0.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.or0.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.or0.Location = new System.Drawing.Point(680, 71);
            this.or0.Name = "or0";
            this.or0.Size = new System.Drawing.Size(33, 18);
            this.or0.TabIndex = 15;
            this.or0.Text = "OR";
            // 
            // arrow0
            // 
            this.arrow0.AutoSize = true;
            this.arrow0.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.arrow0.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.arrow0.Location = new System.Drawing.Point(325, 68);
            this.arrow0.Name = "arrow0";
            this.arrow0.Size = new System.Drawing.Size(71, 24);
            this.arrow0.TabIndex = 12;
            this.arrow0.Text = "------->";
            // 
            // deleteRow0
            // 
            this.deleteRow0.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.deleteRow0.Image = global::MCEBuddy.GUI.Properties.Resources.delete;
            this.deleteRow0.Location = new System.Drawing.Point(29, 68);
            this.deleteRow0.Name = "deleteRow0";
            this.deleteRow0.Size = new System.Drawing.Size(23, 23);
            this.deleteRow0.TabIndex = 9;
            this.deleteRow0.UseVisualStyleBackColor = true;
            this.deleteRow0.Visible = false;
            this.deleteRow0.Click += new System.EventHandler(this.deleteRow_Click);
            // 
            // downloadBannerChk
            // 
            this.downloadBannerChk.AutoSize = true;
            this.downloadBannerChk.Checked = true;
            this.downloadBannerChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.downloadBannerChk.Location = new System.Drawing.Point(278, 32);
            this.downloadBannerChk.Name = "downloadBannerChk";
            this.downloadBannerChk.Size = new System.Drawing.Size(119, 17);
            this.downloadBannerChk.TabIndex = 1012;
            this.downloadBannerChk.Text = "Download cover art";
            this.toolTip.SetToolTip(this.downloadBannerChk, "Enable this to download the cover art from the original video and internet if ava" +
        "ilable.");
            this.downloadBannerChk.UseVisualStyleBackColor = true;
            // 
            // ConversionTaskSetSeriesIDForm
            // 
            this.AcceptButton = this.okCmd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cancelCmd;
            this.ClientSize = new System.Drawing.Size(1069, 163);
            this.Controls.Add(this.downloadBannerChk);
            this.Controls.Add(this.airDateMatchChk);
            this.Controls.Add(this.deleteRow0);
            this.Controls.Add(this.addRowBtn);
            this.Controls.Add(this.arrow0);
            this.Controls.Add(this.or0);
            this.Controls.Add(this.correctedTitleLbl0);
            this.Controls.Add(this.originalTitleLbl0);
            this.Controls.Add(this.correctedTitleTxt0);
            this.Controls.Add(this.originalTitleTxt0);
            this.Controls.Add(this.tvdbSeriesId0);
            this.Controls.Add(this.imdbSeriesId0);
            this.Controls.Add(this.imdbLbl0);
            this.Controls.Add(this.tvdbLbl0);
            this.Controls.Add(this.cancelCmd);
            this.Controls.Add(this.okCmd);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "ConversionTaskSetSeriesIDForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 25, 20);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Internet Lookup Title and Metadata Correction";
            this.Load += new System.EventHandler(this.SetSeriesId_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okCmd;
        private System.Windows.Forms.Button cancelCmd;
        private System.Windows.Forms.Label tvdbLbl0;
        private System.Windows.Forms.Label imdbLbl0;
        private System.Windows.Forms.TextBox imdbSeriesId0;
        private System.Windows.Forms.TextBox tvdbSeriesId0;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.TextBox originalTitleTxt0;
        private System.Windows.Forms.TextBox correctedTitleTxt0;
        private System.Windows.Forms.Label originalTitleLbl0;
        private System.Windows.Forms.Label correctedTitleLbl0;
        private System.Windows.Forms.Label or0;
        private System.Windows.Forms.Label arrow0;
        private System.Windows.Forms.Button addRowBtn;
        private System.Windows.Forms.Button deleteRow0;
        private System.Windows.Forms.CheckBox airDateMatchChk;
        private System.Windows.Forms.CheckBox downloadBannerChk;
    }
}