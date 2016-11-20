using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace MCEBuddy.Globals
{
    // Refer to http://msdn.microsoft.com/en-us/library/windows/desktop/ms685100(v=vs.85).aspx

    /// <summary>
    /// I/O scheduling priority
    /// </summary>
    [Flags]
    public enum ProcessPriority : uint
    {
        ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000,
        BELOW_NORMAL_PRIORITY_CLASS = 0x00004000,
        HIGH_PRIORITY_CLASS = 0x00000080,
        IDLE_PRIORITY_CLASS = 0x00000040,
        NORMAL_PRIORITY_CLASS = 0x00000020,
        PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000, // Not supported by Windows XP/2003
        PROCESS_MODE_BACKGROUND_END = 0x00200000, // Not supported by Windows XP/2003
        REALTIME_PRIORITY_CLASS = 0x00000100
    }

    /// <summary>
    /// Thread Priority
    /// </summary>
    public enum ThreadsPriority : int
    {
        THREAD_MODE_BACKGROUND_BEGIN = 0x00010000,
        THREAD_MODE_BACKGROUND_END = 0x00020000,
        THREAD_PRIORITY_ABOVE_NORMAL = 1,
        THREAD_PRIORITY_BELOW_NORMAL = -1,
        THREAD_PRIORITY_HIGHEST = 2,
        THREAD_PRIORITY_IDLE = -15,
        THREAD_PRIORITY_LOWEST = -2,
        THREAD_PRIORITY_NORMAL = 0,
        THREAD_PRIORITY_TIME_CRITICAL = 15
    }

    public class IOPriority
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetPriorityClass(IntPtr handle, ProcessPriority priorityClass);

        [DllImport("kernel32.dll")]
        static extern bool SetThreadPriority(IntPtr hThread, ThreadsPriority nPriority);
        
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        /// <summary>
        /// Suspends a process
        /// </summary>
        /// <param name="PID">Process ID</param>
        public static void SuspendProcess(int PID)
        {
            Process proc = Process.GetProcessById(PID);

            if (proc.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in proc.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }

                SuspendThread(pOpenThread);
            }
        }

        /// <summary>
        /// Resumes a process
        /// </summary>
        /// <param name="PID">Process ID</param>
        public static void ResumeProcess(int PID)
        {
            Process proc = Process.GetProcessById(PID);

            if (proc.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in proc.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }

                ResumeThread(pOpenThread);
            }
        }

        /// <summary>
        /// Change the scheduling priority for the I/O operations for the current process
        /// </summary>
        /// <param name="priority">I/O Priority</param>
        public static void SetPriority(ProcessPriority priority)
        {
            try { SetPriorityClass(GetCurrentProcess(), priority); }
            catch { }
        }

        /// <summary>
        /// Change the scheduling priority for the I/O operations for a specific process
        /// </summary>
        /// <param name="process">Handle to the process</param>
        /// <param name="priority">I/O Priority</param>
        public static void SetPriority(IntPtr process, ProcessPriority priority)
        {
            if (priority == ProcessPriority.PROCESS_MODE_BACKGROUND_BEGIN || priority == ProcessPriority.PROCESS_MODE_BACKGROUND_END)
                throw new ArgumentException("Process mode background can only set for current process");

            try { SetPriorityClass(process, priority); }
            catch { }
        }

        /// <summary>
        /// Change the scheduling priority for the I/O operations for the current thread
        /// </summary>
        /// <param name="priority">I/O Priority</param>
        public static void SetPriority(ThreadsPriority priority)
        {
            try { SetThreadPriority(GetCurrentThread(), priority); }
            catch { }
        }

        /// <summary>
        /// Change the scheduling priority for the I/O operations for a specific thread
        /// </summary>
        /// <param name="thread">Handle to the thread</param>
        /// <param name="priority">I/O Priority</param>
        public static void SetPriority(IntPtr thread, ThreadsPriority priority)
        {
            if (priority == ThreadsPriority.THREAD_MODE_BACKGROUND_BEGIN || priority == ThreadsPriority.THREAD_MODE_BACKGROUND_END)
                throw new ArgumentException("Process mode background can only set for current process");

            try { SetThreadPriority(thread, priority); }
            catch { }
        }
    }
}
