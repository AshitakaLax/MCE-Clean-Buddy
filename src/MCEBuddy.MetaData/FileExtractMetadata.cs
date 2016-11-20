using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Globalization;

using MCEBuddy.AppWrapper;
using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.MetaData
{
    /// <summary>
    /// Class to extract metadata to files (XML and other formats)
    /// </summary>
    public class FileExtractMetadata
    {
        private string _videoFileName;
        private VideoTags _videoTags;
        private string _tivoMAKKey;
        private Log _jobLog;
        private JobStatus _jobStatus;
        private bool _ignoreSuspend;

        public FileExtractMetadata(string videoFileName, VideoTags videoTags, string tivoMAKKey, bool ignoreSuspend, JobStatus jobStatus, Log jobLog)
        {
            _videoFileName = videoFileName;
            _videoTags = videoTags;
            _jobLog = jobLog;
            _jobStatus = jobStatus;
            _tivoMAKKey = tivoMAKKey;
            _ignoreSuspend = ignoreSuspend;
        }

        /// <summary>
        /// Extracts XML Metadata from XML/NFO file checking for different formats such as Media Portal, nPVR etc
        /// </summary>
        public bool ExtractXMLMetadata()
        {
            bool cleanTivoGenXML = false;

            string nfoFile = Util.FilePaths.GetFullPathWithoutExtension(_videoFileName) + ".nfo"; // XMBC NFO File
            string xmlFile = Util.FilePaths.GetFullPathWithoutExtension(_videoFileName) + ".xml";
            string xmlFileAlt = _videoFileName + ".xml"; // Sometimes ICETV takes the full filename and then creates an XML
            string argFile = Util.FilePaths.GetFullPathWithoutExtension(_videoFileName) + ".arg";
            string sageTVPropertiesFile = Util.FilePaths.GetFullPathWithoutExtension(_videoFileName) + ".properties";
            string sageTVPropertiesFileAlt = _videoFileName + ".properties"; // Sometimes it take the full filename and then adds the .properties to it

            if (Util.FilePaths.CleanExt(_videoFileName) == ".tivo") // First extract the metadata
                if (!File.Exists(xmlFile))
                    if (ExtractTiVOMetadata(xmlFile))
                        cleanTivoGenXML = true; // We generated a TiVO XML file, clean up later

            if (File.Exists(xmlFile))
            {
                _jobLog.WriteEntry(this, "Extracting XML Tags", Log.LogEntryType.Information);

                if (ExtractMPTags(xmlFile))
                    return true; // Media Portal

                if (ExtractNPVRTags(xmlFile))
                    return true; // nPVR

                if (ExtractICETVTags(xmlFile))
                    return true; // ICETv

                if (ExtractSageTVXMLTags(xmlFile))
                    return true; // SageTV XML

                if (ExtractTiVOTags(xmlFile))
                {
                    if (cleanTivoGenXML) // Delete the XML file generated, leave no artifacts behind
                        Util.FileIO.TryFileDelete(xmlFile);

                    return true; // TiVO
                }
            }
            
            if (File.Exists(xmlFileAlt)) // Some like ICETV use alternative naming
            {
                _jobLog.WriteEntry(this, "Extracting Alt XML Tags", Log.LogEntryType.Information);
                if (ExtractICETVTags(xmlFileAlt))
                    return true; // ICETv

                if (ExtractSageTVXMLTags(xmlFileAlt))
                    return true; // SageTV XML
            }

            if (File.Exists(argFile))
            {
                _jobLog.WriteEntry(this, "Extracting ARG Tags", Log.LogEntryType.Information);
                if (ExtractArgusTVTags(argFile))
                    return true; // ArgusTV
            }

            if (File.Exists(sageTVPropertiesFile))
            {
                _jobLog.WriteEntry(this, "Extracting SageTV PROPERTIES Tags", Log.LogEntryType.Information);
                if (ExtractSageTVTags(sageTVPropertiesFile))
                    return true; // SageTV
            }
            
            if (File.Exists(sageTVPropertiesFileAlt))
            {
                _jobLog.WriteEntry(this, "Extracting Alt SageTV PROPERTIES Tags", Log.LogEntryType.Information);
                if (ExtractSageTVTags(sageTVPropertiesFileAlt))
                    return true; // SageTV
            }

            // Do this in the end, lastly if there is nothing else, try for an NFO file
            if (File.Exists(nfoFile))
            {
                _jobLog.WriteEntry(this, "Extracting NFO Tags", Log.LogEntryType.Information);

                if (ExtractXBMCTags(nfoFile))
                    return true; // XMBC/MCEBuddy
            }

            return false; // No matches for metadata
        }

        /// <summary>
        /// Extract the metadata from the TiVO file into a XML file
        /// </summary>
        /// <param name="xmlFile">XML File to create</param>
        private bool ExtractTiVOMetadata(string xmlFile)
        {
            _jobLog.WriteEntry(this, "Extracting TiVO metadata using TDCat", Log.LogEntryType.Information);

            string tivoMetaParams = "";

            if (String.IsNullOrWhiteSpace(_tivoMAKKey))
            {
                _jobLog.WriteEntry(this, "No TiVO MAK key found, cannot extract TiVO Metadata", Log.LogEntryType.Error);
                return false;
            }

            tivoMetaParams += "-m " + _tivoMAKKey + " -1 -o " + Util.FilePaths.FixSpaces(xmlFile) + " " + Util.FilePaths.FixSpaces(_videoFileName);
            TiVOMetaExtract tivoExtract = new TiVOMetaExtract(tivoMetaParams, _jobStatus, _jobLog, _ignoreSuspend);
            tivoExtract.Run();

            if ((Util.FileIO.FileSize(xmlFile) <= 0) || !tivoExtract.Success)
            {
                Util.FileIO.TryFileDelete(xmlFile);
                _jobLog.WriteEntry(this, "TiVODecode failed to extact XML Metadata", Log.LogEntryType.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts Metadata from the XML file written in TiVO format
        /// </summary>
        /// <param name="XMLFile">Path to XML file</param>
        private bool ExtractTiVOTags(string XMLFile)
        {
            bool retVal = false; // no success as yet

            _jobLog.WriteEntry(this, "Extracting TiVO meta data", Log.LogEntryType.Debug);

            try
            {
                XPathDocument Xp;
                if (File.Exists(XMLFile))
                {
                    try
                    {
                        Xp = new XPathDocument(XMLFile);
                    }
                    catch
                    {
                        try
                        {
                            // The XML likely says UTF-8 where as it may be encoded in Latin-1 - http://stackoverflow.com/questions/6829715/invalid-character-in-the-given-encoding
                            Encoding enc = Encoding.GetEncoding("iso-8859-1"); // Try Latin-1
                            StreamReader sr = new StreamReader(XMLFile, enc);
                            Xp = new XPathDocument(sr);
                        }
                        catch
                        {
                            _jobLog.WriteEntry(this, "Invalid XML File", Log.LogEntryType.Warning);
                            return false;
                        }
                    }

                    XPathNavigator Nav = Xp.CreateNavigator();
                    XmlNamespaceManager manager = new XmlNamespaceManager(Nav.NameTable);
                    manager.AddNamespace("TvBusMarshalledStruct", "http://tivo.com/developer/xml/idl/TvBusMarshalledStruct");
                    XPathNodeIterator Itr = Nav.Select("//TvBusMarshalledStruct:TvBusEnvelope/vActualShowing/element", manager);
                    while (Itr.MoveNext())
                    {
                        if (_videoTags.RecordedDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                        {
                            string recordedDateTime = XML.GetXMLTagValue("time", Itr.Current.OuterXml);
                            DateTime.TryParse(recordedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.RecordedDateTime);
                        }
                    }

                    Itr = Nav.Select("//TvBusMarshalledStruct:TvBusEnvelope/vActualShowing/element/program", manager);
                    while (Itr.MoveNext())
                    {
                        string showType = XML.GetXMLTagValue("showType", Itr.Current.OuterXml);
                        if (!String.IsNullOrWhiteSpace(showType))
                        {
                            if (showType.Contains("movie", StringComparison.OrdinalIgnoreCase))
                                _videoTags.IsMovie = true;
                            else if (showType.Contains("sport", StringComparison.OrdinalIgnoreCase))
                                _videoTags.IsSports = true;
                        }
                        string movieYear = XML.GetXMLTagValue("movieYear", Itr.Current.OuterXml).ToLower().Trim();
                        if (!String.IsNullOrWhiteSpace(movieYear)) // Use the movie year to set the original broadcast date as YYYY-05-05 (mid year to avoid timezone issues)
                            DateTime.TryParse(movieYear + "-05-05", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.OriginalBroadcastDateTime);
                        if (String.IsNullOrWhiteSpace(_videoTags.Title)) _videoTags.Title = XML.GetXMLTagValue("title", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.SubTitle)) _videoTags.SubTitle = XML.GetXMLTagValue("episodeTitle", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.Description)) _videoTags.Description = XML.GetXMLTagValue("description", Itr.Current.OuterXml);
                        if (_videoTags.Genres == null) _videoTags.Genres = XML.GetXMLTagValues("vProgramGenre", "element", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.MediaCredits)) _videoTags.MediaCredits = (XML.GetXMLTagValues("vActor", "element", Itr.Current.OuterXml) == null ? "" : String.Join(";", XML.GetXMLTagValues("vActor", "element", Itr.Current.OuterXml)).Replace("|", " "));
                        if (_videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                        {
                            string airDateTime = XML.GetXMLTagValue("originalAirDate", Itr.Current.OuterXml);
                            DateTime.TryParse(airDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.OriginalBroadcastDateTime);
                        }

                        retVal = true; // We got what we wanted here
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "Error extracting TiVO meta data : " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }

            return retVal;
        }

        /// <summary>
        /// Extracts Metadata from the XML file written in SageTV format
        /// </summary>
        /// <param name="XMLFile">Path to XML file</param>
        private bool ExtractSageTVXMLTags(string XMLFile)
        {
            bool retVal = false; // no success as yet

            _jobLog.WriteEntry(this, "Extracting SageTV XML meta data", Log.LogEntryType.Debug);

            try
            {
                XPathDocument Xp;
                if (File.Exists(XMLFile))
                {
                    try
                    {
                        Xp = new XPathDocument(XMLFile);
                    }
                    catch
                    {
                        try
                        {
                            // The XML likely says UTF-8 where as it may be encoded in Latin-1 - http://stackoverflow.com/questions/6829715/invalid-character-in-the-given-encoding
                            Encoding enc = Encoding.GetEncoding("iso-8859-1"); // Try Latin-1
                            StreamReader sr = new StreamReader(XMLFile, enc);
                            Xp = new XPathDocument(sr);
                        }
                        catch
                        {
                            _jobLog.WriteEntry(this, "Invalid XML File", Log.LogEntryType.Warning);
                            return false;
                        }
                    }

                    XPathNavigator Nav = Xp.CreateNavigator();
                    XPathExpression Exp = Nav.Compile("//sageShowInfo/channelList/channel"); // Get the channel info
                    XPathNodeIterator Itr = Nav.Select(Exp);
                    while (Itr.MoveNext())
                    {
                        if (String.IsNullOrWhiteSpace(_videoTags.Network)) _videoTags.Network = XML.GetXMLTagValue("channelName", Itr.Current.OuterXml);
                    }

                    Exp = Nav.Compile("//sageShowInfo/showList/show"); // Get the rest
                    Itr = Nav.Select(Exp);
                    while (Itr.MoveNext())
                    {
                        if (String.IsNullOrWhiteSpace(_videoTags.Title)) _videoTags.Title = XML.GetXMLTagValue("title", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.SubTitle)) _videoTags.SubTitle = XML.GetXMLTagValue("episode", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.Description)) _videoTags.Description = XML.GetXMLTagValue("description", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.MediaCredits)) _videoTags.MediaCredits = (XML.GetXMLTagValues("peopleList", "person", Itr.Current.OuterXml) == null ? "" : String.Join(";", XML.GetXMLTagValues("peopleList", "person", Itr.Current.OuterXml)));
                        if (String.IsNullOrWhiteSpace(_videoTags.Rating)) _videoTags.Rating = XML.GetXMLTagValue("rating", Itr.Current.OuterXml);
                        if (_videoTags.Genres == null)
                        {
                            if (!String.IsNullOrWhiteSpace(XML.GetXMLTagValue("category", Itr.Current.OuterXml)))
                            {
                                _videoTags.Genres = new string[] { XML.GetXMLTagValue("category", Itr.Current.OuterXml) };
                                if (_videoTags.Genres.Any(genre => genre.Contains("Movie", StringComparison.OrdinalIgnoreCase)))
                                    _videoTags.IsMovie = true; // Sometimes MediaType tag is missing, check here for a movie
                                else if (_videoTags.Genres.Any(genre => genre.Contains("Sport", StringComparison.OrdinalIgnoreCase)))
                                    _videoTags.IsSports = true; // Sometimes MediaType tag is missing, check here for a sport
                            }
                        }

                        _videoTags.sageTV.airingDbId = XML.GetXMLTagAttributeValue("airing", "sageDbId", Itr.Current.OuterXml);
                        _videoTags.sageTV.mediaFileDbId = XML.GetXMLTagAttributeValue("airing", "mediafile", "sageDbId", Itr.Current.OuterXml);
                        
                        if (_videoTags.RecordedDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                        {
                            string recordedDateTime = XML.GetXMLTagAttributeValue("airing", "startTime", Itr.Current.OuterXml);
                            DateTime.TryParse(recordedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.RecordedDateTime);
                        }
                        if (_videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                        {
                            string airDateTime;
                            if (_videoTags.IsMovie)
                                airDateTime = XML.GetXMLTagValue("year", Itr.Current.OuterXml) + "-05-05"; // we only get year so add the month and date
                            else
                                airDateTime = XML.GetXMLTagValue("originalAirDate", Itr.Current.OuterXml);
                            DateTime.TryParse(airDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.OriginalBroadcastDateTime);
                        }

                        retVal = true; // We got what we wanted here
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "Error extracting nPVR meta data : " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }

            return retVal;
        }

        /// <summary>
        /// Extracts Metadata from the PROPERTIES file written in SageTV format
        /// </summary>
        /// <param name="PropertiesFile">Path to XML file</param>
        private bool ExtractSageTVTags(string PropertiesFile)
        {
            bool retVal = false; // no success as yet
            bool isTVDBid = false, isTMDBid = false;
            string mediaProviderID = "";

            _jobLog.WriteEntry(this, "Extracting SageTV meta data", Log.LogEntryType.Debug);

            try
            {
                if (File.Exists(PropertiesFile))
                {
                    string line;
                    StreamReader fileRead = new StreamReader(PropertiesFile);

                    while ((line = fileRead.ReadLine()) != null)
                    {
                        if (!String.IsNullOrWhiteSpace(line))
                        {
                            line = line.Replace(@"\=", ""); // Get rid of the escaped = characters since the delimiter here is = (PARAM=VALUE)
                            line = line.Replace(@"\n", ""); // Get rid of the newline characters
                            line = line.Replace(@"\r", ""); // Get rid of the newline characters
                            line = line.Replace(@"\", ""); // Get rid of the remaining escape characters
                            string[] entries = line.Split(new char[] { '=' }, 2); // The format is PARAM=VALUE

                            if (entries.Length == 2) // We are looking for exactly 2 entries here (PARAM=VALUE)
                            {
                                // Check out tag info at https://www.google.com/url?sa=t&rct=j&q=&esrc=s&source=web&cd=5&ved=0CFQQFjAE&url=https%3A%2F%2Fdocs.google.com%2Fdocument%2Fd%2F1C-c6NbMOks48GImP4TxDeL-5G3oqsTGHIUwDTdAQ8Ug%2Fmobilebasic&ei=sKI6U5baAczNsQTYjYGwDw&usg=AFQjCNEw_eOk7U5JqGZpzwh9Twq0CJzp6Q&sig2=XsVonOu_oJkqNvUFKdSU4g&bvm=bv.63934634,d.cWc&cad=rja
                                switch (entries[0].Trim())
                                {
                                    case "MediaTitle":
                                    case "Title":
                                        _videoTags.Title = entries[1].Trim();
                                        retVal = true; // We need atleast the Title to be successful
                                        break;

                                    case "EpisodeName":
                                        _videoTags.SubTitle = entries[1].Trim();
                                        break;

                                    case "Description":
                                        _videoTags.Description = entries[1].Trim();
                                        break;

                                    case "ParentalRating":
                                        _videoTags.Rating = entries[1].Trim();
                                        break;

                                    case "Actor":
                                        _videoTags.MediaCredits = entries[1].Trim();
                                        break;

                                    case "Genre":
                                        _videoTags.Genres = entries[1].Split('/');
                                        if (_videoTags.Genres.Any(genre => genre.Equals("Movie", StringComparison.OrdinalIgnoreCase)))
                                            _videoTags.IsMovie = true; // Sometimes MediaType tag is missing, check here for a movie
                                        else if (_videoTags.Genres.Any(genre => genre.Contains("Sport", StringComparison.OrdinalIgnoreCase)))
                                            _videoTags.IsSports = true; // Sometimes MediaType tag is missing, check here for a sport
                                        break;

                                    case "SeasonNumber":
                                        int.TryParse(entries[1].Trim(), out _videoTags.Season);
                                        break;

                                    case "EpisodeNumber":
                                        int.TryParse(entries[1].Trim(), out _videoTags.Episode);
                                        break;

                                    case "IMDBID":
                                        _videoTags.imdbId = entries[1].Trim();
                                        break;

                                    case "MediaType":
                                        if (entries[1].Trim().ToLower() == "movie")
                                            _videoTags.IsMovie = true;
                                        break;

                                    case "OriginalAirDate":
                                        // Stored in UTC Epoch MS
                                        long unixTimeMS;
                                        if (long.TryParse(entries[1], out unixTimeMS))
                                            if (unixTimeMS > 0) // sometime you have a 0 entry
                                                _videoTags.OriginalBroadcastDateTime = DateAndTime.FromUnixTime(unixTimeMS / 1000); // Convert from milliseconds to seconds
                                        break;

                                    case "MediaProviderDataID": // Keep it for now, we'll process later since we are not sure who this id came from yet
                                        mediaProviderID = entries[1].Trim().ToLower();
                                        break;

                                    case "MediaProviderID": // who is the MediaProviderDataID coming from
                                        switch (entries[1].Trim().ToLower())
                                        {
                                            case "tvdb":
                                                isTVDBid = true;
                                                break;

                                            case "tmdb":
                                                isTMDBid = true;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    default: // Empty value, ignore it
                                        break;
                                }
                            }
                            else
                                _jobLog.WriteEntry(this, "Skipping invalid properties entry -> " + line, Log.LogEntryType.Warning);
                        }
                    }

                    // See if we have a media id and who it came from
                    if (!String.IsNullOrWhiteSpace(mediaProviderID))
                    {
                        if (isTVDBid)
                            _videoTags.tvdbId = mediaProviderID;
                        else if (isTMDBid)
                            _videoTags.tmdbId = mediaProviderID;
                    }

                    fileRead.Close();
                    fileRead.Dispose();
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Error extracting SageTV meta data : " + e.ToString()), Log.LogEntryType.Warning);
                return false;
            }

            return retVal;
        }

        /// <summary>
        /// Extracts Metadata from the ARG (XML) file written in ArgusTV format
        /// </summary>
        /// <param name="XMLFile">Path to XML file</param>
        private bool ExtractArgusTVTags(string XMLFile)
        {
            bool retVal = false; // no success as yet

            _jobLog.WriteEntry(this, "Extracting ArgusTV meta data", Log.LogEntryType.Debug);

            try
            {
                XPathDocument Xp;
                if (File.Exists(XMLFile))
                {
                    try
                    {
                        Xp = new XPathDocument(XMLFile);
                    }
                    catch
                    {
                        try
                        {
                            // The XML likely says UTF-8 where as it may be encoded in Latin-1 - http://stackoverflow.com/questions/6829715/invalid-character-in-the-given-encoding
                            Encoding enc = Encoding.GetEncoding("iso-8859-1"); // Try Latin-1
                            StreamReader sr = new StreamReader(XMLFile, enc);
                            Xp = new XPathDocument(sr);
                        }
                        catch
                        {
                            _jobLog.WriteEntry(this, "Invalid XML File", Log.LogEntryType.Warning);
                            return false;
                        }
                    }

                    XPathNavigator Nav = Xp.CreateNavigator();
                    XPathExpression Exp = Nav.Compile("//Recording");
                    XPathNodeIterator Itr = Nav.Select(Exp);
                    while (Itr.MoveNext())
                    {
                        if (String.IsNullOrWhiteSpace(_videoTags.Title)) _videoTags.Title = XML.GetXMLTagValue("Title", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.SubTitle)) _videoTags.SubTitle = XML.GetXMLTagValue("SubTitle", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.Description)) _videoTags.Description = XML.GetXMLTagValue("Description", Itr.Current.OuterXml);
                        if (_videoTags.Episode == 0) int.TryParse(XML.GetXMLTagValue("EpisodeNumber", Itr.Current.OuterXml), out _videoTags.Episode);
                        if (_videoTags.Season == 0) int.TryParse(XML.GetXMLTagValue("SeriesNumber", Itr.Current.OuterXml), out _videoTags.Season);
                        if (String.IsNullOrWhiteSpace(_videoTags.Network)) _videoTags.Network = XML.GetXMLTagValue("ChannelDisplayName", Itr.Current.OuterXml);
                        if (_videoTags.RecordedDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                        {
                            string recordedDateTime = XML.GetXMLTagValue("RecordingStartTime", Itr.Current.OuterXml);
                            DateTime.TryParse(recordedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.RecordedDateTime);
                        }
                        if (XML.GetXMLTagValue("IsPremiere", Itr.Current.OuterXml).Equals("true", StringComparison.OrdinalIgnoreCase))
                            if (_videoTags.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                _videoTags.SeriesPremiereDate = _videoTags.OriginalBroadcastDateTime = _videoTags.RecordedDateTime;

                        retVal = true; // We got what we wanted here
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "Error extracting ArgusTV meta data : " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }

            return retVal;
        }

        /// <summary>
        /// Extracts Metadata from the XML file written in nPVR format
        /// </summary>
        /// <param name="XMLFile">Path to XML file</param>
        private bool ExtractNPVRTags(string XMLFile)
        {
            bool retVal = false; // no success as yet

            _jobLog.WriteEntry(this, "Extracting nPVR meta data", Log.LogEntryType.Debug);

            try
            {
                XPathDocument Xp;
                if (File.Exists(XMLFile))
                {
                    try
                    {
                        Xp = new XPathDocument(XMLFile);
                    }
                    catch
                    {
                        try
                        {
                            // The XML likely says UTF-8 where as it may be encoded in Latin-1 - http://stackoverflow.com/questions/6829715/invalid-character-in-the-given-encoding
                            Encoding enc = Encoding.GetEncoding("iso-8859-1"); // Try Latin-1
                            StreamReader sr = new StreamReader(XMLFile, enc);
                            Xp = new XPathDocument(sr);
                        }
                        catch
                        {
                            _jobLog.WriteEntry(this, "Invalid XML File", Log.LogEntryType.Warning);
                            return false;
                        }
                    }

                    XPathNavigator Nav = Xp.CreateNavigator();
                    XPathExpression Exp = Nav.Compile("//recording");
                    XPathNodeIterator Itr = Nav.Select(Exp);
                    while (Itr.MoveNext())
                    {
                        if (String.IsNullOrWhiteSpace(_videoTags.Title)) _videoTags.Title = XML.GetXMLTagValue("title", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.SubTitle)) _videoTags.SubTitle = XML.GetXMLTagValue("subtitle", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.Description)) _videoTags.Description = XML.GetXMLTagValue("description", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.Network)) _videoTags.Network = XML.GetXMLTagValue("channel", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.Rating)) _videoTags.Rating = XML.GetXMLTagValue("rating", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.MediaCredits)) _videoTags.MediaCredits = (XML.GetXMLTagValues("actors", "actor", Itr.Current.OuterXml) == null ? "" : String.Join(";", XML.GetXMLTagValues("actors", "actor", Itr.Current.OuterXml)));
                        if (_videoTags.Episode == 0) int.TryParse(XML.GetXMLTagValue("episode", Itr.Current.OuterXml), out _videoTags.Episode);
                        if (_videoTags.Season == 0) int.TryParse(XML.GetXMLTagValue("season", Itr.Current.OuterXml), out _videoTags.Season);
                        if (_videoTags.Genres == null)
                        {
                            _videoTags.Genres = XML.GetXMLTagValues("genres", "genre", Itr.Current.OuterXml);
                            if (_videoTags.Genres.Any(genre => genre.Contains("Movie", StringComparison.OrdinalIgnoreCase)))
                                _videoTags.IsMovie = true; // Sometimes MediaType tag is missing, check here for a movie
                            else if (_videoTags.Genres.Any(genre => genre.Contains("Sport", StringComparison.OrdinalIgnoreCase)))
                                _videoTags.IsSports = true; // Sometimes MediaType tag is missing, check here for a sport
                        }
                        if (_videoTags.RecordedDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                        {
                            string recordedDateTime = XML.GetXMLTagValue("startTime", Itr.Current.OuterXml);
                            DateTime.TryParse(recordedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.RecordedDateTime);
                        }
                        if (_videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                        {
                            string airDateTime = XML.GetXMLTagValue("original_air_date", Itr.Current.OuterXml);
                            DateTime.TryParse(airDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.OriginalBroadcastDateTime);
                        }

                        retVal = true; // We got what we wanted here
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "Error extracting nPVR meta data : " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }

            return retVal;
        }

        /// <summary>
        /// Extracts tags from Media Portal compliant XML file
        /// </summary>
        /// <param name="XMLFile">Path to XML file</param>
        private bool ExtractMPTags(string XMLFile)
        {
            bool retVal = false; // no success as yet

            _jobLog.WriteEntry(this, "Extracting MediaPortal meta data", Log.LogEntryType.Debug);

            try
            {
                XPathDocument Xp;
                if (File.Exists(XMLFile))
                {
                    try
                    {
                        Xp = new XPathDocument(XMLFile);
                    }
                    catch
                    {
                        try
                        {
                            // The XML likely says UTF-8 where as it may be encoded in Latin-1 - http://stackoverflow.com/questions/6829715/invalid-character-in-the-given-encoding
                            Encoding enc = Encoding.GetEncoding("iso-8859-1"); // Try Latin-1
                            StreamReader sr = new StreamReader(XMLFile, enc);
                            Xp = new XPathDocument(sr);
                        }
                        catch
                        {
                            _jobLog.WriteEntry(this, "Invalid XML File", Log.LogEntryType.Warning);
                            return false;
                        }
                    }

                    string CurrName, CurrValue;
                    XPathNavigator Nav = Xp.CreateNavigator();
                    XPathExpression Exp = Nav.Compile("//tags/tag/SimpleTag");
                    XPathNodeIterator Itr = Nav.Select(Exp);
                    while (Itr.MoveNext())
                    {
                        CurrName = XML.GetXMLTagValue("name", Itr.Current.OuterXml);
                        CurrValue = XML.GetXMLTagValue("value", Itr.Current.OuterXml);
                        if ((CurrName != "") & (CurrValue != "") & (CurrValue != "-"))
                        {
                            if ((CurrName.Equals("title", StringComparison.OrdinalIgnoreCase)) && (String.IsNullOrWhiteSpace(_videoTags.Title))) 
                                _videoTags.Title = CurrValue;
                            else if ((CurrName.Equals("seriesname", StringComparison.OrdinalIgnoreCase)) && (String.IsNullOrWhiteSpace(_videoTags.Title))) 
                                _videoTags.Title = CurrValue; // sometimes it is stores as series name
                            else if ((CurrName.Equals("origname", StringComparison.OrdinalIgnoreCase)) && (String.IsNullOrWhiteSpace(_videoTags.Title))) 
                                _videoTags.Title = CurrValue; // If this isn't a series, check if it's a movie (use original name is possible)
                            else if ((CurrName.Equals("moviename", StringComparison.OrdinalIgnoreCase)) && (String.IsNullOrWhiteSpace(_videoTags.Title)))
                            {
                                _videoTags.Title = CurrValue; // Last chance use localized name
                                _videoTags.IsMovie = true;
                            }

                            if ((CurrName.Equals("episodename", StringComparison.OrdinalIgnoreCase)) && (String.IsNullOrWhiteSpace(_videoTags.SubTitle))) _videoTags.SubTitle = CurrValue;
                            else if ((CurrName.Equals("tagline", StringComparison.OrdinalIgnoreCase)) && (String.IsNullOrWhiteSpace(_videoTags.SubTitle))) _videoTags.SubTitle = CurrValue; // movies info is stores in tagline

                            if ((CurrName.Equals("comment", StringComparison.OrdinalIgnoreCase)) && (String.IsNullOrWhiteSpace(_videoTags.Description))) _videoTags.Description = CurrValue;
                            else if ((CurrName.Equals("tagline", StringComparison.OrdinalIgnoreCase)) && (String.IsNullOrWhiteSpace(_videoTags.Description))) _videoTags.Description = CurrValue; // movies info is stores in tagline
                            else if ((CurrName.Equals("storyplot", StringComparison.OrdinalIgnoreCase)) && (String.IsNullOrWhiteSpace(_videoTags.Description))) _videoTags.Description = CurrValue; // sometimes info is stores in story plot

                            if ((CurrName.Equals("channel_name", StringComparison.OrdinalIgnoreCase)) && (String.IsNullOrWhiteSpace(_videoTags.Network)))
                                _videoTags.Network = CurrValue;

                            if (CurrName.Equals("genre", StringComparison.OrdinalIgnoreCase))
                            {
                                if (_videoTags.Genres == null)
                                    _videoTags.Genres = CurrValue.Split(';');
                                else if (_videoTags.Genres.Length == 0) // sometimes we get a null array, so check the length
                                    _videoTags.Genres = CurrValue.Split(';');

                                if (_videoTags.Genres.Any(genre => genre.Contains("Movie", StringComparison.OrdinalIgnoreCase)))
                                    _videoTags.IsMovie = true; // Sometimes MediaType tag is missing, check here for a movie
                                else if (_videoTags.Genres.Any(genre => genre.Contains("Sport", StringComparison.OrdinalIgnoreCase)))
                                    _videoTags.IsSports = true; // Sometimes MediaType tag is missing, check here for a sport
                            }

                            retVal = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "Error extracting Media Portal meta data : " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }

            return retVal;
        }

        /// <summary>
        /// Extracts tags from XBMC compliant NFO file (XML)
        /// </summary>
        /// <param name="NFOFile">Path to NFO file</param>
        private bool ExtractXBMCTags(string NFOFile)
        {
            bool retVal = false; // no success as yet

            _jobLog.WriteEntry(this, "Extracting XMBC metadata", Log.LogEntryType.Debug);

            try
            {
                XPathDocument Xp;
                if (File.Exists(NFOFile))
                {
                    try
                    {
                        Xp = new XPathDocument(NFOFile);
                    }
                    catch
                    {
                        try
                        {
                            // The XML likely says UTF-8 where as it may be encoded in Latin-1 - http://stackoverflow.com/questions/6829715/invalid-character-in-the-given-encoding
                            Encoding enc = Encoding.GetEncoding("iso-8859-1"); // Try Latin-1
                            StreamReader sr = new StreamReader(NFOFile, enc);
                            Xp = new XPathDocument(sr);
                        }
                        catch
                        {
                            _jobLog.WriteEntry(this, "Invalid NFO File", Log.LogEntryType.Warning);
                            return false;
                        }
                    }

                    XPathNavigator Nav = Xp.CreateNavigator();
                    XPathExpression Exp = Nav.Compile("//episodedetails");
                    XPathNodeIterator Itr = Nav.Select(Exp);
                    if (Itr.Count > 0) // This is a Episode
                    {
                        while (Itr.MoveNext())
                        {
                            _videoTags.IsMovie = false;

                            // NOTE: Custom tags not part of standard
                            if (String.IsNullOrWhiteSpace(_videoTags.Title)) _videoTags.Title = XML.GetXMLTagValue("xshow", Itr.Current.OuterXml);
                            if (String.IsNullOrWhiteSpace(_videoTags.Rating)) _videoTags.Rating = XML.GetXMLTagValue("xrating", Itr.Current.OuterXml);
                            if (String.IsNullOrWhiteSpace(_videoTags.tvdbId)) _videoTags.tvdbId = XML.GetXMLTagValue("xtvdbid", Itr.Current.OuterXml);
                            if (String.IsNullOrWhiteSpace(_videoTags.tmdbId)) _videoTags.tmdbId = XML.GetXMLTagValue("xtmdbid", Itr.Current.OuterXml);
                            
                            if (XML.GetXMLTagValue("xsport", Itr.Current.OuterXml).Equals("true", StringComparison.OrdinalIgnoreCase))
                                _videoTags.IsSports = true;
                            else
                                _videoTags.IsSports = false;
                            
                            if (_videoTags.RecordedDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                            {
                                string recordedDateTime = XML.GetXMLTagValue("xrecorded", Itr.Current.OuterXml).Trim();
                                DateTime.TryParse(recordedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.RecordedDateTime);
                            }

                            // XMBC standard tags
                            if (String.IsNullOrWhiteSpace(_videoTags.SubTitle)) _videoTags.SubTitle = XML.GetXMLTagValue("title", Itr.Current.OuterXml);
                            if (_videoTags.Season == 0) int.TryParse(XML.GetXMLTagValue("season", Itr.Current.OuterXml), out _videoTags.Season);
                            if (_videoTags.Episode == 0) int.TryParse(XML.GetXMLTagValue("episode", Itr.Current.OuterXml), out _videoTags.Episode);
                            if (String.IsNullOrWhiteSpace(_videoTags.Description)) _videoTags.Description = XML.GetXMLTagValue("plot", Itr.Current.OuterXml);
                            if (String.IsNullOrWhiteSpace(_videoTags.BannerURL)) _videoTags.BannerURL = XML.GetXMLTagValue("thumb", Itr.Current.OuterXml);
                            if (String.IsNullOrWhiteSpace(_videoTags.imdbId)) _videoTags.imdbId = XML.GetXMLTagValue("id", Itr.Current.OuterXml);
                            if (String.IsNullOrWhiteSpace(_videoTags.MediaCredits)) _videoTags.MediaCredits = XML.GetXMLTagValue("credits", Itr.Current.OuterXml);
                            if (String.IsNullOrWhiteSpace(_videoTags.Network)) _videoTags.Network = XML.GetXMLTagValue("studio", Itr.Current.OuterXml);
                            
                            if (_videoTags.Genres == null) _videoTags.Genres = XML.GetXMLTagValues("genre", Itr.Current.OuterXml);
                            else if (_videoTags.Genres.Length == 0) _videoTags.Genres = XML.GetXMLTagValues("genre", Itr.Current.OuterXml);

                            if (_videoTags.SeriesPremiereDate <= GlobalDefs.NO_BROADCAST_TIME)
                            {
                                string premiereDateTime = XML.GetXMLTagValue("premiered", Itr.Current.OuterXml).Trim();
                                DateTime.TryParse(premiereDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.SeriesPremiereDate);
                            }

                            if (_videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                            {
                                string broadcastDateTime = XML.GetXMLTagValue("aired", Itr.Current.OuterXml).Trim();
                                DateTime.TryParse(broadcastDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.OriginalBroadcastDateTime);
                            }

                            retVal = true; // We got what we wanted here
                        }
                    }
                    else
                    {
                        Exp = Nav.Compile("//movie");
                        Itr = Nav.Select(Exp);
                        if (Itr.Count > 0) // This is a Movie
                        {
                            while (Itr.MoveNext())
                            {
                                _videoTags.IsMovie = true;
                                _videoTags.IsSports = false;

                                // NOTE: Custom tags not part of standard
                                if (String.IsNullOrWhiteSpace(_videoTags.Rating)) _videoTags.Rating = XML.GetXMLTagValue("xrating", Itr.Current.OuterXml);
                                if (String.IsNullOrWhiteSpace(_videoTags.tvdbId)) _videoTags.tvdbId = XML.GetXMLTagValue("xtvdbid", Itr.Current.OuterXml);
                                if (String.IsNullOrWhiteSpace(_videoTags.tmdbId)) _videoTags.tmdbId = XML.GetXMLTagValue("xtmdbid", Itr.Current.OuterXml);

                                if (_videoTags.RecordedDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                                {
                                    string recordedDateTime = XML.GetXMLTagValue("xrecorded", Itr.Current.OuterXml).Trim();
                                    DateTime.TryParse(recordedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.RecordedDateTime);
                                }

                                // XMBC standard tags
                                if (String.IsNullOrWhiteSpace(_videoTags.Title)) _videoTags.Title = XML.GetXMLTagValue("title", Itr.Current.OuterXml);
                                if (String.IsNullOrWhiteSpace(_videoTags.Description)) _videoTags.Description = XML.GetXMLTagValue("outline", Itr.Current.OuterXml);
                                if (String.IsNullOrWhiteSpace(_videoTags.Description)) _videoTags.Description = XML.GetXMLTagValue("plot", Itr.Current.OuterXml); // backup
                                if (String.IsNullOrWhiteSpace(_videoTags.BannerURL)) _videoTags.BannerURL = XML.GetXMLTagValue("thumb", Itr.Current.OuterXml);
                                if (String.IsNullOrWhiteSpace(_videoTags.imdbId)) _videoTags.imdbId = XML.GetXMLTagValue("id", Itr.Current.OuterXml);
                                if (String.IsNullOrWhiteSpace(_videoTags.MediaCredits)) _videoTags.MediaCredits = XML.GetXMLTagValue("credits", Itr.Current.OuterXml);
                                if (String.IsNullOrWhiteSpace(_videoTags.Network)) _videoTags.Network = XML.GetXMLTagValue("studio", Itr.Current.OuterXml);
                                if (String.IsNullOrWhiteSpace(_videoTags.Network)) _videoTags.Network = XML.GetXMLTagValue("company", Itr.Current.OuterXml);

                                if (_videoTags.Genres == null) _videoTags.Genres = XML.GetXMLTagValues("genre", Itr.Current.OuterXml);
                                else if (_videoTags.Genres.Length == 0) _videoTags.Genres = XML.GetXMLTagValues("genre", Itr.Current.OuterXml);

                                if (_videoTags.SeriesPremiereDate <= GlobalDefs.NO_BROADCAST_TIME)
                                {
                                    string premiereDateTime = XML.GetXMLTagValue("premiered", Itr.Current.OuterXml).Trim();
                                    DateTime.TryParse(premiereDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.SeriesPremiereDate);
                                }

                                if (_videoTags.OriginalBroadcastDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                                {
                                    string broadcastDateTime = XML.GetXMLTagValue("year", Itr.Current.OuterXml).Trim();
                                    DateTime.TryParseExact(broadcastDateTime, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.OriginalBroadcastDateTime);
                                }

                                retVal = true; // We got what we wanted here
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "Error extracting XBMC metadata : " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }

            return retVal;
        }

        /// <summary>
        /// Extracts Metadata from the ICE TV (using TV Scheduler Pro) (XML) file
        /// </summary>
        /// <param name="XMLFile">Path to XML file</param>
        private bool ExtractICETVTags(string XMLFile)
        {
            string sampleDateFormat = "Thu May 01 21:45:00 EST 2014";
            string[] dateFormats = { "ddd MMM dd HH:mm:ss zzz yyyy" , "ddd MMM dd HH:mm:ss K yyyy" };

            bool retVal = false; // no success as yet

            _jobLog.WriteEntry(this, "Extracting ICETV meta data", Log.LogEntryType.Debug);

            try
            {
                XPathDocument Xp;
                if (File.Exists(XMLFile))
                {
                    try
                    {
                        Xp = new XPathDocument(XMLFile);
                    }
                    catch
                    {
                        try
                        {
                            // The XML likely says UTF-8 where as it may be encoded in Latin-1 - http://stackoverflow.com/questions/6829715/invalid-character-in-the-given-encoding
                            Encoding enc = Encoding.GetEncoding("iso-8859-1"); // Try Latin-1
                            StreamReader sr = new StreamReader(XMLFile, enc);
                            Xp = new XPathDocument(sr);
                        }
                        catch
                        {
                            _jobLog.WriteEntry(this, "Invalid XML File", Log.LogEntryType.Warning);
                            return false;
                        }
                    }

                    XPathNavigator Nav = Xp.CreateNavigator();
                    XPathExpression Exp = Nav.Compile("//capture/epg_item");
                    XPathNodeIterator Itr = Nav.Select(Exp);
                    while (Itr.MoveNext())
                    {
                        if (String.IsNullOrWhiteSpace(_videoTags.Title)) _videoTags.Title = XML.GetXMLTagValue("epg_title", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.SubTitle)) _videoTags.SubTitle = XML.GetXMLTagValue("epg_subtitle", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.Description)) _videoTags.Description = XML.GetXMLTagValue("epg_description", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.Rating)) _videoTags.Rating = XML.GetXMLTagValue("epg_ratings", Itr.Current.OuterXml);
                        if (String.IsNullOrWhiteSpace(_videoTags.MediaCredits)) if (XML.GetXMLTagValues("epg_actors", Itr.Current.OuterXml) != null) _videoTags.MediaCredits = (XML.GetXMLTagValues("epg_actors", Itr.Current.OuterXml) == null ? "" : String.Join(";", XML.GetXMLTagValues("epg_actors", Itr.Current.OuterXml)));
                        if (String.IsNullOrWhiteSpace(_videoTags.Network)) _videoTags.Network = XML.GetXMLTagValue("epg_channel", Itr.Current.OuterXml);
                        if (XML.GetXMLTagValues("epg_category", Itr.Current.OuterXml) != null)
                        {
                            if (XML.GetXMLTagValues("epg_category", Itr.Current.OuterXml).Any(x => x.ToLower().Trim().Equals("movie"))) // Check if any of the categories are movie
                            {
                                _videoTags.IsMovie = true;
                                if (_videoTags.Title.ToLower().StartsWith("movie:")) // Sometimes ICETV starts with Movie: for movies, remove it
                                    _videoTags.Title = _videoTags.Title.Remove(0, "movie:".Length).Trim();
                            }
                            else if (XML.GetXMLTagValues("epg_category", Itr.Current.OuterXml).Any(x => x.ToLower().Trim().Equals("sport"))) // Check if any of the categories are movie
                                _videoTags.IsSports = true;
                        }
                        
                        if (_videoTags.Genres == null) _videoTags.Genres = XML.GetXMLTagValues("epg_category", Itr.Current.OuterXml);
                        else if (_videoTags.Genres.Length == 0) _videoTags.Genres = XML.GetXMLTagValues("epg_category", Itr.Current.OuterXml);
                        
                        if (_videoTags.RecordedDateTime <= GlobalDefs.NO_BROADCAST_TIME)
                        {
                            string recordedDateTime = XML.GetXMLTagValue("epg_start", Itr.Current.OuterXml).Trim();
                            if (recordedDateTime.Length == sampleDateFormat.Length) // check if it is in the same format
                            {
                                if (!String.IsNullOrWhiteSpace(Util.DateAndTime.TimeZoneToOffset(recordedDateTime.Substring(20, 3))))
                                {
                                    string fixedRecordedDateTime = recordedDateTime.Substring(0, 20) + Util.DateAndTime.TimeZoneToOffset(recordedDateTime.Substring(20, 3)) + recordedDateTime.Substring(23, 5);
                                    DateTime.TryParseExact(fixedRecordedDateTime, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _videoTags.RecordedDateTime);
                                }
                            }
                        }
                        
                        if (XML.GetXMLTagValue("epg_premiere", Itr.Current.OuterXml).ToLower().Trim() == "true") // If this is a premiere then the recorded date/time is also the original broadcast + premiere date/time
                            if (_videoTags.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                _videoTags.SeriesPremiereDate = _videoTags.OriginalBroadcastDateTime = _videoTags.RecordedDateTime;

                        if (XML.GetXMLTagValue("epg_live", Itr.Current.OuterXml).ToLower().Trim() == "true") // If this is a live broadcast then the recorded date/time is also the original broadcast date/time
                            if (_videoTags.RecordedDateTime > GlobalDefs.NO_BROADCAST_TIME)
                                _videoTags.OriginalBroadcastDateTime = _videoTags.RecordedDateTime;

                        retVal = true; // We got what we wanted here
                    }
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "Error extracting ICETV meta data : " + e.ToString(), Log.LogEntryType.Warning);
                return false;
            }

            return retVal;
        }


    }
}
