using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.GUI
{
    public partial class MediaInfoForm : Form
    {
        /// <summary>
        /// Structure for passing parameters to the ShowMedia method
        /// </summary>
        public struct MediaInfoFormParams
        {
            /// <summary>
            /// MediaInfo
            /// </summary>
            public MediaInfo mediaInfo;
            /// <summary>
            /// Source Video
            /// </summary>
            public string sourceVideo;
            /// <summary>
            /// Tooltip object
            /// </summary>
            public ToolTip toolTip;
        };

        public MediaInfoForm()
        {
            InitializeComponent();

            this.KeyDown += new KeyEventHandler(MediaForm_KeyDown);
        }

        private void MediaForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Close(); // Press escape to close
        }

        /// <summary>
        /// Show the media information
        /// </summary>
        /// <param name="mParams">MediaInfoFormParams object</param>
        public void ShowMediaInfo(object mParams)
        {
            MediaInfoFormParams mediaParams = (MediaInfoFormParams)mParams;
            string sourceVideo = mediaParams.sourceVideo;
            MediaInfo mediaInfo = mediaParams.mediaInfo;
            ToolTip toolTip = mediaParams.toolTip;

            // Video Name
            mediaSpace.SelectionAlignment = HorizontalAlignment.Center;
            mediaSpace.SelectionFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            mediaSpace.AppendText(Path.GetFileName(sourceVideo) + "\n");
            mediaSpace.SelectionFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));

            // Video information
            TimeSpan t = TimeSpan.FromSeconds(mediaInfo.VideoInfo.Duration);
            string duration = string.Format("{0:D2}h : {1:D2}m : {2:D2}s",
                                    t.Hours,
                                    t.Minutes,
                                    t.Seconds);
            mediaSpace.AppendText("\n");
            mediaSpace.SelectionFont = new System.Drawing.Font("Microsoft Sans Serif", 8.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            mediaSpace.SelectionAlignment = HorizontalAlignment.Center;
            mediaSpace.AppendText(Localise.GetPhrase("Video") + "\n");
            mediaSpace.SelectionFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            mediaSpace.SelectionAlignment = HorizontalAlignment.Left;
            mediaSpace.AppendText("------------------------------------------------------------------------------");
            mediaSpace.AppendText("\n" + Localise.GetPhrase("Codec") + "\t  :  " + mediaInfo.VideoInfo.VideoCodec);
            mediaSpace.AppendText("\n" + Localise.GetPhrase("Duration") + "\t  :  " + duration);
            mediaSpace.AppendText("\n" + Localise.GetPhrase("Height") + "\t  :  " + mediaInfo.VideoInfo.Height.ToString().Replace("-1", "-"));
            mediaSpace.AppendText("\n" + Localise.GetPhrase("Width") + "\t  :  " + mediaInfo.VideoInfo.Width.ToString().Replace("-1", "-"));
            mediaSpace.AppendText("\n" + Localise.GetPhrase("FPS") + "\t  :  " + mediaInfo.VideoInfo.FPS.ToString(System.Globalization.CultureInfo.InvariantCulture));
            
            // Audio Information
            if (mediaInfo.AudioInfo != null)
            {
                for (int i = 0; i < mediaInfo.AudioInfo.Count; i++)
                {
                    mediaSpace.AppendText("\n\n");
                    mediaSpace.SelectionFont = new System.Drawing.Font("Microsoft Sans Serif", 8.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
                    mediaSpace.SelectionAlignment = HorizontalAlignment.Center;
                    mediaSpace.AppendText(Localise.GetPhrase("Audio") + " " + (i + 1).ToString() + "\n");
                    mediaSpace.SelectionFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
                    mediaSpace.SelectionAlignment = HorizontalAlignment.Left;
                    mediaSpace.AppendText("------------------------------------------------------------------------------");
                    try
                    {
                        mediaSpace.AppendText(Localise.GetPhrase("Language") + " :  " + ISO639_3.GetLanguageName(mediaInfo.AudioInfo[i].Language));
                        if (!String.IsNullOrEmpty(mediaInfo.AudioInfo[i].Language))
                            mediaSpace.AppendText("  (" + mediaInfo.AudioInfo[i].Language + ")");
                        if (mediaInfo.AudioInfo[i].Impaired)
                            mediaSpace.AppendText("  (" + Localise.GetPhrase("Impaired") + ")");
                    }
                    catch
                    {
                        mediaSpace.AppendText(Localise.GetPhrase("Language") + " :  " + Localise.GetPhrase("Unknown")); // Exception thrown if the language code is not known
                        if (mediaInfo.AudioInfo[i].Impaired)
                            mediaSpace.AppendText("  (" + Localise.GetPhrase("Impaired") + ")");
                    }
                    mediaSpace.AppendText("\n" + Localise.GetPhrase("Codec") + "\t :  " + mediaInfo.AudioInfo[i].AudioCodec);
                    mediaSpace.AppendText("\n" + Localise.GetPhrase("Channels") + "  :  " + mediaInfo.AudioInfo[i].Channels.ToString().Replace("-1", "-"));
                }
            }

            // Subtitle Information - no need to show, we don't use it in MCEBuddy
            if (mediaInfo.SubtitleInfo != null)
            {
                for (int i = 0; i < mediaInfo.SubtitleInfo.Count; i++)
                {
                    mediaSpace.AppendText("\n\n");
                    mediaSpace.SelectionFont = new System.Drawing.Font("Microsoft Sans Serif", 8.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
                    mediaSpace.SelectionAlignment = HorizontalAlignment.Center;
                    mediaSpace.AppendText(Localise.GetPhrase("Subtitle") + " " + (i+1).ToString() + "\n");
                    mediaSpace.SelectionFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
                    mediaSpace.SelectionAlignment = HorizontalAlignment.Left;
                    mediaSpace.AppendText("------------------------------------------------------------------------------");

                    try
                    {
                        mediaSpace.AppendText(Localise.GetPhrase("Language") + " :  " + ISO639_3.GetLanguageName(mediaInfo.SubtitleInfo[i].Language));
                        if (!String.IsNullOrEmpty(mediaInfo.SubtitleInfo[i].Language))
                            mediaSpace.AppendText("  (" + mediaInfo.SubtitleInfo[i].Language + ")");
                    }
                    catch
                    {
                        mediaSpace.AppendText(Localise.GetPhrase("Language") + " :  " + Localise.GetPhrase("Unknown")); // Exception thrown if the language code is not known
                    }
                    mediaSpace.AppendText("\n" + Localise.GetPhrase("Codec") + "\t :  " + mediaInfo.SubtitleInfo[i].Name);
                }
            }

            mediaSpace.Height = 170 + 78 * (mediaInfo.AudioInfo == null ? 0 : mediaInfo.AudioInfo.Count) + 60 * (mediaInfo.SubtitleInfo == null ? 0 : mediaInfo.SubtitleInfo.Count); // Name+Video 170, Audio 75, Subtitle 60 each
            this.Height = mediaSpace.Bottom + 10; // Window height buffer from edge
            LocaliseForms.LocaliseForm(this, toolTip); // Localize the Text
            ShowDialog();
            Focus();

            return;
        }

        /// <summary>
        /// A thread safe way to Show the Media Information
        /// </summary>
        /// <param name="MediaParams">MediaInfoFormParams object</param>
        public static void ThreadShowMediaInfo(object MediaParams)
        {
            MediaInfoForm mediaForm = new MediaInfoForm();
            mediaForm.ShowMediaInfo(MediaParams);
        }

        private void MediaInfoForm_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            LocaliseForms.LocaliseForm(this, toolTip);
        }
    }
}
