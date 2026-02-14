// Decompiled with JetBrains decompiler
// Type: Server.Game.Data.Commands.ValuesCommand
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Data.Utils;
using Server.Game.Network;
using Server.Game.Network.ServerPacket;
using System;
using System.Runtime.CompilerServices;


namespace Server.Game.Data.Commands
{
    public class ValuesCommand : ICommand
    {
        public string Command
        {
            
            get => "player";
        }

        public string Description
        {
            
            get => "modify value of player";
        }

        public string Permission
        {
            
            get => "gamemastercommand";
        }

        public string Args
        {
            
            get => "%options1% $options2% %value% %uid%";
        }

        
        public string Execute(string Command, string[] Args, Account Player)
        {
            string lower1 = Args[0].ToLower();
            string lower2 = Args[1].ToLower();
            int GoodId = int.Parse(Args[2]);
            long result;
            bool flag = long.TryParse(Args[3], out result);
            switch (lower1)
            {
                case "gift":
                    Account account1 = flag ? AccountManager.GetAccount(result, 0) : AccountManager.GetAccount(Args[3], 1, 0);
                    switch (lower2)
                    {
                        case "gold":
                            if (account1 == null)
                                return $"Player with {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} doesn't Exist!";
                            if (!DaoManagerSQL.UpdateAccountGold(account1.PlayerId, account1.Gold + GoodId))
                                return ComDiv.ToTitleCase(lower1) + " Command wrong or not founded!";
                            account1.Gold += GoodId;
                            account1.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0U, account1));
                            SendItemInfo.LoadGoldCash(account1);
                            return $"{ComDiv.ToTitleCase(lower1)} {GoodId} Amount Of {ComDiv.ToTitleCase(lower2)} To UID: {account1.PlayerId} ({account1.Nickname})";
                        case "cash":
                            if (account1 == null)
                                return $"Player with {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} doesn't Exist!";
                            if (!DaoManagerSQL.UpdateAccountCash(account1.PlayerId, account1.Cash + GoodId))
                                return ComDiv.ToTitleCase(lower1) + " Command wrong or not founded!";
                            account1.Cash += GoodId;
                            account1.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0U, account1));
                            SendItemInfo.LoadGoldCash(account1);
                            return $"{ComDiv.ToTitleCase(lower1)} {GoodId} Amount Of {ComDiv.ToTitleCase(lower2)} To UID: {account1.PlayerId} ({account1.Nickname})";
                        case "tags":
                            if (account1 == null)
                                return $"Player with {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} doesn't Exist!";
                            if (!DaoManagerSQL.UpdateAccountTags(account1.PlayerId, account1.Tags + GoodId))
                                return ComDiv.ToTitleCase(lower1) + " Command wrong or not founded!";
                            account1.Tags += GoodId;
                            account1.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0U, account1));
                            SendItemInfo.LoadGoldCash(account1);
                            return $"{ComDiv.ToTitleCase(lower1)} {GoodId} Amount Of {ComDiv.ToTitleCase(lower2)} To UID: {account1.PlayerId} ({account1.Nickname})";
                        case "item":
                            if (account1 == null)
                                return $"Player with {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} doesn't Exist!";
                            GoodsItem good = ShopManager.GetGood(GoodId);
                            if (good == null)
                                return $"Goods Id: {GoodId} not founded!";
                            ItemsModel itemsModel = new ItemsModel(good.Item);
                            if (itemsModel == null)
                                return ComDiv.ToTitleCase(lower1) + " Command wrong or not founded!";
                            account1.SendPacket(new PROTOCOL_BASE_NEW_REWARD_POPUP_ACK(Player, itemsModel));
                            if (ComDiv.GetIdStatics(itemsModel.Id, 1) == 6 && Player.Character.GetCharacter(itemsModel.Id) == null)
                                AllUtils.CreateCharacter(Player, itemsModel);
                            else
                                account1.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, account1, itemsModel));
                            return $"{ComDiv.ToTitleCase(lower1)} {itemsModel.Name} To UID: {account1.PlayerId} ({account1.Nickname})";
                    }
                    break;
                case "kick":
                    switch (lower2)
                    {
                        case "uid":
                            Account account2 = AccountManager.GetAccount(result, 0);
                            if (account2 == null)
                                return $"Player with UID: {result} doesn't Exist!";
                            if (account2.PlayerId == Player.PlayerId)
                                return $"Player by UID: {result} failed! (Can't Kick Yourself)";
                            if (account2.Access > Player.Access)
                                return $"Player by UID: {result} failed! (Can't Kick Higher Access Level Than Yours)";
                            account2.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), false);
                            account2.Close(TimeSpan.FromSeconds((double)GoodId).Milliseconds, true);
                            return $"{ComDiv.ToTitleCase(lower1)} Player by UID: {result} Executed in {GoodId} Seconds!";
                        case "nick":
                            Account account3 = AccountManager.GetAccount(Args[3], 1, 0);
                            if (account3 == null)
                                return $"Player with Nickname: {Args[3]} doesn't Exist!";
                            if (account3.Nickname == Player.Nickname)
                                return $"Player by Nickname: {Args[3]} failed! (Can't Kick Yourself)";
                            if (account3.Access > Player.Access)
                                return $"Player by Nickname: {Args[3]} failed! (Can't Kick Higher Access Level Than Yours)";
                            account3.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), false);
                            account3.Close(TimeSpan.FromSeconds((double)GoodId).Milliseconds, true);
                            return $"{ComDiv.ToTitleCase(lower1)} Player by Nickname: {Args[3]} Executed in {GoodId} Seconds!";
                    }
                    break;
                case "ban":
                    Account account4 = flag ? AccountManager.GetAccount(result, 0) : AccountManager.GetAccount(Args[3], 1, 0);
                    switch (lower2)
                    {
                        case "normal":
                            if (account4 == null)
                                return $"Player with {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} doesn't Exist!";
                            if (account4.PlayerId == Player.PlayerId)
                                return $"Player by {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} failed! (Can't Ban Yourself)";
                            if (account4.Access > Player.Access)
                                return $"Player by {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} failed! (Can't Ban Higher Access Level Than Yours)";

                            double num1 = Convert.ToDouble(GoodId);

                            // ✅ PERBAIKAN: Parameter pertama adalah player yang di-ban
                            BanHistory banHistory1 = DaoManagerSQL.SaveBanHistory(
                                account4.PlayerId,  // Player yang di-ban
                                "ACCOUNT",          // Type ban
                                $"{account4.PlayerId}",
                                DateTimeUtil.Now().AddDays(num1),
                                "GM Command"
                            );

                            if (banHistory1 == null)
                                return ComDiv.ToTitleCase(lower1) + " Command wrong or not founded!";

                            // ✅ PERBAIKAN: Update ban_object_id di database
                            ComDiv.UpdateDB("accounts", "ban_object_id", banHistory1.ObjectId, "player_id", account4.PlayerId);

                            using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK Packet = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK($"Player '{account4.Nickname}' has been banned for {num1} Day(s)!"))
                                GameXender.Client.SendPacketToAllClients(Packet);

                            account4.BanObjectId = banHistory1.ObjectId;
                            account4.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), false);
                            account4.Close(1000, true);

                            return $"{ComDiv.ToTitleCase(lower1)} {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} Success for {num1} Day(s)";

                        case "permanent":
                            if (account4 == null)
                                return $"Player with {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} doesn't Exist!";
                            if (account4.PlayerId == Player.PlayerId)
                                return $"Player by {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} failed! (Can't Ban Yourself)";
                            if (account4.Access > Player.Access)
                                return $"Player by {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} failed! (Can't Ban Higher Access Level Than Yours)";

                            double num2 = 999.0;

                            // ✅ PERBAIKAN: Parameter pertama adalah player yang di-ban
                            BanHistory banHistory2 = DaoManagerSQL.SaveBanHistory(
                                account4.PlayerId,  // Player yang di-ban
                                "ACCOUNT",          // Type ban
                                $"{account4.PlayerId}",
                                DateTimeUtil.Now().AddDays(num2),
                                "GM Command"
                            );

                            if (banHistory2 == null)
                                return ComDiv.ToTitleCase(lower1) + " Command wrong or not founded!";

                            // ✅ PERBAIKAN: Update ban_object_id di database
                            ComDiv.UpdateDB("accounts", "ban_object_id", banHistory2.ObjectId, "player_id", account4.PlayerId);

                            using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK Packet = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK($"Player '{account4.Nickname}' has been permanently Banned!"))
                                GameXender.Client.SendPacketToAllClients(Packet);

                            account4.BanObjectId = banHistory2.ObjectId;
                            account4.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), false);
                            account4.Close(1000, true);

                            return $"{ComDiv.ToTitleCase(lower1)} {(flag ? $"UID: {result}" : "Nickname: " + Args[3])} Success for {num2} Day(s)";
                    }
                    break;
            }
            return $"Command {ComDiv.ToTitleCase(lower1)} was not founded!";
        }
    }
}