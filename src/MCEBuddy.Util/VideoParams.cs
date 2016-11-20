using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MCEBuddy.Util;

namespace MCEBuddy.Util
{
    public enum ScanType
    {
        Unknown = 0,
        Interlaced = 1,
        Progressive = 2,
        Telecine = 3
    }

    public static class VideoParams
    {
        /// <summary>
        /// Returns the scan type (Progressive or Interlaced)
        /// </summary>
        /// <param name="rawFileName">File name</param>
        /// <returns>ScanType (Unknown if it cannot be determined)</returns>
        public static ScanType VideoScanType(string rawFileName)
        {
            try
            {
                MediaInfoDll mi = new MediaInfoDll(rawFileName);
                mi.Option("Inform", "Video;%ScanType%");
                string ret = mi.Inform().ToLower().Trim();

                if (ret.Contains("mbaff") || ret.Contains("paff")) // TODO: What's the right way to interpret the results here
                    return ScanType.Interlaced; // MBAFF contains both interlaced and progressive so we treat at interlaced
                else if (ret.Contains("interlaced"))
                    return ScanType.Interlaced;
                else if (ret.Contains("progressive"))
                    return ScanType.Progressive;
                else if (ret.Contains("telecine"))
                    return ScanType.Telecine;
                else
                    return ScanType.Unknown;
            }
            catch (Exception)
            {
                return ScanType.Unknown;
            }
        }

        /// <summary>
        /// Returns the video codec used
        /// </summary>
        /// <param name="rawFileName">File name</param>
        /// <returns>"" if unable to get the video codec name</returns>
        public static string VideoFormat(string rawFileName)
        {
            try
            {
                MediaInfoDll mi = new MediaInfoDll(rawFileName);
                mi.Option("Inform", "General;%Video_Format_WithHint_List%");
                string ret = mi.Inform().ToLower().Trim();
                return ret;
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Gets the audio format
        /// </summary>
        /// <param name="rawFileName">File name</param>
        /// <returns>"" if unable to get the audio codec name</returns>
        public static string AudioFormat(string rawFileName)
        {
            try
            {
                MediaInfoDll mi = new MediaInfoDll(rawFileName);
                mi.Option("Inform", "General;%Audio_Format_WithHint_List%");
                string ret = mi.Inform().ToLower().Trim();
                return ret;
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// FPS for the Video
        /// </summary>
        /// <param name="rawFileName">File name</param>
        /// <returns>0 if unable to get the FPS</returns>
        public static float FPS(string rawFileName)
        {
            try
            {
                float fpsout = 0;

                MediaInfoDll mi = new MediaInfoDll(rawFileName);
                mi.Option("Inform", "Video;%FrameRate%");
                float.TryParse(mi.Inform(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fpsout);
                
                return fpsout;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the Audio Delay from a video file in seconds
        /// </summary>
        /// <param name="rawFileName">Video file</param>
        /// <returns>0 if unable to get audio delay</returns>
        public static float AudioDelay(string rawFileName)
        {
            try
            {
                float audioDelay = 0;

                MediaInfoDll mi = new MediaInfoDll(rawFileName);
                mi.Option("Inform", "Audio; %Video_Delay%");
                float.TryParse(mi.Inform(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out audioDelay);
                audioDelay = ((float)audioDelay) / 1000; // convert to seconds
                
                return audioDelay;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the video width
        /// </summary>
        /// <param name="rawFileName">File name</param>
        /// <returns>0 if unable to get the video width</returns>
        public static int VideoWidth(string rawFileName)
        {
            try
            {
                int width = 0;

                MediaInfoDll mi = new MediaInfoDll(rawFileName);
                mi.Option("Inform", "Video; %Width%");
                int.TryParse(mi.Inform(), out width);
                
                return width;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the video height
        /// </summary>
        /// <param name="rawFileName">File name</param>
        /// <returns>0 if unable to get the video height</returns>
        public static int VideoHeight(string rawFileName)
        {
            try
            {
                int height = 0;

                MediaInfoDll mi = new MediaInfoDll(rawFileName);
                mi.Option("Inform", "Video; %Height%");
                int.TryParse(mi.Inform(), out height);
                
                return height;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        
        
        /// <summary>
        /// Gets the video duration (in seconds)
        /// </summary>
        /// <param name="rawFileName">File name</param>
        /// <returns>0 if unable to get the video duration</returns>
        public static float VideoDuration(string rawFileName)
        {
            try
            {
                float duration = 0;
                int durationMs = 0;

                MediaInfoDll mi = new MediaInfoDll(rawFileName);
                mi.Option("Inform", "Video; %Duration%");
                int.TryParse(mi.Inform(), out durationMs);
                if (durationMs > 0)
                    duration = ((float)durationMs) / 1000;

                return duration;
            }
            catch (Exception)
            {
                return 0;
            }
        }


        /// <summary>
        /// Gets the audio channels
        /// </summary>
        /// <param name="rawFileName">File name</param>
        /// <returns>-1 if unable to get the audio channels</returns>
        public static float AudioChannels(string rawFileName)
        {
            try
            {
                int _audioChannels = -1;

                MediaInfoDll mi = new MediaInfoDll(rawFileName);
                mi.Option("Inform", "Audio; %Channels%");
                int.TryParse(mi.Inform(), out _audioChannels);

                return _audioChannels;
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
