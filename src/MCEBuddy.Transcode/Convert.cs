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
    public class Convert
    {
        private string _convertedFile = "";
        private JobStatus _jobStatus;
        private Log _jobLog;
        private bool _subtitleBurned = false;

        public Convert(JobStatus jobStatus, Log jobLog)
        {
            _jobStatus = jobStatus;
            _jobLog = jobLog;
        }

        /// <summary>
        /// Return the converted file
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

        /// <summary>
        /// Returns the final extension for the file to be converted
        /// </summary>
        /// <param name="conversionOptions">Conversion Options</param>
        /// <returns>Extension as specified in the Conversion Profile</returns>
        public static string GetConversionExtension(ConversionJobOptions conversionOptions)
        {
            Ini ini = new Ini(GlobalDefs.ProfileFile);
            string orderSetting = ini.ReadString(conversionOptions.profile, "order", "").ToLower().Trim();
            if (!orderSetting.Contains("mencoder")) orderSetting = orderSetting.Replace("me","mencoder");
            if (!orderSetting.Contains("handbrake")) orderSetting = orderSetting.Replace("hb", "handbrake");
            if (!orderSetting.Contains("ffmpeg")) orderSetting = orderSetting.Replace("ff", "ffmpeg");

            string[] tool = orderSetting.Split(',');

            // We can check the first tool since all tools will lead to the same final extension
            string extension = ini.ReadString(conversionOptions.profile, tool[0] + "-ext", "").ToLower().Trim();
            string remuxTo = ini.ReadString(conversionOptions.profile, tool[0] + "-remuxto", "").ToLower().Trim();

            if (!string.IsNullOrEmpty(remuxTo))
            {
                if (remuxTo[0] != '.') remuxTo = "." + remuxTo;  // Just in case someone does something dumb like forget the leading "."
                return remuxTo;
            }
            else
            {
                if (extension[0] != '.') extension = "." + extension;  // Just in case someone does something dumb like forget the leading "."
                return extension;
            }
        }

        /// <summary>
        /// Gets the order of the encoders in the specified profile
        /// </summary>
        /// <param name="profile">Profile to get encoders order</param>
        /// <returns>String array containing the encoders in execution order</returns>
        public static string[] GetProfileEncoderOrder(string profile)
        {
            Ini ini = new Ini(GlobalDefs.ProfileFile);

            string orderSetting = ini.ReadString(profile, "order", "").ToLower().Trim();
            if (!orderSetting.Contains("mencoder")) orderSetting = orderSetting.Replace("me", "mencoder");
            if (!orderSetting.Contains("handbrake")) orderSetting = orderSetting.Replace("hb", "handbrake");
            if (!orderSetting.Contains("ffmpeg")) orderSetting = orderSetting.Replace("ff", "ffmpeg");

            string[] order = orderSetting.Split(',');

            return order;
        }

        public bool Run(ConversionJobOptions conversionOptions, VideoInfo videoFile, Scanner commercialScan, string srtFile)
        {
            bool converted = false;
            Ini ini = new Ini(GlobalDefs.ProfileFile);

            // Dump the entire profile for debugging purposes (incase users have customized it)
            _jobLog.WriteEntry("Profile being used : " + conversionOptions.profile + ".\r\nProfile entries ->", Log.LogEntryType.Debug);
            SortedList<string, string> profileEntries = ini.GetSectionKeyValuePairs(conversionOptions.profile);
            foreach (string key in profileEntries.Keys)
            {
                _jobLog.WriteEntry(key + "=" + profileEntries[key], Log.LogEntryType.Debug);
            }

            string[] order = GetProfileEncoderOrder(conversionOptions.profile);

            foreach (string encoder in order)
            {
                switch (encoder.Trim())
                {
                    case "copy":
                        {
                            _jobLog.WriteEntry(this, Localise.GetPhrase("Using special case COPY for converter"), Log.LogEntryType.Information);

                            // Special case, no real encoder, just ignore any recoding and assume the output = input file
                            ConvertWithCopy convertWithCopy = new ConvertWithCopy(conversionOptions, "copy", videoFile, _jobStatus, _jobLog, commercialScan);
                            if (!convertWithCopy.Unsupported)
                            {
                                _jobLog.WriteEntry(this, Localise.GetPhrase("Converting with COPY"), Log.LogEntryType.Information);

                                bool ret = convertWithCopy.Convert();
                                if (ret)
                                {
                                    converted = true;
                                    _convertedFile = convertWithCopy.ConvertedFile;
                                    videoFile.ConversionTool = "copy";
                                }
                                else
                                {
                                    _jobLog.WriteEntry(this, Localise.GetPhrase("COPY did not convert successfully, using fallback if configured"), Log.LogEntryType.Error);
                                }
                            }

                            break;
                        }
                    case "mencoder":
                        {
                            ConvertWithMencoder convertWithMencoder = new ConvertWithMencoder(conversionOptions, "mencoder", videoFile, _jobStatus, _jobLog, commercialScan);
                            if (!convertWithMencoder.Unsupported)
                            {
                                _jobLog.WriteEntry(this, Localise.GetPhrase("Converting with MEncoder"), Log.LogEntryType.Information);
                                
                                bool ret = convertWithMencoder.Convert();
                                if (ret)
                                {
                                    converted = true;
                                    _convertedFile = convertWithMencoder.ConvertedFile;
                                    videoFile.ConversionTool = "mencoder";
                                }
                                else
                                {
                                    _jobLog.WriteEntry(this, Localise.GetPhrase("MEncoder did not convert successfully, using fallback if configured"), Log.LogEntryType.Error);
                                }
                            }
                            else
                                _jobLog.WriteEntry(this, Localise.GetPhrase("Unsupported MEncoder file formats"), Log.LogEntryType.Error);

                            break;
                        }
                    case "handbrake":
                        {
                            ConvertWithHandbrake convertWithHandbrake = new ConvertWithHandbrake(conversionOptions, "handbrake", videoFile, _jobStatus, _jobLog, commercialScan);
                            if (!convertWithHandbrake.Unsupported)
                            {
                                _jobLog.WriteEntry(this, Localise.GetPhrase("Converting with Handbrake"), Log.LogEntryType.Information); 
                                
                                bool ret = convertWithHandbrake.Convert();
                                if (ret)
                                {
                                    converted = true;
                                    _convertedFile = convertWithHandbrake.ConvertedFile;
                                    videoFile.ConversionTool = "handbrake";
                                }
                                else
                                {
                                    _jobLog.WriteEntry(this, Localise.GetPhrase("Handbrake did not convert successfully, using fallback if configured"), Log.LogEntryType.Error);
                                }
                            }
                            else
                                _jobLog.WriteEntry(this, Localise.GetPhrase("Unsupported Handbrake file formats"), Log.LogEntryType.Error);

                            break;
                        }
                    case "ffmpeg":
                        {
                            ConvertWithFfmpeg convertWithFfmpeg = new ConvertWithFfmpeg(conversionOptions, "ffmpeg", videoFile, _jobStatus, _jobLog, commercialScan, srtFile);
                            if (!convertWithFfmpeg.Unsupported)
                            {
                                _jobLog.WriteEntry(this, Localise.GetPhrase("Converting with FFMpeg"), Log.LogEntryType.Information); 

                                bool ret = convertWithFfmpeg.Convert();
                                if (ret)
                                {
                                    converted = true;
                                    _convertedFile = convertWithFfmpeg.ConvertedFile;
                                    _subtitleBurned = convertWithFfmpeg.SubtitleBurned; // Right now only ffmpeg supports subtitle burning
                                    videoFile.ConversionTool = "ffmpeg";
                                }
                                else
                                {
                                    _jobLog.WriteEntry(this, Localise.GetPhrase("FFMpeg did not convert successfully, using fallback if configured"), Log.LogEntryType.Error);
                                }
                            }
                            else
                                _jobLog.WriteEntry(this, Localise.GetPhrase("Unsupported FFMpeg file formats"), Log.LogEntryType.Error);

                            break;
                        }
                    default:
                        {
                            _jobLog.WriteEntry(Localise.GetPhrase("Unsupported converter"), Log.LogEntryType.Error); 
                            break;
                        }
                }

                if (converted || _jobStatus.Cancelled)
                    break;
            }

            if (!converted)
                _jobLog.WriteEntry(this, Localise.GetPhrase("Unable to convert file") + " " + Path.GetFileName(videoFile.SourceVideo) + " " + Localise.GetPhrase("using profile") + " " + conversionOptions.profile, Log.LogEntryType.Error);
            else
            {
                _jobLog.WriteEntry(this, Localise.GetPhrase("Successfully converted file") + " " + Path.GetFileName(videoFile.SourceVideo) + " " + Localise.GetPhrase("using profile") + " " + conversionOptions.profile, Log.LogEntryType.Debug); 

                //Reset the error message incase there was a fallback conversion the suceeded
                _jobStatus.ErrorMsg = "";
            }

            return converted;
        }
    }
}
