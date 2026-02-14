// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.PlayerRankXML
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
    public class PlayerRankXML
    {
        public static readonly List<RankModel> Ranks = new List<RankModel>();

        
        public static void Load()
        {
            string str = "Data/Ranks/Player.xml";
            if (!File.Exists(str))
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            else
                PlayerRankXML.StaticMethod0(str);
            CLogger.Print($"Plugin Loaded: {PlayerRankXML.Ranks.Count} Player Ranks", LoggerType.Info);
        }

        public static void Reload()
        {
            PlayerRankXML.Ranks.Clear();
            PlayerRankXML.Load();
        }

        public static RankModel GetRank(int Id)
        {
            lock (PlayerRankXML.Ranks)
            {
                foreach (RankModel rank in PlayerRankXML.Ranks)
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
                        for (XmlNode xmlNode = xmlDocument.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
                        {
                            if (xmlNode.Name.Equals("List"))
                            {
                                for (XmlNode A_0_1 = xmlNode.FirstChild; A_0_1 != null; A_0_1 = A_0_1.NextSibling)
                                {
                                    if (A_0_1.Name.Equals("Rank"))
                                    {
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)A_0_1.Attributes;
                                        RankModel A_1 = new RankModel(int.Parse(attributes.GetNamedItem("Id").Value))
                                        {
                                            Title = attributes.GetNamedItem("Title").Value,
                                            OnNextLevel = int.Parse(attributes.GetNamedItem("OnNextLevel").Value),
                                            OnGoldUp = int.Parse(attributes.GetNamedItem("OnGoldUp").Value),
                                            OnAllExp = int.Parse(attributes.GetNamedItem("OnAllExp").Value),
                                            Rewards = new SortedList<int, List<int>>()
                                        };
                                        PlayerRankXML.StaticMethod1(A_0_1, A_1);
                                        PlayerRankXML.Ranks.Add(A_1);
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

        public static List<int> GetRewards(int RankId)
        {
            RankModel rank = PlayerRankXML.GetRank(RankId);
            if (rank != null)
            {
                lock (rank.Rewards)
                {
                    List<int> rewards;
                    if (rank.Rewards.TryGetValue(RankId, out rewards))
                        return rewards;
                }
            }
            return new List<int>();
        }

        
        private static void StaticMethod1(XmlNode A_0, RankModel A_1)
        {
            for (XmlNode xmlNode1 = A_0.FirstChild; xmlNode1 != null; xmlNode1 = xmlNode1.NextSibling)
            {
                if (xmlNode1.Name.Equals("Rewards"))
                {
                    for (XmlNode xmlNode2 = xmlNode1.FirstChild; xmlNode2 != null; xmlNode2 = xmlNode2.NextSibling)
                    {
                        if (xmlNode2.Name.Equals("Good"))
                        {
                            int num = int.Parse(xmlNode2.Attributes.GetNamedItem("Id").Value);
                            lock (A_1.Rewards)
                            {
                                if (A_1.Rewards.ContainsKey(A_1.Id))
                                    A_1.Rewards[A_1.Id].Add(num);
                                else
                                    A_1.Rewards.Add(A_1.Id, new List<int>()
                {
                  num
                });
                            }
                        }
                    }
                }
            }
        }
    }
}