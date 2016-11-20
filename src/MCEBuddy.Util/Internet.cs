using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace MCEBuddy.Util
{
    public static class Internet
    {
        public static bool WGet(string URL, string destFileName)
        {
            DateTime EndTime = DateTime.Now.AddSeconds(30);
            try
            {
                WebClient Client = new WebClient();
                Client.DownloadFileAsync(new Uri(URL), destFileName);
                while ((Client.IsBusy) && (DateTime.Now < EndTime))
                {
                    System.Threading.Thread.Sleep(200);
                }
                if (Client.IsBusy) Client.CancelAsync();
                Client.Dispose();
            }
            catch
            {
                if (File.Exists(destFileName)) File.Delete(destFileName);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Opens a link in the default browser or IE as fallback
        /// </summary>
        /// <param name="sUrl">URL to open</param>
        /// <param name="errLogSource">(Optional) Header to identify source in error log</param>
        public static void OpenLink(string sUrl, string errLogSource="")
        {
            if (String.IsNullOrWhiteSpace(sUrl))
                return;

            try
            {
                System.Diagnostics.Process.Start(sUrl);
            }

            catch (Exception exc1)
            {
                // System.ComponentModel.Win32Exception is a known exception that occurs when Firefox is default browser.  
                // It actually opens the browser but STILL throws this exception so we can just ignore it.  If not this exception,
                // then attempt to open the URL in IE instead.

                if (exc1.GetType().ToString() != "System.ComponentModel.Win32Exception")
                {
                    // sometimes throws exception so we have to just ignore
                    // this is a common .NET bug that no one online really has a great reason for so now we just need to try to open
                    // the URL using IE if we can.

                    try
                    {
                        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo("IExplore.exe", sUrl);
                        System.Diagnostics.Process.Start(startInfo);
                        startInfo = null;
                    }

                    catch (Exception e1)
                    {
                        Log.WriteSystemEventLog((String.IsNullOrWhiteSpace(errLogSource) ? "" : errLogSource + " : ") + "Unable to open link", EventLogEntryType.Warning);
                        Log.WriteSystemEventLog(e1.ToString(), EventLogEntryType.Warning);
                    }
                }
            }
        }
    }
}
