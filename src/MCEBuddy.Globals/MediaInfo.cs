using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCEBuddy.Globals
{
    /// <summary>
    /// Class that represents Media information (default values are "" or -1)
    /// </summary>
    [Serializable]
    public class MediaInfo
    {
        /// <summary>
        /// Video information, only 1 video track per file
        /// </summary>
        public Video VideoInfo; // only 1 video track per file
        /// <summary>
        /// List of Audio information, 1 per audio track
        /// </summary>
        public List<Audio> AudioInfo = new List<Audio>(); // can have multiple audio tracks per file
        /// <summary>
        /// List of Subtitle information, 1 per subtitle track
        /// </summary>
        public List<Subtitle> SubtitleInfo = new List<Subtitle>(); // can have multiple subtitles per file
        /// <summary>
        /// List of Chapter information
        /// </summary>
        public List<Chapter> ChapterInfo = new List<Chapter>(); // can have multiple chapters per file

        public override string ToString()
        {
            string print = "";

            print += "\r\nVIDEO TRACK INFO ->\r\n";
            print += VideoInfo.ToString();

            foreach (Audio audio in AudioInfo)
            {
                print += "\r\nAUDIO TRACK INFO ->\r\n";
                print += audio.ToString();
            }

            foreach (Subtitle subtitle in SubtitleInfo)
            {
                print += "\r\nSUBTITLE TRACK INFO ->\r\n";
                print += subtitle.ToString();
            }

            print += "\r\nCHAPTER INFO ->\r\n";
            foreach (Chapter chapter in ChapterInfo)
            {
                print += chapter.ToString() + "\r\n";
            }

            return print;
        }

        /// <summary>
        /// Chapter Information
        /// </summary>
        [Serializable]
        public class Chapter
        {
            /// <summary>
            /// Chatper number
            /// </summary>
            public int No = -1;
            /// <summary>
            /// Start time for the chapter in ms
            /// </summary>
            public int StartTime = -1;
            /// <summary>
            /// End time for the chapter in ms
            /// </summary>
            public int EndTime = -1;
            /// <summary>
            /// Chapter name
            /// </summary>
            public string Name = "";

            public override string ToString()
            {
                string print = "";

                print += "Chapter No -> " + No.ToString() + "\r\n";
                print += "Start time (ms) -> " + StartTime.ToString() + "\r\n";
                print += "End time (ms) -> " + EndTime.ToString() + "\r\n";
                print += "Chapter name -> " + Name + "\r\n";

                return print;
            }
        }

        /// <summary>
        /// Video information
        /// </summary>
        [Serializable]
        public class Video
        {
            /// <summary>
            /// Video stream number
            /// </summary>
            public int Stream = -1;
            /// <summary>
            /// Video codec
            /// </summary>
            public string VideoCodec = "";
            /// <summary>
            /// Duration of video in seconds
            /// </summary>
            public float Duration = 0;
            /// <summary>
            /// Color space format
            /// </summary>
            public string Format = "";
            /// <summary>
            /// Height in pixels
            /// </summary>
            public int Height = 0;
            /// <summary>
            /// Width in pixels
            /// </summary>
            public int Width = 0;
            /// <summary>
            /// Storage aspect ratio
            /// </summary>
            public string SAR = ""; // Storage Aspect Ratio
            /// <summary>
            /// Display aspect ratio
            /// </summary>
            public string DAR = ""; // Display Aspect Ratio
            /// <summary>
            /// Video Bitrate in kb/s
            /// </summary>
            public int BitRate = -1; // Video BitRate
            /// <summary>
            /// Video FPS
            /// </summary>
            public float FPS = 0; // Frames per second
            /// <summary>
            /// PID for the stream
            /// </summary>
            public int PID = -1; // PID for the stream

            public override string ToString()
            {
                string print = "";

                print += "Video stream -> " + Stream.ToString() + "\r\n";
                print += "Video codec -> " + VideoCodec + "\r\n";
                print += "Duration (s) -> " + Duration.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\r\n";
                print += "Color format -> " + Format + "\r\n";
                print += "Height (pixels) -> " + Height.ToString() + "\r\n";
                print += "Width (pixels) -> " + Width.ToString() + "\r\n";
                print += "Storage aspect ratio (SAR) -> " + SAR + "\r\n";
                print += "Display aspect ratio (DAR) -> " + DAR + "\r\n";
                print += "Video bitrate (kb/s) -> " + BitRate.ToString() + "\r\n";
                print += "Frames per seconds (FPS) -> " + FPS.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\r\n";
                print += "Video stream PID -> " + PID.ToString() + "\r\n";

                return print;
            }
        }

        /// <summary>
        /// Audio information
        /// </summary>
        [Serializable]
        public class Audio
        {
            /// <summary>
            /// Audio stream number
            /// </summary>
            public int Stream = -1; // Audio Stream number in the file
            /// <summary>
            /// Audio language
            /// </summary>
            public string Language = ""; // Audio language code
            /// <summary>
            /// Audio codec
            /// </summary>
            public string AudioCodec = "";
            /// <summary>
            /// Sampling frequency
            /// </summary>
            public int Rate = -1; // Sampling frequency
            /// <summary>
            /// Number of audio channels
            /// </summary>
            public int Channels = -1; // Number of channels in this audio stream
            /// <summary>
            /// Number of bits per sample
            /// </summary>
            public int SamplingBits = -1; // number of bits per sample
            /// <summary>
            /// Audio bitrate
            /// </summary>
            public int BitRate = -1; // Audio bitrate
            /// <summary>
            /// Audio PID for stream
            /// </summary>
            public int PID = -1; // PID for the stream
            /// <summary>
            /// Is this a visual / hearing impaired stream
            /// </summary>
            public bool Impaired = false; // This is a hearing/visual impaired stream

            public override string ToString()
            {
                string print = "";

                print += "Audio stream -> " + Stream.ToString() + "\r\n";
                print += "Audio language -> " + Language + "\r\n"; 
                print += "Audio codec -> " + AudioCodec + "\r\n";
                print += "Sampling frequency (Hz) -> " + Rate.ToString() + "\r\n";
                print += "Audio channels -> " + Channels.ToString() + "\r\n";
                print += "Bits per sample -> " + SamplingBits.ToString() + "\r\n";
                print += "Audio bitrate (kb/s) -> " + BitRate.ToString() + "\r\n";
                print += "Audio stream PID -> " + PID.ToString() + "\r\n";
                print += "Impaired track (audio or visual) -> " + Impaired.ToString() + "\r\n";

                return print;
            }
        }

        /// <summary>
        /// Subtitle information
        /// </summary>
        [Serializable]
        public class Subtitle
        {
            /// <summary>
            /// Subtitle stream number
            /// </summary>
            public int Stream = -1; // Subtitle stream number
            /// <summary>
            /// Subtitle language
            /// </summary>
            public string Language = ""; // Subtitle language
            /// <summary>
            /// Subtitle codec name
            /// </summary>
            public string Name = ""; // Subtitle codec name
            /// <summary>
            /// Subtitle PID for stream
            /// </summary>
            public int PID = -1; // PID for the stream

            public override string ToString()
            {
                string print = "";

                print += "Subtitle stream -> " + Stream.ToString() + "\r\n";
                print += "Subtitle language -> " + Language + "\r\n";
                print += "Subtitle codec name -> " + Name + "\r\n";
                print += "Subtitle stream PID -> " + PID.ToString() + "\r\n";

                return print;
            }
        }
    }
}
