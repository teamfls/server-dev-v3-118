// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_BASE_QUEST_BUY_CARD_SET_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using Plugin.Core.Utility;
using Server.Game.Data.Models;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_BASE_QUEST_BUY_CARD_SET_ACK : GameServerPacket
    {
        private readonly Account Field0;
        private readonly EventErrorEnum Field1;

        public PROTOCOL_BASE_QUEST_BUY_CARD_SET_ACK(EventErrorEnum A_1, Account A_2)
        {
            this.Field1 = A_1;
            this.Field0 = A_2;
        }

        public override void Write()
        {
            this.WriteH((short)2365);
            this.WriteD((uint)this.Field1);
            if (this.Field1 != EventErrorEnum.SUCCESS)
                return;
            this.WriteD(this.Field0.Gold);
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
            this.WriteD(this.Field0.Cash);
        }
    }
}