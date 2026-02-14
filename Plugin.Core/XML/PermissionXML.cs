// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.PermissionXML
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using Plugin.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;


namespace Plugin.Core.XML
{
    public class PermissionXML
    {
        private static readonly SortedList<int, string> Field0 = new SortedList<int, string>();
        private static readonly SortedList<AccessLevel, List<string>> Field1 = new SortedList<AccessLevel, List<string>>();
        private static readonly SortedList<int, int> Field2 = new SortedList<int, int>();

        public static void Load()
        {
            PermissionXML.StaticMethod0();
            PermissionXML.StaticMethod1();
            PermissionXML.StaticMethod2();
        }

        public static void Reload()
        {
            PermissionXML.Field0.Clear();
            PermissionXML.Field1.Clear();
            PermissionXML.Field2.Clear();
            PermissionXML.Load();
        }

        
        private static void StaticMethod0()
        {
            string str = "Data/Access/Permission.xml";
            if (!File.Exists(str))
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            else
                PermissionXML.StaticMethod3(str);
            CLogger.Print($"Plugin Loaded: {PermissionXML.Field0.Count} Permissions", LoggerType.Info);
        }

        
        private static void StaticMethod1()
        {
            string str = "Data/Access/PermissionLevel.xml";
            if (!File.Exists(str))
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            else
                PermissionXML.StaticMethod4(str);
            CLogger.Print($"Plugin Loaded: {PermissionXML.Field2.Count} Permission Ranks", LoggerType.Info);
        }

        
        private static void StaticMethod2()
        {
            string str = "Data/Access/PermissionRight.xml";
            if (!File.Exists(str))
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            else
                PermissionXML.StaticMethod5(str);
            CLogger.Print($"Plugin Loaded: {PermissionXML.Field1.Count} Level Permission", LoggerType.Info);
        }

        public static int GetFakeRank(int Level)
        {
            lock (PermissionXML.Field2)
                return !PermissionXML.Field2.ContainsKey(Level) ? -1 : PermissionXML.Field2[Level];
        }

        public static bool HavePermission(string Permission, AccessLevel Level)
        {
            return PermissionXML.Field1.ContainsKey(Level) && PermissionXML.Field1[Level].Contains(Permission);
        }

        
        private static void StaticMethod3(string A_0)
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
                                    if (xmlNode2.Name.Equals("Permission"))
                                    {
                                        XmlAttributeCollection attributes = xmlNode2.Attributes;
                                        int key = int.Parse(attributes.GetNamedItem("Key").Value);
                                        string str1 = attributes.GetNamedItem("Name").Value;
                                        string str2 = attributes.GetNamedItem("Description").Value;
                                        if (!PermissionXML.Field0.ContainsKey(key))
                                            PermissionXML.Field0.Add(key, str1);
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

        
        private static void StaticMethod4(string A_0)
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
                                    if (xmlNode2.Name.Equals("Permission"))
                                    {
                                        XmlAttributeCollection attributes = xmlNode2.Attributes;
                                        int key = int.Parse(attributes.GetNamedItem("Key").Value);
                                        string str1 = attributes.GetNamedItem("Name").Value;
                                        string str2 = attributes.GetNamedItem("Description").Value;
                                        int num = int.Parse(attributes.GetNamedItem("FakeRank").Value);
                                        PermissionXML.Field2.Add(key, num);
                                        PermissionXML.Field1.Add((AccessLevel)key, new List<string>());
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

        
        private static void StaticMethod5(string A_0)
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
                        for (XmlNode xmlNode = xmlDocument.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
                        {
                            if (xmlNode.Name.Equals("List"))
                            {
                                for (XmlNode A_0_1 = xmlNode.FirstChild; A_0_1 != null; A_0_1 = A_0_1.NextSibling)
                                {
                                    if (A_0_1.Name.Equals("Access"))
                                    {
                                        AccessLevel A_1 = ComDiv.ParseEnum<AccessLevel>(A_0_1.Attributes.GetNamedItem("Level").Value);
                                        PermissionXML.StaticMethod6(A_0_1, A_1);
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

        
        private static void StaticMethod6(XmlNode A_0, AccessLevel A_1)
        {
            for (XmlNode xmlNode1 = A_0.FirstChild; xmlNode1 != null; xmlNode1 = xmlNode1.NextSibling)
            {
                if (xmlNode1.Name.Equals("Permission"))
                {
                    for (XmlNode xmlNode2 = xmlNode1.FirstChild; xmlNode2 != null; xmlNode2 = xmlNode2.NextSibling)
                    {
                        if (xmlNode2.Name.Equals("Right"))
                        {
                            int key = int.Parse(xmlNode2.Attributes.GetNamedItem("LevelKey").Value);
                            if (PermissionXML.Field0.ContainsKey(key))
                                PermissionXML.Field1[A_1].Add(PermissionXML.Field0[key]);
                        }
                    }
                }
            }
        }
    }
}