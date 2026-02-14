// Decompiled with JetBrains decompiler
// Type: Plugin.Core.Models.PlayerMissions
// Assembly: Plugin.Core, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: DEEC7026-C3BC-4ECF-BBAB-B23BF4490042
// Assembly location: C:\Users\home\Desktop\dll\Plugin.Core-deobfuscated-Cleaned.dll

using Plugin.Core.Utility;

namespace Plugin.Core.Models
{
    public class PlayerMissions
    {
        public long OwnerId { get; set; }

        public byte[] List1 { get; set; }

        public byte[] List2 { get; set; }

        public byte[] List3 { get; set; }

        public byte[] List4 { get; set; }

        public int ActualMission { get; set; }

        public int Card1 { get; set; }

        public int Card2 { get; set; }

        public int Card3 { get; set; }

        public int Card4 { get; set; }

        public int Mission1 { get; set; }

        public int Mission2 { get; set; }

        public int Mission3 { get; set; }

        public int Mission4 { get; set; }

        public bool SelectedCard { get; set; }

        public PlayerMissions()
        {
            this.List1 = new byte[40];
            this.List2 = new byte[40];
            this.List3 = new byte[40];
            this.List4 = new byte[40];
        }

        public byte[] GetCurrentMissionList()
        {
            if (this.ActualMission == 0)
                return this.List1;
            if (this.ActualMission == 1)
                return this.List2;
            return this.ActualMission != 2 ? this.List4 : this.List3;
        }

        public int GetCurrentCard() => this.GetCard(this.ActualMission);

        public int GetCard(int Index)
        {
            switch (Index)
            {
                case 0:
                    return this.Card1;
                case 1:
                    return this.Card2;
                case 2:
                    return this.Card3;
                default:
                    return this.Card4;
            }
        }

        public int GetCurrentMissionId()
        {
            if (this.ActualMission == 0)
                return this.Mission1;
            if (this.ActualMission == 1)
                return this.Mission2;
            return this.ActualMission != 2 ? this.Mission4 : this.Mission3;
        }

        public void UpdateSelectedCard()
        {
            int currentCard = this.GetCurrentCard();
            if (ushort.MaxValue != ComDiv.GetMissionCardFlags(this.GetCurrentMissionId(), currentCard, this.GetCurrentMissionList()))
                return;
            this.SelectedCard = true;
        }
    }
}