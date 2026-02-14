// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CLAN_WAR_CHANGE_MAX_PER_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Server.Game.Data.Models;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CLAN_WAR_CHANGE_MAX_PER_ACK : GameServerPacket
    {
        public readonly MatchModel match;
        public readonly Account Player;

        public PROTOCOL_CLAN_WAR_CHANGE_MAX_PER_ACK(MatchModel A_1, Account A_2)
        {
            this.match = A_1;
            this.Player = A_2;
        }

        public override void Write()
        {
            this.WriteH((short)6927);
            this.WriteH((short)this.match.MatchId);
            this.WriteH((ushort)this.match.GetServerInfo());
            this.WriteH((ushort)this.match.GetServerInfo());
            this.WriteC((byte)this.match.State);
            this.WriteC((byte)this.match.FriendId);
            this.WriteC((byte)this.match.Training);
            this.WriteC((byte)this.match.GetCountPlayers());
            this.WriteD(this.match.Leader);
            this.WriteC((byte)0);
            this.WriteD(this.match.Clan.Id);
            this.WriteC((byte)this.match.Clan.Rank);
            this.WriteD(this.match.Clan.Logo);
            this.WriteS(this.match.Clan.Name, 17);
            this.WriteT(this.match.Clan.Points);
            this.WriteC((byte)this.match.Clan.NameColor);
            if (this.Player == null)
            {
                this.WriteB(new byte[43]);
            }
            else
            {
                this.WriteC((byte)this.Player.Rank);
                this.WriteS(this.Player.Nickname, 33);
                this.WriteQ(this.Player.PlayerId);
                this.WriteC((byte)this.match.Slots[this.match.Leader].State);
            }
        }
    }
}