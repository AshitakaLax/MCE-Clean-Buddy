using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using MCEBuddy.Globals;

namespace MCEBuddy.Util
{
    public static class FilePaths
    {
        /// <summary>
        /// A safe way to get all the files in a directory and sub directory without crashing on UnauthorizedException
        /// </summary>
        /// <param name="rootPath">Starting directory</param>
        /// <param name="patternMatch">Filename pattern match</param>
        /// <param name="searchOption">Search subdirectories or only top level directory for files</param>
        /// <returns>List of files</returns>
        public static IEnumerable<string> GetDirectoryFiles(string rootPath, string patternMatch, SearchOption searchOption)
        {
            IEnumerable<string> foundFiles = Enumerable.Empty<string>(); // Start with an empty container

            if (searchOption == SearchOption.AllDirectories)
            {
                try
                {
                    IEnumerable<string> subDirs = Directory.EnumerateDirectories(rootPath);
                    foreach (string dir in subDirs)
                    {
                        try
                        {
                            foundFiles = foundFiles.Concat(GetDirectoryFiles(dir, patternMatch, searchOption)); // Add files in subdirectories recursively to the list
                        }
                        catch (UnauthorizedAccessException) { } // Incase we have an access error - we don't want to mask the rest
                    }
                }
                catch (UnauthorizedAccessException) { } // Incase we have an access error - we don't want to mask the rest
            }

            try
            {
                foundFiles = foundFiles.Concat(Directory.EnumerateFiles(rootPath, patternMatch)); // Add files from the current directory to the list
            }
            catch (UnauthorizedAccessException) { } // Incase we have an access error - we don't want to mask the rest

            return foundFiles; // This is it finally
        }

        public static string GetFullPathWithoutExtension(string FileName)
        {
            if ("" == FileName) return "";
            return Path.Combine(Path.GetDirectoryName(FileName), Path.GetFileNameWithoutExtension(FileName));
        }

        /// <summary>
        /// Checks if the characters is an illegal characters from filename AND filepath (different sets) (also [ and ] are not allowed by MCEBuddy)
        /// </summary>
        /// <param name="filePathChar">Character to check</param>
        /// <returns>True if not allowed by file path or file name</returns>
        public static bool IsIllegalFilePathAndNameChar(char filePathChar)
        {
            // /:*? are missing are GetInvalidPathChars
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()) + "/:*?" + "[]"; // [ ] are used in INI section names and cannot be allowed in filenames

            foreach (char lDisallowed in invalid)
            {
                if (filePathChar == lDisallowed)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Removes illegal characters from filename AND filepath (different sets) (also [ and ] are not allowed by MCEBuddy)
        /// </summary>
        /// <param name="filePathAndName">Filepath and filename</param>
        /// <returns>Cleaned filepath and filename</returns>
        public static string RemoveIllegalFilePathAndNameChars(string filePathAndName)
        {
            if (String.IsNullOrWhiteSpace(filePathAndName))
                return "";

            // /:*? are missing from GetInvalidPathChars
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()) + "/:*?" + "[]"; // [ ] are used in INI section names and cannot be allowed in filenames

            foreach (char lDisallowed in invalid)
            {
                filePathAndName = filePathAndName.Replace(lDisallowed.ToString(System.Globalization.CultureInfo.InvariantCulture), "");
            }

            return filePathAndName.Trim();
        }

        /// <summary>
        /// Removes illegal characters from filename (also [ and ] are not allowed by MCEBuddy)
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <returns>Cleaned filename</returns>
        public static string RemoveIllegalFileNameChars(string fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName))
                return "";

            string invalid = new string(Path.GetInvalidFileNameChars()) + "[]"; // [ ] are used in INI section names and cannot be allowed in filenames

            foreach (char lDisallowed in invalid)
            {
                fileName = fileName.Replace(lDisallowed.ToString(System.Globalization.CultureInfo.InvariantCulture), "");
            }

            return fileName.Trim();
        }

        /// <summary>
        /// Removes illegal characters from filepaths (also [ and ] are not allowed by MCEBuddy)
        /// </summary>
        /// <param name="filePath">Filename</param>
        /// <returns>Cleaned filepath</returns>
        public static string RemoveIllegalFilePathChars(string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath))
                return "";

            // /:*? are missing from GetInvalidPathChars
            string invalid = new string(Path.GetInvalidPathChars()) + "/:*?" + "[]"; // [ ] are used in INI section names and cannot be allowed in filenames

            foreach (char lDisallowed in invalid)
            {
                filePath = filePath.Replace(lDisallowed.ToString(System.Globalization.CultureInfo.InvariantCulture), "");
            }

            return filePath.Trim();
        }

        public static string CleanExt(string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath))
                return "";
            else
                return Path.GetExtension(filePath).ToLower().Trim();
        }

        /// <summary>
        /// Adds a quote around the filepath so commandline programs can properly process the filePath.
        /// </summary>
        /// <param name="path">filepath</param>
        /// <returns>Quotes filepath</returns>
        public static string FixSpaces(string path)
        {
            // Code below is not required as command prompt takes care of these somehow - we don't escape the special characters
            /*if (path == "")
                return @"""";

            // Check for backslash
            path = path.Replace("\\", "\\\\");

            // Check for double quotes
            path = path.Replace("\"", "\\\"");*/
            
            // Encapsulate in double quotes
            path = "\"" + path + "\"";

            return path;
        }

        /// <summary>
        /// Escapes all special characters in the path
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>Escaped path characters</returns>
        public static string EscapePath(string path)
        {
            return EscapeString.StringLiteral(path);
        }

        public static void CreateDir(string path)
        {
            if (! Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception)
                { }
            }
        }
    }

    /// <summary>
    /// Creates literal represenation of the escapte characters in a string including spaces
    /// </summary>
    public class EscapeString
    {
        static readonly IDictionary<string, string> m_replaceDict = new Dictionary<string, string>();
        const string ms_regexEscapes = @"[\a\b\f\n\r\t\v\\""\ ]";

        public static string StringLiteral(string i_string)
        {
            return Regex.Replace(i_string, ms_regexEscapes, match);
        }

        public static string CharLiteral(char c)
        {
            return c == '\'' ? @"'\''" : string.Format("'{0}'", c);
        }

        private static string match(Match m)
        {
            string match = m.ToString();
            if (m_replaceDict.ContainsKey(match))
            {
                return m_replaceDict[match];
            }

            throw new NotSupportedException();
        }

        static EscapeString()
        {
            m_replaceDict.Add("\a", @"\a");
            m_replaceDict.Add("\b", @"\b");
            m_replaceDict.Add("\f", @"\f");
            m_replaceDict.Add("\n", @"\n");
            m_replaceDict.Add("\r", @"\r");
            m_replaceDict.Add("\t", @"\t");
            m_replaceDict.Add("\v", @"\v");

            m_replaceDict.Add("\\", @"\\");
            m_replaceDict.Add("\0", @"\0");

            //The SO parser gets fooled by the verbatim version 
            //of the string to replace - @"\"""
            //so use the 'regular' version
            m_replaceDict.Add("\"", "\\\"");
            m_replaceDict.Add(" ", @"\ ");
        }
    }
}
