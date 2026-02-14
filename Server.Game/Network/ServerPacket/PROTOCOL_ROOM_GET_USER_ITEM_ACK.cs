// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_ROOM_GET_USER_ITEM_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Network;
using Server.Game.Data.Models;
using System.Collections.Generic;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_ROOM_GET_USER_ITEM_ACK : GameServerPacket
    {
        private readonly Account Field0;
        private readonly PlayerInventory Field1;
        private readonly PlayerEquipment Field2;
        private readonly List<ItemsModel> Field3;

        public PROTOCOL_ROOM_GET_USER_ITEM_ACK(Account A_1)
        {
            this.Field0 = A_1;
            if (A_1 == null)
                return;
            this.Field1 = A_1.Inventory;
            this.Field2 = A_1.Equipment;
            this.Field3 = this.Field1.GetItemsByType(ItemCategory.Coupon);
        }

        public override void Write()
        {
            this.WriteH((short)3646);
            this.WriteH((short)0);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteB(this.Field1.EquipmentData(this.Field2.DinoItem));
            this.WriteB(this.Field1.EquipmentData(this.Field2.SprayId));
            this.WriteB(this.Field1.EquipmentData(this.Field2.NameCardId));
            this.WriteB(this.Field1.EquipmentData(this.Field2.AccessoryId));
            this.WriteB(this.Field1.EquipmentData(this.Field2.WeaponPrimary));
            this.WriteB(this.Field1.EquipmentData(this.Field2.WeaponSecondary));
            this.WriteB(this.Field1.EquipmentData(this.Field2.WeaponMelee));
            this.WriteB(this.Field1.EquipmentData(this.Field2.WeaponExplosive));
            this.WriteB(this.Field1.EquipmentData(this.Field2.WeaponSpecial));
            this.WriteB(this.Field1.EquipmentData(this.Field2.CharaRedId));
            this.WriteB(this.Field1.EquipmentData(this.Field2.CharaBlueId));
            this.WriteB(this.Field1.EquipmentData(this.Field2.PartHead));
            this.WriteB(this.Field1.EquipmentData(this.Field2.PartFace));
            this.WriteB(this.Field1.EquipmentData(this.Field2.BeretItem));
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