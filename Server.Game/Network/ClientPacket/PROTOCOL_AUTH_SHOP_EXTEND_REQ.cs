using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using Server.Game.Data.Utils;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_AUTH_SHOP_EXTEND_REQ : GameClientPacket
    {
        private List<CartGoods> list_0 = new List<CartGoods>();

        public override void Read()
        {
            this.ReadD();
            int num1 = (int)this.ReadC();
            this.list_0.Add(new CartGoods()
            {
                GoodId = this.ReadD(),
                BuyType = (int)this.ReadC()
            });
            int num2 = (int)this.ReadC();
            this.ReadQ();
        }

        public override void Run()
        {
            try
            {
                Account player = this.Client.Player;
                if (player == null)
                    return;
                if (player.Inventory.Items.Count < 1000)
                {
                    int num1;
                    int num2;
                    int num3;
                    List<GoodsItem> goods = ShopManager.GetGoods(this.list_0, out num1, out num2, out num3);
                    if (goods.Count != 0)
                    {
                        if (0 > player.Gold - num1 || 0 > player.Cash - num2 || 0 > player.Tags - num3)
                            this.Client.SendPacket((GameServerPacket)new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487768U));
                        else if (!DaoManagerSQL.UpdateAccountValuable(player.PlayerId, player.Gold - num1, player.Cash - num2, player.Tags - num3))
                        {
                            this.Client.SendPacket((GameServerPacket)new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487769U));
                        }
                        else
                        {
                            player.Gold -= num1;
                            player.Cash -= num2;
                            player.Tags -= num3;
                            foreach (GoodsItem goodsItem in goods)
                            {
                                int category = ComDiv.GetIdStatics(goodsItem.Item.Id, 1);
                                if (category != 36)
                                {
                                    this.Client.SendPacket((GameServerPacket)new PROTOCOL_INVENTORY_GET_INFO_ACK(0, player, goodsItem.Item));
                                }
                                else
                                {
                                    string itemName = goodsItem.Item.Name.ToUpper();
                                    if (itemName.Contains("INVENTORY") || itemName.Contains("INV") || itemName.Contains("SLOT"))
                                    {
                                        int addSlots = (int)goodsItem.Item.Count;
                                        if (addSlots == 0) addSlots = 100; // Default if count is 0

                                        int newPlus = player.InventoryPlus + addSlots;
                                        if (newPlus > 500) newPlus = 500; // Cap at 500 additional

                                        if (ComDiv.UpdateDB("accounts", "inventory_plus", newPlus, "player_id", player.PlayerId))
                                        {
                                            player.InventoryPlus = newPlus;
                                            this.Client.SendPacket(new PACKET_INVENTORY_MAX_UP_ACK(player, (ushort)(600 + newPlus), 0));
                                            this.Client.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(Translation.GetLabel("STR_POPUP_INVENTORY_EXTEND_SUCCESS")));
                                        }
                                    }
                                    else
                                    {
                                        AllUtils.ProcessBattlepassPremiumBuy(player);
                                        player.UpdateSeasonpass = false;
                                        player.SendPacket((GameServerPacket)new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(0U, player.Battlepass));
                                    }
                                }
                            }
                            this.Client.SendPacket((GameServerPacket)new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(1U, goods, player));
                            // Only send default success if no specific inventory message was sent (or just let it be)
                            // this.Client.SendPacket((GameServerPacket)new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(Translation.GetLabel("STR_POPUP_EXTEND_SUCCESS")));
                        }
                    }
                    else
                        this.Client.SendPacket((GameServerPacket)new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487767U));
                }
                else
                    this.Client.SendPacket((GameServerPacket)new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487929U));
            }
            catch (Exception ex)
            {
                CLogger.Print($"{this.GetType().Name}; {ex.Message}", LoggerType.Error, ex);
            }
        }
    }
}