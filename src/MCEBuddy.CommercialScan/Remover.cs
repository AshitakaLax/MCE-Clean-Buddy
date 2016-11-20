using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

using MCEBuddy.AppWrapper;
using MCEBuddy.Globals;
using MCEBuddy.VideoProperties;
using MCEBuddy.Util;

namespace MCEBuddy.CommercialScan
{
    public class Remover : EDL
    {
        private string _cutVideo = "";
        private string _uncutVideo = "";
        private string _ext = "";
        private string _workingPath = "";
        private float _duration;
        private JobStatus _jobStatus;
        private Log _jobLog;
        private VideoInfo _remuxedVideoFileInfo;
        private bool _cutMP4Alternative = false;
        private bool _universalCommercialRemover = false;
        private bool _useAVIDemuxMerge = false;
        private bool _useFFMPEGMerge = false;
        private FFmpegMediaInfo _uncutVideoMediaInfo;

        public Remover(string profile, string uncutVideo, string workingPath, string edlFile, float initialSkipSeconds, VideoInfo remuxedVideoFileInfo, JobStatus jobStatus, Log jobLog)
            : base(profile, uncutVideo, remuxedVideoFileInfo.Duration, edlFile, initialSkipSeconds, jobStatus, jobLog)
        {
            _remuxedVideoFileInfo = remuxedVideoFileInfo;
            _uncutVideo = uncutVideo;
            _ext = Util.FilePaths.CleanExt(_uncutVideo);
            _workingPath = workingPath;
            _duration = _remuxedVideoFileInfo.Duration;
            _jobStatus = jobStatus;
            _jobLog = jobLog;

            // Read various profile parameters
            Ini configProfileIni = new Ini(GlobalDefs.ProfileFile);
            _cutMP4Alternative = configProfileIni.ReadBoolean(profile, "CutMP4Alternate", false); // for FFMPEG and Handbrake, we have commerical cutting options, for mEncoder cutting is done during conversion using -edl option
            jobLog.WriteEntry("MP4 Alternative Cutting (CutMP4Alternate) -> " + _cutMP4Alternative.ToString(), Log.LogEntryType.Debug);
            _universalCommercialRemover = configProfileIni.ReadBoolean(profile, "UniversalCommercialRemover", false); // Forcing the use of CutFFMPEG which works on all video types
            jobLog.WriteEntry("Universal Commercial Remover (UniversalCommercialRemover) -> " + _universalCommercialRemover.ToString(), Log.LogEntryType.Debug);
            string commercialMerge = configProfileIni.ReadString(profile, "CommercialMergeTool", ""); // Force tool to merge commercial segments
            jobLog.WriteEntry("Force Commercial Segment Merging Tool (CommercialMergeTool) -> " + commercialMerge, Log.LogEntryType.Debug);

            switch (commercialMerge.ToLower())
            {
                case "avidemux":
                    _useAVIDemuxMerge = true;
                    break;

                case "ffmpeg":
                    _useFFMPEGMerge = true;
                    break;

                case "":
                    break;

                default:
                    jobLog.WriteEntry("INVALID Force Commercial Segment Merging Tool -> " + commercialMerge, Log.LogEntryType.Warning);
                    break;
            }
        }

        /// <summary>
        /// Generic cutting algorithm for cutting any file type based on concat protocol specifications using FFMPEG
        /// </summary>
        /// <param name="remuxedVideoFilterAudioLanguage">Used to filter out a specific audio language while cutting the file. Primarily for TS file when used in pre-conversion cutting</param>
        private void CutFFMPEG(bool remuxedVideoFilterAudioLanguage=false)
        {
            long totalSegmentSize = 0;
            List<KeyValuePair<float, float>> keepList = new List<KeyValuePair<float, float>>();

            // Read the EDL file and convert into cut structure
            if (!ParseEDLFile(ref keepList))
                return;

            //Do the cuts and pick them up as we go
            int cutNumber = 0;
            List<string> filesFound = new List<string>();

            foreach (KeyValuePair<float, float> keep in keepList)
            {
                _jobStatus.CurrentAction = Localise.GetPhrase("Cutting commercials from video - segment") + " " + cutNumber.ToString(CultureInfo.InvariantCulture);
                _jobLog.WriteEntry(this, Localise.GetPhrase("CutFFMPEG: Cutting commercials from video - segment ") + cutNumber.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Information);

                // Create the splits - reset success each time
                string cutFileName = CutFileName(_uncutVideo, keep.Key, keep.Value);
                string parameters = " -y -threads 0";

                // http://ffmpeg.org/trac/ffmpeg/wiki/Seeking%20with%20FFmpeg - We use only FAST seek (since accurate seek cuts on a NON KeyFrame which causes Audio Sync Issues)
                parameters += " -ss " + keep.Key.ToString(CultureInfo.InvariantCulture);

                parameters += " -i " + Util.FilePaths.FixSpaces(_uncutVideo);

                // how much to Cut
                parameters += " -t " + (keep.Value - keep.Key).ToString(CultureInfo.InvariantCulture);

                // If we are requested to filter out the selected audio language, check for it's existance and filter it
                if (remuxedVideoFilterAudioLanguage && (_uncutVideoMediaInfo.AudioTracks > 1))
                {
                    if (_remuxedVideoFileInfo.AudioStream == -1)
                    {
                        _jobLog.WriteEntry("Cannot get audio stream selection details, copying all audio streams", Log.LogEntryType.Warning);

                        // Check for video track
                        if (_uncutVideoMediaInfo.MediaInfo.VideoInfo.Stream != -1)
                            parameters += " -map 0:" + _uncutVideoMediaInfo.MediaInfo.VideoInfo.Stream.ToString() + " -vcodec copy"; // Fix for FFMPEG WTV MJPEG ticket #2227
                        else
                            parameters += " -vn";

                        // Audio tracks
                        parameters += " -map 0:a -acodec copy";
                    }
                    else
                    {
                        _jobLog.WriteEntry("Selecting Audio Language " + _remuxedVideoFileInfo.AudioLanguage + " Audio Stream " + _remuxedVideoFileInfo.AudioStream.ToString(), Log.LogEntryType.Debug);

                        // Check for video track
                        if (_uncutVideoMediaInfo.MediaInfo.VideoInfo.Stream != -1)
                            parameters += " -map 0:" + _uncutVideoMediaInfo.MediaInfo.VideoInfo.Stream.ToString() + " -vcodec copy"; // Fix for FFMPEG WTV MJPEG ticket #2227
                        else
                            parameters += " -vn";

                        // Audio track
                        parameters += " -map 0:" + _remuxedVideoFileInfo.AudioStream.ToString() + " -acodec copy"; // Select the Audiotrack we had isolated earlier
                    }
                }
                else
                {
                    // Check for video track
                    if (_uncutVideoMediaInfo.MediaInfo.VideoInfo.Stream != -1)
                        parameters += " -map 0:" + _uncutVideoMediaInfo.MediaInfo.VideoInfo.Stream.ToString() + " -vcodec copy"; // Fix for FFMPEG WTV MJPEG ticket #2227
                    else
                        parameters += " -vn";

                    // Check for Audio tracks
                    if (_uncutVideoMediaInfo.AudioTracks > 0)
                        parameters += " -map 0:a -acodec copy";
                    else
                        parameters += " -an";
                }

                // Stream copy
                parameters += " " + Util.FilePaths.FixSpaces(cutFileName);
                
                if (!FFmpeg.FFMpegExecuteAndHandleErrors(parameters, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(cutFileName), false)) // Don't check for % here since Comskip has a bug that gives EDL cut segments past the end of the file || _jobStatus.PercentageComplete < GlobObjects.ACCEPTABLE_COMPLETION) // each run resets this number or an error in the process
                {
                    filesFound.Add(cutFileName); // add the last file created (which may be partial)
                    CleanupCutFiles(filesFound); //Clean up cut files
                    _jobStatus.PercentageComplete = 0;
                    _jobStatus.ErrorMsg = "CutFFMPEG splitting video segments failed";
                    _jobLog.WriteEntry(this, Localise.GetPhrase("CutFFMPEG splitting video segments failed"), Log.LogEntryType.Error);
                    return;
                }

                filesFound.Add(cutFileName); // to be deleted later
                cutNumber++;

                totalSegmentSize += Util.FileIO.FileSize(cutFileName); // Calculate the total size of all segments for validation later
            }

            if (cutNumber < 1)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("No commercials to remove from ") + EDLFile, Log.LogEntryType.Information);
                _jobStatus.PercentageComplete = 100; //Set to success since sometime mp4box doesn't set to 100 if there no/incomplete pieces to strip
                return;
            }

            // Create a text file with the list of all the files which need to be concatenated
            // http://ffmpeg.org/trac/ffmpeg/wiki/How%20to%20concatenate%20(join,%20merge)%20media%20files
            string concatFileList = "";
            string concatFileName = Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(_uncutVideo) + "_ConcatList.txt"); // Write file list to text

            foreach (string file in filesFound)
                concatFileList += "file " + "'" + file.Replace("'", @"'\''") + "'" + "\r\n"; // File paths are enclosed in single quotes (compensate for ' in filenames with '\'')

            System.IO.File.WriteAllText(concatFileName, concatFileList); // write the text file

            cutNumber = 0;
            string mergeParams = "";

            // Merge the files - copy all video and audio streams
            string tempFile = CutFileName(_uncutVideo, 0, 0);
            mergeParams += "-y -f concat -i " + Util.FilePaths.FixSpaces(concatFileName);

            // Check for video track
            if (_uncutVideoMediaInfo.MediaInfo.VideoInfo.Stream != -1)
                mergeParams += " -map v -vcodec copy";
            else
                mergeParams += " -vn";

            // Check for Audio tracks
            if (_uncutVideoMediaInfo.AudioTracks > 0)
                mergeParams += " -map a -acodec copy";
            else
                mergeParams += " -an";

            // Avoid negative timestamp issues https://trac.ffmpeg.org/wiki/Seeking%20with%20FFmpeg
            mergeParams += " -avoid_negative_ts 1";

            mergeParams += " " + Util.FilePaths.FixSpaces(tempFile); // Output file

            _jobStatus.CurrentAction = Localise.GetPhrase("Merging commercial free segments into new video");
            _jobLog.WriteEntry(this, Localise.GetPhrase("CutFFMPEG: Merging commercial free segments into new video"), Log.LogEntryType.Information);

            if (!FFmpeg.FFMpegExecuteAndHandleErrors(mergeParams, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(tempFile)))
            {
                CleanupCutFiles(filesFound); //Clean up cut files
                Util.FileIO.TryFileDelete(concatFileName);
                _jobStatus.PercentageComplete = 0;
                _jobStatus.ErrorMsg = "CutFFMPEG merging video segments failed";
                _jobLog.WriteEntry(this, Localise.GetPhrase("CutFFMPEG merging video segments failed"), Log.LogEntryType.Error); ;
                return;
            }

            _jobLog.WriteEntry(this, Localise.GetPhrase("CutFFMPEG : Total Expected Segements size [KB] ") + (totalSegmentSize / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            // Move the files
            _jobLog.WriteEntry(this, Localise.GetPhrase("CutFFMPEG trying to replace file") + " Output : " + _uncutVideo + " Temp : " + tempFile, Log.LogEntryType.Debug);
            RenameAndMoveFile(tempFile);

            //Clean up cut files
            Util.FileIO.TryFileDelete(concatFileName);
            CleanupCutFiles(filesFound); //Clean up cut files
        }

        /// <summary>
        /// Remove commercials from TS files using FFMPEG, AVIDemux and FFMPEG as backup
        /// </summary>
        /// <param name="remuxedVideoFilterAudioLanguage">True if you want to use the user selected language to filter out while cutting</param>
        private void CutTS(bool remuxedVideoFilterAudioLanguage)
        {
            bool selectedTSAudioSuccess = false; // have we successfully filtered the audio tracks
            List<KeyValuePair<float, float>> keepList = new List<KeyValuePair<float, float>>();
            long totalSegmentSize = 0;

            // Read the EDL file and convert into cut structure
            if (!ParseEDLFile(ref keepList))
                return;

            //Do the cuts and pick them up as we go
            int cutNumber = 0;
            List<string> filesFound = new List<string>();

            foreach (KeyValuePair<float, float> keep in keepList)
            {
                _jobStatus.CurrentAction = Localise.GetPhrase("Cutting commercials from video - segment") + " " + cutNumber.ToString(CultureInfo.InvariantCulture);
                _jobLog.WriteEntry(this, Localise.GetPhrase("CutTS: Cutting commercials from video - segment ") + cutNumber.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Information);

                // Create the splits - reset success each time
                string cutFileName = CutFileName(_uncutVideo, keep.Key, keep.Value);
                string parameters = " -y -threads 0";

                // http://ffmpeg.org/trac/ffmpeg/wiki/Seeking%20with%20FFmpeg - We use only FAST seek (since accurate seek cuts on a NON KeyFrame which causes Audio Sync Issues)
                parameters += " -ss " + keep.Key.ToString(CultureInfo.InvariantCulture);

                parameters += " -i " + Util.FilePaths.FixSpaces(_uncutVideo);

                // how much to Cut
                parameters += " -t " + (keep.Value - keep.Key).ToString(CultureInfo.InvariantCulture);

                // If we are requested to filter out the selected audio language, check for it's existance and filter it
                if (remuxedVideoFilterAudioLanguage && (_remuxedVideoFileInfo.FFMPEGStreamInfo.AudioTracks > 1))
                {
                    if (_remuxedVideoFileInfo.AudioStream == -1)
                    {
                        _jobLog.WriteEntry("Cannot get audio stream selection details, copying all audio streams", Log.LogEntryType.Warning);

                        // Check for video track
                        if (_uncutVideoMediaInfo.MediaInfo.VideoInfo.Stream != -1)
                            parameters += " -map 0:v -vcodec copy";
                        else
                            parameters += " -vn";

                        // Audio tracks
                        parameters += " -map 0:a -acodec copy";
                    }
                    else
                    {
                        _jobLog.WriteEntry("Selecting Audio Language " + _remuxedVideoFileInfo.AudioLanguage + " Audio Stream " + _remuxedVideoFileInfo.AudioStream.ToString(), Log.LogEntryType.Debug);

                        // Check for video track
                        if (_uncutVideoMediaInfo.MediaInfo.VideoInfo.Stream != -1)
                            parameters += " -map 0:v -vcodec copy";
                        else
                            parameters += " -vn";

                        // Audio track
                        parameters += " -map 0:" + _remuxedVideoFileInfo.AudioStream.ToString() + " -acodec copy"; // Select the Audiotrack we had isolated earlier
                        selectedTSAudioSuccess = true;
                    }
                }
                else
                {
                    // Check for video track
                    if (_uncutVideoMediaInfo.MediaInfo.VideoInfo.Stream != -1)
                        parameters += " -map 0:v -vcodec copy";
                    else
                        parameters += " -vn";

                    // Check for Audio tracks
                    if (_uncutVideoMediaInfo.AudioTracks > 0)
                        parameters += " -map 0:a -acodec copy";
                    else
                        parameters += " -an";
                }

                // Stream copy
                parameters += " " + Util.FilePaths.FixSpaces(cutFileName);

                if (!FFmpeg.FFMpegExecuteAndHandleErrors(parameters, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(cutFileName), false)) // Don't check for % here since Comskip has a bug that gives EDL cut segments past the end of the file || _jobStatus.PercentageComplete < GlobObjects.ACCEPTABLE_COMPLETION) // each run resets this number or an error in the process
                {
                    filesFound.Add(cutFileName); // add the last file created (which may be partial)
                    CleanupCutFiles(filesFound); //Clean up cut files
                    _jobStatus.PercentageComplete = 0;
                    _jobStatus.ErrorMsg = "CutTS splitting video segments failed";
                    _jobLog.WriteEntry(this, Localise.GetPhrase("CutTS splitting video segments failed"), Log.LogEntryType.Error);
                    return;
                }

                filesFound.Add(cutFileName); // to be deleted later
                cutNumber++;

                totalSegmentSize += Util.FileIO.FileSize(cutFileName); // Calculate the total size of all segments for validation later
            }

            if (cutNumber < 1)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("No commercials to remove from ") + EDLFile, Log.LogEntryType.Information);
                _jobStatus.PercentageComplete = 100; //Set to success since sometime mp4box doesn't set to 100 if there no/incomplete pieces to strip
                return;
            }

            // Build the AVIDemux Merge Params
            cutNumber = 0;
            string aviDemuxMergeParams = "";
            foreach (string file in filesFound)
            {
                if (cutNumber++ == 0)
                    aviDemuxMergeParams += Util.FilePaths.FixSpaces(file);
                else
                    aviDemuxMergeParams += " --append " + Util.FilePaths.FixSpaces(file);
            }

            // Save the file as a TS file
            string tempFile = CutFileName(_uncutVideo, 0, 0);
            aviDemuxMergeParams += " --video-codec copy --audio-codec copy --output-format ffts --save " + Util.FilePaths.FixSpaces(tempFile);

            // Build the FFMPEG Merge Params
            // Create a text file with the list of all the files which need to be concatenated
            // http://ffmpeg.org/trac/ffmpeg/wiki/How%20to%20concatenate%20(join,%20merge)%20media%20files
            string concatFileList = "";
            string concatFileName = Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(_uncutVideo) + "_ConcatList.txt");

            foreach (string file in filesFound)
                concatFileList += "file " + "'" + file.Replace("'", @"'\''") + "'" + "\r\n"; // File paths are enclosed in single quotes, compensate for ' with '\'' in the filename

            System.IO.File.WriteAllText(concatFileName, concatFileList); // write the text file

            cutNumber = 0;

            // Merge the files using FFMPEG as a backup
            string ffmpegMergeParams = "-y -f concat -i " + Util.FilePaths.FixSpaces(concatFileName);

            // Check for video track
            if (_uncutVideoMediaInfo.MediaInfo.VideoInfo.Stream != -1)
                ffmpegMergeParams += " -map v -vcodec copy";
            else
                ffmpegMergeParams += " -vn";

            // Check for Audio tracks
            if (_uncutVideoMediaInfo.AudioTracks > 0)
                ffmpegMergeParams += " -map a -acodec copy";
            else
                ffmpegMergeParams += " -an";

            // Avoid negative timestamp issues https://trac.ffmpeg.org/wiki/Seeking%20with%20FFmpeg
            ffmpegMergeParams += " -avoid_negative_ts 1";

            ffmpegMergeParams += " " + Util.FilePaths.FixSpaces(tempFile); // Output file

            _jobStatus.CurrentAction = Localise.GetPhrase("Merging commercial free segments into new video");
            _jobLog.WriteEntry(this, "CutTS: Merging commercial free segments into new video", Log.LogEntryType.Information);

            // Check if the converted file has more than 1 audio track, AVIDemux does not support merging files with > 1 audio track (it drops them randomly)
            bool useFFMPEG = false;
            bool retVal = false;
            FFmpegMediaInfo ffmpegStreamInfo = new FFmpegMediaInfo(_uncutVideo, _jobStatus, _jobLog);
            if (!ffmpegStreamInfo.Success || ffmpegStreamInfo.ParseError)
            {
                CleanupCutFiles(filesFound); //Clean up cut files
                Util.FileIO.TryFileDelete(concatFileName);
                _jobStatus.PercentageComplete = 0;
                _jobStatus.ErrorMsg = "CutTS unable to get video info";
                _jobLog.WriteEntry(this, _jobStatus.ErrorMsg, Log.LogEntryType.Error);
                return;
            }

            AVIDemux aviDemux = new AVIDemux(aviDemuxMergeParams, _jobStatus, _jobLog);

            // If we have more than one audio track we should use FFMPEG instead of AVIDemux
            if (_useFFMPEGMerge)
            {
                _jobLog.WriteEntry(this, "Forcing FFMPEG instead of AVIDemux to merge tracks. There may be artifacts/issues with the video at the merged segments", Log.LogEntryType.Warning);
                useFFMPEG = true;
                retVal = FFmpeg.FFMpegExecuteAndHandleErrors(ffmpegMergeParams, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(tempFile));
            }
            else if (_useAVIDemuxMerge)
            {
                _jobLog.WriteEntry(this, "Forcing AVIDemux to merge tracks. Sometimes merging may hang", Log.LogEntryType.Warning);
                useFFMPEG = false;
                aviDemux.Run();
                retVal = aviDemux.Success;
            }
            /*else if (ffmpegStreamInfo.AudioTracks <= 1 || selectedTSAudioSuccess) // If the source file has 1 audio track or if we have filtered the audio track while cutting the segements
            {
                _jobLog.WriteEntry(this, "Single audio track detected in converted file, using AVIDemux to merge tracks.", Log.LogEntryType.Debug);
                useFFMPEG = false;
                aviDemux.Run(); // Run only if we have 1 or less audio tracks
                retVal = aviDemux.Success;
            }*/ // AVIDemux has issues with failing on some videos making it unreliable, default back to ffmpeg for now. TODO: Do we need to update AVIDemux or find a better fix for this?
            else
            {
                _jobLog.WriteEntry(this, "Using FFMPEG instead of AVIDemux to merge tracks. There may be artifacts/issues with the video at the merged segments, set CommercialMergeTool=avidemux in the profile if you are facing issues", Log.LogEntryType.Warning);
                useFFMPEG = true;
                retVal = FFmpeg.FFMpegExecuteAndHandleErrors(ffmpegMergeParams, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(tempFile));
            }

            _jobLog.WriteEntry(this, "CutTS : Merged Segements size [KB] " + (Util.FileIO.FileSize(tempFile) / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "CutTS : Total Expected Segements size [KB] " + (totalSegmentSize / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            // If we didn't succeed or filesize wasn't as expected then try backup merging
            if (!retVal || (Util.FileIO.FileSize(tempFile) < (GlobalDefs.MINIMUM_MERGED_FILE_THRESHOLD * totalSegmentSize)) || (totalSegmentSize <= 0)) // Check final merged file size to double verify, sometime AVIDemux skips merging segments if there is an error (like video width for segment is different due to commercials)
            {
                _jobLog.WriteEntry(this, "CutTS: Merging video segments failed, retrying with backup", Log.LogEntryType.Warning);

                _jobStatus.CurrentAction = Localise.GetPhrase("Merging commercial free segments into new video");
                _jobLog.WriteEntry(this, "CutTS: Merging commercial free segments into new video", Log.LogEntryType.Information);

                // If we used ffmpeg earlier, then use AVIDemux else use ffmpeg for the backup
                if (useFFMPEG)
                {
                    _jobLog.WriteEntry(this, "Backup merging, forcing AVIDemux to merge tracks. Sometimes merging may hang", Log.LogEntryType.Warning);
                    aviDemux.Run();
                    retVal = aviDemux.Success;
                }
                else // use ffmpeg since we used avidemux earlier
                {
                    _jobLog.WriteEntry(this, "Backup merging, forcing FFMPEG instead of AVIDemux to merge tracks. There may be issues with the video at the merged segments", Log.LogEntryType.Warning);
                    retVal = FFmpeg.FFMpegExecuteAndHandleErrors(ffmpegMergeParams, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(tempFile));
                }

                _jobLog.WriteEntry(this, "CutTS : Backup merged Segements size [KB] " + (Util.FileIO.FileSize(tempFile) / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                _jobLog.WriteEntry(this, "CutTS : Total Expected Segements size [KB] " + (totalSegmentSize / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

                if (!retVal || (Util.FileIO.FileSize(tempFile) < (GlobalDefs.MINIMUM_MERGED_FILE_THRESHOLD * totalSegmentSize)) || (totalSegmentSize <= 0)) // Check final merged file size to double verify, sometime AVIDemux skips merging segments if there is an error (like video width for segment is different due to commercials)
                {
                    CleanupCutFiles(filesFound); //Clean up cut files
                    Util.FileIO.TryFileDelete(concatFileName);
                    _jobStatus.PercentageComplete = 0;
                    _jobStatus.ErrorMsg = "CutTS merging video segments failed";
                    _jobLog.WriteEntry(this, "CutTS merging video segments failed", Log.LogEntryType.Error);
                    return;
                }
            }

            // Clean up
            Util.FileIO.TryFileDelete(concatFileName);

            // Move the files
            _jobLog.WriteEntry(this, Localise.GetPhrase("CutTS trying to replace file") + " Output : " + _uncutVideo + " Temp : " + tempFile, Log.LogEntryType.Debug);
            RenameAndMoveFile(tempFile);

            //Clean up cut files
            CleanupCutFiles(filesFound);
        }

        /// <summary>
        /// Remove commercials from WMV files using ASFBin
        /// </summary>
        private void CutWMV()
        {
            List<KeyValuePair<float, float>> keepList = new List<KeyValuePair<float, float>>();

            // Read the EDL file and convert into MP4Box cut structure
            if (!ParseEDLFile(ref keepList))
                return;

            // Create params to cut the file
            string tempFile = CutFileName(_uncutVideo, 0, 0); // temp cut file
            int cutNumber = 0;
            string cutParams = " -i " + Util.FilePaths.FixSpaces(_uncutVideo);

            foreach (KeyValuePair<float, float> keep in keepList)
            {
                cutParams += " -start " + keep.Key.ToString(CultureInfo.InvariantCulture) + " -dur " + (keep.Value - keep.Key).ToString(CultureInfo.InvariantCulture);
                cutNumber++;
            }

            cutParams += " -o " + Util.FilePaths.FixSpaces(tempFile) + " -y";

            if (cutNumber < 1)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("No commercials to remove from ") + EDLFile, Log.LogEntryType.Information);
                _jobStatus.PercentageComplete = 100; //Set to success since sometime mp4box doesn't set to 100 if there no/incomplete pieces to strip
                return;
            }

            // Cut the file
            _jobStatus.CurrentAction = Localise.GetPhrase("Merging commercial free segments into new video");
            _jobLog.WriteEntry(this, Localise.GetPhrase("CutWMV: Merging commercial free segments into new video"), Log.LogEntryType.Information);
            
            ASFBin asfBin= new ASFBin(cutParams, _jobStatus, _jobLog);
            asfBin.Run();
            if (!asfBin.Success || (Util.FileIO.FileSize(tempFile) <= 0))
            {
                _jobStatus.ErrorMsg = "CutWMV Commercial cutting failed";
                _jobLog.WriteEntry(this, Localise.GetPhrase("CutWMV Commercial cutting failed"), Log.LogEntryType.Error);
                _jobStatus.PercentageComplete = 0;
                return;
            }

            _jobLog.WriteEntry(this, Localise.GetPhrase("CutWMV trying to replace file") + " Output : " + _uncutVideo + " Temp : " + tempFile, Log.LogEntryType.Debug);
            RenameAndMoveFile(tempFile);
        }

        /// <summary>
        /// Remove commercials using Mencoder for AVI, MPG and TS (not good for TS, last backup)
        /// </summary>
        /// <param name="preConversionFilterTSAudioLanguage">True is want to filter out the audio language specified by the user while removing commercials from TS files only</param>
        private void CutMencoder(bool remuxedVideoFilterAudioLanguage = false)
        {
            string tempFile = CutFileName(_uncutVideo, 0, 0);
            string mencoderParams;
            bool oldVersion = false; // Some cases we want to use old version of Mencoder

            switch (_ext)
            {
                case ".mpg":
                    oldVersion = true; // For MPG and TS we use old version of Mencoder

                    // TODO: Need to figure out how to fix the audio video sync, temp fix is to use a special version of mEncoder build here
                    // Do not use -hr-edl-seek as it doesn't work well with -ovc copy
                    // lavdopts supports a max of 8 threads
                    // avoid using threads, as it may cause a problem - anyways we are only copying here
                    mencoderParams = Util.FilePaths.FixSpaces(_uncutVideo) + " -of mpeg -ovc copy -oac copy";

                    mencoderParams += " -edl " + Util.FilePaths.FixSpaces(EDLFile) + " -o " + Util.FilePaths.FixSpaces(tempFile);

                    break;
                
                case ".ts":
                    oldVersion = true; // For MPG and TS we use old version of Mencoder

                    // TODO: Need to figure out how to fix the audio video sync, temp fix is to use a special version of mEncoder build here
                    // Do not use -hr-edl-seek as it doesn't work well with -ovc copy
                    // lavdopts supports a max of 8 threads
                    // avoid using threads, as it may cause a problem - anyways we are only copying here
                    mencoderParams = Util.FilePaths.FixSpaces(_uncutVideo) + " -of lavf -lavfopts format=mpegts -ovc copy -oac copy";

                    // If we are requested to filter out the selected audio language, check for it's existance and filter it
                    if (remuxedVideoFilterAudioLanguage)
                    {
                        if (_remuxedVideoFileInfo.AudioPID == -1)
                            _jobLog.WriteEntry("Cannot get audio stream selection details, copying all audio streams", Log.LogEntryType.Warning);
                        else
                        {
                            _jobLog.WriteEntry("Selecting Audio Language " + _remuxedVideoFileInfo.AudioLanguage + " Audio PID " + _remuxedVideoFileInfo.AudioPID.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                            mencoderParams += " -aid " + (_remuxedVideoFileInfo.AudioPID).ToString(System.Globalization.CultureInfo.InvariantCulture); // Select the Audio track PID we had isolated earlier
                        }
                    }

                    mencoderParams += " -edl " + Util.FilePaths.FixSpaces(EDLFile) + " -o " + Util.FilePaths.FixSpaces(tempFile);

                    break;

                case ".avi":
                    // TODO: bug here for AVI files, using "-mc0 -noskip" helps initially but audio out of sync after cuts - need to figure out. Try the -ni (non interleaved) option for avi files, try -idx to rebuild index for audio video
                    // Do not use -hr-edl-seek as it doesn't work well with -ovc copy
                    // lavdopts supports a max of 8 threads
                    // avoid using thread to increase stability since we are only copying here
                    mencoderParams = Util.FilePaths.FixSpaces(_uncutVideo) + " -oac copy -ovc copy -ni -edl " + Util.FilePaths.FixSpaces(EDLFile) + " -o " + Util.FilePaths.FixSpaces(tempFile);
                    break;

                default:
                    _jobLog.WriteEntry(this, Localise.GetPhrase("CutMencoder unsupported file extension") + " " + _ext, Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "CutMencoder unsupported file extension";
                    _jobStatus.PercentageComplete = 0;
                    return;
            }

            _jobStatus.CurrentAction = Localise.GetPhrase("Merging commercial free segments into new video");
            _jobLog.WriteEntry(this, Localise.GetPhrase("CutMencoder: Merging commercial free segments into new video"), Log.LogEntryType.Information);

            Mencoder mencoder = new Mencoder(mencoderParams, _jobStatus, _jobLog, oldVersion);
            mencoder.Run();
            if (!mencoder.Success || (Util.FileIO.FileSize(tempFile) <= 0)) // do not check for % success here since sometimes it does not show complete number
            {
                _jobStatus.ErrorMsg = "CutMencoder Commercial cutting failed";
                _jobLog.WriteEntry(this, Localise.GetPhrase("CutMencoder Commercial cutting failed"), Log.LogEntryType.Error);
                _jobStatus.PercentageComplete = 0;
                return;
            }

            _jobLog.WriteEntry(this, Localise.GetPhrase("CutMencoder trying to replace file") + " Output : " + _uncutVideo + " Temp : " + tempFile, Log.LogEntryType.Debug);
            RenameAndMoveFile(tempFile);
        }

        /// <summary>
        /// Backup remove commercials from MP4 using MEncoder, not the best, limited in many ways
        /// </summary>
        private void CutMP4Alternate()
        {
            string tempFile = CutFileName(_uncutVideo, 0, 0);

            // TODO: Need to add support for non AAC Audio Codecs
            // Refer to: http://forum.videohelp.com/threads/337215-Mencoder-not-copying-aac-to-mp4?p=2140000
            // and http://www.mplayerhq.hu/DOCS/codecs-status.html#ac
            // Do not use -hr-edl-seek as it doesn't work well with -ovc copy
            // lavdopts supports a max of 8 threads
            // avoid using threads, stablity - only copying here
            string mencoderParams = Util.FilePaths.FixSpaces(_uncutVideo) + " -of lavf -ovc copy -oac copy";

            //Set fafmttag based on the type of audio codec of converted file (currently supported aac, ac3, eac3)
            _jobLog.WriteEntry(this, Localise.GetPhrase("Trying to reading converted file Audio information"), Log.LogEntryType.Information);
            string audioCodec = "";
            FFmpegMediaInfo ffmpegStreamInfo = new FFmpegMediaInfo(_uncutVideo, _jobStatus, _jobLog);
            if (ffmpegStreamInfo.Success && !ffmpegStreamInfo.ParseError)
            {
                // Converted file should contain only 1 audio stream
                audioCodec = ffmpegStreamInfo.MediaInfo.AudioInfo[0].AudioCodec;
                _jobLog.WriteEntry(this, Localise.GetPhrase("Found AudioCodec") + " " + audioCodec, Log.LogEntryType.Information);

                if (String.IsNullOrEmpty(audioCodec))
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Audio codec information is blank, will try to continue without it"), Log.LogEntryType.Warning);
            }
            else
                _jobLog.WriteEntry(this, Localise.GetPhrase("Cannot read Audio codec information, will try to continue without it"), Log.LogEntryType.Warning);

            switch (audioCodec)
            {
                case "aac":
                    _jobLog.WriteEntry(this, Localise.GetPhrase("CutMP4Alternate found AAC Audio Codec"), Log.LogEntryType.Information);
                    mencoderParams += @" -fafmttag 0x706D";
                    break;

                case "ac3":
                case "ac-3":
                    _jobLog.WriteEntry(this, Localise.GetPhrase("CutMP4Alternate found AC3 Audio Codec"), Log.LogEntryType.Information);
                    mencoderParams += @" -fafmttag 0x2000";
                    break;

                case "e-ac-3":
                    _jobLog.WriteEntry(this, Localise.GetPhrase("CutMP4Alternate found E-AC3 Audio Codec"), Log.LogEntryType.Information);
                    mencoderParams += @" -fafmttag 0x33434145";
                    break;

                case "mp3":
                    _jobLog.WriteEntry(this, Localise.GetPhrase("CutMP4Alternate found mp3 Audio Codec"), Log.LogEntryType.Information);
                    mencoderParams += @" -fafmttag 0x55";
                    break;

                case "flac":
                    _jobLog.WriteEntry(this, Localise.GetPhrase("CutMP4Alternate found flac Audio Codec"), Log.LogEntryType.Information);
                    mencoderParams += @" -fafmttag 0xF1AC";
                    break;

                case "mp1":
                case "mp2":
                    _jobLog.WriteEntry(this, Localise.GetPhrase("CutMP4Alternate found mp2 Audio Codec"), Log.LogEntryType.Information);
                    mencoderParams += @" -fafmttag 0x50";
                    break;

                default:
                    _jobLog.WriteEntry(this, Localise.GetPhrase("CutMP4Alternate could not identify Audio Codec, DEFAULTING TO AAC - please check video file and audio encoder type"), Log.LogEntryType.Warning);
                    mencoderParams += @" -fafmttag 0x706D";
                    break;
            }

            mencoderParams += " -edl " + Util.FilePaths.FixSpaces(EDLFile) + " -o " + Util.FilePaths.FixSpaces(tempFile);

            _jobStatus.CurrentAction = Localise.GetPhrase("Merging commercial free segments into new video");
            _jobLog.WriteEntry(this, Localise.GetPhrase("CutMP4Alternate: Merging commercial free segments into new video"), Log.LogEntryType.Information);

            Mencoder mencoder = new Mencoder(mencoderParams, _jobStatus, _jobLog, false);
            mencoder.Run();
            if (!mencoder.Success || (Util.FileIO.FileSize(tempFile) <= 0)) // do not check for % success here since sometimes it does not show complete number
            {
                _jobStatus.ErrorMsg = "CutMP4Alternate Commercial cutting failed";
                _jobLog.WriteEntry(this, Localise.GetPhrase("CutMP4Alternate Commercial cutting failed"), Log.LogEntryType.Error);
                _jobStatus.PercentageComplete = 0;
                return;
            }

            _jobLog.WriteEntry(this, Localise.GetPhrase("CutMP4Alternate trying to replace file") + " Output : " + _uncutVideo + " Temp : " + tempFile, Log.LogEntryType.Debug);
            RenameAndMoveFile(tempFile);
        }

        /// <summary>
        /// Remove commercials from MP4 files using MP4Box
        /// </summary>
        private void CutMP4()
        {
            List<KeyValuePair<float, float>> keepList = new List<KeyValuePair<float, float>>();

            // Read the EDL file and convert into MP4Box cut structure
            if (!ParseEDLFile(ref keepList))
                return;

            // Create the MP4 splits
            //Setup the 'look for splits' variables
            string searchPattern = Path.GetFileNameWithoutExtension(_uncutVideo) + "*" + _ext;
            string searchPath = _workingPath;
            List<string> filesFound = new List<string>();
            filesFound.Add(_uncutVideo);

            //Do the cuts and pick them up as we go
            int cutNumber = 0;
            string mergeParams = "";
            foreach (KeyValuePair<float, float> keep in keepList)
            {
                _jobStatus.CurrentAction = Localise.GetPhrase("Cutting commercials from video - segment") + " " + cutNumber.ToString(CultureInfo.InvariantCulture);
                _jobLog.WriteEntry(this, Localise.GetPhrase("MP4Box: Cutting commercials from video - segment ") + cutNumber.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Information);
                string cutFileName = CutFileName(_uncutVideo, keep.Key, keep.Value);
                string Parameters = " -keep-all -keep-sys -splitx " + keep.Key.ToString(CultureInfo.InvariantCulture) + ":" + keep.Value.ToString(CultureInfo.InvariantCulture) + " -out " + Util.FilePaths.FixSpaces(cutFileName) + " " + Util.FilePaths.FixSpaces(_uncutVideo);
                MP4Box mp4Box = new MP4Box(Parameters, _jobStatus, _jobLog);
                mp4Box.Run();
                if (!mp4Box.Success) // Don't check for % here since Comskip has a bug that gives EDL cut segments past the end of the file || _jobStatus.PercentageComplete < GlobObjects.ACCEPTABLE_COMPLETION) // each run resets this number or an error in the process
                {
                    _jobStatus.PercentageComplete = 0;
                    _jobStatus.ErrorMsg = "Mp4Box splitting video segments failed";
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Mp4Box splitting video segments failed"), Log.LogEntryType.Error); ;
                    return;
                }


                //Setup the directory search - do not use Enumerate files since it's lazy in updating and skips files
                IEnumerable<string> newFilesFound = new List<string>();
                try
                {
                    newFilesFound = Directory.GetFiles(searchPath, searchPattern).OrderBy(File.GetCreationTime); //put them back in the order they were split
                }
                catch (Exception e)
                {
                    _jobStatus.ErrorMsg = "MP4 Commercial cut failed trying to get list of file segments";
                    _jobLog.WriteEntry(this, Localise.GetPhrase("MP4 Commercial cut failed trying to get list of file segments") + "\r\nError : " + e.ToString(), Log.LogEntryType.Error);
                    _jobStatus.PercentageComplete = 0;
                    return;
                }

                //Look for the split name as it cannot be specified before mp4box execution
                foreach (string f in newFilesFound)
                {
                    if (!filesFound.Contains(f))
                    {
                        // Build the merge cmd
                        if (cutNumber == 0)
                            mergeParams += " -keep-all -keep-sys";

                        mergeParams += " -cat " + Util.FilePaths.FixSpaces(f);
                        cutNumber++;
                        filesFound.Add(f);
                        _jobLog.WriteEntry(this, Localise.GetPhrase("MP4Box: Found a new segment to merge"), Log.LogEntryType.Information);
                        _jobLog.WriteEntry(this, "No : " + cutNumber.ToString(CultureInfo.InvariantCulture) + " Filename : " + f, Log.LogEntryType.Debug);
                    }
                }
            }

            if (cutNumber < 1)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("No commercials to remove from ") + EDLFile, Log.LogEntryType.Information);
                _jobStatus.PercentageComplete = 100; //Set to success since sometime mp4box doesn't set to 100 if there no/incomplete pieces to strip
                return;
            }

            // Merge the splits
            string tempFile = CutFileName(_uncutVideo, 0, 0);

            mergeParams += " -new " + Util.FilePaths.FixSpaces(tempFile);
            
            _jobStatus.CurrentAction = Localise.GetPhrase("Merging commercial free segments into new video");
            _jobLog.WriteEntry(this, Localise.GetPhrase("MP4Box: Merging commercial free segments into new video"), Log.LogEntryType.Information);

            MP4Box mp4BoxMerge = new MP4Box(mergeParams, _jobStatus, _jobLog);
            mp4BoxMerge.Run();
            if (!mp4BoxMerge.Success || _jobStatus.PercentageComplete < GlobalDefs.ACCEPTABLE_COMPLETION || (Util.FileIO.FileSize(tempFile) <= 0)) // each run resets this number or an error in the procecss
            {
                CleanupCutFiles(filesFound); //Clean up cut files
                _jobStatus.PercentageComplete = 0;
                _jobStatus.ErrorMsg = "MP4Box merging video segments failed";
                _jobLog.WriteEntry(this, Localise.GetPhrase("MP4Box merging video segments failed"), Log.LogEntryType.Error); ;
                return;
            }


            // Move the files
            _jobLog.WriteEntry(this, Localise.GetPhrase("MP4Box trying to replace file") + " Output : " + _uncutVideo + " Temp : " + tempFile, Log.LogEntryType.Debug);
            RenameAndMoveFile(tempFile);

            //Clean up cut files
            CleanupCutFiles(filesFound);
        }

        /// <summary>
        /// Remove commercials from MKV files useing MKVMerge
        /// </summary>
        private void CutMKV()
        {
            List<KeyValuePair<float, float>> keepList = new List<KeyValuePair<float, float>>();
            string tempFile = CutFileName(_uncutVideo, 0, 0);

            // Read the EDL file and convert into MKVMerge cut structure
            if (!ParseEDLFile(ref keepList))
                return;

            // Create the MKV splits
            //Do the cuts and pick them up as we go
            int cutNumber = 0;
            IEnumerable<string> newFilesFound = new List<string>();

            // Build the list of timecodes to split into invidiual files
            string Parameters = "--clusters-in-meta-seek --split parts:";
            foreach (KeyValuePair<float, float> keep in keepList)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("CutMKV: Found commercials from video - segment") + " " + cutNumber.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

                TimeSpan startCut = TimeSpan.FromSeconds(Convert.ToDouble(keep.Key.ToString(CultureInfo.InvariantCulture)));
                TimeSpan endCut = TimeSpan.FromSeconds(Convert.ToDouble(keep.Value.ToString(CultureInfo.InvariantCulture)));

                if (cutNumber == 0)
                    Parameters += startCut.ToString() + "-" + endCut.ToString();
                else
                    Parameters += ",+" + startCut.ToString() + "-" + endCut.ToString();

                cutNumber++;
            }

            Parameters += " --compression -1:none " + Util.FilePaths.FixSpaces(_uncutVideo) + " -o " + Util.FilePaths.FixSpaces(tempFile);

            if (cutNumber < 1)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("No commercials to remove from ") + EDLFile, Log.LogEntryType.Information);
                _jobStatus.PercentageComplete = 100; //Set to success since sometime mp4box doesn't set to 100 if there no/incomplete pieces to strip
                return;
            }

            _jobStatus.CurrentAction = Localise.GetPhrase("Cutting commercials from video");

            MKVMerge mkvMerge = new MKVMerge(Parameters, _jobStatus, _jobLog);
            mkvMerge.Run();
            if (!mkvMerge.Success || (Util.FileIO.FileSize(tempFile) <= 0)) // check for +ve success
            {
                _jobStatus.PercentageComplete = 0;
                _jobStatus.ErrorMsg = "CutMKV merging video segments failed";
                _jobLog.WriteEntry(this, Localise.GetPhrase("CutMKV merging video segments failed"), Log.LogEntryType.Error); ;
                return;
            }

            // Move the files
            _jobLog.WriteEntry(this, Localise.GetPhrase("CutMKV trying to replace file") + " Output : " + _uncutVideo + " Temp : " + tempFile, Log.LogEntryType.Debug);
            RenameAndMoveFile(tempFile);
        }

        /// <summary>
        /// Renames and move the final cut video to the temp working directory with the name of the original uncut video
        /// </summary>
        /// <param name="tempFile">Path to final cut video</param>
        private void RenameAndMoveFile(string tempFile)
        {
            // If the original uncut file is in the working temp directory, then just replace it
            if (Path.GetDirectoryName(_uncutVideo).ToLower() == _workingPath.ToLower())
                Util.FileIO.TryFileReplace(_uncutVideo, tempFile);
            else // If the original uncut video is not in the working temp directory, then just rename the tempFile with the original name and keep in the temp working directory (don't mangle original video file)
            {
                FileIO.TryFileDelete(Path.Combine(_workingPath, Path.GetFileName(_uncutVideo))); // just in case it exists
                FileIO.MoveAndInheritPermissions(tempFile, Path.Combine(_workingPath, Path.GetFileName(_uncutVideo)));
            }

            _cutVideo = Path.Combine(_workingPath, Path.GetFileName(_uncutVideo)); // The final cut video always lies in the working directory
        }

        /// <summary>
        /// Clean up temporary file cut during the commercial removal process
        /// </summary>
        /// <param name="filesFound">List of files to delete</param>
        private void CleanupCutFiles(List<string> filesFound)
        {
                        //Clean up cut files
            foreach (string f in filesFound)
            {
                if (String.Compare(f, _uncutVideo, true) != 0)
                {
                    Util.FileIO.TryFileDelete(f);
                    Util.FileIO.TryFileDelete(f + ".idx2"); // Delete the IDX2 files also created by AVIDemux
                }
            }
        }

        /// <summary>
        /// Returns the Filename to be used for extracting a cut
        /// </summary>
        /// <param name="fileName">Base Filename</param>
        /// <param name="startSec">Starting second for cut</param>
        /// <param name="endSec">Ending second for cut</param>
        /// <returns>Temp Cut Filename</returns>
        private string CutFileName(string fileName, float startSec, float endSec)
        {
            return Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(_uncutVideo) + "_" + ((int)startSec).ToString(CultureInfo.InvariantCulture) + "_" + ((int)endSec).ToString(CultureInfo.InvariantCulture) + _ext); // DO NOT CHANGE FORMAT AS THIS IS THE FORMAT USED BY MP4Box
        }

        /// <summary>
        /// Check if removing commercials for the specified extension is supported
        /// </summary>
        /// <param name="extension">Extension to check</param>
        /// <param name="profile">Profile for which supported extensions for commercial removal is to be checked. If this is blank or null, then only natively supported extensions are checked and not those covered by generic commercial removal functions.</param>
        /// <returns>True if commercial stripping is support for the extension</returns>
        public static bool IsSupportedExtension(string extension, string profile="")
        {
            switch (extension)
            {
                case ".ts":
                case ".wmv":
                case ".mpg":
                case ".avi":
                case ".m4v":
                case ".mp4":
                case ".mkv":
                    return true;

                default:
                    if (string.IsNullOrWhiteSpace(profile)) // unknown profile
                        return false; // Just the native supported extensions

                    // If we are using the universal commercial remover, than yes ALL extensions are supported else, we use only native extensions
                    Ini configProfileIni = new Ini(GlobalDefs.ProfileFile);
                    bool universalCommercialRemover = configProfileIni.ReadBoolean(profile, "UniversalCommercialRemover", false); // Forcing the use of CutFFMPEG which works on all video types
                    if (universalCommercialRemover)
                        return true;
                    else
                        return false;
            }
        }

        /// <summary>
        /// Removes commercials from the converted file using the EDL file and updates the RemoveAds flag if successful
        /// </summary>
        /// <param name="filterAudioLanguage">True if you want to keep only the identified audio language while cutting TS files (before conversion since VideoFile will be used to determine audio and video track properties). This is useful when commercial remove is done before video conversion since audio language information is lost.</param>
        public void StripCommercials(bool remuxedVideoFilterAudioLanguage = false)
        {
            _jobStatus.PercentageComplete = 100; //all good to start with 
            _jobStatus.ETA = "";

            if (!File.Exists(_uncutVideo))
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("File not found") + " " + _uncutVideo, Log.LogEntryType.Error);
                _jobStatus.PercentageComplete = 0;
                _jobStatus.ErrorMsg = "Strip Commercials file not found";
                return;
            }
            if (!File.Exists(EDLFile))
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("EDL file does not exist - no commercials found") + " " + EDLFile, Log.LogEntryType.Information);
                _jobStatus.ErrorMsg = "Strip commercial EDL file not found";
                _jobStatus.PercentageComplete = 0;
                return;
            }

            if (remuxedVideoFilterAudioLanguage) // If we are being called preconversion then the media info is the same as the remuxed file
                _uncutVideoMediaInfo = _remuxedVideoFileInfo.FFMPEGStreamInfo;
            else // otherwise lets get the media info for the conversted file
            {
                _uncutVideoMediaInfo = new FFmpegMediaInfo(_uncutVideo, _jobStatus, _jobLog);
                if (!_uncutVideoMediaInfo.Success || _uncutVideoMediaInfo.ParseError)
                {
                    _jobStatus.PercentageComplete = 0; // if the file wasn't completely converted the percentage will be low so no worries
                    _jobStatus.ErrorMsg = "Commerical remover, getting video mediainfo failed for " + _uncutVideo;
                    _jobLog.WriteEntry(this, (_jobStatus.ErrorMsg), Log.LogEntryType.Error);
                    return;
                }
            }

            _jobLog.WriteEntry(this, Localise.GetPhrase("Commercial Remove: Before removal file size [KB] ") + (Util.FileIO.FileSize(_uncutVideo)/1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug); ;

            if (_universalCommercialRemover)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Forcing use of Universal Commercial Remover for") + " " + _ext, Log.LogEntryType.Warning);
                CutFFMPEG(remuxedVideoFilterAudioLanguage);
            }
            else
            {
                switch (_ext)
                {
                    case ".ts":
                        _jobLog.WriteEntry(this, Localise.GetPhrase("Removing commercials using CutTS") + " ext -> " + _ext, Log.LogEntryType.Information);
                        CutTS(remuxedVideoFilterAudioLanguage);
                        // TODO: Mencoder based TS cutting is broken, generated unusable files and also EDL cutting does not work
                        /*if (0 == _jobStatus.PercentageComplete) // Try backup
                        {
                            _jobLog.WriteEntry(this, Localise.GetPhrase("CutTS failed, trying to remove commercials using CutMencoder") + " ext -> " + _ext, Log.LogEntryType.Information);
                            _jobStatus.PercentageComplete = 100; //Reset
                            _jobStatus.ErrorMsg = ""; //reset error message since we are trying again
                            CutMencoder(filterTSAudioLanguage);
                        }*/
                        break;

                    case ".wmv":
                        _jobLog.WriteEntry(this, Localise.GetPhrase("Removing commercials using CutWMV") + " ext -> " + _ext, Log.LogEntryType.Information);
                        CutWMV();
                        break;

                    case ".mpg":
                    case ".avi":
                        _jobLog.WriteEntry(this, Localise.GetPhrase("Removing commercials using CutMencoder") + " ext -> " + _ext, Log.LogEntryType.Information);
                        CutMencoder();
                        break;

                    case ".m4v":
                    case ".mp4":
                        if (_cutMP4Alternative)
                        {
                            _jobLog.WriteEntry(this, Localise.GetPhrase("Removing commercials using CutMP4Alternate") + " ext -> " + _ext, Log.LogEntryType.Information);
                            CutMP4Alternate();
                        }
                        else
                        {
                            _jobLog.WriteEntry(this, Localise.GetPhrase("Removing commercials using CutMP4") + " ext -> " + _ext, Log.LogEntryType.Information);
                            CutMP4();
                            if (0 == _jobStatus.PercentageComplete) // CutMP4 can fail for some .TS files that can't be read by MediaInfo, do a backup try
                            {
                                _jobStatus.ErrorMsg = ""; //reset error message since we are trying again
                                _jobLog.WriteEntry(this, Localise.GetPhrase("CutMP4 failed, trying backup CutMP4Alternate to remove commercials"), Log.LogEntryType.Warning);
                                _jobStatus.PercentageComplete = 100; //Reset
                                CutMP4Alternate();
                            }
                        }
                        break;

                    case ".mkv":
                        {
                            _jobLog.WriteEntry(this, Localise.GetPhrase("Removing commercials using CutMKV"), Log.LogEntryType.Information);
                            CutMKV();
                            break;
                        }

                    default:
                        {
                            _jobLog.WriteEntry(this, Localise.GetPhrase("Unsupported extension for removing commercials ") + " " + _ext + ", trying default FFMPEG cutter", Log.LogEntryType.Warning);
                            CutFFMPEG(remuxedVideoFilterAudioLanguage);
                            break;
                        }
                }

                // If all fails, try one last chance with CutFFMPEG
                if (0 == _jobStatus.PercentageComplete) // Try backup
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Cutting failed, trying to remove commercials using CutFFMPEG") + " ext -> " + _ext, Log.LogEntryType.Information);
                    _jobStatus.PercentageComplete = 100; //Reset
                    _jobStatus.ErrorMsg = ""; //reset error message since we are trying again
                    CutFFMPEG(remuxedVideoFilterAudioLanguage);
                }
            }

            if ((_jobStatus.PercentageComplete != 0) && !String.IsNullOrWhiteSpace(_cutVideo))
                _remuxedVideoFileInfo.AdsRemoved = true; // We have removed the Ad's
            else
                _remuxedVideoFileInfo.AdsRemoved = false;

            _jobLog.WriteEntry(this, Localise.GetPhrase("Commercial Remove: After removal  file size [KB] ") + (Util.FileIO.FileSize(_cutVideo) / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug); ;
            _jobLog.WriteEntry(this, Localise.GetPhrase("Commercial Remove: Percentage Complete") + " " + _jobStatus.PercentageComplete.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
        }

        /// <summary>
        /// Path to the video with the commercials removed
        /// </summary>
        public string CommercialFreeVideo
        { get { return _cutVideo; } }
    }
}
