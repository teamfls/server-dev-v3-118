// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CLAN_WAR_CREATE_ROOM_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Server.Game.Data.Models;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CLAN_WAR_CREATE_ROOM_ACK : GameServerPacket
    {
        public readonly MatchModel match;

        public PROTOCOL_CLAN_WAR_CREATE_ROOM_ACK(MatchModel A_1) => this.match = A_1;

        public override void Write()
        {
            this.WriteH((short)1564);
            this.WriteH((short)this.match.MatchId);
            this.WriteD(this.match.GetServerInfo());
            this.WriteH((short)this.match.GetServerInfo());
            this.WriteC((byte)10);
        }
    }
}