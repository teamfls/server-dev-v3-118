// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CLAN_WAR_REGIST_MERCENARY_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Models;
using Server.Game.Data.Models;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CLAN_WAR_REGIST_MERCENARY_ACK : GameServerPacket
    {
        private readonly MatchModel Field0;

        public PROTOCOL_CLAN_WAR_REGIST_MERCENARY_ACK(MatchModel A_1) => this.Field0 = A_1;

        public override void Write()
        {
            this.WriteH((short)6939);
            this.WriteH((short)this.Field0.GetServerInfo());
            this.WriteC((byte)this.Field0.State);
            this.WriteC((byte)this.Field0.FriendId);
            this.WriteC((byte)this.Field0.Training);
            this.WriteC((byte)this.Field0.GetCountPlayers());
            this.WriteD(this.Field0.Leader);
            this.WriteC((byte)0);
            foreach (SlotMatch slot in this.Field0.Slots)
            {
                Account playerBySlot = this.Field0.GetPlayerBySlot(slot);
                if (playerBySlot != null)
                {
                    this.WriteC((byte)playerBySlot.Rank);
                    this.WriteS(playerBySlot.Nickname, 33);
                    this.WriteQ(slot.PlayerId);
                    this.WriteC((byte)slot.State);
                }
                else
                    this.WriteB(new byte[43]);
            }
        }
    }
}