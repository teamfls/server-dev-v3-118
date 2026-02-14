// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.TemplatePackXML
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
    public class TemplatePackXML
    {
        public static List<ItemsModel> Basics = new List<ItemsModel>();
        public static List<ItemsModel> Awards = new List<ItemsModel>();
        public static List<PCCafeModel> Cafes = new List<PCCafeModel>();

        public static void Load()
        {
            TemplatePackXML.StaticMethod0();
            TemplatePackXML.StaticMethod1();
            TemplatePackXML.StaticMethod2();
        }

        public static void Reload()
        {
            TemplatePackXML.Basics.Clear();
            TemplatePackXML.Awards.Clear();
            TemplatePackXML.Cafes.Clear();
            TemplatePackXML.Load();
        }

        
        private static void StaticMethod0()
        {
            string str = "Data/Temps/Basic.xml";
            if (!File.Exists(str))
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            else
                TemplatePackXML.StaticMethod3(str);
            CLogger.Print($"Plugin Loaded: {TemplatePackXML.Basics.Count} Basic Templates", LoggerType.Info);
        }

        
        private static void StaticMethod1()
        {
            string str = "Data/Temps/CafePC.xml";
            if (File.Exists(str))
                TemplatePackXML.StaticMethod4(str);
            else
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            CLogger.Print($"Plugin Loaded: {TemplatePackXML.Cafes.Count} PC Cafes", LoggerType.Info);
        }

        
        private static void StaticMethod2()
        {
            string str = "Data/Temps/Award.xml";
            if (File.Exists(str))
                TemplatePackXML.StaticMethod7(str);
            else
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            CLogger.Print($"Plugin Loaded: {TemplatePackXML.Awards.Count} Award Templates", LoggerType.Info);
        }

        public static PCCafeModel GetPCCafe(CafeEnum Type)
        {
            lock (TemplatePackXML.Cafes)
            {
                foreach (PCCafeModel cafe in TemplatePackXML.Cafes)
                {
                    if (cafe.Type == Type)
                        return cafe;
                }
                return (PCCafeModel)null;
            }
        }

        public static List<ItemsModel> GetPCCafeRewards(CafeEnum Type)
        {
            PCCafeModel pcCafe = TemplatePackXML.GetPCCafe(Type);
            if (pcCafe != null)
            {
                lock (pcCafe.Rewards)
                {
                    List<ItemsModel> pcCafeRewards;
                    if (pcCafe.Rewards.TryGetValue(Type, out pcCafeRewards))
                        return pcCafeRewards;
                }
            }
            return new List<ItemsModel>();
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
                                    if (xmlNode2.Name.Equals("Item"))
                                    {
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                                        int num = int.Parse(attributes.GetNamedItem("Id").Value);
                                        ItemsModel itemsModel = new ItemsModel(num)
                                        {
                                            ObjectId = (long)ComDiv.ValidateStockId(num),
                                            Name = attributes.GetNamedItem("Name").Value,
                                            Count = 1,
                                            Equip = ItemEquipType.Permanent
                                        };
                                        TemplatePackXML.Basics.Add(itemsModel);
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
                        for (XmlNode xmlNode = xmlDocument.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
                        {
                            if (xmlNode.Name.Equals("List"))
                            {
                                for (XmlNode A_0_1 = xmlNode.FirstChild; A_0_1 != null; A_0_1 = A_0_1.NextSibling)
                                {
                                    if (A_0_1.Name.Equals("Cafe"))
                                    {
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)A_0_1.Attributes;
                                        PCCafeModel A_1 = new PCCafeModel(ComDiv.ParseEnum<CafeEnum>(attributes.GetNamedItem("Type").Value))
                                        {
                                            ExpUp = int.Parse(attributes.GetNamedItem("ExpUp").Value),
                                            PointUp = int.Parse(attributes.GetNamedItem("PointUp").Value),
                                            Rewards = new SortedList<CafeEnum, List<ItemsModel>>()
                                        };
                                        TemplatePackXML.StaticMethod5(A_0_1, A_1);
                                        TemplatePackXML.Cafes.Add(A_1);
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

        
        private static void StaticMethod5(XmlNode A_0, PCCafeModel A_1)
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
                            int num = int.Parse(attributes.GetNamedItem("Id").Value);
                            ItemsModel A_1_1 = new ItemsModel(num)
                            {
                                ObjectId = (long)ComDiv.ValidateStockId(num),
                                Name = attributes.GetNamedItem("Name").Value,
                                Count = 1,
                                Equip = ItemEquipType.CafePC
                            };
                            TemplatePackXML.StaticMethod6(A_1, A_1_1);
                        }
                    }
                }
            }
        }

        private static void StaticMethod6(PCCafeModel A_0, ItemsModel A_1)
        {
            lock (A_0.Rewards)
            {
                if (A_0.Rewards.ContainsKey(A_0.Type))
                    A_0.Rewards[A_0.Type].Add(A_1);
                else
                    A_0.Rewards.Add(A_0.Type, new List<ItemsModel>()
        {
          A_1
        });
            }
        }

        
        private static void StaticMethod7(string A_0)
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
                                    if (xmlNode2.Name.Equals("Item"))
                                    {
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                                        ItemsModel itemsModel = new ItemsModel(int.Parse(attributes.GetNamedItem("Id").Value))
                                        {
                                            Name = attributes.GetNamedItem("Name").Value,
                                            Count = uint.Parse(attributes.GetNamedItem("Count").Value),
                                            Equip = ItemEquipType.Durable
                                        };
                                        TemplatePackXML.Awards.Add(itemsModel);
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

        private bool Method0(string A_1) => this.ToString().Equals(A_1);
    }
}