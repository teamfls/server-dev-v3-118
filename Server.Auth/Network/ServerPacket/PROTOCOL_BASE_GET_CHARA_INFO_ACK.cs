// Decompiled with JetBrains decompiler
// Type: Server.Auth.Network.ServerPacket.PROTOCOL_BASE_GET_CHARA_INFO_ACK
// Assembly: Server.Auth, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: D2254E5E-B0BA-4DE9-9720-2DDECE3CD4EF
// Assembly location: C:\Users\home\Desktop\dll\Server.Auth-deobfuscated-Cleaned.dll

using Plugin.Core.Models;
using Server.Auth.Data.Models;
using System.Collections.Generic;


namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_BASE_GET_CHARA_INFO_ACK : AuthServerPacket
    {
        private readonly PlayerInventory Field0;
        private readonly PlayerEquipment Field1;
        private readonly List<CharacterModel> Field2;

        public PROTOCOL_BASE_GET_CHARA_INFO_ACK(Account A_1)
        {
            this.Field0 = A_1.Inventory;
            this.Field1 = A_1.Equipment;
            this.Field2 = A_1.Character.Characters;
        }

        public override void Write()
        {
            this.WriteH((short)2453);
            this.WriteH((short)0);
            this.WriteC((byte)this.Field2.Count);
            foreach (CharacterModel characterModel in this.Field2)
            {
                this.WriteC((byte)characterModel.Slot);
                this.WriteC((byte)20);
                this.WriteB(this.Field0.EquipmentData(characterModel.Id));
            }
            foreach (CharacterModel characterModel in this.Field2)
            {
                this.WriteB(this.Field0.EquipmentData(this.Field1.WeaponPrimary));
                this.WriteB(this.Field0.EquipmentData(this.Field1.WeaponSecondary));
                this.WriteB(this.Field0.EquipmentData(this.Field1.WeaponMelee));
                this.WriteB(this.Field0.EquipmentData(this.Field1.WeaponExplosive));
                this.WriteB(this.Field0.EquipmentData(this.Field1.WeaponSpecial));
                this.WriteB(this.Field0.EquipmentData(characterModel.Id));
                this.WriteB(this.Field0.EquipmentData(this.Field1.PartHead));
                this.WriteB(this.Field0.EquipmentData(this.Field1.PartFace));
                this.WriteB(this.Field0.EquipmentData(this.Field1.PartJacket));
                this.WriteB(this.Field0.EquipmentData(this.Field1.PartPocket));
                this.WriteB(this.Field0.EquipmentData(this.Field1.PartGlove));
                this.WriteB(this.Field0.EquipmentData(this.Field1.PartBelt));
                this.WriteB(this.Field0.EquipmentData(this.Field1.PartHolster));
                this.WriteB(this.Field0.EquipmentData(this.Field1.PartSkin));
                this.WriteB(this.Field0.EquipmentData(this.Field1.BeretItem));
            }
        }

    }
}