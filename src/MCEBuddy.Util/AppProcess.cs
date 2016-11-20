using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.IO;
using System.Reflection;

using MCEBuddy.Globals;

namespace MCEBuddy.Util
{
    public static class AppProcess
    {
        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public uint SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_MANDATORY_LABEL
        {
            public SID_AND_ATTRIBUTES Label;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public uint Attributes;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public uint HighPart;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public SePrivilege Attributes;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        };

        [StructLayoutAttribute(LayoutKind.Sequential)]
        struct SECURITY_DESCRIPTOR
        {
            public byte revision;
            public byte size;
            public short control;
            public IntPtr owner;
            public IntPtr group;
            public IntPtr sacl;
            public IntPtr dacl;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public String lpReserved;
            public String lpDesktop;
            public String lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public StartFlags dwFlags;
            public ShowWindow wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        #endregion

        #region Enumerations

        private enum StdHandle : int
        {
            STD_OUTPUT_HANDLE = -11,
            STD_INPUT_HANDLE = -10,
            STD_ERROR_HANDLE = -12
        }

        [Flags]
        private enum ConsoleModes : uint
        {
            ENABLE_PROCESSED_INPUT = 0x0001,
            ENABLE_LINE_INPUT = 0x0002,
            ENABLE_ECHO_INPUT = 0x0004,
            ENABLE_WINDOW_INPUT = 0x0008,
            ENABLE_MOUSE_INPUT = 0x0010,
            ENABLE_INSERT_MODE = 0x0020,
            ENABLE_QUICK_EDIT_MODE = 0x0040,
            ENABLE_EXTENDED_FLAGS = 0x0080,
            ENABLE_AUTO_POSITION = 0x0100,
            ENABLE_PROCESSED_OUTPUT = 0x0001,
            ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002,
        }
        
        private enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }
        
        [Flags]
        private enum HandleFlags
        {
            INHERIT = 1,
            PROTECT_FROM_CLOSE = 2
        }
        
        private enum ShowWindow : short
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11
        }
        
        [Flags]
        private enum StartFlags : uint
        {
            STARTF_USESHOWWINDOW = 0x00000001,
            STARTF_USESIZE = 0x00000002,
            STARTF_USEPOSITION = 0x00000004,
            STARTF_USECOUNTCHARS = 0x00000008,
            STARTF_USEFILLATTRIBUTE = 0x00000010,
            STARTF_RUNFULLSCREEN = 0x00000020,  // ignored for non-x86 platforms
            STARTF_FORCEONFEEDBACK = 0x00000040,
            STARTF_FORCEOFFFEEDBACK = 0x00000080,
            STARTF_USESTDHANDLES = 0x00000100,
        }
        
        private enum MANDATORY_LEVEL
        {
            MandatoryLevelUntrusted = 0,
            MandatoryLevelLow,
            MandatoryLevelMedium,
            MandatoryLevelHigh,
            MandatoryLevelSystem,
            MandatoryLevelSecureProcess,
            MandatoryLevelCount
        };

        private enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass  // MaxTokenInfoClass should always be the last enum
        };

        [Flags]
        private enum StandardAccess : uint
        {
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            SYNCHRONIZE = 0x00100000,
            STANDARD_RIGHTS_REQUIRED = 0x000F0000,
            STANDARD_RIGHTS_READ = READ_CONTROL,
            STANDARD_RIGHTS_WRITE = READ_CONTROL,
            STANDARD_RIGHTS_EXECUTE = READ_CONTROL,
            STANDARD_RIGHTS_ALL = 0x001F0000,
            SPECIFIC_RIGHTS_ALL = 0x0000FFFF,
            ACCESS_SYSTEM_SECURITY = 0x01000000,
            MAXIMUM_ALLOWED = 0x02000000,
            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000,
        }

        [Flags]
        private enum TokenAccess : uint
        {
            TOKEN_ASSIGN_PRIMARY = 0x0001,
            TOKEN_DUPLICATE = 0x0002,
            TOKEN_IMPERSONATE = 0x0004,
            TOKEN_QUERY = 0x0008,
            TOKEN_QUERY_SOURCE = 0x0010,
            TOKEN_ADJUST_PRIVILEGES = 0x0020,
            TOKEN_ADJUST_GROUPS = 0x0040,
            TOKEN_ADJUST_DEFAULT = 0x0080,
            TOKEN_ADJUST_SESSIONID = 0x0100,
            TOKEN_ALL_ACCESS_P = StandardAccess.STANDARD_RIGHTS_REQUIRED |
                                      TOKEN_ASSIGN_PRIMARY |
                                      TOKEN_DUPLICATE |
                                      TOKEN_IMPERSONATE |
                                      TOKEN_QUERY |
                                      TOKEN_QUERY_SOURCE |
                                      TOKEN_ADJUST_PRIVILEGES |
                                      TOKEN_ADJUST_GROUPS |
                                      TOKEN_ADJUST_DEFAULT,
            TOKEN_ALL_ACCESS = TOKEN_ALL_ACCESS_P | TOKEN_ADJUST_SESSIONID,
            TOKEN_READ = StandardAccess.STANDARD_RIGHTS_READ | TOKEN_QUERY,
            TOKEN_WRITE = StandardAccess.STANDARD_RIGHTS_WRITE |
                                      TOKEN_ADJUST_PRIVILEGES |
                                      TOKEN_ADJUST_GROUPS |
                                      TOKEN_ADJUST_DEFAULT,
            TOKEN_EXECUTE = StandardAccess.STANDARD_RIGHTS_EXECUTE,
            MAXIMUM_ALLOWED = 0x2000000,
        }

        [Flags]
        private enum ProcessAccess : uint
        {
            PROCESS_ALL_ACCESS = PROCESS_CREATE_PROCESS | PROCESS_CREATE_THREAD | PROCESS_DUP_HANDLE | PROCESS_QUERY_INFORMATION |
                               PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_SET_INFORMATION | PROCESS_SET_QUOTA | PROCESS_SUSPEND_RESUME |
                               PROCESS_TERMINATE | PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE,
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_CREATE_THREAD = 0x0002,
            PROCESS_DUP_HANDLE = 0x0040,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
            PROCESS_SET_INFORMATION = 0x0200,
            PROCESS_SET_QUOTA = 0x0100,
            PROCESS_SUSPEND_RESUME = 0x0800,
            PROCESS_TERMINATE = 0x0001,
            PROCESS_VM_OPERATION = 0x0008,
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020,
            MAXIMUM_ALLOWED = 0x2000000,
        }

        [Flags]
        private enum SecurityMandatory : uint
        {
            SECURITY_MANDATORY_UNTRUSTED_RID = 0x00000000,
            SECURITY_MANDATORY_LOW_RID = 0x00001000,
            SECURITY_MANDATORY_MEDIUM_RID = 0x00002000,
            SECURITY_MANDATORY_HIGH_RID = 0x00003000,
            SECURITY_MANDATORY_SYSTEM_RID = 0x00004000,
            SECURITY_MANDATORY_PROTECTED_PROCESS_RID = 0x00005000,
        }

        [Flags]
        private enum SePrivilege : uint
        {
            SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001,
            SE_PRIVILEGE_ENABLED = 0x00000002,
            SE_PRIVILEGE_REMOVED = 0X00000004,
            SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000,
            SE_PRIVILEGE_VALID_ATTRIBUTES = SE_PRIVILEGE_ENABLED_BY_DEFAULT |
                                                     SE_PRIVILEGE_ENABLED |
                                                     SE_PRIVILEGE_REMOVED |
                                                     SE_PRIVILEGE_USED_FOR_ACCESS,
        }

        private enum HResult : int
        {
            S_OK = 0,
            E_INVALIDARG = unchecked((int)0x80070057),
            E_UNEXPECTED = unchecked((int)0x8000FFFF),
            E_ACCESSDENIED = unchecked((int)0x80070005),
        }

        private enum WinError : int
        {
            ERROR_INSUFFICIENT_BUFFER = 122,    // dderror
        }

        private enum TOKEN_TYPE : int
        {
            TokenPrimary = 1,
            TokenImpersonation = 2
        }

        private enum SECURITY_IMPERSONATION_LEVEL : int
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3,
        }

        [Flags]
        private enum CreationFlags : int
        {
            NONE = 0,
            DEBUG_PROCESS = 0x00000001,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            CREATE_SUSPENDED = 0x00000004,
            DETACHED_PROCESS = 0x00000008,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NO_WINDOW = 0x08000000,
            IDLE_PRIORITY_CLASS = 0x40,
            NORMAL_PRIORITY_CLASS = 0x20,
            HIGH_PRIORITY_CLASS = 0x80,
            REALTIME_PRIORITY_CLASS = 0x100,
        }

        #endregion

        #region Constants

        private const int READ_BUFFER_SIZE = 8191; // Max length of cmd line in Windows XP and later

        private const TOKEN_INFORMATION_CLASS TokenIntegrityLevel = ((TOKEN_INFORMATION_CLASS)25);

        private const int SECURITY_DESCRIPTOR_REVISION = 0x1;

        private const string SE_CREATE_TOKEN_NAME = "SeCreateTokenPrivilege";
        private const string SE_ASSIGNPRIMARYTOKEN_NAME = "SeAssignPrimaryTokenPrivilege";
        private const string SE_LOCK_MEMORY_NAME = "SeLockMemoryPrivilege";
        private const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
        private const string SE_UNSOLICITED_INPUT_NAME = "SeUnsolicitedInputPrivilege";
        private const string SE_MACHINE_ACCOUNT_NAME = "SeMachineAccountPrivilege";
        private const string SE_TCB_NAME = "SeTcbPrivilege";
        private const string SE_SECURITY_NAME = "SeSecurityPrivilege";
        private const string SE_TAKE_OWNERSHIP_NAME = "SeTakeOwnershipPrivilege";
        private const string SE_LOAD_DRIVER_NAME = "SeLoadDriverPrivilege";
        private const string SE_SYSTEM_PROFILE_NAME = "SeSystemProfilePrivilege";
        private const string SE_SYSTEMTIME_NAME = "SeSystemtimePrivilege";
        private const string SE_PROF_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";
        private const string SE_INC_BASE_PRIORITY_NAME = "SeIncreaseBasePriorityPrivilege";
        private const string SE_CREATE_PAGEFILE_NAME = "SeCreatePagefilePrivilege";
        private const string SE_CREATE_PERMANENT_NAME = "SeCreatePermanentPrivilege";
        private const string SE_BACKUP_NAME = "SeBackupPrivilege";
        private const string SE_RESTORE_NAME = "SeRestorePrivilege";
        private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        private const string SE_DEBUG_NAME = "SeDebugPrivilege";
        private const string SE_AUDIT_NAME = "SeAuditPrivilege";
        private const string SE_SYSTEM_ENVIRONMENT_NAME = "SeSystemEnvironmentPrivilege";
        private const string SE_CHANGE_NOTIFY_NAME = "SeChangeNotifyPrivilege";
        private const string SE_REMOTE_SHUTDOWN_NAME = "SeRemoteShutdownPrivilege";
        private const string SE_UNDOCK_NAME = "SeUndockPrivilege";
        private const string SE_SYNC_AGENT_NAME = "SeSyncAgentPrivilege";
        private const string SE_ENABLE_DELEGATION_NAME = "SeEnableDelegationPrivilege";
        private const string SE_MANAGE_VOLUME_NAME = "SeManageVolumePrivilege";
        private const string SE_IMPERSONATE_NAME = "SeImpersonatePrivilege";
        private const string SE_CREATE_GLOBAL_NAME = "SeCreateGlobalPrivilege";
        private const string SE_TRUSTED_CREDMAN_ACCESS_NAME = "SeTrustedCredManAccessPrivilege";
        private const string SE_RELABEL_NAME = "SeRelabelPrivilege";
        private const string SE_INC_WORKING_SET_NAME = "SeIncreaseWorkingSetPrivilege";
        private const string SE_TIME_ZONE_NAME = "SeTimeZonePrivilege";
        private const string SE_CREATE_SYMBOLIC_LINK_NAME = "SeCreateSymbolicLinkPrivilege";

        #endregion

        #region Win32 API Imports

        [DllImport("wtsapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool WTSQueryUserToken(uint sessionId, out IntPtr Token);
        
        [DllImport("wtsapi32.dll", ExactSpelling = true, SetLastError = false)]
        public static extern void WTSFreeMemory(IntPtr memory);
        
        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern int WTSEnumerateSessions(System.IntPtr hServer, int Reserved, int Version, ref System.IntPtr ppSessionInfo, ref int pCount);
        
        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);
        
        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(StdHandle nStdHandle);
        
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, ConsoleModes dwMode);
        
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ReadFile(IntPtr hFile, [Out] byte[] pBuffer, int NumberOfBytesToRead, out int pNumberOfBytesRead, IntPtr Overlapped);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetHandleInformation(IntPtr hObject, HandleFlags dwMask, HandleFlags dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, uint nSize);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);
    
        [DllImport("kernel32.dll")]
        static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll", SetLastError = false)]
        static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, ref uint dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)] // Only ANSI, other types will fail
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(ProcessAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool SetSecurityDescriptorDacl(ref SECURITY_DESCRIPTOR sd, bool daclPresent, IntPtr dacl, bool daclDefaulted);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool InitializeSecurityDescriptor(out SECURITY_DESCRIPTOR SecurityDescriptor, uint dwRevision);
        
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern Boolean SetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, ref UInt32 TokenInformation, UInt32 TokenInformationLength);
        
        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateProcessWithTokenW(IntPtr hToken, uint dwLogonFlags, string lpApplicationName, string lpCommandLine, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern IntPtr GetSidSubAuthority(IntPtr sid, UInt32 subAuthorityIndex);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern IntPtr GetSidSubAuthorityCount(IntPtr psid);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles, CreationFlags dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)]bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, UInt32 Zero, IntPtr Null1, IntPtr Null2);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DuplicateTokenEx(IntPtr hExistingToken, TokenAccess dwDesiredAccess, ref SECURITY_ATTRIBUTES lpTokenAttributes, SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, out IntPtr phNewToken);

        [DllImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, TokenAccess DesiredAccess, out IntPtr TokenHandle);

        #endregion

        private static bool SUCCEEDED(int hr)
        {
            if (hr >= 0)
                return true;
            else
                return false;
        }

        private static bool FAILED(int hr)
        {
            if (hr < 0)
                return true;
            else
                return false;
        }

        private static DataReceivedEventArgs CreateDataReceivedEventArgs(string data)
        {
            DataReceivedEventArgs eventArgs = (DataReceivedEventArgs)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(DataReceivedEventArgs));

            FieldInfo[] EventFields = typeof(DataReceivedEventArgs).GetFields(BindingFlags.NonPublic | BindingFlags.Instance |BindingFlags.DeclaredOnly);

            if (EventFields.Count() > 0)
                EventFields[0].SetValue(eventArgs, data);
            else
                throw new ApplicationException("Failed to find _data field!");

            return eventArgs;

        }

        /// <summary>
        /// Function that continuously reads the output from a Pipe read handler and calls the process Output processing handler
        /// It continues until the process terminates or throws an exception
        /// </summary>
        /// <param name="procInfo">Process information structure</param>
        /// <param name="hRead">Redirected output pipe read handle (output of pipe)</param>
        /// <param name="processOutputHandler">Process output processing handler to call</param>
        private static void PipeReadThread(PROCESS_INFORMATION procInfo, IntPtr hRead, DataReceivedEventHandler processOutputHandler, Log log)
        {
            try
            {
                bool res;
                byte[] buffer = new byte[READ_BUFFER_SIZE];
                string content = "";

                // This thread ALWAYS runs at time critical priority to ensure it captures and puts out the console data in a timely manner
                // Refer to http://msdn.microsoft.com/en-us/library/windows/desktop/ms685100(v=vs.85).aspx
                IOPriority.SetPriority(ThreadsPriority.THREAD_PRIORITY_TIME_CRITICAL);

                do
                {
                    // Read the output from the pipe into the buffer (upto the length of the buffer)
                    int readBytes = 0;
                    res = ReadFile(hRead, buffer, buffer.Length, out readBytes, IntPtr.Zero);

                    if (res) // If we read something
                    {
                        // TODO: Is this the right encoding? (From the console output we should be getting ASCII characters or should we use Default Encoding or Unicode encoding?)
                        // Convert the byte to a string using the right encoding
                        if (readBytes > 0) // If we have something to read then add it to the existing content buffer (which may be pending a newline to parse it)
                            content += Encoding.Default.GetString(buffer, 0, readBytes); // Convert if we have something to convert, otherwise it's just an emtpy string

                        if (content.Contains('\n') || content.Contains('\r')) // We wait for a new line (caarriage return is converted to new line) character (send one line at a time to avoid issues with parsing)
                        {
                            string[] allLines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'); // Send each line separately (convert \r\n to \n since they represent a new line essentially and then the remaining \r to \n since they represent a new line)
                            IEnumerable<string> newLines = allLines.Take(allLines.Length - 1); // Skip the last one. If it ends in a "\n" then the last element is "", if not then the last element contains a partial string, we will defer it to the next round
                            foreach (string line in newLines)
                            {
                                // Pass the data to the output processing handler
                                DataReceivedEventArgs args = CreateDataReceivedEventArgs(line); // Store the output
                                processOutputHandler(new object(), args);
                            }

                            content = allLines.Last(); // Put the last one back into the content string. If content ended with a newline "\n" then the last element will be "" else it will contain a partial string which we will process in the next round (cannot send partial lines)
                        }
                    }
                } while (res); // Keep doing it until the process terminates (i.e. no output)

                // Check if anything is left in the content buffer, if so now send it
                if (content != "")
                {
                    DataReceivedEventArgs args = CreateDataReceivedEventArgs(content); // Store the output
                    processOutputHandler(new object(), args);
                }
            }
            catch (Exception e)
            {
                log.WriteEntry("Reading process pipe output exception ->\r\n" + e.ToString(), Log.LogEntryType.Error, true);
            }

            // Close the handle
            CloseHandle(hRead);
            CloseHandle(procInfo.hProcess);
            CloseHandle(procInfo.hThread);

            return;
        }

        /// <summary>
        /// Launches the given application with full admin rights, and in addition bypasses the Vista UAC prompt
        /// This uses the first logged on user's Session (Active Console Session) to start the UI process (Session 1 WinLogon in Vista/7/8)
        /// </summary>
        /// <param name="applicationPath">The name and path of the application to launch</param>
        /// <param name="cmdParams">Parameters to pass to the application</param>
        /// <param name="redirectOutput">True to redirect the console output (std+err) to a custom handler</param>
        /// <param name="processOutputHandler">Output redirect handler if true above, else null</param>
        /// <param name="showWindow">True if new process window needs to be shown, false to be hidden</param>
        /// <returns>Proceess Id if successful else 0</returns>
        public static uint StartAppWithAdminPrivilegesFromNonUISession(String applicationPath, string cmdParams, bool redirectOutput, DataReceivedEventHandler processOutputHandler, bool showWindow, Log log)
        {
            string appStartDirectory = null;
            uint winlogonPid = 0;
            IntPtr hUserTokenDup = IntPtr.Zero, hPToken = IntPtr.Zero, hProcess = IntPtr.Zero, hStdOutWrite = IntPtr.Zero, hStdOutRead = IntPtr.Zero;
            PROCESS_INFORMATION procInfo = new PROCESS_INFORMATION();

            // TODO: We need to add support for handling Unicode strings with CreateProcessAsUserW
            // For now we don't support Unicode names
            if (Util.Text.ContainsUnicode(applicationPath) || Util.Text.ContainsUnicode(cmdParams))
            {
                log.WriteEntry("StartAppWithAdminPrivilegesFromNonUISession does not support Unicode right now.", Log.LogEntryType.Error, true);
                return 0;
            }

            // Check for valid handler
            if (redirectOutput && (hStdOutRead == null))
                throw new ArgumentNullException("processOutputHandler", "ProcessOutputHandler null when RedirectOutput is true");

            // Get the path if it is an absolute path, if it's relative we start in the MCEBuddy directory
            if (Path.IsPathRooted(applicationPath))
                appStartDirectory = Path.GetDirectoryName(applicationPath); // Absolute path, get starting path
            else
                appStartDirectory = null; // Relative path starts with MCEBuddy path

            // obtain the currently active session id; every logged on user in the system has a unique session id
            uint dwSessionId = 0;
            IntPtr pSessionInfo = IntPtr.Zero;
            int dwCount = 0;
            WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref pSessionInfo, ref dwCount);

            Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
            Int32 current = (int)pSessionInfo;
            for (int i = 0; i < dwCount; i++)
            {
                WTS_SESSION_INFO wsi = (WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)current, typeof(WTS_SESSION_INFO));
                if (WTS_CONNECTSTATE_CLASS.WTSActive == wsi.State)
                {
                    dwSessionId = wsi.SessionID;
                    break;
                }

                current += dataSize;
            }

            WTSFreeMemory(pSessionInfo); // Free this up

            // Check if there are any users logged into the current session (remote desktop or local machine console)
            IntPtr currentToken = IntPtr.Zero;
            if (!WTSQueryUserToken(dwSessionId, out currentToken))
            {
                int ret = Marshal.GetLastWin32Error();
                log.WriteEntry("StartAppWithAdminPrivilegesFromNonUISession WTSQueryUserToken failed (No logged on users) with error " + ret.ToString() + ". " + WinErrors.GetSystemMessage(ret), Log.LogEntryType.Error, true);
                return 0;
            }

            CloseHandle(currentToken); // Don't need this anymore, release it

            // obtain the process id of the winlogon process that is running within the currently active session
            Process[] processes = Process.GetProcessesByName("winlogon");
            foreach (Process p in processes)
            {
                if ((uint)p.SessionId == dwSessionId)
                {
                    winlogonPid = (uint)p.Id;
                }
            }

            // obtain a handle to the winlogon process
            hProcess = OpenProcess(ProcessAccess.MAXIMUM_ALLOWED, false, winlogonPid);

            // obtain a handle to the access token of the winlogon process
            if (!OpenProcessToken(hProcess, TokenAccess.MAXIMUM_ALLOWED, out hPToken))
            {
                int ret = Marshal.GetLastWin32Error();
                log.WriteEntry("StartAppWithAdminPrivilegesFromNonUISession OpenProcessToken failed with error " + ret.ToString() + ". " + WinErrors.GetSystemMessage(ret), Log.LogEntryType.Error, true);
                CloseHandle(hProcess);
                return 0;
            }

            // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser
            // I would prefer to not have to use a security attribute variable and to just 
            // simply pass null and inherit (by default) the security attributes
            // of the existing token. However, in C# structures are value types and therefore
            // cannot be assigned the null value.
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.nLength = Marshal.SizeOf(sa);

            // copy the access token of the winlogon process; the newly created token will be a primary token (we want to create an admin process in non session 0)
            if (!DuplicateTokenEx(hPToken, TokenAccess.MAXIMUM_ALLOWED, ref sa, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hUserTokenDup))
            {
                int ret = Marshal.GetLastWin32Error();
                log.WriteEntry("StartAppWithAdminPrivilegesFromNonUISession DuplicateTokenEx failed with error " + ret.ToString() + ". " + WinErrors.GetSystemMessage(ret), Log.LogEntryType.Error, true);
                CloseHandle(hProcess);
                CloseHandle(hPToken);
                return 0;
            }

            // By default CreateProcessAsUser creates a process on a non-interactive window station, meaning
            // the window station has a desktop that is invisible and the process is incapable of receiving
            // user input. To remedy this we set the lpDesktop parameter to indicate we want to enable user 
            // interaction with the new process.
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default"; // interactive window station parameter; basically this indicates that the process created can display a GUI on the desktop

            // flags that specify the priority and creation method of the process - by default no window, a new console for the child process (not shared with parent) (don't use NO_WINDOW since it prevents the console output from working)
            CreationFlags dwCreationFlags = CreationFlags.NORMAL_PRIORITY_CLASS | CreationFlags.CREATE_NEW_CONSOLE;
            si.dwFlags |= StartFlags.STARTF_USESHOWWINDOW; // set showwindow information
            si.wShowWindow |= ShowWindow.SW_HIDE;

            // Set the Window information
            if (showWindow)
            {
                si.wShowWindow |= ShowWindow.SW_SHOWNORMAL; // Show show window, by default don't show
            }

            // Check if we need to redirect the output to a custom handler
            if (redirectOutput)
            {
                // Ensure we create a security descriptor with rights to read the pipe otherwise the ReadFile will fail later
                SECURITY_DESCRIPTOR saDesc = new SECURITY_DESCRIPTOR();
                if (!InitializeSecurityDescriptor(out saDesc, SECURITY_DESCRIPTOR_REVISION))
                {
                    int ret = Marshal.GetLastWin32Error();
                    log.WriteEntry("StartAppWithAdminPrivilegesFromNonUISession create initialize security descriptor with error " + ret.ToString() + ". " + WinErrors.GetSystemMessage(ret), Log.LogEntryType.Error, true);
                    CloseHandle(hProcess);
                    CloseHandle(hPToken);
                    CloseHandle(hUserTokenDup);
                    return 0;
                }

                if (!SetSecurityDescriptorDacl(ref saDesc, true, IntPtr.Zero, false))
                {
                    int ret = Marshal.GetLastWin32Error();
                    log.WriteEntry("StartAppWithAdminPrivilegesFromNonUISession set security descriptor failed with error " + ret.ToString() + ". " + WinErrors.GetSystemMessage(ret), Log.LogEntryType.Error, true);
                    CloseHandle(hProcess);
                    CloseHandle(hPToken);
                    CloseHandle(hUserTokenDup);
                    return 0;
                }

                IntPtr saDescPtr = Marshal.AllocHGlobal(Marshal.SizeOf(saDesc));
                Marshal.StructureToPtr(saDesc, saDescPtr, false);
                SECURITY_ATTRIBUTES saAttr = new SECURITY_ATTRIBUTES();
                saAttr.nLength = Marshal.SizeOf(saAttr);
                saAttr.bInheritHandle = true;
                saAttr.lpSecurityDescriptor = saDescPtr;

                // Create a pipe to attach to the StdOut and StdErr outputs
                if (!CreatePipe(out hStdOutRead, out hStdOutWrite, ref saAttr, 0)) // use default buffer size
                {
                    int ret = Marshal.GetLastWin32Error();
                    log.WriteEntry("StartAppWithAdminPrivilegesFromNonUISession create stdout pipe failed with error " + ret.ToString() + ". " + WinErrors.GetSystemMessage(ret), Log.LogEntryType.Error, true);
                    CloseHandle(hProcess);
                    CloseHandle(hPToken);
                    CloseHandle(hUserTokenDup);
                    return 0;
                }

                // Set the StartInfo structure information to use the redirect handles
                si.dwFlags |= StartFlags.STARTF_USESTDHANDLES; // Use custom redirect handles
                si.hStdOutput = hStdOutWrite; // Redirect StdOut (write into pipe handle)
                si.hStdError = hStdOutWrite; // Redirect StdErr (write into pipe handle)
            }

            // Create the environment
            IntPtr lpEnvironment = IntPtr.Zero;
            if (!CreateEnvironmentBlock(out lpEnvironment, hUserTokenDup, true))
            {
                int ret = Marshal.GetLastWin32Error();
                log.WriteEntry("StartAppWithAdminPrivilegesFromNonUISession create environment failed with error " + ret.ToString() + ". " + WinErrors.GetSystemMessage(ret), Log.LogEntryType.Warning, true);
                lpEnvironment = IntPtr.Zero;
            }
            else
                dwCreationFlags |= CreationFlags.CREATE_UNICODE_ENVIRONMENT; // When environment is not null, this flag should be set

            // create a new process in the current user's logon session
            // Refer to http://stackoverflow.com/questions/4053241/windows-api-createprocess-path-with-space
            bool success = CreateProcessAsUser(hUserTokenDup,        // client's access token
                                            applicationPath,        // file to execute (no encapsulation)
                                            Util.FilePaths.FixSpaces(applicationPath) + " " + cmdParams,              // command line - first param (argv[0]) should be encapsulated appPath
                                            ref sa,                 // pointer to process SECURITY_ATTRIBUTES
                                            ref sa,                 // pointer to thread SECURITY_ATTRIBUTES
                                            true,                  // handles are inheritable otherwise we can't redirect output
                                            dwCreationFlags,        // creation flags
                                            lpEnvironment,          // pointer to new environment block 
                                            appStartDirectory,      // name of current directory
                                            ref si,                 // pointer to STARTUPINFO structure
                                            out procInfo            // receives information about new process
                                            );

            if (!success)
            {
                int ret = Marshal.GetLastWin32Error();
                log.WriteEntry("StartAppWithAdminPrivilegesFromNonUISession create process failed with error " + ret.ToString() + ". " + WinErrors.GetSystemMessage(ret), Log.LogEntryType.Error, true);
            }

            // invalidate the handles
            DestroyEnvironmentBlock(lpEnvironment);
            CloseHandle(hProcess);
            CloseHandle(hPToken);
            CloseHandle(hUserTokenDup);
            CloseHandle(hStdOutWrite); // We don't need this, close it now else the thread hangs

            // If we need to redirect output
            if (success && redirectOutput)
            {
                // Start the thread to read the process output - it'll close the procInfo (thread and process handles), hRead and hWrite handles when it's done
                Thread readOutput = new Thread(() => PipeReadThread(procInfo, hStdOutRead, processOutputHandler, log));
                readOutput.IsBackground = true; // Kill the thread when the process terminates 
                readOutput.Start();
            }

            if (success)
                return procInfo.dwProcessId;
            else
                return 0;
        }

        /// <summary>
        /// Runs a process with Medium Privileges (using current logged on session's Windows Explorer as the base security model).
        /// This can ONLY be called from a User Interactive Session (e.g. Windows XP Session 0 or Session 1 and greater for Windows Vista/7/8)
        /// It should NOT be called from a Service running in a non interactive session (Session 0 in Windows Vista/7/8)
        /// Throws an exception if the DLL or app cannot be loaded
        /// </summary>
        /// <param name="appPath">Full path or relative to application</param>
        /// <param name="cmdParameters">Parameters to pass to application</param>
        /// <param name="waitForExit">TRUE is you want to wait until process completes</param>
        public static void StartAppWithMediumPrivilegeFromUISession(string appPath, string cmdParameters, bool waitForExit)
        {
            // Run the process with Medium rights
            ulong pid = CreateProcessWithExplorerIL(appPath, cmdParameters);

            // Wait for process to exit
            if (pid != 0 && waitForExit)
            {
                Process myProcess = Process.GetProcessById((int)pid);
                while (!myProcess.HasExited)
                    Thread.Sleep(1 * 1000);
            }
            else if (pid == 0)
                throw new ArgumentException("Unable to create process using Explorer IL");
        }

        /// <summary>
        /// Check if the current process has Administrative rights
        /// </summary>
        /// <returns>True if the current process has administrative (UAC) rights</returns>
        public static bool HaveAdministrativeRights()
        {
            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return (pricipal.IsInRole(WindowsBuiltInRole.Administrator));
        }

        /// <summary>
        /// Starts an executable file with Administrative rights (using RunAs)
        /// </summary>
        /// <param name="executablePath">Path to executable</param>
        /// <param name="arguments">(Optional) Arguments to pass to executable</param>
        /// <param name="executableDirectory">(Optional) Directory where the exectuable lies if providing a relative executable path or want to start using windows shell</param>
        /// <returns></returns>
        public static bool StartAppWithAdministrativeRights(string executablePath, string arguments = "", string executableDirectory = "")
        {
            if (String.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                return false; // Not a valid selection
            else
            {
                // Start an administrative version of this app and use it to enable the firewall
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.Verb = "runas";
                processInfo.FileName = executablePath;
                if (!String.IsNullOrWhiteSpace(arguments))
                    processInfo.Arguments = arguments;
                if (!String.IsNullOrWhiteSpace(executableDirectory))
                {
                    processInfo.WorkingDirectory = executableDirectory;
                    processInfo.UseShellExecute = true;
                }

                try
                {
                    Process.Start(processInfo);
                }
                catch (Exception)
                {
                    // Probably the user canceled the UAC window
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// @brief Function enables/disables/removes a privelege associated with the given token
        /// @detailed Calls LookupPrivilegeValue() and AdjustTokenPrivileges()
        /// todo: Removing was checked. To check enabling and disabling.
        /// </summary>
        /// <param name="hToken">access token handle</param>
        /// <param name="lpszPrivilege">name of privilege to enable/disable</param>
        /// <param name="dwAttributes">(SE_PRIVILEGE_ENABLED) to enable or (0) disable or (SE_PRIVILEGE_REMOVED) to remove privilege</param>
        /// <returns>HRESULT code</returns>
        private static int SetPrivilege(IntPtr hToken, String lpszPrivilege, SePrivilege dwAttributes = SePrivilege.SE_PRIVILEGE_ENABLED)
        {
            int hr = (int)HResult.S_OK;
            LUID luid = new LUID();

            if (LookupPrivilegeValue(
                    null,            // lookup privilege on local system
                    lpszPrivilege,   // privilege to lookup 
                    out luid))        // receives LUID of privilege
            {
                TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
                tp.PrivilegeCount = 1;
                tp.Privileges.Luid = luid;
                tp.Privileges.Attributes = dwAttributes;

                // Enable the privilege or disable all privileges.

                if (!AdjustTokenPrivileges(
                        hToken,
                        false,
                        ref tp,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero))
                    hr = Marshal.GetLastWin32Error();
            }//if(LookupPrivilegeValue(...))
            else
                hr = Marshal.GetLastWin32Error();

            return hr;
        }

        /// <summary>
        /// Function removes the priveleges which are not associated by default with explorer.exe at Medium Integration Level in Vista
        /// </summary>
        /// <param name="hToken">access token handle</param>
        /// <returns>HRESULT of the operation on SE_CREATE_GLOBAL_NAME (="SeCreateGlobalPrivilege")</returns>
        private static int ReducePrivilegesForMediumIL(IntPtr hToken)
        {
            int hr = (int)HResult.S_OK;
            hr = SetPrivilege(hToken, SE_CREATE_GLOBAL_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);

            SetPrivilege(hToken, SE_BACKUP_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_CREATE_PAGEFILE_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, "SeCreateSymbolicLinkPrivilege", SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_DEBUG_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_IMPERSONATE_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_INC_BASE_PRIORITY_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_INCREASE_QUOTA_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_LOAD_DRIVER_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_MANAGE_VOLUME_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_PROF_SINGLE_PROCESS_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_REMOTE_SHUTDOWN_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_RESTORE_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_SECURITY_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_SYSTEM_ENVIRONMENT_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_SYSTEM_PROFILE_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_SYSTEMTIME_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);
            SetPrivilege(hToken, SE_TAKE_OWNERSHIP_NAME, SePrivilege.SE_PRIVILEGE_REMOVED);

            return hr;
        }

        /// <summary>
        /// @brief Gets Integration level of the given process in Vista. 
        /// In the older OS assumes the integration level is equal to SECURITY_MANDATORY_HIGH_RID
        /// The function opens the process for all access and opens its token for all access.
        /// Then it extracts token information and closes the handles.
        /// @remarks Function check for OS version by querying the presence of Kernel32.GetProductInfo function.
        /// This way is used due to the function is called from InstallShield12 script, so GetVersionEx returns incorrect value.
        /// todo: restrict access rights when quering for tokens
        /// </summary>
        /// <param name="dwProcessId">ID of the process to operate</param>
        /// <param name="pdwProcessIL">pointer to write the value</param>
        /// <returns>HRESULT</returns>
        private static int GetProcessIL(uint dwProcessId, ref uint pdwProcessIL)
        {
            int hr = (int)HResult.S_OK;

            if (SUCCEEDED(hr))
            {
                bool bVista = false;
                {
                    // When the function is called from IS12, GetVersionEx returns dwMajorVersion=5 on Vista!
                    IntPtr hmodKernel32 = LoadLibrary("Kernel32");

                    if ((hmodKernel32 != IntPtr.Zero) && (GetProcAddress(hmodKernel32, "GetProductInfo") != IntPtr.Zero))
                        bVista = true;

                    if (hmodKernel32 != IntPtr.Zero)
                        FreeLibrary(hmodKernel32);
                }

                uint dwIL = (uint)SecurityMandatory.SECURITY_MANDATORY_HIGH_RID;
                if (bVista)
                {//Vista
                    IntPtr hToken = IntPtr.Zero;
                    IntPtr hProcess = OpenProcess(ProcessAccess.PROCESS_ALL_ACCESS, false, dwProcessId);
                    if (hProcess != IntPtr.Zero)
                    {
                        if (OpenProcessToken(hProcess, TokenAccess.TOKEN_ALL_ACCESS, out hToken))
                        {
                            IntPtr pTIL = IntPtr.Zero;
                            uint dwSize = 0;
                            if (!GetTokenInformation(hToken, TokenIntegrityLevel, IntPtr.Zero, 0, out dwSize)
                                && (Marshal.GetLastWin32Error() == (int)WinError.ERROR_INSUFFICIENT_BUFFER)
                                && (dwSize > 0))
                                pTIL = HeapAlloc(GetProcessHeap(), 0, ref dwSize);

                            if ((pTIL != IntPtr.Zero) && GetTokenInformation(hToken, TokenIntegrityLevel, pTIL, dwSize, out dwSize))
                            {
                                ;
                                IntPtr lpb = GetSidSubAuthorityCount(((TOKEN_MANDATORY_LABEL)Marshal.PtrToStructure(pTIL, typeof(TOKEN_MANDATORY_LABEL))).Label.Sid);

                                if (lpb != IntPtr.Zero)
                                {
                                    uint index = (uint)Marshal.ReadInt16(lpb) - 1;
                                    dwIL = (uint)Marshal.ReadInt32(GetSidSubAuthority(((TOKEN_MANDATORY_LABEL)Marshal.PtrToStructure(pTIL, typeof(TOKEN_MANDATORY_LABEL))).Label.Sid, index));
                                }
                                else
                                    hr = (int)HResult.E_UNEXPECTED;
                            }
                            else
                                hr = (int)HResult.E_UNEXPECTED;

                            if (pTIL != IntPtr.Zero)
                                HeapFree(GetProcessHeap(), 0, pTIL);

                            CloseHandle(hToken);
                        }//if(OpenProcessToken(...))
                        else
                            hr = Marshal.GetLastWin32Error();

                        CloseHandle(hProcess);
                    }//if(hProcess)
                    else
                        hr = Marshal.GetLastWin32Error();
                }//if(bVista)

                if (SUCCEEDED(hr))
                    pdwProcessIL = dwIL;
            }//if(SUCCEEDED(hr))
            return hr;
        }

        /// <summary>
        /// @brief Function launches process with the integration level of Explorer on Vista. On the previous OS, simply creates the process.
        /// Function gets the integration level of the current process and Explorer, then launches the new process.
        /// If the integration levels are equal, CreateProcess is called. 
        /// If Explorer has Medium IL, and the current process has High IL, new token is created, its rights are adjusted 
        /// and CreateProcessWithTokenW is called.If Explorer has Medium IL, and the current process has High IL, error is returned.
        /// @note The function cannot be used in services, due to if uses USER32.FindWindow() to get the proper instance of Explorer. 
        /// The parent of new process in taskmgr.exe, but not the current process.
        /// </summary>
        /// <param name="szProcessName">the name of exe file (see CreatePorcess()) </param>
        /// <param name="szCmdLine">the name of exe file (see CreatePorcess())</param>
        /// <returns>Process Id for new process created</returns>
        private static uint CreateProcessWithExplorerIL(string szProcessName, string szCmdLine)
        {
            int hr = (int)HResult.S_OK;

            bool bRet;
            IntPtr hToken = IntPtr.Zero;
            IntPtr hNewToken = IntPtr.Zero;

            bool bVista = false;
            { // When the function is called from IS12, GetVersionEx returns dwMajorVersion=5 on Vista!
                IntPtr hmodKernel32 = LoadLibrary("Kernel32");
                IntPtr prodInfoAddress = GetProcAddress(hmodKernel32, "GetProductInfo");
                if ((hmodKernel32 != IntPtr.Zero) && (prodInfoAddress != IntPtr.Zero))
                    bVista = true;
                if (hmodKernel32 != IntPtr.Zero)
                    FreeLibrary(hmodKernel32);
            }

            PROCESS_INFORMATION ProcInfo = new PROCESS_INFORMATION();
            STARTUPINFO StartupInfo = new STARTUPINFO();

            if (bVista)
            {
                uint dwCurIL = (uint)SecurityMandatory.SECURITY_MANDATORY_HIGH_RID;
                uint dwExplorerID = 0, dwExplorerIL = (uint)SecurityMandatory.SECURITY_MANDATORY_HIGH_RID;

                IntPtr hwndShell = FindWindow("Progman", null);
                if (hwndShell != IntPtr.Zero)
                    GetWindowThreadProcessId(hwndShell, out dwExplorerID);
                else
                {
                    hr = Marshal.GetLastWin32Error();
                    if (SUCCEEDED(hr))
                        hr = (int) HResult.E_UNEXPECTED; // This is likely because you're running in session 0 so FindWindow couldn't find Progman
                }

                if (SUCCEEDED(hr))
                    hr = GetProcessIL(dwExplorerID, ref dwExplorerIL);
                
                if (SUCCEEDED(hr))
                    hr = GetProcessIL(GetCurrentProcessId(), ref dwCurIL);

                if (SUCCEEDED(hr))
                {
                    if ((dwCurIL >= (uint)SecurityMandatory.SECURITY_MANDATORY_HIGH_RID) && (dwExplorerIL == (uint)SecurityMandatory.SECURITY_MANDATORY_MEDIUM_RID))
                    {
                        IntPtr hProcess = OpenProcess(ProcessAccess.PROCESS_ALL_ACCESS, false, dwExplorerID);
                        if (hProcess != IntPtr.Zero)
                        {
                            if (OpenProcessToken(hProcess, TokenAccess.TOKEN_ALL_ACCESS, out hToken))
                            {
                                SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                                sa.nLength = Marshal.SizeOf(sa);

                                if (!DuplicateTokenEx(hToken, TokenAccess.TOKEN_ALL_ACCESS, ref sa, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out hNewToken))
                                    hr = Marshal.GetLastWin32Error();
                                if (SUCCEEDED(hr))
                                {
                                    hr = ReducePrivilegesForMediumIL(hNewToken);

                                    if (SUCCEEDED(hr))
                                    {
                                        bRet = CreateProcessWithTokenW(hNewToken, 0, szProcessName, szCmdLine, 0, IntPtr.Zero, null, ref StartupInfo, out ProcInfo);
                                        if (!bRet)
                                            hr = Marshal.GetLastWin32Error();
                                    }//if(SUCCEEDED(hr))
                                    CloseHandle(hNewToken);
                                }//if (DuplicateTokenEx(...)
                                else
                                    hr = Marshal.GetLastWin32Error();
                                CloseHandle(hToken);
                            }//if(OpenProcessToken(...))
                            else
                                hr = Marshal.GetLastWin32Error();
                            CloseHandle(hProcess);
                        }//if(hProcess)
                        else
                            hr = Marshal.GetLastWin32Error();
                    }//if(dwCurIL==SECURITY_MANDATORY_HIGH_RID && dwExplorerIL==SECURITY_MANDATORY_MEDIUM_RID)
                    else if ((dwCurIL == (uint)SecurityMandatory.SECURITY_MANDATORY_MEDIUM_RID) && (dwExplorerIL == (uint)SecurityMandatory.SECURITY_MANDATORY_HIGH_RID))
                        hr = (int)HResult.E_ACCESSDENIED;
                }//if(SUCCEEDED(hr))
            }//if(bVista)

            if (SUCCEEDED(hr) && (ProcInfo.dwProcessId == 0))
            {// 2K | XP | Vista & !UAC
                SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                sa.nLength = Marshal.SizeOf(sa);

                bRet = CreateProcess(szProcessName, szCmdLine, ref sa, ref sa, false, 0, IntPtr.Zero, null, ref StartupInfo, out ProcInfo);
                if (!bRet)
                    hr = Marshal.GetLastWin32Error();
            }// 2K | XP | Vista & !UAC

            if (SUCCEEDED(hr))
                return ProcInfo.dwProcessId;
            else
                return 0;
        }
    }
}
