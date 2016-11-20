using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MCEBuddy.Util
{
    public static class Text
    {
        /// <summary>
        /// Overrides default Contains to check for case and culture specific comparison (Turkey Test). E.g. Use StringComparison.OrdinalIgnoreCase
        /// </summary>
        public static bool Contains(this string source, string toCheck, StringComparison options)
        {
            return source.IndexOf(toCheck, options) >= 0;
        }

        public static string StripNonAlphaNumeric(string SourceString)
        {
            return Regex.Replace(SourceString, "[^a-z0-9\\s]", "", RegexOptions.IgnoreCase);
        }

        public static string ReplaceString(string OldString, string stringToBeReplaced, string newString, bool CaseInsensitive)
        {

            int index = 0;

            if (CaseInsensitive)
                OldString.IndexOf(stringToBeReplaced, 0, StringComparison.OrdinalIgnoreCase);
            else
                index = OldString.IndexOf(stringToBeReplaced, 0);

            if (index == -1)
                return OldString;

            OldString = OldString.Substring(0, index) + newString + OldString.Substring(index + stringToBeReplaced.Length);

            return OldString;
        }

        public static string Slugify(string phrase, int maxLength = 50)
        {
            string str = phrase.ToLower();

            // invalid chars, make into spaces
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // convert multiple spaces/hyphens into one space
            str = Regex.Replace(str, @"[\s-]+", " ").Trim();
            // cut and trim it
            str = str.Substring(0, str.Length <= maxLength ? str.Length : maxLength).Trim();
            // hyphens
            str = Regex.Replace(str, @"\s", "-");

            return str;
        }

        public static bool ContainsUnicode(string stringToTest)
        {
            const int MaxAnsiCode = 255;

            return stringToTest.ToCharArray().Any(c => c > MaxAnsiCode);

        }

        /// <summary>
        /// Matches a Wildcard or Regex expression against a pattern
        /// Multiple strings are separated by ; and ~ is the NOT operator
        /// </summary>
        /// <param name="stringName">String to match against pattern</param>
        /// <param name="pattern">Pattern</param>
        /// <returns>True if it's a match else false</returns>
        public static bool WildcardRegexPatternMatch(string stringName, string pattern)
        {
            bool returnMatch = false;

            // Check against wildcard -> file path regex
            string wildcardRegex = pattern;

            if (wildcardRegex.Contains("regex:")) // check if it's a RegEx pattern, then handle directly
            {
                wildcardRegex = wildcardRegex.Replace("regex:", "");
                Regex rxFileMatch = new Regex(wildcardRegex, RegexOptions.IgnoreCase);
                returnMatch = rxFileMatch.IsMatch(stringName.ToLower());
            }
            else foreach (string wildcardPattern in wildcardRegex.Split(';')) // check each pattern individually, allows us to have a not (~) match for each pattern (since this is regular match, we match in lower case)
                {
                    bool avoidList = false; // a NOT pattern
                    string wildcard = wildcardPattern;

                    if (String.IsNullOrEmpty(wildcard)) // incase a smart person ended the pattern with a ;
                        continue;

                    // Check if this is a NOT list, i.e. select all files except... (starts with a ~)
                    if (wildcard.Contains("~"))
                    {
                        wildcard = wildcard.Replace("~", ""); // remove the ~ character from the expression
                        avoidList = true;
                    }

                    wildcard = WildcardToRegex(wildcard); // convert to RegEx for matching
                    Regex rxFileMatch = new Regex(wildcard, RegexOptions.IgnoreCase); // Create a Regex for matching

                    if (avoidList) // Check if it's a NOT pattern
                    {
                        if (rxFileMatch.IsMatch(stringName.ToLower())) // If even one NOT pattern matches, we return false (no need to continue)
                            return false;
                    }
                    else
                        returnMatch = returnMatch || rxFileMatch.IsMatch(stringName.ToLower()); // Keep looking for atleast one match or incase we find a NOT match else where in the pattern loop
                }

            return returnMatch;
        }

        /// <summary>
        /// Converts a string wildcard expression to a regex expression including spaces
        /// </summary>
        /// <param name="wildcard">Wilcard string</param>
        /// <returns>Regex expression</returns>
        public static string WildcardToRegex(string wildcard)
        {
            StringBuilder sb = new StringBuilder(wildcard.Length + 8);

            sb.Append("^");

            for (int i = 0; i < wildcard.Length; i++)
            {
                char c = wildcard[i];
                switch (c)
                {
                    case '*':
                        sb.Append(".*");
                        break;
                    case '?':
                        sb.Append(".");
                        break;
                    case '\\':
                        if (i < wildcard.Length - 1)
                            sb.Append(Regex.Escape(wildcard[++i].ToString(System.Globalization.CultureInfo.InvariantCulture)));
                        break;
                    case ';':
                        sb.Append("$|^");
                        break;
                    default:
                        sb.Append(Regex.Escape(wildcard[i].ToString(System.Globalization.CultureInfo.InvariantCulture)));
                        break;
                }
            }

            sb.Append("$");

            return sb.ToString();
        }

        public class Wildcard : Regex
        {
            /// <summary>
            /// Initializes a wildcard with the given search pattern.
            /// </summary>
            /// <param name="pattern">The wildcard pattern to match.</param>
            public Wildcard(string pattern)
                : base(WildcardToRegex(pattern))
            {
            }

            /// <summary>
            /// Initializes a wildcard with the given search pattern and options.
            /// </summary>
            /// <param name="pattern">The wildcard pattern to match.</param>
            /// <param name="options">A combination of one or more
            /// <see cref="System.Text.RegexOptions"/>.</param>
            public Wildcard(string pattern, RegexOptions options)
                : base(WildcardToRegex(pattern), options)
            {
            }

            /// <summary>
            /// Converts a wildcard to a regex.
            /// </summary>
            /// <param name="pattern">The wildcard pattern to convert.</param>
            /// <returns>A regex equivalent of the given wildcard.</returns>
            public static string WildcardToRegex(string pattern)
            {
                return "^" + Regex.Escape(pattern).
                Replace(";", "$|^").
                Replace("\\*", ".*").
                Replace("\\?", ".") + "$";
            }
        }
    }
}
