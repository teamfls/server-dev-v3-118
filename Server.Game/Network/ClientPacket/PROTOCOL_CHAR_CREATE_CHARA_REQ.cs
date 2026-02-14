// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_CHAR_CREATE_CHARA_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_CHAR_CREATE_CHARA_REQ : GameClientPacket
    {
        private string Field0;
        private List<CartGoods> Field1 = new List<CartGoods>();

        public override void Read()
        {
            int num1 = (int)this.ReadC();
            this.Field0 = this.ReadU((int)this.ReadC() * 2);
            int num2 = (int)this.ReadC();
            this.Field1.Add(new CartGoods()
            {
                GoodId = this.ReadD(),
                BuyType = (int)this.ReadC()
            });
            int num3 = (int)this.ReadC();
        }

        public override void Run()
        {
            try
            {
                Account player = this.Client.Player;
                if (player == null)
                    return;
                if (player.Inventory.Items.Count < 1500 && player.Character.Characters.Count < 150 )
                {
                    int GoldPrice;
                    int CashPrice;
                    int TagsPrice;
                    List<GoodsItem> goods = ShopManager.GetGoods(this.Field1, out GoldPrice, out CashPrice, out TagsPrice);
                    if (goods.Count != 0)
                    {
                        if (0 <= player.Gold - GoldPrice && 0 <= player.Cash - CashPrice && 0 <= player.Tags - TagsPrice)
                        {
                            if (!DaoManagerSQL.UpdateAccountValuable(player.PlayerId, player.Gold - GoldPrice, player.Cash - CashPrice, player.Tags - TagsPrice))
                            {
                                this.Client.SendPacket(new PROTOCOL_CHAR_CREATE_CHARA_ACK(2147487769U, byte.MaxValue, (CharacterModel)null, (Account)null));
                            }
                            else
                            {
                                player.Gold -= GoldPrice;
                                player.Cash -= CashPrice;
                                player.Tags -= TagsPrice;
                                CharacterModel characterModel = this.Method0(goods, player.Character.Characters.Count);
                                if (characterModel != null)
                                {
                                    player.Character.AddCharacter(characterModel);
                                    if (player.Character.GetCharacter(characterModel.Id) != null)
                                        DaoManagerSQL.CreatePlayerCharacter(characterModel, player.PlayerId);
                                }
                                this.Client.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, player, goods));
                                this.Client.SendPacket(new PROTOCOL_CHAR_CREATE_CHARA_ACK(0U, (byte)1, characterModel, player));
                            }
                        }
                        else
                            this.Client.SendPacket(new PROTOCOL_CHAR_CREATE_CHARA_ACK(2147487768U, byte.MaxValue, (CharacterModel)null, (Account)null));
                    }
                    else
                        this.Client.SendPacket(new PROTOCOL_CHAR_CREATE_CHARA_ACK(2147487767U, byte.MaxValue, (CharacterModel)null, (Account)null));
                }
                else
                    this.Client.SendPacket(new PROTOCOL_CHAR_CREATE_CHARA_ACK(2147487929U, byte.MaxValue, (CharacterModel)null, (Account)null));
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        private CharacterModel Method0(List<GoodsItem> A_1, int A_2)
        {
            foreach (GoodsItem goodsItem in A_1)
            {
                if (goodsItem != null && goodsItem.Item.Id != 0)
                    return new CharacterModel()
                    {
                        Id = goodsItem.Item.Id,
                        Slot = A_2++,
                        Name = this.Field0,
                        CreateDate = uint.Parse(DateTimeUtil.Now("yyMMddHHmm")),
                        PlayTime = 0
                    };
            }
            return (CharacterModel)null;
        }
    }
}