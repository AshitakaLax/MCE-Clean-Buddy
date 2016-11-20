using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.AppWrapper
{
    public class FFmpeg : Base
    {
        private const string APP_PATH = "ffmpeg\\ffmpeg.exe";
        private const string DVRMS_APP_PATH = "ffmpeg\\ffmpeg.dvrms.exe";
        private float _Duration = 0;
        private double[] _dupArray = new double[0];
        private double _lastDup = 0;
        private double[] _dropArray = new double[0];
        private double _lastDrop = 0;
        private bool _muxError = false;
        private bool _h264mp4toannexbError = false; // Specific error condition when putting H.264 to MPEGTS format
        private bool _aacadtstoascError = false; // Specific error while converting TS to MP4 format
        private bool _encodeError = false; // Any critical error

        /// <summary>
        /// General FFMPEG to convert a file
        /// </summary>
        /// <param name="Parameters">Parameters to pass to FFMPEG</param>
        /// <param name="jobStatus">Reference to JobStatus</param>
        /// <param name="jobLog">JobLog</param>
        public FFmpeg(string Parameters, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(Parameters, APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            _Parameters = " -probesize 100M -analyzeduration 300M " + _Parameters; // We need to probe deeper into the files to get the correct audio / video track information else it can lead to incorrect channel information and failure
            _success = false; //ffmpeg looks for a +ve output so we have a false to begin with
            _uiAdminSessionProcess = true; // Assume we are always using ffmpeg build with hardware API's (UI Session 1 process)
        }

        /// <summary>
        /// FFMPEG to convert a DVRMS file
        /// </summary>
        /// <param name="Parameters">Parameters to pass to FFMPEG</param>
        /// <param name="DVRMS">Set to true if converting a DVRMS file</param>
        /// <param name="jobStatus">Reference to JobStatus</param>
        /// <param name="jobLog">JobLog</param>
        public FFmpeg(string Parameters, bool DVRMS, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(Parameters, DVRMS_APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            _success = false; //ffmpeg looks for a +ve output so we have a false to begin with
        }

        /// <summary>
        /// True of there is a Non Monotonically Increasing Muxing error
        /// </summary>
        public bool MuxError
        { get { return _muxError; } }

        /// <summary>
        /// True is there is a h264_mp4toannexb bitstream parsing error
        /// </summary>
        public bool H264MP4ToAnnexBError
        { get { return _h264mp4toannexbError; } }

        /// <summary>
        /// True is there is a aac_adtstoasc bitstream parsing error
        /// </summary>
        public bool AACADTSToASCError
        { get { return _aacadtstoascError; } }

        /// <summary>
        /// Returns true if any of the errors detected are a fatal error
        /// </summary>
        private bool FatalError
        { get { return (_muxError || _encodeError || _h264mp4toannexbError || _aacadtstoascError); } }

        /// <summary>
        /// Return the average rate of change in duplicate frames
        /// </summary>
        public double AverageDupROC
        {
            get
            {
                if (_dupArray.Length == 0)
                    return 0;
                else
                    return _dupArray.Average();
            } 
        }

        /// <summary>
        /// Return the average rate of change in dropped frames
        /// </summary>
        public double AverageDropROC
        {
            get
            {
                if (_dropArray.Length == 0)
                    return 0;
                else
                    return _dropArray.Average();
            }    
        }
        
        private float TimeStringToSecs(string timeString)
        {
            // Cater for different cuilds fo ffmpeg and their varying output
            if (timeString.Contains(":"))
            {
                float secs = 0;
                int mult = 1;
                string[] timeVals = timeString.Split(':');
                for (int i = timeVals.Length - 1; i >= 0; i--)
                {
                    float val = 0;
                    float.TryParse(timeVals[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out val);
                    secs += mult * val;
                    mult = mult * 60;
                }
                return secs;
            }
            else
            {
                float secs = 0;
                float.TryParse(timeString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out secs);
                return secs;
            }
        }

        protected override void OutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs ConsoleOutput)
        {
            try
            {
                string StdOut;
                int StartPos, EndPos;

                base.OutputHandler(sendingProcess, ConsoleOutput);
                if (ConsoleOutput.Data == null) return;

                if (!String.IsNullOrEmpty(ConsoleOutput.Data))
                {
                    StdOut = ConsoleOutput.Data;
                    if (StdOut.Contains("Duration:") && StdOut.Contains(",") && (_Duration < 1))
                    {
                        StartPos = StdOut.IndexOf("Duration:") + "Duration:".Length;
                        EndPos = StdOut.IndexOf(",", StartPos);
                        _Duration = TimeStringToSecs(StdOut.Substring(StartPos, EndPos - StartPos));
                        _jobLog.WriteEntry("Video duration=" + _Duration, Log.LogEntryType.Debug);
                    }
                    else if (StdOut.Contains("frame=") && StdOut.Contains("time="))
                    {
                        StartPos = StdOut.IndexOf("time=") + "time=".Length;
                        EndPos = StdOut.IndexOf(" ", StartPos);
                        float timeCoded = TimeStringToSecs(StdOut.Substring(StartPos, EndPos - StartPos));
                        if (timeCoded > 0)
                        {
                            _jobStatus.PercentageComplete = (float)(timeCoded / _Duration * 100);
                            UpdateETAByPercentageComplete();
                        }

                    }

                    // Capture average rate of change in duplicate frames
                    if (StdOut.Contains("dup="))
                    {
                        StartPos = StdOut.IndexOf("dup=") + "dup=".Length;
                        EndPos = StdOut.IndexOf(" ", StartPos);

                        Array.Resize(ref _dupArray, _dupArray.Length + 1); // increase the array size by 1 to store new value
                        double dup = 0;
                        double.TryParse(StdOut.Substring(StartPos, EndPos - StartPos), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out dup);

                        _dupArray[_dupArray.Length - 1] = dup - _lastDup; // We are storing the change in dup value to calcuate the average rate of change in duplicate packets
                        _lastDup = dup;
                    }

                    // Capture average rate of change in dropped frames
                    if (StdOut.Contains("drop="))
                    {
                        StartPos = StdOut.IndexOf("drop=") + "drop=".Length;
                        EndPos = StdOut.IndexOf(" ", StartPos);

                        Array.Resize(ref _dropArray, _dropArray.Length + 1); // increase the array size by 1 to store new value
                        double drop = 0;
                        double.TryParse(StdOut.Substring(StartPos, EndPos - StartPos), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out drop);

                        _dropArray[_dropArray.Length - 1] = drop - _lastDrop; // We are storing the change in dup value to calcuate the average rate of change in duplicate packets
                        _lastDrop = drop;
                    }

                    // TODO: We need to track down more errors from av_interleaved_write_frame() and ffmpeg and put them here, newer builds report global headers even if 1 frame is encoded. The only other option is to look at the return value of ffmpeg for an error, but then wtv files are often cut and this may not work with them.
                    // av_interleaved_write_frame(): Invalid argument
                    // av_interleaved_write_frame(): Invalid data found when processing input
                    if (StdOut.Contains(@"av_interleaved_write_frame(): Invalid"))
                        _encodeError = true;

                    // use audio bitstream filter 'aac_adtstoasc' to fix it
                    if (StdOut.Contains("use") && StdOut.Contains("bitstream filter") && StdOut.Contains("aac_adtstoasc"))
                        _aacadtstoascError = true;

                    // [wtv @ 0000000003ffab20] H.264 bitstream malformed, no startcode found, use the h264_mp4toannexb bitstream filter (-bsf h264_mp4toannexb)
                    // [mpegts @ 02bdd680] H.264 bitstream malformed, no startcode found, use the video bitstream filter 'h264_mp4toannexb' to fix it ('-bsf:v h264_mp4toannexb' option with ffmpeg)
                    if (StdOut.Contains("use") && StdOut.Contains("bitstream filter") && StdOut.Contains("h264_mp4toannexb"))
                        _h264mp4toannexbError = true;

                    if (StdOut.Contains("non monotonically increasing"))
                        _muxError = true;

                    if ((StdOut.Contains("global headers")) && (StdOut.Contains("muxing overhead"))) // This is always the last line printed, so any errors happen before this is printed
                        _success = true && !FatalError;
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "ERROR Processing Console Output.\n" + e.ToString(), Log.LogEntryType.Error);
            }
        }

        /// <summary>
        /// Generic function to run a FFMPEG command handle common errors related to FFMPEG conversion
        /// </summary>
        /// <param name="cmdParams">Command parameters to pass to ffmpeg</param>
        /// <param name="outputFile">Exact name of the output file as it appears in the command paramters (including quotes if required)</param>
        /// <returns>True if successful</returns>
        public static bool FFMpegExecuteAndHandleErrors(string cmdParams, JobStatus jobStatus, Log jobLog, string outputFile)
        {
            return FFMpegExecuteAndHandleErrors(cmdParams, jobStatus, jobLog, outputFile, true);
        }

        /// <summary>
        /// Generic function to run a FFMPEG command handle common errors related to FFMPEG conversion
        /// </summary>
        /// <param name="cmdParams">Command parameters to pass to ffmpeg</param>
        /// <param name="outputFile">Exact name of the output file as it appears in the command paramters (including quotes if required)</param>
        /// <param name="checkZeroOutputFileSize">If true, will check output file size and function will return false if the size is 0 or file doesn't exist, false to ignore output filesize and presence</param>
        /// <returns>True if successful</returns>
        public static bool FFMpegExecuteAndHandleErrors(string cmdParams, JobStatus jobStatus, Log jobLog, string outputFile, bool checkZeroOutputFileSize)
        {
            FFmpeg retFFMpeg; // Dummy
            return FFMpegExecuteAndHandleErrors(cmdParams, jobStatus, jobLog, outputFile, checkZeroOutputFileSize, out retFFMpeg);
        }
        
        /// <summary>
        /// Generic function to run a FFMPEG command handle common errors related to FFMPEG conversion
        /// </summary>
        /// <param name="cmdParams">Command parameters to pass to ffmpeg</param>
        /// <param name="outputFile">Exact name of the output file as it appears in the command paramters (including quotes if required)</param>
        /// <param name="checkZeroOutputFileSize">If true, will check output file size and function will return false if the size is 0 or file doesn't exist, false to ignore output filesize and presence</param>
        /// <param name="ffmpegExecutedObject">Returns a pointer to the final executed ffmpeg object</param>
        /// <returns>True if successful</returns>
        public static bool FFMpegExecuteAndHandleErrors(string cmdParams, JobStatus jobStatus, Log jobLog, string outputFile, bool checkZeroOutputFileSize, out FFmpeg ffmpegExecutedObject)
        {
            FFmpeg ffmpeg = new FFmpeg(cmdParams, jobStatus, jobLog);
            ffmpeg.Run();

            // Check if it's a h264_mp4toannexb error, when converting H.264 video to MPEGTS format this can sometimes be an issue
            if (!ffmpeg.Success && ffmpeg.H264MP4ToAnnexBError)
            {
                jobLog.WriteEntry("h264_mp4toannexb error, retying and setting bitstream flag", Log.LogEntryType.Warning);

                // -bsf h264_mp4toannexb is required when this error occurs, put it just before the output filename
                cmdParams = cmdParams.Insert(cmdParams.IndexOf(outputFile), "-bsf h264_mp4toannexb ");
                ffmpeg = new FFmpeg(cmdParams, jobStatus, jobLog);
                ffmpeg.Run();
            }

            // Check if it's a aac_adtstoasc error, when converting AAC Audio from MPEGTS to MP4 format this can sometimes be an issue
            if (!ffmpeg.Success && ffmpeg.AACADTSToASCError)
            {
                jobLog.WriteEntry("aac_adtstoasc error, retying and setting bitstream flag", Log.LogEntryType.Warning);

                // -bsf aac_adtstoasc is required when this error occurs, put it just before the output filename
                cmdParams = cmdParams.Insert(cmdParams.IndexOf(outputFile), "-bsf:a aac_adtstoasc ");
                ffmpeg = new FFmpeg(cmdParams, jobStatus, jobLog);
                ffmpeg.Run();
            }

            // Check if we are asked to check for output filesize only here (generic errors)
            // Otherwise it might an error related to genpts (check if we ran the previous ffmpeg related to h264_mp4toannexb and it succeded) - genpts is done after trying to fix other issues
            if (checkZeroOutputFileSize)
                jobLog.WriteEntry("Checking output file size [KB] -> " + (Util.FileIO.FileSize(outputFile) / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            if ((!ffmpeg.Success || (checkZeroOutputFileSize ? (FileIO.FileSize(outputFile) <= 0) : false)) && !cmdParams.Contains("genpts")) // Possible that some combinations used prior to calling this already have genpts in the command line
            {
                jobLog.WriteEntry("Ffmpeg conversion failed, retying using GenPts", Log.LogEntryType.Warning);

                // genpt is required sometimes when -ss is specified before the inputs file, see ffmpeg ticket #2054
                cmdParams = "-fflags +genpts " + cmdParams;
                ffmpeg = new FFmpeg(cmdParams, jobStatus, jobLog);
                ffmpeg.Run();
            }

            // Check again post genpts if it's a h264_mp4toannexb error (if not already done), when converting H.264 video to MPEGTS format this can sometimes be an issue
            if (!ffmpeg.Success && ffmpeg.H264MP4ToAnnexBError && !cmdParams.Contains("h264_mp4toannexb"))
            {
                jobLog.WriteEntry("H264MP4ToAnnexBError error, retying and setting bitstream flag", Log.LogEntryType.Warning);

                // -bsf h264_mp4toannexb is required when this error occurs, put it just before the output filename
                cmdParams = cmdParams.Insert(cmdParams.IndexOf(outputFile), "-bsf h264_mp4toannexb ");
                ffmpeg = new FFmpeg(cmdParams, jobStatus, jobLog);
                ffmpeg.Run();
            }

            // Check again post genpts if it's a aac_adtstoasc error (if not already done), when converting AAC Audio from MPEGTS to MP4 format this can sometimes be an issue
            if (!ffmpeg.Success && ffmpeg.AACADTSToASCError && !cmdParams.Contains("aac_adtstoasc"))
            {
                jobLog.WriteEntry("aac_adtstoasc error, retying and setting bitstream flag", Log.LogEntryType.Warning);

                // -bsf aac_adtstoasc is required when this error occurs, put it just before the output filename
                cmdParams = cmdParams.Insert(cmdParams.IndexOf(outputFile), "-bsf:a aac_adtstoasc ");
                ffmpeg = new FFmpeg(cmdParams, jobStatus, jobLog);
                ffmpeg.Run();
            }

            ffmpegExecutedObject = ffmpeg; // Set the return object to the final run ffmpeg object

            jobLog.WriteEntry("FFMpeg output file size [KB] -> " + (Util.FileIO.FileSize(outputFile) / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            return (ffmpeg.Success && (checkZeroOutputFileSize ? (FileIO.FileSize(outputFile) > 0) : true));
        }
    }
}
