// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Managers;
using Plugin.Core.Models;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK : GameServerPacket
    {
        private readonly uint Error;
        private readonly PlayerBattlepass PlayerSeasonpass;
        private readonly BattlePassSeason SeasonData;

        public PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(uint A_1, PlayerBattlepass A_2)
        {
            this.Error = A_1;
            this.PlayerSeasonpass = A_2;
            this.SeasonData = BattlePassManager.GetActiveSeason();
        }

        public override void Write()
        {
            this.WriteH((short)8455);
            this.WriteH((short)0);
            this.WriteD(this.Error);

            if (this.Error != 0U)
                return;

            this.WriteD(this.SeasonData != null && this.SeasonData.SeasonEnabled == 1 ? 1 : 0);
            var levelInfo = BattlePass.GetLevelInfoForSeason((int)PlayerSeasonpass.EarnedPoints);
            int currentLevel = levelInfo.currentLevel;
            int completedLevel = levelInfo.completedLevel;

            this.WriteC((byte)currentLevel);
            this.WriteD(PlayerSeasonpass.EarnedPoints);
            this.WriteC((byte)completedLevel);
            this.WriteC((byte)(PlayerSeasonpass.HavePremium ? completedLevel : 0));
            this.WriteC((byte)(PlayerSeasonpass.HavePremium ? 1 : 0));
            this.WriteD(1);
            this.WriteD(1);
            this.WriteD(1);
            this.WriteD(1);
            this.WriteD(1);
        }
    }
}