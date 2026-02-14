// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CHAR_CREATE_CHARA_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Models;
using Server.Game.Data.Models;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CHAR_CREATE_CHARA_ACK : GameServerPacket
    {
        private readonly uint Field0;
        private readonly Account Field1;
        private readonly CharacterModel Field2;
        private readonly PlayerInventory Field3;
        private readonly PlayerEquipment Field4;
        private readonly byte Field5;

        public PROTOCOL_CHAR_CREATE_CHARA_ACK(uint A_1, byte A_2, CharacterModel A_3, Account A_4)
        {
            this.Field0 = A_1;
            this.Field1 = A_4;
            if (A_4 != null)
            {
                this.Field3 = A_4.Inventory;
                this.Field4 = A_4.Equipment;
            }
            this.Field2 = A_3;
            this.Field5 = A_2;
        }

        public override void Write()
        {
            this.WriteH((short)6146);
            this.WriteH((short)0);
            this.WriteC((byte)0);
            this.WriteD(this.Field0);
            if (this.Field0 == 0)
            {
                this.WriteB(this.Field3.EquipmentData(this.Field4.WeaponPrimary));
                this.WriteB(this.Field3.EquipmentData(this.Field4.WeaponSecondary));
                this.WriteB(this.Field3.EquipmentData(this.Field4.WeaponMelee));
                this.WriteB(this.Field3.EquipmentData(this.Field4.WeaponExplosive));
                this.WriteB(this.Field3.EquipmentData(this.Field4.WeaponSpecial));
                this.WriteB(this.Field3.EquipmentData(this.Field2.Id));
                this.WriteB(this.Field3.EquipmentData(this.Field4.PartHead));
                this.WriteB(this.Field3.EquipmentData(this.Field4.PartFace));
                this.WriteB(this.Field3.EquipmentData(this.Field4.PartJacket));
                this.WriteB(this.Field3.EquipmentData(this.Field4.PartPocket));
                this.WriteB(this.Field3.EquipmentData(this.Field4.PartGlove));
                this.WriteB(this.Field3.EquipmentData(this.Field4.PartBelt));
                this.WriteB(this.Field3.EquipmentData(this.Field4.PartHolster));
                this.WriteB(this.Field3.EquipmentData(this.Field4.PartSkin));
                this.WriteB(this.Field3.EquipmentData(this.Field4.BeretItem));
                this.WriteD(this.Field1.Cash);
                this.WriteD(this.Field1.Gold);
                this.WriteC(this.Field5);
                this.WriteC((byte)20);
                this.WriteC((byte)this.Field2.Slot);
                this.WriteC((byte)1);
            }
        }
    }
}