using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.AppWrapper
{
    public class CCExtractor : Base
    {
        private const string APP_PATH = "ccextractor\\ccextractorwin.exe";
        private bool _formatError = false; // did it identify the format correctly?

        /// <summary>
        /// Potentially, ccextractor did not identify the file format correctly
        /// (also check if output file wasn't created)
        /// </summary>
        public bool FormatReadingError { get { return _formatError; } }

        public CCExtractor(string Parameters, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(Parameters, APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            _success = false; //CCExtractor looks for success criteria
        }

        protected override void OutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs ConsoleOutput)
        {
            try
            {
                string StdOut;
                float perc;

                base.OutputHandler(sendingProcess, ConsoleOutput);
                if (ConsoleOutput.Data == null) return;

                if (!String.IsNullOrEmpty(ConsoleOutput.Data))
                {
                    StdOut = ConsoleOutput.Data;
                    if (StdOut.Contains("%") && StdOut.Contains("|") && StdOut.Contains(":"))
                    {
                        string[] details = StdOut.Split('|');

                        // Update the % completion
                        string percStr = details[0].Substring(0, details[0].IndexOf('%'));
                        float.TryParse(percStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out perc);
                        _jobStatus.PercentageComplete = perc;
                        UpdateETAByPercentageComplete();
                    }

                    // Potential format reading error
                    if (StdOut.Contains("Not a recognized header. Searching for next header."))
                        _formatError = true;

                    // Success Criteria
                    if (StdOut.Contains("Done, processing time"))
                        _success = true;
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "ERROR Processing Console Output.\n" + e.ToString(), Log.LogEntryType.Error);
            }
        }
    }
}
