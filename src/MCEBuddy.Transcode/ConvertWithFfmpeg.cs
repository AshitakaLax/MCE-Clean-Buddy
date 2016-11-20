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
using MCEBuddy.CommercialScan;

namespace MCEBuddy.Transcode
{
    public class ConvertWithFfmpeg : ConvertBase
    {
        private const int DEFAULT_QUALITY_VALUE = 3;
        private const int DEFAULT_BIT_RATE = 1500000;
        private const double DRC = 0.8; // 80% Dynamic Range Compression (DRC)
        private string passLog;
        private string _srtFile = ""; // Subtitle file

        public ConvertWithFfmpeg(ConversionJobOptions conversionOptions, string tool, VideoInfo videoFile, JobStatus jobStatus, Log jobLog, Scanner commercialScan, string srtFile)
            : base(conversionOptions, tool, videoFile, jobStatus, jobLog, commercialScan)
        {
            passLog = Path.Combine(_workingPath, "MCEBuddy2Pass.log"); // Name of passlog file
            //Check if MEncoder EDL Removal has been disabled at conversion time
            Ini ini = new Ini(GlobalDefs.ProfileFile);
            bool subtitleBurn = ini.ReadBoolean(conversionOptions.profile, tool + "-SubtitleBurn", false);
            _jobLog.WriteEntry(this, "FFMPEG subtitle burn (" + tool + "-SubtitleBurn) : " + subtitleBurn.ToString(), Log.LogEntryType.Debug);
            if (subtitleBurn)
                _srtFile = srtFile; // Save the SRT file info otherwise skip it
        }

        protected override void FinalSanityCheck()
        {
            // Check if we don't have a video stream and remove the video mapping
            if (_videoFile.VideoStream == -1) // Check if we have a video stream
            {
                _jobLog.WriteEntry(this, "No Video stream detected, removing support for video stream", Log.LogEntryType.Warning);
                _cmdParams = _cmdParams.Replace("-map 0:v", ""); // Replace the map video command with nothing
            }
            else
                _cmdParams = _cmdParams.Replace("-map 0:v", "-map 0:" + _videoFile.VideoStream.ToString(CultureInfo.InvariantCulture)); // Replace the map video command with the actual video stream (some TS files contain multiple video streams)

            // Audio stream can be -1 if no language is selected do we don't check for it.
            if (_videoFile.FFMPEGStreamInfo.AudioTracks < 1)
            {
                _jobLog.WriteEntry(this, "No Audio stream detected, removing support for audio stream", Log.LogEntryType.Warning);
                _cmdParams = _cmdParams.Replace("-map 0:a", ""); // Replace the map audio command with nothing
            }

            // Check if we need to burn in subtitles, this is done in the VERY end because this filter cannot be replaced since it contains : that will break the MCEBuddy video manipulator functions (only works once)
            // Special characters \ : ' need to escaped for the filter (in that order)
            // Then you need to re-escapte the / ' characters for ffmpeg to parse it (in that order)
            // Refer to FFMPEG Ticket #3334, order if VERY important, first escape the \, then escape the :, then escape the ', then reescape the \ and finally reescape '
            /* Comment from Stefano Sabatini from ffmpeg users forum on "escaping hell" in ffmpeg filters
                the SRT filepath is

                D:\MCEBuddy\MCEBuddy 2.x\MCEBuddy.ServiceCMD\bin\x86\Debug\working0\HD Small'.srt

                So this string contains : which is special according to the filter
                description syntax, and the \ and ' special escaping characters.

                So, first level escaping:
                D\:\\MCEBuddy\\MCEBuddy 2.x\\MCEBuddy.ServiceCMD\\bin\\x86\\Debug\\working0\\HD Small\'.srt

                Now you embed the filter description string in the filtergraph
                description, so you add another escaping level, and you need to escape
                the special \ and ' characters. One way of doing this is as:

                subtitles=D\\:\\\\MCEBuddy\\\\MCEBuddy 2.x\\\\MCEBuddy.ServiceCMD\\\\bin\\\\x86\\\\Debug\\\\working0\\\\HD Small\\\'.srt

                Alternatively you use quoting:
                subtitles='D\:\\MCEBuddy\\MCEBuddy 2.x\\MCEBuddy.ServiceCMD\\bin\\x86\\Debug\\working0\\HD Small'\'.srt
             */

            if (!String.IsNullOrWhiteSpace(_srtFile) && File.Exists(_srtFile) && (ParameterValue("-vcodec") != "copy")) // does not work with copy codec
            {
                ParameterReplaceOrInsertVideoFilter("subtitles", "=" + FilePaths.FixSpaces(_srtFile.Replace(@"\", @"\\").Replace(@":", @"\:").Replace(@"'", @"\'").Replace(@"\", @"\\").Replace(@"'", @"\'")));
                _subtitleBurned = true;
            }
        }

        protected override bool IsPresetVideoWidth()
        {
            // Get the profile conversion width
            string scale = ParameterSubValue("-vf", "scale");
            if (!String.IsNullOrWhiteSpace(scale))
                return true;
            else
                return false;
        }

        protected override bool ConstantVideoQuality
        {
            get { return ((ParameterValue("-qscale") != "") || (ParameterValue("-crf") != "")
                            || (ParameterSubValue("-x265-params", "crf") != "") // TODO: Remove this x265 specific section once ffmpeg starts supporting generic quality parameters for x265
                            ); }
        }

        protected override void SetVideoOutputFrameRate()
        {
            if (String.IsNullOrWhiteSpace(_fps))
                return; // Nothing to do here

            // Do not override any set framerates
            if (ParameterValue("-r") == "")
                ParameterValueReplaceOrInsert("-r", _fps);
            else
                _jobLog.WriteEntry(this, "Found FPS in profile -> " + ParameterValue("-r") + ", skipping setting framerate.", Log.LogEntryType.Warning);
        }

        protected override void SetVideoDeInterlacing()
        {
            // TODO: Need to complete auto deinterlacing
            if (_autoDeInterlacing)
            {
                if (ParameterValue("-vcodec") == "copy") // For copy video stream don't set deinterlacing paramters, it can break the conversion
                {
                    _jobLog.WriteEntry(this, "Video Copy codec detected, skipping autoDeinterlacing", Log.LogEntryType.Warning);
                    return;
                }

                if (_videoFile.VideoScanType == ScanType.Unknown) // We don't know the type of interlacing, use default
                {
                    _jobLog.WriteEntry(this, "FFMpeg Unknown video scan, using profile default options", Log.LogEntryType.Warning);
                    return;
                }

                // Delete all not required and then add those you want
                ParameterDeleteVideoFilter("yadif"); // Deinterlacing not required
                ParameterDeleteVideoFilter("fieldmatch"); // Inverse Inverse Telecine not required
                ParameterDeleteVideoFilter("decimate"); // Part of Inverse Telecine
                ParameterDeleteVideoFilter("pullup"); // Redundant Inverse Telecine filter

                if (_videoFile.VideoScanType == ScanType.Progressive) // If we are progressive, we don't need any interlacing/telecine filters
                {
                    _jobLog.WriteEntry(this, "FFMpeg detected progressive video scan, no filters required", Log.LogEntryType.Debug);
                }
                else if (_videoFile.VideoScanType == ScanType.Interlaced)
                {
                    _jobLog.WriteEntry(this, "FFMpeg detected interlaced (or mbaff/paff) video scan, adding de-interlacing filters", Log.LogEntryType.Debug);
                    ParameterReplaceOrInsertVideoFilter("yadif", "=0:-1:1"); // Deinterlacing with auto interlace frame detect only for interlaced frames (mbaff/paff)
                }
                else if (_videoFile.VideoScanType == ScanType.Telecine)
                {
                    _jobLog.WriteEntry(this, "FFMpeg detected telecined video scan, adding inverse telecining filters", Log.LogEntryType.Debug);
                    ParameterReplaceOrInsertVideoFilter("fieldmatch", "=auto"); // Inverse telecine (fieldmatch is much better than pullup)
                    ParameterReplaceOrInsertVideoFilter("decimate", ""); // Drop field not required
                }
            }
        }

        protected override void SetVideoTrim()
        {
            // Set the start trim
            if (_startTrim != 0)
                ParameterValueReplaceOrInsert("-ss", _startTrim.ToString(CultureInfo.InvariantCulture));

            // Set the end trim (calculate from reducing from video length)
            if (_endTrim != 0)
            {
                // FFMPEG can specify duration of encoding, i.e. encoding_duration = stopTime - startTime
                // startTime = startTrim, stopTime = video_duration - endTrim
                int encDuration = (((int)_videoFile.Duration) - _endTrim) - (_startTrim); // by default _startTrim is 0
                ParameterValueReplaceOrInsert("-t", encDuration.ToString(CultureInfo.InvariantCulture));
            }
        }

        protected override void SetVideoBitrateAndQuality()
        {
            if (ConstantVideoQuality) // We don't need to adjust for resolution changes here since constant quality takes care of that (only user specified changes in quality)
            {
                int quality;
                string qualityStr;
                string qualityParam;

                // Check if crf or qscale is used for constant quality (libx264 uses crf instead of qscale)
                if ((qualityStr = ParameterValue(qualityParam = "-qscale")) == "") // Check if qscale exists
                    qualityStr = ParameterValue(qualityParam = "-crf"); // otherwise assume it's crf

                // TODO: Remove this x265 specific section once ffmpeg starts supporting generic quality parameters for x265
                if (String.IsNullOrWhiteSpace(qualityStr) && (ParameterValue("-vcodec") == "libx265"))
                    qualityStr = ParameterSubValue(qualityParam = "-x265-params", "crf");
                
                if (String.IsNullOrWhiteSpace(qualityStr))
                    return;

                if (!int.TryParse(qualityStr, out quality))
                {
                    quality = DEFAULT_QUALITY_VALUE;
                    _jobLog.WriteEntry(this, "FFMpeg invalid quality in profile, using default quality " + DEFAULT_QUALITY_VALUE.ToString(), Log.LogEntryType.Warning);
                }
                
                if (qualityParam == "-qscale") // range is 1 to 31
                    quality = ConstantQualityValue(31, 1, quality);
                else if (qualityParam == "-crf") // range is 0 to 51
                    quality = ConstantQualityValue(51, 0, quality);
                // TODO: Remove this x265 specific section once ffmpeg starts supporting generic quality parameters for x265
                else if (qualityParam == "-x265-params")
                    quality = ConstantQualityValue(51, 0, quality); // range is 0 to 51
                else
                    _jobLog.WriteEntry(this, "FFMpeg invalid quality parameter in profile, using profile quality " + quality.ToString(), Log.LogEntryType.Warning);

                // TODO: Remove this x265 specific section once ffmpeg starts supporting generic quality parameters for x265
                if (qualityParam == "-x265-params")
                    ParameterSubValueReplaceOrInsert(qualityParam, "crf", "=" + quality.ToString());
                else
                    ParameterValueReplaceOrInsert(qualityParam, quality.ToString());
            }
            else
            {
                // ffmpeg support 2 types of bitrate definition, -b and -b:v
                string bitrateValFormat;
                string bitrateStr;

                bitrateValFormat = "-b";
                bitrateStr = ParameterValue(bitrateValFormat); // First try -b

                if (String.IsNullOrWhiteSpace(bitrateStr))
                {
                    bitrateValFormat = "-b:v"; // try the -b:v format
                    bitrateStr = ParameterValue(bitrateValFormat);

                    // Check if we go something now
                    if (String.IsNullOrWhiteSpace(bitrateStr))
                        return; // nothing to do here
                }

                bitrateStr = bitrateStr.Replace("k", "000");
                bitrateStr = bitrateStr.Replace("m", "000000");
                bitrateStr = bitrateStr.Replace("g", "000000000");
                bitrateStr = bitrateStr.Replace("K", "000");
                bitrateStr = bitrateStr.Replace("M", "000000");
                bitrateStr = bitrateStr.Replace("G", "000000000");
                int bitrate;
                if (!int.TryParse(bitrateStr, out bitrate))
                {
                    _jobLog.WriteEntry(this, "FFMpeg invalid bitrate in profile, using default bitrate " + DEFAULT_BIT_RATE.ToString(), Log.LogEntryType.Warning);
                    bitrate = DEFAULT_BIT_RATE;
                }

                bitrate = (int)((double)bitrate * _bitrateResolutionQuality * _userQuality);
                ParameterValueReplaceOrInsert(bitrateValFormat, bitrate.ToString(CultureInfo.InvariantCulture));
            }
        }

        protected override void SetVideoResize()
        {
            if (ParameterValue("-vcodec") == "copy") // For copy video stream don't set video processing parameters, it can break the conversion
            {
                _jobLog.WriteEntry(this, "Video Copy codec detected, skipping resizing", Log.LogEntryType.Warning);
                return;
            }

            int VideoWidth = _videoFile.Width;
            if ((_videoFile.CropWidth > 0) && (_videoFile.CropWidth < _videoFile.Width))
                VideoWidth = _videoFile.CropWidth; // Use the cropped width if present

            int VideoHeight = _videoFile.Height;
            if ((_videoFile.CropHeight > 0) && (_videoFile.CropHeight < _videoFile.Height))
                VideoHeight = _videoFile.CropHeight; // Use the cropped height if present

            // FFMEG required Width to be divisible by 16 and height to be divisible by 8 (this is post cropping height and width that needs to be scaled)
            int newWidth = VideoWidth;
            if (newWidth > _maxWidth)
                newWidth = _maxWidth; // We use the less of two, MaxWidth of post cropping video width
            newWidth = Util.MathLib.RoundOff(newWidth, 16);

            int newHeight = VideoHeight * newWidth / VideoWidth;
            newHeight = Util.MathLib.RoundOff(newHeight, 8);

            ParameterReplaceOrInsertVideoFilter("scale", "=" + newWidth.ToString(CultureInfo.InvariantCulture) + ":" + newHeight.ToString(CultureInfo.InvariantCulture));
            //AddVideoFilter("scale", newWidth.ToString(CultureInfo.InvariantCulture) + ":-1"); // set the maxWidth and let FFMPEG autoscale the height keeping the same aspect ratio (libxvid has weird restrictions on the ratio) - AUTOSCALE DOES NOT ROUND OFF CAUSING FFMPEG TO FAIL DUE TO INVALID PIXEL HEIGHTS
        }

        protected override void SetVideoCropping()
        {
            if (!_skipCropping)
            {
                if (ParameterValue("-vcodec") == "copy") // For copy video stream don't set video processing, it can break the conversion
                {
                    _jobLog.WriteEntry(this, "Video Copy codec detected, skipping set cropping", Log.LogEntryType.Warning);
                    return;
                }

                // Check if we need to run cropping
                if (String.IsNullOrEmpty(_videoFile.CropString))
                {
                    _jobStatus.CurrentAction = Localise.GetPhrase("Analyzing video information");
                    _videoFile.UpdateCropInfo(_jobLog);
                }

                // Check if we have a valid crop string
                if (!String.IsNullOrEmpty(_videoFile.CropString))
                {
                    ParameterReplaceOrInsertVideoFilter("crop", "=" + _videoFile.CropString);
                    _jobLog.WriteEntry(this, Localise.GetPhrase("FFMpeg setting up cropping"), Log.LogEntryType.Information);
                }
                else
                    _jobLog.WriteEntry(this, Localise.GetPhrase("FFMpeg found no video cropping"), Log.LogEntryType.Information);
            }
            else
                _jobLog.WriteEntry(this, Localise.GetPhrase("FFMpeg Skipping video cropping"), Log.LogEntryType.Information);

        }

        protected override void SetVideoAspectRatio()
        {
            if (ParameterValue("-vcodec") == "copy") // For copy video stream don't set video processing, it can break the conversion
            {
                _jobLog.WriteEntry(this, "Video Copy codec detected, skipping aspect ratio", Log.LogEntryType.Warning);
                return;
            }

            // LibXVid is very finicky and doesn't handle cropping well so we need to set the SAR aspect ration otherwise it mucks it up and fails
            // setsar and setdar should come at the end of the filter chain else it fails
            if (ParameterValue("-vcodec") == "libxvid")
            {
                if (!String.IsNullOrEmpty(_videoFile.FFMPEGStreamInfo.MediaInfo.VideoInfo.SAR))
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Found LibXVid encoder, cropping detected, setting SAR aspect ratio") + " " + _videoFile.FFMPEGStreamInfo.MediaInfo.VideoInfo.SAR, Log.LogEntryType.Information);
                    //ParameterValueReplaceOrInsert("-aspect", _videoFile.FFMPEGStreamInfo.MediaInfo.VideoInfo.DAR); // Set the DAR otherwise it fails sometimes while cropping as libxvid is very sensitive to PAR distortion
                    ParameterReplaceOrInsertVideoFilter("setsar", "=" + _videoFile.FFMPEGStreamInfo.MediaInfo.VideoInfo.SAR.Replace(':', '/')); // Set the SAR otherwise it fails sometimes while cropping as libxvid is very sensitive to SAR distortion
                }
                else
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Found LibXVid encoder, cropping detected, cannot read input SAR aspect ratio, skipping setting SAR - could fail video conversion") + " " + _videoFile.FFMPEGStreamInfo.MediaInfo.VideoInfo.SAR, Log.LogEntryType.Warning);
            }
        }

        protected override void SetAudioPostDRC()
        {
            // Nothing, ffmpeg supports preDRC
        }

        protected override void SetAudioPreDRC() // ffmpeg needs to setup this parameter before the inputs file because it applies to decoding the input
        {
            ParameterValueReplaceOrInsert("-drc_scale", DRC.ToString(CultureInfo.InvariantCulture));
        }

        protected override void SetAudioVolume()
        {
            ParameterReplaceOrInsertAudioFilter("volume", "=" + _volume.ToString("#0.0", CultureInfo.InvariantCulture) + "dB"); // it has to be a floating point number in dB
        }

        protected override void SetAudioLanguage()
        {
            // First clear any audio track selection if asked
            if (_encoderChooseBestAudioTrack) // We let the encoder choose the best audio track
            {
                // Remove all mapping info, let encoder choose the best
                _jobLog.WriteEntry(this, "Letting ffmpeg choose best audio track", Log.LogEntryType.Information);
                _cmdParams = Regex.Replace(_cmdParams, @"-map 0:[\S]*", ""); // Remove all patterns like -map 0:v, -map 0:v, -map 0:s and -map 0:4 so encoder can choose the best tracks based on what types of tracks are available
            }

            if (_videoFile.AudioStream == -1)
                _jobLog.WriteEntry(this, ("Cannot get Audio stream details, continuing with default Audio Language selection"), Log.LogEntryType.Warning);
            else
            {
                _jobLog.WriteEntry(this, "Selecting audio track " + _videoFile.AudioStream.ToString() + " and video track " + _videoFile.VideoStream.ToString(), Log.LogEntryType.Information);
                _cmdParams = _cmdParams.Replace("-map 0:a", ""); // Replace the map audio command with nothing, we will add a custom mapping below
                _cmdParams = _cmdParams.Replace("-map 0:v", ""); // Replace the map video command with nothing, we will add a custom mapping below
                // Leave the subtitle mapping if it exists, it will not impact us
                _cmdParams += " -map 0:" + _videoFile.AudioStream.ToString(CultureInfo.InvariantCulture); // Select the Audiotrack we had isolated earlier
                if (_videoFile.VideoStream != -1) // Check if we have a video stream
                    _cmdParams += " -map 0:" + _videoFile.VideoStream.ToString(CultureInfo.InvariantCulture);
                else
                    _jobLog.WriteEntry(this, ("Cannot get Video stream details, continuing without Video"), Log.LogEntryType.Warning);
            }
        }

        protected override void SetAudioChannels()
        {
            if (ParameterValue("-acodec") == "copy") // copy is not compatible with -ac
            {
                _jobLog.WriteEntry(this, "Skipping over requested to set audio channel information either due to COPY codec", Log.LogEntryType.Warning);
                return;
            }

            if (_2ChannelAudio) // Fix output to 2 channels
            {
                _jobLog.WriteEntry(this, ("Requested to limit Audio Channels to 2"), Log.LogEntryType.Information);
                ParameterValueReplaceOrInsert("-ac", "2");
            }
            else if ((_videoFile.AudioChannels > 0) && (ParameterValue("-ac") == "")) // Don't override what's given in the audio params
            {
                if ((ParameterValue("-acodec") == "libvo_aacenc") && (ParameterValue("-ac") == "")) // Allow for AAC channel override
                {
                    _jobLog.WriteEntry(this, ("FFMPEG LibVO AACEnc Audio Codec detected, settings Audio Channels to 2"), Log.LogEntryType.Information);
                    ParameterValueReplaceOrInsert("-ac", "2"); // libvo_aacenc does not support > 2 channels as of 3/7/12 for FFMPEG
                }
                else if ((ParameterValue("-acodec") == "libmp3lame") && (ParameterValue("-ac") == "")) // Allow for MP3 channel override
                {
                    _jobLog.WriteEntry(this, ("FFMPEG MP3 Lame Audio Codec detected, settings Audio Channels to 2"), Log.LogEntryType.Information);
                    ParameterValueReplaceOrInsert("-ac", "2"); // libmp3lame does not support > 2 channels as of 6/1/14 for FFMPEG
                }
                else
                {
                    _jobLog.WriteEntry(this, ("Setting Audio Channels to") + " " + _videoFile.AudioChannels.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Information);
                    ParameterValueReplaceOrInsert("-ac", _videoFile.AudioChannels.ToString(CultureInfo.InvariantCulture)); // except ac3 nothing else supports > 2 channels as of 3/7/12 for FFMPEG
                }

                if (!((ParameterValue("-acodec") == "ac3") || (ParameterValue("-acodec") == "ac-3") || (ParameterValue("-acodec") == "libmp3lame") || (ParameterValue("-acodec") == "libvo_aacenc") || (ParameterValue("-acodec") == "libfdk_aac")) && (_videoFile.AudioChannels > 2)) // AC3 support, AAC, MP3 taken care of above
                    _jobLog.WriteEntry(this, ("AC3 or LibFdkAAC Codec not detected, FFMPEG may not support > 2 channels for other audio codecs. May lead to failure of conversion"), Log.LogEntryType.Warning);
            }
            else if (ParameterValue("-ac") == "") // don't override the Audio Params if specified, since an audio track is not selected and channel information is not known default back to 6 channel audio (since stereo is not selected)
            {
                _jobLog.WriteEntry(this, "Did not find Audio Channel information, setting default audio channels to 6", Log.LogEntryType.Information);
                ParameterValueReplaceOrInsert("-ac", "6");
            }
            else
                _jobLog.WriteEntry(this, ("Skipping over requested to set audio channel information because audio parameters already contains channel directive"), Log.LogEntryType.Warning);
        }

        protected override void SetInputFileName() // general parameters already setup, now add the input filename details
        {
            _cmdParams = _cmdParams.Trim() + " -y -i " + Util.FilePaths.FixSpaces(SourceVideo);
        }

        protected override void SetOutputFileName() // general + input + video + audio setup, now add the output filename
        {
            // Check if we are in 2 pass mode or single pass mode, if we are in 2 pass mode, then we need to set the pass mode and passlogname BEFORE the output file
            if (_2Pass)
            {
                _cmdParams += " -pass n -passlogfile " + Util.FilePaths.FixSpaces(passLog); // This HAS to come before the output- put generic pass n for now, will replace it later
                _cmdParams = _cmdParams.Trim() + " " + Util.FilePaths.FixSpaces(_convertedFile);
            }
            else
                _cmdParams = _cmdParams.Trim() + " " + Util.FilePaths.FixSpaces(_convertedFile);
        }

        protected override bool ConvertWithTool()
        {
            if (ParameterValue("-vcodec") == "libx264")
                if (String.IsNullOrWhiteSpace(ParameterSubValue("-x264opts", "threads"))) // Auto threads
                    ParameterSubValueReplaceOrInsert("-x264opts", "threads", "=auto");

            if (_2Pass == true)
            {
                // 1st Pass
                string baseParam = _cmdParams; // save the base command params

                { // Optimizing the parameters
                    // The next set of parameters need to come before -pass n, so cut off everything from -pass n to end and attach it back after
                    string endParams = _cmdParams.Substring(_cmdParams.IndexOf(" -pass n")); // save it
                    _cmdParams = _cmdParams.Remove(_cmdParams.IndexOf(" -pass n")); // Remove the end

                    // We replicate -Turbo option from Mencoder to speed up the 1st pass, i.e. reduce subq and framerefs to 1,  partitions none
                    // Other encoders use ffmpeg options which are replaced below, only libx264 uses it's own options
                    // http://forum.doom9.org/archive/index.php/t-134225.html
                    // http://forum.doom9.org/showthread.php?t=141202
                    // http://forum.doom9.org/archive/index.php/t-143668.html
                    // Delete these values if they exist
                    ParameterSubValueDelete("-x264opts", "subq"); // Will be replaced with generic afterwards
                    ParameterSubValueDelete("-x264opts", "ref"); // Will be replaced with generic afterwards
                    ParameterSubValueDelete("-x264opts", "trellis"); // Will be replaced with generic afterwards
                    ParameterSubValueDelete("-x264opts", "b-adapt"); // Will be replaced with generic afterwards
                    ParameterSubValueDelete("-x264opts", "me"); // Will be replaced with generic afterwards
                    ParameterSubValueReplace("-x264opts", "partitions", "=none");
                    ParameterSubValueReplace("-x264opts", "mixed-refs", "=0");
                    ParameterSubValueReplace("-x264opts", "no-fast-pskip", "=0");
                    ParameterSubValueReplace("-x264opts", "weightb", "=0");
                    ParameterSubValueDelete("-x264opts", "8x8dct");

                    // Replace these generic values that will be passed to all encoders
                    ParameterValueReplaceOrInsert("-subq", "1");
                    ParameterValueReplaceOrInsert("-refs", "1");
                    ParameterValueReplaceOrInsert("-trellis", "0");
                    ParameterValueReplaceOrInsert("-b_strategy", "1"); // b-adapt
                    ParameterValueReplaceOrInsert("-me_method", "dia");

                    // Now add it back
                    _cmdParams += endParams;
                }

                // Speed up the 1st pass, no file required
                //_cmdParams = _cmdParams.Replace(Util.FilePaths.FixSpaces(_convertedFile), "-f rawvideo NUL"); // DO NOT DO -f rawvideo NUL, SEE FFMPEG TICKET #3109, 1st PASS MUXER SHOULD MATCH 2nd PASS MUXER OTHERWISE FFMPEG WILL CRASH, PLUS THIS DOES NOT REALLY IMPROVE PERFORMANCEWe don't need file on 1st pass, will compensate for output file in next file with -f rawvideo NULL (empty and fast)
                _cmdParams = _cmdParams.Replace("-pass n", "-pass 1"); // This is 1st pass

                _jobStatus.CurrentAction = Localise.GetPhrase("Converting video file - Pass 1");
                if (!FFmpeg.FFMpegExecuteAndHandleErrors(_cmdParams, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(_convertedFile), false)) // Don't check output file size since 1st pass size may not be reliable
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("FFMpeg Pass 1 conversion failed"), Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "FFMpeg Pass 1 conversion failed";
                    return false;
                }

                // Reset the command line for the 2nd pass
                _cmdParams = baseParam;
                _cmdParams = _cmdParams.Replace("-pass n", "-pass 2"); // This is 2nd pass

                _jobStatus.CurrentAction = Localise.GetPhrase("Converting video file - Pass 2");
                if (!FFmpeg.FFMpegExecuteAndHandleErrors(_cmdParams, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(_convertedFile)))
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("FFMpeg Pass 2 conversion failed"), Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "FFMpeg Pass 2 conversion failed";
                    return false;
                }
            }
            else
            {
                _jobStatus.CurrentAction = Localise.GetPhrase("Converting video file");
                if (!FFmpeg.FFMpegExecuteAndHandleErrors(_cmdParams, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(_convertedFile)))
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("FFMpeg conversion failed"), Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "FFMpeg conversion failed";
                    return false;
                }
            }

            return true;
        }
    }
}
