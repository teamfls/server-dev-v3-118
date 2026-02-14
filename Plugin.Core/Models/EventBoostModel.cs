// Decompiled with JetBrains decompiler
// Type: Plugin.Core.Models.EventBoostModel
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using Plugin.Core.Utility;
using System.Runtime.CompilerServices;

namespace Plugin.Core.Models
{
    public class EventBoostModel
    {
        public int Id { get; set; }

        public int BonusExp { get; set; }

        public int BonusGold { get; set; }

        public int Percent { get; set; }

        public uint BeginDate { get; set; }

        public uint EndedDate { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool Period { get; set; }

        public bool Priority { get; set; }

        public PortalBoostEvent BoostType { get; set; }

        public int BoostValue { get; set; }

        public EventBoostModel()
        {
            this.Name = "";
            this.Description = "";
        }


        public bool EventIsEnabled()
        {
            uint num = uint.Parse(DateTimeUtil.Now("yyMMddHHmm"));
            return this.BeginDate <= num && num < this.EndedDate;
        }
    }
}