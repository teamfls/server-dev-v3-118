// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_BASE_CONNECT_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Utility;
using Server.Game.Data.Models;
using Server.Game.Data.XML;
using System.Collections.Generic;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_BASE_CONNECT_ACK : GameServerPacket
    {
        private readonly int Field0;
        private readonly int Field1;
        private readonly ushort Field2;
        private readonly List<byte[]> Field3;

        public PROTOCOL_BASE_CONNECT_ACK(GameClient A_1)
        {
            this.Field0 = A_1.ServerId;
            this.Field1 = A_1.SessionId;
            this.Field2 = A_1.SessionSeed;
            this.Field3 = Bitwise.GenerateRSAKeyPair(this.Field1, this.SECURITY_KEY, this.SEED_LENGTH);
        }

        public override void Write()
        {
            this.WriteH((short)2306);
            this.WriteH((short)0);
            this.WriteC((byte)ChannelsXML.GetChannels(this.Field0).Count);
            foreach (ChannelModel channel in ChannelsXML.GetChannels(this.Field0))
                this.WriteC((byte)channel.Type);
            this.WriteH((ushort)(this.Field3[0].Length + this.Field3[1].Length + 2));
            this.WriteH((ushort)this.Field3[0].Length);
            this.WriteB(this.Field3[0]);
            this.WriteB(this.Field3[1]);
            this.WriteC((byte)3);
            this.WriteH((short)71 /*0x50*/);
            this.WriteH(this.Field2);
            this.WriteD(this.Field1);
        }
    }
}