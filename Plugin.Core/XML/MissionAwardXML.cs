// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.MissionAwardXML
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;


namespace Plugin.Core.XML
{
    public class MissionAwardXML
    {
        private static List<MissionAwards> Field0 = new List<MissionAwards>();

        
        public static void Load()
        {
            string str = "Data/Cards/MissionAwards.xml";
            if (File.Exists(str))
                MissionAwardXML.StaticMethod0(str);
            else
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            CLogger.Print($"Plugin Loaded: {MissionAwardXML.Field0.Count} Mission Awards", LoggerType.Info);
        }

        public static void Reload()
        {
            MissionAwardXML.Field0.Clear();
            MissionAwardXML.Load();
        }

        public static MissionAwards GetAward(int MissionId)
        {
            lock (MissionAwardXML.Field0)
            {
                foreach (MissionAwards award in MissionAwardXML.Field0)
                {
                    if (award.Id == MissionId)
                        return award;
                }
                return (MissionAwards)null;
            }
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
                                    if (xmlNode2.Name.Equals("Mission"))
                                    {
                                        XmlAttributeCollection attributes = xmlNode2.Attributes;
                                        int A_1 = int.Parse(attributes.GetNamedItem("Id").Value);
                                        int A_2 = int.Parse(attributes.GetNamedItem("MasterMedal").Value);
                                        int A_3 = int.Parse(attributes.GetNamedItem("Exp").Value);
                                        int A_4 = int.Parse(attributes.GetNamedItem("Point").Value);
                                        MissionAwardXML.Field0.Add(new MissionAwards(A_1, A_2, A_3, A_4));
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