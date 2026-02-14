// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CLAN_WAR_MATCH_TEAM_LIST_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Server.Game.Data.Models;
using System.Collections.Generic;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CLAN_WAR_MATCH_TEAM_LIST_ACK : GameServerPacket
    {
        private readonly List<MatchModel> Field0;
        private readonly int Field1;
        private readonly int Field2;

        public PROTOCOL_CLAN_WAR_MATCH_TEAM_LIST_ACK(List<MatchModel> A_1, int A_2)
        {
            this.Field1 = A_2;
            this.Field0 = A_1;
            this.Field2 = A_1.Count - 1;
        }

        public override void Write()
        {
            this.WriteH((short)6917);
            this.WriteH((ushort)this.Field2);
            if (this.Field2 <= 0)
                return;
            this.WriteH((short)1);
            this.WriteH((short)0);
            this.WriteC((byte)this.Field2);
            foreach (MatchModel matchModel in this.Field0)
            {
                if (matchModel.MatchId != this.Field1)
                {
                    this.WriteH((short)matchModel.MatchId);
                    this.WriteH((short)matchModel.GetServerInfo());
                    this.WriteH((short)matchModel.GetServerInfo());
                    this.WriteC((byte)matchModel.State);
                    this.WriteC((byte)matchModel.FriendId);
                    this.WriteC((byte)matchModel.Training);
                    this.WriteC((byte)matchModel.GetCountPlayers());
                    this.WriteD(matchModel.Leader);
                    this.WriteC((byte)0);
                    this.WriteD(matchModel.Clan.Id);
                    this.WriteC((byte)matchModel.Clan.Rank);
                    this.WriteD(matchModel.Clan.Logo);
                    this.WriteS(matchModel.Clan.Name, 17);
                    this.WriteT(matchModel.Clan.Points);
                    this.WriteC((byte)matchModel.Clan.NameColor);
                    Account leader = matchModel.GetLeader();
                    if (leader == null)
                    {
                        this.WriteB(new byte[43]);
                    }
                    else
                    {
                        this.WriteC((byte)leader.Rank);
                        this.WriteS(leader.Nickname, 33);
                        this.WriteQ(leader.PlayerId);
                        this.WriteC((byte)matchModel.Slots[matchModel.Leader].State);
                    }
                }
            }
        }
    }
}