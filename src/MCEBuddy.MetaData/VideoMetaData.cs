using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.XPath;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Globalization;

using WMPLib;

using MCEBuddy.AppWrapper;
using MCEBuddy.Globals;
using MCEBuddy.VideoProperties;
using MCEBuddy.Util;
using MCEBuddy.Configuration;
using MCEBuddy.EMailEngine;

namespace MCEBuddy.MetaData
{
    public class VideoMetaData
    {
        #region Structures
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WMPicture
        {
            public IntPtr pwszMIMEType;
            public byte bPictureType;
            public IntPtr pwszDescription;
            [MarshalAs(UnmanagedType.U4)]
            public int dwDataLen;
            public IntPtr pbData;
        }
        #endregion

        #region Variables
        private string _videoFileName;
        private VideoTags _videoTags = new VideoTags();
        private bool _downloadSeriesDetails = true;
        private bool _downloadBannerFile = true;
        private string _tivoMAKKey = "";
        private string _profile = "";
        private string _taskName = "";
        private ShowType _forceShowType = ShowType.Default;
        private ConversionJobOptions.MetadataCorrectionOptions[] _metadataCorrections = null;
        private bool _prioritizeMatchDate = false;
        private bool _ignoreSuspend = false;
        private bool _titleCorrected = false;

        private JobStatus _jobStatus;
        private Log _jobLog;
        #endregion

        public VideoTags MetaData
        { get { return _videoTags; } }

        /// <summary>
        /// Extract the metadata from the video file (WTV/DVRMS/MP4/MKV/AVI/TS XML) and supplement with information downloaded from TVDB and MovieDB
        /// </summary>
        /// <param name="cjo">Conversion job options</param>
        /// <param name="disableDownloadSeriesDetails">(Optional) True if you want to override adn disable the DownloadSeriesDetails option from TVDB/MovieDB</param>
        public VideoMetaData(ConversionJobOptions cjo, JobStatus jobStatus, Log jobLog, bool disableDownloadSeriesDetails = false)
        {
            _videoFileName = cjo.sourceVideo;
            _downloadSeriesDetails = (cjo.downloadSeriesDetails && !disableDownloadSeriesDetails); // special case, if want to override it
            _downloadBannerFile = (cjo.downloadBanner && !disableDownloadSeriesDetails);
            _jobStatus = jobStatus;
            _jobLog = jobLog;
            _metadataCorrections = cjo.metadataCorrections;
            _tivoMAKKey = cjo.tivoMAKKey;
            _profile = cjo.profile;
            _taskName = cjo.taskName;
            _forceShowType = cjo.forceShowType;
            _prioritizeMatchDate = cjo.prioritizeOriginalBroadcastDateMatch;
        }

        /// <summary>
        /// Extract metadata from the file and downloading supplemental from the internet if required
        /// </summary>
        /// <param name="ignoreSuspend">(Optional) True if you're calling from a GUI or engine with lock where one should not suspend extraction to avoid a hang</param>
        public void Extract(bool ignoreSuspend = false)
        {
            _ignoreSuspend = ignoreSuspend;

            string ext = FilePaths.CleanExt(_videoFileName);
            
            _jobLog.WriteEntry(this, "Extracting metadata from file -> " + _videoFileName, Log.LogEntryType.Debug);

            if ((ext == ".wtv") || (ext == ".dvr-ms"))
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Extracting MCE Tags"), Log.LogEntryType.Information);
                ExtractMCETags();
            }
            else
            {
                FileExtractMetadata extractMetadata = new FileExtractMetadata(_videoFileName, _videoTags, _tivoMAKKey, _ignoreSuspend, _jobStatus, _jobLog);
                if (!extractMetadata.ExtractXMLMetadata())
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Extracting Generic Tags"), Log.LogEntryType.Information);
                    ExtractGenericTags();
                }
            }

            // If we don't have any Title use the filename for the Title for now
            string baseFileName = Path.GetFileNameWithoutExtension(_videoFileName);
            bool titleFromFilename = false;
            if (String.IsNullOrWhiteSpace(_videoTags.Title))
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("No recording meta data can be extracted, using Title extracted from file name. If you are running MCEBuddy on a Windows Server, this is normal as the Media Center filters are not available on that platfom."), Log.LogEntryType.Warning);
                _videoTags.Title = baseFileName; // Default title is name of file
                titleFromFilename = true; // We took the title from the filename, update later if required
            }

            // EXTRACT METADATA FROM FILENAME
            // NextPVR and some software (including custom users for WTV) use the following format -> SHOWNAME_AIRDATE_AIRTIME.<ext> -> AIRDATE (optional) - YYYYMMDD, AIRTIME (optional) - HHMMHHMM (Start-HHMM, End-HHMM)
            // OR some users to download exact data may decide to name the file as -> MOVIE_IMDBID.<ext> or SHOWNAME_IMDBID.<ext> -> IMDBID is the imdb id for the Movie or Episode (Showname taken from title)
            if (_videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME) // if we don't have a Original Broadcast Date and time already extracted
            {
                int pos2ndPart = baseFileName.IndexOf("_"); // Look for AIRDATE position
                if (pos2ndPart > 0 && baseFileName.Length > pos2ndPart) // Title should not end with a _
                {
                    string showName = baseFileName.Substring(0, pos2ndPart); // Extract SHOWNAME without _

                    int pos3rdPart = baseFileName.IndexOf("_", pos2ndPart + 1); // Look for AIRTIME position
                    if (pos3rdPart == -1) // sometime the AIRTIME may be missing
                        pos3rdPart = baseFileName.Length; // Try to parse to end
                    if ((pos3rdPart > 0) && (pos3rdPart > pos2ndPart))
                    {
                        string secondPart = baseFileName.Substring(pos2ndPart + 1, pos3rdPart - pos2ndPart - 1); // Extract AIRDATE (ignore AIRTIME, not required)
                        if (DateTime.TryParseExact(secondPart, "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out _videoTags.OriginalBroadcastDateTime)) // AIRDATE - Assume it's reported in local date/time to avoid messing up the comparison
                        {
                            if (titleFromFilename)
                                if (!String.IsNullOrWhiteSpace(showName)) // If we have an updated showname otherwise stick to original title
                                    _videoTags.Title = showName; // Save SHOWNAME if we are able to parse the AIRDATE ONLY if we took the title from the filename
                            
                            _jobLog.WriteEntry(this, "Extracted Original Broadcast Date from Filename -> " + _videoTags.OriginalBroadcastDateTime.ToString("yyyy-MM-dd"), Log.LogEntryType.Debug);
                        }
                        else if (secondPart.ToLower().StartsWith("tt") && (secondPart.Length == 9)) // Check if we have a IMDB Id (tt0000001-tt9999999)
                        {
                            if (titleFromFilename)
                                if (!String.IsNullOrWhiteSpace(showName)) // If we have an updated showname otherwise stick to original title
                                    _videoTags.Title = showName; // Save SHOWNAME if we are able to parse the AIRDATE ONLY if we took the title from the filename

                            _videoTags.imdbId = secondPart; // save the imdb id
                            _jobLog.WriteEntry(this, "Extracted IMDB ID from Filename -> " + _videoTags.imdbId, Log.LogEntryType.Debug);
                        }
                    }
                }
            }

            if (titleFromFilename)
                _jobLog.WriteEntry(this, "Extracted Showname from Title -> " + _videoTags.Title, Log.LogEntryType.Debug);

            _jobLog.WriteEntry(this, "Video Tags extracted -> \r\n" + _videoTags.ToString(), Log.LogEntryType.Debug);

            _jobLog.WriteEntry(this, "Checking for metadata title and series correction", Log.LogEntryType.Debug);

            // Correct title and set series id if required
            try
            {
                if (_metadataCorrections != null)
                {
                    // First check if have a title match specific to this recording
                    if ((_metadataCorrections.Length > 1) || (!String.IsNullOrWhiteSpace(_metadataCorrections[0].originalTitle))) // If we have more than 1 pattern match or a Original filename in the first correction option, it's not universal
                    {
                        ConversionJobOptions.MetadataCorrectionOptions metaCorrection = null;
                        metaCorrection = _metadataCorrections.FirstOrDefault(x => Util.Text.WildcardRegexPatternMatch(_videoTags.Title, x.originalTitle));
                        if (metaCorrection != null) // We have a match
                        {
                            // Now we have a title match, check for a corrected title
                            if (!String.IsNullOrWhiteSpace(metaCorrection.correctedTitle))
                            {
                                _jobLog.WriteEntry(this, "More than one metadata correction options detected or default title matching detected, matched Original Title -> " + metaCorrection.originalTitle + ", Title correction -> " + metaCorrection.correctedTitle, Log.LogEntryType.Debug);
                                if (metaCorrection.correctedTitle.Contains("regex:"))
                                    _videoTags.Title = Regex.Replace(_videoTags.Title, metaCorrection.originalTitle.Replace("regex:", ""), metaCorrection.correctedTitle.Replace("regex:", ""), RegexOptions.IgnoreCase);
                                else
                                    _videoTags.Title = metaCorrection.correctedTitle;
                                _titleCorrected = true;
                            }
                            else // Otherwise we pick up the series id's
                            {
                                _jobLog.WriteEntry(this, "More than one metadata correction options detected or default title matching detected, matched Original Title -> " + metaCorrection.originalTitle + ", Series forcing TVDB -> " + metaCorrection.tvdbSeriesId + ", IMDB -> " + metaCorrection.imdbSeriesId, Log.LogEntryType.Debug);
                                _videoTags.tvdbId = metaCorrection.tvdbSeriesId;
                                _videoTags.imdbId = metaCorrection.imdbSeriesId;
                            }
                        }
                    }
                    else // Otherwise we assume all shows to be forced with the corrected title or the series id mentioned in the 1st element in the metadata correction array
                    {
                        // Check for a corrected title
                        if (!String.IsNullOrWhiteSpace(_metadataCorrections[0].correctedTitle))
                        {
                            _jobLog.WriteEntry(this, "One metadata correction option detected, matched Original Title -> " + _metadataCorrections[0].originalTitle + ", Title correction -> " + _metadataCorrections[0].correctedTitle, Log.LogEntryType.Debug);
                            if (_metadataCorrections[0].correctedTitle.Contains("regex:"))
                                _videoTags.Title = Regex.Replace(_videoTags.Title, _metadataCorrections[0].originalTitle.Replace("regex:", ""), _metadataCorrections[0].correctedTitle.Replace("regex:", ""), RegexOptions.IgnoreCase);
                            else
                                _videoTags.Title = _metadataCorrections[0].correctedTitle;
                            _titleCorrected = true;
                        }
                        else // Otherwise we pick up the series id's
                        {
                            _jobLog.WriteEntry(this, "One metadata correction option detected, matched Original Title -> " + _metadataCorrections[0].originalTitle + ", Series forcing TVDB -> " + _metadataCorrections[0].tvdbSeriesId + ", IMDB -> " + _metadataCorrections[0].imdbSeriesId, Log.LogEntryType.Debug);
                            _videoTags.tvdbId = _metadataCorrections[0].tvdbSeriesId;
                            _videoTags.imdbId = _metadataCorrections[0].imdbSeriesId;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry("Failed to correct metadata title/series id. Error ->\r\n" + e.ToString(), Log.LogEntryType.Warning);
            }

            // After extracting tags BEFORE downloading metadata, check if the show type has been set
            if (_forceShowType != ShowType.Default)
                _jobLog.WriteEntry(this, "Forcing show type -> " + _forceShowType.ToString(), Log.LogEntryType.Debug);
            switch (_forceShowType)
            {
                case ShowType.Movie:
                    _videoTags.IsMovie = true; // force movie
                    _videoTags.IsSports = false;
                    break;

                case ShowType.Series:
                    _videoTags.IsMovie = false; // Force tv series
                    _videoTags.IsSports = false;
                    break;

                case ShowType.Sports:
                    _videoTags.IsSports = true; // Force sports
                    _videoTags.IsMovie = false;
                    break;

                case ShowType.Default:
                default: // Leave as is
                    break;
            }

            // Download the details from the internet, bootstrap it if we only have Title and IMDB ID
            DownloadSeriesDetails(titleFromFilename && !String.IsNullOrWhiteSpace(_videoTags.imdbId));

            _jobLog.WriteEntry(this, Localise.GetPhrase("Updated Video Tags after downloading details") + " -> \r\n" + _videoTags.ToString(), Log.LogEntryType.Debug);

            if (_videoTags.CopyProtected)
                _jobLog.WriteEntry(this, Localise.GetPhrase("ERROR: VIDEO IS COPYPROTECTED. CONVERSION WILL FAIL"), Log.LogEntryType.Warning);
        }

        /// <summary>
        /// Download metadata from TVDB and MovieDB including banners
        /// </summary>
        /// <param name="titleFromFilename">True to use IMDB ID to get basic information to bootstrap the rest of the information</param>
        private void DownloadSeriesDetails(bool imdbBootstrap)
        {
            string backupBanner = "";

            if ((_downloadSeriesDetails) && (!String.IsNullOrWhiteSpace(_videoTags.Title)))
            {
                _jobLog.WriteEntry(this, ("Downloading Series details"), Log.LogEntryType.Information);

                if (_downloadBannerFile)
                {
                    // If the extraction of metadata led to some banner files, then save them as a backup incase the internet downloading fails
                    if (File.Exists(_videoTags.BannerFile) && FileIO.FileSize(_videoTags.BannerFile) > 0)
                        backupBanner = _videoTags.BannerFile;
                    _videoTags.BannerFile = Path.Combine(GlobalDefs.CachePath, Util.FilePaths.RemoveIllegalFileNameChars(_videoTags.Title) + ".jpg");
                }
                else
                {
                    _jobLog.WriteEntry(this, ("Skipping downloading Banner file"), Log.LogEntryType.Information);
                    _videoTags.BannerFile = "";
                }

                bool downloadSuccess = false;

                // First check if we need to force download of data from IMDB for Movie or Episode (not show), it will automatically detect Movie or Series
                if (imdbBootstrap)
                {
                    _jobLog.WriteEntry(this, "Force downloading metadatata from OMDB for IMDB ID -> " + _videoTags.imdbId, Log.LogEntryType.Information);
                    if (IMDB.BootStrapByIMDBId(_videoTags, _jobLog))
                    {
                        downloadSuccess |= true;
                        _titleCorrected = true; // We have now a fixed title, don't update it
                    }
                    else
                        _jobLog.WriteEntry(this, ("OMDB failed"), Log.LogEntryType.Warning);
                }

                // Check them all in succession, each add more information to the previous
                if (_videoTags.IsMovie) // For movies we have different matching logic
                {
                    _jobLog.WriteEntry(this, "Recording Type Movie", Log.LogEntryType.Information);

                    if (!_jobStatus.Cancelled)
                    {
                        _jobLog.WriteEntry(this, "Checking TheMovieDB", Log.LogEntryType.Information);
                        if (TMDB.DownloadMovieDetails(_videoTags, _titleCorrected, _jobLog))
                            downloadSuccess |= true;
                        else
                            _jobLog.WriteEntry(this, ("TheMovieDB failed"), Log.LogEntryType.Warning);
                    }

                    if (!_jobStatus.Cancelled) // These are log jobs, incase the user presses cancelled, it should not fall through
                    {
                        // NOTE: MyApiFilm (IMDB API wrapper) has a limit of number of 2000 requests per day (per IP address)
                        _jobLog.WriteEntry(this, "Checking IMDB", Log.LogEntryType.Information);
                        if (IMDB.DownloadMovieDetails(_videoTags, _titleCorrected, _jobLog))
                            downloadSuccess |= true;
                        else
                            _jobLog.WriteEntry(this, ("IMDB failed"), Log.LogEntryType.Warning);
                    }
                }
                else
                {
                    _jobLog.WriteEntry(this, "Recording Type Show", Log.LogEntryType.Information);

                    if (!_jobStatus.Cancelled)
                    {
                        _jobLog.WriteEntry(this, Localise.GetPhrase("Checking TheTVDB"), Log.LogEntryType.Information);
                        if (TVDB.DownloadSeriesDetails(_videoTags, _prioritizeMatchDate, _titleCorrected, _jobLog))
                            downloadSuccess |= true;
                        else
                            _jobLog.WriteEntry(this, ("TheTVDB failed"), Log.LogEntryType.Warning);
                    }

                    // Supplement information
                    _jobLog.WriteEntry(this, "Checking TV.com", Log.LogEntryType.Information);
                    if (!_jobStatus.Cancelled)
                    {
                        if (TV.DownloadSeriesDetails(_videoTags, _prioritizeMatchDate, _titleCorrected, _jobLog))
                            downloadSuccess |= true;
                        else
                            _jobLog.WriteEntry(this, "TV.com failed", Log.LogEntryType.Warning);
                    }

                    // Supplement information
                    if (!_jobStatus.Cancelled)
                    {
                        // NOTE: MyApiFilm (IMDB API wrapper) has a limit of number of 2000 requests per day (per IP address)
                        _jobLog.WriteEntry(this, "Checking IMDB", Log.LogEntryType.Information);
                        if (IMDB.DownloadSeriesDetails(_videoTags, _prioritizeMatchDate, _titleCorrected, _jobLog))
                            downloadSuccess |= true;
                        else
                            _jobLog.WriteEntry(this, ("IMDBApi failed"), Log.LogEntryType.Warning);
                    }

                    // Supplement information
                    _jobLog.WriteEntry(this, "Checking TMDB.com", Log.LogEntryType.Information);
                    if (!_jobStatus.Cancelled)
                    {
                        if (TMDB.DownloadSeriesDetails(_videoTags, _prioritizeMatchDate, _titleCorrected, _jobLog))
                            downloadSuccess |= true;
                        else
                            _jobLog.WriteEntry(this, "TMDB.com failed", Log.LogEntryType.Warning);
                    }
                }

                if (!downloadSuccess)
                {   
                    // Send an eMail if required
                    if (MCEBuddyConf.GlobalMCEConfig.GeneralOptions.sendEmail && MCEBuddyConf.GlobalMCEConfig.GeneralOptions.eMailSettings.downloadFailedEvent)
                    {
                        bool skipBody = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.eMailSettings.skipBody;
                        string subject = Localise.GetPhrase("MCEBuddy unable to download series information");
                        string message = Localise.GetPhrase("Source Video") + " -> " + _videoFileName + "\r\n";
                        message += Localise.GetPhrase("Failed At") + " -> " + DateTime.Now.ToString("s", CultureInfo.InvariantCulture);
                        message += "\r\n" + _videoTags.ToString() + "\r\n";

                        // Check for custom subject and process
                        if (!String.IsNullOrWhiteSpace(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.eMailSettings.downloadFailedSubject))
                            subject = UserCustomParams.CustomParamsReplace(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.eMailSettings.downloadFailedSubject, "", "", "", _videoFileName, "", "", "", _profile, _taskName, _videoTags, Log.AppLog);

                        eMailSendEngine.AddEmailToSendQueue(subject, (skipBody ? "" : message)); // Send the eMail through the eMail engine
                    }
                }
            }
            else
                _jobLog.WriteEntry(this, Localise.GetPhrase("Skipping downloading Series details"), Log.LogEntryType.Information);

            if (!File.Exists(_videoTags.BannerFile) || (FileIO.FileSize(_videoTags.BannerFile) <= 0)) // Need to avoid an exception while writing tags
            {
                if (!File.Exists(backupBanner) || (FileIO.FileSize(backupBanner) <= 0)) // Check if we have a backup file available
                    _videoTags.BannerFile = "";
                else // Use the backup file extracted
                    _videoTags.BannerFile = backupBanner;
            }
        }

        /// <summary>
        /// Extract metadata from WTV or DVRMS file
        /// </summary>
        private void ExtractMCETags()
        {
            _jobLog.WriteEntry(this, Localise.GetPhrase("Extracting Windows Media Center meta data"),Log.LogEntryType.Debug);
            try
            {
                using (MetadataEditor editor = (MetadataEditor)new MCRecMetadataEditor(_videoFileName))
                {
                    IDictionary attrs = editor.GetAttributes();

                    _videoTags.Title = GetMetaTagStringValue(attrs, "Title");
                    _videoTags.SubTitle = GetMetaTagStringValue(attrs, "WM/SubTitle");
                    _videoTags.Description = GetMetaTagStringValue(attrs, "WM/SubTitleDescription");
                    _videoTags.Genres = GetMetaTagStringValue(attrs, "WM/Genre").Split(';');
                    _videoTags.Network = GetMetaTagStringValue(attrs, "WM/MediaStationName");
                    _videoTags.Rating = GetMetaTagStringValue(attrs, "WM/ParentalRating");
                    _videoTags.MediaCredits = GetMetaTagStringValue(attrs, "WM/MediaCredits");
                    _videoTags.CopyProtected = GetMetaTagBoolValue(attrs, "WM/WMRVContentProtected");
                    if (GetMetaTagStringValue(attrs, "WM/MediaOriginalBroadcastDateTime") != "")
                    {
                        // DateTime is reported along with timezone info (typically Z i.e. UTC hence assume None)
                        DateTime.TryParse(GetMetaTagStringValue(attrs, "WM/MediaOriginalBroadcastDateTime"), null, DateTimeStyles.None, out _videoTags.OriginalBroadcastDateTime);
                    }
                    if (GetMetaTagQWordValue(attrs, "WM/WMRVEncodeTime") != 0)
                    {
                        // Stored in UTC ticks hence assume Universal
                        DateTime.TryParse((new DateTime(GetMetaTagQWordValue(attrs, "WM/WMRVEncodeTime"))).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _videoTags.RecordedDateTime);
                    }
                    _videoTags.IsSports = GetMetaTagBoolValue(attrs, "WM/MediaIsSport");
                    _videoTags.IsMovie = GetMetaTagBoolValue(attrs, "WM/MediaIsMovie");

                    // Movies will typically not have a Original Broadcast Date and Time set, instead they have the OriginalReleaseTime (Year) set, so get that
                    if (_videoTags.IsMovie && (_videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME))
                    {
                        string movieYear = GetMetaTagStringValue(attrs, "WM/OriginalReleaseTime");
                        if (!String.IsNullOrWhiteSpace(movieYear)) // Use the movie year to set the original broadcast date as YYYY-05-05 (mid year to avoid timezone issues)
                        {
                            DateTime.TryParse(movieYear + "-05-05", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.OriginalBroadcastDateTime);
                            _jobLog.WriteEntry(this, "Original Broadcast DateTime missing, Original Movie Release Year detected -> " + movieYear, Log.LogEntryType.Debug);
                        }
                    }

                    if (!_videoTags.IsMovie && !_videoTags.IsSports)
                    {
                        // Sometimes for TV Shows the Subtitle field is empty and the subtitle description contains the subtitle, extract if possible. See ticket https://mcebuddy2x.codeplex.com/workitem/1910
                        // The format is -> EPISODE/TOTAL_EPISODES_IN_SEASON. SUBTITLE: DESCRIPTION
                        // OR -> COMMENT. SUBTITLE: DESCRIPTION
                        // e.g. -> 4/13. The Doctor's Wife: Science fiction drama. When he follows a Time Lord distress signal, the Doctor puts Amy, Rory and his beloved TARDIS in grave danger. Also in HD. [AD,S]
                        // e.g. -> CBeebies Bedtime Hour. The Mystery: Animated adventures of two friends who live on an island in the middle of the big city. Some of Abney and Teal's favourite objects are missing. [S]
                        if (String.IsNullOrWhiteSpace(_videoTags.SubTitle) && !String.IsNullOrWhiteSpace(_videoTags.Description) && _videoTags.Description.Substring(0, Math.Min(_videoTags.Description.Length, GlobalDefs.MAX_SUBTITLE_DESCRIPTION_EXTRACTION_LENGTH)).Contains(":")) // Check within the Subtitle size limit, otherwise from description it can get too long creating an invalid filename
                        {
                            string[] parts = _videoTags.Description.Split(':');
                            if (parts.Length > 0)
                            {
                                _jobLog.WriteEntry(this, "Extracting subtitle from description field", Log.LogEntryType.Debug);

                                string subtitle = parts[0];
                                try
                                {
                                    if (subtitle.Contains("/")) // It contains a episode number and season number
                                    {
                                        string[] numbers = subtitle.Split(' ');
                                        _videoTags.Episode = int.Parse(numbers[0].Replace(".", "").Split('/')[0]);
                                        int totalEpisodesInSeason = int.Parse(numbers[0].Replace(".", "").Split('/')[1]);

                                        _jobLog.WriteEntry(this, "Extracted Episode:" + _videoTags.Episode.ToString() + " from description field", Log.LogEntryType.Debug);

                                        _videoTags.SubTitle = String.Join(" ", numbers, 1, numbers.Length - 1).Trim(); // Skip the first, concatenate the rest, clean up spaces and save it
                                    }
                                    else
                                        throw new Exception(); // Switch to default parsing
                                }
                                catch // Default parsing
                                {
                                    if (subtitle.Contains(".")) // skip the comment, keep the subtitle
                                        _videoTags.SubTitle = String.Join(".", subtitle.Split('.'), 1, subtitle.Split('.').Length - 1).Trim(); // skip the first
                                    else
                                        _videoTags.SubTitle = subtitle.Trim(); // Clean up whitespaces and save it
                                }
                            }
                        }
                    }

                    // Get the thumbnail at the end
                    if (_downloadBannerFile)
                    {
                        try
                        {
                            byte[] picture = GetMetaTagPicture(attrs);
                            using (MemoryStream stream = new MemoryStream(picture))  //create an in memory stream
                            {
                                // Write to file as a backup
                                _videoTags.BannerFile = Path.Combine(GlobalDefs.CachePath, Util.FilePaths.RemoveIllegalFileNameChars(_videoTags.Title) + "-extract.jpg");
                                Bitmap artWork = new Bitmap(stream);
                                artWork.Save(_videoTags.BannerFile, System.Drawing.Imaging.ImageFormat.Jpeg); // Save as JPEG
                            }
                        }
                        catch (Exception e)
                        {
                            _jobLog.WriteEntry(this, "Error reading WM Artwork -> " + e.ToString(), Log.LogEntryType.Warning);
                            _videoTags.BannerFile = "";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Unable to extract meta data using filters.  Filters may not be present [eg. Windows Server].\nError : " + e.ToString()), Log.LogEntryType.Warning);
                if (FilePaths.CleanExt(_videoFileName) == ".dvr-ms")
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Attempting raw container DVR-MS read"), Log.LogEntryType.Information);
                    ExtractGenericTags();
                }
            }
        }

        /// <summary>
        /// Write the Tags / Metadata for a WTV/DVR-MS file. If the source is a WTV/DVR-MS file it copies all the metadata, else it copies individual components
        /// </summary>
        /// <param name="convertedFile">Path to WTV/DVR-MS file</param>
        private void WriteMCETags(string convertedFile)
        {
            _jobLog.WriteEntry(this, Localise.GetPhrase("Setting WTV/DVR-MS metadata"), Log.LogEntryType.Debug);

            try
            {
                IDictionary sourceAttrs;

                if ((Util.FilePaths.CleanExt(_videoFileName) == ".wtv") || (Util.FilePaths.CleanExt(_videoFileName) == ".dvr-ms")) // Copy all WTV/DVRMS Metadata
                {
                    using (MetadataEditor editor = (MetadataEditor)new MCRecMetadataEditor(_videoFileName)) // Get All Attributes from the Source file
                    {
                        sourceAttrs = editor.GetAttributes(); // Get All Attributes from the Source file

                        // Remove attributes that conflict
                        // Duration and all Thumbnail related attributes need to be removed as they are incorrect/not supported
                        sourceAttrs.Remove("Duration");
                        sourceAttrs = sourceAttrs.Keys.Cast<string>()
                            .Where(key => !key.ToLower().Contains("thumb"))
                            .ToDictionary(key => key, key => sourceAttrs[key]);
                    }
                }
                else // Try to copy as many attributes as possible if the source is not WTV or DVRMS
                {
                    sourceAttrs = new Hashtable();
                    if (!String.IsNullOrWhiteSpace(_videoTags.Title)) sourceAttrs.Add("Title", new MetadataItem("Title", _videoTags.Title, DirectShowLib.SBE.StreamBufferAttrDataType.String));
                    if (!String.IsNullOrWhiteSpace(_videoTags.SubTitle)) sourceAttrs.Add("WM/SubTitle", new MetadataItem("WM/SubTitle", _videoTags.SubTitle, DirectShowLib.SBE.StreamBufferAttrDataType.String));
                    if (!String.IsNullOrWhiteSpace(_videoTags.Description)) sourceAttrs.Add("WM/SubTitleDescription", new MetadataItem("WM/SubTitleDescription", _videoTags.Description, DirectShowLib.SBE.StreamBufferAttrDataType.String));
                    if (!String.IsNullOrWhiteSpace(_videoTags.Rating)) sourceAttrs.Add("WM/ParentalRating", new MetadataItem("WM/ParentalRating", _videoTags.Rating, DirectShowLib.SBE.StreamBufferAttrDataType.String));
                    if (_videoTags.Genres != null) if (_videoTags.Genres.Length > 0) sourceAttrs.Add("WM/Genre", new MetadataItem("WM/Genre", _videoTags.Genres[0], DirectShowLib.SBE.StreamBufferAttrDataType.String));
                    if (!String.IsNullOrWhiteSpace(_videoTags.Network)) sourceAttrs.Add("WM/MediaStationName", new MetadataItem("WM/MediaStationName", _videoTags.Network, DirectShowLib.SBE.StreamBufferAttrDataType.String));
                    if (_videoTags.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) sourceAttrs.Add("WM/MediaOriginalBroadcastDateTime", new MetadataItem("WM/MediaOriginalBroadcastDateTime", _videoTags.OriginalBroadcastDateTime.ToString("s") + "Z", DirectShowLib.SBE.StreamBufferAttrDataType.String)); // It is stored a UTC value, we just need to add "Z" at the end to indicate it
                    if (_videoTags.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME) sourceAttrs.Add("WM/WMRVEncodeTime", new MetadataItem("WM/WMRVEncodeTime", _videoTags.RecordedDateTime.Ticks, DirectShowLib.SBE.StreamBufferAttrDataType.QWord));
                    sourceAttrs.Add("WM/MediaIsSport", new MetadataItem("WM/MediaIsSport", _videoTags.IsSports, DirectShowLib.SBE.StreamBufferAttrDataType.Bool));
                    sourceAttrs.Add("WM/MediaIsMovie", new MetadataItem("WM/MediaIsMovie", _videoTags.IsMovie, DirectShowLib.SBE.StreamBufferAttrDataType.Bool));
                    if (_videoTags.IsMovie && (_videoTags.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME))
                        sourceAttrs.Add("WM/OriginalReleaseTime", new MetadataItem("WM/OriginalReleaseTime", _videoTags.OriginalBroadcastDateTime.Year.ToString(), DirectShowLib.SBE.StreamBufferAttrDataType.String));
                    /*if (File.Exists(_videoTags.BannerFile)) // We Don't support embedding Binary objects into WTV files yet
                    {
                        WMPicture pictureInfo = new WMPicture();
                        Image newImage = Bitmap.FromFile(_videoTags.BannerFile);
                        ImageConverter imageConverter = new ImageConverter();
                        pictureInfo.bPictureType = 6; // Type media picture
                        pictureInfo.pwszMIMEType = Marshal.StringToCoTaskMemUni("image/jpeg\0");
                        pictureInfo.pwszDescription = Marshal.StringToCoTaskMemUni("AlbumArt\0");
                        byte[] data = (byte[])imageConverter.ConvertTo(newImage, typeof(byte[]));
                        newImage.Dispose();
                        pictureInfo.dwDataLen = data.Length;
                        pictureInfo.pbData = Marshal.AllocCoTaskMem(pictureInfo.dwDataLen);
                        Marshal.Copy(data, 0, pictureInfo.pbData, pictureInfo.dwDataLen);
                        IntPtr pictureParam = Marshal.AllocCoTaskMem(Marshal.SizeOf(pictureInfo));
                        Marshal.StructureToPtr(pictureInfo, pictureParam, false);
                        byte[] picbytes = new byte[Marshal.SizeOf(pictureInfo)];
                        Marshal.Copy(pictureParam, picbytes, 0, Marshal.SizeOf(pictureInfo));
                        sourceAttrs.Add("WM/Picture", new MetadataItem("WM/Picture", picbytes, DirectShowLib.SBE.StreamBufferAttrDataType.Binary));
                        Marshal.FreeCoTaskMem(pictureParam);
                        Marshal.FreeCoTaskMem(pictureInfo.pbData);
                        Marshal.FreeCoTaskMem(pictureInfo.pwszDescription);
                        Marshal.FreeCoTaskMem(pictureInfo.pwszMIMEType);
                    }*/
                }

                using (MetadataEditor editor = (MetadataEditor)new MCRecMetadataEditor(convertedFile)) // Write All Attributes to the Converted file
                {
                    editor.SetAttributes(sourceAttrs); // Set All Attributes to the converted file
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Unable to set WTV meta data using filters.  Filters may not be present [eg. Windows Server].\nError : " + e.ToString()), Log.LogEntryType.Warning);
            }
        }

        #region MetaTagExtraction

        private byte[] GetMetaTagBinaryValue(IDictionary attrs, string Key)
        {
            if (attrs.Contains(Key))
            {
                MetadataItem item = (MetadataItem)attrs[Key];
                return (byte[])item.Value;
            }
            else
                return null;
        }

        private Byte[] GetMetaTagPicture(IDictionary attrs)
        {
            string Key = "WM/Picture";
            if (attrs.Contains(Key))
            {
                MetadataItem item = (MetadataItem)attrs[Key];

                // Pin the managed memory while, copy it out the data, then unpin it
                GCHandle handle = GCHandle.Alloc((byte[])item.Value, GCHandleType.Pinned);
                WMPicture pictureInfo = (WMPicture)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(WMPicture));
                handle.Free();
                byte[] picture = new byte[pictureInfo.dwDataLen];
                Marshal.Copy(pictureInfo.pbData, picture, 0, pictureInfo.dwDataLen);
                
                return picture;
            }
            else
                return null;
        }

        private Int64 GetMetaTagQWordValue(IDictionary attrs, string Key)
        {
            if (attrs.Contains(Key))
            {
                MetadataItem item = (MetadataItem)attrs[Key];
                return (Int64) item.Value;
            }
            else
                return 0;
        }

        private Int32 GetMetaTagDWordValue(IDictionary attrs, string Key)
        {
            if (attrs.Contains(Key))
            {
                MetadataItem item = (MetadataItem)attrs[Key];
                return (Int32)item.Value;
            }
            else
                return 0;
        }

        private Int16 GetMetaTagWordValue(IDictionary attrs, string Key)
        {
            if (attrs.Contains(Key))
            {
                MetadataItem item = (MetadataItem)attrs[Key];
                return (Int16)item.Value;
            }
            else
                return 0;
        }

        private string GetMetaTagStringValue(IDictionary attrs, string Key)
        {
            if (attrs.Contains(Key))
            {
                MetadataItem item = (MetadataItem)attrs[Key];
                return item.Value.ToString();
            }
            else
                return "";
        }

        private bool GetMetaTagBoolValue(IDictionary attrs, string Key)
        {
            if (attrs.Contains(Key))
            {
                MetadataItem item = (MetadataItem)attrs[Key];
                bool ret;
                bool.TryParse(item.Value.ToString(), out ret);
                return ret;
            }
            else
                return false;
        }

        #endregion

        /// <summary>
        /// Create an XML file containing the XBMC compatible information file.
        /// Refer to http://wiki.xbmc.org/index.php?title=Import_-_Export_Library for more details
        /// </summary>
        /// <param name="originalVideo">Full path to original video file</param>
        /// <param name="localVideo">Full path to local working video (if different from original video)</param>
        /// <param name="workingPath">Full path to working temp directory</param>
        /// <param name="videoFile">VideoFile metadata structure</param>
        /// <returns></returns>
        public bool WriteXBMCXMLTags(string originalVideo, string localVideo, string workingPath, VideoInfo videoFile)
        {
            string xmlFileName = Path.Combine(workingPath, Path.GetFileNameWithoutExtension(localVideo) + ".nfo"); // Path\FileName.nfo

            _jobLog.WriteEntry(this, Localise.GetPhrase("Extracting info from source video into NFO file (XML)"), Log.LogEntryType.Information);

            // Check if the NFO file exists with the source video and then copy it else extract it
            string sourceXMLFile = Path.Combine(Path.GetDirectoryName(originalVideo), Path.GetFileNameWithoutExtension(originalVideo) + ".nfo");
            if (File.Exists(sourceXMLFile) && (String.Compare(sourceXMLFile, xmlFileName, true) != 0)) // check incase the source and destination are the same
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Found source NFO file, copying to output folder"), Log.LogEntryType.Information);
                Util.FileIO.TryFileDelete(xmlFileName); // just in case target exists, delete it
                File.Copy(sourceXMLFile, xmlFileName);
                return true;
            }

            try
            {
                Util.FileIO.TryFileDelete(xmlFileName); // just incase it exists

                // Create the XMLFile
                XmlWriterSettings set = new XmlWriterSettings();
                set.Indent = true;
                set.IndentChars = "\t";
                set.Encoding = Encoding.UTF8;
                using (XmlWriter writer = XmlWriter.Create(xmlFileName, set))
                {
                    // If this is a TV Espisode - http://wiki.xbmc.org/index.php?title=NFO_files/tvepisodes, https://code.google.com/p/moviejukebox/wiki/NFO_Files
                    if (_videoTags.IsMovie == false)
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("episodedetails");

                        // NOTE: Custom additions, not part of the standard so we can read back the Showname
                        writer.WriteElementString("xshow", _videoTags.Title);
                        writer.WriteElementString("xrating", _videoTags.Rating);
                        writer.WriteElementString("xsport", _videoTags.IsSports.ToString());
                        writer.WriteElementString("xrecorded", (_videoTags.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME ? _videoTags.RecordedDateTime.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : ""));
                        writer.WriteElementString("xtvdbid", _videoTags.tvdbId);
                        writer.WriteElementString("xtmdbid", _videoTags.tmdbId);
                        
                        // Standard Tags
                        writer.WriteElementString("title", _videoTags.SubTitle);
                        writer.WriteElementString("showtitle", _videoTags.Title); // TODO: Some places use Show Title here and other Episode Title - which one is it? (http://mediabrowser.tv/community/index.php?/topic/11713-nfo-file-for-multi-episodes-files/)
                        writer.WriteElementString("season", (_videoTags.Season == 0 ? "" : _videoTags.Season.ToString()));
                        writer.WriteElementString("episode", (_videoTags.Season == 0 ? "" : _videoTags.Episode.ToString()));
                        writer.WriteElementString("plot", _videoTags.Description);
                        writer.WriteElementString("thumb", _videoTags.BannerURL);
                        writer.WriteElementString("id", _videoTags.imdbId);
                        writer.WriteElementString("credits", _videoTags.MediaCredits);
                        if (_videoTags.Genres != null && _videoTags.Genres.Length > 0)
                        {
                            foreach (string genre in _videoTags.Genres)
                            {
                                writer.WriteElementString("genre", genre);
                            }
                        }
                        writer.WriteElementString("premiered", (_videoTags.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME ? _videoTags.SeriesPremiereDate.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : ""));
                        writer.WriteElementString("aired", (_videoTags.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME ? _videoTags.OriginalBroadcastDateTime.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : ""));
                        writer.WriteElementString("studio", _videoTags.Network);

                        writer.WriteStartElement("fileinfo");
                        writer.WriteStartElement("streamdetails");

                        writer.WriteStartElement("audio");
                        writer.WriteElementString("channels", videoFile.AudioChannels.ToString());
                        writer.WriteElementString("codec", videoFile.AudioCodec);
                        writer.WriteEndElement();

                        writer.WriteStartElement("video");
                        writer.WriteElementString("codec", videoFile.VideoCodec);
                        writer.WriteElementString("durationinseconds", ((int)videoFile.Duration).ToString(CultureInfo.InvariantCulture));
                        writer.WriteElementString("height", videoFile.Height.ToString());
                        writer.WriteElementString("language", videoFile.AudioLanguage);
                        writer.WriteElementString("longlanguage", ISO639_3.GetLanguageName(videoFile.AudioLanguage));
                        writer.WriteElementString("width", videoFile.Width.ToString());
                        writer.WriteEndElement();

                        writer.WriteEndElement();
                        writer.WriteEndElement();

                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                    else // This for a movie - http://wiki.xbmc.org/index.php?title=NFO_files/movies, https://code.google.com/p/moviejukebox/wiki/NFO_Files
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("movie");

                        // NOTE: Custom additions, not part of the standard so we can read back the Showname
                        writer.WriteElementString("xrating", _videoTags.Rating);
                        writer.WriteElementString("xrecorded", (_videoTags.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME ? _videoTags.RecordedDateTime.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : ""));
                        writer.WriteElementString("xtvdbid", _videoTags.tvdbId);
                        writer.WriteElementString("xtmdbid", _videoTags.tmdbId);

                        // Standard Tags
                        writer.WriteElementString("title", _videoTags.Title);
                        writer.WriteElementString("outline", _videoTags.Description);
                        writer.WriteElementString("year", _videoTags.OriginalBroadcastDateTime.Year.ToString()); // This is only for a movie
                        writer.WriteElementString("premiered", (_videoTags.SeriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME ? _videoTags.SeriesPremiereDate.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : ""));
                        writer.WriteElementString("plot", _videoTags.Description);
                        writer.WriteElementString("runtime", ((int)(videoFile.Duration / 60)).ToString(CultureInfo.InvariantCulture));
                        writer.WriteElementString("thumb", _videoTags.BannerURL);
                        writer.WriteElementString("id", _videoTags.imdbId);
                        writer.WriteElementString("studio", _videoTags.Network);
                        writer.WriteElementString("company", _videoTags.Network);
                        writer.WriteElementString("credits", _videoTags.MediaCredits);
                        if (_videoTags.Genres != null && _videoTags.Genres.Length > 0)
                        {
                            foreach (string genre in _videoTags.Genres)
                            {
                                writer.WriteElementString("genre", genre);
                            }
                        }

                        writer.WriteStartElement("fileinfo");
                        writer.WriteStartElement("streamdetails");

                        writer.WriteStartElement("video");
                        writer.WriteElementString("codec", videoFile.VideoCodec);
                        writer.WriteElementString("width", videoFile.Width.ToString());
                        writer.WriteElementString("height", videoFile.Height.ToString());
                        writer.WriteEndElement();

                        writer.WriteStartElement("audio");
                        writer.WriteElementString("codec", videoFile.AudioCodec);
                        writer.WriteElementString("language", videoFile.AudioLanguage);
                        writer.WriteElementString("channels", videoFile.AudioChannels.ToString());
                        writer.WriteEndElement();

                        writer.WriteEndElement();
                        writer.WriteEndElement();

                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "Unable to write metadata to NFO.\r\nError : " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }
        }

        /// <summary>
        /// Extract tags from NON WTV/DVRMS files
        /// </summary>
        private bool ExtractGenericTags()
        {
            try
            {
                // Defaults: Will customize as required later
                TagLib.File TagFile = TagLib.File.Create(_videoFileName);
                _videoTags.Title = TagFile.Tag.Title;
                _videoTags.Genres = TagFile.Tag.Genres;
                _videoTags.SubTitle = TagFile.Tag.Album; 
                _videoTags.Description = TagFile.Tag.Comment;
                _videoTags.Season = (int) TagFile.Tag.Disc;
                _videoTags.Episode = (int) TagFile.Tag.Track;

                try
                {
                    if (_downloadBannerFile)
                    {
                        // Read the artwork
                        TagLib.File file = TagLib.File.Create(_videoFileName);
                        TagLib.IPicture pic = file.Tag.Pictures[0];  //pic contains data for image.
                        if (pic != null && !String.IsNullOrWhiteSpace(_videoTags.Title))
                        {
                            _jobLog.WriteEntry(this, "Trying to extract Artwork from file", Log.LogEntryType.Information);
                            using (MemoryStream stream = new MemoryStream(pic.Data.Data))  //create an in memory stream
                            {
                                // Write to file as backup
                                _videoTags.BannerFile = Path.Combine(GlobalDefs.CachePath, Util.FilePaths.RemoveIllegalFileNameChars(_videoTags.Title) + "-extract.jpg");
                                Bitmap artWork = new Bitmap(stream);
                                artWork.Save(_videoTags.BannerFile, System.Drawing.Imaging.ImageFormat.Jpeg); // Save as JPEG
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _jobLog.WriteEntry(this, "Error reading Artwork -> " + e.ToString(), Log.LogEntryType.Warning);
                    _videoTags.BannerFile = "";
                }

                switch (FilePaths.CleanExt(_videoFileName)) // special case processing
                {
                    case ".mkv":
                        _jobLog.WriteEntry(this, "Read Tags: MKV file detected using Matroska", Log.LogEntryType.Information);
                        TagLib.Matroska.Tag MKVTag = TagFile.GetTag(TagLib.TagTypes.Id3v2) as TagLib.Matroska.Tag;
                        if (!String.IsNullOrWhiteSpace(MKVTag.Title)) _videoTags.Title = MKVTag.Title;;
                        if (!String.IsNullOrWhiteSpace(MKVTag.Album)) _videoTags.SubTitle = MKVTag.Album;
                        if (!String.IsNullOrWhiteSpace(MKVTag.Comment)) _videoTags.Description = MKVTag.Comment;
                        if (MKVTag.Performers != null) if (MKVTag.Performers.Length > 0) _videoTags.Genres = MKVTag.Performers;

                        break;

                    case ".mp4":
                    case ".m4v": // Apple MPEG4 Tags ATOMS http://atomicparsley.sourceforge.net/mpeg-4files.html and https://code.google.com/p/mp4v2/wiki/iTunesMetadata
                        _jobLog.WriteEntry(this, "Read Tags: MPEG4 file detected using AppleTag", Log.LogEntryType.Information);
                        TagLib.Mpeg4.AppleTag MP4Tag = TagFile.GetTag(TagLib.TagTypes.Apple) as TagLib.Mpeg4.AppleTag;

                        if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tvsh")) != null)
                            if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tvsh")).Length > 0)
                                if (!String.IsNullOrEmpty(MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tvsh"))[0]))
                                    _videoTags.Title = MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tvsh"))[0]; // Showname
                        // Extract the Title if the Showname is empty. Bug with PlayLater which sets the Title to the filename but keeps the Showname correctly - https://mcebuddy2x.codeplex.com/discussions/572252
                        if (String.IsNullOrWhiteSpace(_videoTags.Title))
                            if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'n', (byte)'a', (byte)'m' })) != null)
                                if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'n', (byte)'a', (byte)'m' })).Length > 0)
                                    if (!String.IsNullOrEmpty(MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'n', (byte)'a', (byte)'m' }))[0]))
                                        _videoTags.Title = MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'n', (byte)'a', (byte)'m' }))[0]; // Title if there is no Showname
                        if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tven")) != null)
                            if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tven")).Length > 0) 
                                if (!String.IsNullOrEmpty(MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tven"))[0])) 
                                    _videoTags.SubTitle = MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tven"))[0]; // Episode Name
                        if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tvnn")) != null) 
                            if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tvnn")).Length > 0) 
                                if (!String.IsNullOrEmpty(MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tvnn"))[0])) 
                                    _videoTags.Network = MP4Tag.GetText(new TagLib.ReadOnlyByteVector("tvnn"))[0]; // Network
                        if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector("desc")) != null) 
                            if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector("desc")).Length > 0) 
                                if (!String.IsNullOrEmpty(MP4Tag.GetText(new TagLib.ReadOnlyByteVector("desc"))[0])) 
                                    _videoTags.Description = MP4Tag.GetText(new TagLib.ReadOnlyByteVector("desc"))[0]; // Description
                        if (String.IsNullOrWhiteSpace(_videoTags.Description)) 
                            if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector("ldes")) != null) 
                                if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector("ldes")).Length > 0) 
                                    if (!String.IsNullOrEmpty(MP4Tag.GetText(new TagLib.ReadOnlyByteVector("ldes"))[0])) 
                                        _videoTags.Description = MP4Tag.GetText(new TagLib.ReadOnlyByteVector("ldes"))[0]; // Description (long) backup 
                        if (String.IsNullOrWhiteSpace(_videoTags.Description))
                            if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'c', (byte)'m', (byte)'t' })) != null)
                                if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'c', (byte)'m', (byte)'t' })).Length > 0)
                                    if (!String.IsNullOrEmpty(MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'c', (byte)'m', (byte)'t' }))[0])) 
                                        _videoTags.Description = MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'c', (byte)'m', (byte)'t' }))[0]; // Description (comment) backup
                        if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'g', (byte)'e', (byte)'n' })) != null)
                            if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'g', (byte)'e', (byte)'n' })).Length > 0)
                                if (!String.IsNullOrEmpty(MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'g', (byte)'e', (byte)'n' }))[0])) 
                                    _videoTags.Genres = new String[] { MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'g', (byte)'e', (byte)'n' }))[0] }; // Genre (only 1)
                        if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'d', (byte)'a', (byte)'y' })) != null) 
                            if (MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'d', (byte)'a', (byte)'y' })).Length > 0) 
                                if (!String.IsNullOrEmpty(MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'d', (byte)'a', (byte)'y' }))[0]))
                                    if (!DateTime.TryParse(MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'d', (byte)'a', (byte)'y' }))[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _videoTags.OriginalBroadcastDateTime)) // Recorded Date and time (assuming the entire date is there (UTC)
                                        DateTime.TryParse(MP4Tag.GetText(new TagLib.ReadOnlyByteVector(new Byte[] { (byte)0xA9, (byte)'d', (byte)'a', (byte)'y' }))[0] + "-05-05", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _videoTags.OriginalBroadcastDateTime); // Sometimes only the year is provided so we mock it up and try (UTC)
                        if (MP4Tag.DataBoxes(new TagLib.ReadOnlyByteVector("stik")) != null)
                        {
                            foreach (TagLib.Mpeg4.AppleDataBox box in MP4Tag.DataBoxes(new TagLib.ReadOnlyByteVector("stik")))
                            {
                                int stik = box.Data.ToInt(); // Movie (9) or TVShow (10) - https://code.google.com/p/mp4v2/wiki/iTunesMetadata
                                if (stik == 9)
                                    _videoTags.IsMovie = true;
                            }
                        }
                        if (MP4Tag.DataBoxes(new TagLib.ReadOnlyByteVector("tvsn")) != null)
                        {
                            foreach (TagLib.Mpeg4.AppleDataBox box in MP4Tag.DataBoxes(new TagLib.ReadOnlyByteVector("tvsn")))
                            {
                                _videoTags.Season = box.Data.ToInt(); // Season
                            }
                        }
                        if (MP4Tag.DataBoxes(new TagLib.ReadOnlyByteVector("tves")) != null)
                        {
                            foreach (TagLib.Mpeg4.AppleDataBox box in MP4Tag.DataBoxes(new TagLib.ReadOnlyByteVector("tves")))
                            {
                                _videoTags.Episode = box.Data.ToInt(); // Season
                            }
                        }
                        break;

                    case ".avi": // AVI Extended INFO Tags: http://abcavi.kibi.ru/infotags.htm
                        _jobLog.WriteEntry(this, "Read Tags: AVI file detected using RiffTag", Log.LogEntryType.Information);
                        TagLib.Riff.InfoTag RiffTag = TagFile.GetTag(TagLib.TagTypes.RiffInfo) as TagLib.Riff.InfoTag;
                        if ((RiffTag.Year > 0) && (_videoTags.Season <= 0)) _videoTags.Season = (int)RiffTag.Year; // Disc is always zero for RiffTag
                        if (RiffTag.GetValuesAsStrings("ISBJ") != null) if(RiffTag.GetValuesAsStrings("ISBJ").Length > 0) if (!String.IsNullOrEmpty(RiffTag.GetValuesAsStrings("ISBJ")[0])) _videoTags.SubTitle = RiffTag.GetValuesAsStrings("ISBJ")[0]; // Subject
                        break;

                    case ".mp3":
                        _jobLog.WriteEntry(this, "Read Tags: MP3 file detected", Log.LogEntryType.Information);
                        if (!String.IsNullOrEmpty(TagFile.Tag.Album)) _videoTags.Title = TagFile.Tag.Album; // Reversed for MP3
                        if (!String.IsNullOrEmpty(TagFile.Tag.Title)) _videoTags.SubTitle = TagFile.Tag.Title;
                        break;

                    case ".wmv":
                        _jobLog.WriteEntry(this, "Read Tags: WMV file detected using AsfTag", Log.LogEntryType.Information);
                        TagLib.Asf.Tag AsfTag = TagFile.GetTag(TagLib.TagTypes.Asf) as TagLib.Asf.Tag;
                        if (!String.IsNullOrWhiteSpace(AsfTag.GetDescriptorString("WM/SubTitle"))) _videoTags.SubTitle = AsfTag.GetDescriptorString("WM/SubTitle");
                        if (!String.IsNullOrWhiteSpace(AsfTag.GetDescriptorString("WM/SubTitleDescription"))) _videoTags.Description = AsfTag.GetDescriptorString("WM/SubTitleDescription");
                        if (!String.IsNullOrWhiteSpace(AsfTag.GetDescriptorString("WM/MediaStationName"))) _videoTags.Network = AsfTag.GetDescriptorString("WM/MediaStationName");
                        foreach (TagLib.Asf.ContentDescriptor descriptor in AsfTag.GetDescriptors("WM/MediaIsMovie"))
                        {
                            _videoTags.IsMovie = descriptor.ToBool();
                        }
                        foreach (TagLib.Asf.ContentDescriptor descriptor in AsfTag.GetDescriptors("WM/MediaIsSport"))
                        {
                            _videoTags.IsSports = descriptor.ToBool();
                        }
                        foreach (TagLib.Asf.ContentDescriptor descriptor in AsfTag.GetDescriptors("WM/WMRVEncodeTime"))
                        {
                            DateTime.TryParse((new DateTime((long)descriptor.ToQWord())).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _videoTags.RecordedDateTime);
                        }
                        if (!String.IsNullOrWhiteSpace(AsfTag.GetDescriptorString("WM/ParentalRating"))) _videoTags.Rating = AsfTag.GetDescriptorString("WM/ParentalRating");
                        if (_videoTags.IsMovie)
                        {
                            string movieYear = AsfTag.GetDescriptorString("WM/OriginalReleaseTime");
                            if (!String.IsNullOrWhiteSpace(movieYear)) // Use the movie year to set the original broadcast date as YYYY-05-05 (mid year to avoid timezone issues)
                                DateTime.TryParse(movieYear + "-05-05", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.OriginalBroadcastDateTime);
                        }
                        else
                        {
                            if (!String.IsNullOrWhiteSpace(AsfTag.GetDescriptorString("WM/MediaOriginalBroadcastDateTime")))
                                DateTime.TryParse(AsfTag.GetDescriptorString("WM/MediaOriginalBroadcastDateTime"), null, DateTimeStyles.None, out _videoTags.OriginalBroadcastDateTime);
                        }

                        break;

                    default:
                        break;
                }

                _jobLog.WriteEntry(this, Localise.GetPhrase("Extracted Generic Tags:") + " " + _videoTags.ToString(), Log.LogEntryType.Debug);
            }
            catch (Exception Ex)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Unable to extract meta data from file") + " " + _videoFileName + ". " + Ex.Message,  Log.LogEntryType.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Write tags/metadata to files (generic)
        /// </summary>
        /// <param name="outputFile">File to write into</param>
        private bool WriteGenericTags(string outputFile)
        {
            TagLib.File NewTagFile;

            try
            {
                TagLib.ByteVector.UseBrokenLatin1Behavior = true; // Use Default Encoding for Latin-1 strings instead of code page 1252 (this is how WMP works, technically broken and should ideally be code page 1252 or UTF-8, but WMP takes the default local encoding). Required for non english symbols
                NewTagFile = TagLib.File.Create(outputFile);

                switch (FilePaths.CleanExt(outputFile))
                {
                    case ".mkv": // Tag mapping taken from http://matroska.org/technical/specs/tagging/othertagsystems/comparetable.html
                        // TODO: Very weak support for MKV tags provided by TagLib (only 4 parameters), need to enhance library to add more support
                        _jobLog.WriteEntry(this, "Write Tags: MKV file detected using Matroska", Log.LogEntryType.Information);
                        TagLib.Matroska.Tag MKVTag = NewTagFile.GetTag(TagLib.TagTypes.Id3v2) as TagLib.Matroska.Tag;
                        MKVTag.Title = _videoTags.Title;
                        MKVTag.Album = _videoTags.SubTitle;
                        MKVTag.Comment = _videoTags.Description;
                        MKVTag.Performers = _videoTags.Genres;

                        break;

                    case ".avi": // AVI Extended INFO Tags: http://abcavi.kibi.ru/infotags.htm
                        _jobLog.WriteEntry(this, "Write Tags: AVI file detected using RiffTag", Log.LogEntryType.Information);
                        TagLib.Riff.InfoTag RiffTag = NewTagFile.GetTag(TagLib.TagTypes.RiffInfo) as TagLib.Riff.InfoTag;
                        RiffTag.Title = _videoTags.Title;
                        RiffTag.SetValue("ISBJ", _videoTags.SubTitle); // Subject
                        RiffTag.Comment = _videoTags.Description;
                        RiffTag.Genres = _videoTags.Genres;
                        RiffTag.Track = (uint)_videoTags.Episode;

                        RiffTag.Year = (uint)_videoTags.Season; // Disc is always zero for RiffTag
                        RiffTag.SetValue("ISFT", "MCEBuddy2x"); // Software
                        
                        if (!String.IsNullOrWhiteSpace(_videoTags.BannerFile))
                        {
                            TagLib.Picture VideoPicture = new TagLib.Picture(_videoTags.BannerFile);
                            RiffTag.Pictures = new TagLib.Picture[] { VideoPicture };
                        }

                        break;

                    case ".wmv":
                        _jobLog.WriteEntry(this, "Write Tags: WMV file detected using AsfTag", Log.LogEntryType.Information);
                        TagLib.Asf.Tag AsfTag = NewTagFile.GetTag(TagLib.TagTypes.Asf) as TagLib.Asf.Tag;
                        AsfTag.Title = _videoTags.Title;
                        AsfTag.Comment = _videoTags.Description;
                        AsfTag.Genres = _videoTags.Genres;
                        AsfTag.Disc = (uint)_videoTags.Season;
                        AsfTag.Track = (uint)_videoTags.Episode;

                        AsfTag.SetDescriptorString(_videoTags.SubTitle, "WM/SubTitle");
                        AsfTag.SetDescriptorString(_videoTags.Description, "WM/SubTitleDescription");
                        AsfTag.SetDescriptorString(_videoTags.Network, "WM/MediaStationName");
                        AsfTag.SetDescriptors("WM/MediaIsMovie", new TagLib.Asf.ContentDescriptor("WM/MediaIsMovie", _videoTags.IsMovie));
                        AsfTag.SetDescriptors("WM/MediaIsSport", new TagLib.Asf.ContentDescriptor("WM/MediaIsSport", _videoTags.IsSports));
                        AsfTag.SetDescriptors("WM/WMRVEncodeTime", new TagLib.Asf.ContentDescriptor("WM/WMRVEncodeTime", (ulong)_videoTags.RecordedDateTime.Ticks));
                        AsfTag.SetDescriptorString(_videoTags.Rating, "WM/ParentalRating");
                        if (_videoTags.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME)
                        {
                            if (_videoTags.IsMovie)
                                AsfTag.SetDescriptorString(_videoTags.OriginalBroadcastDateTime.Year.ToString(), "WM/OriginalReleaseTime");
                            else
                                AsfTag.SetDescriptorString(_videoTags.OriginalBroadcastDateTime.ToString("s") + "Z", "WM/MediaOriginalBroadcastDateTime");
                        }

                        if (!String.IsNullOrWhiteSpace(_videoTags.BannerFile))
                        {
                            TagLib.Picture VideoPicture = new TagLib.Picture(_videoTags.BannerFile);
                            AsfTag.Pictures = new TagLib.Picture[] { VideoPicture };
                        }
                        break;

                    case ".mp3":
                        _jobLog.WriteEntry(this, "Write Tags: MP3 file detected using ID3v2", Log.LogEntryType.Information);
                        TagLib.Id3v2.Tag MP3Tag = NewTagFile.GetTag(TagLib.TagTypes.Id3v2) as TagLib.Id3v2.Tag;
                        if (!String.IsNullOrWhiteSpace(_videoTags.SubTitle))
                            MP3Tag.Title = _videoTags.SubTitle;
                        else
                            MP3Tag.Title = _videoTags.Title;
                        MP3Tag.Album = _videoTags.Title;
                        MP3Tag.Comment = _videoTags.Description;
                        MP3Tag.Genres = _videoTags.Genres;
                        MP3Tag.Disc = (uint)_videoTags.Season;
                        MP3Tag.Track = (uint)_videoTags.Episode;
                        if (!String.IsNullOrWhiteSpace(_videoTags.BannerFile))
                        {
                            TagLib.Picture VideoPicture = new TagLib.Picture(_videoTags.BannerFile);
                            MP3Tag.Pictures = new TagLib.Picture[] { VideoPicture };
                        }
                        break;

                    default:
                        _jobLog.WriteEntry(this, "Write Tags: Unknown file detected -> " + FilePaths.CleanExt(outputFile) + ", writing generic tags", Log.LogEntryType.Warning);
                        NewTagFile.Tag.Title = _videoTags.Title; // Generic Tags
                        NewTagFile.Tag.Album = _videoTags.SubTitle;
                        NewTagFile.Tag.Comment = _videoTags.Description;
                        NewTagFile.Tag.Genres = _videoTags.Genres;
                        NewTagFile.Tag.Genres = _videoTags.Genres;
                        NewTagFile.Tag.Disc = (uint)_videoTags.Season;
                        NewTagFile.Tag.Track = (uint)_videoTags.Episode;
                        if (!String.IsNullOrWhiteSpace(_videoTags.BannerFile))
                        {
                            TagLib.Picture VideoPicture = new TagLib.Picture(_videoTags.BannerFile);
                            NewTagFile.Tag.Pictures = new TagLib.Picture[] { VideoPicture };
                        }
                        break;
                }
            }
            catch (Exception Ex)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Unable to write meta data to file") + " " + outputFile + ". " + Ex.Message, Log.LogEntryType.Warning);
                return false;
            }

            try
            {
                _jobLog.WriteEntry(this, "About to write Tags", Log.LogEntryType.Debug);
                NewTagFile.Save();
            }
            catch (Exception Ex)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Unable to write meta data to file") + " " + outputFile + ". " + Ex.Message, Log.LogEntryType.Warning);
                return false;
            }

            return true;
        }

        
        /// <summary>
        /// Adds a file to the Windows Media Player (WMP) library
        /// </summary>
        /// <param name="filePath">File path and name</param>
        /// <returns>True if successful, false if unable to add</returns>
        public static bool AddFileToWMPLibrary(string filePath, Log jobLog)
        {
            WindowsMediaPlayer wmp = null;

            for (int i = 0; i < 5; i++) // Retry upto 5 times, COM Server is flaky sometimes
            {
                jobLog.WriteEntry("About to add " + filePath + " to the WMP library, try " + i.ToString(), Log.LogEntryType.Debug);

                try
                {
                    wmp = new WindowsMediaPlayer(); // Create a new wmp object
                    //wmp.uiMode = "invisible"; // Hide the window
                    IWMPMediaCollection wmpLibrary = wmp.mediaCollection;

                    IWMPMedia libStatus = wmpLibrary.add(filePath);
                    if (libStatus == null || (((IWMPMedia2)libStatus).Error != null))
                        jobLog.WriteEntry("WARNING: Error adding file to WMP Library.\r\nError code : " + ((IWMPMedia2)libStatus).Error.errorCode.ToString() + ".\r\nError message : " + ((IWMPMedia2)libStatus).Error.errorDescription, Log.LogEntryType.Warning);
                    else
                        jobLog.WriteEntry("Added " + filePath + " successfully to the WMP library", Log.LogEntryType.Information);

                    // Release the COM object and clean up otherwise it hangs later
                    wmp.close(); // Close the player
                    Marshal.FinalReleaseComObject(wmp);
                    wmp = null;
                    return true; // all done
                }
                catch (Exception e)
                {
                    jobLog.WriteEntry("ERROR: Unable to communicate with WMP Library, WMP may not be installed or no user is logged in. File not added to the WMP library.\r\nError : " + e.ToString(), Log.LogEntryType.Error);

                    // Release the COM object
                    if (wmp != null)
                    {
                        try
                        {
                            wmp.close(); // Close the player
                            Marshal.FinalReleaseComObject(wmp);
                        }
                        catch { }
                        wmp = null;

                        Thread.Sleep(10000); // Wait for 10 seconds before retrying
                    }
                }
            }

            return false; // Unsuccessful
        }

        /// <summary>
        /// Write tags to MP4 files using Atomic Parsley
        /// </summary>
        /// <param name="outputFile">MP4 file to write the tags into</param>
        private bool WriteMP4Tags(string outputFile)
        {
            // TODO Replace with Taglib once we can debug the MP4 box write.  Use atomic parlsey in the meantime
            // --overwrite HAS to be first option since it directs AtomicParsley NOT to rename the file (which it does by default)
            // Windows limits the arguments + full filename path to 255 bytes, if the description/comments are too long it windows will clip the arguments
            // hence --overWrite has to be the first parameter else it might be cut off and the filename will change leading to a conversion failure

            string cmdLine = Util.FilePaths.FixSpaces(outputFile) + " --overWrite";
            
            // Ideally the title should be SxEx - EpisodeName, if not then just use EpisodeName, if not then use Title
            cmdLine += " --title " + Util.FilePaths.FixSpaces((!String.IsNullOrWhiteSpace(_videoTags.SubTitle) ? 
                                        ((_videoTags.Season != 0) && (_videoTags.Episode != 0) ? "S" + _videoTags.Season.ToString() + "E" + _videoTags.Episode + " - " : "") + _videoTags.SubTitle  // SxEx - EpisodeName
                                        : _videoTags.Title // Title
                                     ));
            
            // Prioritze information, 255 character limit
            if (_videoTags.IsMovie == false || _videoTags.Season != 0) // TV show
            {
                    cmdLine += " --TVShowName " + Util.FilePaths.FixSpaces(_videoTags.Title) + " --stik \"TV Show\"";
                    if (!String.IsNullOrWhiteSpace(_videoTags.SubTitle)) cmdLine += " --TVEpisode " + Util.FilePaths.FixSpaces(_videoTags.SubTitle);
                    if (_videoTags.Season > 0) cmdLine += " --TVSeasonNum " + Util.FilePaths.FixSpaces(_videoTags.Season.ToString());
                    if (_videoTags.Episode > 0) cmdLine += " --TVEpisodeNum " + Util.FilePaths.FixSpaces(_videoTags.Episode.ToString());
            }
            else
                cmdLine += " --stik \"Short Film\""; // Movie is marked by iTunes as Home Video where as Short Film is marked as a movie

            if (!String.IsNullOrWhiteSpace(_videoTags.Network))
                cmdLine += " --TVNetwork " + Util.FilePaths.FixSpaces(_videoTags.Network);

            if (_videoTags.Genres != null)
                if (_videoTags.Genres.Length > 0) // Check if number of elements is > 0, check for null does not work snice sometimes there is a null array present
                    cmdLine += " --genre " + Util.FilePaths.FixSpaces(_videoTags.Genres[0]);

            if (_videoTags.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) // Add the original broadcast date
            {
                if (_videoTags.IsMovie)
                    cmdLine += " --year " + Util.FilePaths.FixSpaces(_videoTags.OriginalBroadcastDateTime.Year.ToString());
                else
                    cmdLine += " --year " + Util.FilePaths.FixSpaces(_videoTags.OriginalBroadcastDateTime.ToString("s") + "Z");
            }

            if (!String.IsNullOrWhiteSpace(_videoTags.Description))
            {
                cmdLine += " --description " + Util.FilePaths.FixSpaces(_videoTags.Description);
                cmdLine += " --longdesc " + Util.FilePaths.FixSpaces(_videoTags.Description);                
                cmdLine += " --comment " + Util.FilePaths.FixSpaces(_videoTags.Description);
            }

            if (File.Exists(_videoTags.BannerFile))
                cmdLine += " --artwork " + Util.FilePaths.FixSpaces(_videoTags.BannerFile);

            cmdLine += " --encodingTool \"MCEBuddy\""; //final touch
            _jobLog.WriteEntry(this, Localise.GetPhrase("About to write MP4 Tags"), Log.LogEntryType.Debug);

            AppWrapper.AtomicParsley ap = new AtomicParsley(cmdLine, _jobStatus, _jobLog, _ignoreSuspend);
            ap.Run();
            if (!ap.Success)
            {
                _jobStatus.PercentageComplete = 0; //Atomic PArsley didn't find the necessary output or the process failed
                return false;
            }

            return true;
        }

        /// <summary>
        /// Write tags and metadata to file
        /// </summary>
        /// <param name="outputFile">File to write tags/metadata info</param>
        public void WriteTags(string outputFile)
        {
            if (!File.Exists(outputFile)) return;

            string ext = FilePaths.CleanExt(outputFile);
            if (!((ext == ".wtv") || (ext == ".mp4") || (ext == ".m4v") || (ext == ".avi") || (ext == ".wmv") || (ext == ".mp3"))) return;

            if (ext == ".mp4" || ext == ".m4v")
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("About to write MP4 Tags"), Log.LogEntryType.Information);
                WriteMP4Tags(outputFile);
            }
            else if (ext == ".wtv")
            {
                WriteMCETags(outputFile);
            }
            else
            {
                WriteGenericTags(outputFile);
            }
        }

        /// <summary>
        /// Adds the subtitle file and chapters to the target file
        /// </summary>
        /// <param name="srtFile">Path to subtitle file</param>
        /// <param name="neroChapterFile">Path to Nero chapter file</param>
        /// <param name="xmlChapterFile">Path to xml (iTunes) chapter file</param>
        /// <param name="targetFile">Path to target file</param>
        /// <returns>False in case of an error</returns>
        public bool AddSubtitlesAndChaptersToFile(string srtFile, string neroChapterFile, string xmlChapterFile, string targetFile)
        {
            _jobLog.WriteEntry("Subtitle File : " + srtFile + "\nChapter File : " + neroChapterFile + "\nTarget File : " + targetFile, Log.LogEntryType.Debug);

            if (!File.Exists(targetFile))
                return false;

            if (String.IsNullOrWhiteSpace(xmlChapterFile) && String.IsNullOrWhiteSpace(neroChapterFile) && String.IsNullOrWhiteSpace(srtFile)) // Atleast one file should be there to proceeed
                return true; // nothing to do

            if ((FileIO.FileSize(xmlChapterFile) <= 0) && (FileIO.FileSize(neroChapterFile) <= 0) && (FileIO.FileSize(srtFile) < 0)) // Atleast one file should be valid to proceeed
                return true; // nothing to do

            string parameters = "";
            switch (FilePaths.CleanExt(targetFile))
            {
                case ".mp4":
                case ".m4v":
                    if (File.Exists(srtFile)) // Add the subtitles
                        parameters += " -add " + Util.FilePaths.FixSpaces(srtFile) + ":hdlr=sbtl";

                    if (File.Exists(neroChapterFile)) // Add the nero chapters
                        parameters += " -chap " + Util.FilePaths.FixSpaces(neroChapterFile);

                    if (File.Exists(xmlChapterFile)) // Add the iTunes chapters TODO: adding ttxt chapter files leads to incorrect file length (minutes) reported, why is MP4Box doing this? Is the TTXT format incorrect?
                        parameters += " -add " + Util.FilePaths.FixSpaces(xmlChapterFile) + ":chap";

                    parameters += " " + Util.FilePaths.FixSpaces(targetFile); // output file

                    MP4Box mp4Box = new MP4Box(parameters, _jobStatus, _jobLog, _ignoreSuspend);
                    mp4Box.Run();
                    if (!mp4Box.Success || (FileIO.FileSize(targetFile) <= 0))
                    {
                        _jobStatus.PercentageComplete = 0;
                        _jobStatus.ErrorMsg = "Mp4Box adding subtitles failed";
                        _jobLog.WriteEntry(Localise.GetPhrase("Mp4Box adding subtitles failed"), Log.LogEntryType.Error); ;
                        return false;
                    }

                    return true; // all done

                case ".mkv":
                    string outputFile = FilePaths.GetFullPathWithoutExtension(targetFile) + "-temp.mkv";
                    parameters += "--clusters-in-meta-seek -o " + Util.FilePaths.FixSpaces(outputFile) + " --compression -1:none " + Util.FilePaths.FixSpaces(targetFile); // output file

                    if (File.Exists(srtFile)) // Add the subtitles
                        parameters += " --compression -1:none " + Util.FilePaths.FixSpaces(srtFile);

                    if (File.Exists(neroChapterFile)) // Add the chapters
                        parameters += " --compression -1:none --chapters " + Util.FilePaths.FixSpaces(neroChapterFile);

                    MKVMerge mkvMerge = new MKVMerge(parameters, _jobStatus, _jobLog, _ignoreSuspend);
                    mkvMerge.Run();
                    if (!mkvMerge.Success || (Util.FileIO.FileSize(outputFile) <= 0)) // check for +ve success
                    {
                        _jobStatus.PercentageComplete = 0;
                        _jobStatus.ErrorMsg = "MKVMerge adding subtitles failed";
                        _jobLog.WriteEntry(Localise.GetPhrase("MKVMerge adding subtitles failed"), Log.LogEntryType.Error); ;
                        return false;
                    }

                    // Replace the temp file
                    Util.FileIO.TryFileReplace(targetFile, outputFile);

                    if (Util.FileIO.FileSize(targetFile) <= 0)
                    {
                        _jobLog.WriteEntry(Localise.GetPhrase("MKVMerge: Error moving files"), Log.LogEntryType.Error); ;
                        return false; // Something went wrong
                    }

                    return true; // done

                default:
                    return true; // not a valid type, so ignore it
            }
        }

        /// <summary>
        /// Downloads the banner file if needed and available
        /// </summary>
        /// <param name="videoTags">VideoTags structure</param>
        /// <param name="bannerUrl">URL to Banner File</param>
        public static void DownloadBannerFile(VideoTags videoTags, string bannerUrl)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(videoTags.BannerFile))
                {
                    if ((!File.Exists(videoTags.BannerFile)) && (!String.IsNullOrWhiteSpace(bannerUrl)))
                    {
                        Util.Internet.WGet(bannerUrl, videoTags.BannerFile);
                        if (File.Exists(videoTags.BannerFile))
                            videoTags.BannerURL = bannerUrl;
                    }
                    else if (!String.IsNullOrWhiteSpace(bannerUrl) && String.IsNullOrWhiteSpace(videoTags.BannerURL))
                        videoTags.BannerURL = bannerUrl;
                }
            }
            catch (Exception)
            { }
        }
    }
}
