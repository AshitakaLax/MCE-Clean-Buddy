using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.AppWrapper
{
    public class Comskip : Base
    {
        private const string APP_PATH = "comskip\\comskip.exe";
        private bool firstRun = true;
        private float lastPerc = 0;
        private float duration = 0; // total length of the video in seconds
        private bool donator = false; // Is this a donator version detected

        /// <summary>
        /// True if comskip is a donator version
        /// </summary>
        public bool IsDonator
        { get { return donator; } }

        /// <summary>
        /// Special version to check of the comskip is a donator version, the result is stored in IsDonator.
        /// </summary>
        /// <param name="comskipPath">Path to custom comskip.exe, use "" for default comskip.exe</param>
        public Comskip(string comskipPath, Log jobLog)
            : base("-h", (String.IsNullOrWhiteSpace(comskipPath) ? APP_PATH : comskipPath), new JobStatus(), jobLog, true)
        {
            jobLog.WriteEntry("Checking for donator version of Comskip", Log.LogEntryType.Debug);
            _success = true;
            Run(); // Run it and capture if Donator flag exists
            jobLog.WriteEntry("Comskip version : " + (donator ? "DONATOR" : "FREE"), Log.LogEntryType.Information);
        }

        /// <summary>
        /// Runs Comskip
        /// </summary>
        /// <param name="Path">Path to custom comskip.exe, use "" for standard comskip.exe</param>
        /// <param name="Parameters">Parameters</param>
        /// <param name="SecondsDuration">Duration of file in seconds if known</param>
        public Comskip(string comskipPath, string Parameters, float SecondsDuration, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(false, Parameters, (String.IsNullOrWhiteSpace(comskipPath) ? Path.Combine(GlobalDefs.AppPath, APP_PATH) : comskipPath), false, jobStatus, jobLog, ignoreSuspend)
        {
            _success = true; //output handlers don't look for any true, only false can be set by process issues
            duration = SecondsDuration; // Length of the video in seconds
        }

        protected override void OutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs ConsoleOutput)
        {
            try
            {
                string StdOut;
                int StartPos, EndPos;
                float perc;

                base.OutputHandler(sendingProcess, ConsoleOutput);
                if (String.IsNullOrEmpty(ConsoleOutput.Data)) return;

                StdOut = ConsoleOutput.Data;

                try
                {
                    //Parse the Duration details if we can get it
                    if (StdOut.Contains("Duration:") && StdOut.Contains(",") && (duration <= 0))
                    {
                        StartPos = StdOut.IndexOf("Duration:") + "Duration:".Length;
                        EndPos = StdOut.IndexOf(",", StartPos);
                        duration = TimeStringToSecs(StdOut.Substring(StartPos, EndPos - StartPos));
                        _jobLog.WriteEntry(this, "Comskip detected video duration = " + duration.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                    }
                }
                catch (Exception ex)
                {
                    _jobLog.WriteEntry(this, "Comskip error parsing Duration. String:" + StdOut + "\n" + ex.ToString(), Log.LogEntryType.Warning);
                }

                if (StdOut.Contains("Donator")) // Is this a donator version?
                    donator = true;

                if (StdOut.Contains("%")) // Old version of Comskip and new version sometimes doesn't report %, we need to calculate it by other methods
                {
                    EndPos = StdOut.IndexOf("%");
                    for (StartPos = EndPos - 1; StartPos > -1; StartPos--)
                    {
                        if (!char.IsNumber(StdOut[StartPos]))
                        {
                            StartPos++;
                            break;
                        }
                    }

                    // Parse the % complete directly
                    float.TryParse(StdOut.Substring(StartPos, EndPos - StartPos).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out perc);

                    // Backup, if the % is not reported and we have a duration
                    if ((perc <= 0) && duration > 0)
                    {
                        // Calculate the % based on time processed vs total duration
                        StartPos = 0;
                        EndPos = StdOut.IndexOf("-") - 1;
                        TimeSpan time;
                        TimeSpan.TryParse(StdOut.Substring(StartPos, EndPos - StartPos), out time);
                        float timeProcessed = (float)time.TotalSeconds;
                        perc = timeProcessed / duration * 100;
                    }

                    if (perc > 0 && perc <= 100) // old version sometimes reports time duration which can lead to > 100%
                    {
                        if (firstRun) //sometime Comskip makes 2 passes, but sometimes only 1
                        {
                            _jobStatus.CurrentAction = Localise.GetPhrase("Comskip advertisement scan - Pass 1");
                            if (perc < lastPerc)
                            {
                                firstRun = false;
                            }
                            else
                            {
                                lastPerc = perc;
                                _jobStatus.PercentageComplete = perc;
                                UpdateETAByPercentageComplete();
                            }
                        }
                        else
                        {
                            _jobStatus.CurrentAction = Localise.GetPhrase("Comskip advertisement scan - Pass 2");
                            _jobStatus.PercentageComplete = perc;
                            UpdateETAByPercentageComplete();
                        }
                    }
                    else
                    {
                        _jobStatus.CurrentAction = Localise.GetPhrase("Comskip advertisement scan - Pass 1");
                        _jobStatus.PercentageComplete = 0;
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "ERROR Processing Console Output.\n" + e.ToString(), Log.LogEntryType.Error);
            }
        }

        // Convert the string to time in seconds
        private float TimeStringToSecs(string timeString)
        {
            // Cater for different cuilds fo ffmpeg and their varying output
            if (timeString.Contains(":"))
            {
                float secs = 0;
                int mult = 1;
                string[] timeVals = timeString.Split(':');
                for (int i = timeVals.Length - 1; i >= 0; i--)
                {
                    float val = 0;
                    float.TryParse(timeVals[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out val);
                    secs += mult * val;
                    mult = mult * 60;
                }
                return secs;
            }
            else
            {
                float secs = 0;
                float.TryParse(timeString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out secs);
                return secs;
            }
        }
    }
}
