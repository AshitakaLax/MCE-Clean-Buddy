using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.GUI
{
    public partial class HistoryForm : Form
    {
        private ListViewSorter Sorter = new ListViewSorter();
        Dictionary<string, SortedList<string, string>> _history;
        ICore _pipeProxy;

        public HistoryForm(ICore pipeProxy)
        {
            InitializeComponent();

            _pipeProxy = pipeProxy;
        }

        /// <summary>
        /// Get the History from the remote engine and populate the form
        /// </summary>
        private void ShowHistory_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            LocaliseForms.LocaliseForm(this, toolTip);

            GetAndDisplayHistoryLog();
        }

        private void GetAndDisplayHistoryLog()
        {
            showHistoryList.ListViewItemSorter = null; // Clear the sorter function so that it does start sorting as soon as you add an entry (slows down)
            showHistoryList.Items.Clear(); // Clean it up first

            try
            {
                _history = _pipeProxy.GetConversionHistory();
            }
            catch (Exception e1)
            {
                MessageBox.Show(Localise.GetPhrase("Unable to communicate with engine."), Localise.GetPhrase("Communication Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to get History File from engine"), EventLogEntryType.Warning);
                Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
                this.Close(); // done here, nothing to display
                return;
            }

            foreach (string filePath in _history.Keys)
            {
                SortedList<string, string> details = _history[filePath];

                foreach (string key in details.Keys)
                {
                    try
                    {
                        string[] displayEntry = new string[showHistoryList.Columns.Count];
                        DateTime dt, dts;
                        string value = details[key];

                        if (key.Contains("ConvertedAt"))
                        {
                            displayEntry[showHistoryList.Columns["filePath"].Index] = filePath; // File Path is 1st Entry

                            if (DateTime.TryParse(details[key], System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
                                displayEntry[showHistoryList.Columns["convertedOn"].Index] = dt.ToString(System.Globalization.CultureInfo.InvariantCulture); // Time of conversion

                            displayEntry[showHistoryList.Columns["conversionStatus"].Index] = "Converted"; // this entry is converted

                            if (details.Keys.Contains("ConvertedStart" + key.Substring("ConvertedAt".Length)))
                            {
                                string startTime = details["ConvertedStart" + key.Substring("ConvertedAt".Length)];
                                if (dt != null)
                                    if (DateTime.TryParse(startTime, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dts))
                                        displayEntry[showHistoryList.Columns["timeTaken"].Index] = (dt - dts).Hours.ToString("00") + ":" + (dt - dts).Minutes.ToString("00"); // Time taken for conversion
                            }

                            if (details.Keys.Contains("ConvertedUsingTask" + key.Substring("ConvertedAt".Length)))
                                displayEntry[showHistoryList.Columns["task"].Index] = details["ConvertedUsingTask" + key.Substring("ConvertedAt".Length)];

                            if (details.Keys.Contains("ConvertedUsingProfile" + key.Substring("ConvertedAt".Length)))
                                displayEntry[showHistoryList.Columns["profile"].Index] = details["ConvertedUsingProfile" + key.Substring("ConvertedAt".Length)];

                            if (details.Keys.Contains("ConvertedTo" + key.Substring("ConvertedAt".Length)))
                                displayEntry[showHistoryList.Columns["convertedTo"].Index] = details["ConvertedTo" + key.Substring("ConvertedAt".Length)];
                        }
                        else if (key.Contains("ErrorAt"))
                        {
                            displayEntry[showHistoryList.Columns["filePath"].Index] = filePath; // File Path is 1st Entry

                            if (DateTime.TryParse(details[key], System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
                                displayEntry[showHistoryList.Columns["convertedOn"].Index] = dt.ToString(System.Globalization.CultureInfo.InvariantCulture); // Time of error

                            displayEntry[showHistoryList.Columns["conversionStatus"].Index] = "Error"; // this entry is error

                            if (details.Keys.Contains("ErrorUsingTask" + key.Substring("ErrorAt".Length)))
                                displayEntry[showHistoryList.Columns["task"].Index] = details["ErrorUsingTask" + key.Substring("ErrorAt".Length)];

                            if (details.Keys.Contains("ErrorUsingProfile" + key.Substring("ErrorAt".Length)))
                                displayEntry[showHistoryList.Columns["profile"].Index] = details["ErrorUsingProfile" + key.Substring("ErrorAt".Length)];

                            if (details.Keys.Contains("ErrorMessage" + key.Substring("ErrorAt".Length)))
                                displayEntry[showHistoryList.Columns["errorMessage"].Index] = details["ErrorMessage" + key.Substring("ErrorAt".Length)];
                        }
                        else if (key.Contains("CancelledAt"))
                        {
                            displayEntry[showHistoryList.Columns["filePath"].Index] = filePath; // File Path is 1st Entry

                            if (DateTime.TryParse(details[key], System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
                                displayEntry[showHistoryList.Columns["convertedOn"].Index] = dt.ToString(System.Globalization.CultureInfo.InvariantCulture); // Time of cancellation

                            displayEntry[showHistoryList.Columns["conversionStatus"].Index] = "Cancelled"; // this entry is cancelled

                            if (details.Keys.Contains("CancelledUsingTask" + key.Substring("CancelledAt".Length)))
                                displayEntry[showHistoryList.Columns["task"].Index] = details["CancelledUsingTask" + key.Substring("CancelledAt".Length)];

                            if (details.Keys.Contains("CancelledUsingProfile" + key.Substring("CancelledAt".Length)))
                                displayEntry[showHistoryList.Columns["profile"].Index] = details["CancelledUsingProfile" + key.Substring("CancelledAt".Length)];
                        }
                        // Ignore the rest (which should include ConvertedToOutputAt)

                        if (!String.IsNullOrEmpty(displayEntry[showHistoryList.Columns["filePath"].Index])) // We have a valid entry
                            showHistoryList.Items.Add(new ListViewItem(displayEntry)); // Add to the display
                    }
                    catch (Exception e1)
                    {
                        Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Error parsing History File"), EventLogEntryType.Warning);
                        Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
                    }
                }
            }

            this.Text = Localise.GetPhrase("History of Converted Files") + " - " + showHistoryList.Items.Count + " " + Localise.GetPhrase("Files");
            showHistoryList.ListViewItemSorter = Sorter; // Set the sorter function
            Sorter.SortColumn = 1; // We need to sort by Date/Time
            Sorter.Order = SortOrder.Descending; // We want to sort by latest conversion first
            showHistoryList.Sort(); // Sort the logs
        }
        
        private void clearHistoryBtn_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Localise.GetPhrase("Are you sure you want to clear the history of all conversions?\r\nThis will cause MCEBuddy to reconvert all files being monitored."),
                Localise.GetPhrase("Clear History"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes)
            {
                try
                {
                    _pipeProxy.ClearHistory(); // clear the history
                }
                catch (Exception e1)
                {
                    MessageBox.Show(Localise.GetPhrase("Unable to communicate with engine."), Localise.GetPhrase("Communication Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to clear History File"), EventLogEntryType.Warning);
                    Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
                }

                // Reload form
                GetAndDisplayHistoryLog();
            }
        }

        private void showHistoryList_OnColumnClick(Object sender, ColumnClickEventArgs e)
        {
            if (!(showHistoryList.ListViewItemSorter is ListViewSorter))
                return;
            Sorter = (ListViewSorter)showHistoryList.ListViewItemSorter;

            if (Sorter.SortColumn == e.Column)
            {
                if (Sorter.Order == SortOrder.Ascending)
                    Sorter.Order = SortOrder.Descending;
                else
                    Sorter.Order = SortOrder.Ascending;
            }
            else
            {
                Sorter.Order = SortOrder.Descending;
            }
            Sorter.SortColumn = e.Column;

            showHistoryList.Sort();
        }

        private void copyToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyToClipboard();
        }

        private void CopyToClipboard()
        {
            // Bail out early if no selected items
            if (showHistoryList.SelectedItems.Count == 0)
                return;

            StringBuilder buffer = new StringBuilder();
            // Loop over all the selected items
            foreach (ListViewItem currentItem in showHistoryList.SelectedItems)
            {
                // Don't need to look at currentItem, because it is in subitem[0]
                // So just loop over all the subitems of this selected item
                foreach (ListViewItem.ListViewSubItem sub in currentItem.SubItems)
                {
                    // Append the text and tab - paste into excel
                    buffer.Append(sub.Text);
                    buffer.Append("\t");
                }
                // Annoyance: there is a trailing tab in the buffer, get rid of it
                buffer.Remove(buffer.Length - 1, 1);
                // If you only use \n, not all programs (notepad!!!) will recognize the newline
                buffer.Append("\r\n");
            }

            // Set output to clipboard.
            try
            {
                Clipboard.SetDataObject(buffer.ToString(), true, 5, 200);
            }
            catch (Exception e)
            {
                MessageBox.Show(Localise.GetPhrase("Could not copy information to Clipboard, please try again."), "Error copying to Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to copy history to clipboard"), EventLogEntryType.Warning);
                Log.WriteSystemEventLog(e.ToString(), EventLogEntryType.Warning);
            }
        }
        private void showHistoryList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control) // Ctrl-A selects all items in the ListView
            {
                foreach (ListViewItem item in showHistoryList.Items)
                    item.Selected = true;
            }

            if (e.KeyCode == Keys.C && e.Control) // Ctrl-C copies to clipboard
            {
                CopyToClipboard();
            }
        }

        private void reconvertFileBtn_Click(object sender, EventArgs e)
        {
            if (showHistoryList.SelectedItems.Count == 0)
                return;

            if (MessageBox.Show(Localise.GetPhrase("Are you sure you want to reconvert the selected files?"),
                Localise.GetPhrase("Reconvert files"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes)
            {
                foreach (ListViewItem item in showHistoryList.SelectedItems)
                {
                    try
                    {
                        _pipeProxy.DeleteHistoryItem(item.Text);
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show(Localise.GetPhrase("Unable to communicate with engine."), Localise.GetPhrase("Communication Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to clear History File"), EventLogEntryType.Warning);
                        Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
                    }
                }

                // Reload form
                GetAndDisplayHistoryLog();
            }
        }
    }
}
