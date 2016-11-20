using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Globalization;
using System.Net;
using Newtonsoft.Json;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.MetaData
{
    class MyApiFilms
    {
        public class Episode
        {
            public string date { get; set; }
            public int episode { get; set; }
            public string idIMDB { get; set; }
            public string plot { get; set; }
            public string title { get; set; }
            public string urlPoster { get; set; }
        }
        public class Season
        {
            public List<Episode> episodes { get; set; }
            public int numSeason { get; set; }
        }
        public class Writer
        {
            public string name { get; set; }
            public string nameId { get; set; }
        }
        public class Actor
        {
            public string actorId { get; set; }
            public string actorName { get; set; }
            public string character { get; set; }
            public string urlCharacter { get; set; }
            public string urlPhoto { get; set; }
            public string urlProfile { get; set; }
        }
        public class Aka
        {
            public string country { get; set; }
            public string title { get; set; }
        }
        public class Director
        {
            public string name { get; set; }
            public string nameId { get; set; }
        }

        /// <summary>
        /// Search results by title or IMDBId
        /// </summary>
        public class SearchResults
        {
            public List<Actor> actors { get; set; }
            public List<Aka> akas { get; set; }
            public List<object> countries { get; set; }
            public List<object> directors { get; set; }
            public List<object> filmingLocations { get; set; }
            public List<string> genres { get; set; }
            public string idIMDB { get; set; }
            public List<object> languages { get; set; }
            public string metascore { get; set; }
            public string plot { get; set; }
            public string rated { get; set; }
            public string rating { get; set; }
            public string releaseDate { get; set; }
            public List<object> runtime { get; set; }
            public string simplePlot { get; set; }
            public string title { get; set; }
            public string urlIMDB { get; set; }
            public string urlPoster { get; set; }
            public List<object> writers { get; set; }
            public string year { get; set; }
            public List<Season> seasons { get; set; }
        }
    }

    class OMDBApi
    {
        /// <summary>
        /// Results for a search by IMDB Id or Title (which returns a list)
        /// </summary>
        public class SearchResult
        {
            public string Title { get; set; }
            public string Year { get; set; }
            public string Rated { get; set; }
            public string Released { get; set; }
            public string Runtime { get; set; }
            public string Genre { get; set; }
            public string Director { get; set; }
            public string Writer { get; set; }
            public string Actors { get; set; }
            public string Plot { get; set; }
            public string Language { get; set; }
            public string Country { get; set; }
            public string Awards { get; set; }
            public string Poster { get; set; }
            public string Metascore { get; set; }
            public string imdbRating { get; set; }
            public string imdbVotes { get; set; }
            public string imdbID { get; set; }
            public string Type { get; set; }
            public string Response { get; set; }
        }
    }

    public static class IMDB
    {
        private const int MY_API_SEARCH_LIMIT = 10; // Max number of results that can be returned in a single search

        /// <summary>
        /// Downloads the information for a movie or series episode (no matching) given the IMDB ID for the movie or episode (not show)
        /// Uses OMDBApi
        /// </summary>
        /// <param name="videoTags">Video Tags structure with the IMDB ID</param>
        /// <returns>True if successful</returns>
        static public bool BootStrapByIMDBId(VideoTags videoTags, Log jobLog)
        {
            try
            {
                OMDBApi.SearchResult result = new OMDBApi.SearchResult();

                if (String.IsNullOrWhiteSpace(videoTags.imdbId)) // do we have a valid ID to begin with
                    return false;

                try
                {
                    WebClient client = new WebClient();
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)");
                    string response = client.DownloadString(new Uri("http://www.omdbapi.com/?i=" + videoTags.imdbId + "&r=json"));

                    result = JsonConvert.DeserializeObject<OMDBApi.SearchResult>(response);
                }
                catch (Exception e)
                {
                    jobLog.WriteEntry("Unable to bootstrap from IMDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                    return false; // invalid JSON string
                }

                if (String.IsNullOrWhiteSpace(result.Title)) // Check if got a valid result
                {
                    jobLog.WriteEntry("Unable to boot strap, IMDB returned empty Title", Log.LogEntryType.Debug);
                    return false;
                }

                // Check if it's a movie
                if (result.Type.ToLower().Contains("movie"))
                {
                    videoTags.Title = result.Title; // Take what is forced for the imdb movie id

                    videoTags.IsMovie = true;

                    // Get Overview
                    string overview = result.Plot;
                    if (!String.IsNullOrWhiteSpace(overview) && String.IsNullOrWhiteSpace(videoTags.Description))
                        videoTags.Description = overview;

                    // Get original release date
                    DateTime releaseDate = GlobalDefs.NO_BROADCAST_TIME;
                    if (DateTime.TryParseExact(result.Released, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate))
                    {
                        if (releaseDate > GlobalDefs.NO_BROADCAST_TIME)
                            if ((videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.OriginalBroadcastDateTime.Date > releaseDate.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use IMDB dates, IMDB Dates are more reliable than video metadata usually
                                videoTags.OriginalBroadcastDateTime = releaseDate; // IMDB stores time in network (local) timezone
                    }

                    string[] genres = result.Genre.Split(','); // Get Genres
                    if (genres != null)
                    {
                        if (genres.Length > 0)
                        {
                            if (videoTags.Genres != null)
                            {
                                if (videoTags.Genres.Length == 0)
                                    videoTags.Genres = genres;
                            }
                            else
                                videoTags.Genres = genres;
                        }
                    }

                    // Download the banner file
                    VideoMetaData.DownloadBannerFile(videoTags, result.Poster); // Get bannerfile

                    return true; // We found it, get out home free
                }
                else // Process as a series
                {
                    DateTime seriesPremiereDate = GlobalDefs.NO_BROADCAST_TIME;
                    DateTime.TryParseExact(result.Released, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out seriesPremiereDate);

                    string episodeName = result.Title; // Title here is the episode name since we are forcing a IMDB ID leading directly to a episode (the Showname is taken from the filename or metadata)
                    string bannerUrl = result.Poster; // Get Poster URL
                    string[] genres = result.Genre.Split(','); // Get Genres
                    string episodeOverview = result.Plot; // Get Overview

                    if (!String.IsNullOrWhiteSpace(episodeName) && String.IsNullOrWhiteSpace(videoTags.SubTitle)) videoTags.SubTitle = episodeName;
                    if (!String.IsNullOrWhiteSpace(episodeOverview) && String.IsNullOrWhiteSpace(videoTags.Description)) videoTags.Description = episodeOverview;
                    else if (!String.IsNullOrWhiteSpace(episodeOverview) && (String.IsNullOrWhiteSpace(videoTags.Description))) videoTags.Description = episodeOverview;
                    if (seriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME)
                        if ((videoTags.SeriesPremiereDate <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.SeriesPremiereDate.Date > seriesPremiereDate.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use IMDB dates, IMDB Dates are more reliable than video metadata usually
                            videoTags.SeriesPremiereDate = seriesPremiereDate; // IMDB stores time in network (local) timezone

                    if (genres != null)
                    {
                        if (genres.Length > 0)
                        {
                            if (videoTags.Genres != null)
                            {
                                if (videoTags.Genres.Length == 0)
                                    videoTags.Genres = genres;
                            }
                            else
                                videoTags.Genres = genres;
                        }
                    }

                    // Check if it's a sport series
                    if (videoTags.Genres != null)
                        if (videoTags.Genres.Length > 0)
                            if (videoTags.Genres.Contains("sport", StringComparer.OrdinalIgnoreCase))
                                videoTags.IsSports = true;

                    // Download the banner file
                    VideoMetaData.DownloadBannerFile(videoTags, bannerUrl); // Get bannerfile

                    return true; // Golden
                }
            }
            catch (Exception e)
            {
                jobLog.WriteEntry("Unable to use IMDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }
        }

        /// <summary>
        /// Supplements details by searching for a movie by title or IMDB ID
        /// Uses MyApiFilms
        /// </summary>
        /// <param name="videoTags">Video tags information to use and update</param>
        /// <param name="dontOverwriteTitle">True if the title has been manually corrected and not to be overwritten</param>
        /// <param name="offset">Initial search results offset</param>
        /// <returns></returns>
        static public bool DownloadMovieDetails(VideoTags videoTags, bool dontOverwriteTitle, Log jobLog, int offset = 0)
        {
            try
            {
                jobLog.WriteEntry("Searching IMDB Movie with result offset " + offset, Log.LogEntryType.Debug);

                List<MyApiFilms.SearchResults> searchResults = new List<MyApiFilms.SearchResults>();

                if (String.IsNullOrWhiteSpace(videoTags.imdbId)) // If dont' have a specific movieId specified, look up the movie details
                {
                    try
                    {
                        WebClient client = new WebClient();
                        client.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)");

                        // Look for matching title
                        string response = client.DownloadString(new Uri("http://www.myapifilms.com/imdb?title=" + videoTags.Title + "&format=JSON&aka=1&actors=S&limit=" + MY_API_SEARCH_LIMIT.ToString() + "&offset=" + offset.ToString()));
                        searchResults = JsonConvert.DeserializeObject<List<MyApiFilms.SearchResults>>(response);
                    }
                    catch (Exception e)
                    {
                        jobLog.WriteEntry("Unable to connect to IMDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                        return false; // invalid JSON string
                    }
                }
                else
                {
                    // We have a series imdb id to match, use MyApiFilms to get the series details
                    try
                    {
                        WebClient client = new WebClient();
                        client.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)");
                        string response = client.DownloadString(new Uri("http://www.myapifilms.com/imdb?idIMDB=" + videoTags.imdbId + "&format=JSON&actors=S"));
                        searchResults.Add(JsonConvert.DeserializeObject<MyApiFilms.SearchResults>(response));
                    }
                    catch (Exception e)
                    {
                        jobLog.WriteEntry("Unable to connect to IMDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                        return false; // invalid JSON string
                    }
                }

                foreach (MyApiFilms.SearchResults movie in searchResults) // Cycle through all possible combinations
                {
                    // Get and match Movie name
                    string movieName = movie.title;
                    if (String.IsNullOrWhiteSpace(videoTags.imdbId)) // Match names only if the movie imdb id is not forced, else take what is returned by moviedb
                    {
                        List<MyApiFilms.Aka> akaValues = movie.akas; // check AKA names also - Also Known As (for language and localization)
                        string title = videoTags.Title;
                        bool akaMatch = false;
                        if (akaValues != null) // Check if there are any AKA names to match
                            akaMatch = movie.akas.Any(s => (String.Compare(s.title.Trim(), title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) == 0 ? true : false));

                        if ((String.Compare(movieName.Trim(), videoTags.Title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) != 0) && (!akaMatch)) // ignore white space and special characters - check for both, Title and AKA names looking for a match
                            continue; // No match in name or AKA

                        // Match year if available
                        if (videoTags.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) // If we have a time, we can try to narrow the search parameters
                        {
                            // The information is stored on the server using the network timezone
                            // So we assume that the show being converted was recorded locally and is converted locally so the timezones match
                            DateTime dt = videoTags.OriginalBroadcastDateTime.ToLocalTime();
                            if (movie.year.Trim() != dt.Year.ToString())
                                continue;
                        }

                        videoTags.imdbId = movie.idIMDB; // since IMDB movie is not forced, get it here
                    }
                    else if (!String.IsNullOrWhiteSpace(movieName) && !dontOverwriteTitle) // make sure there is actually something returned here otherwise use default title
                        videoTags.Title = movieName; // Take what is forced for the imdb movie id

                    videoTags.IsMovie = true;

                    // Get Overview
                    string overview = movie.simplePlot;
                    if (!String.IsNullOrWhiteSpace(overview) && String.IsNullOrWhiteSpace(videoTags.Description))
                        videoTags.Description = overview;

                    // Get original release date
                    DateTime releaseDate = GlobalDefs.NO_BROADCAST_TIME;
                    string released = movie.releaseDate;
                    if (DateTime.TryParseExact(released, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate))
                    {
                        if (releaseDate > GlobalDefs.NO_BROADCAST_TIME)
                            if ((videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.OriginalBroadcastDateTime.Date > releaseDate.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use IMDB dates, IMDB Dates are more reliable than video metadata usually
                                videoTags.OriginalBroadcastDateTime = releaseDate; // IMDB stores time in network (local) timezone
                    }

                    string[] genres = (movie.genres == null ? null : movie.genres.ToArray()); // Get Genres
                    if (genres != null)
                    {
                        if (genres.Length > 0)
                        {
                            if (videoTags.Genres != null)
                            {
                                if (videoTags.Genres.Length == 0)
                                    videoTags.Genres = genres;
                            }
                            else
                                videoTags.Genres = genres;
                        }
                    }

                    if (String.IsNullOrWhiteSpace(videoTags.MediaCredits)) // Get the media credits
                        videoTags.MediaCredits = ((movie.actors != null) ? String.Join(";", movie.actors.Select(s => s.actorName)) : "");

                    if (String.IsNullOrWhiteSpace(videoTags.Rating)) // Get the ratings
                        videoTags.Rating = movie.rated;

                    // Download the banner file
                    VideoMetaData.DownloadBannerFile(videoTags, movie.urlPoster); // Get bannerfile

                    return true; // We found it, get out home free
                }

                // Check if we have reached the limit for the results (currently 10 is the max returned in a single query), if so then check the next set of results
                if (searchResults.Count == MY_API_SEARCH_LIMIT)
                    return DownloadMovieDetails(videoTags, dontOverwriteTitle, jobLog, offset + MY_API_SEARCH_LIMIT);

                jobLog.WriteEntry("No match found on IMDB", Log.LogEntryType.Debug);
                return false;
            }
            catch (Exception e)
            {
                jobLog.WriteEntry("Unable to initialize IMDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }
        }

        /// <summary>
        /// Supplements details by searching for a series show by title or IMDB ID
        /// Uses MyApiFilms
        /// </summary>
        /// <param name="videoTags">Video tags information to use and update</param>
        /// <param name="prioritizeMatchDate">If true, First match by airDate and then by Episode name and vice versa</param>
        /// <param name="dontOverwriteTitle">True if the title has been manually corrected and not to be overwritten</param>
        /// <returns>True if successful</returns>
        static public bool DownloadSeriesDetails(VideoTags videoTags, bool prioritizeMatchDate, bool dontOverwriteTitle, Log jobLog)
        {
            if (!DownloadSeriesDetails(prioritizeMatchDate, videoTags, dontOverwriteTitle, jobLog)) // First try to match by Episode Name (by default prioritize match date is false)
                return (DownloadSeriesDetails(!prioritizeMatchDate, videoTags, dontOverwriteTitle, jobLog)); // Other try to match by Original Air Date (since multiple shows can be aired on the same date) (by default prioritize match date is false)
            else
                return true; // We were successful
        }

        /// <summary>
        /// Supplements details by searching for a series show by title or IMDB ID
        /// Uses MyApiFilms
        /// </summary>
        /// <param name="matchByAirDate">If true, First match by airDate and then by Episode name and vice versa</param>
        /// <param name="videoTags">Video tags information to use and update</param>
        /// <param name="dontOverwriteTitle">True if the title has been manually corrected and not to be overwritten</param>
        /// <param name="offset">Initial search results offset</param>
        /// <returns>True if successful</returns>
        static private bool DownloadSeriesDetails(bool matchByAirDate, VideoTags videoTags, bool dontOverwriteTitle, Log jobLog, int offset = 0)
        {
            try
            {
                if (matchByAirDate && (videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME))
                {
                    jobLog.WriteEntry("Invalud original broadcast date to match on IMDB", Log.LogEntryType.Debug);
                    return false; // We can only match by airdate if there is something to match against (otherwise we get false positives)
                }
                else if (!matchByAirDate && String.IsNullOrWhiteSpace(videoTags.SubTitle))
                {
                    jobLog.WriteEntry("Invalid episode name to match on IMDB", Log.LogEntryType.Debug);
                    return false; //Nothing to match here
                }

                jobLog.WriteEntry("Searching IMDB Series with result offset " + offset, Log.LogEntryType.Debug);

                List<MyApiFilms.SearchResults> searchResults = new List<MyApiFilms.SearchResults>();

                if (String.IsNullOrWhiteSpace(videoTags.imdbId)) // If dont' have a specific imdb id specified, look up the series details
                {
                    try
                    {
                        WebClient client = new WebClient();
                        client.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)");

                        // Look for matching title
                        string response = client.DownloadString(new Uri("http://www.myapifilms.com/imdb?title=" + videoTags.Title + "&format=JSON&aka=1&actors=S&seasons=1&limit=" + MY_API_SEARCH_LIMIT.ToString() + "&offset=" + offset.ToString()));
                        searchResults = JsonConvert.DeserializeObject<List<MyApiFilms.SearchResults>>(response);
                    }
                    catch (Exception e)
                    {
                        jobLog.WriteEntry("Unable to connect to IMDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                        return false; // invalid JSON string
                    }
                }
                else
                {
                    // We have a series imdb id to match, use MyApiFilms to get the series details
                    try
                    {
                        WebClient client = new WebClient();
                        client.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)");
                        string response = client.DownloadString(new Uri("http://www.myapifilms.com/imdb?idIMDB=" + videoTags.imdbId + "&format=JSON&seasons=1&actors=S"));
                        searchResults.Add(JsonConvert.DeserializeObject<MyApiFilms.SearchResults>(response));
                    }
                    catch (Exception e)
                    {
                        jobLog.WriteEntry("Unable to connect to IMDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                        return false; // invalid JSON string
                    }
                }

                foreach (MyApiFilms.SearchResults show in searchResults) // Cycle through all possible combinations
                {
                    // Get and match the Series name
                    string seriesName = show.title;

                    if (String.IsNullOrWhiteSpace(videoTags.imdbId)) // Match names only if the movie imdb id is not forced, else take what is returned by moviedb
                    {
                        List<MyApiFilms.Aka> akaValues = show.akas; // check AKA names also - Also Known As (for language and localization)
                        string title = videoTags.Title;
                        bool akaMatch = false;
                        if (akaValues != null) // Check if there are any AKA names to match
                            akaMatch = show.akas.Any(s => (String.Compare(s.title.Trim(), title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) == 0 ? true : false));

                        if ((String.Compare(seriesName.Trim(), videoTags.Title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) != 0) && (!akaMatch)) // ignore white space and special characters - check for both, Title and AKA names looking for a match
                            continue; // No match in name or AKA
                    }
                    else if (!String.IsNullOrWhiteSpace(seriesName) && !dontOverwriteTitle) // make sure there is actually something returned here otherwise use default title
                        videoTags.Title = seriesName; // Take what is forced for the imdb id

                    DateTime seriesPremiereDate = GlobalDefs.NO_BROADCAST_TIME;
                    DateTime.TryParseExact(show.releaseDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out seriesPremiereDate);

                    // Look for the right Episode
                    if (show.seasons != null)
                    {
                        foreach (MyApiFilms.Season season in show.seasons)
                        {
                            if (season.episodes != null)
                            {
                                foreach (MyApiFilms.Episode episode in season.episodes)
                                {
                                    // Get the Episode name and match it
                                    string episodeName = episode.title;

                                    // Original broadcast date, some of them have an extra . in the date so get rid of it
                                    DateTime airDate = GlobalDefs.NO_BROADCAST_TIME;
                                    DateTime.TryParseExact(episode.date.Replace(".", "").Trim(), "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out airDate);

                                    if ((!matchByAirDate && (String.Compare(episodeName.Trim(), videoTags.SubTitle.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) == 0)) ||
                                        (matchByAirDate && (videoTags.OriginalBroadcastDateTime.ToLocalTime().Date == airDate.Date)) ||
                                        (matchByAirDate && (videoTags.OriginalBroadcastDateTime.ToUniversalTime().Date == airDate.Date)))
                                    {
                                        // Get Genre's
                                        string[] genres = (show.genres == null ? null : show.genres.ToArray()); // Get Genres

                                        // Get Overview
                                        string episodeOverview = episode.plot;

                                        // Episode
                                        int episodeNo = episode.episode;

                                        // Season
                                        int seasonNo = season.numSeason;

                                        // IMDB Movie Id
                                        string imdbID = show.idIMDB;

                                        // Home free - update all the info where required
                                        if (matchByAirDate) // If we came in matching the Original Air Date - then we overwrite the episode details
                                        {
                                            // TODO: For now we only update subtitle and description if it is missing since IMDB is not upto date on TV series information yet. This needs to be changed and force updated once IMDB is complete
                                            // if (!String.IsNullOrWhiteSpace(episodeName)) videoTags.SubTitle = episodeName; // Overwrite
                                            // if (!String.IsNullOrWhiteSpace(episodeOverview)) videoTags.Description = episodeOverview; // Overwrite
                                            if (!String.IsNullOrWhiteSpace(episodeName) && String.IsNullOrWhiteSpace(videoTags.SubTitle)) videoTags.SubTitle = episodeName;
                                            if (!String.IsNullOrWhiteSpace(episodeOverview) && String.IsNullOrWhiteSpace(videoTags.Description)) videoTags.Description = episodeOverview;
                                        }
                                        else // only update what's missing
                                        {
                                            if (!String.IsNullOrWhiteSpace(episodeName) && String.IsNullOrWhiteSpace(videoTags.SubTitle)) videoTags.SubTitle = episodeName;
                                            if (!String.IsNullOrWhiteSpace(episodeOverview) && String.IsNullOrWhiteSpace(videoTags.Description)) videoTags.Description = episodeOverview;
                                        }

                                        if ((episodeNo != 0) && (videoTags.Episode == 0)) videoTags.Episode = episodeNo;
                                        if ((seasonNo != 0) && (videoTags.Season == 0)) videoTags.Season = seasonNo;
                                        if (!String.IsNullOrWhiteSpace(imdbID) && String.IsNullOrWhiteSpace(videoTags.imdbId)) videoTags.imdbId = imdbID;
                                        if (seriesPremiereDate > GlobalDefs.NO_BROADCAST_TIME)
                                            if ((videoTags.SeriesPremiereDate <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.SeriesPremiereDate.Date > seriesPremiereDate.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use IMDB dates, IMDB Dates are more reliable than video metadata usually
                                                videoTags.SeriesPremiereDate = seriesPremiereDate; // IMDB stores time in network (local) timezone
                                        if (airDate > GlobalDefs.NO_BROADCAST_TIME)
                                            if ((videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.OriginalBroadcastDateTime.Date > airDate.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use IMDB dates, IMDB Dates are more reliable than video metadata usually
                                                videoTags.OriginalBroadcastDateTime = airDate; // IMDB stores time in network (local) timezone


                                        if (genres != null)
                                        {
                                            if (genres.Length > 0)
                                            {
                                                if (videoTags.Genres != null)
                                                {
                                                    if (videoTags.Genres.Length == 0)
                                                        videoTags.Genres = genres;
                                                }
                                                else
                                                    videoTags.Genres = genres;
                                            }
                                        }

                                        if (String.IsNullOrWhiteSpace(videoTags.MediaCredits)) // Get the media credits
                                            videoTags.MediaCredits = ((show.actors != null) ? String.Join(";", show.actors.Select(s => s.actorName)) : "");

                                        if (String.IsNullOrWhiteSpace(videoTags.Rating)) // Get the ratings
                                            videoTags.Rating = show.rated;

                                        // Download the banner file
                                        VideoMetaData.DownloadBannerFile(videoTags, show.urlPoster); // Get bannerfile

                                        return true; // Golden
                                    }
                                }
                            }
                        }
                    }
                }

                // Check if we have reached the limit for the results (currently 10 is the max returned in a single query), if so then check the next set of results
                if (searchResults.Count == MY_API_SEARCH_LIMIT)
                    return DownloadSeriesDetails(matchByAirDate, videoTags, dontOverwriteTitle, jobLog, offset + MY_API_SEARCH_LIMIT);

                jobLog.WriteEntry("No match found on IMDB Series", Log.LogEntryType.Debug);

                return false;
            }
            catch (Exception e)
            {
                jobLog.WriteEntry("Unable to initialize IMDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }
        }
    }
}
