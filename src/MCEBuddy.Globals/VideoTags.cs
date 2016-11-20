using System;
using System.Collections.Generic;
using System.Text;
using MCEBuddy.Globals;

namespace MCEBuddy.Globals 
{
    public class VideoTags
    {
        public class SageTV
        {
            public string airingDbId = "";
            public string mediaFileDbId = "";

            public override string ToString()
            {
                string AllTags = "";
                AllTags += "SageTV FileID: " + airingDbId + "\r\n";
                AllTags += "SageTV MediaFileID: " + mediaFileDbId + "\r\n";
                return AllTags;
            }
        }

        public string Title = "";
        public string SubTitle = "";
        public string Description = "";
        public string Network = "";
        public string Rating = ""; // Parental rating
        public string MediaCredits = ""; // Media credits
        public string[] Genres = null;
        public int Season = 0;
        public int Episode = 0;
        public string BannerFile = "";
        public string BannerURL = ""; 
        public string imdbId = ""; // IMDB ID
        public string tmdbId = ""; // TMDB ID
        public string tvdbId = ""; // TVDB ID
        public bool IsMovie = false;
        public bool IsSports = false;
        public DateTime OriginalBroadcastDateTime = GlobalDefs.NO_BROADCAST_TIME;
        public DateTime RecordedDateTime = GlobalDefs.NO_BROADCAST_TIME;
        public DateTime SeriesPremiereDate = GlobalDefs.NO_BROADCAST_TIME;
        public bool CopyProtected = false;
        public SageTV sageTV = new SageTV();

        /// <summary>
        /// True if all the metadata that can be downloaded from the internet is already populated
        /// </summary>
        public bool DownloadPopulated
        {
            get
            {
                if (String.IsNullOrWhiteSpace(Title) ||
                    String.IsNullOrWhiteSpace(SubTitle) ||
                    String.IsNullOrWhiteSpace(Description) ||
                    String.IsNullOrWhiteSpace(Network) ||
                    String.IsNullOrWhiteSpace(Rating) ||
                    String.IsNullOrWhiteSpace(MediaCredits) ||
                    (Genres == null || Genres.Length == 0) ||
                    (Season == 0) ||
                    (Episode == 0) ||
                    String.IsNullOrWhiteSpace(BannerFile) ||
                    String.IsNullOrWhiteSpace(BannerURL) ||
                    String.IsNullOrWhiteSpace(imdbId) ||
                    String.IsNullOrWhiteSpace(tmdbId) ||
                    String.IsNullOrWhiteSpace(tvdbId) ||
                    (OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME) ||
                    (SeriesPremiereDate <= GlobalDefs.NO_BROADCAST_TIME)
                    // We don't check isMovie, isSports, RecordedDateTime, copyProtected and sageTV since these cannot be downloaded from the internet or not applicable
                    )
                    return false;
                else
                    return true;
            }
        }

        public override string ToString()
        {
            string AllTags = "";
            AllTags += "Title: " + Title + "\r\n";
            AllTags += "SubTitle: " + SubTitle + "\r\n";
            AllTags += "Description: " + Description + "\r\n";
            AllTags += "Network: " + Network + "\r\n";
            AllTags += "Parental Rating: " + Rating + "\r\n";
            AllTags += "Media Credits: " + MediaCredits + "\r\n";
            AllTags += "Genres: "; 
            if (Genres != null)
            {
                foreach (string Genre in Genres)
                {
                     AllTags += Genre + ",";
                }
                AllTags = AllTags.Substring(0, AllTags.Length - 1);
                AllTags += "\r\n";
            }
            AllTags += "Season: " + Season.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\r\n";
            AllTags += "Episode: " + Episode.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\r\n";
            AllTags += "Banner: " + BannerFile + "\r\n";
            AllTags += "Banner URL: " + BannerURL + "\r\n";
            AllTags += "IMDB Id: " + imdbId + "\r\n";
            AllTags += "MovieDB Id: " + tmdbId + "\r\n";
            AllTags += "TVDB Id: " + tvdbId + "\r\n";
            AllTags += "Is Show Movie: " + IsMovie.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\r\n";
            AllTags += "Is Show Sports: " + IsSports.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\r\n";
            AllTags += "OriginalBroadcastDateTime: " + OriginalBroadcastDateTime.ToLocalTime().ToString("s", System.Globalization.CultureInfo.InvariantCulture) + "\r\n";
            AllTags += "RecordedDateTime: " + RecordedDateTime.ToLocalTime().ToString("s", System.Globalization.CultureInfo.InvariantCulture) + "\r\n";
            AllTags += "SeriesPremiereDate: " + SeriesPremiereDate.ToLocalTime().ToString("s", System.Globalization.CultureInfo.InvariantCulture) + "\r\n";
            AllTags += "CopyProtected: " + CopyProtected.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\r\n";
            AllTags += sageTV.ToString();

            return AllTags;
        }
    }
}
