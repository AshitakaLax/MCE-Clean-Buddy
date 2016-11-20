using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MCEBuddy.AppWrapper;
using MCEBuddy.Globals;
using MCEBuddy.VideoProperties;
using MCEBuddy.Util;
using MCEBuddy.CommercialScan;

namespace MCEBuddy.Transcode
{
    public class ConvertWithHandbrake : ConvertBase
    {
        private const int DEFAULT_X264_QUALITY = 20;
        private const int DEFAULT_FFMPEG_QUALITY = 12;
        private const int DEFAULT_THEORA_QUALITY = 45;
        private const int DEFAULT_QUALITY_VALUE = 20; // All other codecs (ffmpeg2, mpeg2, vp8 etc)
        private const int DEFAULT_BIT_RATE = 1500;
        private const double DRC = 2.5; // Dynamic Range Compression (0 to 4, 2.5 is a good value)
        private string[] h264Encoders = { "x264" }; // "ffmpeg", "ffmpeg4", "mpeg4" are all H.263
        // Note: We need to update this list of frame rates eveytime we update Handbrake
        private List<string> supportedFrameRates = new List<string> { "5", "10", "12", "15", "23.976", "24", "25", "29.97", "30", "50", "59.94", "60" };
        private bool hardwareEncodingAvailable = false;

        public ConvertWithHandbrake(ConversionJobOptions conversionOptions, string tool, VideoInfo videoFile, JobStatus jobStatus, Log jobLog, Scanner commercialScan)
            : base(conversionOptions, tool, videoFile, jobStatus, jobLog, commercialScan)
        {
            // Check if we have hardware encoding support available on the system
            Handbrake hb = new Handbrake(jobLog);
            hardwareEncodingAvailable = hb.QuickSyncEncodingAvailable;

            //Check if the profiles is setup for Hardware encoding, if so don't adjust hardware encoding options
            Ini ini = new Ini(GlobalDefs.ProfileFile);
            bool profileHardwareEncoding = ini.ReadBoolean(conversionOptions.profile, tool + "-UsingHardwareEncoding", false);
            _jobLog.WriteEntry(this, "Handbrake profile optimized for hardware encoding, disable auto hardware optimization (" + tool + "-UsingHardwareEncoding) : " + profileHardwareEncoding.ToString(), Log.LogEntryType.Debug);
            if (_preferHardwareEncoding && profileHardwareEncoding)
            {
                _jobLog.WriteEntry(this, "Hardware enabled handbrake profile, disabling auto hardware encoder adjustments", Log.LogEntryType.Debug);
                _preferHardwareEncoding = false; // Don't change any settings, this profile is already setup for hardware encoding
            }

            // Check if we are using any of the h264 codecs, only then can we use hardware encoder for H264
            if (_preferHardwareEncoding && !h264Encoders.Any((new FFMpegMEncoderParams(_videoParams)).ParameterValue("-e").ToLower().Equals))
            {
                _jobLog.WriteEntry(this, "Cannot find h264 encoder, disabling auto hardware h264 encoder adjustments", Log.LogEntryType.Debug);
                _preferHardwareEncoding = false; // Don't use hardware encoder since this isn't a h264 profile
            }
        }

        protected override void FinalSanityCheck()
        {
            // If we have hardware encoding available and user selects prefer hardware encoding, then change the codec to use hardware
            if (hardwareEncodingAvailable && _preferHardwareEncoding)
                ParameterValueReplace("-e", "qsv_h264"); // Use hardware encoder
        }

        protected override bool IsPresetVideoWidth()
        {
            // Get the profile conversion width (could be -w or -X)
            return ((ParameterValue("-w") != "") || (ParameterValue("-X") != ""));
        }

        protected override bool ConstantVideoQuality
        {
            get { return (ParameterValue("-q") != ""); }
        }

        protected override void SetVideoOutputFrameRate()
        {
            if (String.IsNullOrWhiteSpace(_fps))
                return; // Nothing to do here

            double? fpsNo = Util.MathLib.EvaluateBasicExpression(_fps);
            if (fpsNo == null || double.IsNaN(fpsNo.Value) || double.IsInfinity(fpsNo.Value) || fpsNo.Value <= 0)
            {
                _jobLog.WriteEntry(this, "Invalid frame rate -> " + _fps + ", skipping setting framerate.", Log.LogEntryType.Warning);
                return;
            }

            string fps = Util.MathLib.GetClosestNumber(supportedFrameRates, fpsNo.GetValueOrDefault()); // Handbrake only support specific frame rates

            // Do not override any set framerates
            if (ParameterValue("-r") == "")
            {
                ParameterValueReplaceOrInsert("-r", fps);
                _jobLog.WriteEntry(this, "User requested -> " + fpsNo.Value.ToString() + ", selecting closest supported value by Handbrake -> " + fps + ".\r\nIf you want to use an exact value then use ffmpeg encoder (change the order in the profile to ffmpeg)", Log.LogEntryType.Debug);
            }
            else
                _jobLog.WriteEntry(this, "Found FPS in profile -> " + ParameterValue("-r") + ", skipping setting framerate.", Log.LogEntryType.Warning);
        }

        protected override void SetVideoDeInterlacing()
        {
            // TODO: Need to complete auto deinterlacing
            if (_autoDeInterlacing)
            {
                if (ParameterValue("-e") == "copy") // For copy video stream don't set video processing parameters, it can break the conversion
                {
                    _jobLog.WriteEntry(this, "Video Copy codec detected, skipping autoDeinterlacing", Log.LogEntryType.Warning);
                    return;
                }

                if (_videoFile.VideoScanType == ScanType.Unknown) // We don't know the type of interlacing, use default
                {
                    _jobLog.WriteEntry(this, "Handbrake Unknown video scan, using profile default options", Log.LogEntryType.Warning);
                    return;
                }

                // Delete all not required and then add those you want (cannot use ParameterValueDelete or ParameterValueReplace for params without values)
                _cmdParams = _cmdParams.Replace("--decomb ", "");
                _cmdParams = _cmdParams.Replace("--deinterlace ", "");
                _cmdParams = _cmdParams.Replace("--detelecine ", "");

                if (_videoFile.VideoScanType == ScanType.Progressive) // If we are progressive, we don't need any interlacing/telecine filters
                {
                    _jobLog.WriteEntry(this, "Handbrake detected progressive video scan, no filters required", Log.LogEntryType.Debug);
                }
                else if (_videoFile.VideoScanType == ScanType.Interlaced)
                {
                    _jobLog.WriteEntry(this, "Handbrake detected interlaced (or mbaff/paff) video scan, adding de-interlacing filters", Log.LogEntryType.Debug);
                    if (hardwareEncodingAvailable && _preferHardwareEncoding)
                        ParameterValueReplaceOrInsert("--deinterlace", "qsv"); // use hardware deinterlacing
                    else
                        ParameterValueReplaceOrInsert("--decomb", ""); // Don't use decomb for hardware (slows down), for software use decomb instead of deinterlace (quality and speed). https://trac.handbrake.fr/wiki/Decomb
                }
                else if (_videoFile.VideoScanType == ScanType.Telecine)
                {
                    _jobLog.WriteEntry(this, "Handbrake detected telecined video scan, adding inverse telecining filters", Log.LogEntryType.Debug);
                    ParameterValueReplaceOrInsert("--detelecine", "");
                }
            }
        }

        protected override void SetVideoTrim()
        {
            // Set the start trim
            if (_startTrim != 0)
                ParameterValueReplaceOrInsert("--start-at", "duration:" + _startTrim.ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Set the end trim (calculate from reducing from video length)
            if (_endTrim != 0)
            {
                // Handbrake can specify duration of encoding, i.e. encoding_duration = stopTime - startTime
                // startTime = startTrim, stopTime = video_duration - endTrim
                int encDuration = (((int)_videoFile.Duration) - _endTrim) - (_startTrim); // by default _startTrim is 0
                ParameterValueReplaceOrInsert("--stop-at", "duration:" + encDuration.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        protected override void SetVideoAspectRatio()
        {
            // Nothing for Handbrake
            if (ParameterValue("-e") == "copy") // For copy video stream don't set video processing parameters, it can break the conversion
            {
                _jobLog.WriteEntry(this, "Video Copy codec detected, skipping aspect ratio", Log.LogEntryType.Warning);
                return;
            }
        }

        protected override void SetVideoBitrateAndQuality()
        {
            if (ConstantVideoQuality)  // We don't need to adjust for resolution changes here since constant quality takes care of that (only user specified changes in quality)
            {
                int quality;
                string qualityStr = ParameterValue("-q");
                if (String.IsNullOrWhiteSpace(qualityStr))
                    return;

                if (!int.TryParse(qualityStr, out quality))
                {
                    quality = -1;
                }

                // Set up qualityVal
                if (ParameterValue("-e") == "x264")
                {
                    // h.264
                    if (quality == -1)
                    {
                        quality = DEFAULT_X264_QUALITY;
                        _jobLog.WriteEntry(this, "Handbrake invalid quality in profile, using default x264 quality " + DEFAULT_X264_QUALITY.ToString(), Log.LogEntryType.Warning);
                    }
                    quality = ConstantQualityValue(51, 0, quality);
                }
                else if ((ParameterValue("-e") == "ffmpeg") || (ParameterValue("-e") == "mpeg4") || (ParameterValue("-e") == "ffmpeg4"))
                {
                    // MPEG-4 divx
                    if (quality == -1)
                    {
                        quality = DEFAULT_FFMPEG_QUALITY;
                        _jobLog.WriteEntry(this, "Handbrake invalid quality in profile, using default ffmpeg quality " + DEFAULT_FFMPEG_QUALITY.ToString(), Log.LogEntryType.Warning);
                    }
                    quality = ConstantQualityValue(31, 1, quality);
                }
                else if (ParameterValue("-e") == "theora")
                {
                    // theora
                    if (quality == -1)
                    {
                        quality = DEFAULT_THEORA_QUALITY;
                        _jobLog.WriteEntry(this, "Handbrake invalid quality in profile, using default theora quality " + DEFAULT_THEORA_QUALITY.ToString(), Log.LogEntryType.Warning);
                    }
                    quality = ConstantQualityValue(0, 63, quality);
                }
                else
                {
                    // catchall default h.264
                    if (quality == -1)
                    {
                        quality = DEFAULT_QUALITY_VALUE;
                        _jobLog.WriteEntry(this, "Handbrake invalid quality in profile, using default quality " + DEFAULT_QUALITY_VALUE.ToString(), Log.LogEntryType.Warning);
                    }
                    quality = ConstantQualityValue(51, 0, quality);
                }

                ParameterValueReplaceOrInsert("-q", quality.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                string bitrateToken = "-b";
                string bitrateStr = ParameterValue(bitrateToken);
                if (String.IsNullOrEmpty(bitrateStr))
                {
                    bitrateToken = "-br";
                    bitrateStr = ParameterValue(bitrateToken);
                    if (String.IsNullOrEmpty(bitrateStr))
                        return;
                }

                int bitrate;
                if (!int.TryParse(bitrateStr, out bitrate))
                {
                    _jobLog.WriteEntry(this, "Handbrake invalid bitrate in profile, using default bitrate " + DEFAULT_BIT_RATE.ToString(), Log.LogEntryType.Warning);
                    bitrate = DEFAULT_BIT_RATE;
                }

                bitrate = (int)((double)bitrate * _bitrateResolutionQuality * _userQuality);
                ParameterValueReplaceOrInsert(bitrateToken, bitrate.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        protected override void SetVideoResize()
        {
            if (ParameterValue("-e") == "copy") // For copy video stream don't set video processing parameters, it can break the conversion
            {
                _jobLog.WriteEntry(this, "Video Copy codec detected, skipping resizing", Log.LogEntryType.Warning);
                return;
            }

            if (!_cmdParams.Contains("anamorphic"))
                _cmdParams += " --loose-anamorphic";

            ParameterValueReplaceOrInsert("-X", _maxWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        protected override void SetVideoCropping()
        {
            // If skipCropping is defined then we need to disable AutoCropping
            if (ParameterValue("-e") == "copy") // For copy video stream don't set video processing parameters, it can break the conversion
            {
                _jobLog.WriteEntry(this, "Video Copy codec detected, skipping cropping", Log.LogEntryType.Warning);
                return;
            }

            if (_skipCropping)
            {
                if (String.IsNullOrWhiteSpace(ParameterValue("-e"))) // Don't overwrite any user provided crop parameters
                    ParameterValueReplaceOrInsert("--crop", "0:0:0:0");

                _jobLog.WriteEntry(this, Localise.GetPhrase("Skipping video cropping"), Log.LogEntryType.Information);
            }
            else
                _jobLog.WriteEntry(this, Localise.GetPhrase("Handbrake auto video cropping"), Log.LogEntryType.Information);
        }

        protected override void SetAudioPreDRC()
        {
            // Nothing, handbrake supports postDRC
        }

        protected override void SetAudioPostDRC()
        {
            ParameterValueReplaceOrInsert("-D", DRC.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        protected override void SetAudioVolume()
        {
            ParameterValueReplaceOrInsert("--gain", _volume.ToString("#0.0", System.Globalization.CultureInfo.InvariantCulture)); // it has to be a floating point number in DB
        }

        protected override void SetAudioLanguage()
        {
            // First clear any audio track selection if asked
            if (_encoderChooseBestAudioTrack) // We let the encoder choose the best audio track
            {
                _jobLog.WriteEntry(this, "Letting handbrake choose best audio track", Log.LogEntryType.Information);
                ParameterValueDelete("-a"); // Delete this parameter, let handbrake choose the best audio track
            }

            if (_videoFile.AudioTrack == -1)
                _jobLog.WriteEntry(this, Localise.GetPhrase("Cannot get Audio and Video stream details, continuing with default Audio Language selection"), Log.LogEntryType.Warning);
            else
            {
                // We need to compensate for zero channel audio tracks, which are ignored by handbrake
                // Cycle through and skip over zero channel tracks until we get the track we need
                int audioTrack = 1; // Baseline audio track for Handbrake is 1
                for (int i = 0; i < _videoFile.FFMPEGStreamInfo.AudioTracks; i++)
                {
                    if (_videoFile.AudioTrack == i) // We have reached the selected audio track limit
                        break;

                    if (_videoFile.FFMPEGStreamInfo.MediaInfo.AudioInfo[i].Channels != 0) // skip over 0 channel tracks until we reach the selected audio track
                        audioTrack++;
                }

                _jobLog.WriteEntry(this, "Selecting audio (normalized) track " + audioTrack.ToString(), Log.LogEntryType.Information);
                ParameterValueReplaceOrInsert("-a", audioTrack.ToString(System.Globalization.CultureInfo.InvariantCulture)); // Select the Audiotrack we had isolated earlier (1st Audio track is 1, FFMPEGStreamInfo is 0 based)
            }
        }

        protected override void SetAudioChannels()
        {
            if (ParameterValue("-E") == "copy") // copy is not compatible with -6
            {
                _jobLog.WriteEntry(this, "Skipping over requested to set audio channel information either due to COPY codec", Log.LogEntryType.Warning);
                return;
            }

            if (_2ChannelAudio) // set channels to stereo
            {
                _jobLog.WriteEntry(this, ("Requested to limit Audio Channels to 2"), Log.LogEntryType.Information);
                ParameterValueReplaceOrInsert("-6", "stereo"); // force 2 channel audio
            }
            else if ((ParameterValue("-6") == "")) // check if 2 channel audio is fixed and no audio channel information is specified in audio params
            {
                if ((ParameterValue("-E") == "lame") && (ParameterValue("-6") == "")) // Allow for MP3 channel override
                {
                    _jobLog.WriteEntry(this, ("Handbrake MP3 Lame Audio Codec detected, settings Audio Channels to 2"), Log.LogEntryType.Information);
                    ParameterValueReplaceOrInsert("-6", "stereo"); // libmp3lame does not support > 2 channels as of 6/1/14
                }
                else
                {
                    _jobLog.WriteEntry(this, ("Setting audio channels"), Log.LogEntryType.Information);
                    if (_videoFile.AudioChannels == 6)
                        ParameterValueReplaceOrInsert("-6", "5point1"); // By default AUTO for aac audio downmixes to stereo, hence specify 5point1 (it used to be 6ch prior to version 0.9.9)
                    else
                        ParameterValueReplaceOrInsert("-6", "auto");
                }
            }
            else
                _jobLog.WriteEntry(this, ("Skipping over requested to set audio channel information because audio parameters already contains channel directive"), Log.LogEntryType.Warning);
        }

        protected override void SetInputFileName()
        {
            _cmdParams = "-i " + Util.FilePaths.FixSpaces(SourceVideo) + " " + _cmdParams.Trim();
        }

        protected override void SetOutputFileName()
        {
            _cmdParams = _cmdParams.Trim() + " -o " + Util.FilePaths.FixSpaces(_convertedFile);
        }

        protected override bool ConvertWithTool()
        {
            if (_2Pass && !(hardwareEncodingAvailable && (ParameterValue("-e") == "qsv_h264"))) // QuickSync does not support 2 pass encoding yet
                _cmdParams += " -2";
            else if (hardwareEncodingAvailable && (ParameterValue("-e") == "qsv_h264") && _2Pass)
                _jobLog.WriteEntry(this, "QuickSync does not support 2 Pass, using 1 pass.", Log.LogEntryType.Warning);

            // TODO: Update this section based on QuickSync encoder opts updates
            // Handbrake QuickSync has specific encoding opts, enable them if required - https://trac.handbrake.fr/wiki/QuickSyncOptions
            if (hardwareEncodingAvailable && _preferHardwareEncoding && (ParameterValue("-e") == "qsv_h264"))
            {
                // Replace only those that differ from the standard x264 options
                string xValue;

                xValue = ParameterSubValue("-x", "no-cabac");
                if (!String.IsNullOrWhiteSpace(xValue))
                {
                    ParameterSubValueDelete("-x", "no-cabac");
                    ParameterSubValueReplaceOrInsert("-x", "cabac", "=" + (xValue == "0" ? "1" : "0"));
                }

                xValue = ParameterSubValue("-x", "no_cabac");
                if (!String.IsNullOrWhiteSpace(xValue))
                {
                    ParameterSubValueDelete("-x", "no_cabac");
                    ParameterSubValueReplaceOrInsert("-x", "cabac", "=" + (xValue == "0" ? "1" : "0"));
                }

                xValue = ParameterSubValue("-x", "b-pyramid");
                if (!String.IsNullOrWhiteSpace(xValue))
                    ParameterSubValueReplace("-x", "b-pyramid", "=" + (xValue == "none" ? "0" : "1"));

                xValue = ParameterSubValue("-x", "b_pyramid");
                if (!String.IsNullOrWhiteSpace(xValue))
                {
                    ParameterSubValueDelete("-x", "b_pyramid");
                    ParameterSubValueReplaceOrInsert("-x", "b-pyramid", "=" + (xValue == "none" ? "0" : "1"));
                }

                xValue = ParameterSubValue("-x", "rc-lookahead");
                if (!String.IsNullOrWhiteSpace(xValue))
                {
                    ParameterSubValueDelete("-x", "rc-lookahead");
                    ParameterSubValueReplaceOrInsert("-x", "la-depth", "=" + xValue);
                }

                xValue = ParameterSubValue("-x", "rc_lookahead");
                if (!String.IsNullOrWhiteSpace(xValue))
                {
                    ParameterSubValueDelete("-x", "rc_lookahead");
                    ParameterSubValueReplaceOrInsert("-x", "la-depth", "=" + xValue);
                }

                _jobLog.WriteEntry(this, "After adujsting QuickSync encoder opts -> " + _cmdParams, Log.LogEntryType.Debug);
            }
            
            Handbrake hb = new Handbrake(_cmdParams, _jobStatus, _jobLog);
            _jobStatus.CurrentAction = Localise.GetPhrase("Converting video file");
            hb.Run();
            if (!hb.Success) // something didn't complete or went wrong, don't check for % since sometimes handbrake shows less than 90%
            {
                // TODO: When handbrake hardware encoder is stable we need to remove this fallback check
                // Check if we enabled hardware encoding, possibly could have failed due to hardware encoder (unstable). Don't fallback if the user hardcoded hardware encoding
                if (hardwareEncodingAvailable && _preferHardwareEncoding && (ParameterValue("-e") == "qsv_h264"))
                {
                    _jobLog.WriteEntry(this, "Handbrake conversion failed with hardware encoder, retrying with default x264 encoder", Log.LogEntryType.Warning);

                    if (ParameterValue("--deinterlace") == "qsv") // If we are using hardware deinterlacer change it back
                    {
                        _jobLog.WriteEntry(this, "Using standard deinterlacer instead of qsv", Log.LogEntryType.Debug);
                        ParameterValueReplaceOrInsert("--deinterlace", ""); // don't use hardware deinterlacing, default back
                    }

                    _jobLog.WriteEntry(this, "Using default x264 encoder instead of qsv", Log.LogEntryType.Debug);
                    ParameterValueReplaceOrInsert("-e", "x264"); // default back to x264 encoder

                    if (_2Pass && !_cmdParams.Contains(" -2")) // Check if we have 2Pass enabled, x264 supports it (hardware encoder does not)
                    {
                        _jobLog.WriteEntry(this, "Enabling 2 pass conversion for x264", Log.LogEntryType.Debug);
                        _cmdParams += " -2";
                    }

                    hb = new Handbrake(_cmdParams, _jobStatus, _jobLog);
                    _jobStatus.CurrentAction = Localise.GetPhrase("Converting video file");
                    hb.Run();
                    if (!hb.Success) // something didn't complete or went wrong, don't check for % since sometimes handbrake shows less than 90%
                    {
                        _jobLog.WriteEntry(this, "Handbrake fallback conversion failed", Log.LogEntryType.Error);
                        _jobStatus.ErrorMsg = "Handbrake fallback conversion failed";
                        return false;
                    }
                }
                else
                {
                    _jobLog.WriteEntry(this, "Handbrake conversion failed", Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "Handbrake conversion failed";
                    return false;
                }
            }

            return true;
        }
    }
}