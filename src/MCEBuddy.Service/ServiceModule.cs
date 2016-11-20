using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Xml;
using System.ServiceModel.Description;

using MCEBuddy.Engine;
using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.Configuration;

namespace MCEBuddy.Service
{
    public partial class ServiceModule : ServiceBase
    {
        private ServiceHost _host;
        private ICore _pipeProxy;

        public ServiceModule()
        {
            InitializeComponent();

            this.AutoLog = true;
            this.ServiceName = GlobalDefs.MCEBUDDY_SERVICE_NAME;
            this.CanHandlePowerEvent = true;
            this.CanShutdown = true;
            this.CanStop = true;
            this.CanPauseAndContinue = true;

            GlobalDefs.IsEngineRunningAsService = true; // We starting as a service
        }

        protected override void OnStart(string[] args)
        {
            Log.WriteSystemEventLog("MCEBuddy service OnStart called", EventLogEntryType.Information);

            try
            {
                RequestAdditionalTime(10000); // Ask for 10 second to complete initial operations and not mark service as unresponsive

                MCEBuddyConf.GlobalMCEConfig = new MCEBuddyConf(GlobalDefs.ConfigFile); // Read the settings for global objects

                MCEBuddyConf.CheckDefaultJobPaths(); // Update paths before we start the engine to it is read

                _host = new ServiceHost(typeof(Core));
                TimeSpan timeoutPeriod = GlobalDefs.PIPE_TIMEOUT;
                
                // Create a binding for network SOAP WEB SERVICES
                string serverString = GlobalDefs.MCEBUDDY_WEB_SOAP_PIPE;
                serverString = serverString.Replace(GlobalDefs.MCEBUDDY_SERVER_PORT, MCEBuddyConf.GlobalMCEConfig.GeneralOptions.localServerPort.ToString(System.Globalization.CultureInfo.InvariantCulture)); // Update the Server Port with that from the config file
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

                Log.WriteSystemEventLog("MCEBuddy service started on port " + MCEBuddyConf.GlobalMCEConfig.GeneralOptions.localServerPort.ToString(System.Globalization.CultureInfo.InvariantCulture), EventLogEntryType.Information);

                _pipeProxy = MCEBuddyEngineConnect.ConnectToLocalEngine();
                if (MCEBuddyConf.GlobalMCEConfig.GeneralOptions.engineRunning) // Check for last saved state by user
                {
                    _pipeProxy.Start();
                    Log.WriteSystemEventLog(Localise.GetPhrase("Last user saved state: MCEBuddy engine started"), EventLogEntryType.Information);
                }
                else
                    Log.WriteSystemEventLog(Localise.GetPhrase("Last user saved state: MCEBuddy engine NOT started"), EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy service failed to start. Error:") + e.ToString(), EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            Log.WriteSystemEventLog("MCEBuddy service OnStop called", EventLogEntryType.Information);

            StopEngine(true); // We are calling from Stop
        }

        protected override void OnShutdown()
        {
            Log.WriteSystemEventLog("MCEBuddy OnShutdown called by System", EventLogEntryType.Information);

            StopEngine(false); // We are calling from shutdown
        }

        private void StopEngine(bool onStop)
        {
            RequestAdditionalTime(30000); // Ask for 30 seconds to complete operations and not mark service as unresponsive

            try
            {
                _pipeProxy.StopBySystem();
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy engine failed to stop. Error:") + e.ToString(), EventLogEntryType.Error);
            }

            try
            {
                _host.Abort(); // incase we are in faulted state with some resource being held otherwise close takes a long time
                Log.WriteSystemEventLog(Localise.GetPhrase("MCEBuddy service stopped"), EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog("MCEBuddy engine service failed to close Host binding. Error:" + e.ToString(), EventLogEntryType.Warning);
            }
        }

        protected override void OnPause()
        {
            Log.WriteSystemEventLog("MCEBuddy OnPause called by System", EventLogEntryType.Information);

            try
            {
                _pipeProxy.SuspendConversion(true); // suspend and buffers are flushed when suspended
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog("Error trying to suspend MCEBuddy " + e.ToString(), EventLogEntryType.Warning);
            }
        }

        protected override void OnContinue()
        {
            try
            {
                _pipeProxy.SuspendConversion(false); // Resume
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog("Error trying to resume MCEBuddy " + e.ToString(), EventLogEntryType.Warning);
            }
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            Log.WriteSystemEventLog("MCEBuddy OnPowerEvent called by System, Event -> " + powerStatus.ToString(), EventLogEntryType.Information);

            MCEBuddyConf.GlobalMCEConfig = new MCEBuddyConf(GlobalDefs.ConfigFile); // Update the settings for global objects

            switch (powerStatus)
            {
                case PowerBroadcastStatus.PowerStatusChange: // Something changed, lets find out if we are in battery mode or  a/c mode
                    try
                    {
                        Log.WriteSystemEventLog("Power Status Change Notification, Power Status -> " + System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus.ToString(), EventLogEntryType.Information);

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
                        Log.WriteSystemEventLog("Error trying to change suspend mode on Power Event in MCEBuddy " + e.ToString(), EventLogEntryType.Warning);
                    }

                    break;

                case PowerBroadcastStatus.QuerySuspend: // Can the system go into suspend mode?
                    try
                    {
                        return _pipeProxy.AllowSuspend(); // Check if the engine is ready to go into suspend mode
                    }
                    catch (Exception e)
                    {
                        Log.WriteSystemEventLog("Error trying to get suspend permission from MCEBuddy " + e.ToString(), EventLogEntryType.Warning);
                        return true;
                    }

                case PowerBroadcastStatus.BatteryLow: // On battery low automatically suspend the conversion
                case PowerBroadcastStatus.Suspend: // System is being suspended, pause all activity and flush the buffers
                    try
                    {
                        _pipeProxy.SuspendConversion(true); // suspend and buffers are flushed when suspended
                    }
                    catch (Exception e)
                    {
                        Log.WriteSystemEventLog("Error trying to suspend MCEBuddy " + e.ToString(), EventLogEntryType.Warning);
                    }

                    break;

                case PowerBroadcastStatus.ResumeAutomatic: // system resuming from suspend, resume MCEBuddy
                case PowerBroadcastStatus.ResumeCritical:
                case PowerBroadcastStatus.ResumeSuspend:
                    try
                    {
                        if (_pipeProxy.WithinConversionTimes()) // Resume only if we are within Conversion times else it will auto resume at the right time
                            _pipeProxy.SuspendConversion(false); // Resume
                    }
                    catch (Exception e)
                    {
                        Log.WriteSystemEventLog("Error trying to resume MCEBuddy " + e.ToString(), EventLogEntryType.Warning);
                    }
                    break;
            }

            return true;
        }
    }
}
