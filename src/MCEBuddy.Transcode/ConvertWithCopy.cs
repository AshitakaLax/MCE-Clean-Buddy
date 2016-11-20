using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MCEBuddy.Globals;
using MCEBuddy.VideoProperties;
using MCEBuddy.Util;
using MCEBuddy.CommercialScan;

namespace MCEBuddy.Transcode
{
    public class ConvertWithCopy : ConvertBase
    {
        public ConvertWithCopy(ConversionJobOptions conversionOptions, string tool, VideoInfo videoFile, JobStatus jobStatus, Log jobLog, Scanner commercialScan)
            : base(conversionOptions, tool, videoFile, jobStatus, jobLog, commercialScan)
        {

        }

        protected override void FinalSanityCheck()
        {

        }

        protected override bool IsPresetVideoWidth()
        {
            return false;
        }

        protected override bool ConstantVideoQuality
        {
            get { return false; }
        }

        protected override void SetVideoOutputFrameRate()
        {
        }

        protected override void SetVideoTrim()
        {
        }

        protected override void SetVideoBitrateAndQuality()
        {
        }

        protected override void SetVideoResize()
        {
        }

        protected override void SetVideoDeInterlacing()
        {
        }

        protected override void SetVideoCropping()
        {
        }

        protected override void SetVideoAspectRatio()
        {
        }

        protected override void SetAudioLanguage()
        {
        }

        protected override void SetAudioChannels()
        {
        }

        protected override void SetAudioPostDRC()
        {
        }

        protected override void SetAudioPreDRC() // ffmpeg needs to setup this parameter before the inputs file because it applies to decoding the input
        {
        }

        protected override void SetAudioVolume()
        {
        }

        protected override void SetInputFileName() // general parameters already setup, now add the input filename details
        {
        }

        protected override void SetOutputFileName() // general + input + video + audio setup, now add the output filename
        {
        }

        protected override bool ConvertWithTool()
        {
            try
            {
                //nothing here
                File.Copy(SourceVideo, _convertedFile); // Same file just copy it over since we need to preserve the original/remuxed file
                _jobStatus.PercentageComplete = 100;
                return true;
            }
            catch (Exception e)
            {
                _jobLog.WriteEntry("Error processing file with Copy Encoder, error -> " + e.ToString(), Log.LogEntryType.Error);
                return false;
            }
        }
    }
}
