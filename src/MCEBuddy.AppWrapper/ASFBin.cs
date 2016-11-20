using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MCEBuddy.Util;
using MCEBuddy.Globals;

namespace MCEBuddy.AppWrapper
{
    public class ASFBin : Base
    {
        private const string APP_PATH = "ASFBin\\asfbin.exe";
        private bool _finalStage = false;
        private int _segmentCount = 0;
        private int _noOf100 = 0;

        public ASFBin(string Parameters, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(Parameters, APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            _success = false;
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

                    // % calculation is a based on rough estimates of number of file to parse and number of segments
                    if (StdOut.Contains("Segment no."))
                    {
                        int start = StdOut.IndexOf("Segment no.") + "Segment no.".Length;
                        int end = StdOut.IndexOf(":", start);

                        int.TryParse(StdOut.Substring(start, end - start), out _segmentCount); // Get the segment count, this will keep updating upto max)
                    }

                    if (StdOut.Contains("Writing output file")) // Final stage, now look for the 100%
                        _finalStage = true;

                    if (StdOut.Contains("0-100%:") && StdOut.Contains("...100"))
                    {
                        if (_finalStage) // look for success in final stage
                            _success = true; // we actually have success
                        else // Percentage status hack, until we are at final stage (not 100% reliable)
                        {
                            _noOf100++; // we actually have success
                            _jobStatus.PercentageComplete = _noOf100 / _segmentCount * 100;
                            UpdateETAByPercentageComplete();
                        }
                    }

                    // Always check in the end
                    if (StdOut.Contains("PROCESSING FAILED!")) // look for  failure criteria
                        _success = false;
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

            if (_success)
                _jobStatus.PercentageComplete = 100; // Only reliable way to measure percentage
        }
    }
}
