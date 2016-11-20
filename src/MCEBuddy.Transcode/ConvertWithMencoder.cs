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
    public class ConvertWithMencoder : ConvertBase
    {
        private const int DEFAULT_X264_QUALITY = 10;
        private const int DEFAULT_XVID_QUALITY = 4;
        private const int DEFAULT_LAVC_QUALITY = 4;

        private const int DEFAULT_X264_BITRATE = 1800;
        private const int DEFAULT_XVID_BITRATE = 2000;
        private const int DEFAULT_LAVC_BITRATE = 2000;
        private bool mEncoderEDLSkip = false;
        private string _extractCC = "";
        private const double DRC = 0.8; // Dynamic Range Compression to 80%

        public ConvertWithMencoder(ConversionJobOptions conversionOptions, string tool, VideoInfo videoFile, JobStatus jobStatus, Log jobLog, Scanner commercialScan)
            : base(conversionOptions, tool, videoFile, jobStatus, jobLog, commercialScan)
        {
            //Check if MEncoder EDL Removal has been disabled at conversion time
            Ini ini = new Ini(GlobalDefs.ProfileFile);
            mEncoderEDLSkip = ini.ReadBoolean(conversionOptions.profile, "MEncoderEDLSkip", false);
            _jobLog.WriteEntry(this, "MEncoder skip EDL cuts (MEncoderEDLSkip) : " + mEncoderEDLSkip.ToString(), Log.LogEntryType.Debug);
            
            _extractCC = conversionOptions.extractCC;
            if (!String.IsNullOrEmpty(_extractCC)) // If Closed Caption extraction is enabled, we don't use cut EDL using Mencoder during encoding, Mencoder has a bug which causes it to cut out of sync with the EDL file which throws the CC out of sync, it will be cut separately
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Closed Captions Enabled, skipping EDL cutting during encoding"), Log.LogEntryType.Information);
                mEncoderEDLSkip = true;
            }
            
            if ((_startTrim != 0) || (_endTrim != 0)) // If trimming is enabled skip cutting using EDL otherwise MEncoder messes it up
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Trimming Enabled, skipping EDL cutting during encoding"), Log.LogEntryType.Information);
                mEncoderEDLSkip = true;
            }
        }

        private void SetLanguage()
        {
            string langParam = " -alang ";
            if (Localise.TwoLetterISO() != "en")
            {
                langParam += Localise.TwoLetterISO() + ",en";
            }
            else
            {
                langParam += "en";
            }
            _cmdParams = _cmdParams.Trim() + langParam;
            _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder: Setting language parameter") + " " + langParam, Log.LogEntryType.Information);
        }

        protected override void FinalSanityCheck()
        {

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
            get
            {
                if (!String.IsNullOrWhiteSpace(ParameterSubValue("-x264encopts", "qp"))) return true;
                if (!String.IsNullOrWhiteSpace(ParameterSubValue("-xvidencopts", "fixed_quant"))) return true;
                if (!String.IsNullOrWhiteSpace(ParameterSubValue("-lavcopts", "vqscale"))) return true;
                return false;
            }
        }

        protected override void SetVideoOutputFrameRate()
        {
            if (String.IsNullOrWhiteSpace(_fps))
                return; // Nothing to do here

            // Do not override any set framerates
            if (ParameterValue("-ofps") == "")
                ParameterValueReplaceOrInsert("-ofps", _fps);
            else
                _jobLog.WriteEntry(this, "Found FPS in profile -> " + ParameterValue("-ofps") + ", skipping setting framerate.", Log.LogEntryType.Warning);
        }

        protected override void SetVideoDeInterlacing()
        {
            // TODO: Need to complete auto deinterlacing
            if (_autoDeInterlacing)
            {
                if (ParameterValue("-ovc") == "copy") // For copy video stream don't set video prcoessing parameters, it can break the conversion
                {
                    _jobLog.WriteEntry(this, "Video Copy codec detected, skipping autoDeinterlacing", Log.LogEntryType.Warning);
                    return;
                }

                if (_videoFile.VideoScanType == ScanType.Unknown) // We don't know the type of interlacing, use default
                {
                    _jobLog.WriteEntry(this, "Mencoder Unknown video scan, using profile default options", Log.LogEntryType.Warning);
                    return;
                }

                // Delete all not required and then add those you want
                ParameterDeleteVideoFilter("yadif"); // Deinterlacing not required
                ParameterDeleteVideoFilter("pullup"); // Inverse Telecine filter
                ParameterDeleteVideoFilter("softskip"); // Frame dropping following pullup
                ParameterReplaceOrInsertVideoFilter("harddup", ""); // Encode duplicate frames, always needed

                if (_videoFile.VideoScanType == ScanType.Progressive) // If we are progressive, we don't need any interlacing/telecine filters
                {
                    _jobLog.WriteEntry(this, "Mencoder detected progressive video scan, no filters required", Log.LogEntryType.Debug);
                }
                else if (_videoFile.VideoScanType == ScanType.Interlaced)
                {
                    _jobLog.WriteEntry(this, "Mencoder detected interlaced (or mbaff/paff) video scan, adding de-interlacing filters", Log.LogEntryType.Debug);
                    ParameterReplaceOrInsertVideoFilter("yadif", "=0:-1"); // Deinterlacing with auto interlace frame detect
                }
                else if (_videoFile.VideoScanType == ScanType.Telecine)
                {
                    _jobLog.WriteEntry(this, "Mencoder detected telecined video scan, adding inverse telecining filters", Log.LogEntryType.Debug);
                    ParameterReplaceOrInsertVideoFilter("pullup", ""); // Inverse telecine
                    ParameterReplaceOrInsertVideoFilter("softskip", ""); // framedropping following pullup (always) - "http://www.mplayerhq.hu/DOCS/man/en/mplayer.1.html#VIDEO FILTERS"
                }
            }
        }

        protected override void SetVideoTrim()
        {
            // Set the start trim
            if (_startTrim != 0)
                ParameterValueReplaceOrInsert("-ss", _startTrim.ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Set the end trim (calculate from reducing from video length)
            if (_endTrim != 0)
            {
                // Mencoder can specify duration of encoding, i.e. encoding_duration = stopTime - startTime
                // startTime = startTrim, stopTime = video_duration - endTrim
                int encDuration = (((int)_videoFile.Duration) - _endTrim) - (_startTrim); // by default _startTrim is 0
                ParameterValueReplaceOrInsert("-endpos", encDuration.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }                
        }

        protected override void SetVideoAspectRatio()
        {
            // Nothing for Mencoder
            if (ParameterValue("-ovc") == "copy") // For copy video stream don't set video prcoessing parameters, it can break the conversion
            {
                _jobLog.WriteEntry(this, "Video Copy codec detected, skipping aspect ratio", Log.LogEntryType.Warning);
                return;
            }
        }

        private void SetBitRate(string cmd, string subCmd, int defaultBitrate)
        {
            string bitrateStr = ParameterSubValue(cmd, subCmd);
            if (String.IsNullOrWhiteSpace(bitrateStr))
                return;

            int bitrate;
            if (!int.TryParse(bitrateStr, out bitrate))
            {
                bitrate = defaultBitrate;
                _jobLog.WriteEntry(this, "Mencoder invalid bitrate in profile, using default bitrate " + defaultBitrate.ToString(), Log.LogEntryType.Warning);
            }
            bitrate = (int)((double)bitrate * _bitrateResolutionQuality * _userQuality);
            ParameterSubValueReplace(cmd, subCmd, "=" + bitrate.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private void SetConstantQuality(string cmd, string subCmd, int min, int max, int defaultQuality)  // We don't need to adjust for resolution changes here since constant quality takes care of that (only user specified changes in quality)
        {
            string qualityStr = ParameterSubValue(cmd, subCmd);
            if (String.IsNullOrWhiteSpace(qualityStr))
                return;

            int quality;
            if (!int.TryParse(qualityStr, out quality))
            {
                quality = defaultQuality;
                _jobLog.WriteEntry(this, "Mencoder invalid quality in profile, using default quality " + defaultQuality.ToString(), Log.LogEntryType.Warning);
            }
            quality = ConstantQualityValue(min, max, quality);
            ParameterSubValueReplace(cmd, subCmd, "=" + quality.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        protected override void SetVideoBitrateAndQuality()
        {
            if ((_userQuality == 1) && (_bitrateResolutionQuality == 1))
                return;

            if (!ConstantVideoQuality) // Constant quality does not need to be updated since it's constant quality irrespective of resolution
            {
                // Try for the fixed bitrate options
                SetBitRate("x264encopts", "bitrate", DEFAULT_X264_BITRATE);
                SetBitRate("xvidencopts", "bitrate", DEFAULT_XVID_BITRATE);
                SetBitRate("lavcopts", "vbitrate", DEFAULT_LAVC_BITRATE);
            }
            else
            {
                // Try for the constant quality options
                SetConstantQuality("x264encopts", "qp", 51, 1, DEFAULT_X264_QUALITY);
                SetConstantQuality("xvidencopts", "fixed_quant", 31, 1, DEFAULT_XVID_QUALITY);
                SetConstantQuality("lavcopts", "vqscale", 31, 1, DEFAULT_LAVC_QUALITY);
            }
        }

        protected override void SetVideoResize()
        {
            if (ParameterValue("-ovc") == "copy") // For copy video stream don't set video prcoessing parameters, it can break the conversion
            {
                _jobLog.WriteEntry(this, "Video Copy codec detected, skipping resizing", Log.LogEntryType.Warning);
                return;
            }

            int VideoWidth = _videoFile.Width;
            if ((_videoFile.CropWidth > 0) && (_videoFile.CropWidth < _videoFile.Width))
                VideoWidth = _videoFile.CropWidth; // Use the cropped width if present

            // Set the conversion profile width - it must be multiple of 16
            int newWidth = VideoWidth;
            if (newWidth > _maxWidth)
                newWidth = _maxWidth; // We use the less of two, MaxWidth of post cropping video width
            newWidth = Util.MathLib.RoundOff(newWidth, 16);

            string scaleCmd = newWidth.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":-10";
            ParameterReplaceOrInsertVideoFilter("scale", "=" + scaleCmd);
        }

        protected override void SetVideoCropping()
        {
            if (!_skipCropping)
            {
                if (ParameterValue("-ovc") == "copy") // For copy video stream don't set video prcoessing parameters, it can break the conversion
                {
                    _jobLog.WriteEntry(this, "Video Copy codec detected, skipping cropping", Log.LogEntryType.Warning);
                    return;
                }

                // Check if we need to run cropping
                if (String.IsNullOrEmpty(_videoFile.CropString))
                {
                    _jobStatus.CurrentAction = Localise.GetPhrase("Analyzing video information");
                    _videoFile.UpdateCropInfo(_jobLog);
                }

                // Check if we have a valid crop string
                if (!String.IsNullOrWhiteSpace(_videoFile.CropString))
                {
                    ParameterReplaceOrInsertVideoFilter("crop", "=" + _videoFile.CropString);
                    _jobLog.WriteEntry(this, Localise.GetPhrase("MEncoder setting up video cropping"), Log.LogEntryType.Information);
                }
                else
                    _jobLog.WriteEntry(this, Localise.GetPhrase("MEncoder found no video cropping"), Log.LogEntryType.Information);
            }
            else
                _jobLog.WriteEntry(this, Localise.GetPhrase("MEncoder Skipping video cropping"), Log.LogEntryType.Information);
        }

        protected override void SetAudioLanguage()
        {
            // First clear any audio track selection if asked
            if (_encoderChooseBestAudioTrack) // Nothing to do here, Mencoder does not support multiple audio tracks
                _jobLog.WriteEntry(this, "Letting mencoder choose best audio track: Nothing to do, mencoder does not support multiple audio tracks", Log.LogEntryType.Debug);

            if (_videoFile.AudioPID == -1)
                _jobLog.WriteEntry(this, Localise.GetPhrase("Cannot get Audio and Video stream details, continuing with default Audio Language selection"), Log.LogEntryType.Warning);
            else
            {
                if (String.IsNullOrWhiteSpace(ParameterValue("-aid")))
                {
                    _jobLog.WriteEntry(this, "Selecting audio PID " + _videoFile.AudioPID.ToString(), Log.LogEntryType.Information);
                    ParameterValueReplaceOrInsert("-aid", (_videoFile.AudioPID).ToString(System.Globalization.CultureInfo.InvariantCulture)); // Select the Audio track PID we had isolated earlier
                }
                else
                    _jobLog.WriteEntry(this, Localise.GetPhrase("User has specified Audio language selection in profile, continuing without Audio Language selection"), Log.LogEntryType.Warning);
            }
        }

        private void SetAudioDelay()
        {
            if (_audioDelay != "skip") // Check if the user doesn't want us to fix the Audio Delay
            {
                double netAudioDelay = _toolAudioDelay; // Account for the user Audio Delay 
                if (netAudioDelay != 0) // MEncoder hack to speed things up and fix AudioDelay while encoding itself
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder enabling Audio Delay correction during conversion ") + netAudioDelay.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Information);
                    //_cmdParams = _cmdParams.Trim() + " -delay " + _videoFile.AudioDelay.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    ParameterValueReplaceOrInsert("-delay", netAudioDelay.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    _videoFile.AudioDelaySet = true;
                }
                else
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder net Audio Delay correction is 0, skipping during conversion"), Log.LogEntryType.Information);
            }
            else
                _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder Audio Delay correction disabled during conversion"), Log.LogEntryType.Information);
        }

        protected override void SetAudioPreDRC()
        {
            // Nothing, mencoder supports postDRC
        }

        protected override void SetAudioPostDRC()
        {
            ParameterValueReplaceOrInsert("-a52drc", DRC.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        protected override void SetAudioVolume()
        {
            ParameterReplaceOrInsertAudioFilter("volume", "=" + _volume.ToString("#0.0", System.Globalization.CultureInfo.InvariantCulture) + ":0"); // it has to be a floating point number in DB
        }

        protected override void SetAudioChannels()
        {
            if (ParameterValue("-oac") == "copy") // copy is not compatible with -channels
            {
                _jobLog.WriteEntry(this, "Skipping over requested to set audio channel information either due to COPY codec", Log.LogEntryType.Warning);
                return;
            }

            if (_2ChannelAudio) // Fix output to 2 channels
            {
                _jobLog.WriteEntry(this, ("Requested to limit Audio Channels to 2"), Log.LogEntryType.Information);
                ParameterValueReplaceOrInsert("-channels", "2");
            }
            else if (_videoFile.AudioChannels > 0 && (ParameterValue("-channels") == "")) // don't override the Audio Params if specified
            {
                if ((ParameterValue("-oac") == "mp3lame") && (ParameterValue("-channels") == "")) // Allow for MP3 channel override
                {
                    _jobLog.WriteEntry(this, ("Mencoder MP3 Lame Audio Codec detected, settings Audio Channels to 2"), Log.LogEntryType.Information);
                    ParameterValueReplaceOrInsert("-channels", "2"); // libmp3lame does not support > 2 channels as of 6/1/14
                }
                else
                {
                    _jobLog.WriteEntry(this, ("Setting audio channels to") + " " + _videoFile.AudioChannels.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Information);
                    ParameterValueReplaceOrInsert("-channels", _videoFile.AudioChannels.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            else if (ParameterValue("-channels") == "") // don't override the Audio Params if specified, since an audio track is not selected and channel information is not known default back to 6 channel audio (since stereo is not selected)
            {
                _jobLog.WriteEntry(this, "Did not find Audio Channel information, setting default audio channels to 6", Log.LogEntryType.Information);
                ParameterValueReplaceOrInsert("-channels", "6");
            }
            else
                _jobLog.WriteEntry(this, Localise.GetPhrase("Skipping over requested to set audio channel information because audio parameters already contains channel directive"), Log.LogEntryType.Warning);
        }

        protected override void SetInputFileName()
        {
            _cmdParams = Util.FilePaths.FixSpaces(SourceVideo) + " " + _cmdParams.Trim();
            //SetAudioDelay();   // -delay seems to fail
        }

        private void SetAdRemoval()
        {
            if (!mEncoderEDLSkip)
            {
                // Mencoder exception hack to speed things up - remove the ads at transcode time
                _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder: Checking if advertisement removal is required"), Log.LogEntryType.Information);
                if (_commercialScan != null & !_videoFile.AdsRemoved) // check if Commercial Stripping has been enabled AND not done since this function is called for all conversions
                {
                    if (_commercialSkipCut) // do not video removal if we are asked to skip cutting: ie. preserve the EDL file
                        _jobLog.WriteEntry(this, Localise.GetPhrase("MEncoder: Requested to skip cutting commercial"), Log.LogEntryType.Information);
                    else if (_commercialScan.CommercialsFound && File.Exists(_commercialScan.EDLFile))
                    {
                        /*if (Util.FilePaths.CleanExt(SourceVideo) == ".ts")
                        {
                            _jobLog.WriteEntry(this, Localise.GetPhrase("MEncoder: .TS source filed detected, using -mc 0 option for commercial skipping"), Log.LogEntryType.Debug);
                            _cmdParams = _cmdParams.Trim() + " -mc 0 -hr-edl-seek -edl " + Util.FilePaths.FixSpaces(_commercialScan.EDLFile); // use -mc 0 otherwise the audio goes out of sync: http://www.mplayerhq.hu/DOCS/HTML/en/menc-feat-dvd-mpeg4.html
                        }
                        else
                        {
                            _jobLog.WriteEntry(this, Localise.GetPhrase("MEncoder: Non .TS source filed detected, skipping -mc 0 option for commercial skipping"), Log.LogEntryType.Debug);
                            _cmdParams = _cmdParams.Trim() + " -hr-edl-seek -edl " + Util.FilePaths.FixSpaces(_commercialScan.EDLFile); // hr-edl-seek causes the audio to go out of sync sometimes, do not use
                        }*/
                        _cmdParams = _cmdParams.Trim() + " -edl " + Util.FilePaths.FixSpaces(_commercialScan.EDLFile); // keeping it simple for 95% of the videos, need to figure out exception conditions

                        _removedAds = true;
                        _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder setting advertisement removal with EDL file") + " : " + _commercialScan.EDLFile, Log.LogEntryType.Information);
                    }
                    else
                        _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder EDL commercial removal during conversion, EDL File not found, skipping commercial removal through MEncoder"), Log.LogEntryType.Warning);
                }
            }
            else
                _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder EDL commercial removal disabled during conversion"), Log.LogEntryType.Information);
        }

        protected override void SetOutputFileName()
        {
            // TODO: SetAdRemoval here causes the audio to out of sync slightly, so a compensation has been added to profile 0.185 seconds adjustment. Need to double check if there are any other issues (check -mc 0 and -noskip options if required)
            SetAdRemoval(); // EDL command just before the output file command, strip the commericals here itself since we are using Mencoder to avoid a duplication
            _cmdParams = _cmdParams.Trim() + " -o " + Util.FilePaths.FixSpaces(_convertedFile);
        }

        protected override bool ConvertWithTool()
        {
            Mencoder me;

            //Check if threads are specified else add multithreaded decoding, lavdopts supports a max of 8 threads
            if (String.IsNullOrWhiteSpace(ParameterSubValue("-lavdopts", "threads")))
            {
                ParameterSubValueReplaceOrInsert("-lavdopts", "threads", "=" + Math.Min(8, Environment.ProcessorCount).ToString(System.Globalization.CultureInfo.InvariantCulture));
                _jobLog.WriteEntry(this, Localise.GetPhrase("Adding decoding threaded support for") + " " + Environment.ProcessorCount + " Processors", Log.LogEntryType.Information);
            }
            else
                _jobLog.WriteEntry(this, Localise.GetPhrase("Decoding threaded support enabled within profile parameters"), Log.LogEntryType.Debug);

            //Check for use of threads with lavcopts and add multithreaded support if not there, max 8 threads supported
            if (ParameterValue("-ovc") == "lavc")
            {
                if (String.IsNullOrWhiteSpace(ParameterSubValue("-lavcopts", "threads")))
                {
                    ParameterSubValueReplaceOrInsert("-lavcopts", "threads", "=" + Math.Min(8, Environment.ProcessorCount).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Adding lavc threaded support for") + " " + Environment.ProcessorCount + " Processors", Log.LogEntryType.Information);
                }
                else
                    _jobLog.WriteEntry(this, Localise.GetPhrase("lavc threaded support present"), Log.LogEntryType.Debug);
            }

            //Check for use of threads with x264encopts and add auto multithreaded support if not there, 0 threads = auto
            if (ParameterValue("-ovc") == "x264")
            {
                if(String.IsNullOrWhiteSpace(ParameterSubValue("-x264encopts", "threads")))
                {
                    ParameterSubValueReplaceOrInsert("-x264encopts", "threads", "=0");
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Adding x264enc auto threaded support"), Log.LogEntryType.Information);
                }
                else
                    _jobLog.WriteEntry(this, Localise.GetPhrase("x264enc threaded support present"), Log.LogEntryType.Debug);
            }

            //Check for use of threads with xvidencopts and add auto multithreaded support if not there, 0 Thread = Auto
            if (ParameterValue("-ovc") == "xvidenc")
            {
                if (String.IsNullOrWhiteSpace(ParameterSubValue("-xvidencopts", "threads")))
                {
                    ParameterSubValueReplaceOrInsert("-xvidencopts", "threads", "=0");
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Adding xvidenc threaded support for") + " " + Environment.ProcessorCount + " Processors", Log.LogEntryType.Information);
                }
                else
                    _jobLog.WriteEntry(this, Localise.GetPhrase("xvidenc threaded support present"), Log.LogEntryType.Debug);
            }

            if (_2Pass == true)
            {
                string param = "";
                string subPararm = "";

                if (ParameterValue("x264encopts") != "")
                {
                    param = "-x264encopts";
                    subPararm = "pass";
                }
                else if (ParameterValue("xvidencopts") != "")
                {
                    param = "-xvidencopts";
                    subPararm = "pass";
                }
                else if (ParameterValue("lavcopts") != "")
                {
                    param = "-lavcopts";
                    subPararm = "vpass";
                }

                if (param != "")
                {
                    // 1s Pass
                    string baseParam = _cmdParams;
                    string passLog = Path.Combine(_workingPath, "MCEBuddy2Pass.log");
                    ParameterSubValueReplaceOrInsert(param, subPararm, "=1");
                    ParameterSubValueReplaceOrInsert(param, "turbo", "");
                    ParameterValueReplace("-o", "NUL");

                    _jobStatus.CurrentAction = Localise.GetPhrase("Converting video file - Pass 1");

                    me = new Mencoder(_cmdParams + " -passlogfile " + Util.FilePaths.FixSpaces(passLog), _jobStatus, _jobLog, false);
                    me.Run();
                    if (!me.Success)
                    {
                        _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder Pass 1 conversion failed"), Log.LogEntryType.Error);
                        _jobStatus.ErrorMsg = "Mencoder Pass 1 conversion failed";
                        return false;
                    }

                    // 2nd Pass
                    _cmdParams = baseParam;
                    ParameterSubValueReplaceOrInsert(param, subPararm, "=2");

                    _jobStatus.CurrentAction = Localise.GetPhrase("Converting video file - Pass 2");

                    me = new Mencoder(_cmdParams + " -passlogfile " + Util.FilePaths.FixSpaces(passLog), _jobStatus, _jobLog, false);
                    me.Run();
                    if (!me.Success)
                    {
                        _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder Pass 2 conversion failed"), Log.LogEntryType.Error);
                        _jobStatus.ErrorMsg = "Mencoder Pass 2 conversion failed";
                        return false;
                    }
                }
                else
                {
                    _jobStatus.CurrentAction = Localise.GetPhrase("Converting video file");

                    me = new Mencoder(_cmdParams, _jobStatus, _jobLog, false);
                    me.Run();
                    if (!me.Success)
                    {
                        _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder 2 pass no param conversion failed"), Log.LogEntryType.Error);
                        _jobStatus.ErrorMsg = "Mencoder 2 pass no param conversion failed";
                        return false;
                    }
                }
            }
            else
            {
                _jobStatus.CurrentAction = Localise.GetPhrase("Converting video file");
                me = new Mencoder(_cmdParams, _jobStatus, _jobLog, false);
                me.Run();
                if (!me.Success)
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Mencoder conversion failed"), Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "Mencoder conversion failed";
                    return false;
                }
            }
            return (me.Success);
        }
    }
}
