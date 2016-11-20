using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using System.ServiceModel.Description;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Globalization;

using MCEBuddy.Engine;
using MCEBuddy.Globals;
using MCEBuddy.GUI;
using MCEBuddy.Configuration;
using MCEBuddy.Util;

namespace MCEBuddy.ServiceCMD
{
    class Program
    {
        #region Declarations
        internal enum ControlSignalType : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6,
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool HandlerRoutine([In][MarshalAs(UnmanagedType.U4)]ControlSignalType dwCtrlType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetConsoleCtrlHandler([In][Optional][MarshalAs(UnmanagedType.FunctionPtr)] HandlerRoutine HandlerRoutine, [In][MarshalAs(UnmanagedType.Bool)] bool Add);
        #endregion

        static ICore _pipeProxy = null;
        static ServiceHost _host = null;
        static bool _forceClosed = false; // If the user forced a close
        static HandlerRoutine _controlHandler = null; // Keeps it from getting garbage collected (and null exception)

        private static void OnPowerChange(Object sender, PowerModeChangedEventArgs pe)
        {
            Log.WriteSystemEventLog("MCEBuddy engine app OnPowerChange called by System, Event -> " + pe.Mode.ToString(), EventLogEntryType.Information);

            switch (pe.Mode)
            {
                case PowerModes.Resume:
                    try
                    {
                        if (_pipeProxy.WithinConversionTimes()) // Resume only if we are within Conversion times else it will auto resume at the right time
                            _pipeProxy.SuspendConversion(false); // Resume
                    }
                    catch (Exception e)
                    {
                        Log.WriteSystemEventLog("MCEBuddy engine app: Error trying to resume MCEBuddy " + e.ToString(), EventLogEntryType.Warning);
                    }
                    break;

                case PowerModes.Suspend:
                    try
                    {
                        _pipeProxy.SuspendConversion(true); // suspend and buffers are flushed when suspended
                    }
                    catch (Exception e)
                    {
                        Log.WriteSystemEventLog("MCEBuddy engine app: Error trying to suspend MCEBuddy " + e.ToString(), EventLogEntryType.Warning);
                    }

                    break;

                case PowerModes.StatusChange:
                    try
                    {
                        Log.WriteSystemEventLog("MCEBuddy engine app: Power Status Change Notification, Power Status -> " + System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus.ToString(), EventLogEntryType.Information);

                        if (System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline) // If we are on battery power
                        {
                            if (MCEBuddyConf.GlobalMCEConfig.GeneralOptions.suspendOnBattery) // If we are requested to Pause on battery power
                                _pipeProxy.SuspendConversion(true); // Suspend it
                        }
                        else if (_pipeProxy.WithinConversionTimes()) // Resume only if we are within Conversion times else it will auto resume at the right time
                            _pipeProxy.SuspendConversion(false); // Resume it (assume A/C power)
                    }
                    catch (Exception e)
                    {
                        Log.WriteSystemEventLog("MCEBuddy engine app: Error trying to change suspend mode on Power Event in MCEBuddy " + e.ToString(), EventLogEntryType.Warning);
                    }

                    break;
            }
        }

        private static bool ConsoleCloseCheck(ControlSignalType ctrlType)
        {
            _forceClosed = true; // Set the flag incase the user pressed Ctrl+C or break etc, exit the main thread

            if (_pipeProxy == null)
                return true; // We are already shutdown

            Log.WriteSystemEventLog("MCEBuddy engine app shutting down", EventLogEntryType.Information);
            Console.WriteLine("\r\n\n\n\nStopping...");
            
            try {
                if (_pipeProxy != null) _pipeProxy.StopBySystem();
            } catch { }
            try {
                if (_host != null) _host.Abort(); // incase we are in faulted state with some resource being held otherwise close takes a long time
            } catch { }
            
            _pipeProxy = null; // We are done here, free to start another engine
            _host = null;
            Console.WriteLine("\r\nStopped");

            // Restart the the Windows we stopped earlier
            Log.WriteSystemEventLog("Trying to start MCEBuddy engine service.", EventLogEntryType.Information);
            WindowsService.StartService(GlobalDefs.MCEBUDDY_SERVICE_NAME);
            Log.WriteSystemEventLog("MCEBuddy engine app shutdown complete.", EventLogEntryType.Information);

            return true;
        }

        static int Main(string[] args)
        {
            if (_pipeProxy != null)
            {
                Log.WriteSystemEventLog("MCEBuddy engine app already running, cannot start another instance.", EventLogEntryType.Error);
                return -1; // We can't start engine's
            }

            if (!AppProcess.HaveAdministrativeRights())
            {
                Log.WriteSystemEventLog("Start MCEBuddy engine app with Administrative rights.", EventLogEntryType.Information);
                AppProcess.StartAppWithAdministrativeRights(Application.ExecutablePath); // Engine needs Admin rights for firewall and binding
                return -2; // Exit, the process will restart with admin rights
            }

            // Capture the Window close event if the user manually closes it
            _controlHandler = new HandlerRoutine(ConsoleCloseCheck); // Setup a static object otherwise on some systems it crashes with null pointer exception due to garbage collection
            SetConsoleCtrlHandler(_controlHandler, true);

            // Now that we are running, first Stop the the Windows Service otherwise we don't be able to start (stop before starting the GUI to prevent closure)
            Log.WriteSystemEventLog("Trying to stop MCEBuddy engine service.", EventLogEntryType.Information);
            WindowsService.StopService(GlobalDefs.MCEBUDDY_SERVICE_NAME, 10000);
            
            //Start MCEBuddy GUI with Normal user rights (just incase this is running admin rights
            try
            {
                if (AppProcess.HaveAdministrativeRights())
                {
                    AppProcess.StartAppWithMediumPrivilegeFromUISession(Path.Combine(GlobalDefs.AppPath, "MCEBuddy.GUI.exe"), "", false);
                }
                else // Normal user
                {
                    Process Proc = new Process();
                    Proc.StartInfo.FileName = Path.Combine(GlobalDefs.AppPath, "MCEBuddy.GUI.exe");
                    Proc.StartInfo.CreateNoWindow = false;
                    Proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    Proc.Start();
                }
            }
            catch (Exception e)
            {
                // Sometimes the DLL does not work or is missing or running on wrong windows version, catch it and log it
                Log.WriteSystemEventLog("MCEBuddy engine app: Unable to start MCEBuddy GUI. Error ->\r\n" + e.ToString(), EventLogEntryType.Warning);
            }

            Log.WriteSystemEventLog("Starting MCEBuddy engine app.", EventLogEntryType.Information);
            MCEBuddyConf.GlobalMCEConfig = new MCEBuddyConf(GlobalDefs.ConfigFile); // Read the settings for global objects

            MCEBuddyConf.CheckDefaultJobPaths(); // Update paths before we start the engine to it is read

            _host = new ServiceHost(typeof(Core));
            TimeSpan timeoutPeriod = GlobalDefs.PIPE_TIMEOUT;

            // Create a binding for network SOAP WEB SERVICES
            string serverString = GlobalDefs.MCEBUDDY_WEB_SOAP_PIPE;
            serverString = serverString.Replace(GlobalDefs.MCEBUDDY_SERVER_PORT, MCEBuddyConf.GlobalMCEConfig.GeneralOptions.localServerPort.ToString()); // Update the Server Port with that from the config file
            BasicHttpBinding ntb = new BasicHttpBinding(GlobalDefs.MCEBUDDY_PIPE_SECURITY);
            ntb.OpenTimeout = ntb.CloseTimeout = ntb.SendTimeout = ntb.ReceiveTimeout = timeoutPeriod;
            ntb.TransferMode = TransferMode.Buffered;
            ntb.MaxReceivedMessageSize = ntb.MaxBufferPoolSize = ntb.MaxBufferSize = Int32.MaxValue;
            ntb.ReaderQuotas = XmlDictionaryReaderQuotas.Max;
            ServiceEndpoint soapEndpoint = _host.AddServiceEndpoint(typeof(ICore), ntb, serverString);
            // Increase the max objects allowed in serialization channel otherwise we lose the connection when there more than 5K objects in the queue
            foreach (OperationDescription operation in soapEndpoint.Contract.Operations)
            {
                DataContractSerializerOperationBehavior dataContractBehavior = operation.Behaviors[typeof(DataContractSerializerOperationBehavior)] as DataContractSerializerOperationBehavior;

                if (dataContractBehavior != null)
                    dataContractBehavior.MaxItemsInObjectGraph = Int32.MaxValue;
            }

            // Create a binding for local NAMED PIPES
            NetNamedPipeBinding npb = new NetNamedPipeBinding();
            npb.OpenTimeout = npb.CloseTimeout = npb.SendTimeout = npb.ReceiveTimeout = timeoutPeriod;
            npb.TransferMode = TransferMode.Buffered;
            npb.MaxReceivedMessageSize = npb.MaxBufferPoolSize = npb.MaxBufferSize = Int32.MaxValue;
            npb.ReaderQuotas = XmlDictionaryReaderQuotas.Max;
            ServiceEndpoint namedPipeEndpoint = _host.AddServiceEndpoint(typeof(ICore), npb, GlobalDefs.MCEBUDDY_LOCAL_NAMED_PIPE);
            // Increase the max objects allowed in serialization channel otherwise we lose the connection when there more than 5K objects in the queue
            foreach (OperationDescription operation in namedPipeEndpoint.Contract.Operations)
            {
                DataContractSerializerOperationBehavior dataContractBehavior = operation.Behaviors[typeof(DataContractSerializerOperationBehavior)] as DataContractSerializerOperationBehavior;

                if (dataContractBehavior != null)
                    dataContractBehavior.MaxItemsInObjectGraph = Int32.MaxValue;
            }

            _host.CloseTimeout = GlobalDefs.PIPE_TIMEOUT;
            _host.Open();

            _pipeProxy = MCEBuddyEngineConnect.ConnectToLocalEngine();
            if (MCEBuddyConf.GlobalMCEConfig.GeneralOptions.engineRunning)
                _pipeProxy.Start();

            // Now that we are up and running
            SystemEvents.PowerModeChanged += OnPowerChange; // Register for power event changes

            ConsoleKeyInfo cki;
            Console.Title = "MCEBuddy 2.x Service Started, Press ESC to stop";
            Console.WindowHeight = 15;
            Console.WindowWidth = 80;
            int lastNumJobs = 0;
            bool Shutdown = false;
            while (!Shutdown)
            {
                if (_forceClosed)
                    return 1; // Someone forced the closure, the handler will clean up. Exit now

                if (Console.KeyAvailable)
                {
                    cki = Console.ReadKey(true);
                    if (cki.Key == ConsoleKey.Escape)
                    {
                        Log.WriteSystemEventLog("MCEBuddy engine app received user shutdown request.", EventLogEntryType.Information);
                        Shutdown = true;
                    }
                }

                try
                {
                    for (int i = 0, numJobs = _pipeProxy.NumConversionJobs(); i < numJobs; i++)
                    {
                        if (lastNumJobs != numJobs) // The number of conversion jobs has changed
                            Console.Clear(); // Clear the screen

                        string outputStr = "";
                        outputStr = "Job" + i.ToString();
                        JobStatus status = _pipeProxy.GetJobStatus(i);
                        if (status != null)
                        {
                            outputStr += " " + status.CurrentAction;
                            if (status.PercentageComplete > 0)
                                outputStr += " " + status.PercentageComplete.ToString("#0.0", CultureInfo.InvariantCulture) + "%";
                            else if (-1 == status.PercentageComplete)
                                outputStr += " Working..."; //some processes like ReMuxSupp don't update perc

                            if (!String.IsNullOrEmpty(status.ETA))
                                outputStr += " ETA=" + status.ETA;

                            if (!String.IsNullOrEmpty(status.SourceFile))
                                outputStr += " " + Path.GetFileName(status.SourceFile);
                        }
                        else
                        {
                            outputStr += " Idle";
                        }
                        if (outputStr.Length > 78) outputStr = outputStr.Substring(0, 78);
                        Console.SetCursorPosition(0, i);
                        Console.Write(outputStr.PadRight(78));
                        lastNumJobs = _pipeProxy.NumConversionJobs();
                    }
                }
                catch (Exception) //while debugging catch Proxy Faulted state and reset it
                {
                    if (_forceClosed)
                        return 1;

                    if (Shutdown)
                        break; // we are done here

                    _pipeProxy = MCEBuddyEngineConnect.ConnectToLocalEngine();
                }

                System.Threading.Thread.Sleep(GlobalDefs.LOCAL_ENGINE_POLL_PERIOD);
            }

            ConsoleCloseCheck(ControlSignalType.CTRL_CLOSE_EVENT); // Gracefully shutdown the engine and restart the windows service

            return 0; // All good
        }
    }
}
