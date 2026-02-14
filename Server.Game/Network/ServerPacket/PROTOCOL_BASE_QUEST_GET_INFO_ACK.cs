// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_BASE_QUEST_GET_INFO_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Utility;
using Server.Game.Data.Models;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_BASE_QUEST_GET_INFO_ACK : GameServerPacket
    {
        private readonly Account Field0;

        public PROTOCOL_BASE_QUEST_GET_INFO_ACK(Account A_1) => this.Field0 = A_1;

        public override void Write()
        {
            this.WriteH((short)2355);
            this.WriteC((byte)this.Field0.Mission.ActualMission);
            this.WriteC((byte)this.Field0.Mission.ActualMission);
            this.WriteC((byte)this.Field0.Mission.Card1);
            this.WriteC((byte)this.Field0.Mission.Card2);
            this.WriteC((byte)this.Field0.Mission.Card3);
            this.WriteC((byte)this.Field0.Mission.Card4);
            this.WriteB(ComDiv.GetMissionCardFlags(this.Field0.Mission.Mission1, this.Field0.Mission.List1));
            this.WriteB(ComDiv.GetMissionCardFlags(this.Field0.Mission.Mission2, this.Field0.Mission.List2));
            this.WriteB(ComDiv.GetMissionCardFlags(this.Field0.Mission.Mission3, this.Field0.Mission.List3));
            this.WriteB(ComDiv.GetMissionCardFlags(this.Field0.Mission.Mission4, this.Field0.Mission.List4));
            this.WriteC((byte)this.Field0.Mission.Mission1);
            this.WriteC((byte)this.Field0.Mission.Mission2);
            this.WriteC((byte)this.Field0.Mission.Mission3);
            this.WriteC((byte)this.Field0.Mission.Mission4);
        }
    }
}