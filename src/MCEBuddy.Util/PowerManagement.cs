using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Threading;

using MCEBuddy.Globals;

namespace MCEBuddy.Util
{
    public class PowerManagement
    {
        [FlagsAttribute]
        private enum EXECUTION_STATE : uint
        {
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            // Legacy flag, should not be used.
            // ES_USER_PRESENT   = 0x00000004,
            ES_CONTINUOUS = 0x80000000,
            ES_AWAYMODE_REQUIRED = 0x00000040
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        public static void PreventSleep()
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_AWAYMODE_REQUIRED);
        }

        public static void AllowSleep()
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }

        public class WakeUp
        {
            [DllImport("kernel32.dll")]
            public static extern SafeWaitHandle CreateWaitableTimer(IntPtr lpTimerAttributes,
                                                                      bool bManualReset,
                                                                    string lpTimerName);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetWaitableTimer(SafeWaitHandle hTimer,
                                                        [In] ref long pDueTime,
                                                                  int lPeriod,
                                                               IntPtr pfnCompletionRoutine,
                                                               IntPtr lpArgToCompletionRoutine,
                                                                 bool fResume);

            public event EventHandler Woken;
            private EventWaitHandle[] _wakeEventHandles;

            private BackgroundWorker bgWorker = new BackgroundWorker();

            public WakeUp()
            {
                bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
                bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                _wakeEventHandles = new EventWaitHandle[2];
            }

            public void SetWakeUpTime(DateTime time)
            {
                try
                {
                    bgWorker.RunWorkerAsync(time.ToFileTime());
                }
                catch (Exception)
                {
                    Log.AppLog.WriteEntry(this, Localise.GetPhrase("Background timer already running, cannot start another event"), Log.LogEntryType.Warning, true);
                }
            }

            void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                if (Woken != null)
                {
                    Woken(this, new EventArgs());
                }
            }

            public void Abort()
            {
                if (_wakeEventHandles[1] != null)
                {
                    _wakeEventHandles[1].Set();
                }
            }

            private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
            {
                long waketime = (long)e.Argument;

                using (SafeWaitHandle handle = CreateWaitableTimer(IntPtr.Zero, true, this.GetType().Assembly.GetName().Name.ToString() + ".Timer"))
                {
                    if (SetWaitableTimer(handle, ref waketime, 0, IntPtr.Zero, IntPtr.Zero, true)) //set the timer to fire at the desired time and wake up the system if suspended (not call backfunction as this is only to wake up the system), the jobs are handlded by the monitor thread
                    {
                        using (_wakeEventHandles[0] = new EventWaitHandle(false, EventResetMode.ManualReset))
                        {
                            _wakeEventHandles[0].SafeWaitHandle = handle; // OS timer set to wake up the system
                            _wakeEventHandles[1] = new EventWaitHandle(false, EventResetMode.ManualReset); // Manually triggered abort function to signal shutdown of application
                            int index = WaitHandle.WaitAny(_wakeEventHandles); // Wait until either of the above 2 events are triggered to complete this function
                        }
                    }
                    else
                    {
                        Log.AppLog.WriteEntry(this, Localise.GetPhrase("Error setting up wake up timer"), Log.LogEntryType.Error, true);
                    }
                }
            }

        }


    }
}
