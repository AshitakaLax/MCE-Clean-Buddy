using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCEBuddy.Util
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class CsvRow : List<string>
    {
        public string LineText { get; set; }
    }

    public class CsvFileWriter : StreamWriter
    {
        public CsvFileWriter(Stream stream)
            : base(stream)
        {
        }

        public CsvFileWriter(string filename)
            : base(filename)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        public void WriteRow(CsvRow row)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string value in row)
            {
                // Add separator if this isn't the first value
                if (builder.Length > 0)
                    builder.Append('\t');
                
                builder.Append(value);
            }
            row.LineText = builder.ToString();
            WriteLine(row.LineText);
        }
    }

    public class CsvFileReader : StreamReader
    {
        public CsvFileReader(Stream stream)
            : base(stream)
        {
        }

        public CsvFileReader(string filename)
            : base(filename)
        {
        }

        /// <summary>
        /// Reads a row of data from a CSV file
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool ReadRow(CsvRow row)
        {
            row.RemoveRange(0, row.Count); // clear it before starting
            row.LineText = ReadLine();
            if (String.IsNullOrEmpty(row.LineText))
                return false;

            // Tab is the delimiter, break up the lines by tab
            string[] lines = row.LineText.Split('\t');
            foreach (string line in lines)
                row.Add(line);

            // Return true if any columns read
            return (row.Count > 0);
        }
    }
}
