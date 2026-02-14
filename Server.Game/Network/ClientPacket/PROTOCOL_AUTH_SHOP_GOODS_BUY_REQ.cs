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
using System.Runtime.CompilerServices;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_AUTH_SHOP_GOODS_BUY_REQ : GameClientPacket
    {
        private List<CartGoods> Field0 = new List<CartGoods>();

        public override void Read()
        {
            byte num1 = this.ReadC();
            for (byte index = 0; (int)index < (int)num1; ++index)
            {
                int num2 = (int)this.ReadC();
                this.Field0.Add(new CartGoods()
                {
                    GoodId = this.ReadD(),
                    BuyType = (int)this.ReadC()
                });
                int num3 = (int)this.ReadC();
                this.ReadQ();
            }
        }

        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null)
                    return;
                if (player.Inventory.Items.Count >= 1500)
                {
                    this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487929U));
                }
                else
                {
                    int GoldPrice;
                    int CashPrice;
                    int TagsPrice;
                    List<GoodsItem> goods = ShopManager.GetGoods(this.Field0, out GoldPrice, out CashPrice, out TagsPrice);
                    if (goods.Count != 0)
                    {
                        if (0 <= player.Gold - GoldPrice && 0 <= player.Cash - CashPrice && 0 <= player.Tags - TagsPrice)
                        {
                            if (DaoManagerSQL.UpdateAccountValuable(player.PlayerId, player.Gold - GoldPrice, player.Cash - CashPrice, player.Tags - TagsPrice))
                            {
                                player.Gold -= GoldPrice;
                                player.Cash -= CashPrice;
                                player.Tags -= TagsPrice;
                                foreach (GoodsItem goodsItem in goods)
                                {
                                    if (ComDiv.GetIdStatics(goodsItem.Item.Id, 1) == 36)
                                    {
                                        AllUtils.ProcessBattlepassPremiumBuy(player);
                                        player.UpdateSeasonpass = false;
                                        player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(0U, player.Battlepass));
                                    }
                                    else
                                        this.Client.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, player, goodsItem.Item));

                                    // Decrease stock for limited items
                                    if (ShopManager.IsLimitedItem(goodsItem.Id))
                                    {
                                        ShopManager.UpdateLimitedItemStock(goodsItem.Id, 1);
                                        // Broadcast SYNC to ALL connected clients for real-time stock sync
                                        GameXender.BroadcastLimitedSaleSync();
                                    }
                                }
                                this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(1U, goods, player));
                            }
                            else
                                this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487769U));
                        }
                        else
                            this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487768U));
                    }
                    else
                        this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487767U));
                }
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_AUTH_SHOP_GOODS_BUY_REQ: " + ex.Message, LoggerType.Error, ex);
            }
        }
    }
}