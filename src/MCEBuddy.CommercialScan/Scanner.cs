using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using ShowAnalyzerLib;

using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.AppWrapper;
using MCEBuddy.Configuration;

namespace MCEBuddy.CommercialScan
{
    public class Scanner : EDL
    {
        private string _videoFileName;
        private bool _useShowAnalyzer = false;
        private JobStatus _jobStatus;
        private Log _jobLog;
        private string _profile;
        private ConversionJobOptions _convOptions;
        private string _customComskipPath = "";

        public Scanner(ConversionJobOptions conversionOptions, 
          string videoFileName, 
          bool useShowAnalyzer, 
          float duration, 
          JobStatus jobStatus, 
          Log jobLog)
            : base(conversionOptions.profile, videoFileName, duration, "", 0, jobStatus, jobLog)
        {
            _videoFileName = videoFileName;
            _useShowAnalyzer = useShowAnalyzer;
            _jobStatus = jobStatus;
            _jobLog = jobLog;
            _profile = conversionOptions.profile;
            _convOptions = conversionOptions;

            _customComskipPath = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.comskipPath;
            if (!String.IsNullOrWhiteSpace(_customComskipPath))
                _jobLog.WriteEntry(this, "Using Custom Comskip Path -> " + MCEBuddyConf.GlobalMCEConfig.GeneralOptions.comskipPath, Log.LogEntryType.Information);
        }

        private void ScanWithSA()
        {
            bool isSuspended = false;
            ShowAnalyzerLib.ShowAnalyzer sa = null;

            try
            {
                _jobStatus.PercentageComplete = 0; //reset
                _jobStatus.ETA = "";

                int _HangPeriod = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.hangTimeout; // Default period to detect hang due to no output activity on console - 300 seconds
                double _lastPercentage = 0;

                sa = new ShowAnalyzer();
                string fileId = "";
                DateTime _LastTick = DateTime.Now; ;

                sa.AnalyzeFile(_videoFileName, "", ref fileId);
                //sa.ChangePriority(fileId, (int) GlobObjects.Priority); // set the priority to match the users's selection - not implemented yet, throwing exception
                bool completed = false;
                double percentage = 0;
                string status;
                while (!completed)
                {
                    if (_jobStatus.Cancelled)
                    {
                        //Cancel the running process to release file lock and free processor
                        sa.Abort(fileId);
                        _jobLog.WriteEntry(this, Localise.GetPhrase("Cancelled ShowAnalyzer, file ->") + " " + _videoFileName, Log.LogEntryType.Error);
                        Util.FileIO.TryFileDelete(SourceEDLFilePath); // Delete the EDL file since that is used to determine success
                        Util.FileIO.TryFileDelete(SourceEDLPFilePath); // Delete the EDL file since that is used to determine success
                        break;
                    }

                    sa.GetStatus(fileId, out percentage, out status);
                    _jobStatus.PercentageComplete = (float)(100 * percentage);

                    if (status == "done")
                    {
                        break;
                    }

                    if (!isSuspended && GlobalDefs.Pause) // Check if process has to be suspended (if not already)
                    {
                        sa.Pause(fileId); // Suspend it
                        _jobLog.Flush(); // Flush the buffers
                        isSuspended = true;
                    }

                    if (isSuspended && !GlobalDefs.Pause) // Check if we need to resume the process
                    {
                        isSuspended = false;
                        sa.Resume(fileId); // Resume it
                    }

                    if (isSuspended) _LastTick = DateTime.Now; // Since during suspension there will be no output it shouldn't terminate the process

                    // Check if Showanalyzer has hung
                    if (_lastPercentage != percentage) // It has progressed
                    {
                        if (_HangPeriod > 0) _LastTick = DateTime.Now;
                    }
                    else
                    {
                        if ((_HangPeriod > 0) && (DateTime.Now > _LastTick.AddSeconds(_HangPeriod)))
                        {
                            _jobLog.WriteEntry(Localise.GetPhrase("No response from process for ") + _HangPeriod + Localise.GetPhrase(" seconds, process likely hung - killing it"), Log.LogEntryType.Error);
                            sa.Abort(fileId); // Kill it
                            Util.FileIO.TryFileDelete(SourceEDLFilePath); // Delete the EDL file since that is used to determine success
                            Util.FileIO.TryFileDelete(SourceEDLPFilePath); // Delete the EDL file since that is used to determine success
                            break;
                        }
                    }

                    _lastPercentage = percentage; // track the last percentage
                    System.Threading.Thread.Sleep(100);
                }

                // Release the COM object and clean up otherwise it hangs later
                try
                {
                    sa.RemoveItemFromQueue(fileId);
                }
                catch { }

                Marshal.FinalReleaseComObject(sa);
                sa = null;
            }
            catch (Exception Ex)
            {
                // Release the COM object
                if (sa != null)
                {
                    Marshal.FinalReleaseComObject(sa);
                    sa = null;
                }

                _jobLog.WriteEntry(this, Localise.GetPhrase("ShowAnalyzer failed, file ->") + " " + _videoFileName, Log.LogEntryType.Error);
                _jobLog.WriteEntry(this, "Error -> " + Ex.ToString(), Log.LogEntryType.Debug);
            }
        }

        public static bool ShowAnalyzerInstalled()
        {
            ShowAnalyzerLib.ShowAnalyzer sa = null;

            try
            {
                sa = new ShowAnalyzer();
                string reg = sa.GetRegistrationInfo();
                // TODO check for unregistered use - fail
                
                // Release the COM object and clean up otherwise it hangs later
                Marshal.FinalReleaseComObject(sa);
                sa = null;
                return true;
            }
            catch
            {
                // Release the COM object
                if (sa != null)
                {
                    Marshal.FinalReleaseComObject(sa);
                    sa = null;
                }

                return false;
            }
        }

        private void ScanWithComskip()
        {
            string parameters = "";
            
            if(_convOptions.comskipIni != "") // Check for custom Ini file
            {
                if(File.Exists(_convOptions.comskipIni))
                    parameters += "--ini=" + Util.FilePaths.FixSpaces(_convOptions.comskipIni);
                else
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Custom Comskip INI file does not exist, Skipping custom INI"), Log.LogEntryType.Warning);
            }

            parameters += " --output=" + Util.FilePaths.FixSpaces(_convOptions.workingPath); // Redirect all files to working folder (save issues with skipping copying original files and simulatenous scanning on same file)

            parameters += " " + Util.FilePaths.FixSpaces(_videoFileName);

            float Duration = 0;
            Duration = VideoParams.VideoDuration(_videoFileName);
            if (Duration <= 0)
            {
                FFmpegMediaInfo mediaInfo = new FFmpegMediaInfo(_videoFileName, _jobStatus, _jobLog);
                if (!mediaInfo.Success || mediaInfo.ParseError)
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Cannot read video duration"), Log.LogEntryType.Error);

                if (mediaInfo.MediaInfo.VideoInfo.Duration == 0)
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Video duration 0"), Log.LogEntryType.Warning);
                else
                    Duration = mediaInfo.MediaInfo.VideoInfo.Duration;
            }

            // Check for custom version of Comskip path
            Comskip comskip = new Comskip(_customComskipPath, parameters, Duration, _jobStatus, _jobLog); // Use custom comskip or fallback to default
            comskip.Run();
            if (!comskip.Success || !(File.Exists(WorkingEDLFilePath) || File.Exists(WorkingEDLPFilePath))) // Check if the EDL/EDLP file exists (here it is in working path), % does not always work
            {
                _jobLog.WriteEntry(this, "Comskip failed or no output EDL/EDLP file found", Log.LogEntryType.Warning);
                _jobStatus.PercentageComplete = 0;
            }
        }

        public bool Scan()
        {
            _jobStatus.PercentageComplete = 0; //reset 
            _jobStatus.ETA = "";

            // Use EDL/EDLP file if available in the working temp directory
            if (File.Exists(WorkingEDLFilePath) || File.Exists(WorkingEDLPFilePath))
            {
                _jobLog.WriteEntry(this,Localise.GetPhrase("Found existing EDL file, using it"),Log.LogEntryType.Information);
                _jobStatus.PercentageComplete = 100;
            }
            else if (_useShowAnalyzer) // Use ShowAnalyzer if required
            {
                if (ShowAnalyzerInstalled()) // Check if ShowAnalyzer is installed
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Scanning commercials with ShowAnalyzer"), Log.LogEntryType.Information);
                    ScanWithSA();
                }
                else
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("ShowAnalyzer not installed"), Log.LogEntryType.Error);
                    return false;
                }
            }
            else // else Comskip
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Scanning commercials with Comskip"), Log.LogEntryType.Information); 
                ScanWithComskip();
            }

            _jobLog.WriteEntry(this, Localise.GetPhrase("Commercial Scan: Percentage Complete") + " " + _jobStatus.PercentageComplete.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            // Copy the EDL/EDLP files to the working directory (this is our destination always no matter where the source file is)
            if (File.Exists(SourceEDLFilePath) && !File.Exists(WorkingEDLFilePath)) // Move EDL file if it exists in source and not in working
            {
                try
                {
                    _jobLog.WriteEntry(this, "Commercial Scan: Saving EDL file.", Log.LogEntryType.Debug);
                    File.Copy(SourceEDLFilePath, WorkingEDLFilePath);
                }
                catch (Exception e)
                {
                    _jobLog.WriteEntry(this, "Commercial Scan: Unable to save EDL file. Error:\r\n" + e.ToString(), Log.LogEntryType.Error);
                    return false;
                }
            }

            if (File.Exists(SourceEDLPFilePath) && !File.Exists(WorkingEDLPFilePath)) // Move EDLP file if it exists in source and not in working
            {
                try
                {
                    _jobLog.WriteEntry(this, "Commercial Scan: Saving EDLP file.", Log.LogEntryType.Debug);
                    File.Copy(SourceEDLPFilePath, WorkingEDLPFilePath);
                }
                catch (Exception e)
                {
                    _jobLog.WriteEntry(this, "Commercial Scan: Unable to save EDLP file. Error:\r\n" + e.ToString(), Log.LogEntryType.Error);
                    return false;
                }
            }

            return (CheckAndUpdateEDL(WorkingEDLFilePath)); // Verify, validate, clean and store final EDL file
        }

        public bool CommercialsFound
        { get { return (!String.IsNullOrEmpty(EDLFile)); } }

        /// <summary>
        /// Path to EDL file where the original video lies
        /// </summary>
        private string SourceEDLFilePath
        { get { return Util.FilePaths.GetFullPathWithoutExtension(_videoFileName) + ".edl"; } }

        /// <summary>
        /// Path to EDLP file where the original video lies
        /// </summary>
        private string SourceEDLPFilePath
        { get { return Util.FilePaths.GetFullPathWithoutExtension(_videoFileName) + ".edlp"; } }

        /// <summary>
        /// Path to EDL file in the temp working directory
        /// </summary>
        private string WorkingEDLFilePath
        { get { return Path.Combine(_convOptions.workingPath, Path.GetFileNameWithoutExtension(_videoFileName) + ".edl"); } }

        /// <summary>
        /// Path to EDLP file in the temp working directory
        /// </summary>
        private string WorkingEDLPFilePath
        { get { return Path.Combine(_convOptions.workingPath, Path.GetFileNameWithoutExtension(_videoFileName) + ".edlp"); } }
    }
}
