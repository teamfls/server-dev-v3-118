// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CLAN_WAR_CREATE_TEAM_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Server.Game.Data.Models;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CLAN_WAR_CREATE_TEAM_ACK : GameServerPacket
    {
        private readonly uint Field0;
        private readonly MatchModel Field1;

        public PROTOCOL_CLAN_WAR_CREATE_TEAM_ACK(uint A_1, MatchModel A_2 = null)
        {
            this.Field0 = A_1;
            this.Field1 = A_2;
        }

        public override void Write()
        {
            this.WriteH((short)6919);
            this.WriteD(this.Field0);
            if (this.Field0 != 0U)
                return;
            this.WriteH((short)this.Field1.MatchId);
            this.WriteH((short)this.Field1.GetServerInfo());
            this.WriteH((short)this.Field1.GetServerInfo());
            this.WriteC((byte)this.Field1.State);
            this.WriteC((byte)this.Field1.FriendId);
            this.WriteC((byte)this.Field1.Training);
            this.WriteC((byte)this.Field1.GetCountPlayers());
            this.WriteD(this.Field1.Leader);
            this.WriteC((byte)0);
        }
    }
}