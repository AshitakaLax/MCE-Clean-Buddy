using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.AppWrapper
{
    public class RemuxSupp : Base
    {
        private const string APP_PATH = "remuxsupp\\remuxsupp.exe";
        private string _totalOutput = "";

        public RemuxSupp(string Parameters, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(Parameters, APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            _success = true; //ReMuxSupp looks for an error in it's handlers so by default we are true
        }

        protected override void OutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs ConsoleOutput)
        {
            try
            {
                base.OutputHandler(sendingProcess, ConsoleOutput);
                if (ConsoleOutput.Data == null) return;

                if (!String.IsNullOrEmpty(ConsoleOutput.Data))
                {
                    _totalOutput += ConsoleOutput.Data;
                    /* DOES NOT WORK - NO OUTPUT MEASURED DUE TO INTERNAL .NET BUFFER
                     * int startPos = _totalOutput.IndexOf("---");
                    int endPos = -1;
                    if (startPos > 0)
                    {
                        for (int i = startPos; i < _totalOutput.Length; i++)
                        {
                            if (_totalOutput[i] != '-') break;
                            endPos = i;
                        }
                        _jobStatus.PercentageComplete = (float)(((endPos - startPos) / 77) * 100);
                        UpdateETAByPercentageComplete();
                    }*/

                    if (_totalOutput.Contains("Exception in thread"))
                    {
                        _success = false;
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
            //workaround for async buffering for the output handler. Async buffers 1024 bytes of data before flushing it, remuxxsupp doesn't generate 1024 bytes of data causing the code to think it has hand and terminates it, so disable process hang detection for remuxxsupp for now
            _HangPeriod = 0; // disable process hang detection for ReMuxSupp
            base.Run();

            if (_success)
                _jobStatus.PercentageComplete = 100; // Only reliable way to measure percentage
        }
    }
}
