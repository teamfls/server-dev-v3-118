// Decompiled with JetBrains decompiler
// Type: Server.Auth.Network.ServerPacket.PROTOCOL_BASE_CONNECT_ACK
// Assembly: Server.Auth, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: D2254E5E-B0BA-4DE9-9720-2DDECE3CD4EF
// Assembly location: C:\Users\home\Desktop\dll\Server.Auth-deobfuscated-Cleaned.dll

using Plugin.Core.Utility;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_BASE_CONNECT_ACK : AuthServerPacket
    {
        private readonly int Field0;
        private readonly ushort Field1;
        private readonly List<byte[]> Field2;

        public PROTOCOL_BASE_CONNECT_ACK(AuthClient A_1)
        {
            this.Field0 = A_1.SessionId;
            this.Field1 = A_1.SessionSeed;
            this.Field2 = Bitwise.GenerateRSAKeyPair(this.Field0, this.SECURITY_KEY, this.SEED_LENGTH);
        }

        
        public override void Write()
        {
            this.WriteH((short)2306);
            this.WriteH((short)0);
            this.WriteC((byte)11);
            this.WriteB(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, });
            this.WriteH((ushort)(this.Field2[0].Length + this.Field2[1].Length + 2));
            this.WriteH((ushort)this.Field2[0].Length);
            this.WriteB(this.Field2[0]);
            this.WriteB(this.Field2[1]);
            this.WriteC((byte)3);
            this.WriteH((short)80);
            this.WriteH(this.Field1);
            this.WriteD(this.Field0);
        }
    }
}