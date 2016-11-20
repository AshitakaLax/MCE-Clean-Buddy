using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Net;

using MCEBuddy.BaseClasses;
using MCEBuddy.BaseClasses.DirectShow;
using MCEBuddy.BaseClasses.DSHelper;

namespace MCEBuddy.RemuxMediaCenter
{
    [ComVisible(true)]
    public class DumpInputPin : RenderedInputPin
    {
        #region Constructor

        public DumpInputPin(string _name, BaseFilter _filter)
            :base(_name,_filter)
        {
        }

        #endregion

        #region Overridden Methods

        public override int CheckMediaType(AMMediaType pmt)
        {
            return NOERROR;
        }

        public override int OnReceive(ref IMediaSampleImpl _sample)
        {
            HRESULT hr = (HRESULT)CheckStreaming();
            if (hr != S_OK) return hr;
            return (m_Filter as DumpFilter).OnReceive(ref _sample);
        }

        public override int EndOfStream()
        {
            (m_Filter as DumpFilter).EndOfStream();
            return base.EndOfStream();
        }

        #endregion
    }

    [ComVisible(true)]
    [Guid("60DF815A-784A-4725-8493-C42B166F9D92")]
    [AMovieSetup(true)]
    public class DumpFilter : BaseFilter, IFileSinkFilter, IAMFilterMiscFlags
    {
        #region Variables

        private FileStream m_Stream = null;
        private string m_sFileName = "";

        #endregion

        #region Constructor

        public DumpFilter()
            : base("MCEBuddy Dump Filter")
        {

        }

        #endregion

        #region Overridden Methods

        protected override int OnInitializePins()
        {
            AddPin(new DumpInputPin("Input",this));
            return NOERROR;
        }

        public override int Pause()
        {
            if (m_State == FilterState.Stopped && m_Stream == null)
            {
                if (m_sFileName != "")
                {
                    m_Stream = new FileStream(m_sFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
                }
            }
            return base.Pause();
        }

        public override int Stop()
        {
            int hr = base.Stop();
            if (m_Stream != null)
            {
                m_Stream.Dispose();
                m_Stream = null;
            }
            return hr;
        }

        #endregion

        #region Methods

        public int EndOfStream()
        {
            lock (m_Lock)
            {
                if (m_Stream != null)
                {
                    m_Stream.Dispose();
                    m_Stream = null;
                }
            }
            NotifyEvent(EventCode.Complete, (IntPtr)((int)S_OK), Marshal.GetIUnknownForObject(this));
            return S_OK;
        }

        public int OnReceive(ref IMediaSampleImpl _sample)
        {
            lock (m_Lock)
            {
                if (m_Stream == null && m_sFileName != "")
                {
                    m_Stream = new FileStream(m_sFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
                }

                int _length = _sample.GetActualDataLength();
                if (m_Stream != null && _length > 0)
                {
                    byte[] _data = new byte[_length];
                    IntPtr _ptr;
                    _sample.GetPointer(out _ptr);
                    Marshal.Copy(_ptr, _data, 0, _length);
                    m_Stream.Write(_data, 0, _length);
                }
            }
            return S_OK;
        }

        #endregion

        #region IFileSinkFilter Members

        public int SetFileName(string pszFileName, AMMediaType pmt)
        {
            if (string.IsNullOrEmpty(pszFileName)) return E_POINTER;
            if (IsActive) return VFW_E_WRONG_STATE;
            m_sFileName = pszFileName;
            return NOERROR;
        }

        public int GetCurFile(out string pszFileName, AMMediaType pmt)
        {
            pszFileName = m_sFileName;
            if (pmt != null)
            {
                pmt.Set(Pins[0].CurrentMediaType);
            }
            return NOERROR;
        }

        #endregion

        #region IAMFilterMiscFlags Members

        public int GetMiscFlags()
        {
            return 1;
        }

        #endregion
    }
}
