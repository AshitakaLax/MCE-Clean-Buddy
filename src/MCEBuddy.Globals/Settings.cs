using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace MCEBuddy.Globals
{
    public enum DRMType
    {
        All = 0,
        Protected = 1,
        Unprotected = 2,
    }

    public enum CommercialRemovalOptions
    {
        None = 0,
        Comskip = 1,
        ShowAnalyzer = 2,
    }

    public enum ShowType
    {
        Default = 0,
        Series = 1,
        Movie = 2,
        Sports = 3,
    }

    [Serializable]
    public struct ConfSettings
    {
        /// <summary>
        /// List of Conversion Tasks and conversion options for each task
        /// </summary>
        public List<ConversionJobOptions> conversionTasks;
        /// <summary>
        /// List of Monitor Tasks and options for each task
        /// </summary>
        public List<MonitorJobOptions> monitorTasks;
        /// <summary>
        /// General settings for MCEBuddy
        /// </summary>
        public GeneralOptions generalOptions;

        public ConfSettings(List<ConversionJobOptions> cjo, List<MonitorJobOptions> mjo, GeneralOptions go)
        {
            conversionTasks = cjo;
            monitorTasks = mjo;
            generalOptions = go;
        }
    }

    [Serializable]
    public class ConversionJobOptions
    {
        [Serializable]
        public class MetadataCorrectionOptions
        {
            /// <summary>
            /// Original title to compare
            /// </summary>
            public string originalTitle;
            /// <summary>
            /// Corrected title to replace
            /// </summary>
            public string correctedTitle;
            /// <summary>
            /// TVDB Series Id force
            /// </summary>
            public string tvdbSeriesId;
            /// <summary>
            /// IMDB Series Id force
            /// </summary>
            public string imdbSeriesId;

            public override string ToString()
            {
                string allOpts = "";

                allOpts += "Original Title -> " + originalTitle + "\r\n";
                allOpts += "Corrected Title -> " + correctedTitle + "\r\n";
                allOpts += "TVDB Series Id -> " + tvdbSeriesId + "\r\n";
                allOpts += "IMDB Series Id -> " + imdbSeriesId + "\r\n";

                return allOpts;
            }

            /// <summary>
            /// Clones the current object and returns a new instance of the object
            /// </summary>
            /// <returns>New clone object</returns>
            public MetadataCorrectionOptions Clone()
            {
                MetadataCorrectionOptions clone = (MetadataCorrectionOptions)this.MemberwiseClone();
                return clone;
            }
        };

        /// <summary>
        /// Name of conversion task
        /// </summary>
        public string taskName;
        /// <summary>
        /// Profile name used for this task
        /// </summary>
        public string profile;

        /// <summary>
        /// Filepath of source video
        /// </summary>
        public string sourceVideo;
        /// <summary>
        /// The Source SubFolder Path Relative to monitoring root ending with a \ if not blank
        /// </summary>
        public string relativeSourcePath;
        /// <summary>
        /// Output path
        /// </summary>
        public string destinationPath;
        /// <summary>
        /// Temp working path
        /// </summary>
        public string workingPath;
        /// <summary>
        /// Fallback to source directory if destination path is unavailable (for network drives)
        /// </summary>
        public bool fallbackToSourcePath;

        /// <summary>
        /// Skip conversion if destination file exists
        /// </summary>
        public bool skipReprocessing;
        /// <summary>
        /// Check history file for previous conversions
        /// </summary>
        public bool checkReprocessingHistory;
        /// <summary>
        /// Increment the filenames if the destination file exists
        /// </summary>
        public bool autoIncrementFilename;

        /// <summary>
        /// // Add to iTunes library
        /// </summary>
        public bool addToiTunes;
        /// <summary>
        /// Add to WMP Library
        /// </summary>
        public bool addToWMP;

        /// <summary>
        /// Maximum video width
        /// </summary>
        public int maxWidth;
        /// <summary>
        /// Video Quality
        /// </summary>
        public double qualityMultiplier;
        /// <summary>
        /// Frame Rate
        /// </summary>
        public string FPS;
        /// <summary>
        /// Automatically detect interlacing and process it
        /// </summary>
        public bool autoDeInterlace;
        /// <summary>
        /// Automatically detect and enable hardware encoding if available
        /// </summary>
        public bool preferHardwareEncoding;

        /// <summary>
        /// Volume increase/decrease
        /// </summary>
        public double volumeMultiplier;
        /// <summary>
        /// Dynamic Range Control
        /// </summary>
        public bool drc;
        /// <summary>
        /// Limit output to 2 channels
        /// </summary>
        public bool stereoAudio;
        /// <summary>
        /// Let encoder choose the best audio track
        /// </summary>
        public bool encoderSelectBestAudioTrack;
        /// <summary>
        /// Audio Language selection
        /// </summary>
        public string audioLanguage;
        /// <summary>
        /// Audio offset correction
        /// </summary>
        public double audioOffset;

        /// <summary>
        /// Trim initial video
        /// </summary>
        public int startTrim;
        /// <summary>
        /// Trim end of video
        /// </summary>
        public int endTrim;

        /// <summary>
        /// Extract closed captions
        /// </summary>
        public string extractCC;
        /// <summary>
        /// Closed Captions offset
        /// </summary>
        public double ccOffset;
        /// <summary>
        /// Embed the subtitles and chapters into the converted video
        /// </summary>
        public bool embedSubtitlesChapters;

        /// <summary>
        /// Commercial removal program
        /// </summary>
        public CommercialRemovalOptions commercialRemoval;
        /// <summary>
        /// Path to custom Comskip INI file
        /// </summary>
        public string comskipIni;
        /// <summary>
        /// Extract commercial markers from chapters
        /// </summary>
        public bool extractAdsFromChapters;

        /// <summary>
        /// Download information from internet
        /// </summary>
        public bool downloadSeriesDetails;
        /// <summary>
        /// Download the banner file from the internet
        /// </summary>
        public bool downloadBanner;
        /// <summary>
        /// Overwrite title from IMDB
        /// </summary>
        public bool overwriteTitleIMDB;
        /// <summary>
        /// Set of metadata correction options
        /// </summary>
        public MetadataCorrectionOptions[] metadataCorrections;
        /// <summary>
        /// Prioritize matching original broadcast date while retrieving metadata
        /// </summary>
        public bool prioritizeOriginalBroadcastDateMatch;
        /// <summary>
        /// Force movies or tv series
        /// </summary>
        public ShowType forceShowType;
        /// <summary>
        /// Write the metadata to the converted file
        /// </summary>
        public bool writeMetadata;

        /// <summary>
        /// rename by information
        /// </summary>
        public bool renameBySeries;
        /// <summary>
        /// alternate renaming
        /// </summary>
        public bool altRenameBySeries;
        /// <summary>
        /// custom rename file
        /// </summary>
        public string customRenameBySeries;
        /// <summary>
        /// Only rename and move the original file, do not convert or process the video
        /// </summary>
        public bool renameOnly;

        /// <summary>
        /// File selection filter
        /// </summary>
        public string fileSelection;
        /// <summary>
        /// Showname Metadata selection filter
        /// </summary>
        public string metaShowSelection;
        /// <summary>
        /// Network/Channel name Metadata selection filter
        /// </summary>
        public string metaNetworkSelection;
        /// <summary>
        /// Showtype selection
        /// </summary>
        public ShowType metaShowTypeSelection;
        /// <summary>
        /// Filter based on Copy protection type
        /// </summary>
        public DRMType metaDRMSelection;
        /// <summary>
        /// Filter for related Monitor Task name matching
        /// </summary>
        public string[] monitorTaskNames;

        /// <summary>
        /// Insert new conversions at the beginning/top of the queue
        /// </summary>
        public bool insertQueueTop;
        /// <summary>
        /// Create XML file from video properties
        /// </summary>
        public bool extractXML;

        /// <summary>
        /// Disable auto cropping
        /// </summary>
        public bool disableCropping;
        /// <summary>
        /// do commercial scan keep EDL file but skip cutting the commercials
        /// </summary>
        public bool commercialSkipCut;
        /// <summary>
        /// Don't copy to create a backup of original file - DANGEROUS
        /// </summary>
        public bool skipCopyBackup;
        /// <summary>
        /// Don't remux the original file to a TS
        /// </summary>
        public bool skipRemuxing;
        /// <summary>
        /// Ignore Copy Protection flags on recording while converting
        /// </summary>
        public bool ignoreCopyProtection;
        /// <summary>
        /// TiVO MAK key for decrypting and remuxing files and extracting metadata
        /// </summary>
        public string tivoMAKKey;

        /// <summary>
        /// domain name for network credentials
        /// </summary>
        public string domainName = "";
        /// <summary>
        /// user name for network credentials
        /// </summary>
        public string userName = "";
        /// <summary>
        /// password for network credentials
        /// </summary>
        public string password = "";

        /// <summary>
        /// If the conversion task enabled
        /// </summary>
        public bool enabled;

        public override string ToString()
        {
            string allOpts = "";

            allOpts += "Task -> " + taskName + "\r\n";
            allOpts += "Profile -> " + profile + "\r\n";
            allOpts += "Source File -> " + sourceVideo + "\r\n";
            allOpts += "Relative Source Path -> " + relativeSourcePath + "\r\n";
            allOpts += "Destination Path -> " + destinationPath + "\r\n";
            allOpts += "Working Path -> " + workingPath + "\r\n";
            allOpts += "Fallback Destination -> " + fallbackToSourcePath.ToString() + "\r\n";
            allOpts += "Skip ReProcessing -> " + skipReprocessing.ToString() + "\r\n";
            allOpts += "Check Reprocessing History -> " + checkReprocessingHistory.ToString() + "\r\n";
            allOpts += "Auto Increment Filename -> " + autoIncrementFilename.ToString() + "\r\n";
            allOpts += "Add to iTunes Library -> " + addToiTunes.ToString() + "\r\n";
            allOpts += "Add to WMP Library -> " + addToWMP.ToString() + "\r\n";
            allOpts += "Max Width -> " + maxWidth.ToString() + "\r\n";
            allOpts += "Quality Multipltier -> " + qualityMultiplier.ToString(CultureInfo.InvariantCulture) + "\r\n";
            allOpts += "FPS -> " + FPS + "\r\n";
            allOpts += "Auto DeInterlacing -> " + autoDeInterlace.ToString() + "\r\n";
            allOpts += "Prefer Hardware Encoding -> " + preferHardwareEncoding.ToString() + "\r\n";
            allOpts += "Volume Multipltier -> " + volumeMultiplier.ToString(CultureInfo.InvariantCulture) + "\r\n";
            allOpts += "DRC -> " + drc.ToString() + "\r\n";
            allOpts += "Force Stereo -> " + stereoAudio.ToString() + "\r\n";
            allOpts += "Encoder Select Best Audio Track -> " + encoderSelectBestAudioTrack.ToString() + "\r\n";
            allOpts += "Profile Audio Language -> " + audioLanguage.ToUpper() + "\r\n";
            allOpts += "Audio Offset -> " + audioOffset.ToString(CultureInfo.InvariantCulture) + "\r\n";
            allOpts += "Start Trim -> " + startTrim.ToString() + "\r\n";
            allOpts += "End Trim -> " + endTrim.ToString() + "\r\n";
            allOpts += "Closed Captions -> " + extractCC + "\r\n";
            allOpts += "Closed Captions Offset -> " + ccOffset.ToString(CultureInfo.InvariantCulture) + "\r\n";
            allOpts += "Embed Subtitles and Chapters -> " + embedSubtitlesChapters.ToString() + "\r\n";
            allOpts += "Commercial Removal -> " + commercialRemoval.ToString() + "\r\n";
            allOpts += "Custom Comskip INI Path -> " + comskipIni + "\r\n";
            allOpts += "Download Series Details -> " + downloadSeriesDetails.ToString() + "\r\n";
            allOpts += "Download Banner -> " + downloadBanner.ToString() + "\r\n";
            allOpts += "Overwrite Title from Internet -> " + overwriteTitleIMDB.ToString() + "\r\n";
            if (metadataCorrections != null)
            {
                for (int i = 0; i < metadataCorrections.Length; i++ )
                {
                    allOpts += "Metadata Correction => Option " + i.ToString() + "\r\n";
                    allOpts += metadataCorrections[i].ToString();
                }
            }
            allOpts += "Prioritize matching by Original Broadcast Date -> " + prioritizeOriginalBroadcastDateMatch.ToString() + "\r\n";
            allOpts += "Force Show Type -> " + forceShowType.ToString() + "\r\n";
            allOpts += "Write Metadata -> " + writeMetadata.ToString() + "\r\n";
            allOpts += "Rename by Series -> " + renameBySeries.ToString() + "\r\n";
            allOpts += "Alt Rename by Series -> " + altRenameBySeries.ToString() + "\r\n";
            allOpts += "Custom Rename by Series -> " + customRenameBySeries + "\r\n";
            allOpts += "Rename Only -> " + renameOnly.ToString() + "\r\n";
            allOpts += "File Selection Pattern -> " + fileSelection + "\r\n";
            allOpts += "Show Selection Pattern -> " + metaShowSelection + "\r\n";
            allOpts += "Channel Selection Pattern -> " + metaNetworkSelection + "\r\n";
            allOpts += "Show Type Selection -> " + metaShowTypeSelection.ToString() + "\r\n";
            allOpts += "DRM Type Selection -> " + metaDRMSelection.ToString() + "\r\n";
            allOpts += "Monitor Tasks Selection -> " + (monitorTaskNames == null ? "" : String.Join(",", monitorTaskNames)) + "\r\n";
            allOpts += "Insert at Top of Queue -> " + insertQueueTop.ToString() + "\r\n";
            allOpts += "Extract XML -> " + extractXML.ToString() + "\r\n";
            allOpts += "Disable Cropping -> " + disableCropping.ToString() + "\r\n";
            allOpts += "Task Commercial Skip Cut -> " + commercialSkipCut.ToString() + "\r\n";
            allOpts += "Skip Copying Original File for Backup -> " + skipCopyBackup.ToString() + "\r\n";
            allOpts += "Skip Remuxing Original File to TS -> " + skipRemuxing.ToString() + "\r\n";
            allOpts += "Ignore Copy Protection -> " + ignoreCopyProtection.ToString() + "\r\n";
            allOpts += "TiVO MAK Key -> " + tivoMAKKey + "\r\n";
            allOpts += "Domain Name -> " + domainName + "\r\n";
            allOpts += "User Name -> " + userName + "\r\n";
            allOpts += "Password -> " + new String('*', password.Length) + "\r\n"; // mask the password, preserve the length
            allOpts += "Task Enabled -> " + enabled.ToString() + "\r\n";

            return allOpts;
        }

        /// <summary>
        /// Clones the current object and returns a new instance of the object
        /// </summary>
        /// <returns>New clone object</returns>
        public ConversionJobOptions Clone()
        {
            ConversionJobOptions clone = (ConversionJobOptions) this.MemberwiseClone();
            if (metadataCorrections != null)
            {
                clone.metadataCorrections = new MetadataCorrectionOptions[metadataCorrections.Length];
                for (int i = 0; i < metadataCorrections.Length; i++)
                {
                    clone.metadataCorrections[i] = this.metadataCorrections[i].Clone(); // Clone this object as MemberwiseClone only does a shallow copy
                }
            }

            return clone;
        }
    }

    [Serializable]
    public class MonitorJobOptions
    {
        /// <summary>
        /// Name of monitor task
        /// </summary>
        public string taskName;
        /// <summary>
        /// directory to search for files
        /// </summary>
        public string searchPath;
        /// <summary>
        /// file search pattern
        /// </summary>
        public string searchPattern;
        /// <summary>
        /// Delete original file after successful conversion
        /// </summary>
        public bool deleteMonitorOriginal;
        /// <summary>
        /// Archive original file after successful conversion
        /// </summary>
        public bool archiveMonitorOriginal;
        /// <summary>
        /// Archive path
        /// </summary>
        public string archiveMonitorPath;
        /// <summary>
        /// monitor sub directories
        /// </summary>
        public bool monitorSubdirectories;
        /// <summary>
        /// Queue converted files also (by default converted files are ignored)
        /// </summary>
        public bool monitorConvertedFiles;
        /// <summary>
        /// Ignore the history and remonitor all files
        /// </summary>
        public bool reMonitorRecordedFiles;

        /// <summary>
        /// domain name for network credentials
        /// </summary>
        public string domainName = "";
        /// <summary>
        /// user name for network credentials
        /// </summary>
        public string userName = "";
        /// <summary>
        /// password for network credentials
        /// </summary>
        public string password = "";

        public override string ToString()
        {
            string allOpts = "";

            allOpts += "Task -> " + taskName + "\r\n";
            allOpts += "Search Path -> " + searchPath + "\r\n";
            allOpts += "Search Pattern -> " + searchPattern + "\r\n";
            allOpts += "Monitor Delete Original -> " + deleteMonitorOriginal.ToString() + "\r\n";
            allOpts += "Monitor Archive Original -> " + archiveMonitorOriginal.ToString() + "\r\n";
            allOpts += "Monitor Archive Folder -> " + archiveMonitorPath + "\r\n";
            allOpts += "Monitor SubDirectories -> " + monitorSubdirectories.ToString() + "\r\n";
            allOpts += "Monitor Converted Files -> " + monitorConvertedFiles.ToString() + "\r\n";
            allOpts += "ReMonitor Recorded Files -> " + reMonitorRecordedFiles.ToString() + "\r\n";
            allOpts += "Domain Name -> " + domainName + "\r\n";
            allOpts += "User Name -> " + userName + "\r\n";
            allOpts += "Password -> " + new String('*', password.Length) + "\r\n"; // mask the password, preserve the length

            return allOpts;
        }

        /// <summary>
        /// Clones the current object and returns a new instance of the object
        /// </summary>
        /// <returns>New clone object</returns>
        public MonitorJobOptions Clone()
        {
            return (MonitorJobOptions)this.MemberwiseClone();
        }
    }

    [Serializable]
    public class EmailBasicSettings
    {
        /// <summary>
        /// Name of SMTP server
        /// </summary>
        public string smtpServer;
        /// <summary>
        /// SMTP port number
        /// </summary>
        public int port;
        /// <summary>
        /// Use SSL
        /// </summary>
        public bool ssl;

        /// <summary>
        /// username for SMTP server
        /// </summary>
        public string userName;
        /// <summary>
        /// password for SMTP server
        /// </summary>
        public string password;

        /// <summary>
        /// From eMail address
        /// </summary>
        public string fromAddress;
        /// <summary>
        /// to eMail addresses (multiple separated by ;)
        /// </summary>
        public string toAddresses;
        /// <summary>
        /// Bcc eMail addresses (multiple separated by ;)
        /// </summary>
        public string bccAddress;


        public override string ToString()
        {
            string allOpts = "";

            allOpts += "SMTP Server -> " + smtpServer + "\r\n";
            allOpts += "Port -> " + port.ToString() + "\r\n";
            allOpts += "SSL -> " + ssl.ToString() + "\r\n";
            allOpts += "User Name -> " + userName + "\r\n";
            allOpts += "Password -> " + new String('*', password.Length) + "\r\n"; // mask the password, preserve the length
            allOpts += "From -> " + fromAddress + "\r\n";
            allOpts += "To -> " + toAddresses + "\r\n";
            allOpts += "Bcc -> " + bccAddress + "\r\n";

            return allOpts;
        }

        /// <summary>
        /// Clones the current object and returns a new instance of the object
        /// </summary>
        /// <returns>New clone object</returns>
        public EmailBasicSettings Clone()
        {
            return (EmailBasicSettings)this.MemberwiseClone();
        }
    }

    [Serializable]
    public class EMailOptions
    {
        /// <summary>
        /// Basic settings for sending an eMail
        /// </summary>
        public EmailBasicSettings eMailBasicSettings = new EmailBasicSettings();

        /// <summary>
        /// send eMail on successful conversion
        /// </summary>
        public bool successEvent;
        /// <summary>
        /// send eMail on failed conversion
        /// </summary>
        public bool failedEvent;
        /// <summary>
        /// send eMail on cancelled conversion
        /// </summary>
        public bool cancelledEvent;
        /// <summary>
        /// send eMail on start of conversion
        /// </summary>
        public bool startEvent;
        /// <summary>
        /// send eMail if unable to download series information
        /// </summary>
        public bool downloadFailedEvent;
        /// <summary>
        /// send eMail on adding a file to the queue
        /// </summary>
        public bool queueEvent;

        /// <summary>
        /// Custom subject for successful conversion
        /// </summary>
        public string successSubject;
        /// <summary>
        /// Custom subject for failed conversion
        /// </summary>
        public string failedSubject;
        /// <summary>
        /// Custom subject for cancelled conversion
        /// </summary>
        public string cancelledSubject; 
        /// <summary>
        /// Custom subject for start of conversion
        /// </summary>
        public string startSubject;
        /// <summary>
        /// Custom subject if unable to dowload series information
        /// </summary>
        public string downloadFailedSubject;
        /// <summary>
        /// Custom subject when adding a file to the queue for conversion
        /// </summary>
        public string queueSubject;
        /// <summary>
        /// blank eMail body for notification eMails
        /// </summary>
        public bool skipBody;

        public override string ToString()
        {
            string allOpts = "";

            allOpts += "Send eMail Settings -> " + eMailBasicSettings.ToString() + "\r\n";
            allOpts += "eMail On Success -> " + successEvent.ToString() + "\r\n";
            allOpts += "eMail On Failure -> " + failedEvent.ToString() + "\r\n";
            allOpts += "eMail On Cancellation -> " + cancelledEvent.ToString() + "\r\n";
            allOpts += "eMail On Start -> " + startEvent.ToString() + "\r\n";
            allOpts += "eMail On Download Failure -> " + downloadFailedEvent.ToString() + "\r\n";
            allOpts += "eMail On Queueing -> " + queueEvent.ToString() + "\r\n";
            allOpts += "Custom subject for Successful conversion -> " + successSubject + "\r\n";
            allOpts += "Custom subject for Failed conversion -> " + failedSubject + "\r\n";
            allOpts += "Custom subject for Cancelled conversion -> " + cancelledSubject + "\r\n";
            allOpts += "Custom subject for Start of conversion -> " + startSubject + "\r\n";
            allOpts += "Custom subject for Download Failure -> " + downloadFailedSubject + "\r\n";
            allOpts += "Custom subject for Queueing conversion -> " + queueSubject + "\r\n";
            allOpts += "Skip eMail Body for notifications -> " + skipBody.ToString() + "\r\n";

            return allOpts;
        }

        /// <summary>
        /// Clones the current object and returns a new instance of the object
        /// </summary>
        /// <returns>New clone object</returns>
        public EMailOptions Clone()
        {
            EMailOptions clone = (EMailOptions)this.MemberwiseClone();
            clone.eMailBasicSettings = this.eMailBasicSettings.Clone(); // Clone this object as MemberwiseClone only does a shallow copy
            return clone;
        }
    }

    [Serializable]
    public class GeneralOptions
    {
        /// <summary>
        /// domain name for network credentials
        /// </summary>
        public string domainName = "";
        /// <summary>
        /// user name for network credentials
        /// </summary>
        public string userName = "";
        /// <summary>
        /// password for network credentials
        /// </summary>
        public string password = "";

        /// <summary>
        /// Hour to wake system
        /// </summary>
        public int wakeHour;
        /// <summary>
        /// Minute to wake system
        /// </summary>
        public int wakeMinute;
        /// <summary>
        /// New conversion start hour
        /// </summary>
        public int startHour;
        /// <summary>
        /// New conversion start minute
        /// </summary>
        public int startMinute;
        /// <summary>
        /// New conversion stop hour
        /// </summary>
        public int stopHour;
        /// <summary>
        /// New conversion stop minute
        /// </summary>
        public int stopMinute;
        /// <summary>
        /// Days of week to start new conversions and wake up system
        /// </summary>
        public string daysOfWeek;

        /// <summary>
        /// Minimum number of days to keep the file before converting
        /// </summary>
        public int minimumAge;
        /// <summary>
        /// Max no of simutaneous conversions
        /// </summary>
        public int maxConcurrentJobs;

        /// <summary>
        /// Enable job logs
        /// </summary>
        public bool logJobs;
        /// <summary>
        /// Amount of details of logs
        /// </summary>
        public int logLevel;
        /// <summary>
        /// number of days to keep the logs
        /// </summary>
        public int logKeepDays;

        /// <summary>
        /// Use recycle bin for original recordings
        /// </summary>
        public bool useRecycleBin;
        /// <summary>
        /// Delete original file after successful conversion
        /// </summary>
        public bool deleteOriginal;
        /// <summary>
        /// Archive original file after successful conversion
        /// </summary>
        public bool archiveOriginal;
        /// <summary>
        /// Delete converted file when source file is deleted
        /// </summary>
        public bool deleteConverted;

        /// <summary>
        /// Allow system to enter sleep during active conversion
        /// </summary>
        public bool allowSleep;
        /// <summary>
        /// Suspend the conversion when the computer switches to battery mode
        /// </summary>
        public bool suspendOnBattery;

        /// <summary>
        /// Send emails on various events
        /// </summary>
        public bool sendEmail;
        /// <summary>
        /// Settings used for sending eMails
        /// </summary>
        public EMailOptions eMailSettings = new EMailOptions();

        /// <summary>
        /// Locale to be used
        /// </summary>
        public string locale;

        /// <summary>
        /// Temp folder
        /// </summary>
        public string tempWorkingPath;
        /// <summary>
        /// Archive folder
        /// </summary>
        public string archivePath;
        /// <summary>
        /// Path for moving original files on failure
        /// </summary>
        public string failedPath;
        /// <summary>
        /// Check for enough empty space
        /// </summary>
        public bool spaceCheck;
        /// <summary>
        /// Path to custom Comskip (donator version) to use
        /// </summary>
        public string comskipPath;
        /// <summary>
        /// Custom profiles.conf
        /// </summary>
        public string customProfilePath;
        
        /// <summary>
        /// Timeout for apps console output before they are determined as hung
        /// </summary>
        public int hangTimeout;
        /// <summary>
        /// Polling period for scanning for new files in the monitor tasks
        /// </summary>
        public int pollPeriod;
        /// <summary>
        /// Priority of the applications
        /// </summary>
        public string processPriority;
        /// <summary>
        /// Affinity of CPU set by user
        /// </summary>
        public IntPtr CPUAffinity;
        /// <summary>
        /// Last state of the MCEBuddy engine
        /// </summary>
        public bool engineRunning;
        /// <summary>
        /// For each commercial segment cut, incremental amount of seconds to offset the subtitles
        /// </summary>
        public double subtitleSegmentOffset;

        /// <summary>
        /// Port of the local MCEBuddy server engine, to host the service and enable UPnP
        /// </summary>
        public int localServerPort;
        /// <summary>
        /// UPnP support
        /// </summary>
        public bool uPnPEnable;
        /// <summary>
        /// Open a port in the firewall
        /// </summary>
        public bool firewallExceptionEnabled;

        public override string ToString()
        {
            string allOpts = "";

            allOpts += "Domain Name -> " + domainName + "\r\n";
            allOpts += "User Name -> " + userName + "\r\n";
            allOpts += "Password -> " + new String('*', password.Length) + "\r\n"; // mask the password, preserve the length
            allOpts += "Wake Hour -> " + wakeHour.ToString() + "\r\n";
            allOpts += "Wake Minute -> " + wakeMinute.ToString() + "\r\n";
            allOpts += "Start Hour -> " + startHour.ToString() + "\r\n";
            allOpts += "Start Minute -> " + startMinute.ToString() + "\r\n";
            allOpts += "Stop Hour -> " + stopHour.ToString() + "\r\n";
            allOpts += "Stop Minute -> " + stopMinute.ToString() + "\r\n";
            allOpts += "Days of Week -> " + daysOfWeek + "\r\n";
            allOpts += "Minimum Age -> " + minimumAge.ToString() + "\r\n";
            allOpts += "Max Concurrent Jobs -> " + maxConcurrentJobs.ToString() + "\r\n";
            allOpts += "Enable Job Logs -> " + logJobs.ToString() + "\r\n";
            allOpts += "Log Level -> " + logLevel.ToString() + "\r\n";
            allOpts += "Log Keep Days -> " + logKeepDays.ToString() + "\r\n";
            allOpts += "Use Recycle Bin -> " + useRecycleBin.ToString() + "\r\n";
            allOpts += "Delete Original -> " + deleteOriginal.ToString() + "\r\n";
            allOpts += "Archive Original -> " + archiveOriginal.ToString() + "\r\n";
            allOpts += "Sync Converted -> " + deleteConverted.ToString() + "\r\n";
            allOpts += "Allow Sleep During Conversions -> " + allowSleep.ToString() + "\r\n";
            allOpts += "Pause Conversion on Battery Power -> " + suspendOnBattery.ToString() + "\r\n";
            allOpts += "Send eMails -> " + sendEmail.ToString() + "\r\n";
            allOpts += "eMail settings -> " + eMailSettings.ToString() + "\r\n";
            allOpts += "Locale -> " + locale + "\r\n";
            allOpts += "Temp Working Path -> " + tempWorkingPath + "\r\n";
            allOpts += "Archive Folder -> " + archivePath + "\r\n";
            allOpts += "Failed Folder -> " + failedPath + "\r\n";
            allOpts += "Space Check -> " + spaceCheck.ToString() + "\r\n";
            allOpts += "Custom Comskip Path -> " + comskipPath + "\r\n";
            allOpts += "Custom profiles.conf -> " + customProfilePath + "\r\n";
            allOpts += "App Hang Timeout -> " + hangTimeout.ToString() + "\r\n";
            allOpts += "Scan New Files Poll Period -> " + pollPeriod.ToString() + "\r\n";
            allOpts += "Process Priority -> " + processPriority + "\r\n";
            allOpts += "CPU Affinity -> " + CPUAffinity.ToString("d") + "\r\n";
            allOpts += "Engine Running -> " + engineRunning.ToString() + "\r\n";
            allOpts += "Subtitle Cut Segment Incremental Offset -> " + subtitleSegmentOffset.ToString(CultureInfo.InvariantCulture) + "\r\n";
            allOpts += "Local Server Port -> " + localServerPort + "\r\n";
            allOpts += "UPnP Enabled -> " + uPnPEnable.ToString() + "\r\n";
            allOpts += "Firewall Exception Enabled -> " + firewallExceptionEnabled.ToString() + "\r\n";

            return allOpts;
        }

        /// <summary>
        /// Clones the current object and returns a new instance of the object
        /// </summary>
        /// <returns>New clone object</returns>
        public GeneralOptions Clone()
        {
            GeneralOptions clone = (GeneralOptions)this.MemberwiseClone();
            clone.eMailSettings = this.eMailSettings.Clone(); // Clone this object as MemberwiseClone only does a shallow copy
            return clone;
        }
    }
}
