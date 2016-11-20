using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.ServiceModel;
using System.Runtime.InteropServices;
using System.ServiceModel.Description;

using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.ProgressODoom;
using MCEBuddy.Configuration;

namespace MCEBuddy.GUI
{
    public partial class StatusForm : Form
    {
        #region GlobalGUIVariables
        public static List<string[]> profilesSummary; // A 2 dimensional string List containing the Profile name and Description for all profiles in profiles.conf
        public static bool showAnalyzerInstalled; // Is showanalyzer installed on engine system
        public static int engineProcessorCount; // Number of processors on the engine
        #endregion

        #region Imports

        //This section handles the auto-scrolling when an item is dragged to the top or bottom of the list
        [DllImport("user32", EntryPoint = "SendMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int SendMessage(int hwnd, int wMsg, int wParam, ref object lParam);
        
        #endregion

        #region Variables
        
        private ICore _pipeProxy = null;
        private volatile bool _isEngineConnected = false;
        private Thread _tryConnectThread;
        private bool _exit = false;
        private ProgressODoom.ProgressBarEx[] _jobStatus;
        private Label[] _jobLabel;
        private int _maxActiveJobs = 0;
        private int _refreshGUIPeriod = GlobalDefs.GUI_REFRESH_PERIOD; // Refresh GUI frequency
        private int _connectPeriod = GlobalDefs.LOCAL_ENGINE_POLL_PERIOD; // Reconnect/get data from remote engine frequency (default is TCP polling period)
        private float _scale = 1;
        private int _separation = 45;
        private int _jobLabelHeight = 15;
        private int _jobStatusHeight = 20;
        private int _priorityBoxPageEndGap;
        private float _pixelsPerCharacter = (float)8.5;
        private string processPriority;
        private DateTime _lastUpdateCheck = DateTime.Now.AddDays(-1);
        private System.Windows.Forms.Timer _announcementTextScrollTimer = new System.Windows.Forms.Timer();
        private string _announcementText = "";
        private string _announcementLink = "";
        private int _announcementIndex = 0;
        private string[,] _announcementInfo = new string[2, 3]; // [Announcement, Link, ToolTip] for each entry
        private int _announcementTextScrollPosition = 0;
        private string _lastProcessPriority;
        private const int STATUS_MAX_CHARS = 60; // maximum characters on the job status bar
        private Object _configLock = new Object(); // Object to lock for configParameters sync
        private NotifyIcon _notifyIcon;
        private bool _versionMismatch = false;
        private List<JobStatus> _jobs = null;
        private bool _isEngineRunning = false;
        private bool _isEngineSuspended = false;
        private bool _isEngineCrashed = false;
        private bool _startEngineOnInit = false;
        private bool _startMinimizedOnInit = false;
        private bool _isWithinConversionTimes = false;
        private int _maxCharsLabel = 29;
        private float _queueFileColumnWidthRatio, _queueTaskColumnWidthRatio;
        private static UpdateCheck _updateCheck = new UpdateCheck(); // Check for new version, only one instance for all GUI's
        private bool _newUpdateDownloaded = false;
        private bool _forceQueueDisplayRefresh = false;

        // The LVItem being dragged
        private ListViewItem _itemDnD = null;
        private int intScrollDirection;
        private System.Windows.Forms.Timer withEventsField_tmrLVScroll;
        private System.Windows.Forms.Timer tmrLVScroll
        {
            get { return withEventsField_tmrLVScroll; }
            set
            {
                if (withEventsField_tmrLVScroll != null)
                {
                    withEventsField_tmrLVScroll.Tick -= tmrLVScroll_Tick;
                }
                withEventsField_tmrLVScroll = value;
                if (withEventsField_tmrLVScroll != null)
                {
                    withEventsField_tmrLVScroll.Tick += tmrLVScroll_Tick;
                }
            }
        }
        
        #endregion

        public StatusForm(string[] options)
        {
            // Check user options on start up
            foreach (string arg in options)
            {
                switch (arg.ToLower().Trim())
                {
                    case "/startengine":
                        _startEngineOnInit = true;
                        break;

                    case "/startmin":
                        _startMinimizedOnInit = true;
                        break;

                    default:
                        break;
                }
            }

            InitializeComponent();

            // First get the new scale
            using (Graphics g = this.CreateGraphics())
            {
                _scale = g.DpiX / 96; // Get the system DPI (font scaling)
            }

            // Factor in the page end size (for scaling)
            _priorityBoxPageEndGap = this.Height - priorityBox.Location.Y - priorityBox.Size.Height;

            // Rescale the columns - for some reason Windows does not rescale the columns in high DPI mode
            this.fileName.Width = Rescale(this.fileName.Width);
            this.taskName.Width = Rescale(this.taskName.Width);

            _queueFileColumnWidthRatio = (float)currentQueue.Columns["fileName"].Width / (float)currentQueue.Width; // Save the ratio of each column (there may be some empty space at the end
            _queueTaskColumnWidthRatio = (float) currentQueue.Columns["taskName"].Width / (float) currentQueue.Width; // Save the ratio of each column (there may be some empty space at the end

            // Setup code for minimizing to tray
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = Icon.FromHandle((global::MCEBuddy.GUI.Properties.Resources.MCEBuddy_Icon).GetHicon());
            _notifyIcon.Visible = true;
            _notifyIcon.Click +=
                delegate(object sender, EventArgs args)
                {
                    this.Refresh(); // Repaint the screen
                    _forceQueueDisplayRefresh = true; // Refersh the queue contents once
                    this.Show();
                    this.ShowInTaskbar = true;
                    this.WindowState = FormWindowState.Normal;
                };
            _notifyIcon.DoubleClick +=
                delegate(object sender, EventArgs args)
                {
                    if (this.WindowState == FormWindowState.Normal) // Bring to front if it's open
                    {
                        this.WindowState = FormWindowState.Minimized;
                        this.Refresh(); // Repaint the screen
                        _forceQueueDisplayRefresh = true; // Refersh the queue contents once
                        this.Show();
                        this.WindowState = FormWindowState.Normal;
                        this.BringToFront();
                        this.Focus();
                    }
                    else // Open window if minimized
                    {
                        this.Refresh(); // Repaint the screen
                        _forceQueueDisplayRefresh = true; // Refersh the queue contents once
                        this.Show();
                        this.ShowInTaskbar = true;
                        this.WindowState = FormWindowState.Normal;
                    }
                };

            //Initialize Scrolling Timer
            tmrLVScroll = new System.Windows.Forms.Timer();
            tmrLVScroll.Enabled = false;
            tmrLVScroll.Interval = GlobalDefs.CURRENT_QUEUE_SCROLL_PERIOD;

            MCEBuddyConf.GlobalMCEConfig = new MCEBuddyConf(); // Initialize with default parameters for now, we will get the config file from the server and then re-initialize (don't use null as it keeps accessing win.ini) - this is never written to a file (just a memory object)
        }

        private void StatusForm_Load(object sender, EventArgs e)
        {
            // Check for changes in screen resolution and screen changes (multi monitor support) - this HAS to be in Load and NOT in the constructor since the form has not been initiatized yet
            this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations

            // Set the version properties (Versioning is driven as Major.Minor.Build.Revision)
            MajorTitle.Text = "MCEBuddy " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Revision == 0) // BETA versions have revision code (Major.Minor.Build.Revision) as 0
                BetaTitle.Text = "Beta " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Build.ToString(System.Globalization.CultureInfo.InvariantCulture);
            else
                BetaTitle.Text = "Release " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Build.ToString(System.Globalization.CultureInfo.InvariantCulture);

            Ini tempIni = new Ini(GlobalDefs.TempSettingsFile);
            // Check if this is the first time the GUI is start after an installation
            if (tempIni.ReadBoolean("GUI", "FirstStartComplete", false) == false)
            {
                MessageBox.Show(Localise.GetPhrase("Rest your mouse on any button, box or item to get Instant Help on it.\n\nFor more information on how to use, troubleshoot or get support for MCEBuddy click on the <Getting Started> link on the top right corner.\n\nDonations are always welcome :) Enjoy!\n\n  - Ramit, Derek & Goose"), Localise.GetPhrase("Welcome to MCEBuddy"));
                tempIni.Write("GUI", "FirstStartComplete", true);
            }

            // Check for server location and start the thread to keep the UI connected to the MCEBuddy engine
            string remoteServerName = tempIni.ReadString("Engine", "RemoteServerName", GlobalDefs.MCEBUDDY_SERVER_NAME);
            if (remoteServerName != GlobalDefs.MCEBUDDY_SERVER_NAME) // check if it's a remote machine
                _connectPeriod = GlobalDefs.REMOTE_ENGINE_POLL_PERIOD; // remote machine needs be pinged slower

            // If this thread is not the thread that created the control, we'll invoke a callback in a threadsafe way to fix the control
            _tryConnectThread = new Thread(TryConnect);
            _tryConnectThread.IsBackground = true; // Kill this thread if the process is closed
            _tryConnectThread.CurrentCulture = _tryConnectThread.CurrentUICulture = Localise.MCEBuddyCulture;
            _tryConnectThread.Start();

            // Start the announcement text scroll timer
            _announcementTextScrollTimer.Tick += new EventHandler(this.ScrollAnnouncementLabel);
            _announcementTextScrollTimer.Interval = 250;
            _announcementTextScrollTimer.Start();

            // Now start the core background job status thread
            backgroundUpdate.WorkerSupportsCancellation = true;
            backgroundUpdate.RunWorkerAsync();

            this.Show(); // Show the form
        }

        /// <summary>
        /// Sets the localization for the form. NEVER CALL THIS FROM WITHIN A TRY CATCH STATEMENTS SINCE IT CAN THROW AN EXCEPTION FOR APPLICATION RESTART.
        /// This function is Cross UI thread access safe
        /// </summary>
        /// <exception cref="Application Restart">Application Restart</exception>
        private void SetUserLocale()
        {
            // We always need to start with english as the locale otherwise localization will fail
            if (!startCmd.Text.Contains("Start") && (Application.CurrentCulture.Name != MCEBuddyConf.GlobalMCEConfig.GeneralOptions.locale) && !_exit) // We are not currently in English AND we are looking to change the current culture, we need to restart
            {
                _exit = true;

                if (this.InvokeRequired) // Since we call this from a thread other then the background worker thread, we need to check for invocation otherwise the FormClosing will throw a cross thread exception while closing
                {
                    this.Invoke(new MethodInvoker(delegate { Application.Restart(); }));
                }
                else
                    Application.Restart(); // Since the existing text on the controls is not English, we need to restart the app to reload the English text so the phrases can be compared against it
            }

            // If there is no locale specified use the local USER locale. 
            // This is to help fix any issues coming from the service using the system locale and the user using the user's locale where they are different
            string locale = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.locale;

            // Now initialize the locale engine to load the strings
            Localise.Init(locale);

            // If this thread is not the thread that created the control, we'll invoke a callback in a threadsafe way to fix the control
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate { LocaliseForms.LocaliseForm(this, toolTip, fileContextMenuStrip); }));
            }
            else
                LocaliseForms.LocaliseForm(this, toolTip, fileContextMenuStrip);

            Application.CurrentCulture = Localise.MCEBuddyCulture;
        }

        /// <summary>
        /// Rescales the points coordinates to compensate for the large font/dpi increase
        /// </summary>
        private int Rescale(int dimension)
        {
            return (int) (dimension*_scale);
        }

        /// <summary>
        /// Rescales the points coordinates to compensate for the large font/dpi increase
        /// </summary>
        private float Rescale(float dimension)
        {
            return (float)(dimension * _scale);
        }

        /// <summary>
        /// Enables the controls on the form. Do NOT call this function from within a Try Catch statement since it does an Application.Restart.
        /// This should only be called from Backgroundworker ProgressChanged since it access UI elements.
        /// </summary>
        private void EnableControls()
        {
            if (!_isEngineConnected)
                return; // We only enable controls if the engine is connected

            if (this.WindowState == FormWindowState.Minimized)
                return; // We don't need to burn CPU cycles updating the GUI if it's minimized

            // Someone changed the configuration remotely or DisableControls was called and the GUI needs to be restart and reset otherwise the size gets messed up
            if ((_maxActiveJobs > 0) && (_maxActiveJobs != MCEBuddyConf.GlobalMCEConfig.GeneralOptions.maxConcurrentJobs) && !_exit)
            {
                _exit = true;
                Application.Restart();
            }

            if (_jobStatus == null) // Do we need to setup the job objects
            {
                _maxActiveJobs = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.maxConcurrentJobs;

                // Before doing anything, unanchor the bottom control from the bottom and anchor to the top otherwise is messes up the layout
                AnchorBottomControls(AnchorStyles.Bottom, false);
                AnchorBottomControls(AnchorStyles.Top, true);
                currentQueue.Anchor &= ~AnchorStyles.Bottom; // Unhinge the bottom to avoid issues

                // Set the new height before adding components to avoid GUI issues
                this.Height = priorityBox.Bottom + ((_maxActiveJobs - 1) * Rescale(_separation)) + Rescale(_priorityBoxPageEndGap); // Start with adjusting the form height otherwise control won't fit, end the form relative to the priority box

                // Create new Job Status bars
                _jobStatus = new ProgressBarEx[_maxActiveJobs];
                _jobLabel = new Label[_maxActiveJobs];
                for (int i = 0; i < _maxActiveJobs; i++)
                {
                    // Label for each progress bar
                    _jobLabel[i] = new Label();
                    _jobLabel[i].Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top; // Do this first to avoid issues
                    _jobLabel[i].Location = new System.Drawing.Point((errorMsgLbl.Location.X), (errorMsgLbl.Location.Y) + i * Rescale(_separation));
                    _jobLabel[i].Size = new System.Drawing.Size((errorMsgLbl.Width), Rescale(_jobLabelHeight));
                    _jobLabel[i].TextAlign = ContentAlignment.MiddleCenter;
                    _jobLabel[i].Visible = true;
                    this.Controls.Add(_jobLabel[i]);

                    // Progress bars
                    _jobStatus[i] = new ProgressODoom.ProgressBarEx();
                    _jobStatus[i].Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top; // Do this first to avoid issues
                    _jobStatus[i].Location = new System.Drawing.Point((errorMsgLbl.Location.X), (errorMsgLbl.Location.Y) + i * Rescale(_separation) + Rescale(_jobLabelHeight)); // just below the label
                    _jobStatus[i].Size = new System.Drawing.Size((errorMsgLbl.Width), Rescale(_jobStatusHeight));
                    _jobStatus[i].BackgroundPainter = this.plainBackgroundPainter;
                    _jobStatus[i].BorderPainter = this.plainBorderPainter;
                    _jobStatus[i].MarqueePercentage = 25;
                    _jobStatus[i].MarqueeSpeed = 100;
                    _jobStatus[i].MarqueeStep = 1;
                    _jobStatus[i].Maximum = 100;
                    _jobStatus[i].Minimum = 0;
                    _jobStatus[i].Name = "JobStatus" + i.ToString();
                    _jobStatus[i].ProgressPadding = 0;
                    _jobStatus[i].ProgressPainter = this.plainProgressPainter;
                    _jobStatus[i].ProgressType = ProgressODoom.ProgressType.Smooth;
                    _jobStatus[i].ShowPercentage = false;
                    _jobStatus[i].TabIndex = errorMsgLbl.TabIndex; // same tab index as the error message box
                    _jobStatus[i].Value = 0;
                    _jobStatus[i].ForeColor = Color.Brown;
                    _jobStatus[i].Visible = true;
                    this.Controls.Add(_jobStatus[i]);
                }

                // Set component height
                settingsCmd.Top += (_maxActiveJobs - 1) * Rescale(_separation);
                rescanCmd.Top += (_maxActiveJobs - 1) * Rescale(_separation);
                closeCmd.Top += (_maxActiveJobs - 1) * Rescale(_separation);
                priorityBox.Top += (_maxActiveJobs - 1) * Rescale(_separation);
                PriorityLabel.Top += (_maxActiveJobs - 1) * Rescale(_separation);
                remoteEngineCmd.Top += (_maxActiveJobs - 1) * Rescale(_separation);
                serverInfoLbl.Top += (_maxActiveJobs - 1) * Rescale(_separation);
                this.MinimumSize = new Size(this.MinimumSize.Width, this.MinimumSize.Height + (_maxActiveJobs - 1) * Rescale(_separation)); // Do this after increasing the height

                // Now we are done, anchor the control as best required
                if (this.Height >= this.MaximumSize.Height) // We are at maximum size and we are scrolling, anchor to the top otherwise control will dissappear
                {
                    AnchorBottomControls(AnchorStyles.Bottom, false);
                    AnchorBottomControls(AnchorStyles.Top, true);
                }
                else // We can expand, anchor to the bottom
                {
                    AnchorBottomControls(AnchorStyles.Bottom, true);
                    AnchorBottomControls(AnchorStyles.Top, false);
                }
                currentQueue.Anchor |= AnchorStyles.Bottom; // Rehinge the bottom to expand
            }

            errorMsgLbl.Visible = false;

            if (_isEngineRunning)
            {
                startCmd.Enabled = 
                    stopCmd.Enabled =
                    currentQueue.Enabled = 
                    addFileCmd.Enabled =
                    cancelFileCmd.Enabled =
                    rescanCmd.Enabled =
                    settingsCmd.Enabled =
                    priorityBox.Enabled = true;

                settingsCmd.Image = global::MCEBuddy.GUI.Properties.Resources.stop;

                if (!_isEngineSuspended) // Engine is suspended
                {
                    startCmd.Text = Localise.GetPhrase("Pause"); // Allow for pausing
                    startCmd.Image = global::MCEBuddy.GUI.Properties.Resources.media_button_pause_yellow;
                    toolTip.SetToolTip(this.startCmd, Localise.GetPhrase("Pause conversion tasks"));
                }
                else
                {
                    startCmd.Text = " " + Localise.GetPhrase("Resume"); // User has clicked pause
                    startCmd.Image = global::MCEBuddy.GUI.Properties.Resources.media_play_green;
                    toolTip.SetToolTip(this.startCmd, Localise.GetPhrase("Resume conversion tasks"));
                }
            }
            else
            {
                if (_isEngineCrashed) // Check if the Engine has stopped due to a crash
                {
                    errorMsgLbl.Text = Localise.GetPhrase("The MCEBuddy Engine has crashed. Please send mcebuddy.log to the developers");
                    errorMsgLbl.Visible = true;
                }
                else
                    errorMsgLbl.Visible = false;

                startCmd.Enabled =
                    priorityBox.Enabled =
                    settingsCmd.Enabled = true;

                stopCmd.Enabled =
                    currentQueue.Enabled =
                    addFileCmd.Enabled = 
                    cancelFileCmd.Enabled = 
                    rescanCmd.Enabled = false;

                currentQueue.Items.Clear();
                settingsCmd.Image = global::MCEBuddy.GUI.Properties.Resources.control_panel;

                startCmd.Text = Localise.GetPhrase("Start"); // Reset the text
                startCmd.Image = global::MCEBuddy.GUI.Properties.Resources.media_play_green;
                toolTip.SetToolTip(this.startCmd, Localise.GetPhrase("Start the MCEBuddy monitor and conversion tasks"));
            }
        }

        private void DisableControls()
        {
            if (_isEngineConnected) return; // We only disable controls if the engine is not connected

            if (this.WindowState == FormWindowState.Minimized) return; // We don't need to burn CPU cycles updating the GUI if it's minimized

            if (_jobStatus != null)
            {
                // Before doing anything, unanchor the bottom control from the bottom and anchor to the top otherwise is messes up the layout
                AnchorBottomControls(AnchorStyles.Bottom, false);
                AnchorBottomControls(AnchorStyles.Top, true);
                currentQueue.Anchor &= ~AnchorStyles.Bottom; // Unhinge the bottom to avoid issues

                for (int i = 0; i < _jobStatus.Length; i++)
                {
                    this.Controls.Remove(_jobStatus[i]);
                    this.Controls.Remove(_jobLabel[i]);
                    _jobStatus[i] = null;
                    _jobLabel[i] = null;
                }
                _jobStatus = null;

                // Reset the component heights
                settingsCmd.Top -= (_maxActiveJobs - 1) * Rescale(_separation);
                rescanCmd.Top -= (_maxActiveJobs - 1) * Rescale(_separation);
                closeCmd.Top -= (_maxActiveJobs - 1) * Rescale(_separation);
                priorityBox.Top -= (_maxActiveJobs - 1) * Rescale(_separation);
                PriorityLabel.Top -= (_maxActiveJobs - 1) * Rescale(_separation);
                remoteEngineCmd.Top -= (_maxActiveJobs - 1) * Rescale(_separation);
                serverInfoLbl.Top -= (_maxActiveJobs - 1) * Rescale(_separation);
                this.MinimumSize = new Size(this.MinimumSize.Width, this.MinimumSize.Height - (_maxActiveJobs - 1) * Rescale(_separation)); // Do this before reducing the height
                this.Height = priorityBox.Bottom + Rescale(_priorityBoxPageEndGap); // Last thing, downsize the form, end of form relative to priority box bottom

                // Now we are done, anchor the control as best required
                if (this.Height >= this.MaximumSize.Height) // We are at maximum size and we are scrolling, anchor to the top otherwise control will dissappear
                {
                    AnchorBottomControls(AnchorStyles.Bottom, false);
                    AnchorBottomControls(AnchorStyles.Top, true);
                }
                else // We can expand, anchor to the bottom
                {
                    AnchorBottomControls(AnchorStyles.Bottom, true);
                    AnchorBottomControls(AnchorStyles.Top, false);
                }
                currentQueue.Anchor |= AnchorStyles.Bottom; // Rehinge the bottom to expand
            }

            if (_versionMismatch)
                errorMsgLbl.Text = Localise.GetPhrase("Version mismatch. Please ensure that the MCEBuddy client and engine are running the same version.");
            else
                errorMsgLbl.Text = Localise.GetPhrase("The MCEBuddy service is unavailable. Please start from Windows control panel or check engine connection.");
                
            errorMsgLbl.Visible = true;

            startCmd.Enabled = 
                settingsCmd.Enabled = 
                priorityBox.Enabled = false;

            stopCmd.Enabled = 
                addFileCmd.Enabled = 
                cancelFileCmd.Enabled = 
                currentQueue.Enabled = 
                rescanCmd.Enabled = false;

            currentQueue.Items.Clear();

            settingsCmd.Image = global::MCEBuddy.GUI.Properties.Resources.control_panel;
        }

        private void AnchorBottomControls(AnchorStyles anchorLocation, bool set)
        {
            if (set)
            {
                if (_jobStatus != null)
                {
                    for (int i = 0; i < _maxActiveJobs; i++)
                    {
                        _jobLabel[i].Anchor |= anchorLocation;
                        _jobStatus[i].Anchor |= anchorLocation;
                    }
                }

                errorMsgLbl.Anchor |= anchorLocation;
                settingsCmd.Anchor |= anchorLocation;
                rescanCmd.Anchor |= anchorLocation;
                closeCmd.Anchor |= anchorLocation;
                priorityBox.Anchor |= anchorLocation;
                PriorityLabel.Anchor |= anchorLocation;
                remoteEngineCmd.Anchor |= anchorLocation;
                serverInfoLbl.Anchor |= anchorLocation;
            }
            else
            {
                if (_jobStatus != null)
                {
                    for (int i = 0; i < _maxActiveJobs; i++)
                    {
                        _jobLabel[i].Anchor &= ~anchorLocation;
                        _jobStatus[i].Anchor &= ~anchorLocation;
                    }
                }

                errorMsgLbl.Anchor &= ~anchorLocation;
                settingsCmd.Anchor &= ~anchorLocation;
                rescanCmd.Anchor &= ~anchorLocation;
                closeCmd.Anchor &= ~anchorLocation;
                priorityBox.Anchor &= ~anchorLocation;
                PriorityLabel.Anchor &= ~anchorLocation;
                remoteEngineCmd.Anchor &= ~anchorLocation;
                serverInfoLbl.Anchor &= ~anchorLocation;
            }
        }

        /// <summary>
        /// Download the latest updates for the links and announcements from the website
        /// </summary>
        private void DownloadUpdates()
        {
            _updateCheck.Check(false); // True since we are calling from a background thread and not the GUI thread
            _newUpdateDownloaded = true; // We have a new update
        }

        /// <summary>
        /// Check the local file for updates downloaded earlier
        /// </summary>
        private void CheckForUpdates()
        {
            if (!_newUpdateDownloaded) // Nothing to process
                return;
            else
                _newUpdateDownloaded = false; // We have processed this new update

            Monitor.Enter(_announcementTextScrollTimer); // Take the lock before we modify anything

            Ini configIni = new Ini(GlobalDefs.TempSettingsFile);

            // 1st is Announcement [Announcement, Link, ToolTip]
            string announcement = configIni.ReadString("Engine", "Announcement", "");
            string announcementLink = configIni.ReadString("Engine", "AnnouncementLink", "");
            _announcementInfo[0, 0] = announcement;
            _announcementInfo[0, 1] = announcementLink;
            if (String.IsNullOrWhiteSpace(announcementLink))
                _announcementInfo[0, 2] = ""; // Clear the pop up tooltip
            else
                _announcementInfo[0, 2] = Localise.GetPhrase("Click here to open link"); // Set a pop up tooltip

            // 2nd is Version [Announcement, Link, ToolTip]
            //Get the version of this app
            Version CurrentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            // Check for any new version and write to form if required (this comes in the end, it trumps all annoucements
            Version LatestVersion = CurrentVersion;
            string LatestVersionString = configIni.ReadString("Engine", "LatestVersion", "");

            if (!String.IsNullOrWhiteSpace(LatestVersionString))
                LatestVersion = new Version(LatestVersionString);

            if (LatestVersion > CurrentVersion)
            {
                string MCEBuddyVersion = LatestVersion.Major.ToString(CultureInfo.InvariantCulture) + "." + LatestVersion.Minor.ToString(CultureInfo.InvariantCulture) + "." + LatestVersion.Build.ToString(CultureInfo.InvariantCulture);
                if (LatestVersion.Revision == 1) //Revision version 1 = Release version else BETA version
                    _announcementInfo[1, 0] = Localise.GetPhrase("New version") + " " + MCEBuddyVersion + " " + Localise.GetPhrase("available");
                else
                    _announcementInfo[1, 0] = Localise.GetPhrase("New Beta version") + " " + MCEBuddyVersion + " " + Localise.GetPhrase("available");
                _announcementInfo[1, 1] = Crypto.Decrypt(GlobalDefs.MCEBUDDY_DOWNLOAD_NEW_VERSION);
                _announcementInfo[1, 2] = Localise.GetPhrase("Click here to download new version of MCEBuddy"); // Set a pop up tooltip
            }
            else
                _announcementInfo[1, 0] = _announcementInfo[1, 1] = _announcementInfo[1, 2] = "";

            Monitor.Exit(_announcementTextScrollTimer);
        }

        /// <summary>
        /// Used to scroll the text in the Announcement Label, called from timer
        /// </summary>
        private void ScrollAnnouncementLabel(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                return; // Don't waste CPU cycles if the form is minimized

            if (String.IsNullOrWhiteSpace(_announcementText))
            {
                LoadNextAnnouncement(); // Get the next announcement
                if (String.IsNullOrWhiteSpace(_announcementText)) // it's empty forget it
                    return;
            }

            string displayText = new String(' ', _maxCharsLabel) + _announcementText + new String(' ', _maxCharsLabel);
            _announcementTextScrollPosition++;
            int iLmt = displayText.Length - _announcementTextScrollPosition;
            if (iLmt < _maxCharsLabel)
            {
                LoadNextAnnouncement(); // Load the next announcement
                if (String.IsNullOrWhiteSpace(_announcementText)) // it's empty forget it
                    return;
                displayText = new String(' ', _maxCharsLabel) + _announcementText + new String(' ', _maxCharsLabel); // reload with new text
            }

            announcementLabel.Text = displayText.Substring(_announcementTextScrollPosition, _maxCharsLabel);
        }

        private void LoadNextAnnouncement()
        {
            Monitor.Enter(_announcementTextScrollTimer);

            _announcementIndex++;
            if (_announcementIndex >= _announcementInfo.GetLength(0)) // Should not exceed the max number of announcements
                _announcementIndex = 0; // Reset it back to the first announcement

            announcementLabel.Visible = false; // Label is disabled by default, will be enabled if required

            _announcementText = _announcementInfo[_announcementIndex, 0];
            _announcementLink = _announcementInfo[_announcementIndex, 1];
            toolTip.SetToolTip(announcementLabel, _announcementInfo[_announcementIndex, 2]); // Set/Clear the pop up tooltip

            if (!String.IsNullOrWhiteSpace(_announcementText)) // If we have something, then we show the label
                announcementLabel.Visible = true;

            _announcementTextScrollPosition = 0; // Reset the scroll position

            Monitor.Exit(_announcementTextScrollTimer);
        }

        private void announcementLabel_Click(object sender, EventArgs e)
        {
            Internet.OpenLink(_announcementLink, "MCEBuddy GUI");
        }

        /// <summary>
        /// Tries to keep connected to the MCEBuddy Engine over TCP/IP. Once connected it downloads the MCEBuddy Config options from the server.
        /// </summary>
        /// <returns>True if it is connected to the Pipe</returns>
        private void TryConnect()
        {
            while (!_exit) // Keep trying to connect as long as app is running
            {
                bool configLockTaken = false; // Flag to see state of lock
                
                try
                {
                    Monitor.Enter(_configLock, ref configLockTaken); // Take a lock before getting settings, incase Settings are being changed by the user
                    if (_pipeProxy.ServiceShutdownBySystem()) // Has the system initated shutdown, if so we need to shutdown the GUI (to avoid uninstall issues)
                    {
                        Monitor.Exit(_configLock);
                        configLockTaken = false;
                        _exit = true; // We are done, release all threads and resources
                        this.Close(); // Close the GUI
                        continue;
                    }
                    MCEBuddyConf.GlobalMCEConfig = new MCEBuddyConf(_pipeProxy.GetConfigParameters()); // Get the parameters from the Engine we are connected to
                    _jobs = _pipeProxy.GetAllJobsInQueueStatus();
                    _isEngineRunning = _pipeProxy.EngineRunning();
                    _isEngineSuspended = _pipeProxy.IsSuspended();
                    _isEngineCrashed = _pipeProxy.EngineCrashed();
                    _isWithinConversionTimes = _pipeProxy.WithinConversionTimes();
                    Monitor.Exit(_configLock);
                    configLockTaken = false;
                    _isEngineConnected = true; // All good, we are connected

                    Thread.Sleep(_connectPeriod * (WindowState == FormWindowState.Minimized ? GlobalDefs.GUI_MINIMIZED_ENGINE_POLL_SLOW_FACTOR : 1)); // Check again in x seconds, refresh y times slower
                    continue;
                }
                catch (Exception) // Whoops pipe broken!
                {
                    if (configLockTaken)
                    {
                        Monitor.Exit(_configLock); // Release the lock
                        configLockTaken = false;
                    }
                    _isWithinConversionTimes = _isEngineConnected = _isEngineCrashed = _isEngineRunning = _isEngineSuspended = false; // We are no longer connected
                    _pipeProxy = null; // We should start over
                }

                try // Try to reconnect
                {
                    Ini tempIni = new Ini(GlobalDefs.TempSettingsFile);
                    string remoteServerName = tempIni.ReadString("Engine", "RemoteServerName", GlobalDefs.MCEBUDDY_SERVER_NAME);
                    int remoteServerPort = tempIni.ReadInteger("Engine", "RemoteServerPort", int.Parse(GlobalDefs.MCEBUDDY_SERVER_PORT));

                    if (serverInfoLbl.InvokeRequired) // Cannot access UI elements directly from a non UI thread
                        serverInfoLbl.Invoke(new MethodInvoker(delegate { serverInfoLbl.Text = "Engine: " + remoteServerName + "  Port: " + remoteServerPort.ToString(); }));
                    else
                        serverInfoLbl.Text = "Engine: " + remoteServerName + "  Port: " + remoteServerPort.ToString();

                    Monitor.Enter(_configLock, ref configLockTaken);
                    ICore tempPipeProxy = MCEBuddyEngineConnect.ConnectToRemoteEngine(remoteServerName, remoteServerPort);
                    MCEBuddyConf.GlobalMCEConfig = new MCEBuddyConf(tempPipeProxy.GetConfigParameters()); // Get the parameters from the Engine we are connected to
                    _jobs = tempPipeProxy.GetAllJobsInQueueStatus();
                    _isEngineRunning = tempPipeProxy.EngineRunning();
                    _isEngineSuspended = tempPipeProxy.IsSuspended();
                    _isEngineCrashed = tempPipeProxy.EngineCrashed();
                    _isWithinConversionTimes = tempPipeProxy.WithinConversionTimes();
                    Monitor.Exit(_configLock);
                    configLockTaken = false;

                    // all good again, update the status
                    _pipeProxy = tempPipeProxy;
                    _versionMismatch = false; // set after pipeProxy
                    _isEngineConnected = true;
                }
                catch (Exception e)
                {
                    // Check for version mistmatch
                    if ((e.InnerException is System.Runtime.Serialization.SerializationException) || (e is System.ServiceModel.ActionNotSupportedException))
                        _versionMismatch = true;
                    else
                        _versionMismatch = false;

                    //WriteEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to create pipe to MCEBuddy service"), EventLogEntryType.Warning); //not required since this call will fail when MCEBuddy Service has not started and will overflow the eventlog. The message will be shown on the GUI
                    if (configLockTaken)
                    {
                        Monitor.Exit(_configLock);
                        configLockTaken = false;
                    }
                    _isWithinConversionTimes = _isEngineConnected = _isEngineCrashed = _isEngineRunning = _isEngineSuspended = false; // Still broken
                    _pipeProxy = null; // Last item

                    Thread.Sleep(_connectPeriod * (WindowState == FormWindowState.Minimized ? GlobalDefs.GUI_MINIMIZED_ENGINE_POLL_SLOW_FACTOR : 1)); // Try to connect every x seconds
                }

                if (_isEngineConnected) // SetUserLocale should never be called from within a try catch statement since it can cause an exception if there is an application restart
                    SetUserLocale(); // ReInitialize MCEBuddy (we may have been disconnected and parameters changed, connected to a new engine etc)
            }
        }

        /// <summary>
        /// Background worker thread, waits until the app exits and keep calling ProgressChanged
        /// </summary>
        private void backgroundUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_exit)
            {
                backgroundUpdate.ReportProgress(1);
                Thread.Sleep(_refreshGUIPeriod);
            }
        }

        /// <summary>
        /// Called by system timer to update the GUI status
        /// </summary>
        private void backgroundUpdate_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (DateTime.Now > _lastUpdateCheck.AddHours(1)) //check once in 1 hour for any updates to MCEBuddy
            {
                _lastUpdateCheck = DateTime.Now;
                // Run as thread so as not to block the GUI for a slow connection
                Thread updateThread = new Thread(DownloadUpdates);
                updateThread.IsBackground = true; // Kill this thread if the process is closed
                updateThread.SetApartmentState(ApartmentState.STA);
                updateThread.CurrentCulture = updateThread.CurrentUICulture = Localise.MCEBuddyCulture;
                updateThread.Start();
            }

            CheckForUpdates(); // Check if we have new updates

            if (!_isEngineConnected)
            {
                _notifyIcon.Text = Localise.GetPhrase("MCEBuddy service not connected");
                this.Text = Localise.GetPhrase("MCEBuddy Status") + " - " + Localise.GetPhrase("MCEBuddy service not connected");
                DisableControls();
                return;
            }
            else
                EnableControls(); // Enable the controls

            try
            {
                // Check if the user requested an engine start by default
                if (_startEngineOnInit)
                {
                    _pipeProxy.Start();
                    _startEngineOnInit = false; // done
                }

                if (_isEngineRunning)
                {
                    if (_isEngineSuspended)
                        _notifyIcon.Text = Localise.GetPhrase("MCEBuddy PAUSED");
                    else if (_jobs.Count == 0)
                        _notifyIcon.Text = Localise.GetPhrase("MCEBuddy idle");
                    else
                        _notifyIcon.Text = _jobs.Count.ToString(CultureInfo.InvariantCulture) + " " + Localise.GetPhrase("conversions in progress");

                    if (this.WindowState != FormWindowState.Minimized) // We don't need to burn CPU cycles updating the GUI if it's minimized
                    {
                        if ((_jobs.Count != currentQueue.Items.Count) || _forceQueueDisplayRefresh) // Update the queue only if required
                        {
                            currentQueue.Items.Clear();
                            int count = 0;
                            foreach (JobStatus job in _jobs) // each list item contains 2 strings (File Name, Task Name)
                            {
                                string[] fn = new string[currentQueue.Columns.Count];
                                fn[currentQueue.Columns["fileName"].Index] = Path.GetFileName(job.SourceFile);
                                fn[currentQueue.Columns["taskName"].Index] = job.TaskName;
                                ListViewItem queueItem = new ListViewItem(fn); // Get only the filename, strip out the path

                                if (count++ < _maxActiveJobs)
                                    queueItem.ForeColor = Color.DarkGreen;
                                else
                                    queueItem.ForeColor = Color.FromArgb(0, 0, 0);

                                currentQueue.Items.Add(queueItem); // Recreate the queue
                            }

                            _forceQueueDisplayRefresh = false; // Done
                        }

                        // Update the Title bar status
                        this.Text = Localise.GetPhrase("MCEBuddy Status");
                        if (_isEngineSuspended) // Tag on paused
                            this.Text += " - " + Localise.GetPhrase("PAUSED");
                        if (_jobs.Count == 0) // Tag on the status
                        {
                            this.Text += " - " + Localise.GetPhrase("idle");
                            if (currentQueue.ShowItemToolTips != false) // don't update forever, it's updating so fast, it doesn't work
                            {
                                currentQueue.ShowItemToolTips = false; // No queue items, show the general tip
                                toolTip.SetToolTip(currentQueue, Localise.GetPhrase("This box shows all the files in queue for conversion.\r\nDrag and drop files here or press the Add button to add files to the conversion queue")); // Restore the general tool tip
                            }
                        }
                        else
                        {
                            this.Text += " - " + _jobs.Count.ToString() + " " + Localise.GetPhrase("conversions in progress"); // Update the Title status
                            if (currentQueue.ShowItemToolTips != true)
                            {
                                toolTip.SetToolTip(currentQueue, null); // Remove the general tooltip, otherwise the ListViewItem tooltip will not show. Set this BEFORE setting the ShowItemToolTips to true otherwise it will fail
                                currentQueue.ShowItemToolTips = true; // Queue items, show the queue item tip
                            }
                        }

                        // Set the job status
                        for (int i = 0; i < _maxActiveJobs; i++)
                        {
                            _jobStatus[i].Visible =
                                _jobLabel[i].Visible = true;

                            try
                            {
                                JobStatus job = _jobs[i];

                                if (job.Cancelled)
                                {
                                    string labelText = Path.GetFileName(job.SourceFile);
                                    if (("Closing ".Length + labelText.Length) > STATUS_MAX_CHARS) labelText = labelText.Substring(0, STATUS_MAX_CHARS - "Closing ".Length);
                                    labelText = Localise.GetPhrase("Closing") + " " + labelText;
                                    _jobLabel[i].Text = labelText;
                                    _jobStatus[i].Text = "";
                                    _jobStatus[i].Value = 0;
                                }
                                else
                                {
                                    if (!_isWithinConversionTimes && _isEngineSuspended) // Conversion will not start - tell the user
                                        job.CurrentAction = Localise.GetPhrase("PAUSED: Check conversion schedule");
                                    else if (_isEngineSuspended)
                                        job.CurrentAction = Localise.GetPhrase("MCEBuddy PAUSED");

                                    string labelText = Path.GetFileName(job.SourceFile);
                                    if ((job.CurrentAction.Length + " - ".Length + labelText.Length) > STATUS_MAX_CHARS) labelText = labelText.Substring(0, Math.Max(0, STATUS_MAX_CHARS - job.CurrentAction.Length - " - ".Length));
                                    if (!String.IsNullOrEmpty(job.CurrentAction)) labelText += " - " + job.CurrentAction;
                                    _jobLabel[i].Text = labelText;
                                    string progressText = "";
                                    int perc = (int)job.PercentageComplete;
                                    if (perc > 0) progressText = perc.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
                                    if (!String.IsNullOrEmpty(job.ETA)) progressText += " - " + job.ETA;
                                    _jobStatus[i].Text = progressText;
                                    _jobStatus[i].Value = (int)job.PercentageComplete;
                                }
                            }
                            catch // If there is no job at this position then it's Idle
                            {
                                if (!_isWithinConversionTimes && _isEngineSuspended) // Conversion will not start - tell the user
                                    _jobLabel[i].Text = Localise.GetPhrase("PAUSED: Check conversion schedule");
                                else
                                    _jobLabel[i].Text = Localise.GetPhrase("Idle");
                                _jobStatus[i].Text = "";
                                _jobStatus[i].Value = 0;
                            }
                        }
                    }
                }
                else
                {
                    if (this.WindowState != FormWindowState.Minimized) // We don't need to burn CPU cycles updating the GUI if it's minimized
                    {

                        // Clear the job status
                        for (int i = 0; i < _maxActiveJobs; i++)
                        {
                            _jobStatus[i].Visible =
                                _jobLabel[i].Visible = false;
                        }
                    }

                    _notifyIcon.Text = Localise.GetPhrase("MCEBuddy stopped");
                    this.Text = Localise.GetPhrase("MCEBuddy Status") + " - " + Localise.GetPhrase("MCEBuddy stopped"); ;
                }

                // Process Priority, if it has changed
                processPriority = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.processPriority;
                if (processPriority != _lastProcessPriority)
                    priorityBox.Text = processPriority;
                _lastProcessPriority = processPriority;
            }
            catch (Exception e1)
            {
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to update tasks"), EventLogEntryType.Warning);
                Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if ( !_exit && (e.CloseReason == CloseReason.UserClosing)) // Prevent closure ONLY if the user closed the form, not for a shutdown or anything else
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
            else
                base.OnFormClosing(e);
        }
        
        private void StatusForm_Closed(object sender, FormClosedEventArgs e)
        {
            _exit = true; // We are done, release all threads and resources

            // Abort any running threads
            backgroundUpdate.CancelAsync();
            backgroundUpdate.Dispose();
            _tryConnectThread.Abort();

            _notifyIcon.Visible = false; // Hide the icon, it'll be cleared later
        }

        private void closeCmd_Click(object sender, EventArgs e)
        {
            _exit = true; // We are done, release all threads and resources

            this.Close();
        }

        private void startCmd_Click(object sender, EventArgs e)
        {
            SetUserLocale(); // Localize this form incase the locale has changed

            try
            {
                if (!stopCmd.Enabled) // This is a start and not a resume
                {
                    startCmd.Enabled = settingsCmd.Enabled = false;
                    _pipeProxy.Start();
                    _isEngineRunning = true; // TODO: Temp hack due to delay in updating this parameter in TryConnect until next fetch
                    // Need to set this or it doesn't work after a start
                    toolTip.SetToolTip(currentQueue, null); // Remove the general tooltip, otherwise the ListViewItem tooltip will not show. Set this BEFORE setting the ShowItemToolTips to true otherwise it will fail
                    currentQueue.ShowItemToolTips = true; // Queue items, show the queue item tip
                }
                else if (!_isEngineSuspended) // We are currently running and user has clicked Pause (ensure we aren't already suspended)
                {
                    startCmd.Text = " " + Localise.GetPhrase("Resume"); // User has clicked pause
                    startCmd.Image = global::MCEBuddy.GUI.Properties.Resources.media_play_green;
                    toolTip.SetToolTip(this.startCmd, Localise.GetPhrase("Resume conversion tasks"));
                    _pipeProxy.SuspendConversion(true); // Suspend the conversion
                    _isEngineSuspended = true; // TODO: Temp hack due to delay in updating this parameter in TryConnect until next fetch
                }
                else // We are currently Paused and the user has clicked Resume
                {
                    startCmd.Text = Localise.GetPhrase("Pause");
                    startCmd.Image = global::MCEBuddy.GUI.Properties.Resources.media_button_pause_yellow;
                    toolTip.SetToolTip(this.startCmd, Localise.GetPhrase("Pause conversion tasks"));
                    _pipeProxy.SuspendConversion(false); // Resume the conversion
                    _isEngineSuspended = false; // TODO: Temp hack due to delay in updating this parameter in TryConnect until next fetch
                }
            }
            catch (Exception e1)
            {
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to start MCEBuddy service"), EventLogEntryType.Warning);
                Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
            }
        }

        private void stopCmd_Click(object sender, EventArgs e)
        {
            try
            {
                startCmd.Text = Localise.GetPhrase("Start"); // Reset the text
                startCmd.Image = global::MCEBuddy.GUI.Properties.Resources.media_play_green;
                toolTip.SetToolTip(this.startCmd, Localise.GetPhrase("Start the MCEBuddy monitor and conversion tasks"));
                
                startCmd.Enabled = 
                    stopCmd.Enabled = 
                    addFileCmd.Enabled = 
                    cancelFileCmd.Enabled = 
                    settingsCmd.Enabled = 
                    rescanCmd.Enabled = false; // Disable to Stop command to prevent a race condition
                
                _pipeProxy.Stop(true); // User asked for it, so preserve state
                _isEngineRunning = false; // TODO: Temp hack due to delay in updating this parameter in TryConnect until next fetch
            }
            catch (Exception e1)
            {
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to stop MCEBuddy service"), EventLogEntryType.Warning);
                Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
            }
        }

        private void cancelFileCmd_Click(object sender, EventArgs e)
        {
            if (currentQueue.SelectedItems.Count > 0)
            {
                try
                {
                    // Build the list of indices to pass
                    int[] idxList = new int[currentQueue.SelectedIndices.Count];
                    for (int i = 0; i < currentQueue.SelectedIndices.Count; i++)
                        idxList[i] = currentQueue.SelectedIndices[i];

                    _pipeProxy.CancelJob(idxList); // Pass the list
                }
                catch (Exception e1)
                {
                    Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to cancel job"), EventLogEntryType.Warning);
                    Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
                }
            }
        }

        private void currentQueue_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (Util.Net.IsUNCPath(Util.Net.GetUNCPath(files[0]))) // check if the files are on a remote computer
                MessageBox.Show(Localise.GetPhrase("Warning: Networked files will be accessed using the logon credentials of the MCEBuddy Service, not the currently logged on user. You can manually specify the network credentials from the Settings -> Expert Settings page in MCEBuddy."),
                                Localise.GetPhrase("Credential Warning"),
                                MessageBoxButtons.OK);

            // First check if it's a file or folder, if a directory then recurse through the directory getting all the files
            foreach (string file in files)
            {
                if (Directory.Exists(file)) // Check if it's a directory
                {
                    IEnumerable<string> subDirFiles = FilePaths.GetDirectoryFiles(file, "*", SearchOption.AllDirectories); // Get ALL files in the directory AND subdirectories to the queue

                    foreach (string subFile in subDirFiles) // user foreach and not to array since it's still enumerating in the background
                    {
                        addFileToQueue(subFile);
                    }
                }
                else
                    addFileToQueue(file); // it's a regular file add it
            }
        }

        private void currentQueue_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
                e.Effect = DragDropEffects.All;
        }

        /// <summary>
        /// Adds files to the conversion queue.
        /// </summary>
        /// <param name="fileList">Array of Paths of files to add to queue</param>
        private void addFileToQueue(string[] fileList)
        {
            foreach (string videoFile in fileList)
            {
                addFileToQueue(videoFile);
            }
        }

        /// <summary>
        /// Adds a file to the conversion queue
        /// </summary>
        /// <param name="file">Path of file to add to queue</param>
        private void addFileToQueue(string filePath)
        {
            string videoFileUNC = Util.Net.GetUNCPath(filePath); // Check if the file is a UNC from a mapped drive - mapped drives don't work in MCEBuddy

            try
            {
                _pipeProxy.AddManualJob(videoFileUNC);
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to add manual file to queue.\r\nError:" + e.ToString()), EventLogEntryType.Warning);
            }
        }

        private void addFileCmd_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string[] files = openFileDialog.FileNames;

                if (Util.Net.IsUNCPath(Util.Net.GetUNCPath(files[0]))) // check if the files are on a remote computer
                   MessageBox.Show(Localise.GetPhrase("Warning: Networked files will be accessed using the logon credentials of the MCEBuddy Service, not the currently logged on user. You can manually specify the network credentials from the Settings -> Expert Settings page in MCEBuddy."),
                                    Localise.GetPhrase("Credential Warning"),
                                    MessageBoxButtons.OK);

                addFileToQueue(files);
            }
        }

        private void settingsCmd_Click(object sender, EventArgs e)
        {
            bool active = false;

            Monitor.Enter(_configLock); // Capture this lock so that the TryConnect thread does not update the MCEBuddy config parameters until we finish with the configuration

            try
            {
                active = _pipeProxy.Active();
            }
            catch (Exception e1)
            {
                MessageBox.Show(Localise.GetPhrase("Unable to communicate with engine."), Localise.GetPhrase("Communication Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to get pipe active status"), EventLogEntryType.Warning);
                Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
                Monitor.Exit(_configLock); // We are done changing the configuration, so release the lock
                return;
            }

            if (active)
            {
                var res = MessageBox.Show(Localise.GetPhrase("The conversions must be stopped to change settings. Stop the conversions?"), Localise.GetPhrase("Stop Engine"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.No)
                {
                    Monitor.Exit(_configLock); // We are done changing the configuration, so release the lock
                    return;
                }
            }

            try
            {
                stopCmd.Enabled = addFileCmd.Enabled = cancelFileCmd.Enabled = rescanCmd.Enabled = false; // Disable to Stop command to prevent a race condition
                if (_pipeProxy.EngineRunning()) // Check if engine is stopped, DON'T restop since it will overwrite the mcebuddy.conf file (overwriting any changes made by the user while the engine was stopped)
                    _pipeProxy.Stop(true);
                _isEngineRunning = false; // TODO: Temp hack due to delay in updating this parameter in TryConnect until next fetch
                while (_pipeProxy.EngineRunning())
                    Thread.Sleep(100); // Wait until the engine stops

                StatusForm.profilesSummary = _pipeProxy.GetProfilesSummary(); // Get a list of all the profiles and descriptions
                StatusForm.engineProcessorCount = _pipeProxy.GetProcessorCount(); // Number of processors on the engine machine
                try // special case, ShowAnalyzer sometimes hangs the system and fails to respond, ignore and continue with log
                {
                    StatusForm.showAnalyzerInstalled = _pipeProxy.ShowAnalyzerInstalled(); // Check if Showanalyzer is installed
                }
                catch (Exception e2)
                {
                    Log.WriteSystemEventLog("Unable to get status of ShowAnalyzer, please reinstall ShowAnalyzer and reboot the system. Error " + e2.ToString(), EventLogEntryType.Error);
                    MessageBox.Show(Localise.GetPhrase("ShowAnalyzer is not responding. Please the reboot system or reinstall ShowAnalyzer."), Localise.GetPhrase("ShowAnalyzer Not Responding"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Monitor.Exit(_configLock); // We are done changing the configuration, so release the lock
                    return;
                }
            }
            catch (Exception e1)
            {
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to stop MCEBuddy service from settings click"), EventLogEntryType.Warning);
                Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
                var res = MessageBox.Show(Localise.GetPhrase("Unable to stop MCEBuddy engine, settings may not be saved. Do you want to continue?"), Localise.GetPhrase("Error Stopping Engine"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.No)
                {
                    Monitor.Exit(_configLock); // We are done changing the configuration, so release the lock
                    return;
                }
            }

            // Store the current details when the form loaded (which we need to verify later)
            string loadCulture = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.locale;
            bool loadUPnP = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.uPnPEnable;
            bool loadFirewall = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.firewallExceptionEnabled;
            int loadMaxJobs = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.maxConcurrentJobs;

            // Get engine to reload and return the latest settings (incase the user changed the mcebuddy.conf file while stopped)
            MCEBuddyConf mceOptions;
            try
            {
                ConfSettings? latestConf = _pipeProxy.ReloadAndGetConfigParameters();
                if (latestConf == null)
                    throw new Exception("Engine not stopped, cannot reload configuration settings");
                mceOptions = new MCEBuddyConf((ConfSettings) latestConf);
            }
            catch (Exception e1)
            {
                MessageBox.Show(Localise.GetPhrase("Unable to communicate with engine."), Localise.GetPhrase("Communication Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to get latest configuration from engine"), EventLogEntryType.Warning);
                Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
                Monitor.Exit(_configLock); // We are done changing the configuration, so release the lock
                return;
            }

            SettingsForm setForm = new SettingsForm(_pipeProxy, mceOptions);
            if (setForm.ShowDialog() != System.Windows.Forms.DialogResult.Abort) // Send the updated settings back to the server if the user pressed okay
            {
                MCEBuddyConf.GlobalMCEConfig.UpdateConfigOptions(mceOptions.ConfigSettings, false); // Update the settings

                try
                {
                    if (_pipeProxy.UpdateConfigParameters(mceOptions.ConfigSettings) == false) // Send the updated parameters to the Engine
                    {
                        Log.WriteSystemEventLog("MCEBuddy GUI: Unable to update Engine Configuration Settings, engine not Stopped", EventLogEntryType.Warning);
                        MessageBox.Show(Localise.GetPhrase("Engine not stopped. Settings have NOT been saved."), Localise.GetPhrase("Unable To Save Settings"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Monitor.Exit(_configLock); // We are done changing the configuration, so release the lock
                        return;
                    }
                }
                catch (Exception e2)
                {
                    Log.WriteSystemEventLog("MCEBuddy GUI: Unable to update Engine Configuration Settings", EventLogEntryType.Warning);
                    Log.WriteSystemEventLog(e2.ToString(), EventLogEntryType.Warning);
                    MessageBox.Show(Localise.GetPhrase("Unable to communicate with engine. Settings have NOT been saved."), Localise.GetPhrase("Communication Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Monitor.Exit(_configLock); // We are done changing the configuration, so release the lock
                    return;
                }
            }

            // Check if the locale or UPnP has changed, if so then restart the app to reload the locale else just close the window
            string closeCulture = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.locale;
            bool closeUPnP = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.uPnPEnable;
            bool closeFirewall = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.firewallExceptionEnabled;
            int closeMaxJobs = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.maxConcurrentJobs;

            if (loadUPnP != closeUPnP)
            {
                try
                {
                    _pipeProxy.SetUPnPState(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.uPnPEnable);
                }
                catch (Exception e3)
                {
                    Log.WriteSystemEventLog("MCEBuddy GUI: Unable to update UPnP Settings", EventLogEntryType.Warning);
                    Log.WriteSystemEventLog(e3.ToString(), EventLogEntryType.Warning);
                    MessageBox.Show(Localise.GetPhrase("Unable to communicate with engine. UPnP settings have not been updated."), Localise.GetPhrase("Communication Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (loadFirewall != closeFirewall)
            {
                try
                {
                    _pipeProxy.SetFirewallException(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.firewallExceptionEnabled);
                }
                catch (Exception e3)
                {
                    Log.WriteSystemEventLog("MCEBuddy GUI: Unable to update firewall exception Settings", EventLogEntryType.Warning);
                    Log.WriteSystemEventLog(e3.ToString(), EventLogEntryType.Warning);
                    MessageBox.Show(Localise.GetPhrase("Unable to communicate with engine. Firewall exception settings have not been updated."), Localise.GetPhrase("Communication Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            Monitor.Exit(_configLock); // We are done changing the configuration and sending the changes, so release the lock - not before sending ALL changes

            // If Culture has changed we may need to restart the application because the phrases only work against English Text.
            // If the current text is not english, then we need to restart the application to reload the english text on the control and then map the phrases against it
            if (loadCulture != closeCulture)
                SetUserLocale(); // this will work since the existing text is in English the new locale can be mapped

            // If the number of concurrent jobs has changed we need to restart the app to reset the GUI otherwise it gets messed up while resizing
            if ((loadMaxJobs != closeMaxJobs) && !_exit)
            {
                _exit = true;
                Application.Restart();
            }
        }

        private void reScanCmd_Click(object sender, EventArgs e)
        {
            try
            {
                _pipeProxy.Rescan();
                _forceQueueDisplayRefresh = true; // Refresh the GUI also if forced by user
            }
            catch (Exception e1)
            {
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy GUI: Unable to rescan files"), EventLogEntryType.Warning);
                Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
            }
        }

        private void showFileInfo(object sourceFiles)
        {
            List<string> sourceVideos = (List<string>)sourceFiles;

            foreach (string sourceVideo in sourceVideos)
            {
                try
                {
                    MediaInfo videoInfo = _pipeProxy.GetFileInfo(sourceVideo);
                    MediaInfoForm.MediaInfoFormParams mediaParams = new MediaInfoForm.MediaInfoFormParams { mediaInfo = videoInfo, sourceVideo = sourceVideo, toolTip = toolTip };
                    Thread showFileInfoThread = new Thread(MediaInfoForm.ThreadShowMediaInfo);
                    showFileInfoThread.IsBackground = true; // Close the thread when process exits
                    showFileInfoThread.CurrentCulture = showFileInfoThread.CurrentUICulture = Localise.MCEBuddyCulture;
                    showFileInfoThread.Start(mediaParams);
                }
                catch (Exception e)
                {
                    MessageBox.Show(Localise.GetPhrase("Error trying to get Audio Video information"), Localise.GetPhrase("Unable to Read Media File"), MessageBoxButtons.OK);
                    Log.WriteSystemEventLog(": Error trying to get Audio Video information -> " + e.ToString(), EventLogEntryType.Warning);
                }
            }
        }

        private void facebookBtn_Click(object sender, EventArgs e)
        {
            Ini configIni = new Ini(GlobalDefs.TempSettingsFile);
            string facebookLink = configIni.ReadString("Engine", "FacebookLink", "");
            if (String.IsNullOrWhiteSpace(facebookLink))
                facebookLink = Crypto.Decrypt(GlobalDefs.MCEBUDDY_FACEBOOK_PAGE); // backup

            Internet.OpenLink(facebookLink, "MCEBuddy GUI");
        }

        private void paypalButton_Click(object sender, EventArgs e)
        {
            Ini configIni = new Ini(GlobalDefs.TempSettingsFile);
            string donationLink = configIni.ReadString("Engine", "DonationLink", "");
            if (String.IsNullOrWhiteSpace(donationLink))
                donationLink = Crypto.Decrypt(GlobalDefs.MCEBUDDY_HOME_PAGE); // backup

            Internet.OpenLink(donationLink, "MCEBuddy GUI");
        }

        private void mceBuddyBox_Click(object sender, EventArgs e)
        {
            Internet.OpenLink(Crypto.Decrypt(GlobalDefs.MCEBUDDY_HOME_PAGE), "MCEBuddy GUI");
        }

        private void StatusForm_Shown(object sender, EventArgs e)
        {
            if (_startMinimizedOnInit) // Check if the user asked for a minimized on start
                this.WindowState = FormWindowState.Minimized;
        }

        private void PriorityBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (_pipeProxy != null) // check if the engine been intialized
                    _pipeProxy.ChangePriority(priorityBox.Text); //Change the priority for the MCEBuddy Engine service
            }
            catch (Exception e1)
            {
                Log.WriteSystemEventLog("MCEBuddy GUI: Unable to change priority due to pipe error " + e1.ToString(), EventLogEntryType.Warning);
                priorityBox.Text = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.processPriority; // Reset it
                return; // We're done here
            }
        }

        private void priorityBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void gettingStarted_Click(object sender, EventArgs e)
        {
            Internet.OpenLink((Crypto.Decrypt(GlobalDefs.MCEBUDDY_DOCUMENTATION)), "MCEBuddy GUI");
        }

        private void logLabel_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(GlobalDefs.LogPath);
            }
            catch { }
        }

        private void eventLogLbl_Click(object sender, EventArgs e)
        {
            Thread showFileInfoThread = new Thread(displayEventLogEntries);
            showFileInfoThread.IsBackground = true; // Kill this thread if the process is closed
            showFileInfoThread.SetApartmentState(ApartmentState.STA); // Must be run as STA since we are using clipboard
            showFileInfoThread.CurrentCulture = showFileInfoThread.CurrentUICulture = Localise.MCEBuddyCulture;
            showFileInfoThread.Start();
        }

        private void displayEventLogEntries()
        {
            try
            {
                List<EventLogEntry> eventLogEntries = _pipeProxy.GetWindowsEventLogs(); // Get the list of items in the queue
                EventLogDisplayForm eventLogs = new EventLogDisplayForm(eventLogEntries);
                eventLogs.ShowDialog();
            }
            catch (Exception e1)
            {
                Log.WriteSystemEventLog("MCEBuddy GUI: Unable to get Event Log Entries due to pipe error " + e1.ToString(), EventLogEntryType.Warning);
                MessageBox.Show(Localise.GetPhrase("Unable to get Event Log Entries from the MCEBuddy engine."), Localise.GetPhrase("Error retrieving Event Logs"));
                return; // We're done here
            }
        }

        private void remoteEngineCmd_Click(object sender, EventArgs e)
        {
            RemoteEngineForm rEngine = new RemoteEngineForm();
            if ((rEngine.ShowDialog() == System.Windows.Forms.DialogResult.OK) && !_exit)
            {
                _exit = true;
                Application.Restart();
            }
        }

        private void mediaInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Now get all the items selected in the queue and show the info for them
                List<String> listVideo = new List<String>();
                foreach (ListViewItem fileItem in currentQueue.SelectedItems)
                    listVideo.Add(_jobs[fileItem.Index].SourceFile); // Name of source video

                Thread showFileInfoThread = new Thread(showFileInfo);
                showFileInfoThread.IsBackground = true; // Close the thread when process exits
                showFileInfoThread.CurrentCulture = showFileInfoThread.CurrentUICulture = Localise.MCEBuddyCulture;
                showFileInfoThread.Start(listVideo);
            }
            catch (Exception e1)
            {
                Log.WriteSystemEventLog("MCEBuddy GUI: Unable to get FileQueue due to pipe error " + e1.ToString(), EventLogEntryType.Warning);
                return; // We're done here
            }
        }

        private void tmrLVScroll_Tick(Object sender, System.EventArgs e)
        {
            ScrollControl(ref currentQueue, ref intScrollDirection);
        }

        private void ScrollControl(ref ListViewEx objControl, ref int intDirection)
        {
            const int WM_SCROLL = 0x115;
            object iParam = null;
            SendMessage(objControl.Handle.ToInt32(), WM_SCROLL, intDirection, ref iParam);
        }

        private void currentQueue_MouseDown(object sender, MouseEventArgs e)
        {
            if (currentQueue.SelectedItems.Count != 1) // TODO: We don't support multiple movements for now (account for 0 selections on mouse down)
                return;

            if (currentQueue.Items.Count <= (_maxActiveJobs + 1)) // We can't move jobs if there is 1 pending running
                return;

            if (currentQueue.SelectedItems[0].Index < (_maxActiveJobs)) // We can't move jobs currently running
                return;

            _itemDnD = currentQueue.GetItemAt(e.X, e.Y);
            // if the LV is still empty, no item will be found anyway, so we don't have to consider this case
        }

        private void currentQueue_MouseMove(object sender, MouseEventArgs e)
        {
            if (_itemDnD == null)
            {
                if (tmrLVScroll.Enabled)
                    tmrLVScroll.Enabled = false;
                return;
            }

            // Show the user that a drag operation is happening
            Cursor = Cursors.Hand;

            //Scroll the listview when close to the top or bottom
            Point position = new Point(0, 0);

            position.X = e.X;
            position.Y = e.Y;

            if (position.Y <= currentQueue.Font.Height / 2) // Scroll Up
            {
                intScrollDirection = 0;
                tmrLVScroll.Enabled = true;
            }
            else if (position.Y >= currentQueue.ClientSize.Height - currentQueue.Font.Height / 2) // Scroll Down
            {
                intScrollDirection = 1;
                tmrLVScroll.Enabled = true;
            }
            else
            {
                tmrLVScroll.Enabled = false;
            }

            // calculate the bottom of the last item in the LV so that you don't have to stop your drag at the last item
            int lastItemBottom = Math.Min(e.Y, currentQueue.Items[currentQueue.Items.Count - 1].GetBounds(ItemBoundsPortion.Entire).Bottom - 1);

            // use 0 instead of e.X so that you don't have to keep inside the columns while dragging
            ListViewItem itemOver = currentQueue.GetItemAt(0, lastItemBottom);
            if (itemOver == null)
                return;

            // Calculate the last top position (we cannot get ahead of current active queue)
            ListViewItem itemTop = currentQueue.Items[_maxActiveJobs - 1];
            if (e.Y <= itemTop.GetBounds(ItemBoundsPortion.Entire).Bottom)
                return;


            Rectangle rc = itemOver.GetBounds(ItemBoundsPortion.Entire);
            if (e.Y < rc.Top + (rc.Height / 2))
            {
                currentQueue.LineBefore = itemOver.Index;
                currentQueue.LineAfter = -1;
            }
            else
            {
                currentQueue.LineBefore = -1;
                currentQueue.LineAfter = itemOver.Index;
            }

            // invalidate the LV so that the insertion line is shown
            currentQueue.Invalidate();
        }

        private void currentQueue_MouseUp(object sender, MouseEventArgs e)
        {
            if (_itemDnD == null)
                return;

            try
            {
                // calculate the bottom of the last item in the LV so that you don't have to stop your drag at the last item
                int lastItemBottom = Math.Min(e.Y, currentQueue.Items[currentQueue.Items.Count - 1].GetBounds(ItemBoundsPortion.Entire).Bottom - 1);

                // use 0 instead of e.X so that you don't have to keep inside the columns while dragging
                ListViewItem itemOver = currentQueue.GetItemAt(0, lastItemBottom);

                if (itemOver == null)
                    return;

                // Calculate the last top position (we cannot get ahead of current active queue)
                ListViewItem itemTop = currentQueue.Items[_maxActiveJobs - 1];
                if (e.Y <= itemTop.GetBounds(ItemBoundsPortion.Entire).Bottom)
                    itemOver = currentQueue.Items[_maxActiveJobs];

                Rectangle rc = itemOver.GetBounds(ItemBoundsPortion.Entire);

                // find out if we insert before or after the item the mouse is over
                bool insertBefore;
                if (e.Y < rc.Top + (rc.Height / 2))
                {
                    insertBefore = true;
                }
                else
                {
                    insertBefore = false;
                }

                if (_itemDnD != itemOver) // if we dropped the item on itself, nothing is to be done
                {
                    if (insertBefore)
                    {
                        if (_pipeProxy.UpdateFileQueue(_itemDnD.Index, itemOver.Index)) // First update the queue on the engine
                        {
                            currentQueue.Items.Remove(_itemDnD);
                            currentQueue.Items.Insert(itemOver.Index, _itemDnD);
                        }
                    }
                    else
                    {
                        if (_pipeProxy.UpdateFileQueue(_itemDnD.Index, itemOver.Index + 1)) // First update the queue on the engine
                        {
                            currentQueue.Items.Remove(_itemDnD);
                            currentQueue.Items.Insert(itemOver.Index + 1, _itemDnD);
                        }
                    }
                }

                // clear the insertion line
                currentQueue.LineAfter = currentQueue.LineBefore = -1;

                currentQueue.Invalidate();

            }
            finally
            {
                // finish drag&drop operation
                _itemDnD = null;
                Cursor = Cursors.Default;
            }
        }

        private void currentQueue_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.A) && e.Control) // Ctrl + A selects all items
            {
                foreach (ListViewItem item in currentQueue.Items)
                {
                    item.Selected = true;
                }
            }
        }

        private void StatusForm_Resize(object sender, EventArgs e)
        {
            if (_exit) // We are exiting
                return;

            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.ShowInTaskbar = false;
                _notifyIcon.BalloonTipTitle = "MCEBuddy " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Build.ToString(System.Globalization.CultureInfo.InvariantCulture);
                _notifyIcon.BalloonTipText = Localise.GetPhrase("Click here to show MCEBuddy status");
                _notifyIcon.BalloonTipIcon = ToolTipIcon.None;
                _notifyIcon.ShowBalloonTip(GlobalDefs.NOTIFY_ICON_TIP_TIMEOUT);
                return; // done, nothing else to process
            }

            // Resize the queue columns to compensate for change in window size, there may be some gap the end to accomodate the scrollbar
            if (_queueFileColumnWidthRatio > 0) // Sometimes we are called before initialization is complete in custom DPI situations
            {
                currentQueue.Columns["fileName"].Width = (int)(_queueFileColumnWidthRatio * currentQueue.Width);
                currentQueue.Columns["taskName"].Width = (int)(_queueTaskColumnWidthRatio * currentQueue.Width);
            }

            // Recalcuate the number of characters in the scroll bar
            _maxCharsLabel = (int) (announcementLabel.Width / Rescale(_pixelsPerCharacter)); // 8.5 pixels per character
        }

        /// <summary>
        /// This function is called after the form is resized AND after it is moved by a user (not programatically).
        /// This is better than using LocationChanged or Move as one cannot resize a window in those events
        /// </summary>
        private void StatusForm_ResizeEnd(object sender, EventArgs e)
        {
            if (_exit) // We are exiting
                return;

            // Check for changes in screen resolution and screen changes (multi monitor support)
            // If we are moving to a smaller screen then readjust the anchoring to get the scroll bars
            if (this.PreferredSize.Height >= Screen.FromControl(this).WorkingArea.Height) // We are at maximum size and we are scrolling, anchor to the top otherwise control will dissappear
            {
                currentQueue.Anchor &= ~AnchorStyles.Bottom; // Unhinge from bottom to prevent resize
                AnchorBottomControls(AnchorStyles.Bottom, false);
                AnchorBottomControls(AnchorStyles.Top, true);
                // Don't worry about the height it will automatically reduce based on MaximumSize parameter below
                this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations
            }
            else // We can expand, anchor to the bottom
            {
                // First set the Maxsize otherwise screen height will not change
                this.MaximumSize = Screen.FromControl(this).WorkingArea.Size; // Set the maximum size for the form based on working areas so we don't end up with dead/inaccessible locations
                AnchorBottomControls(AnchorStyles.Top, false); // First unhinge the top
                // Need to get the "correct" size in a loop, for some reason the Preferred Height gets updated incrementally each time you assign it to Height
                int i = 0; // prevent infinitely loop
                while ((this.Height != this.PreferredSize.Height) && (i++ < 50))
                    this.Height = this.PreferredSize.Height; // Update size here before rehinging controls to bottom
                AnchorBottomControls(AnchorStyles.Bottom, true); // Rehinge to the bottom now
                currentQueue.Anchor |= AnchorStyles.Bottom; // Rehinge to bottom to enable resize
                // Don't set minimum size here since the height is a dynamic iterative process above and can mess up the minimum size height
            }
        }

        private void showHistoryLbl_Click(object sender, EventArgs e)
        {
            Thread showHistoryThread = new Thread(displayHistory);
            showHistoryThread.IsBackground = true; // Kill this thread if the process is closed
            showHistoryThread.SetApartmentState(ApartmentState.STA); // Must be run as STA since we are using clipboard
            showHistoryThread.CurrentCulture = showHistoryThread.CurrentUICulture = Localise.MCEBuddyCulture;
            showHistoryThread.Start();
        }

        private void displayHistory()
        {
            HistoryForm sh = new HistoryForm(_pipeProxy);
            sh.ShowDialog();
        }
    }
}
