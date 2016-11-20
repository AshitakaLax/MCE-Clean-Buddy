using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Globalization;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.CommercialScan;

namespace MCEBuddy.CustomCutsGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string UsageHelpText =
@"Use this tool to manually select the sections of a video you want MCEBuddy to cut and save them to a EDL file for MCEBuddy to use.

1. Load the video using the 'Open Video' button.
2. Move the slider to mark the start of the section of the video you want to cut and then click the 'Start Cut' button.
3. Move the slider at mark the end of the section of the video you want to cut and then click the 'End Cut' button.
4. Repeat the above process for each section of the video you want to cut.
5. Select any section you don't want from the 'Sections to Cut' box and click the 'Remove' button to remove it from the list.
6. When done creating the sections to cut, click on the 'Save EDL' button to save the list to a EDL file.

Make sure the EDL file is saved with the same name and in the same directory as the video file BEFORE MCEBuddy starts processing the video.";

        private class CutStartEnd
        {
            public string CutStart { get; set; }
            public string CutEnd { get; set; }
        }

        private double gridWidthMargin = 0, gridHeightMargin = 0;
        private bool _savedEDLCuts = false;
        private bool _edlLoading = false, _initializing = false;
        private bool _playing = false;
        private TimeSpan _totalVideoTime = new TimeSpan();
        DispatcherTimer _sliderUpdateTimer, _mediaLoadTimer;
        private const double MEDIA_LOAD_TIMEOUT = 20000;
        private double _startCut = -1;
        private double StartCut
        {
            get { return _startCut; }
            set
            {
                _startCut = value;
                if (_startCut < 0)
                    startLbl.Content = ""; 
                else 
                    startLbl.Content = (new TimeSpan(0, 0, 0, 0, (int)value)).ToString(@"hh\:mm\:ss\.fff");
            }
        }

        public MainWindow()
        {
            _initializing = true;

            InitializeComponent();

            // Setup a handler to handle changes in the EDL list items
            ((INotifyCollectionChanged)edlCuts.Items).CollectionChanged += edlCuts_ItemsChanged;

            _initializing = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaxHeight = System.Windows.SystemParameters.WorkArea.Height; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations
            this.MaxWidth = System.Windows.SystemParameters.WorkArea.Width; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            gridWidthMargin = this.ActualWidth - displayGrid.ActualWidth;
            gridHeightMargin = this.ActualHeight - displayGrid.ActualHeight;

            scrollBars.MaxHeight = this.MaxHeight - gridHeightMargin;
            scrollBars.MaxWidth = this.MaxWidth -gridWidthMargin;

            LocaliseForms.LocaliseForm(this); // Localize form

            // Create a timer that will update the counters and the time slider
            _sliderUpdateTimer = new DispatcherTimer();
            _sliderUpdateTimer.Interval = TimeSpan.FromMilliseconds(100);
            _sliderUpdateTimer.Tick += new EventHandler(sliderTimer_Tick);

            // Create a timer that will check for load timeouts
            _mediaLoadTimer = new DispatcherTimer();
            _mediaLoadTimer.Interval = TimeSpan.FromMilliseconds(MEDIA_LOAD_TIMEOUT); // Give it 10 seconds to timeout to load file
            _mediaLoadTimer.Tick += new EventHandler(mediaLoadTimer_Tick);
        }

        private ICore ConnectToMCEBuddyEngine()
        {
            // Connect to the engine
            try
            {
                ICore pipeProxy = MCEBuddyEngineConnect.ConnectToLocalEngine();
                pipeProxy.EngineRunning(); // Test to check if we are really connected
                return pipeProxy;
            }
            catch
            {
                return null;
            }
        }

        private bool AddFileToMCEBuddyQueue(ICore pipeProxy, string file)
        {
            try
            {
                pipeProxy.AddManualJob(file);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CheckAndStopPlay()
        {
            if (_playing) // If we are playing something first stop it
                playStopCmd.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        /// <summary>
        /// Add a cut start stop to the EDL box
        /// </summary>
        /// <param name="cutStartMS">Cut Start (milliseconds)</param>
        /// <param name="cutEndMS">Cut Stop (milliseconds)</param>
        private void AddEDLStartStopEntry(double cutStartMS, double cutEndMS)
        {
            // Add it to the Cuts List in seconds
            CutStartEnd newCut = new CutStartEnd();
            newCut.CutStart = (new TimeSpan(0, 0, 0, 0, (int)cutStartMS)).ToString(@"hh\:mm\:ss\.fff");
            newCut.CutEnd = (new TimeSpan(0, 0, 0, 0, (int)cutEndMS)).ToString(@"hh\:mm\:ss\.fff");
            edlCuts.Items.Add(newCut);
        }

        /// <summary>
        /// Write the EDL Box data to the EDL File
        /// </summary>
        /// <param name="edlFile">EdlFile to write to</param>
        /// <returns>True if successful</returns>
        private bool WriteEDL(string edlFile)
        {
            try
            {
                // Create an EDL File
                string edlEntries = "";
                foreach (CutStartEnd cut in edlCuts.Items)
                {
                    edlEntries += TimeSpan.ParseExact(cut.CutStart, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture).TotalSeconds.ToString("0.000", CultureInfo.InvariantCulture) + "\t" + TimeSpan.ParseExact(cut.CutEnd, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture).TotalSeconds.ToString("0.000", CultureInfo.InvariantCulture) + "\t" + "0" + "\r\n";
                }

                // Write to EDL file
                if (!String.IsNullOrWhiteSpace(edlEntries)) // Check if these is something to write
                {
                    System.IO.File.WriteAllText(edlFile, edlEntries, System.Text.Encoding.UTF8); // UTF format
                    MessageBox.Show(Localise.GetPhrase("Commercial cuts saved to EDL file") + "\r\n" + edlFile, Localise.GetPhrase("Save Complete"), MessageBoxButton.OK, MessageBoxImage.Information);
                    _savedEDLCuts = true; // We saved it
                }

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(Localise.GetPhrase("Error saving commercial cuts to EDL file") + "\r\n" + edlFile + "\r\n" + "Error : " + e.ToString(), Localise.GetPhrase("Save Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Read the EDL data from the EDL File into the EDLBox
        /// </summary>
        /// <param name="edlFile">EdlFile to read from</param>
        /// <returns>True if successful</returns>
        private bool ReadEDL(string edlFile)
        {
            _edlLoading = true; // We are loading a EDL file, no need to mark as unsaved

            try
            {
                System.IO.StreamReader edlS = new System.IO.StreamReader(edlFile);
                string line;
                while ((line = edlS.ReadLine()) != null)
                {
                    string[] cuts = Regex.Split(line, @"\s+");
                    if (cuts.Length == 3)
                    {
                        if (cuts[0] != cuts[1])
                        {
                            float cutStart, cutEnd;
                            if ((float.TryParse(cuts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out cutStart)) && (float.TryParse(cuts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out cutEnd)))
                                AddEDLStartStopEntry(cutStart * 1000, cutEnd * 1000);
                        }
                    }
                }
                edlS.Close();
                edlS.Dispose();
                _edlLoading = false; // We are done here

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(Localise.GetPhrase("Error loading commercial cuts EDL file") + "\r\n" + edlFile + "\r\n" + "Error : " + e.ToString(), Localise.GetPhrase("Load Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                _edlLoading = false; // We are done here
                return false;
            }
        }

        private void LoadMediaFile(string mediaFile)
        {
            openMediaCmd.Content = Localise.GetPhrase("Loading..."); // Let the user know we are loading
            openMediaCmd.IsEnabled = false;
            playStopCmd.IsEnabled = timeSlider.IsEnabled = cutStartCmd.IsEnabled = cutEndCmd.IsEnabled = false; // Disable them
            cutStartCmd.IsEnabled = cutEndCmd.IsEnabled = false;
            edlCuts.Items.Clear(); // Clear any Cuts
            StartCut = -1; // Reset the start cut
            _savedEDLCuts = true; // Reset the save indicator
            timeSlider.Value = 0; // Reset slider
            mediaPlayer.LoadedBehavior = MediaState.Close; // First close the old one
            mediaPlayer.LoadedBehavior = MediaState.Pause; // Load it paused
            _mediaLoadTimer.Start(); // Timeout to check for load failure
            mediaPlayer.Source = new Uri(mediaFile);
            this.Title = Localise.GetPhrase("MCEBuddy Custom Cuts") + " - " + mediaPlayer.Source.LocalPath;
        }

        private void openMediaCmd_Click(object sender, RoutedEventArgs e)
        {
            CheckAndStopPlay(); // If we are playing something first stop it

            if (!_savedEDLCuts && (edlCuts.Items.Count > 0))
            {
                if (MessageBox.Show(Localise.GetPhrase("Do you want to save the changes?"), Localise.GetPhrase("Unsaved EDL Cuts"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    saveEDLCmd_Click(sender, e);
            }

            System.Windows.Forms.OpenFileDialog openFile = new System.Windows.Forms.OpenFileDialog();
            openFile.Multiselect = false;
            openFile.Filter = "Video files|*.wtv;*.dvr-ms;*.asf;*.avi;*.divx;*.dv;*.flv;*.gxf;*.m1v;*.m2v;*.m2ts;*.m4v;*.mkv;*.mov;*.mp2;*.mp4;*.mpeg;*.mpeg1;*.mpeg2;*.mpeg4;*.mpg;*.mts;*.mxf;*.ogm;*.ts;*.vob;*.wmv;*.tp;*.tivo|All files|*.*";
            if (openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                LoadMediaFile(openFile.FileName);
        }

        private void mediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            ResetMediaOpenButton();

            if (!mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                MessageBox.Show(Localise.GetPhrase("Failed to read media duration"), Localise.GetPhrase("Media Read Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _totalVideoTime = mediaPlayer.NaturalDuration.TimeSpan; // Total video time
            timeSlider.Maximum = _totalVideoTime.TotalMilliseconds; // Set the slider max value

            // NOTE: Update the timeStamp - don't fire event (simulate button) since it sets the mediaplayer to mode to manual and it fails to show the first frame of the video
            RoutedPropertyChangedEventArgs<double> newEventArgs = new RoutedPropertyChangedEventArgs<double>(1, 0);
            timeSlider_ValueChanged(sender, newEventArgs);

            // Enable button in the end after setting everything
            if (!_playing)
            {
                playStopCmd.IsEnabled = timeSlider.IsEnabled = cutStartCmd.IsEnabled = true;
                cutEndCmd.IsEnabled = false;
                mediaPlayer.LoadedBehavior = MediaState.Manual; // Set to manual and pause it so that the seek function works (otherwise it needs to be manually played for seek to work)
                mediaPlayer.Pause(); // Load and pause the video
            }

            // Load the EDL file if found
            if (edlCuts.Items.Count <= 0) // MediaOpened is called multiple times sometimes, only load it once
            {
                string edlFile = FilePaths.GetFullPathWithoutExtension(mediaPlayer.Source.LocalPath) + ".edl";
                string vprjFile = FilePaths.GetFullPathWithoutExtension(mediaPlayer.Source.LocalPath) + ".vprj";
                if (FileIO.FileSize(edlFile) > 0) // Check if it exists
                    ReadEDL(edlFile);
                else if (FileIO.FileSize(vprjFile) > 0) // Check for VPRJ file
                    if (EDL.ConvertVPRJtoEDL(vprjFile, edlFile, new Log())) // Convert to EDL
                        if (FileIO.FileSize(edlFile) > 0)
                            ReadEDL(edlFile);
            }
        }

        private void sliderTimer_Tick(object sender, EventArgs e)
        {
                // Updating time slider
                timeSlider.Value = mediaPlayer.Position.TotalMilliseconds;
        }

        private void ResetMediaOpenButton()
        {
            openMediaCmd.Content = Localise.GetPhrase("Open Video"); // Reset button
            openMediaCmd.IsEnabled = true;
        }

        private void mediaLoadTimer_Tick(object sender, EventArgs e)
        {
            // If the media is not loaded by now abort
            if (!openMediaCmd.IsEnabled)
            {
                MessageBox.Show(Localise.GetPhrase("Failed to load media file"), Localise.GetPhrase("Media Load Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                ResetMediaOpenButton();
            }

            _mediaLoadTimer.Stop(); // Stop the timer
        }

        private void mediaPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ResetMediaOpenButton();

            CheckAndStopPlay(); // If we are playing something first stop it

            MessageBox.Show(Localise.GetPhrase("Failed to load media file") + ", error:\r\n" + e.ErrorException, Localise.GetPhrase("Media Load Error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void mediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            CheckAndStopPlay(); // If we are playing something first stop it
        }

        // NOTE: http://stackoverflow.com/questions/9511569/how-i-will-get-dropping-media-on-mediaelement-in-wpf
        private void mediaPlayer_DropFile(object sender, System.Windows.DragEventArgs e)
        {
            CheckAndStopPlay(); // If we are playing something first stop it

            if (!_savedEDLCuts && (edlCuts.Items.Count > 0))
            {
                if (MessageBox.Show(Localise.GetPhrase("Do you want to save the changes?"), Localise.GetPhrase("Unsaved EDL Cuts"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    saveEDLCmd_Click(sender, e);
            }

            string[] mediaFile = (string[])e.Data.GetData(DataFormats.FileDrop);
            LoadMediaFile(mediaFile[0]);
        }

        private void mediaPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            return;
        }

        private void playStopCmd_Click(object sender, RoutedEventArgs e)
        {
            // Cut button is disabled while playing and enabled while paused
            if (!_playing) // Play
            {
                if (mediaPlayer.Position >= _totalVideoTime)
                    return; // We are at the end

                playStopCmd.Content = Localise.GetPhrase("Pause");
                cutStartCmd.IsEnabled = cutEndCmd.IsEnabled = false;
                mediaPlayer.LoadedBehavior = MediaState.Manual;
                mediaPlayer.Play();
                _sliderUpdateTimer.Start();
                _playing = true;
            }
            else // Pause
            {
                mediaPlayer.LoadedBehavior = MediaState.Manual;
                mediaPlayer.Pause();
                _sliderUpdateTimer.Stop();
                playStopCmd.Content = Localise.GetPhrase("Play");
                cutStartCmd.IsEnabled = (StartCut < 0);
                cutEndCmd.IsEnabled = !(StartCut < 0);
                _playing = false;
            }
        }

        private void timeSlider_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Left)
            {
                CheckAndStopPlay(); // If we are playing something first stop it
                timeSlider.Value -= 100; // Move it by 100 ms
            }

            if (e.Key == System.Windows.Input.Key.Right)
            {
                CheckAndStopPlay(); // If we are playing something first stop it
                timeSlider.Value += 100; // Move it by 100 ms
            }
        }

        private void timeSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CheckAndStopPlay(); // If we are playing something first stop it
        }

        private void timeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int SliderValue = (int)timeSlider.Value;
            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);

            if (playStopCmd.IsEnabled) // Don't set MediaPlayer to manual while loading
            {
                mediaPlayer.LoadedBehavior = MediaState.Manual; // Set it to manual just incase otherwise we can't set the media position
                mediaPlayer.Position = ts;
            }

            timeStamp.Content = mediaPlayer.Position.ToString(@"hh\:mm\:ss\.fff") + " / " + _totalVideoTime.ToString(@"hh\:mm\:ss\.fff");
        }

        private void edlCuts_ItemsChanged(object sender, EventArgs e)
        {
            if (!_edlLoading && edlCuts.Items.Count > 0) // If we are loading the EDL with the Media file then ignore this section
                _savedEDLCuts = false; // We have unsaved edits

            if (edlCuts.Items.Count <= 0)
                sendToMCEBuddy.IsEnabled = saveEDLCmd.IsEnabled = false; // disable the Save EDL and Process with MCEBuddy button if we are empty
            else
                sendToMCEBuddy.IsEnabled = saveEDLCmd.IsEnabled = true; // Enable Save EDL and Process with MCEBuddy button
        }

        private void edlCuts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (edlCuts.SelectedIndex < 0)
                cutDeleteCmd.IsEnabled = false; // Disable Remove button
            else
                cutDeleteCmd.IsEnabled = true; // Enable Remove button
        }

        private void cutStartCmd_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Position >= _totalVideoTime)
                return; // invalid

            // Start cut cannot be before the last end cut
            if (edlCuts.Items.Count > 0)
            {
                if (mediaPlayer.Position <= TimeSpan.ParseExact(((CutStartEnd)edlCuts.Items[edlCuts.Items.Count - 1]).CutEnd, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture))
                {
                    MessageBox.Show(Localise.GetPhrase("Start cut has to be after the previous end cut"), Localise.GetPhrase("Invalid Start Cut"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }

            // Save it
            StartCut = mediaPlayer.Position.TotalMilliseconds;
            cutStartCmd.IsEnabled = false;
            cutEndCmd.IsEnabled = true;
        }

        private void cutEndCmd_Click(object sender, RoutedEventArgs e)
        {
            double endCut = mediaPlayer.Position.TotalMilliseconds;

            // End cannot be before start
            if (endCut <= StartCut)
            {
                MessageBox.Show(Localise.GetPhrase("End cut has to be after the start cut"), Localise.GetPhrase("Invalid End Cut"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            // Add it to the Cuts List
            AddEDLStartStopEntry(StartCut, endCut);

            StartCut = -1; // Reset it

            cutStartCmd.IsEnabled = true;
            cutEndCmd.IsEnabled = false;
        }

        private void cutDeleteCmd_Click(object sender, RoutedEventArgs e)
        {
            if (edlCuts.SelectedIndex < 0)
                return; // Nothing selected

            edlCuts.Items.RemoveAt(edlCuts.SelectedIndex); // Delete it
        }

        private void saveEDLCmd_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog edlFile = new System.Windows.Forms.SaveFileDialog();
            edlFile.InitialDirectory = System.IO.Path.GetDirectoryName(mediaPlayer.Source.LocalPath);
            edlFile.FileName = FilePaths.GetFullPathWithoutExtension(mediaPlayer.Source.LocalPath) + ".edl"; // Default filename is same as original filename
            edlFile.DefaultExt = "*.edl";
            edlFile.AddExtension = true;
            edlFile.Filter = "EDL files|*.edl|All files|*.*";
            if (edlFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                WriteEDL(edlFile.FileName);
        }

        private void helpLbl_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MessageBox.Show(Localise.GetPhrase(UsageHelpText), Localise.GetPhrase("How to Use this Tool"));
        }

        private void payPalBtn_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Ini configIni = new Ini(GlobalDefs.TempSettingsFile);
            string donationLink = configIni.ReadString("Engine", "DonationLink", "");
            if (String.IsNullOrWhiteSpace(donationLink))
                donationLink = Crypto.Decrypt(GlobalDefs.MCEBUDDY_HOME_PAGE); // backup

            Internet.OpenLink(donationLink, "MCEBuddy Commercial Editor");
        }

        private void mceBuddyLogo_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Internet.OpenLink(Crypto.Decrypt(GlobalDefs.MCEBUDDY_HOME_PAGE), "MCEBuddy Commercial Editor");
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Resize the column in the sections box
            GridView gv = edlCuts.View as GridView;
            if (gv != null)
            {
                foreach (var c in gv.Columns)
                {
                    if (!double.IsNaN(c.Width) && (e.PreviousSize.Width > 0) && (e.NewSize.Width > 0))
                        c.Width += (e.NewSize.Width - e.PreviousSize.Width) / 2;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CheckAndStopPlay(); // If we are playing something first stop it

            if (!_savedEDLCuts && (edlCuts.Items.Count > 0))
            {
                if (MessageBox.Show(Localise.GetPhrase("Do you want to save the changes?"), Localise.GetPhrase("Unsaved EDL Cuts"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    e.Cancel = true; // Cancel closing
                    saveEDLCmd.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Do it async here
                    return;
                }
            }

            _mediaLoadTimer.Stop();
            _sliderUpdateTimer.Stop();
            mediaPlayer.LoadedBehavior = mediaPlayer.UnloadedBehavior = MediaState.Close; // Close the media
        }

        private void sendToMCEBuddy_Click(object sender, RoutedEventArgs e)
        {
            CheckAndStopPlay(); // If we are playing something first stop it

            // First save the EDL File if not saved
            if (!_savedEDLCuts && (edlCuts.Items.Count > 0))
            {
                MessageBox.Show(Localise.GetPhrase("First save the cuts to an EDL file"), Localise.GetPhrase("Unsaved EDL Cuts"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            // Find out which port MCEBuddy is running on (assuming local machine is running Custom Cuts)
            // TODO: Do we support remote machines for Custom Cuts?
            // Connect to the engine
            ICore proxy = ConnectToMCEBuddyEngine();
            
            // Send the file to MCEBuddy for processing
            if (proxy != null)
            {
                if (Util.Net.IsUNCPath(Util.Net.GetUNCPath(mediaPlayer.Source.LocalPath))) // check if the files are on a remote computer
                    MessageBox.Show(Localise.GetPhrase("Networked files will be accessed using the logon credentials of the MCEBuddy Service, not the currently logged on user. You can manually specify the network credentials from the Settings -> Expert Settings page in MCEBuddy."), Localise.GetPhrase("Remote Network File Detected"), MessageBoxButton.OK, MessageBoxImage.Warning);
                if (!AddFileToMCEBuddyQueue(proxy, mediaPlayer.Source.LocalPath))
                {
                    MessageBox.Show(Localise.GetPhrase("Unable to add video file to MCEBuddy queue for processing"), Localise.GetPhrase("Connection Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show(Localise.GetPhrase("Unable to connect to the MCEBuddy Engine on local machine"), Localise.GetPhrase("Connection Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(Localise.GetPhrase("Added video file to MCEBuddy queue for processing"), Localise.GetPhrase("Sent for Processing"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void displayGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_initializing) // We aren't ready yet
                return;

            this.Height = displayGrid.ActualHeight + gridHeightMargin; // Resize as much as we can without invoking the scrollbars
            this.Width = displayGrid.ActualWidth + gridWidthMargin;
        }
    }
}
