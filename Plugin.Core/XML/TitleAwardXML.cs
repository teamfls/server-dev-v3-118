// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.TitleAwardXML
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Plugin.Core.XML
{
    public class TitleAwardXML
    {
        public static List<TitleAward> Awards = new List<TitleAward>();

        
        public static void Load()
        {
            string str = "Data/Titles/Rewards.xml";
            if (File.Exists(str))
                TitleAwardXML.StaticMethod0(str);
            else
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            CLogger.Print($"Plugin Loaded: {TitleAwardXML.Awards.Count} Title Awards", LoggerType.Info);
        }

        public static void Reload()
        {
            TitleAwardXML.Awards.Clear();
            TitleAwardXML.Load();
        }

        public static List<ItemsModel> GetAwards(int titleId)
        {
            List<ItemsModel> awards = new List<ItemsModel>();
            lock (TitleAwardXML.Awards)
            {
                foreach (TitleAward award in TitleAwardXML.Awards)
                {
                    if (award.Id == titleId)
                        awards.Add(award.Item);
                }
            }
            return awards;
        }

        public static bool Contains(int TitleId, int ItemId)
        {
            if (ItemId == 0)
                return false;
            foreach (TitleAward award in TitleAwardXML.Awards)
            {
                if (award.Id == TitleId && award.Item.Id == ItemId)
                    return true;
            }
            return false;
        }

        
        private static void StaticMethod0(string A_0)
        {
            XmlDocument xmlDocument = new XmlDocument();
            using (FileStream inStream = new FileStream(A_0, FileMode.Open))
            {
                if (inStream.Length > 0L)
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
                                    if (A_0_1.Name.Equals("Title"))
                                    {
                                        int A_1 = int.Parse(A_0_1.Attributes.GetNamedItem("Id").Value);
                                        TitleAwardXML.StaticMethod1(A_0_1, A_1);
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

        
        private static void StaticMethod1(XmlNode A_0, int A_1)
        {
            for (XmlNode xmlNode1 = A_0.FirstChild; xmlNode1 != null; xmlNode1 = xmlNode1.NextSibling)
            {
                if (xmlNode1.Name.Equals("Rewards"))
                {
                    for (XmlNode xmlNode2 = xmlNode1.FirstChild; xmlNode2 != null; xmlNode2 = xmlNode2.NextSibling)
                    {
                        if (xmlNode2.Name.Equals("Item"))
                        {
                            XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                            TitleAward titleAward = new TitleAward()
                            {
                                Id = A_1
                            };
                            if (titleAward != null)
                            {
                                int num = int.Parse(attributes.GetNamedItem("Id").Value);
                                ItemsModel itemsModel = new ItemsModel(num)
                                {
                                    Name = attributes.GetNamedItem("Name").Value,
                                    Count = uint.Parse(attributes.GetNamedItem("Count").Value),
                                    Equip = (ItemEquipType)int.Parse(attributes.GetNamedItem("Equip").Value)
                                };
                                if (itemsModel.Equip == ItemEquipType.Permanent)
                                    itemsModel.ObjectId = (long)ComDiv.ValidateStockId(num);
                                titleAward.Item = itemsModel;
                                TitleAwardXML.Awards.Add(titleAward);
                            }
                        }
                    }
                }
            }
        }

        private bool Method0(string A_1) => this.ToString().Equals(A_1);
    }
}