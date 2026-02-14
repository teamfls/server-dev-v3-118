// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.RedeemCodeXML
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
    public class RedeemCodeXML
    {
        public static List<TicketModel> Tickets = new List<TicketModel>();

        
        public static void Load()
        {
            string str = "Data/RedeemCodes.xml";
            if (File.Exists(str))
                RedeemCodeXML.StaticMethod0(str);
            else
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            CLogger.Print($"Plugin Loaded: {RedeemCodeXML.Tickets.Count} Redeem Codes", LoggerType.Info);
        }

        public static void Reload()
        {
            RedeemCodeXML.Tickets.Clear();
            RedeemCodeXML.Load();
        }

        public static TicketModel GetTicket(string Token, TicketType Type)
        {
            lock (RedeemCodeXML.Tickets)
            {
                foreach (TicketModel ticket in RedeemCodeXML.Tickets)
                {
                    if (ticket.Token == Token && ticket.Type == Type)
                        return ticket;
                }
                return (TicketModel)null;
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
                                    if (A_0_1.Name.Equals("Ticket"))
                                    {
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)A_0_1.Attributes;
                                        TicketModel A_1 = new TicketModel()
                                        {
                                            Token = attributes.GetNamedItem("Token").Value,
                                            Type = ComDiv.ParseEnum<TicketType>(attributes.GetNamedItem("Type").Value),
                                            TicketCount = uint.Parse(attributes.GetNamedItem("Count").Value),
                                            PlayerRation = uint.Parse(attributes.GetNamedItem("PlayerRation").Value),
                                            Rewards = new List<int>()
                                        };
                                        if (A_1.Type == TicketType.VOUCHER)
                                        {
                                            A_1.GoldReward = int.Parse(attributes.GetNamedItem("GoldReward").Value);
                                            A_1.CashReward = int.Parse(attributes.GetNamedItem("CashReward").Value);
                                            A_1.TagsReward = int.Parse(attributes.GetNamedItem("TagsReward").Value);
                                        }
                                        if (A_1.Type == TicketType.COUPON)
                                            RedeemCodeXML.StaticMethod1(A_0_1, A_1);
                                        RedeemCodeXML.Tickets.Add(A_1);
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

        
        private static void StaticMethod1(XmlNode A_0, TicketModel A_1)
        {
            for (XmlNode xmlNode1 = A_0.FirstChild; xmlNode1 != null; xmlNode1 = xmlNode1.NextSibling)
            {
                if (xmlNode1.Name.Equals("Rewards"))
                {
                    for (XmlNode xmlNode2 = xmlNode1.FirstChild; xmlNode2 != null; xmlNode2 = xmlNode2.NextSibling)
                    {
                        if (xmlNode2.Name.Equals("Goods"))
                        {
                            XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                            A_1.Rewards.Add(int.Parse(attributes.GetNamedItem("Id").Value));
                        }
                    }
                }
            }
        }
    }
}