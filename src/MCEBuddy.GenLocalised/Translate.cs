using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;

using MCEBuddy.Globals;
using MCEBuddy.Util;

namespace MCEBuddy.GenLocalised
{
    public class Translate
    {
        private static string ClientID = "MCEBuddy";
        private static string ClientSecret = "rOOsKEhkSN4GngEmTPdhRoR/haJdGO8odA1ILGu5i30=";

        private TranslateService.LanguageServiceClient _client;

        private string _localisationPath;
        private string _languageCode, _languageName;
        private string _englishFilePath;

        private int _progress = 0;
        private int _replaced = 0;
        private int _downloaded = 0;
        private int _manual = 0;
        private int _duplicate = 0;
        private bool _active = false;
        private bool _success = false;
        private bool _quotaError = false;

        private Thread _workerProc;

        //private int bingCount = 0;
        private int quotaCount = 0;
        private int _row = 0;
        private static bool _cancelled = false;

        private static AccessToken admToken = null;

        public Translate(string localisationPath, string languageCode, string languageName, int row)
        {
            _localisationPath = localisationPath;
            _languageCode = languageCode;
            _languageName = languageName;
            _row = row;
            _englishFilePath = Path.Combine(_localisationPath, "en.txt");
            if (!File.Exists(_englishFilePath))
            {
                Console.WriteLine("English source file not found " + _englishFilePath);
                _cancelled = true; // exit the app
            }

            if (admToken == null) // We initialize it just one until the timer expires
            {
                //Get Client Id and Client Secret from https://datamarket.azure.com/developer/applications/
                //Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 
                AuthenticationToken admAuth = new AuthenticationToken(ClientID, ClientSecret);
                admToken = admAuth.GetAccessToken();
            }
        }

        public static string[] LanguageNames(string[] languageCodes)
        {
            if (admToken == null) // We initialize it just one until the timer expires
            {
                //Get Client Id and Client Secret from https://datamarket.azure.com/developer/applications/
                //Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 
                AuthenticationToken admAuth = new AuthenticationToken(ClientID, ClientSecret);
                admToken = admAuth.GetAccessToken();
            }

            TranslateService.LanguageServiceClient client = new TranslateService.LanguageServiceClient();
            string[] langNames = client.GetLanguageNames("Bearer" + " " + admToken.access_token, CultureInfo.CurrentCulture.Name, languageCodes);
            client.Close();
            return langNames;
        }

        public static string[] SupportedLanguages()
        {
            if (admToken == null) // We initialize it just one until the timer expires
            {
                //Get Client Id and Client Secret from https://datamarket.azure.com/developer/applications/
                //Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 
                AuthenticationToken admAuth = new AuthenticationToken(ClientID, ClientSecret);
                admToken = admAuth.GetAccessToken();
            }

            TranslateService.LanguageServiceClient client = new TranslateService.LanguageServiceClient();
            string[] supportedLangs = client.GetLanguagesForTranslate("Bearer" + " " + admToken.access_token);
            client.Close();
            return supportedLangs;
        }

        public int Progress
        { get { return _progress; } }

        public bool Active
        { get { return _active; } }

        public bool Success
        { get { return _success; } }

        public bool QuotaError
        { get { return _quotaError; } }

        public int Downloaded
        { get { return _downloaded; } }

        public int Replaced
        { get { return _replaced; } }

        public int Manual
        { get { return _manual; } }

        public int Duplicate
        { get { return _duplicate; } }

        private string TranslateText(string englishText, string language)
        {
            try
            {
                // Keep alternating to avoid quota limit issues
                return _client.Translate("Bearer" + " " + admToken.access_token, englishText, "en", language, "text/plain", "general");
            }
            catch (Exception)
            {
                // Maybe IP over quota in which case we need to back off for a while
                // Authentication token expired (valid only 10 minutes at a time)
                // Get a new authentication token
                _client.Close(); // Close it
                _quotaError = true;

                // Wait for 150 seconds for each failure and try again with exponential waiting time each period
                for (long i = 150 * (long)System.Math.Pow((double)++quotaCount, (double)2); i > 0; i--)
                {
                    if (_cancelled)
                        return null;

                    if (Console.KeyAvailable) // if any thread detected an cancel signal (ESC)
                    {
                        ConsoleKeyInfo cki = Console.ReadKey(true);
                        if ((cki.Key == ConsoleKey.Escape) || ((cki.Modifiers == ConsoleModifiers.Control) && (cki.Key == ConsoleKey.C)))
                        {
                            _cancelled = true;
                            return null;
                        }
                    }
                    string consoleTxt = _languageCode + " (" + _languageName + ")" + " -> " + _progress + " translations. Quota exceeded, retrying in " + i + " seconds...";
                    TranslateAll.WriteStatus(consoleTxt, _row, ConsoleColor.Red);
                    Thread.Sleep(1000); // Wait 1 second
                }

                _client = new TranslateService.LanguageServiceClient(); // Create a new connection
     
                try
                {
                    _quotaError = false;
                    AuthenticationToken admAuth = new AuthenticationToken(ClientID, ClientSecret);
                    admToken = admAuth.GetAccessToken();
                    return _client.Translate("Bearer" + " " + admToken.access_token, englishText, "en", language, "text/plain", "general");
                }
                catch (Exception)
                {
                    // Give up and return error
                    //Console.WriteLine(e.ToString()); // Dump the error on the screen
                    return null; // Return a null to indicate an error
                }
            }
        }

        public Thread TranslateFile()
        {
            _workerProc = new Thread(WorkerProc);
            _workerProc.Start();
            return _workerProc;
        }

        public void WorkerProc()
        {
            _success = true; // all is good unless something fails
            _active = true;

            _client = new TranslateService.LanguageServiceClient();

            // Get the manually corrected values if they exist
            System.Collections.Generic.SortedList<string, string> translatedValues = new SortedList<string, string>(); // automatic translations
            System.Collections.Generic.SortedList<string, string> manualValues = new SortedList<string, string>(); // User translations

            string fixedLanguageFileName = Path.Combine(_localisationPath, _languageCode + "-fixed.txt");

            if (File.Exists(fixedLanguageFileName))
            {
                using (CsvFileReader reader = new CsvFileReader(fixedLanguageFileName))
                {
                    CsvRow row = new CsvRow();
                    while (reader.ReadRow(row))
                    {
                        if (row.Count > 1)
                        {
                            if (!manualValues.ContainsKey(row[0]))
                            {
                                manualValues.Add(row[0], row[1]); // Create a sorted list of values with manual updates
                            }
                        }
                    }
                }
            }

            // Create the language file
            string languageFileName = Path.Combine(_localisationPath, _languageCode + ".txt");

            // If the file already exists then don't overwrite it
            // So we don't overwrite from scratch. If new translations are required then the existing files needs to be deleted first
            // If the language file already exists then load the file so we can add new strings and deletes one not being used
            // BING now limits the amount of data that can be downloaded so we should only update what's required instead of downloading everthing again
            CsvRow orgRow = new CsvRow();
            if (File.Exists(languageFileName))
            {
                using (CsvFileReader orgReader = new CsvFileReader(languageFileName))
                {
                    while (orgReader.ReadRow(orgRow)) // Read all the data in
                    {
                        if (manualValues.ContainsKey(orgRow[0]))
                            _manual++; // We have a manual translation

                        if (!translatedValues.ContainsKey(orgRow[0]))
                            translatedValues.Add(orgRow[0], orgRow[1]); // Add to the sorted listed - we don't add manual translations here, just track them. They are added on the fly at runtime
                        else
                            _duplicate++; // Duplicate
                    }
                }
            }

            using (CsvFileWriter writer = new CsvFileWriter(languageFileName))
            {
                using (CsvFileReader reader = new CsvFileReader(_englishFilePath))
                {
                    CsvRow row = new CsvRow();
                    while (reader.ReadRow(row))
                    {
                        if (row.Count > 0)
                        {
                            while (row.Count > 1) row.RemoveAt(1);
                            string transText = "";
                            if (translatedValues.ContainsKey(row[0]))
                            {
                                transText = translatedValues[row[0]];
                                _replaced++;
                            }
                            else
                            {
                                transText = TranslateText(row[0], _languageCode);

                                // check for a null return, then BING failed due to quota issues
                                if (transText == null)
                                {
                                    _success = false; // break out and clean up the file
                                    break;
                                }
                                else
                                    _downloaded++;
                            }

                            row.Add(transText);
                            writer.WriteRow(row); // Write the data
                            _progress++;

                            if (_cancelled)
                            {
                                _success = false;
                                break;
                            }

                            if (Console.KeyAvailable) // check if any other thread caught the cancellation event (ESC)
                            {
                                ConsoleKeyInfo cki = Console.ReadKey(true);
                                if ((cki.Key == ConsoleKey.Escape) || ((cki.Modifiers == ConsoleModifiers.Control) && (cki.Key == ConsoleKey.C)))
                                {
                                    _cancelled = true; // signal all thread to exit
                                    _success = false; // Clean up the file
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            _client.Close();

            _active = false; // First set this to avoid overwriting console

            // Check for failure, if so delete the file - we do not keep partially translated files
            if (_success == false)
            {
                string consoleTxt = _languageCode + " (" + _languageName + ")" +" -> " + "Translation cancelled...";
                TranslateAll.WriteStatus(consoleTxt, _row, ConsoleColor.DarkMagenta);
                File.Delete(languageFileName);
            }
            else
            {
                string consoleTxt = _languageCode + " (" + _languageName + ")" + " -> " + _progress + " translations complete! Downloaded " + _downloaded + " Existing " + _replaced + " Manual " + _manual + " Duplicate " + _duplicate;
                TranslateAll.WriteStatus(consoleTxt, _row, ConsoleColor.Green);
            }

            return;
        }
    }
}
