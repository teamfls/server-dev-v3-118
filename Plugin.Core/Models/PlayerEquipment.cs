// Decompiled with JetBrains decompiler
// Type: Plugin.Core.Models.PlayerEquipment
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

namespace Plugin.Core.Models
{
    public class PlayerEquipment
    {
        public long OwnerId { get; set; }

        public int WeaponPrimary { get; set; }

        public int WeaponSecondary { get; set; }

        public int WeaponMelee { get; set; }

        public int WeaponExplosive { get; set; }

        public int WeaponSpecial { get; set; }

        public int CharaRedId { get; set; }

        public int CharaBlueId { get; set; }

        public int PartHead { get; set; }

        public int PartFace { get; set; }

        public int PartJacket { get; set; }

        public int PartPocket { get; set; }

        public int PartGlove { get; set; }

        public int PartBelt { get; set; }

        public int PartHolster { get; set; }

        public int PartSkin { get; set; }

        public int BeretItem { get; set; }

        public int DinoItem { get; set; }

        public int AccessoryId { get; set; }

        public int SprayId { get; set; }

        public int NameCardId { get; set; }

        public PlayerEquipment()
        {
            this.WeaponPrimary = 103004;
            this.WeaponSecondary = 202003;
            this.WeaponMelee = 301001;
            this.WeaponExplosive = 407001;
            this.WeaponSpecial = 508001;
            this.CharaRedId = 601001;
            this.CharaBlueId = 602002;
            this.PartHead = 1000700000;
            this.PartFace = 1000800000;
            this.PartJacket = 1000900000;
            this.PartPocket = 1001000000;
            this.PartGlove = 1001100000;
            this.PartBelt = 1001200000;
            this.PartHolster = 1001300000;
            this.PartSkin = 1001400000;
            this.DinoItem = 1500511;
        }
    }
}