// Decompiled with JetBrains decompiler
// Type: Plugin.Core.XML.CouponEffectXML
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
    public static class CouponEffectXML
    {
        private static List<CouponFlag> Field0 = new List<CouponFlag>();

        
        public static void Load()
        {
            string str = "Data/CouponFlags.xml";
            if (File.Exists(str))
                CouponEffectXML.StaticMethod0(str);
            else
                CLogger.Print("File not found: " + str, LoggerType.Warning);
            CLogger.Print($"Plugin Loaded: {CouponEffectXML.Field0.Count} Coupon Effects", LoggerType.Info);
        }

        public static void Reload()
        {
            CouponEffectXML.Field0.Clear();
            CouponEffectXML.Load();
        }

        public static CouponFlag GetCouponEffect(int id)
        {
            lock (CouponEffectXML.Field0)
            {
                for (int index = 0; index < CouponEffectXML.Field0.Count; ++index)
                {
                    CouponFlag couponEffect = CouponEffectXML.Field0[index];
                    if (couponEffect.ItemId == id)
                        return couponEffect;
                }
                return (CouponFlag)null;
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
                                    if (xmlNode2.Name.Equals("Coupon"))
                                    {
                                        XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                                        CouponFlag couponFlag = new CouponFlag()
                                        {
                                            ItemId = int.Parse(attributes.GetNamedItem("ItemId").Value),
                                            EffectFlag = ComDiv.ParseEnum<CouponEffects>(attributes.GetNamedItem("EffectFlag").Value)
                                        };
                                        CouponEffectXML.Field0.Add(couponFlag);
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
        private static bool Method0(string A_1) => nameof(CouponEffectXML).Equals(A_1);
    }
}