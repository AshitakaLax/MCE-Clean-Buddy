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
    public partial class RemoteEngineForm : Form
    {
        public RemoteEngineForm()
        {
            InitializeComponent();

            Ini tempIni = new Ini(GlobalDefs.TempSettingsFile);
            string remoteServerName = tempIni.ReadString("Engine", "RemoteServerName", GlobalDefs.MCEBUDDY_SERVER_NAME);
            int remoteServerPort = tempIni.ReadInteger("Engine", "RemoteServerPort", int.Parse(GlobalDefs.MCEBUDDY_SERVER_PORT));

            engineNameTxt.Text = remoteServerName;
            portNoTxt.Text = remoteServerPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (engineNameTxt.Text == "" || portNoTxt.Text == "")
            {
                MessageBox.Show(Localise.GetPhrase("Please enter a valid Engine Name and Engine Port Number"), Localise.GetPhrase("Invalid Name/Port"));
                return;
            }

            Ini tempIni = new Ini(GlobalDefs.TempSettingsFile);
            tempIni.Write("Engine", "RemoteServerName", engineNameTxt.Text.Trim());
            tempIni.Write("Engine", "RemoteServerPort", portNoTxt.Text.Trim());
        }

        private void RemoteEngine_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            LocaliseForms.LocaliseForm(this, toolTip);
        }

        private void DefaultButton_Click(object sender, EventArgs e)
        {
            engineNameTxt.Text = GlobalDefs.MCEBUDDY_SERVER_NAME;
            portNoTxt.Text = GlobalDefs.MCEBUDDY_SERVER_PORT;
        }

        private void portNoTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            const char Delete = (char)8;
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != Delete;
        }
    }
}
