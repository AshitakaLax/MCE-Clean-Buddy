using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace MCEBuddy.Util
{
    public class Ini
    {
        /// <summary>
        /// Flag indicates whether to handle issues with special characters like [;= in the INI files or just pass through the content exactly as is
        /// </summary>
        private static bool realWorldSubstitutions = true;

        /// <summary>
        /// Cleans up the section name and removes unacceptable characters
        ///  ] character cannot be used in the Section Header
        /// </summary>
        /// <param name="section">Section string</param>
        private static string clean_section(string section)
        {
            if (!realWorldSubstitutions)
                return section;

            if (String.IsNullOrWhiteSpace(section))
                return section;
            else
                return section.Replace("]", "");
        }

        /// <summary>
        /// Cleans up the Key and removes unacceptable characters
        ///  = character cannot be used in the Key and it cannot start with a ;
        /// </summary>
        /// <param name="key">Key string</param>
        private static string clean_key(string key)
        {
            if (!realWorldSubstitutions)
                return key;

            if (String.IsNullOrWhiteSpace(key))
                return key;
            else
            {
                if (key.Trim()[0] == ';') // Check for 1st ; character
                    key = key.Trim().Substring(1, key.Trim().Length - 1); // Skip the 1st ; character ignoring whitespaces
                return key.Replace("=", "");
            }
        }

        /// <summary>
        /// Convert a byte array into a string
        /// </summary>
        /// <param name="data">Byte array</param>
        /// <param name="code_page">Code page to use to convert byte to string (Hint: Encoding.Default.CodePage</param>
        /// <returns>Byte array converted to a String</returns>
        private static string byte_to_string(byte[] data, int code_page)
        {
            Encoding Enc = Encoding.GetEncoding(code_page);
            int inx = Array.FindIndex(data, 0, (x) => x == 0); //search for 0
            if (inx >= 0)
                return (Enc.GetString(data, 0, inx));
            else
                return (Enc.GetString(data));
        }

        // API declarations
        /// <summary>
        /// The GetPrivateProfileInt function retrieves an integer associated with a key in the specified section of an initialization file.
        /// </summary>
        /// <param name="lpApplicationName">Pointer to a null-terminated string specifying the name of the section in the initialization file.</param>
        /// <param name="lpKeyName">Pointer to the null-terminated string specifying the name of the key whose value is to be retrieved. This value is in the form of a string; the GetPrivateProfileInt function converts the string into an integer and returns the integer.</param>
        /// <param name="nDefault">Specifies the default value to return if the key name cannot be found in the initialization file.</param>
        /// <param name="lpFileName">Pointer to a null-terminated string that specifies the name of the initialization file. If this parameter does not contain a full path to the file, the system searches for the file in the Windows directory.</param>
        /// <returns>The return value is the integer equivalent of the string following the specified key name in the specified initialization file. If the key is not found, the return value is the specified default value. If the value of the key is less than zero, the return value is zero.</returns>
        [DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileInt(string lpApplicationName, string lpKeyName, int nDefault, string lpFileName);
        /// <summary>
        /// The WritePrivateProfileString function copies a string into the specified section of an initialization file.
        /// </summary>
        /// <param name="lpApplicationName">Pointer to a null-terminated string containing the name of the section to which the string will be copied. If the section does not exist, it is created. The name of the section is case-independent; the string can be any combination of uppercase and lowercase letters.</param>
        /// <param name="lpKeyName">Pointer to the null-terminated string containing the name of the key to be associated with a string. If the key does not exist in the specified section, it is created. If this parameter is NULL, the entire section, including all entries within the section, is deleted.</param>
        /// <param name="lpString">Pointer to a null-terminated string to be written to the file. If this parameter is NULL, the key pointed to by the lpKeyName parameter is deleted.</param>
        /// <param name="lpFileName">Pointer to a null-terminated string that specifies the name of the initialization file.</param>
        /// <returns>If the function successfully copies the string to the initialization file, the return value is nonzero; if the function fails, or if it flushes the cached version of the most recently accessed initialization file, the return value is zero.</returns>
        [DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
        private static extern int WritePrivateProfileString(string lpApplicationName, string lpKeyName, string lpString, string lpFileName);
        /// <summary>
        /// The GetPrivateProfileString function retrieves a string from the specified section in an initialization file.
        /// </summary>
        /// <param name="lpApplicationName">Pointer to a null-terminated string that specifies the name of the section containing the key name. If this parameter is NULL, the GetPrivateProfileString function copies all section names in the file to the supplied buffer.</param>
        /// <param name="lpKeyName">Pointer to the null-terminated string specifying the name of the key whose associated string is to be retrieved. If this parameter is NULL, all key names in the section specified by the lpAppName parameter are copied to the buffer specified by the lpReturnedString parameter.</param>
        /// <param name="lpDefault">Pointer to a null-terminated default string. If the lpKeyName key cannot be found in the initialization file, GetPrivateProfileString copies the default string to the lpReturnedString buffer. This parameter cannot be NULL. <br>Avoid specifying a default string with trailing blank characters. The function inserts a null character in the lpReturnedString buffer to strip any trailing blanks.</br></param>
        /// <param name="lpReturnedString">Pointer to the buffer that receives the retrieved string.</param>
        /// <param name="nSize">Specifies the size, in TCHARs, of the buffer pointed to by the lpReturnedString parameter.</param>
        /// <param name="lpFileName">Pointer to a null-terminated string that specifies the name of the initialization file. If this parameter does not contain a full path to the file, the system searches for the file in the Windows directory.</param>
        /// <returns>The return value is the number of characters copied to the buffer, not including the terminating null character.</returns>
        [DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string lpApplicationName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);
        /// <summary>
        /// The GetPrivateProfileSectionNames function retrieves the names of all sections in an initialization file.
        /// </summary>
        /// <param name="lpszReturnBuffer">Pointer to a buffer that receives the section names associated with the named file. The buffer is filled with one or more null-terminated strings; the last string is followed by a second null character.</param>
        /// <param name="nSize">Specifies the size, in TCHARs, of the buffer pointed to by the lpszReturnBuffer parameter.</param>
        /// <param name="lpFileName">Pointer to a null-terminated string that specifies the name of the initialization file. If this parameter is NULL, the function searches the Win.ini file. If this parameter does not contain a full path to the file, the system searches for the file in the Windows directory.</param>
        /// <returns>The return value specifies the number of characters copied to the specified buffer, not including the terminating null character. If the buffer is not large enough to contain all the section names associated with the specified initialization file, the return value is equal to the length specified by nSize minus two.</returns>
        [DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileSectionNames(byte[] lpszReturnBuffer, int nSize, string lpFileName);
        /// <summary>
        /// The WritePrivateProfileSection function replaces the keys and values for the specified section in an initialization file.
        /// </summary>
        /// <param name="lpAppName">Pointer to a null-terminated string specifying the name of the section in which data is written. This section name is typically the name of the calling application.</param>
        /// <param name="lpString">Pointer to a buffer containing the new key names and associated values that are to be written to the named section.</param>
        /// <param name="lpFileName">Pointer to a null-terminated string containing the name of the initialization file. If this parameter does not contain a full path for the file, the function searches the Windows directory for the file. If the file does not exist and lpFileName does not contain a full path, the function creates the file in the Windows directory. The function does not create a file if lpFileName contains the full path and file name of a file that does not exist.</param>
        /// <returns>If the function succeeds, the return value is nonzero.<br>If the function fails, the return value is zero.</br></returns>
        [DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
        private static extern int WritePrivateProfileSection(string lpAppName, string lpString, string lpFileName);
        /// <summary>
        /// The GetPrivateProfileSection function retrieves all the keys and values for the specified section of an initialization file.
        /// </summary>
        /// <param name="lpApplicationName">Pointer to a null-terminated string containing the name of the section to which the string will be copied. If the section does not exist, it is created. The name of the section is case-independent; the string can be any combination of uppercase and lowercase letters.</param>
        /// <param name="lpszReturnBuffer">A pointer to a buffer that receives the key name and value pairs associated with the named section. The buffer is filled with one or more null-terminated strings; the last string is followed by a second null character.</param>
        /// <param name="nSize">The size of the buffer pointed to by the lpReturnedString parameter, in characters. The maximum profile section size is 32,767 characters.</param>
        /// <param name="lpFileName">Pointer to a null-terminated string that specifies the name of the initialization file.</param>
        /// <returns>The return value specifies the number of characters copied to the buffer, not including the terminating null character. If the buffer is not large enough to contain all the key name and value pairs associated with the named section, the return value is equal to nSize minus two.</returns>
        [DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
        static extern uint GetPrivateProfileSection(string lpApplicationName, byte[] lpszReturnBuffer, int nSize, string lpFileName);

        /// <summary>Constructs a new IniReader instance.</summary>
        /// <param name="file">Specifies the full path to the INI file (the file doesn't have to exist).</param>
        public Ini(string file)
        {
            Filename = file;

            try
            {
                // Create a new file if it's doesn't exist in UTF16-LE with BOM (only supported Unicode style for INI files)
                // Refer to http://www.codeproject.com/Articles/9071/Using-Unicode-in-INI-files
                if (!File.Exists(file))
                {
                    using (FileStream fs = File.Create(file))
                    {
                        using (StreamWriter sw = new StreamWriter(fs, new UnicodeEncoding(false, true))) // UTF16, LE with BOM
                        {
                            sw.Close();
                        }
                        fs.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Log.AppLog.WriteEntry("Unable to create INI file. Error : " + e.ToString(), Log.LogEntryType.Error);
                Filename = ""; // No where to write, ignore it
            }
        }

        /// <summary>Gets or sets the full path to the INI file.</summary>
        /// <value>A String representing the full path to the INI file.</value>
        public string Filename
        {
            get
            {
                return m_Filename;
            }
            set
            {
                m_Filename = value;
            }
        }
        /// <summary>Gets or sets the section you're working in. (aka 'the active section')</summary>
        /// <value>A String representing the section you're working in.</value>
        public string Section
        {
            get
            {
                return m_Section;
            }
            set
            {
                m_Section = clean_section(value);
            }
        }
        /// <summary>Reads an Integer from the specified key of the specified section.</summary>
        /// <param name="section">The section to search in.</param>
        /// <param name="key">The key from which to return the value.</param>
        /// <param name="defVal">The value to return if the specified key isn't found.</param>
        /// <returns>Returns the value of the specified section/key pair, or returns the default value if the specified section/key pair isn't found in the INI file.</returns>
        public int ReadInteger(string section, string key, int defVal)
        {
            return GetPrivateProfileInt(clean_section(section), clean_key(key), defVal, Filename);
        }
        /// <summary>Reads an Integer from the specified key of the specified section.</summary>
        /// <param name="section">The section to search in.</param>
        /// <param name="key">The key from which to return the value.</param>
        /// <returns>Returns the value of the specified section/key pair, or returns 0 if the specified section/key pair isn't found in the INI file.</returns>
        public int ReadInteger(string section, string key)
        {
            return ReadInteger(section, key, 0);
        }
        /// <summary>Reads an Integer from the specified key of the active section.</summary>
        /// <param name="key">The key from which to return the value.</param>
        /// <param name="defVal">The section to search in.</param>
        /// <returns>Returns the value of the specified Key, or returns the default value if the specified Key isn't found in the active section of the INI file.</returns>
        public int ReadInteger(string key, int defVal)
        {
            return ReadInteger(Section, key, defVal);
        }
        /// <summary>Reads an Integer from the specified key of the active section.</summary>
        /// <param name="key">The key from which to return the value.</param>
        /// <returns>Returns the value of the specified key, or returns 0 if the specified key isn't found in the active section of the INI file.</returns>
        public int ReadInteger(string key)
        {
            return ReadInteger(key, 0);
        }
        /// <summary>Reads a String from the specified key of the specified section.</summary>
        /// <param name="section">The section to search in.</param>
        /// <param name="key">The key from which to return the value.</param>
        /// <param name="defVal">The value to return if the specified key isn't found.</param>
        /// <returns>Returns the value of the specified section/key pair, or returns the default value if the specified section/key pair isn't found in the INI file.</returns>
        public string ReadString(string section, string key, string defVal)
        {
            StringBuilder sb = new StringBuilder(MAX_SECTION_ENTRY);
            int Ret = GetPrivateProfileString(clean_section(section), clean_key(key), defVal, sb, MAX_SECTION_ENTRY, Filename);
            //return byte_to_string(sb, Encoding.Default.CodePage); // convert a byte array to a string using the current code page
            //return Encoding.ASCII.GetString(sb).Trim('\0'); // convert byte array to string removing null characters
            return sb.ToString();
        }
        /// <summary>Reads a String from the specified key of the specified section.</summary>
        /// <param name="section">The section to search in.</param>
        /// <param name="key">The key from which to return the value.</param>
        /// <returns>Returns the value of the specified section/key pair, or returns an empty String if the specified section/key pair isn't found in the INI file.</returns>
        public string ReadString(string section, string key)
        {
            return ReadString(section, key, "");
        }
        /// <summary>Reads a String from the specified key of the active section.</summary>
        /// <param name="key">The key from which to return the value.</param>
        /// <returns>Returns the value of the specified key, or returns an empty String if the specified key isn't found in the active section of the INI file.</returns>
        public string ReadString(string key)
        {
            return ReadString(Section, key);
        }
        /// <summary>Reads a Long from the specified key of the specified section.</summary>
        /// <param name="section">The section to search in.</param>
        /// <param name="key">The key from which to return the value.</param>
        /// <param name="defVal">The value to return if the specified key isn't found.</param>
        /// <returns>Returns the value of the specified section/key pair, or returns the default value if the specified section/key pair isn't found in the INI file.</returns>
        public long ReadLong(string section, string key, long defVal)
        {
            try
            {
                return long.Parse(ReadString(section, key, defVal.ToString()));
            }
            catch
            {
                return defVal;
            }
        }
        /// <summary>Reads a Long from the specified key of the specified section.</summary>
        /// <param name="section">The section to search in.</param>
        /// <param name="key">The key from which to return the value.</param>
        /// <returns>Returns the value of the specified section/key pair, or returns 0 if the specified section/key pair isn't found in the INI file.</returns>
        public long ReadLong(string section, string key)
        {
            return ReadLong(section, key, 0);
        }
        /// <summary>Reads a Long from the specified key of the active section.</summary>
        /// <param name="key">The key from which to return the value.</param>
        /// <param name="defVal">The section to search in.</param>
        /// <returns>Returns the value of the specified key, or returns the default value if the specified key isn't found in the active section of the INI file.</returns>
        public long ReadLong(string key, long defVal)
        {
            return ReadLong(Section, key, defVal);
        }
        /// <summary>Reads a Long from the specified key of the active section.</summary>
        /// <param name="key">The key from which to return the value.</param>
        /// <returns>Returns the value of the specified Key, or returns 0 if the specified Key isn't found in the active section of the INI file.</returns>
        public long ReadLong(string key)
        {
            return ReadLong(key, 0);
        }
        /// <summary>Reads a Byte array from the specified key of the specified section.</summary>
        /// <param name="section">The section to search in.</param>
        /// <param name="key">The key from which to return the value.</param>
        /// <returns>Returns the value of the specified section/key pair, or returns null (Nothing in VB.NET) if the specified section/key pair isn't found in the INI file.</returns>
        public byte[] ReadByteArray(string section, string key)
        {
            try
            {
                return Convert.FromBase64String(ReadString(section, key));
            }
            catch { }
            return null;
        }
        /// <summary>Reads a Byte array from the specified key of the active section.</summary>
        /// <param name="key">The key from which to return the value.</param>
        /// <returns>Returns the value of the specified key, or returns null (Nothing in VB.NET) if the specified key pair isn't found in the active section of the INI file.</returns>
        public byte[] ReadByteArray(string key)
        {
            return ReadByteArray(Section, key);
        }
        /// <summary>Reads a Boolean from the specified key of the specified section.</summary>
        /// <param name="section">The section to search in.</param>
        /// <param name="key">The key from which to return the value.</param>
        /// <param name="defVal">The value to return if the specified key isn't found.</param>
        /// <returns>Returns the value of the specified section/key pair, or returns the default value if the specified section/key pair isn't found in the INI file.</returns>
        public bool ReadBoolean(string section, string key, bool defVal)
        {
            try
            {
                return Boolean.Parse(ReadString(section, key, defVal.ToString()));
            }
            catch
            {
                return defVal;
            }
        }
        /// <summary>Reads a Boolean from the specified key of the specified section.</summary>
        /// <param name="section">The section to search in.</param>
        /// <param name="key">The key from which to return the value.</param>
        /// <returns>Returns the value of the specified section/key pair, or returns false if the specified section/key pair isn't found in the INI file.</returns>
        public bool ReadBoolean(string section, string key)
        {
            return ReadBoolean(section, key, false);
        }
        /// <summary>Reads a Boolean from the specified key of the specified section.</summary>
        /// <param name="key">The key from which to return the value.</param>
        /// <param name="defVal">The value to return if the specified key isn't found.</param>
        /// <returns>Returns the value of the specified key pair, or returns the default value if the specified key isn't found in the active section of the INI file.</returns>
        public bool ReadBoolean(string key, bool defVal)
        {
            return ReadBoolean(Section, key, defVal);
        }
        /// <summary>Reads a Boolean from the specified key of the specified section.</summary>
        /// <param name="key">The key from which to return the value.</param>
        /// <returns>Returns the value of the specified key, or returns false if the specified key isn't found in the active section of the INI file.</returns>
        public bool ReadBoolean(string key)
        {
            return ReadBoolean(Section, key);
        }
        /// <summary>Writes an Integer to the specified key in the specified section.</summary>
        /// <param name="section">The section to write in.</param>
        /// <param name="key">The key to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool Write(string section, string key, int value)
        {
            return Write(section, key, value.ToString());
        }
        /// <summary>Writes an Integer to the specified key in the active section.</summary>
        /// <param name="key">The key to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool Write(string key, int value)
        {
            return Write(Section, key, value);
        }
        /// <summary>Writes a String to the specified key in the specified section.</summary>
        /// <param name="section">Specifies the section to write in.</param>
        /// <param name="key">Specifies the key to write to.</param>
        /// <param name="value">Specifies the value to write.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool Write(string section, string key, string value)
        {
            return (WritePrivateProfileString(clean_section(section), clean_key(key), value, Filename) != 0);
        }
        /// <summary>Writes a String to the specified key in the active section.</summary>
        ///	<param name="key">The key to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool Write(string key, string value)
        {
            return Write(Section, key, value);
        }
        /// <summary>Writes a Long to the specified key in the specified section.</summary>
        /// <param name="section">The section to write in.</param>
        /// <param name="key">The key to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool Write(string section, string key, long value)
        {
            return Write(section, key, value.ToString());
        }
        /// <summary>Writes a Long to the specified key in the active section.</summary>
        /// <param name="key">The key to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool Write(string key, long value)
        {
            return Write(Section, key, value);
        }
        /// <summary>Writes a Byte array to the specified key in the specified section.</summary>
        /// <param name="section">The section to write in.</param>
        /// <param name="key">The key to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool Write(string section, string key, byte[] value)
        {
            if (value == null)
                return Write(section, key, (string)null);
            else
                return Write(section, key, value, 0, value.Length);
        }
        /// <summary>Writes a Byte array to the specified key in the active section.</summary>
        /// <param name="key">The key to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool Write(string key, byte[] value)
        {
            return Write(Section, key, value);
        }
        /// <summary>Writes a Byte array to the specified key in the specified section.</summary>
        /// <param name="section">The section to write in.</param>
        /// <param name="key">The key to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="offset">An offset in <i>value</i>.</param>
        /// <param name="length">The number of elements of <i>value</i> to convert.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool Write(string section, string key, byte[] value, int offset, int length)
        {
            if (value == null)
                return Write(section, key, (string)null);
            else
                return Write(section, key, Convert.ToBase64String(value, offset, length));
        }
        /// <summary>Writes a Boolean to the specified key in the specified section.</summary>
        /// <param name="section">The section to write in.</param>
        /// <param name="key">The key to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool Write(string section, string key, bool value)
        {
            return Write(section, key, value.ToString());
        }
        /// <summary>Writes a Boolean to the specified key in the active section.</summary>
        /// <param name="key">The key to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool Write(string key, bool value)
        {
            return Write(Section, key, value);
        }
        /// <summary>Deletes a key from the specified section.</summary>
        /// <param name="section">The section to delete from.</param>
        /// <param name="key">The key to delete.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool DeleteKey(string section, string key)
        {
            return (WritePrivateProfileString(clean_section(section), clean_key(key), null, Filename) != 0);
        }
        /// <summary>Deletes a key from the active section.</summary>
        /// <param name="key">The key to delete.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool DeleteKey(string key)
        {
            return (WritePrivateProfileString(Section, clean_key(key), null, Filename) != 0);
        }
        /// <summary>Deletes a section from an INI file.</summary>
        /// <param name="section">The section to delete.</param>
        /// <returns>Returns true if the function succeeds, false otherwise.</returns>
        public bool DeleteSection(string section)
        {
            return WritePrivateProfileSection(clean_section(section), null, Filename) != 0;
        }
        /// <summary>Retrieves a list of all available sections in the INI file.</summary>
        /// <returns>Returns an ArrayList with all available sections.</returns>

        public List<string> GetSectionNames()
        {
            try
            {
                byte[] buffer = new byte[MAX_SECTION_NAMES];
                GetPrivateProfileSectionNames(buffer, MAX_SECTION_NAMES, Filename);
                string[] parts = Encoding.Unicode.GetString(buffer).Trim('\0').Split('\0'); // INI is stored in UTF-16 LE
                List<string> SectionList = new List<string>(parts);
                return SectionList;
            }
            catch { }
            return new List<string>();
        }

        public List<string> GetSectionNames(string PartialSectionName)
        {
            List<string> SectionList = GetSectionNames();
            for (int Idx = 0; Idx < SectionList.Count; Idx++)
            {
                if (!SectionList[Idx].ToLower().Contains(clean_section(PartialSectionName).ToLower()))
                {
                    SectionList.RemoveAt(Idx);
                    Idx--;
                }
            }
            return SectionList;
        }

        public SortedList<string, string> GetSectionKeyValuePairs(string SectionName)
        {
            try
            {
                byte[] buffer = new byte[MAX_SECTION_ENTRY];
                GetPrivateProfileSection(clean_section(SectionName), buffer, MAX_SECTION_ENTRY, Filename);
                string[] parts = Encoding.Unicode.GetString(buffer).Trim('\0').Split('\0'); // INI is stored in UTF-16 LE
                SortedList<string, string> SectionValues = new SortedList<string, string>();
                foreach (string kvp in parts)
                {
                    if (kvp.Contains("="))
                    {
                        string[] kvpa = kvp.Split(new char[] { '=' }, 2);
                        SectionValues.Add(kvpa[0], kvpa[1]);
                    }
                }
                return SectionValues;
            }
            catch { }
            return new SortedList<string, string>();
        }

        //Private variables and constants
        /// <summary>
        /// Holds the full path to the INI file.
        /// </summary>
        private string m_Filename;
        /// <summary>
        /// Holds the active section name
        /// </summary>
        private string m_Section;
        /// <summary>
        /// The maximum number of bytes in a section buffer.
        /// </summary>
        private const int MAX_SECTION_ENTRY = 100 * 1024; // 100 KB Max size of each INI file section (~800 emtries)
        private const int MAX_SECTION_NAMES = 3 * 1024 * 1024; // 3 MB Max size of total INI file section list (~250,000 entries)
    }
}
