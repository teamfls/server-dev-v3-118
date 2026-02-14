// Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// Plugin.Core.XML.RandomBoxXML
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.XML;

public class RandomBoxXML
{
    public static SortedList<int, RandomBoxModel> RBoxes = new SortedList<int, RandomBoxModel>();


    public static void Load()
    {
        DirectoryInfo a_ = StaticMethod5(StaticMethod4(StaticMethod3(), "\\Data\\RBoxes"));
        if (!StaticMethod6((FileSystemInfo)a_))
        {
            return;
        }
        FileInfo[] array = StaticMethod7(a_);
        foreach (FileInfo a_2 in array)
        {
            try
            {
                StaticMethod0(int.Parse(StaticMethod10(StaticMethod8((FileSystemInfo)a_2), 0, StaticMethod9(StaticMethod8((FileSystemInfo)a_2)) - 4)));
            }
            catch (Exception ex)
            {
                CLogger.Print(StaticMethod11(ex), LoggerType.Error, ex);
            }
        }
        CLogger.Print(StaticMethod12("Plugin Loaded: {0} Random Boxes", (object)RBoxes.Count), LoggerType.Info);
    }

    public static void Reload()
    {
        RBoxes.Clear();
        Load();
    }


    private static void StaticMethod0(int A_0)
    {
        string text = StaticMethod12("Data/RBoxes/{0}.xml", (object)A_0);
        if (StaticMethod13(text))
        {
            StaticMethod1(text, A_0);
        }
        else
        {
            CLogger.Print(StaticMethod4("File not found: ", text), LoggerType.Warning);
        }
    }


    private static void StaticMethod1(string A_0, int A_1)
    {
        XmlDocument a_ = StaticMethod14();
        FileStream fileStream = StaticMethod15(A_0, FileMode.Open);
        try
        {
            if (StaticMethod16((Stream)fileStream) == 0L)
            {
                CLogger.Print(StaticMethod4("File is empty: ", A_0), LoggerType.Warning);
            }
            else
            {
                try
                {
                    StaticMethod17(a_, (Stream)fileStream);
                    for (XmlNode xmlNode = StaticMethod18((XmlNode)a_); xmlNode != null; xmlNode = StaticMethod23(xmlNode))
                    {
                        if (Method0("List", StaticMethod19(xmlNode)))
                        {
                            for (XmlNode xmlNode2 = StaticMethod18(xmlNode); xmlNode2 != null; xmlNode2 = StaticMethod23(xmlNode2))
                            {
                                if (Method0("Item", StaticMethod19(xmlNode2)))
                                {
                                    XmlNamedNodeMap a_2 = StaticMethod20(xmlNode2);
                                    RandomBoxModel randomBoxModel = new RandomBoxModel
                                    {
                                        ItemsCount = int.Parse(StaticMethod22(StaticMethod21(a_2, "Count"))),
                                        Items = new List<RandomBoxItem>()
                                    };
                                    StaticMethod2(xmlNode2, randomBoxModel);
                                    randomBoxModel.SetTopPercent();
                                    RBoxes.Add(A_1, randomBoxModel);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    CLogger.Print(StaticMethod11(ex), LoggerType.Error, ex);
                }
            }
            StaticMethod24((Stream)fileStream);
            StaticMethod25((Stream)fileStream);
        }
        finally
        {
            if (fileStream != null)
            {
                StaticMethod26((IDisposable)fileStream);
            }
        }
    }


    private static void StaticMethod2(XmlNode A_0, RandomBoxModel A_1)
    {
        for (XmlNode xmlNode = StaticMethod18(A_0); xmlNode != null; xmlNode = StaticMethod23(xmlNode))
        {
            if (Method0("Rewards", StaticMethod19(xmlNode)))
            {
                for (XmlNode xmlNode2 = StaticMethod18(xmlNode); xmlNode2 != null; xmlNode2 = StaticMethod23(xmlNode2))
                {
                    if (Method0("Good", StaticMethod19(xmlNode2)))
                    {
                        XmlNamedNodeMap a_ = StaticMethod20(xmlNode2);
                        RandomBoxItem item = new RandomBoxItem
                        {
                            Index = int.Parse(StaticMethod22(StaticMethod21(a_, "Index"))),
                            GoodsId = int.Parse(StaticMethod22(StaticMethod21(a_, "Id"))),
                            Percent = int.Parse(StaticMethod22(StaticMethod21(a_, "Percent"))),
                            Special = bool.Parse(StaticMethod22(StaticMethod21(a_, "Special")))
                        };
                        A_1.Items.Add(item);
                    }
                }
            }
        }
    }

    public static bool ContainsBox(int Id)
    {
        return RBoxes.ContainsKey(Id);
    }

    public static RandomBoxModel GetBox(int Id)
    {
        try
        {
            return RBoxes[Id];
        }
        catch
        {
            return null;
        }
    }

    // helper method for string compare
    private static bool Method0(string expected, string actual)
    {
        return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
    }

    static string StaticMethod3()
    {
        return Directory.GetCurrentDirectory();
    }

    static string StaticMethod4(string A_0, string A_1)
    {
        return A_0 + A_1;
    }

    static DirectoryInfo StaticMethod5(string A_0)
    {
        return new DirectoryInfo(A_0);
    }

    static bool StaticMethod6(FileSystemInfo A_0)
    {
        return A_0.Exists;
    }

    static FileInfo[] StaticMethod7(DirectoryInfo A_0)
    {
        return A_0.GetFiles();
    }

    static string StaticMethod8(FileSystemInfo A_0)
    {
        return A_0.Name;
    }

    static int StaticMethod9(string A_0)
    {
        return A_0.Length;
    }

    static string StaticMethod10(string A_0, int A_1, int A_2)
    {
        return A_0.Substring(A_1, A_2);
    }

    static string StaticMethod11(Exception A_0)
    {
        return A_0.Message;
    }

    static string StaticMethod12(string A_0, object A_1)
    {
        return string.Format(A_0, A_1);
    }

    static bool StaticMethod13(string A_0)
    {
        return File.Exists(A_0);
    }

    static XmlDocument StaticMethod14()
    {
        return new XmlDocument();
    }

    static FileStream StaticMethod15(string A_0, FileMode A_1)
    {
        return new FileStream(A_0, A_1);
    }

    static long StaticMethod16(Stream A_0)
    {
        return A_0.Length;
    }

    static void StaticMethod17(XmlDocument A_0, Stream A_1)
    {
        A_0.Load(A_1);
    }

    static XmlNode StaticMethod18(XmlNode A_0)
    {
        return A_0.FirstChild;
    }

    static string StaticMethod19(XmlNode A_0)
    {
        return A_0.Name;
    }

    static XmlAttributeCollection StaticMethod20(XmlNode A_0)
    {
        return A_0.Attributes;
    }

    static XmlNode StaticMethod21(XmlNamedNodeMap A_0, string A_1)
    {
        return A_0.GetNamedItem(A_1);
    }

    static string StaticMethod22(XmlNode A_0)
    {
        return A_0.Value;
    }

    static XmlNode StaticMethod23(XmlNode A_0)
    {
        return A_0.NextSibling;
    }

    static void StaticMethod24(Stream A_0)
    {
        A_0.Dispose();
    }

    static void StaticMethod25(Stream A_0)
    {
        A_0.Close();
    }

    static void StaticMethod26(IDisposable A_0)
    {
        A_0.Dispose();
    }
}
