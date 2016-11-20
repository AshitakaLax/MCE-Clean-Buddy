using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MCEBuddy.Util;
using MCEBuddy.Globals;
using MCEBuddy.AppWrapper;

namespace MCEBuddy.Transcode
{
    public class TrimVideo
    {
        private string _trimmedVideo = "";
        private JobStatus _jobStatus;
        private Log _jobLog;
        private string _profile;

        public TrimVideo(string profile, JobStatus jobStatus, Log jobLog)
        {
            _profile = profile;
            _jobLog = jobLog;
            _jobStatus = jobStatus;
        }

        public bool Trim(string sourceVideo, string workingPath, int startTrim, int endTrim)
        {
            _jobLog.WriteEntry(this, "Start Trim : " + startTrim.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Stop Trim : " + endTrim.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Video File : " + sourceVideo, Log.LogEntryType.Debug);

            if ((startTrim == 0) && (endTrim == 0))
            {
                _trimmedVideo = sourceVideo; // It's the same file
                return true; // nothing to do here
            }

            string tempFile = Path.Combine(workingPath, Path.GetFileNameWithoutExtension(sourceVideo) + "-temp" + Util.FilePaths.CleanExt(sourceVideo));

            // Get the length of the video, needed to calculate end point
            float Duration;
            Duration = VideoParams.VideoDuration(sourceVideo);
            FFmpegMediaInfo ffmpegStreamInfo = new FFmpegMediaInfo(sourceVideo, _jobStatus, _jobLog);
            if (!ffmpegStreamInfo.Success || ffmpegStreamInfo.ParseError)
            {
                _jobLog.WriteEntry(this, "Cannot read video info", Log.LogEntryType.Error);
                return false;
            }

            if (Duration <= 0)
            {
                // Converted file should contain only 1 audio stream
                Duration = ffmpegStreamInfo.MediaInfo.VideoInfo.Duration;
                _jobLog.WriteEntry(this, "Video duration : " + Duration.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Information);

                if (Duration == 0)
                {
                    _jobLog.WriteEntry(this, "Video duration 0", Log.LogEntryType.Error);
                    return false;
                }
            }

            // dont' use threads here since we are copying video to improve stability
            string ffmpegParams = "-y";

            ffmpegParams += " -i " + Util.FilePaths.FixSpaces(sourceVideo);

            // While setting the start trim before the input file (FAST seek) can speed up seeking and (http://ffmpeg.org/trac/ffmpeg/wiki/Seeking%20with%20FFmpeg) FAST seek (since accurate seek cuts on a NON KeyFrame which causes Audio Sync Issues)
            // Due to ffmpeg ticket #3252 we need to use ACCURATE seek (trim after input file) to avoid PTS<DTS error
            if (startTrim != 0)
            {
                if (startTrim < Duration)
                    ffmpegParams += " -ss " + startTrim.ToString(System.Globalization.CultureInfo.InvariantCulture);
                else
                {
                    _jobLog.WriteEntry(this, "Start trim (" + startTrim.ToString() + ") greater than file duration (" + Duration.ToString(System.Globalization.CultureInfo.InvariantCulture) + "). Skipping start trimming.", Log.LogEntryType.Warning);
                    startTrim = 0; // Skip it
                }
            }

            // Set the end trim (calculate from reducing from video length)
            if (endTrim != 0)
            {
                // FFMPEG can specify duration of encoding, i.e. encoding_duration = stopTime - startTime
                // startTime = startTrim, stopTime = video_duration - endTrim
                int encDuration = (((int)Duration) - endTrim) - (startTrim); // by default _startTrim is 0
                if (encDuration > 0)
                    ffmpegParams += " -t " + encDuration.ToString(System.Globalization.CultureInfo.InvariantCulture);
                else
                {
                    _jobLog.WriteEntry(this, "End trim (" + endTrim.ToString() + ") + Start trim (" + startTrim.ToString() + ") greater than file duration (" + Duration.ToString(System.Globalization.CultureInfo.InvariantCulture) + "). Skipping end trimming.", Log.LogEntryType.Warning);
                    endTrim = 0;
                }
            }

            // Sanity check once more
            if ((startTrim == 0) && (endTrim == 0))
            {
                _jobLog.WriteEntry(this, "Start trim and end trim skipped. Skipping trimming.", Log.LogEntryType.Warning);
                _trimmedVideo = sourceVideo; // It's the same file
                return true; // nothing to do here
            }

            // Check for audio channels
            if (ffmpegStreamInfo.AudioTracks > 0)
                ffmpegParams += " -map 0:a -acodec copy";
            else
                ffmpegParams += " -an";

            // Check for video stream
            if (ffmpegStreamInfo.MediaInfo.VideoInfo.Stream != -1)
                ffmpegParams += " -map 0:" + ffmpegStreamInfo.MediaInfo.VideoInfo.Stream.ToString() + " -vcodec copy"; // Fix for FFMPEG WTV MJPEG ticket #2227
            else
                ffmpegParams += " -vn";

            //Output file
            ffmpegParams += " " + Util.FilePaths.FixSpaces(tempFile);

            if (!FFmpeg.FFMpegExecuteAndHandleErrors(ffmpegParams, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(tempFile))) //check of file is created, outputhandler reports success (Percentage not requires since Success is more accurate)
            {
                _jobLog.WriteEntry("Failed to trim video at " + _jobStatus.PercentageComplete.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%", Log.LogEntryType.Error);
                return false;
            }

            // Replace the file
            if (File.Exists(tempFile))
            {
                _jobLog.WriteEntry(this, "TrimVideo trying to replace file Source : " + sourceVideo + " Temp : " + tempFile, Log.LogEntryType.Debug);

                // If the original uncut file is in the working temp directory, then just replace it
                if (Path.GetDirectoryName(sourceVideo).ToLower() == workingPath.ToLower())
                    Util.FileIO.TryFileReplace(sourceVideo, tempFile);
                else // If the original uncut video is not in the working temp directory, then just rename the tempFile with the original name and keep in the temp working directory (don't mangle original video file)
                {
                    FileIO.TryFileDelete(Path.Combine(workingPath, Path.GetFileName(sourceVideo))); // Just in case it exists
                    FileIO.MoveAndInheritPermissions(tempFile, Path.Combine(workingPath, Path.GetFileName(sourceVideo)));
                }

                _trimmedVideo = Path.Combine(workingPath, Path.GetFileName(sourceVideo)); // The final cut video always lies in the working directory
            }
            else
            {
                _jobLog.WriteEntry(this, "TrimVideo cannot find temp file " + tempFile, Log.LogEntryType.Error);
                _jobStatus.PercentageComplete = 0;
                return false;
            }

            return true; // All good here
        }

        /// <summary>
        /// Path to trimmed video file (source if untrimmed)
        /// </summary>
        public string TrimmedVideo
        { get { return _trimmedVideo; } }
    }
}
