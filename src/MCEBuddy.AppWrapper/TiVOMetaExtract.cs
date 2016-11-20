using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.AppWrapper
{
    public class TiVOMetaExtract : Base
    {
        private const string APP_PATH = "tivo\\tdcat.exe";

        public TiVOMetaExtract(string Parameters, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(Parameters, APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            _success = true;
        }
    }
}
