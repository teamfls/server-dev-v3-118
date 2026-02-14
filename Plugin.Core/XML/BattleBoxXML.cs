using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Network;

namespace Plugin.Core.XML
{
    public class BattleBoxXML
    {
        public static List<BattleBoxModel> BBoxes = new List<BattleBoxModel>();
        public static List<ShopData> ShopDataBattleBoxes = new List<ShopData>();
        public static int TotalBoxes;

        public static void Load()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "BBoxes");
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
                return;

            foreach (FileInfo fileInfo in directoryInfo.GetFiles("*.xml"))
            {
                try
                {
                    int couponId = int.Parse(Path.GetFileNameWithoutExtension(fileInfo.Name));
                    LoadBattleBoxFile(couponId);
                }
                catch (Exception ex)
                {
                    CLogger.Print(ex.Message, LoggerType.Error, ex);
                }
            }

            InitializeShopData();
            CLogger.Print($"Plugin Loaded: {BBoxes.Count} Battle Boxes", LoggerType.Info);
        }

        public static void Reload()
        {
            BBoxes.Clear();
            ShopDataBattleBoxes.Clear();
            TotalBoxes = 0;
            Load();
        }

        private static void LoadBattleBoxFile(int couponId)
        {
            string filePath = $"Data/BBoxes/{couponId}.xml";
            if (!File.Exists(filePath))
            {
                CLogger.Print($"File not found: {filePath}", LoggerType.Warning);
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                using (var fs = new FileStream(filePath, FileMode.Open))
                {
                    if (fs.Length == 0)
                    {
                        CLogger.Print($"File is empty: {filePath}", LoggerType.Warning);
                        return;
                    }
                    doc.Load(fs);
                }

                foreach (XmlNode node in doc.SelectNodes("//List/BattleBox"))
                {
                    int requireTags = int.Parse(node.Attributes["RequireTags"].Value);
                    var battleBox = new BattleBoxModel
                    {
                        CouponId = couponId,
                        RequireTags = requireTags,
                        Items = new List<BattleBoxItem>()
                    };

                    foreach (XmlNode rewardNode in node.SelectNodes("Rewards/Good"))
                    {
                        var item = new BattleBoxItem
                        {
                            GoodsId = int.Parse(rewardNode.Attributes["Id"].Value),
                            Percent = int.Parse(rewardNode.Attributes["Percent"].Value)
                        };
                        battleBox.Items.Add(item);
                    }

                    battleBox.InitItemPercentages();
                    BBoxes.Add(battleBox);
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        private static void InitializeShopData()
        {
            List<BattleBoxModel> boxesCopy;
            lock (BBoxes)
            {
                boxesCopy = new List<BattleBoxModel>(BBoxes);
            }

            TotalBoxes = boxesCopy.Count;
            int pages = (int)Math.Ceiling(boxesCopy.Count / 100.0);

            for (int i = 0; i < pages; i++)
            {
                int itemsCount = 0;
                byte[] buffer = SerializeBattleBoxes(100, i, ref itemsCount, boxesCopy);

                ShopData shopData = new ShopData
                {
                    Buffer = buffer,
                    ItemsCount = itemsCount,
                    Offset = i * 100
                };

                ShopDataBattleBoxes.Add(shopData);
            }
        }

        private static byte[] SerializeBattleBoxes(int pageSize, int pageIndex, ref int itemsCount, List<BattleBoxModel> boxes)
        {
            itemsCount = 0;
            using (SyncServerPacket packet = new SyncServerPacket())
            {
                for (int i = pageIndex * pageSize; i < boxes.Count; i++)
                {
                    WriteBattleBoxData(boxes[i], packet);
                    itemsCount++;
                    if (itemsCount == pageSize)
                        break;
                }
                return packet.ToArray();
            }
        }

        private static void WriteBattleBoxData(BattleBoxModel box, SyncServerPacket packet)
        {
            packet.WriteD(box.CouponId);
            packet.WriteH((ushort)box.RequireTags);
            packet.WriteH(0);
            packet.WriteH(0);
            packet.WriteC(0);
        }

        public static BattleBoxModel GetBattleBox(int battleBoxId)
        {
            if (battleBoxId == 0) return null;

            lock (BBoxes)
            {
                foreach (var box in BBoxes)
                {
                    if (box.CouponId == battleBoxId)
                        return box;
                }
            }
            return null;
        }
    }
}
