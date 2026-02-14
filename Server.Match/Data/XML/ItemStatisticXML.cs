// Decompiled with JetBrains decompiler
// Type: Server.Match.Data.XML.ItemStatisticXML
// Assembly: Server.Match, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: CE18A1E1-67C7-4FA9-8510-2DD553448D5A
// Assembly location: C:\Users\home\Desktop\dll\Server.Match-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Server.Match.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;


namespace Server.Match.Data.XML
{
    public class ItemStatisticXML
    {
        public static List<ItemsStatistic> Stats = new List<ItemsStatistic>();

        
        public static void Load()
        {
            string str = "Data/Match/ItemStatistics.xml";
            if (File.Exists(str))
                ItemStatisticXML.StaticMethod0(str);
            else
                CLogger.Print("File not found: " + str, LoggerType.Warning);
        }

        public static void Reload()
        {
            ItemStatisticXML.Stats.Clear();
            ItemStatisticXML.Load();
        }

        public static ItemsStatistic GetItemStats(int ItemId)
        {
            lock (ItemStatisticXML.Stats)
            {
                foreach (ItemsStatistic stat in ItemStatisticXML.Stats)
                {
                    if (stat.Id == ItemId)
                        return stat;
                }
                return (ItemsStatistic)null;
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
                                    if (xmlNode2.Name.Equals("Statistic"))
                                    {
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                                        ItemsStatistic itemsStatistic = new ItemsStatistic()
                                        {
                                            Id = int.Parse(attributes.GetNamedItem("Id").Value),
                                            Name = attributes.GetNamedItem("Name").Value,
                                            BulletLoaded = int.Parse(attributes.GetNamedItem("LoadedBullet").Value),
                                            BulletTotal = int.Parse(attributes.GetNamedItem("TotalBullet").Value),
                                            Damage = int.Parse(attributes.GetNamedItem("Damage").Value),
                                            FireDelay = float.Parse(attributes.GetNamedItem("FireDelay").Value),
                                            HelmetPenetrate = int.Parse(attributes.GetNamedItem("HelmetPenetrate").Value),
                                            Range = float.Parse(attributes.GetNamedItem("Range").Value)
                                        };
                                        ItemStatisticXML.Stats.Add(itemsStatistic);
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