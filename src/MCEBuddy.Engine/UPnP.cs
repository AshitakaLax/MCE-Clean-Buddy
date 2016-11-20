using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Net;
using System.Numerics;

using ManagedUPnP;
using ManagedUPnP.Descriptions;
using MCEBuddy.Util;

namespace MCEBuddy.Engine
{
    public static class UPnP
    {
        /// <summary>
        /// Enabled NAT Forwarding on all UPnP devices on the network (and opens Firewall) for the specified port on the local machine
        /// </summary>
        /// <param name="onPort">Port number to map</param>
        /// <param name="verbose">Write detailed logs</param>
        public static void EnableUPnP(int onPort, bool verbose)
        {
            try
            {
                bool searchCompleted;

                if (verbose)
                    Log.AppLog.WriteEntry("UPnP", "Enabling Firewall for UPnP", Log.LogEntryType.Debug, true);

                // Enable windows firewall for allowing UPnP (if it's not already enabled)
                if (WindowsFirewall.UPnPPortsOpen != WindowsFirewall.Status.Open)
                {
                    WindowsFirewall.UPnPPortsOpen = WindowsFirewall.Status.Open;
                    if (WindowsFirewall.UPnPPortsOpen != WindowsFirewall.Status.Open)
                    {
                        Log.AppLog.WriteEntry("UPnP", "Unable to open Windows Firewall UPnP ports, please MANUALLY ALLOW/ENABLE the UPnP ports (TCP:2869 and UDP:1900)", Log.LogEntryType.Warning, true);
                        Log.AppLog.WriteEntry("UPnP", "Windows Firewall Status -> " + WindowsFirewall.UPnPPortsOpen.ToString(), Log.LogEntryType.Warning, true);
                    }
                }

                if (verbose)
                    Log.AppLog.WriteEntry("UPnP", "Searching for UPnP devices", Log.LogEntryType.Debug, true);

                // Search for UPnP devices
                Services lsServices = Discovery.FindServices(null, int.MaxValue, int.MaxValue, out searchCompleted, AddressFamilyFlags.IPvBoth, true);

                // Check for an incomplete search
                if (!searchCompleted)
                {
                    if (verbose)
                        Log.AppLog.WriteEntry("UPnP", "UPnP search incomplete, retrying again", Log.LogEntryType.Information, true);
                    lsServices = Discovery.FindServices(null, int.MaxValue, int.MaxValue, out searchCompleted, AddressFamilyFlags.IPvBoth, false);

                    if (!searchCompleted)
                        Log.AppLog.WriteEntry("UPnP", "UPnP search incomplete, UPnP enablement may not succeed", Log.LogEntryType.Warning, true);
                }

                foreach (ManagedUPnP.Service lsService in lsServices)
                {
                    ServiceDescription lsdDesc = lsService.Description();
                    if (lsdDesc.Actions.ContainsKey("AddPortMapping")) // Check to see if is a WAN UPnP device that supports Port Mappings, if we so need to enable Port Forwarding for each such device
                    {
                        object[] inParams;

                        try
                        {
                            inParams = new object[] { "", onPort, "tcp" };
                            lsService.InvokeAction("DeletePortMapping", inParams); // Delete the port mapping (we will create a fresh one later)
                        }
                        catch (Exception)
                        {
                        }

                        // For the UPnP device, add a port mapping for each network interface adapter/IPaddress that can connect to the device
                        foreach (IPAddress ifAddress in lsService.Device.AdapterIPAddresses)
                        {
                            // IPv4 addressed UPnP devices cannot handle IPv6 address entries, so skip it (even if there is one IPv6 Host address, we can then process an IPv6 entry)
                            if (lsService.Device.RootHostAddresses.All(hostAddress => ((hostAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) && (ifAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6))))
                            {
                                if (verbose)
                                    Log.AppLog.WriteEntry("UPnP", "Skipping Port Mapping due to incompatible IPAddress Types. UPnP Device : " + lsService.Device.RootHostAddresses[0].ToString() + ", Adapter : " + ifAddress.ToString(), Log.LogEntryType.Debug, true);

                                continue;
                            }

                            try
                            {
                                // For some IPv6 addresses we need to remove the Interface Identifier from the string (%x at the end of the ip6 address) if present
                                if (verbose)
                                    Log.AppLog.WriteEntry("UPnP", "Adding Port Mapping to " + (ifAddress.ToString().Contains("%") ? ifAddress.ToString().Substring(0, ifAddress.ToString().IndexOf("%")) : ifAddress.ToString()) + " Port " + onPort.ToString(), Log.LogEntryType.Debug, true);

                                // Now we add the port mapping for the current MCEBuddy server
                                inParams = new object[] { "", onPort, "tcp", onPort, (ifAddress.ToString().Contains("%") ? ifAddress.ToString().Substring(0, ifAddress.ToString().IndexOf("%")) : ifAddress.ToString()), true, "mcebuddy2x", 0 };
                                lsService.InvokeAction("AddPortMapping", inParams);
                            }
                            catch (Exception e)
                            {
                                if (verbose)
                                {
                                    Log.AppLog.WriteEntry("UPnP", "Unable to Add Port Mapping to " + lsService.Device.RootHostName, Log.LogEntryType.Warning, true);
                                    Log.AppLog.WriteEntry("UPnP", "Add Error " + e.ToString(), Log.LogEntryType.Warning, true);
                                }
                            }
                        }
                    }

                    lsService.Dispose(); // need to dispose else it causes a memory leakage
                }
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry("UPnP", "Error trying to enable UPnP -> " + e.ToString(), Log.LogEntryType.Warning, true);
            }
        }

        /// <summary>
        /// Disable NAT Forwarding on all UPnP devices on the network for the specified port on the local machine
        /// </summary>
        /// <param name="onPort">Port number to map</param>
        /// <param name="verbose">Write detailed logs</param>
        public static void DisableUPnP(int onPort, bool verbose)
        {
            try
            {
                bool searchCompleted;

                if (verbose)
                    Log.AppLog.WriteEntry("UPnP", "Searching for UPnP devices", Log.LogEntryType.Debug, true);

                // Search for UPnP devices
                Services lsServices = Discovery.FindServices(null, int.MaxValue, int.MaxValue, out searchCompleted, AddressFamilyFlags.IPvBoth, false);

                // Check for an incomplete search
                if (!searchCompleted)
                {
                    if (verbose)
                        Log.AppLog.WriteEntry("UPnP", "UPnP search incomplete, retrying again", Log.LogEntryType.Information, true);
                    lsServices = Discovery.FindServices(null, int.MaxValue, int.MaxValue, out searchCompleted, AddressFamilyFlags.IPvBoth, false);

                    if (!searchCompleted)
                        Log.AppLog.WriteEntry("UPnP", "UPnP search incomplete, UPnP enablement may not succeed", Log.LogEntryType.Warning, true);
                }

                foreach (ManagedUPnP.Service lsService in lsServices)
                {
                    ServiceDescription lsdDesc = lsService.Description();
                    if (lsdDesc.Actions.ContainsKey("DeletePortMapping")) // Check to see if is a WAN UPnP device that supports Port Mappings, if we so need to disable Port Forwarding for each such device
                    {
                        object[] inParams;

                        try
                        {
                            if (verbose)
                                Log.AppLog.WriteEntry("UPnP", "Deleting Port Mapping from " + lsService.Device.RootHostName + " Port " + onPort.ToString(), Log.LogEntryType.Debug, true);

                            inParams = new object[] { "", onPort, "tcp" };
                            lsService.InvokeAction("DeletePortMapping", inParams); // Delete the port mapping (we will create a fresh one later)
                        }
                        catch (Exception e)
                        {
                            if (verbose)
                            {
                                Log.AppLog.WriteEntry("UPnP", "Unable to Delete Port Mapping from " + lsService.Device.RootHostName, Log.LogEntryType.Warning, true);
                                Log.AppLog.WriteEntry("UPnP", "Delete Error " + e.ToString(), Log.LogEntryType.Warning, true);
                            }
                        }
                    }

                    lsService.Dispose(); // need to dispose else it causes a memory leakage
                }
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry("UPnP", "Error trying to disable UPnP -> " + e.ToString(), Log.LogEntryType.Warning, true);
            }
        }
    }
}
