using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ServiceProcess;

namespace MCEBuddy.Util
{
    public static class WindowsService
    {
        /// <summary>
        /// Gets the current status of the service in Windows Service Manager
        /// </summary>
        /// <param name="serviceName">Name of service as given in the Service Manager</param>
        /// <returns>Service status, null if the service does not exist</returns>
        public static ServiceControllerStatus? GetServiceStatus(string serviceName)
        {
            if (!CheckServiceExists(serviceName)) // check if the service exists otherwise the installation can fail
                return null;

            try
            {
                ServiceController controller = new ServiceController(serviceName);
                return controller.Status;
            }
            catch (Exception)
            {
                // no such service exists
                return null;
            }
        }

        /// <summary>
        /// Stop a running service in Windows Service Manager
        /// </summary>
        /// <param name="serviceName">Name of service as given in the Service Manager</param>
        /// <param name="timeoutMilliseconds">Timeout in Milliseconds</param>
        /// <returns>True if successful, false if error or service not found</returns>
        public static bool StopService(string serviceName, int timeoutMilliseconds)
        {
            if (!CheckServiceExists(serviceName)) // check if the service exists otherwise the installation can fail
                return false;

            try
            {
                ServiceController controller = new ServiceController(serviceName);

                if ((controller.Status != ServiceControllerStatus.Stopped) && (controller.Status != ServiceControllerStatus.StopPending))
                {
                    TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                    controller.Stop();
                    controller.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                }
            }
            catch (Exception e)
            {
                // no such service exists
                Log.WriteSystemEventLog("Error stopping " + serviceName + " service. Error:\r\n" + e.ToString(), EventLogEntryType.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Used to start a service in Windows Service Manager
        /// </summary>
        /// <param name="serviceName">Name of service as given in the Service Manager</param>
        /// <returns>True if successful, false if error or service not found</returns>
        public static bool StartService(string serviceName)
        {
            if (!CheckServiceExists(serviceName)) // check if the service exists otherwise the installation can fail
                return false;

            try
            {
                var controller = new ServiceController(serviceName);

                if ((controller.Status == ServiceControllerStatus.Stopped) || (controller.Status == ServiceControllerStatus.StopPending))
                    controller.Start();
            }
            catch (Exception e)
            {
                // no such service exists
                Log.WriteSystemEventLog("Error starting " + serviceName + " service. Error:\r\n" + e.ToString(), EventLogEntryType.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Used to check if a windows service is installed
        /// </summary>
        /// <param name="serviceName">Name of service as given in the service manager</param>
        /// <returns>True if it exists, false if service not found</returns>
        public static bool CheckServiceExists(string serviceName) //check if the Service Exists
        {
            try
            {
                ServiceController service = new ServiceController(serviceName);
                ServiceControllerStatus status = service.Status; // Check if we can get the status, otherwise it will lead to an exception for an nonexisting service
                return true; //it exists
            }
            catch (Exception)
            {
                return false; //it does not exist
            }
        }

        /// <summary>
        /// Uninstall a windows service
        /// </summary>
        /// <param name="serviceName">Name of service as given in the service manager</param>
        /// <returns>True if successful, false if error or service not found</returns>
        public static bool UnInstallService(string serviceName) // Uninstall the service
        {
            if (!CheckServiceExists(serviceName)) // check if the service exists otherwise the installation can fail
                return false;

            try
            {
                Process Proc = new Process();
                Proc.StartInfo.FileName = "sc.exe";
                Proc.StartInfo.Arguments = "delete " + serviceName;
                Proc.StartInfo.CreateNoWindow = false;
                Proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                Proc.StartInfo.WorkingDirectory = Environment.SystemDirectory;
                Proc.StartInfo.UseShellExecute = true;
                Proc.Start();
                Proc.WaitForExit(); // Wait until the installation completes before returning
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog("Error deleting " + serviceName + " service. Error:\r\n" + e.ToString(), EventLogEntryType.Warning);
                return false;
            }

            return true;
        }
    }
}
