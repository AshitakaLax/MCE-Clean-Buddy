using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using System.IO;

using Newtonsoft.Json;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.AppWrapper
{
    // This class collects information about the Video file using the output of FFMPEG (Video and Audio information)
    public class FFmpegMediaInfo : Base
    {
        #region Classes
        public class InterlaceDetection
        {
            public long TFF;
            public long BFF;
            public long Progressive;
            public long Undetermined;

            public override string ToString()
            {
                string ret = "";

                ret += "Top Frame First -> " + TFF.ToString() + "\r\n";
                ret += "Bottom Frame First -> " + BFF.ToString() + "\r\n";
                ret += "Progressive Frames -> " + Progressive.ToString() + "\r\n";
                ret += "Undetermined Frames -> " + Undetermined.ToString() + "\r\n";

                return ret;
            }
        }

        public class Disposition
        {
            public int @default { get; set; }
            public int dub { get; set; }
            public int original { get; set; }
            public int comment { get; set; }
            public int lyrics { get; set; }
            public int karaoke { get; set; }
            public int forced { get; set; }
            public int hearing_impaired { get; set; }
            public int visual_impaired { get; set; }
            public int clean_effects { get; set; }
            public int attached_pic { get; set; }
        }

        public class Tags
        {
            public string language { get; set; }
            public string title { get; set; }
        }

        public class Stream
        {
            public int index { get; set; }
            public string codec_type { get; set; }
            public string codec_time_base { get; set; }
            public string codec_tag_string { get; set; }
            public string codec_tag { get; set; }
            public string id { get; set; }
            public string r_frame_rate { get; set; }
            public string avg_frame_rate { get; set; }
            public string time_base { get; set; }
            public long start_pts { get; set; }
            public string start_time { get; set; }
            public long duration_ts { get; set; }
            public string duration { get; set; }
            public Disposition disposition { get; set; }
            public string codec_name { get; set; }
            public string codec_long_name { get; set; }
            public string sample_fmt { get; set; }
            public string sample_rate { get; set; }
            public int? channels { get; set; }
            public string channel_layout { get; set; }
            public int? bits_per_sample { get; set; }
            public string dmix_mode { get; set; }
            public string ltrt_cmixlev { get; set; }
            public string ltrt_surmixlev { get; set; }
            public string loro_cmixlev { get; set; }
            public string loro_surmixlev { get; set; }
            public string bit_rate { get; set; }
            public Tags tags { get; set; }
            public string profile { get; set; }
            public int? width { get; set; }
            public int? height { get; set; }
            public int? has_b_frames { get; set; }
            public string sample_aspect_ratio { get; set; }
            public string display_aspect_ratio { get; set; }
            public string pix_fmt { get; set; }
            public int? level { get; set; }
            public string timecode { get; set; }
        }

        public class Format
        {
            public string filename { get; set; }
            public int nb_streams { get; set; }
            public int nb_programs { get; set; }
            public string format_name { get; set; }
            public string format_long_name { get; set; }
            public string start_time { get; set; }
            public string duration { get; set; }
            public string size { get; set; }
            public string bit_rate { get; set; }
            public int probe_score { get; set; }
            public object tags { get; set; }
        }

        public class TagsP
        {
            public string service_name { get; set; }
            public string service_provider { get; set; }
        }

        public class Program
        {
            public int program_id { get; set; }
            public int program_num { get; set; }
            public int nb_streams { get; set; }
            public int pmt_pid { get; set; }
            public int pcr_pid { get; set; }
            public long start_pts { get; set; }
            public string start_time { get; set; }
            public long end_pts { get; set; }
            public string end_time { get; set; }
            public TagsP tags { get; set; }
            public List<Stream> streams { get; set; }
        }
        
        public class FFProbeResults
        {
            public List<Program> programs { get; set; }
            public List<Stream> streams { get; set; }
            public Format format { get; set; }
        }
        #endregion

        #region Variables
        private InterlaceDetection singleFrameIDETResults = new InterlaceDetection();
        private InterlaceDetection multiFrameIDETResults = new InterlaceDetection();
        private ulong IDET_SKIP_SECONDS = 240; // Skip the first x seconds while detecting the interlace mode in a video (skip the initial parts of the video)
        private ulong IDET_ANALYZE_SECONDS = 600; // Number of seconds of the video to analyze

        private const string FFMPEG_APP_PATH = "ffmpeg\\ffmpeg.exe";
        private const string FFPROBE_APP_PATH = "ffmpeg\\ffprobe.exe";
        private const float MAX_FPS = 10000.0F; // Maximum valid frame rate (beyond which it's corrupted and assumed 0)
        private const float FPS_MULTIPLIER_THRESHOLD = 1.5F; // Max acceptable "real" frame rate (TBR) over "average" frame rate (FPS)
        private MediaInfo mediaInfo;
        private int audioTracks = 0; // keep count of how many audio there are reported
        private int zeroChannelTracks = 0; // keep count of how many zero bit rate audio tracks are reported
        private int impariedAudioTracks = 0; // keep count of how many imparied audio tracks there are reported
        private int subtitleTracks = 0; // keep count of subtitle tracks
        private bool parseError = false;
        private bool parseCompleted = false;
        private bool idetMode = false; // Are we running an interlaced idet video filter detection (special mode)
        private bool checkVersionMode = false; // Are we just checking the version or dumping, then don't parse output
        private bool useFFProbe = true; // Use ffprobe instead of ffmpeg to get media information by default
        private string ffProbeOutput = ""; // Output to parse from ffprobe
        #endregion

        #region Constants
        // Various bit sample format supported by FFMPEG
        private static string[,] sampleFmts = {
                                        {"u8","8"},
                                        {"s16","16"},
                                        {"s32","32"},
                                        {"flt","32"},
                                        {"dbl","64"},
                                        {"u8p","8"},
                                        {"s16p","16"},
                                        {"s32p","32"},
                                        {"fltp","32"},
                                        {"dblp","64"}
                                   };

        private static string[,] channelStrings = {
                                                      {"9.1","10"},
                                                      {"9.0","9"},
                                                      {"9 channel","9"},
                                                      {"8.1","9"},
                                                      {"8.0","8"},
                                                      {"8 channel","8"},
                                                      {"7.1","8"},
                                                      {"7.0","7"},
                                                      {"7 channel","7"},
                                                      {"6.1","7"},
                                                      {"6.0","6"},
                                                      {"6 channel", "6"}, // since we doing a Contains we are looking for channel and not channels
                                                      {"5.1","6"},
                                                      {"5.0","5"},
                                                      {"5 channel","5"},
                                                      {"4.1","5"},
                                                      {"4.0","4"},
                                                      {"4 channel","4"},
                                                      {"3.1","4"},
                                                      {"3.0","3"},
                                                      {"3 channel","3"},
                                                      {"2.1","3"},
                                                      {"2.0","2"},
                                                      {"2 channel","2"},
                                                      {"stereo","2"},
                                                      {"1.1","2"},
                                                      {"1.0","1"},
                                                      {"1 channel","1"},
                                                      {"mono","1"},
                                                      {"0.1","1"},
                                                      {"0 channel","0"},
                                                      {"0.0","0"},
                                                      {"no audio","0"}
                                                  };
        #endregion

        #region PublicVariables
        /// <summary>
        /// True if there was an error while trying to parse the media information
        /// </summary>
        public bool ParseError
        { get { return parseError; } }

        /// <summary>
        /// Total number of audio tracks detected
        /// </summary>
        public int AudioTracks
        { get { return audioTracks; } }

        /// <summary>
        /// Total number of subtitle tracks detected
        /// </summary>
        public int SubtitleTracks
        { get { return subtitleTracks; } }

        /// <summary>
        /// Total number of Zero Channel Audio tracks detected
        /// </summary>
        public int ZeroChannelAudioTrackCount
        { get { return zeroChannelTracks; } }

        /// <summary>
        /// Total number of Impaired (all types) Audio tracks detected
        /// </summary>
        public int ImpariedAudioTrackCount
        { get { return impariedAudioTracks; } }

        /// <summary>
        /// Object which contains the detailed information about the file analyzed
        /// </summary>
        public MediaInfo MediaInfo
        { get { return mediaInfo; } }

        /// <summary>
        /// Results of the idet video filter Multi Frame Detection when scan detection is run
        /// </summary>
        public InterlaceDetection MFInterlaceDetectionResults
        { get { return multiFrameIDETResults; } }

        /// <summary>
        /// Results of the idet video filter Single Frame Detection when scan detection is run
        /// </summary>
        public InterlaceDetection SFInterlaceDetectionResults
        { get { return singleFrameIDETResults; } }
        #endregion

        /// <summary>
        /// Gets information about the video file and stores it.
        /// The ParseError flag is set if there is an error trying to parse the video information, in which the information available in not reliable.
        /// Run automatically on initialization
        /// </summary>
        /// <param name="ignoreSuspend">Set this if you want to ignore the suspend/pause command, typically used when this function is called from a GUI to prevent a deadlock/hang</param>
        public FFmpegMediaInfo(string fileName, JobStatus jobStatus, Log jobLog, bool ignoreSuspend = false)
            : base(fileName, FFPROBE_APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            // Check if FFProbe exists, if not then fallback to FFMpeg
            if (useFFProbe && !File.Exists(_ApplicationPath))
            {
                jobLog.WriteEntry(this, "FFProbe not found, switching to FFMpeg", Log.LogEntryType.Warning);
                _ApplicationPath = Path.Combine(GlobalDefs.AppPath, FFMPEG_APP_PATH);
                useFFProbe = false;
            }

            mediaInfo = new MediaInfo();
            mediaInfo.VideoInfo = new MediaInfo.Video(); // We have only 1 video track per file, audio/subtitle tracks are created and added as detected
            _success = true; // information always suceeds unless we find an error in the output
            // -probesize 100M -analyzeduration 300M are important to identify broken audio streams in some files
            // TODO: FFPROBE -> For now we only process independent streams and ignore streams embedded within programs (-show_programs). This is because streams embedded within programs do not have unique stream ID's and PID's (they repeat within each program). How does MCEBuddy handle mpegts_service_id (programs)?
            if (useFFProbe) // FFPROBE
                _Parameters = " -probesize 100M -analyzeduration 300M -v quiet -print_format json -show_format -show_streams -i " + Util.FilePaths.FixSpaces(fileName); // FFPROBE create the format for run the command
            else // FFMPEG
                _Parameters = " -probesize 100M -analyzeduration 300M -i " + Util.FilePaths.FixSpaces(fileName); // FFMPEG create the format for run the command
            Run();
        }

        /// <summary>
        /// Special function to run Scan Type detection using the IDET video filter.
        /// By default, this will skip 240 seconds into the video and then analyze it for 600 seconds to collect data about the field frames.
        /// You can override the default <paramref name="skipSeconds"/> and <paramref name="analyzeSeconds"/> by putting non 0 values in the respective parameters.
        /// The results are stored in the MFInterlaceDetectionResults (Multi Frame MORE reliable) and SFInterlaceDetectionResults (Single Frame).
        /// When this mode is active, ONLY interlace detection is done, the rest of the MediaInfo is NOT available (null)
        /// Run automatically on initialization
        /// </summary>
        /// <param name="skipSeconds">Number of seconds of the initial video to skip before starting interlace detection (0 for default)</param>
        /// <param name="analyzeSeconds">Number of seconds of the video to analyze for interlace detection (0 for default)</param>
        public FFmpegMediaInfo(string fileName, JobStatus jobStatus, Log jobLog, ulong skipSeconds, ulong analyzeSeconds, bool ignoreSuspend = false)
            : base(fileName, FFMPEG_APP_PATH, jobStatus, jobLog, ignoreSuspend)
        {
            mediaInfo = null; // In this mode, there is no media info
            idetMode = true; // We are running a special mode
            useFFProbe = false; // We are using FFMPEG here not FFProbe
            parseError = true; // In this mode parse error is default until we find what we need
            _success = true; // information always suceeds unless we find an error in the output

            // Check for custom overrides
            if (skipSeconds > 0)
                IDET_SKIP_SECONDS = skipSeconds;

            if (analyzeSeconds > 0)
                IDET_ANALYZE_SECONDS = analyzeSeconds;

            // -probesize 100M -analyzeduration 300M are important to identify broken audio streams in some files
            _Parameters = " -probesize 100M -analyzeduration 300M -y -ss " + IDET_SKIP_SECONDS.ToString(CultureInfo.InvariantCulture) + " -i " + Util.FilePaths.FixSpaces(fileName) + " -vf idet -an -sn -t " + IDET_ANALYZE_SECONDS.ToString(CultureInfo.InvariantCulture) + " -f rawvideo NUL"; // create the format for run the command
            Run();
        }

        /// <summary>
        /// Resets the video params to default (except Bitrate and Duration), to be called ONLY by FFMPEG parser
        /// </summary>
        private void ResetFFmpegVideoInfo()
        {
            // Save the Bitrate and Duration since FFMPEG parses those before the video streams
            float duration = mediaInfo.VideoInfo.Duration;
            int bitrate = mediaInfo.VideoInfo.BitRate;
            mediaInfo.VideoInfo = new MediaInfo.Video(); // Create new with defaults
            mediaInfo.VideoInfo.BitRate = bitrate; // Restore it
            mediaInfo.VideoInfo.Duration = duration;  // Restore it
        }

        /// <summary>
        /// Convert the time string (e.g. 00:28:13.87) to time in seconds
        /// </summary>
        /// <param name="timeString">Time in string format (e.g. 00:28:13.87)</param>
        /// <returns>Time in seconds</returns>
        private float TimeStringToSecs(string timeString)
        {
            // Cater for different builds of ffmpeg and their varying output
            if (timeString.Contains(":"))
            {
                float secs = 0;
                int mult = 1;
                string[] timeVals = timeString.Split(':');
                for (int i = timeVals.Length - 1; i >= 0; i--)
                {
                    float val = 0;
                    float.TryParse(timeVals[i], NumberStyles.Float, CultureInfo.InvariantCulture, out val);
                    secs += mult * val;
                    mult = mult * 60;
                }
                return secs;
            }
            else
            {
                float secs = 0;
                float.TryParse(timeString, NumberStyles.Float, CultureInfo.InvariantCulture, out secs);
                return secs;
            }
        }

        private int NthIndex(string String, char Character, int Index)
        {
            int count = 0;
            for (int i = 0; i < String.Length; i++)
            {
                if (String[i] == Character)
                {
                    count++;
                    if (count == Index)
                        return i;
                }
            }
            return -1;
        }

        private int CheckFmts(string fmt)
        {
            for (int i = 0; i < sampleFmts.Length / 2; i++)
            {
                if (fmt.Contains(sampleFmts[i, 0]))
                    return int.Parse(sampleFmts[i, 1]); // Find bits per sample
            }

            return -1;
        }

        private int CheckChannels(string fmt)
        {
            for (int i = 0; i < channelStrings.Length / 2; i++)
            {
                if (fmt.Contains(channelStrings[i, 0]))
                    return int.Parse(channelStrings[i, 1]); // Channel information
            }

            return -1;
        }

        private void ParseFFMPEGMediaInformation(string StdOut)
        {
            /*  SAMPLE OUTPUTS of FFMPEG
             * 
             *   Duration: 00:28:13.87, start: 44.031933, bitrate: 14735 kb/s
             *   Duration: N/A, start: 1.400000, bitrate: N/A
             *   Stream #0:28[0x942]: Video: mpeg2video ([2][0][0][0] / 0x0002), 90k tbr, 90k tbn
             *   Stream #0:0: Video: mpeg2video (Main), yuv420p(tv), 1920x1080 [SAR 1:1 DAR 16:9], max. 24000 kb/s, SAR 91:64 DAR 91:36, 30k fps, 29.97 tbr, 1k tbn, 59.94 tbc (default)                         *   Stream #0:0(und): Video: h264 (High) (avc1 / 0x31637661), yuv420p, 688x352 [SAR 8:9 DAR 172:99], 1720 kb/s, SAR 611:688 DAR 611:352, 29.97 fps, 29.97 tbr, 120k tbn, 59.94 tbc
             *   Stream #0:1[0x2b]: Video: mpeg2video (Main), yuv420p, 1920x1080 [SAR 1:1 DAR 16:9], 65000 kb/s, 1018.98 fps, 59.94 tbr, 10000k tbn, 59.94 tbc
             *   Stream #0:1[0x12]: Video: mpeg2video (Main), yuv420p, 720x480 [SAR 8:9 DAR 4:3], 44.96 fps, 29.97 tbr, 10000k tbn, 59.94 tbc
             *   Stream #0:2(eng): Video: mpeg2video (Main) (DVR  / 0x20525644), yuv420p, 1920x1080 [SAR 1:1 DAR 16:9], 17098 kb/s, 30.30 fps, 29.97 tbr, 1k tbn, 59.94 tbc
             *   Stream #0:1[0x33]: Video: mpeg2video (Main), yuv420p(tv), 720x480 [SAR 8:9 DAR 4:3], max. 9000 kb/s, 29.97 fps, 29.97 tbr, 10000k tbn, 59.94 tbc
             *   Stream #0:0[0xa]: Video: mpeg2video (Main), yuv420p(tv, bt709), 1920x1080 [SAR 1:1 DAR 16:9], max. 16578 kb/s, 29.97 fps, 29.97 tbr, 10000k tbn, 59.94 tbc
             *   Stream #0:4[0x4e]: Video: h264 (High), yuv420p(tv, bt709), 1920x1080 [SAR 1:1 DAR 16:9], 25 fps, 50 tbr, 10000k tbn, 50 tbc
             *   Stream #0:0: Video: mpeg4 (Simple Profile) (XVID / 0x44495658), yuv420p, 640x464 [SAR 1:1 DAR 40:29], 23.98 tbr, 23.98 tbn, 23.98 tbc
             *   Stream #0:1: Video: mpeg4 (Advanced Simple Profile) (xvid / 0x64697678), yuv420p, 720x400 [SAR 83:91 DAR 747:455], SAR 595:603 DAR 119:67, 29.97 fps, 29.97 tbr, 29.97 tbn, 30k tbc
             *   Stream #0:0: Video: msmpeg4 (DIV3 / 0x33564944), yuv420p, 352x288, 29.97 tbr, 29.97 tbn, 29.97 tbc
             *   Stream #0:1[0x1e0]: Video: mpeg1video, yuv420p, 352x240 [SAR 200:219 DAR 880:657], 1150 kb/s, 29.97 fps, 29.97 tbr, 90k tbn, 29.97 tbc
             *   Stream #0:0(und): Video: h264 (Constrained Baseline) (avc1 / 0x31637661), yuv420p, 640x464 [SAR 159:160 DAR 159:116], 1753 kb/s, 23.98 fps, 90k tbr, 90k tbn, 180k tbc
             *   Stream #0:0(eng): Video: h264 (High), yuv420p, 1920x800, SAR 1:1 DAR 12:5, 23.98 fps, 23.98 tbr, 1k tbn, 47.95 tbc (default)
             *   Stream #0:1: Video: mpeg2video (DVR  / 0x20525644), yuv420p, 720x480 [SAR 8:9 DAR 4:3], q=2-31, 8250 kb/s, 31.57 fps, 90k tbn, 29.97 tbc
             *   Stream #0:0[0x1100]: Video: mpeg2video (Main) (2000 / 0x0002), yuv420p, 720x576 SAR 64:45 DAR 16:9, 15000 kb/s, 25.84 fps, 25 tbr, 90k tbn, 50 tbc
             *   Stream #0:5[0xffffffff]: Video: mjpeg, yuvj420p(pc), 200x113 [SAR 96:96 DAR 200:113], 90k tbr, 90k tbn, 90k tbc
             *   Stream #0:2[0x2c](spa): Audio: ac3, 48000 Hz, stereo, s16, 192 kb/s
             *   Stream #0:0[0x2a](eng): Audio: ac3, 48000 Hz, 5.1(side), s16, 384 kb/s
             *   Stream #0:1(eng): Audio: dts (DTS-ES), 48000 Hz, 7 channels (FL+FR+FC+LFE+BC+SL+SR), s16, 1536 kb/s (default)
             *   Stream #0:2[0x13]: Audio: mp2 (P[0][0][0] / 0x0050), 48000 Hz, stereo, s16, 384 kb/s
             *   Stream #0:1[0x843]: Audio: mp1, 0 channels, s16
             *   Stream #0:0: Audio: ac3 ([0] [0][0] / 0x2000), 48000 Hz, stereo, s16, 192 kb/s
             *   Stream #0:0(eng): Audio: ac3, 48000 Hz, 5.1(side), s16, 384 kb/s
             *   Stream #0:1[0x46](): Audio: ac3 (AC-3 / 0x332D4341), 48000 Hz, stereo, fltp, 192 kb/s
             *   Stream #0:1: Audio: mp3 (U[0][0][0] / 0x0055), 12000 Hz, stereo, s16, 32 kb/s
             *   Stream #0:4[0x2d](nar): Audio: mp2 (P[0][0][0] / 0x0050), 48000 Hz, stereo, s16p, 256 kb/s (visual impaired)
             *   Stream #0:0[0x75](eng): Audio: mp2 (P[0][0][0] / 0x0050), 48000 Hz, mono, s16p, 64 kb/s (hearing impaired)
             *   Stream #0:1[0x3f](eng): Audio: aac_latm ([2][22][0][0] / 0x1602), 48000 Hz, stereo, fltp (hearing impaired)
             *   Stream #0:0[0x11]: Subtitle: dvb_teletext
             *   Stream #0:3[0x2d](eng): Subtitle: dvb_teletext
             */

            // TODO: For now we only process independent streams and ignore programs and streams embedded within programs. This is because streams embedded within programs do not have unique stream ID's and PID's (they repeat within each program). How does MCEBuddy handle mpegts_service_id (programs)?
            // To handle only independent streams and ignore programs and  embedded streams, independent streams show up at the end, so if the streams id is repeated we overwrite the existing stream id and typically indepedent streams outnumber the embedded streams
            try
            {
                // Parsing the Video section - we break this up into 4 sections, <Stream #0> <1[0x2b](eng)> <Video> <the rest...>
                if (StdOut.Contains("Stream #0:") && StdOut.Contains("Video")) // Consider only the 1st video stream with a width, some formats such as TiVO have multiple video streams and WTV with newer ffmpeg have mjpeg attachments reported as video also
                {
                    string section1 = StdOut.Substring(0, NthIndex(StdOut, ':', 1)); // Header, <Stream #0> - JUNK
                    string section2 = StdOut.Substring(NthIndex(StdOut, ':', 1) + 1, NthIndex(StdOut, ':', 2) - NthIndex(StdOut, ':', 1) - 1); // <1[0x2b](eng)>
                    string section3 = StdOut.Substring(NthIndex(StdOut, ':', 2) + 1, NthIndex(StdOut, ':', 3) - NthIndex(StdOut, ':', 2) - 1); // <Video> - JUNK
                    string section4 = StdOut.Substring(NthIndex(StdOut, ':', 3) + 1); // <the rest...>

                    // SANITY CHECK - Sometimes WTV has mjpeg attachments which FFMPEG (bug #2227) incorrectly reports as video streams - IGNORE THEM - DO THIS FIRST
                    // Parse section 4 to extract the remaining information
                    // First parse the codec
                    string codecString = section4.Substring(0, section4.IndexOf(','));
                    string videoCodec = "";
                    if (codecString.Contains("("))
                        videoCodec = codecString.Substring(0, codecString.IndexOf('(')).ToLower().Trim(); // Video codec
                    else
                        videoCodec = codecString.Substring(0).ToLower().Trim(); // Video codec

                    if (videoCodec == "mjpeg") // DO NOT PROCESS MJPEG AS VIDEO - hack even though ffmpeg reports it as video
                        goto DoneParsingVideo; // Get out and clean up

                    // Parse Section 2 to extract the stream and PID
                    int stream = -1, pid = -1;
                    if (section2.Contains('['))
                    {
                        stream = int.Parse(section2.Substring(0, section2.IndexOf('['))); // Video stream
                        string PID = section2.Substring(section2.IndexOf('x') + 1, section2.IndexOf(']') - (section2.IndexOf('x') + 1));
                        pid = Convert.ToInt32(PID, 16); // get the video PID
                    }
                    else if (section2.Contains('('))
                        stream = int.Parse(section2.Substring(0, section2.IndexOf('('))); // Video stream
                    else
                        stream = int.Parse(section2.Substring(0)); // Video stream

                    // To handle only independent streams and ignore programs and  embedded streams, independent streams show up at the end, so if the streams id is repeated we overwrite the existing stream id and typically indepedent streams outnumber the embedded streams
                    if ((stream != mediaInfo.VideoInfo.Stream) && (mediaInfo.VideoInfo.Width > 0)) // If we already have a video stream (width) and this is a new stream (id's dont match), keep what we have (1st indepdendent video stream)
                        goto DoneParsingVideo; // We keep what we have, done here
                    else if (mediaInfo.VideoInfo.Width > 0)
                    {
                        _jobLog.WriteEntry(this, "Found existing video track with Stream ID " + stream.ToString() + ". Overwriting with new stream.", Log.LogEntryType.Warning);
                        ResetFFmpegVideoInfo(); // Clear it for the new video track
                    }

                    // Save it
                    mediaInfo.VideoInfo.VideoCodec = videoCodec; // Video Codec
                    mediaInfo.VideoInfo.Stream = stream; // Video stream
                    mediaInfo.VideoInfo.PID = pid; // Video PID

                    // Now parse the rest - throw an exception if the float numbers aren't really number don't try to mask it
                    string restString = section4.Substring(section4.IndexOf(',') + 1);
                    foreach (string subsection in restString.Split(','))
                    {
                        // TODO: Per https://github.com/x42/harvid/pull/2 one should use avg_frame_rate (FPS), however r_frame_rate is TBR (real frame rate) is more reliable (except when it reports the Field Rate which is 2 times the FPS)
                        // https://ffmpeg.org/faq.html#AVStream_002er_005fframe_005frate-is-wrong_002c-it-is-much-larger-than-the-frame-rate_002e
                        // r_frame_rate is NOT the average frame rate, it is the smallest frame rate that can accurately represent all timestamps. So no, it is not wrong if it is larger than the average! For example, if you have mixed 25 and 30 fps content, then r_frame_rate will be 150 (it is the least common multiple). If you are looking for the average frame rate, see AVStream.avg_frame_rate.
                        if (subsection.Contains("fps"))
                        {
                            if (!subsection.Contains("k")) // Sometime FFMPEG reports a faulty fps e.g. 30k
                                mediaInfo.VideoInfo.FPS = float.Parse(subsection.Substring(0, subsection.IndexOf("fps")).Trim(), CultureInfo.InvariantCulture); // fps always comes before tbr, if tbr is corrupted we end up using fps
                        }
                        else if (subsection.Contains("tbr")) // tbr wins over fps when possible (more accurate estimation, tbr always comes after fps)
                        {
                            if (!subsection.Contains("k")) // Sometime FFMPEG reports a faulty tbr e.g. 90k
                            {
                                float tbr = float.Parse(subsection.Substring(0, subsection.IndexOf("tbr")).Trim(), CultureInfo.InvariantCulture); // FPS - use tbr since this is the most accurate
                                if ((tbr < (FPS_MULTIPLIER_THRESHOLD * mediaInfo.VideoInfo.FPS)) || (mediaInfo.VideoInfo.FPS == 0)) // tbr reported sometimes is double the FPS (field rate instead of frame rate), in such cases ignore it
                                    mediaInfo.VideoInfo.FPS = tbr;
                            }
                        }
                        else if (subsection.Contains("kb/s"))
                        {
                            if (!subsection.Contains("max.") || (mediaInfo.VideoInfo.BitRate <= 0)) // Skip if it contains 'max' (variable bitrate) instead rely on the overall bitrate unless we don't have a bitrate
                                mediaInfo.VideoInfo.BitRate = (int)(float.Parse(subsection.Replace("max.", "").Substring(0, subsection.Replace("max.", "").IndexOf("kb/s")).Trim(), CultureInfo.InvariantCulture) * 1000); // Bit Rate (get rid of the max. in some strings)
                        }
                        else if (subsection.Contains("[")) // SAR/DAR with [] and resolutions
                        {
                            mediaInfo.VideoInfo.Width = int.Parse(subsection.Substring(0, subsection.IndexOf('x')).Trim()); // Width
                            mediaInfo.VideoInfo.Height = int.Parse(subsection.Substring(subsection.IndexOf('x') + 1, subsection.IndexOf('[') - (subsection.IndexOf('x') + 1)).Trim()); // Height

                            int sar = subsection.IndexOf("SAR");
                            int dar = subsection.IndexOf("DAR");
                            mediaInfo.VideoInfo.SAR = subsection.Substring(sar + 3, dar - (sar + 3)).Trim();
                            mediaInfo.VideoInfo.DAR = subsection.Substring(dar + 3, subsection.IndexOf(']') - (dar + 3)).Trim();
                        }
                        else if (subsection.Contains("SAR") && !subsection.Contains("[")) // SAR/DAR without []
                        {
                            // Sometimes the resolution lies with the SAR/DAR without []
                            if (subsection.Contains("x"))
                            {
                                mediaInfo.VideoInfo.Width = int.Parse(subsection.Substring(0, subsection.IndexOf('x')).Trim()); // Width
                                mediaInfo.VideoInfo.Height = int.Parse(subsection.Substring(subsection.IndexOf('x') + 1, subsection.IndexOf('S') - (subsection.IndexOf('x') + 1)).Trim()); // Height
                            }

                            // SAR/DAR inside [] take preference to those without []
                            if ((String.IsNullOrEmpty(mediaInfo.VideoInfo.SAR)) || (String.IsNullOrEmpty(mediaInfo.VideoInfo.DAR)))
                            {
                                int sar = subsection.IndexOf("SAR");
                                int dar = subsection.IndexOf("DAR");
                                mediaInfo.VideoInfo.SAR = subsection.Substring(sar + 3, dar - (sar + 3)).Trim();
                                mediaInfo.VideoInfo.DAR = subsection.Substring(dar + 3).Trim();
                            }
                        }
                        else if (subsection.Contains("x") && !subsection.Contains("SAR")) // standalone without SAR/DAR
                        {
                            mediaInfo.VideoInfo.Width = int.Parse(subsection.Substring(0, subsection.IndexOf('x')).Trim()); // Width
                            mediaInfo.VideoInfo.Height = int.Parse(subsection.Substring(subsection.IndexOf('x') + 1).Trim()); // Height
                        }
                        else if (subsection.Contains("tbn")) { } // capture all events else they will end up in format
                        else if (subsection.Contains("tbc")) { } // capture all events else they will end up in format
                        else if (subsection.Contains("q=")) { } // capture all events else they will end up in format
                        else
                        {
                            if (String.IsNullOrEmpty(mediaInfo.VideoInfo.Format)) // If it already contains something, we have parsed it, this is just junk or subsection
                            {
                                mediaInfo.VideoInfo.Format = subsection.Trim(); // Video format
                                if (mediaInfo.VideoInfo.Format.Contains("("))
                                    mediaInfo.VideoInfo.Format = mediaInfo.VideoInfo.Format.Substring(0, mediaInfo.VideoInfo.Format.IndexOf('(')).Trim(); // get rid of junk at end
                            }
                            else
                                _jobLog.WriteEntry(this, "FFMPEGMediaInfo Parse Warning: Unknown segment to parse ->" + subsection + "<-", Log.LogEntryType.Debug);
                        }
                    }

                    if ((mediaInfo.VideoInfo.Width == 0) || (mediaInfo.VideoInfo.Height == 0)) // Check for invalid video stream (some TS files have multiple video streams reported but they don't have any height or width)
                    {
                        _jobLog.WriteEntry(this, "FFMPEGMediaInfo Video Stream Warning: Invalid Video stream, skipping", Log.LogEntryType.Debug);
                        ResetFFmpegVideoInfo(); // Clear it
                    }
                    else
                    {
                        _jobLog.WriteEntry(this, "Video stream = " + mediaInfo.VideoInfo.Stream.ToString(), Log.LogEntryType.Debug);
                        _jobLog.WriteEntry(this, "Video codec = " + mediaInfo.VideoInfo.VideoCodec, Log.LogEntryType.Debug);
                        _jobLog.WriteEntry(this, "Video format = " + mediaInfo.VideoInfo.Format, Log.LogEntryType.Debug);
                        _jobLog.WriteEntry(this, "Video width = " + mediaInfo.VideoInfo.Width.ToString(), Log.LogEntryType.Debug);
                        _jobLog.WriteEntry(this, "Video height = " + mediaInfo.VideoInfo.Height.ToString(), Log.LogEntryType.Debug);
                        _jobLog.WriteEntry(this, "Video SAR = " + mediaInfo.VideoInfo.SAR, Log.LogEntryType.Debug);
                        _jobLog.WriteEntry(this, "Video DAR = " + mediaInfo.VideoInfo.DAR, Log.LogEntryType.Debug);
                        _jobLog.WriteEntry(this, "Video bit rate = " + mediaInfo.VideoInfo.BitRate.ToString(), Log.LogEntryType.Debug);
                        _jobLog.WriteEntry(this, "Video FPS = " + mediaInfo.VideoInfo.FPS.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                        _jobLog.WriteEntry(this, "Video PID = " + mediaInfo.VideoInfo.PID.ToString(), Log.LogEntryType.Debug);
                    }
                }

            DoneParsingVideo: ; // Done here with video

            }
            catch (Exception ex)
            {
                _jobLog.WriteEntry(this, "Error parsing Video. String : " + StdOut + "\r\n" + ex.ToString(), Log.LogEntryType.Error);
                parseError = true;
            }

            try
            {
                //Parse the Duration details
                if (StdOut.Contains("Duration:") && StdOut.Contains(",") && (mediaInfo.VideoInfo.Duration < 1))
                {
                    int StartPos = StdOut.IndexOf("Duration:") + "Duration:".Length;
                    int EndPos = StdOut.IndexOf(",", StartPos);
                    mediaInfo.VideoInfo.Duration = TimeStringToSecs(StdOut.Substring(StartPos, EndPos - StartPos));
                    _jobLog.WriteEntry(this, "Video duration = " + mediaInfo.VideoInfo.Duration.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

                    // Capture the bitrate also, if video shows it later it will be overwritten (if it's N/A let it throw an exception, we can't rely on these parameters)
                    StartPos = StdOut.IndexOf("bitrate:") + "bitrate:".Length;
                    EndPos = StdOut.IndexOf("kb/s", StartPos);
                    mediaInfo.VideoInfo.BitRate = (int)(float.Parse(StdOut.Substring(StartPos, EndPos - StartPos), CultureInfo.InvariantCulture) * 1000);
                    _jobLog.WriteEntry(this, "Overall bit rate = " + mediaInfo.VideoInfo.BitRate.ToString(), Log.LogEntryType.Debug);
                }
            }
            catch (Exception ex)
            {
                _jobLog.WriteEntry(this, "Error parsing Duration. String:" + StdOut + "\r\n" + ex.ToString(), Log.LogEntryType.Error);
                parseError = true;
            }

            try
            {
                // Parse the Audio Stream details (Multiple audio tracks per file)
                if (StdOut.Contains("Stream #0:") && StdOut.Contains(",") && StdOut.Contains("Audio"))
                {
                    int index;
                    bool newAudioStream = false; // Is this a new stream or overwriting an existing one

                    string[] parseChunk = StdOut.Split(':');

                    int stream = -1, pid = -1;
                    if (parseChunk[1].Contains('[')) // not all files contain the PID
                    {
                        stream = int.Parse(parseChunk[1].Substring(0, parseChunk[1].IndexOf('['))); // Audio Stream
                        string PID = parseChunk[1].Substring(parseChunk[1].IndexOf('x') + 1, parseChunk[1].IndexOf(']') - (parseChunk[1].IndexOf('x') + 1));
                        pid = Convert.ToInt32(PID, 16); // get the stream PID
                    }
                    else if (parseChunk[1].Contains('(')) // some return only lang without PID
                        stream = int.Parse(parseChunk[1].Substring(0, parseChunk[1].IndexOf('('))); // Audio Stream
                    else
                        stream = int.Parse(parseChunk[1].Substring(0)); // Audio Stream

                    // To handle only independent streams and ignore programs and  embedded streams, independent streams show up at the end, so if the streams id is repeated we overwrite the existing stream id and typically indepedent streams outnumber the embedded streams
                    if ((audioTracks > 0) && (Array.FindIndex(mediaInfo.AudioInfo.ToArray(), (s) => s.Stream == stream) > -1)) // If we already have a audio stream with the same stream Id then overwrite what we have (indepdendent audio stream come at the end) otherwise create a new audio track
                    {
                        _jobLog.WriteEntry(this, "Found existing audio track with Stream ID " + stream.ToString() + ". Overwriting with new stream.", Log.LogEntryType.Warning);
                        index = Array.FindIndex(mediaInfo.AudioInfo.ToArray(), s => s.Stream == stream); // Overwrite an existing audio track with same stream id
                    }
                    else // It's a new track, allocate an audio track
                    {
//                        Array.Resize(ref mediaInfo.AudioInfo, audioTracks + 1); // Increase the array size
                        mediaInfo.AudioInfo.Insert(audioTracks, new MediaInfo.Audio());
                        index = audioTracks;
                        newAudioStream = true;
                    }

                    // Reset/Create a new Audio object for the stream
                    mediaInfo.AudioInfo[index] = new MediaInfo.Audio();

                    // Save it
                    mediaInfo.AudioInfo[index].Stream = stream; // Stream id
                    mediaInfo.AudioInfo[index].PID = pid; // PID

                    if (StdOut.ToLower().Contains("impaired"))
                    {
                        mediaInfo.AudioInfo[index].Impaired = true; // This is a visual / hearing impaired track
                        impariedAudioTracks++; // keep track of this
                        _jobLog.WriteEntry(this, "Impaired audio track reported, likely empty channel with no audio", Log.LogEntryType.Warning);
                    }

                    if (parseChunk[1].Contains('(') && parseChunk[1].Contains(')')) // Not all outputs contains the lanuage
                        mediaInfo.AudioInfo[index].Language = parseChunk[1].Substring(parseChunk[1].IndexOf('(') + 1, parseChunk[1].IndexOf(')') - (parseChunk[1].IndexOf('(') + 1)); // language code

                    string[] parseAudio = parseChunk[3].Split(','); // Audio codec
                    if (parseAudio[0].Contains('('))
                        mediaInfo.AudioInfo[index].AudioCodec = parseAudio[0].Substring(0, parseAudio[0].IndexOf('(')).ToLower().Trim();
                    else
                        mediaInfo.AudioInfo[index].AudioCodec = parseAudio[0].ToLower().Trim();

                    // Parse the rest
                    string parseRest = parseChunk[3].Substring(parseChunk[3].IndexOf(',') + 1);

                    foreach (string section in parseRest.Split(','))
                    {

                        if (section.Contains("Hz")) // check for sampling rate
                            mediaInfo.AudioInfo[index].Rate = (int)(float.Parse(section.Substring(0, section.IndexOf("Hz")).Trim(), CultureInfo.InvariantCulture)); // Sample rate

                        else if (section.Contains("kb/s")) // check for bitrate
                            mediaInfo.AudioInfo[index].BitRate = (int)(float.Parse(section.Substring(0, section.IndexOf("kb/s")).Trim(), CultureInfo.InvariantCulture) * 1000); // Bit Rate

                        else if (CheckFmts(section.Trim()) != -1) // check for bits per sample
                            mediaInfo.AudioInfo[index].SamplingBits = CheckFmts(section.Trim()); // Bits per sample

                        else // We are left with audio channels
                        {
                            // TODO: Need to add support for more audio channel types, where to get the list??
                            // Audio channels
                            if (CheckChannels(section.Trim().ToLower()) != -1)
                            {
                                // We need to keep track of 0 channel tracks for some encoders
                                if (CheckChannels(section.Trim().ToLower()) == 0)
                                {
                                    _jobLog.WriteEntry(this, "0 channel audio track reported", Log.LogEntryType.Warning);
                                    zeroChannelTracks++; // keep track of this
                                }

                                mediaInfo.AudioInfo[index].Channels = CheckChannels(section.Trim().ToLower()); // Store the channel information
                            }
                            else
                            {
                                parseError = true;
                                _jobLog.WriteEntry(this, "Unrecognized Audio Channels : " + StdOut, Log.LogEntryType.Warning);
                            }
                        }
                    }

                    _jobLog.WriteEntry(this, "Audio track = " + index.ToString(), Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Audio stream = " + mediaInfo.AudioInfo[index].Stream.ToString(), Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Audio language = " + mediaInfo.AudioInfo[index].Language, Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Audio codec = " + mediaInfo.AudioInfo[index].AudioCodec, Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Audio sampling rate = " + mediaInfo.AudioInfo[index].Rate.ToString(), Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Audio channels = " + mediaInfo.AudioInfo[index].Channels.ToString(), Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Audio bits per sample = " + mediaInfo.AudioInfo[index].SamplingBits.ToString(), Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Audio bit rate = " + mediaInfo.AudioInfo[index].BitRate.ToString(), Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Audio PID = " + mediaInfo.AudioInfo[index].PID.ToString(), Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Audio impaired = " + mediaInfo.AudioInfo[index].Impaired.ToString(), Log.LogEntryType.Debug);

                    if (newAudioStream) // If it's a new audio stream and not being overwritten
                        audioTracks++; // We are done adding this track
                }
            }
            catch (Exception ex)
            {
                _jobLog.WriteEntry(this, "Error parsing Audio. String:" + StdOut + "\r\n" + ex.ToString(), Log.LogEntryType.Error);
                parseError = true;
            }

            try
            {
                // Parse the Subtitle Stream details (Multiple audio tracks per file)
                if (StdOut.Contains("Stream #0:") && StdOut.Contains("Subtitle"))
                {
   //                 Array.Resize(ref mediaInfo.SubtitleInfo, subtitleTracks + 1); // Increase the array size
          mediaInfo.SubtitleInfo.Insert(subtitleTracks, new MediaInfo.Subtitle());
//                    mediaInfo.SubtitleInfo[subtitleTracks] = new MediaInfo.Subtitle(); // Create a new Subtitle object for each stream we find
                    string[] parseChunk = StdOut.Split(':');

                    if (parseChunk[1].Contains('[')) // not all contain PID
                    {
                        mediaInfo.SubtitleInfo[subtitleTracks].Stream = int.Parse(parseChunk[1].Substring(0, parseChunk[1].IndexOf('['))); // Subtitle Stream
                        string PID = parseChunk[1].Substring(parseChunk[1].IndexOf('x') + 1, parseChunk[1].IndexOf(']') - (parseChunk[1].IndexOf('x') + 1));
                        mediaInfo.SubtitleInfo[subtitleTracks].PID = Convert.ToInt32(PID, 16); // get the stream PID
                    }
                    else if (parseChunk[1].Contains('(')) // some return only lang without PID
                        mediaInfo.SubtitleInfo[subtitleTracks].Stream = int.Parse(parseChunk[1].Substring(0, parseChunk[1].IndexOf('('))); // Subtitle Stream
                    else
                        mediaInfo.SubtitleInfo[subtitleTracks].Stream = int.Parse(parseChunk[1].Substring(0)); // Subtitle Stream

                    if (parseChunk[1].Contains('(') && parseChunk[1].Contains(')')) // Not all outputs contains the lanuage
                        mediaInfo.SubtitleInfo[subtitleTracks].Language = parseChunk[1].Substring(parseChunk[1].IndexOf('(') + 1, 3); // 3 char language code

                    mediaInfo.SubtitleInfo[subtitleTracks].Name = parseChunk[3].Trim(); // Name of the subtitle

                    _jobLog.WriteEntry(this, "Subtitle track = " + subtitleTracks.ToString(), Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Subtitle stream = " + mediaInfo.SubtitleInfo[subtitleTracks].Stream.ToString(), Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Subtitle language = " + mediaInfo.SubtitleInfo[subtitleTracks].Language, Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Subtitle name = " + mediaInfo.SubtitleInfo[subtitleTracks].Name, Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Subtitle PID = " + mediaInfo.SubtitleInfo[subtitleTracks].PID.ToString(), Log.LogEntryType.Debug);

                    subtitleTracks++; // We are done with this track
                }
            }
            catch (Exception ex)
            {
                _jobLog.WriteEntry(this, "Error parsing Subtitles. String:" + StdOut + "\r\n" + ex.ToString(), Log.LogEntryType.Error);
                parseError = true;
            }

            if (StdOut.Contains("At least one output file must be specified"))
                parseCompleted = true; // FFMPEG has finished processing the file
        }

        /// <summary>
        /// Checks if the output handler has completed and ready for parsing when using FFProbe and sets the parseCompleted flag
        /// </summary>
        /// <param name="StdOut">Output to parse</param>
        private void CheckFFProbeOutputComplete(string StdOut)
        {
            if (String.IsNullOrWhiteSpace(StdOut)) // If the string is empty, then the output hasn't been processed yet
                return;

            try
            {
                FFProbeResults ffprobeResults = JsonConvert.DeserializeObject<FFProbeResults>(StdOut);
                parseCompleted = true; // FFMPEG has finished processing the file
            }
            catch { }
        }

        /// <summary>
        /// Parses the output from FFprobe to extract media information
        /// </summary>
        /// <param name="StdOut">Output to parse</param>
        private void ParseFFPROBEMediaInformation(string StdOut)
        {
            try
            {
                FFProbeResults ffprobeResults = JsonConvert.DeserializeObject<FFProbeResults>(StdOut);

                // Process the information
                // TODO: For now we only process independent streams and ignore programs (don't use -show_programs) and streams embedded within programs. This is because streams embedded within programs do not have unique stream ID's and PID's (they repeat within each program). How does MCEBuddy handle mpegts_service_id (programs)?
                foreach (Stream stream in ffprobeResults.streams)
                {
                    // Skip invalid streams
                    if (stream.codec_type == null)
                        continue;

                    // Process the video stream (skip MJPEG streams) and use only the first Video stream with a width (ignore subsequent ones)
                    if ((stream.codec_type.Trim().ToLower() == "video") && (stream.codec_name.Trim().ToLower() != "mjpeg") && (mediaInfo.VideoInfo.Width == 0))
                    {
                         if (!int.TryParse(stream.bit_rate, out mediaInfo.VideoInfo.BitRate)) // Video bitrate, sometimes it's a N/A
                             int.TryParse(ffprobeResults.format.bit_rate, out mediaInfo.VideoInfo.BitRate); // Use overall bitrate instead
                        mediaInfo.VideoInfo.DAR = stream.display_aspect_ratio; // DAR
                        mediaInfo.VideoInfo.Format = stream.pix_fmt; // Color space pixel format
                        // TODO: Check https://ffmpeg.org/pipermail/ffmpeg-user/2014-June/022196.html and per https://github.com/x42/harvid/pull/2 one should use avg_frame_rate (FPS), however r_frame_rate is TBR (real frame rate) is more reliable (except when it reports the Field Rate which is 2 times the FPS)
                        // https://ffmpeg.org/faq.html#AVStream_002er_005fframe_005frate-is-wrong_002c-it-is-much-larger-than-the-frame-rate_002e
                        // r_frame_rate is NOT the average frame rate, it is the smallest frame rate that can accurately represent all timestamps. So no, it is not wrong if it is larger than the average! For example, if you have mixed 25 and 30 fps content, then r_frame_rate will be 150 (it is the least common multiple). If you are looking for the average frame rate, see AVStream.avg_frame_rate.
                        double? fps = MathLib.EvaluateBasicExpression(stream.avg_frame_rate);
                        mediaInfo.VideoInfo.FPS = ((fps == null) || double.IsNaN(fps.Value) || double.IsInfinity(fps.Value) ? 0 : (float)Math.Round((float)fps.Value, 3)); // LibAV says use average frame rate, round off to 3 decimal points (valid are 23.976, 29.97, 59.94)
                        double? rRate = MathLib.EvaluateBasicExpression(stream.r_frame_rate);
                        float tbr = ((rRate == null) || double.IsNaN(rRate.Value) || double.IsInfinity(rRate.Value ) ? 0 : (float)Math.Round((float)rRate.Value, 3)); // r_frame_rate is usually more accurate unless it reports Field Rate (2 times the FPS) in which case ignore it. Round off to 3 decimal points (valid are 23.976, 29.97, 59.94)
                        if ((tbr < (FPS_MULTIPLIER_THRESHOLD * mediaInfo.VideoInfo.FPS)) || (mediaInfo.VideoInfo.FPS == 0)) // tbr reported sometimes is double the FPS (field rate instead of frame rate), in such cases ignore it
                            mediaInfo.VideoInfo.FPS = tbr;
                        if (mediaInfo.VideoInfo.FPS > MAX_FPS)
                        {
                            _jobLog.WriteEntry(this, "Invalid FPS " + mediaInfo.VideoInfo.FPS.ToString(CultureInfo.InvariantCulture) + ", resetting to 0", Log.LogEntryType.Warning);
                            mediaInfo.VideoInfo.FPS = 0;
                        }
                        mediaInfo.VideoInfo.Height = (stream.height == null ? 0 : (int)stream.height); // Height
                        mediaInfo.VideoInfo.PID = (String.IsNullOrWhiteSpace(stream.id) ? -1 : Convert.ToInt32(stream.id.Replace("0x", ""), 16)); // PID
                        mediaInfo.VideoInfo.SAR = stream.sample_aspect_ratio; // SAR
                        mediaInfo.VideoInfo.Stream = stream.index; // Stream index
                        mediaInfo.VideoInfo.VideoCodec = stream.codec_name; // Video codec name
                        mediaInfo.VideoInfo.Width = (stream.width == null ? 0 : (int)stream.width); // Width

                        if ((mediaInfo.VideoInfo.Width == 0) || (mediaInfo.VideoInfo.Height == 0)) // Check for invalid video stream (some TS files have multiple video streams reported but they don't have any height or width)
                        {
                            _jobLog.WriteEntry(this, "FFMPEGMediaInfo Video Stream Warning: Invalid Video stream, skipping", Log.LogEntryType.Debug);
                            ResetFFmpegVideoInfo(); // Clear it
                        }
                    }
                    else if (stream.codec_type.Trim().ToLower() == "audio") // Parse the Audio Track
                    {
                        mediaInfo.AudioInfo.Insert(audioTracks, new MediaInfo.Audio());
                        //Array.Resize(ref mediaInfo.AudioInfo, audioTracks + 1); // Increase the array size
                        //mediaInfo.AudioInfo[audioTracks] = new MediaInfo.Audio(); // Create a new Audio object for each stream we find

                        // Check for imparired tracks
                        if (stream.disposition != null)
                        {
                            if (stream.disposition.hearing_impaired != 0 || stream.disposition.visual_impaired != 0)
                            {
                                mediaInfo.AudioInfo[audioTracks].Impaired = true; // This is a visual / hearing impaired track
                                impariedAudioTracks++; // keep track of this
                                _jobLog.WriteEntry(this, "Impaired audio track reported, likely empty channel with no audio", Log.LogEntryType.Warning);
                            }
                        }

                        mediaInfo.AudioInfo[audioTracks].AudioCodec = stream.codec_name; // Audio codec name
                        int.TryParse(stream.bit_rate, out mediaInfo.AudioInfo[audioTracks].BitRate); // Audio Bitrate
                        
                        // We need to keep track of 0 channel tracks for some encoders
                        if (stream.channels == null || stream.channels == 0)
                        {
                            _jobLog.WriteEntry(this, "0 channel audio track reported", Log.LogEntryType.Warning);
                            zeroChannelTracks++; // keep track of this
                        }
                        mediaInfo.AudioInfo[audioTracks].Channels = (stream.channels == null ? 0 : (int)stream.channels); // Store the channel information

                        mediaInfo.AudioInfo[audioTracks].Language = (stream.tags == null ? "" : stream.tags.language); // Language
                        mediaInfo.AudioInfo[audioTracks].PID = (String.IsNullOrWhiteSpace(stream.id) ? -1 : Convert.ToInt32(stream.id.Replace("0x", ""), 16)); // PID
                        int.TryParse(stream.sample_rate, out mediaInfo.AudioInfo[audioTracks].Rate); // Hz
                        mediaInfo.AudioInfo[audioTracks].SamplingBits = (stream.bits_per_sample == null ? -1 : (int)stream.bits_per_sample); // Bits per sample
                        mediaInfo.AudioInfo[audioTracks].Stream = stream.index; // Stream index

                        audioTracks++; // We are done with this track
                    }
                    else if (stream.codec_type.Trim().ToLower() == "subtitle") // Parse the Subtitle Track
                    {
//                        Array.Resize(ref mediaInfo.SubtitleInfo, subtitleTracks + 1); // Increase the array size
                          mediaInfo.SubtitleInfo.Insert(subtitleTracks, new MediaInfo.Subtitle());
//                        mediaInfo.SubtitleInfo[subtitleTracks] = new MediaInfo.Subtitle(); // Create a new Subtitle object for each stream we find

                        mediaInfo.SubtitleInfo[subtitleTracks].Language = (stream.tags == null ? "" : stream.tags.language);
                        // TODO: FFProbe doesn't seem to report the codec name (e.g eia_608) which is done by ffmpeg. How do we translate the Codec ID from LibAV to a name?
                        mediaInfo.SubtitleInfo[subtitleTracks].Name = stream.codec_name; // Subtitle Codec name
                        mediaInfo.SubtitleInfo[subtitleTracks].PID = (String.IsNullOrWhiteSpace(stream.id) ? -1 : Convert.ToInt32(stream.id.Replace("0x", ""), 16)); // PId
                        mediaInfo.SubtitleInfo[subtitleTracks].Stream = stream.index; // Stream index

                        subtitleTracks++; // We are done with this track
                    }
                }

                // Get the duration (could be either audio only, video only or both streams, so we do it outside)
                float.TryParse(ffprobeResults.format.duration, NumberStyles.Any, CultureInfo.InvariantCulture, out mediaInfo.VideoInfo.Duration); // Overall duration
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "ERROR Processing FFProbe Output.\n" + e.ToString(), Log.LogEntryType.Error);
                parseError = true;
            }

            _jobLog.WriteEntry(this, mediaInfo.ToString(), Log.LogEntryType.Debug);
        }

        private void ParseFFMPEGInterlaceInformation(string StdOut)
        {
            try
            {
                /*
                 * frame=  500 fps=120 q=0.0 Lsize=  303750kB time=00:00:20.00 bitrate=124416.0kbits/s dup=185 drop=157
                 * video:303750kB audio:0kB subtitle:0 global headers:0kB muxing overhead 0.000000%
                 * [Parsed_idet_0 @ 0337aa60] Single frame detection: TFF:473 BFF:0 Progressive:0 Undetermined:0
                 * [Parsed_idet_0 @ 0337aa60] Multi frame detection: TFF:473 BFF:0 Progressive:0 Undetermined:0
                 */
                if (StdOut.Contains("Multi frame detection:"))
                {
                    ParseIDETResults(StdOut, multiFrameIDETResults);
                    _jobLog.WriteEntry(this, "Parsed Multi frame Interlace Detection. Results ->\r\n" + multiFrameIDETResults.ToString(), Log.LogEntryType.Debug);
                    parseCompleted = true; // This is the last one to show up, now we are all done
                }

                if (StdOut.Contains("Single frame detection:"))
                {
                    ParseIDETResults(StdOut, singleFrameIDETResults);
                    _jobLog.WriteEntry(this, "Parsed Single frame Interlace Detection. Results ->\r\n" + singleFrameIDETResults.ToString(), Log.LogEntryType.Debug);
                }

                if ((StdOut.Contains("global headers")) && (StdOut.Contains("muxing overhead")))
                {
                    parseError = false; // By default on this more ParseError is true, now we are good, the file has been processed successfully
                    parseCompleted = true; // We are done here
                }
            }
            catch (Exception ex)
            {
                _jobLog.WriteEntry(this, "Error parsing Interlace Detection Mode. String:" + StdOut + "\r\n" + ex.ToString(), Log.LogEntryType.Error);
            }
        }

        protected override void OutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs ConsoleOutput)
        {
            try
            {
                string StdOut;

                base.OutputHandler(sendingProcess, ConsoleOutput); // Log the output
                
                if (ConsoleOutput.Data == null)
                    return;

                if (checkVersionMode) // nothing to do just checking version
                    return;

                if (!String.IsNullOrEmpty(ConsoleOutput.Data))
                {
                    StdOut = ConsoleOutput.Data;

                    // Check IDET mode first since ffprobe is a const
                    if (idetMode) // Special mode, we are running the idet video filter to detect the interlace mode, in this more ParseError is true by default unless we find what we want
                        ParseFFMPEGInterlaceInformation(StdOut);
                    else if (useFFProbe) // Check if we are using FFProbe mode
                        ffProbeOutput += StdOut; // Store it, we will parse it once the console output is complete to avoid errors related to partial lines
                    else // FFMpeg mode
                        ParseFFMPEGMediaInformation(StdOut);
                }
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "ERROR Processing Console Output.\n" + e.ToString(), Log.LogEntryType.Error);
                parseError = true;
            }
        }

        private void ParseIDETResults(string StdOut, InterlaceDetection idetResults)
        {
            // FORMAT:
            // [Parsed_idet_0 @ 03360920] Single frame detection: TFF:0 BFF:0 Progressive:246 Undetermined:1210
            // [Parsed_idet_0 @ 03360920] Multi frame detection: TFF:0 BFF:0 Progressive:1454 Undetermined:2

            int StartPos, EndPos;

            // Get TFF
            StartPos = StdOut.IndexOf("TFF:") + "TFF:".Length;
            EndPos = StdOut.IndexOf("BFF:");
            long.TryParse(StdOut.Substring(StartPos, EndPos - StartPos), out idetResults.TFF);

            // Get BFF
            StartPos = StdOut.IndexOf("BFF:") + "BFF:".Length;
            EndPos = StdOut.IndexOf("Progressive:");
            long.TryParse(StdOut.Substring(StartPos, EndPos - StartPos), out idetResults.BFF);

            // Get Progressive
            StartPos = StdOut.IndexOf("Progressive:") + "Progressive:".Length;
            EndPos = StdOut.IndexOf("Undetermined:");
            long.TryParse(StdOut.Substring(StartPos, EndPos - StartPos), out idetResults.Progressive);

            // Get Progressive
            StartPos = StdOut.IndexOf("Undetermined:") + "Undetermined:".Length;
            long.TryParse(StdOut.Substring(StartPos), out idetResults.Undetermined);
        }

        /// <summary>
        /// Dumps all media information about file into the log using either FFProbe or FFMPEG, does NOT analyze, parse or store the information
        /// </summary>
        /// <param name="fileName">Filename to dump information about</param>
        public static void DumpFileInformation(string fileName, JobStatus jobStatus, Log jobLog)
        {

            jobLog.WriteEntry("Dumping complete information about the file " + fileName, Log.LogEntryType.Debug);

            // Check if FFProbe exists, if not then fallback to FFMpeg
            string applicationPath = FFPROBE_APP_PATH;
            if (!File.Exists(Path.Combine(GlobalDefs.AppPath, FFPROBE_APP_PATH)))
            {
                jobLog.WriteEntry("FFProbe not found, switching to FFMpeg", Log.LogEntryType.Warning);
                applicationPath = FFMPEG_APP_PATH;
            }

            // -probesize 100M -analyzeduration 300M are important to identify broken audio streams in some files
            string parameters = " -probesize 100M -analyzeduration 300M -i " + Util.FilePaths.FixSpaces(fileName); // FFMPEG create the format for run the command

            Base mediaInfo = new Base(parameters, applicationPath, jobStatus, jobLog);
            mediaInfo.Run(); // Dump it
        }

        public override void Run()
        {
            if (parseCompleted) // Incase someone accidentally called Run
                return;

            float _percentageComplete = _jobStatus.PercentageComplete; // Save it since this function does not really do any work, just checks mediainfo
            base.Run();

            if (useFFProbe && !checkVersionMode) // Check if we are using FFProbe mode
                CheckFFProbeOutputComplete(ffProbeOutput); // Check if the output if ready to be parsed and completed (sets the parseCompleted flag)

            int i = 0;
            while (_success && !checkVersionMode && !parseCompleted && (i++ < 20) && !_jobStatus.Cancelled) // Check if the initialized failed
            {
                Thread.Sleep(500); // Wait for messages to be flushed and parsing completed or until 10 seconds (incase of a failure to read) or job cancellation
                if (useFFProbe && !checkVersionMode) // Check if we are using FFProbe mode
                    CheckFFProbeOutputComplete(ffProbeOutput); // Check if the output if ready to be parsed and completed (sets the parseCompleted flag)
            }

            // Process the FFProbe output
            if (!_jobStatus.Cancelled)
                if (useFFProbe && !checkVersionMode) // Check if we are using FFProbe mode
                    ParseFFPROBEMediaInformation(ffProbeOutput);

            _jobStatus.PercentageComplete = _percentageComplete; // restore it
        }
    }
}
