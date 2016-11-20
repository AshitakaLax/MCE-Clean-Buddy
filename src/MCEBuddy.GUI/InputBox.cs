using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using MCEBuddy.Util;

namespace MCEBuddy.GUI
{
    public partial class InputBox : Form
    {
        /// <summary>
        /// Delegate used to validate the object
        /// </summary>
        private InputBoxValidatingHandler _validator;

        /// <summary>
        /// Delegate used for keypress on the object
        /// </summary>
        private KeyPressEventHandler _keyPress;

        private InputBox()
        {
            InitializeComponent();
        }

        private void InputBox_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            LocaliseForms.LocaliseForm(this, toolTip);
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Validator = null;
            this.Close();
        }

        private void oKcmd_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Displays a prompt in a dialog box, waits for the user to input text or click a button.
        /// </summary>
        /// <param name="prompt">String expression displayed as the message in the dialog box</param>
        /// <param name="title">String expression displayed in the title bar of the dialog box</param>
        /// <param name="defaultResponse">String expression displayed in the text box as the default response</param>
        /// <param name="validator">Delegate used to validate the text</param>
        /// <param name="keyPressHandler">Delete used to handle keypress events of the textbox</param>
        /// <param name="xpos">Numeric expression that specifies the distance of the left edge of the dialog box from the left edge of the screen.</param>
        /// <param name="ypos">Numeric expression that specifies the distance of the upper edge of the dialog box from the top of the screen</param>
        /// <returns>An InputBoxResult object with the Text and the OK property set to true when OK was clicked.</returns>
        public static InputBoxResult Show(string prompt, string title, string defaultResponse, InputBoxValidatingHandler validator, KeyPressEventHandler keyPressHandler, int xpos, int ypos)
        {
            using (InputBox form = new InputBox())
            {
                form.label.Text = prompt;
                form.Text = title;
                form.textBox.Text = defaultResponse;
                if (xpos >= 0 && ypos >= 0)
                {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Left = xpos;
                    form.Top = ypos;
                }

                form.Validator = validator;
                form.KeyPressed = keyPressHandler;

                DialogResult result = form.ShowDialog();

                InputBoxResult retval = new InputBoxResult();
                if (result == DialogResult.OK)
                {
                    retval.Text = form.textBox.Text;
                    retval.OK = true;
                }

                return retval;
            }
        }

        /// <summary>
        /// Displays a prompt in a dialog box, waits for the user to input text or click a button.
        /// </summary>
        /// <param name="prompt">String expression displayed as the message in the dialog box</param>
        /// <param name="title">String expression displayed in the title bar of the dialog box</param>
        /// <param name="defaultResponse">String expression displayed in the text box as the default response</param>
        /// <param name="validator">Delegate used to validate the text</param>
        /// <param name="keyPressHandler">Delete used to handle keypress events of the textbox</param>
        /// <returns>An InputBoxResult object with the Text and the OK property set to true when OK was clicked.</returns>
        public static InputBoxResult Show(string prompt, string title, string defaultText, InputBoxValidatingHandler validator, KeyPressEventHandler keyPressHandler)
        {
            return Show(prompt, title, defaultText, validator, keyPressHandler, -1, -1);
        }


        /// <summary>
        /// Reset the ErrorProvider
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxText_TextChanged(object sender, EventArgs e)
        {
            errorProviderText.SetError(textBox, "");
        }

        /// <summary>
        /// Key press event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (KeyPressed != null)
            {
                KeyPressed(this, e);
            }
        }

        /// <summary>
        /// Validate the Text using the Validator
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxText_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Validator != null)
            {
                InputBoxValidatingArgs args = new InputBoxValidatingArgs();
                args.Text = textBox.Text;
                Validator(this, args);
                if (args.Cancel)
                {
                    e.Cancel = true;
                    errorProviderText.SetError(textBox, args.Message);
                }
            }
        }

        protected InputBoxValidatingHandler Validator
        {
            get { return (this._validator); }
            set { this._validator = value; }
        }

        protected KeyPressEventHandler KeyPressed
        {
            get { return (this._keyPress); }
            set { this._keyPress = value; }
        }

        /// <summary>
        /// Class used to store the result of an InputBox.Show message.
        /// </summary>
        public class InputBoxResult
        {
            public bool OK = false;
            public string Text = "";
        }

        /// <summary>
        /// EventArgs used to Validate an InputBox
        /// </summary>
        public class InputBoxValidatingArgs : EventArgs
        {
            public string Text;
            public string Message;
            public bool Cancel;
        }

        /// <summary>
        /// Delegate used to Validate an InputBox
        /// </summary>
        public delegate void InputBoxValidatingHandler(object sender, InputBoxValidatingArgs e);
    }
}
