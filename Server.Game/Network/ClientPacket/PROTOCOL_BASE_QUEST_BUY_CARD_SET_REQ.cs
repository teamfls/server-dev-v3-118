// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_BASE_QUEST_BUY_CARD_SET_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.XML;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Runtime.CompilerServices;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_BASE_QUEST_BUY_CARD_SET_REQ : GameClientPacket
    {
        private int Field0;
        private EventErrorEnum Field1;

        public override void Read()
        {
            this.Field0 = (int)this.ReadC();
            int num = (int)this.ReadC();
        }

        
        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null)
                    return;
                PlayerMissions mission1 = player.Mission;
                if (mission1 != null && mission1.Mission1 != this.Field0 && mission1.Mission2 != this.Field0 && mission1.Mission3 != this.Field0)
                {
                    MissionStore mission2 = MissionConfigXML.GetMission(this.Field0);
                    if (mission2 != null && ShopManager.GetItemId(mission2.ItemId) != null)
                    {
                        if (mission1.Mission1 == 0)
                        {
                            if (!DaoManagerSQL.UpdatePlayerMissionId(player.PlayerId, this.Field0, 0))
                            {
                                this.Field1 = EventErrorEnum.MISSION_FAIL_BUY_CARD_BY_NO_CARD_INFO;
                            }
                            else
                            {
                                mission1.Mission1 = this.Field0;
                                mission1.List1 = new byte[40];
                                mission1.ActualMission = 0;
                                mission1.Card1 = 0;
                            }
                        }
                        else if (mission1.Mission2 != 0)
                        {
                            if (mission1.Mission3 == 0)
                            {
                                if (DaoManagerSQL.UpdatePlayerMissionId(player.PlayerId, this.Field0, 2))
                                {
                                    mission1.Mission3 = this.Field0;
                                    mission1.List3 = new byte[40];
                                    mission1.ActualMission = 2;
                                    mission1.Card3 = 0;
                                }
                                else
                                    this.Field1 = EventErrorEnum.MISSION_FAIL_BUY_CARD_BY_NO_CARD_INFO;
                            }
                            else
                                this.Field1 = EventErrorEnum.MISSION_LIMIT_CARD_COUNT;
                        }
                        else if (DaoManagerSQL.UpdatePlayerMissionId(player.PlayerId, this.Field0, 1))
                        {
                            mission1.Mission2 = this.Field0;
                            mission1.List2 = new byte[40];
                            mission1.ActualMission = 1;
                            mission1.Card2 = 0;
                        }
                        else
                            this.Field1 = EventErrorEnum.MISSION_FAIL_BUY_CARD_BY_NO_CARD_INFO;
                    }
                    else
                    {
                        CLogger.Print("There is an error on Mission Config. Please check the configuration!", LoggerType.Warning);
                        this.Field1 = EventErrorEnum.MISSION_NO_POINT_TO_GET_ITEM;
                    }
                    int priceGold = ShopManager.GetItemId(mission2.ItemId).PriceGold;
                    if (this.Field1 == EventErrorEnum.SUCCESS && 0 <= player.Gold - priceGold)
                    {
                        if (priceGold != 0 && !DaoManagerSQL.UpdateAccountGold(player.PlayerId, player.Gold - priceGold))
                            this.Field1 = EventErrorEnum.MISSION_FAIL_BUY_CARD_BY_NO_CARD_INFO;
                        else
                            player.Gold -= priceGold;
                    }
                    else
                        this.Field1 = EventErrorEnum.MISSION_NO_POINT_TO_GET_ITEM;
                    this.Client.SendPacket(new PROTOCOL_BASE_QUEST_BUY_CARD_SET_ACK(this.Field1, player));
                }
                else
                    this.Client.SendPacket(new PROTOCOL_BASE_QUEST_BUY_CARD_SET_ACK(EventErrorEnum.MISSION_NO_POINT_TO_GET_ITEM, (Account)null));
            }
            catch (Exception ex)
            {
                CLogger.Print($"{this.GetType().Name}: {ex.Message}", LoggerType.Error, ex);
            }
        }
    }
}