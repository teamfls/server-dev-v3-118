using Plugin.Core.Enums;
using Plugin.Core;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.Network;
using Plugin.Core.SQL;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using System;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CS_DETAIL_INFO_ACK : GameServerPacket
    {
        private readonly ClanModel Clan;
        private readonly int Error;
        private readonly Account Player;
        private readonly int Players;
        private readonly ClanSeasonalStats SeasonalStats;
        private readonly ClanWeeklyStats WeeklyStats;

        public PROTOCOL_CS_DETAIL_INFO_ACK(int Error, ClanModel Clan)
        {
            this.Error = Error;
            this.Clan = Clan;
            if (Clan != null)
            {
                Player = AccountManager.GetAccount(Clan.OwnerId, 31);
                Players = DaoManagerSQL.GetClanPlayers(Clan.Id);
                SeasonalStats = ClanManager.CalculateClanSeasonalStats(Clan.Id);
                WeeklyStats = ClanManager.CalculateClanWeeklyStats(Clan.Id);
            }
        }

        public override void Write()
        {
            WriteH(801);
            WriteD(Error);

            // ✅ FIX: Jika error atau clan null, kirim packet minimal
            if (Clan == null || Error != 0)
            {
                WriteD(0); // Clan ID = 0
                WriteU("", 34); // Empty name
                WriteC(0); // rank
                WriteC(0); // players
                WriteC(0); // max players
                WriteD(0); // creation date
                WriteD(0); // logo
                WriteC(0); // name color
                WriteC(0); // effect
                WriteC(0); // clan unit
                WriteD(0); // exp
                WriteQ(0); // owner id
                WriteC(0);
                WriteC(0);
                WriteC(0);
                WriteC(0);
                WriteB(new byte[68]); // Empty owner data
                WriteU("", 510); // Empty info
                WriteB(new byte[41]);
                WriteC(0); // join type
                WriteC(0); // rank limit
                WriteC(0); // max age
                WriteC(0); // min age
                WriteC(0); // authority
                WriteU("", 510); // Empty news

                // Stats semua 0
                for (int i = 0; i < 63; i++)
                {
                    WriteD(0);
                }
                WriteC(0);
                WriteD(0);
                return;
            }

            WriteD(Clan.Id);
            WriteU(Clan.Name, 34);
            WriteC((byte)Clan.Rank);
            WriteC((byte)Players);
            WriteC((byte)Clan.MaxPlayers);
            WriteD(Clan.CreationDate);
            WriteD(Clan.Logo);
            WriteC((byte)Clan.NameColor);
            WriteC((byte)Clan.Effect);
            WriteC((byte)Clan.GetClanUnit());
            WriteD(Clan.Exp);
            WriteQ(Clan.OwnerId);
            WriteC(0);
            WriteC(0);
            WriteC(0);
            WriteC(0);
            WriteB(ClanOwnerData(Player));
            WriteU(Clan.Info, 510);
            WriteB(new byte[41]);
            WriteC((byte)Clan.JoinType);
            WriteC((byte)Clan.RankLimit);
            WriteC((byte)Clan.MaxAgeLimit);
            WriteC((byte)Clan.MinAgeLimit);
            WriteC((byte)Clan.Authority);
            WriteU(Clan.News, 510);

            WriteD((int)Clan.Points);
            WriteD(Clan.MatchWins);
            WriteD(Clan.MatchLoses);

            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);

            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);

            WriteD(Clan.Matches);
            WriteD(Clan.MatchWins);
            WriteD(Clan.MatchLoses);
            WriteD(0);
            WriteD(0);
            WriteD(Clan.TotalKills);
            WriteD(Clan.TotalAssists);
            WriteD(Clan.TotalDeaths);
            WriteD(Clan.TotalHeadshots);
            WriteD(Clan.TotalEscapes);

            WriteD(0);
            WriteD(SeasonalStats?.CurrentSeason?.Medals ?? 0);
            WriteD(SeasonalStats?.CurrentSeason?.Matches ?? 0);
            WriteD(SeasonalStats?.CurrentSeason?.MatchWins ?? 0);
            WriteD(SeasonalStats?.CurrentSeason?.MatchLoses ?? 0);
            WriteD(SeasonalStats?.CurrentSeason?.SeasonRank ?? 0);
            WriteD(SeasonalStats?.CurrentSeason?.PointsGained ?? 0);
            WriteD(SeasonalStats?.CurrentSeason?.TotalKills ?? 0);
            WriteD(SeasonalStats?.CurrentSeason?.TotalAssists ?? 0);
            WriteD(SeasonalStats?.CurrentSeason?.TotalDeaths ?? 0);
            WriteD(SeasonalStats?.CurrentSeason?.TotalHeadshots ?? 0);
            WriteD(SeasonalStats?.CurrentSeason?.TotalEscapes ?? 0);

            WriteD(0);
            WriteD(SeasonalStats?.PreviousSeason?.Medals ?? 0);
            WriteD(SeasonalStats?.PreviousSeason?.Matches ?? 0);
            WriteD(SeasonalStats?.PreviousSeason?.MatchWins ?? 0);
            WriteD(SeasonalStats?.PreviousSeason?.MatchLoses ?? 0);
            WriteD(SeasonalStats?.PreviousSeason?.SeasonRank ?? 0);
            WriteD(SeasonalStats?.PreviousSeason?.PointsGained ?? 0);
            WriteD(SeasonalStats?.PreviousSeason?.TotalKills ?? 0);
            WriteD(SeasonalStats?.PreviousSeason?.TotalAssists ?? 0);
            WriteD(SeasonalStats?.PreviousSeason?.TotalDeaths ?? 0);
            WriteD(SeasonalStats?.PreviousSeason?.TotalHeadshots ?? 0);
            WriteD(SeasonalStats?.PreviousSeason?.TotalEscapes ?? 0);

            WriteD(WeeklyStats?.CurrentWeekMedals ?? 0);
            WriteD(WeeklyStats?.PreviousWeekMedals ?? 0);
            WriteC(0);
            WriteD(Clan.Rank);
        }

        private byte[] ClanOwnerData(Account Player)
        {
            using (SyncServerPacket S = new SyncServerPacket())
            {
                if (Player != null)
                {
                    S.WriteU(Player.Nickname, 66);
                    S.WriteC((byte)Player.NickColor);
                    S.WriteC((byte)Player.Rank);
                }
                else
                {
                    S.WriteU("", 66);
                    S.WriteC(0);
                    S.WriteC(0);
                }
                return S.ToArray();
            }
        }
    }
}