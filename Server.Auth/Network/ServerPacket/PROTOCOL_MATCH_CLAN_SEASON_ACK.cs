// Decompiled with JetBrains decompiler
// Type: Server.Auth.Network.ServerPacket.PROTOCOL_MATCH_CLAN_SEASON_ACK
// Assembly: Server.Auth, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: D2254E5E-B0BA-4DE9-9720-2DDECE3CD4EF
// Assembly location: C:\Users\home\Desktop\dll\Server.Auth-deobfuscated-Cleaned.dll

using Plugin.Core.Utility;
using System.Runtime.CompilerServices;


namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_MATCH_CLAN_SEASON_ACK : AuthServerPacket
    {
        private readonly int Field0;

        public PROTOCOL_MATCH_CLAN_SEASON_ACK(int A_1) => this.Field0 = A_1;

        
        public override void Write()
        {
            this.WriteH((short)7700);
            this.WriteH((short)0);
            this.WriteD(2);
            this.WriteB(ComDiv.AddressBytes("127.0.0.1"));
            this.WriteB(ComDiv.AddressBytes("255.255.255.255"));
            this.WriteB(new byte[109]);
            this.WriteB(ComDiv.AddressBytes("127.0.0.1"));
            this.WriteB(ComDiv.AddressBytes("127.0.0.1"));
            this.WriteB(ComDiv.AddressBytes("255.255.255.255"));
            this.WriteB(new byte[103]);
        }
    }
}
