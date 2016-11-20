using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace MCEBuddy.Util
{
    public static class OSVersion
    {
        #region Enumerations
        /// <summary>
        /// List of known Windows Operating Systems
        /// </summary>
        public enum OS
        {
            UNKNOWN = 0,
            WIN_CE,
            WIN_3_1,
            WIN_95,
            WIN_98,
            WIN_ME,
            NT_3_51,
            NT_4_0,
            WIN_2000,
            WIN_XP,
            WIN_2003,
            WIN_VISTA_2008_SERVER,
            WIN_7_2008_SERVER_R2,
            WIN_8_2012_SERVER,
            WIN_8_1_2013_SERVER_R2,
        }
        #endregion

        #region Imports
        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public short wServicePackMajor;
            public short wServicePackMinor;
            public short wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        [DllImport("kernel32.dll")]
        private static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);
        #endregion

        #region PrivateConstants
        private const int VER_NT_WORKSTATION = 0x0000001;
        private const int VER_NT_DOMAIN_CONTROLLER = 0x0000002;
        private const int VER_NT_SERVER = 0x0000003;

        private const int VER_SUITE_BACKOFFICE = 0x00000004;
        private const int VER_SUITE_BLADE = 0x00000400;
        private const int VER_SUITE_COMPUTE_SERVER = 0x00004000;
        private const int VER_SUITE_DATACENTER = 0x00000080;
        private const int VER_SUITE_ENTERPRISE = 0x00000002;
        private const int VER_SUITE_EMBEDDEDNT = 0x00000040;
        private const int VER_SUITE_PERSONAL = 0x00000200;
        private const int VER_SUITE_SINGLEUSERTS = 0x00000100;
        private const int VER_SUITE_SMALLBUSINESS = 0x00000001;
        private const int VER_SUITE_SMALLBUSINESS_RESTRICTED = 0x00000020;
        private const int VER_SUITE_STORAGE_SERVER = 0x00002000;
        private const int VER_SUITE_TERMINAL = 0x00000010;
        private const int VER_SUITE_WH_SERVER = 0x00008000;
        #endregion

        #region PublicProperties

        /// <summary>
        /// Returns the Environment.OSVersion (compensating for newer builds like Win 8.1)
        /// </summary>
        public static OperatingSystem TrueOSVersion
        { get { return Environment.OSVersion; } }

        /// <summary>
        /// Gets the full version of the operating system running on this computer.
        /// </summary>
        public static string OSVersionNumber
        { get { return Environment.OSVersion.Version.ToString(); } }

        /// <summary>
        /// Gets the major version of the operating system running on this computer.
        /// </summary>
        public static int OSMajorVersion
        { get { return Environment.OSVersion.Version.Major; } }

        /// <summary>
        /// Gets the minor version of the operating system running on this computer.
        /// </summary>
        public static int OSMinorVersion
        { get { return Environment.OSVersion.Version.Minor; } }

        /// <summary>
        /// Gets the build version of the operating system running on this computer.
        /// </summary>
        public static int OSBuildVersion
        { get { return Environment.OSVersion.Version.Build; } }

        /// <summary>
        /// Gets the revision version of the operating system running on this computer.
        /// </summary>
        public static int OSRevisionVersion
        { get { return Environment.OSVersion.Version.Revision; } }
        
        #endregion


        /// <summary>
        /// Returns the product type of the operating system running on this computer.
        /// </summary>
        /// <returns>A string containing the the operating system product type.</returns>
        public static string GetOSProductType()
        {
            OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX();
            OperatingSystem osInfo = Environment.OSVersion;

            osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

            if (!GetVersionEx(ref osVersionInfo))
            {
                return "";
            }
            else
            {
                if (osInfo.Version.Major == 4) // Windows NT
                {
                    if (osVersionInfo.wProductType == VER_NT_WORKSTATION)
                    {
                        // Windows NT 4.0 Workstation
                        return "Workstation";
                    }
                    else if (osVersionInfo.wProductType == VER_NT_SERVER)
                    {
                        // Windows NT 4.0 Server
                        return "Server";
                    }
                    else
                    {
                        return "";
                    }
                }
                else if (osInfo.Version.Major >= 5) // Windows 2000 or later
                {
                    if (osVersionInfo.wProductType == VER_NT_WORKSTATION)
                    {
                        if ((osVersionInfo.wSuiteMask & VER_SUITE_PERSONAL) == VER_SUITE_PERSONAL)
                        {
                            // Windows XP Home Edition
                            return "Home Edition";
                        }
                        else
                        {
                            // Windows XP / Windows 2000 Professional
                            return "Professional";
                        }
                    }
                    else // Server or Domain Controller
                    {
                        if ((osVersionInfo.wSuiteMask & VER_SUITE_DATACENTER) == VER_SUITE_DATACENTER)
                        {
                            // Windows Server 2003 Datacenter Edition
                            return "Datacenter Edition";
                        }
                        else if ((osVersionInfo.wSuiteMask & VER_SUITE_ENTERPRISE) == VER_SUITE_ENTERPRISE)
                        {
                            // Windows Server 2003 Enterprise Edition
                            return "Enterprise Edition";
                        }
                        else if ((osVersionInfo.wSuiteMask & VER_SUITE_BLADE) == VER_SUITE_BLADE)
                        {
                            // Windows Server 2003 Web Edition
                            return "Web Edition";
                        }
                        else if ((osVersionInfo.wSuiteMask & VER_SUITE_COMPUTE_SERVER) == VER_SUITE_COMPUTE_SERVER)
                        {
                            // Windows Server 2003 Web Edition
                            return "Compute Cluster Edition";
                        }
                        else if ((osVersionInfo.wSuiteMask & VER_SUITE_STORAGE_SERVER) == VER_SUITE_STORAGE_SERVER)
                        {
                            // Windows Server 2003 Web Edition
                            return "Storage Server Edition";
                        }
                        else if ((osVersionInfo.wSuiteMask & VER_SUITE_WH_SERVER) == VER_SUITE_WH_SERVER)
                        {
                            // Windows Server 2003 Web Edition
                            return "Home Server Edition";
                        }
                        else
                        {
                            // Windows Server 2003 Standard Edition
                            return "Standard Edition";
                        }
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// Returns the service pack information of the operating system running on this computer.
        /// </summary>
        /// <returns>A string containing the the operating system service pack information.</returns>
        public static string GetOSServicePack()
        {
            OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX();

            osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

            if (!GetVersionEx(ref osVersionInfo))
            {
                return "";
            }
            else
            {
                return " " + osVersionInfo.szCSDVersion;
            }
        }

        /// <summary>
        /// Returns the current Operating System
        /// </summary>
        /// <returns>Operating System</returns>
        public static OS GetOSVersion()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                    return OS.WIN_3_1;

                case PlatformID.Win32Windows:
                    switch (Environment.OSVersion.Version.Minor)
                    {
                        case 0:
                            return OS.WIN_95;
                        case 10:
                            return OS.WIN_98;
                        case 90:
                            return OS.WIN_ME;
                        default:
                            return OS.UNKNOWN;
                    }

                case PlatformID.Win32NT:
                    switch (Environment.OSVersion.Version.Major)
                    {
                        case 3:
                            return OS.NT_3_51;
                        case 4:
                            return OS.NT_4_0;
                        case 5:
                            switch (Environment.OSVersion.Version.Minor)
                            {
                                case 0:
                                    return OS.WIN_2000;
                                case 1:
                                    return OS.WIN_XP;
                                case 2:
                                    return OS.WIN_2003;
                                default:
                                    return OS.UNKNOWN;
                            }
                        case 6:
                            switch (Environment.OSVersion.Version.Minor)
                            {
                                case 0:
                                    return OS.WIN_VISTA_2008_SERVER;
                                case 1:
                                    return OS.WIN_7_2008_SERVER_R2;
                                case 2:
                                    return OS.WIN_8_2012_SERVER;
                                case 3:
                                    return OS.WIN_8_1_2013_SERVER_R2;
                                default:
                                    return OS.UNKNOWN;
                            }
                        default:
                            return OS.UNKNOWN;
                    }

                case PlatformID.WinCE:
                    return OS.WIN_CE;

                default:
                    return OS.UNKNOWN;
            }
        }
    }
}
