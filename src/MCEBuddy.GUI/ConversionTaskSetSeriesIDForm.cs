using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using MCEBuddy.Util;
using MCEBuddy.Configuration;
using MCEBuddy.Globals;

namespace MCEBuddy.GUI
{
    public partial class ConversionTaskSetSeriesIDForm : Form
    {
        private ConversionJobOptions _cjo;
        private int rowCount = 1; // keep count of number of row added or deleted
        private const int rowSpacing = 25; // Each row is x pixels apart
        private const int itemsPerRow = 11; // How many control per row to calculate tabindex

        public ConversionTaskSetSeriesIDForm(ConversionJobOptions conversionJob)
        {
            InitializeComponent();

            _cjo = conversionJob;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(originalTitleTxt0.Text) && String.IsNullOrWhiteSpace(correctedTitleTxt0.Text) && String.IsNullOrWhiteSpace(tvdbSeriesId0.Text) && String.IsNullOrWhiteSpace(imdbSeriesId0.Text))
                _cjo.metadataCorrections = null; // If all 4 of the first row being empty is a valid configuration, so we mark it null
            else
            {
                if (!CheckValidEntries())
                    return;

                // Check the number of rows and create objects to save the information
                _cjo.metadataCorrections = new ConversionJobOptions.MetadataCorrectionOptions[rowCount];

                for (int i = 0; i < rowCount; i++) // now iterate and save it
                {
                    string originalTitle = ((TextBox)this.Controls.Find("originalTitleTxt" + i.ToString(), true)[0]).Text;
                    string correctedTitle = ((TextBox)this.Controls.Find("correctedTitleTxt" + i.ToString(), true)[0]).Text;
                    string tvdbSeriesId = ((TextBox)this.Controls.Find("tvdbSeriesId" + i.ToString(), true)[0]).Text;
                    string imdbSeriesId = ((TextBox)this.Controls.Find("imdbSeriesId" + i.ToString(), true)[0]).Text;

                    _cjo.metadataCorrections[i] = new ConversionJobOptions.MetadataCorrectionOptions();
                    _cjo.metadataCorrections[i].originalTitle = originalTitle;
                    _cjo.metadataCorrections[i].correctedTitle = correctedTitle;
                    _cjo.metadataCorrections[i].tvdbSeriesId = tvdbSeriesId;
                    _cjo.metadataCorrections[i].imdbSeriesId = imdbSeriesId;
                }
            }

            _cjo.prioritizeOriginalBroadcastDateMatch = airDateMatchChk.Checked;
            _cjo.downloadBanner = downloadBannerChk.Checked;

            this.Close();
        }

        private void SetSeriesId_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            // Create and populate the necessary rows
            if (_cjo.metadataCorrections != null)
            {
                for (int i = 0; i < _cjo.metadataCorrections.Length; i++)
                {
                    if (i > 0)
                        AddRow(); // add a row

                    ((TextBox)this.Controls.Find("originalTitleTxt" + i.ToString(), true)[0]).Text = _cjo.metadataCorrections[i].originalTitle;
                    ((TextBox)this.Controls.Find("correctedTitleTxt" + i.ToString(), true)[0]).Text = _cjo.metadataCorrections[i].correctedTitle;
                    ((TextBox)this.Controls.Find("tvdbSeriesId" + i.ToString(), true)[0]).Text = _cjo.metadataCorrections[i].tvdbSeriesId;
                    ((TextBox)this.Controls.Find("imdbSeriesId" + i.ToString(), true)[0]).Text = _cjo.metadataCorrections[i].imdbSeriesId;
                }
            }

            airDateMatchChk.Checked = _cjo.prioritizeOriginalBroadcastDateMatch; // Priortize matching air date
            downloadBannerChk.Checked = _cjo.downloadBanner; // Download banner

            LocaliseForms.LocaliseForm(this, toolTip);
        }

        private void tvdbSeriesId_KeyPress(object sender, KeyPressEventArgs e)
        {
            const char Delete = (char)8;
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != Delete;
        }

        private bool CheckValidEntries()
        {
            for (int i = 0; i < rowCount; i++)
            {
                string originalTitle = ((TextBox)this.Controls.Find("originalTitleTxt" + i.ToString(), true)[0]).Text;
                string correctedTitle = ((TextBox)this.Controls.Find("correctedTitleTxt" + i.ToString(), true)[0]).Text;
                string tvdbSeriesId = ((TextBox)this.Controls.Find("tvdbSeriesId" + i.ToString(), true)[0]).Text;
                string imdbSeriesId = ((TextBox)this.Controls.Find("imdbSeriesId" + i.ToString(), true)[0]).Text;

                if (rowCount > 1) // If we have more than one entry, either enter a Corrected Title or enter a Series Id and MUST enter an original title
                {
                    if (String.IsNullOrWhiteSpace(originalTitle) ||
                        (String.IsNullOrWhiteSpace(correctedTitle) && String.IsNullOrWhiteSpace(tvdbSeriesId) && String.IsNullOrWhiteSpace(imdbSeriesId)) ||
                        (!String.IsNullOrWhiteSpace(correctedTitle) && (!String.IsNullOrWhiteSpace(tvdbSeriesId) || !String.IsNullOrWhiteSpace(imdbSeriesId))))
                    {
                        MessageBox.Show(Localise.GetPhrase("For each row, please enter the Original title to match the show name AND either a Corrected title or a TVDB/IMDB Series ID"), Localise.GetPhrase("Error"));
                        return false;
                    }
                }
                else // We have only one entry, either enter a corrected title or a series id (original title is optional)
                {
                    if ((String.IsNullOrWhiteSpace(correctedTitle) && String.IsNullOrWhiteSpace(tvdbSeriesId) && String.IsNullOrWhiteSpace(imdbSeriesId)) ||
                        (!String.IsNullOrWhiteSpace(correctedTitle) && (!String.IsNullOrWhiteSpace(tvdbSeriesId) || !String.IsNullOrWhiteSpace(imdbSeriesId))))
                    {
                        MessageBox.Show(Localise.GetPhrase("Please enter either a Corrected title or a TVDB/IMDB Series ID"), Localise.GetPhrase("Error"));
                        return false;
                    }
                }
            }

            return true; // All good
        }

        private void addRowBtn_Click(object sender, EventArgs e)
        {
            // Check if we have a valid entries on the rows before adding more
            if (!CheckValidEntries())
                return;

            AddRow();
        }

        private void AddRow()
        {
            // Now add a new row with all the controls
            SuspendLayout();

            //
            // All the new controls to add
            //
            Button deleteRow = new System.Windows.Forms.Button();
            Label tvdbLbl = new System.Windows.Forms.Label();
            Label imdbLbl = new System.Windows.Forms.Label();
            TextBox imdbSeriesId = new System.Windows.Forms.TextBox();
            TextBox tvdbSeriesId = new System.Windows.Forms.TextBox();
            TextBox originalTitleTxt = new System.Windows.Forms.TextBox();
            TextBox correctedTitleTxt = new System.Windows.Forms.TextBox();
            Label originalTitleLbl = new System.Windows.Forms.Label();
            Label correctedTitleLbl = new System.Windows.Forms.Label();
            Label or = new System.Windows.Forms.Label();
            Label arrow = new System.Windows.Forms.Label();

            // 
            // tvdbLblx
            // 
            tvdbLbl.AutoSize = tvdbLbl0.AutoSize;
            tvdbLbl.Location = new System.Drawing.Point(tvdbLbl0.Location.X, tvdbLbl0.Location.Y + (rowSpacing * rowCount));
            tvdbLbl.Name = "tvdbLbl" + (rowCount).ToString();
            tvdbLbl.Size = tvdbLbl0.Size;
            tvdbLbl.TabIndex = tvdbLbl0.TabIndex + (itemsPerRow * rowCount);
            tvdbLbl.Text = tvdbLbl0.Text;
            toolTip.SetToolTip(tvdbLbl, toolTip.GetToolTip(tvdbLbl0));
            // 
            // imdbLbl
            // 
            imdbLbl.AutoSize = imdbLbl0.AutoSize;
            imdbLbl.Location = new System.Drawing.Point(imdbLbl0.Location.X, imdbLbl0.Location.Y + (rowSpacing * rowCount));
            imdbLbl.Name = "imdbLbl" + (rowCount).ToString();
            imdbLbl.Size = imdbLbl0.Size;
            imdbLbl.TabIndex = imdbLbl0.TabIndex + (itemsPerRow * rowCount);
            imdbLbl.Text = imdbLbl0.Text;
            toolTip.SetToolTip(imdbLbl, toolTip.GetToolTip(imdbLbl0));
            // 
            // imdbSeriesId
            // 
            imdbSeriesId.Location = new System.Drawing.Point(imdbSeriesId0.Location.X, imdbSeriesId0.Location.Y + (rowSpacing * rowCount));
            imdbSeriesId.Name = "imdbSeriesId" + (rowCount).ToString();
            imdbSeriesId.Size = imdbSeriesId0.Size;
            imdbSeriesId.TabIndex = imdbSeriesId0.TabIndex + (itemsPerRow * rowCount);
            // 
            // tvdbSeriesId
            // 
            tvdbSeriesId.Location = new System.Drawing.Point(tvdbSeriesId0.Location.X, tvdbSeriesId0.Location.Y + (rowSpacing * rowCount));
            tvdbSeriesId.Name = "tvdbSeriesId" + (rowCount).ToString();
            tvdbSeriesId.Size = tvdbSeriesId0.Size;
            tvdbSeriesId.TabIndex = tvdbSeriesId0.TabIndex + (itemsPerRow * rowCount);
            tvdbSeriesId.KeyPress += new System.Windows.Forms.KeyPressEventHandler(tvdbSeriesId_KeyPress);
            // 
            // originalTitleTxt
            // 
            originalTitleTxt.Location = new System.Drawing.Point(originalTitleTxt0.Location.X, originalTitleTxt0.Location.Y + (rowSpacing * rowCount));
            originalTitleTxt.Name = "originalTitleTxt" + (rowCount).ToString();
            originalTitleTxt.Size = originalTitleTxt0.Size;
            originalTitleTxt.TabIndex = originalTitleTxt0.TabIndex + (itemsPerRow * rowCount);
            // 
            // correctedTitleTxt
            // 
            correctedTitleTxt.Location = new System.Drawing.Point(correctedTitleTxt0.Location.X, correctedTitleTxt0.Location.Y + (rowSpacing * rowCount));
            correctedTitleTxt.Name = "correctedTitleTxt" + (rowCount).ToString();
            correctedTitleTxt.Size = correctedTitleTxt0.Size;
            correctedTitleTxt.TabIndex = correctedTitleTxt0.TabIndex + (itemsPerRow * rowCount);
            // 
            // originalTitleLbl
            // 
            originalTitleLbl.AutoSize = originalTitleLbl0.AutoSize;
            originalTitleLbl.Location = new System.Drawing.Point(originalTitleLbl0.Location.X, originalTitleLbl0.Location.Y + (rowSpacing * rowCount));
            originalTitleLbl.Name = "originalTitleLbl" + (rowCount).ToString();
            originalTitleLbl.Size = originalTitleLbl0.Size;
            originalTitleLbl.TabIndex = originalTitleLbl0.TabIndex + (itemsPerRow * rowCount);
            originalTitleLbl.Text = originalTitleLbl0.Text;
            toolTip.SetToolTip(originalTitleLbl, toolTip.GetToolTip(originalTitleLbl0));
            // 
            // correctedTitleLbl
            // 
            correctedTitleLbl.AutoSize = correctedTitleLbl0.AutoSize;
            correctedTitleLbl.Location = new System.Drawing.Point(correctedTitleLbl0.Location.X, correctedTitleLbl0.Location.Y + (rowSpacing * rowCount));
            correctedTitleLbl.Name = "correctedTitleLbl" + (rowCount).ToString();
            correctedTitleLbl.Size = correctedTitleLbl0.Size;
            correctedTitleLbl.TabIndex = correctedTitleLbl0.TabIndex + (itemsPerRow * rowCount);
            correctedTitleLbl.Text = correctedTitleLbl0.Text;
            toolTip.SetToolTip(correctedTitleLbl, toolTip.GetToolTip(correctedTitleLbl0));
            // 
            // or
            // 
            or.AutoSize = or0.AutoSize;
            or.Font = or0.Font;
            or.ForeColor = or0.ForeColor;
            or.Location = new System.Drawing.Point(or0.Location.X, or0.Location.Y + (rowSpacing * rowCount));
            or.Name = "or" + (rowCount).ToString();
            or.Size = or0.Size;
            or.TabIndex = or0.TabIndex + (itemsPerRow * rowCount);
            or.Text = or0.Text;
            // 
            // arrow
            // 
            arrow.AutoSize = arrow0.AutoSize;
            arrow.Font = arrow0.Font;
            arrow.ForeColor = arrow0.ForeColor;
            arrow.Location = new System.Drawing.Point(arrow0.Location.X, arrow0.Location.Y + (rowSpacing * rowCount));
            arrow.Name = "arrow" + (rowCount).ToString();
            arrow.Size = arrow0.Size;
            arrow.TabIndex = arrow0.TabIndex + (itemsPerRow * rowCount);
            arrow.Text = arrow0.Text;
            // 
            // deleteRow
            // 
            deleteRow.FlatStyle = deleteRow0.FlatStyle;
            deleteRow.Image = deleteRow0.Image;
            deleteRow.Location = new System.Drawing.Point(deleteRow0.Location.X, deleteRow0.Location.Y + (rowSpacing * rowCount));
            deleteRow.Name = "deleteRow" + (rowCount).ToString();
            deleteRow.Size = deleteRow0.Size;
            deleteRow.TabIndex = deleteRow0.TabIndex + (itemsPerRow * rowCount);
            deleteRow.UseVisualStyleBackColor = deleteRow0.UseVisualStyleBackColor;
            deleteRow.Visible = true; // Except the 1st row which can't be deleted, all the rest are visible
            deleteRow.Click += new System.EventHandler(deleteRow_Click);
            //
            // Move the OK and Cancel buttons down
            //
            okCmd.Location = new System.Drawing.Point(okCmd.Location.X, okCmd.Location.Y + rowSpacing);
            cancelCmd.Location = new System.Drawing.Point(cancelCmd.Location.X, cancelCmd.Location.Y + rowSpacing);
            // 
            // Add the new row of controls to the page
            // 
            Controls.Add(arrow);
            Controls.Add(or);
            Controls.Add(correctedTitleLbl);
            Controls.Add(originalTitleLbl);
            Controls.Add(correctedTitleTxt);
            Controls.Add(originalTitleTxt);
            Controls.Add(tvdbSeriesId);
            Controls.Add(imdbSeriesId);
            Controls.Add(imdbLbl);
            Controls.Add(tvdbLbl);
            Controls.Add(deleteRow);

            ResumeLayout(false);
            PerformLayout();
            
            rowCount++; // We now have one more row, keep track
        }

        private void deleteRow_Click(object sender, EventArgs e)
        {
            // Delete the entire row and and dispose of the controls
            Button deleteRow;
            Label tvdbLbl;
            Label imdbLbl;
            TextBox imdbSeriesId;
            TextBox tvdbSeriesId;
            TextBox originalTitleTxt;
            TextBox correctedTitleTxt;
            Label originalTitleLbl;
            Label correctedTitleLbl;
            Label or;
            Label arrow;
            int rowIndex; // 0 based row index

            // First find the controls which we need to delete
            deleteRow = (Button)sender; // Get the button control that was pressed
            rowIndex = int.Parse(deleteRow.Name.Replace("deleteRow", "")); // Find out which button was pressed, we need to delete the entire row
            tvdbLbl = (Label)Controls.Find("tvdbLbl" + rowIndex.ToString(), true)[0];
            imdbLbl = (Label)Controls.Find("imdbLbl" + rowIndex.ToString(), true)[0];
            imdbSeriesId = (TextBox)Controls.Find("imdbSeriesId" + rowIndex.ToString(), true)[0];
            tvdbSeriesId = (TextBox)Controls.Find("tvdbSeriesId" + rowIndex.ToString(), true)[0];
            originalTitleTxt = (TextBox)Controls.Find("originalTitleTxt" + rowIndex.ToString(), true)[0];
            correctedTitleTxt = (TextBox)Controls.Find("correctedTitleTxt" + rowIndex.ToString(), true)[0];
            originalTitleLbl = (Label)Controls.Find("originalTitleLbl" + rowIndex.ToString(), true)[0];
            correctedTitleLbl = (Label)Controls.Find("correctedTitleLbl" + rowIndex.ToString(), true)[0];
            or = (Label)Controls.Find("or" + rowIndex.ToString(), true)[0];
            arrow = (Label)Controls.Find("arrow" + rowIndex.ToString(), true)[0];

            // Deregister any event handlers
            deleteRow.Click -= new System.EventHandler(deleteRow_Click);
            tvdbSeriesId.KeyPress -= new System.Windows.Forms.KeyPressEventHandler(tvdbSeriesId_KeyPress);

            // Remove the controls
            Controls.Remove(arrow);
            Controls.Remove(or);
            Controls.Remove(correctedTitleLbl);
            Controls.Remove(originalTitleLbl);
            Controls.Remove(correctedTitleTxt);
            Controls.Remove(originalTitleTxt);
            Controls.Remove(tvdbSeriesId);
            Controls.Remove(imdbSeriesId);
            Controls.Remove(imdbLbl);
            Controls.Remove(tvdbLbl);
            Controls.Remove(deleteRow);

            // Finally dispose off the controls
            deleteRow.Dispose();
            tvdbLbl.Dispose();
            imdbLbl.Dispose();
            imdbSeriesId.Dispose();
            tvdbSeriesId.Dispose();
            originalTitleTxt.Dispose();
            correctedTitleTxt.Dispose();
            originalTitleLbl.Dispose();
            correctedTitleLbl.Dispose();
            or.Dispose();
            arrow.Dispose();

            // Adjust the positions and names of the rest of the controls ahead of the one that was just deleted
            for (int i = rowIndex + 1; i < rowCount; i++)
            {
                // Locate the controls
                deleteRow = (Button)Controls.Find("deleteRow" + i.ToString(), true)[0];
                tvdbLbl = (Label)Controls.Find("tvdbLbl" + i.ToString(), true)[0];
                imdbLbl = (Label)Controls.Find("imdbLbl" + i.ToString(), true)[0];
                imdbSeriesId = (TextBox)Controls.Find("imdbSeriesId" + i.ToString(), true)[0];
                tvdbSeriesId = (TextBox)Controls.Find("tvdbSeriesId" + i.ToString(), true)[0];
                originalTitleTxt = (TextBox)Controls.Find("originalTitleTxt" + i.ToString(), true)[0];
                correctedTitleTxt = (TextBox)Controls.Find("correctedTitleTxt" + i.ToString(), true)[0];
                originalTitleLbl = (Label)Controls.Find("originalTitleLbl" + i.ToString(), true)[0];
                correctedTitleLbl = (Label)Controls.Find("correctedTitleLbl" + i.ToString(), true)[0];
                or = (Label)Controls.Find("or" + i.ToString(), true)[0];
                arrow = (Label)Controls.Find("arrow" + i.ToString(), true)[0];

                // Change the names
                deleteRow.Name = deleteRow.Name.Replace(i.ToString(), (i - 1).ToString());
                tvdbLbl.Name = tvdbLbl.Name.Replace(i.ToString(), (i - 1).ToString());
                imdbLbl.Name = imdbLbl.Name.Replace(i.ToString(), (i - 1).ToString());
                imdbSeriesId.Name = imdbSeriesId.Name.Replace(i.ToString(), (i - 1).ToString());
                tvdbSeriesId.Name = tvdbSeriesId.Name.Replace(i.ToString(), (i - 1).ToString());
                originalTitleTxt.Name = originalTitleTxt.Name.Replace(i.ToString(), (i - 1).ToString());
                correctedTitleTxt.Name = correctedTitleTxt.Name.Replace(i.ToString(), (i - 1).ToString());
                originalTitleLbl.Name = originalTitleLbl.Name.Replace(i.ToString(), (i - 1).ToString());
                correctedTitleLbl.Name = correctedTitleLbl.Name.Replace(i.ToString(), (i - 1).ToString());
                or.Name = or.Name.Replace(i.ToString(), (i - 1).ToString());
                arrow.Name = arrow.Name.Replace(i.ToString(), (i - 1).ToString());

                // Adjust the tabindex
                deleteRow.TabIndex -= itemsPerRow;
                tvdbLbl.TabIndex -= itemsPerRow;
                imdbLbl.TabIndex -= itemsPerRow;
                imdbSeriesId.TabIndex -= itemsPerRow;
                tvdbSeriesId.TabIndex -= itemsPerRow;
                originalTitleTxt.TabIndex -= itemsPerRow;
                correctedTitleTxt.TabIndex -= itemsPerRow;
                originalTitleLbl.TabIndex -= itemsPerRow;
                correctedTitleLbl.TabIndex -= itemsPerRow;
                or.TabIndex -= itemsPerRow;
                arrow.TabIndex -= itemsPerRow;

                // Adjust the locations
                deleteRow.Location = new System.Drawing.Point(deleteRow.Location.X, deleteRow.Location.Y - rowSpacing);
                tvdbLbl.Location = new System.Drawing.Point(tvdbLbl.Location.X, tvdbLbl.Location.Y - rowSpacing);
                imdbLbl.Location = new System.Drawing.Point(imdbLbl.Location.X, imdbLbl.Location.Y - rowSpacing);
                imdbSeriesId.Location = new System.Drawing.Point(imdbSeriesId.Location.X, imdbSeriesId.Location.Y - rowSpacing);
                tvdbSeriesId.Location = new System.Drawing.Point(tvdbSeriesId.Location.X, tvdbSeriesId.Location.Y - rowSpacing);
                originalTitleTxt.Location = new System.Drawing.Point(originalTitleTxt.Location.X, originalTitleTxt.Location.Y - rowSpacing);
                correctedTitleTxt.Location = new System.Drawing.Point(correctedTitleTxt.Location.X, correctedTitleTxt.Location.Y - rowSpacing);
                originalTitleLbl.Location = new System.Drawing.Point(originalTitleLbl.Location.X, originalTitleLbl.Location.Y - rowSpacing);
                correctedTitleLbl.Location = new System.Drawing.Point(correctedTitleLbl.Location.X, correctedTitleLbl.Location.Y - rowSpacing);
                or.Location = new System.Drawing.Point(or.Location.X, or.Location.Y - rowSpacing);
                arrow.Location = new System.Drawing.Point(arrow.Location.X, deleteRow.Location.Y - rowSpacing);
            }

            // Finally adjust the Ok and Cancel buttons
            okCmd.Location = new System.Drawing.Point(okCmd.Location.X, okCmd.Location.Y - rowSpacing);
            cancelCmd.Location = new System.Drawing.Point(cancelCmd.Location.X, cancelCmd.Location.Y - rowSpacing);

            rowCount--; // One less row to manage
        }
    }
}
