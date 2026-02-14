// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.CompetitiveXML
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
    public class CompetitiveXML
    {
        public static List<CompetitiveRank> Ranks = new List<CompetitiveRank>();

        
        public static void Load()
        {
            string str = "Data/Competitions.xml";
            if (!File.Exists(str))
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            else
                CompetitiveXML.StaticMethod0(str);
            CLogger.Print($"Plugin Loaded: {CompetitiveXML.Ranks.Count} Competitive Ranks", LoggerType.Info);
        }

        public static void Reload()
        {
            CompetitiveXML.Ranks.Clear();
            CompetitiveXML.Load();
        }

        public static CompetitiveRank GetRank(int Level)
        {
            lock (CompetitiveXML.Ranks)
            {
                foreach (CompetitiveRank rank in CompetitiveXML.Ranks)
                {
                    if (rank.Id == Level)
                        return rank;
                }
                return (CompetitiveRank)null;
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
                                    if (xmlNode2.Name.Equals("Competitive"))
                                    {
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                                        CompetitiveRank competitiveRank = new CompetitiveRank()
                                        {
                                            Id = int.Parse(attributes.GetNamedItem("Id").Value),
                                            TourneyLevel = int.Parse(attributes.GetNamedItem("TourneyLevel").Value),
                                            Points = int.Parse(attributes.GetNamedItem("Points").Value),
                                            Name = attributes.GetNamedItem("Name").Value
                                        };
                                        CompetitiveXML.Ranks.Add(competitiveRank);
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