// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.EventRankUpXML
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
    public class EventRankUpXML
    {
        public static List<EventRankUpModel> Events = new List<EventRankUpModel>();


        public static void Load()
        {
            string str = "Data/Events/Rank.xml";
            if (!File.Exists(str))
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            else
                EventRankUpXML.StaticMethod0(str);
            CLogger.Print($"Plugin Loaded: {EventRankUpXML.Events.Count} Event Rank Up", LoggerType.Info);
        }

        public static void Reload()
        {
            EventRankUpXML.Events.Clear();
            EventRankUpXML.Load();
        }


        public static EventRankUpModel GetRunningEvent()
        {
            lock (EventRankUpXML.Events)
            {
                uint num = uint.Parse(DateTimeUtil.Now("yyMMddHHmm"));
                foreach (EventRankUpModel runningEvent in EventRankUpXML.Events)
                {
                    if (runningEvent.BeginDate <= num && num < runningEvent.EndedDate)
                        return runningEvent;
                }
                return (EventRankUpModel)null;
            }
        }

        public static EventRankUpModel GetEvent(int EventId)
        {
            lock (EventRankUpXML.Events)
            {
                foreach (EventRankUpModel eventRankUpModel in EventRankUpXML.Events)
                {
                    if (eventRankUpModel.Id == EventId)
                        return eventRankUpModel;
                }
                return (EventRankUpModel)null;
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
                                    if (A_0_1.Name.Equals("Event"))
                                    {
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)A_0_1.Attributes;
                                        EventRankUpModel A_1 = new EventRankUpModel()
                                        {
                                            Id = int.Parse(attributes.GetNamedItem("Id").Value),
                                            BeginDate = uint.Parse(attributes.GetNamedItem("Begin").Value),
                                            EndedDate = uint.Parse(attributes.GetNamedItem("Ended").Value),
                                            Name = attributes.GetNamedItem("Name").Value,
                                            Description = attributes.GetNamedItem("Description").Value,
                                            Period = bool.Parse(attributes.GetNamedItem("Period").Value),
                                            Priority = bool.Parse(attributes.GetNamedItem("Priority").Value),
                                            Ranks = new List<int[]>()
                                        };
                                        EventRankUpXML.StaticMethod1(A_0_1, A_1);
                                        EventRankUpXML.Events.Add(A_1);
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


        private static void StaticMethod1(XmlNode A_0, EventRankUpModel A_1)
        {
            for (XmlNode xmlNode1 = A_0.FirstChild; xmlNode1 != null; xmlNode1 = xmlNode1.NextSibling)
            {
                if (xmlNode1.Name.Equals("Rewards"))
                {
                    for (XmlNode xmlNode2 = xmlNode1.FirstChild; xmlNode2 != null; xmlNode2 = xmlNode2.NextSibling)
                    {
                        if (xmlNode2.Name.Equals("Rank"))
                        {
                            XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                            int[] numArray = new int[4]
                            {
                                int.Parse(attributes.GetNamedItem("UpId").Value),
                                int.Parse(attributes.GetNamedItem("BonusExp").Value),
                                int.Parse(attributes.GetNamedItem("BonusPoint").Value),
                                int.Parse(attributes.GetNamedItem("Percent").Value)
                            };
                            A_1.Ranks.Add(numArray);
                        }
                    }
                }
            }
        }
    }
}