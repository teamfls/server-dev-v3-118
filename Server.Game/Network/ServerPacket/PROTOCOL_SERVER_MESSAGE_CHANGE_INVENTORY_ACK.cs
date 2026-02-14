// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_SERVER_MESSAGE_CHANGE_INVENTORY_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using Plugin.Core.Models;
using Server.Game.Data.Models;
using System.Collections.Generic;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_SERVER_MESSAGE_CHANGE_INVENTORY_ACK : GameServerPacket
    {
        private readonly PlayerInventory Field0;
        private readonly PlayerCharacters Field1;
        private readonly PlayerEquipment Field2;
        private readonly PlayerEquipment Field3;
        private readonly List<ItemsModel> Field4;
        private readonly List<int> Field5;
        private readonly List<int> Field6;
        private readonly int Field7;

        public PROTOCOL_SERVER_MESSAGE_CHANGE_INVENTORY_ACK(Account A_1, SlotModel A_2)
        {
            this.Field6 = new List<int>();
            this.Field5 = new List<int>();
            if (A_1 == null)
                return;
            this.Field3 = A_1.Equipment;
            this.Field0 = A_1.Inventory;
            this.Field1 = A_1.Character;
            this.Field4 = A_1.Inventory.GetItemsByType(ItemCategory.Coupon);
            RoomModel room = A_1.Room;
            if (room == null || A_2 == null)
                return;
            this.Field2 = A_2.Equipment;
            if (this.Field2.CharaRedId != this.Field3.CharaRedId)
                this.Field6.Add(this.Field1.GetCharacter(this.Field3.CharaRedId).Slot);
            if (this.Field2.CharaBlueId != this.Field3.CharaBlueId)
                this.Field6.Add(this.Field1.GetCharacter(this.Field3.CharaBlueId).Slot);
            this.Field7 = room.ValidateTeam(A_2.Team, A_2.CostumeTeam) == TeamEnum.FR_TEAM ? this.Field3.CharaRedId : this.Field3.CharaBlueId;
            if (this.Field2.DinoItem != this.Field3.DinoItem)
                this.Field5.Add(this.Field3.DinoItem);
            if (this.Field2.SprayId != this.Field3.SprayId)
                this.Field5.Add(this.Field3.SprayId);
            if (this.Field2.NameCardId == this.Field3.NameCardId)
                return;
            this.Field5.Add(this.Field3.NameCardId);
        }

        public override void Write()
        {
            this.WriteH((short)3082);
            this.WriteH((short)0);
            this.WriteC((byte)this.Field6.Count);
            foreach (byte num in this.Field6)
            {
                this.WriteC(num);
                this.WriteC((byte)0);
                this.WriteC((byte)0);
                this.WriteC((byte)0);
            }
            this.WriteC((byte)0);
            this.WriteB(this.Field0.EquipmentData(this.Field3.AccessoryId));
            this.WriteC((byte)this.Field4.Count);
            foreach (ItemsModel itemsModel in this.Field4)
                this.WriteD(itemsModel.Id);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteC((byte)0);
            this.WriteB(this.Field0.EquipmentData(this.Field3.WeaponPrimary));
            this.WriteB(this.Field0.EquipmentData(this.Field3.WeaponSecondary));
            this.WriteB(this.Field0.EquipmentData(this.Field3.WeaponMelee));
            this.WriteB(this.Field0.EquipmentData(this.Field3.WeaponExplosive));
            this.WriteB(this.Field0.EquipmentData(this.Field3.WeaponSpecial));
            this.WriteB(this.Field0.EquipmentData(this.Field7));
            this.WriteB(this.Field0.EquipmentData(this.Field3.PartHead));
            this.WriteB(this.Field0.EquipmentData(this.Field3.PartFace));
            this.WriteB(this.Field0.EquipmentData(this.Field3.PartJacket));
            this.WriteB(this.Field0.EquipmentData(this.Field3.PartPocket));
            this.WriteB(this.Field0.EquipmentData(this.Field3.PartGlove));
            this.WriteB(this.Field0.EquipmentData(this.Field3.PartBelt));
            this.WriteB(this.Field0.EquipmentData(this.Field3.PartHolster));
            this.WriteB(this.Field0.EquipmentData(this.Field3.PartSkin));
            this.WriteB(this.Field0.EquipmentData(this.Field3.BeretItem));
            this.WriteC((byte)this.Field5.Count);
            foreach (int ItemId in this.Field5)
                this.WriteB(this.Field0.EquipmentData(ItemId));
        }
    }
}