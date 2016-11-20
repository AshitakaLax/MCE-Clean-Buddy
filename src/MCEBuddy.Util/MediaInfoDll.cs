// MediaInfoDLL - All info about media files, for DLL
// Copyright (C) 2002-2009 Jerome Martinez, Zen@MediaArea.net
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// MediaInfoDLL - All info about media files, for DLL
// Copyright (C) 2002-2009 Jerome Martinez, Zen@MediaArea.net
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Microsoft Visual C# wrapper for MediaInfo Library
// See MediaInfo.h for help
//
// To make it working, you must put MediaInfo.Dll
// in the executable folder
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

using System;
using System.Runtime.InteropServices;

namespace MCEBuddy.Util
{
    public enum StreamKind
    {
        General,
        Video,
        Audio,
        Text,
        Chapters,
        Image
    }

    public enum InfoKind
    {
        Name,
        Text,
        Measure,
        Options,
        NameText,
        MeasureText,
        Info,
        HowTo
    }

    public enum InfoOptions
    {
        ShowInInform,
        Support,
        ShowInSupported,
        TypeOfValue
    }

    public enum InfoFileOptions
    {
        FileOption_Nothing = 0x00,
        FileOption_NoRecursive = 0x01,
        FileOption_CloseAll = 0x02,
        FileOption_Max = 0x04
    };


    public class MediaInfoDll
    {
        #region DLLImport32
        //Import of DLL functions. DO NOT USE until you know what you do (MediaInfo DLL do NOT use CoTaskMemAlloc to allocate memory)  
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_New")]
        private static extern IntPtr MediaInfo_New32();
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_Delete")]
        private static extern void MediaInfo_Delete32(IntPtr Handle);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_Open")]
        private static extern IntPtr MediaInfo_Open32(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoA_Open")]
        private static extern IntPtr MediaInfoA_Open32(IntPtr Handle, IntPtr FileName);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_Open_Buffer_Init")]
        private static extern IntPtr MediaInfo_Open_Buffer_Init32(IntPtr Handle, Int64 File_Size, Int64 File_Offset);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoA_Open")]
        private static extern IntPtr MediaInfoA_Open32(IntPtr Handle, Int64 File_Size, Int64 File_Offset);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_Open_Buffer_Continue")]
        private static extern IntPtr MediaInfo_Open_Buffer_Continue32(IntPtr Handle, IntPtr Buffer, IntPtr Buffer_Size);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoA_Open_Buffer_Continue")]
        private static extern IntPtr MediaInfoA_Open_Buffer_Continue32(IntPtr Handle, Int64 File_Size, byte[] Buffer, IntPtr Buffer_Size);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_Open_Buffer_Continue_GoTo_Get")]
        private static extern Int64 MediaInfo_Open_Buffer_Continue_GoTo_Get32(IntPtr Handle);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoA_Open_Buffer_Continue_GoTo_Get")]
        private static extern Int64 MediaInfoA_Open_Buffer_Continue_GoTo_Get32(IntPtr Handle);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_Open_Buffer_Finalize")]
        private static extern IntPtr MediaInfo_Open_Buffer_Finalize32(IntPtr Handle);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoA_Open_Buffer_Finalize")]
        private static extern IntPtr MediaInfoA_Open_Buffer_Finalize32(IntPtr Handle);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_Close")]
        private static extern void MediaInfo_Close32(IntPtr Handle);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_Inform")]
        private static extern IntPtr MediaInfo_Inform32(IntPtr Handle, IntPtr Reserved);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoA_Inform")]
        private static extern IntPtr MediaInfoA_Inform32(IntPtr Handle, IntPtr Reserved);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_GetI")]
        private static extern IntPtr MediaInfo_GetI32(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoA_GetI")]
        private static extern IntPtr MediaInfoA_GetI32(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_Get")]
        private static extern IntPtr MediaInfo_Get32(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoA_Get")]
        private static extern IntPtr MediaInfoA_Get32(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_Option")]
        private static extern IntPtr MediaInfo_Option32(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoA_Option")]
        private static extern IntPtr MediaInfoA_Option32(IntPtr Handle, IntPtr Option, IntPtr Value);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_State_Get")]
        private static extern IntPtr MediaInfo_State_Get32(IntPtr Handle);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfo_Count_Get")]
        private static extern IntPtr MediaInfo_Count_Get32(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber);
        #endregion

        #region DLLImport64
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_New")]
        private static extern IntPtr MediaInfo_New64();
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_Delete")]
        private static extern void MediaInfo_Delete64(IntPtr Handle);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_Open")]
        private static extern IntPtr MediaInfo_Open64(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoA_Open")]
        private static extern IntPtr MediaInfoA_Open64(IntPtr Handle, IntPtr FileName);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_Open_Buffer_Init")]
        private static extern IntPtr MediaInfo_Open_Buffer_Init64(IntPtr Handle, Int64 File_Size, Int64 File_Offset);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoA_Open")]
        private static extern IntPtr MediaInfoA_Open64(IntPtr Handle, Int64 File_Size, Int64 File_Offset);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_Open_Buffer_Continue")]
        private static extern IntPtr MediaInfo_Open_Buffer_Continue64(IntPtr Handle, IntPtr Buffer, IntPtr Buffer_Size);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoA_Open_Buffer_Continue")]
        private static extern IntPtr MediaInfoA_Open_Buffer_Continue64(IntPtr Handle, Int64 File_Size, byte[] Buffer, IntPtr Buffer_Size);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_Open_Buffer_Continue_GoTo_Get")]
        private static extern Int64 MediaInfo_Open_Buffer_Continue_GoTo_Get64(IntPtr Handle);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoA_Open_Buffer_Continue_GoTo_Get")]
        private static extern Int64 MediaInfoA_Open_Buffer_Continue_GoTo_Get64(IntPtr Handle);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_Open_Buffer_Finalize")]
        private static extern IntPtr MediaInfo_Open_Buffer_Finalize64(IntPtr Handle);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoA_Open_Buffer_Finalize")]
        private static extern IntPtr MediaInfoA_Open_Buffer_Finalize64(IntPtr Handle);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_Close")]
        private static extern void MediaInfo_Close64(IntPtr Handle);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_Inform")]
        private static extern IntPtr MediaInfo_Inform64(IntPtr Handle, IntPtr Reserved);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoA_Inform")]
        private static extern IntPtr MediaInfoA_Inform64(IntPtr Handle, IntPtr Reserved);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_GetI")]
        private static extern IntPtr MediaInfo_GetI64(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoA_GetI")]
        private static extern IntPtr MediaInfoA_GetI64(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_Get")]
        private static extern IntPtr MediaInfo_Get64(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoA_Get")]
        private static extern IntPtr MediaInfoA_Get64(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_Option")]
        private static extern IntPtr MediaInfo_Option64(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoA_Option")]
        private static extern IntPtr MediaInfoA_Option64(IntPtr Handle, IntPtr Option, IntPtr Value);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_State_Get")]
        private static extern IntPtr MediaInfo_State_Get64(IntPtr Handle);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfo_Count_Get")]
        private static extern IntPtr MediaInfo_Count_Get64(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber);
        #endregion

        private IntPtr Handle = IntPtr.Zero;
        private bool MustUseAnsi;
        private bool Is32Bit = (IntPtr.Size == 4 ? true : false);

        // TODO: Cannot update MediaInfo beyond 0.7.58 due to a hang while trying to Open while reading some network UNC filepaths and some local files while Stream Muxing. See bug https://sourceforge.net/p/mediainfo/bugs/845/
        //MediaInfo class
        public MediaInfoDll(string FileName)
        {
            Handle = (Is32Bit ? MediaInfo_New32() : MediaInfo_New64());
            if (Util.OSVersion.TrueOSVersion.ToString().IndexOf("Windows") == -1)
                MustUseAnsi = true;
            else
                MustUseAnsi = false;

            Open(FileName);
        }
        
        ~MediaInfoDll()
        {
            Close();

            if (Is32Bit)
                MediaInfo_Delete32(Handle);
            else
                MediaInfo_Delete64(Handle);
        }
        
        private int Open(String FileName)
        {
            if (MustUseAnsi)
            {
                IntPtr FileName_Ptr = Marshal.StringToHGlobalAnsi(FileName);
                int ToReturn = (int)(Is32Bit ? MediaInfoA_Open32(Handle, FileName_Ptr) : MediaInfoA_Open64(Handle, FileName_Ptr));
                Marshal.FreeHGlobal(FileName_Ptr);
                return ToReturn;
            }
            else
                return (int)(Is32Bit ? MediaInfo_Open32(Handle, FileName) : MediaInfo_Open64(Handle, FileName));
        }

        private void Close()
        {
            if (Is32Bit)
                MediaInfo_Close32(Handle);
            else
                MediaInfo_Close64(Handle);
        }

        public int Open_Buffer_Init(Int64 File_Size, Int64 File_Offset)
        {
            return (int)(Is32Bit ? MediaInfo_Open_Buffer_Init32(Handle, File_Size, File_Offset) : MediaInfo_Open_Buffer_Init64(Handle, File_Size, File_Offset));
        }

        public int Open_Buffer_Continue(IntPtr Buffer, IntPtr Buffer_Size)
        {
            return (int)(Is32Bit ? MediaInfo_Open_Buffer_Continue32(Handle, Buffer, Buffer_Size) : MediaInfo_Open_Buffer_Continue64(Handle, Buffer, Buffer_Size));
        }
        
        public Int64 Open_Buffer_Continue_GoTo_Get()
        {
            return (int)(Is32Bit ? MediaInfo_Open_Buffer_Continue_GoTo_Get32(Handle) : MediaInfo_Open_Buffer_Continue_GoTo_Get64(Handle));
        }
        
        public int Open_Buffer_Finalize()
        {
            return (int)(Is32Bit ? MediaInfo_Open_Buffer_Finalize32(Handle) : MediaInfo_Open_Buffer_Finalize64(Handle));
        }
        
        public String Inform()
        {
            if (MustUseAnsi)
                return Marshal.PtrToStringAnsi((Is32Bit ? MediaInfoA_Inform32(Handle, (IntPtr)0) : MediaInfoA_Inform64(Handle, (IntPtr)0)));
            else
                return Marshal.PtrToStringUni((Is32Bit ? MediaInfo_Inform32(Handle, (IntPtr)0) : MediaInfo_Inform64(Handle, (IntPtr)0)));
        }
        
        public String Get(StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo, InfoKind KindOfSearch)
        {
            if (MustUseAnsi)
            {
                IntPtr Parameter_Ptr = Marshal.StringToHGlobalAnsi(Parameter);
                String ToReturn = Marshal.PtrToStringAnsi((Is32Bit ? MediaInfoA_Get32(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter_Ptr, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch) : MediaInfoA_Get64(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter_Ptr, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch)));
                Marshal.FreeHGlobal(Parameter_Ptr);
                return ToReturn;
            }
            else
                return Marshal.PtrToStringUni((Is32Bit ? MediaInfo_Get32(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch) : MediaInfo_Get64(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch)));
        }
        
        public String Get(StreamKind StreamKind, int StreamNumber, int Parameter, InfoKind KindOfInfo)
        {
            if (MustUseAnsi)
                return Marshal.PtrToStringAnsi((Is32Bit ? MediaInfoA_GetI32(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo) : MediaInfoA_GetI64(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo)));
            else
                return Marshal.PtrToStringUni((Is32Bit ? MediaInfo_GetI32(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo) : MediaInfo_GetI64(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo)));
        }
        
        public String Option(String Option, String Value)
        {
            if (MustUseAnsi)
            {
                IntPtr Option_Ptr = Marshal.StringToHGlobalAnsi(Option);
                IntPtr Value_Ptr = Marshal.StringToHGlobalAnsi(Value);
                String ToReturn = Marshal.PtrToStringAnsi((Is32Bit ? MediaInfoA_Option32(Handle, Option_Ptr, Value_Ptr) : MediaInfoA_Option64(Handle, Option_Ptr, Value_Ptr)));
                Marshal.FreeHGlobal(Option_Ptr);
                Marshal.FreeHGlobal(Value_Ptr);
                return ToReturn;
            }
            else
                return Marshal.PtrToStringUni((Is32Bit ? MediaInfo_Option32(Handle, Option, Value) : MediaInfo_Option64(Handle, Option, Value)));
        }
        
        public int State_Get()
        { 
            return (int)(Is32Bit ? MediaInfo_State_Get32(Handle) : MediaInfo_State_Get64(Handle)); 
        }
        
        public int Count_Get(StreamKind StreamKind, int StreamNumber) 
        { 
            return (int)(Is32Bit ? MediaInfo_Count_Get32(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber) : MediaInfo_Count_Get64(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber)); 
        }

        //Default values, if you know how to set default values in C#, say me
        public String Get(StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo) 
        { 
            return Get(StreamKind, StreamNumber, Parameter, KindOfInfo, InfoKind.Name); 
        }
        
        public String Get(StreamKind StreamKind, int StreamNumber, String Parameter) 
        { 
            return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text, InfoKind.Name); 
        }
        
        public String Get(StreamKind StreamKind, int StreamNumber, int Parameter) 
        { 
            return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text); 
        }
        
        public String Option(String Option_) 
        { 
            return Option(Option_, ""); 
        }
        
        public int Count_Get(StreamKind StreamKind) 
        { 
            return Count_Get(StreamKind, -1); 
        }
    }


    public class MediaInfoList
    {
        //Import of DLL functions. DO NOT USE until you know what you do (MediaInfo DLL do NOT use CoTaskMemAlloc to allocate memory)  
        #region DLLImport32
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoList_New")]
        private static extern IntPtr MediaInfoList_New32();
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoList_Delete")]
        private static extern void MediaInfoList_Delete32(IntPtr Handle);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoList_Open")]
        private static extern IntPtr MediaInfoList_Open32(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName, IntPtr Options);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoList_Close")]
        private static extern void MediaInfoList_Close32(IntPtr Handle, IntPtr FilePos);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoList_Inform")]
        private static extern IntPtr MediaInfoList_Inform32(IntPtr Handle, IntPtr FilePos, IntPtr Reserved);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoList_GetI")]
        private static extern IntPtr MediaInfoList_GetI32(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoList_Get")]
        private static extern IntPtr MediaInfoList_Get32(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoList_Option")]
        private static extern IntPtr MediaInfoList_Option32(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoList_State_Get")]
        private static extern IntPtr MediaInfoList_State_Get32(IntPtr Handle);
        [DllImport("MediaInfo32.dll", EntryPoint = "MediaInfoList_Count_Get")]
        private static extern IntPtr MediaInfoList_Count_Get32(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber);
        #endregion

        #region DLLImport64
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoList_New")]
        private static extern IntPtr MediaInfoList_New64();
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoList_Delete")]
        private static extern void MediaInfoList_Delete64(IntPtr Handle);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoList_Open")]
        private static extern IntPtr MediaInfoList_Open64(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName, IntPtr Options);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoList_Close")]
        private static extern void MediaInfoList_Close64(IntPtr Handle, IntPtr FilePos);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoList_Inform")]
        private static extern IntPtr MediaInfoList_Inform64(IntPtr Handle, IntPtr FilePos, IntPtr Reserved);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoList_GetI")]
        private static extern IntPtr MediaInfoList_GetI64(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoList_Get")]
        private static extern IntPtr MediaInfoList_Get64(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoList_Option")]
        private static extern IntPtr MediaInfoList_Option64(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoList_State_Get")]
        private static extern IntPtr MediaInfoList_State_Get64(IntPtr Handle);
        [DllImport("MediaInfo64.dll", EntryPoint = "MediaInfoList_Count_Get")]
        private static extern IntPtr MediaInfoList_Count_Get64(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber);
        #endregion

        private IntPtr Handle = IntPtr.Zero;
        private bool Is32Bit = (IntPtr.Size == 4 ? true : false);

        //MediaInfo class
        public MediaInfoList() 
        {
                Handle = (Is32Bit ? MediaInfoList_New32() : MediaInfoList_New64());
        }

        ~MediaInfoList()
        {
            if (Is32Bit) 
                MediaInfoList_Delete32(Handle); 
            else 
                MediaInfoList_Delete64(Handle);
        }

        public int Open(String FileName, InfoFileOptions Options) 
        { 
            return (int)(Is32Bit ? MediaInfoList_Open32(Handle, FileName, (IntPtr)Options) : MediaInfoList_Open64(Handle, FileName, (IntPtr)Options)); 
        }
        
        public void Close(int FilePos) 
        { 
            if(Is32Bit) 
                MediaInfoList_Close32(Handle, (IntPtr)FilePos); 
            else 
                MediaInfoList_Close64(Handle, (IntPtr)FilePos); 
        }
        
        public String Inform(int FilePos) 
        { 
            return Marshal.PtrToStringUni((Is32Bit ? MediaInfoList_Inform32(Handle, (IntPtr)FilePos, (IntPtr)0) : MediaInfoList_Inform64(Handle, (IntPtr)FilePos, (IntPtr)0))); 
        }
        
        public String Get(int FilePos, StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo, InfoKind KindOfSearch) 
        { 
            return Marshal.PtrToStringUni((Is32Bit ? MediaInfoList_Get32(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch) : MediaInfoList_Get64(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch))); 
        }
        
        public String Get(int FilePos, StreamKind StreamKind, int StreamNumber, int Parameter, InfoKind KindOfInfo) 
        { 
            return Marshal.PtrToStringUni((Is32Bit ? MediaInfoList_GetI32(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo) : MediaInfoList_GetI64(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo))); 
        }
        
        public String Option(String Option, String Value) 
        { 
            return Marshal.PtrToStringUni((Is32Bit ? MediaInfoList_Option32(Handle, Option, Value) : MediaInfoList_Option64(Handle, Option, Value))); 
        }
        
        public int State_Get() 
        { 
            return (int)(Is32Bit ? MediaInfoList_State_Get32(Handle) : MediaInfoList_State_Get64(Handle)); 
        }
        
        public int Count_Get(int FilePos, StreamKind StreamKind, int StreamNumber) 
        { 
            return (int)(Is32Bit ? MediaInfoList_Count_Get32(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber) : MediaInfoList_Count_Get64(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber)); 
        }

        //Default values, if you know how to set default values in C#, say me
        public void Open(String FileName) 
        { 
            Open(FileName, 0); 
        }
        
        public void Close() 
        { 
            Close(-1); 
        }
        
        public String Get(int FilePos, StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo) 
        { 
            return Get(FilePos, StreamKind, StreamNumber, Parameter, KindOfInfo, InfoKind.Name); 
        }
        
        public String Get(int FilePos, StreamKind StreamKind, int StreamNumber, String Parameter) 
        { 
            return Get(FilePos, StreamKind, StreamNumber, Parameter, InfoKind.Text, InfoKind.Name); 
        }
        
        public String Get(int FilePos, StreamKind StreamKind, int StreamNumber, int Parameter) 
        { 
            return Get(FilePos, StreamKind, StreamNumber, Parameter, InfoKind.Text); 
        }
        
        public String Option(String Option_) 
        { 
            return Option(Option_, ""); 
        }
        
        public int Count_Get(int FilePos, StreamKind StreamKind) 
        { 
            return Count_Get(FilePos, StreamKind, -1); 
        }
    }

} //NameSpace
