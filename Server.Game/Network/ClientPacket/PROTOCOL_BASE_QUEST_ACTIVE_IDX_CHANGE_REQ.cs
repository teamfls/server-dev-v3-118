// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_BASE_QUEST_ACTIVE_IDX_CHANGE_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using System;
using System.Runtime.CompilerServices;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_BASE_QUEST_ACTIVE_IDX_CHANGE_REQ : GameClientPacket
    {
        private int Field0;
        private int Field1;
        private int Field2;

        public override void Read()
        {
            this.Field1 = (int)this.ReadC();
            this.Field0 = (int)this.ReadC();
            this.Field2 = (int)this.ReadUH();
        }

        
        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null)
                    return;
                DBQuery dbQuery = new DBQuery();
                PlayerMissions mission = player.Mission;
                if (mission.GetCard(this.Field1) != this.Field0)
                {
                    if (this.Field1 == 0)
                        mission.Card1 = this.Field0;
                    else if (this.Field1 == 1)
                        mission.Card2 = this.Field0;
                    else if (this.Field1 == 2)
                        mission.Card3 = this.Field0;
                    else if (this.Field1 == 3)
                        mission.Card4 = this.Field0;
                    dbQuery.AddQuery($"card{this.Field1 + 1}", (object)this.Field0);
                }
                mission.SelectedCard = this.Field2 == (int)ushort.MaxValue;
                if (mission.ActualMission != this.Field1)
                {
                    dbQuery.AddQuery("current_mission", (object)this.Field1);
                    mission.ActualMission = this.Field1;
                }
                ComDiv.UpdateDB("player_missions", "owner_id", (object)this.Client.PlayerId, dbQuery.GetTables(), dbQuery.GetValues());
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_BASE_QUEST_ACTIVE_IDX_CHANGE_REQ: " + ex.Message, LoggerType.Error, ex);
            }
        }
    }
}