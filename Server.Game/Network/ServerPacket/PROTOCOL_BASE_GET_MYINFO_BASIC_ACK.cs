using Plugin.Core.Models;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_BASE_GET_MYINFO_BASIC_ACK : GameServerPacket
    {
        private readonly Account account;
        private readonly ClanModel clanModel;

        public PROTOCOL_BASE_GET_MYINFO_BASIC_ACK(Account Account)
        {
            account = Account;
            if (Account != null)
            {
                clanModel = ClanManager.GetClan(Account.ClanId);
            }
        }

        public override void Write()
        {
            WriteH(2371);
            WriteU(account.Nickname, 66);
            WriteD(account.GetRank());
            WriteD(account.GetRank());
            WriteD(account.Gold);
            WriteD(account.Exp);
            WriteD(0);
            WriteC(0);
            WriteQ(0);
            WriteC((byte)account.AuthLevel());
            WriteC(0);
            WriteD(account.Tags);
            WriteD(account.Cash);
            WriteD(clanModel.Id);
            WriteD(account.ClanAccess);
            WriteQ(account.StatusId());
            WriteC((byte)account.CafePC);
            WriteC((byte)account.TourneyLevel()); //CountryFlags);
            //WriteC((byte)account.CountryFlags); //CountryFlags);
            WriteU(clanModel.Name, 34);
            WriteC((byte)clanModel.Rank);
            WriteC((byte)clanModel.GetClanUnit());
            WriteD(clanModel.Logo);
            WriteC((byte)clanModel.NameColor);
            WriteC((byte)clanModel.Effect);
            WriteD(account.Statistic.Season.Matches);
            WriteD(account.Statistic.Season.MatchWins);
            WriteD(account.Statistic.Season.MatchLoses);
            WriteD(account.Statistic.Season.MatchDraws);
            WriteD(account.Statistic.Season.KillsCount);
            WriteD(account.Statistic.Season.HeadshotsCount);
            WriteD(account.Statistic.Season.DeathsCount);
            WriteD(account.Statistic.Season.TotalMatchesCount);
            WriteD(account.Statistic.Season.TotalKillsCount);
            WriteD(account.Statistic.Season.EscapesCount);
            WriteD(account.Statistic.Season.AssistsCount);
            WriteD(account.Statistic.Season.MvpCount);
            WriteD(account.Statistic.Basic.Matches);
            WriteD(account.Statistic.Basic.MatchWins);
            WriteD(account.Statistic.Basic.MatchLoses);
            WriteD(account.Statistic.Basic.MatchDraws);
            WriteD(account.Statistic.Basic.KillsCount);
            WriteD(account.Statistic.Basic.HeadshotsCount);
            WriteD(account.Statistic.Basic.DeathsCount);
            WriteD(account.Statistic.Basic.TotalMatchesCount);
            WriteD(account.Statistic.Basic.TotalKillsCount);
            WriteD(account.Statistic.Basic.EscapesCount);
            WriteD(account.Statistic.Basic.AssistsCount);
            WriteD(account.Statistic.Basic.MvpCount);
            WriteC((byte)account.Bonus.NickBorderColor);
        }
    }
}