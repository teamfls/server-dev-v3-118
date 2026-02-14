// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_BASE_QUEST_DELETE_CARD_SET_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Runtime.CompilerServices;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_BASE_QUEST_DELETE_CARD_SET_REQ : GameClientPacket
    {
        private uint Field0;
        private int Field1;

        public override void Read() => this.Field1 = (int)this.ReadC();

        
        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null)
                    return;
                PlayerMissions mission = player.Mission;
                bool flag = false;
                if (this.Field1 >= 3 || this.Field1 == 0 && mission.Mission1 == 0 || this.Field1 == 1 && mission.Mission2 == 0 || this.Field1 == 2 && mission.Mission3 == 0)
                    flag = true;
                if (!flag && DaoManagerSQL.UpdatePlayerMissionId(player.PlayerId, 0, this.Field1))
                {
                    if (ComDiv.UpdateDB("player_missions", "owner_id", (object)player.PlayerId, new string[2]
                    {
          $"card{this.Field1 + 1}",
          $"mission{this.Field1 + 1}_raw"
                    }, (object)0, (object)new byte[0]))
                    {
                        if (this.Field1 == 0)
                        {
                            mission.Mission1 = 0;
                            mission.Card1 = 0;
                            mission.List1 = new byte[40];
                            goto label_12;
                        }
                        if (this.Field1 == 1)
                        {
                            mission.Mission2 = 0;
                            mission.Card2 = 0;
                            mission.List2 = new byte[40];
                            goto label_12;
                        }
                        if (this.Field1 == 2)
                        {
                            mission.Mission3 = 0;
                            mission.Card3 = 0;
                            mission.List3 = new byte[40];
                            goto label_12;
                        }
                        goto label_12;
                    }
                }
                this.Field0 = 2147487824U /*0x80001050*/;
            label_12:
                this.Client.SendPacket(new PROTOCOL_BASE_QUEST_DELETE_CARD_SET_ACK(this.Field0, player));
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_BASE_QUEST_DELETE_CARD_SET_REQ: " + ex.Message, LoggerType.Error, ex);
            }
        }
    }
}