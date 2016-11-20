using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.AppWrapper
{
    public class Eac3To : Base
    {
        protected bool _SafeExit = false;
        private const string APP_PATH = "eac3to\\eac3to.exe";

        public Eac3To(string Parameters, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(Parameters, APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            _success = true; //no output handlers here, so we assume everything is good unless the process is terminated
        }

        public override void Run()
        {
            base.Run();

            if (_success)
                _jobStatus.PercentageComplete = 100; // we are good
        }
    }
}
