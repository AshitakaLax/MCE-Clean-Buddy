using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.AppWrapper
{
    public class MP4Box : Base
    {
        protected bool _SafeExit = false;
        private const string APP_PATH = "mp4Box\\mp4box.exe";

        public MP4Box(string Parameters, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(Parameters, APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            _success = true; //handlders don't look for anything, only look at terminating process for errors
        }

        protected override void OutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs ConsoleOutput)
        {
            try
            {
                string StdOut;
                int StartPos, EndPos;
                float perc;

                base.OutputHandler(sendingProcess, ConsoleOutput);
                if (ConsoleOutput.Data == null) return;

                if (!String.IsNullOrEmpty(ConsoleOutput.Data))
                {
                    StdOut = ConsoleOutput.Data;
                    if (StdOut.Contains("/100"))
                    {
                        EndPos = StdOut.IndexOf("/100");
                        for (StartPos = EndPos - 1; StartPos > -1; StartPos--)
                        {
                            if ((!char.IsNumber(StdOut[StartPos])) && (StdOut[StartPos] != '.') && (StdOut[StartPos] != ' '))
                            {
                                StartPos++;
                                break;
                            }
                        }
                        float.TryParse(StdOut.Substring(StartPos, EndPos - StartPos).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out perc);
                        _jobStatus.PercentageComplete = perc;
                        UpdateETAByPercentageComplete();
                    }

                    if (StdOut.Contains("Error importing"))
                        _success = false;

                    if (StdOut.Contains("Error writing data (Invalid argument): -1 blocks to write but 0 blocks written"))
                    {
                        _success = false;
                        _unrecoverableError = true; // This is just going to hang forever in this loop, terminate the process
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "ERROR Processing Console Output.\n" + e.ToString(), Log.LogEntryType.Error);
            }
        }
    }
}
