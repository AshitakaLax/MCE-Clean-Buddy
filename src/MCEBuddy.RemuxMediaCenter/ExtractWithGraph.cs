using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using MCEBuddy.BaseClasses;
using MCEBuddy.BaseClasses.DSHelper;
using MCEBuddy.BaseClasses.DirectShow;
using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.Configuration;

namespace MCEBuddy.RemuxMediaCenter
{
    public class ExtractWithGraph : IDisposable
    {
        #region Definitions
        [Flags]
        public enum ExtractMediaType
        {
            None = 0x0,
            Audio = 0x1,
            Video = 0x2,
            Subtitle = 0x4
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint DllGetClassObject(
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid rclsid,
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid riid,
            [MarshalAs(UnmanagedType.IUnknown, IidParameterIndex=1)]
            out object ppv
        );
        
        [ComImport, ComVisible(false), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000001-0000-0000-C000-000000000046")]
        public interface IClassFactory
        {
            void CreateInstance(
                [MarshalAs(UnmanagedType.IUnknown)] 
                object pUnkOuter, 
                [MarshalAs(UnmanagedType.LPStruct)] 
                Guid riid,
                [MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 1)]
                out object ppvObject);
        }
        #endregion

        private const double INFINITE_LOOP_CHECK_THRESHOLD = 1.5; // Max size of extracted stream
        private Guid CLSID_DVRMSDecryptTag = new Guid("{C4C4C4F2-0049-4E2B-98FB-9537F6CE516D}");
        private Guid CLSID_WTVDecryptTag = new Guid("{09144FD6-BB29-11DB-96F1-005056C00008}");
        private Guid CLSID_DumpFilter = new Guid("{60DF815A-784A-4725-8493-C42B166F9D92}");
        //private Guid CLSID_SubtitleDecoder = new Guid("{212690FB-83E5-4526-8FD7-74478B7939CD}");
        //private Guid CLSID_StreamBufferSourceFilter = new Guid("{C9F5FE02-F851-4EB5-99EE-AD602AF1E619}");
        //private Guid CLSID_TivoSourceFilter = new Guid("{A65FA79B-2D2C-42BD-BAB2-D474B8F01248}");
        //private Guid CLSID_MainConceptDeMultiplexer = new Guid("{136DCBF5-3874-4B70-AE3E-15997D6334F7}");

        private string _SourceFile = "";
        private JobStatus _jobStatus;
        private Log _jobLog;
        private ExtractMediaType _extractMediaType = ExtractMediaType.None;
        private string _workPath = "";

        private Guid _CLSI_Decryptor;
        private string _Ext = "";

        private string _VideoPart = "";
        private List<string> _AudioParts = new List<string>();
        private List<string> _SubtitleParts = new List<string>();
        private bool _SuccessfulExtraction = false;
        
        private FilterGraph _fg;
        private IGraphBuilder _gb;
        IBaseFilter _SourceF;
        private int _gbFiltersCount = 0;

        public bool SuccessfulExtraction
        { get { return _SuccessfulExtraction; } }

        public string VideoPart
        { get { return _VideoPart; } }

        public List<string> AudioParts
        { get { return _AudioParts; } }

        public List<string> SubtitleParts
        { get { return _SubtitleParts; } }

        public ExtractWithGraph(string SourceFile, string workPath, ExtractMediaType mediaType, JobStatus jobStatus, Log jobLog)
        {
            _jobStatus = jobStatus;
            _extractMediaType = mediaType;
            _SourceFile = SourceFile;
            _workPath = workPath;
            _jobLog = jobLog;

            _Ext = FilePaths.CleanExt(SourceFile).Replace(".", "");

            //Set the decryptor type depending on the file type DVR-MS or WTV or TIVO
            if (_Ext == "dvr-ms")
                _CLSI_Decryptor = CLSID_DVRMSDecryptTag;
            else if (_Ext == "wtv")
                _CLSI_Decryptor = CLSID_WTVDecryptTag;

            // Set up base graph
            _fg = new FilterGraph();
            _gb = (IGraphBuilder)_fg;
        }

        private void checkHR(int hr)
        {
            if (hr < 0)
            {
                HRESULT.ThrowExceptionForHR(hr);
            }
        }

        private bool FAILED(int hr)
        {
            if (hr < 0)
                return true;
            else
                return false;
        }

        private bool SUCCEEDED(int hr)
        {
            if (hr < 0)
                return false;
            else
                return true;
        }

        private string GetFullPathWithoutExtension(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
        }

        /// <summary>
        /// Find all the immediate upstream or downstream filters given a filter reference
        /// </summary>
        /// <param name="pFilter">Starting filter</param>
        /// <param name="Dir">Direction to search (upstream or downstream)</param>
        /// <param name="FilterList">Collect the results in this filter list</param>
        /// <returns>True if successful in getting filter chain</returns>
        private bool GetFilterChain(IBaseFilter pFilter, PinDirection Dir, List<IBaseFilter> FilterList)
        {
            int hr;
            IntPtr fetched = IntPtr.Zero;

            if (pFilter == null || FilterList == null)
                return false;

            IEnumPins pEnum;
            IPin[] pPin = new IPin[1];
            hr = pFilter.EnumPins(out pEnum);
            if (FAILED(hr))
                return false;
            
            while (pEnum.Next(pPin.Length, pPin, fetched) == 0)
            {
                // See if this pin matches the specified direction.
                PinDirection ThisPinDir;
                hr = pPin[0].QueryDirection(out ThisPinDir);
                if (FAILED(hr))
                {
                    // Something strange happened.
                    return false;
                }

                if (ThisPinDir == Dir)
                {
                    // Check if the pin is connected to another pin.
                    IPin pPinNext;
                    IntPtr ptr;
                    hr = pPin[0].ConnectedTo(out ptr);
                    if (SUCCEEDED(hr))
                    {
                        // Get the filter that owns that pin.
                        PinInfo PinInfo;
                        pPinNext = (IPin)Marshal.GetObjectForIUnknown(ptr);
                        hr = pPinNext.QueryPinInfo(out PinInfo);
                        if (FAILED(hr) || (PinInfo.filter == null))
                        {
                            // Something strange happened.
                            return false;
                        }

                        // Insert the filter into the list.
                        AddFilterUnique(FilterList, PinInfo.filter);

                        // Go recursive through the filter chain
                        GetFilterChain(PinInfo.filter, Dir, FilterList);
                    }
                }
            }

            return true;
        }

        void AddFilterUnique(List<IBaseFilter> FilterList, IBaseFilter pNew)
        {
            if (pNew == null || FilterList == null)
                return;

            if (!FilterList.Contains(pNew))
                FilterList.Add(pNew);

            return;
        }

        /* // TODO: We need to update this
         * WTV Files Pin Mapping (pin name between ||)
         *  Audio       -> Source Pin |DVR Out - 1| -> PBDA DT Filter |In(Enc/Tag)| |Out| -> Dump |Input|
         *  Video       -> Source Pin |DVR Out - 2| -> PBDA DT Filter |In(Enc/Tag)| |Out| -> Dump |Input|
         *  Subtitle    -> Source Pin |DVR Out - 5| -> PBDA DT Filter |In(Enc/Tag)| |Out| -> Dump |Input|
         *  
         * DVRMS Files Pin Mapping (pin name between ||)
         *  Audio       -> Source Pin |DVR Out - 1| -> Decrypt/Tag Filter |In(Enc/Tag)| |Out| -> Dump |Input|
         *  Video       -> Source Pin |DVR Out - 3| -> Decrypt/Tag Filter |In(Enc/Tag)| |Out| -> Dump |Input|
         *  Subtitle    -> Source Pin |DVR Out - 2| -> Decrypt/Tag Filter |In(Enc/Tag)| |Out| -> Dump |Input|
         */
        private void ConnectDecryptedDump(string sourceOutPinName, string DumpFileName)
        {
            int hr;
            Type comtype;
            IBaseFilter DecryptF;
            IPin PinOut, PinIn;

            //Create the decrypt filter
            if (_CLSI_Decryptor != MediaType.Null)
            {
                _jobLog.WriteEntry(this, "Connecting Decryption filter", Log.LogEntryType.Debug);
                comtype = Type.GetTypeFromCLSID(_CLSI_Decryptor);
                DecryptF = (IBaseFilter)Activator.CreateInstance(comtype);
                hr = _gb.AddFilter((IBaseFilter)DecryptF, "Decrypt" + _gbFiltersCount++.ToString(CultureInfo.InvariantCulture));
                checkHR(hr);

                DecryptF.FindPin("In(Enc/Tag)", out PinIn); // Get the decrypt filter pinIn |In(Enc/Tag)|
                _SourceF.FindPin(sourceOutPinName, out PinOut); // Get the Source filter pinOut (name taken from sourceOutPinName)

                try
                {
                    // Try to connect the decrypt filter if it is needed
                    hr = _gb.ConnectDirect(PinOut, PinIn, null); // Connect the source filter pinOut to the decrypt filter pinIn
                    checkHR(hr);
                    DecryptF.FindPin("Out", out PinOut); // Get the Decrypt filter pinOut |Out| (for the next filter to connect to)
                }
                catch
                {
                    // Otherwise go direct
                    _SourceF.FindPin(sourceOutPinName, out PinOut); // Otherwise, go direct and get the source filter pinOut (name taken from sourceOutPinName) for the next filter to connect to
                }
            }
            else
                _SourceF.FindPin(sourceOutPinName, out PinOut);  // Otherwise, go direct and get the source filter pinOut (name taken from sourceOutPinName) for the next filter to connect to

            // Check if we need a Video Subtitle decoder (Line 21) (here use the Microsoft DTV decoder) - the subtitles are embedded in the Video stream
            /*if (UseVideoSubtitleDecoder)
            {
                IBaseFilter SubtitleF;
             
                // TODO: We need to add TEE splitter here and a new DUMP filter here and connect the tee output to the DTV decoder and then Line21 to Dump otherwise we end up with either video or Line21, we want both
                _jobLog.WriteEntry(this, "Connecting Video Subtitle Extraction filter", Log.LogEntryType.Debug);
                comtype = Type.GetTypeFromCLSID(CLSID_SubtitleDecoder);
                SubtitleF = (IBaseFilter)Activator.CreateInstance(comtype);
                hr = _gb.AddFilter((IBaseFilter)SubtitleF, "Subtitle" + _gbFilters.Count.ToString(CultureInfo.InvariantCulture));
                checkHR(hr);
                _gbFilters.Add(SubtitleF); // Keep track of filters to be released afterwards

                // Get the subtitle filter pinIn |Video Input|
                SubtitleF.FindPin("Video Input", out PinIn);

                // Try to connect the subtitle filter pinIn to the previous filter pinOut
                hr = _gb.ConnectDirect(PinOut, PinIn, null);
                checkHR(hr);
                SubtitleF.FindPin("~Line21 Output", out PinOut); // Get the new pinOut |~Line21 Output| from the subtitle filter for the next filter to connect to
            }*/

            // Create the dump filter
            DumpFilter df = new DumpFilter();

            // Add the filter to the graph
            hr = _gb.AddFilter(df, "Dump" + _gbFiltersCount++.ToString(CultureInfo.InvariantCulture));
            checkHR(hr);

            // Set destination filename
            hr = df.SetFileName(DumpFileName, null);
            checkHR(hr);

            // Connect the dump filter pinIn |Input| to the previous filter pinOut
            _jobLog.WriteEntry(this, "Connecting MCEBuddy DumpStreams filter pins", Log.LogEntryType.Debug);
            hr = df.FindPin("Input", out PinIn);
            checkHR(hr);
            hr = _gb.ConnectDirect(PinOut, PinIn, null);
            checkHR(hr);
            
            _jobLog.WriteEntry(this, "All filters successfully connected", Log.LogEntryType.Debug);
        }

        /*
         * TIVO Files Pin Mapping (pin name between ||) (NOTE: XXXX changes from each machine and AC3 changes if the audio codec changes)
         *  Audio       -> Source Pin |Output| -> MainConcept MPEG DeMultiplexer |Input| |AC3 (PID XXXX @ Prog# 1)|    -> Dump |Input|
         *  Video       -> Source Pin |Output| -> MainConcept MPEG DeMultiplexer |Input| |Video (PID XXXX @ Prog# 1)|  -> Dump |Input|
         */
        public void BuildGraph()
        {
            int hr;

            IntPtr fetched = IntPtr.Zero;
            IntPtr fetched2 = IntPtr.Zero;
            IEnumPins FilterPins;
            IPin[] pins = new IPin[1];
            string PinID;

            // TiVO Directshow filters are only accessible through userspace otherwise decryption fails, so if we are running the engine as a service (instead of command line) we should prompt the user
            if ((_Ext == "tivo") && GlobalDefs.IsEngineRunningAsService)
                _jobLog.WriteEntry(this, "You need to start MCEBuddy engine as a Command line program. TiVO Desktop Directshow decryption filters do not work with a Windows Service.", Log.LogEntryType.Error);

            // Create the source filter for dvrms or wtv or TIVO (will automatically connect to TIVODecryptorTag in source itself)
            _jobLog.WriteEntry(this, "Loading file using DirectShow source filter", Log.LogEntryType.Debug);
            hr = _gb.AddSourceFilter(_SourceFile, "Source Filter", out _SourceF);
            checkHR(hr);

            // If this is a TIVO while, while the source filter automatically decrypts the inputs we need to connect the MPEG demultiplexer to get the audio and video output pins
            if (_Ext == "tivo")
            {
                IPin PinOut, PinIn;
                IntPtr ptr;
                PinInfo demuxPinInfo;
                List<IBaseFilter> filterList = new List<IBaseFilter>();

                // Check if the source filter is a TiVO source filter (otherwise sometimes it tries to use the normal source filter which will fail since the stream in encrypted)
                string vendorInfo;
                FilterInfo filterInfo;

                _SourceF.QueryFilterInfo(out filterInfo);
                _SourceF.QueryVendorInfo(out vendorInfo);

                _jobLog.WriteEntry(this, "TiVO Source filter loaded by Directshow -> " + filterInfo.achName + " (" + vendorInfo + ")", Log.LogEntryType.Debug);

                if (vendorInfo == null || !vendorInfo.ToLower().Contains("tivo"))
                {
                    string exception = "";
                    
                    // Check if you are running 64Bit MCEBuddy, TiVO needs 32bit MCEBuddy since TiVO directshow dll are 32bit and can only be loaded by 32bit processes
                    if (IntPtr.Size == 8)
                        exception += "You need to run 32bit MCEBuddy, TiVO Directshow fiters cannot be accessed by a 64bit program.";
                    else
                        exception += "TiVO Desktop installation not detected by Windows DirectShow.";

                    throw new Exception(exception); // Get out of here and let the parent know something is wrong
                }

                hr = _SourceF.FindPin("Output", out PinOut); // Get the Source filter pinOut |Output|
                checkHR(hr);

                // When TIVO desktop is installed, Render automatically builds the filter graph with the necessary demuxing filters - we cannot manually add the MainConcept demux filter since the class isn't registered but somehow Render is able to find it and load it (along with other redundant filters like DTV, audio etc which we need to remove)
                _jobLog.WriteEntry(this, "DirectShow building TiVO filter chain", Log.LogEntryType.Debug);
                hr = _gb.Render(PinOut);
                checkHR(hr);

                hr = PinOut.ConnectedTo(out ptr); // Find out which input Pin (Mainconcept Demux filter) the output of the Source Filter is connected to
                checkHR(hr);
                PinIn = (IPin)Marshal.GetObjectForIUnknown(ptr);

                hr = PinIn.QueryPinInfo(out demuxPinInfo); // Get the mainconcept demux filter from the pin
                checkHR(hr);

                demuxPinInfo.filter.QueryFilterInfo(out filterInfo);
                demuxPinInfo.filter.QueryVendorInfo(out vendorInfo);
                _jobLog.WriteEntry(this, "Checking downstream TiVO filter chain starting with TiVO Demux filter -> " + filterInfo.achName + " (" + vendorInfo + ")", Log.LogEntryType.Debug);
                if (!GetFilterChain(demuxPinInfo.filter, PinDirection.Output, filterList)) // Get the list of all downstreams (redudant) filters (like DTV, Audio, video render etc) from the demux filter that were added by the automatic Render function above (check if there are no downstream filters, then TIVO desktop is not installed)
                    throw new Exception("Unable to get TIVO filter chain");

                // Now remove all the filters in the chain downstream after the demux filter from the graph builder (we dont' need them, we will add out own filters later)
                _jobLog.WriteEntry(this, "Removing redundant filters from TiVO filter chain", Log.LogEntryType.Debug);
                foreach (IBaseFilter filter in filterList)
                {
                    filter.QueryFilterInfo(out filterInfo);
                    filter.QueryVendorInfo(out vendorInfo);
                    _jobLog.WriteEntry(this, "Removing filter -> " + filterInfo.achName + " (" + vendorInfo + ")", Log.LogEntryType.Debug);
                    _gb.RemoveFilter(filter);
                    Marshal.FinalReleaseComObject(filter); // Release the COM object
                }

                // Now the TIVO MainConcept Demux Filter is our new "Source" filter
                _SourceF = demuxPinInfo.filter;
            }

            // TODO: We need to find a way to insert a filter which can allow us to select audio streams (e.g. LAV filter, currently it only allows us access to the default audio stream and not multiple audio streams)

            // Cycle through pins, connecting as appropriate
            hr = _SourceF.EnumPins(out FilterPins);
            checkHR(hr);
            while (FilterPins.Next(pins.Length, pins, fetched) == 0)
            {
                IntPtr ptypes = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(IntPtr)));
                AMMediaType mtypes;
                IEnumMediaTypes enummtypes;
                IntPtr ptrEnum;
                pins[0].EnumMediaTypes(out ptrEnum);
                enummtypes = (IEnumMediaTypes)Marshal.GetObjectForIUnknown(ptrEnum);
                while (enummtypes.Next(1, ptypes, fetched2) == 0)
                {
                    /* Extract Audio, Video or Subtitle streams -> References:
                     * http://nate.deepcreek.org.au/svn/DigitalWatch/trunk/bin/MediaTypes.txt
                     * http://msdn.microsoft.com/en-us/library/ms932033.aspx
                     * https://sourceforge.net/p/tsubget/home/Dumping%20a%20Stream/
                     * http://msdn.microsoft.com/en-us/library/windows/desktop/dd695343(v=vs.85).aspx
                     * http://msdn.microsoft.com/en-us/library/windows/desktop/dd390660(v=vs.85).aspx
                     * http://msdn.microsoft.com/en-us/library/windows/desktop/dd407354(v=vs.85).aspx
                     * http://whrl.pl/RcRv5p (extracting Teletext from WTV/DVRMS)
                     */
                    IntPtr ptrStructure = Marshal.ReadIntPtr(ptypes);
                    mtypes = (AMMediaType)Marshal.PtrToStructure(ptrStructure, typeof(AMMediaType));
                    if ((mtypes.majorType == MediaType.Video) ||
                        (mtypes.majorType == MediaType.Audio) ||
                        //(mtypes.majorType == MediaType.Mpeg2PES) ||
                        (mtypes.majorType == MediaType.Stream) ||
                        (mtypes.majorType == MediaType.AuxLine21Data) ||
                        (mtypes.majorType == MediaType.VBI) ||
                        (mtypes.majorType == MediaType.MSTVCaption) ||
                        (mtypes.majorType == MediaType.DTVCCData) ||
                        (mtypes.majorType == MediaType.Mpeg2Sections && mtypes.subType == MediaSubType.None && mtypes.formatType == FormatType.None))
                    {
                        string DumpFileName = "";

                        if ((mtypes.majorType == MediaType.Video) && ((_extractMediaType & ExtractMediaType.Video) != 0)) // Video
                        {
                            DumpFileName = Path.Combine(_workPath, Path.GetFileNameWithoutExtension(_SourceFile) + "_VIDEO");
                            _VideoPart = DumpFileName;
                            _jobLog.WriteEntry(this, "Found Video stream, extracting -> " + DumpFileName, Log.LogEntryType.Debug);
                        }
                        //else if (((mtypes.majorType == MediaType.Audio) || // Audio types https://msdn.microsoft.com/en-us/library/windows/desktop/dd390676(v=vs.85).aspx
                        //                ((mtypes.majorType == MediaType.Mpeg2PES) && ((mtypes.subType == MediaSubType.DolbyAC3) || (mtypes.subType == MediaSubType.DTS) || (mtypes.subType == MediaSubType.DvdLPCMAudio) || (mtypes.subType == MediaSubType.Mpeg2Audio))) ||
                        //                ((mtypes.majorType == MediaType.Stream) && ((mtypes.subType == MediaSubType.DolbyAC3) || (mtypes.subType == MediaSubType.MPEG1Audio) || (mtypes.subType == MediaSubType.Mpeg2Audio) || (mtypes.subType == MediaSubType.DolbyDDPlus) || (mtypes.subType == MediaSubType.MpegADTS_AAC) || (mtypes.subType == MediaSubType.MpegLOAS)))
                        //            ) &&
                        //            ((_extractMediaType & ExtractMediaType.Audio) != 0))
                        //{
                        //    DumpFileName = Path.Combine(_workPath, Path.GetFileNameWithoutExtension(_SourceFile) + "_AUDIO" + AudioParts.Count.ToString());
                        //    _AudioParts.Add(DumpFileName);
                        //    _jobLog.WriteEntry(this, "Found Audio stream, extracting -> " + DumpFileName, Log.LogEntryType.Debug);
                        //}
                        else if ((_extractMediaType & ExtractMediaType.Subtitle) != 0)// Subtitles
                        {
                            DumpFileName = Path.Combine(_workPath, Path.GetFileNameWithoutExtension(_SourceFile) + "_SUBTITLE" + SubtitleParts.Count.ToString());
                            SubtitleParts.Add(DumpFileName);
                            _jobLog.WriteEntry(this, "Found Subtitle stream, extracting -> " + DumpFileName, Log.LogEntryType.Debug);
                        }

                        if (!String.IsNullOrWhiteSpace(DumpFileName)) // If we are asked to extract something
                        {
                            hr = pins[0].QueryId(out PinID);
                            ConnectDecryptedDump(PinID, DumpFileName);
                        }
                    }
                    else
                    {
                        // Debug - looking for more subtitle types (very poorly documented by Microsoft)
                        Guid type = mtypes.majorType;
                        Guid subtype = mtypes.subType;
                        Guid formattyype = mtypes.formatType;
                    }
                }
                Marshal.FreeCoTaskMem(ptypes); // Free up the memory
            }
        }

        public void RunGraph()
        {
            DateTime lastTick = DateTime.Now;
            int hangPeriod = MCEBuddyConf.GlobalMCEConfig.GeneralOptions.hangTimeout;
            long lastVideoSize = 0, lastAudioSize = 0, lastSubtitleSize = 0;
            long totalPartsSize = 0, sourceSize = 0;
            bool AbortError = false;
            int hr = 0;

            IMediaControl mediaControl = (IMediaControl)_fg;
            IMediaEvent mediaEvent = (IMediaEvent)_fg;
            hr = mediaControl.Run();
            checkHR(hr);

            // Change the priority temporarily (need to reset it back after Dumping Streams)
            ProcessPriorityClass lastPriority = GlobalDefs.Priority; // Set it up
            Process.GetCurrentProcess().PriorityClass = GlobalDefs.Priority; // Set the CPU Priority
            IOPriority.SetPriority(GlobalDefs.IOPriority); // First set the CPU priority
                
            // Get filesize of source file
            sourceSize = Util.FileIO.FileSize(_SourceFile); // Sanity checking
            if (sourceSize <= 0)
            {
                _jobLog.WriteEntry(this, "Unable to get source file size, disabling infinite loop checking.", Log.LogEntryType.Warning);
                hangPeriod = 0;
            }

            bool stop = false, isSuspended = false;
            while (!stop)
            {
                System.Threading.Thread.Sleep(100);
                if (_jobStatus.Cancelled)
                {
                    // Received a shutdown command external to the filter extraction
                    _jobLog.WriteEntry(this, "Stream extraction cancelled, aborting graph.", Log.LogEntryType.Warning);
                    stop = true;
                    AbortError = true;
                    mediaControl.Stop();
                    break;
                }

                if (isSuspended)
                    lastTick = DateTime.Now; // Since during suspension there will be no output it shouldn't terminate the process

                if (!isSuspended && GlobalDefs.Pause) // Check if process has to be suspended (if not already)
                {
                    _jobLog.WriteEntry(this, "Stream extraction paused", Log.LogEntryType.Information);
                    mediaControl.Pause();
                    isSuspended = true;
                }

                if (isSuspended && !GlobalDefs.Pause) // Check if we need to resume the process
                {
                    _jobLog.WriteEntry(this, "Stream extraction resumed", Log.LogEntryType.Information);
                    isSuspended = false;
                    mediaControl.Run();
                }

                if (lastPriority != GlobalDefs.Priority) // Check if the priority was changed and if so update it
                {
                    _jobLog.WriteEntry(this, "Stream extraction priority changed", Log.LogEntryType.Information);
                    lastPriority = GlobalDefs.Priority;
                    Process.GetCurrentProcess().PriorityClass = GlobalDefs.Priority; // Set the CPU Priority
                    IOPriority.SetPriority(GlobalDefs.IOPriority); // First set the CPU priority
                }

                EventCode ev;
                IntPtr p1, p2;
                if (mediaEvent.GetEvent(out ev, out p1, out p2, 0) == 0)
                {
                    if (ev == EventCode.Complete)
                    {
                        mediaControl.Stop();
                        stop = true;
                    }
                    else if (ev == EventCode.ErrorAbort || ev == EventCode.UserAbort || ev == EventCode.StErrStopped || ev == EventCode.ErrorAbortEx)
                    {
                        mediaControl.Stop();
                        stop = true;
                        //AbortError = true; - some partial/corrupted files are errored out, we'll handle extraction errors later
                    }
                    mediaEvent.FreeEventParams(ev, p1, p2);
                }

                // Sanity checking to prevent infinite loop for extraction (sometimes steams extracts infinitely)
                // Check if the filesize exceed the initial file size and if so abort the operation
                if (sourceSize > 0)
                {
                    totalPartsSize = 0;

                    // Video file check
                    if ((_extractMediaType & ExtractMediaType.Video) != 0)
                    {
                        long videoSize = Util.FileIO.FileSize(_VideoPart);
                        if (videoSize < 0)
                            _jobLog.WriteEntry(this, "Unable to get extracted video stream file size for infinite loop detection.", Log.LogEntryType.Warning);
                        else if (videoSize > (sourceSize * INFINITE_LOOP_CHECK_THRESHOLD))
                        {
                            _jobLog.WriteEntry(this, "Extracted video stream is greater than " + INFINITE_LOOP_CHECK_THRESHOLD.ToString(CultureInfo.InvariantCulture) + " times the source file size " + (sourceSize/ 1024).ToString("N", CultureInfo.InvariantCulture) + " [KB].\r\nExtraction likely hung, terminating streams extraction.", Log.LogEntryType.Error);
                            stop = true;
                            AbortError = true;
                            mediaControl.Stop();
                            break;
                        }

                        if (hangPeriod > 0)
                            if (videoSize > lastVideoSize) // If we have progress
                                lastTick = DateTime.Now;

                        totalPartsSize += videoSize;
                        lastVideoSize = videoSize;
                    }

                    // Audio file check
                    if ((_extractMediaType & ExtractMediaType.Audio) != 0)
                    {
                        foreach (string audioPart in _AudioParts)
                        {
                            long audioSize = Util.FileIO.FileSize(audioPart);
                            if (audioSize < 0)
                                _jobLog.WriteEntry(this, "Unable to get extracted audio stream file size for infinite loop detection.", Log.LogEntryType.Warning);
                            else if (audioSize > (sourceSize * INFINITE_LOOP_CHECK_THRESHOLD))
                            {
                                _jobLog.WriteEntry(this, "Extracted audio stream is greater than " + INFINITE_LOOP_CHECK_THRESHOLD.ToString(CultureInfo.InvariantCulture) + " times the source file size " + (sourceSize / 1024).ToString("N", CultureInfo.InvariantCulture) + " [KB].\r\nExtraction likely hung, terminating streams extraction.", Log.LogEntryType.Error);
                                stop = true;
                                AbortError = true;
                                mediaControl.Stop();
                                break;
                            }

                            if (hangPeriod > 0)
                                if (audioSize > lastAudioSize) // If we have progress
                                    lastTick = DateTime.Now;

                            totalPartsSize += audioSize;
                            lastAudioSize = audioSize;
                        }
                    }

                    // Subtitle file check
                    if ((_extractMediaType & ExtractMediaType.Subtitle) != 0)
                    {
                        foreach (string subtitlePart in _SubtitleParts)
                        {
                            long subtitleSize = Util.FileIO.FileSize(subtitlePart);
                            if (subtitleSize < 0)
                                _jobLog.WriteEntry(this, "Unable to get extracted subtitle stream file size for infinite loop detection.", Log.LogEntryType.Warning);
                            else if (subtitleSize > (sourceSize * INFINITE_LOOP_CHECK_THRESHOLD))
                            {
                                _jobLog.WriteEntry(this, "Extracted subtitle stream is greater than " + INFINITE_LOOP_CHECK_THRESHOLD.ToString(CultureInfo.InvariantCulture) + " times the source file size " + (sourceSize / 1024).ToString("N", CultureInfo.InvariantCulture) + " [KB].\r\nExtraction likely hung, terminating streams extraction.", Log.LogEntryType.Error);
                                stop = true;
                                AbortError = true;
                                mediaControl.Stop();
                                break;
                            }

                            if (hangPeriod > 0)
                                if (subtitleSize > lastSubtitleSize) // If we have progress
                                    lastTick = DateTime.Now;

                            totalPartsSize += subtitleSize;
                            lastSubtitleSize = subtitleSize;
                        }
                    }

                    if (totalPartsSize < 0)
                        totalPartsSize = 0; // Incase we get -ve numbers

                    _jobStatus.PercentageComplete = (((float)totalPartsSize / (float)sourceSize) > 1 ? 100 : ((float)totalPartsSize / (float)sourceSize) * 100); // Calculate % complete from size estimation (since no recoding is happening) and cap at 100%
                    _jobLog.WriteEntry(this, "Percentage complete : " + _jobStatus.PercentageComplete.ToString("0.00", CultureInfo.InvariantCulture) + " %", Log.LogEntryType.Debug); // Write to file
                }

                // Check if we have reached the end of the file or runs out of disk space, sometime windows just loops endlessly without any incremental output
                // TODO: Should we treat this as an error or normal processing
                if ((hangPeriod > 0) && (DateTime.Now > lastTick.AddSeconds(hangPeriod)))
                {
                    _jobLog.WriteEntry("No response from stream extraction for " + hangPeriod + " seconds, process likely finished, continuing.", Log.LogEntryType.Warning); // Don't treat as an error for now
                    stop = true;
                    // AbortError = true;  // Don't treat as an error for now
                    mediaControl.Stop();
                    break;
                }
            }

            // Reset Priority to Normal
            IOPriority.SetPriority(GlobalDefs.EngineIOPriority); // Set CPU priority after restoring the scheduling priority
            Process.GetCurrentProcess().PriorityClass = GlobalDefs.EnginePriority; // Set the CPU Priority back to Above Normal (engine always runs above normal)

            List<string> parts = _AudioParts.Concat(_SubtitleParts).ToList(); // Create a list of all subtitle, audio parts
            if (!String.IsNullOrWhiteSpace(_VideoPart))
                parts.Add(_VideoPart); // add video part
            _jobLog.WriteEntry(this, "Source " + _SourceFile + " filesize [KB] : " + (sourceSize / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug); // Write to file
            foreach (string part in parts)
            {
                _jobLog.WriteEntry(this, part + " extracted filesize [KB] : " + (FileIO.FileSize(part) / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug); // Write to file
            }
            _jobLog.WriteEntry(this, "Total extracted parts size [KB] : " + (totalPartsSize / 1024).ToString("N", CultureInfo.InvariantCulture), Log.LogEntryType.Debug); // Write to file

            if (!AbortError)
                _SuccessfulExtraction = true;
        }

        public void DeleteParts()
        {
            Util.FileIO.TryFileDelete(_VideoPart);
            foreach (string audioPart in _AudioParts)
            {
                Util.FileIO.TryFileDelete(audioPart);
            }
            foreach (string subtitlePart in _SubtitleParts)
            {
                Util.FileIO.TryFileDelete(subtitlePart);
            }
        }

        public void Dispose()
        {
            // Be goot - release the COM objects
            IEnumFilters filters;
            IntPtr fetched = IntPtr.Zero;
            int hr = _gb.EnumFilters(out filters);
            IBaseFilter[] baseFilters = new IBaseFilter[1];
            while (filters.Next(baseFilters.Length, baseFilters, fetched) == 0)
            {
                FilterInfo info;
                baseFilters[0].QueryFilterInfo(out info);
                if (Marshal.IsComObject(baseFilters[0])) // Only release COM objects
                    Marshal.FinalReleaseComObject(baseFilters[0]);
            }

            Marshal.FinalReleaseComObject(_gb);
        }

    }
}
