using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCEBuddy.Util
{
    /// <summary>
    /// Used to open the firewall to allow MCEBuddy to communicate
    /// </summary>
    static public class Firewall
    {
        public enum NET_FW_ACTION
        {
            NET_FW_ACTION_BLOCK = 0,
            NET_FW_ACTION_ALLOW = 1,
        }

        public enum NET_FW_IP_PROTOCOL
        {
            NET_FW_IP_PROTOCOL_TCP = 6,
            NET_FW_IP_PROTOCOL_UDP = 17,
            NET_FW_IP_PROTOCOL_ANY = 256,
        }

        public enum NET_FW_IP_VERSION
        {
            NET_FW_IP_VERSION_V4 = 0,
            NET_FW_IP_VERSION_V6 = 1,
            NET_FW_IP_VERSION_ANY = 2,
        }

        public enum NET_FW_SCOPE
        {
            NET_FW_SCOPE_ALL = 0,
            NET_FW_SCOPE_LOCAL_SUBNET = 1,
            NET_FW_SCOPE_CUSTOM = 2,
        }

        // ProgID for the AuthorizedApplication object
        private const string PROGID_AUTHORIZED_APPLICATION = "HNetCfg.FwAuthorizedApplication";
        private const string PROGID_FIREWALL_MANAGER = "HNetCfg.FwMgr";
        private const string PROGID_OPEN_PORT = "HNetCfg.FWOpenPort";
        private const string PROGID_FW_RULE = "HNetCfg.FWRule";
        private const string PROGID_FW_POLICY = "HNetCfg.FwPolicy2";

        /// <summary>
        /// Creates a COM Object by name.
        /// </summary>
        /// <param name="comName">The Application name of the COM Object to create.</param>
        /// <returns>The created COM object or null if not available.</returns>
        private static dynamic CreateCOMObject(string comName)
        {
            // Get the type
            System.Type ltCOMType = System.Type.GetTypeFromProgID(comName);

            try
            {
                // If type found
                if (ltCOMType != null)
                    // Create the instance
                    return System.Activator.CreateInstance(ltCOMType);
                else
                    // Otherwise return null
                    return null;
            }
            catch
            {
                // Any errors and we return null.
                return null;
            }
        }

        /// <summary>
        /// Authorize an application to work through the firewall
        /// </summary>
        /// <param name="title">Name of Firewall Rule</param>
        /// <param name="applicationPath">Path to executable</param>
        /// <param name="scope">Scope (All, Local, Custom)</param>
        /// <param name="ipVersion">IPv4, IpV6 or both</param>
        /// <returns>True if it succeeds</returns>
        public static bool AuthorizeApplication(string title, string applicationPath, NET_FW_SCOPE scope, NET_FW_ACTION action, NET_FW_IP_VERSION ipVersion)
        {
            try
            {
                if (OSVersion.GetOSVersion() <= OSVersion.OS.WIN_2003)
                {
                    dynamic fwMgr = CreateCOMObject(PROGID_FIREWALL_MANAGER);
                    dynamic profile = fwMgr.LocalPolicy.CurrentProfile;

                    dynamic authApp = CreateCOMObject(PROGID_AUTHORIZED_APPLICATION);
                    authApp.Name = title;
                    authApp.ProcessImageFileName = applicationPath;
                    authApp.Scope = scope;
                    authApp.IpVersion = ipVersion;
                    authApp.Enabled = true;

                    profile.AuthorizedApplications.Add(authApp);
                }
                else
                {
                    dynamic firewallRule = CreateCOMObject(PROGID_FW_RULE);
                    firewallRule.Action = action;
                    firewallRule.Name = title;
                    firewallRule.ApplicationName = applicationPath;
                    firewallRule.Enabled = true;
                    firewallRule.InterfaceTypes = "All";
                    firewallRule.EdgeTraversal = true;

                    dynamic firewallPolicy = CreateCOMObject(PROGID_FW_POLICY);
                    firewallPolicy.Rules.Add(firewallRule);
                }
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog("Error authorizing firewall application -> " + e.ToString(), System.Diagnostics.EventLogEntryType.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Opens a port on a the Windows Firewall
        /// </summary>
        /// <param name="title">Name of Firewall Rule</param>
        /// <param name="portNo">Port number</param>
        /// <param name="scope">All, Subnet, Custom</param>
        /// <param name="protocol">TCP, UDP</param>
        /// <param name="ipVersion">IPv4, IPv6, Both</param>
        /// <returns>True if successful</returns>
        public static bool AuthorizePort(string title, int portNo, NET_FW_SCOPE scope, NET_FW_IP_PROTOCOL protocol, NET_FW_IP_VERSION ipVersion)
        {
            try
            {
                if (OSVersion.GetOSVersion() <= OSVersion.OS.WIN_2003)
                {
                    dynamic fwMgr = CreateCOMObject(PROGID_FIREWALL_MANAGER);
                    dynamic profile = fwMgr.LocalPolicy.CurrentProfile;

                    dynamic port = CreateCOMObject(PROGID_OPEN_PORT);
                    port.Name = title;
                    port.Port = portNo;
                    port.Scope = scope;
                    port.Protocol = protocol;
                    port.IpVersion = ipVersion;

                    profile.GloballyOpenPorts.Add(port);
                }
                else
                {
                    dynamic firewallRule = CreateCOMObject(PROGID_FW_RULE);
                    firewallRule.Name = title;
                    firewallRule.Protocol = protocol;
                    firewallRule.LocalPorts = portNo.ToString();
                    firewallRule.Enabled = true;
                    firewallRule.InterfaceTypes = "All";
                    firewallRule.EdgeTraversal = true;

                    dynamic firewallPolicy = CreateCOMObject(PROGID_FW_POLICY);
                    firewallPolicy.Rules.Add(firewallRule);
                }
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog("Error enabling firewall port -> " + e.ToString(), System.Diagnostics.EventLogEntryType.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// DeAuthorize an application from the firewall
        /// </summary>
        /// <param name="title">Name of Firewall Rule</param>
        /// <param name="applicationPath">Path to the executable</param>
        /// <returns>True if it succeeds</returns>
        public static bool DeAuthorizeApplication(string title, string applicationPath)
        {
            try
            {
                if (OSVersion.GetOSVersion() <= OSVersion.OS.WIN_2003)
                {
                    dynamic fwMgr = CreateCOMObject(PROGID_FIREWALL_MANAGER);
                    dynamic profile = fwMgr.LocalPolicy.CurrentProfile;

                    profile.AuthorizedApplications.Remove(applicationPath);
                }
                else
                {
                    dynamic firewallPolicy = CreateCOMObject(PROGID_FW_POLICY);
                    firewallPolicy.Rules.Remove(title);
                }
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog("Error DeAuthorizing firewall application -> " + e.ToString(), System.Diagnostics.EventLogEntryType.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes a port on a the Windows Firewall
        /// </summary>
        /// <param name="title">Name of Firewall Rule</param>
        /// <param name="portNo">Port number</param>
        /// <param name="protocol">TCP, UDP</param>
        /// <returns>True if successful</returns>
        public static bool DeAuthorizePort(string title, int portNo, NET_FW_IP_PROTOCOL protocol)
        {
            try
            {
                if (OSVersion.GetOSVersion() <= OSVersion.OS.WIN_2003)
                {
                    dynamic fwMgr = CreateCOMObject(PROGID_FIREWALL_MANAGER);
                    dynamic profile = fwMgr.LocalPolicy.CurrentProfile;

                    profile.GloballyOpenPorts.Remove(portNo, protocol);
                }
                else
                {
                    dynamic firewallPolicy = CreateCOMObject(PROGID_FW_POLICY);
                    firewallPolicy.Rules.Remove(title);
                }
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog("Error DeAuthorizing firewall port -> " + e.ToString(), System.Diagnostics.EventLogEntryType.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes all references in the firewall for the specified ports/protocol combination and named rules entries
        /// </summary>
        /// <param name="appTitle">Name of Firewall Rule for Authorizing application</param>
        /// <param name="applicationPath">Path to the executable</param>
        /// <param name="portTitle">Name of Firewall Rule for Authorizing port</param>
        /// <param name="portNo">Port number</param>
        /// <param name="protocol">TCP, UDP</param>
        /// <returns>True if successful</returns>
        public static bool CleanUpFirewall(string appTitle, string applicationPath, string portTitle, int portNo, NET_FW_IP_PROTOCOL protocol)
        {
            try
            {
                if (OSVersion.GetOSVersion() <= OSVersion.OS.WIN_2003)
                {
                    // Windows XP always only makes one entry even if multiple calls are made to add. For some reason getting list (Item) throws an exception
                    DeAuthorizePort(portTitle, portNo, protocol);
                    DeAuthorizeApplication(appTitle, applicationPath);
                }
                else
                {
                    dynamic firewallPolicy = CreateCOMObject(PROGID_FW_POLICY);

                    try
                    {
                        while (true)
                        {
                            firewallPolicy.Rules.Item(portTitle); // Get the item
                            firewallPolicy.Rules.Remove(portTitle); // Keep removing all entries for open ports (duplicates)
                        }
                    }
                    catch { } // When the entries run out, Item throws an exception, H_RESULT_NOT_FOUND

                    try
                    {
                        while (true)
                        {
                            firewallPolicy.Rules.Item(appTitle); // Get the item
                            firewallPolicy.Rules.Remove(appTitle); // Keep removing all entries for Authorized apps (duplicates)
                        }
                    }
                    catch { } // When the entries run out, Item throws an exception, H_RESULT_NOT_FOUND
                }
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog("Error cleaning up firewall entries -> " + e.ToString(), System.Diagnostics.EventLogEntryType.Warning);
                return false;
            }

            return true;
        }
    }
}
