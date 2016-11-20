using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MCEBuddy.Globals;
using MCEBuddy.RemuxMediaCenter;
using MCEBuddy.Util;
using MCEBuddy.Configuration;

namespace MCEBuddy.RemuxTiVOStreams
{
    class Program
    {
        static int Main(string[] args)
        {
            MCEBuddyConf.GlobalMCEConfig = new MCEBuddyConf(GlobalDefs.ConfigFile); // Read the settings for global objects
            ConversionJobOptions cjo = new ConversionJobOptions(); // Start with an empty project
            cjo.workingPath = Environment.CurrentDirectory; // Set the default path to here
            Log.AppLog = new Log(Log.LogDestination.Console); // Redirect to console all output
            Log.LogLevel = Log.LogEntryType.Debug; // Print all messages
            string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Log.AppLog.WriteEntry("", "\r\nRemux TiVO file using DirectShow streams and TiVODecode as fallback", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "This file remuxes a TiVO file into a TS file.\r\nIf TiVO Desktop is installed, it will try to use the TiVO DirectShow filter to decrypt and then use FFMPEG.exe to remux the streams into a TS file.\r\nAs a fallback it will try to use TiVODecode.exe to decrypt and remux into a TS file.", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "Copyright (c) Ramit Bhalla, Build Version : " + currentVersion, Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "Build Date : " + System.IO.File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString(System.Globalization.CultureInfo.InvariantCulture), Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "", Log.LogEntryType.Debug);

            try
            {
                switch (args.Length) // Bad coding but efficient :)
                {
                    case 4: // GOOD SECTION
                        cjo.audioLanguage = args[3];
                        Log.AppLog.WriteEntry("", "RemuxTiVOStreams Audio Langage Code : " + cjo.audioLanguage, Log.LogEntryType.Debug);
                        goto case 3;
                    case 3:
                        cjo.tivoMAKKey = args[2];
                        Log.AppLog.WriteEntry("", "RemuxTiVOStreams MAK : " + cjo.tivoMAKKey, Log.LogEntryType.Debug);
                        goto case 2;
                    case 2:
                        if (!String.IsNullOrWhiteSpace(args[1])) // If it's empty use the current directory
                            cjo.workingPath = args[1];
                        Log.AppLog.WriteEntry("", "RemuxTiVOStreams Temp Path : " + cjo.workingPath, Log.LogEntryType.Debug);
                        goto case 1;
                    case 1:
                        if (String.IsNullOrWhiteSpace(args[0]))
                            goto default; // Bad usage
                        cjo.sourceVideo = args[0];
                        if (String.IsNullOrWhiteSpace(Path.GetDirectoryName(cjo.sourceVideo)))
                            cjo.sourceVideo = Path.Combine(Environment.CurrentDirectory, cjo.sourceVideo); // If the video doesn't have a path, it's in the current directory
                        Log.AppLog.WriteEntry("", "RemuxTiVOStreams Source File : " + cjo.sourceVideo, Log.LogEntryType.Debug);
                        break;
                    
                    case 0: // NO GOOD SECTION
                        goto default;
                    default:
                        Log.AppLog.WriteEntry("", "\r\nRemuxTiVOStreams Invalid Input", Log.LogEntryType.Debug);
                        Usage();
                        return -1; // Bad usage
                }
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry("", "\r\nRemuxTiVOStreams Invalid Input Error -> " + e.ToString() + "\r\n", Log.LogEntryType.Error);

                Usage();
                return -1; // Bad usage
            }

            Log.AppLog.WriteEntry("", "\r\nRemuxTiVOStreams trying to Remux TiVO file\r\n", Log.LogEntryType.Debug);

            try
            {
                RemuxMCERecording remuxTivo = new RemuxMCERecording(cjo, new JobStatus(), Log.AppLog);
                if (remuxTivo.RemuxTiVO())
                {
                    Log.AppLog.WriteEntry("", "\r\nRemuxTiVOStreams Successful!!", Log.LogEntryType.Debug);
                    return 0; // we good here
                }
                else
                {
                    Log.AppLog.WriteEntry("", "\r\nRemuxTiVOStreams Failed!!", Log.LogEntryType.Debug);
                    return -2; // too bad
                }
            }
            catch (Exception e1)
            {
                Log.AppLog.WriteEntry("", "\r\nRemuxTiVOStreams Crashed with Error -> " + e1.ToString() + "\r\n", Log.LogEntryType.Error);
                return -3; // We bugged out
            }
        }

        static void Usage()
        {
            Log.AppLog.WriteEntry("", "", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "Usage: RemuxTiVOStreams <TiVOFile> <TempPath> <MAK> <AudioLanguage>", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t<TiVOFile> -> Name of input TiVO file with extension .tivo", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t<TempPath> -> (Optional) Temporary path to use, otherwise it will use the current directory", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t<MAK> -> (Optional) Media Access Key (10 digit key) to use the fallback TiVODecode.exe (for TiVO Desktop, ensure you entered the MAK key in the application to use Streams decoding)", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\t<AudioLanguage> -> (Optional) 3 digit audio language to isolate while remuxing if detected", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\r\nThe program returns 0 if successful, -1 for bad input parameters and -2 if failed to remux, -3 if it crashes abnormally", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\r\nExamples:", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tRemuxTiVOStreams \"My Test.tivo\"", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tRemuxTiVOStreams \"My Test.tivo\" \"\" 1234567890", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tRemuxTiVOStreams \"My Test.tivo\" \"\" 1234567890 eng", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tRemuxTiVOStreams \"My Test.tivo\" \"\" \"\" eng", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tRemuxTiVOStreams \"My Test.tivo\" \"C:\\Temp\" 1234567890", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tRemuxTiVOStreams \"My Test.tivo\" \"C:\\Temp\" 1234567890 eng", Log.LogEntryType.Debug);
            Log.AppLog.WriteEntry("", "\tRemuxTiVOStreams \"My Test.tivo\" \"C:\\Temp\" \"\" eng", Log.LogEntryType.Debug);
        }
    }
}
