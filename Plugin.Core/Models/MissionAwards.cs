// Decompiled with JetBrains decompiler
// Type: Plugin.Core.Models.MissionAwards
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

namespace Plugin.Core.Models
{
    public class MissionAwards
    {
        public int Id { get; set; }

        public int MasterMedal { get; set; }

        public int Exp { get; set; }

        public int Gold { get; set; }

        public MissionAwards(int A_1, int A_2, int A_3, int A_4)
        {
            this.Id = A_1;
            this.MasterMedal = A_2;
            this.Exp = A_3;
            this.Gold = A_4;
        }
    }
}
