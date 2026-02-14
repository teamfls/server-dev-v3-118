// Decompiled with JetBrains decompiler
// Type: Plugin.Core.Managers.PortalManager
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Network;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Plugin.Core.Managers
{
    public static class PortalManager
    {
        public static SortedList<string, PortalEvents> AllEvents = new SortedList<string, PortalEvents>();


        public static void Load()
        {
            foreach (EventBoostModel eventBoostModel in EventBoostXML.Events)
            {
                if (eventBoostModel != null && eventBoostModel.EventIsEnabled())
                    PortalManager.AllEvents.Add($"Boost_{eventBoostModel.Id}", PortalEvents.BoostEvent);
            }
            foreach (EventRankUpModel eventRankUpModel in EventRankUpXML.Events)
            {
                if (eventRankUpModel != null && eventRankUpModel.EventIsEnabled())
                    PortalManager.AllEvents.Add($"RankUp_{eventRankUpModel.Id}", PortalEvents.RankUpEvent);
            }
            foreach (EventLoginModel eventLoginModel in EventLoginXML.Events)
            {
                if (eventLoginModel != null && eventLoginModel.EventIsEnabled())
                    PortalManager.AllEvents.Add($"Login_{eventLoginModel.Id}", PortalEvents.LoginEvent);
            }
            foreach (EventPlaytimeModel eventPlaytimeModel in EventPlaytimeJSON.Events)
            {
                if (eventPlaytimeModel != null && eventPlaytimeModel.EventIsEnabled())
                    PortalManager.AllEvents.Add($"Playtime_{eventPlaytimeModel.Id}", PortalEvents.PlaytimeEvent);
            }
            CLogger.Print($"Plugin Loaded: {PortalManager.AllEvents.Count} Listed Event Portal", LoggerType.Info);
        }


        public static int GetInitialId(string Input)
        {
            Match match = Regex.Match(Input, "\\d+");
            int result;
            return match.Success && int.TryParse(match.Value, out result) ? result : -1;
        }

        public static byte[] InitEventData(
          PortalEvents Portal,
          int Id,
          uint[] DateTime,
          string[] Info,
          byte[] Type,
          ushort Image)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)Portal);
                syncServerPacket.WriteD(Id);
                syncServerPacket.WriteC(Type[0]);
                syncServerPacket.WriteD(DateTime[0]);
                syncServerPacket.WriteD(DateTime[1]);
                syncServerPacket.WriteD(0);
                syncServerPacket.WriteC(Type[1]);
                syncServerPacket.WriteU(Info[0], 120);
                syncServerPacket.WriteU(Info[1], 200);
                syncServerPacket.WriteH(Image);
                return syncServerPacket.ToArray();
            }
        }

        public static byte[] InitRankUpData(EventRankUpModel RankUp)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)RankUp.Ranks.Count);
                foreach (int[] rank in RankUp.Ranks)
                {
                    syncServerPacket.WriteD(rank[0]);
                    syncServerPacket.WriteD(ComDiv.Percentage(rank[1], rank[3]));
                    syncServerPacket.WriteD(ComDiv.Percentage(rank[2], rank[3]));
                }
                return syncServerPacket.ToArray();
            }
        }

        public static byte[] InitPlaytimeData(EventPlaytimeModel Playtime)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteD(Playtime.Minutes1 * 60);
                syncServerPacket.WriteD(Playtime.Minutes2 * 60);
                syncServerPacket.WriteD(Playtime.Minutes3 * 60);
                foreach (int num in Playtime.Goods1)
                    syncServerPacket.WriteD(num);
                syncServerPacket.WriteB(new byte[(20 - Playtime.Goods1.Count) * 4]);
                foreach (int num in Playtime.Goods2)
                    syncServerPacket.WriteD(num);
                syncServerPacket.WriteB(new byte[(20 - Playtime.Goods2.Count) * 4]);
                foreach (int num in Playtime.Goods3)
                    syncServerPacket.WriteD(num);
                syncServerPacket.WriteB(new byte[(20 - Playtime.Goods3.Count) * 4]);
                return syncServerPacket.ToArray();
            }
        }

        public static byte[] InitBoostData(EventBoostModel Boost)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteH((ushort)Boost.BoostType);
                syncServerPacket.WriteD(Boost.BoostValue);
                syncServerPacket.WriteD(ComDiv.Percentage(Boost.BonusExp, Boost.Percent));
                syncServerPacket.WriteD(ComDiv.Percentage(Boost.BonusGold, Boost.Percent));
                return syncServerPacket.ToArray();
            }
        }

        public static byte[] InitLoginData(EventLoginModel Login)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)1);
                syncServerPacket.WriteC(Login.Goods.Count > 4 ? (byte)4 : (byte)Login.Goods.Count);
                for (int index = 0; index < 4; ++index)
                {
                    if (Login.Goods.Count < index + 1)
                        syncServerPacket.WriteD(0);
                    else
                        syncServerPacket.WriteD(Login.Goods[index]);
                }
                return syncServerPacket.ToArray();
            }
        }
    }
}