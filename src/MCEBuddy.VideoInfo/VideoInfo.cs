using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MCEBuddy.AppWrapper;
using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.VideoProperties
{
    public class VideoInfo
    {
        // External Parameters set by other functions related to video properties
        public string ConversionTool = "";
        public bool AdsRemoved = false;
        public bool TrimmingDone = false;
        public bool AudioDelaySet = false;
        public string Extension = "";
        public string RemuxTo = "";

        // Parameters that are updated at runtime and don't need reinitialization
        protected JobStatus _jobStatus;
        protected Log _jobLog;
        private string _EDLFile;
        private string _requestedAudioLanguage; // Language that we were requested to look for
        private string _originalFileName;
        private string _remuxedFileName;
        private bool _skipCropDetect;
        private bool _detectInterlacing; // What is the video source type
        private FFmpegMediaInfo _originalFileFFmpegStreamInfo;
        private FFmpegMediaInfo _ffmpegStreamInfo;
        private bool _ignoreSuspend = false; // If called from UI, we ignore any suspend requests to avoid hanging
        private const double TELECINE_HIGH_P_TO_I_RATIO = 2.0; // Telecine is measure by ratio of progressive to interlaced frames, this is a ratio measurement within a band
        private const double TELECINE_LOW_P_TO_I_RATIO = 1.0; // Less than 1 means more interlaced frames than progressive

        // IMPORTANT ADD PARAMETERS TO RESET SECTION: VideoInfo Parameters to be reset each time Update is called - these are to be reset from scratch for a clean update by the Update function
        private bool _error;
        private string _videoCodec;
        private string _audioCodec;
        private float _audioDelay;
        private int _audioChannels;
        private float _fps;
        private int _height;
        private int _width;
        private float _duration;
        private int _cropHeight;
        private int _cropWidth;
        private string _cropString;
        private int _audioStream; // Stream number of the selected Audio Channel
        private int _audioTrack; // Audio track number of the selected Audio Channel (0 based reference, i.e. 0 indicates 1st audio)
        private int _videoStream; // Stream number for the video stream (there is only 1 video stream per file)
        private int _videoPID; // Video stream PID
        private int _audioPID; // Audio stream PID
        private string _audioLanguage; // Store the language we are selecting
        private bool _selectedAudioImpaired; // Is this audio selection impaired audio
        private ScanType _scanType = ScanType.Unknown; // What type of video is this

        private void ResetParameters()
        {
            _error = false;
            _videoCodec = "";
            _audioCodec = "";
            _audioDelay = 0;
            _audioChannels = 0;
            _fps = 0;
            _height = 0;
            _width = 0;
            _duration = 0;
            _cropHeight = 0;
            _cropWidth = 0;
            _cropString = "";
            _audioStream = -1; // Stream number of the selected Audio Channel
            _audioTrack = -1; // Audio track number of the selected Audio Channel (0 based reference, i.e. 0 indicates 1st audio)
            _videoStream = -1; // Stream number for the video stream (there is only 1 video stream per file)
            _videoPID = -1; // Video stream PID
            _audioPID = -1; // Audio stream PID
            _audioLanguage = ""; // Store the language we are selecting
            _selectedAudioImpaired = false; // no imparied audio
            _scanType = ScanType.Unknown; // Do not know the scan type yet
        }

        /// <summary>
        /// Gets the audio / video information for a file, does not check cropping information or analyze interlacing type
        /// </summary>
        /// <param name="videoFileName">Full path to the video file to get information about</param>
        /// <param name="ignoreSuspend">True if called from a GUI</param>
        public VideoInfo(string videoFileName, JobStatus jobStatus, Log jobLog, bool ignoreSuspend)
        {
            _ignoreSuspend = ignoreSuspend;
            UpdateVideoInfo(true, false, videoFileName, "", "", "", jobStatus, jobLog);
        }

        /// <summary>
        /// Gets the Video file properties structure, check for crop, audio and video information
        /// </summary>
        /// <param name="skipCropDetect">True to skip detecting cropping parameters</param>
        /// <param name="detectInterlace">Extracts the video intelacing type by analyzing it in depth</param>
        /// <param name="videoFileName">Path to Original Source Video</param>
        /// <param name="remuxedFileName">Path to Remuxed video, else null or empty string</param>
        /// <param name="edlFile">Path to EDL file else null or empty string</param>
        /// <param name="audioLanguage">Audio Language</param>
        /// <param name="jobStatus">JobStatus</param>
        /// <param name="jobLog">JobLog</param>
        public VideoInfo(bool skipCropDetect, bool detectInterlace, string videoFileName, string remuxedFileName, string edlFile, string audioLanguage, JobStatus jobStatus, Log jobLog)
        {
            UpdateVideoInfo(skipCropDetect, detectInterlace, videoFileName, remuxedFileName, edlFile, audioLanguage, jobStatus, jobLog);
        }

        /// <summary>
        /// Updates the Video file properties structure, check for crop, audio and video information
        /// </summary>
        /// <param name="skipCropDetect">True to skip detecting cropping parameters</param>
        /// <param name="detectInterlace">Extracts the video intelacing type by analyzing it in depth</param>
        /// <param name="videoFileName">Path to Original Source Video</param>
        /// <param name="remuxedFileName">Path to Remuxed video, else null or empty string</param>
        /// <param name="edlFile">Path to EDL file else null or empty string</param>
        /// <param name="audioLanguage">Audio Language</param>
        /// <param name="jobStatus">JobStatus</param>
        /// <param name="jobLog">JobLog</param>
        public void UpdateVideoInfo(bool skipCropDetect, bool detectInterlace, string videoFileName, string remuxedFileName, string edlFile, string audioLanguage, JobStatus jobStatus, Log jobLog)
        {
            ResetParameters(); // Reset VideoInfo parameters

            _jobStatus = jobStatus;
            _jobLog = jobLog;
            _EDLFile = edlFile;
            _requestedAudioLanguage = audioLanguage;
            _originalFileName = videoFileName;
            _remuxedFileName = remuxedFileName;

            _skipCropDetect = skipCropDetect;
            _detectInterlacing = detectInterlace;

            _jobLog.WriteEntry(this, "Reading MediaInfo from " + SourceVideo, Log.LogEntryType.Information);

            _videoCodec = VideoParams.VideoFormat(SourceVideo);
            jobLog.WriteEntry(this, "Video Codec : " + _videoCodec, Log.LogEntryType.Debug);
            if (String.IsNullOrWhiteSpace(_videoCodec))
                _error = true;

            _audioCodec = VideoParams.AudioFormat(SourceVideo);
            jobLog.WriteEntry(this, "Audio Codec : " + _audioCodec, Log.LogEntryType.Debug);
            if (String.IsNullOrWhiteSpace(_audioCodec))
                _error = true;

            _fps = VideoParams.FPS(SourceVideo);
            jobLog.WriteEntry(this, "Video FPS : " + _fps.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            if (_fps <= 0)
                _error = true;

            _width = VideoParams.VideoWidth(SourceVideo);
            jobLog.WriteEntry(this, "Video Width : " + _width.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            if (_width <= 0)
                _error = true;

            _height = VideoParams.VideoHeight(SourceVideo);
            jobLog.WriteEntry(this, "Video Height : " + _height.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            if (_height <= 0)
                _error = true;

            _duration = VideoParams.VideoDuration(SourceVideo);
            jobLog.WriteEntry(this, "Video Duration : " + _duration.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            if (_duration <= 0)
                _error = true;

            _audioDelay = VideoParams.AudioDelay(SourceVideo);
            jobLog.WriteEntry(this, "Audio Delay : " + _audioDelay.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);

            if (_detectInterlacing) // Get Interlacing from FFMPEG more reliable then MediaInfo - avoid unnecessary cycles if not required
            {
                jobLog.WriteEntry(this, "Scan type unknown, trying with FFMPEGMediaInfo", Log.LogEntryType.Debug);

                _ffmpegStreamInfo = new FFmpegMediaInfo(SourceVideo, _jobStatus, _jobLog, 0, 0, _ignoreSuspend); // Run interlace detection with defaults
                if (_ffmpegStreamInfo.Success && !_ffmpegStreamInfo.ParseError)
                {
                    // Now calcuate whether it's Interlaced or Progressive based on the Multi Frame Interlaced Detection Results
                    long totalInterlaced = _ffmpegStreamInfo.MFInterlaceDetectionResults.BFF + _ffmpegStreamInfo.MFInterlaceDetectionResults.TFF;
                    long totalProgressive = _ffmpegStreamInfo.MFInterlaceDetectionResults.Progressive;
                    long totalUndetermined = _ffmpegStreamInfo.MFInterlaceDetectionResults.Undetermined; // TODO: What to do with this?

                    if (totalInterlaced == 0 && totalProgressive == 0) // Boundary conditions
                        _scanType = ScanType.Unknown;
                    else if (totalInterlaced == 0) // Avoid divide by zero exception
                        _scanType = ScanType.Progressive;
                    else
                    {
                        double PtoIRatio = totalProgressive / totalInterlaced; // See below comment

                        // Refer to this, how to tell if the video is interlaced or telecine
                        // http://forum.videohelp.com/threads/295007-Interlaced-or-telecined-how-to-tell?p=1797771&viewfull=1#post1797771
                        // It is a statistical ratio, telecine has approx split of 3 progressive and 2 interlaced (i.e. ratio of about 1.5 progressive to interlaced)
                        // TODO: We need to revisit the logic below for telecine, interlaced or progressive detection (check idet filter for updates ffmpeg ticket #3073)
                        if ((totalProgressive == 0) && (totalProgressive == 0)) // Unknown - could not find
                            _scanType = ScanType.Unknown;
                        else if ((PtoIRatio > TELECINE_LOW_P_TO_I_RATIO) && (PtoIRatio < TELECINE_HIGH_P_TO_I_RATIO)) // Let us keep a band to measure telecine ratio, see comment above
                            _scanType = ScanType.Telecine;
                        else if (PtoIRatio <= TELECINE_LOW_P_TO_I_RATIO) // We play safe, more interlaced than progressive
                            _scanType = ScanType.Interlaced;
                        else if (PtoIRatio >= TELECINE_HIGH_P_TO_I_RATIO) // Progressive has the clear lead
                            _scanType = ScanType.Progressive;
                        else
                            _scanType = ScanType.Unknown; // No idea where we are
                    }

                    jobLog.WriteEntry(this, "FFMPEG Video Scan Type : " + _scanType.ToString(), Log.LogEntryType.Debug);
                }
                else
                    jobLog.WriteEntry(this, "Error reading scan type from FFMPEGMediaInfo", Log.LogEntryType.Warning);

                if (_scanType == ScanType.Unknown) // If we couldn't get it from FFMPEG lets try MediaInfo as a backup
                {
                    _scanType = VideoParams.VideoScanType(SourceVideo);
                    jobLog.WriteEntry(this, " MediaInfo Video Scan Type : " + _scanType.ToString(), Log.LogEntryType.Debug);
                }
            }

            // We don't get AudioChannel information here as it interfers with FFMPEG
            /*mi.Option("Inform", "Audio; %Channels%");
            int.TryParse(mi.Inform(), out _audioChannels);
            jobLog.WriteEntry(this, "Audio Channels : " + _audioChannels.ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);*/

            // Supplement with extracting Video and Audio information (sometimes MediaInfo fails) using FFMPEG and selected the Audio Language specified
            _jobLog.WriteEntry(this, "Supplementing Media information using FFMPEG", Log.LogEntryType.Information);
            _ffmpegStreamInfo = new FFmpegMediaInfo(SourceVideo, _jobStatus, _jobLog, _ignoreSuspend); // this may be called from the UI request
            if (_ffmpegStreamInfo.Success && !_ffmpegStreamInfo.ParseError)
            {
                // Store the video information (there's only 1 video per file)
                _width = _ffmpegStreamInfo.MediaInfo.VideoInfo.Width;
                _height = _ffmpegStreamInfo.MediaInfo.VideoInfo.Height;
                if ((_fps <= 0) || ((_fps > _ffmpegStreamInfo.MediaInfo.VideoInfo.FPS) && (_ffmpegStreamInfo.MediaInfo.VideoInfo.FPS > 0))) // Check _fps, sometimes MediaInfo get it below 0 or too high (most times it's reliable)
                    _fps = _ffmpegStreamInfo.MediaInfo.VideoInfo.FPS;
                else
                    _ffmpegStreamInfo.MediaInfo.VideoInfo.FPS = _fps; // Store the value from MediaInfo, more reliable
                _duration = _ffmpegStreamInfo.MediaInfo.VideoInfo.Duration;
                _videoCodec = _ffmpegStreamInfo.MediaInfo.VideoInfo.VideoCodec;
                _videoStream = _ffmpegStreamInfo.MediaInfo.VideoInfo.Stream;
                _videoPID = _ffmpegStreamInfo.MediaInfo.VideoInfo.PID; // video PID
                
                // Default Check if all audio streams have same codec, if so populate the field for later use during reading profiles
                for (int i = 0; i < _ffmpegStreamInfo.AudioTracks; i++)
                {
                    if (i == 0)
                        _audioCodec = _ffmpegStreamInfo.MediaInfo.AudioInfo[i].AudioCodec; // baseline the codec name
                    else if (_audioCodec != _ffmpegStreamInfo.MediaInfo.AudioInfo[i].AudioCodec)
                    {
                        _audioCodec = ""; // All codecs are not the same, reset it and let the encoder figure it out
                        break; // we're done here
                    }
                }

                // Default check if all audio streams have same channels, if so populate the field for later use during reading profiles
                for (int i = 0; i < _ffmpegStreamInfo.AudioTracks; i++)
                {
                    if (i == 0)
                        _audioChannels = _ffmpegStreamInfo.MediaInfo.AudioInfo[i].Channels; // baseline the channels
                    else if (_audioChannels != _ffmpegStreamInfo.MediaInfo.AudioInfo[i].Channels)
                    {
                        _audioChannels = 0; // All channels are not the same, reset it and let the encoder figure it out
                        break; // we're done here
                    }
                }

                // Audio parameters - find the best Audio channel for the selected language or best audio track if there are imparired tracks otherwise by default the encoder will select the best audio channel (encoders do not do a good job of ignoring imparired tracks)
                bool selectedTrack = false;
                if ((!String.IsNullOrEmpty(_requestedAudioLanguage) || (_ffmpegStreamInfo.ImpariedAudioTrackCount > 0)) && (_ffmpegStreamInfo.AudioTracks > 1)) // More than 1 audio track to choose from and either we have a language match request or a presence of an imparied channel (likely no audio)
                {
                    for (int i = 0; i < _ffmpegStreamInfo.AudioTracks; i++)
                    {
                        bool processTrack = false; // By default we don't need to process

                        // Language selection check, if the user has picked a specific language code, look for it
                        // If we find a match, we look the one with the highest number of channels in it
                        if (!String.IsNullOrEmpty(_requestedAudioLanguage))
                        {
                            if ((_ffmpegStreamInfo.MediaInfo.AudioInfo[i].Language.ToLower() == _requestedAudioLanguage) && (_ffmpegStreamInfo.MediaInfo.AudioInfo[i].Channels > 0))
                            {
                                if (selectedTrack)
                                {
                                    if (!( // take into account impaired tracks (since impaired tracks typically have no audio)
                                        ((_ffmpegStreamInfo.MediaInfo.AudioInfo[i].Channels > _audioChannels) && !_ffmpegStreamInfo.MediaInfo.AudioInfo[i].Impaired) || // PREFERENCE to non-imparied Audio tracks with the most channels
                                        ((_ffmpegStreamInfo.MediaInfo.AudioInfo[i].Channels > _audioChannels) && _selectedAudioImpaired) || // PREFERENCE to Audio tracks with most channels if currently selected track is impaired
                                        (!_ffmpegStreamInfo.MediaInfo.AudioInfo[i].Impaired && _selectedAudioImpaired) // PREFER non impaired audio over currently selected impaired
                                        ))
                                        continue; // we have found a lang match, now we are looking for more channels only now
                                }
                             
                                processTrack = true; // All conditions met, we need to process this track
                            }
                        }
                        else if (_ffmpegStreamInfo.MediaInfo.AudioInfo[i].Channels > 0)// we have a imparied audio track, select the non impaired track with the highest number of tracks or bitrate or frequency
                        {
                            if (selectedTrack)
                            {
                                if (!( // take into account impaired tracks (since impaired tracks typically have no audio)
                                    ((_ffmpegStreamInfo.MediaInfo.AudioInfo[i].Channels > _audioChannels) && !_ffmpegStreamInfo.MediaInfo.AudioInfo[i].Impaired) || // PREFERENCE to non-imparied Audio tracks with the most channels
                                    ((_ffmpegStreamInfo.MediaInfo.AudioInfo[i].Channels > _audioChannels) && _selectedAudioImpaired) || // PREFERENCE to Audio tracks with most channels if currently selected track is impaired
                                    (!_ffmpegStreamInfo.MediaInfo.AudioInfo[i].Impaired && _selectedAudioImpaired) // PREFER non impaired audio over currently selected impaired
                                    ))
                                    continue; // we have found a lang match, now we are looking for more channels only now
                            }

                            processTrack = true; // All conditions met, we need to process this track
                        }

                        if (processTrack) // We need to process this track
                        {
                            _audioChannels = _ffmpegStreamInfo.MediaInfo.AudioInfo[i].Channels;
                            _audioStream = _ffmpegStreamInfo.MediaInfo.AudioInfo[i].Stream; // store the stream number for the selected audio channel
                            _audioCodec = _ffmpegStreamInfo.MediaInfo.AudioInfo[i].AudioCodec;
                            _audioPID = _ffmpegStreamInfo.MediaInfo.AudioInfo[i].PID; // Audio PID
                            _audioTrack = i; // Store the audio track number we selected
                            _audioLanguage = _ffmpegStreamInfo.MediaInfo.AudioInfo[i].Language.ToLower(); // this is what we selected
                            _selectedAudioImpaired = _ffmpegStreamInfo.MediaInfo.AudioInfo[i].Impaired; // Is this an imparied audio track?
                            selectedTrack = true; // We found a suitable track

                            if (!String.IsNullOrEmpty(_requestedAudioLanguage))
                                _jobLog.WriteEntry(this, "Found Audio Language match for language" + " " + _requestedAudioLanguage.ToUpper() + ", " + "Audio Stream" + " " + _audioStream.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Audio Track") + " " + _audioTrack.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Channels") + " " + _audioChannels.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Codec") + " " + _audioCodec + " Impaired " + _selectedAudioImpaired.ToString(), Log.LogEntryType.Debug);
                            else
                                _jobLog.WriteEntry(this, "Compensating for audio impaired tracks, selected track with language" + " " + _requestedAudioLanguage.ToUpper() + ", " + "Audio Stream" + " " + _audioStream.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Audio Track") + " " + _audioTrack.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Channels") + " " + _audioChannels.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Codec") + " " + _audioCodec + " Impaired " + _selectedAudioImpaired.ToString(), Log.LogEntryType.Debug);
                        }
                    }

                    if (!selectedTrack)
                        _jobLog.WriteEntry(this, ("Could not find a match for selected Audio Language Code") + " " + _requestedAudioLanguage + ", letting encoder choose best audio language", Log.LogEntryType.Warning);
                    else
                        _jobLog.WriteEntry(this, ("Selected Audio Stream") + " " + _audioStream.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Audio Track") + " " + _audioTrack.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Channels") + " " + _audioChannels.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Codec") + " " + _audioCodec + ", Impaired " + _selectedAudioImpaired.ToString(), Log.LogEntryType.Debug);
                }
                else if (_ffmpegStreamInfo.AudioTracks == 1) // We have just one audio track, then populate the information otherwise the encoding operations will have a hard time determining audio information
                {
                    if (_ffmpegStreamInfo.MediaInfo.AudioInfo[0].Channels > 0)
                    {
                        _audioChannels = _ffmpegStreamInfo.MediaInfo.AudioInfo[0].Channels;
                        _audioStream = _ffmpegStreamInfo.MediaInfo.AudioInfo[0].Stream; // store the stream number for the selected audio channel
                        _audioCodec = _ffmpegStreamInfo.MediaInfo.AudioInfo[0].AudioCodec;
                        _audioPID = _ffmpegStreamInfo.MediaInfo.AudioInfo[0].PID; // Audio PID
                        _audioTrack = 0; // Store the audio track number we selected
                        _audioLanguage = _ffmpegStreamInfo.MediaInfo.AudioInfo[0].Language.ToLower(); // this is what we selected
                        _selectedAudioImpaired = _ffmpegStreamInfo.MediaInfo.AudioInfo[0].Impaired; // Is this an imparied audio track?

                        _jobLog.WriteEntry(this, "Only one audio track present, " + ("Audio Stream") + " " + _audioStream.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Audio Track") + " " + _audioTrack.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Channels") + " " + _audioChannels.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Codec") + " " + _audioCodec + " Impaired " + _selectedAudioImpaired.ToString(), Log.LogEntryType.Debug);
                    }
                }
                else
                {
                    _jobLog.WriteEntry(this, ("Audio Stream") + " " + _audioStream.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Audio Track") + " " + _audioTrack.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Channels") + " " + _audioChannels.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + ("Codec") + " " + _audioCodec + ", Impaired " + _selectedAudioImpaired.ToString(), Log.LogEntryType.Debug);
                    _jobLog.WriteEntry(this, "No audio language selected, letting encoder choose best audio language", Log.LogEntryType.Warning);
                }

                _error = false; // all good now
            }
            else
                _error = true;
            
            if (_error)
            {
                _jobLog.WriteEntry(this, ("Unable to read media information using FFMPEG or MediaInfo"), Log.LogEntryType.Error);
                return;
            }

            // Get the video properties for the original video
            _jobLog.WriteEntry(this, "Reading Original File Media information", Log.LogEntryType.Information);
            _originalFileFFmpegStreamInfo = new FFmpegMediaInfo(_originalFileName, _jobStatus, _jobLog, _ignoreSuspend);
            if (!_originalFileFFmpegStreamInfo.Success || _originalFileFFmpegStreamInfo.ParseError)
                _jobLog.WriteEntry(this, ("Unable to read media information using FFMPEG"), Log.LogEntryType.Warning);

            if (_skipCropDetect)
                _jobLog.WriteEntry(this, "Skipping crop information", Log.LogEntryType.Information);
            else
                UpdateCropInfo(jobLog);
        }

        /// <summary>
        /// Updates the Crop information for the SourceVideo
        /// </summary>
        public void UpdateCropInfo(Log jobLog)
        {
            _jobLog.WriteEntry(this, "Getting crop information using MEncoder", Log.LogEntryType.Information);

            MencoderCropDetect mencoderCropDetect = new MencoderCropDetect(SourceVideo, _EDLFile, _jobStatus, jobLog);
            if (!mencoderCropDetect.Success || _jobStatus.PercentageComplete < GlobalDefs.ACCEPTABLE_COMPLETION)
            {
                _jobLog.WriteEntry(("MEncoder Crop Detect Process Error - cropping will not take place"), Log.LogEntryType.Warning);
                /*_error = true; // Crop detection failing is not a show stopper
                return;*/
            }
            else if ((!String.IsNullOrEmpty(mencoderCropDetect.CropString)) && (mencoderCropDetect.CropHeight > 0) && (mencoderCropDetect.CropWidth > 0))
            {
                if ((mencoderCropDetect.CropWidth <= _width) && (mencoderCropDetect.CropHeight <= _height))
                {
                    _cropWidth = Util.MathLib.RoundOff(mencoderCropDetect.CropWidth, 16); // Round width to the nearest multiple of 16
                    _cropHeight = Util.MathLib.RoundOff(mencoderCropDetect.CropHeight, 8); // Round height to the nearest multiple of 8
                    _cropString = mencoderCropDetect.GenerateCropString(_cropWidth, _cropHeight, mencoderCropDetect.CropStartX, mencoderCropDetect.CropStartY); // Get the new crop string
                    jobLog.WriteEntry(this, "Crop String : " + _cropString + ", Crop Height " + mencoderCropDetect.CropHeight + ", Crop Width " + mencoderCropDetect.CropWidth, Log.LogEntryType.Debug);
                }
                else
                    _jobLog.WriteEntry(this, ("MEncoder Crop Detect Process error, Crop size > Video size, ignoring cropping. Crop Width:" + _cropWidth + " Crop Height:" + _cropHeight + " Video Width:" + _width + " Video Height:" + _height), Log.LogEntryType.Warning);
            }
            else
                _jobLog.WriteEntry(this, ("MEncoder Crop Detect Process - no cropping detected"), Log.LogEntryType.Information);
        }

        /// <summary>
        /// Video codec name (remuxed video if passed else original)
        /// </summary>
        public string VideoCodec
        { get { return _videoCodec; } }

        /// <summary>
        /// Audio codec name (remuxed video if passed else original)
        /// </summary>
        public string AudioCodec
        { get { return _audioCodec; } }

        /// <summary>
        /// FPS of video (remuxed video if passed else original)
        /// </summary>
        public float Fps
        { get { return _fps; } }

        /// <summary>
        /// Height of the video (remuxed video if passed else original)
        /// </summary>
        public int Height
        { get { return _height; } }

        /// <summary>
        /// Width of the video (remuxed video if passed else original)
        /// </summary>
        public int Width
        { get { return _width; } }

        /// <summary>
        /// Final cropped height (remuxed video if passed else original)
        /// </summary>
        public int CropHeight
        { get { return _cropHeight; } }

        /// <summary>
        /// Final cropped width (remuxed video if passed else original)
        /// </summary>
        public int CropWidth
        { get { return _cropWidth; } }

        /// <summary>
        /// Raw crop string from cropdetect (remuxed video if passed else original)
        /// </summary>
        public string CropString
        { get { return _cropString; } }

        /// <summary>
        /// True if error getting video properties (remuxed video if passed else original)
        /// </summary>
        public bool Error
        { get { return _error; } }

        /// <summary>
        /// Path to EDL File
        /// </summary>
        public string EDLFile
        { get { return _EDLFile; } }

        /// <summary>
        /// Original filename
        /// </summary>
        public string OriginalFileName
        { get { return _originalFileName; } }

        /// <summary>
        /// Original file extension
        /// </summary>
        public string OriginalFileExtension
        { get { return (FilePaths.CleanExt(_originalFileName)); } }

        /// <summary>
        /// True if cropping was enabled and detected (remuxed video if passed else original)
        /// </summary>
        public bool CropDetected
        { get { return ((_cropHeight > 0) && (_cropWidth > 0)); } }

        /// <summary>
        /// Duration of video (remuxed video if passed else original)
        /// </summary>
        public float Duration
        { get { return _duration; } }

        /// <summary>
        /// Audio delay in video (remuxed video if passed else original)
        /// </summary>
        public float AudioDelay
        { get { return _audioDelay; } }

        /// <summary>
        /// Remuxed filename if passed
        /// </summary>
        public string RemuxedFileName
        { get { return _remuxedFileName; } }

        /// <summary>
        /// This represents the source file active in use for conversion. If remuxing has been done, then it returns the remuxed file else the original file (or temp copied file)
        /// </summary>
        public string SourceVideo
        {
            get
            {
                if (!String.IsNullOrEmpty(_remuxedFileName))
                    return _remuxedFileName;
                else
                    return _originalFileName;
            }
        }

        /// <summary>
        /// Number of audio channels if an audio track was selected (remuxed video if passed else original)
        /// </summary>
        public int AudioChannels
        { get { return _audioChannels; } }

        /// <summary>
        /// Audio stream identifier if an audio track was selected (remuxed video if passed else original)
        /// </summary>
        public int AudioStream
        { get { return _audioStream; } }

        /// <summary>
        /// Audio track number if audio track was selected (remuxed video if passed else original)
        /// </summary>
        public int AudioTrack
        { get { return _audioTrack; } }

        /// <summary>
        /// Video stream identifier  (remuxed video if passed else original)
        /// </summary>
        public int VideoStream
        { get { return _videoStream; } }

        /// <summary>
        /// Audio PID if audio track was selected (remuxed video if passed else original)
        /// </summary>
        public int AudioPID
        { get { return _audioPID; } }

        /// <summary>
        /// Video PID of video track
        /// </summary>
        public int VideoPID
        { get { return _videoPID; } }

        /// <summary>
        /// FFMPEG Media Info (remuxed video if passed else original)
        /// </summary>
        public FFmpegMediaInfo FFMPEGStreamInfo
        { get { return _ffmpegStreamInfo; } }

        /// <summary>
        /// FFMPEG Media Info for original file
        /// </summary>
        public FFmpegMediaInfo OriginalVideoFFMPEGStreamInfo
        { get { return _originalFileFFmpegStreamInfo; } }

        /// <summary>
        /// Audio language if audio track was selected (remuxed video if passed else original)
        /// </summary>
        public string AudioLanguage
        { get { return _audioLanguage; } }

        /// <summary>
        /// Requested audio language for filtering while checking video properties
        /// </summary>
        public string RequestedAudioLanguage
        { get { return _requestedAudioLanguage; } }

        /// <summary>
        /// Type of Video scan (remuxed video if passed else original)
        /// </summary>
        public ScanType VideoScanType
        { get { return _scanType; } }
    }
}
