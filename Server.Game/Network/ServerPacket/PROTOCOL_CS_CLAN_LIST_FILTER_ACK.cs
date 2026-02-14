// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CS_CLAN_LIST_FILTER_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CS_CLAN_LIST_FILTER_ACK : GameServerPacket
    {
        private readonly byte Field0;
        private readonly int Field1;
        private readonly byte[] Field2;

        public PROTOCOL_CS_CLAN_LIST_FILTER_ACK(byte A_1, int A_2, byte[] A_3)
        {
            this.Field0 = A_1;
            this.Field1 = A_2;
            this.Field2 = A_3;
        }

        public override void Write()
        {
            this.WriteH((short)998);
            this.WriteH((short)0);
            this.WriteC((byte)0);
            this.WriteC(this.Field0);
            this.WriteH((ushort)this.Field1);
            this.WriteD(this.Field1);
            this.WriteB(this.Field2);
        }
    }
}
