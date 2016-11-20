using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Globalization;

using HtmlAgilityPack;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.MetaData
{
    public static class TV
    {
        static public bool DownloadSeriesDetails(VideoTags videoTags, bool prioritizeMatchDate, bool dontOverwriteTitle, Log jobLog)
        {
            if (!DownloadSeriesDetails(prioritizeMatchDate, videoTags, dontOverwriteTitle, jobLog)) // First try to match by Episode Name (by default prioritize match date is false)
                return (DownloadSeriesDetails(!prioritizeMatchDate, videoTags, dontOverwriteTitle, jobLog)); // Other try to match by Original Air Date (since multiple shows can be aired on the same date) (by default prioritize match date is false)
            else
                return true; // We were successful
        }

        /// <summary>
        /// Download the information about the show from TV.com
        /// </summary>
        /// <param name="matchByAirDate">True to match the Episode by Original AirDate, False to match by Episode Name</param>
        /// <param name="dontOverwriteTitle">True if the title has been manually corrected and not to be overwritten</param>
        /// <returns>True if found a match</returns>
        static private bool DownloadSeriesDetails(bool matchByAirDate, VideoTags videoTags, bool dontOverwriteTitle, Log jobLog)
        {
            HtmlDocument htmlDoc;

            try
            {
                if (matchByAirDate && (videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME))
                {
                    jobLog.WriteEntry("Invalid original broadcast date to match on TV.com", Log.LogEntryType.Debug);
                    return false; // We can only match by airdate if there is something to match against (otherwise we get false positives)
                }
                else if (!matchByAirDate && String.IsNullOrWhiteSpace(videoTags.SubTitle))
                {
                    jobLog.WriteEntry("Invalid episode name to match on TV.com", Log.LogEntryType.Debug);
                    return false; //Nothing to match here
                }

                htmlDoc = new HtmlWeb().Load("http://www.tv.com/search?q=" + videoTags.Title);

                // Get the matching shows list
                HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes("//li[@class='result show']");
                foreach (HtmlNode node in nodes)
                {
                    string seriesTitle = "", seriesLink = "", bannerUrl = "";

                    HtmlDocument subDoc = new HtmlDocument();
                    subDoc.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(node.InnerHtml)));

                    // Get the banner URL
                    HtmlNode urlNode = subDoc.DocumentNode.SelectSingleNode("//div[@class='mask']//img[@src]");
                    if (urlNode != null)
                        if (urlNode.HasAttributes)
                            bannerUrl = urlNode.Attributes["src"].Value.Trim(); // URL
                        else
                            bannerUrl = ""; // reset each cycle
                    else
                        bannerUrl = ""; // reset each cycle

                    // Get the series name and link to page
                    HtmlNode subNode = subDoc.DocumentNode.SelectSingleNode("//div[@class='info']//h4//a[@href]");
                    if (subNode != null)
                    {
                        seriesTitle = subNode.InnerText.Trim(); // Get the title of the series

                        // Compare the series title with the title of the recording
                        if (String.Compare(seriesTitle.Trim(), videoTags.Title.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) == 0)
                        {
                            HtmlNode subNode1 = subDoc.DocumentNode.SelectSingleNode("//ul[@class='sub_links _inline_navigation']");
                            if (subNode1 != null)
                            {
                                HtmlDocument subDoc1 = new HtmlDocument();
                                subDoc1.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(subNode1.InnerHtml)));
                                HtmlNode subNode2 = subDoc1.DocumentNode.SelectSingleNode("//li//a[@href]");
                                if (subNode2 != null)
                                {
                                    if (subNode2.HasAttributes)
                                    {
                                        seriesLink = subNode2.Attributes["href"].Value; // Get the link for the episodes in the series

                                        // Now the links to the various season pages
                                        HtmlDocument sDoc = new HtmlWeb().Load("http://www.tv.com" + seriesLink);
                                        
                                        // Get the premiere date
                                        HtmlNode pNode = sDoc.DocumentNode.SelectSingleNode("//span[@class='divider']");
                                        int start = pNode.InnerText.IndexOf("Premiered") + "Premiered".Length;
                                        int length = pNode.InnerText.IndexOf("In") - start;
                                        string premiereString = pNode.InnerText.Substring(start, length).Trim();
                                        DateTime premiereDate = GlobalDefs.NO_BROADCAST_TIME;
                                        DateTime.TryParse(premiereString, out premiereDate);

                                        // Get the seasons
                                        HtmlNodeCollection sNodes = sDoc.DocumentNode.SelectNodes("//li[@class='filter ']//a[@href]");
                                        foreach (HtmlNode sNode in sNodes) // go through each season
                                        {
                                            string seasonLink;

                                            // Now extract the link to the season episodes page
                                            if (sNode.HasAttributes)
                                            {
                                                seasonLink = sNode.Attributes["href"].Value; // the href has the link to the season page

                                                // Now the links to the various season pages
                                                HtmlDocument eDoc = new HtmlWeb().Load("http://www.tv.com" + seasonLink);

                                                HtmlNodeCollection eNodes = eDoc.DocumentNode.SelectNodes("//div[@class='no_toggle_wrapper _clearfix']");
                                                foreach (HtmlNode eNode in eNodes) // Now extract the episode names, original air dates and compare
                                                {
                                                    string episodeName = "", episodeDesc = "";
                                                    DateTime airDate = GlobalDefs.NO_BROADCAST_TIME;
                                                    int episodeNo = 0, seasonNo = 0;

                                                    HtmlNode tempNode;
                                                    HtmlDocument tempDoc = new HtmlDocument();
                                                    tempDoc.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(eNode.InnerHtml)));

                                                    // Extract the season number
                                                    tempNode = eDoc.DocumentNode.SelectSingleNode("//li[@class='filter selected']");
                                                    if (tempNode != null)
                                                        if (tempNode.HasAttributes)
                                                            int.TryParse(tempNode.Attributes["data-season"].Value.Trim(), out seasonNo); // Season number

                                                    // Extract the episode name
                                                    tempNode = tempDoc.DocumentNode.SelectSingleNode("//a[@class='title']");
                                                    if (tempNode != null)
                                                        episodeName = tempNode.InnerText.Trim(); // Episode Name

                                                    // Extract the episode number
                                                    tempNode = tempDoc.DocumentNode.SelectSingleNode("//div[@class='ep_info']");
                                                    if (tempNode != null)
                                                        int.TryParse(tempNode.InnerText.Trim().Replace("Episode", ""), out episodeNo); // Episode number

                                                    // Extract the original broadcast date
                                                    tempNode = tempDoc.DocumentNode.SelectSingleNode("//div[@class='date']");
                                                    if (tempNode != null)
                                                        DateTime.TryParse(tempNode.InnerText.Trim(), null, DateTimeStyles.AssumeLocal, out airDate); // Air Date

                                                    // Extract the episode description
                                                    tempNode = tempDoc.DocumentNode.SelectSingleNode("//div[@class='description']");
                                                    if (tempNode != null)
                                                        episodeDesc = tempNode.InnerText.Trim(); // Episode description

                                                    // Now match and store - match either episode name or air date
                                                    // The information is stored on the server using the network timezone
                                                    // So we assume that the show being converted was recorded locally and is converted locally so the timezones match
                                                    // Sometimes the timezones get mixed up so we check local time or universal time for a match
                                                    if ((!matchByAirDate && (String.Compare(episodeName.Trim(), videoTags.SubTitle.Trim(), CultureInfo.InvariantCulture, (CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase)) == 0)) ||
                                                        (matchByAirDate && (videoTags.OriginalBroadcastDateTime.ToLocalTime().Date == airDate.Date)) ||
                                                        (matchByAirDate && (videoTags.OriginalBroadcastDateTime.ToUniversalTime().Date == airDate.Date)))
                                                    {
                                                        // Home free - update all the info where required
                                                        if (matchByAirDate) // If we came in matching the Original Air Date - then we overwrite the episode details
                                                        {
                                                            if (!String.IsNullOrWhiteSpace(episodeName)) videoTags.SubTitle = episodeName; // Overwrite
                                                            if (!String.IsNullOrWhiteSpace(episodeDesc)) videoTags.Description = episodeDesc; // Overwrite
                                                        }
                                                        else // only update what's missing
                                                        {
                                                            if (!String.IsNullOrWhiteSpace(episodeName) && String.IsNullOrWhiteSpace(videoTags.SubTitle)) videoTags.SubTitle = episodeName;
                                                            if (!String.IsNullOrWhiteSpace(episodeDesc) && String.IsNullOrWhiteSpace(videoTags.Description)) videoTags.Description = episodeDesc;
                                                        }
                                                        if ((episodeNo != 0) && (videoTags.Episode == 0)) videoTags.Episode = episodeNo;
                                                        if ((seasonNo != 0) && (videoTags.Season == 0)) videoTags.Season = seasonNo;
                                                        if (airDate > GlobalDefs.NO_BROADCAST_TIME)
                                                            if ((videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.OriginalBroadcastDateTime.Date > airDate.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use TV dates, TV Dates are more reliable than video metadata usually
                                                                videoTags.OriginalBroadcastDateTime = airDate; // TV stores time in network (local) timezone
                                                        if (premiereDate > GlobalDefs.NO_BROADCAST_TIME)
                                                            if ((videoTags.SeriesPremiereDate <= GlobalDefs.NO_BROADCAST_TIME) || (videoTags.SeriesPremiereDate.Date > premiereDate.Date)) // Sometimes the metadata from the video recordings are incorrect and report the recorded date (which is more recent than the release date) then use IMDB dates, IMDB Dates are more reliable than video metadata usually
                                                                videoTags.SeriesPremiereDate = premiereDate; // IMDB stores time in network (local) timezone

                                                        VideoMetaData.DownloadBannerFile(videoTags, bannerUrl); // Get bannerfile

                                                        // All Good now
                                                        return true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                jobLog.WriteEntry("No match found on TV.com", Log.LogEntryType.Debug);

                return false;
            }
            catch (Exception e)
            {
                jobLog.WriteEntry("Unable to connect to TV.com\r\nError -> " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }
        }
    }
}
