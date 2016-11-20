using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.AppWrapper;
using MCEBuddy.VideoProperties;

namespace MCEBuddy.Transcode
{
    public class RemuxExt
    {
        private JobStatus _jobStatus;
        private Log _jobLog;
        private string _remuxTo = "";
        private string _extension = "";
        private string _originalFile = "";
        private string _remuxedFile = "";
        private string _workingPath = "";
        private double _fps;

        public RemuxExt(string originalFile, string workingPath, double fps, JobStatus jobStatus, Log jobLog, string remuxTo)
        {
            _jobLog = jobLog;
            _jobStatus = jobStatus;
            _originalFile = originalFile; // By default if we aren't remuxing then the remuxed file is the original file
            _fps = fps;
            _remuxTo = remuxTo.ToLower();
            _extension = FilePaths.CleanExt(originalFile);
            _workingPath = workingPath;

            _jobLog.WriteEntry(this, "Remux To : " + _remuxTo, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Extension : " + _extension, Log.LogEntryType.Debug);
        }

        public bool RemuxFile()
        {
            _jobStatus.ErrorMsg = "";
            _jobStatus.PercentageComplete = 100; //all good to start with
            _jobStatus.ETA = "";

            bool ret;

            if (String.IsNullOrWhiteSpace(_remuxTo))
            {
                _remuxedFile = _originalFile; // Same file, nothing processed
                return true;
            }

            if (_remuxTo[0] != '.') _remuxTo = "." + _remuxTo;  // Just in case someone does something dumb like forget the leading "."

            // NOTE: When using Copy converter and Skip Remuxing the extension and remuxto is sometimes the same
            // TODO: Do we need to force remuxing even if the extensions are the same?
            if (_remuxTo == _extension)
            {
                _jobLog.WriteEntry(this, "Skipping, remuxing to same extension", Log.LogEntryType.Warning);
                _remuxedFile = _originalFile; // Same file, nothing processed
                return true;
            }

            _jobStatus.CurrentAction = Localise.GetPhrase("Remuxing to") + " " + _remuxTo;

            if (MP4File(_remuxTo)) // to MPEG4 formats
            {
                if (_extension == ".avi") // AVI to MPEG4
                {
                    _jobLog.WriteEntry(this, ("Remuxing video using MP4BoxRemuxAvito") + " " + _remuxTo, Log.LogEntryType.Debug);
                    ret = MP4BoxRemuxAvi();
                }
                else
                {
                    _jobLog.WriteEntry(this, ("Remuxing video using FFMpeg to") + " " + _remuxTo, Log.LogEntryType.Debug);
                    ret = FfmpegRemux(); // First try ffmpeg, mp4box doesn't always work on all ts.h264 to mp4
                    if (!ret)
                    {
                        _jobLog.WriteEntry(this, ("FFMPEG Remuxing failed, Remuxing video using MP4BoxRemux to") + " " + _remuxTo, Log.LogEntryType.Debug);
                        ret = MP4BoxRemux();
                    }
                }
            }
            else if (_remuxTo == ".mkv")
            {
                _jobLog.WriteEntry(this, ("Remuxing video using MKVMerge to") + " " + _remuxTo, Log.LogEntryType.Debug);
                ret = MKVRemux();
            }
            else
                ret = false;

            if (!ret)// default and on failure try FFMPEG
            {
                _jobLog.WriteEntry(this, ("Remuxing video using default FFMpeg to") + " " + _remuxTo, Log.LogEntryType.Debug);
                ret = FfmpegRemux();
            }

            _jobLog.WriteEntry(this, ("Finished video file remuxing, file size [KB]") + " " + (Util.FileIO.FileSize(_remuxedFile) / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            return ret;
        }

        /// <summary>
        /// Remuxes the converted file to the specified extension/format using FFMPEG.
        /// (Optionally) If a new Audio Stream file is specified, the audio from the new file is taken and video from the original file
        /// </summary>
        /// <param name="newAudioStream">(Optional) New Audio stream to use</param>
        /// <returns>True is successful</returns>
        public bool FfmpegRemux(string newAudioStream = "")
        {
            _jobStatus.ErrorMsg = "";
            _jobStatus.PercentageComplete = 100; //all good to start with
            _jobStatus.ETA = "";

            Util.FileIO.TryFileDelete(RemuxedTempFile);

            FFmpegMediaInfo ffmpegStreamInfo = new FFmpegMediaInfo(_originalFile, _jobStatus, _jobLog);
            if (!ffmpegStreamInfo.Success || ffmpegStreamInfo.ParseError)
            {
                _jobStatus.ErrorMsg = "Unable to read video information";
                _jobLog.WriteEntry(this, (_jobStatus.ErrorMsg), Log.LogEntryType.Error);
                _jobStatus.PercentageComplete = 0;
                return false;
            }

            // Input original
            string ffmpegParams = "-y -i " + FilePaths.FixSpaces(_originalFile);
            
            // Add New Audio stream
            if (!String.IsNullOrEmpty(newAudioStream))
            {
                // Take audio stream from new audio file
                ffmpegParams += " -i " + FilePaths.FixSpaces(newAudioStream) + " -map 1:a -acodec copy";

                // Video from the original file
                if (ffmpegStreamInfo.MediaInfo.VideoInfo.Stream != -1)
                    ffmpegParams += " -map 0:" + ffmpegStreamInfo.MediaInfo.VideoInfo.Stream.ToString() + " -vcodec copy"; // Fix for FFMPEG WTV MJPEG ticket #2227
                else
                    ffmpegParams += " -vn";
            }
            else
            {
                // Check for audio tracks
                if (ffmpegStreamInfo.AudioTracks > 0)
                    ffmpegParams += " -map 0:a -acodec copy";
                else
                    ffmpegParams += " -an";

                // Check for video tracks
                if (ffmpegStreamInfo.MediaInfo.VideoInfo.Stream != -1)
                    ffmpegParams += " -map 0:" + ffmpegStreamInfo.MediaInfo.VideoInfo.Stream.ToString() + " -vcodec copy"; // Fix for FFMPEG WTV MJPEG ticket #2227
                else
                    ffmpegParams += " -vn";
            }

            ffmpegParams += " " + FilePaths.FixSpaces(RemuxedTempFile);

            if (!FFmpeg.FFMpegExecuteAndHandleErrors(ffmpegParams, _jobStatus, _jobLog, FilePaths.FixSpaces(RemuxedTempFile))) // Let this function handle error conditions since it's just a simple execute
            {
                _jobStatus.ErrorMsg = Localise.GetPhrase("ffmpeg remux failed");
                _jobLog.WriteEntry(this, (_jobStatus.ErrorMsg), Log.LogEntryType.Error);
                return false;
            }

            _jobLog.WriteEntry(this, Localise.GetPhrase("FFMPEG ReMux moving file"), Log.LogEntryType.Information);
            return ReplaceTempRemuxed();
        }

        private bool MKVRemux()
        {
            _jobStatus.ErrorMsg = "";
            _jobStatus.PercentageComplete = 100; //all good to start with
            _jobStatus.ETA = "";
            
            Util.FileIO.TryFileDelete(RemuxedTempFile);

            string parameters = "--clusters-in-meta-seek -o " + FilePaths.FixSpaces(RemuxedTempFile) + " --compression -1:none " + FilePaths.FixSpaces(_originalFile);
            MKVMerge mkvMerge = new MKVMerge(parameters, _jobStatus, _jobLog);

            mkvMerge.Run();
            if (!mkvMerge.Success)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("MKVMerge failed"), Log.LogEntryType.Error);
                _jobStatus.ErrorMsg = Localise.GetPhrase("MKVMerge failed");
                return false;
            }

            _jobLog.WriteEntry(this, Localise.GetPhrase("MKVMerge remux moving file"), Log.LogEntryType.Information);
            return ReplaceTempRemuxed();
        }

        private bool MP4BoxRemux()
        {
            _jobStatus.ErrorMsg = "";
            _jobStatus.PercentageComplete = 100; //all good to start with
            _jobStatus.ETA = "";

            Util.FileIO.TryFileDelete(RemuxedTempFile);
            string Parameters = " -keep-sys -keep-all";

            //Check for Null FPS (bug with MediaInfo for some .TS files)
            if (_fps <= 0)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Mp4BoxRemuxAVI FPS 0 reported by video file - non compliant video file, skipping adding to parameter"), Log.LogEntryType.Warning);
                Parameters += " -add " + FilePaths.FixSpaces(_originalFile) +
                              " -new " + FilePaths.FixSpaces(RemuxedTempFile);
            }
            else
            {
                Parameters += " -fps " + _fps.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                              " -add " + FilePaths.FixSpaces(_originalFile) +
                              " -new " + FilePaths.FixSpaces(RemuxedTempFile);
            }

            MP4Box mp4Box = new MP4Box(Parameters, _jobStatus, _jobLog);
            mp4Box.Run();
            if (!mp4Box.Success || _jobStatus.PercentageComplete < GlobalDefs.ACCEPTABLE_COMPLETION)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("MP4BoxRemux failed"), Log.LogEntryType.Error);
                _jobStatus.ErrorMsg = Localise.GetPhrase("MP4BoxRemux failed");
                return false;
            }

            _jobLog.WriteEntry(this, Localise.GetPhrase("MP4Box remux moving file"), Log.LogEntryType.Information);
            return ReplaceTempRemuxed();
        }

        private bool MP4BoxRemuxAvi()
        {
            _jobStatus.ErrorMsg = "";
            _jobStatus.PercentageComplete = 100; //all good to start with
            _jobStatus.ETA = "";

            string fileNameBase = Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(_originalFile));
            string audioStream = fileNameBase + "_audio.raw";
            string newAudioStream = fileNameBase + "_audio.aac";
            string videoStream = fileNameBase + "_video.h264";

            Util.FileIO.TryFileDelete(RemuxedTempFile);

            // Video
            string Parameters = " -keep-sys -aviraw video -out " + FilePaths.FixSpaces(videoStream) + " " + FilePaths.FixSpaces(_originalFile);

            _jobStatus.CurrentAction = Localise.GetPhrase("Remuxing to") + " " + _remuxTo.ToLower() + " " + Localise.GetPhrase("Part") + " 1";

            MP4Box mp4Box = new MP4Box(Parameters, _jobStatus, _jobLog);
            mp4Box.Run();
            if (!mp4Box.Success || _jobStatus.PercentageComplete < GlobalDefs.ACCEPTABLE_COMPLETION) // check for completion of job
            {
                _jobStatus.ErrorMsg = "MP4Box Remux Video AVI failed";
                _jobLog.WriteEntry(this, _jobStatus.ErrorMsg + " at " + _jobStatus.PercentageComplete.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Error);
                return false;
            }

            // Audio
            Parameters = " -keep-all -keep-sys -aviraw audio -out " + FilePaths.FixSpaces(audioStream) + " " + FilePaths.FixSpaces(_originalFile);

            _jobStatus.CurrentAction = Localise.GetPhrase("Remuxing to") + " " + _remuxTo.ToLower() + " " + Localise.GetPhrase("Part") + " 2";

            mp4Box = new MP4Box(Parameters, _jobStatus, _jobLog);
            mp4Box.Run();
            if (!mp4Box.Success || _jobStatus.PercentageComplete < GlobalDefs.ACCEPTABLE_COMPLETION) //check for completion of job
            {
                _jobStatus.ErrorMsg = "MP4Box Remux Audio AVI failed";
                _jobLog.WriteEntry(this, _jobStatus.ErrorMsg + " at " + _jobStatus.PercentageComplete.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Error);
                return false;
            }

            // Check if streams are extracted
            if ((File.Exists(audioStream)) && (File.Exists(videoStream)))
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("MP4Box remux avi moving file"), Log.LogEntryType.Information);
                try
                {
                    Util.FileIO.TryFileDelete(newAudioStream);
                    FileIO.MoveAndInheritPermissions(audioStream, newAudioStream);
                }
                catch (Exception e)
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Unable to move remuxed stream") + " " + audioStream + " to " + newAudioStream + "\r\nError : " + e.ToString(), Log.LogEntryType.Error);
                    _jobStatus.PercentageComplete = 0;
                    _jobStatus.ErrorMsg = "Unable to move muxed stream";
                    return false;
                }

                string mergeParameters = " -keep-sys -keep-all";

                //Check for Null FPS (bug with MediaInfo for some .TS files)
                if (_fps <= 0)
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Mp4BoxRemuxAVI FPS 0 reported by video file - non compliant video file, skipping adding to parameter"), Log.LogEntryType.Warning);
                    mergeParameters += " -add " + FilePaths.FixSpaces(videoStream) +
                                       " -add " + FilePaths.FixSpaces(newAudioStream) +
                                       " -new " + FilePaths.FixSpaces(RemuxedTempFile);
                }
                else
                {
                    mergeParameters += " -fps " + _fps.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                                       " -add " + FilePaths.FixSpaces(videoStream) +
                                       " -add " + FilePaths.FixSpaces(newAudioStream) +
                                       " -new " + FilePaths.FixSpaces(RemuxedTempFile);
                }


                _jobStatus.CurrentAction = Localise.GetPhrase("Remuxing to") + " " + _remuxTo.ToLower() + " " + Localise.GetPhrase("Part") + " 3";

                mp4Box = new MP4Box(mergeParameters, _jobStatus, _jobLog);
                mp4Box.Run();
                if (!mp4Box.Success || _jobStatus.PercentageComplete < GlobalDefs.ACCEPTABLE_COMPLETION) // check for completion
                {
                    _jobStatus.ErrorMsg = "Mp4Box Remux Merger AVI with FPS conversion failed";
                    _jobLog.WriteEntry(this, _jobStatus.ErrorMsg, Log.LogEntryType.Error);
                    return false;
                }

                Util.FileIO.TryFileDelete(videoStream);
                Util.FileIO.TryFileDelete(newAudioStream);

                _jobLog.WriteEntry(this, Localise.GetPhrase("MP4Box remux AVI trying to move remuxed file"), Log.LogEntryType.Information);
                return ReplaceTempRemuxed();
            }
            else
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("MP4Box Remux AVI of") + " " + _originalFile + " " + Localise.GetPhrase("failed.  Extracted video and audio streams not found."), Log.LogEntryType.Error);
                _jobStatus.PercentageComplete = 0;
                _jobStatus.ErrorMsg = "Remux failed, extracted video streams not found";
                return false;
            }
        }

        /// <summary>
        /// Renames and moved the Remuxed file
        /// </summary>
        /// <returns></returns>
        private bool ReplaceTempRemuxed()
        {
            string newName = Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(_originalFile) + _remuxTo);
            _jobLog.WriteEntry(this, Localise.GetPhrase("Changing rexumed file names") + " \nOriginalFile : " + _originalFile + " \nReMuxedFile : " + RemuxedTempFile + " \nConvertedFile : " + newName, Log.LogEntryType.Debug);
            try
            {
                FileIO.TryFileDelete(newName); // Incase it exists, delete it
                FileIO.MoveAndInheritPermissions(RemuxedTempFile, newName); // Change the name of the remuxed file

                // If the original file is in the working temp directory, then delete it
                if (Path.GetDirectoryName(_originalFile).ToLower() == _workingPath.ToLower())
                    Util.FileIO.TryFileDelete(_originalFile);
                
                _remuxedFile = newName; // Point it to the renamed file
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Unable to move remuxed file") + " " + RemuxedTempFile + "\r\nError : " + e.ToString(), Log.LogEntryType.Error);
                _jobStatus.ErrorMsg = "Unable to move remux file";
                _jobStatus.PercentageComplete = 0;
                return false;
            }

            return true;
        }

        private string RemuxedTempFile
        {
            get { return Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(_originalFile) + "_REMUX" + _remuxTo); }
        }

        private bool MP4File(string ext)
        {
            switch (ext.ToLower())
            {
                case ".mp4":
                case ".m4v":
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Path to remuxed file
        /// </summary>
        public string RemuxedFile
        { get { return _remuxedFile; } }
    }
}
