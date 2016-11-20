using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

using MCEBuddy.Globals;

namespace MCEBuddy.Util
{
    public static class UserCustomParams
    {
        /// <summary>
        /// Replaces a string of user custom parameters from the metadata
        /// </summary>
        /// <param name="commandParameters">Original string of user custom Parameters</param>
        /// <param name="workingPath">Temp working path</param>
        /// <param name="destinationPath">Destination path for converted file</param>
        /// <param name="convertedFile">Full path to final converted file</param>
        /// <param name="sourceFile">Full path to original source file</param>
        /// <param name="remuxFile">Full path to intermediate remuxed file</param>
        /// <param name="edlFile">Full path to EDL file</param>
        /// <param name="srtFile">Full path to SRT file</param>
        /// <param name="profile">Profile name</param>
        /// <param name="taskName">Task Name</param>
        /// <param name="metaData">Video metadata structure for source file</param>
        /// <param name="jobLog">JobLog</param>
        /// <returns>Converted string of custom parameters, empty string if there is an error</returns>
        public static string CustomParamsReplace(
          string commandParameters, 
          string workingPath, 
          string destinationPath, 
          string convertedFile, 
          string sourceFile, 
          string remuxFile, 
          string edlFile, 
          string srtFile, 
          string profile, 
          string taskName, 
          VideoTags metaData, 
          Log jobLog)
        {
            string translatedCommand = "";

            if (metaData == null)
                metaData = new VideoTags(); // Incase Metadata does not exit, create an empty metadata so it doesn't crash the function

            // SRT and EDl files are substitued if they exist otherwise they are ""
            if (!File.Exists(srtFile))
                srtFile = "";

            if (!File.Exists(edlFile))
                edlFile = "";

            try
            {
                char[] commandBytes = commandParameters.ToCharArray();
                for (int i = 0; i < commandBytes.Length; i++)
                {
                    switch (commandBytes[i])
                    {
                        case '%':
                            string command = "";
                            while (commandBytes[++i] != '%')
                                command += commandBytes[i].ToString(System.Globalization.CultureInfo.InvariantCulture).ToLower();

                            string format = "";
                            switch (command)
                            {
                                case "taskname":
                                    translatedCommand += (taskName); // Preserve case for parameters
                                    break;

                                case "profile":
                                    translatedCommand += (profile); // Preserve case for parameters
                                    break;

                                case "convertedfile":
                                    translatedCommand += (convertedFile); // Preserve case for parameters
                                    break;

                                case "sourcefile":
                                    translatedCommand += (sourceFile); // Preserve case for parameters
                                    break;

                                case "remuxfile":
                                    translatedCommand += (remuxFile); // Preserve case for parameters
                                    break;

                                case "workingpath":
                                    translatedCommand += (workingPath); // Preserve case for parameters
                                    break;

                                case "destinationpath":
                                    translatedCommand += (destinationPath); // Preserve case for parameters
                                    break;

                                case "srtfile":
                                    translatedCommand += (srtFile); // Preserve case for parameters
                                    break;

                                case "edlfile":
                                    translatedCommand += (edlFile); // Preserve case for parameters
                                    break;

                                case "originalfilepath":
                                    translatedCommand += (Path.GetDirectoryName(sourceFile)); // Preserve case for parameters
                                    break;

                                case "originalfilename":
                                    translatedCommand += (Path.GetFileNameWithoutExtension(sourceFile)); // Preserve case for parameters
                                    break;

                                case "originalext": // Extension of the source file
                                    translatedCommand += FilePaths.CleanExt(sourceFile).Replace(".", "");
                                    break;

                                case "convertedext": // Extension of the converted file
                                    translatedCommand += FilePaths.CleanExt(convertedFile).Replace(".", "");
                                    break;

                                case "showname":
                                    translatedCommand += (metaData.Title); // Preserve case for parameters
                                    break;

                                case "genre":
                                    translatedCommand += (metaData.Genres != null ? (metaData.Genres.Length > 0 ? metaData.Genres[0] : "") : ""); // Preserve case for parameters
                                    break;

                                case "episodename":
                                    translatedCommand += (metaData.SubTitle); // Preserve case for parameters
                                    break;

                                case "episodedescription":
                                    translatedCommand += (metaData.Description); // Preserve case for parameters
                                    break;

                                case "network":
                                    translatedCommand += (metaData.Network); // Preserve case for parameters
                                    break;

                                case "bannerfile":
                                    translatedCommand += (metaData.BannerFile); // Preserve case for parameters
                                    break;

                                case "bannerurl":
                                    translatedCommand += (metaData.BannerURL); // Preserve case for parameters
                                    break;

                                case "movieid":
                                    translatedCommand += (metaData.tmdbId); // Preserve case for parameters
                                    break;

                                case "imdbmovieid":
                                    translatedCommand += (metaData.imdbId); // Preserve case for parameters
                                    break;

                                case "seriesid":
                                    translatedCommand += (metaData.tvdbId); // Preserve case for parameters
                                    break;

                                case "season":
                                    format = "";
                                    try
                                    {
                                        if (commandBytes[i + 1] == '#')
                                        {
                                            while (commandBytes[++i] == '#')
                                                format += "0";

                                            --i; // adjust for last increment
                                        }
                                    }
                                    catch { } // this is normal incase it doesn't exist

                                    translatedCommand += ((metaData.Season == 0 ? "" : metaData.Season.ToString(format))); // Preserve case for parameters
                                    break;

                                case "episode":
                                    format = "";
                                    try
                                    {
                                        if (commandBytes[i + 1] == '#')
                                        {
                                            while (commandBytes[++i] == '#')
                                                format += "0";

                                            --i; // adjust for last increment
                                        }
                                    }
                                    catch { } // this is normal incase it doesn't exist

                                    translatedCommand += ((metaData.Episode == 0 ? "" : metaData.Episode.ToString(format))); // Preserve case for parameters
                                    break;

                                case "issport":
                                    translatedCommand += (metaData.IsSports.ToString(CultureInfo.InvariantCulture)); // Preserve case for parameters
                                    break;

                                case "ismovie":
                                    translatedCommand += (metaData.IsMovie.ToString(CultureInfo.InvariantCulture)); // Preserve case for parameters
                                    break;

                                case "premiereyear":
                                    translatedCommand += ((metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME) ? metaData.SeriesPremiereDate.ToLocalTime().ToString("yyyy") : ""); // Preserve case for parameters
                                    break;

                                case "premieremonth":
                                    translatedCommand += ((metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME) ? metaData.SeriesPremiereDate.ToLocalTime().ToString("%M") : ""); // Preserve case for parameters
                                    break;

                                case "premieremonthshort":
                                    translatedCommand += ((metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME) ? metaData.SeriesPremiereDate.ToLocalTime().ToString("MMM") : ""); // Preserve case for parameters, culture sensitive
                                    break;

                                case "premieremonthlong":
                                    translatedCommand += ((metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME) ? metaData.SeriesPremiereDate.ToLocalTime().ToString("MMMM") : ""); // Preserve case for parameters, culture sensitive
                                    break;

                                case "premiereday":
                                    translatedCommand += ((metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME) ? metaData.SeriesPremiereDate.ToLocalTime().ToString("%d") : ""); // Preserve case for parameters
                                    break;

                                case "premieredayshort":
                                    translatedCommand += ((metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME) ? metaData.SeriesPremiereDate.ToLocalTime().ToString("ddd") : ""); // Preserve case for parameters, culture sensitive
                                    break;

                                case "premieredaylong":
                                    translatedCommand += ((metaData.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME) ? metaData.SeriesPremiereDate.ToLocalTime().ToString("dddd") : ""); // Preserve case for parameters, culture sensitive
                                    break;
                                
                                case "airyear":
                                    translatedCommand += ((metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("yyyy") : ""); // Preserve case for parameters
                                    break;

                                case "airmonth":
                                    translatedCommand += ((metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("%M") : ""); // Preserve case for parameters
                                    break;

                                case "airmonthshort":
                                    translatedCommand += ((metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("MMM") : ""); // Preserve case for parameters, culture sensitive
                                    break;

                                case "airmonthlong":
                                    translatedCommand += ((metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("MMMM") : ""); // Preserve case for parameters, culture sensitive
                                    break;

                                case "airday":
                                    translatedCommand += ((metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("%d") : ""); // Preserve case for parameters
                                    break;

                                case "airdayshort":
                                    translatedCommand += ((metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("ddd") : ""); // Preserve case for parameters, culture sensitive
                                    break;

                                case "airdaylong":
                                    translatedCommand += ((metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("dddd") : ""); // Preserve case for parameters, culture sensitive
                                    break;

                                case "airhour":
                                    translatedCommand += ((metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("%h") : ""); // Preserve case for parameters
                                    break;

                                case "airhourampm":
                                    translatedCommand += ((metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("tt") : ""); // Preserve case for parameters
                                    break;

                                case "airminute":
                                    translatedCommand += ((metaData.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.OriginalBroadcastDateTime.ToLocalTime().ToString("%m") : ""); // Preserve case for parameters
                                    break;

                                case "recordyear":
                                    translatedCommand += ((metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.RecordedDateTime.ToLocalTime().ToString("yyyy") : Util.FileIO.GetFileCreationTime(sourceFile).ToString("yyyy")); // Preserve case for parameters
                                    break;

                                case "recordmonth":
                                    translatedCommand += ((metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.RecordedDateTime.ToLocalTime().ToString("%M") : Util.FileIO.GetFileCreationTime(sourceFile).ToString("%M")); // Preserve case for parameters
                                    break;

                                case "recordmonthshort":
                                    translatedCommand += ((metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.RecordedDateTime.ToLocalTime().ToString("MMM") : Util.FileIO.GetFileCreationTime(sourceFile).ToString("MMM")); // Preserve case for parameters, culture sensitive
                                    break;

                                case "recordmonthlong":
                                    translatedCommand += ((metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.RecordedDateTime.ToLocalTime().ToString("MMMM") : Util.FileIO.GetFileCreationTime(sourceFile).ToString("MMMM")); // Preserve case for parameters, culture sensitive
                                    break;

                                case "recordday":
                                    translatedCommand += ((metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.RecordedDateTime.ToLocalTime().ToString("%d") : Util.FileIO.GetFileCreationTime(sourceFile).ToString("%d")); // Preserve case for parameters
                                    break;

                                case "recorddayshort":
                                    translatedCommand += ((metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.RecordedDateTime.ToLocalTime().ToString("ddd") : Util.FileIO.GetFileCreationTime(sourceFile).ToString("ddd")); // Preserve case for parameters, culture sensitive
                                    break;

                                case "recorddaylong":
                                    translatedCommand += ((metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.RecordedDateTime.ToLocalTime().ToString("dddd") : Util.FileIO.GetFileCreationTime(sourceFile).ToString("dddd")); // Preserve case for parameters, culture sensitive
                                    break;

                                case "recordhour":
                                    translatedCommand += ((metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.RecordedDateTime.ToLocalTime().ToString("%h") : Util.FileIO.GetFileCreationTime(sourceFile).ToString("%h")); // Preserve case for parameters
                                    break;

                                case "recordhourampm":
                                    translatedCommand += ((metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.RecordedDateTime.ToLocalTime().ToString("tt") : Util.FileIO.GetFileCreationTime(sourceFile).ToString("tt")); // Preserve case for parameters
                                    break;

                                case "recordminute":
                                    translatedCommand += ((metaData.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME) ? metaData.RecordedDateTime.ToLocalTime().ToString("%m") : Util.FileIO.GetFileCreationTime(sourceFile).ToString("%m")); // Preserve case for parameters
                                    break;

                                case "rating": // Parental rating
                                    translatedCommand += metaData.Rating;
                                    break;

                                case "airingdbid": // SageTV Airing DbId
                                    translatedCommand += metaData.sageTV.airingDbId;
                                    break;

                                case "mediafiledbid": // SageTV MediaFile DbId
                                    translatedCommand += metaData.sageTV.mediaFileDbId;
                                    break;

                                default:
                                    jobLog.WriteEntry(Localise.GetPhrase("Invalid custom command format detected, skipping") + " : " + command, Log.LogEntryType.Warning); // We had an invalid format
                                    break;
                            }
                            break;

                        default:
                            translatedCommand += commandBytes[i];
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                jobLog.WriteEntry("Invalid custom params replace. Error " + e.ToString(), Log.LogEntryType.Warning);
                return ""; // return nothing
            }

            return translatedCommand;
        }
    }
}
