using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.Configuration
{
    /// <summary>
    /// MCEBuddyConf contains all the Engine settings (GeneralOptions), Conversion Jobs (ConversionJobOptions) and Monitor Jobs (MonitorJobOptions) in mcebuddy.conf
    /// It can also read and write settings from a conf file into a new conf file (configIni)
    /// It does not contain any profile information
    /// </summary>
    public class MCEBuddyConf
    {
        public static MCEBuddyConf GlobalMCEConfig; // One static instance to be shared globally across all modules of the program

        private ConfSettings mceBuddyConfSettings; // Serializabele group of all settings
        private Ini configIni = null; // Config Ini file

        /// <summary>
        /// Returns the Conf settings options with all the settings
        /// </summary>
        public ConfSettings ConfigSettings
        {
            get { return new ConfSettings(AllConversionTasks, AllMonitorTasks, GeneralOptions); }
        }

        /// <summary>
        /// Returns a copy of the list of all conversion tasks
        /// </summary>
        public List<ConversionJobOptions> AllConversionTasks
        {
            get
            {
                List<ConversionJobOptions> cjoList = new List<ConversionJobOptions>();
                foreach (ConversionJobOptions cjo in mceBuddyConfSettings.conversionTasks)
                    cjoList.Add(cjo.Clone());
                return cjoList;
            }
        }

        /// <summary>
        /// Returns a copy of the list of all monitor tasks
        /// </summary>
        public List<MonitorJobOptions> AllMonitorTasks
        {
            get
            {
                List<MonitorJobOptions> mjoList = new List<MonitorJobOptions>();
                foreach (MonitorJobOptions mjo in mceBuddyConfSettings.monitorTasks)
                    mjoList.Add(mjo.Clone());
                return mjoList;
            }
        }

        /// <summary>
        /// Gets a copy of the General Options object
        /// </summary>
        public GeneralOptions GeneralOptions
        {
            get { return mceBuddyConfSettings.generalOptions.Clone(); }
        }

        /// <summary>
        /// Get a copy of Conversion Task Object by Name
        /// </summary>
        /// <param name="taskName">Name of conversion task</param>
        /// <returns>Copy of the conversion task object if found, else NULL</returns>
        public ConversionJobOptions GetConversionTaskByName(string taskName)
        {
            int index = mceBuddyConfSettings.conversionTasks.FindIndex(item => item.taskName == taskName);

            if (index < 0) // new task, cannot find it
                return null;
            else
                return mceBuddyConfSettings.conversionTasks[index].Clone();
        }

        /// <summary>
        /// Get a copy of the Monitor Task Object by Name
        /// </summary>
        /// <param name="taskName">Name of monitor task</param>
        /// <returns>Copy of the monitor task object if found, else NULL</returns>
        public MonitorJobOptions GetMonitorTaskByName(string taskName)
        {
            int index = mceBuddyConfSettings.monitorTasks.FindIndex(item => item.taskName == taskName);

            if (index < 0) // new task, cannot find it
                return null;
            else
                return mceBuddyConfSettings.monitorTasks[index].Clone();
        }

        /// <summary>
        /// Update the Configuration option settings, optionally write to file
        /// </summary>
        /// <param name="configOptions">Configuration option settings</param>
        /// <param name="write">Write to configuration file immediately</param>
        public void UpdateConfigOptions(ConfSettings configOptions, bool write)
        {
            // Clone it, to avoid conflict
            mceBuddyConfSettings.conversionTasks = new List<ConversionJobOptions>();
            mceBuddyConfSettings.monitorTasks = new List<MonitorJobOptions>();
            mceBuddyConfSettings.generalOptions = new GeneralOptions();

            mceBuddyConfSettings.generalOptions = configOptions.generalOptions; // Copy of the General Options

            foreach (MonitorJobOptions mjo in configOptions.monitorTasks)
                AddOrUpdateMonitorTask(mjo, true); // Add the MonitorTasks

            foreach (ConversionJobOptions cjo in configOptions.conversionTasks)
                AddOrUpdateConversionTask(cjo, true); // Add the Conversion Tasks

            if (write)
                WriteSettings();
        }

        /// <summary>
        /// Update the General Options, optionally write to file
        /// </summary>
        /// <param name="go">General options settings</param>
        /// <param name="write">Write to configuration file immediately</param>
        public void UpdateGeneralOptions(GeneralOptions go, bool write)
        {
            // Clone it, to avoid conflict
            mceBuddyConfSettings.generalOptions = go.Clone();

            if (write)
                WriteGeneralSettings(configIni);
        }

        /// <summary>
        /// Update a Monitor Task or Add to the list if not found (by Name), optionally write to file
        /// </summary>
        /// <param name="mjo">Monitor job options</param>
        /// <param name="write">Write to configuration file immediately</param>
        public void AddOrUpdateMonitorTask(MonitorJobOptions mjo, bool write)
        {
            int index = mceBuddyConfSettings.monitorTasks.FindIndex(item => item.taskName == mjo.taskName);

            // Clone it, to avoid conflict
            if (index < 0) // new task, cannot find it
                mceBuddyConfSettings.monitorTasks.Add(mjo.Clone());
            else
                mceBuddyConfSettings.monitorTasks[index] = mjo.Clone();

            if (write)
                WriteMonitorSettings(configIni);
        }

        /// <summary>
        /// Update a Conversion Task or Add to the list if not found (by Name), optionally write to file
        /// </summary>
        /// <param name="cjo">Conversion job options</param>
        /// <param name="write">Write to configuration file immediately</param>
        public void AddOrUpdateConversionTask(ConversionJobOptions cjo, bool write)
        {
            int index = mceBuddyConfSettings.conversionTasks.FindIndex(item => item.taskName == cjo.taskName);

            // Clone it, to avoid conflict
            if (index < 0) // new task, cannot find it
                mceBuddyConfSettings.conversionTasks.Add(cjo.Clone());
            else
                mceBuddyConfSettings.conversionTasks[index] = cjo.Clone();

            if (write)
                WriteConversionSettings(configIni);
        }

        /// <summary>
        /// Delete a conversion task from the list and optionally write to the configuration file
        /// </summary>
        /// <param name="taskName">Name of conversion task</param>
        /// <param name="write">Write to configuration file immediately</param>
        public void DeleteConversionTask(string taskName, bool write)
        {
            ConversionJobOptions cjo = mceBuddyConfSettings.conversionTasks.Find(item => item.taskName == taskName);
            if (cjo != null)
            {
                mceBuddyConfSettings.conversionTasks.Remove(cjo); // Remove from list
                if (write)
                {
                    configIni.DeleteSection(taskName); // Remove from file
                    WriteConversionTasksList(configIni);
                }
            }
        }

        /// <summary>
        /// Delete a monitor task from the list and optionally write to the configuration file
        /// </summary>
        /// <param name="taskName">Name of monitor task</param>
        /// <param name="write">Write to configuration file immediately</param>
        public void DeleteMonitorTask(string taskName, bool write)
        {
            MonitorJobOptions mjo = mceBuddyConfSettings.monitorTasks.Find(item => item.taskName == taskName);
            if (mjo != null)
            {
                mceBuddyConfSettings.monitorTasks.Remove(mjo); // Remove from list
                if (write)
                {
                    configIni.DeleteSection(taskName); // Remove from file
                    WriteMonitorTasksList(configIni);
                }
            }
        }

        /// <summary>
        /// The MCEBuddyConf object is initialized with values read from the config file specified.
        /// If there are missing values in a config file, then default values are initialized.
        /// If no config file is specified, default values are initialized and no config file is written
        /// </summary>
        /// <param name="configFile">Path to config file</param>
        public MCEBuddyConf(string configFile = "")
        {
            if (configFile == null)
                configFile = ""; // don't use null as it leads to excessive reads / writes of win.ini

            mceBuddyConfSettings.conversionTasks = new List<ConversionJobOptions>();
            mceBuddyConfSettings.monitorTasks = new List<MonitorJobOptions>();
            mceBuddyConfSettings.generalOptions = new GeneralOptions();

            if (!String.IsNullOrEmpty(configFile))
            {
                configIni = new Ini(configFile);
                ReadMonitorSettings(configIni);
                ReadConversionSettings(configIni);
                ReadGeneralSettings(configIni);
            }
        }

        /// <summary>
        /// A MCEBuddyConf object is initialized and a new config file is created/written from the MCEBuddyConf parameters passed.
        /// If no config file is specified, default values are initialized and no config file is written
        /// </summary>
        /// <param name="configOptions">Set of MCEBuddyConf parameters to copy</param>
        /// <param name="configFile">Path to config file</param>
        public MCEBuddyConf(ConfSettings configOptions, string configFile = "")
        {
            if (configFile == null)
                configFile = ""; // don't use null as it leads to excessive reads / writes of win.ini

            mceBuddyConfSettings.conversionTasks = new List<ConversionJobOptions>();
            mceBuddyConfSettings.monitorTasks = new List<MonitorJobOptions>();
            mceBuddyConfSettings.generalOptions = new GeneralOptions();

            mceBuddyConfSettings.generalOptions = configOptions.generalOptions; // Copy of the General Options

            foreach (MonitorJobOptions mjo in configOptions.monitorTasks)
                AddOrUpdateMonitorTask(mjo, true); // Add the MonitorTasks
            
            foreach (ConversionJobOptions cjo in configOptions.conversionTasks)
                AddOrUpdateConversionTask(cjo, true); // Add the Conversion Tasks

            if (!String.IsNullOrEmpty(configFile))
            {
                configIni = new Ini(configFile);
                WriteSettings(); // Write the settings to the config file
            }
        }

        /// <summary>
        /// Creates a new Config File and a new MCEBuddyConf object with parameters copied from the individual options objects passed.
        /// If no config file is specified, default values are initialized and no config file is written
        /// </summary>
        /// <param name="configFile">Config file to write to</param>
        /// <param name="go">GeneralOptions parameters</param>
        /// <param name="mjoList">List of MonitorJobOption parameters</param>
        /// <param name="cjoList">List of ConversionJobOption parameters</param>
        public MCEBuddyConf(GeneralOptions go, List<MonitorJobOptions> mjoList, List<ConversionJobOptions> cjoList, string configFile = "")
        {
            if (configFile == null)
                configFile = ""; // don't use null as it leads to excessive reads / writes of win.ini

            mceBuddyConfSettings.conversionTasks = new List<ConversionJobOptions>();
            mceBuddyConfSettings.monitorTasks = new List<MonitorJobOptions>();
            mceBuddyConfSettings.generalOptions = new GeneralOptions();

            mceBuddyConfSettings.generalOptions = go.Clone();
            
            foreach (MonitorJobOptions mjo in mjoList)
                AddOrUpdateMonitorTask(mjo, false);
            
            foreach (ConversionJobOptions cjo in cjoList)
                AddOrUpdateConversionTask(cjo, false);

            if (!String.IsNullOrEmpty(configFile))
            {
                configIni = new Ini(configFile);
                WriteSettings();
            }
        }

        /// <summary>
        /// Used to write the MCEBuddyConf configuration settings to the config file specified while creating this object
        /// </summary>
        public void WriteSettings()
        {
            if (configIni != null)
            {
                WriteMonitorSettings(configIni);
                WriteConversionSettings(configIni);
                WriteGeneralSettings(configIni);
            }
        }

        /// <summary>
        /// Used to write the MCEBuddyConf configuration settings to a new config file
        /// </summary>
        /// <param name="configfile">Path to config file</param>
        public void WriteSettings(string configFile)
        {
            if (configFile == null)
                configFile = ""; // don't use null as it leads to excessive reads / writes of win.ini

            if (!String.IsNullOrEmpty(configFile))
            {
                Ini newConfigIni = new Ini(configFile);

                WriteMonitorSettings(newConfigIni);
                WriteConversionSettings(newConfigIni);
                WriteGeneralSettings(newConfigIni);
            }
        }


        private void ReadHourMinute(Ini configIni, ref int hour, ref int minute, string prefix)
        {
            hour = configIni.ReadInteger("Engine", prefix + "Hour", -1);
            minute = configIni.ReadInteger("Engine", prefix + "Minute", -1);

            if ((hour < 0) || (hour > 24)) hour = -1;
            if ((minute < 0) || (minute > 59)) minute = -1;
        }

        private void ReadEMailSettings(Ini configIni)
        {
            mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.smtpServer = configIni.ReadString("Engine", "eMailServer", "");
            mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.port = configIni.ReadInteger("Engine", "eMailPort", 25); // default port is 25
            mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.ssl = configIni.ReadBoolean("Engine", "eMailSSL", false);
            mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.fromAddress = configIni.ReadString("Engine", "eMailFrom", "");
            mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.toAddresses = configIni.ReadString("Engine", "eMailTo", "");
            mceBuddyConfSettings.generalOptions.eMailSettings.successEvent = configIni.ReadBoolean("Engine", "eMailSuccess", true);
            mceBuddyConfSettings.generalOptions.eMailSettings.failedEvent = configIni.ReadBoolean("Engine", "eMailFailed", true);
            mceBuddyConfSettings.generalOptions.eMailSettings.cancelledEvent = configIni.ReadBoolean("Engine", "eMailCancelled", true);
            mceBuddyConfSettings.generalOptions.eMailSettings.startEvent = configIni.ReadBoolean("Engine", "eMailStart", true);
            mceBuddyConfSettings.generalOptions.eMailSettings.downloadFailedEvent = configIni.ReadBoolean("Engine", "eMailDownloadFailed", true);
            mceBuddyConfSettings.generalOptions.eMailSettings.queueEvent = configIni.ReadBoolean("Engine", "eMailQueue", true);
            mceBuddyConfSettings.generalOptions.eMailSettings.successSubject = configIni.ReadString("Engine", "eMailSuccessSubject", "");
            mceBuddyConfSettings.generalOptions.eMailSettings.failedSubject = configIni.ReadString("Engine", "eMailFailedSubject", "");
            mceBuddyConfSettings.generalOptions.eMailSettings.cancelledSubject = configIni.ReadString("Engine", "eMailCancelledSubject", "");
            mceBuddyConfSettings.generalOptions.eMailSettings.startSubject = configIni.ReadString("Engine", "eMailStartSubject", "");
            mceBuddyConfSettings.generalOptions.eMailSettings.downloadFailedSubject = configIni.ReadString("Engine", "eMailDownloadFailedSubject", "");
            mceBuddyConfSettings.generalOptions.eMailSettings.queueSubject = configIni.ReadString("Engine", "eMailQueueSubject", "");
            mceBuddyConfSettings.generalOptions.eMailSettings.skipBody = configIni.ReadBoolean("Engine", "eMailSkipBody", false);
            mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.userName = configIni.ReadString("Engine", "eMailUsername", "");
            mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.password = configIni.ReadString("Engine", "eMailPassword", "");
            if (!String.IsNullOrEmpty(mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.password))
                mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.password = Util.Crypto.Decrypt(mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.password); // password is stored encrypted
        }

        private void ReadGeneralSettings(Ini configIni)
        {
            // Read the General Settings
            ReadHourMinute(configIni, ref mceBuddyConfSettings.generalOptions.wakeHour, ref mceBuddyConfSettings.generalOptions.wakeMinute, "Wake");
            ReadHourMinute(configIni, ref mceBuddyConfSettings.generalOptions.startHour, ref mceBuddyConfSettings.generalOptions.startMinute, "Start");
            ReadHourMinute(configIni, ref mceBuddyConfSettings.generalOptions.stopHour, ref mceBuddyConfSettings.generalOptions.stopMinute, "Stop");

            mceBuddyConfSettings.generalOptions.domainName = configIni.ReadString("Engine", "DomainName", "");
            mceBuddyConfSettings.generalOptions.userName = configIni.ReadString("Engine", "UserName", "Guest");
            mceBuddyConfSettings.generalOptions.password = configIni.ReadString("Engine", "Password", "");
            if (!String.IsNullOrEmpty(mceBuddyConfSettings.generalOptions.password))
                mceBuddyConfSettings.generalOptions.password = Crypto.Decrypt(mceBuddyConfSettings.generalOptions.password); // Password is kept as encrypted
            mceBuddyConfSettings.generalOptions.daysOfWeek = configIni.ReadString("Engine", "DaysOfWeek", "Sunday,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday");
            mceBuddyConfSettings.generalOptions.maxConcurrentJobs = configIni.ReadInteger("Engine", "MaxConcurrentJobs", 1);
            mceBuddyConfSettings.generalOptions.logJobs = configIni.ReadBoolean("Engine", "LogJobs", true);
            mceBuddyConfSettings.generalOptions.logLevel = configIni.ReadInteger("Engine", "LogLevel", 3);
            mceBuddyConfSettings.generalOptions.logKeepDays = configIni.ReadInteger("Engine", "LogKeepDays", 15);
            mceBuddyConfSettings.generalOptions.deleteOriginal = configIni.ReadBoolean("Engine", "DeleteOriginal", false);
            mceBuddyConfSettings.generalOptions.useRecycleBin = configIni.ReadBoolean("Engine", "UseRecycleBin", false);
            mceBuddyConfSettings.generalOptions.archiveOriginal = configIni.ReadBoolean("Engine", "ArchiveOriginal", false);
            mceBuddyConfSettings.generalOptions.deleteConverted = configIni.ReadBoolean("Engine", "DeleteConverted", false); 
            mceBuddyConfSettings.generalOptions.allowSleep = configIni.ReadBoolean("Engine", "AllowSleep", true);
            mceBuddyConfSettings.generalOptions.suspendOnBattery = configIni.ReadBoolean("Engine", "SuspendOnBattery", false);
            mceBuddyConfSettings.generalOptions.minimumAge = configIni.ReadInteger("Engine", "MinimumAge", 0);
            mceBuddyConfSettings.generalOptions.sendEmail = configIni.ReadBoolean("Engine", "SendEmail", false);
            mceBuddyConfSettings.generalOptions.locale = configIni.ReadString("Engine", "Locale", CultureInfo.CurrentCulture.Name);
            mceBuddyConfSettings.generalOptions.tempWorkingPath = configIni.ReadString("Engine", "TempWorkingPath", "");
            CheckPathEnding(ref mceBuddyConfSettings.generalOptions.tempWorkingPath);
            mceBuddyConfSettings.generalOptions.archivePath = configIni.ReadString("Engine", "ArchivePath", "");
            CheckPathEnding(ref mceBuddyConfSettings.generalOptions.archivePath);
            mceBuddyConfSettings.generalOptions.failedPath = configIni.ReadString("Engine", "FailedPath", "");
            CheckPathEnding(ref mceBuddyConfSettings.generalOptions.failedPath);
            mceBuddyConfSettings.generalOptions.spaceCheck = configIni.ReadBoolean("Engine", "SpaceCheck", true);
            mceBuddyConfSettings.generalOptions.comskipPath = configIni.ReadString("Engine", "CustomComskipPath", "");
            mceBuddyConfSettings.generalOptions.customProfilePath = configIni.ReadString("Engine", "CustomProfilePath", "");
            mceBuddyConfSettings.generalOptions.hangTimeout = configIni.ReadInteger("Engine", "HangPeriod", GlobalDefs.HANG_PERIOD_DETECT);
            mceBuddyConfSettings.generalOptions.pollPeriod = configIni.ReadInteger("Engine", "PollPeriod", GlobalDefs.MONITOR_POLL_PERIOD);
            mceBuddyConfSettings.generalOptions.processPriority = configIni.ReadString("Engine", "ProcessPriority", "Normal");
            mceBuddyConfSettings.generalOptions.CPUAffinity = (IntPtr) configIni.ReadLong("Engine", "CPUAffinity", 0);
            mceBuddyConfSettings.generalOptions.engineRunning = configIni.ReadBoolean("Engine", "EngineRunning", false);
            mceBuddyConfSettings.generalOptions.localServerPort = configIni.ReadInteger("Engine", "LocalServerPort", int.Parse(GlobalDefs.MCEBUDDY_SERVER_PORT));
            mceBuddyConfSettings.generalOptions.uPnPEnable = configIni.ReadBoolean("Engine", "UPnPEnable", false);
            mceBuddyConfSettings.generalOptions.firewallExceptionEnabled = configIni.ReadBoolean("Engine", "FirewallExceptionEnable", false);
            string srtSegmentOffset = configIni.ReadString("Engine", "SubtitleSegmentOffset", GlobalDefs.SEGMENT_CUT_OFFSET_GOP_COMPENSATE);
            double.TryParse(srtSegmentOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out mceBuddyConfSettings.generalOptions.subtitleSegmentOffset);
            
            ReadEMailSettings(configIni);
        }

        private void WriteEMailSettings(Ini configIni)
        {
            if (configIni == null)
                return;

            configIni.Write("Engine", "eMailServer", mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.smtpServer);
            configIni.Write("Engine", "eMailPort", mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.port);
            configIni.Write("Engine", "eMailSSL", mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.ssl);
            configIni.Write("Engine", "eMailFrom", mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.fromAddress);
            configIni.Write("Engine", "eMailTo", mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.toAddresses);
            configIni.Write("Engine", "eMailSuccess", mceBuddyConfSettings.generalOptions.eMailSettings.successEvent);
            configIni.Write("Engine", "eMailFailed", mceBuddyConfSettings.generalOptions.eMailSettings.failedEvent);
            configIni.Write("Engine", "eMailCancelled", mceBuddyConfSettings.generalOptions.eMailSettings.cancelledEvent);
            configIni.Write("Engine", "eMailStart", mceBuddyConfSettings.generalOptions.eMailSettings.startEvent);
            configIni.Write("Engine", "eMailDownloadFailed", mceBuddyConfSettings.generalOptions.eMailSettings.downloadFailedEvent);
            configIni.Write("Engine", "eMailQueue", mceBuddyConfSettings.generalOptions.eMailSettings.queueEvent);
            configIni.Write("Engine", "eMailSuccessSubject", mceBuddyConfSettings.generalOptions.eMailSettings.successSubject);
            configIni.Write("Engine", "eMailFailedSubject", mceBuddyConfSettings.generalOptions.eMailSettings.failedSubject);
            configIni.Write("Engine", "eMailCancelledSubject", mceBuddyConfSettings.generalOptions.eMailSettings.cancelledSubject);
            configIni.Write("Engine", "eMailStartSubject", mceBuddyConfSettings.generalOptions.eMailSettings.startSubject);
            configIni.Write("Engine", "eMailDownloadFailedSubject", mceBuddyConfSettings.generalOptions.eMailSettings.downloadFailedSubject);
            configIni.Write("Engine", "eMailQueueSubject", mceBuddyConfSettings.generalOptions.eMailSettings.queueSubject);
            configIni.Write("Engine", "eMailSkipBody", mceBuddyConfSettings.generalOptions.eMailSettings.skipBody);
            configIni.Write("Engine", "eMailUsername", mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.userName);
            if (!String.IsNullOrEmpty(mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.password))
                configIni.Write("Engine", "eMailPassword", Util.Crypto.Encrypt(mceBuddyConfSettings.generalOptions.eMailSettings.eMailBasicSettings.password)); // password is stored encrypted
        }

        private void WriteGeneralSettings(Ini configIni)
        {
            if (configIni == null)
                return;

            // Write the General Settings
            configIni.Write("Engine", "DomainName", mceBuddyConfSettings.generalOptions.domainName);
            configIni.Write("Engine", "UserName", mceBuddyConfSettings.generalOptions.userName);
            if (!String.IsNullOrEmpty(mceBuddyConfSettings.generalOptions.password))
                configIni.Write("Engine", "Password", Crypto.Encrypt(mceBuddyConfSettings.generalOptions.password)); // Password is written as encrypted
            configIni.Write("Engine", "WakeHour", mceBuddyConfSettings.generalOptions.wakeHour);
            configIni.Write("Engine", "WakeMinute", mceBuddyConfSettings.generalOptions.wakeMinute);
            configIni.Write("Engine", "StartHour", mceBuddyConfSettings.generalOptions.startHour);
            configIni.Write("Engine", "StartMinute", mceBuddyConfSettings.generalOptions.startMinute);
            configIni.Write("Engine", "StopHour", mceBuddyConfSettings.generalOptions.stopHour);
            configIni.Write("Engine", "StopMinute", mceBuddyConfSettings.generalOptions.stopMinute);
            configIni.Write("Engine", "DaysOfWeek", mceBuddyConfSettings.generalOptions.daysOfWeek);
            configIni.Write("Engine", "MaxConcurrentJobs", mceBuddyConfSettings.generalOptions.maxConcurrentJobs);
            configIni.Write("Engine", "LogJobs", mceBuddyConfSettings.generalOptions.logJobs);
            configIni.Write("Engine", "LogLevel", mceBuddyConfSettings.generalOptions.logLevel);
            configIni.Write("Engine", "LogKeepDays", mceBuddyConfSettings.generalOptions.logKeepDays);
            configIni.Write("Engine", "DeleteOriginal", mceBuddyConfSettings.generalOptions.deleteOriginal);
            configIni.Write("Engine", "UseRecycleBin", mceBuddyConfSettings.generalOptions.useRecycleBin);
            configIni.Write("Engine", "ArchiveOriginal", mceBuddyConfSettings.generalOptions.archiveOriginal);
            configIni.Write("Engine", "DeleteConverted", mceBuddyConfSettings.generalOptions.deleteConverted); 
            configIni.Write("Engine", "AllowSleep", mceBuddyConfSettings.generalOptions.allowSleep);
            configIni.Write("Engine", "SuspendOnBattery", mceBuddyConfSettings.generalOptions.suspendOnBattery);
            configIni.Write("Engine", "MinimumAge", mceBuddyConfSettings.generalOptions.minimumAge);
            configIni.Write("Engine", "SendEmail", mceBuddyConfSettings.generalOptions.sendEmail);
            configIni.Write("Engine", "Locale", mceBuddyConfSettings.generalOptions.locale);
            configIni.Write("Engine", "TempWorkingPath", mceBuddyConfSettings.generalOptions.tempWorkingPath);
            configIni.Write("Engine", "ArchivePath", mceBuddyConfSettings.generalOptions.archivePath);
            configIni.Write("Engine", "FailedPath", mceBuddyConfSettings.generalOptions.failedPath);
            configIni.Write("Engine", "SpaceCheck", mceBuddyConfSettings.generalOptions.spaceCheck);
            configIni.Write("Engine", "CustomComskipPath", mceBuddyConfSettings.generalOptions.comskipPath);
            configIni.Write("Engine", "CustomProfilePath", mceBuddyConfSettings.generalOptions.customProfilePath);
            configIni.Write("Engine", "HangPeriod", mceBuddyConfSettings.generalOptions.hangTimeout);
            configIni.Write("Engine", "PollPeriod", mceBuddyConfSettings.generalOptions.pollPeriod);
            configIni.Write("Engine", "ProcessPriority", mceBuddyConfSettings.generalOptions.processPriority);
            configIni.Write("Engine", "CPUAffinity", mceBuddyConfSettings.generalOptions.CPUAffinity.ToInt32());
            configIni.Write("Engine", "EngineRunning", mceBuddyConfSettings.generalOptions.engineRunning);
            configIni.Write("Engine", "LocalServerPort", mceBuddyConfSettings.generalOptions.localServerPort);
            configIni.Write("Engine", "UPnPEnable", mceBuddyConfSettings.generalOptions.uPnPEnable);
            configIni.Write("Engine", "FirewallExceptionEnable", mceBuddyConfSettings.generalOptions.firewallExceptionEnabled);
            configIni.Write("Engine", "SubtitleSegmentOffset", mceBuddyConfSettings.generalOptions.subtitleSegmentOffset.ToString(CultureInfo.InvariantCulture));

            WriteEMailSettings(configIni);
        }



        private void ReadConversionSettings(Ini configIni)
        {
            // Read the Conversion Tasks
            string[] conversionRecords = configIni.ReadString("Engine", "Tasks", "").Split(',');
            foreach (string conversionRecord in conversionRecords)
            {
                if (String.IsNullOrEmpty(conversionRecord))
                    continue;

                ConversionJobOptions cjo = new ConversionJobOptions();

                cjo.taskName = conversionRecord;
                cjo.profile = configIni.ReadString(conversionRecord, "Profile", "");
                cjo.destinationPath = configIni.ReadString(conversionRecord, "DestinationPath", "");
                CheckPathEnding(ref cjo.destinationPath);
                cjo.workingPath = configIni.ReadString(conversionRecord, "WorkingPath", "");
                CheckPathEnding(ref cjo.workingPath);
                cjo.fallbackToSourcePath = configIni.ReadBoolean(conversionRecord, "FallbackDestination", false);
                cjo.autoIncrementFilename = configIni.ReadBoolean(conversionRecord, "AutoIncrementFilename", false);
                cjo.skipReprocessing = configIni.ReadBoolean(conversionRecord, "SkipReprocessing", false);
                cjo.checkReprocessingHistory = configIni.ReadBoolean(conversionRecord, "CheckReprocessingHistory", false);
                cjo.addToiTunes = configIni.ReadBoolean(conversionRecord, "AddToiTunesLibrary", false);
                cjo.addToWMP = configIni.ReadBoolean(conversionRecord, "AddToWMPLibrary", false);
                cjo.maxWidth = configIni.ReadInteger(conversionRecord, "MaxWidth", 720);
                cjo.FPS = configIni.ReadString(conversionRecord, "FPS", "");
                cjo.renameBySeries = configIni.ReadBoolean(conversionRecord, "RenameBySeries", true);
                cjo.altRenameBySeries = configIni.ReadBoolean(conversionRecord, "AltRenameBySeries", true);
                cjo.customRenameBySeries = configIni.ReadString(conversionRecord, "CustomRenameBySeries", "");
                cjo.renameOnly = configIni.ReadBoolean(conversionRecord, "RenameOnly", false);
                cjo.fileSelection = configIni.ReadString(conversionRecord, "FileSelection", "");
                cjo.metaShowSelection = configIni.ReadString(conversionRecord, "MetaSelection", "");
                cjo.metaNetworkSelection = configIni.ReadString(conversionRecord, "MetaChannelSelection", "");
                string monitorNameList = configIni.ReadString(conversionRecord, "MonitorTaskNames", "");
                if (String.IsNullOrWhiteSpace(monitorNameList))
                    cjo.monitorTaskNames = null; // list should be empty if nothing is there
                else
                    cjo.monitorTaskNames = monitorNameList.Split(',');
                cjo.audioLanguage = configIni.ReadString(conversionRecord, "AudioLanguage", "");
                string audioOffsetStr = configIni.ReadString(conversionRecord, "AudioOffset", "0");
                double.TryParse(audioOffsetStr, NumberStyles.Float, CultureInfo.InvariantCulture, out cjo.audioOffset);
                cjo.drc = configIni.ReadBoolean(conversionRecord, "DRC", true);
                cjo.stereoAudio = configIni.ReadBoolean(conversionRecord, "StereoAudio", true);
                cjo.encoderSelectBestAudioTrack = configIni.ReadBoolean(conversionRecord, "EncoderSelectBestAudioTrack", true);
                cjo.autoDeInterlace = configIni.ReadBoolean(conversionRecord, "AutoDeInterlace", true);
                cjo.preferHardwareEncoding = configIni.ReadBoolean(conversionRecord, "PreferHardwareEncoding", true);
                cjo.startTrim = configIni.ReadInteger(conversionRecord, "StartTrim", 0);
                cjo.endTrim = configIni.ReadInteger(conversionRecord, "EndTrim", 0);
                cjo.insertQueueTop = configIni.ReadBoolean(conversionRecord, "InsertQueueTop", false);
                cjo.extractXML = configIni.ReadBoolean(conversionRecord, "ExtractXML", false);
                cjo.writeMetadata = configIni.ReadBoolean(conversionRecord, "WriteMetadata", true);
                cjo.disableCropping = configIni.ReadBoolean(conversionRecord, "DisableCropping", false);
                cjo.commercialSkipCut = configIni.ReadBoolean(conversionRecord, "TaskCommercialSkipCut", false);
                cjo.skipCopyBackup = configIni.ReadBoolean(conversionRecord, "SkipCopyBackup", false);
                cjo.skipRemuxing = configIni.ReadBoolean(conversionRecord, "SkipRemux", false);
                cjo.ignoreCopyProtection = configIni.ReadBoolean(conversionRecord, "IgnoreCopyProtection", false);
                cjo.tivoMAKKey = configIni.ReadString(conversionRecord, "TiVOMAKKey", "");
                cjo.downloadSeriesDetails = configIni.ReadBoolean(conversionRecord, "DownloadSeriesDetails", true);
                cjo.downloadBanner = configIni.ReadBoolean(conversionRecord, "DownloadBanner", true);
                cjo.enabled = configIni.ReadBoolean(conversionRecord, "Enabled", true);
                cjo.extractCC = configIni.ReadString(conversionRecord, "ExtractCC", "");
                cjo.embedSubtitlesChapters = configIni.ReadBoolean(conversionRecord, "EmbedSubtitlesChapters", false);
                cjo.prioritizeOriginalBroadcastDateMatch = configIni.ReadBoolean(conversionRecord, "PrioritizeOriginalBroadcastDateMatch", false);

                string ccOffsetStr = configIni.ReadString(conversionRecord, "CCOffset", GlobalDefs.DEFAULT_CC_OFFSET);
                double.TryParse(ccOffsetStr, NumberStyles.Float, CultureInfo.InvariantCulture, out cjo.ccOffset);

                string qualityMultiplierStr = configIni.ReadString(conversionRecord, "QualityMultiplier", "1");
                double.TryParse(qualityMultiplierStr, NumberStyles.Float, CultureInfo.InvariantCulture, out cjo.qualityMultiplier);
                if (cjo.qualityMultiplier <= 0.01) cjo.qualityMultiplier = 0.01F;
                if (cjo.qualityMultiplier > 4) cjo.qualityMultiplier = 4F;

                string volumeMultiplierStr = configIni.ReadString(conversionRecord, "VolumeMultiplier", "0");
                double.TryParse(volumeMultiplierStr, NumberStyles.Float, CultureInfo.InvariantCulture, out cjo.volumeMultiplier);
                if (cjo.volumeMultiplier <= -20) cjo.volumeMultiplier = -20F; //-10db minimum
                if (cjo.volumeMultiplier > 60) cjo.volumeMultiplier = 60F; //30db max

                string metaShowType = configIni.ReadString(conversionRecord, "MetaShowTypeSelection", ShowType.Default.ToString());
                if (String.Compare(metaShowType, ShowType.Movie.ToString(), true) == 0)
                    cjo.metaShowTypeSelection = ShowType.Movie;
                else if (String.Compare(metaShowType, ShowType.Series.ToString(), true) == 0)
                    cjo.metaShowTypeSelection = ShowType.Series;
                else if (String.Compare(metaShowType, ShowType.Sports.ToString(), true) == 0)
                    cjo.metaShowTypeSelection = ShowType.Sports;
                else
                    cjo.metaShowTypeSelection = ShowType.Default;

                string drmType = configIni.ReadString(conversionRecord, "MetaDRMTypeSelection", DRMType.All.ToString());
                if (String.Compare(drmType, DRMType.Protected.ToString(), true) == 0)
                    cjo.metaDRMSelection = DRMType.Protected;
                else if (String.Compare(drmType, DRMType.Unprotected.ToString(), true) == 0)
                    cjo.metaDRMSelection = DRMType.Unprotected;
                else
                    cjo.metaDRMSelection = DRMType.All;

                string showType = configIni.ReadString(conversionRecord, "ForceShowType", ShowType.Default.ToString());
                if (String.Compare(showType, ShowType.Movie.ToString(), true) == 0)
                    cjo.forceShowType = ShowType.Movie;
                else if (String.Compare(showType, ShowType.Series.ToString(), true) == 0)
                    cjo.forceShowType = ShowType.Series;
                else if (String.Compare(showType, ShowType.Sports.ToString(), true) == 0)
                    cjo.forceShowType = ShowType.Sports;
                else
                    cjo.forceShowType = ShowType.Default;
                
                string commercialRemovalStr = configIni.ReadString(conversionRecord, "CommercialRemoval", CommercialRemovalOptions.Comskip.ToString());
                if (String.Compare(commercialRemovalStr, CommercialRemovalOptions.Comskip.ToString(), true) == 0)
                    cjo.commercialRemoval = CommercialRemovalOptions.Comskip;
                else if (String.Compare(commercialRemovalStr, CommercialRemovalOptions.ShowAnalyzer.ToString(), true) == 0)
                    cjo.commercialRemoval = CommercialRemovalOptions.ShowAnalyzer;
                else
                    cjo.commercialRemoval = CommercialRemovalOptions.None;
                
                cjo.comskipIni = configIni.ReadString(conversionRecord, "ComskipINI", "");

                cjo.domainName = configIni.ReadString(conversionRecord, "DomainName", "");
                cjo.userName = configIni.ReadString(conversionRecord, "UserName", "Guest");
                cjo.password = configIni.ReadString(conversionRecord, "Password", "");
                if (!String.IsNullOrEmpty(cjo.password))
                    cjo.password = Crypto.Decrypt(cjo.password); // Password is kept as encrypted

                int metaCorrectionsCount = configIni.ReadInteger(conversionRecord, "MetaCorrectionsCount", 0);
                if (metaCorrectionsCount < 1)
                    cjo.metadataCorrections = null;
                else
                {
                    cjo.metadataCorrections = new ConversionJobOptions.MetadataCorrectionOptions[metaCorrectionsCount];
                    for (int i = 0; i < metaCorrectionsCount; i++) // The entries are kept in their own section, easier to manage
                    {
                        cjo.metadataCorrections[i] = new ConversionJobOptions.MetadataCorrectionOptions();
                        cjo.metadataCorrections[i].originalTitle = configIni.ReadString(conversionRecord + "-MetaCorrectionEntries", "OriginalTitle" + i.ToString(), "");
                        cjo.metadataCorrections[i].correctedTitle = configIni.ReadString(conversionRecord + "-MetaCorrectionEntries", "CorrectedTitle" + i.ToString(), "");
                        cjo.metadataCorrections[i].tvdbSeriesId = configIni.ReadString(conversionRecord + "-MetaCorrectionEntries", "TVDBSeriesId" + i.ToString(), "");
                        cjo.metadataCorrections[i].imdbSeriesId = configIni.ReadString(conversionRecord + "-MetaCorrectionEntries", "IMDBSeriesId" + i.ToString(), "");
                    }
                }

                mceBuddyConfSettings.conversionTasks.Add(cjo); // Add the Monitor Task object
            }
        }

        private void WriteConversionTasksList(Ini configIni)
        {
            if (configIni == null)
                return;

            string conversionTaskNames = ""; // A list of all the conversion task names

            // Write the Converstion Task Settings
            foreach (ConversionJobOptions conversionTask in mceBuddyConfSettings.conversionTasks)
            {
                string section = conversionTask.taskName;

                if (conversionTaskNames == "")
                    conversionTaskNames = section;
                else
                    conversionTaskNames += "," + section;
            }

            configIni.Write("Engine", "Tasks", conversionTaskNames); // this list goes in the Engine section
        }

        private void WriteConversionSettings(Ini configIni)
        {
            if (configIni == null)
                return;

            // First read and delete all Conversion tasks - start with a clean slate (incase there were previous delete conversion tasks without writing)
            string[] conversionRecords = configIni.ReadString("Engine", "Tasks", "").Split(',');
            foreach (string conversionRecord in conversionRecords)
            {
                if (String.IsNullOrEmpty(conversionRecord))
                    continue;

                configIni.DeleteSection(conversionRecord);
            }

            // Write the Converstion Task Settings
            foreach (ConversionJobOptions conversionTask in mceBuddyConfSettings.conversionTasks)
            {
                string section = conversionTask.taskName;

                configIni.Write(section, "Profile", conversionTask.profile);
                configIni.Write(section, "DestinationPath", conversionTask.destinationPath);
                configIni.Write(section, "WorkingPath", conversionTask.workingPath);
                configIni.Write(section, "FallbackDestination", conversionTask.fallbackToSourcePath);
                configIni.Write(section, "CheckReprocessingHistory", conversionTask.checkReprocessingHistory);
                configIni.Write(section, "AddToiTunesLibrary", conversionTask.addToiTunes);
                configIni.Write(section, "AddToWMPLibrary", conversionTask.addToWMP);
                configIni.Write(section, "AutoIncrementFilename", conversionTask.autoIncrementFilename);
                configIni.Write(section, "SkipReprocessing", conversionTask.skipReprocessing);
                configIni.Write(section, "MaxWidth", conversionTask.maxWidth);
                configIni.Write(section, "FPS", conversionTask.FPS);
                configIni.Write(section, "VolumeMultiplier", conversionTask.volumeMultiplier.ToString(CultureInfo.InvariantCulture));
                configIni.Write(section, "QualityMultiplier", conversionTask.qualityMultiplier.ToString(CultureInfo.InvariantCulture));
                configIni.Write(section, "RenameBySeries", conversionTask.renameBySeries);
                configIni.Write(section, "AltRenameBySeries", conversionTask.altRenameBySeries);
                configIni.Write(section, "CustomRenameBySeries", conversionTask.customRenameBySeries);
                configIni.Write(section, "RenameOnly", conversionTask.renameOnly);
                configIni.Write(section, "DownloadSeriesDetails", conversionTask.downloadSeriesDetails);
                configIni.Write(section, "DownloadBanner", conversionTask.downloadBanner); 
                configIni.Write(section, "FileSelection", conversionTask.fileSelection);
                configIni.Write(section, "MetaSelection", conversionTask.metaShowSelection);
                configIni.Write(section, "MetaChannelSelection", conversionTask.metaNetworkSelection);
                configIni.Write(section, "MonitorTaskNames", (conversionTask.monitorTaskNames == null ? "" : String.Join(",", conversionTask.monitorTaskNames)));
                configIni.Write(section, "DRC", conversionTask.drc);
                configIni.Write(section, "AudioLanguage", conversionTask.audioLanguage);
                configIni.Write(section, "AudioOffset", conversionTask.audioOffset.ToString(CultureInfo.InvariantCulture));
                configIni.Write(section, "InsertQueueTop", conversionTask.insertQueueTop);
                configIni.Write(section, "ExtractXML", conversionTask.extractXML);
                configIni.Write(section, "WriteMetadata", conversionTask.writeMetadata);
                configIni.Write(section, "AutoDeInterlace", conversionTask.autoDeInterlace);
                configIni.Write(section, "PreferHardwareEncoding", conversionTask.preferHardwareEncoding);
                configIni.Write(section, "StereoAudio", conversionTask.stereoAudio);
                configIni.Write(section, "EncoderSelectBestAudioTrack", conversionTask.encoderSelectBestAudioTrack);
                configIni.Write(section, "DisableCropping", conversionTask.disableCropping);
                configIni.Write(section, "StartTrim", conversionTask.startTrim);
                configIni.Write(section, "EndTrim", conversionTask.endTrim);
                configIni.Write(section, "ExtractCC", conversionTask.extractCC);
                configIni.Write(section, "CCOffset", conversionTask.ccOffset.ToString(CultureInfo.InvariantCulture));
                configIni.Write(section, "EmbedSubtitlesChapters", conversionTask.embedSubtitlesChapters);
                configIni.Write(section, "PrioritizeOriginalBroadcastDateMatch", conversionTask.prioritizeOriginalBroadcastDateMatch);
                configIni.Write(section, "TaskCommercialSkipCut", conversionTask.commercialSkipCut);
                configIni.Write(section, "SkipCopyBackup", conversionTask.skipCopyBackup);
                configIni.Write(section, "SkipRemux", conversionTask.skipRemuxing);
                configIni.Write(section, "IgnoreCopyProtection", conversionTask.ignoreCopyProtection);
                configIni.Write(section, "TiVOMAKKey", conversionTask.tivoMAKKey);
                configIni.Write(section, "Enabled", conversionTask.enabled);
                configIni.Write(section, "ForceShowType", conversionTask.forceShowType.ToString());
                configIni.Write(section, "MetaShowTypeSelection", conversionTask.metaShowTypeSelection.ToString());
                configIni.Write(section, "MetaDRMTypeSelection", conversionTask.metaDRMSelection.ToString());
                configIni.Write(section, "CommercialRemoval", conversionTask.commercialRemoval.ToString());
                configIni.Write(section, "ComskipINI", conversionTask.comskipIni);
                configIni.Write(section, "DomainName", conversionTask.domainName);
                configIni.Write(section, "UserName", conversionTask.userName);
                if (!String.IsNullOrEmpty(conversionTask.password))
                    configIni.Write(section, "Password", Crypto.Encrypt(conversionTask.password)); // Password is written as encrypted

                // First wipe the MetaCorrectionEntries section clean, to remove old/redundant data and then start afresh since we don't know how many entries may exist
                configIni.DeleteSection(section + "-MetaCorrectionEntries");

                if (conversionTask.metadataCorrections == null)
                    configIni.Write(section, "MetaCorrectionsCount", 0);
                else
                {
                    configIni.Write(section, "MetaCorrectionsCount", conversionTask.metadataCorrections.Length);

                    for (int i = 0; i < conversionTask.metadataCorrections.Length; i++) // the Enteries are kept in their own section
                    {
                        configIni.Write(section + "-MetaCorrectionEntries", "OriginalTitle" + i.ToString(), conversionTask.metadataCorrections[i].originalTitle);
                        configIni.Write(section + "-MetaCorrectionEntries", "CorrectedTitle" + i.ToString(), conversionTask.metadataCorrections[i].correctedTitle);
                        configIni.Write(section + "-MetaCorrectionEntries", "TVDBSeriesId" + i.ToString(), conversionTask.metadataCorrections[i].tvdbSeriesId);
                        configIni.Write(section + "-MetaCorrectionEntries", "IMDBSeriesId" + i.ToString(), conversionTask.metadataCorrections[i].imdbSeriesId);
                    }
                }
            }

            WriteConversionTasksList(configIni); // this list goes in the Engine section
        }



        private void ReadMonitorSettings(Ini configIni)
        {
            // Read the Monitor Tasks
            string[] searchRecords = configIni.ReadString("Engine", "SearchRecords", "").Split(',');
            foreach (string searchRecord in searchRecords)
            {
                if (String.IsNullOrEmpty(searchRecord))
                    continue;

                MonitorJobOptions mjo = new MonitorJobOptions();

                mjo.taskName = searchRecord;
                mjo.searchPath = configIni.ReadString(searchRecord, "SearchPath", "");
                CheckPathEnding(ref mjo.searchPath);
                mjo.searchPattern = configIni.ReadString(searchRecord, "SearchPattern", GlobalDefs.DEFAULT_VIDEO_STRING);
                mjo.searchPattern = mjo.searchPattern.Replace(GlobalDefs.DEFAULT_VIDEO_STRING, GlobalDefs.DEFAULT_VIDEO_FILE_TYPES);
                mjo.deleteMonitorOriginal = configIni.ReadBoolean(searchRecord, "DeleteMonitorOriginal", false);
                mjo.archiveMonitorOriginal = configIni.ReadBoolean(searchRecord, "ArchiveMonitorOriginal", false);
                mjo.archiveMonitorPath = configIni.ReadString(searchRecord, "ArchiveMonitorPath", "");
                CheckPathEnding(ref mjo.archiveMonitorPath);
                mjo.monitorSubdirectories = configIni.ReadBoolean(searchRecord, "MonitorSubdirectories", true);
                mjo.monitorConvertedFiles = configIni.ReadBoolean(searchRecord, "MonitorConvertedFiles", false);
                mjo.reMonitorRecordedFiles = configIni.ReadBoolean(searchRecord, "ReMonitorRecordedFiles", false);

                mjo.domainName = configIni.ReadString(searchRecord, "DomainName", "");
                mjo.userName = configIni.ReadString(searchRecord, "UserName", "Guest");
                mjo.password = configIni.ReadString(searchRecord, "Password", "");
                if (!String.IsNullOrEmpty(mjo.password))
                    mjo.password = Crypto.Decrypt(mjo.password); // Password is kept as encrypted

                mceBuddyConfSettings.monitorTasks.Add(mjo); // Add the Monitor Task object
            }
        }

        private void WriteMonitorTasksList(Ini configIni)
        {
            if (configIni == null)
                return;

            string monitorTaskNames = ""; // A list of of all the monitor task names

            // Write the Monitor Tasks Settings
            foreach (MonitorJobOptions monitorTask in mceBuddyConfSettings.monitorTasks)
            {
                string section = monitorTask.taskName;

                if (monitorTaskNames == "")
                    monitorTaskNames = section;
                else
                    monitorTaskNames += "," + section;
            }

            configIni.Write("Engine", "SearchRecords", monitorTaskNames); // this list goes in the Engine section
        }

        private void WriteMonitorSettings(Ini configIni)
        {
            if (configIni == null)
                return;

            // First read and delete all Monitor tasks - start with a clean slate (incase there were previous delete monitor tasks without writing)
            string[] searchRecords = configIni.ReadString("Engine", "SearchRecords", "").Split(',');
            foreach (string searchRecord in searchRecords)
            {
                if (String.IsNullOrEmpty(searchRecord))
                    continue;

                configIni.DeleteSection(searchRecord);
            }

            // Write the Monitor Tasks Settings
            foreach (MonitorJobOptions monitorTask in mceBuddyConfSettings.monitorTasks)
            {
                string section = monitorTask.taskName;

                configIni.Write(section, "SearchPath", monitorTask.searchPath);
                monitorTask.searchPattern = monitorTask.searchPattern.Replace(GlobalDefs.DEFAULT_VIDEO_STRING, GlobalDefs.DEFAULT_VIDEO_FILE_TYPES);
                configIni.Write(section, "SearchPattern", monitorTask.searchPattern);
                configIni.Write(section, "DeleteMonitorOriginal", monitorTask.deleteMonitorOriginal);
                configIni.Write(section, "ArchiveMonitorOriginal", monitorTask.archiveMonitorOriginal);
                configIni.Write(section, "ArchiveMonitorPath", monitorTask.archiveMonitorPath);
                configIni.Write(section, "MonitorSubdirectories", monitorTask.monitorSubdirectories);
                configIni.Write(section, "MonitorConvertedFiles", monitorTask.monitorConvertedFiles);
                configIni.Write(section, "ReMonitorRecordedFiles", monitorTask.reMonitorRecordedFiles);

                configIni.Write(section, "DomainName", monitorTask.domainName);
                configIni.Write(section, "UserName", monitorTask.userName);
                if (!String.IsNullOrEmpty(monitorTask.password))
                    configIni.Write(section, "Password", Crypto.Encrypt(monitorTask.password)); // Password is written as encrypted
            }

            WriteMonitorTasksList(configIni); // this list goes in the Engine section
        }

        /// <summary>
        /// Check if the path ends in a \ and remove its since it is invalid (except drive roots like C:\)
        /// </summary>
        /// <param name="path">Path</param>
        private void CheckPathEnding(ref string path)
        {
            if (!String.IsNullOrWhiteSpace(path) && (path.Length > 3)) // Check if the path ends in a \, which is not valid, C:\ is valid so check for > 3 characters
                if (path.Substring(path.Length - 1, 1) == @"\")
                    path = path.Substring(0, path.Length - 1); // Skip the last \
        }

        public override string ToString()
        {
            string allOpts = "";

            foreach (ConversionJobOptions cjo in mceBuddyConfSettings.conversionTasks)
                allOpts += "CONVERSION TASK OPTIONS ==>\n" + cjo.ToString() + "\n";

            foreach (MonitorJobOptions mjo in mceBuddyConfSettings.monitorTasks)
                allOpts += "MONITOR TASK OPTIONS ==>\n" + mjo.ToString() + "\n";

            allOpts += "GENERAL OPTIONS ==>\n" + mceBuddyConfSettings.generalOptions.ToString() + "\n";

            return allOpts;
        }

        /// <summary>
        /// Checks and updates the default Monitor Location and Conversion Output paths depending on the Windows version (XP or Vista/Later)
        /// </summary>
        public static void CheckDefaultJobPaths()
        {
            // Check and fix the Default Monitor Path
            MonitorJobOptions mjo = MCEBuddyConf.GlobalMCEConfig.GetMonitorTaskByName("Windows Default");

            // If we running Windows XP we need to update the default path for the recorded TV for monitor locations
            if (mjo != null) // This is still the default profile, not customized
            {
                if (mjo.searchPath == GlobalDefs.DEFAULT_WINVISTA_AND_LATER_PATH || mjo.searchPath == GlobalDefs.DEFAULT_WINXP_PATH) // Check if the search path is still pointing to the default Windows search path and update to real path (not customized yet)
                {
                    OperatingSystem OS = Util.OSVersion.TrueOSVersion;

                    if (OS.Version.Major == 5) // Windows XP has major version 5, Vista/Win7 has 6
                        mjo.searchPath = Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "Documents", "Recorded TV"); // Get the location of all users and set the recorded tv path
                    else
                        mjo.searchPath = Path.Combine(Environment.GetEnvironmentVariable("PUBLIC"), "Recorded TV"); // Get the location of all users and set the recorded tv path

                    MCEBuddyConf.GlobalMCEConfig.AddOrUpdateMonitorTask(mjo, true); // write it back
                }
            }

            // Check and fix the Default Conversion Path
            ConversionJobOptions cjo = MCEBuddyConf.GlobalMCEConfig.GetConversionTaskByName("Convert to MP4");

            // If we running Windows XP we need to update the default path for the output videos folder for conversion task
            if (cjo != null) // This is still the default profile, not customized
            {
                if (cjo.destinationPath == GlobalDefs.DEFAULT_WINVISTA_AND_LATER_PATH_OUTPUT || cjo.destinationPath == GlobalDefs.DEFAULT_WINXP_PATH_OUTPUT) // Check if the search path is still pointing to the default Windows output path and update to real path (not customized yet)
                {
                    OperatingSystem OS = Util.OSVersion.TrueOSVersion;

                    if (OS.Version.Major == 5) // Windows XP has major version 5, Vista/Win7 has 6
                        cjo.destinationPath = Path.Combine(Environment.GetEnvironmentVariable("ALLUSERSPROFILE"), "Documents", "My Videos"); // Get the location of all users and set the video output path
                    else
                        cjo.destinationPath = Path.Combine(Environment.GetEnvironmentVariable("PUBLIC"), "Videos"); // Get the location of all users and set the output videos path

                    MCEBuddyConf.GlobalMCEConfig.AddOrUpdateConversionTask(cjo, true); // write it back
                }
            }
        }
    }
}
