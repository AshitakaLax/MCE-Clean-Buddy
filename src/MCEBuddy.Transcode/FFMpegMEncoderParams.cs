using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCEBuddy.Transcode
{
    public class FFMpegMEncoderParams
    {
        protected volatile string _cmdParams = "";
        bool _caseSensitive;

        /// <summary>
        /// Get the whole command line parameter
        /// </summary>
        public string WholeCommandParameter
        { 
            get { return _cmdParams; }
        }

        /// <summary>
        /// This class is used to build and modify a command line used by ffmpeg and mencoder
        /// </summary>
        /// <param name="caseSensitive">True if the command parameter is case sensitive</param>
        public FFMpegMEncoderParams(bool caseSensitive = true)
        {
            _caseSensitive = caseSensitive;
        }

        /// <summary>
        /// This class is used to build and modify a command line used by ffmpeg and mencoder
        /// </summary>
        /// <param name="InitializeCommandParameter">Initial set of parameters to start with</param>
        /// <param name="caseSensitive">True if the command parameter is case sensitive</param>
        public FFMpegMEncoderParams(string InitializeCommandParameter, bool caseSensitive = true)
        {
            _caseSensitive = caseSensitive;
            _cmdParams = InitializeCommandParameter;
        }

        /// <summary>
        /// Appends a string of preformatted parameters to the end of the conversion command line as is
        /// </summary>
        /// <param name="parameter">String of preformatted parameters</param>
        public void AppendParameters(string parameter)
        {
            _cmdParams = (_cmdParams + " " + parameter).Trim();
        }

        /// <summary>
        /// Return the start position and length of the value of the parameter, relative to the entire command line
        /// e.g. -b 150k -> -b is the parameter and 150k is the value
        /// </summary>
        /// <param name="cmd">Parameter</param>
        /// <param name="start">Returns start position (relative to beginning of command line) of value of parameter</param>
        /// <param name="length">Returns length (no of characters) of value of sub parameter</param>
        /// <returns>True if found the Parameter</returns>
        private bool ParameterValueStartEnd(string cmd, out int start, out int length)
        {
            start = -1;
            length = -1;

            if (String.IsNullOrWhiteSpace(cmd))
                return false;

            cmd = cmd + " ";

            int idx = -1;
            if (!_caseSensitive)
                idx = _cmdParams.ToLower().IndexOf(cmd.ToLower());
            else
                idx = _cmdParams.IndexOf(cmd);

            // Find the start of the parameter
            if (idx < 0) return false;
            idx = idx + cmd.Length;
            while (idx < _cmdParams.Length)
            {
                if (!char.IsWhiteSpace(_cmdParams[idx])) break;
                idx++;
            }

            // Find the end of the parameter
            int endidx = -1;
            bool inQuotes = false;
            for (int i = idx; i < _cmdParams.Length; i++)
            {
                if ((char.IsWhiteSpace(_cmdParams[i])) && (!inQuotes))
                {
                    endidx = i;
                    break;
                }
                else if (_cmdParams[i] == '\"')
                {
                    inQuotes = !inQuotes;
                }
            }

            if (!inQuotes)
            {
                // Found a valid parameter
                if (endidx == -1) endidx = _cmdParams.Length;
                start = idx;
                length = endidx - idx;
                return (length > 0);
            }
            return false;
        }

        /// <summary>
        /// Return the start position and length of the value of the sub parameter in the parameter, relative to the entire command line
        /// e.g. -vf scale=1024x768,hqdn3d -> parameter is -vf, sub parameters are scale and hqdn3d and the returned values are "1024x768" and "" (without the quotes)
        /// e.g. -x264opts subq=1:ref -> parameter is -x264opts, sub parameters are subq and ref and returned values are "1" and "" (without the quotes)
        /// </summary>
        /// <param name="cmd">Parameter</param>
        /// <param name="subCmd">Sub Parameter</param>
        /// <param name="start">Returns start position (relative to beginning of command line) of value of sub parameter</param>
        /// <param name="length">Returns length (no of characters) of the value of sub parameter, if there is no = in the sub parameter, it returns 0</param>
        /// <param name="equalTo">Return true if there is an equal to in the value of the sub parameter</param>
        /// <returns>True if found the Parameter and Sub Parameter</returns>
        private bool ParameterSubValueStartEnd(string cmd, string subCmd, out int start, out int length, out bool equalTo)
        {
            start = -1;
            length = -1;
            equalTo = false;

            if (String.IsNullOrWhiteSpace(cmd) || String.IsNullOrWhiteSpace(subCmd))
                return false;

            int paramStart = -1;
            int paramLength = -1;
            string param = "";

            // Get the parameter value and the start/length 
            if (ParameterValueStartEnd(cmd, out paramStart, out paramLength))
                param = _cmdParams.Substring(paramStart, paramLength);
            else
                return false;

            if (!_caseSensitive)
                subCmd = subCmd.ToLower();

            // Fix for mencoder -vf as it varies from everything else
            string[] subParams;
            if (cmd == "-vf")
                subParams = param.Split(',');
            else
                subParams = param.Split(':');

            string subStr = "";
            foreach (string s in subParams)
            {
                string s2 = "";

                if (_caseSensitive)
                    s2 = s;
                else
                    s2 = s.ToLower();

                if (s2.Contains("="))
                    s2 = s2.Substring(0, s2.IndexOf("="));

                if (s2 == subCmd)
                {
                    subStr = s;
                    break;
                }
            }

            if (subStr != "")
            {
                start = paramStart + param.IndexOf(subStr);
                if (subStr.Contains("="))
                {
                    start += subStr.IndexOf("=") + 1;
                    length = subStr.Length - subStr.IndexOf("=") - 1;
                    equalTo = true;
                }
                else
                {
                    start += subCmd.Length;
                    length = 0;
                }

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Return the value of a parameter from the conversion command line
        /// e.g. -b 150k -> -b is the parameter and 150k is the value
        /// </summary>
        /// <param name="cmd">Parameter identifier who's value to find</param>
        /// <returns>Paramter value if found else ""</returns>
        public string ParameterValue(string cmd)
        {
            int start, length;

            if (ParameterValueStartEnd(cmd, out start, out length))
                return _cmdParams.Substring(start, length);
            else
                return "";
        }

        /// <summary>
        /// Replaces the value of a parameter in the conversion command line
        /// e.g. -b 150k -> -b is the parameter and 150k is the value
        /// </summary>
        /// <param name="cmd">Parameter identifier</param>
        /// <param name="newValue">Parameter value</param>
        /// <returns>True if parameter is found and replaced</returns>
        public bool ParameterValueReplace(string cmd, string newValue)
        {
            int start, length;

            if (ParameterValueStartEnd(cmd, out start, out length))
            {
                string newCmdLine = _cmdParams.Substring(0, start - 1).Trim();
                newCmdLine += " " + newValue;
                newCmdLine += " " + _cmdParams.Substring(start + length).Trim();
                _cmdParams = newCmdLine;
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Replaces a value of a parameter or inserts it if parameters is not found
        /// e.g. -b 150k -> -b is the parameter and 150k is the value
        /// </summary>
        /// <param name="cmd">Parameter identifier</param>
        /// <param name="newValue">Parameter value</param>
        /// <param name="beginning">(does not work if parameter exists, only for insert) True to insert the new parameter at the beginning, false to insert at the end</param>
        public void ParameterValueReplaceOrInsert(string cmd, string newValue, bool beginning = false)
        {
            ParameterValueReplaceOrInsert(cmd, newValue, "", beginning);
        }

        /// <summary>
        /// Replaces a value of a parameter or inserts it if parameters is not found
        /// e.g. -b 150k -> -b is the parameter and 150k is the value
        /// </summary>
        /// <param name="cmd">Parameter identifier</param>
        /// <param name="newValue">Parameter value</param>
        /// <param name="refExistingCmd">Insert the new sub parameter in reference to an existing sub parameter</param>
        /// <param name="before">(does not work if parameter exists, only for insert) True to insert the new parameter before the existing reference parameter, false to insert after. If <paramref name="refExistingCmd"/> is null of empty, it will be placed at the beginning of the chain if true or at the end if false.</param>
        public void ParameterValueReplaceOrInsert(string cmd, string newValue, string refExistingCmd, bool before = false)
        {
            if (!ParameterValueReplace(cmd, newValue))
            {
                int start, length;

                if (ParameterValueStartEnd(refExistingCmd, out start, out length)) // Does the reference parameter exist
                {
                    if (before) // Insert before ref sub param
                        _cmdParams = _cmdParams.Insert(start - refExistingCmd.Length - 1, cmd + " " + newValue + " ");
                    else // Insert after
                        _cmdParams = _cmdParams.Insert(start + length, " " + cmd + " " + newValue);
                }
                else if (before)
                    _cmdParams = cmd + " " + newValue + " " + _cmdParams; // Beginning of the chain
                else
                    _cmdParams += " " + cmd + " " + newValue; // Default end of chain
            }
        }

        /// <summary>
        /// Deletes a parameter from the conversion command line if found (DO NOT USE FOR PARAMETERS WITHOUT VALUES, e.g. --decomb)
        /// e.g. -b 150k -> -b is the parameter and 150k is the value
        /// </summary>
        /// <param name="cmd">Parameter to delete</param>
        /// <returns>True if the parameter is found and deleted, false if not found</returns>
        public bool ParameterValueDelete(string cmd)
        {
            if (ParameterValueReplace(cmd, "")) // First remove the value
            {
                int start, length;
                ParameterValueStartEnd(cmd, out start, out length); // Get the starting point of the end of the cmd
                _cmdParams = _cmdParams.Remove(start - cmd.Length - 2, cmd.Length + 1).Trim(); // remove the cmd and space

                return true; // We found and deleted the parameter
            }
            else
                return false; // We didn't find it
        }

        /// <summary>
        /// Returns the value of a sub parameter within a parameter 
        /// e.g. -vf scale=1024x768,hqdn3d -> parameter is -vf, sub parameters are scale and hqdn3d and the returned values are "1024x768" and "" (without the quotes)
        /// e.g. -x264opts subq=1:ref -> parameter is -x264opts, sub parameters are subq and ref and the returned values are "1" and "" (without the quotes)
        /// </summary>
        /// <param name="cmd">Parameter identifier</param>
        /// <param name="subCmd">Sub-parameter identifier</param>
        /// <returns>Value of sub parameter (without =) if found, "" if sub parameter exists but has no value and null if the sub parameter does not exit</returns>
        public string ParameterSubValue(string cmd, string subCmd)
        {
            int start, length;
            bool eq;

            if (ParameterSubValueStartEnd(cmd, subCmd, out start, out length, out eq))
                return _cmdParams.Substring(start, length);
            else
                return null;
        }

        /// <summary>
        /// Replaces the value of a sub parameter within a parameter 
        /// e.g. -vf scale=1024x768,hqdn3d -> parameter is -vf, sub parameters are scale and hqdn3d and the set values are "=1024x768" and "" (without the quotes)
        /// e.g. -x264opts subq=1:ref -> parameter is -x264opts, sub parameters are subq and ref and the set values are "=1" and "" (without the quotes)
        /// </summary>
        /// <param name="cmd">Parameter identifier</param>
        /// <param name="subCmd">Sub-parameter identifier</param>
        /// <param name="newValue">New value of the sub parameter to replace (include = if required)</param>
        /// <returns>True if sub parameter is found and value replaced (including = if required)</returns>
        public bool ParameterSubValueReplace(string cmd, string subCmd, string newValue)
        {
            int start, length;
            bool eq;

            if (ParameterSubValueStartEnd(cmd, subCmd, out start, out length, out eq))
            {
                string newCmdLine = "";
                if (eq)
                    if (!String.IsNullOrEmpty(newValue))
                        newValue = newValue.Replace("=", ""); // Don't duplicate the =

                newCmdLine = _cmdParams.Substring(0, start);
                newCmdLine += newValue;
                newCmdLine += _cmdParams.Substring(start + length);
                _cmdParams = newCmdLine;

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Replaces the value of a sub parameter within a parameter or inserts it if not found
        /// e.g. -vf scale=1024x768,hqdn3d -> parameter is -vf, sub parameters are scale and hqdn3d and the set values are "=1024x768" and "" (without the quotes)
        /// e.g. -x264opts subq=1:ref -> parameter is -x264opts, sub parameters are subq and ref and the set values are "=1" and "" (without the quotes)
        /// </summary>
        /// <param name="cmd">Parameter identifier</param>
        /// <param name="subCmd">Sub-parameter identifier</param>
        /// <param name="newValue">New value of the sub parameter to replace (include = if required)</param>
        /// <param name="beginning">(does not work if sub parameter exists, only for insert) True to insert the new sub parameter at the beginning, false to insert at the end</param>
        public void ParameterSubValueReplaceOrInsert(string cmd, string subCmd, string newValue, bool beginning = false)
        {
            ParameterSubValueReplaceOrInsert(cmd, subCmd, newValue, "", beginning);
        }

        /// <summary>
        /// Replaces the value of a sub parameter within a parameter or inserts it if not found
        /// e.g. -vf scale=1024x768,hqdn3d -> parameter is -vf, sub parameters are scale and hqdn3d and the set values are "=1024x768" and "" (without the quotes)
        /// e.g. -x264opts subq=1:ref -> parameter is -x264opts, sub parameters are subq and ref and the set values are "=1" and "" (without the quotes)
        /// </summary>
        /// <param name="cmd">Parameter identifier</param>
        /// <param name="subCmd">Sub-parameter identifier</param>
        /// <param name="newValue">New value of the sub parameter to replace (include = if required)</param>
        /// <param name="refExistingSubCmd">Insert the new sub parameter in reference to an existing sub parameter</param>
        /// <param name="before">(does not work if sub parameter exists, only for insert) True to insert the new sub parameter before the existing reference sub parameter, false to insert after. If <paramref name="refExistingSubCmd"/> is null of empty, it will be placed at the beginning of the chain if true or at the end if false.</param>
        public void ParameterSubValueReplaceOrInsert(string cmd, string subCmd, string newValue, string refExistingSubCmd, bool before = false)
        {
            if (!ParameterSubValueReplace(cmd, subCmd, newValue))
            {
                string paramValue, separator;

                // Fix for mencoder -vf and -af as it varies from everything else
                if (cmd == "-vf" || cmd == "-af")
                    separator = ",";
                else
                    separator = ":";

                if (!String.IsNullOrWhiteSpace(ParameterValue(cmd))) // avoid the last comma if this is a new key
                {
                    int start, length;
                    bool eq;

                    if (ParameterSubValueStartEnd(cmd, refExistingSubCmd, out start, out length, out eq)) // Does the reference sub parameter exist
                    {
                        if (before) // Insert before ref sub param
                            _cmdParams = _cmdParams.Insert(start - refExistingSubCmd.Length - (eq ? 1 : 0), subCmd + newValue + separator);
                        else // Insert after
                            _cmdParams = _cmdParams.Insert(start + length, separator + subCmd + newValue);

                        return; // We are done here
                    }
                    else if (before) // place the beginning
                        paramValue = subCmd + newValue + separator + ParameterValue(cmd);
                    else // place at the end
                        paramValue = ParameterValue(cmd) + separator + subCmd + newValue;
                }
                else
                    paramValue = subCmd + newValue;

                ParameterValueReplaceOrInsert(cmd, paramValue);
            }
        }

        /// <summary>
        /// Deletes a sub-parameter within the parameter from the conversion command line if found
        /// e.g. -vf scale=1024x768,hqdn3d -> parameter is -vf, sub parameters are scale and hqdn3d
        /// e.g. -x264opts subq=1:ref -> parameter is -x264opts, sub parameters are subq and ref
        /// </summary>
        /// <param name="cmd">Parameter identifier</param>
        /// <param name="subCmd">Sub-parameter to delete</param>
        /// <returns>True if the sub parameter is found and deleted, false if not found</returns>
        public bool ParameterSubValueDelete(string cmd, string subCmd)
        {
            int start, length;
            bool eq;

            // Before replacing first check if this is the last sub parameter in the parameter (i.e. there will be no , or : before or after the sub parameter)
            if (ParameterSubValueStartEnd(cmd, subCmd, out start, out length, out eq)) // If the sub parameter exists
            {
                _cmdParams += " "; // Put a space at the end since if the parameter is at the end of the command line then the next section checking for , or : past will cause n past end of array exception - no harm done

                bool lastSubParam;
                if (eq) // If the subvalue contains an = or not, we calculate the position of the , and : accordingly
                    lastSubParam = (!((_cmdParams[start + length] == ':') || (_cmdParams[start + length] == ',') || (_cmdParams[start - subCmd.Length - 2] == ':') || (_cmdParams[start - subCmd.Length - 2] == ','))); // If there isn't a leading or trailing : or , then this is the last sub parameter
                else
                    lastSubParam = (!((_cmdParams[start] == ':') || (_cmdParams[start] == ',') || (_cmdParams[start - subCmd.Length - 1] == ':') || (_cmdParams[start - subCmd.Length - 1] == ','))); // If there isn't a leading or trailing : or , then this is the last sub parameter

                if (lastSubParam) // Is this is the last sub parameter, delete the entire parameter (including sub since this is the last)
                {
                    ParameterValueDelete(cmd);
                    return true;
                }
                else if (ParameterSubValueReplace(cmd, subCmd, "")) // First remove the value
                {
                    // Be careful don't use .Replace, since special cases like '-volume=18.4 -x264opt me=hex:intra=3' when removing subCmd 'me', it will also remove it from volume if you use .Replace
                    if (eq) // = in the sub parameter
                    {
                        // Delete the subParam
                        _cmdParams = _cmdParams.Remove(start - subCmd.Length - 1, subCmd.Length + 1).Trim(); // remove the cmd and =

                        if ((_cmdParams[start - subCmd.Length - 1] == ':') || (_cmdParams[start - subCmd.Length - 1] == ',')) // Check for trailing : or , (if we are the beginning or the middle)
                            _cmdParams = _cmdParams.Remove(start - subCmd.Length - 1, 1); // get rid of it
                        else if ((_cmdParams[start - subCmd.Length - 2] == ':') || (_cmdParams[start - subCmd.Length - 2] == ',')) // Otherwise check for a leading : or , (we are the last parameter in the filter chain)
                            _cmdParams = _cmdParams.Remove(start - subCmd.Length - 2, 1); // get rid of it
                    }
                    else // no = in the sub parameter
                    {
                        // Delete the subParam
                        _cmdParams = _cmdParams.Remove(start - subCmd.Length, subCmd.Length).Trim(); // remove the cmd

                        if ((_cmdParams[start - subCmd.Length] == ':') || (_cmdParams[start - subCmd.Length] == ',')) // Check for trailing : or , (if we are the beginning or the middle)
                            _cmdParams = _cmdParams.Remove(start - subCmd.Length, 1); // get rid of it
                        else if ((_cmdParams[start - subCmd.Length - 1] == ':') || (_cmdParams[start - subCmd.Length - 1] == ',')) // Otherwise check for a leading : or , (we are the last parameter in the filter chain)
                            _cmdParams = _cmdParams.Remove(start - subCmd.Length - 1, 1); // get rid of it
                    }

                    return true; // We found and deleted the parameter
                }
            }

            return false;
        }

        /// <summary>
        /// Deletes a video filter from the conversion command parameters (within -vf) (applies to ffmpeg and mencoder)
        /// e.g. -vf scale=1024x768,hqdn3d -> parameter is -vf, sub parameters are scale and hqdn3d
        /// </summary>
        /// <param name="subCmd">Video sub parameter to delete</param>
        /// <returns>True if the sub parameter is found and deleted, false if not found</returns>
        public bool ParameterDeleteVideoFilter(string subCmd)
        {
            return ParameterSubValueDelete("-vf", subCmd);
        }

        /// <summary>
        /// Adds a video filter to the conversion command parameters (within -vf) (applies to ffmpeg and mencoder)
        /// This function preserves and specific filter chain requirements for video filters (e.g. crop always comes before scale)
        /// e.g. -vf scale=1024x768,hqdn3d -> parameter is -vf, sub parameters are scale and hqdn3d and the set values are "=1024x768" and "" (without the quotes)
        /// </summary>
        /// <param name="subCmd">Video sub parameter</param>
        /// <param name="value">Sub parameter value (including = if required)</param>
        public void ParameterReplaceOrInsertVideoFilter(string subCmd, string value)
        {
            // First try replacing an existing filter
            if (ParameterSubValueReplace("-vf", subCmd, value))
                return;

            string vfParams = ParameterValue("-vf");

            // Check if we have a special order to manage
            switch (subCmd)
            {
                // COMMON FILTERS (FFMPEG + MENCODER)
                case "scale": // scale always comes after crop or at the end
                    if (vfParams.Contains("scale"))
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, "crop", false);
                    else
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, false); // default at the end
                    break;

                case "crop": // crop always comes before scale or at the end
                    if (vfParams.Contains("crop"))
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, "scale", true);
                    else
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, false); // default at the end
                    break;

                case "yadif": // yadif comes after decimate/softskip and after fieldmatch/pullup or at the beginning if nothing
                    if (vfParams.Contains("decimate"))
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, "decimate", false);
                    else if (vfParams.Contains("fieldmatch"))
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, "fieldmatch", false);
                    if (vfParams.Contains("softskip"))
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, "softskip", false);
                    else if (vfParams.Contains("fieldmatch"))
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, "fieldmatch", false);
                    else
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, true); // Otherwise at the beginning of the filter chain
                    break;

                // FFMPEG FILTERS - "http://www.ffmpeg.org/ffmpeg-all.html#Video-Filters"
                case "fieldmatch": // fieldmatch always is the first filter in the video chain
                    ParameterSubValueReplaceOrInsert("-vf", subCmd, value, true);
                    break;

                case "decimate": // decimate comes after fieldmatch and before yadif or at the beginning if nothing
                    if (vfParams.Contains("fieldmatch"))
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, "fieldmatch", false);
                    else if (vfParams.Contains("yadif"))
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, "yadif", true);
                    else
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, true); // Otherwise at the beginning of the filter chain
                    break;

                // MENCODER FILTERS "http://www.mplayerhq.hu/DOCS/man/en/mplayer.1.html#VIDEO FILTERS"
                case "harddup": // harddup always is the last filter in the video chain
                    ParameterSubValueReplaceOrInsert("-vf", subCmd, value, false);
                    break;

                case "pullup": // pullup always is the first filter in the video chain
                    ParameterSubValueReplaceOrInsert("-vf", subCmd, value, true);
                    break;

                case "softskip": // softskip comes after pullup and before yadif or at the beginning if nothing
                    if (vfParams.Contains("pullup"))
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, "pullup", false);
                    else if (vfParams.Contains("yadif"))
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, "yadif", true);
                    else
                        ParameterSubValueReplaceOrInsert("-vf", subCmd, value, true); // Otherwise at the beginning of the filter chain
                    break;

                default:
                    ParameterSubValueReplaceOrInsert("-vf", subCmd, value); // Default at the end of the filter chain
                    break;
            }
        }

        /// <summary>
        /// Deletes an audio filter from the conversion command parameters (within -af) (applies to ffmpeg and mencoder)
        /// e.g. -af volume=0.5,anull -> audio parameter is af, sub parameters are volume and anull
        /// </summary>
        /// <param name="subCmd">Audio sub parameter to delete</param>
        /// <returns>True if the sub parameter is found and deleted, false if not found</returns>
        public bool ParameterDeleteAudioFilter(string subCmd)
        {
            return ParameterSubValueDelete("-af", subCmd);
        }

        /// <summary>
        /// Adds a audio filter to the conversion command parameters (within -af) (applies to ffmpeg and mencoder)
        /// This function preserves and specific filter chain requirements for audio filters
        /// e.g. -af volume=0.5,anull -> audio parameter is af, sub parameters are volume and anull and the set values are "=0.5" and "" (without the quotes)
        /// </summary>
        /// <param name="subCmd">Audio sub parameter</param>
        /// <param name="value">Sub parameter value (including = if required)</param>
        public void ParameterReplaceOrInsertAudioFilter(string subCmd, string value)
        {
            // First try replacing an existing filter
            if (ParameterSubValueReplace("-af", subCmd, value))
                return;

            string afParams = ParameterValue("-af");

            // TODO: Any special filter rules/toolchain rules for audio filters?
            // "http://www.ffmpeg.org/ffmpeg-all.html#Audio-Filters"
            // "http://www.mplayerhq.hu/DOCS/man/en/mplayer.1.html#AUDIO FILTERS"
            switch (subCmd)
            {
                default:
                    ParameterSubValueReplaceOrInsert("-af", subCmd, value); // By default at end of filter chain
                    break;
            }
        }
    }
}
