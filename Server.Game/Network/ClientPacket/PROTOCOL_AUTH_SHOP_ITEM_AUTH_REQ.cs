// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_AUTH_SHOP_ITEM_AUTH_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Utils;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_AUTH_SHOP_ITEM_AUTH_REQ : GameClientPacket
    {
        private long Field0;
        private int Field1;
        private long Field2;
        private uint Field3 = 1;
        private readonly Random Field4 = new Random();
        private readonly object Field5 = new object();

        public override void Read()
        {
            this.Field0 = (long)this.ReadUD();
            int num = (int)this.ReadC();
        }

        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null)
                    return;
                ItemsModel A_2 = player.Inventory.GetItem(this.Field0);
                if (A_2 != null)
                {
                    this.Field1 = A_2.Id;
                    this.Field2 = (long)A_2.Count;
                    if (A_2.Category == ItemCategory.Coupon && player.Inventory.Items.Count >= 1500)
                    {
                        this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_ITEM_AUTH_ACK(2147487785U));
                        return;
                    }
                    if (this.Field1 == 1800049)
                    {
                        if (!DaoManagerSQL.UpdatePlayerKD(player.PlayerId, 0, 0, player.Statistic.Season.HeadshotsCount, player.Statistic.Season.TotalKillsCount))
                        {
                            this.Field3 = 2147483648U /*0x80000000*/;
                        }
                        else
                        {
                            player.Statistic.Season.KillsCount = 0;
                            player.Statistic.Season.DeathsCount = 0;
                            this.Client.SendPacket(new PROTOCOL_BASE_GET_MYINFO_RECORD_ACK(player.Statistic));
                        }
                    }
                    else if (this.Field1 == 1800048)
                    {
                        if (DaoManagerSQL.UpdatePlayerMatches(0, 0, 0, 0, player.Statistic.Season.TotalMatchesCount, player.PlayerId))
                        {
                            player.Statistic.Season.Matches = 0;
                            player.Statistic.Season.MatchWins = 0;
                            player.Statistic.Season.MatchLoses = 0;
                            player.Statistic.Season.MatchDraws = 0;
                            this.Client.SendPacket(new PROTOCOL_BASE_GET_MYINFO_RECORD_ACK(player.Statistic));
                        }
                        else
                            this.Field3 = 2147483648U /*0x80000000*/;
                    }
                    else if (this.Field1 == 1800050)
                    {
                        if (!ComDiv.UpdateDB("player_stat_seasons", "escapes_count", (object)0, "owner_id", (object)player.PlayerId))
                        {
                            this.Field3 = 2147483648U /*0x80000000*/;
                        }
                        else
                        {
                            player.Statistic.Season.EscapesCount = 0;
                            this.Client.SendPacket(new PROTOCOL_BASE_GET_MYINFO_RECORD_ACK(player.Statistic));
                        }
                    }
                    else if (this.Field1 == 1800053)
                    {
                        if (!DaoManagerSQL.UpdateClanBattles(player.ClanId, 0, 0, 0))
                        {
                            this.Field3 = 2147483648U /*0x80000000*/;
                        }
                        else
                        {
                            ClanModel clan = ClanManager.GetClan(player.ClanId);
                            if (clan.Id > 0 && clan.OwnerId == this.Client.PlayerId)
                            {
                                clan.Matches = 0;
                                clan.MatchWins = 0;
                                clan.MatchLoses = 0;
                                this.Client.SendPacket(new PROTOCOL_CS_RECORD_RESET_RESULT_ACK());
                            }
                            else
                                this.Field3 = 2147483648U /*0x80000000*/;
                        }
                    }
                    else if (this.Field1 == 1800055)
                    {
                        ClanModel clan = ClanManager.GetClan(player.ClanId);
                        if (clan.Id > 0 && clan.OwnerId == this.Client.PlayerId)
                        {
                            if (clan.MaxPlayers + 50 <= 250 && ComDiv.UpdateDB("system_clan", "max_players", (object)(clan.MaxPlayers + 50), "id", (object)player.ClanId))
                            {
                                clan.MaxPlayers += 50;
                                this.Client.SendPacket(new PROTOCOL_CS_REPLACE_PERSONMAX_ACK(clan.MaxPlayers));
                            }
                            else
                                this.Field3 = 2147487830U;
                        }
                        else
                            this.Field3 = 2147487830U;
                    }
                    else if (this.Field1 != 1800056)
                    {
                        if (this.Field1 > 1800113 && this.Field1 < 1800119)
                        {
                            int A_1 = this.Field1 == 1800114 ? 500 : (this.Field1 == 1800115 ? 1000 : (this.Field1 == 1800116 ? 5000 : (this.Field1 == 1800117 ? 10000 : 30000)));
                            if (ComDiv.UpdateDB("accounts", "gold", (object)(player.Gold + A_1), "player_id", (object)player.PlayerId))
                            {
                                player.Gold += A_1;
                                this.Client.SendPacket(new PROTOCOL_SHOP_PLUS_POINT_ACK(A_1, player.Gold, 0));
                            }
                            else
                                this.Field3 = 2147483648U /*0x80000000*/;
                        }
                        else if (this.Field1 == 1801145)
                        {
                            int num = 0;
                            int A_3 = new Random().Next(0, 9);
                            switch (A_3)
                            {
                                case 0:
                                    num = 1;
                                    break;

                                case 1:
                                    num = 2;
                                    break;

                                case 2:
                                    num = 3;
                                    break;

                                case 3:
                                    num = 4;
                                    break;

                                case 4:
                                    num = 5;
                                    break;

                                case 5:
                                    num = 10;
                                    break;

                                case 6:
                                    num = 15;
                                    break;

                                case 7:
                                    num = 25;
                                    break;

                                case 8:
                                    num = 30;
                                    break;

                                case 9:
                                    num = 50;
                                    break;
                            }
                            if (num > 0)
                            {
                                if (DaoManagerSQL.UpdateAccountTags(player.PlayerId, player.Tags + num))
                                {
                                    player.Tags += num;
                                    this.Client.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0U, player));
                                    this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_CAPSULE_ACK(new ItemsModel(), this.Field1, A_3));
                                }
                                else
                                    this.Field3 = 2147483648U /*0x80000000*/;
                            }
                            else
                                this.Field3 = 2147483648U /*0x80000000*/;
                        }


                        else if (A_2.Category == ItemCategory.Coupon && RandomBoxXML.ContainsBox(this.Field1))
                        {
                            RandomBoxModel box = RandomBoxXML.GetBox(this.Field1);
                            if (box != null)
                            {
                                List<RandomBoxItem> sortedList = box.GetSortedList(this.Method0(1, 100));
                                List<RandomBoxItem> rewardList = box.GetRewardList(sortedList, this.Method0(0, sortedList.Count));
                                if (rewardList.Count > 0)
                                {
                                    int index = rewardList[0].Index;
                                    List<ItemsModel> itemsModelList = new List<ItemsModel>();
                                    foreach (RandomBoxItem randomBoxItem in rewardList)
                                    {
                                        GoodsItem good = ShopManager.GetGood(randomBoxItem.GoodsId);
                                        if (good != null)
                                            itemsModelList.Add(good.Item);
                                        else if (DaoManagerSQL.UpdateAccountGold(player.PlayerId, player.Gold + index))
                                        {
                                            player.Gold += index;
                                            this.Client.SendPacket(new PROTOCOL_SHOP_PLUS_POINT_ACK(index, player.Gold, 0));
                                        }
                                        else
                                        {
                                            this.Field3 = 2147483648U /*0x80000000*/;
                                            break;
                                        }
                                        if (randomBoxItem.Special)
                                        {
                                            using (PROTOCOL_AUTH_SHOP_JACKPOT_ACK Packet = new PROTOCOL_AUTH_SHOP_JACKPOT_ACK(player.Nickname, this.Field1, index))
                                                GameXender.Client.SendPacketToAllClients(Packet);
                                        }
                                    }
                                    this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_CAPSULE_ACK(itemsModelList, this.Field1, index));
                                    if (itemsModelList.Count > 0)
                                        this.Client.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, player, itemsModelList));
                                }
                                else
                                    this.Field3 = 2147483648U /*0x80000000*/;
                            }
                            else
                                this.Field3 = 2147483648U /*0x80000000*/;
                        }
                        else
                        {
                            int idStatics = ComDiv.GetIdStatics(A_2.Id, 1);
                            if ((idStatics < 1 || idStatics > 8) && idStatics != 15 && idStatics != 27 && (idStatics < 30 || idStatics > 35))
                            {
                                switch (idStatics)
                                {
                                    case 17:
                                        this.Method1(player, A_2.Name);
                                        break;

                                    case 20:
                                        this.Method2(player, A_2.Id);
                                        break;

                                    case 37:
                                        this.CouponIncreaseBP(player, A_2.Id);
                                        break;

                                    default:
                                        this.Field3 = 2147483648U /*0x80000000*/;
                                        break;
                                }
                            }
                            else if (A_2.Equip == ItemEquipType.Durable)
                            {
                                A_2.Equip = ItemEquipType.Temporary;
                                A_2.Count = Convert.ToUInt32(DateTimeUtil.Now().AddSeconds((double)A_2.Count).ToString("yyMMddHHmm"));
                                ComDiv.UpdateDB("player_items", "object_id", (object)this.Field0, "owner_id", (object)player.PlayerId, new string[2]
                                {
                "count",
                "equip"
                                }, (object)(long)A_2.Count, (object)(int)A_2.Equip);
                                if (idStatics == 6)
                                {
                                    CharacterModel character = player.Character.GetCharacter(A_2.Id);
                                    if (character != null)
                                        this.Client.SendPacket(new PROTOCOL_CHAR_CHANGE_STATE_ACK(character));
                                }
                            }
                            else
                                this.Field3 = 2147483648U /*0x80000000*/;
                        }
                    }
                    else
                    {
                        ClanModel clan = ClanManager.GetClan(player.ClanId);
                        if (clan.Id > 0 && (double)clan.Points != 1000.0)
                        {
                            if (ComDiv.UpdateDB("system_clan", "points", (object)1000f, "id", (object)player.ClanId))
                            {
                                clan.Points = 1000f;
                                this.Client.SendPacket(new PROTOCOL_CS_POINT_RESET_RESULT_ACK());
                            }
                            else
                                this.Field3 = 2147487830U;
                        }
                        else
                            this.Field3 = 2147487830U;
                    }
                }
                else
                    this.Field3 = 2147483648U /*0x80000000*/;
                this.Client.SendPacket(new PROTOCOL_AUTH_SHOP_ITEM_AUTH_ACK(this.Field3, A_2, player));
            }
            catch (OverflowException ex)
            {
                CLogger.Print($"Obj: {this.Field0} ItemId: {this.Field1} Count: {this.Field2} PlayerId: {this.Client.GetAccount()} Name: '{this.Client.GetAccount().Nickname}' {ex.Message}", LoggerType.Error, (Exception)ex);
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_AUTH_SHOP_ITEM_AUTH_REQ: " + ex.Message, LoggerType.Error, ex);
            }
        }

        private int Method0(int A_1, int A_2)
        {
            lock (this.Field5)
                return this.Field4.Next(A_1, A_2);
        }

        private void Method1(Account A_1, string A_2)
        {
            int itemId = ComDiv.CreateItemId(16 /*0x10*/, 0, ComDiv.GetIdStatics(this.Field1, 3));
            int idStatics = ComDiv.GetIdStatics(this.Field1, 2);
            if (!AllUtils.CheckDuplicateCouponEffects(A_1, itemId))
            {
                ItemsModel A_3 = A_1.Inventory.GetItem(itemId);
                if (A_3 == null)
                {
                    int num = A_1.Bonus.AddBonuses(itemId) ? 1 : 0;
                    CouponFlag couponEffect = CouponEffectXML.GetCouponEffect(itemId);
                    if (couponEffect != null && couponEffect.EffectFlag > (CouponEffects)0 && !A_1.Effects.HasFlag((Enum)couponEffect.EffectFlag))
                    {
                        A_1.Effects |= couponEffect.EffectFlag;
                        DaoManagerSQL.UpdateCouponEffect(A_1.PlayerId, A_1.Effects);
                    }
                    if (num != 0)
                        DaoManagerSQL.UpdatePlayerBonus(A_1.PlayerId, A_1.Bonus.Bonuses, A_1.Bonus.FreePass);
                    this.Client.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, A_1, new ItemsModel(itemId, A_2 + " [Active]", ItemEquipType.Temporary, Convert.ToUInt32(DateTimeUtil.Now().AddDays((double)idStatics).ToString("yyMMddHHmm")))));
                }
                else
                {
                    DateTime exact = DateTime.ParseExact(A_3.Count.ToString(), "yyMMddHHmm", (IFormatProvider)CultureInfo.InvariantCulture);
                    A_3.Count = Convert.ToUInt32(exact.AddDays((double)idStatics).ToString("yyMMddHHmm"));
                    ComDiv.UpdateDB("player_items", "count", (object)(long)A_3.Count, "object_id", (object)A_3.ObjectId, "owner_id", (object)A_1.PlayerId);
                    this.Client.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(1, A_1, A_3));
                }
            }
            else
                this.Field3 = 2147483648U /*0x80000000*/;
        }

        private void Method2(Account A_1, int A_2)
        {
            int A_1_1 = ComDiv.GetIdStatics(A_2, 3) * 100 + ComDiv.GetIdStatics(A_2, 2) * 100000;
            if (!DaoManagerSQL.UpdateAccountGold(A_1.PlayerId, A_1.Gold + A_1_1))
            {
                this.Field3 = 2147483648U /*0x80000000*/;
            }
            else
            {
                A_1.Gold += A_1_1;
                this.Client.SendPacket(new PROTOCOL_SHOP_PLUS_POINT_ACK(A_1_1, A_1.Gold, 0));
            }
        }

        private void CouponIncreaseBP(Account Player, int CouponId)
        {
            PlayerBattlepass Pass = Player.Battlepass;
            if (Pass != null)
            {
                int EarnedPoints = ComDiv.GetIdStatics(CouponId, 3) * 10;
                EarnedPoints += ComDiv.GetIdStatics(CouponId, 2) * 100000;

                Pass.EarnedPoints += EarnedPoints;
                if (ComDiv.UpdateDB("player_battlepass", "earned_points", Pass.EarnedPoints, "owner_id", Player.PlayerId))
                {
                    Player.UpdateSeasonpass = true;
                    AllUtils.UpdateSeasonPass(Player);
                }
                else
                {
                    Field3 = 0x80000000;
                }
            }
            else
            {
                Field3 = 0x80000000;
            }
        }
    }
}