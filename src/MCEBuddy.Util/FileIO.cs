using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.AccessControl;

namespace MCEBuddy.Util
{
    public class FileIO
    {
        public static void TryFileDelete(string FileName, bool useRecycleBin=false)
        {
                try
                {
                    if (useRecycleBin)
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(FileName, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    else
                        File.Delete(FileName);
                }
                catch { }
        }

        /// <summary>
        /// When you have 2 files move the second time to take the place of the first file and delete the first file
        /// </summary>
        /// <param name="FileName">First file</param>
        /// <param name="ReplacementFileName">Second File</param>
        public static void TryFileReplace(string FileName, string ReplacementFileName)
        {
            string tempFile = FileName + ".tmp";
            if ((File.Exists(FileName)) && (File.Exists(ReplacementFileName)))
            {
                // Rename the original file to a temp file
                try
                {
                    FileIO.MoveAndInheritPermissions(FileName, tempFile);
                }
                catch
                {
                    return;
                }
                // Rename the replacement file to the same as the original and revert if this fails
                try
                {
                    FileIO.MoveAndInheritPermissions(ReplacementFileName, FileName);
                }
                catch
                {
                    try
                    {
                        FileIO.MoveAndInheritPermissions(tempFile, FileName);
                    }
                    catch { }
                    return;
                }

                // Delete the temp file
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    return;
                }
            }
        }

        public static long FileSize(string FileName)
        {
            if (!String.IsNullOrWhiteSpace(FileName)) // Check if the filename is enclosed in quotes ("), if so remove it before checking
                FileName = FileName.Replace("\"", ""); // Remove the quotes (quotes are in valid filename character anyways)

            if (File.Exists( FileName))
            {
                try
                {
                    FileInfo fi = new FileInfo(FileName);
                    return fi.Length;
                }
                catch (Exception)
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);


        public static long GetFreeDiskSpace(string FileName)
        {
            FileName = Path.GetFullPath(FileName);

            if ((FileName.Length > 1) && (FileName.Substring(1, 1) == ":"))
            {
                System.IO.DriveInfo di = new DriveInfo(FileName.Substring(0, 2));
                return di.AvailableFreeSpace;
            }
            else
            {
                ulong FreeBytesAvailable;
                ulong TotalNumberOfBytes;
                ulong TotalNumberOfFreeBytes;

                bool success = GetDiskFreeSpaceEx(Path.GetPathRoot(FileName) + "\\", out FreeBytesAvailable, out TotalNumberOfBytes, out TotalNumberOfFreeBytes);
                if (!success)
                {
                    return -1;
                }
                else
                {
                    return (long)FreeBytesAvailable;
                }
            }
        }

        public static bool FileLocked( string FileName)
        {
            FileStream fs = null;

            try
            {
                fs = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read); //Get read access to file, We can share reading by other processes.
            }
            catch (Exception)
            {
                return true;
            }
            finally
            {
                if (fs != null ) fs.Close();
            }
            return false;
        }

        public static void ClearFolder( string folder)
        {
            if (Directory.Exists(folder))
            {
                System.IO.DirectoryInfo downloadedMessageInfo = null;
                try
                {
                    downloadedMessageInfo = new DirectoryInfo(folder);
                }
                catch (Exception)
                {
                    return;
                }

                foreach (FileInfo file in downloadedMessageInfo.GetFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception)
                    {
                    }
                }
                foreach (DirectoryInfo dir in downloadedMessageInfo.GetDirectories())
                {
                    try
                    {
                        dir.Delete(true);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public static DateTime GetFileCreationTime(string fileName)
        {
            try
            {
                FileInfo fi = new FileInfo(fileName);
                return fi.CreationTime;
            }
            catch (Exception)
            {
                return DateTime.Parse("1900-01-01T00:00:00Z");
            }
        }

        public static DateTime GetFileModifiedTime(string fileName)
        {
            try
            {
                FileInfo fi = new FileInfo(fileName);
                return fi.LastWriteTime;
            }
            catch (Exception)
            {
                return DateTime.Parse("1900-01-01T00:00:00Z");
            }
        }

        /// <summary>
        /// Moves the file from source to destination and inherits the access control from the destination folder. This does NOT reset the explicit permissions from the source folder
        /// </summary>
        /// <returns>NULL if successful, error message if inheriting the permissions failed</returns>
        public static string MoveAndInheritPermissions(string source, string destination)
        {
            File.Move(source, destination); //  Move the file

            try
            {
                // Inherit the Access Control from destination
                // NOTE: Do we need to remove the permissions from the source location (explicit)? http://stackoverflow.com/questions/12811850/setting-a-files-acl-to-be-inherited
                FileSecurity fileSecurity = File.GetAccessControl(destination);
                fileSecurity.SetAccessRuleProtection(false, true);
                File.SetAccessControl(destination, fileSecurity);
            }
            catch (Exception e)
            {
                return e.ToString();
            }

            return null;
        }
    }
}
