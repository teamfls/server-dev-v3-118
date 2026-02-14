// Decompiled with JetBrains decompiler
// Type: Server.Auth.Network.ServerPacket.PROTOCOL_BASE_GET_SYSTEM_INFO_ACK
// Assembly: Server.Auth, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: D2254E5E-B0BA-4DE9-9720-2DDECE3CD4EF
// Assembly location: C:\Users\home\Desktop\dll\Server.Auth-deobfuscated-Cleaned.dll

using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.Network;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using Server.Auth.Data.Models;
using Server.Auth.Data.XML;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_BASE_GET_SYSTEM_INFO_ACK : AuthServerPacket
    {
        private readonly ServerConfig Field0;
        private readonly List<SChannelModel> Field1;
        private readonly List<RankModel> Field2;
        private readonly EventPlaytimeModel Field3;
        private readonly string[] Field4;

        
        public PROTOCOL_BASE_GET_SYSTEM_INFO_ACK(ServerConfig A_1)
        {
            this.Field0 = A_1;
            if (A_1 != null)
            {
                this.Field1 = SChannelXML.Servers;
                this.Field2 = PlayerRankXML.Ranks;
                this.Field3 = EventPlaytimeJSON.GetRunningEvent();
            }
            this.Field4 = new string[2]
            {
      "ded9a5bc68c44c6b885ac376be4f08c6",
      "5c67549f9ea01f1c7429d2a6bb121844"
            };
        }

        public override void Write()
        {
            this.WriteH((short)2315);
            this.WriteH((short)0);
            this.WriteC((byte)this.Field4[0].Length);
            this.WriteS(this.Field4[0], this.Field4[0].Length);
            this.WriteC((byte)this.Field4[1].Length);
            this.WriteS(this.Field4[1], this.Field4[1].Length);
            this.WriteD(0);
            this.WriteD(16 /*0x10*/);
            this.WriteB(new byte[61]);
            this.WriteH((short)5);
            this.WriteH((short)120);
            this.WriteH((short)1026);
            this.WriteC((byte)0);
            this.WriteH((short)770);
            this.WriteC((byte)1);
            this.WriteH((short)200);
            this.WriteH((short)0);
            this.WriteD(50);
            this.WriteD(50);
            this.WriteC((byte)1);
            this.WriteH((short)1000);
            this.WriteC((byte)0);
            this.WriteD(153699);
            this.WriteC((byte)0);
            this.WriteC((byte)1);
            this.WriteB(new byte[373]);
            this.WriteC((byte)this.Field0.Showroom);
            this.WriteC((byte)5);
            this.WriteC((byte)4);
            this.WriteH((short)3500);
            this.WriteH((short)0);
            this.WriteH((short)1450);
            this.WriteH((short)0);
            this.WriteD(49);
            this.WriteD(1);
            this.WriteH((short)1793);
            this.WriteC((byte)1);
            this.WriteH((short)8483);
            this.WriteH((short)0);
            this.WriteB(new byte[52]);
            this.WriteH((short)2565);
            this.WriteB(new byte[229]);
            this.WriteB(this.Method2(this.Field3));
            this.WriteC(this.Field0.Missions ? (byte)1 : (byte)0);
            this.WriteH((ushort)MissionConfigXML.MissionPage1);
            this.WriteH((ushort)MissionConfigXML.MissionPage2);
            this.WriteH((ushort)this.SECURITY_KEY);
            this.WriteB(this.Method0(this.Field1));
            this.WriteC((byte)1);
            this.WriteC((byte)this.NATIONS);
            this.WriteC((byte)0);
            this.WriteH((short)this.Field0.ShopURL.Length);
            this.WriteS(this.Field0.ShopURL, this.Field0.ShopURL.Length);
            this.WriteB(this.Method1(this.Field2));
            this.WriteC((byte)0);
            this.WriteC((byte)6);
        }

        
        private byte[] Method0(List<SChannelModel> A_1)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)A_1.Count);
                foreach (SChannelModel schannelModel in A_1)
                {
                    syncServerPacket.WriteD(schannelModel.State ? 1 : 0);
                    syncServerPacket.WriteB(ComDiv.AddressBytes(schannelModel.Host));
                    syncServerPacket.WriteB(ComDiv.AddressBytes(schannelModel.Host));
                    syncServerPacket.WriteH(schannelModel.Port);
                    syncServerPacket.WriteC((byte)schannelModel.Type);
                    syncServerPacket.WriteH((ushort)schannelModel.MaxPlayers);
                    syncServerPacket.WriteD(schannelModel.LastPlayers);
                    if (schannelModel.Id == 0)
                    {
                        syncServerPacket.WriteB(Bitwise.HexStringToByteArray("01 01 01 01 01 01 01 01 01 01 0E 00 00 00 00"));
                    }
                    else
                    {
                        foreach (ChannelModel channel in ChannelsXML.GetChannels(schannelModel.Id))
                            syncServerPacket.WriteC((byte)channel.Type);
                        syncServerPacket.WriteC((byte)schannelModel.Type);
                        syncServerPacket.WriteC((byte)0);
                        syncServerPacket.WriteH((short)0);
                    }
                }
                return syncServerPacket.ToArray();
            }
        }

        private byte[] Method1(List<RankModel> A_1)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)A_1.Count);
                foreach (RankModel rankModel in A_1)
                {
                    syncServerPacket.WriteC((byte)rankModel.Id);
                    List<int> rewards = PlayerRankXML.GetRewards(rankModel.Id);
                    foreach (int GoodId in rewards)
                    {
                        GoodsItem good = ShopManager.GetGood(GoodId);
                        syncServerPacket.WriteD(good == null ? 0 : good.Id);
                    }
                    for (int count = rewards.Count; 4 - count > 0; ++count)
                        syncServerPacket.WriteD(0);
                }
                return syncServerPacket.ToArray();
            }
        }

        private byte[] Method2(EventPlaytimeModel A_1)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)3);
                if (A_1 != null && A_1.EventIsEnabled())
                {
                    syncServerPacket.WriteD(A_1.Minutes1 * 60);
                    syncServerPacket.WriteD(A_1.Minutes2 * 60);
                    syncServerPacket.WriteD(A_1.Minutes3 * 60);
                }
                else
                    syncServerPacket.WriteB(new byte[12]);
                return syncServerPacket.ToArray();
            }
        }
    }
}