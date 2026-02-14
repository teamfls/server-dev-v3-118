// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_MATCH_CLAN_SEASON_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Utility;
using System.Runtime.CompilerServices;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_MATCH_CLAN_SEASON_ACK : GameServerPacket
    {
        private readonly bool Field0;

        public PROTOCOL_MATCH_CLAN_SEASON_ACK(bool A_1) => this.Field0 = A_1;

        
        public override void Write()
        {
            this.WriteH((short)7700);
            this.WriteH((short)0);
            this.WriteD(2);
            this.WriteD(this.Field0 ? 1 : 0);
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