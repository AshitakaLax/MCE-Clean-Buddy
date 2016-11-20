using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.Linq;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.MetaData
{
    public static class TVDB
    {
        private const string MCEBUDDY_THETVDB_API_KEY = "24BC47A0DF94324E";
        private static string[] THETVDB_SUPPORTED_LANGUAGES = { "en", "da", "fi", "nl", "de", "it","es", "fr","pl","hu","el","tr","ru","he","ja","pt","zh","cs","sl","hr","ko","sv","no"};

        public static bool DownloadSeriesDetails(VideoTags videoTags, bool prioritizeMatchDate, bool dontOverwriteTitle, Log jobLog)
        {
            XPathDocument Xp;
            XPathNavigator Nav;
            XPathExpression Exp;
            XPathNodeIterator Itr;

            // There are no TheTVDB mirrors, so skip that step

            // ******************
            // Get the series ID
            // ******************
            try
            {
                if (!String.IsNullOrWhiteSpace(videoTags.imdbId)) // If we have a specific IMDB movieId specified, look up the movie details on TVDB
                {
                    Xp = new XPathDocument("http://www.thetvdb.com/api/GetSeriesByRemoteID.php?imdbid=" + videoTags.imdbId);
                }
                else if (!String.IsNullOrWhiteSpace(videoTags.tvdbId)) // If we have a specific TVDB seriesId specified, look up the series details
                {
                    // First match by Episode name and then by Original broadcast date (by default prioritize match date is false)
                    if (!MatchSeriesInformation(videoTags, videoTags.tvdbId, prioritizeMatchDate, dontOverwriteTitle, jobLog))
                        return MatchSeriesInformation(videoTags, videoTags.tvdbId, !prioritizeMatchDate, dontOverwriteTitle, jobLog);
                    else
                        return true;
                }
                else // Generic search by name
                {
                    Xp = new XPathDocument("http://www.thetvdb.com/api/GetSeries.php?seriesname=" + videoTags.Title);
                }

                Nav = Xp.CreateNavigator();
                Exp = Nav.Compile("//Data/Series");
                Itr = Nav.Select(Exp);
            }
            catch (Exception e)
            {
                jobLog.WriteEntry("Unable to connect to TVDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }

            while (Itr.MoveNext()) // loop through all series returned trying to find a match
            {
                string seriesID = XML.GetXMLTagValue("seriesid", Itr.Current.OuterXml);
                string seriesTitle = XML.GetXMLTagValue("SeriesName", Itr.Current.OuterXml);
                string[] aliasNames = XML.GetXMLTagValue("AliasNames", Itr.Current.OuterXml).Split('|'); // sometimes the alias matches

                // Compare the series title with the title of the recording
                if ((String.Compare(seriesTitle.Trim(), videoTags.Title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) != 0) &&
                    (!aliasNames.Any(s => (String.Compare(s.Trim(), videoTags.Title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) == 0))))
                    continue; // Name mismatch

                if (String.IsNullOrWhiteSpace(seriesID))
                    continue; // can't do anything without seriesID

                // First match by Episode name and then by Original broadcast date (by default prioritize match date is false)
                if (!MatchSeriesInformation(videoTags, seriesID, prioritizeMatchDate, dontOverwriteTitle, jobLog))
                {
                    if (MatchSeriesInformation(videoTags, seriesID, !prioritizeMatchDate, dontOverwriteTitle, jobLog))
                        return true;

                    // Else we continue looping through the returned series looking for a match if nothing matches
                }
                else
                    return true;
            }

            jobLog.WriteEntry("No match found on TVDB", Log.LogEntryType.Debug);

            return false; // no match found
        }

        /// <summary>
        /// Match and download the series information
        /// </summary>
        /// <param name="videoTags">Video tags</param>
        /// <param name="seriesID">TVDB Series ID</param>
        /// <param name="matchByAirDate">True to match by original broadcast date, false to match by episode name</param>
        /// <param name="dontOverwriteTitle">True if the title has been manually corrected and not to be overwritten</param>
        /// <returns></returns>
        private static bool MatchSeriesInformation(VideoTags videoTags, string seriesID, bool matchByAirDate, bool dontOverwriteTitle, Log jobLog)
        {
            if (matchByAirDate) // User requested priority
                // Match by original broadcast date
                return MatchByBroadcastTime(videoTags, seriesID, dontOverwriteTitle, jobLog);
            else
                // Match by Episode name
                return MatchByEpisodeName(videoTags, seriesID, jobLog);
        }
        
        /// <summary>
        /// Match the series information by Original Broadcast date
        /// </summary>
        private static bool MatchByBroadcastTime(VideoTags videoTags, string seriesID, bool dontOverwriteTitle, Log jobLog)
        {
            // If we have no original broadcasttime 
            if (videoTags.OriginalBroadcastDateTime <= Globals.GlobalDefs.NO_BROADCAST_TIME)
            {
                jobLog.WriteEntry("No original broadcast date time", Log.LogEntryType.Debug);
                return false;
            }

            // **************************************
            // Get the series and episode information
            // **************************************
            string lang = Localise.TwoLetterISO();

            if (!((IList<string>)THETVDB_SUPPORTED_LANGUAGES).Contains(lang))
                lang = "en";

            string queryUrl = "http://www.thetvdb.com/api/" + MCEBUDDY_THETVDB_API_KEY + "/series/" + seriesID + "/all/" + lang + ".xml";
            XPathDocument XpS;
            XPathNavigator NavS;
            XPathExpression ExpS;
            XPathNodeIterator ItrS;
            string overview = "";
            string seriesName = "";
            string bannerUrl = "";
            string imdbID = "";
            string firstAiredStr = "";
            DateTime firstAired = GlobalDefs.NO_BROADCAST_TIME;
            int seasonNo = 0;
            int episodeNo = 0;
            string episodeName = "";
            string episodeOverview = "";
            string network = "";
            DateTime premiereDate = GlobalDefs.NO_BROADCAST_TIME;
            List<string> genres = new List<string>();

            try
            {
                // Get the Series information
                XpS = new XPathDocument(queryUrl);
                NavS = XpS.CreateNavigator();
                ExpS = NavS.Compile("//Data/Series"); // Series information
                ItrS = NavS.Select(ExpS);
                ItrS.MoveNext();
                seriesName = XML.GetXMLTagValue("SeriesName", ItrS.Current.OuterXml);
                overview = XML.GetXMLTagValue("Overview", ItrS.Current.OuterXml);
                if (String.IsNullOrWhiteSpace(bannerUrl = XML.GetXMLTagValue("poster", ItrS.Current.OuterXml))) // We get the poster first
                    if (String.IsNullOrWhiteSpace(bannerUrl = XML.GetXMLTagValue("fanart", ItrS.Current.OuterXml))) // We get the fanart next
                        bannerUrl = XML.GetXMLTagValue("banner", ItrS.Current.OuterXml); // We get the banner last
                imdbID = XML.GetXMLTagValue("IMDB_ID", ItrS.Current.OuterXml);
                network = XML.GetXMLTagValue("Network", ItrS.Current.OuterXml);
                DateTime.TryParse(XML.GetXMLTagValue("FirstAired", ItrS.Current.OuterXml), out premiereDate);
                string genreValue = XML.GetXMLTagValue("Genre", ItrS.Current.OuterXml);
                if (!String.IsNullOrWhiteSpace(genreValue))
                    foreach (string genre in genreValue.Split('|'))
                        if (!String.IsNullOrWhiteSpace(genre)) genres.Add(genre);

                // Get the Episode information
                XpS = new XPathDocument(queryUrl);
                NavS = XpS.CreateNavigator();
                ExpS = NavS.Compile("//Data/Episode");
                ItrS = NavS.Select(ExpS);
            }
            catch (Exception e)
            {
                jobLog.WriteEntry("Unable to navigate TVDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }

            while (ItrS.MoveNext())
            {
                firstAiredStr = XML.GetXMLTagValue("FirstAired", ItrS.Current.OuterXml);
                if (DateTime.TryParse(firstAiredStr, null, DateTimeStyles.AssumeLocal, out firstAired))
                {
                    if (firstAired <= GlobalDefs.NO_BROADCAST_TIME)
                        continue;

                    // The information is stored on the server using the network timezone
                    // So we assume that the show being converted was recorded locally and is converted locally so the timezones match
                    // Sometimes the timezones get mixed up so we check local time or universal time for a match
                    if ((firstAired.Date == videoTags.OriginalBroadcastDateTime.ToLocalTime().Date) || // TVDB only reports the date not the time
                        (firstAired.Date == videoTags.OriginalBroadcastDateTime.ToUniversalTime().Date))
                    {
                        episodeName = XML.GetXMLTagValue("EpisodeName", ItrS.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(episodeName))
                        {
                            jobLog.WriteEntry("Empty episode name", Log.LogEntryType.Debug);
                            return false; // WRONG series, if there is no name we're in the incorrect series (probably wrong country)
                        }

                        int.TryParse(XML.GetXMLTagValue("SeasonNumber", ItrS.Current.OuterXml), out seasonNo);
                        int.TryParse(XML.GetXMLTagValue("EpisodeNumber", ItrS.Current.OuterXml), out episodeNo);
                        episodeOverview = XML.GetXMLTagValue("Overview", ItrS.Current.OuterXml);
                            
                        // ********************
                        // Get the banner file
                        // ********************
                        VideoMetaData.DownloadBannerFile(videoTags, "http://www.thetvdb.com/banners/" + bannerUrl); // Get bannerfile

                        if ((episodeNo != 0) && (videoTags.Episode == 0)) videoTags.Episode = episodeNo;
                        if ((seasonNo != 0) && (videoTags.Season == 0)) videoTags.Season = seasonNo;
                        if (!String.IsNullOrWhiteSpace(seriesName) && !dontOverwriteTitle) videoTags.Title = seriesName; // Overwrite Series name since we matching by broadcast time and the name didn't match earlier so likely an issue with the name
                        if (!String.IsNullOrWhiteSpace(episodeName)) videoTags.SubTitle = episodeName; // Overwrite episode name, it didn't match earlier in match by episode name, so it's probably wrong on the metadata
                        if (!String.IsNullOrWhiteSpace(episodeOverview)) videoTags.Description = episodeOverview; // Overwrite
                            else if (!String.IsNullOrWhiteSpace(overview)) videoTags.Description = overview; // Overwrite
                        if (!String.IsNullOrWhiteSpace(seriesID) && String.IsNullOrWhiteSpace(videoTags.tvdbId)) videoTags.tvdbId = seriesID;
                        if (!String.IsNullOrWhiteSpace(imdbID) && String.IsNullOrWhiteSpace(videoTags.imdbId)) videoTags.imdbId = imdbID;
                        if (!String.IsNullOrWhiteSpace(network) && String.IsNullOrWhiteSpace(videoTags.Network)) videoTags.Network = network;
                        if (premiereDate > GlobalDefs.NO_BROADCAST_TIME)
                            if ((videoTags.SeriesPremiereDate <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.SeriesPremiereDate.Date > premiereDate.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use TVDB dates, TVDB Dates are more reliable than video metadata usually
                                videoTags.SeriesPremiereDate = premiereDate; // TVDB stores time in network (local) timezone
                        if (genres.Count > 0)
                        {
                            if (videoTags.Genres != null)
                            {
                                if (videoTags.Genres.Length == 0)
                                    videoTags.Genres = genres.ToArray();
                            }
                            else
                                videoTags.Genres = genres.ToArray();
                        }

                        return true; // Found a match got all the data, we're done here
                    }
                }
            }

            jobLog.WriteEntry("No match found on TVDB for language " + lang, Log.LogEntryType.Warning);

            return false;
        }

        /// <summary>
        /// Match the series information by Episode name
        /// </summary>
        private static bool MatchByEpisodeName(VideoTags videoTags, string seriesID, Log jobLog)
        {

            if (String.IsNullOrWhiteSpace(videoTags.SubTitle))
            {
                jobLog.WriteEntry("No episode name to match", Log.LogEntryType.Debug);
                return false; //Nothing to match here
            }

            // **************************************
            // Get the series and episode information
            // **************************************
            foreach (string lang in THETVDB_SUPPORTED_LANGUAGES) // Cycle through all languages looking for a match since people in different countries/locales could be viewing shows recorded in different languages
            {
                jobLog.WriteEntry("Looking for Episode name match on TVDB using language " + lang, Log.LogEntryType.Debug);

                string queryUrl = "http://www.thetvdb.com/api/" + MCEBUDDY_THETVDB_API_KEY + "/series/" + seriesID + "/all/" + lang + ".xml";
                XPathDocument XpS;
                XPathNavigator NavS;
                XPathExpression ExpS;
                XPathNodeIterator ItrS;
                string overview = "";
                string bannerUrl = "";
                string imdbID = "";
                List<String> genres = new List<string>();;
                int seasonNo = 0;
                int episodeNo = 0;
                string episodeName = "";
                string episodeOverview = "";
                string network = "";
                DateTime premiereDate = GlobalDefs.NO_BROADCAST_TIME;
                string firstAiredStr = "";
                DateTime firstAired = GlobalDefs.NO_BROADCAST_TIME;

                try
                {
                    // Get the Series information
                    XpS = new XPathDocument(queryUrl);
                    NavS = XpS.CreateNavigator();
                    ExpS = NavS.Compile("//Data/Series"); // Series information
                    ItrS = NavS.Select(ExpS);
                    ItrS.MoveNext();
                    overview = XML.GetXMLTagValue("Overview", ItrS.Current.OuterXml);
                    if (String.IsNullOrWhiteSpace(bannerUrl = XML.GetXMLTagValue("poster", ItrS.Current.OuterXml))) // We get the poster first
                        if (String.IsNullOrWhiteSpace(bannerUrl = XML.GetXMLTagValue("fanart", ItrS.Current.OuterXml))) // We get the fanart next
                            bannerUrl = XML.GetXMLTagValue("banner", ItrS.Current.OuterXml); // We get the banner last
                    imdbID = XML.GetXMLTagValue("IMDB_ID", ItrS.Current.OuterXml);
                    network = XML.GetXMLTagValue("Network", ItrS.Current.OuterXml);
                    DateTime.TryParse(XML.GetXMLTagValue("FirstAired", ItrS.Current.OuterXml), out premiereDate);
                    string genreValue = XML.GetXMLTagValue("Genre", ItrS.Current.OuterXml);
                    if (!String.IsNullOrWhiteSpace(genreValue))
                        foreach (string genre in genreValue.Split('|'))
                            if (!String.IsNullOrWhiteSpace(genre)) genres.Add(genre);

                    // Get the episode information
                    XpS = new XPathDocument(queryUrl);
                    NavS = XpS.CreateNavigator();
                    ExpS = NavS.Compile("//Data/Episode"); // Episode information
                    ItrS = NavS.Select(ExpS);
                }
                catch (Exception e)
                {
                    jobLog.WriteEntry("Unable to nagivate TMDB for language " + lang + "\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                    return false;
                }


                while (ItrS.MoveNext())
                {
                    episodeName = XML.GetXMLTagValue("EpisodeName", ItrS.Current.OuterXml);
                    if (!String.IsNullOrWhiteSpace(episodeName))
                    {
                        if (String.Compare(videoTags.SubTitle.Trim(), episodeName.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) == 0) // Compare the episode names (case / special characters / whitespace can change very often)
                        {
                            int.TryParse(XML.GetXMLTagValue("SeasonNumber", ItrS.Current.OuterXml), out seasonNo);
                            int.TryParse(XML.GetXMLTagValue("EpisodeNumber", ItrS.Current.OuterXml), out episodeNo);
                            episodeOverview = XML.GetXMLTagValue("Overview", ItrS.Current.OuterXml);

                            // ********************
                            // Get the banner file
                            // ********************
                            VideoMetaData.DownloadBannerFile(videoTags, "http://www.thetvdb.com/banners/" + bannerUrl); // Get bannerfile

                            if ((episodeNo != 0) && (videoTags.Episode == 0)) videoTags.Episode = episodeNo;
                            if ((seasonNo != 0) && (videoTags.Season == 0)) videoTags.Season = seasonNo;
                            if (!String.IsNullOrWhiteSpace(episodeOverview) && String.IsNullOrWhiteSpace(videoTags.Description)) videoTags.Description = episodeOverview;
                            else if (!String.IsNullOrWhiteSpace(overview) && (String.IsNullOrWhiteSpace(videoTags.Description))) videoTags.Description = overview;
                            if (!String.IsNullOrWhiteSpace(seriesID) && String.IsNullOrWhiteSpace(videoTags.tvdbId)) videoTags.tvdbId = seriesID;
                            if (!String.IsNullOrWhiteSpace(imdbID) && String.IsNullOrWhiteSpace(videoTags.imdbId)) videoTags.imdbId = imdbID;
                            if (!String.IsNullOrWhiteSpace(network) && String.IsNullOrWhiteSpace(videoTags.Network)) videoTags.Network = network;
                            if (premiereDate > GlobalDefs.NO_BROADCAST_TIME)
                                if ((videoTags.SeriesPremiereDate <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.SeriesPremiereDate.Date > premiereDate.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use TVDB dates, TVDB Dates are more reliable than video metadata usually
                                    videoTags.SeriesPremiereDate = premiereDate; // TVDB stores time in network (local) timezone
                            if (genres.Count > 0)
                            {
                                if (videoTags.Genres != null)
                                {
                                    if (videoTags.Genres.Length == 0)
                                        videoTags.Genres = genres.ToArray();
                                }
                                else
                                    videoTags.Genres = genres.ToArray();
                            }

                            firstAiredStr = XML.GetXMLTagValue("FirstAired", ItrS.Current.OuterXml);
                            if (DateTime.TryParse(firstAiredStr, null, DateTimeStyles.AssumeLocal, out firstAired))
                            {
                                if (firstAired > GlobalDefs.NO_BROADCAST_TIME)
                                    if ((videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.OriginalBroadcastDateTime.Date > firstAired.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use TVDB dates, TVDB Dates are more reliable than video metadata usually
                                        videoTags.OriginalBroadcastDateTime = firstAired; // TVDB stores time in network (local) timezone
                            }

                            return true; // Found a match got all the data, we're done here
                        }
                    }
                }
            }

            jobLog.WriteEntry("No match found on TVDB for Episode Name", Log.LogEntryType.Debug);

            return false;
        }
    }
}
