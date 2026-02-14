// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CLAN_WAR_MATCH_TEAM_INFO_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Models;
using Plugin.Core.SQL;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CLAN_WAR_MATCH_TEAM_INFO_ACK : GameServerPacket
    {
        private readonly uint Field0;
        private readonly ClanModel Field1;
        private readonly Account Field2;
        private readonly int Field3;

        public PROTOCOL_CLAN_WAR_MATCH_TEAM_INFO_ACK(uint A_1, ClanModel A_2)
        {
            this.Field0 = A_1;
            this.Field1 = A_2;
            if (this.Field1 == null)
                return;
            this.Field3 = DaoManagerSQL.GetClanPlayers(A_2.Id);
            this.Field2 = AccountManager.GetAccount(A_2.OwnerId, 31 /*0x1F*/);
            if (this.Field2 != null)
                return;
            this.Field0 = 2147483648U /*0x80000000*/;
        }

        public PROTOCOL_CLAN_WAR_MATCH_TEAM_INFO_ACK(uint A_1) => this.Field0 = A_1;

        public override void Write()
        {
            this.WriteH((short)1570);
            this.WriteD(this.Field0);
            if (this.Field0 != 0U)
                return;
            this.WriteD(this.Field1.Id);
            this.WriteS(this.Field1.Name, 17);
            this.WriteC((byte)this.Field1.Rank);
            this.WriteC((byte)this.Field3);
            this.WriteC((byte)this.Field1.MaxPlayers);
            this.WriteD(this.Field1.CreationDate);
            this.WriteD(this.Field1.Logo);
            this.WriteC((byte)this.Field1.NameColor);
            this.WriteC((byte)this.Field1.GetClanUnit());
            this.WriteD(this.Field1.Exp);
            this.WriteD(0);
            this.WriteQ(this.Field1.OwnerId);
            this.WriteS(this.Field2.Nickname, 33);
            this.WriteC((byte)this.Field2.Rank);
            this.WriteS("", (int)byte.MaxValue);
        }
    }
}