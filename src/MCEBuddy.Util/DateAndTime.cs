using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCEBuddy.Util
{
    public static class DateAndTime
    {
        #region TimeZones
        /// <summary>
        /// Array of timezones, each timezone format is string array of {3 Digit Timezone, numeric timezone, timezone full name}
        /// E.g. {"ACDT", "-1030", "Australian Central Daylight"}
        /// </summary>
        public static string[][] TimeZones = new string[][] {
            new string[] {"ACDT", "+1030", "Australian Central Daylight"},
            new string[] {"ACST", "+0930", "Australian Central Standard"},
            new string[] {"ADT", "-0300", "(US) Atlantic Daylight"},
            new string[] {"AEDT", "+1100", "Australian East Daylight"},
            new string[] {"AEST", "+1000", "Australian East Standard"},
            new string[] {"AHDT", "-0900", ""},
            new string[] {"AHST", "-1000", ""},
            new string[] {"AST", "-0400", "(US) Atlantic Standard"},
            new string[] {"AT", "-0200", "Azores"},
            new string[] {"AWDT", "+0900", "Australian West Daylight"},
            new string[] {"AWST", "+0800", "Australian West Standard"},
            new string[] {"BAT", "+0300", "Bhagdad"},
            new string[] {"BDST", "+0200", "British Double Summer"},
            new string[] {"BET", "-1100", "Bering Standard"},
            new string[] {"BST", "-0300", "Brazil Standard"},
            new string[] {"BT", "+0300", "Baghdad"},
            new string[] {"BZT2", "-0300", "Brazil Zone 2"},
            new string[] {"CADT", "+1030", "Central Australian Daylight"},
            new string[] {"CAST", "+0930", "Central Australian Standard"},
            new string[] {"CAT", "-1000", "Central Alaska"},
            new string[] {"CCT", "+0800", "China Coast"},
            new string[] {"CDT", "-0500", "(US) Central Daylight"},
            new string[] {"CED", "+0200", "Central European Daylight"},
            new string[] {"CET", "+0100", "Central European"},
            new string[] {"CST", "-0600", "(US) Central Standard"},
            new string[] {"EAST", "+1000", "Eastern Australian Standard"},
            new string[] {"EDT", "-0400", "(US) Eastern Daylight"},
            new string[] {"EED", "+0300", "Eastern European Daylight"},
            new string[] {"EET", "+0200", "Eastern Europe"},
            new string[] {"EEST", "+0300", "Eastern Europe Summer"},
            new string[] {"EST", "-0500", "(US) Eastern Standard"},
            new string[] {"FST", "+0200", "French Summer"},
            new string[] {"FWT", "+0100", "French Winter"},
            new string[] {"GMT", "-0000", "Greenwich Mean"},
            new string[] {"GST", "+1000", "Guam Standard"},
            new string[] {"HDT", "-0900", "Hawaii Daylight"},
            new string[] {"HST", "-1000", "Hawaii Standard"},
            new string[] {"IDLE", "+1200", "Internation Date Line East"},
            new string[] {"IDLW", "-1200", "Internation Date Line West"},
            new string[] {"IST", "+0530", "Indian Standard"},
            new string[] {"IT", "+0330", "Iran"},
            new string[] {"JST", "+0900", "Japan Standard"},
            new string[] {"JT", "+0700", "Java"},
            new string[] {"MDT", "-0600", "(US) Mountain Daylight"},
            new string[] {"MED", "+0200", "Middle European Daylight"},
            new string[] {"MET", "+0100", "Middle European"},
            new string[] {"MEST", "+0200", "Middle European Summer"},
            new string[] {"MEWT", "+0100", "Middle European Winter"},
            new string[] {"MST", "-0700", "(US) Mountain Standard"},
            new string[] {"MT", "+0800", "Moluccas"},
            new string[] {"NDT", "-0230", "Newfoundland Daylight"},
            new string[] {"NFT", "-0330", "Newfoundland"},
            new string[] {"NT", "-1100", "Nome"},
            new string[] {"NST", "+0630", "North Sumatra"},
            new string[] {"NZ", "+1100", "New Zealand "},
            new string[] {"NZST", "+1200", "New Zealand Standard"},
            new string[] {"NZDT", "+1300", "New Zealand Daylight"},
            new string[] {"NZT", "+1200", "New Zealand"},
            new string[] {"PDT", "-0700", "(US) Pacific Daylight"},
            new string[] {"PST", "-0800", "(US) Pacific Standard"},
            new string[] {"ROK", "+0900", "Republic of Korea"},
            new string[] {"SAD", "+1000", "South Australia Daylight"},
            new string[] {"SAST", "+0900", "South Australia Standard"},
            new string[] {"SAT", "+0900", "South Australia Standard"},
            new string[] {"SDT", "+1000", "South Australia Daylight"},
            new string[] {"SST", "+0200", "Swedish Summer"},
            new string[] {"SWT", "+0100", "Swedish Winter"},
            new string[] {"USZ3", "+0400", "USSR Zone 3"},
            new string[] {"USZ4", "+0500", "USSR Zone 4"},
            new string[] {"USZ5", "+0600", "USSR Zone 5"},
            new string[] {"USZ6", "+0700", "USSR Zone 6"},
            new string[] {"UT", "-0000", "Universal Coordinated"},
            new string[] {"UTC", "-0000", "Universal Coordinated"},
            new string[] {"UZ10", "+1100", "USSR Zone 10"},
            new string[] {"WAT", "-0100", "West Africa"},
            new string[] {"WET", "-0000", "West European"},
            new string[] {"WST", "+0800", "West Australian Standard"},
            new string[] {"YDT", "-0800", "Yukon Daylight"},
            new string[] {"YST", "-0900", "Yukon Standard"},
            new string[] {"ZP4", "+0400", "USSR Zone 3"},
            new string[] {"ZP5", "+0500", "USSR Zone 4"},
            new string[] {"ZP6", "+0600", "USSR Zone 5"}
            };
        #endregion

        /// <summary>
        /// Converts a timezone string (EST) to an offset which can be used by DateTime
        /// </summary>
        /// <param name="timeZone">3 letter Timezone string</param>
        /// <returns>DateTime compatible timezone offset (e.g. +0500), "" if timezone not found</returns>
        public static string TimeZoneToOffset(string timeZone)
        {
            timeZone = timeZone.ToUpper().Trim();
            for (int i = 0; i < TimeZones.Length; i++)
            {
                if (((string)((string[])TimeZones.GetValue(i)).GetValue(0)) == timeZone)
                    return ((string)((string[])TimeZones.GetValue(i)).GetValue(1)); // Found it
            }
            
            return ""; // Default return current timezone
        }

        /// <summary>
        /// Converts the Unix (epoch) timestamps into a DateTime (UTC)
        /// Unix time, which is defined as the number of seconds since midnight (UTC) on 1st January 1970
        /// </summary>
        /// <param name="unixTimeSeconds">Unix timestamp (epoch seconds)</param>
        /// <returns>DateTime in UTC timezone</returns>
        public static DateTime FromUnixTime(long unixTimeSeconds, bool useLocalTimeZone = false)
        {
            DateTime epoch;
            if (useLocalTimeZone)
                epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            else
                epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            
            return epoch.AddSeconds(unixTimeSeconds);
        }

        /// <summary>
        /// Converts the DateTime into Unix (epoch) seconds timestamp.
        /// Unix time, which is defined as the number of seconds since midnight on 1st January 1970
        /// </summary>
        /// <param name="dateTime">DateTime to convert</param>
        /// <param name="convertUTC">True to convert the input DateTime into UTC format before calculating Unix time</param>
        /// <returns>Unix timestamps (epoch seconds)</returns>
        public static long ToUnixTime(DateTime dateTime, bool convertUTC = false)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            
            if (convertUTC)
                return Convert.ToInt64((dateTime.ToUniversalTime() - epoch).TotalSeconds);
            else
                return Convert.ToInt64((dateTime - epoch).TotalSeconds);
        }
    }
}
