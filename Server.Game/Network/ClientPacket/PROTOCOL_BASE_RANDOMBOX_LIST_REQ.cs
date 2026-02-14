using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;

namespace Server.Game.Network.ClientPacket
{
    // Token: 0x02000152 RID: 338
    public class PROTOCOL_BASE_RANDOMBOX_LIST_REQ : GameClientPacket
    {
        // Token: 0x06000365 RID: 869 RVA: 0x0001CE99 File Offset: 0x0001B099
        public override void Read()
        {
            this.Field0 = base.ReadS(32);
        }

        // Token: 0x06000366 RID: 870 RVA: 0x0001CEAC File Offset: 0x0001B0AC

        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                bool flag = player != null;
                if (flag)
                {
                    bool flag2 = !player.LoadedShop;
                    if (flag2)
                    {
                        player.LoadedShop = true;
                        foreach (ShopData a_ in ShopManager.ShopDataItems)
                        {
                            this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_ITEMLIST_ACK(a_, ShopManager.TotalItems));
                        }
                        foreach (ShopData a_2 in ShopManager.ShopDataGoods)
                        {
                            this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODSLIST_ACK(a_2, ShopManager.TotalGoods));
                        }
                        foreach (ShopData a_3 in ShopManager.ShopDataItemRepairs)
                        {
                            this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_REPAIRLIST_ACK(a_3, ShopManager.TotalRepairs));
                        }
                        foreach (ShopData a_4 in BattleBoxXML.ShopDataBattleBoxes)
                        {
                            this.Client.SendPacket(new PROTOCOL_BATTLEBOX_GET_LIST_ACK(a_4, BattleBoxXML.TotalBoxes));
                        }
                        bool flag3 = player.CafePC == CafeEnum.None;
                        if (flag3)
                        {
                            using (List<ShopData>.Enumerator enumerator5 = ShopManager.ShopDataMt1.GetEnumerator())
                            {
                                while (enumerator5.MoveNext())
                                {
                                    ShopData a_5 = enumerator5.Current;
                                    this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_MATCHINGLIST_ACK(a_5, ShopManager.TotalMatching1));
                                }
                                goto IL_20E;
                            }
                        }
                        foreach (ShopData a_6 in ShopManager.ShopDataMt2)
                        {
                            this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_MATCHINGLIST_ACK(a_6, ShopManager.TotalMatching2));
                        }
                    }
                IL_20E:
                    bool flag4 = Bitwise.ReadFile(Environment.CurrentDirectory + "/Data/Raws/RandomBox.dat") == this.Field0;
                    if (flag4)
                    {
                        this.Client.SendPacket(new PROTOCOL_BASE_RANDOMBOX_LIST_ACK(false));
                    }
                    else
                    {
                        this.Client.SendPacket(new PROTOCOL_BASE_RANDOMBOX_LIST_ACK(true));
                    }
                }
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_BASE_RANDOMBOX_LIST_REQ: " + ex.Message, LoggerType.Error, ex);
            }
        }

        // Token: 0x04000269 RID: 617
        private string Field0;
    }
}
