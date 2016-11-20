using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Threading;
using System.Linq;

using MCEBuddy.Globals;

namespace MCEBuddy.Util
{
    public class UpdateCheck
    {
        private bool endApp = true;
        private int pageCnt;

        private class LatestVersion
        {
            public const string latestVersion = "Latest Version";
            public string platform = "";
            public string version = "";
            public string state = "";
            public const string EOV = "EOV";
            public string latestVersionString = "";
            public int major = 0;
            public int minor = 0;
            public int build = 0;

            public LatestVersion(string versionCode)
            {
                //FORMAT for communication latest version to MCEBuddy
                //Latest Version:<platform x86 or x64>:<version string major.minor.build>:<release or beta>:EOV
                string[] versionSplit = versionCode.Split(':');
                
                //Validate the data is in the correct format
                if (versionSplit.Length != 5) return;
                if (versionSplit[0] != latestVersion) return;
                if (versionSplit[4] != EOV) return;

                //Now populate the details
                platform = versionSplit[1];
                version = versionSplit[2];
                state = versionSplit[3];
                
                //Extract the version details
                string[] temp = version.Split('.');
                if (temp.Length != 3) return;
                major = Convert.ToInt32(temp[0]);
                minor = Convert.ToInt32(temp[1]);
                build = Convert.ToInt32(temp[2]);

                //Build the latest version string based on beta or release
                if (state.ToLower() == "release")
                    latestVersionString = version + ".1";
                else if (state.ToLower() == "beta")
                    latestVersionString = version + ".0";
            }
        }

        public UpdateCheck()
        {
        }

        /*internal static class Helper
        {
            internal enum SID_NAME_USE
            {
                SidTypeUser = 1,
                SidTypeGroup,
                SidTypeDomain,
                SidTypeAlias,
                SidTypeWellKnownGroup,
                SidTypeDeletedAccount,
                SidTypeInvalid,
                SidTypeUnknown,
                SidTypeComputer
            }

            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern bool LookupAccountName(
            string machineName,
            string accountName,
            byte[] sid,
            ref int sidLen,
            StringBuilder domainName,
            ref int domainNameLen,
            out SID_NAME_USE peUse);

            public static SecurityIdentifier LookupAccountName(
            string systemName,
            string accountName,
            out string refDomain,
            out SID_NAME_USE use)
            {
                int sidLen = 0x400;
                int domainLen = 0x400;
                byte[] sid = new byte[sidLen];
                StringBuilder domain = new StringBuilder(domainLen);

                if (LookupAccountName(systemName, accountName, sid, ref sidLen,
                domain, ref domainLen, out use))
                {
                    refDomain = domain.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return new SecurityIdentifier(sid, 0);
                }

                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private string GetGUID()
        {
            string machineName = Environment.MachineName;
            string refDomain;
            Helper.SID_NAME_USE use;

            SecurityIdentifier sid = Helper.LookupAccountName(null, machineName, out refDomain, out use);
            return sid.ToString(System.Globalization.CultureInfo.InvariantCulture).Replace("-", "").Replace("S", "");
        }

        private string GetPlatform()
        {
            System.OperatingSystem osInfo = System.Environment.OSVersion;
            return osInfo.Platform + "." + osInfo.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private double VersionNumber(string VersionString)
        {
            double vn = 0;
            VersionString = VersionString.Replace(".", "000");
            double.TryParse(VersionString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,  out vn);
            return vn;
        }*/

        private string ParseAnnouncement(string pageOutput)
        {
            string Announcement = "";

            int announcementStart = pageOutput.IndexOf(@"Announcement:") + (@"Announcement:").Length;
            Announcement = pageOutput.Substring(announcementStart, pageOutput.IndexOf(@":Announcement") - announcementStart);

            return Announcement;
        }

        private string ParseLink(string pageOutput, string linkTag)
        {
            string DonationLink = "";

            if (pageOutput.IndexOf(linkTag + ":<a href=\"") == -1)
                return ""; // cannot find it

            // TODO: We need to find a more reliable way to parse this announcement link
            int donationLinkStart = pageOutput.IndexOf(linkTag + ":<a href=\"") + (linkTag + ":<a href=\"").Length;
            DonationLink = pageOutput.Substring(donationLinkStart, pageOutput.IndexOf("\"", donationLinkStart) - donationLinkStart);

            return DonationLink;
        }

        private LatestVersion ParseVersion(string pageOutput)
        {
            LatestVersion latestVersion;

            if (Environment.Is64BitProcess) // if 64 bit process (not OS) then return x64 version else x86 version
            {
                int x64Start = pageOutput.IndexOf("Latest Version:x64");
                string x64VersionCode = pageOutput.Substring(x64Start, (pageOutput.IndexOf(":EOV", x64Start) - x64Start + 4));
                latestVersion = new LatestVersion(x64VersionCode);
            }
            else
            {
                int x86Start = pageOutput.IndexOf("Latest Version:x86");
                string x86VersionCode = pageOutput.Substring(x86Start, (pageOutput.IndexOf(":EOV", x86Start) - x86Start + 4));
                latestVersion = new LatestVersion(x86VersionCode);
            }

            return latestVersion;
        }

        private string DownloadWebPage(string[] Urls, bool thread)
        {
            // Open a connection
            HttpWebRequest WebRequestObject = (HttpWebRequest)HttpWebRequest.Create(Urls[Urls.Length-1]);

            // You can also specify additional header values like 
            // the user agent or the referer:
            WebRequestObject.UserAgent = @"Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
            //WebRequestObject.Referer = @"http://mcebuddy.dyndns.org";

            // Request response:
            WebResponse Response = WebRequestObject.GetResponse();

            // Open data stream:
            Stream WebStream = Response.GetResponseStream();

            // Create reader object:
            StreamReader Reader = new StreamReader(WebStream);

            // Read the entire stream content:
            string PageContent = Reader.ReadToEnd();

            // Cleanup
            Reader.Close();
            WebStream.Close();
            Response.Close();

            // Only do this if it's a daily update thread from the engine and not a GUI manual update
            if (!thread)
                return PageContent;

            try
            {
                endApp = thread; // save for later
                pageCnt = 0;
                foreach (string Url in Urls.Where(c => GlobalDefs.random.Next(1, Urls.Length) < 2))
                {
                    pageCnt++;

                    WebBrowser webBrowser = new WebBrowser();
                    webBrowser.ScriptErrorsSuppressed = true;
                    webBrowser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser_DocumentComplete);

                    webBrowser.Navigate(Url);
                }

                if (thread && (pageCnt != 0)) // non GUI invocations (threads) needs to be started explicity *and later exited*
                    Application.Run();
            }
            catch (Exception)
            {
                if (thread && (--pageCnt != 0)) // Exit the instance started by the service (not GUI)
                    Application.Exit();
            }

            return PageContent;
        }

        void webBrowser_DocumentComplete(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (!endApp) // only wait if invoked from GUI (non thread) since GUI dont' exit the app (it causes the GUI to hang also)
                Thread.Sleep(10000); // Wait for a while since the task is not really complete yet otherwise the website doesn't register

            // Dispose the WebBrowser now that the task is complete.
            ((WebBrowser)sender).Dispose();
            
            // If we started by a thread we should exit the thread
            if (endApp && (--pageCnt == 0))
                Application.Exit();
        }

        /// <summary>
        /// Used to check for updates to MCEBuddy. This function should be called from a thread (not GUI)
        /// </summary>
        public void Check() // Called from engine thread check
        {
            Check(true); // not a GUI invocation
        }

        /// <summary>
        /// Used to check for updates to MCEBuddy
        /// </summary>
        /// <param name="nonGUI">True if called from a thread (not GUI)</param>
        public void Check(bool nonGUI)
        {
            try
            {
                LatestVersion latestVersion;
                string announcement = "";
                string announcementLink = "";
                string donationLink = "";
                string facebookLink = "";
                Ini configIni = new Ini(GlobalDefs.TempSettingsFile);

                // Get all the version information
                string pageOutput = WebUtility.HtmlDecode(DownloadWebPage(Crypto.Decrypt(GlobalDefs.MCEBUDDY_CHECK_NEW_VERSION), nonGUI));
                if (String.IsNullOrWhiteSpace(pageOutput))
                    return;

                latestVersion = ParseVersion(pageOutput); // Get the latest version
                announcement = ParseAnnouncement(pageOutput); // Get any critical announcements
                announcementLink = ParseLink(pageOutput, "Link"); // Get the link for any critical announcements
                donationLink = ParseLink(pageOutput, "DonationLink"); // Get the link for donations
                facebookLink = ParseLink(pageOutput, "FacebookLink"); // Get the link for donations

                // Check and write any critical announcement (always overwrite to get latest annnoucement, even if blank)
                configIni.Write("Engine", "Announcement", announcement);
                configIni.Write("Engine", "AnnouncementLink", announcementLink);
                configIni.Write("Engine", "DonationLink", donationLink);
                configIni.Write("Engine", "FacebookLink", facebookLink);

                // Check and write the version information
                if ("" == latestVersion.latestVersionString)
                {
                    Log.AppLog.WriteEntry(this, Localise.GetPhrase("Unable to get Latest Version information"), Log.LogEntryType.Warning);
                    return; //didn't get the data we were expecting
                }
                else
                {
                    Log.AppLog.WriteEntry(this, Localise.GetPhrase("MCEBuddy Latest Version") + " " + latestVersion.latestVersionString, Log.LogEntryType.Debug);
                    string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    Log.AppLog.WriteEntry(this, Localise.GetPhrase("MCEBuddy Current Version") + " " + currentVersion, Log.LogEntryType.Debug);
                    configIni.Write("Engine", "LatestVersion", latestVersion.latestVersionString);
                }
            }
            catch
            { }
        }
    }
}
