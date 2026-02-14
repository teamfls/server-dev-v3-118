// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.MissionConfigXML
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
    public class MissionConfigXML
    {
        public static uint MissionPage1;
        public static uint MissionPage2;
        private static List<MissionStore> Field0 = new List<MissionStore>();

        
        public static void Load()
        {
            string str = "Data/MissionConfig.xml";
            if (File.Exists(str))
                MissionConfigXML.StaticMethod0(str);
            else
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            CLogger.Print($"Plugin Loaded: {MissionConfigXML.Field0.Count} Mission Stores", LoggerType.Info);
        }

        public static void Reload()
        {
            MissionConfigXML.MissionPage1 = 0U;
            MissionConfigXML.MissionPage2 = 0U;
            MissionConfigXML.Field0.Clear();
            MissionConfigXML.Load();
        }

        public static MissionStore GetMission(int MissionId)
        {
            lock (MissionConfigXML.Field0)
            {
                foreach (MissionStore mission in MissionConfigXML.Field0)
                {
                    if (mission.Id == MissionId)
                        return mission;
                }
                return (MissionStore)null;
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
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                                        MissionStore missionStore = new MissionStore()
                                        {
                                            Id = int.Parse(attributes.GetNamedItem("Id").Value),
                                            ItemId = int.Parse(attributes.GetNamedItem("ItemId").Value),
                                            Enable = bool.Parse(attributes.GetNamedItem("Enable").Value)
                                        };
                                        uint num1 = (uint)(1 << missionStore.Id);
                                        int num2 = (int)Math.Ceiling((double)missionStore.Id / 32.0);
                                        if (missionStore.Enable)
                                        {
                                            switch (num2)
                                            {
                                                case 1:
                                                    MissionConfigXML.MissionPage1 += num1;
                                                    break;
                                                case 2:
                                                    MissionConfigXML.MissionPage2 += num1;
                                                    break;
                                            }
                                        }
                                        MissionConfigXML.Field0.Add(missionStore);
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