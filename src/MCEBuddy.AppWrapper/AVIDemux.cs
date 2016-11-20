using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.AppWrapper
{
    public class AVIDemux : Base
    {
        private const string APP_PATH = "AVIDemux\\AVIDemux_cli.exe";

        public AVIDemux(string Parameters, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(Parameters, APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            _success = true;
        }

        protected override void OutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs ConsoleOutput)
        {
            try
            {
                string StdOut;

                base.OutputHandler(sendingProcess, ConsoleOutput);
                if (ConsoleOutput.Data == null) return;

                if (!String.IsNullOrEmpty(ConsoleOutput.Data))
                {
                    StdOut = ConsoleOutput.Data;

                    if (StdOut.Contains("use the h264_mp4toannexb bitstream filter")) // This video operations is toast cannot be recovered, terminate the operation
                    {
                        _jobLog.WriteEntry(this, "Unrecoverable error, terminating processing of AviDemux", Log.LogEntryType.Warning);
                        _unrecoverableError = true;
                    }

                    // TODO: Need a reliable way to check the output for errors (* Error * - is NOT reliable, since it throws an error on success also)
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "ERROR Processing Console Output.\n" + e.ToString(), Log.LogEntryType.Error);
            }
        }

        public override void Run()
        {
            _HangPeriod = 30 * 60; // AviDemux can appear to hang, with no output for long period of time, give it 30 minutes

            base.Run();

            if (_success)
                _jobStatus.PercentageComplete = 100; // we are good
        }
    }
}
