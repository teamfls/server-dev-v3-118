using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Network;
using Plugin.Core.Utility;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using System.Runtime.CompilerServices;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_ROOM_GET_PLAYERINFO_ACK : GameServerPacket
    {
        private readonly Account Field0;
        private readonly PlayerInventory Field1;
        private readonly PlayerEquipment Field2;
        private readonly ClanModel Field3;

        public PROTOCOL_ROOM_GET_PLAYERINFO_ACK(Account A_1)
        {
            this.Field0 = A_1;
            if (A_1 == null)
                return;
            this.Field1 = A_1.Inventory;
            this.Field2 = A_1.Equipment;
            this.Field3 = ClanManager.GetClan(A_1.ClanId);
        }

        public override void Write()
        {
            this.WriteH((short)3597);
            this.WriteD(0);
            this.WriteD(0);
            this.WriteD(0);
            this.WriteH((short)0);
            this.WriteD(this.Field0.Statistic.Season.Matches);
            this.WriteD(this.Field0.Statistic.Season.MatchWins);
            this.WriteD(this.Field0.Statistic.Season.MatchLoses);
            this.WriteD(this.Field0.Statistic.Season.MatchDraws);
            this.WriteD(this.Field0.Statistic.Season.KillsCount);
            this.WriteD(this.Field0.Statistic.Season.HeadshotsCount);
            this.WriteD(this.Field0.Statistic.Season.DeathsCount);
            this.WriteD(this.Field0.Statistic.Season.TotalMatchesCount);
            this.WriteD(this.Field0.Statistic.Season.TotalKillsCount);
            this.WriteD(this.Field0.Statistic.Season.EscapesCount);
            this.WriteD(this.Field0.Statistic.Season.AssistsCount);
            this.WriteD(this.Field0.Statistic.Season.MvpCount);
            this.WriteD(this.Field0.Statistic.Basic.Matches);
            this.WriteD(this.Field0.Statistic.Basic.MatchWins);
            this.WriteD(this.Field0.Statistic.Basic.MatchLoses);
            this.WriteD(this.Field0.Statistic.Basic.MatchDraws);
            this.WriteD(this.Field0.Statistic.Basic.KillsCount);
            this.WriteD(this.Field0.Statistic.Basic.HeadshotsCount);
            this.WriteD(this.Field0.Statistic.Basic.DeathsCount);
            this.WriteD(this.Field0.Statistic.Basic.TotalMatchesCount);
            this.WriteD(this.Field0.Statistic.Basic.TotalKillsCount);
            this.WriteD(this.Field0.Statistic.Basic.EscapesCount);
            this.WriteD(this.Field0.Statistic.Basic.AssistsCount);
            this.WriteD(this.Field0.Statistic.Basic.MvpCount);
            this.WriteC((byte)3);
            this.WriteB(this.Field1.EquipmentData(this.Field2.DinoItem));
            this.WriteB(this.Field1.EquipmentData(this.Field2.SprayId));
            this.WriteB(this.Field1.EquipmentData(this.Field2.NameCardId));
            this.WriteB(this.Field1.EquipmentData(this.Field2.WeaponPrimary));
            this.WriteB(this.Field1.EquipmentData(this.Field2.WeaponSecondary));
            this.WriteB(this.Field1.EquipmentData(this.Field2.WeaponMelee));
            this.WriteB(this.Field1.EquipmentData(this.Field2.WeaponExplosive));
            this.WriteB(this.Field1.EquipmentData(this.Field2.WeaponSpecial));
            this.WriteB(this.Method0(this.Field0, this.Field2));
            this.WriteB(this.Field1.EquipmentData(this.Field2.PartHead));
            this.WriteB(this.Field1.EquipmentData(this.Field2.PartFace));
            this.WriteB(this.Field1.EquipmentData(this.Field2.PartJacket));
            this.WriteB(this.Field1.EquipmentData(this.Field2.PartPocket));
            this.WriteB(this.Field1.EquipmentData(this.Field2.PartGlove));
            this.WriteB(this.Field1.EquipmentData(this.Field2.PartBelt));
            this.WriteB(this.Field1.EquipmentData(this.Field2.PartHolster));
            this.WriteB(this.Field1.EquipmentData(this.Field2.PartSkin));
            this.WriteB(this.Field1.EquipmentData(this.Field2.BeretItem));
            this.WriteD(590851);
            this.WriteU(this.Field0.Nickname, 66);
            this.WriteD(this.Field0.GetRank());
            this.WriteD(this.Field0.Rank);
            this.WriteD(this.Field0.Gold);
            this.WriteD(this.Field0.Exp);
            this.WriteD(0);
            this.WriteC((byte)0);
            this.WriteQ(0L);
            this.WriteC((byte)this.Field0.AuthLevel());
            this.WriteC((byte)0);
            this.WriteD(this.Field0.Tags);
            this.WriteH((short)0);
            this.WriteD(uint.Parse(DateTimeUtil.Now("yyMMddHHmm")));
            this.WriteH((ushort)this.Field0.InventoryPlus);
            this.WriteD(this.Field0.Cash);
            this.WriteD(this.Field3.Id);
            this.WriteD(this.Field0.ClanAccess);
            this.WriteQ(this.Field0.StatusId());
            this.WriteC((byte)this.Field0.CafePC);
            this.WriteC((byte)this.Field0.TourneyLevel()); //CountryFlags);
            //this.WriteC((byte)this.Field0.CountryFlags);//CountryFlags);
            this.WriteU(this.Field3.Name, 34);
            this.WriteC((byte)this.Field3.Rank);
            this.WriteC((byte)this.Field3.GetClanUnit());
            this.WriteD(this.Field3.Logo);
            this.WriteC((byte)this.Field3.NameColor);
            this.WriteC((byte)this.Field3.Effect);
            this.WriteC(GameXender.Client.Config.EnableBlood ? (byte)this.Field0.Age : (byte)24);
        }

        private byte[] Method0(Account A_1, PlayerEquipment A_2)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                RoomModel room = A_1.Room;
                SlotModel Slot;
                if (room != null && room.GetSlot(A_1.SlotId, out Slot))
                {
                    int ItemId = room.ValidateTeam(Slot.Team, Slot.CostumeTeam) == TeamEnum.FR_TEAM ? A_2.CharaRedId : A_2.CharaBlueId;
                    syncServerPacket.WriteB(this.Field1.EquipmentData(ItemId));
                }
                return syncServerPacket.ToArray();
            }
        }
    }
}