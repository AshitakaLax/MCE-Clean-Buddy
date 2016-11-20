namespace MCEBuddy.GUI
{
    partial class HistoryForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HistoryForm));
            this.showHistoryList = new System.Windows.Forms.ListView();
            this.filePath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.convertedOn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.conversionStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.timeTaken = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.task = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.profile = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.convertedTo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.errorMessage = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearHistoryBtn = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.reconvertFileBtn = new System.Windows.Forms.Button();
            this.cancelBtn = new System.Windows.Forms.Button();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // showHistoryList
            // 
            this.showHistoryList.AllowColumnReorder = true;
            this.showHistoryList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.showHistoryList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.filePath,
            this.convertedOn,
            this.conversionStatus,
            this.timeTaken,
            this.task,
            this.profile,
            this.convertedTo,
            this.errorMessage});
            this.showHistoryList.ContextMenuStrip = this.contextMenuStrip;
            this.showHistoryList.FullRowSelect = true;
            this.showHistoryList.GridLines = true;
            this.showHistoryList.Location = new System.Drawing.Point(12, 12);
            this.showHistoryList.Name = "showHistoryList";
            this.showHistoryList.ShowItemToolTips = true;
            this.showHistoryList.Size = new System.Drawing.Size(707, 388);
            this.showHistoryList.TabIndex = 0;
            this.showHistoryList.UseCompatibleStateImageBehavior = false;
            this.showHistoryList.View = System.Windows.Forms.View.Details;
            this.showHistoryList.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.showHistoryList_OnColumnClick);
            this.showHistoryList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.showHistoryList_KeyDown);
            // 
            // filePath
            // 
            this.filePath.Name = "filePath";
            this.filePath.Text = "Original File";
            this.filePath.Width = 364;
            // 
            // convertedOn
            // 
            this.convertedOn.Name = "convertedOn";
            this.convertedOn.Text = "Converted On";
            this.convertedOn.Width = 118;
            // 
            // conversionStatus
            // 
            this.conversionStatus.Name = "conversionStatus";
            this.conversionStatus.Text = "Conversion Status";
            this.conversionStatus.Width = 99;
            // 
            // timeTaken
            // 
            this.timeTaken.Name = "timeTaken";
            this.timeTaken.Text = "Time Taken (hh:mm)";
            this.timeTaken.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.timeTaken.Width = 110;
            // 
            // task
            // 
            this.task.Name = "task";
            this.task.Text = "Task Name";
            this.task.Width = 147;
            // 
            // profile
            // 
            this.profile.Name = "profile";
            this.profile.Text = "Profile Name";
            this.profile.Width = 113;
            // 
            // convertedTo
            // 
            this.convertedTo.Name = "convertedTo";
            this.convertedTo.Text = "Converted File";
            this.convertedTo.Width = 364;
            // 
            // errorMessage
            // 
            this.errorMessage.Name = "errorMessage";
            this.errorMessage.Text = "Error Message";
            this.errorMessage.Width = 200;
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToClipboardToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(170, 48);
            // 
            // copyToClipboardToolStripMenuItem
            // 
            this.copyToClipboardToolStripMenuItem.Name = "copyToClipboardToolStripMenuItem";
            this.copyToClipboardToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.copyToClipboardToolStripMenuItem.Text = "Copy to clipboard";
            this.copyToClipboardToolStripMenuItem.Click += new System.EventHandler(this.copyToClipboardToolStripMenuItem_Click);
            // 
            // clearHistoryBtn
            // 
            this.clearHistoryBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.clearHistoryBtn.Location = new System.Drawing.Point(337, 423);
            this.clearHistoryBtn.Name = "clearHistoryBtn";
            this.clearHistoryBtn.Size = new System.Drawing.Size(75, 23);
            this.clearHistoryBtn.TabIndex = 3;
            this.clearHistoryBtn.Text = "Clear history";
            this.toolTip.SetToolTip(this.clearHistoryBtn, "Click this button to clear the history of all converted files.\r\nThis is cause MCE" +
        "Buddy to reconvert all files being monitored.");
            this.clearHistoryBtn.UseVisualStyleBackColor = true;
            this.clearHistoryBtn.Click += new System.EventHandler(this.clearHistoryBtn_Click);
            // 
            // reconvertFileBtn
            // 
            this.reconvertFileBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.reconvertFileBtn.Location = new System.Drawing.Point(12, 423);
            this.reconvertFileBtn.Name = "reconvertFileBtn";
            this.reconvertFileBtn.Size = new System.Drawing.Size(94, 23);
            this.reconvertFileBtn.TabIndex = 4;
            this.reconvertFileBtn.Text = "Reconvert files";
            this.toolTip.SetToolTip(this.reconvertFileBtn, "Select the files you want to reconvert and then click this button.\r\nIf the origin" +
        "al files are still being monitored by MCEBuddy they will be queued for reconvers" +
        "ion.");
            this.reconvertFileBtn.UseVisualStyleBackColor = true;
            this.reconvertFileBtn.Click += new System.EventHandler(this.reconvertFileBtn_Click);
            // 
            // cancelBtn
            // 
            this.cancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelBtn.Location = new System.Drawing.Point(643, 423);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(75, 23);
            this.cancelBtn.TabIndex = 2;
            this.cancelBtn.Text = "Close";
            this.cancelBtn.UseVisualStyleBackColor = true;
            // 
            // HistoryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.CancelButton = this.cancelBtn;
            this.ClientSize = new System.Drawing.Size(737, 468);
            this.Controls.Add(this.cancelBtn);
            this.Controls.Add(this.reconvertFileBtn);
            this.Controls.Add(this.clearHistoryBtn);
            this.Controls.Add(this.showHistoryList);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "HistoryForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "History of Converted Files";
            this.Load += new System.EventHandler(this.ShowHistory_Load);
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView showHistoryList;
        private System.Windows.Forms.ColumnHeader filePath;
        private System.Windows.Forms.ColumnHeader convertedOn;
        private System.Windows.Forms.ColumnHeader conversionStatus;
        private System.Windows.Forms.Button clearHistoryBtn;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ColumnHeader timeTaken;
        private System.Windows.Forms.ColumnHeader task;
        private System.Windows.Forms.ColumnHeader profile;
        private System.Windows.Forms.ColumnHeader errorMessage;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem copyToClipboardToolStripMenuItem;
        private System.Windows.Forms.Button cancelBtn;
        private System.Windows.Forms.Button reconvertFileBtn;
        private System.Windows.Forms.ColumnHeader convertedTo;
    }
}