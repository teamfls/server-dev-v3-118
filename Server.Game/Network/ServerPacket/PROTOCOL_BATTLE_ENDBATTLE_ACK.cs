using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Network;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Utils;
using System;
using System.Runtime.CompilerServices;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_BATTLE_ENDBATTLE_ACK : GameServerPacket
    {
        private readonly RoomModel roomModel;
        private readonly Account account;
        private readonly ClanModel Field2;
        private readonly int Winner = 2;
        private readonly int SlotFlag;
        private readonly int MissionsFlag;
        private readonly bool IsBotMode;
        private readonly byte[] SlotInfoData;

        public PROTOCOL_BATTLE_ENDBATTLE_ACK(Account Acccount)
        {
            account = Acccount;
            if (Acccount != null)
            {
                roomModel = Acccount.GetRoom();
                if (roomModel != null)
                {
                    Winner = roomModel.RoomType == RoomCondition.Tutorial ? 0 : (int)AllUtils.GetWinnerTeam(roomModel);
                    Field2 = ClanManager.GetClan(Acccount.ClanId);
                    IsBotMode = roomModel.IsBotMode();
                    AllUtils.GetBattleResult(roomModel, out MissionsFlag, out SlotFlag, out SlotInfoData);
                }
            }
        }

        public PROTOCOL_BATTLE_ENDBATTLE_ACK(Account Acccount, int Winner, int SlotFlag, int MissionsFlag, bool IsBotMode, byte[] SlotInfoData)
        {
            account = Acccount;
            this.Winner = Winner;
            this.SlotFlag = SlotFlag;
            this.MissionsFlag = MissionsFlag;
            this.IsBotMode = IsBotMode;
            this.SlotInfoData = SlotInfoData;
            if (Acccount != null)
            {
                roomModel = Acccount.GetRoom();
                Field2 = ClanManager.GetClan(Acccount.ClanId);
            }
        }

        public PROTOCOL_BATTLE_ENDBATTLE_ACK(Account Acccount, TeamEnum Winner, int SlotFlag, int MissionsFlag, bool IsBotMode, byte[] SlotInfoData)
        {
            account = Acccount;
            this.Winner = (int)Winner;
            this.SlotFlag = SlotFlag;
            this.MissionsFlag = MissionsFlag;
            this.IsBotMode = IsBotMode;
            this.SlotInfoData = SlotInfoData;
            if (Acccount != null)
            {
                roomModel = Acccount.GetRoom();
                Field2 = ClanManager.GetClan(Acccount.ClanId);
            }
        }

        public override void Write()
        {
            WriteH((short)5140);
            WriteD(SlotFlag);
            WriteC((byte)Winner);
            WriteB(SlotInfoData);
            WriteD(MissionsFlag);
            WriteB(Method0(roomModel, IsBotMode));
            WriteC((byte)0);
            WriteC((byte)0);
            WriteC((byte)0);
            WriteB(new byte[5]);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteH((short)0);
            WriteH((short)0);
            WriteB(new byte[14]);
            WriteB(Method1(roomModel));
            WriteB(new byte[27]);
            WriteB(Method5(roomModel));
            WriteB(Method2(roomModel));
            WriteC((byte)(account.Nickname.Length * 2));
            WriteU(account.Nickname, account.Nickname.Length * 2);
            WriteD(account.GetRank());
            WriteD(account.Rank);
            WriteD(account.Gold);
            WriteD(account.Exp);
            WriteD(0);
            WriteC((byte)0);
            WriteQ(0);
            WriteC((byte)account.AuthLevel());
            WriteC((byte)0);
            WriteD(account.Tags);
            WriteD(account.Cash);
            WriteD(Field2.Id);
            WriteD(account.ClanAccess);
            WriteQ(account.StatusId());
            WriteC((byte)account.CafePC);
            WriteC((byte)account.TourneyLevel()); //CountryFlags);
            //WriteC((byte)account.CountryFlags); //CountryFlags);
            WriteC((byte)(Field2.Name.Length * 2));
            WriteU(Field2.Name, Field2.Name.Length * 2);
            WriteC((byte)Field2.Rank);
            WriteC((byte)Field2.GetClanUnit());
            WriteD(Field2.Logo);
            WriteC((byte)Field2.NameColor);
            WriteC((byte)Field2.Effect);
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
            WriteH((ushort)account.Statistic.Daily.Matches);
            WriteH((ushort)account.Statistic.Daily.MatchWins);
            WriteH((ushort)account.Statistic.Daily.MatchLoses);
            WriteH((ushort)account.Statistic.Daily.MatchDraws);
            WriteH((ushort)account.Statistic.Daily.KillsCount);
            WriteH((ushort)account.Statistic.Daily.HeadshotsCount);
            WriteH((ushort)account.Statistic.Daily.DeathsCount);
            WriteD(account.Statistic.Daily.ExpGained);
            WriteD(account.Statistic.Daily.PointGained);
            WriteD(account.Statistic.Daily.Playtime);
            WriteB(Method3(account));
            WriteD(0);
            WriteC((byte)0);
            WriteD(0);
            WriteC((byte)0);
            WriteD(0);
            WriteH((short)0);
            WriteC((byte)0);
            WriteB(Method4(account));
        }

        private byte[] Method0(RoomModel Acccount, bool A_2)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                if (A_2)
                {
                    foreach (SlotModel slot in Acccount.Slots)
                        syncServerPacket.WriteH((ushort)slot.Score);
                }
                else if (Acccount.ThisModeHaveRounds() || Acccount.IsDinoMode())
                {
                    syncServerPacket.WriteH(Acccount.IsDinoMode("DE") ? (ushort)Acccount.FRDino : (Acccount.IsDinoMode("CC") ? (ushort)Acccount.FRKills : (ushort)Acccount.FRRounds));
                    syncServerPacket.WriteH(Acccount.IsDinoMode("DE") ? (ushort)Acccount.CTDino : (Acccount.IsDinoMode("CC") ? (ushort)Acccount.CTKills : (ushort)Acccount.CTRounds));
                    foreach (SlotModel slot in Acccount.Slots)
                        syncServerPacket.WriteC((byte)slot.Objects);
                    syncServerPacket.WriteH((short)0);
                    syncServerPacket.WriteH((short)0);
                }
                return syncServerPacket.ToArray();
            }
        }

        private byte[] Method1(RoomModel Acccount)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteH(Acccount.ThisModeHaveRounds() ? (ushort)Acccount.FRRounds : (ushort)0);
                syncServerPacket.WriteH(Acccount.ThisModeHaveRounds() ? (ushort)Acccount.CTRounds : (ushort)0);
                return syncServerPacket.ToArray();
            }
        }

        private byte[] Method2(RoomModel Acccount)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                foreach (SlotModel slot in Acccount.Slots)
                {
                    Account Player;
                    if (Acccount.GetPlayerBySlot(slot, out Player))
                        syncServerPacket.WriteC((byte)Player.Rank);
                    else
                        syncServerPacket.WriteC((byte)AllUtils.InitBotRank(Acccount.IsStartingMatch() ? (int)Acccount.IngameAiLevel : (int)Acccount.AiLevel));
                    syncServerPacket.WriteH((short)0);
                    syncServerPacket.WriteD(1);
                }
                return syncServerPacket.ToArray();
            }
        }

        private byte[] Method3(Account Acccount)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                PlayerEvent playerEvent = Acccount.Event;
                if (playerEvent != null)
                {
                    syncServerPacket.WriteC((byte)playerEvent.LastPlaytimeFinish);
                    syncServerPacket.WriteD((uint)playerEvent.LastPlaytimeValue);
                }
                else
                    syncServerPacket.WriteB(new byte[5]);
                return syncServerPacket.ToArray();
            }
        }

        private byte[] Method4(Account Acccount)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                SlotModel slot = roomModel.GetSlot(Acccount.SlotId);
                if (slot != null)
                {
                    syncServerPacket.WriteB(new byte[44]);
                    syncServerPacket.WriteD(0);
                    syncServerPacket.WriteB(new byte[16 /*0x10*/]);
                    syncServerPacket.WriteH((ushort)slot.SeasonPoint);
                    syncServerPacket.WriteH((ushort)slot.BonusBattlePass);
                    syncServerPacket.WriteC((byte)0);
                    syncServerPacket.WriteB(new byte[20]);
                    syncServerPacket.WriteD(0);
                    syncServerPacket.WriteH((ushort)(600 + Acccount.InventoryPlus + 8));
                }
                return syncServerPacket.ToArray();
            }
        }

        private byte[] Method5(RoomModel Acccount)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                try
                {
                    if (Acccount?.SlotRewards != null)
                    {
                        if (Acccount.SlotRewards.Item1 != null)
                        {
                            foreach (byte num in Acccount.SlotRewards.Item1)
                                syncServerPacket.WriteC(num);
                        }
                        if (Acccount.SlotRewards.Item2 != null)
                        {
                            foreach (int num in Acccount.SlotRewards.Item2)
                                syncServerPacket.WriteD(num);
                        }
                    }
                }
                catch (Exception)
                {
                    // Log error jika perlu
                }
                return syncServerPacket.ToArray();
            }
        }
    }
}