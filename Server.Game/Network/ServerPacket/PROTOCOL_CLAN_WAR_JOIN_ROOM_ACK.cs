// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CLAN_WAR_JOIN_ROOM_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Server.Game.Data.Models;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CLAN_WAR_JOIN_ROOM_ACK : GameServerPacket
    {
        private readonly MatchModel Field0;
        private readonly int Field1;
        private readonly int Field2;

        public PROTOCOL_CLAN_WAR_JOIN_ROOM_ACK(MatchModel A_1, int A_2, int A_3)
        {
            this.Field0 = A_1;
            this.Field1 = A_2;
            this.Field2 = A_3;
        }

        public override void Write()
        {
            this.WriteH((short)1566);
            this.WriteD(this.Field1);
            this.WriteH((ushort)this.Field2);
            this.WriteH((ushort)this.Field0.GetServerInfo());
        }
    }
}