using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Globalization;
using TMDbLib;
using TMDbLib.Client;
using TMDbLib.Objects.Changes;
using TMDbLib.Objects.Collections;
using TMDbLib.Objects.Companies;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Genres;
using TMDbLib.Objects.Lists;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using TMDbLib.Utilities;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.MetaData
{
    public static class TMDB
    {
        private const string MCEBUDDY_TMDB_APIKey = "5e48613cb827da54f11ab3c8e952bb54";

        static public bool DownloadMovieDetails(VideoTags videoTags, bool dontOverwriteTitle, Log jobLog)
        {
            // The new v3 database is accessed via the TMDbLib API's
            try
            {
                TMDbClient client = new TMDbClient(MCEBUDDY_TMDB_APIKey);
                // List<SearchMovie> movieSearch = new List<SearchMovie>();
        SearchContainer<SearchMovie> movieSearch = new SearchContainer<SearchMovie>();
                Movie movieMatch = null;

                // TODO: Add support for multiple language searches
                if (String.IsNullOrWhiteSpace(videoTags.imdbId)) // If dont' have a specific movieId specified, look up the movie details
                {
                    if (videoTags.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) // Release date narrow down
                    {
                        // The information is stored on the server using the network timezone
                        // So we assume that the show being converted was recorded locally and is converted locally so the timezones match
                        DateTime dt = videoTags.OriginalBroadcastDateTime.ToLocalTime();
                        movieSearch = client.SearchMovieAsync(videoTags.Title.Trim().ToLower(), 0, true, dt.Year).Result;
          }
                    else // Title Check
                        movieSearch = client.SearchMovieAsync(videoTags.Title.Trim().ToLower(), 0, true, 0).Result;
                }
                else // Specific ID
                {
                    movieMatch = client.GetMovieAsync(videoTags.imdbId).Result; // We have a specific movie to work with
                }

                if (movieMatch == null) // If we haven't forced a movie match
                {
                    foreach (SearchMovie movieResult in movieSearch.Results) // Cycle through all possible combinations
                    {
                        Movie movie = client.GetMovieAsync(movieResult.Id).Result;
                        List<AlternativeTitle> akaValues = null;
                        if (movie.AlternativeTitles != null)
                            akaValues = movie.AlternativeTitles.Titles;
                        bool akaMatch = false;
                        string title = videoTags.Title;
                        if (akaValues != null) // Check if there are any AKA names to match
                            akaMatch = akaValues.Any(s => (String.Compare(s.Title.Trim(), title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) == 0 ? true : false));

                        // Get and match Movie name (check both titles and aka values)
                        if (String.Compare(movie.Title.Trim(), videoTags.Title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) != 0) // ignore white space and special characters
                            if (String.Compare(movie.OriginalTitle.Trim(), videoTags.Title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) != 0) // ignore white space and special characters
                                if (!akaMatch) // check for aka value matches
                                    continue; // No match in name

                        // If we got here, then we found a match
                        movieMatch = movie;
                        break; // We are done here
                    }
                }

                if (movieMatch != null) // We have a match
                {
                    if (!String.IsNullOrWhiteSpace(videoTags.imdbId) && !dontOverwriteTitle) // Match names only if the movie imdb id is not forced, else take what is returned by moviedb
                        videoTags.Title = movieMatch.Title; // Take what is forced for the imdb movie id

                    // Get Movie Id
                    videoTags.tmdbId = movieMatch.Id.ToString();

                    videoTags.IsMovie = true; // this is a movie

                    // Get Overview
                    string overview = movieMatch.Overview;
                    if (!String.IsNullOrWhiteSpace(overview) && String.IsNullOrWhiteSpace(videoTags.Description))
                        videoTags.Description = overview;

                    // Get original release date
                    if (movieMatch.ReleaseDate != null)
                    {
                        DateTime releaseDate = (DateTime)movieMatch.ReleaseDate;
                        if (releaseDate > GlobalDefs.NO_BROADCAST_TIME)
                            if ((videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.OriginalBroadcastDateTime.Date > releaseDate.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use MovieDB dates, MovieDB Dates are more reliable than video metadata usually
                                videoTags.OriginalBroadcastDateTime = releaseDate; // MovieDB stores time in network (local) timezone
                    }

                    // Get Genres
                    List<string> genres = new List<string>();
                    foreach (Genre genre in movieMatch.Genres)
                    {
                        genres.Add(genre.Name);
                    }
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

                    // Download the banner file
                    client.GetConfig(); // First we need to get the config
                    VideoMetaData.DownloadBannerFile(videoTags, client.GetImageUrl("original", movieMatch.PosterPath).OriginalString); // Get bannerfile

                    return true; // home free, we're good
                }

                jobLog.WriteEntry("No match found in TMDB", Log.LogEntryType.Debug);

                return false;
            }
            catch (Exception e)
            {
                jobLog.WriteEntry("Unable to connect to TMDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }
        }

        static public bool DownloadSeriesDetails(VideoTags videoTags, bool prioritizeMatchDate, bool dontOverwriteTitle, Log jobLog)
        {
            // The new v3 database is accessed via the TMDbLib API's
            try
            {
                TMDbClient client = new TMDbClient(MCEBUDDY_TMDB_APIKey);
                List<SearchTv> showSearch = new List<SearchTv>();

                // TODO: Add support for multiple language searches
                if (String.IsNullOrWhiteSpace(videoTags.tmdbId)) // If dont' have a specific movieId specified, look up the show details
                {
                    if (videoTags.OriginalBroadcastDateTime > GlobalDefs.NO_BROADCAST_TIME) // Release date narrow down
                    {
                        // The information is stored on the server using the network timezone
                        // So we assume that the show being converted was recorded locally and is converted locally so the timezones match
                        DateTime dt = videoTags.OriginalBroadcastDateTime.ToLocalTime();
                        showSearch = client.SearchTvShowAsync(videoTags.Title.Trim().ToLower(), 0).Result.Results;
                    }
                    else // Title Check
                        showSearch = client.SearchTvShowAsync(videoTags.Title.Trim().ToLower(), 0).Result.Results;
                }
                else // Specific ID
                {
                    TvShow showMatch = client.GetTvShowAsync(int.Parse(videoTags.tmdbId)).Result; // We have a specific show to work with

                    // First match by Episode name and then by Original broadcast date (by default prioritize match date is false)
                    if (!MatchSeriesInformation(client, videoTags, showMatch, prioritizeMatchDate, dontOverwriteTitle, jobLog))
                        return MatchSeriesInformation(client, videoTags, showMatch, !prioritizeMatchDate, dontOverwriteTitle, jobLog);
                    else
                        return true;
                }

                foreach (SearchTv showResult in showSearch) // Cycle through all possible combinations
                {
                    TvShow show = client.GetTvShowAsync(showResult.Id).Result;
                    string title = videoTags.Title;

                    // Get and match Show name (check both titles and aka values)
                    if (String.Compare(show.Name.Trim(), videoTags.Title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) != 0) // ignore white space and special characters
                        if (String.Compare(show.OriginalName.Trim(), videoTags.Title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) != 0) // ignore white space and special characters
                            continue; // No match in name

                    // If we got here, then we found a match
                    // First match by Episode name and then by Original broadcast date (by default prioritize match date is false)
                    if (!MatchSeriesInformation(client, videoTags, show, prioritizeMatchDate, dontOverwriteTitle, jobLog))
                    {
                        if (MatchSeriesInformation(client, videoTags, show, !prioritizeMatchDate, dontOverwriteTitle, jobLog))
                            return true;

                        // Else we continue looping through the returned series looking for a match if nothing matches
                    }
                    else
                        return true;
                }

                jobLog.WriteEntry("No match found on TMDB Series", Log.LogEntryType.Debug);

                return false;
            }
            catch (Exception e)
            {
                jobLog.WriteEntry("Unable to connect to TMDB\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }
        }

        /// <summary>
        /// Match and download the series information
        /// </summary>
        /// <param name="videoTags">Video tags</param>
        /// <param name="tvShow">TVDB Series ID</param>
        /// <param name="matchByAirDate">True to match by original broadcast date, false to match by episode name</param>
        /// <param name="dontOverwriteTitle">True if the title has been manually corrected and not to be overwritten</param>
        /// <returns></returns>
        private static bool MatchSeriesInformation(TMDbClient client, VideoTags videoTags, TvShow tvShow, bool matchByAirDate, bool dontOverwriteTitle, Log jobLog)
        {
            if (matchByAirDate) // User requested priority
                // Match by original broadcast date
                return MatchByBroadcastTime(client, videoTags, tvShow, dontOverwriteTitle, jobLog);
            else
                // Match by Episode name
                return MatchByEpisodeName(client, videoTags, tvShow, jobLog);
        }
        
        /// <summary>
        /// Match the series information by Original Broadcast date
        /// </summary>
        private static bool MatchByBroadcastTime(TMDbClient client, VideoTags videoTags, TvShow tvShow, bool dontOverwriteTitle, Log jobLog)
        {
            if (videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME)
            {
                jobLog.WriteEntry("Invalid original broadcast date to match", Log.LogEntryType.Debug);
                return false; //Nothing to match here
            }


            // Cycle through all Seasons and Episodes looking for a match
            for (int sNo = 0; sNo <= tvShow.NumberOfSeasons; sNo++)
            {
                TvSeason season = client.GetTvSeasonAsync(tvShow.Id, sNo).Result;
                if (season == null || season.Episodes == null)
                    continue;

                for (int eNo = 0; eNo <= season.Episodes.Count; eNo++)
                {
                    TvEpisode episode = client.GetTvEpisodeAsync(tvShow.Id, sNo, eNo).Result;
                    if (episode == null)
                        continue;

                    string episodeName = episode.Name;

                    if (!String.IsNullOrWhiteSpace(episodeName))
                    {
                        DateTime firstAired = (DateTime)episode.AirDate;
                        if (firstAired == null || firstAired <= GlobalDefs.NO_BROADCAST_TIME)
                            continue;

                        if ((firstAired.Date == videoTags.OriginalBroadcastDateTime.ToLocalTime().Date) || // TMDB only reports the date not the time
                            (firstAired.Date == videoTags.OriginalBroadcastDateTime.ToUniversalTime().Date))
                        {
                            int episodeNo = episode.EpisodeNumber;
                            int seasonNo = season.SeasonNumber;
                            string episodeOverview = episode.Overview;
                            string overview = season.Overview;
                            string tvdbID = (episode.ExternalIds != null ? (episode.ExternalIds.TvdbId != null ? episode.ExternalIds.TvdbId.ToString() : "") : "");
                            string imdbID = (episode.ExternalIds != null ? episode.ExternalIds.ImdbId : "");
                            string tmdbID = (tvShow.Id != 0 ? tvShow.Id.ToString() : "");
                            string network = (tvShow.Networks != null ? String.Join(";", tvShow.Networks.Select(s => s.Name)) : "");
                            DateTime premiereDate = GlobalDefs.NO_BROADCAST_TIME;
                            if (tvShow.FirstAirDate != null)
                                premiereDate = (DateTime)tvShow.FirstAirDate;
                            List<string> genres = (tvShow.Genres != null ? tvShow.Genres.Select(s => s.Name).ToList() : new List<string>());
                            string mediaCredits = (tvShow.Credits != null ? ((tvShow.Credits.Cast != null) ? String.Join(";", tvShow.Credits.Cast.Select(s => s.Name)) : "") : "");
                            string seriesName = tvShow.Name;

                            client.GetConfig(); // First we need to get the config
                            VideoMetaData.DownloadBannerFile(videoTags, client.GetImageUrl("original", tvShow.PosterPath).OriginalString); // Get bannerfile

                            // TODO: At what point do we go from supplementing to being primary?
                            // Since TVDB is primary we only supplement data
                            if ((episodeNo != 0) && (videoTags.Episode == 0)) videoTags.Episode = episodeNo;
                            if ((seasonNo != 0) && (videoTags.Season == 0)) videoTags.Season = seasonNo;
                            if (!String.IsNullOrWhiteSpace(seriesName) && String.IsNullOrWhiteSpace(videoTags.Title) && !dontOverwriteTitle) videoTags.Title = seriesName;
                            if (!String.IsNullOrWhiteSpace(episodeName) && String.IsNullOrWhiteSpace(videoTags.SubTitle)) videoTags.SubTitle = episodeName;
                            if (!String.IsNullOrWhiteSpace(episodeOverview) && String.IsNullOrWhiteSpace(videoTags.Description)) videoTags.Description = episodeOverview;
                            else if (!String.IsNullOrWhiteSpace(overview) && String.IsNullOrWhiteSpace(videoTags.Description)) videoTags.Description = overview;
                            if (!String.IsNullOrWhiteSpace(tvdbID) && String.IsNullOrWhiteSpace(videoTags.tvdbId)) videoTags.tvdbId = tvdbID;
                            if (!String.IsNullOrWhiteSpace(imdbID) && String.IsNullOrWhiteSpace(videoTags.imdbId)) videoTags.imdbId = imdbID;
                            if (!String.IsNullOrWhiteSpace(tmdbID) && String.IsNullOrWhiteSpace(videoTags.tmdbId)) videoTags.tmdbId = tmdbID;
                            if (!String.IsNullOrWhiteSpace(network) && String.IsNullOrWhiteSpace(videoTags.Network)) videoTags.Network = network;
                            if (!String.IsNullOrWhiteSpace(mediaCredits) && String.IsNullOrWhiteSpace(videoTags.MediaCredits)) videoTags.MediaCredits = mediaCredits;
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
            }

            jobLog.WriteEntry("No match found for broadcast date on TMDB", Log.LogEntryType.Debug);

            return false; // nothing matches
        }

        /// <summary>
        /// Match the series information by Episode name
        /// </summary>
        private static bool MatchByEpisodeName(TMDbClient client, VideoTags videoTags, TvShow tvShow, Log jobLog)
        {
            if (String.IsNullOrWhiteSpace(videoTags.SubTitle))
            {
                jobLog.WriteEntry("Invalid episode name to match", Log.LogEntryType.Debug);
                return false; //Nothing to match here
            }


            // Cycle through all Seasons and Episodes looking for a match
            for (int sNo = 0; sNo <= tvShow.NumberOfSeasons; sNo++)
            {
                TvSeason season = client.GetTvSeasonAsync(tvShow.Id, sNo).Result;
                if (season == null || season.Episodes == null)
                    continue;

                for (int eNo = 0; eNo <= season.Episodes.Count; eNo++)
                {
                    TvEpisode episode = client.GetTvEpisodeAsync(tvShow.Id, sNo, eNo).Result;
                    if (episode == null)
                        continue;

                    string episodeName = episode.Name;

                    if (!String.IsNullOrWhiteSpace(episodeName))
                    {
                        if (String.Compare(videoTags.SubTitle.Trim(), episodeName.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) == 0) // Compare the episode names (case / special characters / whitespace can change very often)
                        {
                            int episodeNo = episode.EpisodeNumber;
                            int seasonNo = season.SeasonNumber;
                            string episodeOverview = episode.Overview;
                            string overview = season.Overview;
                            string tvdbID = (episode.ExternalIds != null ? (episode.ExternalIds.TvdbId != null ? episode.ExternalIds.TvdbId.ToString() : "") : "");
                            string imdbID = (episode.ExternalIds != null ? episode.ExternalIds.ImdbId : "");
                            string tmdbID = (tvShow.Id != 0 ? tvShow.Id.ToString() : "");
                            string network = (tvShow.Networks != null ? String.Join(";", tvShow.Networks.Select(s => s.Name)) : "");
                            DateTime premiereDate = GlobalDefs.NO_BROADCAST_TIME;
                            if (tvShow.FirstAirDate != null)
                                premiereDate = (DateTime)tvShow.FirstAirDate;
                            List<string> genres = (tvShow.Genres != null ? tvShow.Genres.Select(s => s.Name).ToList() : new List<string>());
                            DateTime firstAired = (DateTime)(episode.AirDate != null ? episode.AirDate : GlobalDefs.NO_BROADCAST_TIME);
                            string mediaCredits = (tvShow.Credits != null ? ((tvShow.Credits.Cast != null) ? String.Join(";", tvShow.Credits.Cast.Select(s => s.Name)) : "") : "");

                            client.GetConfig(); // First we need to get the config
                            VideoMetaData.DownloadBannerFile(videoTags, client.GetImageUrl("original", tvShow.PosterPath).OriginalString); // Get bannerfile

                            if ((episodeNo != 0) && (videoTags.Episode == 0)) videoTags.Episode = episodeNo;
                            if ((seasonNo != 0) && (videoTags.Season == 0)) videoTags.Season = seasonNo;
                            if (!String.IsNullOrWhiteSpace(episodeOverview) && String.IsNullOrWhiteSpace(videoTags.Description)) videoTags.Description = episodeOverview;
                            else if (!String.IsNullOrWhiteSpace(overview) && (String.IsNullOrWhiteSpace(videoTags.Description))) videoTags.Description = overview;
                            if (!String.IsNullOrWhiteSpace(tvdbID) && String.IsNullOrWhiteSpace(videoTags.tvdbId)) videoTags.tvdbId = tvdbID;
                            if (!String.IsNullOrWhiteSpace(imdbID) && String.IsNullOrWhiteSpace(videoTags.imdbId)) videoTags.imdbId = imdbID;
                            if (!String.IsNullOrWhiteSpace(tmdbID) && String.IsNullOrWhiteSpace(videoTags.tmdbId)) videoTags.tmdbId = tmdbID;
                            if (!String.IsNullOrWhiteSpace(network) && String.IsNullOrWhiteSpace(videoTags.Network)) videoTags.Network = network;
                            if (!String.IsNullOrWhiteSpace(mediaCredits) && String.IsNullOrWhiteSpace(videoTags.MediaCredits)) videoTags.MediaCredits = mediaCredits;
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

                            if (firstAired > GlobalDefs.NO_BROADCAST_TIME)
                                if ((videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.OriginalBroadcastDateTime.Date > firstAired.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use TVDB dates, TVDB Dates are more reliable than video metadata usually
                                    videoTags.OriginalBroadcastDateTime = firstAired; // TVDB stores time in network (local) timezone

                            return true; // Found a match got all the data, we're done here
                        }
                    }
                }
            }

            jobLog.WriteEntry("No match found by episode name on TMDB", Log.LogEntryType.Debug);

            return false; // nothing matches
        }
    }
}
