// Decompiled with JetBrains decompiler
// Type: Server.Auth.Network.ServerPacket.PROTOCOL_BASE_GET_USER_INFO_ACK
// Assembly: Server.Auth, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: D2254E5E-B0BA-4DE9-9720-2DDECE3CD4EF
// Assembly location: C:\Users\home\Desktop\dll\Server.Auth-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Network;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using Server.Auth.Data.Managers;
using Server.Auth.Data.Models;
using Server.Auth.Data.Utils;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_BASE_GET_USER_INFO_ACK : AuthServerPacket
    {
        private readonly Account account;
        private readonly ClanModel clanModel;
        private readonly PlayerInventory playerInventory;
        private readonly PlayerEquipment playerEquipment;
        private readonly PlayerStatistic playerStatistic;
        private readonly EventVisitModel eventVisit;
        private readonly List<QuickstartModel> quickstarts;
        private readonly List<CharacterModel> characters;
        private readonly uint Error;
        private readonly uint Date;

        public PROTOCOL_BASE_GET_USER_INFO_ACK(Account Account)
        {
            account = Account;
            if (Account != null)
            {
                playerInventory = Account.Inventory;
                playerEquipment = Account.Equipment;
                playerStatistic = Account.Statistic;
                Date = uint.Parse(Account.LastLoginDate.ToString("yyMMddHHmm"));
                clanModel = ClanManager.GetClanDB((object)Account.ClanId, 1);
                quickstarts = Account.Quickstart.Quickjoins;
                characters = Account.Character.Characters;
                eventVisit = EventVisitXML.GetRunningEvent();
            }
            else
            {
                Error = (uint)EventErrorEnum.FAIL;
            }
        }

        public override void Write()
        {
            WriteH((short)2317);
            WriteH((short)0);
            WriteD(Error);
            if (Error != 0U)
                return;
            WriteB(new byte[21]);
            WriteD(1);
            WriteC((byte)1);
            WriteC((byte)6);
            WriteC((byte)1);
            WriteB(new byte[160 /*0xA0*/]);
            WriteD(playerStatistic.Battlecup.Matches);
            WriteD(playerStatistic.GetBCWinRatio());
            WriteD(playerStatistic.Battlecup.MatchLoses);
            WriteD(playerStatistic.Battlecup.KillsCount);
            WriteD(playerStatistic.Battlecup.DeathsCount);
            WriteD(playerStatistic.Battlecup.HeadshotsCount);
            WriteD(playerStatistic.Battlecup.AssistsCount);
            WriteD(playerStatistic.Battlecup.EscapesCount);
            WriteD(playerStatistic.GetBCKDRatio());
            WriteD(playerStatistic.Battlecup.MatchWins);
            WriteD(playerStatistic.Battlecup.AverageDamage);
            WriteD(playerStatistic.Battlecup.PlayTime);
            WriteD(playerStatistic.Acemode.Matches);
            WriteD(playerStatistic.Acemode.MatchWins);
            WriteD(playerStatistic.Acemode.MatchLoses);
            WriteD(playerStatistic.Acemode.Kills);
            WriteD(playerStatistic.Acemode.Deaths);
            WriteD(playerStatistic.Acemode.Headshots);
            WriteD(playerStatistic.Acemode.Assists);
            WriteD(playerStatistic.Acemode.Escapes);
            WriteD(playerStatistic.Acemode.Winstreaks);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteB(playerInventory.EquipmentData(playerEquipment.AccessoryId));
            WriteB(UnknownArrayData(3));
            WriteB(UnknownListData(3));
            WriteD(playerStatistic.Weapon.AssaultKills);
            WriteD(playerStatistic.Weapon.AssaultDeaths);
            WriteD(playerStatistic.Weapon.SmgKills);
            WriteD(playerStatistic.Weapon.SmgDeaths);
            WriteD(playerStatistic.Weapon.SniperKills);
            WriteD(playerStatistic.Weapon.SniperDeaths);
            WriteD(playerStatistic.Weapon.MachinegunKills);
            WriteD(playerStatistic.Weapon.MachinegunDeaths);
            WriteD(playerStatistic.Weapon.ShotgunKills);
            WriteD(playerStatistic.Weapon.ShotgunDeaths);
            WriteD(playerStatistic.Weapon.ShieldKills);
            WriteD(playerStatistic.Weapon.ShieldDeaths);
            WriteC((byte)characters.Count);
            WriteC((byte)NATIONS);
            WriteC((byte)0);
            WriteB(QuickstartData(quickstarts));
            WriteB(new byte[33]);
            WriteC((byte)4);
            WriteB(new byte[20]);
            WriteD(account.Title.Slots);
            WriteC((byte)3);
            WriteC((byte)account.Title.Equiped1);
            WriteC((byte)account.Title.Equiped2);
            WriteC((byte)account.Title.Equiped3);
            WriteQ(account.Title.Flags);
            WriteC((byte)160 /*0xA0*/);
            WriteB(account.Mission.List1);
            WriteB(account.Mission.List2);
            WriteB(account.Mission.List3);
            WriteB(account.Mission.List4);
            WriteC((byte)account.Mission.ActualMission);
            WriteC((byte)account.Mission.Card1);
            WriteC((byte)account.Mission.Card2);
            WriteC((byte)account.Mission.Card3);
            WriteC((byte)account.Mission.Card4);
            WriteB(ComDiv.GetMissionCardFlags(account.Mission.Mission1, account.Mission.List1));
            WriteB(ComDiv.GetMissionCardFlags(account.Mission.Mission2, account.Mission.List2));
            WriteB(ComDiv.GetMissionCardFlags(account.Mission.Mission3, account.Mission.List3));
            WriteB(ComDiv.GetMissionCardFlags(account.Mission.Mission4, account.Mission.List4));
            WriteC((byte)account.Mission.Mission1);
            WriteC((byte)account.Mission.Mission2);
            WriteC((byte)account.Mission.Mission3);
            WriteC((byte)account.Mission.Mission4);
            WriteD(account.MasterMedal);
            WriteD(account.Medal);
            WriteD(account.Ensign);
            WriteD(account.Ribbon);
            WriteD(0);
            WriteC((byte)0);
            WriteD(0);
            WriteC((byte)2);
            WriteB(new byte[406]);
            WriteB(AttendanceData(account, eventVisit));
            WriteC((byte)2);
            WriteD(0);
            WriteC((byte)0);
            WriteD(0);
            WriteB(CheckEventVisit(account, eventVisit, Date));
            WriteB(ComDiv.AddressBytes("127.0.0.1"));
            WriteD(Date);
            WriteC(characters.Count == 0 ? (byte)0 : (byte)account.Character.GetCharacter(playerEquipment.CharaRedId).Slot);
            WriteC(characters.Count == 0 ? (byte)1 : (byte)account.Character.GetCharacter(playerEquipment.CharaBlueId).Slot);
            WriteB(playerInventory.EquipmentData(playerEquipment.DinoItem));
            WriteB(playerInventory.EquipmentData(playerEquipment.SprayId));
            WriteB(playerInventory.EquipmentData(playerEquipment.NameCardId));
            WriteQ(AllUtils.LoadCouponEffects(account));
            WriteD(0);
            WriteC((byte)0);
            WriteT(account.PointUp());
            WriteT(account.ExpUp());
            WriteC((byte)0);
            WriteC((byte)account.NickColor);
            WriteD(account.Bonus.FakeRank);
            WriteD(account.Bonus.FakeRank);
            WriteU(account.Bonus.FakeNick, 66);
            WriteH((short)account.Bonus.CrosshairColor);
            WriteH((short)account.Bonus.MuzzleColor);
            WriteC((byte)account.Bonus.NickBorderColor);
            WriteD(playerStatistic.Season.Matches);
            WriteD(playerStatistic.Season.MatchWins);
            WriteD(playerStatistic.Season.MatchLoses);
            WriteD(playerStatistic.Season.MatchDraws);
            WriteD(playerStatistic.Season.KillsCount);
            WriteD(playerStatistic.Season.HeadshotsCount);
            WriteD(playerStatistic.Season.DeathsCount);
            WriteD(playerStatistic.Season.TotalMatchesCount);
            WriteD(playerStatistic.Season.TotalKillsCount);
            WriteD(playerStatistic.Season.EscapesCount);
            WriteD(playerStatistic.Season.AssistsCount);
            WriteD(playerStatistic.Season.MvpCount);
            WriteD(playerStatistic.Basic.Matches);
            WriteD(playerStatistic.Basic.MatchWins);
            WriteD(playerStatistic.Basic.MatchLoses);
            WriteD(playerStatistic.Basic.MatchDraws);
            WriteD(playerStatistic.Basic.KillsCount);
            WriteD(playerStatistic.Basic.HeadshotsCount);
            WriteD(playerStatistic.Basic.DeathsCount);
            WriteD(playerStatistic.Basic.TotalMatchesCount);
            WriteD(playerStatistic.Basic.TotalKillsCount);
            WriteD(playerStatistic.Basic.EscapesCount);
            WriteD(playerStatistic.Basic.AssistsCount);
            WriteD(playerStatistic.Basic.MvpCount);
            WriteU(account.Nickname, 66);
            WriteD(account.Rank);
            WriteD(account.GetRank());
            WriteD(account.Gold);
            WriteD(account.Exp);
            WriteD(0);
            WriteC(0);
            WriteQ(0);
            WriteC((byte)account.AuthLevel());
            WriteC(0);
            WriteD(account.Tags);
            WriteH(0);
            WriteD(Date);
            WriteH((ushort)account.InventoryPlus);
            WriteD(account.Cash);
            WriteD(clanModel.Id);
            WriteD(account.ClanAccess);
            WriteQ(account.StatusId());
            WriteC((byte)account.CafePC);
            WriteC((byte)account.TourneyLevel()); //CountryFlags);
            WriteU(clanModel.Name, 34);
            WriteC((byte)clanModel.Rank);
            WriteC((byte)clanModel.GetClanUnit());
            WriteD(clanModel.Logo);
            WriteC((byte)clanModel.NameColor);
            WriteC((byte)clanModel.Effect);
            WriteC(AuthXender.Client.Config.EnableBlood ? (byte)account.Age : (byte)42);
        }

        private byte[] CheckEventVisit(Account Player, EventVisitModel eventVisit, uint A_3)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                PlayerEvent playerEvent = Player.Event;
                if (eventVisit != null && playerEvent != null && eventVisit.EventIsEnabled())
                {
                    uint num1 = uint.Parse($"{DateTimeUtil.Convert($"{A_3}"):yyMMdd}");
                    uint num2 = uint.Parse($"{DateTimeUtil.Convert($"{playerEvent.LastVisitDate}"):yyMMdd}");
                    syncServerPacket.WriteD(eventVisit.Id);
                    syncServerPacket.WriteC((byte)playerEvent.LastVisitCheckDay);
                    syncServerPacket.WriteC((byte)(playerEvent.LastVisitCheckDay - 1));
                    syncServerPacket.WriteC(num2 < num1 ? (byte)1 : (byte)2);
                    syncServerPacket.WriteC((byte)playerEvent.LastVisitSeqType);
                    syncServerPacket.WriteC((byte)1);
                }
                else
                {
                    syncServerPacket.WriteB(new byte[9]);
                }
                return syncServerPacket.ToArray();
            }
        }

        private byte[] AttendanceData(Account Player, EventVisitModel eventVisit)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                PlayerEvent playerEvent = Player.Event;
                if (eventVisit != null && eventVisit.EventIsEnabled())
                {
                    EventVisitModel eventVisitModel = EventVisitXML.GetEvent(eventVisit.Id + 1);
                    syncServerPacket.WriteU(eventVisit.Title, 70);
                    syncServerPacket.WriteC((byte)playerEvent.LastVisitCheckDay);
                    syncServerPacket.WriteC((byte)eventVisit.Checks);
                    syncServerPacket.WriteD(eventVisit.Id);
                    syncServerPacket.WriteD(eventVisit.BeginDate);
                    syncServerPacket.WriteD(eventVisit.EndedDate);
                    syncServerPacket.WriteD(eventVisitModel != null ? eventVisitModel.BeginDate : 0U);
                    syncServerPacket.WriteD(eventVisitModel != null ? eventVisitModel.EndedDate : 0U);
                    syncServerPacket.WriteD(0);
                    for (int index = 0; index < 31 /*0x1F*/; ++index)
                    {
                        VisitBoxModel box = eventVisit.Boxes[index];
                        syncServerPacket.WriteC(box.IsBothReward ? (byte)1 : (byte)0);
                        syncServerPacket.WriteC((byte)box.RewardCount);
                        syncServerPacket.WriteD(box.Reward1.GoodId);
                        syncServerPacket.WriteD(box.Reward2.GoodId);
                    }
                }
                else
                {
                    syncServerPacket.WriteB(new byte[406]);
                }
                return syncServerPacket.ToArray();
            }
        }

        private byte[] QuickstartData(List<QuickstartModel> Player)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)Player.Count);
                foreach (QuickstartModel quickstartModel in Player)
                {
                    syncServerPacket.WriteC((byte)quickstartModel.MapId);
                    syncServerPacket.WriteC((byte)quickstartModel.Rule);
                    syncServerPacket.WriteC((byte)quickstartModel.StageOptions);
                    syncServerPacket.WriteC((byte)quickstartModel.Type);
                }
                return syncServerPacket.ToArray();
            }
        }

        private byte[] UnknownListData(int Player)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)Player);
                for (int index = 0; index < Player; ++index)
                {
                    syncServerPacket.WriteC(0);
                    syncServerPacket.WriteC(3);
                    syncServerPacket.WriteB(new byte[43]);
                }
                return syncServerPacket.ToArray();
            }
        }

        private byte[] UnknownArrayData(int Player)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)Player);
                for (int index = 0; index < Player; ++index)
                {
                    syncServerPacket.WriteB(new byte[45]);
                }
                return syncServerPacket.ToArray();
            }
        }
    }
}