using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Globalization;

using MCEBuddy.Globals;
using MCEBuddy.Util;
using MCEBuddy.CommandLine;

namespace MCEBuddy.UserCLI
{
    // Define a class to receive parsed values
    class CLIOptions
    {
        [OptionAttribute(null, "command", Required = true, HelpText = "Command to be processed")]
        public string Command = "";

        [OptionAttribute(null, "action", Required = true, HelpText = "Action to be taken")]
        public string Action = "";

        [OptionAttribute(null, "server", Required = false, HelpText = "MCEBuddy Engine hostname or address")]
        public string Server = GlobalDefs.MCEBUDDY_SERVER_NAME;

        [OptionAttribute(null, "port", Required = false, HelpText = "MCEBuddy Engine port")]
        public int Port = int.Parse(GlobalDefs.MCEBUDDY_SERVER_PORT);
    }

    class Program
    {
        static int Main(string[] args)
        {
            Log.AppLog = new Log(Log.LogDestination.Console); // Redirect to console all output
            Log.LogLevel = Log.LogEntryType.Debug; // Print all messages
            ICore _pipeProxy = null;
            CLIOptions cliOptions = new CLIOptions();
            string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Log.AppLog.WriteEntry("", "\r\nMCEBuddy.UserCLI is a Command Line Interface for users to interact with the MCEBuddy engine", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "Copyright (c) Ramit Bhalla, Build Version : " + currentVersion, Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "Build Date : " + System.IO.File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "", Log.LogEntryType.Debug);

            if (args.Length < 2) // Atleast 2 arguments are required
            {
                Usage();
                return -1;
            }

            try
            {
                CommandLineParser parser = new CommandLineParser();
                if (!parser.ParseArguments(args, cliOptions))
                    throw new Exception("Invalid Options");
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI Invalid Input Error -> " + e.ToString() + "\r\n", Log.LogEntryType.Error);
                Usage();
                return -1; // Bad usage
            }

            Log.AppLog.WriteEntry("", "\r\nMCEBuddy.UserCLI trying to connect to Engine " + cliOptions.Server + " on Port " + cliOptions.Port + "\r\n", Log.LogEntryType.Debug);

            // Connect to the engine
            try // Try to reconnect
            {
                string remoteServerName = cliOptions.Server;
                int remoteServerPort = cliOptions.Port;

                _pipeProxy = MCEBuddyEngineConnect.ConnectToRemoteEngine(remoteServerName, remoteServerPort);
                _pipeProxy.EngineRunning(); // Test to check if we are really connected

                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI successfully connected to MCEBuddy engine", Log.LogEntryType.Debug);

                // Pass the command to the engine
                switch (cliOptions.Command)
                {
                    // Engine commands
                    case "engine":
                        Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI processing command engine", Log.LogEntryType.Debug);
                        
                        // Engine actions
                        switch (cliOptions.Action)
                        {
                            case "start":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI starting engine", Log.LogEntryType.Debug);
                                _pipeProxy.Start();
                                break;

                            case "stop":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI stopping engine", Log.LogEntryType.Debug);
                                _pipeProxy.Stop(true);
                                break;

                            case "pause":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI pausing engine", Log.LogEntryType.Debug);
                                _pipeProxy.SuspendConversion(true);
                                break;

                            case "resume":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI resuming engine", Log.LogEntryType.Debug);
                                _pipeProxy.SuspendConversion(false);
                                break;

                            case "rescan":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI rescanning monitor locations and logs", Log.LogEntryType.Debug);
                                _pipeProxy.Rescan();
                                break;

                            default:
                                Log.AppLog.WriteEntry("", "Invalid action " + cliOptions.Action, Log.LogEntryType.Error);
                                Usage();
                                return -1;
                        }
                        break;

                    // Adding a file to the queue
                    case "addfile":
                        Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI processing command addfile", Log.LogEntryType.Debug);
                        if (String.IsNullOrWhiteSpace(cliOptions.Action))
                        {
                            Log.AppLog.WriteEntry("", "Invalid filename " + cliOptions.Action, Log.LogEntryType.Error);
                            Usage();
                            return -1;
                        }

                        Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI adding file " + cliOptions.Action + " to the conversion queue", Log.LogEntryType.Debug);
                        if (Util.Net.IsUNCPath(Util.Net.GetUNCPath(cliOptions.Action))) // check if the files are on a remote computer
                            Log.AppLog.WriteEntry("", "Networked files will be accessed using the logon credentials of the MCEBuddy Service, not the currently logged on user. You can manually specify the network credentials from the Settings -> Expert Settings page in MCEBuddy.", Log.LogEntryType.Warning);
                        _pipeProxy.AddManualJob(cliOptions.Action);
                        break;

                    // Removing a job from the queue
                    case "removejob":
                        Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI processing command removejob", Log.LogEntryType.Debug);
                        int fileNo;
                        if (String.IsNullOrWhiteSpace(cliOptions.Action) || (!int.TryParse(cliOptions.Action, out fileNo)) || (fileNo < 1)) // job starts at 1 for users
                        {
                            Log.AppLog.WriteEntry("", "Invalid job number " + cliOptions.Action, Log.LogEntryType.Error);
                            Usage();
                            return -1;
                        }

                        Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI removing job " + fileNo + " from the conversion queue", Log.LogEntryType.Debug);
                        _pipeProxy.CancelJob(new int[] {--fileNo}); // MCEBuddy start jobs at 0
                        break;

                    // Change priority
                    case "priority":
                        Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI processing command change priority", Log.LogEntryType.Debug);
                        switch (cliOptions.Action)
                        {
                            case "low":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI setting priority to low", Log.LogEntryType.Debug);
                                _pipeProxy.ChangePriority("Low");
                                break;

                            case "normal":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI setting priority to normal", Log.LogEntryType.Debug);
                                _pipeProxy.ChangePriority("Normal");
                                break;

                            case "high":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI setting priority to high", Log.LogEntryType.Debug);
                                _pipeProxy.ChangePriority("High");
                                break;

                            default:
                                Log.AppLog.WriteEntry("", "Invalid priority " + cliOptions.Action, Log.LogEntryType.Error);
                                Usage();
                                return -1;
                        }
                        break;

                    // Remove file from History
                    case "deletehistoryitem":
                        Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI processing command deleting file entry from history", Log.LogEntryType.Debug);
                        if (String.IsNullOrWhiteSpace(cliOptions.Action))
                        {
                            Log.AppLog.WriteEntry("", "Invalid filename " + cliOptions.Action, Log.LogEntryType.Error);
                            Usage();
                            return -1;
                        }

                        Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI deleting item " + cliOptions.Action + " from History", Log.LogEntryType.Debug);
                        _pipeProxy.DeleteHistoryItem(cliOptions.Action);
                        break;

                    // UPnP
                    case "upnp":
                        Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI processing command UPnP", Log.LogEntryType.Debug);
                        switch (cliOptions.Action)
                        {
                            case "enable":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI enabling UPnP access port fowarding", Log.LogEntryType.Debug);
                                _pipeProxy.SetUPnPState(true);
                                break;

                            case "disable":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI disabling UPnP access port fowarding", Log.LogEntryType.Debug);
                                _pipeProxy.SetUPnPState(false);
                                break;

                            default:
                                Log.AppLog.WriteEntry("", "Invalid UPnP state " + cliOptions.Action, Log.LogEntryType.Error);
                                Usage();
                                return -1;
                        }
                        break;

                    // Firewall
                    case "firewall":
                        Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI processing command Firewall", Log.LogEntryType.Debug);
                        switch (cliOptions.Action)
                        {
                            case "enable":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI enabling Firewall exception for MCEBuddy remote access", Log.LogEntryType.Debug);
                                _pipeProxy.SetFirewallException(true);
                                break;

                            case "disable":
                                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI disabling Firewall exception for MCEBuddy remote access", Log.LogEntryType.Debug);
                                _pipeProxy.SetFirewallException(false);
                                break;

                            default:
                                Log.AppLog.WriteEntry("", "Invalid Firewall exception state " + cliOptions.Action, Log.LogEntryType.Error);
                                Usage();
                                return -1;
                        }
                        break;

                    default:
                        Log.AppLog.WriteEntry("", "Invalid command " + cliOptions.Command, Log.LogEntryType.Error);
                        Usage();
                        return -1;
                }

                // Successful
                Log.AppLog.WriteEntry("", "\r\nMCEBuddy.UserCLI Successful!!", Log.LogEntryType.Debug);
                return 0; // we good here
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry("", "MCEBuddy.UserCLI Error -> " + e.ToString() + "\r\n", Log.LogEntryType.Error);
                Log.AppLog.WriteEntry("", "\r\nMCEBuddy.UserCLI Failed!!", Log.LogEntryType.Debug);
                return -2; // too bad
            }
        }

        static void Usage()
        {
            Log.AppLog.WriteEntry("", "", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "Usage: MCEBuddy.UserCLI --command=<option> --action=<value> --server=<server> --port=<port>", Log.LogEntryType.Debug);

            Log.AppLog.WriteEntry("", "\t--command=engine -> Change the engine state", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=start -> Start engine", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=stop -> Stop engine", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=pause -> Pause engine", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=resume -> Resume engine", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=rescan -> Rescan monitor locations and logs", Log.LogEntryType.Debug);

            Log.AppLog.WriteEntry("", "\r\n\t--command=addfile -> Add file to the conversion queue", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=\"<full file path>\" -> Full path to file to add to conversion queue", Log.LogEntryType.Debug);

            Log.AppLog.WriteEntry("", "\r\n\t--command=removejob -> Removes a job from the conversion queue", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=<queue no> -> Enter the job number in the queue to remove the job from the conversion queue", Log.LogEntryType.Debug);

            Log.AppLog.WriteEntry("", "\r\n\t--command=priority -> Change MCEBuddy priority", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=low -> Low priority", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=normal -> Normal priority", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=high -> High priority", Log.LogEntryType.Debug);

            Log.AppLog.WriteEntry("", "\r\n\t--command=deletehistoryitem -> Remove a file entry from the history to enable reconversion", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=\"<full file path>\" -> Full path to processed file to remove from history", Log.LogEntryType.Debug);

            Log.AppLog.WriteEntry("", "\r\n\t--command=upnp -> Set UPnP in gateway/router for MCEBuddy engine remote access", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=enable -> Enable UPnP port forwarding for MCEBuddy", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=disable -> Disable UPnP port forwarding for MCEBuddy", Log.LogEntryType.Debug);

            Log.AppLog.WriteEntry("", "\r\n\t--command=firewall -> Configure firewall exception for MCEBuddy engine remote access", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=enable -> Enable firewall exception for MCEBuddy", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t\t--action=disable -> Disable firewall exception for MCEBuddy", Log.LogEntryType.Debug);

            Log.AppLog.WriteEntry("", "\r\n\t--server=<localhost/NetBIOS name/IP Address> -> (Optional) Address of MCEBuddy engine (default is localhost)", Log.LogEntryType.Debug);

            Log.AppLog.WriteEntry("", "\r\n\t--port=<port no> -> (Optional) Port for MCEBuddy engine (default is 23332)", Log.LogEntryType.Debug);

            Log.AppLog.WriteEntry("", "\r\nThe program returns 0 if successful, -1 for bad input parameters and -2 if failed to process the command", Log.LogEntryType.Debug);

            Log.AppLog.WriteEntry("", "\r\nExamples:", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tMCEBuddy.UserCLI --command=addfile --action=\"C:\\Videos\\My Test.wtv\"", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tMCEBuddy.UserCLI --command=removejob --action=2", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tMCEBuddy.UserCLI --command=priority --action=low", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tMCEBuddy.UserCLI --command=deletehistoryitem --action=\"C:\\Videos\\My Test.wtv\"", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tMCEBuddy.UserCLI --command=engine --action=pause", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tMCEBuddy.UserCLI --command=engine --action=pause --server=192.168.1.3", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tMCEBuddy.UserCLI --command=engine --action=pause --server=localhost --port=2234", Log.LogEntryType.Debug);
        }
    }
}
