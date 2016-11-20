using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.MetaData
{
    public class CustomRename
    {
        /// <summary>
        /// This function is used to create a custom filename and path.
        /// This function throws an exception if an invalid rename pattern is provided
        /// </summary>
        /// <param name="customRenamePattern">The renaming pattern</param>
        /// <param name="newFileName">Reference to a string that will contain the new custom Filename</param>
        /// <param name="destinationPath">Reference to a string that will contains the new custom Path</param>
        /// <param name="sourceVideo">Path to Source Video file</param>
        /// <param name="metaData">Metadata for the Source Video file</param>
        /// <param name="jobLog">Log object</param>
        public static void CustomRenameFilename(string customRenamePattern, ref string newFileName, ref string destinationPath, string sourceVideo, VideoTags metaData, Log jobLog)
        {
            char[] renameBytes = customRenamePattern.ToCharArray();
            for (int i = 0; i < renameBytes.Length; i++)
            {
                switch (renameBytes[i])
                {
                    case '%':
                        string command = "";
                        while (renameBytes[++i] != '%')
                            command += renameBytes[i].ToString(System.Globalization.CultureInfo.InvariantCulture).ToLower();

                        string format = "";
                        switch (command)
                        {
                            case "premiereyear": // %ad% - premiere Air Year
                                if (metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.SeriesPremiereDate.ToLocalTime().Year.ToString("0000", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                    jobLog.WriteEntry(("Cannot find PremiereYear"), Log.LogEntryType.Warning);
                                break;

                            case "premieremonth": // %ad% - premiere Air Month
                                if (metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.SeriesPremiereDate.ToLocalTime().Month.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                    jobLog.WriteEntry(("Cannot find PremiereMonth"), Log.LogEntryType.Warning);
                                break;

                            case "premieremonthshort": // %ad% - premiere Air Month abbreviation
                                if (metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.SeriesPremiereDate.ToLocalTime().ToString("MMM"); // need to keep it culture sensitive here
                                else
                                    jobLog.WriteEntry(("Cannot find PremiereMonthShort"), Log.LogEntryType.Warning);
                                break;

                            case "premieremonthlong": // %ad% - premiere Air Month full name
                                if (metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.SeriesPremiereDate.ToLocalTime().ToString("MMMM"); // need to keep it culture sensitive here
                                else
                                    jobLog.WriteEntry(("Cannot find PremiereMonthLong"), Log.LogEntryType.Warning);
                                break;

                            case "premiereday": // %ad% - premiere Air Date
                                if (metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.SeriesPremiereDate.ToLocalTime().Day.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                    jobLog.WriteEntry(("Cannot find PremiereDay"), Log.LogEntryType.Warning);
                                break;

                            case "premieredayshort": // %ad% - premiere Air Day of week abbreviation
                                if (metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.SeriesPremiereDate.ToLocalTime().ToString("ddd");  // need to keep it culture sensitive here
                                else
                                    jobLog.WriteEntry(("Cannot find PremiereDayShort"), Log.LogEntryType.Warning);
                                break;

                            case "premieredaylong": // %ad% - premiere Air Day of week full name
                                if (metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.SeriesPremiereDate.ToLocalTime().ToString("dddd");  // need to keep it culture sensitive here
                                else
                                    jobLog.WriteEntry(("Cannot find PremiereDayLong"), Log.LogEntryType.Warning);
                                break;
                            
                            case "airyear": // %ad% - original Air Year
                                if (metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.OriginalBroadcastDateTime.ToLocalTime().Year.ToString("0000", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                    jobLog.WriteEntry(("Cannot find AirYear"), Log.LogEntryType.Warning);
                                break;

                            case "airmonth": // %ad% - original Air Month
                                if (metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.OriginalBroadcastDateTime.ToLocalTime().Month.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                    jobLog.WriteEntry(("Cannot find AirMonth"), Log.LogEntryType.Warning);
                                break;

                            case "airmonthshort": // %ad% - original Air Month abbreviation
                                if (metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("MMM"); // need to keep it culture sensitive here
                                else
                                    jobLog.WriteEntry(("Cannot find AirMonthShort"), Log.LogEntryType.Warning);
                                break;

                            case "airmonthlong": // %ad% - original Air Month full name
                                if (metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("MMMM"); // need to keep it culture sensitive here
                                else
                                    jobLog.WriteEntry(("Cannot find AirMonthLong"), Log.LogEntryType.Warning);
                                break;

                            case "airday": // %ad% - original Air Date
                                if (metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.OriginalBroadcastDateTime.ToLocalTime().Day.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                    jobLog.WriteEntry(("Cannot find AirDay"), Log.LogEntryType.Warning);
                                break;

                            case "airdayshort": // %ad% - original Air Day of week abbreviation
                                if (metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("ddd");  // need to keep it culture sensitive here
                                else
                                    jobLog.WriteEntry(("Cannot find AirDayShort"), Log.LogEntryType.Warning);
                                break;

                            case "airdaylong": // %ad% - original Air Day of week full name
                                if (metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("dddd");  // need to keep it culture sensitive here
                                else
                                    jobLog.WriteEntry(("Cannot find AirDayLong"), Log.LogEntryType.Warning);
                                break;

                            case "airhour": // %ad% - original Air Hour
                                if (metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.OriginalBroadcastDateTime.ToLocalTime().Hour.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                    jobLog.WriteEntry("Cannot find AirHour", Log.LogEntryType.Warning);
                                break;

                            case "airhourampm": // %ad% - original Air Hour AM/PM
                                if (metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("tt");
                                else
                                    jobLog.WriteEntry("Cannot find AirHourAMPM", Log.LogEntryType.Warning);
                                break;

                            case "airminute": // %ad% - original Air Minute
                                if (metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.OriginalBroadcastDateTime.ToLocalTime().Minute.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                    jobLog.WriteEntry("Cannot find AirMinute", Log.LogEntryType.Warning);
                                break;

                            case "recordyear": // %rd% - Record Year
                                if (metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.RecordedDateTime.ToLocalTime().Year.ToString("0000", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                {
                                    jobLog.WriteEntry(("Cannot find RecordYear using File Creation Date"), Log.LogEntryType.Warning);
                                    DateTime dt = Util.FileIO.GetFileCreationTime(sourceVideo);
                                    if (dt > GlobalDefs.NO_BROADCAST_TIME)
                                        newFileName += dt.Year.ToString("0000", System.Globalization.CultureInfo.InvariantCulture);
                                }
                                break;

                            case "recordmonth": // %rd% - Record Month
                                if (metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.RecordedDateTime.ToLocalTime().Month.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                {
                                    jobLog.WriteEntry(("Cannot find RecordMonth using File Creation Date"), Log.LogEntryType.Warning);
                                    DateTime dt = Util.FileIO.GetFileCreationTime(sourceVideo);
                                    if (dt > GlobalDefs.NO_BROADCAST_TIME)
                                        newFileName += dt.Month.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                }
                                break;

                            case "recordmonthshort": // %rd% - Record Month abbreviation
                                if (metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.RecordedDateTime.ToLocalTime().ToString("MMM"); // Need to keep it culture sensitive
                                else
                                {
                                    jobLog.WriteEntry(("Cannot find RecordMonthShort using File Creation Date"), Log.LogEntryType.Warning);
                                    DateTime dt = Util.FileIO.GetFileCreationTime(sourceVideo);
                                    if (dt > GlobalDefs.NO_BROADCAST_TIME)
                                        newFileName += dt.ToString("MMM");
                                }
                                break;

                            case "recordmonthlong": // %rd% - Record Month full name
                                if (metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.RecordedDateTime.ToLocalTime().ToString("MMMM"); // Need to keep it culture sensitive
                                else
                                {
                                    jobLog.WriteEntry(("Cannot find RecordMonthLong using File Creation Date"), Log.LogEntryType.Warning);
                                    DateTime dt = Util.FileIO.GetFileCreationTime(sourceVideo);
                                    if (dt > GlobalDefs.NO_BROADCAST_TIME)
                                        newFileName += dt.ToString("MMMM");
                                }
                                break;

                            case "recordday": // %rd% - Record Day
                                if (metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.RecordedDateTime.ToLocalTime().Day.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                {
                                    jobLog.WriteEntry(("Cannot find RecordDay using File Creation Date"), Log.LogEntryType.Warning);
                                    DateTime dt = Util.FileIO.GetFileCreationTime(sourceVideo);
                                    if (dt > GlobalDefs.NO_BROADCAST_TIME)
                                        newFileName += dt.Day.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                }
                                break;

                            case "recorddayshort": // %rd% - Record Day of week abbreviation
                                if (metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.RecordedDateTime.ToLocalTime().ToString("ddd"); // Keep it culture sensitive
                                else
                                {
                                    jobLog.WriteEntry(("Cannot find RecordDayShort using File Creation Date"), Log.LogEntryType.Warning);
                                    DateTime dt = Util.FileIO.GetFileCreationTime(sourceVideo);
                                    if (dt > GlobalDefs.NO_BROADCAST_TIME)
                                        newFileName += dt.ToString("ddd");
                                }
                                break;

                            case "recorddaylong": // %rd% - Record Day of week full name
                                if (metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.RecordedDateTime.ToLocalTime().ToString("dddd"); // Keep it culture sensitive
                                else
                                {
                                    jobLog.WriteEntry(("Cannot find RecordDayLong using File Creation Date"), Log.LogEntryType.Warning);
                                    DateTime dt = Util.FileIO.GetFileCreationTime(sourceVideo);
                                    if (dt > GlobalDefs.NO_BROADCAST_TIME)
                                        newFileName += dt.ToString("dddd");
                                }
                                break;

                            case "recordhour": // Record Hour
                                if (metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.RecordedDateTime.ToLocalTime().Hour.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                {
                                    jobLog.WriteEntry(("Cannot find RecordHour using File Creation Date"), Log.LogEntryType.Warning);
                                    DateTime dt = Util.FileIO.GetFileCreationTime(sourceVideo);
                                    if (dt > GlobalDefs.NO_BROADCAST_TIME)
                                        newFileName += dt.Hour.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                }
                                break;

                            case "recordhourampm": // Record Hour AM/PM
                                if (metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.RecordedDateTime.ToLocalTime().ToString("tt");
                                else
                                {
                                    jobLog.WriteEntry(("Cannot find RecordHourAMPM using File Creation Date"), Log.LogEntryType.Warning);
                                    DateTime dt = Util.FileIO.GetFileCreationTime(sourceVideo);
                                    if (dt > GlobalDefs.NO_BROADCAST_TIME)
                                        newFileName += dt.ToString("tt");
                                }
                                break;

                            case "recordminute": // Record minute
                                if (metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                    newFileName += metaData.RecordedDateTime.ToLocalTime().Minute.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                else
                                {
                                    jobLog.WriteEntry(("Cannot find RecordMinute using File Creation Date"), Log.LogEntryType.Warning);
                                    DateTime dt = Util.FileIO.GetFileCreationTime(sourceVideo);
                                    if (dt > GlobalDefs.NO_BROADCAST_TIME)
                                        newFileName += dt.Minute.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                                }
                                break;

                            case "originalfilename": // Name of the source file without the extension or path
                                newFileName += Path.GetFileNameWithoutExtension(sourceVideo);
                                break;

                            case "originalext": // Extension of the source file
                                newFileName += FilePaths.CleanExt(sourceVideo).Replace(".", "");
                                break;

                            case "showname": // %sn% - Showname / title
                                newFileName += metaData.Title;
                                break;

                            case "episodename": // %en% - episode name / subtitle
                                if (!String.IsNullOrEmpty(metaData.SubTitle))
                                    newFileName += metaData.SubTitle;
                                else
                                    jobLog.WriteEntry(("Cannot find Episode Name"), Log.LogEntryType.Warning);
                                break;

                            case "network": // %en% - recorded channel name
                                if (!String.IsNullOrEmpty(metaData.Network))
                                    newFileName += metaData.Network;
                                else
                                    jobLog.WriteEntry(("Cannot find Network Channel Name"), Log.LogEntryType.Warning);
                                break;

                            case "season": // %ss%### - season no
                                format = "";
                                try
                                {
                                    if (renameBytes[i + 1] == '#')
                                    {
                                        while (renameBytes[++i] == '#')
                                            format += "0";

                                        --i; // adjust for last increment
                                    }
                                }
                                catch { } // this is normal incase it doesn't exist

                                if (metaData.Season != 0)
                                    newFileName += metaData.Season.ToString(format);
                                else
                                    jobLog.WriteEntry(("Cannot find Season No"), Log.LogEntryType.Warning);
                                break;

                            case "episode": // %ee%### - episode no
                                format = "";
                                try
                                {
                                    if (renameBytes[i + 1] == '#')
                                    {
                                        while (renameBytes[++i] == '#')
                                            format += "0";

                                        --i; // adjust for last increment
                                    }
                                }
                                catch { } // this is normal incase it doesn't exist

                                if (metaData.Episode != 0)
                                    newFileName += metaData.Episode.ToString(format);
                                else
                                    jobLog.WriteEntry(("Cannot find Episode No"), Log.LogEntryType.Warning);
                                break;

                            case "ismovie": // Special condition allowing for separate renaming if it's a movie or not
                                // FORMAT: %ismovie%<RenamePatternIfTrue,RenamePatternIfFalse>
                                {
                                    string truePattern = "", falsePattern = "";
                                    int nestedCount = 0; // number of < found for nested %ismovie%<Movie,%issport%<Sport,TV>,TV> type patterns
                                    while (renameBytes[++i] != '<') ; // Skip until you get a <
                                    while (renameBytes[++i] != ',') // Is it's a movie, rename pattern
                                        truePattern += renameBytes[i].ToString(CultureInfo.InvariantCulture);
                                    while (renameBytes[++i] != '>' || (nestedCount > 0)) // Is it's NOT a movie, rename pattern
                                    {
                                        if (renameBytes[i].ToString(CultureInfo.InvariantCulture) == "<")
                                            nestedCount++;
                                        if (renameBytes[i].ToString(CultureInfo.InvariantCulture) == ">") // compensate for the nested pattern
                                            nestedCount--;
                                        falsePattern += renameBytes[i].ToString(CultureInfo.InvariantCulture);
                                    }

                                    // Now parse the rename pattern and we're done
                                    if (metaData.IsMovie)
                                        CustomRenameFilename(truePattern, ref newFileName, ref destinationPath, sourceVideo, metaData, jobLog);
                                    else
                                        CustomRenameFilename(falsePattern, ref newFileName, ref destinationPath, sourceVideo, metaData, jobLog);
                                }
                                break; // We're done

                            case "issport": // Special condition allowing for separate renaming if it's a sports show or not
                                // FORMAT: %issports%<RenamePatternIfTrue,RenamePatternIfFalse>
                                {
                                    string truePattern = "", falsePattern = "";
                                    int nestedCount = 0; // number of < found for nested %issport%<Sport,%ismovie%<Movie,TV>,TV> type patterns
                                    while (renameBytes[++i] != '<') ; // Skip until you get a <
                                    while (renameBytes[++i] != ',') // Is it's a sports show, rename pattern
                                        truePattern += renameBytes[i].ToString(CultureInfo.InvariantCulture);
                                    while (renameBytes[++i] != '>' || (nestedCount > 0)) // Is it's NOT a sports show, rename pattern
                                    {
                                        if (renameBytes[i].ToString(CultureInfo.InvariantCulture) == "<")
                                            nestedCount++;
                                        if (renameBytes[i].ToString(CultureInfo.InvariantCulture) == ">") // compensate for the nested pattern
                                            nestedCount--;
                                        falsePattern += renameBytes[i].ToString(CultureInfo.InvariantCulture);
                                    }

                                    // Now parse the rename pattern and we're done
                                    if (metaData.IsSports)
                                        CustomRenameFilename(truePattern, ref newFileName, ref destinationPath, sourceVideo, metaData, jobLog);
                                    else
                                        CustomRenameFilename(falsePattern, ref newFileName, ref destinationPath, sourceVideo, metaData, jobLog);
                                }
                                break; // We're done

                            case "rating": // Parental rating
                                if (!String.IsNullOrEmpty(metaData.Rating))
                                    newFileName += metaData.Rating;
                                else
                                    jobLog.WriteEntry(("Cannot find Parental Rating"), Log.LogEntryType.Warning);
                                break;

                            case "airingdbid": // SageTV Airing DbId
                                if (!String.IsNullOrEmpty(metaData.sageTV.airingDbId))
                                    newFileName += metaData.sageTV.airingDbId;
                                else
                                    jobLog.WriteEntry(("Cannot find SageTV Airing DbId"), Log.LogEntryType.Warning);
                                break;

                            case "mediafiledbid": // SageTV MediaFile DbId
                                if (!String.IsNullOrEmpty(metaData.sageTV.mediaFileDbId))
                                    newFileName += metaData.sageTV.mediaFileDbId;
                                else
                                    jobLog.WriteEntry(("Cannot find SageTV MediaFile DbId"), Log.LogEntryType.Warning);
                                break;

                            default:
                                jobLog.WriteEntry(("Invalid file naming format detected, skipping") + " : " + command, Log.LogEntryType.Warning); // We had an invalid format
                                break;
                        }
                        break;

                    case '\\':
                        if (!string.IsNullOrWhiteSpace(destinationPath) && (destinationPath.Substring(destinationPath.Length - 1) != "\\")) // First directory should not start with a '\' and there should not be any consecutive '\'
                            destinationPath += "\\";
                        destinationPath += newFileName; // Don't check for illegal filepaths here, do it in the end
                        newFileName = ""; // reset the new filename
                        break;

                    default:
                        newFileName += renameBytes[i]; // Don't check now for illegal filenames since it could become a filepath, will check in the end
                        break;
                }
            }

            newFileName = Util.FilePaths.RemoveIllegalFilePathAndNameChars(newFileName); // Only accept characters which are not illegal in file paths and file names otherwise ignore
        }

        /// <summary>
        /// Generates a new name and path for a file using the metadata and options provided
        /// </summary>
        /// <param name="conversionOptions">Conversion job options</param>
        /// <param name="metaData">Metadata for video file</param>
        /// <param name="originalFileName">Full path and name for original video file</param>
        /// <param name="renamedFileExt">File extension for the renamed file</param>
        /// <param name="newFileName">Will contain the name of the renamed file if successful</param>
        /// <param name="subDestinationPath">Will contain the path of the renamed file if successful</param>
        /// <param name="jobLog">JobLog</param>
        /// <returns>True if rename was successful, false if there was no rename</returns>
        public static bool GetRenameByMetadata(ConversionJobOptions conversionOptions, VideoMetaData metaData, string originalFileName, string renamedFileExt, out string newFileName, out string subDestinationPath, Log jobLog)
        {
            newFileName = subDestinationPath = "";

            if (metaData != null)
            {
                if ((conversionOptions.renameBySeries) & (!String.IsNullOrEmpty(metaData.MetaData.Title)))
                {
                    string title = metaData.MetaData.Title;
                    string subTitle = metaData.MetaData.SubTitle;

                    //Get the date field
                    string date;
                    if (metaData.MetaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                    {
                        date = metaData.MetaData.RecordedDateTime.ToLocalTime().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        DateTime dt = Util.FileIO.GetFileCreationTime(originalFileName);

                        if (dt > GlobalDefs.NO_BROADCAST_TIME)
                        {
                            date = dt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            jobLog.WriteEntry("Cannot get recorded date and time, using current date and time", Log.LogEntryType.Warning);
                            date = DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }

                    // Build the new file name, check which naming convention we are using
                    if (!String.IsNullOrEmpty(conversionOptions.customRenameBySeries))
                    {
                        jobLog.WriteEntry("Custom Renaming Command -> " + conversionOptions.customRenameBySeries, Log.LogEntryType.Debug);
                        try
                        {
                            CustomRename.CustomRenameFilename(conversionOptions.customRenameBySeries, ref newFileName, ref subDestinationPath, originalFileName, metaData.MetaData, jobLog);

                            newFileName = newFileName.Replace(@"\\", @"\");
                            newFileName += renamedFileExt;
                        }
                        catch (Exception e)
                        {
                            jobLog.WriteEntry("Error in file naming format detected, fallback to default naming convention.\r\nError : " + e.ToString(), Log.LogEntryType.Warning); // We had an invalid format
                            newFileName = ""; // Reset since we had an error so fall back can work
                            subDestinationPath = ""; // Reset path for failure
                        }
                    }
                    else if (conversionOptions.altRenameBySeries) // Alternate renaming pattern
                    {
                        // ALTERNATE MC COMPATIBLE --> SHOWNAME/SEASON XX/SXXEYY-EPISODENAME.ext

                        if ((metaData.MetaData.Season > 0) && (metaData.MetaData.Episode > 0))
                        {
                            newFileName += "S" + metaData.MetaData.Season.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + "E" + metaData.MetaData.Episode.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                            if (subTitle != "")
                                newFileName += "-" + subTitle;
                        }
                        else
                        {
                            jobLog.WriteEntry("No Season and Episode information available, using show name", Log.LogEntryType.Warning); // if there is not season episode name available
                            newFileName = title;
                            if (subTitle != "")
                                newFileName += "-" + subTitle;
                            else
                                newFileName += "-" + date + " " + DateTime.Now.ToString("HH:MM", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        newFileName = newFileName.Replace(@"\\", @"\");
                        newFileName += renamedFileExt;

                        // Create the directory structure
                        subDestinationPath += metaData.MetaData.Title;
                        if ((metaData.MetaData.Season > 0))
                            subDestinationPath += "\\Season " + metaData.MetaData.Season.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                    }

                    if (newFileName == "") // this is our default/fallback option
                    {
                        // STANDARD --> SHOWNAME/SHOWNAME-SXXEYY-EPISODENAME<-RECORDDATE>.ext // Record date is used where there is no season and episode info

                        newFileName = title;

                        if ((metaData.MetaData.Season > 0) && (metaData.MetaData.Episode > 0))
                        {
                            newFileName += "-" + "S" + metaData.MetaData.Season.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + "E" + metaData.MetaData.Episode.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                            if (subTitle != "") newFileName += "-" + subTitle;
                        }
                        else
                        {
                            jobLog.WriteEntry("No Season and Episode information available, using episode name/record date", Log.LogEntryType.Warning); // if there is not season episode name available
                            if (subTitle != "")
                                newFileName += "-" + subTitle;
                            else
                                newFileName += "-" + date + " " + DateTime.Now.ToString("HH:MM", System.Globalization.CultureInfo.InvariantCulture); // Backup to create a unique name if season/episode is not available
                        }

                        newFileName = newFileName.Replace(@"\\", @"\");
                        newFileName += renamedFileExt;

                        // Create the directory structure
                        subDestinationPath += metaData.MetaData.Title;
                    }

                    subDestinationPath = Util.FilePaths.RemoveIllegalFilePathChars(subDestinationPath); // clean it up
                    newFileName = Util.FilePaths.RemoveIllegalFileNameChars(newFileName); // clean it up

                    return true; // We have a new name and path

                }
                else
                    jobLog.WriteEntry("Skipping Renaming by Series details", Log.LogEntryType.Information);
            }
            else
                jobLog.WriteEntry("Renaming by Series, no Metadata", Log.LogEntryType.Warning);

            return false; // No new name
        }
    }
}
