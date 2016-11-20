using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;

using MCEBuddy.Configuration;
using MCEBuddy.Util;
using MCEBuddy.Globals;
using MCEBuddy.CommercialScan;

namespace MCEBuddy.Service
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        private const int INSTALL_TIMEOUT = 60; // timeout in seconds for installation of custom apps
        private const string engineName = GlobalDefs.MCEBUDDY_SERVICE_NAME;

        public ProjectInstaller()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Runs a process with Medium Privileges (using Windows Explorer as the base security model).
        /// </summary>
        /// <param name="appPath">Full path or relative to application</param>
        /// <param name="cmdParameters">Parameters to pass to application</param>
        /// <param name="waitForExit">TRUE is you want to wait until process completes</param>
        /// <returns></returns>
        private bool RunCustomAppWithMediumPrivilege(string appPath, string cmdParameters, bool waitForExit)
        {
            try
            {
                AppProcess.StartAppWithMediumPrivilegeFromUISession(appPath, cmdParameters, waitForExit);
            }
            catch (Exception exp)
            {
                Log.WriteSystemEventLog("Failed to run custom app with medium rights. Error:" + exp.ToString(), EventLogEntryType.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Runs a custom installation application with parameters
        /// </summary>
        /// <param name="appPathName">Full path and name of executable</param>
        /// <param name="parameters">Parameters to pass to the installer</param>
        /// <param name="waitForExit">TRUE is you want to wait until process completes</param>
        /// <returns>True if successful, false otherwise</returns>
        private bool RunCustomApp(string appPathName, string parameters, bool waitForExit)
        {
            try
            {
                //Set up the process
                Process Proc = new Process();
                Proc.StartInfo.FileName = Path.GetFileName(appPathName); // the app lies in the extras directory
                Proc.StartInfo.Arguments = parameters;
                if (!String.IsNullOrWhiteSpace(Path.GetDirectoryName(appPathName))) // Set the working directory for batch files if there is a path
                    Proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(appPathName);
                Proc.StartInfo.UseShellExecute = true;
                Proc.StartInfo.CreateNoWindow = true;
                Proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                Proc.Start();
                if (waitForExit)
                    return Proc.WaitForExit(INSTALL_TIMEOUT * 1000); // Wait upto 60 seconds for the installation to complete
                else
                    return true;
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog("Unable to Install custom app -> " + appPathName + ". Error:" + e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// Used to delete the a directory, a clean up activity
        /// </summary>
        /// <param name="dirPath">Path to directory</param>
        private void DeleteDirectory(string dirPath)
        {
            try
            {
                Directory.Delete(dirPath, true); //Delete the compelte installation directory and any sub directories
            }
            catch (Exception e)
            {
                Log.WriteSystemEventLog("Unable to delete installation directory. Error:" + e.ToString(), EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Creates a directory that is fully exposed to read, write and execute by everyone
        /// </summary>
        /// <param name="dirPath">Path to directory</param>
        private void CreateExposedDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                try
                {
                    Directory.CreateDirectory(dirPath);
                }
                catch (Exception e)
                {
                    Log.WriteSystemEventLog("Unable to create directory during installation. Error:" + e.ToString(), EventLogEntryType.Error);
                }
            }

            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);

            SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
            NTAccount acct = sid.Translate(typeof(System.Security.Principal.NTAccount)) as System.Security.Principal.NTAccount;

            FileSystemAccessRule rule = new FileSystemAccessRule(acct.ToString(), FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
            if (!dirInfo.Exists)
            {
                DirectorySecurity security = new DirectorySecurity();
                security.SetAccessRule(rule);
                dirInfo.Create(security);
            }
            else
            {
                DirectorySecurity security = dirInfo.GetAccessControl();
                security.AddAccessRule(rule);
                dirInfo.SetAccessControl(security);
            }
        }

        private void serviceInstaller_AfterUninstall(object sender, InstallEventArgs e)
        {
            //MessageBox.Show("Called After Uninstall");

            //Stop the service if running
            WindowsService.StopService(engineName, 10000);

            //Delete the installation directory completely to avoid any artifacts
            string installPath = Context.Parameters["TARGETDIR"].Replace(@"\\", @"\").Trim();
            DeleteDirectory(installPath);
        }

        private void serviceInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            //MessageBox.Show("Called Before Uninstall");
            WindowsService.StopService(engineName, 10000);

            // Backup the history, config and profile files in the %HOMEPATH%, this only works if we are installing the same directory as it was originally installed
            string installPath = Context.Parameters["TARGETDIR"].Replace(@"\\", @"\").Trim();
            try
            {
                if (File.Exists(Path.Combine(installPath, @"config\history")))
                    File.Copy(Path.Combine(installPath, @"config\history"), Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "history"), true);
                else // If the History file doesn't exist, then the user cleared it, so ensure we delete the backup also to prevent it from coming back
                    File.Delete(Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "history"));
                if (File.Exists(Path.Combine(installPath, @"config\mcebuddy.conf")))
                    File.Copy(Path.Combine(installPath, @"config\mcebuddy.conf"), Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "mcebuddy.conf"), true);
                if (File.Exists(Path.Combine(installPath, @"config\profiles.conf")))
                    File.Copy(Path.Combine(installPath, @"config\profiles.conf"), Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "profiles.conf"), true);
                if (File.Exists(Path.Combine(installPath, @"comskip\comskip.ini")))
                    File.Copy(Path.Combine(installPath, @"comskip\comskip.ini"), Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "comskip.ini"), true);

                File.Delete(GlobalDefs.TempSettingsFile); // Delete the temp file
            }
            catch (Exception exp)
            {
                Log.WriteSystemEventLog("Failed to save config files. Error:" + exp.ToString(), EventLogEntryType.Error);
            }

        }

        private void serviceInstaller_Committed(object sender, InstallEventArgs e)
        {
            //MessageBox.Show("Called Committed");
        }
        
        private void serviceInstaller_BeforeInstall(object sender, InstallEventArgs e)
        {
            //MessageBox.Show("Called Before Install");

            //Stop the service
            //StopService(engineName, 10000);

            //By now the service is already installed so deleting it causes an install failure
            //We need to do this in the override install section
            //UnInstallService(engineName); // Delete the service if it exists otherwise the installer gives an error that service already exists
            //while (CheckServiceExists(engineName)) Thread.Sleep(1000); // Wait until the service is delete before proceeding otherwise we get an error
        }

        private void serviceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            //MessageBox.Show("Called AfterInstall");

            // Restore the history file from the %HOMEPATH% if it exists
            string installPath = Context.Parameters["TARGETDIR"].Replace(@"\\", @"\").Trim();
            try
            {
                // Copy the backup files back to the MCEBuddy installation directory with .old extensions (except history)
                // Convert History file to unicode while copying it back
                if (File.Exists(Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "history")))
                {
                    string sourceHistory = Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "history");
                    string destinationHistory = Path.Combine(installPath, @"config\history");
                    
                    // First check if the file is a UTF16LE file (required for INI files)
                    if (Encodings.GetFileEncoding(sourceHistory) != Encoding.Unicode)
                    {
                        using (StreamReader sourceRead = new StreamReader(sourceHistory)) // Open the source file for reading
                        {
                            FileIO.TryFileDelete(destinationHistory); // Try to delete the destination file first
                            using (FileStream destinationFile = File.Create(destinationHistory)) // Create the destination file
                            {
                                using (StreamWriter destinationWrite = new StreamWriter(destinationFile, new UnicodeEncoding(false, true))) // UTF16, LE with BOM for INI files
                                {
                                    // Read everything from the file
                                    string line;
                                    while ((line = sourceRead.ReadLine()) != null)
                                    {
                                        destinationWrite.WriteLine(line); // Write to the new file
                                    }
                                    destinationWrite.Close();
                                }
                                destinationFile.Close();
                            }
                            sourceRead.Close();
                        }
                    }
                    else // We are good, just copy the file
                        File.Copy(Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "history"), Path.Combine(installPath, @"config\history"), true);
                }
                if (File.Exists(Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "mcebuddy.conf")))
                    File.Copy(Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "mcebuddy.conf"), Path.Combine(installPath, @"config\mcebuddy.conf.old"), true);
                if (File.Exists(Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "profiles.conf")))
                    File.Copy(Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "profiles.conf"), Path.Combine(installPath, @"config\profiles.conf.old"), true);
                if (File.Exists(Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "comskip.ini")))
                    File.Copy(Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "comskip.ini"), Path.Combine(installPath, @"comskip\comskip.ini.old"), true);

                // Copy the settings from the last config file (retain user settings) (this will also take care of UTF16LE conversion for ini)
                if (File.Exists(Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "mcebuddy.conf")))
                {
                    MCEBuddyConf mceConf = new MCEBuddyConf(Path.Combine(Environment.GetEnvironmentVariable("HOMEPATH"), "mcebuddy.conf")); // Read the old config file
                    mceConf.WriteSettings(Path.Combine(installPath, @"config\mcebuddy.conf")); // Write to the new installation file
                }
            }
            catch (Exception exp)
            {
                Log.WriteSystemEventLog("Failed to restore config files. Error:" + exp.ToString(), EventLogEntryType.Error);
            }

            try
            {
                // Install extras (such as filters etc)
                // Check if Haali Media Splitter is installed, else install it
                string haaliKey = "";

                if (Environment.Is64BitOperatingSystem)
                    haaliKey = @"Software\Wow6432Node\HaaliMkx";
                else
                    haaliKey = @"Software\HaaliMkx";    

                if (Registry.LocalMachine.OpenSubKey(haaliKey) == null)
                {
                    if (MessageBox.Show("MCEBuddy suggests installing Haali Media Splitter for MKV playback support.\nPress OK to Install.", "MKV Playback Support Not Found", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.OK)
                        RunCustomApp(Path.Combine(installPath, "extras", "MatroskaSplitter"), "/S", true); // Install Haali Media Splitter (required for MKV playback)
                }

                // WTV Playback: Uninstall Windows Update KB2670838 if the resulting file does not play in MCE/WMP - nothing to do with ffdshow
                // Check if FFDSHOW is installed, else install FFDSHOW for MPEG2/AVI support else NoRecode profiles does not work
                bool modifyReg = true;
                string ffdshowKey = "";

                if (Environment.Is64BitOperatingSystem)
                    ffdshowKey = @"Software\GNU\ffdshow64";
                else
                    ffdshowKey = @"Software\GNU\ffdshow";

                if (Registry.LocalMachine.OpenSubKey(ffdshowKey) == null) // if the key doesn't exist i.e. not installed, it returns null
                {
                    if (MessageBox.Show("MCEBuddy suggests installing FFDShow for MPEG2/AVI playback support.\nPress OK to install.", "FFDShow Not Found", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.OK)
                    {
                        if (RunCustomAppWithMediumPrivilege(Path.Combine(installPath, "extras", "ffdshow.exe"), " /silent", true) == true) // FFDShow needs to be installed with Medium Privileges else the installation does not work
                            Registry.LocalMachine.DeleteSubKeyTree(@"Software\GNU"); // Delete all the keys under GNU (we will only create those required later) - TODO: Can we find a clean way to do a custom install for FFDSHOw, default Silent install enables many filters not required
                        else
                        {
                            MessageBox.Show("FFDShow installation error, please install it manually from the Extra's folder in the MCEBuddy Installation directory.\nEnsure you select MPEG2 during installation", "Error installing FFDSHOW");
                            modifyReg = false;
                        }
                    }
                    else
                        modifyReg = false;
                }

                // FFDSHOW, enable the MPEG-2 splitter in the registry (whenever it's installed)
                if (modifyReg)
                {
                    Registry.LocalMachine.CreateSubKey(ffdshowKey); // Create the key if required (since the silent installation does not create one)
                    Registry.LocalMachine.OpenSubKey(ffdshowKey, true).SetValue("mpegAVI", 1, RegistryValueKind.DWord);
                    Registry.LocalMachine.OpenSubKey(ffdshowKey, true).SetValue("mpg2", 1, RegistryValueKind.DWord);
                    Registry.LocalMachine.OpenSubKey(ffdshowKey, true).SetValue("mpg1", 1, RegistryValueKind.DWord);
                }

                // Install ShowAnalyzer - last one, delayed start to avoid MSI install conflict (only 1 at a time)
                if (!Scanner.ShowAnalyzerInstalled()) // only if not installed
                    if (MessageBox.Show("MCEBuddy comes with Comskip commercial detection software built in. However some countries have better commercial detection with ShowAnalyzer.\nPress OK if you would like to install ShowAnalyzer.", "Commercial detection options", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.OK)
                        RunCustomApp(Path.Combine(installPath, "extras", "InstallShowAnalyzerDelayed.bat"), "", false);
            }
            catch (Exception exp)
            {
                Log.WriteSystemEventLog("Failed to install custom apps. Error:" + exp.ToString(), EventLogEntryType.Error);
            }

            try
            {
                //Set the restart properties for the service
                ServiceControlManager scm = new ServiceControlManager();
                scm.SetRestartOnFailure(engineName);
            }
            catch (Exception exp)
            {
                Log.WriteSystemEventLog("Failed to set MCEBuddy service restart parameters. Error:" + exp.ToString(), EventLogEntryType.Error);
            }

            /*string installPath = Context.Parameters["TARGETDIR"].Replace(@"\\", @"\").Trim();
            CreateExposedDirectory(Path.Combine(installPath, "config"));
            CreateExposedDirectory(Path.Combine(installPath, "log"));
            CreateExposedDirectory(Path.Combine(installPath, "working"));*/ //Dont' create directories here since it create Canonical issues, create within application at runtime

            WindowsService.StartService(engineName);
        }
    }
}
