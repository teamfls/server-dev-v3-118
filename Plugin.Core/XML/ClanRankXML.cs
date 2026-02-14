// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.ClanRankXML
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
    public class ClanRankXML
    {
        private static List<RankModel> Field0 = new List<RankModel>();

        
        public static void Load()
        {
            string str = "Data/Ranks/Clan.xml";
            if (File.Exists(str))
                ClanRankXML.StaticMethod0(str);
            else
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            CLogger.Print($"Plugin Loaded: {ClanRankXML.Field0.Count} Clan Ranks", LoggerType.Info);
        }

        public static void Reload()
        {
            ClanRankXML.Field0.Clear();
            ClanRankXML.Load();
        }

        public static RankModel GetRank(int Id)
        {
            lock (ClanRankXML.Field0)
            {
                foreach (RankModel rank in ClanRankXML.Field0)
                {
                    if (rank.Id == Id)
                        return rank;
                }
                return (RankModel)null;
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
                                    if (xmlNode2.Name.Equals("Rank"))
                                    {
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                                        RankModel rankModel = new RankModel((int)byte.Parse(attributes.GetNamedItem("Id").Value))
                                        {
                                            Title = attributes.GetNamedItem("Title").Value,
                                            OnNextLevel = int.Parse(attributes.GetNamedItem("OnNextLevel").Value),
                                            OnGoldUp = 0,
                                            OnAllExp = int.Parse(attributes.GetNamedItem("OnAllExp").Value)
                                        };
                                        ClanRankXML.Field0.Add(rankModel);
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