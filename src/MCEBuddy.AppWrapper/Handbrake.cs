using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.AppWrapper 
{
    public class Handbrake : Base
    {
        private const string APP_PATH = "handbrake\\handbrakecli.exe";
        private const string APP_PATH_XP = "handbrake\\handbrakecli_xp.exe"; // special build for windows xp (QuickSync not supported on XP)
        private bool quickSyncEnabled = false;
        private bool parseCompleted = false;
        private DateTime _lastUpdate = DateTime.Now;
        private float _lastPercentage = 0;

        /// <summary>
        /// True if hardware encoding is enabled (quick sync enabled and accessible)
        /// </summary>
        public bool QuickSyncEncodingAvailable
        { get { return quickSyncEnabled; } }

        /// <summary>
        /// Check for hardware accelleration enablement (QuickSync). The result is stored in QuickSyncEncodingAvailable.
        /// </summary>
        public Handbrake(Log jobLog)
            : base("-i null -o null", (OSVersion.GetOSVersion() <= OSVersion.OS.WIN_2003 ? APP_PATH_XP : APP_PATH), new JobStatus(), jobLog, true)
        {
            _jobLog.WriteEntry(this, "Handbrake checking for OpenCL and QuickSync support", Log.LogEntryType.Information);
            _uiAdminSessionProcess = true; // This is a hardware enabled check
            Run();
            _success = true; // We dont have output here, so assume we are good, do this AFTER calling Run since the exit code may be non zero
            _jobLog.WriteEntry(this, "QuickSync encoding supported availble -> " + quickSyncEnabled.ToString(), Log.LogEntryType.Information);
        }

        public Handbrake(string Parameters, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(Parameters, (OSVersion.GetOSVersion() <= OSVersion.OS.WIN_2003 ? APP_PATH_XP : APP_PATH), jobStatus, jobLog, ignoreSuspend)
        {
            _uiAdminSessionProcess = true; // Always assume handbrake is running hardware API's, safety (no harm in running in UI session 1 always)
            _success = false; //Handbrake look for a +ve output in it's handlers so we start with a false
        }

        protected override void OutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs ConsoleOutput)
        {
            try
            {
                string StdOut;
                int StartPos, EndPos;
                float Perc;

                base.OutputHandler(sendingProcess, ConsoleOutput);
                if (ConsoleOutput.Data == null) return;

                if (!String.IsNullOrEmpty(ConsoleOutput.Data))
                {
                    StdOut = ConsoleOutput.Data;
                    if (StdOut.Contains("Encoding:") && StdOut.Contains(",") && StdOut.Contains("%"))
                    {
                        EndPos = StdOut.IndexOf("%");
                        for (StartPos = EndPos - 1; StartPos > -1; StartPos--)
                        {
                            if ((!char.IsNumber(StdOut[StartPos])) && (StdOut[StartPos] != '.') && (StdOut[StartPos] != ' '))
                            {
                                StartPos++;
                                break;
                            }
                        }
                        float.TryParse(StdOut.Substring(StartPos, EndPos - StartPos).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out Perc);
                        _jobStatus.PercentageComplete = Perc;

                        if (StdOut.Contains("ETA ") && StdOut.Contains(")"))
                        {
                            string ETAStr = "";
                            for (int idx = StdOut.IndexOf("ETA "); idx < StdOut.IndexOf(")"); idx++)
                            {
                                if (char.IsNumber(StdOut[idx]))
                                    ETAStr += StdOut[idx];
                            }
                            int ETAVal = 0;
                            int.TryParse(ETAStr, out ETAVal);
                            int Hours = ETAVal / 10000;
                            int Minutes = (ETAVal - (Hours * 10000)) / 100;
                            int Seconds = ETAVal - (Hours * 10000) - (Minutes * 100);
                            UpdateETA(Hours, Minutes, Seconds);
                        }
                    }

                    if (StdOut.Contains("task 1 of 2"))
                        _jobStatus.CurrentAction = Localise.GetPhrase("Converting video file - Pass 1");
                    else if (StdOut.Contains("task 2 of 2"))
                        _jobStatus.CurrentAction = Localise.GetPhrase("Converting video file - Pass 2");

                    if (StdOut.Contains("Rip done!") || StdOut.Contains("Encode done!"))
                        _success = true;

                    if (StdOut.Contains("HandBrake has exited"))
                        parseCompleted = true;

                    if (StdOut.Contains("Intel Quick Sync Video support:")) // Check for hardware drivers enabled (quick sync)
                    {
                        string answer = StdOut.Substring(StdOut.LastIndexOf(':') + 1).ToLower().Trim();
                        if (answer == "yes")
                            quickSyncEnabled = true;
                    }

                    // TODO: Need a reliable way to check for OpenCL version since the formats are not consistent and sometimes the numbers are missing
                    // - OpenCL version: 1.2 AMD-APP (1445.5)
                    // - OpenCL version: 1.2
                    // - OpenCL version: 1.1
                    // - OpenCL device type: GPU
                    // - OpenCL version: 1.1 CUDA
                    // OpenCL: library not available

                    // TODO: Intel QuickSync hardware hang detection hack. Is there a better way to do this?
                    if (quickSyncEnabled) // Sometimes hardware quicksync intel drivers are unstable and hang, detect hang condition
                    {
                        if (_lastPercentage != _jobStatus.PercentageComplete)
                        {
                            _lastPercentage = _jobStatus.PercentageComplete;
                            _lastUpdate = DateTime.Now;
                        }
                        else if (_lastUpdate.AddSeconds(GlobalDefs.HANG_PERIOD_DETECT) < DateTime.Now)  // Check for no progress
                        {
                            _jobLog.WriteEntry(this, "Hardware encoding appears to have hung, no progress in the last " + GlobalDefs.HANG_PERIOD_DETECT.ToString() + " seconds.\r\nThis is likely due to an unstable Intel Display Driver. Try updating or using a stable Intel Display Driver.\r\nTerminating process.", Log.LogEntryType.Error);
                            _unrecoverableError = true; // We are hung terminate the process
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "ERROR Processing Console Output.\n" + e.ToString(), Log.LogEntryType.Error);
            }
        }

        public override void Run()
        {
            base.Run();

            // Check the exit code, Handbrake sometime fails with a small file conversion and puts out encode done even on a failure but the exit code is not 0
            if (_exitCode != 0)
            {
                _jobLog.WriteEntry(this, "Handbrake failed, non 0 return code", Log.LogEntryType.Warning);
                _success = false;
            }

            int i = 0;
            while (_success && !parseCompleted && (i++ < 20) && !_jobStatus.Cancelled) // Check if the initialized failed
                Thread.Sleep(500); // Wait for messages to be flushed and parsing completed or until 10 seconds (incase of a failure to read) or job cancellation
        }
    }
}
