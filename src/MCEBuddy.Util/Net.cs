using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using BOOL = System.Boolean;
using DWORD = System.UInt32;
using LPWSTR = System.String;
using NET_API_STATUS = System.UInt32;

namespace MCEBuddy.Util
{
    public class Net
    {
        #region Constants
        private const int NO_ERROR = 0;
        private const int ERROR_MORE_DATA = 234;
        private const int ERROR_NOT_CONNECTED = 2250;
        private const int UNIVERSAL_NAME_INFO_LEVEL = 1;
        private const int ERROR_INVALID_PARAMETER = 87;
        #endregion

        #region Structures
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct USE_INFO_2
        {
            internal LPWSTR ui2_local;
            internal LPWSTR ui2_remote;
            internal LPWSTR ui2_password;
            internal DWORD ui2_status;
            internal DWORD ui2_asg_type;
            internal DWORD ui2_refcount;
            internal DWORD ui2_usecount;
            internal LPWSTR ui2_username;
            internal LPWSTR ui2_domainname;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct UNIVERSAL_NAME_INFO
        {
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpUniversalName;
        }
        #endregion

        #region DllImports
        [DllImport("NetApi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern UInt32 NetUseAdd(string UncServerName, UInt32 Level, ref USE_INFO_2 Buf, out UInt32 ParmError);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WNetGetConnection([MarshalAs(UnmanagedType.LPTStr)] string localName, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder remoteName, ref int length);

        [DllImport("mpr.dll")]
        private static extern int WNetGetUniversalName(string lpLocalPath, int dwInfoLevel, ref UNIVERSAL_NAME_INFO lpBuffer, ref int lpBufferSize);

        [DllImport("mpr", CharSet = CharSet.Auto)]
        private static extern int WNetGetUniversalName(string lpLocalPath, int dwInfoLevel, IntPtr lpBuffer, ref int lpBufferSize);
        #endregion


        public static string GetUNCPath(string mappedDrive)
        {
            UNIVERSAL_NAME_INFO rni = new UNIVERSAL_NAME_INFO();
            int bufferSize = Marshal.SizeOf(rni);

            int nRet = WNetGetUniversalName(mappedDrive, UNIVERSAL_NAME_INFO_LEVEL, ref rni, ref bufferSize);

            if (ERROR_MORE_DATA == nRet)
            {
                IntPtr pBuffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    nRet = WNetGetUniversalName(mappedDrive, UNIVERSAL_NAME_INFO_LEVEL, pBuffer, ref bufferSize);

                    if (NO_ERROR == nRet)
                    {
                        rni = (UNIVERSAL_NAME_INFO)Marshal.PtrToStructure(pBuffer, typeof(UNIVERSAL_NAME_INFO));
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pBuffer);
                }
            }
            switch (nRet)
            {
                case NO_ERROR:
                    return rni.lpUniversalName;

                case ERROR_NOT_CONNECTED:
                    return mappedDrive;

                default:
                    return mappedDrive;
            }
        }

        private static int CharOccurs(string stringToSearch, char charToFind)
        {
            int count = 0;
            char[] chars = stringToSearch.ToCharArray();
            foreach (char c in chars)
            {
                if (c == charToFind)
                {
                    count++;
                }
            }
            return count;
        }

        public static bool IsUNCPath(string NetPath)
        {
            if (NetPath.Length < 5) // Smallest path is 5 characters -> \\x\x
                return false;
            
            if (NetPath.Substring(0, 2) == "\\\\")
            {
                if (CharOccurs(NetPath, '\\') > 2) // atleast 3 slashes in the path
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Authenticates a network share
        /// </summary>
        /// <param name="unc">UNC to network share</param>
        /// <param name="domainName">Domain name</param>
        /// <param name="userName">Username</param>
        /// <param name="password">Password</param>
        /// <param name="invalidParamName">If windows returns error 87 (Invalid Param), this contains the name of the parameter which was invalid</param>
        /// <returns>Returns the error code from Windows NetUseAdd</returns>
        public static int ConnectShare(string unc, string domainName, string userName, string password, out string invalidParamName)
        {
            invalidParamName = "";

            USE_INFO_2 useInfo = new USE_INFO_2();
            useInfo.ui2_local = string.Empty;
            useInfo.ui2_remote = unc;
            useInfo.ui2_password = password;
            useInfo.ui2_asg_type = 0;    //disk drive
            useInfo.ui2_usecount = 1;
            useInfo.ui2_username = userName;
            useInfo.ui2_domainname = domainName;
            uint paramErrorIndex;

            uint returnCode = NetUseAdd(null, 2, ref useInfo, out paramErrorIndex);

            if (returnCode == ERROR_INVALID_PARAMETER) // If we have an invalid parameter, then identify the type of invalid parameter
            {
                switch (paramErrorIndex)
                {
                    case 1:
                        invalidParamName = "1: Local device name (null)";
                        break;

                    case 2:
                        invalidParamName = "2: Remote share name (" + unc + ")";
                        break;

                    case 3:
                        invalidParamName = "3: Password (" + new String('*', password.Length) + ")";
                        break;

                    case 4:
                        invalidParamName = "4: ASG Remote resource type (disk)";
                        break;

                    case 5:
                        invalidParamName = "5: Username (" + userName + ")";
                        break;

                    case 6:
                        invalidParamName = "6: Domain name (" + domainName + ")";
                        break;

                    default:
                        invalidParamName = paramErrorIndex.ToString() + ": Unknown parameter";
                        break;
                }
            }

            return (int)returnCode;
        }
    }
}
