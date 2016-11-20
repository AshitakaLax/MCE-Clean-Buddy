using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

using MCEBuddy.AppWrapper;
using MCEBuddy.Util;
using MCEBuddy.Util.Combinatorics;
using MCEBuddy.Globals;
using MCEBuddy.VideoProperties;
using MCEBuddy.CommercialScan;

namespace MCEBuddy.Transcode
{
    public abstract class ConvertBase : FFMpegMEncoderParams
    {
        protected const int DEFAULT_VIDEO_WIDTH = 720;

        protected string _generalParams = "";
        protected string _videoParams = "";
        protected string _audioParams = "";

        protected string _extension = "";
        protected string _remuxTo = "";
        protected string _convertedFile = "";
        protected string _workingPath = "";

        protected VideoInfo _videoFile;
        protected Scanner _commercialScan;
        protected int _maxWidth = 1000000;
        protected double _bitrateResolutionQuality = 1; // Quality adjustment to bitrate by changing the resolution
        protected double _userQuality = 1; // Quality as set bthe user
        protected bool _videoOptimized = false;
        protected bool _audioOptimized = false;
        protected bool _fixedResolution = false;
        protected bool _skipCropping = false;
        protected bool _2Pass = false;
        protected bool _2ChannelAudio = false;
        protected bool _drc = false;
        protected double _volume = 1;
        protected int _startTrim = 0;
        protected int _endTrim = 0;
        protected bool _encoderChooseBestAudioTrack = false;
        protected bool _autoDeInterlacing = false;
        protected bool _commercialSkipCut = false;
        protected double _toolAudioDelay = 0;
        protected string _audioDelay = "";
        protected string _fps = ""; // output framerate

        protected bool _unsupported = false;
        protected bool _removedAds = false;
        protected bool _isPresetVideoWidth = false;
        protected bool _subtitleBurned = false;
        protected bool _preferHardwareEncoding = false;

        protected JobStatus _jobStatus;
        protected Log _jobLog;

        private string _renameConvertedFileWithOriginalName = ""; // Keep track incase of filename conflict

        protected ConvertBase(ConversionJobOptions conversionOptions, string tool, VideoInfo videoFile, JobStatus jobStatus, Log jobLog, Scanner commercialScan)
            : base (true)
        {
            //Setup log and status
            _jobStatus = jobStatus;
            _jobLog = jobLog;

            //Set the destination paths
            _workingPath = conversionOptions.workingPath;
            Util.FilePaths.CreateDir(_workingPath);

            // Check first up to see if the source video uses an unsupported combination for this profile
            // Container, Video Codec, Audio Codec and whether it was originally a Media Center recording or not
            _videoFile = videoFile;
            _commercialScan = commercialScan;

            if (CheckUnsupported(conversionOptions.profile, tool))
                return;

            // Set the input params and get the standard settings
            _maxWidth = conversionOptions.maxWidth;
            _userQuality = conversionOptions.qualityMultiplier;
            _volume = conversionOptions.volumeMultiplier;
            _drc = conversionOptions.drc;
            if (!_videoFile.TrimmingDone) // Check if we need to do trimming
            {
                _startTrim = conversionOptions.startTrim;
                _endTrim = conversionOptions.endTrim;
            }
            else
                _startTrim = _endTrim = 0;

            _encoderChooseBestAudioTrack = conversionOptions.encoderSelectBestAudioTrack;
            _fps = conversionOptions.FPS;
            _preferHardwareEncoding = conversionOptions.preferHardwareEncoding;

            Ini ini = new Ini(GlobalDefs.ProfileFile);

            // Profile override parameters - if default (i.e. does not exist then use conversion options else use profile parameters)
            if (ini.ReadString(conversionOptions.profile, "2ChannelAudio", "default") == "default")
                _2ChannelAudio = conversionOptions.stereoAudio;
            else
                _2ChannelAudio = ini.ReadBoolean(conversionOptions.profile, "2ChannelAudio", false); // Fix output to 2 channels (from profile)
            
            if (ini.ReadString(conversionOptions.profile, "SkipCropping", "default") == "default")
                _skipCropping = conversionOptions.disableCropping;
            else
                _skipCropping = ini.ReadBoolean(conversionOptions.profile, "SkipCropping", false); // Cropping can be forced in the profile
            
            if (ini.ReadString(conversionOptions.profile, "AutoDeinterlace", "default") == "default")
                _autoDeInterlacing = conversionOptions.autoDeInterlace;
            else
                _autoDeInterlacing = ini.ReadBoolean(conversionOptions.profile, "AutoDeinterlace", false);

            if (conversionOptions.renameOnly)
                _commercialSkipCut = true; //no cutting if we are renaming only
            else if (ini.ReadString(conversionOptions.profile, "CommercialSkipCut", "default") == "default")
                _commercialSkipCut = conversionOptions.commercialSkipCut;
            else _commercialSkipCut = ini.ReadBoolean(conversionOptions.profile, "CommercialSkipCut", false);

            // Profile only parameters
            _fixedResolution = ini.ReadBoolean(conversionOptions.profile, "FixedResolution", false);
            _2Pass = ini.ReadBoolean(conversionOptions.profile, "2pass", false);
            _generalParams = ini.ReadString(conversionOptions.profile, tool + "-general", "");
            _videoParams = ini.ReadString(conversionOptions.profile, tool + "-video", "");
            _audioParams = ini.ReadString(conversionOptions.profile, tool + "-audio", "");
            _extension = _videoFile.Extension = ini.ReadString(conversionOptions.profile, tool + "-ext", "").ToLower().Trim();
            if (string.IsNullOrWhiteSpace(_extension)) // Special case copy converter if there is no specified extension, it will be using the source file extension
                _extension = FilePaths.CleanExt(SourceVideo);
            _remuxTo = _videoFile.RemuxTo = ini.ReadString(conversionOptions.profile, tool + "-remuxto", "").ToLower().Trim();
            _audioDelay = ini.ReadString(conversionOptions.profile, tool + "-audiodelay", "skip").ToLower().Trim();
            _videoOptimized = ini.ReadBoolean(conversionOptions.profile, tool + "-VideoOptimized", false);
            _audioOptimized = ini.ReadBoolean(conversionOptions.profile, tool + "-AudioOptimized", false);

            if (_audioDelay == "auto") // Use the audio delay specified in the file
                _toolAudioDelay = videoFile.AudioDelay;
            else if (_audioDelay != "skip")
                double.TryParse(_audioDelay, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _toolAudioDelay);

            if (conversionOptions.audioOffset != 0) // Conversion options Audio Delay takes priority over profile Audio Delay
                _toolAudioDelay = conversionOptions.audioOffset; 

            // Audio select the AC3 audio option if the source video has AC3)
            if (((videoFile.AudioCodec == "ac-3") || (videoFile.AudioCodec == "ac3") || (videoFile.AudioCodec != "e-ac-3") || (videoFile.AudioCodec != "eac3")) && (ini.ReadString(conversionOptions.profile, tool + "-audioac3", "") != ""))
                _audioParams = ini.ReadString(conversionOptions.profile, tool + "-audioac3", _audioParams);

            // E-AC3 test option if the source video has E-AC3 - Not required as mencoder can handle eac3 audio
            /*
            if (videoFile.AudioCodec == "e-ac-3" || _videoFile.AudioCodec != "eac3")
            {
                _audioParams = ini.ReadString(conversionOptions.profile, tool + "-audioeac3", _audioParams);
                if ((_audioParams == "") && (tool == "mencoder"))
                    _audioParams = "-noaudio ";
            }*/

            // Important to use temp name while converting - sometimes the sources files are copied to the working directory and the converted files conflict with teh original filename, compensate. E.g. TS file input, TS file output with Copy converter
            _convertedFile = Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(SourceVideo) + "-converted" + _extension);
            _renameConvertedFileWithOriginalName = Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(SourceVideo) + _extension);
        }

        public bool Convert() // THE MAIN CONVERSION ROUTINE
        {
            if (_unsupported)
                return false;

            _jobLog.WriteEntry(this, "Main conversion routine DEBUG", Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Source Video File : " + _videoFile.SourceVideo, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Extension : " + _extension, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Remux To : " + _remuxTo, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Auto Enable Hardware Encoding : " + _preferHardwareEncoding.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "2 Pass : " + _2Pass.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Fixed Resolution : " + _fixedResolution.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Video Audio Delay : " + _videoFile.AudioDelay.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Manual Audio Delay : " + _toolAudioDelay.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Skip Audio Delay : " + _audioDelay, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Max Width : " + _maxWidth.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "User Quality : " + _userQuality.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Audio Track : " + _videoFile.AudioTrack.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Start Trim : " + _startTrim.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Stop Trim : " + _endTrim.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Output FPS : " + _fps, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Audio profile optimized by user : " + _audioOptimized.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Video profile optimized by user : " + _videoOptimized.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Skip Cropping (profile (SkipCropping) + task) : " + _skipCropping.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Stereo force (profile (2ChannelAudio) + task) : " + _2ChannelAudio.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Auto DeInterlacing (profile (AutoDeinterlace) + task) : " + _autoDeInterlacing.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Skip Cutting Commercials (profile (CommercialSkipCut) + task) : " + _commercialSkipCut.ToString(), Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Working Path : " + _workingPath, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Temp Converted File : " + _convertedFile, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, "Final Converted File : " + _renameConvertedFileWithOriginalName, Log.LogEntryType.Debug);
            _jobLog.WriteEntry(this, ("Source video file, file size [KB]") + " " + (FileIO.FileSize(SourceVideo) / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            // WAY TO BUILD THE COMMAND LINE
            // GeneralParameters + InputFile + VideoOptions + AudioOptions + OutputFile

            if (_audioOptimized)
                _jobLog.WriteEntry(this, ("Audio profile optimized by user, skipping audio adjustments"), Log.LogEntryType.Warning);

            if (_videoOptimized)
                _jobLog.WriteEntry(this, ("Video profile optimized by user, skipping video adjustments"), Log.LogEntryType.Warning);

            // GENERAL PARAMETERS
            _jobLog.WriteEntry(this, ("Setting up General conversion parameters :") + " " + _generalParams, Log.LogEntryType.Information);
            AppendParameters(_generalParams); // General Parameters

            if (!_audioOptimized) // Don't process audio if the profile is optimized
            {
                // Set the DRC before the input file for some encoders like ffmpeg
                if (_drc)
                {
                    if ((_videoFile.AudioCodec == "ac-3") || (_videoFile.AudioCodec == "ac3") || (_videoFile.AudioCodec != "e-ac-3") || (_videoFile.AudioCodec != "eac3")) // DRC only applies to AC3 audio
                    {
                        _jobLog.WriteEntry(this, ("Setting up PreDRC"), Log.LogEntryType.Information);

                        if (_audioParams.ToLower().Contains("copy"))
                            _jobLog.WriteEntry(this, ("Copy Audio stream detected, skipping DRC settings"), Log.LogEntryType.Warning);
                        else
                            SetAudioPreDRC(); // do this before we BEFORE setting the input filename
                    }
                    else
                        _jobLog.WriteEntry(this, ("Non AC3 Source Audio, DRC not applicable"), Log.LogEntryType.Information);
                }
            }

            // INPUT FILE
            _jobLog.WriteEntry(this, ("Setting up input file name parameters"), Log.LogEntryType.Information);
            SetInputFileName(); // Input Filename

            // VIDEO PARAMETERS
            _jobLog.WriteEntry(this, ("Setting up video conversion parameters :") + " " + _videoParams, Log.LogEntryType.Information);
            AppendParameters(_videoParams); // Video Parameters

            // Get the value of the preset width before we start modifying
            _isPresetVideoWidth = IsPresetVideoWidth();
            _jobLog.WriteEntry(this, "Is preset video size -> " + _isPresetVideoWidth.ToString(), Log.LogEntryType.Information);

            // Set the start and end trim parameters (after the video parameters) if trimming is not already done
            if ((_startTrim != 0) || (_endTrim != 0))
            {
                bool trimFile = false;
                _jobLog.WriteEntry(this, "Setting up trim parameters", Log.LogEntryType.Information);
                
                //Sanity checking if we need to trim
                if (_startTrim != 0)
                {
                    if (_startTrim < _videoFile.Duration)
                        trimFile = true;
                    else
                    {
                        _jobLog.WriteEntry(this, "Start trim (" + _startTrim.ToString() + ") greater than file duration (" + _videoFile.Duration.ToString(System.Globalization.CultureInfo.InvariantCulture) + "). Skipping start trimming.", Log.LogEntryType.Warning);
                        _startTrim = 0;
                    }
                }

                if (_endTrim != 0)
                {
                    int encDuration = (((int)_videoFile.Duration) - _endTrim) - (_startTrim); // by default _startTrim is 0
                    if (encDuration > 0)
                        trimFile = true;
                    else
                    {
                        _jobLog.WriteEntry(this, "End trim (" + _endTrim.ToString() + ") + Start trim (" + _startTrim.ToString() + ") greater than file duration (" + _videoFile.Duration.ToString(System.Globalization.CultureInfo.InvariantCulture) + "). Skipping end trimming.", Log.LogEntryType.Warning);
                        _endTrim = 0;
                    }
                }

                if (trimFile)
                    SetVideoTrim(); // Set Trim Parameters
                else
                    _jobLog.WriteEntry(this, "Start trim and end trim skipped. Skipping trimming.", Log.LogEntryType.Warning);
            }

            if (!_videoOptimized) // Don't process video if the profile is optimized
            {
                // Set the interlacing filters as the first filter in the chain, before processing anything
                SetVideoDeInterlacing();

                // Set the pre cropping BEFORE scaling, filter chain rule
                _jobLog.WriteEntry(this, "Setting up crop parameters", Log.LogEntryType.Information);
                SetVideoCropping();

                //Set the scaling
                if (!_fixedResolution && !_isPresetVideoWidth)
                {
                    int VideoWidth = _videoFile.Width;

                    _jobLog.WriteEntry(this, ("Checking if video resizing required"), Log.LogEntryType.Information);
                    if ((_videoFile.CropWidth > 0) && (_videoFile.CropWidth < _videoFile.Width))
                    {
                        VideoWidth = _videoFile.CropWidth;
                    }
                    // If we do not need to scale, don't
                    if (VideoWidth > _maxWidth)
                    {
                        _jobLog.WriteEntry(this, ("Setting up video resize parameters"), Log.LogEntryType.Information);
                        SetVideoResize(); // Set Resize parameters
                    }
                }
                else
                    _jobLog.WriteEntry(this, ("Fixed resolution video, no resizing"), Log.LogEntryType.Information);

                // Sometimes we need to set the Aspect Ratio as the last parameter in the video filter chain (e.g. with libxvid)
                _jobLog.WriteEntry(this, ("Setting up aspect ratio if required"), Log.LogEntryType.Information);
                SetVideoAspectRatio();

                // Adjust the bitrate to compensate for resizing and cropping
                _jobLog.WriteEntry(this, ("Setting up bitrate and quality parameters"), Log.LogEntryType.Information);
                AdjustResizeCropBitrateQuality();
                if (_userQuality <= 0.1)
                    _userQuality = 0.1;

                // If we using quality instead of bitrate
                if (ConstantVideoQuality && (_userQuality > 2))
                    _userQuality = 2;

                // Update the BitRate/Quality parameters
                SetVideoBitrateAndQuality();

                // Set the output framerate if required
                SetVideoOutputFrameRate();
            }

            // AUDIO PARAMETERS
            _jobLog.WriteEntry(this, ("Setting up audio conversion parameters :") + " " + _audioParams, Log.LogEntryType.Information);
            AppendParameters(_audioParams); // Audio Parameters

            if (!_audioOptimized) // Don't process audio if the profile is optimized
            {
                // Select the audio language specified by the user if multiple audio languages exist
                if ((!String.IsNullOrEmpty(_videoFile.RequestedAudioLanguage) || _encoderChooseBestAudioTrack) && (_videoFile.FFMPEGStreamInfo.AudioTracks > 1)) // check if we were requested to isolate a language manually and if there is more than one audio track
                {
                    _jobLog.WriteEntry(this, ("Selecting Audio Track :") + " " + _videoFile.AudioTrack.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Information);
                    SetAudioLanguage(); // Select the right Audio Track if required, do this before we AFTER setting the audio parameters
                }
                else
                    _jobLog.WriteEntry(this, ("Skipping over Audio Track selection, no language request or only one Audio Track found"), Log.LogEntryType.Information);

                // Set the volume
                if (_volume != 0) // volume is in dB (0dB is no change)
                {
                    _jobLog.WriteEntry(this, ("Setting up volume adjustment :") + " " + _volume.ToString("#0.0", System.Globalization.CultureInfo.InvariantCulture) + "dB", Log.LogEntryType.Information);

                    if (_audioParams.ToLower().Contains("copy"))
                        _jobLog.WriteEntry(this, ("Copy Audio stream detected, skipping volume settings"), Log.LogEntryType.Warning);
                    else
                        SetAudioVolume(); // do this before we AFTER setting the audio parameters
                }

                // Set the DRC with the remaining audio options for most encoders (except ffmpeg)
                if (_drc)
                {
                    if ((_videoFile.AudioCodec == "ac-3") || (_videoFile.AudioCodec == "ac3") || (_videoFile.AudioCodec != "e-ac-3") || (_videoFile.AudioCodec != "eac3")) // DRC only applies to AC3 audio
                    {
                        _jobLog.WriteEntry(this, ("Setting up PostDRC"), Log.LogEntryType.Information);

                        if (_audioParams.ToLower().Contains("copy"))
                            _jobLog.WriteEntry(this, ("Copy Audio stream detected, skipping DRC settings"), Log.LogEntryType.Warning);
                        else
                            SetAudioPostDRC(); // do this before we BEFORE setting the input filename
                    }
                    else
                        _jobLog.WriteEntry(this, ("Non AC3 Source Audio, DRC not applicable"), Log.LogEntryType.Information);
                }

                //Set audio channels
                _jobLog.WriteEntry(this, ("Setting up Audio channels"), Log.LogEntryType.Information);
                SetAudioChannels(); // Multi channel Audio
            }

            // OUTPUT FILE
            //Set the output file names
            _jobLog.WriteEntry(this, ("Setting up Output filename"), Log.LogEntryType.Information);
            SetOutputFileName();

            // Replace user specific parameters in the final command line
            _jobLog.WriteEntry(this, "Replacing user specified parameters", Log.LogEntryType.Information);
            ReplaceUserParameters();

            // One final santiy check on the parameters before calling the convert function
            FinalSanityCheck();

            //Convert the video - MAIN ONE
            if (!_jobStatus.Cancelled)
            {
                _jobLog.WriteEntry(this, ("Converting the video - Main conversion"), Log.LogEntryType.Information);
                bool ret = ConvertWithTool();
                _jobLog.WriteEntry(this, ("Conversion: Percentage Complete") + " " + _jobStatus.PercentageComplete.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                _jobLog.WriteEntry(this, ("Original file size [KB]") + " " + (FileIO.FileSize(SourceVideo) / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                _jobLog.WriteEntry(this, ("Finished video conversion, file size [KB]") + " " + (FileIO.FileSize(_convertedFile) / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                if (!ret) // with unsuccessful or incomplete
                {
                    _jobStatus.ErrorMsg = ("Conversion of video failed");
                    _jobLog.WriteEntry(this, (_jobStatus.ErrorMsg), Log.LogEntryType.Error);
                    return false;
                }
            }

            // EAC3 exception handling - redundant, not required as encoders can handle eac3 audio and function is incorrectly written
            /*
            if (!_jobStatus.Cancelled)
            {
                _jobLog.WriteEntry(this, ("Checking EAC3 Audio conversion"), Log.LogEntryType.Information);
                bool ret = ConvertEAC3();
                _jobLog.WriteEntry(this, ("EAC3 conversion: Percentage Complete") + " " + _jobStatus.PercentageComplete.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                _jobLog.WriteEntry(this, ("Finished EAC3 conversion, file size [KB]") + " " + (FileIO.FileSize(_convertedFile) / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                if (!ret || _jobStatus.PercentageComplete == 0 || (FileIO.FileSize(_convertedFile) <= 0)) // Check for total failure as some component like FFMPEG dont' return a correct %
                {
                    _jobStatus.ErrorMsg = ("Conversion of EAC3 failed");
                    _jobLog.WriteEntry(this, (_jobStatus.ErrorMsg), Log.LogEntryType.Error);
                    return false;
                }
            }*/

            // Set the audio delay post conversion
            if (!_jobStatus.Cancelled)
            {
                _jobLog.WriteEntry(this, ("Correcting Audio Delay if required"), Log.LogEntryType.Information);
                bool ret = FixAudioDelay();
                _jobLog.WriteEntry(this, ("Fix Audio Delay: Percentage Complete") + " " + _jobStatus.PercentageComplete.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                _jobLog.WriteEntry(this, ("Finished fixing audio delay, file size [KB]") + " " + (FileIO.FileSize(_convertedFile) / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                if (!ret || (FileIO.FileSize(_convertedFile) <= 0))
                {
                    _jobStatus.ErrorMsg = ("Fix AudioSync failed");
                    _jobLog.WriteEntry(this, (_jobStatus.ErrorMsg), Log.LogEntryType.Error);
                    return false;
                }
            }

            // Fix the converted filename conflict before remuxing
            if (!String.IsNullOrEmpty(_renameConvertedFileWithOriginalName))
            {
                try
                {
                    FileIO.TryFileDelete(_renameConvertedFileWithOriginalName); // Delete if the file with the replacement name exists (sometime with .TS file and TS profiles this happens)
                    FileIO.MoveAndInheritPermissions(_convertedFile, _renameConvertedFileWithOriginalName);
                    _convertedFile = _renameConvertedFileWithOriginalName;
                }
                catch (Exception e)
                {
                    _jobStatus.ErrorMsg = ("Unable to rename file after conversion");
                    _jobLog.WriteEntry(this, ("Unable to rename file after conversion"), Log.LogEntryType.Error);
                    _jobLog.WriteEntry(this, "Error -> " + e.ToString(), Log.LogEntryType.Error);
                    return false;
                }
            }

            // Remux to new Extension if required
            if (!_jobStatus.Cancelled)
            {
                _jobLog.WriteEntry(this, ("Remuxing video if required"), Log.LogEntryType.Information);

                double fps = 0;
                double.TryParse(_fps, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fps);
                RemuxExt remuxVideo = new RemuxExt(_convertedFile, _workingPath, (fps <= 0 ? _videoFile.Fps : fps), _jobStatus, _jobLog, _remuxTo); // Use output FPS if it exists otherwise the source file FPS (since it has not changed)
                bool ret = remuxVideo.RemuxFile();
                _convertedFile = remuxVideo.RemuxedFile;
                _jobLog.WriteEntry(this, ("Conversion Remux: Percentage Complete") + " " + _jobStatus.PercentageComplete.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                _jobLog.WriteEntry(this, ("Finished conversion remuxing video, file size [KB]") + " " + (FileIO.FileSize(_convertedFile) / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                if (!ret || (FileIO.FileSize(_convertedFile) <= 0)) // Check for total failure as some component like FFMPEG dont' return a correct %
                {
                    _jobStatus.ErrorMsg = ("Remux failed");
                    _jobLog.WriteEntry(this, (_jobStatus.ErrorMsg), Log.LogEntryType.Error);
                    return false;
                }
            }

            if (!_jobStatus.Cancelled)
            {
                if (!_videoFile.TrimmingDone)
                    _videoFile.TrimmingDone = true; // We have done trimming if it wasn't done earlier

                if (_removedAds) // We have successfully removed ad's
                    _videoFile.AdsRemoved = true;
                return true;
            }
            else
            {
                _jobStatus.ErrorMsg = ("Job cancelled, Aborting conversion");
                _jobLog.WriteEntry(this, ("Job cancelled, Aborting conversion"), Log.LogEntryType.Error);
                return false;
            }
        }


        private void AddCombinations(ref List<string> comboList)
        {
            List<string> originalList = new List<string>(comboList);

            for (int combinationCount = 2; combinationCount <= originalList.Count; combinationCount++)
            {
                var combos = new Util.Combinatorics.Combinations<string>(originalList, combinationCount);
                foreach (IList<string> combo in combos)
                {
                    var permutations = new Util.Combinatorics.Permutations<string>(combo); // Make all possible permutations now
                    foreach (IList<string> permutation in permutations)
                    {
                        string newPermutation = "";
                        foreach (string s in permutation)
                        {
                            if (newPermutation == "")
                                newPermutation = s;
                            else
                                newPermutation += "+" + s;
                        }

                        if (!comboList.Contains(newPermutation))
                            comboList.Add(newPermutation);
                    }
                }
            }
        }

        private bool CheckUnsupported(string profile, string tool)
        {
            _jobLog.WriteEntry(this, ("Checking for Unsupported profile for container / codec combination"), Log.LogEntryType.Information);
            Ini ini = new Ini(GlobalDefs.ProfileFile);
            string unsupportedCombinations = ini.ReadString(profile, tool + "-unsupported", "").ToLower().Trim();

            if (unsupportedCombinations != "")
            {
                List<string> videoItems = new List<string>();

                // Use properties for ORIGINAL video (not remuxed file, since the user is referring to original video properties, remuxed is always MPEG2 and TS)
                // Derive all possible combinations from the source video (video, audio and extension)
                videoItems.Add(_videoFile.OriginalVideoFFMPEGStreamInfo.MediaInfo.VideoInfo.VideoCodec);
                foreach (MediaInfo.Audio audioInfo in _videoFile.OriginalVideoFFMPEGStreamInfo.MediaInfo.AudioInfo) // check all audio codecs found
                    videoItems.Add(audioInfo.AudioCodec);
                videoItems.Add(_videoFile.OriginalFileExtension.ToLower().Replace(".", ""));

                AddCombinations(ref videoItems);

                // Get the unsupported list and check to see if there are any matches
                string[] unsupported = unsupportedCombinations.Split(',');
                foreach (string combo in unsupported)
                {
                    string c = combo.ToLower().Trim();
                    if (videoItems.Contains(c))
                    {
                        _jobLog.WriteEntry(this, 
                            ("Unsupported profile for container / codec combination") + " " + c + " " + profile,
                            Log.LogEntryType.Warning);
                        _unsupported = true;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Handle eac3 conversions for mencoder
        /// TODO: This function is redundant and incorrectly written, handbrake, ffmpeg and mencoder are able to handle eac3 audio why do we need this?
        /// </summary>
        private bool ConvertMencoderEAC3()
        {
            string audioStream = "";

            _jobStatus.PercentageComplete = 100; //all good by default
            _jobStatus.ETA = "";

            if ((_videoFile.AudioCodec != "e-ac-3") && (_videoFile.AudioCodec != "eac3"))
                return true;
            
            // Only supports MP4, MKV and AVI
            if ((_extension != ".mp4") && (_extension != ".mkv") && (_extension != ".avi"))
                return true;

            _jobStatus.CurrentAction = Localise.GetPhrase("Converting E-AC3");
            _jobLog.WriteEntry(this, ("Converting E-AC3"), Log.LogEntryType.Information);

            // Convert EAC3 file
            string eac3toParams;
            string audiop = _audioParams.Trim();
            if (audiop.Contains("faac") || audiop.Contains("libfaac") || audiop.Contains("aac"))
            {
                audioStream = Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(SourceVideo) + "_AUDIO.mp4");
                eac3toParams = Util.FilePaths.FixSpaces(SourceVideo) + " " + Util.FilePaths.FixSpaces(audioStream) + " -384";
            }
            else // TODO: what about other audio formats?
            {
                audioStream = Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(SourceVideo) + "_AUDIO.ac3");
                eac3toParams = Util.FilePaths.FixSpaces(SourceVideo) + " " + Util.FilePaths.FixSpaces(audioStream) + " -384";
            }

            FileIO.TryFileDelete(audioStream); // Clean before starting
            Eac3To eac3to = new AppWrapper.Eac3To(eac3toParams, _jobStatus, _jobLog);
            eac3to.Run();
            if (!eac3to.Success)
            {
                FileIO.TryFileDelete(audioStream); // Clean
                _jobLog.WriteEntry(this, ("E-AC3 conversion unsuccessful"), Log.LogEntryType.Error);
                _jobStatus.ErrorMsg = "E-AC3 conversion unsuccessful";
                _jobStatus.PercentageComplete = 0;
                return false; // something went wrong
            }

            // Mux into destination 
            if ((_extension == ".mp4") || (_extension == ".m4v"))
            {
                _jobLog.WriteEntry(this, ("Muxing E-AC3 using MP4Box"), Log.LogEntryType.Information);
                string mp4boxParams = " -keep-sys -keep-all -add " + FilePaths.FixSpaces(audioStream) + " " + FilePaths.FixSpaces(_convertedFile);
                _jobStatus.PercentageComplete = 0; //reset
                _jobStatus.ETA = "";
                MP4Box mp4box = new MP4Box(mp4boxParams, _jobStatus, _jobLog);
                mp4box.Run();
                if (!mp4box.Success || _jobStatus.PercentageComplete < GlobalDefs.ACCEPTABLE_COMPLETION) // check for incomplete output or process issues
                {
                    FileIO.TryFileDelete(audioStream);
                    _jobLog.WriteEntry(this, ("E-AC3 muxing using MP4Box failed at") + " " + _jobStatus.PercentageComplete.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "E-AC3 muxing using MP4Box failed";
                    _jobStatus.PercentageComplete = 0; // something went wrong with the process
                    return false;
                }
            }
            else if (_extension == ".mkv")
            {
                _jobLog.WriteEntry(this, ("Muxing E-AC3 using MKVMerge"), Log.LogEntryType.Information);
                string remuxedFile = Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(_convertedFile) + "_REMUX.mkv");
                FileIO.TryFileDelete(remuxedFile);
                string mkvmergeParams = "--clusters-in-meta-seek --compression -1:none " + FilePaths.FixSpaces(_convertedFile) + " --compression -1:none " + FilePaths.FixSpaces(audioStream) + " -o " + FilePaths.FixSpaces(remuxedFile);
                _jobStatus.PercentageComplete = 0; //reset
                _jobStatus.ETA = "";
                MKVMerge mkvmerge = new MKVMerge(mkvmergeParams, _jobStatus, _jobLog);
                mkvmerge.Run();
                if (!mkvmerge.Success)
                {
                    FileIO.TryFileDelete(audioStream);
                    FileIO.TryFileDelete(remuxedFile);
                    _jobLog.WriteEntry(this, ("Muxing E-AC3 using MKVMerge failed"), Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "Muxing E-AC3 using MKVMerge failed";
                    _jobStatus.PercentageComplete = 0; // something went wrong with the process
                    return false;
                }

                try
                {
                    _jobLog.WriteEntry(this, ("Moving MKVMerge muxed E-AC3"), Log.LogEntryType.Information);
                    FileIO.TryFileDelete(_convertedFile);
                    FileIO.MoveAndInheritPermissions(remuxedFile, _convertedFile);
                    _jobStatus.PercentageComplete = 100; //proxy for job done since mkvmerge doesn't report
                    _jobStatus.ETA = "";
                }
                catch (Exception e)
                {
                    FileIO.TryFileDelete(audioStream);
                    FileIO.TryFileDelete(remuxedFile);
                    _jobLog.WriteEntry(this, ("Unable to move MKVMerge remuxed E-AC3 file") + " " + remuxedFile + "\r\nError : " + e.ToString(), Log.LogEntryType.Error);
                    _jobStatus.PercentageComplete = 0;
                    _jobStatus.ErrorMsg = "Unable to move MKVMerge remuxed E-AC3 file";
                    return false;
                }
            }
            else
            {
                _jobStatus.PercentageComplete = 0; //reset
                _jobStatus.ETA = "";
                _jobLog.WriteEntry(this, ("Muxing E-AC3 using FFMPEGRemux"), Log.LogEntryType.Information);
                double fps = 0;
                double.TryParse(_fps, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fps);
                RemuxExt remuxFile = new RemuxExt(_convertedFile, _workingPath, (fps <= 0 ? _videoFile.Fps : fps), _jobStatus, _jobLog, _remuxTo); // Use output FPS if it exists otherwise the source file FPS (since it has not changed)
                if (remuxFile.FfmpegRemux(audioStream))
                {
                    _convertedFile = remuxFile.RemuxedFile;
                }
                else
                {
                    FileIO.TryFileDelete(audioStream);
                    _jobLog.WriteEntry(this, ("Error Muxing E-AC3 using FFMPEGRemux"), Log.LogEntryType.Error);
                    _jobStatus.PercentageComplete = 0;
                    _jobStatus.ErrorMsg = "Error Muxing E-AC3 using FFMPEGRemux";
                    return false;
                }
            }

            FileIO.TryFileDelete(audioStream); // Clean up
            _jobLog.WriteEntry(this, ("Finished EAC3 conversion, file size [KB]") + " " + (FileIO.FileSize(_convertedFile) / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            return true;
        }

        private bool FixAudioDelay()
        {
            string encoderParams;

            _jobStatus.PercentageComplete = 100; //all good to start with
            _jobStatus.ETA = "";

            if (_videoFile.AudioDelaySet || _toolAudioDelay == 0)
                return true; //It's already been done (probably by mencoder) or been requested to skip

            // Check if the converted file has Audio AND Video streams (if one is missing, then skip this step)
            FFmpegMediaInfo ffmpegInfo = new FFmpegMediaInfo(_convertedFile, _jobStatus, _jobLog);
            if (!ffmpegInfo.Success || ffmpegInfo.ParseError)
            {
                _jobStatus.PercentageComplete = 0; // if the file wasn't completely converted the percentage will be low so no worries
                _jobStatus.ErrorMsg = "Fix AudioSync getting mediainfo Failed for " + _convertedFile;
                _jobLog.WriteEntry(this, (_jobStatus.ErrorMsg), Log.LogEntryType.Error);
                return false;
            }

            if ((ffmpegInfo.MediaInfo.VideoInfo.Stream == -1) || (ffmpegInfo.AudioTracks < 1))
            {
                _jobLog.WriteEntry(this, "Fix audiosync, No video or no audio track detected - skipping audio sync", Log.LogEntryType.Warning);
                return true;
            }

            double audioDelay = _toolAudioDelay;

            if (audioDelay != 0)
            {
                _jobLog.WriteEntry(this, ("Fixing Audio Delay, Detected :") + " " + _videoFile.AudioDelay.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", Manual Adj : " + _toolAudioDelay.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Information);

                string ext = FilePaths.CleanExt(_convertedFile);
                string fixedFile = Path.Combine(_workingPath, Path.GetFileNameWithoutExtension(_convertedFile) + "_AVFIX" + FilePaths.CleanExt(_convertedFile));
                FileIO.TryFileDelete(fixedFile);

                _jobStatus.CurrentAction = Localise.GetPhrase("Correcting audio delay");

                switch (ext)
                {
                    case ".wmv":
                        _jobLog.WriteEntry(this, ("Using ASFBin to correct audio sync for extension ") + ext, Log.LogEntryType.Debug);

                        encoderParams = " -i " + Util.FilePaths.FixSpaces(_convertedFile) + " -adelay " + audioDelay.ToString(System.Globalization.CultureInfo.InvariantCulture) + " -o " + Util.FilePaths.FixSpaces(fixedFile) + " -y";
                        ASFBin asfBin = new ASFBin(encoderParams, _jobStatus, _jobLog);
                        asfBin.Run();
                        if (!asfBin.Success || (FileIO.FileSize(fixedFile) <= 0))
                        {
                            _jobStatus.ErrorMsg = "Fixing Audio Delay for WMV failed";
                            _jobLog.WriteEntry(this, ("Fixing Audio Delay for WMV failed"), Log.LogEntryType.Error);
                            _jobStatus.PercentageComplete = 0;
                            return false;
                        }

                        break;

                    case ".avi":
                        _jobLog.WriteEntry(this, ("Using Mencoder to correct audio sync for extension ") + ext, Log.LogEntryType.Debug);

                        encoderParams = Util.FilePaths.FixSpaces(_convertedFile) + " -oac copy -ovc copy -ni -delay " + (-1 * audioDelay).ToString(System.Globalization.CultureInfo.InvariantCulture) + " -o " + Util.FilePaths.FixSpaces(fixedFile.ToString(System.Globalization.CultureInfo.InvariantCulture)); // avoid using threads since we are copying to increase stability

                        _jobLog.WriteEntry(this, "Fixing Audio Delay using MEncoder with Parameters: " + encoderParams, Log.LogEntryType.Debug);
                        Mencoder mencoderAVI = new Mencoder(encoderParams, _jobStatus, _jobLog, false);
                        mencoderAVI.Run();
                        if (!mencoderAVI.Success) // something failed or was incomplete, do not check for % completion as Mencoder looks fro success criteria
                        {
                            _jobStatus.PercentageComplete = 0;
                            _jobStatus.ErrorMsg = ("Fix AudioSync failed for") + " " + ext;
                            _jobLog.WriteEntry(this, (_jobStatus.ErrorMsg), Log.LogEntryType.Error);
                            return false;
                        }

                        break;

                    default:
                        _jobLog.WriteEntry(this, ("Using FFMPEG to correct audio sync for extension ") + ext, Log.LogEntryType.Debug);

                        if (audioDelay > 0) // Map same file as 2 inputs, shift and take the audio in one and take the video from the other
                        {
                            encoderParams = "-y -i " + Util.FilePaths.FixSpaces(_convertedFile) +
                                            " -ss " + audioDelay.ToString(System.Globalization.CultureInfo.InvariantCulture) +" -i " + Util.FilePaths.FixSpaces(_convertedFile) +
                                            " -map 1:v -map 0:a -acodec copy -vcodec copy";

                            _jobLog.WriteEntry(this, "Fixing +ve Audio Delay using FFMPEG", Log.LogEntryType.Debug);

                        } // if audio is behind the video skip seconds from the 2nd input file and remap to ouput (keeping the audio shift positive)
                        else
                        {
                            encoderParams = "-y -ss " + (audioDelay * -1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " -i " + Util.FilePaths.FixSpaces(_convertedFile) +
                                            " -i " + Util.FilePaths.FixSpaces(_convertedFile) +
                                            " -map 1:v -map 0:a -acodec copy -vcodec copy";

                            _jobLog.WriteEntry(this, "Fixing -ve Audio Delay using FFMPEG", Log.LogEntryType.Debug);
                        }

                        encoderParams += " " + Util.FilePaths.FixSpaces(fixedFile);

                        if (!FFmpeg.FFMpegExecuteAndHandleErrors(encoderParams, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(fixedFile))) // Do not check for % completion since FFMPEG doesn't always report a % for this routine for some reason
                        {
                            _jobStatus.PercentageComplete = 0; // if the file wasn't completely converted the percentage will be low so no worries
                            _jobStatus.ErrorMsg = "Fix AudioSync Failed for " + ext;
                            _jobLog.WriteEntry(this, (_jobStatus.ErrorMsg), Log.LogEntryType.Error);
                            return false;
                        }

                        break;
                }
                
                try
                {
                    _jobLog.WriteEntry(this, ("Fix Audio Delay trying to move fixed file"), Log.LogEntryType.Information);
                    FileIO.TryFileDelete(_convertedFile);
                    FileIO.MoveAndInheritPermissions(fixedFile, _convertedFile);
                }
                catch (Exception e)
                {
                    _jobLog.WriteEntry(this, ("Unable to move audio sync corrected file") + " " + fixedFile + "\r\nError : " + e.ToString(), Log.LogEntryType.Error);
                    _jobStatus.ErrorMsg = "Unable to move audio sync file";
                    _jobStatus.PercentageComplete = 0;
                    return false;
                }
                _jobLog.WriteEntry(this, ("Finished Audio Delay Correction, file size [KB]") + " " + (FileIO.FileSize(_convertedFile) / 1024).ToString("N", System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            }
            else
                _jobLog.WriteEntry(this, ("Fix Audio Delay, net correction 0, skipping correction"), Log.LogEntryType.Information);

            return true;
        }

        private void ReplaceUserParameters()
        {
            _jobLog.WriteEntry(this, "Command line parameters -> " + _cmdParams, Log.LogEntryType.Debug);

            // Insert the source filename without extension
            _cmdParams = _cmdParams.Replace("<source_without_ext>", Util.FilePaths.GetFullPathWithoutExtension(SourceVideo));

            // Insert the source filename with extension
            _cmdParams = _cmdParams.Replace("<source>", SourceVideo);

            // Insert the output filename without extension
            _cmdParams = _cmdParams.Replace("<converted_without_ext>", Util.FilePaths.GetFullPathWithoutExtension(ConvertedFile));

            // Insert the output filename with extension
            _cmdParams = _cmdParams.Replace("<converted>", ConvertedFile);
        }

        /// <summary>
        /// Calculates adjustment to quality and bitrate due to changes in resolution and cropping
        /// </summary>
        private void AdjustResizeCropBitrateQuality()
        {
            if (ConstantVideoQuality) return;

            if (_fixedResolution || _isPresetVideoWidth) return; // The size has been hardcoded into the paramters and bitrate optimized, no need to recalculate here - if fixedresolution is not set, then the video may be resized if required and we need to recalculate the bitrate

            // Set the quality multiplier if you are using fixed bitrate
            int VideoWidth = _videoFile.Width;

            if ((_videoFile.CropWidth > 0) && (_videoFile.CropWidth < _videoFile.Width))
            {
                VideoWidth = _videoFile.CropWidth;
            }

            // If we are scaling down, then reduce the quality multiplier
            if (VideoWidth > _maxWidth ) VideoWidth = _maxWidth;

            _bitrateResolutionQuality = _bitrateResolutionQuality * (float)VideoWidth / (float)DEFAULT_VIDEO_WIDTH; // Here we adjust the bitrate quality due to changes in resolutin (not user specified quality)

            _jobLog.WriteEntry(this, "Adjusted bitrate quality due to changes in resolution (OriginalWidth " + _videoFile.Width.ToString() + ", CropWidth " + _videoFile.CropWidth.ToString() + ", MaxWidth " + _maxWidth.ToString() + ", FinalWidth " + VideoWidth.ToString() + ") by a factor of " +  _bitrateResolutionQuality.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
        }

        /// <summary>
        /// Adjust the constant quality scale based on % quality adjustment made by user
        /// </summary>
        /// <param name="lowQuality">Lowest quality value in scale</param>
        /// <param name="highQuality">Highest quality value in scale</param>
        /// <param name="qualCurrent">Current/profile quality value to adjust</param>
        /// <returns>Adjusted quality value</returns>
        protected int ConstantQualityValue(int lowQuality, int highQuality, int qualCurrent)
        {
            if (_userQuality == 1) return qualCurrent;

            int qualBoundary;
            double qualMult;

            // Chooose the boundary value to head to - remember highQuality  can be the LOWEST OR THE HIGHEST value 
            if (_userQuality < 1)
            {
                qualBoundary = lowQuality;
                qualMult = _userQuality;
            }
            else
            {
                qualBoundary = highQuality;
                qualMult = _userQuality - 1;
            }

            return (int)((qualBoundary - qualCurrent) * (qualMult)) + qualCurrent;
        }

        /// <summary>
        /// Input source recording name and path
        /// </summary>
        protected string SourceVideo
        {
            get { return _videoFile.SourceVideo; }
        }

        /// <summary>
        /// True if there is an unsupported combination of audio codec, video codec and extension in the source file
        /// </summary>
        public bool Unsupported
        {
            get { return _unsupported; }
        }

        /// <summary>
        /// Converted file name and path
        /// </summary>
        public string ConvertedFile
        {
            get { return _convertedFile; }
        }

        /// <summary>
        /// Indicates if subtitles were burnt into the video while converting
        /// </summary>
        public bool SubtitleBurned
        {
            get { return _subtitleBurned; }
        }

        // Interfaces for converters
        protected abstract bool ConvertWithTool();
        protected abstract void SetVideoCropping();
        protected abstract void SetVideoBitrateAndQuality();
        protected abstract void SetVideoResize();
        protected abstract void SetInputFileName();
        protected abstract void SetOutputFileName();
        protected abstract void SetAudioChannels();
        protected abstract void SetAudioLanguage();
        protected abstract bool ConstantVideoQuality { get; }
        protected abstract void SetAudioVolume();
        protected abstract void SetAudioPreDRC();
        protected abstract void SetAudioPostDRC();
        protected abstract void SetVideoAspectRatio();
        protected abstract void SetVideoTrim();
        protected abstract bool IsPresetVideoWidth();
        protected abstract void SetVideoDeInterlacing();
        protected abstract void FinalSanityCheck();
        protected abstract void SetVideoOutputFrameRate();
    }
}
