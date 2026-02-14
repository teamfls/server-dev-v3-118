// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CLAN_WAR_TEAM_CHATTING_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using System.Runtime.CompilerServices;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CLAN_WAR_TEAM_CHATTING_ACK : GameServerPacket
    {
        private readonly int Field0;
        private readonly int Field1;
        private readonly string Field2;
        private readonly string Field3;

        public PROTOCOL_CLAN_WAR_TEAM_CHATTING_ACK(string A_1, string A_2)
        {
            this.Field3 = A_1;
            this.Field2 = A_2;
        }

        public PROTOCOL_CLAN_WAR_TEAM_CHATTING_ACK(int A_1, int A_2)
        {
            this.Field0 = A_1;
            this.Field1 = A_2;
        }

        
        public override void Write()
        {
            this.WriteH((short)6929);
            this.WriteC((byte)this.Field0);
            if (this.Field0 == 0)
            {
                this.WriteC((byte)(this.Field3.Length + 1));
                this.WriteN(this.Field3, this.Field3.Length + 2, "UTF-16LE");
                this.WriteC((byte)(this.Field2.Length + 1));
                this.WriteN(this.Field2, this.Field2.Length + 2, "UTF-16LE");
            }
            else
                this.WriteD(this.Field1);
        }
    }
}