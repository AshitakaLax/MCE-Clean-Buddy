using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

using MCEBuddy.Util;
using MCEBuddy.Globals;
using MCEBuddy.AppWrapper;
using MCEBuddy.CommercialScan;

namespace MCEBuddy.Transcode
{
    public class CCandSubtitles
    {
        private string _profile;
        private JobStatus _jobStatus;
        private Log _jobLog;
        private string _extractedSRTFile = "";

        public CCandSubtitles(string profile, JobStatus jobStatus, Log jobLog)
        {
            _profile = profile;
            _jobLog = jobLog;
            _jobStatus = jobStatus;
        }

        public string SRTFile
        { get { return _extractedSRTFile; } }

        /// <summary>
        /// Extract closed captions from the source file into the temp path with same filename and SRT format
        /// </summary>
        /// <param name="sourceFile">Original file</param>
        /// <param name="workingPath">Temp folder</param>
        /// <param name="ccOptions">CC extraction options</param>
        /// <param name="startTrim">Trim initial</param>
        /// <param name="endTrim">Trim end</param>
        /// <param name="ccOffset">Offset initial</param>
        /// <returns>True if successful</returns>
        public bool ExtractCC(string sourceFile, string workingPath, string ccOptions, int startTrim, int endTrim, double ccOffset)
        {
            // TODO: Do we need process WTV files separately with -wtvmpeg2 option and also Teletext and DVB with -teletext and -codec dvb options?

            _jobLog.WriteEntry(this, ("Extracting Closed Captions as SRT file"), Log.LogEntryType.Information);
            _jobLog.WriteEntry(this, "Source File : " + sourceFile, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Working Path " + workingPath, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "CC Options : " + ccOptions, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Start Trim : " + startTrim.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Stop Trim : " + endTrim.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Offset : " + ccOffset.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            if (String.IsNullOrWhiteSpace(ccOptions))
                return true; // nothing to do, accidentally called

            // Output SRT file has to be working directory, will be copied to output afterwards
            string tmpExtractedSRTFile = Path.Combine(workingPath, Path.GetFileNameWithoutExtension(sourceFile)) + ".srt";
            string ccExtractorParams = "";
            string field = "";
            string channel = "";

            if (ccOptions.ToLower() != "default") // let ccextrator choose defaults if specified
            {
                // CCOptions are encoded as field,channel
                field = ccOptions.Split(',')[0];
                channel = ccOptions.Split(',')[1];
            }

            if (field == "1" || field == "2")
                ccExtractorParams += " -" + field; // Field is -1 or -2 (we don't support -12 since it creates 2 SRT files and there's no way to handle that)

            if (channel == "2")
                ccExtractorParams += " -cc2"; // By default is Channel 1, there is no parameter for it

            // NOTE: delay does not work reliably do not use
            // Adjust for any offset required during extraction (opposite direction, so -ve)
            if (ccOffset != 0)
                ccExtractorParams += " -delay " + (-ccOffset * 1000).ToString(CultureInfo.InvariantCulture); // ccOffset is in seconds while -delay required milliseconds

            // Get the length of the video, needed to calculate end point
            float Duration = 0;
            Duration = VideoParams.VideoDuration(sourceFile);
            if (Duration <= 0)
            {
                FFmpegMediaInfo ffmpegStreamInfo = new FFmpegMediaInfo(sourceFile, _jobStatus, _jobLog);
                if (ffmpegStreamInfo.Success && !ffmpegStreamInfo.ParseError)
                {
                    // Converted file should contain only 1 audio stream
                    Duration = ffmpegStreamInfo.MediaInfo.VideoInfo.Duration;
                    _jobLog.WriteEntry(this, ("Video duration") + " : " + Duration.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Information);

                    if (Duration == 0)
                    {
                        _jobLog.WriteEntry(this, ("Video duration 0"), Log.LogEntryType.Error);
                        return false;
                    }
                }
                else
                {
                    _jobLog.WriteEntry(this, ("Cannot read video duration"), Log.LogEntryType.Error);
                    return false;
                }
            }

            // Set the start trim time (it only accepts +ve)
            if (startTrim > 0)
                ccExtractorParams += " -startat " + TimeSpan.FromSeconds((double)startTrim).ToString();
            else if (startTrim < 0)
                _jobLog.WriteEntry(this, "Skipping start trim since it's negative", Log.LogEntryType.Warning);

            // Set the end trim time
            if (endTrim > 0)
            {
                // startTime = startTrim, stopTime = video_duration - endTrim
                int encDuration = (((int)Duration) - endTrim) - (startTrim); // by default _startTrim is 0

                ccExtractorParams += " -endat " + TimeSpan.FromSeconds((double)encDuration).ToString();
            }
            else if (endTrim < 0)
                _jobLog.WriteEntry(this, "Skipping end trim since it's negative", Log.LogEntryType.Warning);

            // Set the input file
            ccExtractorParams += " " + Util.FilePaths.FixSpaces(sourceFile);

            // set output file
            ccExtractorParams += " -o " + Util.FilePaths.FixSpaces(tmpExtractedSRTFile);

            // Run the command
            CCExtractor ccExtractor = new CCExtractor(ccExtractorParams, _jobStatus, _jobLog);
            ccExtractor.Run();

            if (!ccExtractor.Success) // check for termination/success
            {
                _jobLog.WriteEntry("CCExtractor failed. Disabling detection , retrying using TS format", Log.LogEntryType.Warning);

                // TODO: Right format to pick (TS/ES/PS etc) - doing TS for now
                // Sometimes it doesn't detect the format correctly so try to force it (TS)
                ccExtractorParams += " -ts";
                ccExtractor = new CCExtractor(ccExtractorParams, _jobStatus, _jobLog);
                ccExtractor.Run();

                if (!ccExtractor.Success) // check for termination/success
                {
                    _jobLog.WriteEntry("CCExtractor failed to extract closed captions", Log.LogEntryType.Error);
                    return false;
                }
            }

            // Check if we have a format identification error (sometimes ccextractor misidentifies files)
            if ((Util.FileIO.FileSize(tmpExtractedSRTFile) <= 0) && ccExtractor.FormatReadingError)
            {
                FileIO.TryFileDelete(tmpExtractedSRTFile); // Delete the empty file
                _jobLog.WriteEntry(this, "No SRT file found and format error detected. CCExtractor may not have identified the format correctly, forcing TS format and retrying extraction.", Log.LogEntryType.Warning);
                ccExtractorParams += " -in=ts"; // force TS format and retry
                ccExtractor = new CCExtractor(ccExtractorParams, _jobStatus, _jobLog);
                ccExtractor.Run();

                if (!ccExtractor.Success) // check for termination/success
                {
                    _jobLog.WriteEntry(("Retrying: CCExtractor failed to extract closed captions"), Log.LogEntryType.Error);
                    return false;
                }
            }

            // Check for empty file
            if (Util.FileIO.FileSize(tmpExtractedSRTFile) <= 0)
            {
                FileIO.TryFileDelete(tmpExtractedSRTFile); // Delete the empty file
                _jobLog.WriteEntry(this, ("No valid SRT file found"), Log.LogEntryType.Warning);
                return true; // no error
            }

            _extractedSRTFile = tmpExtractedSRTFile; // We got the file
            _jobLog.WriteEntry(this, ("Extracted closed captions to") + " " + _extractedSRTFile, Log.LogEntryType.Information);

            return true;
        }

        /// <summary>
        /// Extracts subtitles from a video file into a SRT format (with same name) and cleans it up.
        /// It will overwrite any existing SRT files
        /// If there are multiple subtitles it extracts them into multiple files with incremental names.
        /// </summary>
        /// <param name="sourceFile">Path to video file</param>
        /// <param name="offset">Offset of the subtitles during extraction</param>
        /// <param name="overWrite">True to overwrite existing SRT files, false to create new ones with incremental names</param>
        /// <param name="languageExtractList">List of 3 digit language codes to extract (blank to extract all, unnamed languages will always be extracted)</param>
        /// <returns>True if successful</returns>
        public bool ExtractSubtitles(string sourceFile, string workingPath, int startTrim, int endTrim, double offset, bool overWrite, List<string> languageExtractList)
        {
            _jobLog.WriteEntry(this, ("Extracting Subtitles from " + sourceFile + " into SRT file"), Log.LogEntryType.Information);
            _jobLog.WriteEntry(this, "Source File : " + sourceFile, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Working Path " + workingPath, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Start Trim : " + startTrim.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Stop Trim : " + endTrim.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Offset : " + offset.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            if (String.IsNullOrEmpty(sourceFile))
                return true; // nothing to do

            if (!File.Exists(sourceFile))
            {
                _jobLog.WriteEntry(this, ("File does not exist " + sourceFile), Log.LogEntryType.Warning);
                return true; //nothing to process
            }

            FFmpegMediaInfo mediaInfo = new FFmpegMediaInfo(sourceFile, _jobStatus, _jobLog);
            if (!mediaInfo.Success || mediaInfo.ParseError)
            {
                _jobLog.WriteEntry(this, ("Error reading subtitle info from file"), Log.LogEntryType.Error);
                return false;
            }

            _jobLog.WriteEntry(this, "Found " + mediaInfo.SubtitleTracks.ToString() + " Subtitle tracks, extract only the first matching track", Log.LogEntryType.Debug);

            bool extractedSubtitle = false;

            for (int i = 0; i < mediaInfo.SubtitleTracks; i++)
            {
                if (extractedSubtitle) // Only extract and use one subtitle (sometimes chapter tracks are misidentified as subtitle tracks)
                    continue;

                // Build the command line
                string parameters = "";
                string outputSRTFile = ""; // Using Serviio subtitle filename format (filename.srt or filename_language.srt or filename_uniquenumber.srt)

                // Check for language comparison if required
                if (languageExtractList != null)
                {
                    if (languageExtractList.Count > 0) // If list is empty, we extract all
                    {
                        _jobLog.WriteEntry(this, "Subtitle language extraction list -> " + String.Join(",", languageExtractList.ToArray()), Log.LogEntryType.Debug);

                        if (!String.IsNullOrWhiteSpace(mediaInfo.MediaInfo.SubtitleInfo[i].Language)) // check if we have a language defined for this track
                        {
                            if (!languageExtractList.Contains(mediaInfo.MediaInfo.SubtitleInfo[i].Language)) // This language is not in the list of extraction
                            {
                                _jobLog.WriteEntry(this, "Skipping subtitle extraction since subtitle language >" + mediaInfo.MediaInfo.SubtitleInfo[i].Language + "< is NOT in the subtitle language list", Log.LogEntryType.Warning);
                                continue; // Skip this subtitle track
                            }
                        }
                        else
                            _jobLog.WriteEntry(this, "Extracting subtitle since there is no language defined for track", Log.LogEntryType.Debug);
                    }
                }

                // Check for existing SRT files
                if (overWrite)
                {
                    outputSRTFile = Path.Combine(workingPath, Path.GetFileNameWithoutExtension(sourceFile)) + (i > 0 ? (String.IsNullOrWhiteSpace(mediaInfo.MediaInfo.SubtitleInfo[i].Language) ? "_" + i.ToString() : "_" + mediaInfo.MediaInfo.SubtitleInfo[i].Language) : "") + ".srt"; // First file user default name, then try to name with language first, if not give a unique number
                    parameters += " -y";
                }
                else // Create a unique SRT file name
                {
                    int existingSRTCount = 0;
                    outputSRTFile = Path.Combine(workingPath, Path.GetFileNameWithoutExtension(sourceFile)) + ".srt"; // Try default name
                    while (File.Exists(outputSRTFile))
                    {
                        _jobLog.WriteEntry(this, "Subtitle file " + outputSRTFile + " exists, creating a new unique SRT filename", Log.LogEntryType.Debug);
                        outputSRTFile = Path.Combine(workingPath, Path.GetFileNameWithoutExtension(sourceFile)) + (String.IsNullOrWhiteSpace(mediaInfo.MediaInfo.SubtitleInfo[i].Language) ? "_" + existingSRTCount.ToString() : "_" + mediaInfo.MediaInfo.SubtitleInfo[i].Language + (existingSRTCount > 0 ? existingSRTCount.ToString() : "")) + ".srt"; // Create a unique SRT filename, try with language first, if not give a unique identifier, avoid a loop
                        existingSRTCount++;
                    }
                }

                // Build ffmpeg command line
                parameters += " -i " + FilePaths.FixSpaces(sourceFile);
                parameters += " -an -vn";
                parameters += " -map 0:" + mediaInfo.MediaInfo.SubtitleInfo[i].Stream.ToString(); // Subtitle steam no
                parameters += " -scodec copy -copyinkf -f srt " + FilePaths.FixSpaces(outputSRTFile);

                // Now extract it
                _jobLog.WriteEntry(this, "Extracting Subtitle " + (i + 1).ToString() + " with language >" + mediaInfo.MediaInfo.SubtitleInfo[i].Language + "< to " + outputSRTFile, Log.LogEntryType.Debug);
                FFmpeg ffmpeg = new FFmpeg(parameters, _jobStatus, _jobLog);
                ffmpeg.Run();
                if (!ffmpeg.Success)
                {
                    FileIO.TryFileDelete(outputSRTFile); // Delete partial file

                    // Backup, try using MP4Box instead to extract it
                    _jobLog.WriteEntry(this, ("FFMPEG failed to extract subtitles into SRT file, retrying using MP4Box"), Log.LogEntryType.Warning);
                    parameters = "-srt " + (mediaInfo.MediaInfo.SubtitleInfo[i].Stream + 1).ToString() + " " + FilePaths.FixSpaces(sourceFile);
                    // MP4Box create an output file called <input>_<track>_text.srt
                    // Check if the output srt exists and then rename it if it does
                    string tempSrtOutput = FilePaths.GetFullPathWithoutExtension(sourceFile) + "_" + (mediaInfo.MediaInfo.SubtitleInfo[i].Stream + 1).ToString() + "_text.srt";
                    bool tempSrtExists = false;
                    if (File.Exists(tempSrtOutput)) // Save the output srt filename if it exists
                    {
                        try
                        {
                            FileIO.MoveAndInheritPermissions(tempSrtOutput, tempSrtOutput + ".tmp");
                        }
                        catch (Exception e)
                        {
                            _jobLog.WriteEntry(this, ("Error extracting subtitles into SRT file.\r\n" + e.ToString()), Log.LogEntryType.Error);
                            return false;
                        }
                        tempSrtExists = true;
                    }

                    // Extract the subtitle
                    MP4Box mp4Box = new MP4Box(parameters, _jobStatus, _jobLog);
                    mp4Box.Run();
                    if (!mp4Box.Success)
                    {
                        _jobLog.WriteEntry(this, ("Error extracting subtitles into SRT file"), Log.LogEntryType.Error);
                        FileIO.TryFileDelete(tempSrtOutput); // Delete partial file
                        if (tempSrtExists)
                            RestoreSavedSrt(tempSrtOutput);
                        return false;
                    }

                    if (FileIO.FileSize(tempSrtOutput) <= 0) // MP4Box always return success even if nothing is extracted, so check if has been extracted
                    {
                        _jobLog.WriteEntry(this, "No or empty Subtitle file " + tempSrtOutput + " extracted by MP4Box, skipping", Log.LogEntryType.Debug);
                        FileIO.TryFileDelete(tempSrtOutput); // Delete empty file
                    }
                    else
                    {
                        // Rename the temp output SRT to the expected name
                        try
                        {
                            FileIO.MoveAndInheritPermissions(tempSrtOutput, outputSRTFile);
                        }
                        catch (Exception e)
                        {
                            _jobLog.WriteEntry(this, ("Error extracting subtitles into SRT file.\r\n" + e.ToString()), Log.LogEntryType.Error);
                            FileIO.TryFileDelete(tempSrtOutput); // Delete partial file
                            if (tempSrtExists)
                                RestoreSavedSrt(tempSrtOutput);
                            return false;
                        }
                    }

                    // Restore temp SRT file if it exists
                    if (tempSrtExists)
                        RestoreSavedSrt(tempSrtOutput);
                }

                if (FileIO.FileSize(outputSRTFile) <= 0) // Check for empty files
                {
                    _jobLog.WriteEntry(this, "Empty Subtitle file " + outputSRTFile + " extracted, deleting it", Log.LogEntryType.Warning);
                    FileIO.TryFileDelete(outputSRTFile); // Delete empty file
                }
                else
                {
                    // Trim the SRT file if required
                    if (startTrim > 0 || endTrim > 0)
                    {
                        // Get the length of the video, needed to calculate end point
                        float Duration = 0;
                        Duration = VideoParams.VideoDuration(sourceFile);
                        if (Duration <= 0)
                        {
                            FFmpegMediaInfo ffmpegStreamInfo = new FFmpegMediaInfo(sourceFile, _jobStatus, _jobLog);
                            if (ffmpegStreamInfo.Success && !ffmpegStreamInfo.ParseError)
                            {
                                // Converted file should contain only 1 audio stream
                                Duration = ffmpegStreamInfo.MediaInfo.VideoInfo.Duration;
                                _jobLog.WriteEntry(this, ("Video duration") + " : " + Duration.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Information);

                                if (Duration == 0)
                                {
                                    _jobLog.WriteEntry(this, ("Video duration 0"), Log.LogEntryType.Error);
                                    return false;
                                }
                            }
                            else
                            {
                                _jobLog.WriteEntry(this, ("Cannot read video duration"), Log.LogEntryType.Error);
                                return false;
                            }
                        }

                        // Trim the subtitle
                        if (!TrimSubtitle(outputSRTFile, workingPath, startTrim, endTrim, Duration, 0))
                        {
                            _jobLog.WriteEntry(this, ("Error trimming SRT file"), Log.LogEntryType.Error);
                            return false;
                        }
                    }

                    // Clean it up and offset the SRT if required
                    if (!SRTValidateAndClean(outputSRTFile, offset))
                    {
                        _jobLog.WriteEntry(this, ("Cannot clean and set offset for SRT file"), Log.LogEntryType.Error);
                        return false;
                    }

                    // Check for empty file
                    if (Util.FileIO.FileSize(outputSRTFile) <= 0)
                    {
                        FileIO.TryFileDelete(outputSRTFile); // Delete the empty file
                        _jobLog.WriteEntry(this, ("No valid SRT file found"), Log.LogEntryType.Warning);
                        continue; // check for the next subtitle track
                    }

                    _extractedSRTFile = outputSRTFile; // Save it
                    _jobLog.WriteEntry(this, "Extracted Subtitle file " + outputSRTFile + ", size [KB] " + (FileIO.FileSize(outputSRTFile) / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                    extractedSubtitle = true; // We have success
                }
            }

            return true;
        }

        public bool TrimSubtitle(string srtFile, string workingPath, int startTrim, int endTrim, float Duration, double offset)
        {
            string TempEDLFile = Path.Combine(workingPath, "TempSRTEDLTrimFile.edl"); // Temp EDL file to trim SRT

            if (String.IsNullOrWhiteSpace(srtFile))
                return true; // Nothing to do

            if ((startTrim <= 0) && (endTrim <= 0))
                return true; // Nothing to do

            if (Duration <= 0)
            {
                _jobLog.WriteEntry(this, ("Invalid video duration"), Log.LogEntryType.Error);
                return false;
            }

            // Create a EDL file which trims beginning and ending
            List<KeyValuePair<float, float>> keepList = new List<KeyValuePair<float, float>>();
            keepList.Add(new KeyValuePair<float, float>((startTrim > 0 ? startTrim : 0), (endTrim > 0 ? Duration - endTrim : Duration))); // Add Start and end trim to create EDL file
            EDL edl = new EDL(_profile, "", GlobalDefs.MAX_VIDEO_DURATION, TempEDLFile, 0, _jobStatus, _jobLog, true); // Ignore minimum segment since we are trimming subtitles
            if (!edl.CreateEDLFile(keepList)) // Create the EDL file
            {
                _jobLog.WriteEntry(this, ("Cannot trim SRT file"), Log.LogEntryType.Error);
                return false;
            }

            // Now use the EDL file to trim the SRT file
            CutWithEDL(TempEDLFile, srtFile, offset, 0, true); // Ignore minimum cut since we are trimming here and not cutting commercials
            FileIO.TryFileDelete(TempEDLFile); // clean up temp edl file
            
            return true;
        }

        /// <summary>
        /// Adjust the Subtitle file to compensate for the cut segments represented in a EDL file
        /// </summary>
        /// <param name="edlFile">EDL file containing segment information</param>
        /// <param name="srtFile">Subtitle file</param>
        /// <param name="ccOffset">Offset to shift the timebase for all subtitles (+ve or -ve)</param>
        /// <param name="segmentOffset">Incremental offset to shift the timebase for subtitles AFTER each segment is cut (+ve or -ve). Used to compensate for progressive shifting in EDL cutting due to GOP boundary issues</param>
        /// <param name="ignoreMinimumCut">Ignore the mininum segment size for cutting (true when not cutting for an encoder, e.g. trimming)</param>
        /// <returns>True if successful</returns>
        public bool CutWithEDL(string edlFile, string srtFile, double ccOffset, double segmentOffset, bool ignoreMinimumCut = false)
        {
            _jobLog.WriteEntry(this, ("Syncing SRT file with EDL file"), Log.LogEntryType.Information);
            _jobLog.WriteEntry(this, "EDL File " + edlFile, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "SRT File " + srtFile, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Offset : " + ccOffset.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Progressive Segment Cut Correction : " + segmentOffset.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            if (String.IsNullOrEmpty(srtFile) || String.IsNullOrEmpty(edlFile))
                return true; // nothing to do

            if (!File.Exists(srtFile) || !File.Exists(edlFile) || FileIO.FileSize(srtFile) == 0 || FileIO.FileSize(edlFile) == 0)
            {
                _jobLog.WriteEntry(this, ("SRT/EDL file does not exist or is 0 bytes in size"), Log.LogEntryType.Warning);
                return true; // nothing to do
            }

            // Taken from MACHYY1's srt_edl_cutter.ps1 (translated and enhanced from PS1 to C#)
            List<List<string>> srt_array = new List<List<string>>();

            // Read the EDL File
            List<KeyValuePair<float, float>> edlKeepList = new List<KeyValuePair<float,float>>();
            EDL edl = new EDL(_profile, "", GlobalDefs.MAX_VIDEO_DURATION, edlFile, 0, _jobStatus, _jobLog, ignoreMinimumCut); // assume infinite duration, Ignore minimum segment since we are trimming subtitles
            if (!edl.ParseEDLFile(ref edlKeepList))
            {
                _jobLog.WriteEntry(this, ("Error processing EDL file"), Log.LogEntryType.Error);
                return false;
            }

            if (edlKeepList.Count == 0)
                return true; // All done, nothing to adjust

            // Read the SRT File
            try
            {
                System.IO.StreamReader srtS = new System.IO.StreamReader(srtFile);
                string line;
                int sequence = 1, temp;
                List<string> line_text = new List<string>();
                double srt_start_time = 0, srt_end_time = 0;
                List<string> srt_line;
                string[] fields;

                while ((line = srtS.ReadLine()) != null)
                {
                    if (line == "") //blank line - so write the info to an array (end of current entry)
                    {
                        if (sequence > 0)
                        {
                            int edl_segment = 0; // If the keep doesn't start at 0 then we have 1 segment already cut
                            double total_time_cut = ccOffset; // Set to offset
                            double last_keep = 0;
                            foreach (KeyValuePair<float, float> keep in edlKeepList)
                            {
                                total_time_cut += keep.Key - last_keep; // Track how much time has been cut so far

                                if (srt_start_time >= keep.Key && srt_start_time <= keep.Value) // record to keep
                                {
                                    srt_line = new List<string>(); // Build the SRT entry
                                    string startTimeCode = (seconds_to_hhmmss(srt_start_time - total_time_cut + (edl_segment * segmentOffset))); // Compensate progressively for each segment
                                    if (srt_end_time > keep.Value)  // SANITY CHECK: end time should not exceed the end of keep video/srt time (otherwise it looks like a burned in video with previous subtitle overlapping the next at the cut boundary)
                                        srt_end_time = keep.Value;
                                    string endTimeCode = (seconds_to_hhmmss(srt_end_time - total_time_cut + (edl_segment * segmentOffset))); // Compensate progressively for each segment.

                                    /* SRT FORMAT:
                                     * SubRip (SubRip Text) files are named with the extension .srt, and contain formatted plain text.
                                     * The time format used is hours:minutes:seconds,milliseconds. The decimal separator used is the comma, since the program was written in France.
                                     * The line break used is often the CR+LF pair.
                                     * Subtitles are numbered sequentially, starting at 1.
                                        Subtitle number
                                        Start time --> End time
                                        Text of subtitle (one or more lines)
                                        Blank line
                                     */
                                    srt_line.Add(sequence.ToString()); // Subtitle No
                                    srt_line.Add(startTimeCode + " --> " + endTimeCode); // Start time --> End time
                                    foreach (string text in line_text) // Text of subtitle (one or more lines)
                                        srt_line.Add(text);
                                    srt_line.Add(""); // Blank line

                                    srt_array.Add(srt_line); // Build the SRT file
                                    sequence++;
                                    break;
                                }

                                edl_segment++;
                                last_keep = keep.Value;
                            }

                            srt_start_time = srt_end_time = 0;
                            line_text.Clear(); // Reset the content
                        }
                    }
                    else if (int.TryParse(line, out temp)) // Line number
                        continue; // do nothing here
                    else if (line.Contains(" --> ")) // Timecodes
                    {
                        line = line.Replace(" --> ", "\t");
                        fields = line.Split('\t');
                        srt_start_time = hhmmss_to_seconds(fields[0]);
                        srt_end_time = hhmmss_to_seconds(fields[1]);
                    }
                    else if (line != "") // Text Content
                    {
                        if (!String.IsNullOrWhiteSpace(line)) // don't add blank lines, it causes programs like MP4Box to choke
                            line_text.Add(line);
                    }
                    else
                        throw new System.ArgumentException("Invalid SRT file format");
                }

                srtS.Close();
                srtS.Dispose();
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, ("Error processing SRT file") + " " + e.ToString(), Log.LogEntryType.Error);
                return false;
            }

            // Delete the original SRT file
            FileIO.TryFileDelete(srtFile);

            try
            {
                // Write the new SRT file
                StreamWriter srtWrite = new System.IO.StreamWriter(srtFile);
                foreach (List<string> srt_line in srt_array)
                    foreach (string entry in srt_line)
                        srtWrite.WriteLine(entry); // Format is already prebuilt above, just dump into the file

                srtWrite.Flush();
                srtWrite.Close();
                srtWrite.Dispose();
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, ("Error writing to SRT file") + " " + e.ToString(), Log.LogEntryType.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate the SRT format and clean up if possible to avoid issues with other programs
        /// </summary>
        /// <param name="srtFile">SRT file to check and clean</param>
        /// <param name="offset">Number of seconds to shift the subtitle time base</param>
        /// <returns>True if successful</returns>
        public bool SRTValidateAndClean(string srtFile, double offset = 0)
        {
            List<List<string>> srt_array = new List<List<string>>();

            _jobLog.WriteEntry(this, ("Validating and cleaning SRT file"), Log.LogEntryType.Information);
            _jobLog.WriteEntry(this, "SRT File " + srtFile, Log.LogEntryType.Debug);

            if (String.IsNullOrEmpty(srtFile))
                return true; // nothing to do

            if (!File.Exists(srtFile) || FileIO.FileSize(srtFile) == 0)
            {
                _jobLog.WriteEntry(this, ("SRT file does not exist or is 0 bytes in size"), Log.LogEntryType.Warning);
                return true; // nothing to do
            }

            // Read the SRT File
            try
            {
                System.IO.StreamReader srtS = new System.IO.StreamReader(srtFile);
                string line;
                int sequence = 1, temp;
                List<string> line_text = new List<string>();
                double srt_start_time = 0, srt_end_time = 0;
                List<string> srt_line;
                string[] fields;

                while ((line = srtS.ReadLine()) != null)
                {
                    if (line == "") //blank line - so write the info to an array (end of current entry)
                    {
                        if (sequence > 0)
                        {
                            if (((srt_start_time - offset) < 0) || ((srt_end_time - offset) < 0))
                            {
                                srt_start_time = srt_end_time = 0;
                                line_text.Clear(); // Reset the content
                                continue; // Skip it, we shouldn't have -ve time stamps
                            }

                            srt_line = new List<string>(); // Build the SRT entry
                            string startTimeCode = (seconds_to_hhmmss(srt_start_time - offset)); // adjust for offset
                            string endTimeCode = (seconds_to_hhmmss(srt_end_time - offset)); // adjust for offset

                            /* SRT FORMAT:
                                * SubRip (SubRip Text) files are named with the extension .srt, and contain formatted plain text.
                                * The time format used is hours:minutes:seconds,milliseconds. The decimal separator used is the comma, since the program was written in France.
                                * The line break used is often the CR+LF pair.
                                * Subtitles are numbered sequentially, starting at 1.
                                Subtitle number
                                Start time --> End time
                                Text of subtitle (one or more lines)
                                Blank line
                                */
                            srt_line.Add(sequence.ToString()); // Subtitle No
                            srt_line.Add(startTimeCode + " --> " + endTimeCode); // Start time --> End time
                            foreach (string text in line_text) // Text of subtitle (one or more lines)
                                srt_line.Add(text);
                            srt_line.Add(""); // Blank line

                            srt_array.Add(srt_line); // Build the SRT file
                            sequence++;

                            srt_start_time = srt_end_time = 0;
                            line_text.Clear(); // Reset the content
                        }
                    }
                    else if (int.TryParse(line, out temp)) // Line number
                        continue; // do nothing here
                    else if (line.Contains(" --> ")) // Timecodes
                    {
                        line = line.Replace(" --> ", "\t");
                        fields = line.Split('\t');
                        try
                        {
                            srt_start_time = hhmmss_to_seconds(fields[0]);
                            srt_end_time = hhmmss_to_seconds(fields[1]);
                        }
                        catch
                        {
                            _jobLog.WriteEntry(this, "Invalid timestamps, skipping entry : " + line, Log.LogEntryType.Warning);
                            srt_start_time = srt_end_time = -9999999999F; // Set to negative so that we skip this entry
                            continue;
                        }
                    }
                    else if (line != "") // Text Content
                    {
                        if (!String.IsNullOrWhiteSpace(line)) // don't add blank lines, it causes programs like MP4Box to choke
                            line_text.Add(line);
                    }
                    else
                        throw new System.ArgumentException("Invalid SRT file format");
                }

                srtS.Close();
                srtS.Dispose();
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, ("Error validating SRT file") + " " + e.ToString(), Log.LogEntryType.Error);
                return false;
            }

            // Delete the original SRT file
            FileIO.TryFileDelete(srtFile);

            try
            {
                // Write the new SRT file
                StreamWriter srtWrite = new System.IO.StreamWriter(srtFile);
                foreach (List<string> srt_line in srt_array)
                    foreach (string entry in srt_line)
                        srtWrite.WriteLine(entry); // Format is already prebuilt above, just dump into the file

                srtWrite.Flush();
                srtWrite.Close();
                srtWrite.Dispose();
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, ("Error writing to clean SRT file") + " " + e.ToString(), Log.LogEntryType.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Restore the SRT (filename conflict) that was saved before running MP4Box to extract SRT file
        /// </summary>
        /// <param name="savedSrtFile"></param>
        private void RestoreSavedSrt(string savedSrtFile)
        {
            // Rename the original temp SRT file if it existed
            try
            {
                FileIO.TryFileDelete(savedSrtFile); // Delete existing file
                FileIO.MoveAndInheritPermissions(savedSrtFile + ".tmp", savedSrtFile);
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, ("Error restoring saved temp SRT file.\r\n" + e.ToString()), Log.LogEntryType.Error);
            }
        }

        private double hhmmss_to_seconds(string hhmmss)
        {
            bool negative = false;
            if (hhmmss[0] == '-') // Check for -ve timestamp
            {
                hhmmss = hhmmss.Substring(1, hhmmss.Length - 1);
                negative = true;
            }

            double hour = 3600 * double.Parse(hhmmss.Substring(0, 2), CultureInfo.InvariantCulture); // throw an expcetion if it's an invalid
            double minute = 60 * double.Parse(hhmmss.Substring(3, 2), CultureInfo.InvariantCulture); // throw an expcetion if it's an invalid
            double second = double.Parse(hhmmss.Substring(6, 2), CultureInfo.InvariantCulture); // throw an expcetion if it's an invalid
            double millisecond = double.Parse(hhmmss.Substring(9, 3), CultureInfo.InvariantCulture) / 1000; // throw an expcetion if it's an invalid

            double secondsTime = hour + minute + second + millisecond;

            if (negative)
                return -secondsTime;
            else
                return secondsTime;
        }

        private string seconds_to_hhmmss(double seconds)
        {
            TimeSpan st = TimeSpan.FromSeconds(seconds);
            if (seconds < 0)
                return st.ToString(@"\-hh\:mm\:ss\,fff", CultureInfo.InvariantCulture); //-00:25:30,978
            else
                return st.ToString(@"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture); //00:25:30,978
        }
    }
}
