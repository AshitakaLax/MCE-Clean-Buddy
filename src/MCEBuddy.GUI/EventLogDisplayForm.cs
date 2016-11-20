using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.GUI
{
    public partial class EventLogDisplayForm : Form
    {
        private ListViewSorter Sorter = new ListViewSorter();
        private List<EventLogEntry> _eventLogEntries;

        public EventLogDisplayForm(List<EventLogEntry> EventLogEntries)
        {
            InitializeComponent();

            _eventLogEntries = EventLogEntries;

            // First get the new scale
            using (Graphics g = this.CreateGraphics())
            {
                float _scale = g.DpiX / 96; // Get the system DPI (font scaling)

                // Rescale the columns - for some reason Windows does not rescale the columns in high DPI mode
                this.eventList.Size = this.splitContainer.Panel1.ClientSize;
                this.eventMsg.Size = this.splitContainer.Panel2.ClientSize;
                this.dateTime.Width = (int)(this.dateTime.Width * _scale);
                this.summary.Width = (int)(this.summary.Width * _scale);
            }
        }

        private void cmdClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void EventLogDisplay_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            LocaliseForms.LocaliseForm(this, toolTip);

            DisplayeEventLog();
        }

        private void DisplayeEventLog()
        {
            eventList.ListViewItemSorter = null; // Clear the sorter function to speed it up, else it sorts while adding
            eventList.Items.Clear(); // Clear the history

            foreach (EventLogEntry eventLogEntry in _eventLogEntries)
            {
                string message = "";

                foreach (string msg in eventLogEntry.ReplacementStrings)
                    message += msg + "\n";

                string[] evItem = new string[eventList.Columns.Count]; // create list of Event Log entries
                evItem[eventList.Columns["type"].Index] = eventLogEntry.EntryType.ToString(); // Type
                evItem[eventList.Columns["dateTime"].Index] = eventLogEntry.TimeGenerated.ToString("d-MMM-yyyy   h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
                evItem[eventList.Columns["summary"].Index] = message;
                eventList.Items.Add(new ListViewItem(evItem));
            }

            this.Text = Localise.GetPhrase("MCEBuddy Windows Event Log Viewer") + " - " + eventList.Items.Count + " " + Localise.GetPhrase("Entries");
            eventList.ListViewItemSorter = Sorter; // Set the sorter function
            Sorter.SortColumn = 1; // We need to sort by Date/Time
            Sorter.Order = SortOrder.Descending; // We want to sort by latest logs first
            eventList.Sort(); // Sort the logs
        }

        private void eventList_SelectionChanged(Object sender, EventArgs e)
        {
            if (eventList.SelectedItems.Count == 0)
                return;

            eventMsg.Text = eventList.SelectedItems[0].SubItems[0].Text + "\t" + eventList.SelectedItems[0].SubItems[1].Text + "\r\n" + eventList.SelectedItems[0].SubItems[2].Text;
        }

        private void eventList_OnColumnClick(Object sender, ColumnClickEventArgs e)
        {
            if (!(eventList.ListViewItemSorter is ListViewSorter))
                return;
            Sorter = (ListViewSorter)eventList.ListViewItemSorter;

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

            eventList.Sort();
        }
    }
}
