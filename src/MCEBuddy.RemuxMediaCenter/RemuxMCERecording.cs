using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using MCEBuddy.AppWrapper;
using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.Transcode;
using MCEBuddy.Configuration;

namespace MCEBuddy.RemuxMediaCenter
{
    public class RemuxMCERecording
    {
        enum FFMPEGRemuxType
        {
            Copy = 0,
            Recode = 1,
            Both = 2
        }

        private string[] _supportedCodecs = GlobalDefs.mpeg1Codecs.Union(GlobalDefs.mpeg2Codecs).Union(GlobalDefs.mpeg4Part2Codecs).Union(GlobalDefs.mpeg4Part10Codecs).Union(GlobalDefs.h263Codecs).ToArray();

        private string _RecordingFile;
        private string _RemuxedFile;
        private ExtractWithGraph _extract;
        protected JobStatus _jobStatus;
        protected Log _jobLog;
        private string _destinationPath;
        private string _requestedAudioLanguage = "";
        private bool _useRemuxsupp = false;
        private bool _allowH264CopyRemuxing = false;
        private bool _allowAllCopyRemuxing = false;
        private bool _forceWTVStreamsRemuxing = false;
        private string _tivoMAKKey = "";
        FFmpegMediaInfo _RecordingFileMediaInfo;
        private bool slowRemuxed = false;
        private float skipInitialSeconds = 0;

        /// <summary>
        /// Number of initial seconds skipping while remuxing the file
        /// </summary>
        public float SkipInitialSeconds
        { get { return skipInitialSeconds; } }

        /// <summary>
        /// Path to remuxed file
        /// </summary>
        public string RemuxedFile
        { get { return _RemuxedFile; } }

        /// <summary>
        /// True if the original video stream was recoded (not stream copied)
        /// </summary>
        public bool VideoStreamRecoded
        { get { return slowRemuxed; } }

        public RemuxMCERecording(ConversionJobOptions cjo, JobStatus jobStatus, Log jobLog)
        {
            _jobStatus = jobStatus;
            _jobLog = jobLog;
            _RecordingFile = cjo.sourceVideo;
            _destinationPath = cjo.workingPath;
            _requestedAudioLanguage = cjo.audioLanguage;
            _tivoMAKKey = cjo.tivoMAKKey;

            if (Util.FilePaths.CleanExt(_RecordingFile) == ".ts") // Handle TS files difference since they will have the same namess
                _RemuxedFile = Path.Combine(_destinationPath, Path.GetFileNameWithoutExtension(_RecordingFile) + "-REMUXED.ts");
            else
                _RemuxedFile = Path.Combine(_destinationPath, Path.GetFileNameWithoutExtension(_RecordingFile) + ".ts");

            // Read various profile parameters
            Ini configProfileIni = new Ini(GlobalDefs.ProfileFile);
            _useRemuxsupp = configProfileIni.ReadBoolean(cjo.profile, "UseWTVRemuxsupp", false); // Some videos fail with FFMPEG remuxing and Mencoder encoding (use remuxsupp for remuxing there)
            _jobLog.WriteEntry(this, "Force Remuxsupp (UseWTVRemuxsupp) : " + _useRemuxsupp.ToString(), Log.LogEntryType.Debug);

            _forceWTVStreamsRemuxing = configProfileIni.ReadBoolean(cjo.profile, "ForceWTVStreamsRemuxing", false); // Use Streams remuxing for DVRMS and WTV files
            _jobLog.WriteEntry(this, "Force Streams Remuxing (ForceWTVStreamsRemuxing) : " + _forceWTVStreamsRemuxing.ToString(), Log.LogEntryType.Debug);

            _allowH264CopyRemuxing = configProfileIni.ReadBoolean(cjo.profile, "AllowH264CopyRemuxing", true); // Allow H.264 files to be remuxed into TS without recoding to MPEG2
            _jobLog.WriteEntry(this, "Allow H264 Copy Remuxing (AllowH264CopyRemuxing) (default: true) : " + _allowH264CopyRemuxing.ToString(), Log.LogEntryType.Debug);

            _allowAllCopyRemuxing = configProfileIni.ReadBoolean(cjo.profile, "AllowAllCopyRemuxing", false); // Allow any video codec to be remuxed into TS without recoding to MPEG2
            _jobLog.WriteEntry(this, "Allow All Video codec formats Copy Remuxing (AllowAllCopyRemuxing) (default: false) : " + _allowAllCopyRemuxing.ToString(), Log.LogEntryType.Debug);

            // Get the media info for the recording file once for the entire operation to reuse
            _jobLog.WriteEntry(this, "Reading Recording file " + _RecordingFile + " media information", Log.LogEntryType.Debug);
            _RecordingFileMediaInfo = new FFmpegMediaInfo(_RecordingFile, _jobStatus, _jobLog);

            // Check for donator version of Comskip
            Comskip checkComskip = new Comskip(MCEBuddyConf.GlobalMCEConfig.GeneralOptions.comskipPath, _jobLog);

            // Check if we are using a mpeg4 video and allowing h264 video codec for commercial skipping purposes
            if (_allowH264CopyRemuxing)
            {
                if (GlobalDefs.mpeg4Part2Codecs.Union(GlobalDefs.mpeg4Part10Codecs).Union(GlobalDefs.h263Codecs).ToArray().Any(s => s.Contains(_RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec.ToLower())))
                {
                    if (cjo.commercialRemoval == CommercialRemovalOptions.Comskip)
                    {
                        if (checkComskip.IsDonator)
                            _jobLog.WriteEntry(this, "AllowH264CopyRemuxing will run fast for commercial detection, using donator version of Comskip", Log.LogEntryType.Information);
                        else
                            _jobLog.WriteEntry(this, "AllowH264CopyRemuxing is SLOW with the bundled Comskip. Use ShowAnalyzer or Comskip Donator version (http://www.comskip.org) to speed up commercial detection. Codec detected -> " + _RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec, Log.LogEntryType.Warning);
                    }
                }
            }

            // Check if we are using an unsupported codec and copying to TS format
            if (_allowAllCopyRemuxing)
                if (!_supportedCodecs.Any(s => s.Contains(_RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec.ToLower()))) // Check if we using any of the default supported codecs
                        _jobLog.WriteEntry(this, "AllowAllCopyRemuxing is enabled and an unsupported codec in the source video is detected. Some underlying programs may not work with this codec. Codec detected -> " + _RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec, Log.LogEntryType.Warning);
        }

        /// <summary>
        /// Check if the source video needs to be slow remuxed based on the codecs and remux configuration for mcebuddy
        /// </summary>
        /// <returns>True if a slow remux is required</returns>
        public bool SlowRemuxRequired()
        {
            if (!( // If not any of the below, then skip the copy remux
                        _allowAllCopyRemuxing || // Copy all
                        (_allowH264CopyRemuxing && GlobalDefs.mpeg4Part2Codecs.Union(GlobalDefs.mpeg4Part10Codecs).Union(GlobalDefs.h263Codecs).ToArray().Any(s => s.Contains(_RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec.ToLower()))) || // Copy h264
                        GlobalDefs.mpeg2Codecs.Any(s => s.Contains(_RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec.ToLower())) || // mpeg2
                        GlobalDefs.mpeg1Codecs.Any(s => s.Contains(_RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec.ToLower())) // mpeg1
                        ))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Fixed the name of the remuxed file to remove the -REMUXED at the end of it if the original file is a TS file
        /// </summary>
        private void FixTSRemuxedFileName()
        {
            if (Util.FilePaths.CleanExt(_RecordingFile) != ".ts")
                return;

            string fixedRemuxedFileName = Path.Combine(_destinationPath, Path.GetFileNameWithoutExtension(_RecordingFile) + ".ts");
            Util.FileIO.TryFileDelete(fixedRemuxedFileName);
            FileIO.MoveAndInheritPermissions(_RemuxedFile, fixedRemuxedFileName);
            _RemuxedFile = fixedRemuxedFileName;
        }

        /// <summary>
        /// Used to Remux files to MPEG TS files (with special handling for WTV, DVRMS and TiVO files)
        /// </summary>
        /// <returns></returns>
        public bool Remux()
        {
            _jobStatus.ErrorMsg = ""; // Start clean
            Util.FileIO.TryFileDelete(RemuxedFile);

            if (Util.FilePaths.CleanExt(_RecordingFile) == ".tivo") // Check for TiVO files
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Attempting to decode and remux TiVO file"), Log.LogEntryType.Information);
                _jobStatus.CurrentAction = Localise.GetPhrase("Remuxing TiVO file");

                // TODO: we need to relook at this, tivo filters still don't work in user space when launched from a service
                if (GlobalDefs.IsEngineRunningAsService)
                    _jobLog.WriteEntry(this, "You need to start MCEBuddy engine as a Command line program to for TiVO Fast Transfers or to use TiVO Desktop. TiVO Desktop Directshow decryption filters do not work through a Windows Service.", Log.LogEntryType.Warning);
                
                _jobLog.WriteEntry(this, "Starting RemuxTiVOStreams as a admin user program", Log.LogEntryType.Information);

                // Usage: RemuxTiVOStreams <TiVOFile> <TempPath> <MAK> <AudioLanguage> (each has to be enclosed in quotes)
                string tivoUserRemuxParams = FilePaths.FixSpaces(_RecordingFile) + " " + FilePaths.FixSpaces(_destinationPath) + " " + FilePaths.FixSpaces(_tivoMAKKey) + " " + FilePaths.FixSpaces(_requestedAudioLanguage);
                TiVOUserRemux tivoUserRemux = new TiVOUserRemux(tivoUserRemuxParams, _jobStatus, _jobLog);
                tivoUserRemux.Run();
                if (!tivoUserRemux.Success)
                {
                    _jobStatus.ErrorMsg = "Unable to remux TiVO using RemuxTiVOStreams";
                    _jobLog.WriteEntry(this, _jobStatus.ErrorMsg, Log.LogEntryType.Error);
                    return false; // There are no other options for remuxing hence return here
                }
                else
                    return true;
            }

            // If it's DVRMS first try the special version of FFMPEG, if that fails fall back to regular FFMPEG below
            if (Util.FilePaths.CleanExt(_RecordingFile) == ".dvr-ms") // DVRMS is always MPEG2 video
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Attempting directshow streams based DVRMS remux, extracting streams"), Log.LogEntryType.Information);
                _jobStatus.CurrentAction = Localise.GetPhrase("Extracting streams");

                // Try a streams based remux
                if (DirectShowExtractAndRemuxStreams())
                    return true; // this is pretty good, try first
                else
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Streams remuxing for DVRMS failed, trying FFMpegDVRMS Remuxing"), Log.LogEntryType.Warning);
                    _jobStatus.CurrentAction = Localise.GetPhrase("DVRMS Remuxing");

                    if (!RemuxDVRMSFFmpeg()) // Special FFMPEG for DVRMS succeded to create a ts mpeg file
                        _jobLog.WriteEntry(this, "Unable to remux DVRMS using DVRMSFFMpegRemux", Log.LogEntryType.Warning);
                    else
                        return true;
                }
            }
            else if (Util.FilePaths.CleanExt(_RecordingFile) == ".wtv") // WTV can contain both MPEG2 and MPEG4 video
            {
                if (!(_RecordingFileMediaInfo.Success && !_RecordingFileMediaInfo.ParseError))
                {
                    _jobLog.WriteEntry(this, "FFMpeg unable to read WTV file, trying RemuxxSupp", Log.LogEntryType.Warning);
                    _useRemuxsupp = true; // FFMPEG Can't read the file, could be another issue, try falling back to remuxsupp
                }

                if (_forceWTVStreamsRemuxing) // if we are using H264 copy remuxing then first try DirectShow based streams remuxing (WTV supports only mpeg2 and mpeg4)
                {
                    if (_allowH264CopyRemuxing || GlobalDefs.mpeg2Codecs.Any(s => s.Contains(_RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec.ToLower()))) // By default we only copy mpeg2 streams and transcode mpeg4 to mpeg2 unless we are asked to copy mpeg4
                    {
                        _jobLog.WriteEntry(this, Localise.GetPhrase("Force Streams WTV Remuxing, attempting directshow based remux, extracting streams"), Log.LogEntryType.Information);
                        _jobStatus.CurrentAction = Localise.GetPhrase("Extracting streams");
                        if (!DirectShowExtractAndRemuxStreams())
                            _jobLog.WriteEntry(this, "Unable to remux WTV using directshow streams", Log.LogEntryType.Warning);
                        else
                            return true;
                    }
                }

                if (_useRemuxsupp)
                {
                    if (_allowH264CopyRemuxing || GlobalDefs.mpeg2Codecs.Any(s => s.Contains(_RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec.ToLower()))) // By default we only copy mpeg2 streams and transcode mpeg4 to mpeg2 unless we are asked to copy mpeg4
                    {
                        _jobLog.WriteEntry(this, Localise.GetPhrase("WTV Byte stream remuxing"), Log.LogEntryType.Information);
                        _jobStatus.CurrentAction = Localise.GetPhrase("Byte stream remuxing");
                        // Otherwise try a direct byte mux
                        if (!RemuxSuppWTV())
                            _jobLog.WriteEntry(this, "Unable to remux WTV using byte stream remuxing", Log.LogEntryType.Warning);
                        else
                            return true; //remuxsupp for wtv succeded to create a ts mpeg file
                    }
                }
            }

            //continue to try other fallback methods, try Ffmpeg
            _jobLog.WriteEntry(this, Localise.GetPhrase("Fast Remuxing"), Log.LogEntryType.Information);
            _jobStatus.CurrentAction = Localise.GetPhrase("Fast Remuxing");
            if (RemuxFFmpeg(FFMPEGRemuxType.Both)) //trying as a fallback directly conversion from WTV/DVRMS to TS using FFMPEG, no framerate conversion
            {
                FixTSRemuxedFileName(); // Fix the file name if source file is TS
                return true;
            }
            else
                _jobLog.WriteEntry(this, "Unable to copy remux using FFMPEG, trying other methods", Log.LogEntryType.Warning);

            // Note: We need to find a better way to handle remuxsupp (rebuild the source?)
            // Do this as the last option since it is very unreliable and often copies broken streams which fail later
            // Try backup for WTV files, if FFMPEG fails (still not 100% stable)
            if (FilePaths.CleanExt(_RecordingFile) == ".wtv") // Try remuxing at the byte level next with remuxsupp
            {
                // Note: Some H264 files with REmuxsupp just hang it! Remuxsupp is not good with H264, unless asked for now limit RemuxSupp to mpeg2video files
                if (GlobalDefs.mpeg2Codecs.Any(s => s.Contains(_RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec.ToLower()))) // By default we only copy mpeg2 streams and transcode mpeg4 to mpeg2 unless we are asked to copy mpeg4
                {
                    // Otherwise try a direct byte mux
                    _jobStatus.CurrentAction = Localise.GetPhrase("Byte stream remuxing");
                    if (RemuxSuppWTV())
                        return true; //remuxsupp for wtv succeded to create a ts mpeg file
                    else
                        _jobLog.WriteEntry(this, "Unable to remux WTV using byte stream remuxing", Log.LogEntryType.Warning);
                }
                else
                    _jobLog.WriteEntry(this, "Skipping RemuxSupp since video is not MPEG2VIDEO (sometimes hangs with H264 video, specify UseWTVRemuxsupp=true in profile if you want to force use of RemuxSupp)", Log.LogEntryType.Warning);
            }

            // Extract the audio and video streams to files using directshow as last resort
            _jobLog.WriteEntry(this, Localise.GetPhrase("Attempting directshow based remux, extracting streams"), Log.LogEntryType.Information);
            _jobStatus.CurrentAction = Localise.GetPhrase("Extracting streams");
            if (DirectShowExtractAndRemuxStreams())
            {
                FixTSRemuxedFileName(); // Fix the file name if source file is TS
                return true;
            }
            else
                _jobLog.WriteEntry(this, "DirectShow unable to extract and remux streams", Log.LogEntryType.Warning);

            _jobStatus.ErrorMsg = "All remux options failed";
            _jobLog.WriteEntry(this, _jobStatus.ErrorMsg, Log.LogEntryType.Error);
            return false; // We failed...
        }

        /// <summary>
        /// Remux TiVO files using DirectShow Streams (TiVO Desktop) first and then fallback to TiVODecode
        /// </summary>
        /// <returns>True if successful</returns>
        public bool RemuxTiVO()
        {
            if (Util.FilePaths.CleanExt(_RecordingFile) == ".tivo") // Check for TiVO files
            {
                if (!DirectShowExtractAndRemuxStreams())
                {
                    _jobLog.WriteEntry(this, "Unable to remux TIVO using directshow streams, trying alternative method", Log.LogEntryType.Warning);
                    if (!RemuxTiVODecode())
                    {
                        _jobStatus.ErrorMsg = "Unable to remux TiVO";
                        _jobLog.WriteEntry(this, _jobStatus.ErrorMsg, Log.LogEntryType.Error);
                        return false; // There are no other options for remuxing hence return here
                    }
                    else
                        return true;
                }
                else
                    return true;
            }
            else
            {
                _jobStatus.ErrorMsg = "File not a TiVO file, skipping remuxing";
                _jobLog.WriteEntry(this, _jobStatus.ErrorMsg, Log.LogEntryType.Error);
                return false;
            }
        }

        private bool RemuxTiVODecode()
        {
            string orgRecordingFile = _RecordingFile; // Track it since we will replace it
            FFmpegMediaInfo orgRecordingFileMediaInfo = _RecordingFileMediaInfo; // Track it since we will replace it

            Util.FileIO.TryFileDelete(RemuxedFile);

            _jobLog.WriteEntry(this, "Remuxing TiVO file using TiVODecode", Log.LogEntryType.Information);

            string tivoRemuxParams = "";
            string mpgFile = Path.Combine(_destinationPath, Path.GetFileNameWithoutExtension(_RecordingFile) + ".mpg"); // Intermediate file from TiVODecode

            if (String.IsNullOrWhiteSpace(_tivoMAKKey))
            {
                _jobLog.WriteEntry(this, "No TiVO MAK key found", Log.LogEntryType.Error);
                return false;
            }

            tivoRemuxParams += "-m " + _tivoMAKKey + " -o " + Util.FilePaths.FixSpaces(mpgFile) + " " + Util.FilePaths.FixSpaces(_RecordingFile);
            TiVODecode tivoDecode = new TiVODecode(tivoRemuxParams, _jobStatus, _jobLog);
            tivoDecode.Run();

            if ((Util.FileIO.FileSize(mpgFile) <= 0) || !tivoDecode.Success)
            {
                Util.FileIO.TryFileDelete(mpgFile);
                _jobLog.WriteEntry(this, "TiVODecode failed to remux to mpg", Log.LogEntryType.Error);
                return false;
            }

            _jobLog.WriteEntry(this, "Remuxing TiVO MPG file to TS", Log.LogEntryType.Information);

            // Repoint the remuxing file to MPG
            _RecordingFile = mpgFile;
            // Get the media info for the mpg file
            _jobLog.WriteEntry(this, "Reading MPG file " + _RecordingFile + " media information", Log.LogEntryType.Debug);
            _RecordingFileMediaInfo = new FFmpegMediaInfo(_RecordingFile, _jobStatus, _jobLog);

            // Use the FFMPEGRemux to remux the MPG to TS
            if (!RemuxFFmpeg(FFMPEGRemuxType.Both))
            {
                Util.FileIO.TryFileDelete(mpgFile);
                Util.FileIO.TryFileDelete(_RemuxedFile);
                _jobLog.WriteEntry(this, "TiVO ffmpeg failed to remux mpg to ts", Log.LogEntryType.Error);
                return false;
            }

            bool retVal = RemuxedFileOK(); // Check against the mpg file

            _RecordingFile = orgRecordingFile; // Reset it for future use, incase this function has failed
            _RecordingFileMediaInfo = orgRecordingFileMediaInfo; // Reset it for future use, incase this function has failed

            Util.FileIO.TryFileDelete(mpgFile); // MPG file is no longer required

            return retVal;
        }

        /// <summary>
        /// Uses RemuxSupp for Remux WTV files to TS files
        /// Checks for 0 channel audio and re-remuxes it with the selected language
        /// </summary>
        /// <returns>Success or Failure</returns>
        private bool RemuxSuppWTV()
        {
            string RemuxToolParameters = "";

            RemuxToolParameters = "-i " + Util.FilePaths.FixSpaces(_RecordingFile) + " -o " + Util.FilePaths.FixSpaces(_RemuxedFile) + " -all"; // copy all Audio Streams, we'll select them later

            string tempFileName = "";

            RemuxSupp remuxsupp = new RemuxSupp(RemuxToolParameters, _jobStatus, _jobLog);
            remuxsupp.Run();

            if (tempFileName != "") Util.FileIO.TryFileDelete(tempFileName);

            if (!(remuxsupp.Success && RemuxedFileOK())) //remux succedded and file exists and no zero channel audio track
            {
                if (CheckForNoneOrZeroChannelAudioTrack(RemuxedFile, _jobStatus, _jobLog))
                {
                    // Found a 0 channel audio, now try once more with just the language required
                    _jobLog.WriteEntry(this, Localise.GetPhrase("RemuxSupp found 0 channel audio, trying again with language selection") + " : " + Localise.ThreeLetterISO(), Log.LogEntryType.Warning);
                    _jobStatus.CurrentAction = Localise.GetPhrase("Re-ReMuxing due to audio error");

                    RemuxToolParameters = "-i " + Util.FilePaths.FixSpaces(_RecordingFile) + " -o " + Util.FilePaths.FixSpaces(_RemuxedFile) + " -lang " + Localise.ThreeLetterISO();

                    tempFileName = "";

                    remuxsupp = new RemuxSupp(RemuxToolParameters, _jobStatus, _jobLog);
                    remuxsupp.Run();
                    
                    if (tempFileName != "") Util.FileIO.TryFileDelete(tempFileName);

                    if (!(remuxsupp.Success && RemuxedFileOK())) //remux succedded and file exists and no zero channel audio
                    {
                        _jobStatus.PercentageComplete = 0;
                        _jobLog.WriteEntry(this, "ReMuxxSupp failed", Log.LogEntryType.Error);
                        return false;
                    }
                }
                else
                {
                    _jobLog.WriteEntry(this, "ReMuxxSupp failed", Log.LogEntryType.Error);
                    return false;
                }
            }
            
            // All's good
            return true;
        }

        /// <summary>
        /// Remux the DVRMS file directly to TSMPEG using a special FFMPEG
        /// </summary>
        /// <returns>Success or Failure</returns>
        private bool RemuxDVRMSFFmpeg()
        {
            string profile = "DVRMSRemux"; // Reading the remux parameters from the file
            string ffmpegParams = "";
            Ini configProfileIni = new Ini(GlobalDefs.ConfigFile);

            // Threads 0 causes an error in some streams, avoid
            _jobLog.WriteEntry(this, Localise.GetPhrase("DVRMS file, using special FFMPEG to remux"), Log.LogEntryType.Information);

            // First try with regular FFMPEG and GenPTs, since the DVRMS tends to created corrupted files
            _jobStatus.CurrentAction = Localise.GetPhrase("Fast Remuxing");

            _jobLog.WriteEntry(this, Localise.GetPhrase("Reading DVRMS Remux parameters"), Log.LogEntryType.Information);

            // First try to copy all the streams directly (read parmeters for copy profile)
            string baseRemuxParams = configProfileIni.ReadString(profile, "Remux", "");

            if (String.IsNullOrWhiteSpace(baseRemuxParams)) // Have we used up all the CopyRemux profiles, then we're done here - try something else
            {
                _jobLog.WriteEntry(Localise.GetPhrase("DVRMS Remuxing disabled in config file, no parameters"), Log.LogEntryType.Error);
                Util.FileIO.TryFileDelete(RemuxedFile);
                return false;
            }

            // Check for input file placeholders and substitute
            if (baseRemuxParams.Contains("-i <source>")) // Check if we have a input file placeholder (useful if we need to put commands before -i)
            {
                baseRemuxParams = baseRemuxParams.Replace("-i <source>", "-i " + Util.FilePaths.FixSpaces(_RecordingFile) + " ");
                ffmpegParams = "-fflags +genpts -y " + baseRemuxParams + " " + Util.FilePaths.FixSpaces(RemuxedFile);
            }
            else
                ffmpegParams = "-fflags +genpts -y -i " + Util.FilePaths.FixSpaces(_RecordingFile) + " " + baseRemuxParams + " " + Util.FilePaths.FixSpaces(RemuxedFile); // DO NOT USE -async 1 with COPY

            FFmpeg ffmpeg;
            FFmpeg.FFMpegExecuteAndHandleErrors(ffmpegParams, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(RemuxedFile), false, out ffmpeg); // Run and handle errors, don't need to check output file here
            if (!ffmpeg.Success || !RemuxedFileOK()) //check of file is created, outputhandler reports success (Percentage not requires since Success is more accurate)
            {
                // Use Special build of DVRMS FFMPEG to handle now
                _jobLog.WriteEntry(Localise.GetPhrase("DVRMS ReMux using FFMPEG GenPTS failed at") + " " + _jobStatus.PercentageComplete.ToString(CultureInfo.InvariantCulture) + "%. Retrying using special DVRMS FFMpeg", Log.LogEntryType.Warning);

                // DVR-MS supports only one audio stream
                ffmpegParams = ffmpegParams.Replace("-fflags +genpts", ""); // don't need genpts for special build dvrms ffmpeg

                ffmpeg = new FFmpeg(ffmpegParams, true, _jobStatus, _jobLog); // use SPECIAL BUILD DVRMS-FFMPEG, so we can't use FFMpegExecuteAndHandleErrors
                ffmpeg.Run();

                if (!ffmpeg.Success || !RemuxedFileOK()) //check of file is created, outputhandler reports success (Percentage not requires since Success is more accurate)
                {
                    _jobLog.WriteEntry(Localise.GetPhrase("DVRMS ReMux using FFMPEG failed at") + " " + _jobStatus.PercentageComplete.ToString(CultureInfo.InvariantCulture) + "%", Log.LogEntryType.Error);
                    Util.FileIO.TryFileDelete(RemuxedFile);
                    return false;
                }
            }

            // Since we are successful, keep track of how many seconds we skipped while remuxing the file
            FFMpegMEncoderParams checkParams = new FFMpegMEncoderParams(baseRemuxParams);
            float.TryParse(checkParams.ParameterValue("-ss"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out skipInitialSeconds);

            return true;
        }

        /// <summary>
        /// Remux the source recorded file using the specified base parameters, but also check if the source file (or Remux file) has any zero channel audio streams in it.
        /// If so it tries to remux using FFMPEG and the given parameters but compensating to keep only one audio channel
        /// It also checks to see if it can locate the user identified audio language within the source recorded file if required
        /// </summary>
        /// <param name="baseRemuxParams">Base parameters used for remuxing</param>
        /// <param name="FPS">Frame rate for the Recorded file</param>
        /// <returns>True if there are no zero channel audio tracks or on a successful remux</returns>
        private bool FFMPEGRemuxZeroChannelFix(string baseRemuxParams, float FPS)
        {
            FFmpeg ffmpegObj = null; // Dummy not required here
            return FFMPEGRemuxZeroChannelFix(baseRemuxParams, FPS, false, ref ffmpegObj);
        }
        
        /// <summary>
        /// Remux the source recorded file using the specified base parameters, but also check if the source file (or Remux file) has any zero channel audio streams in it.
        /// If so it tries to remux using FFMPEG and the given parameters but compensating to keep only one audio channel
        /// It also checks to see if it can locate the user identified audio language within the source recorded file if required
        /// </summary>
        /// <param name="baseRemuxParams">Base parameters used for remuxing</param>
        /// <param name="FPS">Frame rate for the Recorded file</param>
        /// <param name="fixRemuxedFile">True if Remuxed file file needs to be FIXED for Zero Channel Audio, false if Recorded file needs to be checked and then fixed</param>
        /// <param name="ffmpeg">Point to the last executed ffmpeg object (used for analysis) since this is a recursive reentrant function, we need to keep track</param>
        /// <returns>True if there are no zero channel audio tracks or on a successful remux</returns>
        private bool FFMPEGRemuxZeroChannelFix(string baseRemuxParams, float FPS, bool fixRemuxedFile, ref FFmpeg ffmpeg)
        {
            bool autoFPS = false; // Used to check if Auto FPS was used

            _jobLog.WriteEntry(this, "Verifying " + (fixRemuxedFile ? "Remuxed" : "Recorded") + " file audio streams for Zero Channel Audio", Log.LogEntryType.Debug);

            // Compensate for FFMPEG bug #2227 where the mjpeg is identified as a video stream hence breaking -map 0:v, rather replace 'v' with the actual video stream number
            if (_RecordingFileMediaInfo.Success && !_RecordingFileMediaInfo.ParseError)
            {
                if (baseRemuxParams.Contains("-map 0:v"))
                {
                    if (_RecordingFileMediaInfo.MediaInfo.VideoInfo.Stream != -1) // Check if we have a file with only audio streams
                        baseRemuxParams = baseRemuxParams.Replace("-map 0:v", "-map 0:" + _RecordingFileMediaInfo.MediaInfo.VideoInfo.Stream.ToString(CultureInfo.InvariantCulture)); // replace 0:v with the actual video stream number
                    else
                    {
                        _jobLog.WriteEntry(this, "No Video stream detected in original file, removing support for video stream selection", Log.LogEntryType.Warning);
                        baseRemuxParams = baseRemuxParams.Replace("-map 0:v", ""); // remove 0:v since we have no video stream
                    }
                }

                // Check of the original file has no audio, then remove support for audio mappings
                if (_RecordingFileMediaInfo.AudioTracks < 1)
                {
                    _jobLog.WriteEntry(this, "No Audio stream detected in original file, removing support for audio stream selection", Log.LogEntryType.Warning);
                    baseRemuxParams = baseRemuxParams.Replace("-map 0:a", ""); // remove 0:v since we have no video stream
                }
            }
            else
            {
                _jobLog.WriteEntry(this, "Error reading audio and video streams, removing support for audio and video stream selection", Log.LogEntryType.Warning);
                baseRemuxParams = Regex.Replace(baseRemuxParams, @"-map 0:[\S]*", ""); // Remove all patterns like -map 0:v or -map 0:4 since we cannot read ffmpeg stream info
            }

            // We have a 0 channel audio we try to compensate for it by selecting the appropriate audio channel
            if (CheckForNoneOrZeroChannelAudioTrack((fixRemuxedFile ? RemuxedFile : _RecordingFile), _jobStatus, _jobLog))
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Found 0 channel audio while remuxing, re-remuxing using a single audio track"), Log.LogEntryType.Information);
                _jobStatus.CurrentAction = Localise.GetPhrase("Re-ReMuxing due to audio error");

                // DO NOT USE MAP ALL commands, we only need to copy one audio and one video stream
                baseRemuxParams = Regex.Replace(baseRemuxParams, @"-map 0:[\S]*", ""); // Remove all patterns like -map 0:v or -map 0:4

                if (_RecordingFileMediaInfo.Success && !_RecordingFileMediaInfo.ParseError)
                {
                    // Audio parameters - find the best Audio channel for the selected language or best audio track if there are imparired tracks otherwise by default the encoder will select the best audio channel (encoders do not do a good job of ignoring imparired tracks)
                    bool selectedTrack = false;
                    int audioChannels = 0;
                    int audioStream = -1;
                    int videoStream = -1;
                    bool selectedAudioImpaired = false;

                    if ((!String.IsNullOrEmpty(_requestedAudioLanguage) || (_RecordingFileMediaInfo.ImpariedAudioTrackCount > 0)) && (_RecordingFileMediaInfo.AudioTracks > 1)) // More than 1 audio track to choose from and either we have a language match request or a presence of an imparied channel (likely no audio)
                    {
                        for (int i = 0; i < _RecordingFileMediaInfo.AudioTracks; i++)
                        {
                            bool processTrack = false; // By default we don't need to process

                            // Language selection check, if the user has picked a specific language code, look for it
                            // If we find a match, we look the one with the highest number of channels in it
                            if (!String.IsNullOrEmpty(_requestedAudioLanguage))
                            {
                                if ((_RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Language.ToLower() == _requestedAudioLanguage) && (_RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Channels > 0))
                                {
                                    if (selectedTrack)
                                    {
                                        if (!( // take into account impaired tracks (since impaired tracks typically have no audio)
                                            ((_RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Channels > audioChannels) && !_RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Impaired) || // PREFERENCE to non-imparied Audio tracks with the most channels
                                            ((_RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Channels > audioChannels) && selectedAudioImpaired) || // PREFERENCE to Audio tracks with most channels if currently selected track is impaired
                                            (!_RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Impaired && selectedAudioImpaired) // PREFER non impaired audio over currently selected impaired
                                            ))
                                            continue; // we have found a lang match, now we are looking for more channels only now
                                    }

                                    processTrack = true; // All conditions met, we need to process this track
                                }
                            }
                            else if (_RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Channels > 0)// we have a imparied audio track, select the non impaired track with the highest number of tracks or bitrate or frequency
                            {
                                if (selectedTrack)
                                {
                                    if (!( // take into account impaired tracks (since impaired tracks typically have no audio)
                                        ((_RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Channels > audioChannels) && !_RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Impaired) || // PREFERENCE to non-imparied Audio tracks with the most channels
                                        ((_RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Channels > audioChannels) && selectedAudioImpaired) || // PREFERENCE to Audio tracks with most channels if currently selected track is impaired
                                        (!_RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Impaired && selectedAudioImpaired) // PREFER non impaired audio over currently selected impaired
                                        ))
                                        continue; // we have found a lang match, now we are looking for more channels only now
                                }
                             
                                processTrack = true; // All conditions met, we need to process this track
                            }

                            if (processTrack) // We need to process this track
                            {
                                audioChannels = _RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Channels;
                                audioStream = _RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Stream; // store the stream number for the selected audio channel
                                string audioCodec = _RecordingFileMediaInfo.MediaInfo.AudioInfo[i].AudioCodec;
                                int audioTrack = i; // Store the audio track number we selected
                                string audioLanguage = _RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Language.ToLower(); // this is what we selected
                                selectedAudioImpaired = _RecordingFileMediaInfo.MediaInfo.AudioInfo[i].Impaired; // Is this an imparied track?
                                videoStream = _RecordingFileMediaInfo.MediaInfo.VideoInfo.Stream; // Store the video information
                                selectedTrack = true; // We found a suitable track

                                if (!String.IsNullOrEmpty(_requestedAudioLanguage))
                                    _jobLog.WriteEntry(this, Localise.GetPhrase("Found Audio Language match for language") + " " + _requestedAudioLanguage.ToUpper() + ", " + Localise.GetPhrase("Audio Stream") + " " + audioStream.ToString(CultureInfo.InvariantCulture) + ", " + Localise.GetPhrase("Audio Track") + " " + audioTrack.ToString(CultureInfo.InvariantCulture) + ", " + Localise.GetPhrase("Channels") + " " + audioChannels.ToString(CultureInfo.InvariantCulture) + ", " + Localise.GetPhrase("Codec") + "->" + audioCodec + ", Audio Impaired->" + selectedAudioImpaired.ToString(), Log.LogEntryType.Debug);
                                else
                                    _jobLog.WriteEntry(this, Localise.GetPhrase("Compensating for audio impaired tracks, selected track with language") + " " + _requestedAudioLanguage.ToUpper() + ", " + Localise.GetPhrase("Audio Stream") + " " + audioStream.ToString(CultureInfo.InvariantCulture) + ", " + Localise.GetPhrase("Audio Track") + " " + audioTrack.ToString(CultureInfo.InvariantCulture) + ", " + Localise.GetPhrase("Channels") + " " + audioChannels.ToString(CultureInfo.InvariantCulture) + ", " + Localise.GetPhrase("Codec") + "->" + audioCodec + ", Audio Impaired->" + selectedAudioImpaired.ToString(), Log.LogEntryType.Debug);
                            }
                        }
                    }

                    // If we have a found a suitable language, select it else let FFMPEG select it automatically
                    if (selectedTrack)
                    {
                        if (audioStream != -1)
                            baseRemuxParams += " -map 0:" + audioStream.ToString(CultureInfo.InvariantCulture); // Select the Audiotrack we had isolated earlier
                        
                        if (videoStream != -1)
                            baseRemuxParams += " -map 0:" + videoStream.ToString(CultureInfo.InvariantCulture); // Check if we have a video stream
                    }
                    else
                        _jobLog.WriteEntry(this, "No audio language match found, letting encoder choose best audio language", Log.LogEntryType.Warning);
                }
                else
                    _jobLog.WriteEntry(this, "Error reading audio streams, letting encoder choose best audio language", Log.LogEntryType.Warning);
            }

            // Build the command line to remux the file now
            string ffmpegParams = "";

            // Check for auto frame rate and replace with video framerate
            if (baseRemuxParams.Contains("-r auto"))
            {
                if (FPS > 0)
                {
                    _jobLog.WriteEntry(this, "Detected Auto FPS request, setting frame rate to " + FPS.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                    baseRemuxParams = baseRemuxParams.Replace("-r auto", "-r " + FPS.ToString(CultureInfo.InvariantCulture));
                    autoFPS = true;
                }
                else
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Cannot read frame rate from file, skipping frame rate adjustment"), Log.LogEntryType.Warning);
                    baseRemuxParams = baseRemuxParams.Replace("-r auto", ""); // no framerate since we can't read it
                    autoFPS = false;
                }
            }
            else
                autoFPS = false;

            // Check for input file placeholders and substitute
            if (baseRemuxParams.Contains("-i <source>")) // Check if we have a input file placeholder (useful if we need to put commands before -i)
                ffmpegParams = "-y " + baseRemuxParams.Replace("-i <source>", "-i " + Util.FilePaths.FixSpaces(_RecordingFile) + " ") + " " + Util.FilePaths.FixSpaces(RemuxedFile);
            else
                ffmpegParams = "-y -i " + Util.FilePaths.FixSpaces(_RecordingFile) + " " + baseRemuxParams + " " + Util.FilePaths.FixSpaces(RemuxedFile); // DO NOT USE -async 1 with COPY

            FFmpeg.FFMpegExecuteAndHandleErrors(ffmpegParams, _jobStatus, _jobLog, Util.FilePaths.FixSpaces(RemuxedFile), false, out ffmpeg); // Run ffmpeg and check for common errors, we will get back the final executed ffmpeg object which we can then check for errors/analyze
            if (!ffmpeg.Success || !RemuxedFileOK()) //check of file is created, outputhandler reports success (Percentage not requires since Success is more accurate)
            {
                // last ditch effort, try to fix for all errors
                if (ffmpeg.Success && !fixRemuxedFile) // avoid infinite loop, fix remuxed file only if we started out checking the recorded file
                {
                    if (!FFMPEGRemuxZeroChannelFix(baseRemuxParams, FPS, true, ref ffmpeg)) // Call ZeroChannelAudioFix this time to fix the remuxed file
                    {
                        _jobLog.WriteEntry(Localise.GetPhrase("0 Channel ReMux using FFMPEG failed at") + " " + _jobStatus.PercentageComplete.ToString(CultureInfo.InvariantCulture) + "%", Log.LogEntryType.Error);
                        Util.FileIO.TryFileDelete(RemuxedFile);
                        return false;
                    }
                }
                else
                {
                    Util.FileIO.TryFileDelete(RemuxedFile);
                    return false; // Avoid infinite loop, we are done here, nothing can be done, failed...
                }
            }
            else if (fixRemuxedFile) // If we are in the loop fixing the remuxed file, we did good, return now - don't process further yet
                return true;

            // Remux succeeded, check for Dropped or Duplicate packets due to incorrect FPS
            _jobLog.WriteEntry("Average rate of dropped frames :" + " " + ffmpeg.AverageDropROC.ToString("#0.00", CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            _jobLog.WriteEntry("Average rate of duplicate frames :" + " " + ffmpeg.AverageDupROC.ToString("#0.00", CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            // Read ReMux Parameters from Config Profile
            Ini configProfileIni = new Ini(GlobalDefs.ConfigFile);
            string profile = "FFMpegBackupRemux"; // This is where the Fallback Remux parameters are stored
            
            // Read the Drop frame threshhold
            double dropThreshold;
            double.TryParse(configProfileIni.ReadString(profile, "RemuxDropThreshold", "3.0"), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out dropThreshold);

            // Read the Duplicate frame threshhold
            double dupThreshold;
            double.TryParse(configProfileIni.ReadString(profile, "RemuxDuplicateThreshold", "3.0"), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out dupThreshold);

            if ((ffmpeg.AverageDropROC > dropThreshold) || (ffmpeg.AverageDupROC > dupThreshold))
            {
                if (autoFPS) // Check if we used AutoFPS and also if this isn't a going into an infinite loop
                    _jobLog.WriteEntry(Localise.GetPhrase("Remuxed file has too many dropped or duplicate frames, try to manually set the frame rate. Auto FPS used ->") + " " + FPS.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Warning);
                else
                    _jobLog.WriteEntry(Localise.GetPhrase("Remuxed file has too many dropped or duplicate frames, check/set the manual remux frame rate"), Log.LogEntryType.Warning);
            }

            // Since we are successful, keep track of how many seconds we skipped while remuxing the file
            FFMpegMEncoderParams checkParams = new FFMpegMEncoderParams(baseRemuxParams);
            float.TryParse(checkParams.ParameterValue("-ss"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out skipInitialSeconds);

            return true; // All done here
        }

        /// <summary>
        /// Remux the WTV file directly to TSMPEG using FFMPEG
        /// Uses 3 levels of remuxing, stream copy, video transcode and video+audio transcode with support for Auto FPS detection or manual FPS override
        /// Sets the fixCorruptedRemux flag is it falls back to transcodes the video and audio using remux
        /// </summary>
        /// <param name="remuxTypes">Type of remuxing to try, copy, slow or both</param>
        /// <returns>Success or Failure</returns>
        private bool RemuxFFmpeg(FFMPEGRemuxType remuxTypes)
        {
            float FPS = 0;
            bool skipCopyRemux = false; // Do we need to skip stream copy
            bool skipSlowRemux = false; // Do we need to skip the slow copy

            if (remuxTypes == FFMPEGRemuxType.Recode) // Check if user asked to skip the copy remux
                skipCopyRemux = true;
            else if (remuxTypes == FFMPEGRemuxType.Copy)
                skipSlowRemux = true;

            // Read ReMux Parameters from Config Profile
            Ini configProfileIni = new Ini(GlobalDefs.ConfigFile);
            string profile = "FFMpegBackupRemux"; // This is where the Fallback Remux parameters are stored

            // MediaInfo is more reliable than FFMPEG but it doesn't always succeed
            _jobLog.WriteEntry(this, "Reading MediaInfo from " + _RecordingFile, Log.LogEntryType.Debug);
            FPS = VideoParams.FPS(_RecordingFile);
            _jobLog.WriteEntry(this, "Video FPS : " + FPS.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            if ((FPS <= 0) || ((FPS > _RecordingFileMediaInfo.MediaInfo.VideoInfo.FPS) && (_RecordingFileMediaInfo.MediaInfo.VideoInfo.FPS > 0))) // MediaInfo did not succeed or got wrong value
            {
                _jobLog.WriteEntry(this, "Reading FFMPEG info from " + _RecordingFile, Log.LogEntryType.Debug);

                if (_RecordingFileMediaInfo.Success && !_RecordingFileMediaInfo.ParseError)
                    FPS = _RecordingFileMediaInfo.MediaInfo.VideoInfo.FPS; // Get the FPS
                else
                    _jobLog.WriteEntry(this, "ERROR reading FFMPEG Media info from " + _RecordingFile + ", disabling AutoFPS support", Log.LogEntryType.Warning);
            }

            // First check if the video is mpeg1 or mpeg2 video, else we need to move on to Slow Remux which will convert the video to MPEG2VIDEO
            // If it's mpeg1 or mpeg2, we can stream copy it directly
            // Check if we are copying mpeg4 video or if we are asked to copy all video codecs the copy remux it
            if (_RecordingFileMediaInfo.Success && !_RecordingFileMediaInfo.ParseError)
            {
                if (remuxTypes != FFMPEGRemuxType.Copy) // If we are forced to do copy remux then skip the check
                {
                    // Check if we have a valid copy remux configuration
                    if (SlowRemuxRequired())
                    {
                        _jobLog.WriteEntry(this, "Video codec is not supported for fast copy remux in current configuration, skipping to slow remux. Video Codec found -> " + _RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec.ToLower(), Log.LogEntryType.Information);
                        skipCopyRemux = true;
                    }
                }
                else
                    _jobLog.WriteEntry(this, "Fast copy remuxing forced, skipping check for valid copy remux configuration. Video Codec found -> " + _RecordingFileMediaInfo.MediaInfo.VideoInfo.VideoCodec.ToLower(), Log.LogEntryType.Warning);
            }

            if (!skipCopyRemux)
            {
                int profileCount = 0;
                while (true) // We do this in a loop until all MPEG2 CopyRemux profiles have been consumed
                {
                    // Check for Multiple Audio Streams
                    // Copy all streams if there is an audio selection specified and we'll extract it later (make sure you use the -map 0:a command to copy ALL audio stream, -acodec copy copies only 1 audio stream withou the map command)
                    // Use coptyb and copyts to avoid invalid DTS errors
                    // Threads 0 causes an error in some streams, avoid
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Copy remux loop ") + profileCount.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Information);

                    // First try to copy all the streams directly (read parmeters for copy profile)
                    string copyRemuxParams = configProfileIni.ReadString(profile, "CopyRemux" + profileCount.ToString(CultureInfo.InvariantCulture), "");

                    if (String.IsNullOrWhiteSpace(copyRemuxParams)) // Have we used up all the CopyRemux profiles, then we're done here - try something else
                        break;

                    // Some files have zero channel audio tracks in them, if so then copying all audio track will fails
                    // So we check for these tracks and run the remux
                    if (FFMPEGRemuxZeroChannelFix(copyRemuxParams, FPS))
                        break; // We're good, all done

                    profileCount++; // try the next one
                }
            }

            if (!skipSlowRemux && !RemuxedFileOK()) //check if file is created and if we aren't skipping the slow remux
            {
                _jobStatus.PercentageComplete = 100; // reset it to try again
                _jobStatus.CurrentAction = Localise.GetPhrase("Slow Remuxing");

                int profileCount = 0;
                while (true) // Now loop through all the SlowRemux profiles until we succeed or fail
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Slow remux loop ") + profileCount.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Information);

                    // Now try to copy audio and transcode video (read parameters for Slow Remux profile)
                    string slowRemuxParams = configProfileIni.ReadString(profile, "SlowRemux" + profileCount.ToString(CultureInfo.InvariantCulture), "");

                    if (String.IsNullOrWhiteSpace(slowRemuxParams)) // Have we used up all the SlowRemux profiles, then we're done here
                        break;

                    // Some files have zero channel audio tracks in them, if so then copying all audio track will fails
                    // So we check for these tracks and run the remux
                    if (FFMPEGRemuxZeroChannelFix(slowRemuxParams, FPS))
                        break; // We're good, all done

                    profileCount++; // Move onto the next profile iteration
                }

                if (!RemuxedFileOK()) //check of file is created
                {
                    _jobLog.WriteEntry(Localise.GetPhrase("Slow Remux using FFMPEG failed at") + " " + _jobStatus.PercentageComplete.ToString(CultureInfo.InvariantCulture) + "%", Log.LogEntryType.Error);
                    Util.FileIO.TryFileDelete(RemuxedFile);
                    return false;
                }
                else
                    slowRemuxed = true; // We have successfully remuxed the file but using the slow method (i.e. possibly destorying any CC data)
            }
            else if (skipSlowRemux) // Check if the copy remuxed file is okay since we aren't doing slow remux
            {
                if (RemuxedFileOK())
                    return true;
                else
                {
                    Util.FileIO.TryFileDelete(RemuxedFile); // Clean up
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Remux with adjustments the extracted raw stream parts individually using ffmpeg
        /// </summary>
        /// <param name="originalFile">Path to original source video</param>
        /// <param name="remuxedFile">Path to output remuxed file</param>
        /// <param name="extractGraphResults">Extract graph object which has extracts the stream into raw parts</param>
        /// <returns>Success or Failure</returns>
        public static bool RemuxRawPartsFFmpeg(string originalFile, string remuxedFile, ExtractWithGraph extractGraphResults, JobStatus jobStatus, Log jobLog)
        {
            float audioDelay = 0, fps = 0;
            string vcodec = "";

            if (extractGraphResults == null || String.IsNullOrWhiteSpace(originalFile) || String.IsNullOrWhiteSpace(remuxedFile))
                return false;

            // Atleast 1 audio or video stream
            if (String.IsNullOrWhiteSpace(extractGraphResults.VideoPart) && (extractGraphResults.AudioParts.Count < 1))
                return false;

            Util.FileIO.TryFileDelete(remuxedFile); // Start clean

            // Audio Delay on the original file
            if (extractGraphResults.AudioParts.Count > 0)
            {
                audioDelay = VideoParams.AudioDelay(originalFile);
                jobLog.WriteEntry("Audio Delay : " + audioDelay.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            }

            // FPS and codec of the video
            if (!String.IsNullOrWhiteSpace(extractGraphResults.VideoPart))
            {
                fps = VideoParams.FPS(extractGraphResults.VideoPart);
                vcodec = VideoParams.VideoFormat(extractGraphResults.VideoPart).ToLower();
                FFmpegMediaInfo ffmpegStreamInfo = new FFmpegMediaInfo(extractGraphResults.VideoPart, jobStatus, jobLog);
                if (String.IsNullOrWhiteSpace(vcodec) || (fps <= 0) || ((fps > ffmpegStreamInfo.MediaInfo.VideoInfo.FPS) && (ffmpegStreamInfo.MediaInfo.VideoInfo.FPS > 0)))
                {
                    jobLog.WriteEntry("MediaInfo reading FPS/Vcodec failed, Reading FFMPEG info from " + extractGraphResults.VideoPart, Log.LogEntryType.Debug);

                    if (ffmpegStreamInfo.Success) // Don't check for parse error since this is raw video stream it will always give an parse error for N/A duration
                    {
                        if ((fps <= 0) || ((fps > ffmpegStreamInfo.MediaInfo.VideoInfo.FPS) && (ffmpegStreamInfo.MediaInfo.VideoInfo.FPS > 0))) // MediaInfo did not succeed or got wrong value
                            fps = ffmpegStreamInfo.MediaInfo.VideoInfo.FPS; // Get the FPS

                        if (String.IsNullOrWhiteSpace(vcodec))
                            vcodec = ffmpegStreamInfo.MediaInfo.VideoInfo.VideoCodec; // Get video codecc
                    }

                    if (fps <= 0) // sometimes ExtractWithGraph creates invalid video streams, if it's valid it will have a FPS and codec
                    {
                        jobLog.WriteEntry("Unable to read FPS from " + originalFile + ", invalid or no video stream", Log.LogEntryType.Error);
                        return false;
                    }

                    if (String.IsNullOrWhiteSpace(vcodec))
                    {
                        jobLog.WriteEntry("No Video Codec detected in raw stream, invalid or no video stream", Log.LogEntryType.Error);
                        return false;
                    }
                }

                jobLog.WriteEntry("Video Codec detected : " + vcodec + ", FPS : " + fps.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            }

            string FFmpegParams = "";
            string ffmpegMapParams = ""; // Keep track of the tracks added since we need to map them
            int ffmpegMapCount = 0;

            // Generate the timestamps, framerate and specify input format since it's raw video and ffmpeg won't recognize it
            FFmpegParams += "-fflags +genpts -y";
            if (fps > 0) // Check if we have a video stream
                FFmpegParams += " -r " + fps.ToString(CultureInfo.InvariantCulture);

            // Check for valid video stream otherwise just audio
            if (!String.IsNullOrWhiteSpace(vcodec) && fps > 0)
            {
                if (audioDelay == 0)
                    jobLog.WriteEntry("Skipping Audio Delay correction, cannnot read", Log.LogEntryType.Warning);

                if (audioDelay > 0) // positive is used on video track
                    FFmpegParams += " -itsoffset " + audioDelay.ToString(CultureInfo.InvariantCulture);

                // Define input video stream type
                if ((vcodec == "avc") || (vcodec == "h264"))
                    FFmpegParams += " -f h264";
                else
                    FFmpegParams += " -f mpegvideo";

                FFmpegParams += " -i " + Util.FilePaths.FixSpaces(extractGraphResults.VideoPart); // Video
                ffmpegMapParams += " -map " + ffmpegMapCount.ToString() + ":v";
                ffmpegMapCount++;

            }

            // Insert the audio streams
            foreach (string audioPart in extractGraphResults.AudioParts)
            {
                if (!String.IsNullOrWhiteSpace(vcodec) && fps > 0)
                    if (audioDelay < 0) // Negative is used on audio tracks
                        FFmpegParams += " -itsoffset " + (-1 * audioDelay).ToString(CultureInfo.InvariantCulture);

                FFmpegParams += " -i " + Util.FilePaths.FixSpaces(audioPart); // Audio streams
                ffmpegMapParams += " -map " + ffmpegMapCount.ToString() + ":a";
                ffmpegMapCount++;
            }

            // Copy all Streams to output file
            FFmpegParams += ffmpegMapParams;
            
            // Audio
            if (extractGraphResults.AudioParts.Count > 0)
                FFmpegParams += " -acodec copy";
            else
                FFmpegParams += " -an";

            // Video
            if (!String.IsNullOrWhiteSpace(vcodec) && fps > 0)
                FFmpegParams += " -vcodec copy";
            else
                FFmpegParams += " -vn";

            // Output file
            FFmpegParams += " -f mpegts " + Util.FilePaths.FixSpaces(remuxedFile);

            if (!FFmpeg.FFMpegExecuteAndHandleErrors(FFmpegParams, jobStatus, jobLog, Util.FilePaths.FixSpaces(remuxedFile))) // process was terminated or failed
            {
                jobLog.WriteEntry("FFmpeg Remux Parts failed", Log.LogEntryType.Error);
                Util.FileIO.TryFileDelete(remuxedFile);
                return false;
            }

            return (FileIO.FileSize(remuxedFile) <= 0 ? false : true); // check if the file exists
        }

        /// <summary>
        /// Put the extracting raw streams back together in TS format using TsMuxer
        /// </summary>
        /// <param name="originalFile">Path to original source video</param>
        /// <param name="remuxedFile">Path to output remuxed file</param>
        /// <param name="extractGraphResults">Extract graph object which has extracts the stream into raw parts</param>
        /// <returns>True if successful</returns>
        public static bool RemuxRawTSMuxer(string originalFile, string remuxedFile, ExtractWithGraph extractGraphResults, JobStatus jobStatus, Log jobLog)
        {
            float audioDelay = 0, fps = 0;
            string vcodec = "";

            if (extractGraphResults == null || String.IsNullOrWhiteSpace(originalFile) || String.IsNullOrWhiteSpace(remuxedFile))
                return false;
            
            // Atleast 1 audio or video stream
            if (String.IsNullOrWhiteSpace(extractGraphResults.VideoPart) && (extractGraphResults.AudioParts.Count < 1))
                return false;

            Util.FileIO.TryFileDelete(remuxedFile); // Start clean

            // Audio Delay on the original file
            if (extractGraphResults.AudioParts.Count > 0)
            {
                audioDelay = VideoParams.AudioDelay(originalFile);
                jobLog.WriteEntry("Audio Delay : " + audioDelay.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            }

            // fps and codec of video
            if (!String.IsNullOrWhiteSpace(extractGraphResults.VideoPart))
            {
                fps = VideoParams.FPS(extractGraphResults.VideoPart);
                vcodec = VideoParams.VideoFormat(extractGraphResults.VideoPart).ToLower();
                FFmpegMediaInfo ffmpegStreamInfo = new FFmpegMediaInfo(extractGraphResults.VideoPart, jobStatus, jobLog);
                if (String.IsNullOrWhiteSpace(vcodec) || (fps <= 0) || ((fps > ffmpegStreamInfo.MediaInfo.VideoInfo.FPS) && (ffmpegStreamInfo.MediaInfo.VideoInfo.FPS > 0)))
                {
                    jobLog.WriteEntry("MediaInfo reading FPS/Vcodec failed, Reading FFMPEG info from " + extractGraphResults.VideoPart, Log.LogEntryType.Debug);

                    if (ffmpegStreamInfo.Success) // Don't check for parse error since this is raw video stream it will always give an parse error for N/A duration
                    {
                        if ((fps <= 0) || ((fps > ffmpegStreamInfo.MediaInfo.VideoInfo.FPS) && (ffmpegStreamInfo.MediaInfo.VideoInfo.FPS > 0))) // MediaInfo did not succeed or got wrong value
                            fps = ffmpegStreamInfo.MediaInfo.VideoInfo.FPS; // Get the FPS

                        if (String.IsNullOrWhiteSpace(vcodec))
                            vcodec = ffmpegStreamInfo.MediaInfo.VideoInfo.VideoCodec; // Get video codecc
                    }

                    if (fps <= 0) // sometimes ExtractWithGraph creates invalid video streams, if it's valid it will have a FPS and codec
                    {
                        jobLog.WriteEntry("Unable to read FPS from " + originalFile + ", invalid or no video stream", Log.LogEntryType.Error);
                        return false;
                    }

                    if (String.IsNullOrWhiteSpace(vcodec))
                    {
                        jobLog.WriteEntry("No Video Codec detected in raw stream, invalid or no video stream", Log.LogEntryType.Error);
                        return false;
                    }
                }

                jobLog.WriteEntry("Video Codec detected : " + vcodec + ", FPS : " + fps.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            }

            // Setup the TSMuxer - http://forum.doom9.org/archive/index.php/t-142559.html
            string MetaFile = MCEBuddy.Util.FilePaths.GetFullPathWithoutExtension(remuxedFile) + ".meta";
            string MetaFileContents = "MUXOPT --no-pcr-on-video-pid --new-audio-pes --vbr --vbv-len=500\r\n";

            // TODO: How do we handle non MPEG2/MPEG4 streams?
            if (String.IsNullOrWhiteSpace(vcodec) || fps <= 0)
                jobLog.WriteEntry("No video stream detected, skipping video" + fps.ToString(CultureInfo.InvariantCulture), Log.LogEntryType.Debug); // Nothing to do, no video stream
            else if ((vcodec == "avc") || (vcodec == "h264"))
                MetaFileContents += "V_MPEG4/ISO/AVC, " + Util.FilePaths.FixSpaces(extractGraphResults.VideoPart) + ", fps=" + fps.ToString(CultureInfo.InvariantCulture) + ", insertSEI, contSPS\r\n";
            else
                MetaFileContents += "V_MPEG-2, " + Util.FilePaths.FixSpaces(extractGraphResults.VideoPart) + ", fps=" + fps.ToString(CultureInfo.InvariantCulture) + "\r\n";

            // Setup the Audio streams
            foreach (string AudioPart in extractGraphResults.AudioParts)
            {
                string acodec = VideoParams.AudioFormat(AudioPart).ToLower();
                if (String.IsNullOrWhiteSpace(acodec))
                {
                    jobLog.WriteEntry("No Audio Codec detected in raw stream", Log.LogEntryType.Error);
                    return false;
                }

                jobLog.WriteEntry("Audio Stream Codec detected : " + acodec, Log.LogEntryType.Debug);

                if (acodec.Contains("ac3") || acodec.Contains("ac-3"))
                    MetaFileContents += "A_AC3";
                else if (acodec.Contains("aac"))
                    MetaFileContents += "A_AAC";
                else if (acodec.Contains("dts"))
                    MetaFileContents += "A_DTS";
                else
                    MetaFileContents += "A_MP3";

                MetaFileContents += ", " + Util.FilePaths.FixSpaces(AudioPart);

                if (audioDelay == 0)
                    jobLog.WriteEntry("Skipping Audio Delay correction, cannnot read", Log.LogEntryType.Warning);
                else
                    MetaFileContents += ", timeshift=" + (-1 * audioDelay).ToString(CultureInfo.InvariantCulture) + "s";

                MetaFileContents += "\r\n";
            }

            try
            {
                jobLog.WriteEntry("Writing TsMuxer Meta commands : \r\n" + MetaFileContents + "\r\nTo : " + MetaFile, Log.LogEntryType.Debug);
                System.IO.File.WriteAllText(MetaFile, MetaFileContents);
            }
            catch (Exception e)
            {
                jobLog.WriteEntry(Localise.GetPhrase("ReMuxTsMuxer failed to write Metafile...") + "\r\nError : " + e.ToString(), Log.LogEntryType.Error);
                return false;
            }

            AppWrapper.TSMuxer tsmuxer = new TSMuxer(Util.FilePaths.FixSpaces(MetaFile) + " " + Util.FilePaths.FixSpaces(remuxedFile), jobStatus, jobLog);
            tsmuxer.Run();

            Util.FileIO.TryFileDelete(MetaFile);

            if (!tsmuxer.Success) // process was terminated or failed
            {
                jobLog.WriteEntry("ReMuxTsMuxer failed", Log.LogEntryType.Error);
                Util.FileIO.TryFileDelete(remuxedFile);
                return false;
            }

            return (FileIO.FileSize(remuxedFile) <= 0 ? false : true); // check if the file exists
        }

        /// <summary>
        /// Extract the Audio and Video Streams from the file using Windows DirectShow Graph Filter and then remux using TSMuxer and FFMPEG.
        /// Currently only support extracting 1 audio stream, so a good option for DVRMS (backup for WTV)
        /// </summary>
        /// <returns>True if successful in Remuxing to TS</returns>
        private bool DirectShowExtractAndRemuxStreams()
        {
            if (_jobStatus.Cancelled)
            {
                _jobLog.WriteEntry("Job cancelled, skipping DirectShow Streams Extraction", Log.LogEntryType.Error);
                return false;
            }

            switch (Util.FilePaths.CleanExt(_RecordingFile))
            {
                case ".wtv":
                case ".dvr-ms":
                case ".tivo":
                    break;

                default:
                    _jobLog.WriteEntry(this, "DirectShow Streams remuxing only supported for WTV, DVR-MS and TIVO files", Log.LogEntryType.Error);
                    return false;
            }

            try
            {
                _jobLog.WriteEntry("Extracting with Graph....", Log.LogEntryType.Debug);
                // Set video extraction bit only if video track exists since ExtractWithGraph otherwise always seems to extract a video track
                _extract = new ExtractWithGraph(_RecordingFile, _destinationPath, ExtractWithGraph.ExtractMediaType.Audio | (_RecordingFileMediaInfo.MediaInfo.VideoInfo.Stream >= 0 ? ExtractWithGraph.ExtractMediaType.Video : 0), _jobStatus, _jobLog); // Extract only audio and video streams, we don't need subtitle
            }
            catch (Exception e)
            {
                try
                {
                    if (_extract != null)
                        _extract.Dispose();
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Error Extracting with Graph....") + "\r\nError : " + e.ToString(), Log.LogEntryType.Error, true);
                }
                catch (Exception e1)
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Error disposing objects after graph failure....") + "\r\nError : " + e1.ToString(), Log.LogEntryType.Error, true);
                }

                return false;
            }

            try
            {
                _jobLog.WriteEntry("Building with Graph....", Log.LogEntryType.Debug);
                _extract.BuildGraph();
            }
            catch (Exception e)
            {
                try
                {
                    _jobLog.WriteEntry(this, ("Error building graph.\r\nError : " + e.ToString()), Log.LogEntryType.Error);
                    _extract.DeleteParts();
                    _extract.Dispose();
                }
                catch (Exception e1)
                {
                    _jobLog.WriteEntry(this, Localise.GetPhrase("Error disposing objects after graph failure....") + "\r\nError : " + e1.ToString(), Log.LogEntryType.Error, true);
                }

                return false;
            }

            // Atleast 1 audio or 1 video channel should be there
            if (String.IsNullOrWhiteSpace(_extract.VideoPart) && (_extract.AudioParts.Count < 1))
            {
                try
                {
                    _jobLog.WriteEntry(this, "Graph invalid number of video/audio streams", Log.LogEntryType.Error);
                    _extract.DeleteParts();
                    _extract.Dispose();
                }
                catch (Exception e1)
                {
                    _jobLog.WriteEntry(this, "Invalid Graph audio/video streams disposing.\r\nError : " + e1.ToString(), Log.LogEntryType.Error);
                }
                return false;
            }

            try
            {
                _jobLog.WriteEntry("Running with Graph....", Log.LogEntryType.Debug);
                _extract.RunGraph();
            }
            catch (Exception e)
            {
                try
                {
                    _jobLog.WriteEntry(this, ("Error running graph.\r\nError : " + e.ToString()), Log.LogEntryType.Error);
                    _extract.DeleteParts();
                    _extract.Dispose();
                }
                catch (Exception e1)
                {
                    _jobLog.WriteEntry(this, "Error disposing graph.\r\nError : " + e1.ToString(), Log.LogEntryType.Error);
                }
                return false;
            }

            // Did not abort
            if ((!_extract.SuccessfulExtraction))
            {
                try
                {
                    _jobLog.WriteEntry(this, "Error extracting streams using Graph", Log.LogEntryType.Error);
                    _extract.DeleteParts();
                    _extract.Dispose();
                }
                catch (Exception e1)
                {
                    _jobLog.WriteEntry(this, "Error extracting streams disposing.\r\nError : " + e1.ToString(), Log.LogEntryType.Error);
                }
                return false;
            }

            // Dispose the Graph object
            try
            {
                _jobLog.WriteEntry("Disposing Graph....", Log.LogEntryType.Debug);
                _extract.Dispose();
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry(this, "Error disposing graph.\r\nError : " + e.ToString(), Log.LogEntryType.Error); // We can still continue here
            }

            // Put the streams back together in TS format
            // Check to see if we can use FFmpegParts to do the job
            _jobLog.WriteEntry(this, Localise.GetPhrase("Muxing streams with TsMuxer"), Log.LogEntryType.Information);
            _jobStatus.CurrentAction = Localise.GetPhrase("Muxing streams");
            if (!RemuxRawTSMuxer(_RecordingFile, RemuxedFile, _extract, _jobStatus, _jobLog) || !RemuxedFileOK())
            {
                _jobLog.WriteEntry(this, "TsMuxer Streams Muxing failed", Log.LogEntryType.Error);

                // Otherwise try tsMuxer
                _jobLog.WriteEntry(this, Localise.GetPhrase("Fallback muxing streams with FFMpegParts"), Log.LogEntryType.Information);
                _jobStatus.CurrentAction = Localise.GetPhrase("Fallback muxing streams");
                if (!RemuxRawPartsFFmpeg(_RecordingFile, RemuxedFile, _extract, _jobStatus, _jobLog) || !RemuxedFileOK())
                {
                    _extract.DeleteParts(); // Clean up
                    _jobLog.WriteEntry(this, "Fallback Remux Streams FFMpegParts failed", Log.LogEntryType.Error);
                    return false;
                }
            }

            _extract.DeleteParts(); // Clean up

            return true;
        }

        /// <summary>
        /// Checkes the Remuxed file for the following:
        /// 1. File exists
        /// 2. There are no Zero Channel Audio Tracks
        /// 3. File size is reasonable
        /// </summary>
        /// <returns>True if all good, false if any of the above fail</returns>
        private bool RemuxedFileOK()
        {
            if (!File.Exists(RemuxedFile))
            {
                _jobLog.WriteEntry(this, "ReMuxer failed to create remux file", Log.LogEntryType.Error);
                return false;
            }
            else
            {
                try
                {
                    FileInfo remuxFi = new FileInfo(RemuxedFile);
                    FileInfo originalFi = new FileInfo(_RecordingFile);
                    _jobLog.WriteEntry(this, "Original file size [KB] -> " + (originalFi.Length / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "Remuxed file size [KB] -> " + (remuxFi.Length / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
                    if (remuxFi.Length <= 0)
                    {
                        // The remuxed file is too small
                        Util.FileIO.TryFileDelete(RemuxedFile);
                        _jobLog.WriteEntry(this, "Remux file is zero length", Log.LogEntryType.Error);
                        return false;
                    }
                }
                catch
                {
                    Util.FileIO.TryFileDelete(RemuxedFile);
                    _jobLog.WriteEntry(this, "Unable to get remux file size", Log.LogEntryType.Error);
                    return false;
                }

                // Finally - Check for 0 channel audio stream in Remuxed file
                if (CheckForNoneOrZeroChannelAudioTrack(RemuxedFile, _jobStatus, _jobLog))
                    return false;

                return true; // it worked!
            }
        }

        /// <summary>
        /// Checks if the specified file has any zero channel audio tracks or no audio tracks (except original file)
        /// </summary>
        /// <param name="file">Full path to file to check</param>
        /// <returns>True if there are any zero channel audio tracks or No Audio Tracks (except Original file) or failure to read the audio streams</returns>
        private bool CheckForNoneOrZeroChannelAudioTrack(string file, JobStatus jobStatus, Log jobLog)
        {
            jobLog.WriteEntry("Checking for 0 Channel Audio Tracks in " + file, Log.LogEntryType.Debug);

            // Check for 0 channel audio stream in Remuxed file
            FFmpegMediaInfo ffmpegStreamInfo = new FFmpegMediaInfo(file, jobStatus, jobLog);
            if (ffmpegStreamInfo.Success && !ffmpegStreamInfo.ParseError)
            {
                if (ffmpegStreamInfo.ZeroChannelAudioTrackCount > 0)
                {
                    jobLog.WriteEntry("Found 0 channel audio track in file " + file, Log.LogEntryType.Warning);
                    return true;
                }

                if (String.Compare(file, _RecordingFile, true) != 0) // We don't check original file for no audio tracks (it is allowed to have no audio tracks)
                {
                    if (ffmpegStreamInfo.AudioTracks == 0 && _RecordingFileMediaInfo.AudioTracks != 0 && (_RecordingFileMediaInfo.AudioTracks > _RecordingFileMediaInfo.ImpariedAudioTrackCount)) // The original file itself may have no audio tracks or should have atleast 1 non imparired audio track
                    {
                        jobLog.WriteEntry("Found No audio tracks in remuxed file", Log.LogEntryType.Warning);
                        return true;
                    }
                }
            }
            else
            {
                jobLog.WriteEntry("Unable to read FFMPEG MediaInfo to verify audio streams in file " + file, Log.LogEntryType.Error);
                return true;
            }

            return false; // good
        }
    }
}
