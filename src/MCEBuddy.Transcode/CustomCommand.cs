using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

using MCEBuddy.Util;
using MCEBuddy.Globals;
using MCEBuddy.AppWrapper;
using MCEBuddy.MetaData;

namespace MCEBuddy.Transcode
{
    public class CustomCommand
    {
        private string _profile;
        private JobStatus _jobStatus;
        private Log _jobLog;
        private string commandPath = "";
        private string commandParameters = "";
        private int hangPeriod = -1;
        private Base baseCommand;
        private string _workingPath;
        private string _destinationPath;
        private string _convertedFile;
        private string _sourceFile;
        private string _remuxFile;
        private bool customCommandCritical = false;
        private bool customCommandUISession = false;
        private bool customCommandShowWindow = false;
        private bool customCommandExitCodeCheck = false;
        private VideoTags _metaData;
        private string _edlFile;
        private string _srtFile;
        private string _taskName;
        private string _prefix;

        /// <summary>
        /// Used to execute custom commands after the conversion process is compelte just before the file is moved to the desination directory
        /// </summary>
        /// <param name="prefix">Prefix for reading lines from profile</param>
        /// <param name="profile">Profile name</param>
        /// <param name="taskName">Task Name</param>
        /// <param name="workingPath">Temp working path</param>
        /// <param name="destinationPath">Destination path for converted file</param>
        /// <param name="convertedFile">Full path to final converted file</param>
        /// <param name="sourceFile">Full path to original source file</param>
        /// <param name="remuxFile">Full path to intermediate remuxed file</param>
        /// <param name="edlFile">Full path to EDL file</param>
        /// <param name="srtFile">Full path to SRT file</param>
        /// <param name="metaData">Video metadata structure for source file</param>
        /// <param name="jobStatus">ref to JobStatus</param>
        /// <param name="jobLog">JobLog</param>
        public CustomCommand(string prefix, string profile, string taskName, string workingPath, string destinationPath, string convertedFile, string sourceFile, string remuxFile, string edlFile, string srtFile, VideoTags metaData, JobStatus jobStatus, Log jobLog)
        {
            _profile = profile;
            _taskName = taskName;
            _jobLog = jobLog;
            _jobStatus = jobStatus;
            _workingPath = workingPath;
            _destinationPath = destinationPath;
            _convertedFile = convertedFile;
            _sourceFile = sourceFile;
            _remuxFile = remuxFile;
            _edlFile = edlFile;
            _srtFile = srtFile;
            _metaData = metaData;
            _prefix = prefix;

            Ini ini = new Ini(GlobalDefs.ProfileFile);
            commandPath = ini.ReadString(profile, prefix + "Path", "").ToLower().Trim();
            commandParameters = ini.ReadString(profile, prefix + "Parameters", "");
            hangPeriod = ini.ReadInteger(profile, prefix + "HangPeriod", GlobalDefs.HANG_PERIOD_DETECT);
            customCommandCritical = ini.ReadBoolean(profile, prefix + "Critical", false); // NOTE: if customCommandCritical is TRUE will need to return false in case it's a failure
            customCommandUISession = ini.ReadBoolean(profile, prefix + "UISession", false); // Does the custom command need a UI Session (Session 1) with admin privileges
            customCommandShowWindow = ini.ReadBoolean(profile, prefix + "ShowWindow", true); // Show the window or hide it
            customCommandExitCodeCheck = ini.ReadBoolean(profile, prefix + "ExitCodeCheck", false); // Don't check for exit code

            _jobLog.WriteEntry(this, "Custom command parameters read -> " + " \n" + _prefix + "Path = " + commandPath + " \n" + _prefix + "Parameters = " + commandParameters + " \n" + _prefix + "HangPeriod = " + hangPeriod.ToString(System.Globalization.CultureInfo.InvariantCulture) + " \n" + _prefix + "Critical = " + customCommandCritical.ToString() + " \n" + _prefix + "UISession = " + customCommandUISession.ToString() + " \n" + _prefix + "ShowWindow = " + customCommandShowWindow.ToString() + " \n" + _prefix + "ExitCodeCheck = " + customCommandExitCodeCheck.ToString(), Log.LogEntryType.Debug);
        }

        public bool Run()
        {
            string translatedCommand = "";

            if (commandPath == "")
            {
                _jobLog.WriteEntry(this, "No custom commands found", Log.LogEntryType.Information);
                return true; // not a critical failure
            }

            // Get the path if it is an absolute path, if it's relative we start in the MCEBuddy directory
            if (!Path.IsPathRooted(commandPath))
                commandPath = Path.Combine(GlobalDefs.AppPath, commandPath); // Relative path starts with MCEBuddy path

            if ((hangPeriod < 0) || (!File.Exists(commandPath)))
            {
                if (hangPeriod < 0)
                    _jobLog.WriteEntry(this, _prefix + "HangPeriod NOT specified!", Log.LogEntryType.Error);
                else
                    _jobLog.WriteEntry(this, _prefix + "Path does NOT exist!", Log.LogEntryType.Error);

                _jobLog.WriteEntry(this, "Invalid custom command parameters" + " \n" + _prefix + "Path = " + commandPath + " \n" + _prefix + "Parameters = " + commandParameters + " \n" + _prefix + "HangPeriod = " + hangPeriod.ToString(System.Globalization.CultureInfo.InvariantCulture) + " \n" + _prefix + "Critical = " + customCommandCritical.ToString() + " \n" + _prefix + "UISession = " + customCommandUISession.ToString() + " \n" + _prefix + "ShowWindow = " + customCommandShowWindow.ToString() + " \n" + _prefix + "ExitCodeCheck = " + customCommandExitCodeCheck.ToString(), Log.LogEntryType.Error);
                
                if (customCommandCritical)
                    _jobStatus.ErrorMsg = "Invalid custom command parameters"; // Set an error message on if we are failing the conversion
                
                return !customCommandCritical; // return the opposite of the critical (if it's true then return false)
            }

            // Translate the user commands
            if (!String.IsNullOrWhiteSpace(commandParameters)) // Check if there was a custom command parameter
            {
                translatedCommand = UserCustomParams.CustomParamsReplace(commandParameters, _workingPath, _destinationPath, _convertedFile, _sourceFile, _remuxFile, _edlFile, _srtFile, _profile, _taskName, _metaData, _jobLog);
                if (String.IsNullOrWhiteSpace(translatedCommand))
                {
                    _jobLog.WriteEntry(this, "Invalid custom command. Error", (customCommandCritical ? Log.LogEntryType.Error : Log.LogEntryType.Warning));
                    if (customCommandCritical)
                        _jobStatus.ErrorMsg = Localise.GetPhrase("Invalid custom command"); // Set an error message on if we are failing the conversion
                    return !customCommandCritical; // return the opposite of the critical (if it's true then return false)
                }
            }

            try
            {
                baseCommand = new Base(customCommandShowWindow, translatedCommand, commandPath, customCommandUISession, _jobStatus, _jobLog); // send the absolute command path and by default success is true until process is terminated and show Window
            }
            catch (FileNotFoundException)
            {
                _jobLog.WriteEntry(this, "Invalid custom command path" + " " + _prefix + "Path = " + commandPath, (customCommandCritical ? Log.LogEntryType.Error : Log.LogEntryType.Warning));
                if (customCommandCritical)
                    _jobStatus.ErrorMsg = "Invalid custom command path"; // Set an error message on if we are failing the conversion
                return !customCommandCritical; // return the opposite of the critical (if it's true then return false)
            }

            // Set the hang detection period
            baseCommand.HangPeriod = hangPeriod;

            _jobLog.WriteEntry(this, "About to run custom command with parameters:" + " \n" + _prefix + "Path = " + commandPath + " \n" + _prefix + "Parameters = " + commandParameters + " \n" + _prefix + "HangPeriod = " + hangPeriod.ToString(System.Globalization.CultureInfo.InvariantCulture) + " \n" + _prefix + "Critical = " + customCommandCritical.ToString() + " \n" + _prefix + "UISession = " + customCommandUISession.ToString() + " \n" + _prefix + "ShowWindow = " + customCommandShowWindow.ToString() + " \n" + _prefix + "ExitCodeCheck = " + customCommandExitCodeCheck.ToString(), Log.LogEntryType.Debug);

            // Run the custom command
            baseCommand.Run();

            // Check for hang/termination
            if (!baseCommand.Success)
            {
                _jobLog.WriteEntry(this, "Custom command hung, process was terminated", (customCommandCritical ? Log.LogEntryType.Error : Log.LogEntryType.Warning));
                
                if (customCommandCritical)
                    _jobStatus.ErrorMsg = "Custom command hung, process was terminated";
                else
                    _jobStatus.ErrorMsg = ""; // Clear any errors

                return !customCommandCritical; // return the opposite, see above
            }

            // Check if exit code not equal to0 indicating failure, if required
            if (customCommandExitCodeCheck && (baseCommand.ExitCode != 0))
            {
                _jobStatus.ErrorMsg = "Custom command failed with exit code " + baseCommand.ExitCode.ToString();
                _jobLog.WriteEntry(this, "Custom command failed with Exit Code " + baseCommand.ExitCode.ToString(), (customCommandCritical ? Log.LogEntryType.Error : Log.LogEntryType.Warning));
                return false; // failed
            }

            return true;
        }
    }
}
