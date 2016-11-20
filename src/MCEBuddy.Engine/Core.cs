using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Globalization;

using MCEBuddy.Engine;
using MCEBuddy.Util;
using MCEBuddy.Globals;
using MCEBuddy.Configuration;
using MCEBuddy.AppWrapper;
using MCEBuddy.CommercialScan;
using MCEBuddy.EMailEngine;

namespace MCEBuddy.Engine
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single, MaxItemsInObjectGraph = Int32.MaxValue)] // Max objects allowed in serialized communication channel
    public class Core : ICore 
    {
        private const int MAX_SHUTDOWN_WAIT = 2000;

        volatile private QueueManager _queueManager;
        private int _maxConcurrentJobs;
        private int _pollPeriod;
        private bool _rescan = false;
        private int _wakeHour = -1;
        private int _wakeMinute = -1; 
        private int _startHour = -1;
        private int _startMinute = -1;
        private int _stopHour = -1;
        private int _stopMinute = -1;
        private int logKeepDays = 0; // Number of days to keep log files
        volatile private ConversionJob[] _conversionJobs;
        private Util.PowerManagement.WakeUp _wakeUp;
        private bool _deleteOriginal = false;
        private bool _useRecycleBin = false;
        private bool _archiveOriginal = false;
        private bool _failedMoveOriginal = false;
        private bool _userAllowSleep = true;
        private Thread _uPnPCheckThread = null;
        private UpdateCheck _updateCheck = null;
        private string[] _daysOfWeek;
        volatile private Thread _monitorThread;
        volatile private bool _engineCrashed = false; // check if engine crashed
        private bool _uPnPEnabled;
        private bool _firewallExceptionEnabled;
        static private Object monitorLock = new Object(); // Object to lock for monitorThread sync
        private bool _serviceShutdownBySystem = false;
        private bool _allowSleep = true; // Can we allow standby and suspend
        private bool _autoPause = false; // Pause when outside conversion times

        public Core()
        {
            try
            {
                CreateExposedDirectory(GlobalDefs.ConfigPath); // Get admin access to the config directory so we can write to it
                CreateExposedDirectory(GlobalDefs.LogPath); // Get admin access to the log directory so we can write to it
                CreateExposedDirectory(GlobalDefs.CachePath); // Artwork Cache
                try
                {
                    Log.AppLog = new Log(GlobalDefs.AppLogFile);
                }
                catch (Exception e) // Log file is not CRITICAL to engine operation, so log it in the System event
                {
                    Log.WriteSystemEventLog("Unable to create mcebuddy.log for MCEBuddy Engine Core.\r\nError: " + e.ToString(), EventLogEntryType.Error);
                }
                _updateCheck = new UpdateCheck(); // Create only instance of this and resuse, otherwise it fails due to embedded objects
                MCEBuddyConf.GlobalMCEConfig = new MCEBuddyConf(GlobalDefs.ConfigFile);

                Process process = Process.GetCurrentProcess(); //Get an instance of current process 
                process.PriorityClass = GlobalDefs.EnginePriority; // Core Engine thread ALWAYS runs at above normal to ensure that all it's monitoring and response functions are running responsive

                ReadConfig();

                //UPnP
                if (_uPnPEnabled)
                {
                    // Create a thread that keeps checking on the UPnP port mapping (some routers drop the mapping)
                    _uPnPCheckThread = new Thread(UPnPMonitorThread);
                    _uPnPCheckThread.CurrentCulture = _uPnPCheckThread.CurrentUICulture = Localise.MCEBuddyCulture;
                    _uPnPCheckThread.Start();
                }

                // Open the Firewall port to allow MCEBuddy to accept incoming connections from the network
                if (_firewallExceptionEnabled)
                {
                    new Thread(() => FirewallException(true)).Start(); // Do it as a thead so it doesn't impact the engine incase of a thread
                }
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry(this, "Unable to complete initialization of MCEBuddy Engine Core.\r\nError: " + e.ToString(), Log.LogEntryType.Error, true); // This may or may not work depending upon whether the llog has been initialized otherwise it goes into NULL log
                Log.WriteSystemEventLog("Unable to complete initialization of MCEBuddy Engine Core.\r\nError: " + e.ToString(), EventLogEntryType.Error);
                throw e; // Continue throwing the exception so that the engine stops
            }
        }

        public bool IsEngineRunningAsService()
        {
            return GlobalDefs.IsEngineRunningAsService;
        }

        public int GetProcessorCount()
        {
            return Environment.ProcessorCount;
        }

        public Dictionary<string, SortedList<string, string>> GetConversionHistory()
        {
            Ini historyIni = new Ini(GlobalDefs.HistoryFile);
            Dictionary<string, SortedList<string, string>> retVal = new Dictionary<string,SortedList<string,string>>();

            try
            {
                List<string> fileNames = historyIni.GetSectionNames();
                foreach (string filePath in fileNames)
                {
                    try
                    {
                        SortedList<string, string> entries = historyIni.GetSectionKeyValuePairs(filePath);
                        retVal.Add(filePath, entries); // Add the file and the entries for the file
                    }
                    catch (Exception e1)
                    {
                        Log.AppLog.WriteEntry(this, "Error processing history file section entry -> " + filePath + "\r\nError -> " + e1.ToString(), Log.LogEntryType.Error, true);
                    }
                }
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry(this, "Unable to get History file entries.\r\nError -> " + e.ToString(), Log.LogEntryType.Error, true);
            }

            return retVal;
        }

        public void ClearHistory()
        {
            Util.FileIO.TryFileDelete(GlobalDefs.HistoryFile, true); // Delete the history file but send it to the recycle bin just incase
        }

        public bool AllowSuspend()
        {
            return _allowSleep;
        }

        public bool EngineRunning()
        {
            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            bool ret = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.engineRunning;
            Monitor.Exit(monitorLock);

            return ret;
        }

        public bool EngineCrashed()
        {
            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            bool ret = _engineCrashed;
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            return ret;
        }

        public bool Active()
        {
            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            bool ret = GlobalDefs.Active;
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            return ret;
        }

        public void Rescan()
        {
            _rescan = true;
        }

        public bool ShowAnalyzerInstalled()
        {
            return Scanner.ShowAnalyzerInstalled();
        }

        public bool ServiceShutdownBySystem()
        {
            return _serviceShutdownBySystem;
        }

        public int NumConversionJobs()
        {
            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            int ret = _maxConcurrentJobs;
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            return ret;
        }

        public bool TestEmailSettings(EmailBasicSettings emailSettings)
        {
            if (emailSettings == null)
            {
                Log.AppLog.WriteEntry(this, "No eMail settings found to test.", Log.LogEntryType.Error, true);
                return false;
            }

            string subject = Localise.GetPhrase("MCEBuddy Test eMail");
            string message = Localise.GetPhrase("MCEBuddy - Have a great day!");

            return eMail.SendEMail(emailSettings, subject, message, Log.AppLog);
        }

        public void DeleteHistoryItem(string sourceFileName)
        {
            if (String.IsNullOrWhiteSpace(sourceFileName))
            {
                Log.AppLog.WriteEntry(this, "No filename to delete from History", Log.LogEntryType.Error, true);
                return;
            }

            Ini historyFile = new Ini(GlobalDefs.HistoryFile);
            historyFile.DeleteSection(sourceFileName);
        }

        public List<EventLogEntry> GetWindowsEventLogs()
        {
            List<EventLogEntry> retList = new List<EventLogEntry>();

            //try
            //{
            //    retList = Log.GetWindowsEventLogs(GlobalDefs.MAX_EVENT_MESSAGES_TRANSFER);
            //}
            //catch (Exception e)
            //{
                Log.AppLog.WriteEntry(this, "Unable to get Event Log Entries", Log.LogEntryType.Error, true);
            //    Log.AppLog.WriteEntry(this, "Error -> " + e.ToString(), Log.LogEntryType.Error, true);
            //}

            return retList;
        }

        public List<string[]> GetProfilesSummary()
        {
            List<string[]> profileSummary = new List<string[]>();

            // Open and read all profiles
            Ini profileIni = new Ini(GlobalDefs.ProfileFile);
            foreach (string profile in profileIni.GetSectionNames())
                profileSummary.Add(new String[] { profile, profileIni.ReadString(profile, "Description", "") }); // 2 array string -> Profile Name, Description

            return profileSummary;
        }

        public bool UpdateConfigParameters(ConfSettings configOptions)
        {
            if (EngineRunning()) // Check if engine has been stopped
                return false;

            Monitor.Enter(monitorLock); // Only one action update the object at a time

            // Set the engine running flag to Stop, just in case wrong settings were passed
            configOptions.generalOptions.engineRunning = false;

            MCEBuddyConf.GlobalMCEConfig = new MCEBuddyConf(configOptions, GlobalDefs.ConfigFile); // Create a new object and write the settings
            ReadConfig(); // Load the new configuration
            
            Monitor.Exit(monitorLock);

            return true;
        }

        public ConfSettings GetConfigParameters()
        {
            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            ConfSettings retVal = MCEBuddyConf.GlobalMCEConfig.ConfigSettings;
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            return retVal;
        }

        public ConfSettings? ReloadAndGetConfigParameters()
        {
            if (EngineRunning()) // Check if engine has been stopped
                return null;

            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            // Reload the configuration settings
            MCEBuddyConf.GlobalMCEConfig = new MCEBuddyConf(GlobalDefs.ConfigFile);
            
            // Set the engine running flag to Stop, just in case
            MCEBuddyConf.GlobalMCEConfig.GeneralOptions.engineRunning = false;

            ReadConfig(); // Read the configuration

            ConfSettings retVal = MCEBuddyConf.GlobalMCEConfig.ConfigSettings;
            
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            return retVal;
        }

        public JobStatus GetJobStatus(int jobNumber)
        {
            JobStatus retStatus = null;

            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            Monitor.Enter(_queueManager.Queue); //the lock is always on the jobQueue
            try
            {
                if (jobNumber < _maxConcurrentJobs)
                    if (_conversionJobs[jobNumber] != null)
                        if (_conversionJobs[jobNumber].Status != null)
                            retStatus = _conversionJobs[jobNumber].Status;
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry(this, "Unable to get Job Status. MaxJobs = " + _maxConcurrentJobs.ToString() + " Job Stautus Req = " + jobNumber.ToString() + " Conversion Jobs Initialized = " + _conversionJobs.Length.ToString(), Log.LogEntryType.Error, true);
                Log.AppLog.WriteEntry(this, "Error -> " + e.ToString(), Log.LogEntryType.Error, true);
            }
            Monitor.Exit(_queueManager.Queue); //the lock is always on the jobQueue
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            return retStatus;
        }

        public List<JobStatus> GetAllJobsInQueueStatus()
        {
            List<JobStatus> retStatus = new List<JobStatus>();

            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            Monitor.Enter(_queueManager.Queue); //the lock is always on the jobQueue
            try
            {
                foreach (ConversionJob cjo in _queueManager.Queue)
                {
                    retStatus.Add(cjo.Status);
                }
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry(this, "Unable to get all Job Statuses. MaxJobs = " + _maxConcurrentJobs.ToString() + " Conversion Jobs Initialized = " + _conversionJobs.Length.ToString(), Log.LogEntryType.Error, true);
                Log.AppLog.WriteEntry(this, "Error -> " + e.ToString(), Log.LogEntryType.Error, true);
            }
            Monitor.Exit(_queueManager.Queue); //the lock is always on the jobQueue
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            return retStatus;
        }

        public bool UpdateFileQueue(int currentJobNo, int newJobNo)
        {
            bool ret = false;

            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            Monitor.Enter(_queueManager.Queue); // Important otherwise we get an exception that queue has changed
            try
            {
                if ((currentJobNo >= _maxConcurrentJobs) && (newJobNo >= _maxConcurrentJobs) && (currentJobNo < _queueManager.Queue.Count) && (newJobNo <= _queueManager.Queue.Count))
                {
                    ConversionJob item = _queueManager.Queue[currentJobNo];

                    _queueManager.Queue.RemoveAt(currentJobNo);

                    if (newJobNo > currentJobNo)
                        newJobNo--; // the actual index could have shifted due to the removal 

                    _queueManager.Queue.Insert(newJobNo, item);

                    ret = true; // Success
                }
                else
                    Log.AppLog.WriteEntry(this, "Unable to move Job, current of new job number less then number of max concurrent jobs or greater than current queue size", Log.LogEntryType.Error, true);
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry(this, "Unable to move Job. MaxJobs = " + _maxConcurrentJobs.ToString() + " Current Job No = " + currentJobNo.ToString() + " New Job No = " + newJobNo.ToString() + " Conversion Jobs Initialized = " + _conversionJobs.Length.ToString(), Log.LogEntryType.Error, true);
                Log.AppLog.WriteEntry(this, "Error -> " + e.ToString(), Log.LogEntryType.Error, true);
            }
            Monitor.Exit(_queueManager.Queue);
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            return ret;
        }

        [Obsolete("FileQueue is deprecated, please use GetAllJobsInQueueStatus instead.")]
        public List<string[]> FileQueue()
        {
            List<string[]> fileList = new List<string[]>();

            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            Monitor.Enter(_queueManager.Queue); // Important otherwise we get an exception that queue has changed

            try
            {
                foreach (ConversionJob job in _queueManager.Queue)
                {
                    string[] item = new string[2];
                    item[0] = job.OriginalFileName;
                    item[1] = job.TaskName;
                    fileList.Add(item);
                }
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry(this, "Unable to get FileQueue Statuses. MaxJobs = " + _maxConcurrentJobs.ToString() + " Conversion Jobs Initialized = " + _conversionJobs.Length.ToString(), Log.LogEntryType.Error, true);
                Log.AppLog.WriteEntry(this, "Error -> " + e.ToString(), Log.LogEntryType.Error, true);
            }

            Monitor.Exit(_queueManager.Queue);
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            return fileList;
        }

        public void AddManualJob(string videoFilePath)
        {
            if (String.IsNullOrWhiteSpace(videoFilePath))
            {
                Log.AppLog.WriteEntry(this, "Empty filepath passed to AddManualJob", Log.LogEntryType.Error, true);
                return;
            }

            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            // Clear the status on the manually added file incase it was converted earlier or was an output of a conversion earlier, otherwise ReScan won't pick up
            Ini historyIni = new Ini(GlobalDefs.HistoryFile);
            historyIni.Write(videoFilePath, "Status", "");

            // Add to the manual queue file to pick up
            Ini manualQueueIni = new Ini(GlobalDefs.ManualQueueFile);
            manualQueueIni.Write("ManualQueue", videoFilePath, videoFilePath); // Write to a common section as key AND value since due to INI restriction only the Value can hold special characters like ];= which maybe contained in the filename, hence the Value is used to capture the "TRUE" filename (Key and Section have restrictions)

            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            _rescan = true; // Rescan the queue for new jobs
        }

        public void StopBySystem() //called by MCEBuddy ServiceShutdown
        {
            _serviceShutdownBySystem = true; // Indicate to app to close to avoid uninstall issues
            Thread.Sleep((int) (GlobalDefs.LOCAL_ENGINE_POLL_PERIOD * GlobalDefs.GUI_MINIMIZED_ENGINE_POLL_SLOW_FACTOR * 1.1)); // Give the local GUI time to register the shutdown (GUI needs to shutdown incase we are say uninstalling the app)

            Log.AppLog.WriteEntry(this, "MCEBuddy Stop by system initiated", Log.LogEntryType.Information, true);

            Stop(false); // Stop the service but dont' store it since it's a reboot, we want it to start on reboot

            if (_uPnPCheckThread != null)
            {
                _uPnPEnabled = false; // Signal for thread to exit (incase the Join fails)

                // Stop the UPnP Thread
                _uPnPCheckThread.Abort(); // Abort the thread (it will disable UPnP also)
            }

            if (_firewallExceptionEnabled)
                FirewallException(false); // Remove the firewall exception

            Log.AppLog.Close(); // Service shutdown, now close the system log so we can cleanup
        }

        public void Stop(bool preserveState)
        {
            Monitor.Enter(monitorLock);

            _autoPause = GlobalDefs.Pause = false; // Reset suspension state
            GlobalDefs.Shutdown = true;

            if (_wakeUp != null)
            {
                _wakeUp.Abort();
                _wakeUp = null; //let the timer not fire again
            }

            while (_monitorThread != null)
            {
                Thread.Sleep(300);
            }

            Log.AppLog.WriteEntry(this, Localise.GetPhrase("MCEBuddy engine stopped"), Log.LogEntryType.Information, true);

            GeneralOptions go = MCEBuddyConf.GlobalMCEConfig.GeneralOptions;
            go.engineRunning = false;
            MCEBuddyConf.GlobalMCEConfig.UpdateGeneralOptions(go, preserveState); // Write the engine settings
            if (preserveState)
                MCEBuddyConf.GlobalMCEConfig.WriteSettings(); // Write all configuration settings to file (last used state)

            if (preserveState) // Check if we are asked to preserve the stop state
                Log.AppLog.WriteEntry(this, Localise.GetPhrase("Setting engine last running state to stop"), Log.LogEntryType.Information, true);

            Monitor.Exit(monitorLock);
        }

        public void Start()
        {
            Monitor.Enter(monitorLock);

            if (_monitorThread != null)
            {
                Monitor.Exit(monitorLock);
                return;
            }

            // Re-read the Settings from the file here, this is called when the engine is Started, i.e. the settings may have changed
            MCEBuddyConf.GlobalMCEConfig = new MCEBuddyConf(GlobalDefs.ConfigFile);

            ReadConfig();
            _autoPause = GlobalDefs.Pause = false; // Reset just in case it was set earlier

            _monitorThread = new Thread(MonitorThread);
            _monitorThread.CurrentCulture = _monitorThread.CurrentUICulture = Localise.MCEBuddyCulture;
            _monitorThread.Start();

            if ((_wakeHour > -1) && (_wakeMinute > -1))
            {
                _wakeUp = new PowerManagement.WakeUp();
                _wakeUp.Woken += WakeUp_Woken;
                SetNextWake();
            }
            Log.AppLog.WriteEntry(this, Localise.GetPhrase("MCEBuddy engine started.  Setting engine last running state to start."), Log.LogEntryType.Debug, true);

            GeneralOptions go = MCEBuddyConf.GlobalMCEConfig.GeneralOptions;
            go.engineRunning = true;
            MCEBuddyConf.GlobalMCEConfig.UpdateGeneralOptions(go, true); // Write the engine settings

            Monitor.Exit(monitorLock);
        }

        public void CancelJob(int[] jobList)
        {
            if (jobList == null)
            {
                Log.AppLog.WriteEntry(this, "Empty job list passed to cancel jobs", Log.LogEntryType.Error, true);
                return;
            }

            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            Monitor.Enter(_queueManager.Queue);

            try
            {
                List<ConversionJob> convJob = new List<ConversionJob>();
                for (int i = 0; i < jobList.Length; i++)
                {
                    //Build the list of conversion jobs to cancel, don't use job numbers since they will change once removed from the queue
                    if ((jobList[i] >= 0) && (jobList[i] < _queueManager.Queue.Count))
                        convJob.Add(_queueManager.Queue[jobList[i]]);
                }

                foreach (ConversionJob cj in convJob)
                {
                    if (cj != null)
                    {
                        if (cj.Active) // if job is active, remove from manual queue and mark it cancelled for monitor thread to process
                        {
                            Log.AppLog.WriteEntry(this, Localise.GetPhrase("Active job signalled cancellation for") + " " + cj.OriginalFileName, Log.LogEntryType.Information, true);
                            cj.Status.Cancelled = true;
                        }
                        else
                        {
                            Log.AppLog.WriteEntry(this, Localise.GetPhrase("Inactive job cancelled") + " " + cj.OriginalFileName, Log.LogEntryType.Information, true);

                            cj.Status.Cancelled = true; // mark the job as cancelled (for write history and tracking)

                            // First delete the job from the manual file entry while the ref is active, if this is the last task for the job
                            if (_queueManager.JobCount(cj.OriginalFileName) <= 1)
                            {
                                Ini iniManualQueue = new Ini(GlobalDefs.ManualQueueFile);
                                iniManualQueue.DeleteKey("ManualQueue", cj.Status.SourceFile);
                            }

                            WriteHistory(cj); //  Write to history file (it will check for last task for job)

                            _queueManager.Queue.Remove(cj); // now remove the job from the queue
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry(this, "Unable to cancel jobs. JobList = " + string.Join(",", jobList.Select(x => x.ToString()).ToArray()) + " MaxJobs = " + _maxConcurrentJobs.ToString() + " Conversion Jobs Initialized = " + _conversionJobs.Length.ToString(), Log.LogEntryType.Error, true);
                Log.AppLog.WriteEntry(this, "Error -> " + e.ToString(), Log.LogEntryType.Error, true);
            }

            Monitor.Exit(_queueManager.Queue);
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
        }


        public void SuspendConversion(bool suspend)
        {
            GlobalDefs.Pause = suspend;
        }

        public bool IsSuspended()
        {
            return GlobalDefs.Pause;
        }

        public void ChangePriority(string processPriority)
        {
            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            //Change the priority of the of Global Objects used to start conversion theads
            // Do not change core engine process as it makes it unresponsive and faults the pipe timeout and also impacts threads reading console output - Core engine process always runs above normal to make it responsive

            switch (processPriority)
            {
                case "High":
                    GlobalDefs.Priority = ProcessPriorityClass.AboveNormal;
                    GlobalDefs.IOPriority = ProcessPriority.ABOVE_NORMAL_PRIORITY_CLASS;
                    IOPriority.SetPriority(ProcessPriority.PROCESS_MODE_BACKGROUND_END); // Reset it
                    break;

                case "Normal":
                    GlobalDefs.Priority = ProcessPriorityClass.Normal;
                    GlobalDefs.IOPriority = ProcessPriority.NORMAL_PRIORITY_CLASS;
                    IOPriority.SetPriority(ProcessPriority.PROCESS_MODE_BACKGROUND_END); // Reset it
                    break;

                case "Low":
                    GlobalDefs.Priority = ProcessPriorityClass.Idle;
                    GlobalDefs.IOPriority = ProcessPriority.IDLE_PRIORITY_CLASS;
                    IOPriority.SetPriority(ProcessPriority.PROCESS_MODE_BACKGROUND_BEGIN); // If we set to IDLE IO Priority we need to set the background mode begin (only valid on CURRENT process and all CHILD process inherit) - MCEBuddy has ONLY 1 process so we set it here, all children are threads
                    break;

                default:
                    GlobalDefs.Priority = ProcessPriorityClass.Normal;
                    GlobalDefs.IOPriority = ProcessPriority.NORMAL_PRIORITY_CLASS;
                    IOPriority.SetPriority(ProcessPriority.PROCESS_MODE_BACKGROUND_END); // Reset it
                    break;
            }

            // Store the new value
            GeneralOptions go = MCEBuddyConf.GlobalMCEConfig.GeneralOptions;
            go.processPriority = processPriority;
            MCEBuddyConf.GlobalMCEConfig.UpdateGeneralOptions(go, true);
            
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
        }

        public void SetUPnPState(bool enable)
        {
            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            if (enable)
            {
                // Check if UPnP Monitor thread is running, if not then start it
                if (_uPnPCheckThread == null)
                {
                    _uPnPEnabled = true; // Flag to keep thread going

                    // Create a thread that enabled UPnP keeps checking on the UPnP port mapping (some routers drop the mapping)
                    _uPnPCheckThread = new Thread(UPnPMonitorThread);
                    _uPnPCheckThread.CurrentCulture = _uPnPCheckThread.CurrentUICulture = Localise.MCEBuddyCulture;
                    _uPnPCheckThread.Start();
                }
            }
            else
            {
                // Check if the UPnP thread is running, if so then stop it
                if (_uPnPCheckThread != null)
                {
                    _uPnPEnabled = false; // Just in case abort/join fails
                    _uPnPCheckThread.Abort(); // Abort the thread (it will disable UPnP also)
                }
            }

            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
        }

        public void SetFirewallException(bool createException)
        {
            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            if (FirewallException(createException)) // If we are successful in updating the firewall
                _firewallExceptionEnabled = createException; // Update the status so it can be appropriately handled during shutdown
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
        }

        public MediaInfo GetFileInfo(string videoFilePath)
        {
            if (String.IsNullOrWhiteSpace(videoFilePath))
            {
                Log.AppLog.WriteEntry(this, "Empty filepath passed to get file info", Log.LogEntryType.Error, true);
                return null;
            }

            // Get the properties of this source video
            JobStatus jobStatus = new JobStatus();

            // Get the FPS from MediaInfo, more reliable then FFMPEG but it doesn't always work
            float FPS = VideoParams.FPS(videoFilePath);

            FFmpegMediaInfo videoInfo = new FFmpegMediaInfo(videoFilePath, jobStatus, Log.AppLog, true); // cannot suspend during a UI request else it hangs
            if (videoInfo.Success && !videoInfo.ParseError)
            {
                if ((FPS > 0) && (FPS <= videoInfo.MediaInfo.VideoInfo.FPS)) // Check _fps, sometimes MediaInfo get it below 0 or too high (most times it's reliable)
                    videoInfo.MediaInfo.VideoInfo.FPS = FPS; // update it
            }
            else
            {
                Log.AppLog.WriteEntry("Error trying to get Audio Video information. Unable to Read Media File -> " + videoFilePath, Log.LogEntryType.Error, true);
            }

            return videoInfo.MediaInfo;
        }

        public bool WithinConversionTimes()
        {
            // We can take a lock here since this is not called from MonitorThread
            Monitor.Enter(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode
            bool retVal = CheckIfWithinConversionTime();
            Monitor.Exit(monitorLock); // Need to ensure when this function is called, the Engine is not in Starting or stopping mode

            return retVal;
        }




        /// <summary>
        /// Checks if the current time is within the configured Conversion Start times
        /// </summary>
        /// <returns>True is it is within the configured conversion start time</returns>
        private bool CheckIfWithinConversionTime()
        {
            // This functional CANNOT take a monitor lock directly - no function called from MonitorThread can take a Monitor Lock
            if ((_startHour < 0) || (_startMinute < 0) || (_stopHour < 0) || (_stopMinute < 0))
                return true;

            bool retVal;
            DateTime rn = DateTime.Now; // Get a the current reference DateTime and keep fixed to avoid midnight issues
            DateTime startTime, endTime;

            // Find the next day/date/time of the week (starting today) when we are scheduled to run
            startTime = GetNextScheduledDateTime(_startHour, _startMinute, _daysOfWeek, rn.AddDays(-1)); // Get the next start time based on the allowed days of week, starting yesterday
            endTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, _stopHour, _stopMinute, 0); // End time is same day but with different hour and minute

            // Fix for start time > end time (e.g. 11PM to 11AM), then we assumed the end time is the next day
            if (startTime > endTime)
                endTime = endTime.AddDays(1); // we jump the endTime forward to next day

            retVal = ((rn >= startTime) && (rn <= endTime)); // If we are between Start and Stop time (StartTime and EndTime take care of Day of the week also

            return retVal;
        }

        /// <summary>
        /// Create administrative access to directory for reading, writing and modifying
        /// </summary>
        /// <param name="dirPath">Path to directory</param>
        private void CreateExposedDirectory(string dirPath) // this enables us to take administrative right on a directory and files that was installed by a 3rd party (e.g. installer) so we can write to it
        {
            if (!Directory.Exists(dirPath))
            {
                try
                {
                    Directory.CreateDirectory(dirPath);
                }
                catch (Exception e)
                {
                    Log.WriteSystemEventLog("Unable to create directory during installation. Error:" + e.ToString(), EventLogEntryType.Error);
                }
            }

            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);

            SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
            NTAccount acct = sid.Translate(typeof(System.Security.Principal.NTAccount)) as System.Security.Principal.NTAccount;

            FileSystemAccessRule rule = new FileSystemAccessRule(acct.ToString(), FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
            if (!dirInfo.Exists)
            {
                DirectorySecurity security = new DirectorySecurity();
                security.SetAccessRule(rule);
                dirInfo.Create(security);
            }
            else
            {
                DirectorySecurity security = dirInfo.GetAccessControl();
                security.AddAccessRule(rule);
                dirInfo.SetAccessControl(security);
            }
        }

        /// <summary>
        /// Setup the engine configuration from the GlobalMCEConfig object
        /// </summary>
        private void ReadConfig()
        {
            GeneralOptions go = MCEBuddyConf.GlobalMCEConfig.GeneralOptions;

            // First check for custom profiles.conf
            if (File.Exists(go.customProfilePath))
                GlobalDefs.ProfileFile = go.customProfilePath;
            else // reset it
                GlobalDefs.ProfileFile = Path.Combine(GlobalDefs.ConfigPath, "profiles.conf");

            if ((!String.IsNullOrEmpty(go.tempWorkingPath)) && (!Directory.Exists(go.tempWorkingPath)))
            {
                Log.AppLog.WriteEntry(this, "Temporary working directory " + go.tempWorkingPath + " does not exist! Trying to create it.", Log.LogEntryType.Information, true);
                try
                {
                    Directory.CreateDirectory(go.tempWorkingPath);
                }
                catch (Exception e)
                {
                    Log.AppLog.WriteEntry(this, "Unable to create temporary working directory " + go.tempWorkingPath + ". Using default working directory. Error: " + e.ToString(), Log.LogEntryType.Error, true);
                    go.tempWorkingPath = "";
                }
            }
            int logLevel = go.logLevel;
            if ((logLevel < 0) || (logLevel > 3)) logLevel = 1;
            Log.LogLevel = (Log.LogEntryType)logLevel;

            _maxConcurrentJobs = go.maxConcurrentJobs;
            _pollPeriod = go.pollPeriod;
            logKeepDays = go.logKeepDays;

            _wakeHour = go.wakeHour;
            _wakeMinute = go.wakeMinute; 
            _startHour = go.startHour;
            _startMinute = go.startMinute;
            _stopHour = go.stopHour;
            _stopMinute = go.stopMinute;
            if ((_startHour < 0) || (_startHour > 23)) _startHour = -1;
            if ((_startMinute < 0) || (_startMinute > 59)) _startMinute = -1;
            if ((_stopHour < 0) || (_stopHour > 23)) _stopHour = -1;
            if ((_stopMinute < 0) || (_stopMinute > 59)) _stopMinute = -1;
            if ((_wakeHour < 0) || (_wakeHour > 23)) _wakeHour = -1;
            if ((_wakeMinute < 0) || (_wakeMinute > 59)) _wakeMinute = -1;

            _daysOfWeek = go.daysOfWeek.Split(',');

            _deleteOriginal = go.deleteOriginal;
            _useRecycleBin = go.useRecycleBin;
            _archiveOriginal = go.archiveOriginal;
            _failedMoveOriginal = !String.IsNullOrWhiteSpace(go.failedPath);
            _userAllowSleep = go.allowSleep;

            ChangePriority(go.processPriority); // Set the default process priority on load

            _conversionJobs = new ConversionJob[_maxConcurrentJobs]; // Update the number of jobs
            _queueManager = new QueueManager(); // update the search paths and UNC credentials

            _uPnPEnabled = go.uPnPEnable;
            _firewallExceptionEnabled = go.firewallExceptionEnabled;

            string locale = go.locale;
            Localise.Init(locale);

            Log.AppLog.WriteEntry(this, Localise.GetPhrase("Loaded MCEBuddy engine settings"), Log.LogEntryType.Debug, true);
            
            //Debug, dump all the settings to help with debugging
            Log.AppLog.WriteEntry("Windows OS Version -> " + Util.OSVersion.TrueOSVersion.ToString() + " (" + Util.OSVersion.GetOSVersion().ToString() + ", " + Util.OSVersion.GetOSProductType() + ")", Log.LogEntryType.Information);
            Log.AppLog.WriteEntry("Windows Platform -> " + (Environment.Is64BitOperatingSystem ? "64 Bit" : "32 Bit"), Log.LogEntryType.Information);
            Log.AppLog.WriteEntry("MCEBuddy Build Platform -> " + ((IntPtr.Size == 4) ? "32 Bit" : "64 Bit"), Log.LogEntryType.Information);
            string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Log.AppLog.WriteEntry("MCEBuddy Build Version : " + currentVersion, Log.LogEntryType.Information);
            Log.AppLog.WriteEntry("MCEBuddy Build Date : " + File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Information);
            Log.AppLog.WriteEntry("MCEBuddy Running as Service : " + GlobalDefs.IsEngineRunningAsService.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Information);
            Log.AppLog.WriteEntry("Locale Language -> " + Localise.ThreeLetterISO().ToUpper(), Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry(this, "MCEBuddy engine settings -> " + MCEBuddyConf.GlobalMCEConfig.ToString(), Log.LogEntryType.Debug);
        }

        /// <summary>
        /// Returns the number of days to add to the given day of
        /// the week in order to calculate the next occurrence of the
        /// desired day of the week.
        /// </summary>
        /// <param name="current">
        ///		The current day of the week.
        /// </param>
        /// <param name="desired">
        ///		The desired day of the week.
        /// </param>
        /// <returns>
        ///		The number of days to add to <var>current</var> day of week
        ///		in order to achieve the next <var>desired</var> day of week.
        /// </returns>
        private int DaysToAdd(DayOfWeek current, DayOfWeek desired)
        {
            // f( c, d ) = [7 - (c - d)] mod 7
            //   where 0 <= c < 7 and 0 <= d < 7

            int c = (int)current;
            int d = (int)desired;
            return (7 - c + d) % 7;
        }

        /// <summary>
        /// Returns the next Scheduled Start Date and Time
        /// </summary>
        /// <param name="hour">Schedule start Hour</param>
        /// <param name="minute">Schedule start Minute</param>
        /// <param name="daysOfWeek">Days of the week when for the Schedule is enabled</param>
        /// <param name="reference">Reference DateTime from when to calculate the next scheduled start</param>
        /// <returns>DateTime of the next scheduled start</returns>
        private DateTime GetNextScheduledDateTime(int hour, int minute, string[] daysOfWeek, DateTime reference)
        {
            //Calc the next start date and time based on the reference Year, Month and Day
            DateTime nextStart = new DateTime(reference.Year, reference.Month, reference.Day, hour, minute, 0);
            
            // Check if the next Start is before the reference in which case it'll be the next day
            if (nextStart < reference) nextStart = nextStart.AddDays(1);

            // Now re-calculate the next day of the week to set the next start Date/Time based on what days are chosen
            // First get the current scheduled start day of the week
            DayOfWeek cd = nextStart.DayOfWeek;

            // Find the next day of the week enabled for a start, loop through all the days of a the week (0-> Sunday, 6->Saturday)
            while (true)
            {
                if (daysOfWeek.Contains(cd.ToString()))
                {
                    nextStart = nextStart.AddDays(DaysToAdd(nextStart.DayOfWeek, cd));
                    break;
                }

                cd = (DayOfWeek)(((int)cd + 1) % 7); // Check the next day, loop around EOW
            }

            return nextStart;
        }
        
        /// <summary>
        /// Sets the next wake timer after the system wakesup from sleep
        /// </summary>
        private void SetNextWake()
        {
            if ((_wakeHour < 0) || (_wakeHour > 23) || (_wakeMinute < 0) ||( _wakeMinute > 59))
                return;

            //Calc the next wake time
            DateTime wakeTime = GetNextScheduledDateTime(_wakeHour, _wakeMinute, _daysOfWeek, DateTime.Now);

            //Set the wake timer
            if (_wakeUp != null) // if it null then MCEBuddy has been stopped or shutdown, no timer - will be reset when the engine starts
            {
                _wakeUp.Abort();
                _wakeUp.SetWakeUpTime(wakeTime);
            }
        }

        /// <summary>
        /// System entry after the systems awakes from sleep (configured wake timer from MCEBuddy)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WakeUp_Woken(object sender, EventArgs e)
        {
            SetNextWake();
        }

        /// <summary>
        /// List of valid statuses for Recorded Source files
        /// </summary>
        public static string[] SourceFileStatuses
        { get { return new string[] { "Converted", "Error", "Cancelled" }; } }

        /// <summary>
        /// List of valid statuses for Converted files
        /// </summary>
        public static string[] ConvertedFileStatuses
        { get { return new string[] { "OutputFromConversion" }; } }

        private void WriteHistory(ConversionJob job)
        {
            Ini historyIni = new Ini(GlobalDefs.HistoryFile);
            GeneralOptions go = MCEBuddyConf.GlobalMCEConfig.GeneralOptions;

            bool sendEMail = go.sendEmail; // do we need to send an eMail after each job
            bool sendSuccess = go.eMailSettings.successEvent;
            bool sendFailed = go.eMailSettings.failedEvent;
            bool sendCancelled = go.eMailSettings.cancelledEvent;
            bool skipBody = go.eMailSettings.skipBody;
            string sendSuccessSubject = go.eMailSettings.successSubject;
            string sendFailedSubject = go.eMailSettings.failedSubject;
            string sendCancelledSubject = go.eMailSettings.cancelledSubject;

            string result = "Converted";
            if (job.Status.Error) result = "Error";
            if (job.Status.Cancelled) result = "Cancelled"; // Cancelled should be the last status to set because an error can be set if is cancelled

            int convCount = 0;
            while (historyIni.ReadString(job.OriginalFileName, result + "At" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), "") != "")
                convCount++;
            historyIni.Write(job.OriginalFileName, result + "At" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture));

            switch (result)
            {
                case "Converted":
                    {
                        if (job.Status.SuccessfulSkipConversion)
                            Log.AppLog.WriteEntry(this, "Job for " + job.OriginalFileName + " skipped ReConverting successfully using Conversion Task " + job.TaskName + " and Profile " + job.Profile, Log.LogEntryType.Information, true);
                        else
                            Log.AppLog.WriteEntry(this, "Job for " + job.OriginalFileName + " converted successfully to " + job.ConvertedFile + " using Conversion Task " + job.TaskName + " and Profile " + job.Profile, Log.LogEntryType.Information, true);

                        historyIni.Write(job.OriginalFileName, "ConvertedTo" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), job.ConvertedFile);
                        historyIni.Write(job.OriginalFileName, "ConvertedUsingProfile" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), job.Profile);
                        historyIni.Write(job.OriginalFileName, "ConvertedUsingTask" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), job.TaskName);
                        historyIni.Write(job.OriginalFileName, "ConvertedStart" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), job.ConversionStartTime.ToString("s", System.Globalization.CultureInfo.InvariantCulture));

                        //Ensure converted files are not then re-converted during a scan
                        historyIni.Write(job.ConvertedFile, "Status", "OutputFromConversion"); // Status indicates destination file is output of an conversion, if the same file is added back for reconversion then it would log as converted
                        if (job.Status.SuccessfulSkipConversion)
                            historyIni.Write(job.ConvertedFile, "SkipConvertedToOutputAt" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), job.ConversionEndTime.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
                        else
                            historyIni.Write(job.ConvertedFile, "ConvertedToOutputAt" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), job.ConversionEndTime.ToString("s", System.Globalization.CultureInfo.InvariantCulture));

                        // Send an eMail if required
                        if (sendEMail && sendSuccess)
                        {
                            string subject = Localise.GetPhrase("MCEBuddy successfully converted a video");
                            string message = Localise.GetPhrase("Source Video") + " -> " + job.OriginalFileName + "\r\n";
                            message += Localise.GetPhrase("Converted Video") + " -> " + job.ConvertedFile + "\r\n";
                            message += Localise.GetPhrase("Profile") + " -> " + job.Profile + "\r\n";
                            message += Localise.GetPhrase("Conversion Task") + " -> " + job.TaskName + "\r\n";
                            message += Localise.GetPhrase("Converted At") + " -> " + job.ConversionEndTime.ToString("s", System.Globalization.CultureInfo.InvariantCulture) + "\r\n";
                            message += Localise.GetPhrase("Time taken to convert") + " (hh:mm) -> " + (job.ConversionEndTime - job.ConversionStartTime).Hours.ToString("00") + ":" + (job.ConversionEndTime - job.ConversionStartTime).Minutes.ToString("00") + "\r\n";

                            // Check for custom subject and process
                            if (!String.IsNullOrWhiteSpace(sendSuccessSubject))
                                subject = UserCustomParams.CustomParamsReplace(
                                  sendSuccessSubject, 
                                  job.WorkingPath, 
                                  Path.GetDirectoryName(job.ConvertedFile), 
                                  job.ConvertedFile, 
                                  job.OriginalFileName, 
                                  "", 
                                  "", 
                                  "", 
                                  job.Profile, 
                                  job.TaskName, 
                                  //job.ConversionJobOptions.relativeSourcePath, 
                                  job.MetaData, 
                                  Log.AppLog);

                            eMailSendEngine.AddEmailToSendQueue(subject, (skipBody ? "" : message)); // Send the eMail through the email engine
                        }
                    }
                    break;

                case "Error":
                    {
                        Log.AppLog.WriteEntry(this, "Job for " + job.OriginalFileName + " has Error " + job.Status.ErrorMsg + " using Conversion Task " + job.TaskName + " and Profile " + job.Profile, Log.LogEntryType.Information, true);

                        historyIni.Write(job.OriginalFileName, "ErrorMessage" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), job.Status.ErrorMsg);
                        historyIni.Write(job.OriginalFileName, "ErrorUsingProfile" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), job.Profile);
                        historyIni.Write(job.OriginalFileName, "ErrorUsingTask" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), job.TaskName);

                        // Send an eMail if required
                        if (sendEMail && sendFailed)
                        {
                            string subject = Localise.GetPhrase("MCEBuddy failed to converted a video");
                            string message = Localise.GetPhrase("Source Video") + " -> " + job.OriginalFileName + "\r\n";
                            message += Localise.GetPhrase("Profile") + " -> " + job.Profile + "\r\n";
                            message += Localise.GetPhrase("Conversion Task") + " -> " + job.TaskName + "\r\n";
                            message += Localise.GetPhrase("Error") + " -> " + job.Status.ErrorMsg + "\r\n";
                            message += Localise.GetPhrase("Failed At") + " -> " + job.ConversionEndTime.ToString("s", System.Globalization.CultureInfo.InvariantCulture) + "\r\n";

                            // Check for custom subject and process
                            if (!String.IsNullOrWhiteSpace(sendFailedSubject))
                                subject = UserCustomParams.CustomParamsReplace(
                                  sendFailedSubject, 
                                  job.WorkingPath, 
                                  "", 
                                  job.ConvertedFile, 
                                  job.OriginalFileName, 
                                  "", 
                                  "", 
                                  "", 
                                  job.Profile, 
                                  job.TaskName, 
                                  //job.ConversionJobOptions.relativeSourcePath, 
                                  job.MetaData, 
                                  Log.AppLog);

                            eMailSendEngine.AddEmailToSendQueue(subject, (skipBody ? "" : message)); // Send the eMail through the eMail engine
                        }
                    }
                    break;

                case "Cancelled":
                    {
                        Log.AppLog.WriteEntry(this, "Job for " + job.OriginalFileName + " Cancelled using Conversion Task " + job.TaskName + " and Profile " + job.Profile, Log.LogEntryType.Information, true);
                        historyIni.Write(job.OriginalFileName, "CancelledUsingProfile" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), job.Profile);
                        historyIni.Write(job.OriginalFileName, "CancelledUsingTask" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), job.TaskName);

                        // Send an eMail if required
                        if (sendEMail && sendCancelled)
                        {
                            string subject = Localise.GetPhrase("MCEBuddy cancelled a video conversion");
                            string message = Localise.GetPhrase("Source Video") + " -> " + job.OriginalFileName + "\r\n";
                            message += Localise.GetPhrase("Profile") + " -> " + job.Profile + "\r\n";
                            message += Localise.GetPhrase("Conversion Task") + " -> " + job.TaskName + "\r\n";
                            message += Localise.GetPhrase("Cancelled At") + " -> " + DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture) + "\r\n"; // It can be cancelled before it starts so end time is not a good indicator

                            // Check for custom subject and process
                            if (!String.IsNullOrWhiteSpace(sendCancelledSubject))
                                subject = UserCustomParams.CustomParamsReplace(
                                  sendCancelledSubject, 
                                  job.WorkingPath, 
                                  "", 
                                  job.ConvertedFile, 
                                  job.OriginalFileName, 
                                  "", 
                                  "", 
                                  "", 
                                  job.Profile, 
                                  job.TaskName, 
                                  //job.ConversionJobOptions.relativeSourcePath, 
                                  job.MetaData, 
                                  Log.AppLog);

                            eMailSendEngine.AddEmailToSendQueue(subject, (skipBody ? "" : message)); // Send the eMail through the eMail engine
                        }
                    }
                    break;

                default:
                    Log.AppLog.WriteEntry(this, "INVALID STATE (" + result + ") -> Job for " + job.OriginalFileName + " Converted using Conversion Task " + job.TaskName + " and Profile " + job.Profile, Log.LogEntryType.Error, true);

                    break;
            }

            if ( _queueManager.JobCount(job.OriginalFileName) <= 1)
            {
                // This is the last job processing this file, so write the status to the history file STATUS
                historyIni.Write(job.OriginalFileName, "Status", result); // Source file status
            }
        }

        /// <summary>
        /// Checks the log folder to clean up old log files if required
        /// </summary>
        /// <returns></returns>
        private void CleanLogFiles()
        {
            if (logKeepDays == 0)
                return; // Keep log files forever

            IEnumerable<string> foundFiles;
            try
            {
                // Check if the MCEBuddy.log (global log) is too large, if so clear it first
                if (Util.FileIO.FileSize(GlobalDefs.AppLogFile) > GlobalDefs.GLOBAL_LOGFILE_SIZE_THRESHOLD)
                {
                    Log.AppLog.Clear();
                    Log.AppLog.WriteEntry("MCEBuddy Log file cleared since it was larger than " + ((GlobalDefs.GLOBAL_LOGFILE_SIZE_THRESHOLD) / 1024 / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture) + " MB, starting afresh.", Log.LogEntryType.Warning, true);
                }

                foundFiles = FilePaths.GetDirectoryFiles(GlobalDefs.LogPath, "*.log", SearchOption.TopDirectoryOnly).OrderBy(File.GetLastWriteTime); // We sort the files by last modified time

                if (foundFiles != null)
                {
                    foreach (string foundFile in foundFiles)
                    {
                        // Found a file
                        if (File.GetLastWriteTime(foundFile).AddDays(logKeepDays) < DateTime.Now) // check if the log file is older than requested
                        {
                            Log.AppLog.WriteEntry("Deleting log file" + " " + foundFile + "\r\n", Log.LogEntryType.Debug, true);
                            Util.FileIO.TryFileDelete(foundFile); // Delete the log file
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Log.AppLog.WriteEntry(Localise.GetPhrase("Unable to search for log files in location") + " " + GlobalDefs.LogPath + "\r\n" + ex.Message, Log.LogEntryType.Warning);
                foundFiles = new List<string>();
            }

            return;
        }

        /// <summary>
        /// Create an exception in the firewall for MCEBuddy (port and application)
        /// </summary>
        /// <param name="createException">True to create, false to remove</param>
        /// <returns>True if successful</returns>
        private bool FirewallException(bool createException)
        {
            try
            {
                if (createException)
                {
                    Firewall.AuthorizeApplication("MCEBuddy2x", Path.Combine(GlobalDefs.AppPath, @"MCEBuddy.Service.exe"), Firewall.NET_FW_SCOPE.NET_FW_SCOPE_ALL, Firewall.NET_FW_ACTION.NET_FW_ACTION_ALLOW, Firewall.NET_FW_IP_VERSION.NET_FW_IP_VERSION_ANY); // Open the firewall
                    Firewall.AuthorizePort("MCEBuddy2x Port", MCEBuddyConf.GlobalMCEConfig.GeneralOptions.localServerPort, Firewall.NET_FW_SCOPE.NET_FW_SCOPE_ALL, Firewall.NET_FW_IP_PROTOCOL.NET_FW_IP_PROTOCOL_TCP, Firewall.NET_FW_IP_VERSION.NET_FW_IP_VERSION_ANY); // Authorize port
                    Log.WriteSystemEventLog("MCEBuddy creating firewall exception complete", EventLogEntryType.Information);
                }
                else
                {
                    Firewall.CleanUpFirewall("MCEBuddy2x", Path.Combine(GlobalDefs.AppPath, @"MCEBuddy.Service.exe"), "MCEBuddy2x Port", MCEBuddyConf.GlobalMCEConfig.GeneralOptions.localServerPort, Firewall.NET_FW_IP_PROTOCOL.NET_FW_IP_PROTOCOL_TCP); // Clean up firewall etries
                    Log.WriteSystemEventLog("MCEBuddy removing firewall exception complete", EventLogEntryType.Information);
                }
            }
            catch (Exception e)
            {
                if (createException)
                    Log.WriteSystemEventLog("MCEBuddy error enabling firewall exception. Error -> \r\n" + e.ToString(), EventLogEntryType.Error);
                else
                    Log.WriteSystemEventLog("MCEBuddy error disabling firewall exception. Error -> \r\n" + e.ToString(), EventLogEntryType.Error);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Thread to keep monitoring status of UPnP and keep the mappings active
        /// </summary>
        private void UPnPMonitorThread()
        {
            Log.WriteSystemEventLog("MCEBuddy starting UPnP Monitor Thread", EventLogEntryType.Information);

            // Check/Enable UPnP - verbose
            UPnP.EnableUPnP(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.localServerPort, true);

            try
            {
                while (_uPnPEnabled) // Check this since sometimes the Join/Abort signal may not come (EnableUPnP uses try/catch which can interrupt a join/abort)
                {
                    Thread.Sleep(GlobalDefs.UPNP_POLL_PERIOD); // Wait for a while to repoll

                    // Check/Enable UPnP - non verbose
                    UPnP.EnableUPnP(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.localServerPort, false);
                }
            }
            catch // Catch an Abort or Join
            {
                // Disable UPnP Port Forwarding - verbose
                UPnP.DisableUPnP(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.localServerPort, true);

                _uPnPCheckThread = null; // This thread is dead
                Log.WriteSystemEventLog("MCEBuddy exiting UPnP Monitor Thread - abort successful", EventLogEntryType.Information);
                return;
            }

            // Just incase the Abort or Join exception was not caught
            // Disable UPnP Port Forwarding - verbose
            UPnP.DisableUPnP(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.localServerPort, true);

            _uPnPCheckThread = null; // This thread is dead
            Log.WriteSystemEventLog("MCEBuddy exiting UPnP Monitor Thread - abort Failed", EventLogEntryType.Information);
            return;
        }

        /// <summary>
        /// Main CORE engine thread of MCEBuddy which runs in the background to check for new jobs, scans for new files, checks for updates to MCEBuddy etc
        /// </summary>
        private void MonitorThread() // Background thread that check for job starting, additions and completion
        {
            Thread updateCheckThread = null, scanThread = null, syncThread = null;

            try
            {
                DateTime lastUpdateCheck = DateTime.Now.AddDays(-1);
                DateTime lastPollCheck = DateTime.Now.AddSeconds(-_pollPeriod);
                GlobalDefs.Shutdown = false;
                while (!GlobalDefs.Shutdown)
                {
                    // Check for updated version and messages
                    if (DateTime.Now > lastUpdateCheck.AddHours(GlobalDefs.random.Next(12, 25)))
                    {
                        Log.AppLog.WriteEntry(this, Localise.GetPhrase("Checking for new version of MCEBuddy"), Log.LogEntryType.Information);
                        lastUpdateCheck = DateTime.Now;

                        if ((updateCheckThread != null) && updateCheckThread.IsAlive)
                            continue; // Don't start another one yet
                        updateCheckThread = new Thread(new ThreadStart(_updateCheck.Check));
                        updateCheckThread.SetApartmentState(ApartmentState.STA);
                        updateCheckThread.CurrentCulture = updateCheckThread.CurrentUICulture = Localise.MCEBuddyCulture;
                        updateCheckThread.IsBackground = true; // Kill the thread if the process exits
                        updateCheckThread.Start();
                    }

                    //Check for new files and clean up log files
                    if ((DateTime.Now > lastPollCheck.AddSeconds(_pollPeriod)) || (_rescan))
                    {
                        _rescan = false;
                        lastPollCheck = DateTime.Now;

                        // Check for new files in the monitor folders - run as thread to keep it responsive
                        if ((scanThread != null) && scanThread.IsAlive)
                            continue; // Don't start another one yet
                        scanThread = new Thread(() => _queueManager.ScanForFiles());
                        scanThread.CurrentCulture = scanThread.CurrentUICulture = Localise.MCEBuddyCulture;
                        scanThread.IsBackground = true; // Kill the thread if the process exits
                        scanThread.Start();

                        // check if converted files need to kept in sync with source files (deleted) - run as thread to keep it responsive
                        if (MCEBuddyConf.GlobalMCEConfig.GeneralOptions.deleteConverted)
                        {
                            if ((syncThread != null) && syncThread.IsAlive)
                                continue; // Don't start another one yet
                            syncThread = new Thread(() => _queueManager.SyncConvertedFiles(_useRecycleBin)); // Check for new files in the monitor folders
                            syncThread.CurrentCulture = syncThread.CurrentUICulture = Localise.MCEBuddyCulture;
                            syncThread.IsBackground = true; // Kill the thread if the process exits
                            syncThread.Start();
                        }

                        // check for log files clean up
                        CleanLogFiles();                        
                    }

                    // Resume jobs if we are within the conversion time
                    if (CheckIfWithinConversionTime()) // Call the internal function, cannot take a lock
                    {
                        // We are within the conversion time, if the engine has not auto resumed, then resume it
                        if (_autoPause) // If we are auto paused, then resume it since we are within the conversion time
                            _autoPause = GlobalDefs.Pause = false; // Resume it
                    }
                    else
                    {
                        // We are outside the conversion time, if the engine has not auto paused, then pause it
                        if (!_autoPause) // If we are not paused, then pause it since we are outside the conversion time
                            _autoPause = GlobalDefs.Pause = true; // Pause it
                    }

                    //Check the status of the jobs and start new ones 
                    bool SomeRunning = false;
                    for (int i = 0; i < _conversionJobs.Length; i++)
                    {
                        if (_conversionJobs[i] != null)
                        {
                            // Check for running and clean up completed jobs
                            if (_conversionJobs[i].Completed)
                            {
                                Monitor.Enter(_queueManager.Queue); //the lock is always on the jobQueue

                                Log.AppLog.WriteEntry(this, Localise.GetPhrase("Job for") + " " + _conversionJobs[i].OriginalFileName + " " + Localise.GetPhrase("completed"), Log.LogEntryType.Information, true);

                                // Now delete the job from the manual file entry, if it has succeeded great, if it has failed we don't want an endless loop of conversion
                                if ((_queueManager.JobCount(_conversionJobs[i].OriginalFileName) <= 1)) // Whether it is cancelled of finished delete from the manual queue either way
                                {
                                    Ini iniManualQueue = new Ini(GlobalDefs.ManualQueueFile);
                                    iniManualQueue.DeleteKey("ManualQueue", _conversionJobs[i].OriginalFileName);
                                }

                                bool archiveOriginal = false;
                                bool deleteOriginal = false;

                                // Monitor task specific options take priority first for archiving and deleting originals
                                if ((_conversionJobs[i].MonitorJobOptions != null) && (_conversionJobs[i].MonitorJobOptions.archiveMonitorOriginal || _conversionJobs[i].MonitorJobOptions.deleteMonitorOriginal))
                                {
                                    if (_conversionJobs[i].MonitorJobOptions.archiveMonitorOriginal)
                                        archiveOriginal = true;
                                    else if (_conversionJobs[i].MonitorJobOptions.deleteMonitorOriginal)
                                        deleteOriginal = true;
                                }
                                // Check General Options for Archive or Delete original
                                else if (_archiveOriginal)
                                    archiveOriginal = true;
                                else if (_deleteOriginal)
                                    deleteOriginal = true;

                                // Archive only if the conversion was successful and original marked for archiving and it is the last task for the job and the original file and converted file don't have the same name+path (since it's been replaced already)
                                if ((archiveOriginal) && (_conversionJobs[i].Status.SuccessfulConversion) && (_queueManager.JobCount(_conversionJobs[i].OriginalFileName) <= 1) && (String.Compare(_conversionJobs[i].OriginalFileName, _conversionJobs[i].ConvertedFile, true) != 0))
                                {
                                    string pathName = Path.GetDirectoryName(_conversionJobs[i].OriginalFileName); //get the directory name
                                    if (!pathName.ToLower().Contains((string.IsNullOrEmpty(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.archivePath) ? GlobalDefs.MCEBUDDY_ARCHIVE.ToLower() : MCEBuddyConf.GlobalMCEConfig.GeneralOptions.archivePath.ToLower())) // Global Archive Path
                                        && (_conversionJobs[i].MonitorJobOptions == null ? true : !pathName.ToLower().Contains((string.IsNullOrEmpty(_conversionJobs[i].MonitorJobOptions.archiveMonitorPath) ? GlobalDefs.MCEBUDDY_ARCHIVE.ToLower() : _conversionJobs[i].MonitorJobOptions.archiveMonitorPath.ToLower()))) // Monitor task Archive path
                                        ) //check if we are currently operating from the archive folder, in which case don't archive
                                    {
                                        string archivePath;

                                        // First check and use the monitor archive path
                                        if ((_conversionJobs[i].MonitorJobOptions != null) && !String.IsNullOrWhiteSpace(_conversionJobs[i].MonitorJobOptions.archiveMonitorPath))
                                            archivePath = _conversionJobs[i].MonitorJobOptions.archiveMonitorPath;
                                        else
                                            archivePath = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.archivePath; // use the global archive path

                                        if (String.IsNullOrWhiteSpace(archivePath)) // Default archive location to be used
                                            archivePath = Path.Combine(pathName, GlobalDefs.MCEBUDDY_ARCHIVE); //update the path name for a new sub-directory called Archive

                                        Util.FilePaths.CreateDir(archivePath); //create the sub-directory if required
                                        string newFilePath = Path.Combine(archivePath, Path.GetFileName(_conversionJobs[i].OriginalFileName));

                                        Log.AppLog.WriteEntry(this, "Archiving original file " + _conversionJobs[i].OriginalFileName + " to Archive folder " + archivePath, Log.LogEntryType.Debug);

                                        try
                                        {
                                            // If the converted file path and original file path are the same, then don't archive the support file (since they may be created by the converted file)
                                            if (String.Compare(Path.GetDirectoryName(_conversionJobs[i].OriginalFileName), Path.GetDirectoryName(_conversionJobs[i].ConvertedFile), true) != 0)
                                            {
                                                    // Archive the EDL, SRT, XML, NFO etc files also along with the original file if present
                                                    foreach (string supportFileExt in GlobalDefs.supportFilesExt)
                                                    {
                                                        string extFile = Path.Combine(Path.GetDirectoryName(_conversionJobs[i].OriginalFileName), Path.GetFileNameWithoutExtension(_conversionJobs[i].OriginalFileName) + supportFileExt); // Saved support file
                                                        if (File.Exists(extFile))
                                                            FileIO.MoveAndInheritPermissions(extFile, Path.Combine(archivePath, Path.GetFileName(extFile)));
                                                    }
                                            }

                                            // Last file to move
                                            FileIO.MoveAndInheritPermissions(_conversionJobs[i].OriginalFileName, newFilePath); //move the file into the archive folder
                                        }
                                        catch (Exception e)
                                        {
                                            Log.AppLog.WriteEntry(this, "Unable to move original file " + _conversionJobs[i].OriginalFileName + " to Archive folder " + archivePath, Log.LogEntryType.Error);
                                            Log.AppLog.WriteEntry(this, "Error : " + e.ToString(), Log.LogEntryType.Error);
                                        }
                                    }
                                }
                                // Delete only if the conversion was successful and original marked for deletion and it is the last task for the job and the original file and converted file don't have the same name+path (since it's been replaced already)
                                else if ((deleteOriginal) && (_conversionJobs[i].Status.SuccessfulConversion) && (_queueManager.JobCount(_conversionJobs[i].OriginalFileName) <= 1) && (String.Compare(_conversionJobs[i].OriginalFileName, _conversionJobs[i].ConvertedFile, true) != 0))
                                {
                                    Log.AppLog.WriteEntry(this, "Deleting original file " + _conversionJobs[i].OriginalFileName, Log.LogEntryType.Debug, true);

                                    // If the converted file path and original file path are the same, then don't delete the support file (since they may be created by the converted file)
                                    if (String.Compare(Path.GetDirectoryName(_conversionJobs[i].OriginalFileName), Path.GetDirectoryName(_conversionJobs[i].ConvertedFile), true) != 0)
                                    {
                                        // Delete the EDL, SRT, XML, NFO etc files also along with the original file if present
                                        foreach (string supportFileExt in GlobalDefs.supportFilesExt)
                                        {
                                            string extFile = Path.Combine(Path.GetDirectoryName(_conversionJobs[i].OriginalFileName), Path.GetFileNameWithoutExtension(_conversionJobs[i].OriginalFileName) + supportFileExt); // support file
                                            Util.FileIO.TryFileDelete(extFile, _useRecycleBin); // Delete support file
                                        }
                                    }

                                    Util.FileIO.TryFileDelete(_conversionJobs[i].OriginalFileName, _useRecycleBin); // delete original file
                                }

                                // Check of it's a failure and the original file needs to be moved
                                if ((_failedMoveOriginal) && (_conversionJobs[i].Status.Error) && (_queueManager.JobCount(_conversionJobs[i].OriginalFileName) <= 1))
                                {
                                    string pathName = Path.GetDirectoryName(_conversionJobs[i].OriginalFileName); //get the directory name
                                    string failedPath = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.failedPath; // use the specified path to move the file conversion original file

                                    Util.FilePaths.CreateDir(failedPath); //create the sub-directory if required
                                    string newFilePath = Path.Combine(failedPath, Path.GetFileName(_conversionJobs[i].OriginalFileName));

                                    Log.AppLog.WriteEntry(this, "Moving original file " + _conversionJobs[i].OriginalFileName + " to Failed conversion folder " + failedPath, Log.LogEntryType.Debug);

                                    try
                                    {
                                        // Move the EDL, SRT, XML, NFO etc files also along with the original file if present
                                        foreach (string supportFileExt in GlobalDefs.supportFilesExt)
                                        {
                                            string extFile = Path.Combine(Path.GetDirectoryName(_conversionJobs[i].OriginalFileName), Path.GetFileNameWithoutExtension(_conversionJobs[i].OriginalFileName) + supportFileExt); // Saved support file
                                            if (File.Exists(extFile))
                                                FileIO.MoveAndInheritPermissions(extFile, Path.Combine(failedPath, Path.GetFileName(extFile)));
                                        }

                                        // Last file to move
                                        FileIO.MoveAndInheritPermissions(_conversionJobs[i].OriginalFileName, newFilePath); //move the file into the archive folder
                                    }
                                    catch (Exception e)
                                    {
                                        Log.AppLog.WriteEntry(this, "Unable to move original file " + _conversionJobs[i].OriginalFileName + " to Failed conversion folder " + failedPath, Log.LogEntryType.Error);
                                        Log.AppLog.WriteEntry(this, "Error : " + e.ToString(), Log.LogEntryType.Error);
                                    }
                                }

                                //First write the history and finish the job, then remove from the queue
                                WriteHistory(_conversionJobs[i]);

                                _queueManager.Queue.Remove(_conversionJobs[i]);
                                _conversionJobs[i] = null;

                                Monitor.Exit(_queueManager.Queue);
                            }
                            else
                            {
                                SomeRunning = true;
                            }
                        }
                        else
                        {
                            // Start new jobs if conversions are not paused
                            if (!GlobalDefs.Pause)
                            {
                                Monitor.Enter(_queueManager.Queue);
                                for (int j = 0; j < _queueManager.Queue.Count; j++)
                                {
                                    if ((!_queueManager.Queue[j].Completed) && (!_queueManager.Queue[j].Active))
                                    {
                                        _conversionJobs[i] = _queueManager.Queue[j];
                                        
                                        // Checking for a custom temp working path for conversion (other drives/locations)
                                        _conversionJobs[i].WorkingPath = Path.Combine((!String.IsNullOrEmpty(_conversionJobs[i].WorkingPath) ? _conversionJobs[i].WorkingPath // Local temp folder takes precedence
                                                                                            : (!String.IsNullOrEmpty(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.tempWorkingPath) ? MCEBuddyConf.GlobalMCEConfig.GeneralOptions.tempWorkingPath  // Check global temp path
                                                                                                : GlobalDefs.AppPath)) // Nothing specified, use default temp path
                                                                                        , "working" + i.ToString()); // create sub working directories for each simultaneous task otherwise they conflict
                                        
                                        // Start the conversion
                                        _conversionJobs[i].StartConversionThread();

                                        SomeRunning = true; // Update status

                                        // Send an eMail if required
                                        GeneralOptions go = MCEBuddyConf.GlobalMCEConfig.GeneralOptions;
                                        bool sendEMail = go.sendEmail; // do we need to send an eMail after each job
                                        bool sendStart = go.eMailSettings.startEvent;
                                        string sendStartSubject = go.eMailSettings.startSubject;
                                        bool skipBody = go.eMailSettings.skipBody;
                                        if (sendEMail && sendStart)
                                        {
                                            string subject = Localise.GetPhrase("MCEBuddy started a video conversion");
                                            string message = Localise.GetPhrase("Source Video") + " -> " + _conversionJobs[i].OriginalFileName + "\r\n";
                                            message += Localise.GetPhrase("Profile") + " -> " + _conversionJobs[i].Profile + "\r\n";
                                            message += Localise.GetPhrase("Conversion Task") + " -> " + _conversionJobs[i].TaskName + "\r\n";
                                            message += Localise.GetPhrase("Conversion Started At") + " -> " + DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture) + "\r\n";

                                            // Check for custom subject and process
                                            if (!String.IsNullOrWhiteSpace(sendStartSubject))
                                                subject = UserCustomParams.CustomParamsReplace(
                                                  sendStartSubject, 
                                                  _conversionJobs[i].WorkingPath, 
                                                  "", 
                                                  _conversionJobs[i].ConvertedFile, 
                                                  _conversionJobs[i].OriginalFileName, 
                                                  "", 
                                                  "", 
                                                  "", 
                                                  _conversionJobs[i].Profile, 
                                                  _conversionJobs[i].TaskName, 
                                                  //_conversionJobs[i].ConversionJobOptions.relativeSourcePath, 
                                                  _conversionJobs[i].MetaData, 
                                                  Log.AppLog);

                                            eMailSendEngine.AddEmailToSendQueue(subject, (skipBody ? "" : message)); // Send the eMail through the eMail engine
                                        }

                                        Log.AppLog.WriteEntry(this, "Job for " + _conversionJobs[i].OriginalFileName + " started using Conversion Task " + _conversionJobs[i].TaskName + " and Profile " + _conversionJobs[i].Profile, Log.LogEntryType.Information, true);
                                        Log.AppLog.WriteEntry(this, "Temp working path is " + _conversionJobs[i].WorkingPath, Log.LogEntryType.Debug, true);
                                        break;
                                    }
                                }
                                Monitor.Exit(_queueManager.Queue);
                            }
                        }
                    }

                    if ((GlobalDefs.Active) && !SomeRunning) // Was running jobs earlier, no active jobs now
                    {
                        Log.AppLog.WriteEntry(this, Localise.GetPhrase("No conversions running , allowing system sleep"), Log.LogEntryType.Debug, true);
                        Util.PowerManagement.AllowSleep();
                        _allowSleep = true;
                    }
                    else if ((!GlobalDefs.Active) && SomeRunning) // Wasn't running jobs earlier, has new active jobs now
                    {
                        if (_userAllowSleep) // User allows sleep while converting
                        {
                            Log.AppLog.WriteEntry(this, Localise.GetPhrase("Starting new conversions, allowing system sleep"), Log.LogEntryType.Debug, true);
                            Util.PowerManagement.AllowSleep();
                            _allowSleep = true;
                        }
                        else
                        {
                            Log.AppLog.WriteEntry(this, Localise.GetPhrase("Starting new conversions, preventing system sleep"), Log.LogEntryType.Debug, true);
                            Util.PowerManagement.PreventSleep();
                            _allowSleep = false;
                        }
                    }

                    // Check if the conversion is paused while the job is active, if so allow sleep
                    if (GlobalDefs.Pause && SomeRunning && !_allowSleep)
                    {
                        Log.AppLog.WriteEntry(this, Localise.GetPhrase("Active jobs paused, allowing system sleep"), Log.LogEntryType.Debug, true);
                        Util.PowerManagement.AllowSleep();
                        _allowSleep = true;
                    }
                    else if (!GlobalDefs.Pause && SomeRunning && !_userAllowSleep && _allowSleep) // disable sleep once the job has been resumed and user does not allow sleeping while converting
                    {
                        Log.AppLog.WriteEntry(this, Localise.GetPhrase("Active jobs resumed, user does not allow sleep while converting, preventing system sleep"), Log.LogEntryType.Debug, true);
                        Util.PowerManagement.PreventSleep();
                        _allowSleep = false;
                    }

                    GlobalDefs.Active = SomeRunning;
                    _engineCrashed = false; // we've reached here so, reset it

                    // Sleep then shutdown check
                    Thread.Sleep(GlobalDefs.ENGINE_CORE_SLEEP_PERIOD);
                }

                // Shutdown support threads
                if ((syncThread != null) && syncThread.IsAlive)
                    syncThread.Abort();
                if ((scanThread != null) && scanThread.IsAlive)
                    scanThread.Abort();
                if ((updateCheckThread != null) && updateCheckThread.IsAlive)
                    updateCheckThread.Abort();

                // Shut down the conversion threads
                Monitor.Enter(_queueManager.Queue);
                for (int i = 0; i < _conversionJobs.Length; i++)
                {
                    if (_conversionJobs[i] != null)
                    {
                        _conversionJobs[i].StopConversionThread();
                        _conversionJobs[i] = null;
                    }
                }
                _queueManager.Queue.Clear(); // Clear the entire queue
                Monitor.Exit(_queueManager.Queue);

                Util.PowerManagement.AllowSleep(); //reset to default
                _allowSleep = true;
                GlobalDefs.Active = false;
                _monitorThread = null; // this thread is done
            }
            catch (Exception e)
            {
                _autoPause = GlobalDefs.Pause = false; // Reset suspension state
                GlobalDefs.Shutdown = true; // Terminate everything since we had a major crash

                // Shutdown support threads
                if ((syncThread != null) && syncThread.IsAlive)
                    syncThread.Abort();
                if ((scanThread != null) && scanThread.IsAlive)
                    scanThread.Abort();
                if ((updateCheckThread != null) && updateCheckThread.IsAlive)
                    updateCheckThread.Abort();

                // Release the queue lock if taken
                try { Monitor.Exit(_queueManager.Queue); } // Incase it's taken release it, if not taken it will throw an exception
                catch { }

                Util.PowerManagement.AllowSleep(); //reset to default
                _allowSleep = true;
                GlobalDefs.Active = false;
                _engineCrashed = true;

                GeneralOptions go = MCEBuddyConf.GlobalMCEConfig.GeneralOptions;
                go.engineRunning = false;
                MCEBuddyConf.GlobalMCEConfig.UpdateGeneralOptions(go, true); // Write the stop engine settings, since it crashed

                Log.AppLog.WriteEntry(this, "MCEBuddy Monitor Thread Crashed. Error: " + e.ToString(), Log.LogEntryType.Error, true); // This may or may not work depending upon whether the llog has been initialized otherwise it goes into NULL log
                Log.WriteSystemEventLog("MCEBuddy Monitor Thread Crashed. Error: " + e.ToString(), EventLogEntryType.Error);

                _monitorThread = null; // this thread is done
            }
        }
    }
}
