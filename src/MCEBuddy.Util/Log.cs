using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

using MCEBuddy.Globals;

namespace MCEBuddy.Util
{
    public class Log : IDisposable
    {
        private string _logFile = "";
        /// <summary>
        /// Default global Log level for all log files
        /// </summary>
        public static Log.LogEntryType LogLevel = Log.LogEntryType.Information; // Log Level
        
        /// <summary>
        /// This it the global instance of the MCEBuddy log
        /// </summary>
        public static Log AppLog = new Log(Log.LogDestination.Null); // Global AppLog

        public enum LogEntryType
        {
            Error,
            Warning,
            Information,
            Debug
        } ;

        public enum LogDestination
        {
            Null,
            Console,
            LogFile
        };

        private LogDestination _logDestination = LogDestination.Null;

        private StreamWriter _logStream = null;

        /// <summary>
        /// Create a log file
        /// </summary>
        /// <param name="LogFile">Path to log file</param>
        public Log(string LogFile)
        {
            if (String.IsNullOrEmpty(LogFile))
            {
                throw new ArgumentException("Log file name must be specified");
            }

            _logDestination = LogDestination.LogFile;

            // Log to a file
            _logStream = new StreamWriter(LogFile, true, Encoding.Unicode);
            _logStream.AutoFlush = true;
            _logFile = LogFile;
        }

        /// <summary>
        /// Create a console or Null log
        /// </summary>
        /// <param name="logDestination"></param>
        public Log(LogDestination logDestination)
        {
            if (logDestination == LogDestination.LogFile)
            {
                throw new ArgumentException("Log file destination must supply file name");
            }

            _logDestination = logDestination;
        }

        public LogDestination Destination
        {
            get { return _logDestination; }
        }

        /// <summary>
        /// Create an empty log
        /// </summary>
        public Log()
        {
            // Log to null
            _logDestination = LogDestination.Null;
        }

        public void WriteEntry(string entryText, LogEntryType entryType, bool timeStamp = false)
        {
            WriteEntry(null, entryText, entryType, timeStamp);
        }

        public void WriteEntry(object obj, string entryText, LogEntryType entryType, bool timeStamp = true)
        {
            WriteEntry(obj.GetType().ToString(), entryText, entryType, timeStamp);
        }

        public void WriteEntry(string entryHeader, string entryText, LogEntryType entryType, bool timeStamp = false)
        {
            if (_logDestination == LogDestination.Null)
                return;

            if (entryType <= Log.LogLevel)
            {
                string logLines = "", timeLog = "";
                if (entryType < LogEntryType.Debug)
                {
                    // do it the hard way so I can refactor for internationalisation later
                    switch (entryType)
                    {
                        case LogEntryType.Error:
                                logLines += "ERROR" + "> ";
                                break;
                        case LogEntryType.Warning:
                                logLines += "WARNING" + "> ";
                                break;
                        case LogEntryType.Information:
                                logLines += "INFORMATION" + "> ";
                                break;
                    }
                }
                
                if (timeStamp)
                    timeLog = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture) + " ";

                if (entryHeader != null)
                    logLines += timeLog + (String.IsNullOrWhiteSpace(entryHeader) ? entryHeader : entryHeader + " --> ") + entryText;
                else
                    logLines += timeLog + "--> " + entryText;

                // Logging is not app critical, so do exception thrown
                // Can't dump to file so it goes to the bit bucket
                if (_logDestination == LogDestination.Console)
                {
                    try
                    {
                        Console.WriteLine(logLines);
                    }
                    catch { }
                }
                else if (_logDestination == LogDestination.LogFile)
                {
                    try
                    {
                        _logStream.WriteLine(logLines);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Flush the log file and write all pending entries
        /// </summary>
        public void Flush()
        {
            if (_logStream != null)
            {
                try
                {
                    _logStream.Flush();
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Close the log file and flush all pending entries
        /// </summary>
        public void Close()
        {
            if (_logDestination == LogDestination.LogFile)
            {
                if (_logStream != null)
                {
                    try
                    {
                        _logStream.Flush();
                        _logStream.Close();
                        _logStream = null; //mark it out
                    }
                    catch (Exception)
                    { }
                }
            }
        }

        /// <summary>
        /// Clear the log file and start afresh
        /// </summary>
        public void Clear()
        {
            if (_logDestination == LogDestination.LogFile)
            {
                if (_logStream != null)
                {
                    try
                    {
                        _logStream.Flush();
                        _logStream.Close();
                        _logStream = null; //mark it out

                        // Now reopen the log file NOT in append mode so it overwrites the existing log afresh
                        _logStream = new StreamWriter(_logFile, false, Encoding.Unicode);
                        _logStream.AutoFlush = true;
                    }
                    catch (Exception)
                    { }
                }
            }
        }

        public void Dispose()
        {
            WriteEntry(this, "Log Dispose function called...", Log.LogEntryType.Warning);
            Close();
        }

        /// <summary>
        /// Writes an entry to the event log but takes care of any exceptions (like source not found or permission issues)
        /// </summary>
        /// <param name="message">Message to write to log</param>
        /// <param name="type">Type of message</param>
        public static void WriteSystemEventLog(string message, EventLogEntryType type)
        {
            try
            {
                EventLog.WriteEntry(GlobalDefs.MCEBUDDY_EVENT_LOG_SOURCE, message, type, GlobalDefs.MCEBUDDY_EVENT_LOG_ID);
            }
            catch { }
        }
    }

}
