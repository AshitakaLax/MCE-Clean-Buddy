using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.IO;

namespace MCEBuddy.Util
{
    public static class XML
    {
        private static List<KeyValuePair<string, string>> specialXML = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("&quot;", "\""),
            new KeyValuePair<string, string>("&apos;", "'"),
            new KeyValuePair<string, string>("&lt;", "<"),
            new KeyValuePair<string, string>("&gt;", ">"),
            new KeyValuePair<string, string>("&amp;", "&"),
        };

        /// <summary>
        /// Encodes the 5 special characters in a XML string
        /// </summary>
        /// <param name="xmlString">Text string</param>
        /// <returns>XML encoded string</returns>
        public static string EncodeXMLSpecialChars(string textString)
        {
            if (String.IsNullOrWhiteSpace(textString))
                return textString;

            foreach (KeyValuePair<string, string> entry in specialXML)
            {
                textString = textString.Replace(entry.Value, entry.Key);
            }

            return textString;
        }

        /// <summary>
        /// Decodes the 5 special characters in a XML string
        /// </summary>
        /// <param name="xmlString">XML encoded string</param>
        /// <returns>Text string</returns>
        public static string DecodeXMLSpecialChars(string xmlString)
        {
            if (String.IsNullOrWhiteSpace(xmlString))
                return xmlString;

            foreach (KeyValuePair<string, string> entry in specialXML)
            {
                xmlString = xmlString.Replace(entry.Key, entry.Value);
            }

            return xmlString;
        }

        /// <summary>
        /// Gets Attribute value from a Tag from a XML stream
        /// </summary>
        /// <param name="Tag">Tag which contains the attribute</param>
        /// <param name="Attribute">Attribute from which to read the value</param>
        /// <param name="Source">XML Stream</param>
        /// <returns>Value of the Attribute</returns>
        public static string GetXMLTagAttributeValue(string Tag, string Attribute, string Source)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(Source);
                XmlNode xmlRootNode = xmlDoc.FirstChild;
                XmlNode xmlNode = xmlRootNode.SelectSingleNode(Tag);

                if (xmlNode != null)
                    return DecodeXMLSpecialChars(xmlNode.Attributes[Attribute].Value);
                else
                    return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Gets Attribute value from a Tag from a XML stream
        /// </summary>
        /// <param name="Tag">Tag which contains the SubTag</param>
        /// <param name="SubTag">SubTag which contains the attribute</param>
        /// <param name="Attribute">Attribute from which to read the value</param>
        /// <param name="Source">XML Stream</param>
        /// <returns>Value of the Attribute</returns>
        public static string GetXMLTagAttributeValue(string Tag, string SubTag, string Attribute, string Source)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(Source);
                XmlNode xmlRootNode = xmlDoc.FirstChild;
                XmlNode xmlNode = xmlRootNode.SelectSingleNode(Tag);

                if (xmlNode != null)
                    if (xmlNode[SubTag] != null)
                        return DecodeXMLSpecialChars(xmlNode[SubTag].Attributes[Attribute].Value);

                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Gets Attribute value from a Tag from a XML stream
        /// </summary>
        /// <param name="Tag">Tag which contains the SubTag</param>
        /// <param name="SubTag">SubTag which contains the child SubTag</param>
        /// <param name="SubTag2">Child SubTag which contains the attribute</param>
        /// <param name="Attribute">Attribute from which to read the value</param>
        /// <param name="Source">XML Stream</param>
        /// <returns>Value of the Attribute</returns>
        public static string GetXMLTagAttributeValue(string Tag, string SubTag, string SubTag2, string Attribute, string Source)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(Source);
                XmlNode xmlRootNode = xmlDoc.FirstChild;
                XmlNode xmlNode = xmlRootNode.SelectSingleNode(Tag);

                if (xmlNode != null)
                    if (xmlNode[SubTag] != null)
                        if (xmlNode[SubTag][SubTag2] != null)
                            return DecodeXMLSpecialChars(xmlNode[SubTag][SubTag2].Attributes[Attribute].Value);

                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Gets the value of a Tag from a XML stream
        /// </summary>
        /// <param name="Tag">Tag from which to read the value</param>
        /// <param name="Source">XML Stream</param>
        /// <returns>Value of the Tag</returns>
        public static string GetXMLTagValue(string Tag, string Source)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(Source);
                XmlNode xmlRootNode = xmlDoc.FirstChild;
                XmlNode xmlNode = xmlRootNode.SelectSingleNode(Tag);

                if (xmlNode != null)
                    return DecodeXMLSpecialChars(xmlNode.InnerText);
                else
                    return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Gets first the Level 2 SubTag values of a Tag from a XML stream
        /// </summary>
        /// <param name="Tag">Tag</param>
        /// <param name="SubTag">SubTag in Tag from which to read the value</param>
        /// <param name="Source">XML Stream</param>
        /// <returns>Value of the SubTag</returns>
        public static string GetXMLTagValue(string Tag, string SubTag, string Source)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(Source);
                XmlNode xmlRootNode = xmlDoc.FirstChild;
                XmlNode xmlNode = xmlRootNode.SelectSingleNode(Tag);

                if (xmlNode != null)
                    if (xmlNode[SubTag] != null)
                        return DecodeXMLSpecialChars(xmlNode[SubTag].InnerText);

                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Gets the first Level 3 SubTag values of a Tag from a XML stream
        /// </summary>
        /// <param name="Tag">Tag</param>
        /// <param name="SubTag">SubTag in Tag</param>
        /// <param name="SubTag2">SubTag in SubTag from which to read the value</param>
        /// <param name="Source">XML Stream</param>
        /// <returns>Array of Values of the SubTag</returns>
        public static string GetXMLTagValue(string Tag, string SubTag, string SubTag2, string Source)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(Source);
                XmlNode xmlRootNode = xmlDoc.FirstChild;
                XmlNode xmlNode = xmlRootNode.SelectSingleNode(Tag);

                if (xmlNode != null)
                    if (xmlNode[SubTag] != null)
                        if (xmlNode[SubTag][SubTag2] != null)
                            return DecodeXMLSpecialChars(xmlNode[SubTag][SubTag2].InnerText);

                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Gets the value of a Tags (multiple) from a XML stream
        /// </summary>
        /// <param name="Tag">Tag from which to read the value</param>
        /// <param name="Source">XML Stream</param>
        /// <returns>Array of values of the Tag, null if empty</returns>
        public static string[] GetXMLTagValues(string Tag, string Source)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(Source);
                XmlNode xmlRootNode = xmlDoc.FirstChild;
                XmlNodeList xmlNodes = xmlRootNode.SelectNodes(Tag);

                List<string> retVal = new List<string>();
                if (xmlNodes != null)
                {
                    foreach (XmlNode node in xmlNodes)
                    {
                        retVal.Add(DecodeXMLSpecialChars(node.InnerText));
                    }

                    if (retVal.Count > 0)
                        return retVal.ToArray();
                    else
                        return null;
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets all the Level 2 SubTags values of a Tag from a XML stream (assuming all SubTags have the same name)
        /// </summary>
        /// <param name="Tag">Tag</param>
        /// <param name="SubTag">SubTag in Tag from which to read the values</param>
        /// <param name="Source">XML Stream</param>
        /// <returns>Array of Values of the SubTag</returns>
        public static string[] GetXMLTagValues(string Tag, string SubTag, string Source)
        {
            try
            {
                using (var reader = new StringReader(Source))
                using (var Tree = XmlReader.Create(reader))
                {
                    List<string> retVal = new List<string>();

                    while (Tree.Read())
                    {
                        if ((Tree.NodeType == XmlNodeType.Element) && (Tree.Name == Tag))
                        {
                            XmlReader subTree = Tree.ReadSubtree();
                            while (subTree.Read())
                            {
                                if ((subTree.NodeType == XmlNodeType.Element) && (subTree.Name == SubTag))
                                {
                                    string val = subTree.ReadElementContentAsString();
                                    if (!String.IsNullOrEmpty(val))
                                        retVal.Add(DecodeXMLSpecialChars(val));
                                }
                            }
                        }
                    }

                    if (retVal.Count == 0)
                        return null;
                    else
                        return retVal.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets all the Level 3 SubTag values of a Tag from a XML stream (assuming all SubTags have the same name)
        /// </summary>
        /// <param name="Tag">Tag</param>
        /// <param name="SubTag">SubTag in Tag</param>
        /// <param name="SubTag2">SubTag in SubTag from which to read the values</param>
        /// <param name="Source">XML Stream</param>
        /// <returns>Array of Values of the SubTag</returns>
        public static string[] GetXMLTagValues(string Tag, string SubTag, string SubTag2, string Source)
        {
            try
            {
                using (var reader = new StringReader(Source))
                using (var Tree = XmlReader.Create(reader))
                {
                    List<string> retVal = new List<string>();

                    while (Tree.Read())
                    {
                        if ((Tree.NodeType == XmlNodeType.Element) && (Tree.Name == Tag))
                        {
                            XmlReader subTree = Tree.ReadSubtree();
                            while (subTree.Read())
                            {
                                if ((subTree.NodeType == XmlNodeType.Element) && (subTree.Name == SubTag))
                                {
                                    XmlReader subTree2 = subTree.ReadSubtree();
                                    while (subTree2.Read())
                                    {
                                        if ((subTree2.NodeType == XmlNodeType.Element) && (subTree2.Name == SubTag2))
                                        {
                                            string val = subTree.ReadElementContentAsString();
                                            if (!String.IsNullOrEmpty(val))
                                                retVal.Add(DecodeXMLSpecialChars(val));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (retVal.Count == 0)
                        return null;
                    else
                        return retVal.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
