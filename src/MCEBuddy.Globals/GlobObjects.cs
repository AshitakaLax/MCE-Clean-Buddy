using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.ServiceModel;
using System.Security.Permissions;

namespace MCEBuddy.Globals
{
    public static class GlobalDefs
    {
        public static Random random = new System.Random();
        public static bool IsEngineRunningAsService = false; // Are we running the engine as a service

        volatile public static bool Shutdown = false;
        volatile public static bool Active = false;
        volatile public static bool Pause = false;

        public const string UNICODE_TEMPNAME = "UnicodeTempName"; // Temporary filename to use when a unicode filename is detected
        public const double GLOBAL_LOGFILE_SIZE_THRESHOLD = 50 * 1024 * 1024; // Global log file size 50MB max
        public const int MAX_SHUTDOWN_WAIT = 5000; // Maximum time to wait for thread to exit clean on stop before aborting it
        public const double SPACE_REQUIRED_MULTIPLIER = 2.5; // Ideally this should be 2, 1 for the TS file and 1 incase of NoRecode which doesn't compress, but most folks who use multiple conversions are converting to mp4 so 1.5 is more than enough.
        public const double MAX_CONCURRENT_SPACE_REQUIRED_MULTIPLIER = 0.7; // Then multiple tasks are running what are the chances of requiring free space simultaneously
        public const int MAX_SUBTITLE_DESCRIPTION_EXTRACTION_LENGTH = 100; // When extracting subtitles, the maximum length to consider (to avoid invalid filenames)
        public const int MAX_EVENT_MESSAGES_TRANSFER = 500; // Maximum number of messages to transfer (avoid pipe overload)
        public const int EMAIL_SEND_ENGINE_RETRY_PERIOD = 5 * 60 * 1000; // Email sending retry period (minutes)
        public const int ENGINE_CORE_SLEEP_PERIOD = 200; // Time for which core engine sleeps between status updates
        public const int MONITOR_POLL_PERIOD = 300; // Default poll period
        public const int HANG_PERIOD_DETECT = 300; // Default hang detection period
        public const string SEGMENT_CUT_OFFSET_GOP_COMPENSATE = "0.0"; // average number of second to compensate for GOP alignment when cutting segments (used to Subtitle Alignment)
        public const double MINIMUM_SEGMENT_LENGTH = 20; // Number of seconds each file segment will be when cutting files during commercial removal, keep high to avoid AviDemux from hanging (atleast one GOP every x seconds)
        public const int NOTIFY_ICON_TIP_TIMEOUT = 1; // Timeout for Ballon tip in system tray
        public const int CURRENT_QUEUE_SCROLL_PERIOD = 150; // Scroll timer for moving items in the queue
        public const int ACCEPTABLE_COMPLETION = 85; //What % of the process are accepted as complete in the output handlers, typically there is a small buffer to it doesn't show 100% even though it's completed
        public const int PERCENTAGE_HISTORY_SIZE = 300; // How many previous timestamps of the reported percentage numbers to capture to calculate ETA
        public const double MINIMUM_MERGED_FILE_THRESHOLD = 0.99; // Minimum size of merged file as a % of cut files during commercial removals
        public static TimeSpan PIPE_TIMEOUT = TimeSpan.MaxValue; // Communication pipe message timeout
        public const string MCEBUDDY_ARCHIVE = "MCEBuddyArchive";
        public const string DEFAULT_NETWORK_USERNAME = "Guest"; // Default username for accessing network shares
        public const int MCEBUDDY_EVENT_LOG_ID = 23332; // Unique id for the event log
        public const string MCEBUDDY_EVENT_LOG_SOURCE = "MCEBuddy2x"; // name of application source in System event log
        public const string MCEBUDDY_SERVICE_NAME = "MCEBuddy2x";
        public const string DEFAULT_CC_OFFSET = "2.5"; // Default offset in seconds, this has to be kept in sync with -ss parameter in the profiles video encoding parameters
        public const float MAX_VIDEO_DURATION = 9999999999F; // Maximum length of any video file in seconds
        public const int SMTP_TIMEOUT = 60 * 1000; // Smtp time out in ms
        public const string DEFAULT_VIDEO_STRING = "[video]"; // String to represent DEFAULT_VIDEO_FILE_TYPES
        public const string DEFAULT_VIDEO_FILE_TYPES = "*.dvr-ms;*.wtv;*.asf;*.avi;*.divx;*.dv;*.flv;*.gxf;*.m1v;*.m2v;*.m2ts;*.m4v;*.mkv;*.mov;*.mp2;*.mp4;*.mpeg;*.mpeg1;*.mpeg2;*.mpeg4;*.mpg;*.mts;*.mxf;*.ogm;*.ts;*.vob;*.wmv;*.tp;*.tivo";
        public static DateTime NO_BROADCAST_TIME = DateTime.Parse("1900-01-01T00:00:00Z");
        public const string MCEBUDDY_SERVER_NAME = "localhost";
        public const string MCEBUDDY_SERVER_PORT = "23332";
        public const string MCEBUDDY_WEB_SOAP_PIPE = "http://" + MCEBUDDY_SERVER_NAME + ":" + MCEBUDDY_SERVER_PORT + "/MCEBuddy2x";
        public const string MCEBUDDY_LOCAL_NAMED_PIPE = "net.pipe://" + MCEBUDDY_SERVER_NAME + "/MCEBuddy2x";
        public const int GUI_REFRESH_PERIOD = 300; // GUI refresh period in ms
        public const int LOCAL_ENGINE_POLL_PERIOD = 500; // Poll the engine for status on local machine
        public const int REMOTE_ENGINE_POLL_PERIOD = 2000; // Poll the engine for status on remote machine over TCP/IP
        public const int GUI_MINIMIZED_ENGINE_POLL_SLOW_FACTOR = 4; // When GUI is minimized we poll for updates from engine x times slower than normal to save CPU cycles
        public const int UPNP_POLL_PERIOD = 30 * 60 * 1000; // Poll UPnP device every 'x' minutes for port mappings
        public const BasicHttpSecurityMode MCEBUDDY_PIPE_SECURITY = BasicHttpSecurityMode.None; // Security for Pipe communication using WCF
        public const long SRT_FILE_MINIMUM_SIZE = 50; // Smaller valid SRT file size in bytes

        public const string MCEBUDDY_DOCUMENTATION = @"QAuka3atGmUjD0FwLUr79ldA3+xCl7HoOzfOjhg/inp4J+U8xTorW8EyMj6l4A0I";
        public const string MCEBUDDY_HOME_PAGE = @"NbuyXidyOPpP4UvO4Of1vwGUTbP6KxLEaaz0KLu+1T8=";
        public const string MCEBUDDY_FACEBOOK_PAGE = @"Xvr7aJ0Ow2qBVCnx+hTowoowhlpZpQpbHCHiwCFF8YMEznl9gVWfFm2w5yXJFK4F";
        public const string MCEBUDDY_DOWNLOAD_NEW_VERSION = @"QAuka3atGmUjD0FwLUr79ldA3+xCl7HoOzfOjhg/inpy78es8qovJroO6F6hCBDi";
        public static string[] MCEBUDDY_CHECK_NEW_VERSION = { @"QAuka3atGmUjD0FwLUr79ldA3+xCl7HoOzfOjhg/inr7Rn5fm1uom2wGvDC2T1UD",
                                                             @"QAuka3atGmUjD0FwLUr79ldA3+xCl7HoOzfOjhg/inp4J+U8xTorW8EyMj6l4A0I",
                                                             @"QAuka3atGmUjD0FwLUr79ldA3+xCl7HoOzfOjhg/inpy78es8qovJroO6F6hCBDi",
                                                             @"QAuka3atGmUjD0FwLUr79ldA3+xCl7HoOzfOjhg/inp34pNo/LY03EfNQQCVZHG5",
                                                             @"QAuka3atGmUjD0FwLUr79ldA3+xCl7HoOzfOjhg/inrgUssorloPuQEiE5lPPNheCFSqfCw1tMD47I1AUe0lTQ==",
                                                             @"QAuka3atGmUjD0FwLUr79ldA3+xCl7HoOzfOjhg/inofcQfLYNWhEzxktHEaFXxdHjqdEJlkQJ58JEx3WuMkhA=="
                                                           };

        public static string AppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);
        public static string CachePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName), "cache");
        public static string ConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName), "config");
        public static string LogPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName), "log");
        public static string LocalisationPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName), "localisation");

        public static string ConfigFile = Path.Combine(ConfigPath, "mcebuddy.conf"); // Store MCEBuddy settings carried over between upgrades
        public static string ProfileFile = Path.Combine(ConfigPath, "profiles.conf"); // Store the conversion base profiles
        public static string HistoryFile = Path.Combine(ConfigPath, "history."); // Store the history of conversions and status
        public static string TempSettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "mcebuddy.");  // Temp settings not carried over between upgrades
        public static string ManualQueueFile = Path.Combine(ConfigPath, "manualqueue.");
        public static string AppLogFile = Path.Combine(LogPath, "mcebuddy.log");

        public const string DEFAULT_WINVISTA_AND_LATER_PATH = @"C:\Users\Public\Recorded TV";
        public const string DEFAULT_WINXP_PATH = @"C:\Documents and Settings\All Users\Documents\Recorded TV";
        public const string DEFAULT_WINVISTA_AND_LATER_PATH_OUTPUT = @"C:\Users\Public\Videos";
        public const string DEFAULT_WINXP_PATH_OUTPUT = @"C:\Documents and Settings\All Users\Documents\My Videos";

        public static string[] supportFilesExt = { ".edl", ".srt", ".xml", ".nfo", ".arg" }; // All extra files generated/supported by MCEBuddy along with converted/original file

        public static ProcessPriorityClass Priority = ProcessPriorityClass.Normal;
        public static ProcessPriority IOPriority = ProcessPriority.NORMAL_PRIORITY_CLASS;

        public const ProcessPriorityClass EnginePriority = ProcessPriorityClass.AboveNormal;
        public const ProcessPriority EngineIOPriority = ProcessPriority.ABOVE_NORMAL_PRIORITY_CLASS;

        public static string[] mpeg1Codecs = { "mpegvideo", "mpeg1video" };
        public static string[] mpeg2Codecs = { "mpeg2video" };
        public static string[] mpeg4Part2Codecs = { "msmpeg4", "msmpeg4v1", "msmpeg4v2", "mpeg4", "mpeg4video", "xvid", "divx" };
        public static string[] mpeg4Part10Codecs = { "h264", "h.264", "avc" };
        public static string[] h263Codecs = { "h263", "h263i", "h263p" };
    }
}
