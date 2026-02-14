// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.SynchronizeXML
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
    public class SynchronizeXML
    {
        public static List<Synchronize> Servers = new List<Synchronize>();

        
        public static void Load()
        {
            string str = "Data/Synchronize.xml";
            if (File.Exists(str))
                SynchronizeXML.StaticMethod0(str);
            else
                CLogger.Print("File not found: " + str, LoggerType.Warning);
        }

        public static void Reload()
        {
            SynchronizeXML.Servers.Clear();
            SynchronizeXML.Load();
        }

        public static Synchronize GetServer(int Port)
        {
            if (SynchronizeXML.Servers.Count == 0)
                return (Synchronize)null;
            try
            {
                lock (SynchronizeXML.Servers)
                {
                    foreach (Synchronize server in SynchronizeXML.Servers)
                    {
                        if (server.RemotePort == Port)
                            return server;
                    }
                    return (Synchronize)null;
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return (Synchronize)null;
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
                                XmlAttributeCollection attributes1 = xmlNode1.Attributes;
                                for (XmlNode xmlNode2 = xmlNode1.FirstChild; xmlNode2 != null; xmlNode2 = xmlNode2.NextSibling)
                                {
                                    if (xmlNode2.Name.Equals("Sync"))
                                    {
                                        XmlNamedNodeMap attributes2 = (XmlNamedNodeMap)xmlNode2.Attributes;
                                        Synchronize synchronize = new Synchronize(attributes2.GetNamedItem("Host").Value, int.Parse(attributes2.GetNamedItem("Port").Value))
                                        {
                                            RemotePort = int.Parse(attributes2.GetNamedItem("RemotePort").Value)
                                        };
                                        SynchronizeXML.Servers.Add(synchronize);
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