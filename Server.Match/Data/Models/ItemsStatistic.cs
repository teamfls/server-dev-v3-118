// Decompiled with JetBrains decompiler
// Type: Server.Match.Data.Models.ItemsStatistic
// Assembly: Server.Match, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: CE18A1E1-67C7-4FA9-8510-2DD553448D5A
// Assembly location: C:\Users\home\Desktop\dll\Server.Match-deobfuscated-Cleaned.dll


namespace Server.Match.Data.Models
{
    public class ItemsStatistic
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Damage { get; set; }

        public int HelmetPenetrate { get; set; }

        public int BulletLoaded { get; set; }

        public int BulletTotal { get; set; }

        public float FireDelay { get; set; }

        public float Range { get; set; }
    }
}