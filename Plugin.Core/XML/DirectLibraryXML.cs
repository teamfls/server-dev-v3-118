// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.DirectLibraryXML
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Plugin.Core.XML
{
    public class DirectLibraryXML
    {
        public static List<string> HashFiles = new List<string>();

        
        public static void Load()
        {
            string str = "Data/DirectLibrary.xml";
            if (File.Exists(str))
                DirectLibraryXML.StaticMethod0(str);
            else
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            CLogger.Print($"Plugin Loaded: {DirectLibraryXML.HashFiles.Count} Lib Hases", LoggerType.Info);
        }

        public static void Reload()
        {
            DirectLibraryXML.HashFiles.Clear();
            DirectLibraryXML.Load();
        }

        public static bool IsValid(string md5)
        {
            if (string.IsNullOrEmpty(md5))
                return true;
            for (int index = 0; index < DirectLibraryXML.HashFiles.Count; ++index)
            {
                if (DirectLibraryXML.HashFiles[index] == md5)
                    return true;
            }
            return false;
        }

        
        private static void StaticMethod0(string A_0)
        {
            XmlDocument xmlDocument = new XmlDocument();
            using (FileStream inStream = new FileStream(A_0, FileMode.Open))
            {
                if (inStream.Length == 0L)
                {
                    CLogger.Print("File is empty: " + A_0, LoggerType.Warning);
                }
                else
                {
                    try
                    {
                        xmlDocument.Load((Stream)inStream);
                        for (XmlNode xmlNode1 = xmlDocument.FirstChild; xmlNode1 != null; xmlNode1 = xmlNode1.NextSibling)
                        {
                            if (xmlNode1.Name.Equals("List"))
                            {
                                for (XmlNode xmlNode2 = xmlNode1.FirstChild; xmlNode2 != null; xmlNode2 = xmlNode2.NextSibling)
                                {
                                    if (xmlNode2.Name.Equals("D3D9"))
                                    {
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                                        DirectLibraryXML.HashFiles.Add(attributes.GetNamedItem("MD5").Value);
                                    }
                                }
                            }
                        }
                    }
                    catch (XmlException ex)
                    {
                        CLogger.Print(ex.Message, LoggerType.Error, (Exception)ex);
                    }
                }
                inStream.Dispose();
                inStream.Close();
            }
        }
    }
}
