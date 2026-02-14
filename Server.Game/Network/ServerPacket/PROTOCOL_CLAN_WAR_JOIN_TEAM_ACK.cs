// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CLAN_WAR_JOIN_TEAM_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Models;
using Server.Game.Data.Models;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CLAN_WAR_JOIN_TEAM_ACK : GameServerPacket
    {
        private readonly MatchModel Field0;
        private readonly uint Field1;

        public PROTOCOL_CLAN_WAR_JOIN_TEAM_ACK(uint A_1, MatchModel A_2 = null)
        {
            this.Field1 = A_1;
            this.Field0 = A_2;
        }

        public override void Write()
        {
            this.WriteH((short)6921);
            this.WriteD(this.Field1);
            if (this.Field1 != 0U)
                return;
            this.WriteH((short)this.Field0.MatchId);
            this.WriteH((ushort)this.Field0.GetServerInfo());
            this.WriteH((ushort)this.Field0.GetServerInfo());
            this.WriteC((byte)this.Field0.State);
            this.WriteC((byte)this.Field0.FriendId);
            this.WriteC((byte)this.Field0.Training);
            this.WriteC((byte)this.Field0.GetCountPlayers());
            this.WriteD(this.Field0.Leader);
            this.WriteC((byte)0);
            this.WriteD(this.Field0.Clan.Id);
            this.WriteC((byte)this.Field0.Clan.Rank);
            this.WriteD(this.Field0.Clan.Logo);
            this.WriteS(this.Field0.Clan.Name, 17);
            this.WriteT(this.Field0.Clan.Points);
            this.WriteC((byte)this.Field0.Clan.NameColor);
            for (int index = 0; index < this.Field0.Training; ++index)
            {
                SlotMatch slot = this.Field0.Slots[index];
                Account playerBySlot = this.Field0.GetPlayerBySlot(slot);
                if (playerBySlot != null)
                {
                    this.WriteC((byte)playerBySlot.Rank);
                    this.WriteS(playerBySlot.Nickname, 33);
                    this.WriteQ(playerBySlot.PlayerId);
                    this.WriteC((byte)slot.State);
                }
                else
                    this.WriteB(new byte[43]);
            }
        }
    }
}