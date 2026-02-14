using Plugin.Core.Models;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_ROOM_GET_ACEMODE_PLAYERINFO_ACK : GameServerPacket
    {
        private readonly Account account;
        private readonly StatisticAcemode Field1;
        private readonly ClanModel Field2;

        public PROTOCOL_ROOM_GET_ACEMODE_PLAYERINFO_ACK(Account account)
        {
            this.account = account;
            if (account != null)
            {
                Field1 = account.Statistic.Acemode;
                Field2 = ClanManager.GetClan(account.ClanId);
            }
        }

        public override void Write()
        {
            WriteH((short)3681);
            WriteH((short)0);
            WriteD((uint)account.PlayerId);
            WriteD(Field1.Matches);
            WriteD(Field1.MatchWins);
            WriteD(Field1.MatchLoses);
            WriteD(Field1.Kills);
            WriteD(Field1.Deaths);
            WriteD(Field1.Headshots);
            WriteD(Field1.Assists);
            WriteD(Field1.Escapes);
            WriteD(Field1.Winstreaks);
            WriteB(new byte[122]);
            WriteU(account.Nickname, 66);
            WriteD(account.Rank);
            WriteD(account.GetRank());
            WriteD(0);
            WriteD(0);
            WriteD(account.Gold);
            WriteD(account.Exp);
            WriteD(account.Tags);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteC((byte)0);
            WriteC((byte)0);
            WriteC((byte)0);
            WriteD(account.Cash);
            WriteD(account.ClanId);
            WriteD(account.ClanAccess);
            WriteQ(account.StatusId());
            WriteC((byte)account.CafePC);
            WriteC((byte)account.TourneyLevel()); //CountryFlags);
            //WriteC((byte)account.CountryFlags); //CountryFlags);
            WriteU(Field2.Name, 34);
            WriteC((byte)Field2.Rank);
            WriteC((byte)Field2.GetClanUnit());
            WriteD(Field2.Logo);
            WriteC((byte)Field2.NameColor);
            WriteC((byte)Field2.Effect);
            WriteC(GameXender.Client.Config.EnableBlood ? (byte)account.Age : (byte)24);
        }
    }
}