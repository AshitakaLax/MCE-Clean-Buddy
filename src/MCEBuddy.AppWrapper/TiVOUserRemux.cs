using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MCEBuddy.Util;
using MCEBuddy.Globals;

namespace MCEBuddy.AppWrapper
{
    public class TiVOUserRemux : Base
    {
        private const string APP_PATH = "MCEBuddy.RemuxTiVOStreams.exe";

        public TiVOUserRemux(string Parameters, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(Parameters, APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            // TODO: we need to relook at this, tivo filters still don't work in user space when launched from a service
            _uiAdminSessionProcess = true; // This apps needs to be run in User Space
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

                    // Check for percentage complete - First part is done via Streams (0% - 50%)
                    if (StdOut.Contains("Percentage complete :") && StdOut.Contains("%"))
                    {
                        int StartPos = StdOut.IndexOf("Percentage complete :") + "Percentage complete :".Length;
                        int EndPos = StdOut.IndexOf("%");

                        // Parse the % complete directly
                        float perc;
                        float.TryParse(StdOut.Substring(StartPos, EndPos - StartPos).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out perc);

                        // Backup, if the % is not reported and we have a duration
                        _jobStatus.PercentageComplete = perc / 2; // 0 to 50%
                        UpdateETAByPercentageComplete();
                    }

                    // Check for percentage complete - Second part done via TsMuxer (50% - 100%)
                    if (StdOut.Contains("% complete"))
                    {
                        int StartPos = StdOut.IndexOf("TSMuxer -->") + "TSMuxer -->".Length;
                        int EndPos = StdOut.IndexOf("%");
                        float Perc;
                        float.TryParse(StdOut.Substring(StartPos, EndPos - StartPos).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out Perc);
                        _jobStatus.PercentageComplete = 50 + Perc / 2; // 50 to 100%
                        UpdateETAByPercentageComplete();
                    }

                    // Always check in the end
                    if (StdOut.Contains("RemuxTiVOStreams Successful!!")) // look for success criteria
                        _success = true;
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "ERROR Processing Console Output.\n" + e.ToString(), Log.LogEntryType.Error);
            }
        }

        public override void Run()
        {
            _HangPeriod = 0; // disable process hang detection since it invokes TivoDecode and since there is no output and can appear to hang for large files
            base.Run();

            if (_success)
                _jobStatus.PercentageComplete = 100; // Only reliable way to measure percentage
        }
    }
}
