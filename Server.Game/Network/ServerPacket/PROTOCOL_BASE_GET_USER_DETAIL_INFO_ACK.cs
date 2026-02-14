using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Network;
using Plugin.Core.Utility;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using System;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_BASE_GET_USER_DETAIL_INFO_ACK : GameServerPacket
    {
        private const ushort PROTOCOL_ID = 2438;
        private const int NICKNAME_LENGTH = 66;
        private const int CLAN_NAME_LENGTH = 34;

        private readonly uint _errorCode;
        private readonly Account _account;
        private readonly PlayerStatistic _statistic;
        private readonly PlayerInventory _inventory;
        private readonly PlayerEquipment _equipment;
        private readonly StatisticSeason _seasonStats;
        private readonly StatisticWeapon _weaponStats;
        private readonly StatisticAcemode _acemodeStats;
        private readonly StatisticBattlecup _battlecupStats;
        private readonly int _characterDisplayId;

        public PROTOCOL_BASE_GET_USER_DETAIL_INFO_ACK(uint error, Account account, int characterId)
        {
            _errorCode = error;
            _account = account;

            if (account != null)
            {
                _statistic = account.Statistic ?? new PlayerStatistic();
                _inventory = account.Inventory ?? new PlayerInventory();
                _equipment = account.Equipment ?? new PlayerEquipment();
                _seasonStats = _statistic.Season ?? new StatisticSeason();
                _weaponStats = _statistic.Weapon ?? new StatisticWeapon();
                _acemodeStats = _statistic.Acemode ?? new StatisticAcemode();
                _battlecupStats = _statistic.Battlecup ?? new StatisticBattlecup();
                _characterDisplayId = characterId == int.MaxValue ? _equipment.CharaRedId : characterId;
            }
        }

        public override void Write()
        {
            try
            {
                WritePacketHeader();
                WriteD(_errorCode);

                if (_errorCode != 0)
                    return;

                WritePlayerBasicInfo();
                WriteSeasonStatistics();
                WritePaddingAndStatus();
                WritePlayerCustomization();
                WriteEquipment();
                WriteWeaponStatistics();
                WriteCharacterInfo();
                WriteAcemodeStatistics();
                WriteBattlecupStatistics();
            }
            catch (Exception ex)
            {
                CLogger.Print($"PROTOCOL_BASE_GET_USER_DETAIL_INFO_ACK: {ex.Message}", LoggerType.Error, ex);
            }
        }

        private void WritePacketHeader()
        {
            WriteH(PROTOCOL_ID);
            WriteH(0);
        }

        private void WritePlayerBasicInfo()
        {
            WriteQ(_account.PlayerId);
            WriteU(_account.Nickname, NICKNAME_LENGTH);
            WriteB(GetClanData(_account));
            WriteC((byte)_account.GetRank());
            WriteD(_account.Exp);
        }

        private void WriteSeasonStatistics()
        {
            WriteD(_seasonStats.Matches);
            WriteD(_seasonStats.MatchWins);
            WriteD(_seasonStats.MatchDraws);
            WriteD(_seasonStats.MatchLoses);
            WriteD(_seasonStats.EscapesCount);
            WriteD(_seasonStats.KillsCount);
            WriteD(_seasonStats.DeathsCount);
            WriteD(_seasonStats.HeadshotsCount);
            WriteD(_seasonStats.AssistsCount);
            WriteD(_seasonStats.MvpCount);
        }

        private void WritePaddingAndStatus()
        {
            WriteB(new byte[45]);
            WriteD(1000);
            WriteB(new byte[82]);
            WriteD(0);
            WriteB(new byte[128]);
            WriteC(5);
            WriteC(1);
            WriteC(0);
            WriteD(ComDiv.GetPlayerStatus(_account.Status, _account.IsOnline));
            WriteB(GetPlayerLocationData(_account));
            WriteC(8);
            WriteC(0);
            WriteC(7);
            WriteC(0);
            WriteD(52);
            WriteC(1);
            WriteD(0);
            WriteD(0);
            WriteD(0);
            WriteC(0);
        }

        private void WritePlayerCustomization()
        {
            WriteC((byte)_account.NickColor);
            WriteD(_account.Bonus.FakeRank);
            WriteD(_account.Bonus.FakeRank);
            WriteU(_account.Bonus.FakeNick, NICKNAME_LENGTH);
            WriteH((short)_account.Bonus.CrosshairColor);
            WriteH((short)_account.Bonus.MuzzleColor);
            WriteC((byte)_account.Bonus.NickBorderColor);
            WriteC(2);
            WriteC(0);
        }

        private void WriteEquipment()
        {
            // Weapons
            WriteB(_inventory.EquipmentData(_equipment.WeaponPrimary));
            WriteB(_inventory.EquipmentData(_equipment.WeaponSecondary));
            WriteB(_inventory.EquipmentData(_equipment.WeaponMelee));
            WriteB(_inventory.EquipmentData(_equipment.WeaponExplosive));
            WriteB(_inventory.EquipmentData(_equipment.WeaponSpecial));
            WriteB(_inventory.EquipmentData(_characterDisplayId));

            // Equipment Parts
            WriteB(_inventory.EquipmentData(_equipment.PartHead));
            WriteB(_inventory.EquipmentData(_equipment.PartFace));
            WriteB(_inventory.EquipmentData(_equipment.PartJacket));
            WriteB(_inventory.EquipmentData(_equipment.PartPocket));
            WriteB(_inventory.EquipmentData(_equipment.PartGlove));
            WriteB(_inventory.EquipmentData(_equipment.PartBelt));
            WriteB(_inventory.EquipmentData(_equipment.PartHolster));
            WriteB(_inventory.EquipmentData(_equipment.PartSkin));
            WriteB(_inventory.EquipmentData(_equipment.BeretItem));
            WriteC(0);
        }

        private void WriteWeaponStatistics()
        {
            WriteD(_equipment.CharaRedId);
            WriteD(_equipment.CharaBlueId);
            WriteB(new byte[631]);

            WriteD(_weaponStats.AssaultKills);
            WriteD(_weaponStats.AssaultDeaths);
            WriteD(_weaponStats.SmgKills);
            WriteD(_weaponStats.SmgDeaths);
            WriteD(_weaponStats.SniperKills);
            WriteD(_weaponStats.SniperDeaths);
            WriteD(_weaponStats.MachinegunKills);
            WriteD(_weaponStats.MachinegunDeaths);
            WriteD(_weaponStats.ShotgunKills);
            WriteD(_weaponStats.ShotgunDeaths);
            WriteD(_weaponStats.ShieldKills);
            WriteD(_weaponStats.ShieldDeaths);
        }

        private void WriteCharacterInfo()
        {
            WriteD(_equipment.CharaRedId);
            WriteD(_equipment.CharaBlueId);
            WriteC(0);
            WriteD(16);
            WriteD(_equipment.NameCardId);
            WriteC(0);
            WriteB(_inventory.EquipmentData(_equipment.SprayId));
        }

        private void WriteAcemodeStatistics()
        {
            WriteD(_acemodeStats.Matches);
            WriteD(_acemodeStats.MatchWins);
            WriteD(_acemodeStats.MatchLoses);
            WriteD(_acemodeStats.Kills);
            WriteD(_acemodeStats.Deaths);
            WriteD(_acemodeStats.Headshots);
            WriteD(_acemodeStats.Assists);
            WriteD(_acemodeStats.Escapes);
            WriteD(_acemodeStats.Winstreaks);
        }

        private void WriteBattlecupStatistics()
        {
            WriteD(_battlecupStats.Matches);
            WriteD(_statistic.GetBCWinRatio());
            WriteD(_battlecupStats.MatchLoses);
            WriteD(_battlecupStats.KillsCount);
            WriteD(_battlecupStats.DeathsCount);
            WriteD(_battlecupStats.HeadshotsCount);
            WriteD(_battlecupStats.AssistsCount);
            WriteD(_battlecupStats.EscapesCount);
            WriteD(_statistic.GetBCKDRatio());
            WriteD(_battlecupStats.MatchWins);
            WriteD(_battlecupStats.AverageDamage);
            WriteD(_battlecupStats.PlayTime);
        }

        private byte[] GetClanData(Account account)
        {
            using (SyncServerPacket packet = new SyncServerPacket())
            {
                ClanModel clan = ClanManager.GetClan(account.ClanId);
                if (clan == null)
                {
                    packet.WriteU(string.Empty, CLAN_NAME_LENGTH);
                    packet.WriteD(0);
                    packet.WriteC(0);
                }
                else
                {
                    packet.WriteU(clan.Name, CLAN_NAME_LENGTH);
                    packet.WriteD(clan.Logo);
                    packet.WriteC((byte)clan.Effect);
                }
                return packet.ToArray();
            }
        }

        private byte[] GetPlayerLocationData(Account account)
        {
            if (account?.Status == null)
                return new byte[4] { 3, 0, 0, 0 };

            ComDiv.GetPlayerLocation(
                account.Status,
                account.IsOnline,
                out FriendState friendState,
                out int channelId,
                out int roomId,
                out int serverId
            );

            using (SyncServerPacket packet = new SyncServerPacket())
            {
                packet.WriteC((byte)friendState);
                packet.WriteC((byte)serverId);
                packet.WriteC((byte)roomId);
                packet.WriteC((byte)channelId);
                return packet.ToArray();
            }
        }
    }
}