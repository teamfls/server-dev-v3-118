// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_LOBBY_NEW_MYINFO_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Models;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_LOBBY_NEW_MYINFO_ACK : GameServerPacket
    {
        private readonly Account Field0;
        private readonly ClanModel Field1;
        private readonly StatisticClan Field2;

        public PROTOCOL_LOBBY_NEW_MYINFO_ACK(Account A_1)
        {
            this.Field0 = A_1;
            if (A_1 == null)
                return;
            this.Field1 = ClanManager.GetClan(A_1.ClanId);
            this.Field2 = A_1.Statistic.Clan;
        }

        public override void Write()
        {
            this.WriteH((short)977);
            this.WriteD(0);
            this.WriteQ(this.Field0.PlayerId);
            this.WriteD(1000);
            this.WriteD(900);
            this.WriteD(100);
            this.WriteD(0);
            this.WriteD(1);
            this.WriteD(2);
            this.WriteD(3);
            this.WriteD(4);
            this.WriteD(5);
            this.WriteD(6);
            this.WriteD(7);
            this.WriteD(8);
            this.WriteD(1111);
            this.WriteD(2222);
            this.WriteD(3333);
            this.WriteD(4444);
            this.WriteD(5555);
            this.WriteD(6666);
            this.WriteD(7777);
            this.WriteD(8888);
            this.WriteD(9999);
            this.WriteD(111111);
            this.WriteD(222222);
            this.WriteD(333333);
            this.WriteD(444444);
            this.WriteD(555555);
            this.WriteD(666666);
            this.WriteD(777777);
            this.WriteD(888888);
            this.WriteD(999999);
            this.WriteD(111111);
            this.WriteD(222222);
            this.WriteD(0);
        }
    }
}