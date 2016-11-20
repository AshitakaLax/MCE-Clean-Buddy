using System;

using MCEBuddy.Globals;

namespace MCEBuddy.Util
{
    public static class LocaliseForms
    {
        /// <summary>
        /// Localize WPF form
        /// </summary>
        public static void LocaliseForm(System.Windows.Window sourceWindow)
        {
            sourceWindow.Title = Localise.GetPhrase(sourceWindow.Title);
            if (sourceWindow.ToolTip != null)
                sourceWindow.ToolTip = Localise.GetPhrase((string)sourceWindow.ToolTip);
            LocaliseControl(sourceWindow);
        }

        /// <summary>
        /// Localize Windows form with tooltip and context menu strip
        /// </summary>
        public static void LocaliseForm(System.Windows.Forms.Form sourceForm, System.Windows.Forms.ToolTip toolTip, System.Windows.Forms.ContextMenuStrip contextMenuStrip)
        {
            LocaliseForm(sourceForm, toolTip);
            foreach (System.Windows.Forms.ToolStripItem item in contextMenuStrip.Items)
            {
                item.Text = Localise.GetPhrase(item.Text);
                if (item.AutoToolTip)
                    item.ToolTipText = Localise.GetPhrase(item.ToolTipText);
            }
        }

        /// <summary>
        /// Localize Windows form with tooltip
        /// </summary>
        public static void LocaliseForm(System.Windows.Forms.Form sourceForm, System.Windows.Forms.ToolTip toolTip)
        {
            sourceForm.Text = Localise.GetPhrase(sourceForm.Text);
            LocaliseControl(sourceForm.Controls, toolTip);
        }

        private static void LocaliseControl<T>(T control, System.Windows.Forms.ToolTip toolTip = null)
        {
            if (control is System.Windows.Forms.Control.ControlCollection) // Window forms controls
            {
                System.Windows.Forms.Control.ControlCollection ctl = control as System.Windows.Forms.Control.ControlCollection;

                foreach (System.Windows.Forms.Control childCtl in ctl) // look for all child controls
                {
                    if (toolTip != null)
                        toolTip.SetToolTip(childCtl, Localise.GetPhrase(toolTip.GetToolTip(childCtl)));

                    if ((childCtl.GetType() == typeof(System.Windows.Forms.Label)) || (childCtl.GetType() == typeof(System.Windows.Forms.GroupBox)) || (childCtl.GetType() == typeof(System.Windows.Forms.CheckBox)) || (childCtl.GetType() == typeof(System.Windows.Forms.Button)))
                        childCtl.Text = Localise.GetPhrase(childCtl.Text);

                    LocaliseControl(childCtl.Controls, toolTip); // Localize recursively for nested controls
                }
            }
            else // WPF controls
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(control as System.Windows.DependencyObject); i++) // look for all child controls
                {
                    dynamic childCtl = System.Windows.Media.VisualTreeHelper.GetChild(control as System.Windows.DependencyObject, i);
                    if (IsSettingsExist(childCtl, "ToolTip")) // Check and change tooltip
                        if (childCtl.ToolTip != null)
                            childCtl.ToolTip = Localise.GetPhrase((string)childCtl.ToolTip);

                    if (((childCtl.GetType() == typeof(System.Windows.Controls.Label))) || ((childCtl.GetType() == typeof(System.Windows.Controls.GroupBox))) || ((childCtl.GetType() == typeof(System.Windows.Controls.CheckBox))) || ((childCtl.GetType() == typeof(System.Windows.Controls.Button))))
                    {
                        if ((childCtl.Content != null) && (childCtl.Content.GetType() == typeof(System.Windows.Controls.TextBlock))) // Check for nested labels
                            childCtl.Content.Text = Localise.GetPhrase(childCtl.Content.Text);
                        else
                            childCtl.Content = Localise.GetPhrase(childCtl.Content);
                    }

                    LocaliseControl(childCtl, null); // Localize recursively for nested controls
                }
            }
        }

        private static bool IsSettingsExist(dynamic settings, string name)
        {
            return settings.GetType().GetProperty(name) != null;
        }

        static public System.Drawing.Font ChangeFontSize(System.Drawing.Font font, float fontSize)
        {
            if (font != null)
            {
                float currentSize = font.Size;
                if (currentSize != fontSize)
                {
                    font = new System.Drawing.Font(font.Name, fontSize,
                        font.Style, font.Unit,
                        font.GdiCharSet, font.GdiVerticalFont);
                }
            }
            return font;
        }
    }
}
