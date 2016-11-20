using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

using MCEBuddy.Util;
using MCEBuddy.VideoProperties;
using MCEBuddy.Globals;

namespace MCEBuddy.CommercialScan
{
    public class EDL
    {
        private bool _forceEDL, _forceEDLP;
        private string _profile, _videoFileName;
        private float _duration;
        private JobStatus _jobStatus;
        private Log _jobLog;
        private float _initialSkipSeconds;
        private double _minimumSegmentSize = 0;

        private string _EDLFile = ""; // If the EDL File exists
        private string _CHAPFile = ""; // If the CHAP file exists
        private string _XMLCHAPFile = ""; // If the XML CHAP file exists

        /// <summary>
        /// Class for manipulating EDL, EDLP and CHAP files
        /// </summary>
        /// <param name="profile">Conversion Profile name</param>
        /// <param name="fileName">Full path Video from which EDL, EDLP and CHAP filenames will be dervied</param>
        /// <param name="duration">Length of video</param>
        /// <param name="edlFile">EDL File used to set the initial EDLFile property</param>
        /// <param name="initialSkipSeconds">Number of seconds of the file cut which needs to be adjusted while calculating cut segments</param>
        /// <param name="ignoreMinimumCut">Ignore the mininum segment size for cutting (some encoders choke if trying to encode small sement)</param>
        public EDL(string profile, string fileName, float duration, string edlFile, float initialSkipSeconds, JobStatus jobStatus, Log jobLog, bool ignoreMinimumCut = false)
        {
            _profile = profile;
            _videoFileName = fileName;
            _duration = duration;
            _initialSkipSeconds = initialSkipSeconds;
            _jobLog = jobLog;
            _jobStatus = jobStatus;
            _EDLFile = edlFile;

            // Check if we are using minimum segment size
            if (ignoreMinimumCut)
                _minimumSegmentSize = 0;
            else
                _minimumSegmentSize = GlobalDefs.MINIMUM_SEGMENT_LENGTH;

            _jobLog.WriteEntry(this, "EDL: Minimum segment size " + _minimumSegmentSize.ToString(CultureInfo.InvariantCulture) + " seconds.", Log.LogEntryType.Debug);

            // check if we need to use the EDL file instead of the EDLP file
            Ini ini = new Ini(GlobalDefs.ProfileFile);
            _forceEDL = ini.ReadBoolean(_profile, "ForceEDL", false);
            _forceEDLP = ini.ReadBoolean(_profile, "ForceEDLP", false);

            jobLog.WriteEntry("EDL Setting (ForceEDL) -> " + _forceEDL.ToString(), Log.LogEntryType.Debug);
            jobLog.WriteEntry("EDL Settings (ForceEDLP) -> " + _forceEDLP.ToString(), Log.LogEntryType.Debug);

            // Fix duration for initial skip seconds
            _duration = (_duration - _initialSkipSeconds < 0 ? 0 : _duration - _initialSkipSeconds);

            _jobLog.WriteEntry(this, "EDL: Initial skip seconds adjustment " + _initialSkipSeconds.ToString(CultureInfo.InvariantCulture) + " seconds.", Log.LogEntryType.Debug);
        }


        /// <summary>
        /// Converts a VPRJ file to a EDL file (overwrites if it exists) by reading the Start and End Cuts (NOT chapters)
        /// </summary>
        /// <param name="sourceVPRJ">Source VPRJ file path</param>
        /// <param name="destEDL">Destination EDL file path</param>
        /// <returns>True if successful</returns>
        public static bool ConvertVPRJtoEDL(string sourceVPRJ, string destEDL, Log jobLog)
        {
            if (String.IsNullOrWhiteSpace(sourceVPRJ) || String.IsNullOrWhiteSpace(destEDL))
                return false; // Invalid configuration

            try
            {
                StreamReader vprj = new System.IO.StreamReader(sourceVPRJ);
                string line = "", edlEntries = "";
                while (!String.IsNullOrWhiteSpace(line = vprj.ReadLine()))
                {
                    // Format of VPRJ Cuts
                    // <cut Sequence="2" CutStart="00:13:08;14" CutEnd="00:16:26;13" Elapsed="00:09:52;17">
                    if (line.Contains("CutStart=") && line.Contains("CutEnd="))
                    {
                        string startTime = line.Substring(line.IndexOf("CutStart=") + "CutStart=".Length + 1, "hh:mm:ss;ff".Length);
                        string endTime = line.Substring(line.IndexOf("CutEnd=") + "CutEnd=".Length + 1, "hh:mm:ss;ff".Length);

                        TimeSpan cutStart = TimeSpan.ParseExact(startTime, @"hh\:mm\:ss\;ff", CultureInfo.InvariantCulture);
                        TimeSpan cutEnd = TimeSpan.ParseExact(endTime, @"hh\:mm\:ss\;ff", CultureInfo.InvariantCulture);

                        jobLog.WriteEntry("VPRJtoEDL: Cut segment" + " Start:" + cutStart.TotalSeconds.ToString(CultureInfo.InvariantCulture) + " Stop:" + cutEnd.TotalSeconds.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                        edlEntries += cutStart.TotalSeconds.ToString(CultureInfo.InvariantCulture) + "\t" + cutEnd.TotalSeconds.ToString(CultureInfo.InvariantCulture) + "\t" + "0" + "\r\n";
                    }
                }

                // Write to EDL file
                if (!String.IsNullOrWhiteSpace(edlEntries)) // Check if these is something to write
                {
                    jobLog.WriteEntry("Writing EDL file to " + destEDL, Log.LogEntryType.Information);
                    System.IO.File.WriteAllText(destEDL, edlEntries, System.Text.Encoding.UTF8);
                }
                else
                    jobLog.WriteEntry("No valid entries found to write, not creating EDL file", Log.LogEntryType.Warning);

                return true;
            }
            catch(Exception e)
            {
                jobLog.WriteEntry("Error converting VPRJ to EDL. Error : " + e.ToString(), Log.LogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// Creates and EDL file using the list of segments provided to keep (rest will be cut)
        /// </summary>
        /// <param name="keepList">List of segments to keep</param>
        /// <returns>True if successful</returns>
        public bool CreateEDLFile(List<KeyValuePair<float, float>> keepList)
        {
            FileIO.TryFileDelete(_EDLFile); // Just incase it exists, delete it

            try
            {
                if (keepList.Count == 0)
                    return true; // Nothing to do here

                string edlEntries = "";

                float lastKeepEnd = 0;
                foreach (KeyValuePair<float, float> keep in keepList)
                {
                    // Compensate for any initial skip seconds if requested
                    float cutStart = (lastKeepEnd - _initialSkipSeconds < 0 ? 0 : lastKeepEnd - _initialSkipSeconds); // We cut at the end of the last keep
                    float cutEnd = (keep.Key - _initialSkipSeconds < 0 ? 0 : keep.Key - _initialSkipSeconds); // We end the start of the current keep
                    lastKeepEnd = (keep.Value - _initialSkipSeconds < 0 ? 0 : keep.Value - _initialSkipSeconds); // Update the end of the keep

                    // Dont' keep small segments (from keep values)
                    if ((keep.Value - keep.Key) <= _minimumSegmentSize)
                    {
                        _jobLog.WriteEntry(this, ("CreateEDL: Skipping keep segment, too small") + " Start:" + keep.Key.ToString(CultureInfo.InvariantCulture) + " Stop:" + keep.Value.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                        continue;
                    }
                    else // EDL format is -> CutStart   CutEnd  0
                    {
                        _jobLog.WriteEntry(this, ("CreateEDL: Cutting segment") + " Start:" + cutStart.ToString(CultureInfo.InvariantCulture) + " Stop:" + cutEnd.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                        edlEntries += cutStart.ToString(CultureInfo.InvariantCulture) + "\t" + cutEnd.ToString(CultureInfo.InvariantCulture) + "\t" + "0" + "\r\n";
                    }
                }

                // Add the last end entry
                // Check for 0 file duration, since some .TS are not read by MediaInfo
                if (_duration <= 0)
                {
                    _jobStatus.ErrorMsg = "CreateEDL Failed, cannot read video length - set to 0";
                    _jobLog.WriteEntry(this, ("CreateEDL Failed, cannot read video length - set to 0"), Log.LogEntryType.Error);
                    _jobStatus.PercentageComplete = 0;
                    _EDLFile = ""; // No EDL file
                    return false;
                }
                else if ((_duration - lastKeepEnd) <= _minimumSegmentSize)
                    _jobLog.WriteEntry(this, ("CreateEDL: Skipping end segment, too small") + " Start:" + lastKeepEnd.ToString(CultureInfo.InvariantCulture) + " Stop:" + _duration.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                else
                {
                    _jobLog.WriteEntry(this, ("CreateEDL: Cutting end segment") + " Start:" + lastKeepEnd.ToString(CultureInfo.InvariantCulture) + " Stop:" + _duration.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                    edlEntries += lastKeepEnd.ToString(CultureInfo.InvariantCulture) + "\t" + _duration.ToString(CultureInfo.InvariantCulture) + "\t" + "0" + "\r\n";
                }

                // Write to EDL file
                if (!String.IsNullOrWhiteSpace(edlEntries)) // Check if these is something to write
                {
                    _jobLog.WriteEntry(this, "Writing EDL file to " + _EDLFile, Log.LogEntryType.Information);
                    System.IO.File.WriteAllText(_EDLFile, edlEntries, System.Text.Encoding.UTF8);
                }
                else
                    _jobLog.WriteEntry(this, "No valid entries found to write, not creating EDL file", Log.LogEntryType.Warning);

                return true;
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, ("Error writing EDL File. Error : ") + e.ToString(), Log.LogEntryType.Error);
                _jobStatus.ErrorMsg = "Error writing EDL file";
                _EDLFile = ""; // No EDL file
                return false;
            }
        }
        
        /// <summary>
        /// Reads the EDL file and Parses the data into a KeyValuePair(float,float) list in the format: Start Time in seconds, End Time seconds
        /// Each entry refers to the Start and End time of the video to KEEP.
        /// Remember to check the return list for an empty list
        /// </summary>
        /// <param name="keepList">Reference to KeyValue pair list to populate with the segments of video to keep</param>
        /// <returns>True if successful, false if there is an error (which is set in the JobStatus)</returns>
        public bool ParseEDLFile(ref List<KeyValuePair<float, float>> keepList)
        {
            try
            {
                StreamReader edlS = new System.IO.StreamReader(_EDLFile);
                string line;
                float lastCut = 0;
                bool onlyHeadCut = false;
                while ((line = edlS.ReadLine()) != null)
                {
                    string[] cuts = Regex.Split(line, @"\s+");
                    if (cuts.Length == 3)
                    {
                        if (cuts[0] != cuts[1])
                        {
                            float cutStart, cutEnd;
                            if ((float.TryParse(cuts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out cutStart)) && (float.TryParse(cuts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out cutEnd)))
                            {
                                // Compensate for any initial skip seconds if requested
                                cutStart = (cutStart - _initialSkipSeconds < 0 ? 0 : cutStart - _initialSkipSeconds);
                                cutEnd = (cutEnd - _initialSkipSeconds < 0 ? 0 : cutEnd - _initialSkipSeconds);

                                if (cutEnd > cutStart)
                                {
                                    if (cutStart <= _minimumSegmentSize)
                                    {
                                        _jobLog.WriteEntry(this, ("ParseEDL: Skipping initial segment, too small") + " Start:" + lastCut.ToString(CultureInfo.InvariantCulture) + " Stop:" + cutStart.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                                        lastCut = cutEnd;
                                        onlyHeadCut = true; // Set this if we have a header to be cut
                                    }
                                    else if (cutStart > lastCut)
                                    {
                                        if ((cutStart - lastCut) > _minimumSegmentSize) // Don't do very small cuts as it fails the commercial removal sometimes
                                        {
                                            _jobLog.WriteEntry(this, ("ParseEDL: Keeping segment") + " Start:" + lastCut.ToString(CultureInfo.InvariantCulture) + " Stop:" + cutStart.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                                            keepList.Add(new KeyValuePair<float, float>(lastCut, cutStart));
                                        }
                                        else
                                            _jobLog.WriteEntry(this, ("ParseEDL: Skipping segment, too small") + " Start:" + lastCut.ToString(CultureInfo.InvariantCulture) + " Stop:" + cutStart.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                                        lastCut = cutEnd;
                                        onlyHeadCut = false; // we have a follow up cut
                                    }
                                }
                            }
                        }
                    }
                }
                edlS.Close();
                edlS.Dispose();
                if (keepList.Count > 0 || onlyHeadCut) // check if we have just a head to be cut or more than 1 cut (the end needs to be cut)
                {
                    //Check for 0 file duration, since some .TS are not read by MediaInfo
                    if (_duration <= 0)
                    {
                        _jobStatus.ErrorMsg = "Parse EDL Failed, cannot read video length - set to 0";
                        _jobLog.WriteEntry(this, ("ParseEDL Failed, cannot read video length - set to 0"), Log.LogEntryType.Error);
                        _jobStatus.PercentageComplete = 0;
                        return false;
                    }
                    else
                    {
                        if ((_duration - lastCut) > _minimumSegmentSize) // Don't do very small cuts as it fails the commercial removal sometimes
                        {
                            _jobLog.WriteEntry(this, ("ParseEDL: Keeping end segment") + " Start:" + lastCut.ToString(CultureInfo.InvariantCulture) + " Stop:" + _duration.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                            keepList.Add(new KeyValuePair<float, float>(lastCut, _duration));
                        }
                        else
                            _jobLog.WriteEntry(this, ("ParseEDL: Skipping end segment, too small") + " Start:" + lastCut.ToString(CultureInfo.InvariantCulture) + " Stop:" + _duration.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                    }
                }
            }
            catch (Exception e)
            {
                _jobStatus.ErrorMsg = "Commercial Removal failed with EDL error";
                _jobLog.WriteEntry(this, ("Commercial Removal failed with EDL error.\r\n" + e.ToString()), Log.LogEntryType.Error);
                _jobStatus.PercentageComplete = 0;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks the EDL and EDLP file for consistency and existance. If it doesn't checkout it clears the EDLFile property and deletes the EDL File
        /// </summary>
        /// <returns>True if EDL file checks out</returns>
        public bool CheckAndUpdateEDL(string EDLFilePath)
        {
            string edlFile = EDLFilePath;
            string edlPFile = Path.GetFileNameWithoutExtension(edlFile) + ".edlp";
            string newEdl = "";

            if (_forceEDL)
                _jobLog.WriteEntry(this, ("ForceEDL Set, using EDL file for commercial removal"), Log.LogEntryType.Information);
            else if (_forceEDLP)
                _jobLog.WriteEntry(this, ("ForceEDLP Set, using EDLP file for commercial removal"), Log.LogEntryType.Information);

            // TS files use EDL others use EDLP
            // If forceEDLP and forceEDL are set, EDL wins
            if (!_forceEDL)
            {
                if ((_forceEDLP && File.Exists(edlPFile)) || ((FilePaths.CleanExt(_videoFileName) != ".ts") && File.Exists(edlPFile))) // use EDLP if forced
                {
                    _jobLog.WriteEntry(this, ("Using EDLP file for commercial removal"), Log.LogEntryType.Information);
                    Util.FileIO.TryFileDelete(edlFile);
                    try
                    {
                        FileIO.MoveAndInheritPermissions(edlPFile, edlFile);
                    }
                    catch (Exception e)
                    {
                        _jobLog.WriteEntry(this, ("Unable to copy EDL File") + "\r\nError : " + e.ToString(), Log.LogEntryType.Error);
                        _jobStatus.ErrorMsg = "Unable to copy EDL file";
                        Util.FileIO.TryFileDelete(_EDLFile); // Delete EDL file
                        _EDLFile = ""; // No EDL file
                        return false;
                    }
                }
                else
                {
                    Util.FileIO.TryFileDelete(edlPFile); // Delete redundant EDLP file
                    _jobLog.WriteEntry(this, ("Skipping EDLP, using EDL file for commercial removal"), Log.LogEntryType.Information);
                    _EDLFile = ""; // No EDL file for now
                }
            }
            else
                _jobLog.WriteEntry(this, ("Using EDL file for commercial removal"), Log.LogEntryType.Information);

            if (File.Exists(edlFile))
            {
                try
                {
                    System.IO.StreamReader edlS = new System.IO.StreamReader(edlFile);
                    string line;
                    while ((line = edlS.ReadLine()) != null)
                    {
                        string[] cuts = Regex.Split(line, @"\s+");
                        if (cuts.Length == 3)
                        {
                            if (cuts[0] != cuts[1])
                                newEdl += line + "\r\n";
                        }
                    }
                    edlS.Close();
                    edlS.Dispose();
                    if (!String.IsNullOrWhiteSpace(newEdl)) // if blank, there's no EDL file but scanning was successful, so continue
                    {
                        System.IO.File.WriteAllText(edlFile, newEdl);
                        _EDLFile = edlFile; // We're successful
                    }
                    else
                    {
                        _jobLog.WriteEntry(this, ("Empty EDL File"), Log.LogEntryType.Warning);
                        Util.FileIO.TryFileDelete(_EDLFile); // Delete EDL file
                        _EDLFile = ""; // No EDL file
                        return true; // We are still good to go, just no commerical cutting
                    }
                }
                catch
                {
                    _jobLog.WriteEntry(this, ("Invalid EDL File"), Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "Invalid EDL file";
                    Util.FileIO.TryFileDelete(_EDLFile); // Delete EDL file
                    _EDLFile = ""; // No EDL file
                    return false;
                }

            }
            else
            {
                _jobLog.WriteEntry(this, ("Cannot find EDL File"), Log.LogEntryType.Warning);
                _jobStatus.ErrorMsg = "Cannot find EDL file";
                _EDLFile = ""; // No EDL file
                return true; // We are still good to go, no commercials found
            }

            // Test the EDL file validity
            if (!String.IsNullOrWhiteSpace(_EDLFile)) // If we have a EDL file here by now it should be valid
            {
                _jobLog.WriteEntry(this, "Testing EDL File Validity", Log.LogEntryType.Debug);
                List<KeyValuePair<float, float>> keepList = new List<KeyValuePair<float, float>>();
                if (!ParseEDLFile(ref keepList))
                {
                    Util.FileIO.TryFileDelete(_EDLFile); // Delete EDL file
                    _EDLFile = ""; // No EDL file
                    _jobLog.WriteEntry(this, "Invalid EDL file detected", Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "Invalid EDL file detected";
                    return false;
                }

                // Check if the CutList is empty, if so continue as if there is no EDL file
                if (keepList.Count <= 0)
                {
                    _jobLog.WriteEntry(this, "Empty EDL file detected", Log.LogEntryType.Warning);
                    Util.FileIO.TryFileDelete(_EDLFile); // Delete EDL file
                    _EDLFile = ""; // No EDL file
                    return true; // We still continue, empty file is not an error
                }
            }
            else
            {
                _jobLog.WriteEntry(this, "No Valid EDL file detected", Log.LogEntryType.Warning);
                Util.FileIO.TryFileDelete(_EDLFile); // Delete EDL file
                _EDLFile = ""; // No EDL file
                return true; // We still continue, no file is not an error
            }

            return true;
        }

        /// <summary>
        /// Converts the EDL file to a Chapter (.CHAP) file format (preserving the original EDL file)
        /// It will adjust the chapter markers automatically based on whether the video aleady been cut
        /// </summary>
        /// <param name="alreadyCut">True if the video has already been cut with the EDL file</param>
        /// <returns>True on success</returns>
        public bool ConvertEDLToChapters(bool alreadyCut)
        {
            string chapterLists = "";
            string xmlChapterLists = 
@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<!-- GPAC 3GPP Text Stream -->
<TextStream version=""1.1"">
<TextStreamHeader width=""400"" height=""80"" layer=""0"" translation_x=""0"" translation_y=""0"">
<TextSampleDescription horizontalJustification=""center"" verticalJustification=""bottom"" backColor=""00 00 00 00"" verticalText=""no"" fillTextRegion=""no"" continuousKaraoke=""no"" scroll=""None"">
<FontTable>
<FontTableEntry fontName=""Serif"" fontID=""1""/>
</FontTable>
<TextBox top=""0"" left=""0"" bottom=""0"" right=""0""/>
<Style styles="""" fontID=""1"" fontSize=""18"" color=""ff ff ff ff""/>
</TextSampleDescription>
</TextStreamHeader>
";

            if (File.Exists(_EDLFile))
            {
                try
                {
                    // Parse the EDL file into start and end segments i.e. chapters
                    List<KeyValuePair<float, float>> keepList = new List<KeyValuePair<float, float>>();
                    if (ParseEDLFile(ref keepList))
                    {
                        long chapCount = 1;
                        float totalCut = 0;
                        float lastKeepEnd = 0;

                        foreach (KeyValuePair<float, float> keep in keepList)
                        {
                            float chapterMarker;

                            if (alreadyCut) // The video is already but, we have to compensate for the cut time and just mark the beginning of each chapter
                            {
                                totalCut += keep.Key - lastKeepEnd; // Keep track of how much video has been cut between the chapters
                                chapterMarker = keep.Key - totalCut; // Compensate the chapters start with the total amount cut between chapters
                                lastKeepEnd = keep.Value; // Keep track of end of last chapter
                            }
                            else // video is not cut, each chapter just corresponds to the start of each keep segment
                                chapterMarker = keep.Key;

                            // Chapter file format common to MKVMerge and MP4Box - This format consists of pairs of lines that start with 'CHAPTERxx=' and 'CHAPTERxxNAME=' respectively
                            // CHAPTER01=00:00:00.000
                            // CHAPTER01NAME=Intro
                            // CHAPTER02=00:02:30.000
                            // CHAPTER02NAME=Baby prepares to rock
                            // CHAPTER03=00:02:42.300
                            // CHAPTER03NAME=Baby rocks the house
                            chapterLists += "CHAPTER" + chapCount.ToString("00") + "=" + TimeSpan.FromSeconds(chapterMarker).ToString(@"hh\:mm\:ss\.fff") + "\r\n";
                            chapterLists += "CHAPTER" + chapCount.ToString("00") + "NAME=Chapter " + chapCount.ToString() + "\r\n";

                            // Chapter file format for apple iTunes and MP4Box compatability
                            /* http://forum.doom9.org/showthread.php?t=158296
                             * http://gpac.wp.mines-telecom.fr/mp4box/ttxt-format-documentation/
                                <?xml version="1.0" encoding="UTF-8" ?>
                                <!-- GPAC 3GPP Text Stream -->
                                <TextStream version="1.1">
                                <FontTable>
                                <FontTableEntry fontName="Arial" fontID="1"/>
                                </FontTable>
                                <TextSample sampleTime="00:00:00.000">Intro</TextSample>
                                <TextSample sampleTime="00:01:00.000">Middle</TextSample>
                                <TextSample sampleTime="00:02:00.000">End</TextSample>
                                <TextSample sampleTime="00:06:00.000"/>
                                </TextStream>
                             */
                            xmlChapterLists += @"<TextSample sampleTime=""" + TimeSpan.FromSeconds(chapterMarker).ToString(@"hh\:mm\:ss\.fff") + @""">Chapter " + chapCount.ToString() + @"</TextSample>" + "\r\n";

                            chapCount++;
                        }

                        if (chapCount > 1) // there was something
                        {
                            /*
                             * It needs to end with a blank entry with the time set of the duration of the file otherwise it leads to a wrong total file time
                             * <TextSample sampleTime="00:06:00.000"/>
                             * https://sourceforge.net/p/gpac/discussion/287547/thread/102f2531/#e8ee
                             */
                            if (_duration > 0)
                                xmlChapterLists += @"<TextSample sampleTime=""" + TimeSpan.FromSeconds(_duration).ToString(@"hh\:mm\:ss\.fff") + @"""/>" + "\r\n";
                            else
                                _jobLog.WriteEntry(this, "Total video time duration 0, this may lead to incorrect wrong file duration while writing chapters", Log.LogEntryType.Warning);

                            // Finish it off
                            xmlChapterLists += @"</TextStream>";

                            // Write to chap file
                            _jobLog.WriteEntry(this, "Writing chapters to " + CHAPFilePath(), Log.LogEntryType.Information);
                            FileIO.TryFileDelete(CHAPFilePath()); // Just incase it exists, delete it
                            System.IO.File.WriteAllText(CHAPFilePath(), chapterLists, System.Text.Encoding.UTF8);

                            _jobLog.WriteEntry(this, "Writing XML chapters to " + XMLCHAPFilePath(), Log.LogEntryType.Information);
                            FileIO.TryFileDelete(XMLCHAPFilePath()); // Just incase it exists, delete it
                            System.IO.File.WriteAllText(XMLCHAPFilePath(), xmlChapterLists, System.Text.Encoding.UTF8);

                            _CHAPFile = CHAPFilePath(); // We're successful
                            _XMLCHAPFile = XMLCHAPFilePath(); // We're successful

                            return true;
                        }
                        else
                        {
                            _jobLog.WriteEntry(this, ("Empty EDL File"), Log.LogEntryType.Error);
                            _jobStatus.ErrorMsg = "Empty EDL file";
                            return false;
                        }
                    }
                    else
                    {
                        _jobLog.WriteEntry(this, ("Invalid EDL File"), Log.LogEntryType.Error);
                        _jobStatus.ErrorMsg = "Invalid EDL file";
                        return false;
                    }
                }
                catch (Exception e)
                {
                    _jobLog.WriteEntry(this, ("Error writing CHAP File. Error : ") + e.ToString(), Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "Error writing CHAP file";
                    return false;
                }
            }
            else
            {
                _jobLog.WriteEntry(this, ("Cannot find EDL File"), Log.LogEntryType.Error);
                _jobStatus.ErrorMsg = "Cannot find EDL file";
                return false;
            }
        }

        protected string CHAPFilePath()
        {
            return Util.FilePaths.GetFullPathWithoutExtension(_videoFileName) + ".chap";
        }

        protected string XMLCHAPFilePath()
        {
            return Util.FilePaths.GetFullPathWithoutExtension(_videoFileName) + ".ttxt";
        }

        /// <summary>
        /// Returns to the path to a valid EDL file if CheckEDL was successful
        /// </summary>
        public string EDLFile
        { get { return _EDLFile; } }

        /// <summary>
        /// /// Returns to the path to a valid Nero Chapter file if ConvertEDLToChapters was successful
        /// </summary>
        public string CHAPFile
        { get { return _CHAPFile; } }

        /// <summary>
        /// /// Returns to the path to a valid XML Chapter file if ConvertEDLToChapters was successful
        /// </summary>
        public string XMLCHAPFile
        { get { return _XMLCHAPFile; } }
    }
}
