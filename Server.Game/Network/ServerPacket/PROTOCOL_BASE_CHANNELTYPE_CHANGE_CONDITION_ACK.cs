// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_BASE_CHANNELTYPE_CHANGE_CONDITION_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Models;
using Plugin.Core.Network;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using System.Collections.Generic;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_BASE_CHANNELTYPE_CHANGE_CONDITION_ACK : GameServerPacket
    {
        public override void Write()
        {
            this.WriteH((short)2490);
            this.WriteB(this.Method0(SChannelXML.Servers));
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
                    syncServerPacket.WriteH(schannelModel.Port);
                    syncServerPacket.WriteC((byte)schannelModel.Type);
                    syncServerPacket.WriteH((ushort)schannelModel.MaxPlayers);
                    syncServerPacket.WriteD(schannelModel.LastPlayers);
                }
                syncServerPacket.WriteC((byte)0);
                return syncServerPacket.ToArray();
            }
        }
    }
}