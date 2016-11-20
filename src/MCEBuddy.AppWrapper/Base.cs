using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.Configuration;

namespace MCEBuddy.AppWrapper
{
    public class Base
    {
        /// <summary>
        /// Path to application
        /// </summary>
        protected string _ApplicationPath;
        /// <summary>
        /// Parameters to pass to application
        /// </summary>
        protected string _Parameters;
        /// <summary>
        /// Time taken by process to run
        /// </summary>
        protected TimeSpan _ExecutionTime;
        /// <summary>
        /// How many seconds to check if the application has hung, 0 to disable
        /// </summary>
        protected int _HangPeriod = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.hangTimeout; // Default period to detect hang due to no output activity on console - 300 seconds, it can be overridden later
        /// <summary>
        /// False if the process terminated abnormally or was cancelled
        /// </summary>
        protected bool _success = false;
        /// <summary>
        /// Current status of the job
        /// </summary>
        protected JobStatus _jobStatus;
        /// <summary>
        /// Log object
        /// </summary>
        protected Log _jobLog;
        /// <summary>
        /// If set by the Output Handler indicates it encountered an unrecoverable error and process must be terminated to avoid a hang
        /// </summary>
        protected bool _unrecoverableError = false;
        /// <summary>
        /// True if we need to force run a process in a UI session with Admin privileges (default is Session 0 which is non UI session)
        /// </summary>
        protected bool _uiAdminSessionProcess = false;
        /// <summary>
        /// True to show a window for the new process, false to hide it (by default windows are hidden)
        /// </summary>
        protected bool _showWindow = false;
        /// <summary>
        /// Exit code for the process
        /// </summary>
        protected int _exitCode = 0; // Default code

        // Private variables
        private DateTime _LastTick;
        private bool isSuspended = false;
        private bool _ignoreSuspend = false;
        private float _lastPercentageComplete = 0;
        private Collections.FixedSizeQueue<KeyValuePair<float, DateTime>> _percentageHistory = new Collections.FixedSizeQueue<KeyValuePair<float, DateTime>>(GlobalDefs.PERCENTAGE_HISTORY_SIZE); // Track last 5 % entries

        /// <summary>
        /// Default base class for starting a process within the MCEBuddy installation directory
        /// </summary>
        /// <param name="parameters">Parameters for a process</param>
        /// <param name="appPath">Relative path of app to MCEBuddy installation directory</param>
        /// <param name="ignoreSuspend">Don't stop the process if MCEBuddy is suspended by user (useful if app is being called by the GUI or can deadlock)</param>
        public Base(string parameters, string appPath, JobStatus jobStatus, Log jobLog, bool ignoreSuspend=false)
        {
            _ApplicationPath = Path.Combine(GlobalDefs.AppPath, appPath);
            _Parameters = parameters;
            _jobStatus = jobStatus;
            _jobLog = jobLog;
            _ignoreSuspend = ignoreSuspend;
        }

        /// <summary>
        /// Base class for starting a custom process with custom parameters and absolute path
        /// </summary>
        /// <param name="showWindow">If false then the window for the custom app is hidden</param>
        /// <param name="parameters">Parameters for the custom process</param>
        /// <param name="appPath">Absolute Path to custom process</param>
        /// <param name="uiAdminSession">True if the app needs to run in UI Session 1</param>
        /// <param name="ignoreSuspend">Don't stop the process if MCEBuddy is suspended by user (useful if app is being called by the GUI or can deadlock)</param>
        public Base(bool showWindow, string parameters, string appPath, bool uiAdminSession, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
        {
            _showWindow = showWindow; // Do we need to hide the window?
            _ApplicationPath = appPath; // Absolute path
            _Parameters = parameters;
            _jobStatus = jobStatus;
            _jobLog = jobLog;
            _uiAdminSessionProcess = uiAdminSession;
            _ignoreSuspend = ignoreSuspend;

            // Set the default, it can be overriden later
            _success = true; // by default for custom apps, there is no output handler to assume success is true
        }

        protected virtual void OutputHandler(object sendingProcess, DataReceivedEventArgs ConsoleOutput)
        {
            if (_HangPeriod > 0)
                _LastTick = DateTime.Now;

            if (String.IsNullOrEmpty(ConsoleOutput.Data))
            {
                //_jobLog.WriteEntry(this, Localise.GetPhrase("OUTPUT: ConsoleOuput received NULL data"), Log.LogEntryType.Debug);
                return;
            }

            _jobLog.WriteEntry(this, ConsoleOutput.Data, Log.LogEntryType.Debug, true);
        }

        /// <summary>
        /// Number of seconds for which if there is not new line output in the console the process is deemed as hung, 0 to disable
        /// </summary>
        public int HangPeriod
        {
            get { return _HangPeriod; }
            set { _HangPeriod = value; }
        }

        /// <summary>
        /// Parameters for starting the process
        /// </summary>
        public string Parameters
        {
            get { return _Parameters; }
        }

        /// <summary>
        /// True is the proccess completed successfully without any exceptions/crashes/hangs/unrecoverables errors (does NOT check Exit Code)
        /// </summary>
        public bool Success
        {
            get { return _success; }
        }

        /// <summary>
        /// Exit code for a process run successfully, default is 0
        /// </summary>
        public int ExitCode
        {
            get { return _exitCode; }
        }

        protected void UpdateETAByPercentageComplete()
        {
            if (_jobStatus.PercentageComplete < _lastPercentageComplete)
            {
                _lastPercentageComplete = 0; // Reset it the history can start over
                _percentageHistory.Clear(); // Clear the history
            }

            if (_jobStatus.PercentageComplete != _lastPercentageComplete) // Change in percentage reported
            {
                // Store the value and current time stamp in the history
                _percentageHistory.Enqueue(new KeyValuePair<float, DateTime>(_jobStatus.PercentageComplete, DateTime.Now));

                // Check if we have more than 1 item in the history then calculate the ETA, Don't show for less than 1%
                if ((_percentageHistory.Count > 1) && (_jobStatus.PercentageComplete > 1))
                {
                    KeyValuePair<float, DateTime>[] history = _percentageHistory.ToArray(); // Get a copy of the history

                    // Derive the time between the oldest and newest members in the history
                    float percElapsed = (history[history.Length - 1].Key - history[0].Key) / (float)100;
                    TimeSpan timeElapsed = history[history.Length - 1].Value - history[0].Value;

                    // Calculate the ETA
                    float secsToGo = ((float)timeElapsed.TotalSeconds / percElapsed) * (1 - (_jobStatus.PercentageComplete / (float)100));
                    TimeSpan ts = TimeSpan.FromSeconds((int)secsToGo);
                    UpdateETA(ts.Hours, ts.Minutes, ts.Seconds);
                }
            }

            _lastPercentageComplete = _jobStatus.PercentageComplete; // keep track of changes
        }

        protected void UpdateETA(int Hours, int Minutes, int Seconds)
        {
            string ETADisplay = "";
            if (Hours == 1) ETADisplay += Hours.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + Localise.GetPhrase("hour")+ " ";
            if (Hours > 1) ETADisplay += Hours.ToString(System.Globalization.CultureInfo.InvariantCulture) + " "+ Localise.GetPhrase("hours") + " ";
            if (Minutes == 1) ETADisplay += Minutes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + Localise.GetPhrase("minute") + " ";
            if (Minutes > 1) ETADisplay += Minutes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + Localise.GetPhrase("minutes")+ " ";
            if (Seconds == 1) ETADisplay += Seconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + Localise.GetPhrase("second")+ " ";
            if (Seconds > 1) ETADisplay += Seconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + Localise.GetPhrase("seconds")+ " ";
            _jobStatus.ETA = ETADisplay.Trim();
        }

        public virtual void Run()
        {
                
            //_success = false; //initial state to be defined by each inheriting class depending on it's handlers, we only update error conditions in this routine
            Process Proc = null;
            DateTime ExecutionStartTime;

            // Last thing to check, for custom commands, this will log an error and exit
            if (!File.Exists(_ApplicationPath))
            {
                _jobStatus.ErrorMsg = "Application File Not Found";
                _jobLog.WriteEntry(this, _jobStatus.ErrorMsg + " : " + _ApplicationPath, Log.LogEntryType.Error);
                _jobStatus.PercentageComplete = 0;
                _success = false;
                return;
            }

            if (_Parameters.Length > 8192)
                _jobLog.WriteEntry(this, Localise.GetPhrase("Warning - command parameters exceeding 8192 characters.  This may fail on some systems such as Windows XP"), Log.LogEntryType.Warning);

            // Reset any progress counters
            _jobStatus.PercentageComplete = 0;
            _jobStatus.ETA = Localise.GetPhrase("Working") + "..."; //some processes like ReMuxSupp don't update perc, put a default message

            // Check if job has been cancelled, needed for some long running functions called Base
            if (_jobStatus.Cancelled)
            {
                _jobStatus.ErrorMsg = "Job cancelled, killing process";
                _jobLog.WriteEntry(this, _jobStatus.ErrorMsg, Log.LogEntryType.Error);
                _success = false; //process has been terminated
                return;
            }

            try
            {
                if (_HangPeriod > 0)
                    _LastTick = DateTime.Now;

                _jobLog.WriteEntry(this, "Launching process " + _ApplicationPath, Log.LogEntryType.Debug);
                _jobLog.WriteEntry(this, "Process arguments " + _Parameters, Log.LogEntryType.Debug);

                if (GlobalDefs.IsEngineRunningAsService) // these params only make sense when running as a service
                    _jobLog.WriteEntry(this, "UI Session Admin Process : " + _uiAdminSessionProcess.ToString(), Log.LogEntryType.Debug);

                //Start the process
                ExecutionStartTime = DateTime.Now;

                // Check if we need to run a UISession process - It can only be run from a service, if we are running from a non service console, we don't need this
                // Apps like handbrake with hardware accelleration cannot run from a non UI session in windows (even in Windows 8 there are limitations with ffmpeg and OpenCL from Session 0) due to unavailability of hardware (OpenCL) API's in NonUI sessions mode
                // These apps needs to be started in a UI session separately to run
                // Check if we are running as a Service (Session 0 non UI Interactive) and if the user forcing use of UI Session 1
                bool checkUICompliance = (GlobalDefs.IsEngineRunningAsService && _uiAdminSessionProcess);
                if (checkUICompliance)
                {
                    _jobLog.WriteEntry(this, "Starting process as a UISession process with Admin privileges. This requires atleast 1 user to be logged into the system (remote desktop or locally)", Log.LogEntryType.Debug);
                    uint procId = AppProcess.StartAppWithAdminPrivilegesFromNonUISession(_ApplicationPath, _Parameters, true, OutputHandler, _showWindow, _jobLog);
                    if (procId == 0)
                    {
                        _jobLog.WriteEntry(this, "Unable to create UI Session process with Admin Privileges from NonUI Session. Is any user logged on?", Log.LogEntryType.Warning);
                        _jobLog.WriteEntry(this, "Retrying process creation as a NonUI Session process with Admin privileges", Log.LogEntryType.Warning);
                        _jobLog.WriteEntry(this, "Some functions like hardware encoding may not work in this mode", Log.LogEntryType.Warning);
                        Proc = null;
                    }
                    else
                        Proc = Process.GetProcessById((int)procId); // Get the process identifier
                }
                
                // Create the process if it hasn't already been created
                if(Proc == null)
                {
                    //Set up the process
                    Proc = new Process();
                    Proc.StartInfo.FileName = _ApplicationPath;
                    Proc.StartInfo.Arguments = _Parameters;
                    if (_showWindow)
                    {
                        Proc.StartInfo.CreateNoWindow = false; // for custom apps we create a window
                        Proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    }
                    else
                    {
                        Proc.StartInfo.CreateNoWindow = true;
                        Proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    }
                    Proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(_ApplicationPath);
                    Proc.StartInfo.RedirectStandardOutput = true;
                    Proc.StartInfo.RedirectStandardError = true;
                    Proc.StartInfo.UseShellExecute = false; // always false else error handlers break
                    Proc.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                    Proc.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);

                    Proc.Start(); // First start the process else we get an exception
                    Proc.BeginOutputReadLine();
                    Proc.BeginErrorReadLine();
                }
            }
            catch (Exception Ex)
            {
                _jobStatus.ErrorMsg = "Unable to start process";
                _jobLog.WriteEntry(this, _jobStatus.ErrorMsg + "\r\n" + Ex.Message, Log.LogEntryType.Error);
                _success = false; //process did not start
                return; // Exit now or we get an exception
            }

            // NOTE: Do all this after starting the process outside the catch block. Sometimes the process exists quickly (e.g. when process priority is IDLE) before this code runs and hence this code throws an exception
            try
            {
                _jobLog.WriteEntry(this, "Setting process priority to " + GlobalDefs.Priority.ToString(), Log.LogEntryType.Debug);
                Proc.PriorityClass = GlobalDefs.Priority; // Set the CPU Priority
                IOPriority.SetPriority(Proc.Handle, GlobalDefs.IOPriority); // Set the IO Priority

                if (MCEBuddyConf.GlobalMCEConfig.GeneralOptions.CPUAffinity != (IntPtr)0)
                {
                    _jobLog.WriteEntry(this, "Setting CPU affinity to -> " + MCEBuddyConf.GlobalMCEConfig.GeneralOptions.CPUAffinity.ToString("d"), Log.LogEntryType.Debug);
                    Proc.ProcessorAffinity = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.CPUAffinity;
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "Error trying process priority or setting CPU affinity to -> " + MCEBuddyConf.GlobalMCEConfig.GeneralOptions.CPUAffinity.ToString("d") + "\n" + e.ToString(), Log.LogEntryType.Warning);
            }

            //Wait for an end
            while ((!Proc.HasExited))
            {
                try
                {
                    if (!_ignoreSuspend) // for some processes like ffmpegMediaInfo initiated by UI, we cannot block since it will hang the application
                    {
                        if (!isSuspended && GlobalDefs.Pause) // Check if process has to be suspended (if not already)
                        {
                            _jobLog.WriteEntry(this, "Suspending process", Log.LogEntryType.Information);
                            IOPriority.SuspendProcess(Proc.Id);
                            _jobLog.Flush(); // Flush the pending writes
                            isSuspended = true;
                        }

                        if (isSuspended && !GlobalDefs.Pause) // Check if we need to resume the process
                        {
                            _jobLog.WriteEntry(this, "Resuming process", Log.LogEntryType.Information);
                            isSuspended = false;
                            _percentageHistory.Clear(); // Lose the history and start from here
                            IOPriority.ResumeProcess(Proc.Id);
                        }
                    }

                    if (Proc.PriorityClass != GlobalDefs.Priority) // Check if the priority was changed and if so update it
                    {
                        _jobLog.WriteEntry(this, "Process priority changed to " + GlobalDefs.Priority.ToString(), Log.LogEntryType.Information);
                        Proc.PriorityClass = GlobalDefs.Priority;
                        IOPriority.SetPriority(Proc.Handle, GlobalDefs.IOPriority); // First set the CPU priority
                    }
                }
                catch { } //incase process exits in the background - not an issue, just avoid a crash

                if (_jobStatus.Cancelled) // if job has been cancelled kill the process
                {
                    try
                    {
                        Proc.Kill();
                        _jobStatus.ErrorMsg = "Job cancelled, killing process";
                        _jobLog.WriteEntry(_jobStatus.ErrorMsg, Log.LogEntryType.Error);
                        _success = false; //process has been terminated
                    }
                    catch (Exception Ex)
                    {
                        _jobStatus.ErrorMsg = "Job cancelled, unable to kill process";
                        _jobLog.WriteEntry(_jobStatus.ErrorMsg, Log.LogEntryType.Error);
                        _jobLog.WriteEntry(Ex.Message, Log.LogEntryType.Warning);
                        _success = false; //process has been terminated
                    }
                    break;
                }

                if (_unrecoverableError)
                {
                    _jobLog.WriteEntry("Unrecoverable error encountered. Process likely hung, killing it", Log.LogEntryType.Error);
                    _success = false;
                    break;
                }

                if (isSuspended)
                    _LastTick = DateTime.Now; // Since during suspension there will be no output it shouldn't terminate the process

                if ((_HangPeriod > 0) && (DateTime.Now > _LastTick.AddSeconds(_HangPeriod)))
                {
                    _jobLog.WriteEntry("No response from process for " + _HangPeriod + " seconds, process likely hung - killing it", Log.LogEntryType.Error);
                    _success = false;
                    break;
                }

                System.Threading.Thread.Sleep(100); // sleep and check again
            }

            if (!_jobStatus.Cancelled)
                System.Threading.Thread.Sleep(2000);    //Allow the last set of messages to be written

            if (!Proc.HasExited) // If we broke out of the loop, somethign bad happened
            {
                try
                {
                    Proc.Kill();
                    _jobStatus.ErrorMsg = "Process hung, killing process";
                    _jobLog.WriteEntry(_jobStatus.ErrorMsg, Log.LogEntryType.Error);
                    _success = false; //process has been terminated
                }
                catch (Exception Ex)
                {
                    _jobStatus.ErrorMsg = "Unable to terminate process";
                    _jobLog.WriteEntry(_jobStatus.ErrorMsg + "\r\n" + Ex.Message, Log.LogEntryType.Error);
                    _success = false; //process has been terminated
                }
            }
            else
            {
                // Sometimes for some reason the process exits without the output redirect async handlers completing causing failures
                // MSDN http://msdn.microsoft.com/en-us/library/fb4aw7b8(v=vs.110).aspx recommends using WaitForExit(x) followed by WaitForExit() to flush all output async handlers
                // TODO: Aysnc output is broken in .NET even with WaitForExit(), see http://alabaxblog.info/2013/06/redirectstandardoutput-beginoutputreadline-pattern-broken/ and http://stackoverflow.com/questions/9533070/how-to-read-to-end-process-output-asynchronously-in-c
                try
                {
                    while (!Proc.WaitForExit(10)); // Wait for it to return True
                    Proc.WaitForExit(); // Now wait for it to flush the buffers
                }
                catch { }

                _exitCode = Proc.ExitCode; // Store the exit code
                _jobLog.WriteEntry("Process exited with code " + _exitCode.ToString(), Log.LogEntryType.Debug);
            }


            _ExecutionTime = DateTime.Now - ExecutionStartTime;

            //Close out
            try
            {
                Proc.Close();
                Proc.Dispose();
            }
            catch (Exception Ex)
            {
                _jobLog.WriteEntry("Unable to cleanly close process"+ "\r\n" + Ex.Message, Log.LogEntryType.Error);
            }

            Proc = null;

            if (!_jobStatus.Cancelled)
                System.Threading.Thread.Sleep(100);    //Allow for the process to be flush any pending messages
        }
    }
}
