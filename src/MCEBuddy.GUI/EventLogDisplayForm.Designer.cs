namespace MCEBuddy.GUI
{
    partial class EventLogDisplayForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EventLogDisplayForm));
            this.eventList = new System.Windows.Forms.ListView();
            this.type = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.dateTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.summary = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.eventMsg = new System.Windows.Forms.TextBox();
            this.cmdClose = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // eventList
            // 
            this.eventList.AllowColumnReorder = true;
            this.eventList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.eventList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.type,
            this.dateTime,
            this.summary});
            this.eventList.FullRowSelect = true;
            this.eventList.Location = new System.Drawing.Point(3, 3);
            this.eventList.MultiSelect = false;
            this.eventList.Name = "eventList";
            this.eventList.Size = new System.Drawing.Size(516, 187);
            this.eventList.TabIndex = 0;
            this.eventList.UseCompatibleStateImageBehavior = false;
            this.eventList.View = System.Windows.Forms.View.Details;
            this.eventList.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.eventList_OnColumnClick);
            this.eventList.SelectedIndexChanged += new System.EventHandler(this.eventList_SelectionChanged);
            // 
            // type
            // 
            this.type.Name = "type";
            this.type.Text = "Type";
            // 
            // dateTime
            // 
            this.dateTime.Name = "dateTime";
            this.dateTime.Text = "Date/Time";
            this.dateTime.Width = 128;
            // 
            // summary
            // 
            this.summary.Name = "summary";
            this.summary.Text = "Summary";
            this.summary.Width = 300;
            // 
            // eventMsg
            // 
            this.eventMsg.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.eventMsg.BackColor = System.Drawing.SystemColors.Info;
            this.eventMsg.ForeColor = System.Drawing.SystemColors.WindowText;
            this.eventMsg.Location = new System.Drawing.Point(3, 3);
            this.eventMsg.Multiline = true;
            this.eventMsg.Name = "eventMsg";
            this.eventMsg.ReadOnly = true;
            this.eventMsg.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.eventMsg.Size = new System.Drawing.Size(516, 177);
            this.eventMsg.TabIndex = 1;
            // 
            // cmdClose
            // 
            this.cmdClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.cmdClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdClose.Location = new System.Drawing.Point(235, 404);
            this.cmdClose.Name = "cmdClose";
            this.cmdClose.Size = new System.Drawing.Size(75, 23);
            this.cmdClose.TabIndex = 2;
            this.cmdClose.Text = "Close";
            this.cmdClose.UseVisualStyleBackColor = true;
            this.cmdClose.Click += new System.EventHandler(this.cmdClose_Click);
            // 
            // splitContainer
            // 
            this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer.Location = new System.Drawing.Point(12, 12);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.eventList);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.eventMsg);
            this.splitContainer.Size = new System.Drawing.Size(522, 386);
            this.splitContainer.SplitterDistance = 193;
            this.splitContainer.SplitterWidth = 6;
            this.splitContainer.TabIndex = 3;
            // 
            // EventLogDisplayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMargin = new System.Drawing.Size(10, 10);
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.cmdClose;
            this.ClientSize = new System.Drawing.Size(545, 439);
            this.Controls.Add(this.cmdClose);
            this.Controls.Add(this.splitContainer);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "EventLogDisplayForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MCEBuddy Windows Event Log Viewer";
            this.Load += new System.EventHandler(this.EventLogDisplay_Load);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView eventList;
        private System.Windows.Forms.TextBox eventMsg;
        private System.Windows.Forms.Button cmdClose;
        private System.Windows.Forms.ColumnHeader dateTime;
        private System.Windows.Forms.ColumnHeader summary;
        private System.Windows.Forms.ColumnHeader type;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.SplitContainer splitContainer;

    }
}