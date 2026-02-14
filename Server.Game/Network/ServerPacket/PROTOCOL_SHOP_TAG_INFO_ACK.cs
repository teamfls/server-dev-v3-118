// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_SHOP_TAG_INFO_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_SHOP_TAG_INFO_ACK : GameServerPacket
    {
        public override void Write()
        {
            this.WriteH((short)1099);
            this.WriteH((short)0);
            this.WriteC((byte)7);
            this.WriteC((byte)5);
            this.WriteH((short)0);
            this.WriteC((byte)0);
            this.WriteD(0);
            this.WriteH((short)0);
            this.WriteC((byte)3);
            this.WriteQ(0L);
            this.WriteC((byte)0);
            this.WriteC((byte)4);
            this.WriteQ(0L);
            this.WriteC((byte)0);
            this.WriteC((byte)2);
            this.WriteQ(0L);
            this.WriteC((byte)0);
            this.WriteC((byte)6);
            this.WriteQ(0L);
            this.WriteC((byte)0);
            this.WriteC((byte)1);
            this.WriteQ(0L);
            this.WriteD(0);
            this.WriteC((byte)0);
            this.WriteC(byte.MaxValue);
            this.WriteC(byte.MaxValue);
            this.WriteC(byte.MaxValue);
            this.WriteC((byte)0);
            this.WriteC(byte.MaxValue);
            this.WriteC((byte)1);
            this.WriteC((byte)7);
            this.WriteC((byte)2);
        }
    }
}