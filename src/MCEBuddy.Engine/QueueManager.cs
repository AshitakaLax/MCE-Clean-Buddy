using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using MCEBuddy.Globals;
using MCEBuddy.Engine;
using MCEBuddy.Util;
using MCEBuddy.Configuration;
using MCEBuddy.MetaData;
using MCEBuddy.EMailEngine;

namespace MCEBuddy.Engine
{
    public class QueueManager
    {
        /// <summary>
        /// Job queue for all conversion jobs
        /// </summary>
        private List<ConversionJob> _jobQueue = new List<ConversionJob>();
        /// <summary>
        /// Keep track of converted files
        /// </summary>
        private HashSet<string> _processedFiles = new HashSet<string>();
        /// <summary>
        /// Keep track of monitor task file type filter mismatched files for each monitor task (filename + monitor task processed)
        /// </summary>
        private Dictionary<string, List<string>> _monitorTaskFilterMismatchFiles = new Dictionary<string, List<string>>();
        /// <summary>
        /// Keep track of conversion task filter mismatch files
        /// </summary>
        private HashSet<string> _conversionTaskFilterMismatchFiles = new HashSet<string>();
        /// <summary>
        /// Keep track of archived files
        /// </summary>
        private HashSet<string> _archivedFiles = new HashSet<string>();
        /// <summary>
        /// Keep track of source/converted files being monitored for syncing
        /// </summary>
        private HashSet<string> _monitorSyncFiles = new HashSet<string>();

        private int _minimumAge = 0;

        public QueueManager()
        {
            // Connect net drives and setup search records for source locations
            foreach (MonitorJobOptions mjo in MCEBuddyConf.GlobalMCEConfig.AllMonitorTasks)
            {
                string searchPath = mjo.searchPath;
                string domainName = mjo.domainName;
                string userName = mjo.userName;
                string password = mjo.password;

                // Connect network drives if needed
                if (Util.Net.IsUNCPath(searchPath))
                    ConnectNet(searchPath, domainName, userName, password);
            }

            // Connect net drives for task destinations
            foreach (ConversionJobOptions cjo in MCEBuddyConf.GlobalMCEConfig.AllConversionTasks)
            {
                string destinationPath = cjo.destinationPath;
                string domainName = cjo.domainName;
                string userName = cjo.userName;
                string password = cjo.password;

                // Connect network drives if needed - If there is a temp folder specific in the task, then we assume the temp folder is connected to same machine as the destination path
                if (Util.Net.IsUNCPath(destinationPath))
                    ConnectNet(destinationPath, domainName, userName, password);
            }

            // Check if the temp folder is a network drive if so, connect using the general authentication
            GeneralOptions go = MCEBuddyConf.GlobalMCEConfig.GeneralOptions;
            if (Util.Net.IsUNCPath(go.tempWorkingPath))
                ConnectNet(go.tempWorkingPath, go.domainName, go.userName, go.password);

            _minimumAge = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.minimumAge;
        }

        /// <summary>
        /// Connect to a network path using the given authentication parameters
        /// This function is thread safe
        /// </summary>
        /// <param name="path">Network UNC Path</param>
        /// <param name="domain">(optional) Domain</param>
        /// <param name="userName">Username</param>
        /// <param name="password">Password</param>
        private void ConnectNet(string path, string domain, string userName, string password)
        {
            string invalidParam = "";
            int ret = 0;
            string noConnectError = ("Unable to connect to network location") + " " + path + "\r\n" + "Domain name:" + domain + "\r\nUsername:" + userName + "\r\nPassword:" + new String('*', password.Length) + "\r\n";

            Log.AppLog.WriteEntry(this, ("Attempting to connect to network share")+ " " + path, Log.LogEntryType.Debug);
            try
            {
                ret = Util.Net.ConnectShare(path, domain, userName, password, out invalidParam);
            }
            catch (Exception ex)
            {
                Log.AppLog.WriteEntry(noConnectError + ex.Message, Log.LogEntryType.Error);
            }
            if (ret == 87) // Invalid Param
            {
                System.ComponentModel.Win32Exception wex = new System.ComponentModel.Win32Exception(ret);
                Log.AppLog.WriteEntry(noConnectError + wex.Message + "\r\n" + ("The invalid parameter according to Windows is ->" + invalidParam), Log.LogEntryType.Error);
            }
            else if (ret == 86)
            {
                System.ComponentModel.Win32Exception wex = new System.ComponentModel.Win32Exception(ret);
                Log.AppLog.WriteEntry(noConnectError + wex.Message + "\r\n" + ("This is most likely caused by the currently logged on user being having a drive connected to the network location.  Please disconnect all network drives to the specified network locations for MCEBuddy."), Log.LogEntryType.Error);
            }
            else if (ret != 0)
            {
                System.ComponentModel.Win32Exception wex = new System.ComponentModel.Win32Exception(ret);
                Log.AppLog.WriteEntry(noConnectError + "Return code is " + ret.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\r\n" + wex.Message, Log.LogEntryType.Error);
            }
        }

        /// <summary>
        /// Checks if the file is in the current queue.
        /// It does NOT acquire a lock on the queue, so ensure that the queue lock is acquired before calling it. This function is NOT thread safe
        /// </summary>
        /// <param name="filePath">Filepath to check</param>
        /// <returns>True if is in the current conversion queue</returns>
        public bool QueueContains(string filePath)
        {
            foreach (ConversionJob job in _jobQueue)
            {
                if (job.OriginalFileName == filePath)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Number of the conversion jobs for a particular file in the queue
        /// It does NOT acquire a lock on the queue, so ensure that the queue lock is acquired before calling it. This function is NOT thread safe
        /// </summary>
        /// <param name="filePath">Filename/path to check</param>
        /// <returns>Number of jobs for the file in the queue</returns>
        public int JobCount(string filePath)
        {
            int jobCount = 0;
            foreach (ConversionJob job in _jobQueue)
            {
                if (job.OriginalFileName == filePath) jobCount++;
            }

            return jobCount;
        }

        private VideoMetaData ExtractMetadata(ConversionJobOptions cjo)
        {
            // Extract the video metadata first - we DO NOT download the information now since
            // 1. This will hang the thread and we have a queue lock already here, so the GUI will hang
            // 2. We dont' need the additional information, the information about Filename, Show and Network are already available in existing metadata is not overwritten/supplemented by internet data
            Log.AppLog.WriteEntry("Extracting metadata from file " + cjo.sourceVideo, Log.LogEntryType.Information);
            VideoMetaData metaData = new VideoMetaData(cjo, new JobStatus(), Log.AppLog, true); // Do NOT download internet information
            metaData.Extract(true); // Ignore the suspend since we have a Queue lock right now otherwise the engine will hang if the user pauses

            return metaData;
        }

        /// <summary>
        /// Matches the various types of metadata from the recording (after extracting) to the conversion job options
        /// This function is thread safe
        /// </summary>
        /// <returns>Null if there is a metadata filter mismatch, else returns the recording metadata object</returns>
        private VideoMetaData MetadataMatchFilters(ConversionJobOptions cjo)
        {
            // Extract the metadata
            VideoMetaData metaData = ExtractMetadata(cjo);

            Log.AppLog.WriteEntry(this, "Checking Metadata for File -> " + cjo.sourceVideo + ", Conversion Task ->" + cjo.taskName, Log.LogEntryType.Debug);

            // Check the filename matching filters
            Log.AppLog.WriteEntry(this, "File >" + Path.GetFileName(cjo.sourceVideo) + "<, checking filename filter >" + cjo.fileSelection + "<", Log.LogEntryType.Debug);
            if (!String.IsNullOrWhiteSpace(cjo.fileSelection))
            {
                if (!Util.Text.WildcardRegexPatternMatch(Path.GetFileName(cjo.sourceVideo), cjo.fileSelection))
                {
                    Log.AppLog.WriteEntry(this, "File " + Path.GetFileName(cjo.sourceVideo) + " did not match Filename meta data pattern", Log.LogEntryType.Debug);
                    return null;
                }
            }

            // Check for the Showname match filters
            Log.AppLog.WriteEntry(this, "Show >" + metaData.MetaData.Title + "<, checking for showname filter >" + cjo.metaShowSelection + "<", Log.LogEntryType.Debug);
            if (!String.IsNullOrEmpty(cjo.metaShowSelection))
            {
                if (!Util.Text.WildcardRegexPatternMatch(metaData.MetaData.Title, cjo.metaShowSelection))
                {
                    Log.AppLog.WriteEntry(this, "File " + Path.GetFileName(cjo.sourceVideo) + " did not match Showname meta data pattern", Log.LogEntryType.Debug);
                    return null;
                }
            }

            // Check for the Network name match filters
            Log.AppLog.WriteEntry(this, "Network >" + metaData.MetaData.Network + "<, checking for network/channel filter >" + cjo.metaNetworkSelection + "<", Log.LogEntryType.Debug);
            if (!String.IsNullOrEmpty(cjo.metaNetworkSelection))
            {
                if (!Util.Text.WildcardRegexPatternMatch(metaData.MetaData.Network, cjo.metaNetworkSelection))
                {
                    Log.AppLog.WriteEntry(this, "File " + Path.GetFileName(cjo.sourceVideo) + " did not match Network/Channel meta data pattern", Log.LogEntryType.Debug);
                    return null;
                }
            }

            // Check for the Show type match filters
            Log.AppLog.WriteEntry(this, "IsSports >" + metaData.MetaData.IsSports.ToString() + "<, IsMovie >" + metaData.MetaData.IsMovie.ToString() + "<, checking for show type filter >" + cjo.metaShowTypeSelection.ToString() + "<", Log.LogEntryType.Debug);
            switch (cjo.metaShowTypeSelection)
            {
                case ShowType.Movie: // Asked to only process movies
                    if (!metaData.MetaData.IsMovie) // Show is NOT a Movie
                    {
                        Log.AppLog.WriteEntry(this, "File " + Path.GetFileName(cjo.sourceVideo) + " did not match Show Type Movie meta data pattern", Log.LogEntryType.Debug);
                        return null;
                    }
                    break;

                case ShowType.Series: // Asked to only process Series
                    if (metaData.MetaData.IsMovie || metaData.MetaData.IsSports) // Show is NOT a series
                    {
                        Log.AppLog.WriteEntry(this, "File " + Path.GetFileName(cjo.sourceVideo) + " did not match Show Type Series meta data pattern", Log.LogEntryType.Debug);
                        return null;
                    }
                    break;

                case ShowType.Sports: // Asked to only process Sports
                    if (!metaData.MetaData.IsSports) // Show is NOT a series
                    {
                        Log.AppLog.WriteEntry(this, "File " + Path.GetFileName(cjo.sourceVideo) + " did not match Show Type Series meta data pattern", Log.LogEntryType.Debug);
                        return null;
                    }
                    break;

                case ShowType.Default:
                default:
                    break; // nothing to do here, no matching required
            }

            // Check for the DRM type match filters
            if (cjo.renameOnly) // Only works with Rename Only
            {
                Log.AppLog.WriteEntry(this, "Is CopyProtected >" + metaData.MetaData.CopyProtected.ToString() + "<, checking for DRM type filter >" + cjo.metaDRMSelection.ToString() + "<", Log.LogEntryType.Debug);
                switch (cjo.metaDRMSelection)
                {
                    case DRMType.Protected: // Asked to only process only copy protected files
                        if (!metaData.MetaData.CopyProtected) // Show is NOT copy protected
                        {
                            Log.AppLog.WriteEntry(this, "File " + Path.GetFileName(cjo.sourceVideo) + " did not match DRM Type Protected meta data pattern", Log.LogEntryType.Debug);
                            return null;
                        }
                        break;

                    case DRMType.Unprotected: // Asked to only process only un protected
                        if (metaData.MetaData.CopyProtected) // Show is copy protected
                        {
                            Log.AppLog.WriteEntry(this, "File " + Path.GetFileName(cjo.sourceVideo) + " did not match DRM Type UnProtected meta data pattern", Log.LogEntryType.Debug);
                            return null;
                        }
                        break;

                    case DRMType.All:
                    default:
                        break; // nothing to do here, no matching required
                }
            }

            // ALL DONE - CLEARED ALL MATCHES
            return metaData;
        }

        /// <summary>
        /// Check the History if the file has been converted (check output/converted filename and path).
        /// This function is thread safe
        /// </summary>
        /// <param name="convertedFile">Output Filename and path to check</param>
        /// <returns>True of the output filename and path exists in the history file</returns>
        public static bool DoesConvertedFileExistCheckHistory(string convertedFile)
        {
            // TODO: A very very large history file can cause the computer to "hang" and have high CPU utilization, how does one handle this situation?
            try
            {
                // Check if the file has been converted in the past
                Ini historyIni = new Ini(GlobalDefs.HistoryFile);
                List<string> fileNames = historyIni.GetSectionNames();
                foreach (string filePath in fileNames)
                {
                    if (filePath.ToLower() == convertedFile.ToLower()) // Check if the converted file exists in the History list, ignore case
                        if (historyIni.GetSectionKeyValuePairs(filePath)["Status"] == "OutputFromConversion") // Double check that this file is an output from the conversion
                            return true;
                }
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry("Unable to check History file entries", Log.LogEntryType.Error, true);
                Log.AppLog.WriteEntry("Error -> " + e.ToString(), Log.LogEntryType.Error, true);
            }

            return false;
        }

        /// <summary>
        /// Adds a conversion job to the queue
        /// This function does NOT take a lock on the queue before modifying it. This function is not thread safe
        /// </summary>
        /// <param name="filePath">File to add</param>
        /// <param name="monitorTask">Monitor task name which found the file</param>
        /// <param name="manual">True if this is a manuall added entry, false if it's added by a monitor task</param>
        private void AddJobs(string filePath, MonitorJobOptions monitorTask, bool manual)
        {
            bool filterMatchSuccessful = false;

            // Check if the file has already been processed in the past if so skip
            if (_conversionTaskFilterMismatchFiles.Contains(filePath))
                return; //

            foreach (ConversionJobOptions conversionTask in MCEBuddyConf.GlobalMCEConfig.AllConversionTasks)
            {
                // Check if the task is disabled, which case skip
                if (!conversionTask.enabled)
                {
                    Log.AppLog.WriteEntry(this, Localise.GetPhrase("Conversion Task") + " " + conversionTask.taskName + " " + Localise.GetPhrase("disabled, skipping file") + " " + filePath, Log.LogEntryType.Debug, true);
                    continue;
                }

                conversionTask.sourceVideo = filePath;

                // Monitor Task name matching if not empty
                if ((monitorTask != null) && !String.IsNullOrWhiteSpace(monitorTask.taskName) && (conversionTask.monitorTaskNames != null))
                {
                    bool foundMatch = false;
                    foreach (string matchMonitorTaskName in conversionTask.monitorTaskNames)
                    {
                        if (monitorTask.taskName.ToLower().Trim() != matchMonitorTaskName.ToLower().Trim()) // match the list of a name
                            continue; // move onto next monitor task name
                        else
                        {
                            foundMatch = true;
                            break;
                        }
                    }

                    if (!foundMatch)
                    {
                        Log.AppLog.WriteEntry(this, "Skipping Conversion task " + conversionTask.taskName + " for file " + filePath + " since Monitor task " + monitorTask.taskName + " does not match the list of monitor tasks in the conversion task.", Log.LogEntryType.Debug, true);
                        continue; // move into next conversion task
                    }
                }

                // Metadata extract and pattern match from conversion task
                VideoMetaData metaData = MetadataMatchFilters(conversionTask);
                if (metaData != null) // We got a match - process the file
                {
                    // Calculate where to add the job in the queue
                    int idx;
                    if (manual || conversionTask.insertQueueTop) // Manual jobs to the head of the queue, just after the current active job, or check if the conversion task is asking to add the job at the head of the queue
                    {
                        for (idx = 0; idx < _jobQueue.Count; idx++)
                        {
                            if (!_jobQueue[idx].Active)
                                break;
                        }
                    }
                    else
                        idx = _jobQueue.Count; // Add the job to the end of the active queue

                    // If it's a manual entry then we need to convert, reset the skip reconverting flag if it's set
                    if (manual && conversionTask.skipReprocessing)
                    {
                        conversionTask.skipReprocessing = conversionTask.checkReprocessingHistory = false; // We can make a direct change since this is a copy object
                        Log.AppLog.WriteEntry(this, "Manually added file, resetting the skip reprocessing option, the file will converted even if it has been processed before", Log.LogEntryType.Debug, true);
                    }

                    Log.AppLog.WriteEntry(this, Localise.GetPhrase("Added new job to queue for") + " " + filePath, Log.LogEntryType.Information, true);
                    ConversionJob job = new ConversionJob(conversionTask, monitorTask, metaData);
                    _jobQueue.Insert(idx, job);
                    filterMatchSuccessful = true; // we cleared filters and processed the file

                    // Send an eMail if required
                    GeneralOptions go = MCEBuddyConf.GlobalMCEConfig.GeneralOptions;
                    bool sendEMail = go.sendEmail; // do we need to send an eMail after adding a job to the queue
                    bool sendQueue = go.eMailSettings.queueEvent;
                    string sendQueueSubject = go.eMailSettings.queueSubject;
                    bool skipBody = go.eMailSettings.skipBody;
                    if (sendEMail && sendQueue)
                    {
                        string subject = Localise.GetPhrase("MCEBuddy added a video conversion to the queue");
                        string message = Localise.GetPhrase("Source Video") + " -> " + job.OriginalFileName + "\r\n";
                        message += Localise.GetPhrase("Profile") + " -> " + job.Profile + "\r\n";
                        message += Localise.GetPhrase("Conversion Task") + " -> " + job.TaskName + "\r\n";

                        // Check for custom subject and process
                        if (!String.IsNullOrWhiteSpace(sendQueueSubject))
                            subject = UserCustomParams.CustomParamsReplace(
                              sendQueueSubject, 
                              job.WorkingPath, 
                              "", 
                              "", 
                              job.OriginalFileName, 
                              "", 
                              "", 
                              "", 
                              job.Profile, 
                              job.TaskName, 
                              //conversionTask.relativeSourcePath, 
                              metaData.MetaData, 
                              Log.AppLog);

                        eMailSendEngine.AddEmailToSendQueue(subject, (skipBody ? "" : message)); // Send the eMail through the eMail engine
                    }
                }
                else if (manual) // Delete manual file entry if we didn't create a conversion job, otherwise the engine will clear it on completion
                {
                    // Manual files may be added multiple times and the filter may already have it from the last time, so be safe and delete it
                    Ini iniManualQueue = new Ini(GlobalDefs.ManualQueueFile); // Delete the entry from the manual queue
                    iniManualQueue.DeleteKey("ManualQueue", filePath);
                }
            }

            // If we have been through all the conversion tasks and not a single task has processed this file, hence we add it to the filter mismatch list so it won't be processed in future (MetadataExtract is very intensive and also prevent overburdening the log file)
            if (!filterMatchSuccessful)
                _conversionTaskFilterMismatchFiles.Add(filePath);
        }

        /// <summary>
        /// Check if a file is in the queue, if not add it to the queue.
        /// This function does NOT take a lock on the queue before modifying it. This function is not thread safe
        /// </summary>
        /// <param name="filePath">Filename to check and add</param>
        /// <param name="monitorTask">Name of the monitor task adding the file, blank if manually added</param>
        private void CheckAndAddFile(string filePath, MonitorJobOptions monitorTask)
        {
            bool manualFile = (monitorTask == null ? true : false);
            Ini iniHistory = new Ini(GlobalDefs.HistoryFile);

            if (!QueueContains(filePath))
            {
                if (!Util.FileIO.FileLocked(filePath))
                {
                    string historyRec = iniHistory.ReadString(filePath, "Status", "");

                    // If it's an output file (you can skip output file reconversion by setting SkipReconversion in Conversion Task Settings)
                    if ((monitorTask != null) && monitorTask.monitorConvertedFiles)
                        if (Core.ConvertedFileStatuses.Any(x => x.Equals(historyRec)))
                            historyRec = ""; // This is a converted file and we have been asked to process converted files also

                    // Are we asked to reconvert recorded files
                    if ((monitorTask != null) && monitorTask.reMonitorRecordedFiles)
                        if (Core.SourceFileStatuses.Any(x => x.Equals(historyRec))) // Check if the status matches any of the source file status
                            historyRec = "";

                    if (historyRec == "" || manualFile) // Either the file has not been processed (Status is missing) or readded manually (Status is blank)
                    {
                        if (File.Exists(filePath))
                        {
                            if (manualFile)
                            {
                                AddJobs(filePath, monitorTask, true); //add manual jobs for conversion at the head of queue (after the last currently active job)
                            }
                            else
                            {
                                // Check to see if the age of the file is old enough
                                DateTime timeStamp = Util.FileIO.GetFileCreationTime(filePath);
                                if (timeStamp.AddHours(_minimumAge) < DateTime.Now)
                                {
                                    AddJobs(filePath, monitorTask, false); // Added by a monitor task
                                }
                            }
                        }
                        else if (manualFile) //delete only if a manual entry is made
                        {
                            Log.AppLog.WriteEntry(this, "Unable to queue file for conversion - file not found " + filePath + "\r\n", Log.LogEntryType.Warning);
                            Ini iniManualQueue = new Ini(GlobalDefs.ManualQueueFile);
                            iniManualQueue.DeleteKey("ManualQueue", filePath);
                        }
                    }
                    else
                    {
                        if (!_processedFiles.Contains(filePath)) // Keep a track of processed file so we don't overload the mcebuddy.log file
                        {
                            _processedFiles.Add(filePath); // to the list of processed file so we don't end up doing it again
                            Log.AppLog.WriteEntry(this, "File " + filePath + " already converted with status " + historyRec + "\r\n", Log.LogEntryType.Debug);
                        }

                        // Manual file entry may have been readded multiple times, each time we log it and remove it from the ini file
                        if (manualFile) // Delete the manual entry
                        {
                            Log.AppLog.WriteEntry(this, "Manual file " + filePath + " already converted with status " + historyRec + "\r\n", Log.LogEntryType.Debug);
                            Ini iniManualQueue = new Ini(GlobalDefs.ManualQueueFile);
                            iniManualQueue.DeleteKey("ManualQueue", filePath);
                        }
                    }
                }
                else
                {
                    if (!_processedFiles.Contains(filePath)) // Keep a track of processed file so we don't overload the mcebuddy.log file
                    {
                        _processedFiles.Add(filePath); // to the list of processed file so we don't end up doing it again
                        Log.AppLog.WriteEntry(this, "Unable to queue file for conversion - file inaccessible/locked by another process " + filePath + "\r\n", Log.LogEntryType.Debug);
                    }

                    // Manual file entry may have been readded multiple times, each time we log it and remove it from the ini file
                    if (manualFile) // Delete the manual entry
                    {
                        Ini iniManualQueue = new Ini(GlobalDefs.ManualQueueFile);
                        iniManualQueue.DeleteKey("ManualQueue", filePath);
                        Log.AppLog.WriteEntry(this, "Unable to queue manual file for conversion - file inaccessible/locked by another process " + filePath + "\r\n", Log.LogEntryType.Debug);
                    }
                }
            }
        }

        /// <summary>
        /// Scans the Monitored directories and Manual Queue for new files to process
        /// Applies the filter tests
        /// Processes conversion task jobs for the files and adds them to the queue
        /// This function takes a lock on the Queue while modifying the queue and is thread safe
        /// </summary>
        public void ScanForFiles()
        {
            // Search the specific directories
            foreach (MonitorJobOptions monitorTask in MCEBuddyConf.GlobalMCEConfig.AllMonitorTasks)
            {
                IEnumerable<string> foundFiles = null;
                try
                {
                    // Directory.EnumerateFiles throws an exception if it comes across a protected/unaccesible directory and the ENTIRE list is empty
                    // Instead we need to handle exceptions, skip the protected file/directory and continue walking down the rest
                    foundFiles = FilePaths.GetDirectoryFiles(monitorTask.searchPath, "*", (monitorTask.monitorSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)).OrderBy(File.GetLastWriteTime); // We sort the files by last modified time when scanning and adding (oldest to newest)

                    if (foundFiles != null && foundFiles.Count() > 0) // Check for no files (could be due to security access/protection errors
                    {
                        foreach (string foundFile in foundFiles)
                        {
                            if (GlobalDefs.Shutdown) // Check for a shutdown command, this can be an intensive loop
                                return; // Exit - we're done here

                            Monitor.Enter(_monitorTaskFilterMismatchFiles); // Make this thread safe
                            if (_monitorTaskFilterMismatchFiles.ContainsKey(foundFile)) // Check if this file has been processed by this monitor task and doesn't have a filter match, if so skip it (the filters do not change until the engine is stopped, settings changed, engine restarted which will create a new queue)
                            {
                                if (_monitorTaskFilterMismatchFiles[foundFile].Contains(monitorTask.taskName))
                                {
                                    Monitor.Exit(_monitorTaskFilterMismatchFiles);
                                    continue;
                                }
                            }
                            Monitor.Exit(_monitorTaskFilterMismatchFiles);

                            Monitor.Enter(_archivedFiles);
                            if (_archivedFiles.Contains(foundFile)) // Check if the file has been marked as an archive file if so skip it
                            {
                                Monitor.Exit(_archivedFiles);
                                continue;
                            }
                            Monitor.Exit(_archivedFiles);

                            // First check if this file if archiving is enabled and if it is in the MCEBuddyArchive or custom archive directories (which contains converted files, if so skip them)
                            if ((monitorTask.archiveMonitorOriginal 
                                    || MCEBuddyConf.GlobalMCEConfig.GeneralOptions.archiveOriginal) 
                                && (Path.GetDirectoryName(foundFile).ToLower().Contains((string.IsNullOrEmpty(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.archivePath) ? GlobalDefs.MCEBUDDY_ARCHIVE.ToLower() : MCEBuddyConf.GlobalMCEConfig.GeneralOptions.archivePath.ToLower())) // Global archive path
                                    || (Path.GetDirectoryName(foundFile).ToLower().Contains((string.IsNullOrEmpty(monitorTask.archiveMonitorPath) ? GlobalDefs.MCEBUDDY_ARCHIVE.ToLower() : monitorTask.archiveMonitorPath.ToLower()))) // Monitor task Archive path
                                ))
                            {
                                Monitor.Enter(_archivedFiles);
                                _archivedFiles.Add(foundFile); // add to the list
                                Monitor.Exit(_archivedFiles);
                                Log.AppLog.WriteEntry(this, "File " + foundFile + " is in the archive directory, skipping monitoring", Log.LogEntryType.Debug);
                                continue;
                            }

                            // 1st Level Pattern Check - Found a file, filename pattern match from monitor task
                            if (Util.Text.WildcardRegexPatternMatch(Path.GetFileName(foundFile), monitorTask.searchPattern)) // Check pattern match for Monitor Locations filter
                            {
                                // Take a lock here for EACH file before processing and modifying queue
                                // This is done per file and not for all files so that we don't lock up the engine interfaces which need the same lock to respond to GUI queries
                                // CheckAndAddFile is very intensive time consuming process for each file as it extracts metadata and compares filters
                                // Also EnumerateFiles is a very intensive process for very large nested and remote directories which will lock up the thread if the lock is taken
                                Monitor.Enter(Queue); // Take a lock on the queue before modifying the queue
                                CheckAndAddFile(foundFile, monitorTask); // Check history, monitor and conversion task filters and creates conversion job for file
                                Monitor.Exit(Queue); // Release the lock on the queue after modifying the queue
                            }
                            else // File type mismatch, log and keep track of them for each monitor task processed
                            {
                                Monitor.Enter(_monitorTaskFilterMismatchFiles); // Make it thread safe
                                if (!_monitorTaskFilterMismatchFiles.ContainsKey(foundFile)) /// Check if this file does not have a key, then create one
                                    _monitorTaskFilterMismatchFiles.Add(foundFile, new List<string>()); // Make a new key for the file
                                _monitorTaskFilterMismatchFiles[foundFile].Add(monitorTask.taskName); // Add this task for the file as a filter mismatch
                                Monitor.Exit(_monitorTaskFilterMismatchFiles);

                                Log.AppLog.WriteEntry(this, "File " + Path.GetFileName(foundFile) + " did not match wildcard" + " " + monitorTask.searchPattern + " for monitor task " + monitorTask.taskName, Log.LogEntryType.Debug);
                            }
                        }
                    }
                    else
                        Log.AppLog.WriteEntry(this, "No accessible files founds in location " + monitorTask.searchPath + " for monitor task " + monitorTask.taskName, Log.LogEntryType.Information);
                }
                catch (Exception ex)
                {
                    Log.AppLog.WriteEntry(this, "Unable to search for files in location " + monitorTask.searchPath + " for monitor task " + monitorTask.taskName + "\r\nERROR : " + ex.Message, Log.LogEntryType.Warning);
                    foundFiles = null;

                    try { Monitor.Exit(Queue); } // Release queue lock
                    catch { }

                    try { Monitor.Exit(_monitorTaskFilterMismatchFiles); } // Release monitor mismatch list lock
                    catch { }

                    try { Monitor.Exit(_archivedFiles); } // Release archived list lock
                    catch { }
                }
            }

            // Read the manual queue - manual selections are always first
            Ini iniQueue = new Ini(GlobalDefs.ManualQueueFile);
            SortedList<string, string> manualQueue = iniQueue.GetSectionKeyValuePairs("ManualQueue");
            foreach (KeyValuePair<string, string> manualFile in manualQueue)
            {
                string filePath = manualFile.Value; // Due to INI restriction only the Value can hold special characters like ];= which maybe contained in the filename, hence the Value is used to capture the "TRUE" filename (Key and Section have restrictions)

                if (GlobalDefs.Shutdown) // Check for a shutdown command, this can be an intensive loop
                    return; // Exit - we're done here

                // Check for valid entry
                if (String.IsNullOrWhiteSpace(filePath))
                    continue;

                string destinationPath = Path.GetDirectoryName(filePath);
                if (String.IsNullOrWhiteSpace(destinationPath)) // check for a null directory name here (happens with some root level network paths)
                {
                    iniQueue.DeleteSection(filePath); // Remove the key from the manual file
                    continue;
                }

                // Connect network drives if needed for each manual entry
                GeneralOptions go = MCEBuddyConf.GlobalMCEConfig.GeneralOptions;
                if (Util.Net.IsUNCPath(destinationPath))
                {
                    if (!String.IsNullOrWhiteSpace(go.userName))
                    {
                        ConnectNet(destinationPath, go.domainName, go.userName, go.password);
                    }
                    else
                    {
                        Log.AppLog.WriteEntry(this, "No network authentication username found, defaulting to Guest authentication", Log.LogEntryType.Warning);
                        ConnectNet(destinationPath, "", GlobalDefs.DEFAULT_NETWORK_USERNAME, "");
                    }
                }

                if (Directory.Exists(filePath)) // After we are connected, check if the target is a directory (accidentally happens), we don't process it
                {
                    Log.AppLog.WriteEntry(this, "Manually selected file " + filePath + " is a directory, skipping", Log.LogEntryType.Warning);
                    iniQueue.DeleteSection(filePath); // Remove the key from the manual file
                    continue;
                }

                if (!File.Exists(filePath)) // After we are connected, check if the file actually exists
                {
                    Log.AppLog.WriteEntry(this, "Manually selected file " + filePath + " does not exist or MCEBuddy doesn't have read permissions, skipping", Log.LogEntryType.Warning);
                    iniQueue.DeleteSection(filePath); // Remove the key from the manual file
                    continue;
                }

                Log.AppLog.WriteEntry(this, "Manually selected file " + filePath + " added to queue", Log.LogEntryType.Debug);
                // Take a lock here for EACH file before processing and modifying queue
                // This is done per file and not for all files so that we don't lock up the engine interfaces which need the same lock to respond to GUI queries
                // CheckAndAddFile is very intensive time consuming process for each file as it extracts metadata and compares filters
                try
                {
                    Monitor.Enter(Queue); // Take a lock on the queue before modifying the queue
                    CheckAndAddFile(filePath, null);
                    Monitor.Exit(Queue); // Release the lock on the queue after modifying the queue
                }
                catch (Exception ex)
                {
                    Log.AppLog.WriteEntry(this, "Add manual files terminated.\r\nERROR : " + ex.Message, Log.LogEntryType.Warning);

                    try { Monitor.Exit(Queue); } // Release queue lock
                    catch { }
                }
            }
        }

        /// <summary>
        /// Deletes the Parent directory recursively if the directories are empty
        /// This function is reentrant
        /// </summary>
        /// <param name="filePath">File name with complete path to scan recursively up the parent chain of directories</param>
        private void DeleteParentDirectoryChainIfEmpty(string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath))
                return;

            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                return;

            try
            {
                if (FilePaths.GetDirectoryFiles(Path.GetDirectoryName(filePath), "*.*", SearchOption.TopDirectoryOnly).ToList<string>().Count == 0) // there are no files in the directory
                {
                    Log.AppLog.WriteEntry(this, "Deleting directory " + Path.GetDirectoryName(filePath), Log.LogEntryType.Debug);
                    Directory.Delete(Path.GetDirectoryName(filePath)); // Delete the directory
                    DeleteParentDirectoryChainIfEmpty(Path.GetDirectoryName(filePath)); // Kick it up one more level to check for empty directories
                }
            }
            catch (Exception ex)
            {
                Log.AppLog.WriteEntry(this, "Unable to read parent directory contents " + Path.GetDirectoryName(filePath) + "\r\n" + ex.ToString(), Log.LogEntryType.Warning);
            }
        }

        /// <summary>
        /// Checks if the converted files need to be kept in sync with source files. It deletes the converted file if the source file is deleted.
        /// This function is thread safe
        /// </summary>
        /// <param name="useRecycleBin">True to use Recycle Bin while deleting</param>
        public void SyncConvertedFiles(bool useRecycleBin)
        {
            try
            {
                Monitor.Enter(_monitorSyncFiles); // Make this thread safe

                // Build a list of files to be monitored from the History file to sync source with converted files
                Ini historyIni = new Ini(GlobalDefs.HistoryFile);

                try
                {
                    List<string> convertedFiles = historyIni.GetSectionNames(); // Get list of all files (includes converted and source)

                    foreach (string foundFile in convertedFiles)
                    {
                        if (String.IsNullOrWhiteSpace(historyIni.ReadString(foundFile, "ConvertedTo0", ""))) // Atleast one entry should exist for source files that have successfully converted
                            continue;

                        if (!_monitorSyncFiles.Contains(foundFile)) // We just keep building the list here and check if they are deleted later
                        {
                            _monitorSyncFiles.Add(foundFile); // add to the list
                            Log.AppLog.WriteEntry(this, "File " + foundFile + " is being monitored for syncing with output file", Log.LogEntryType.Debug);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.AppLog.WriteEntry(this, "Unable to get History section names", Log.LogEntryType.Error, true);
                    Log.AppLog.WriteEntry(this, "Error -> " + e.ToString(), Log.LogEntryType.Error, true);
                }

                // Check if the sources files are deleted and delete the converted files
                foreach (string sourceFile in new List<string>(_monitorSyncFiles)) // Create a new list to iterate through else it throws an exception when we modify it
                {
                    if (File.Exists(sourceFile))
                        continue; // Source file still exists, nothing to do

                    // Source file no longer, exists, it has been deleted
                    // Check if the file has been converted and if so then get the location of the converted file from the history file
                    int convCount = 0;
                    string convFile = "";
                    while (!String.IsNullOrWhiteSpace((convFile = historyIni.ReadString(sourceFile, "ConvertedTo" + convCount.ToString(System.Globalization.CultureInfo.InvariantCulture), ""))))
                    {
                        // Delete the EDL, SRT, XML, NFO etc files also along with the original file if present
                        foreach (string supportFileExt in GlobalDefs.supportFilesExt)
                        {
                            string extFile = Path.Combine(Path.GetDirectoryName(convFile), Path.GetFileNameWithoutExtension(convFile) + supportFileExt); // support file

                            if (File.Exists(extFile)) // don't overburden the log
                                Log.AppLog.WriteEntry(this, "Source File " + sourceFile + " deleted, deleting converted support file " + extFile, Log.LogEntryType.Debug);

                            FileIO.TryFileDelete(extFile, useRecycleBin); // Delete support file
                        }

                        if (File.Exists(convFile)) // don't overburden the log
                            Log.AppLog.WriteEntry(this, "Source File " + sourceFile + " deleted, deleting converted file " + convFile, Log.LogEntryType.Debug);

                        FileIO.TryFileDelete(convFile, useRecycleBin); // Try to delete the converted file since the source file is deleted
                        DeleteParentDirectoryChainIfEmpty(convFile); // Delete the parent directory chain if empty for the converted file

                        convCount++;
                    }

                    _monitorSyncFiles.Remove(sourceFile); // remove to the list
                    Log.AppLog.WriteEntry(this, "Source file " + sourceFile + " is stopped being monitored since it has been deleted", Log.LogEntryType.Debug);
                }

                Monitor.Exit(_monitorSyncFiles);
            }
            catch (Exception e) // Incase the thread terminates, release the lock and exit gracefully
            {
                // Release the queue lock if taken
                try { Monitor.Exit(_monitorSyncFiles); } // Incase it's taken release it, if not taken it will throw an exception
                catch { }

                Log.AppLog.WriteEntry(this, "Sync Converted Files terminated", Log.LogEntryType.Warning, true);
                Log.AppLog.WriteEntry(this, "Error -> " + e.ToString(), Log.LogEntryType.Warning, true);
            }
        }

        public List<ConversionJob> Queue
        { get { return _jobQueue; } }
    }
}
