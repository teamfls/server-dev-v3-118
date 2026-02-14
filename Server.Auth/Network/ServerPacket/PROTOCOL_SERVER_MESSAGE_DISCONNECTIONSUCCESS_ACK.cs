// Decompiled with JetBrains decompiler
// Type: Server.Auth.Network.ServerPacket.PROTOCOL_SERVER_MESSAGE_DISCONNECTIONSUCCESS_ACK
// Assembly: Server.Auth, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: D2254E5E-B0BA-4DE9-9720-2DDECE3CD4EF
// Assembly location: C:\Users\home\Desktop\dll\Server.Auth-deobfuscated-Cleaned.dll

using Plugin.Core.Utility;
using System.Runtime.CompilerServices;


namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_SERVER_MESSAGE_DISCONNECTIONSUCCESS_ACK : AuthServerPacket
    {
        private readonly uint Field0;
        private readonly bool Field1;

        public PROTOCOL_SERVER_MESSAGE_DISCONNECTIONSUCCESS_ACK(uint A_1, bool A_2)
        {
            this.Field0 = A_1;
            this.Field1 = A_2;
        }

        
        public override void Write()
        {
            this.WriteH((short)3074);
            this.WriteD(uint.Parse(DateTimeUtil.Now("yyMMddHHmm")));
            this.WriteD(this.Field0);
            this.WriteD(this.Field1 ? 1 : 0);
            if (!this.Field1)
                return;
            this.WriteD(0);
        }
    }
}