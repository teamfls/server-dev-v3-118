// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_ROOM_CREATE_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Server.Game.Data.Models;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_ROOM_CREATE_ACK : GameServerPacket
    {
        private readonly RoomModel Field0;
        private readonly uint Field1;

        public PROTOCOL_ROOM_CREATE_ACK(uint A_1, RoomModel A_2)
        {
            this.Field1 = A_1;
            this.Field0 = A_2;
        }

        public override void Write()
        {
            this.WriteH((short)3593);
            this.WriteD(this.Field1 == 0U ? (uint)this.Field0.RoomId : this.Field1);
            if (this.Field1 != 0U)
                return;
            this.WriteD(this.Field0.RoomId);
            this.WriteU(this.Field0.Name, 46);
            this.WriteC((byte)this.Field0.MapId);
            this.WriteC((byte)this.Field0.Rule);
            this.WriteC((byte)this.Field0.Stage);
            this.WriteC((byte)this.Field0.RoomType);
            this.WriteC((byte)this.Field0.State);
            this.WriteC((byte)this.Field0.GetCountPlayers());
            this.WriteC((byte)this.Field0.GetSlotCount());
            this.WriteC((byte)this.Field0.Ping);
            this.WriteH((ushort)this.Field0.WeaponsFlag);
            this.WriteD(this.Field0.GetFlag());
            this.WriteH((short)0);
            this.WriteD(this.Field0.NewInt);
            this.WriteH((short)0);
            this.WriteU(this.Field0.LeaderName, 66);
            this.WriteD(this.Field0.KillTime);
            this.WriteC(this.Field0.Limit);
            this.WriteC(this.Field0.WatchRuleFlag);
            this.WriteH((ushort)this.Field0.BalanceType);
            this.WriteB(this.Field0.RandomMaps);
            this.WriteC(this.Field0.CountdownIG == 0 ? (byte)5 : (byte)5);
            this.WriteB(this.Field0.LeaderAddr);
            this.WriteC(this.Field0.KillCam);
            this.WriteH((short)0);
        }
    }
}