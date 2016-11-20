using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MCEBuddy.GenLocalised
{
    public class TranslateAll
    {
        private Translate[] _translateThreads;
        private string[] _languageCodes, _languageNames;
        private string _localisationPath;
        private static Queue<string> qLock = new Queue<string>(); // Just a locking object
        private const int _consoleWidth = 110; // Width in characters of console
        
        public TranslateAll(string localisationPath)
        {
            WriteStatus("Getting supported languages...", 0, ConsoleColor.White);
            _localisationPath = localisationPath;
            _languageCodes = Translate.SupportedLanguages();
            _languageNames = Translate.LanguageNames(_languageCodes);
            _translateThreads = new Translate[_languageCodes.Length];
            Console.SetWindowSize(_consoleWidth, _languageCodes.Length + 1);
            Console.Title = "MCEBuddy GenLocalized, generate localized translations - " + _languageCodes.Length + " Languages";
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("GenLocalized - Generate localised strings for MCEBuddy, press ESC to cancel...");
            Console.ResetColor();
            Console.CursorVisible = false;
        }

        public static void WriteStatus(string outputStr, int row, ConsoleColor color)
        {
            Monitor.Enter(qLock); // It has be one operation to avoid color issues
            if (outputStr.Length > (_consoleWidth - 2)) outputStr = outputStr.Substring(0, (_consoleWidth - 2));
            Console.CursorLeft = 0;
            Console.CursorTop = row;
            Console.ForegroundColor = color;
            Console.Write(outputStr.PadRight((_consoleWidth - 2), ' ') + "\r");
            Console.ResetColor();
            Console.Out.Flush(); // Ensure everything is written
            Monitor.Exit(qLock);
        }


        public void Run()
        {
            // Now we only download new translations that don't exist due to BING Quota issues
            // NOTE: Translate will add/delete individual translations for any files that already exist, so DELETE the file to create fresh translation files
            for (int i = 0; i < _languageCodes.Length; i++)
            {
                _translateThreads[i] = new Translate(_localisationPath, _languageCodes[i], _languageNames[i], i+1);
                if (_languageCodes[i] != "en")
                {
                    WriteStatus(_languageCodes[i] + " (" + _languageNames[i] + ")" + " -> " + "Starting translations...", i + 1,ConsoleColor.White);
                    Thread workerThread = _translateThreads[i].TranslateFile();

                    // Workaround code start
                    // This code limits to one thread download at a time
                    // This is required since BING reports us sending data too fast and blocks the IP - workaround
                    while (workerThread.ThreadState != ThreadState.Stopped)
                    {
                        if (!_translateThreads[i].QuotaError) // If we are in Quota error don't overwrite the text
                            WriteStatus(_languageCodes[i] + " (" + _languageNames[i] + ")" + " -> " + _translateThreads[i].Progress + " translations. Downloaded " + _translateThreads[i].Downloaded + " Existing " + _translateThreads[i].Replaced + " Manual " + _translateThreads[i].Manual + " Duplicate " + _translateThreads[i].Duplicate, i + 1, ConsoleColor.Blue);
                        Thread.Sleep(200);
                    }
                    // End of workaround code
                }
                else
                    WriteStatus(_languageCodes[i] + " (" + _languageNames[i] + ")" + " -> " + "Master file, no translation required", i + 1, ConsoleColor.DarkYellow);
            }

            // Wait for all thread to complete
            Thread.Sleep(300); // Give the thread some time to start
            bool done = false;
            while (!done)
            {
                done = true;
                for (int i = 0; i < _languageCodes.Length; i++)
                {
                    if (_translateThreads[i].Active)
                    {
                        if (!_translateThreads[i].QuotaError) // If we are in Quota error don't overwrite the text
                            WriteStatus(_languageCodes[i] + " (" + _languageNames[i] + ")" + " -> " + _translateThreads[i].Progress + " translations. Downloaded " + _translateThreads[i].Downloaded + " Existing " + _translateThreads[i].Replaced + " Manual " + _translateThreads[i].Manual + " Duplicate " + _translateThreads[i].Duplicate, i + 1, ConsoleColor.Blue);
                        done = false;
                    }
                }
                Thread.Sleep(200);
            }

            // Handle the chinese exception, select traditional
            string chineseFileBase = Path.Combine(_localisationPath, "zh");
            if (File.Exists(chineseFileBase + "-CHT.txt")) File.Copy(chineseFileBase + "-CHT.txt",chineseFileBase + ".txt",true);

            Console.BackgroundColor = ConsoleColor.Green;
            WriteStatus("GenLocalized MCEBuddy - DONE!!! press any key to continue...", 0, ConsoleColor.Yellow);
            ConsoleKeyInfo cki = Console.ReadKey(true);
        }
    }
}
