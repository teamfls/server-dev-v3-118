using Npgsql;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Plugin.Core.SQL
{
    public static class DaoManagerSQL
    {
        public static List<ItemsModel> GetPlayerInventoryItems(long OwnerId)
        {
            try
            {
                List<ItemsModel> playerInventoryItems = new List<ItemsModel>();
                if (OwnerId != 0)
                {
                    using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                    {
                        NpgsqlCommand command = npgsqlConnection.CreateCommand();
                        npgsqlConnection.Open();
                        command.Parameters.AddWithValue("@owner", (object)OwnerId);
                        command.CommandText = "SELECT * FROM player_items WHERE owner_id=@owner ORDER BY object_id ASC;";
                        command.CommandType = CommandType.Text;
                        NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                        while (npgsqlDataReader.Read())
                        {
                            ItemsModel itemsModel = new ItemsModel(int.Parse(npgsqlDataReader["id"].ToString()))
                            {
                                ObjectId = long.Parse(npgsqlDataReader["object_id"].ToString()),
                                Name = npgsqlDataReader["name"].ToString(),
                                Count = uint.Parse(npgsqlDataReader["count"].ToString()),
                                Equip = (ItemEquipType)int.Parse(npgsqlDataReader["equip"].ToString())
                            };
                            playerInventoryItems.Add(itemsModel);
                        }
                        command.Dispose();
                        npgsqlDataReader.Close();
                        npgsqlConnection.Dispose();
                        npgsqlConnection.Close();
                    }
                }
                return playerInventoryItems;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return null;
            }
        }

        public static bool UpdatePlaytimeEventData(long OwnerId, uint lastPlaytimeDate, long lastPlaytimeValue, int lastPlaytimeFinish, int currentPlaytimeEventId, string playtimeCompletedLevels)
        {
            if (OwnerId == 0)
            {
                CLogger.Print("UpdatePlaytimeEventData: OwnerId inválido", LoggerType.Warning);
                return false;
            }

            try
            {
                using (NpgsqlConnection Connection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand Command = Connection.CreateCommand();
                    Connection.Open();
                    Command.Parameters.AddWithValue("@owner_id", OwnerId);
                    Command.Parameters.AddWithValue("@last_playtime_date", (long)lastPlaytimeDate);
                    Command.Parameters.AddWithValue("@last_playtime_value", lastPlaytimeValue);
                    Command.Parameters.AddWithValue("@last_playtime_finish", lastPlaytimeFinish);
                    Command.Parameters.AddWithValue("@current_playtime_event_id", currentPlaytimeEventId);
                    Command.Parameters.AddWithValue("@playtime_completed_levels", playtimeCompletedLevels ?? "");

                    Command.CommandText = @"
                        UPDATE player_events SET
                            last_playtime_date = @last_playtime_date,
                            last_playtime_value = @last_playtime_value,
                            last_playtime_finish = @last_playtime_finish,
                            current_playtime_event_id = @current_playtime_event_id,
                            playtime_completed_levels = @playtime_completed_levels
                        WHERE owner_id = @owner_id";

                    Command.CommandType = CommandType.Text;
                    int rowsAffected = Command.ExecuteNonQuery();
                    Command.Dispose();
                    Connection.Dispose();
                    Connection.Close();

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                //CLogger.Print($"Error actualizando datos de evento de tiempo para {OwnerId}: {ex.Message}", LoggerType.Error, ex);
                return false;
            }
        }

        public static bool CreatePlayerInventoryItem(ItemsModel Item, long OwnerId)
        {
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.Parameters.AddWithValue("@itmId", (object)Item.Id);
                    command.Parameters.AddWithValue("@ItmNm", (object)Item.Name);
                    command.Parameters.AddWithValue("@count", (object)(long)Item.Count);
                    command.Parameters.AddWithValue("@equip", (object)(int)Item.Equip);
                    command.CommandText = "INSERT INTO player_items(owner_id, id, name, count, equip) VALUES(@owner, @itmId, @ItmNm, @count, @equip) RETURNING object_id";
                    object obj = command.ExecuteScalar();
                    Item.ObjectId = Item.Equip != ItemEquipType.Permanent ? (long)obj : Item.ObjectId;
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static bool DeletePlayerInventoryItem(long ObjectId, long OwnerId)
        {
            return ObjectId != 0 && OwnerId != 0 && ComDiv.DeleteDB("player_items", "object_id", (object)ObjectId, "owner_id", (object)OwnerId);
        }

        public static BanHistory GetAccountBan(long ObjectId)
        {
            BanHistory accountBan = new BanHistory();
            if (ObjectId == 0)
            {
                return accountBan;
            }
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@obj", (object)ObjectId);
                    command.CommandText = "SELECT * FROM base_ban_history WHERE object_id=@obj";
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        accountBan.ObjectId = long.Parse(npgsqlDataReader["object_id"].ToString());
                        accountBan.PlayerId = long.Parse(npgsqlDataReader["owner_id"].ToString());
                        accountBan.Type = npgsqlDataReader["type"].ToString();
                        accountBan.Value = npgsqlDataReader["value"].ToString();
                        accountBan.Reason = npgsqlDataReader["reason"].ToString();
                        accountBan.StartDate = DateTime.Parse(npgsqlDataReader["start_date"].ToString());
                        accountBan.EndDate = DateTime.Parse(npgsqlDataReader["expire_date"].ToString());
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return null;
            }
            return accountBan;
        }

        public static List<string> GetHwIdList()
        {
            List<string> hwIdList = new List<string>();
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandText = "SELECT * FROM base_ban_hwid";
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        string str = npgsqlDataReader["hardware_id"].ToString();
                        if (str != null || str.Length != 0)
                            hwIdList.Add(str);
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return (List<string>)null;
            }
            return hwIdList;
        }

        public static void GetBanStatus(string MAC, string IP4, out bool ValidMac, out bool ValidIp4)
        {
            ValidMac = false;
            ValidIp4 = false;
            try
            {
                DateTime dateTime = DateTimeUtil.Now();
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@mac", (object)MAC);
                    command.Parameters.AddWithValue("@ip", (object)IP4);
                    command.CommandText = "SELECT * FROM base_ban_history WHERE value in (@mac, @ip)";
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        string str1 = npgsqlDataReader["type"].ToString();
                        string str2 = npgsqlDataReader["value"].ToString();
                        if (!(DateTime.Parse(npgsqlDataReader["expire_date"].ToString()) < dateTime))
                        {
                            if (str1 == nameof(MAC) && str2 == MAC)
                                ValidMac = true;
                            else if (str1 == nameof(IP4) && str2 == IP4)
                                ValidIp4 = true;
                        }
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        public static void CheckLicenseBan(string licenseKey, out bool isBanned)
        {
            isBanned = false;
            try
            {
                using (NpgsqlConnection connection = ConnectionSQL.GetInstance().Conn())
                {
                    connection.Open();
                    using (NpgsqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT 1 FROM ban_license WHERE license_key = @licenseKey LIMIT 1";
                        command.Parameters.AddWithValue("@licenseKey", licenseKey);

                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isBanned = true; // License ditemukan di tabel ban, artinya diblokir
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        public static BanHistory SaveBanHistory(long PlayerId, string Type, string Value, DateTime EndDate, string Reason = "")
        {
            BanHistory Ban = new BanHistory()
            {
                PlayerId = PlayerId,
                Type = Type,
                Value = Value,
                EndDate = EndDate,
                Reason = Reason
            };

            try
            {
                using (NpgsqlConnection Connection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand Command = Connection.CreateCommand();
                    Connection.Open();

                    // ✅ PENTING: Tambahkan parameter owner_id
                    Command.Parameters.AddWithValue("@owner_id", Ban.PlayerId);
                    Command.Parameters.AddWithValue("@type", Ban.Type);
                    Command.Parameters.AddWithValue("@value", Ban.Value);
                    Command.Parameters.AddWithValue("@reason", Ban.Reason);
                    Command.Parameters.AddWithValue("@start", Ban.StartDate);
                    Command.Parameters.AddWithValue("@end", Ban.EndDate);

                    // ✅ PENTING: Insert owner_id dan player_id ke database untuk kompatibilitas 3.80
                    Command.CommandText = @"
                INSERT INTO base_ban_history(owner_id, player_id, type, value, reason, start_date, expire_date) 
                VALUES(@owner_id, @owner_id, @type, @value, @reason, @start, @end) 
                RETURNING object_id";

                    object data = Command.ExecuteScalar();
                    Ban.ObjectId = (long)data;

                    Command.Dispose();
                    Connection.Dispose();
                    Connection.Close();
                    return Ban;
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return null;
            }
        }

        public static BanHistory GetActiveBanForPlayer(long playerId)
        {
            if (playerId == 0)
                return null;

            try
            {
                using (NpgsqlConnection connection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = connection.CreateCommand();
                    connection.Open();

                    command.Parameters.AddWithValue("@playerId", playerId);
                    command.Parameters.AddWithValue("@now", DateTimeUtil.Now());

                    // ✅ Query berdasarkan kolom owner_id ATAU player_id (untuk kompatibilitas 3.80)
                    command.CommandText = @"
                SELECT * FROM base_ban_history 
                WHERE (owner_id = @playerId OR player_id = @playerId)
                  AND expire_date > @now 
                ORDER BY object_id DESC 
                LIMIT 1";

                    command.CommandType = CommandType.Text;

                    NpgsqlDataReader reader = command.ExecuteReader();
                    BanHistory ban = null;

                    if (reader.Read())
                    {
                        ban = new BanHistory
                        {
                            ObjectId = long.Parse(reader["object_id"].ToString()),
                            PlayerId = long.Parse(reader["owner_id"].ToString()),
                            Type = reader["type"].ToString(),
                            Value = reader["value"].ToString(),
                            Reason = reader["reason"].ToString(),
                            StartDate = DateTime.Parse(reader["start_date"].ToString()),
                            EndDate = DateTime.Parse(reader["expire_date"].ToString())
                        };
                    }

                    reader.Close();
                    command.Dispose();
                    connection.Close();

                    return ban;
                }
            }
            catch (Exception ex)
            {
                CLogger.Print($"Error getting active ban for player {playerId}: {ex.Message}", LoggerType.Error, ex);
                return null;
            }
        }

        public static bool SaveAutoBan(long PlayerId, string Username, string Nickname, string Type, string Time, string Address, string HackType)
        {
            if (PlayerId == 0)
            {
                return false;
            }
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@player_id", (object)PlayerId);
                    command.Parameters.AddWithValue("@login", (object)Username);
                    command.Parameters.AddWithValue("@player_name", (object)Nickname);
                    command.Parameters.AddWithValue("@type", (object)Type);
                    command.Parameters.AddWithValue("@time", (object)Time);
                    command.Parameters.AddWithValue("@ip", (object)Address);
                    command.Parameters.AddWithValue("@hack_type", (object)HackType);
                    command.CommandText = "INSERT INTO base_auto_ban(owner_id, username, nickname, type, time, ip4_address, hack_type) VALUES(@player_id, @login, @player_name, @type, @time, @ip, @hack_type)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static bool SaveBanReason(long ObjectId, string Reason)
        {
            return ObjectId != 0 && ComDiv.UpdateDB("base_ban_history", "reason", (object)Reason, "object_id", (object)ObjectId);
        }

        public static bool CreateClan(out int ClanId, string Name, long OwnerId, string ClanInfo, uint CreateDate)
        {
            try
            {
                ClanId = -1;
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.Parameters.AddWithValue("@name", (object)Name);
                    command.Parameters.AddWithValue("@date", (object)(long)CreateDate);
                    command.Parameters.AddWithValue("@info", (object)ClanInfo);
                    command.Parameters.AddWithValue("@best", (object)"0-0");
                    command.CommandText = "INSERT INTO system_clan (name, owner_id, create_date, info, best_exp, best_participants, best_wins, best_kills, best_headshots) VALUES (@name, @owner, @date, @info, @best, @best, @best, @best, @best) RETURNING id";
                    object obj = command.ExecuteScalar();
                    ClanId = (int)obj;
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                ClanId = -1;
                return false;
            }
        }

        public static bool UpdateClanInfo(
          int ClanId,
          int Authority,
          int RankLimit,
          int MinAge,
          int MaxAge,
          int JoinType)
        {
            if (ClanId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@ClanId", (object)ClanId);
                    command.Parameters.AddWithValue("@Authority", (object)Authority);
                    command.Parameters.AddWithValue("@RankLimit", (object)RankLimit);
                    command.Parameters.AddWithValue("@MinAge", (object)MinAge);
                    command.Parameters.AddWithValue("@MaxAge", (object)MaxAge);
                    command.Parameters.AddWithValue("@JoinType", (object)JoinType);
                    command.CommandText = "UPDATE system_clan SET authority=@Authority, rank_limit=@RankLimit, min_age_limit=@MinAge, max_age_limit=@MaxAge, join_permission=@JoinType WHERE id=@ClanId";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static void UpdateClanBestPlayers(ClanModel Clan)
        {
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)Clan.Id);
                    command.Parameters.AddWithValue("@bp1", (object)Clan.BestPlayers.Exp.GetSplit());
                    command.Parameters.AddWithValue("@bp2", (object)Clan.BestPlayers.Participation.GetSplit());
                    command.Parameters.AddWithValue("@bp3", (object)Clan.BestPlayers.Wins.GetSplit());
                    command.Parameters.AddWithValue("@bp4", (object)Clan.BestPlayers.Kills.GetSplit());
                    command.Parameters.AddWithValue("@bp5", (object)Clan.BestPlayers.Headshots.GetSplit());
                    command.CommandType = CommandType.Text;
                    command.CommandText = "UPDATE system_clan SET best_exp=@bp1, best_participants=@bp2, best_wins=@bp3, best_kills=@bp4, best_headshots=@bp5 WHERE id=@id";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        public static bool UpdateClanLogo(int ClanId, uint logo)
        {
            return ClanId != 0 && ComDiv.UpdateDB("system_clan", nameof(logo), (object)(long)logo, "id", (object)ClanId);
        }

        public static bool UpdateClanPoints(int ClanId, float Gold)
        {
            return ClanId != 0 && ComDiv.UpdateDB("system_clan", "gold", (object)Gold, "id", (object)ClanId);
        }

        public static bool UpdateClanExp(int ClanId, int Exp)
        {
            return ClanId != 0 && ComDiv.UpdateDB("system_clan", "exp", (object)Exp, "id", (object)ClanId);
        }

        public static bool UpdateClanRank(int ClanId, int Rank)
        {
            return ClanId != 0 && ComDiv.UpdateDB("system_clan", "rank", (object)Rank, "id", (object)ClanId);
        }

        public static bool UpdateClanBattles(int ClanId, int Matches, int Wins, int Loses)
        {
            if (ClanId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@clan", (object)ClanId);
                    command.Parameters.AddWithValue("@partidas", (object)Matches);
                    command.Parameters.AddWithValue("@vitorias", (object)Wins);
                    command.Parameters.AddWithValue("@derrotas", (object)Loses);
                    command.CommandText = "UPDATE system_clan SET matches=@partidas, match_wins=@vitorias, match_loses=@derrotas WHERE id=@clan";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static int GetClanPlayers(int ClanId)
        {
            int clanPlayers = 0;
            if (ClanId == 0)
                return clanPlayers;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@clan", (object)ClanId);
                    command.CommandText = "SELECT COUNT(*) FROM accounts WHERE clan_id=@clan";
                    clanPlayers = Convert.ToInt32(command.ExecuteScalar());
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return clanPlayers;
        }

        public static MessageModel GetMessage(long ObjectId, long PlayerId)
        {
            MessageModel message = (MessageModel)null;
            if (ObjectId != 0)
            {
                if (PlayerId != 0)
                {
                    try
                    {
                        using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                        {
                            NpgsqlCommand command = npgsqlConnection.CreateCommand();
                            npgsqlConnection.Open();
                            command.Parameters.AddWithValue("@obj", (object)ObjectId);
                            command.Parameters.AddWithValue("@owner", (object)PlayerId);
                            command.CommandText = "SELECT * FROM player_messages WHERE object_id=@obj AND owner_id=@owner";
                            command.CommandType = CommandType.Text;
                            NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                            while (npgsqlDataReader.Read())
                                message = new MessageModel((long)uint.Parse(npgsqlDataReader["expire_date"].ToString()), DateTimeUtil.Now())
                                {
                                    ObjectId = ObjectId,
                                    SenderId = long.Parse(npgsqlDataReader["sender_id"].ToString()),
                                    SenderName = npgsqlDataReader["sender_name"].ToString(),
                                    ClanId = int.Parse(npgsqlDataReader["clan_id"].ToString()),
                                    ClanNote = (NoteMessageClan)int.Parse(npgsqlDataReader["clan_note"].ToString()),
                                    Text = npgsqlDataReader["text"].ToString(),
                                    Type = (NoteMessageType)int.Parse(npgsqlDataReader["type"].ToString()),
                                    State = (NoteMessageState)int.Parse(npgsqlDataReader["state"].ToString())
                                };
                            command.Dispose();
                            npgsqlDataReader.Close();
                            npgsqlConnection.Dispose();
                            npgsqlConnection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        CLogger.Print(ex.Message, LoggerType.Error, ex);
                        return (MessageModel)null;
                    }
                    return message;
                }
            }
            return message;
        }

        public static List<MessageModel> GetGiftMessages(long OwnerId)
        {
            List<MessageModel> giftMessages = new List<MessageModel>();
            if (OwnerId == 0)
                return giftMessages;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_messages WHERE owner_id=@owner";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        NoteMessageType noteMessageType = (NoteMessageType)int.Parse(npgsqlDataReader["type"].ToString());
                        if (noteMessageType == NoteMessageType.Gift)
                        {
                            MessageModel messageModel = new MessageModel((long)uint.Parse(npgsqlDataReader["expire_date"].ToString()), DateTimeUtil.Now())
                            {
                                ObjectId = long.Parse(npgsqlDataReader["object_id"].ToString()),
                                SenderId = long.Parse(npgsqlDataReader["sender_id"].ToString()),
                                SenderName = npgsqlDataReader["sender_name"].ToString(),
                                ClanId = int.Parse(npgsqlDataReader["clan_id"].ToString()),
                                ClanNote = (NoteMessageClan)int.Parse(npgsqlDataReader["clan_note"].ToString()),
                                Text = npgsqlDataReader["text"].ToString(),
                                Type = noteMessageType,
                                State = (NoteMessageState)int.Parse(npgsqlDataReader["state"].ToString())
                            };
                            giftMessages.Add(messageModel);
                        }
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return giftMessages;
        }

        public static List<MessageModel> GetMessages(long OwnerId)
        {
            List<MessageModel> messages = new List<MessageModel>();
            if (OwnerId == 0)
                return messages;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_messages WHERE owner_id=@owner";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        NoteMessageType noteMessageType = (NoteMessageType)int.Parse(npgsqlDataReader["type"].ToString());
                        if (noteMessageType != NoteMessageType.Gift)
                        {
                            MessageModel messageModel = new MessageModel((long)uint.Parse(npgsqlDataReader["expire_date"].ToString()), DateTimeUtil.Now())
                            {
                                ObjectId = long.Parse(npgsqlDataReader["object_id"].ToString()),
                                SenderId = long.Parse(npgsqlDataReader["sender_id"].ToString()),
                                SenderName = npgsqlDataReader["sender_name"].ToString(),
                                ClanId = int.Parse(npgsqlDataReader["clan_id"].ToString()),
                                ClanNote = (NoteMessageClan)int.Parse(npgsqlDataReader["clan_note"].ToString()),
                                Text = npgsqlDataReader["text"].ToString(),
                                Type = noteMessageType,
                                State = (NoteMessageState)int.Parse(npgsqlDataReader["state"].ToString())
                            };
                            messages.Add(messageModel);
                        }
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return messages;
        }

        public static bool MessageExists(long ObjectId, long OwnerId)
        {
            if (ObjectId != 0)
            {
                if (OwnerId != 0)
                {
                    try
                    {
                        int num = 0;
                        using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                        {
                            NpgsqlCommand command = npgsqlConnection.CreateCommand();
                            npgsqlConnection.Open();
                            command.Parameters.AddWithValue("@obj", (object)ObjectId);
                            command.Parameters.AddWithValue("@owner", (object)OwnerId);
                            command.CommandText = "SELECT COUNT(*) FROM player_messages WHERE object_id=@obj AND owner_id=@owner";
                            num = Convert.ToInt32(command.ExecuteScalar());
                            command.Dispose();
                            npgsqlConnection.Dispose();
                            npgsqlConnection.Close();
                        }
                        return num > 0;
                    }
                    catch (Exception ex)
                    {
                        CLogger.Print(ex.Message, LoggerType.Error, ex);
                    }
                    return false;
                }
            }
            return false;
        }

        public static int GetMessagesCount(long OwnerId)
        {
            int messagesCount = 0;
            if (OwnerId == 0)
                return messagesCount;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "SELECT COUNT(*) FROM player_messages WHERE owner_id=@owner";
                    messagesCount = Convert.ToInt32(command.ExecuteScalar());
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return messagesCount;
        }

        public static bool CreateMessage(long OwnerId, MessageModel Message)
        {
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.Parameters.AddWithValue("@sendid", (object)Message.SenderId);
                    command.Parameters.AddWithValue("@clan", (object)Message.ClanId);
                    command.Parameters.AddWithValue("@sendname", (object)Message.SenderName);
                    command.Parameters.AddWithValue("@text", (object)Message.Text);
                    command.Parameters.AddWithValue("@type", (object)(int)Message.Type);
                    command.Parameters.AddWithValue("@state", (object)(int)Message.State);
                    command.Parameters.AddWithValue("@expire", (object)Message.ExpireDate);
                    command.Parameters.AddWithValue("@cb", (object)(int)Message.ClanNote);
                    command.CommandType = CommandType.Text;
                    command.CommandText = "INSERT INTO player_messages(owner_id, sender_id, sender_name, clan_id, clan_note, text, type, state, expire_date) VALUES(@owner, @sendid, @sendname, @clan, @cb, @text, @type, @state, @expire) RETURNING object_id";
                    object obj = command.ExecuteScalar();
                    Message.ObjectId = (long)obj;
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static void UpdateState(long ObjectId, long OwnerId, int Value)
        {
            ComDiv.UpdateDB("player_messages", "state", (object)Value, "object_id", (object)ObjectId, "owner_id", (object)OwnerId);
        }

        public static void UpdateExpireDate(long ObjectId, long OwnerId, uint Date)
        {
            ComDiv.UpdateDB("player_messages", "expire_date", (object)(long)Date, "object_id", (object)ObjectId, "owner_id", (object)OwnerId);
        }

        public static bool DeleteMessage(long ObjectId, long OwnerId)
        {
            return ObjectId != 0 && OwnerId != 0 && ComDiv.DeleteDB("player_messages", "object_id", (object)ObjectId, "owner_id", (object)OwnerId);
        }

        public static bool DeleteMessages(List<object> ObjectIds, long OwnerId)
        {
            return ObjectIds.Count != 0 && OwnerId != 0 && ComDiv.DeleteDB("player_messages", "object_id", ObjectIds.ToArray(), "owner_id", (object)OwnerId);
        }

        public static void RecycleMessages(long OwnerId, List<MessageModel> Messages)
        {
            List<object> ObjectIds = new List<object>();
            for (int index = 0; index < Messages.Count; ++index)
            {
                MessageModel message = Messages[index];
                if (message.DaysRemaining == 0)
                {
                    ObjectIds.Add((object)message.ObjectId);
                    Messages.RemoveAt(index--);
                }
            }
            DaoManagerSQL.DeleteMessages(ObjectIds, OwnerId);
        }

        public static PlayerEquipment GetPlayerEquipmentsDB(long OwnerId)
        {
            PlayerEquipment playerEquipmentsDb = (PlayerEquipment)null;
            if (OwnerId == 0)
                return playerEquipmentsDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_equipments WHERE owner_id=@id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        playerEquipmentsDb = new PlayerEquipment()
                        {
                            OwnerId = OwnerId,
                            WeaponPrimary = int.Parse(npgsqlDataReader["weapon_primary"].ToString()),
                            WeaponSecondary = int.Parse(npgsqlDataReader["weapon_secondary"].ToString()),
                            WeaponMelee = int.Parse(npgsqlDataReader["weapon_melee"].ToString()),
                            WeaponExplosive = int.Parse(npgsqlDataReader["weapon_explosive"].ToString()),
                            WeaponSpecial = int.Parse(npgsqlDataReader["weapon_special"].ToString()),
                            CharaRedId = int.Parse(npgsqlDataReader["chara_red_side"].ToString()),
                            CharaBlueId = int.Parse(npgsqlDataReader["chara_blue_side"].ToString()),
                            DinoItem = int.Parse(npgsqlDataReader["dino_item_chara"].ToString()),
                            PartHead = int.Parse(npgsqlDataReader["part_head"].ToString()),
                            PartFace = int.Parse(npgsqlDataReader["part_face"].ToString()),
                            PartJacket = int.Parse(npgsqlDataReader["part_jacket"].ToString()),
                            PartPocket = int.Parse(npgsqlDataReader["part_pocket"].ToString()),
                            PartGlove = int.Parse(npgsqlDataReader["part_glove"].ToString()),
                            PartBelt = int.Parse(npgsqlDataReader["part_belt"].ToString()),
                            PartHolster = int.Parse(npgsqlDataReader["part_holster"].ToString()),
                            PartSkin = int.Parse(npgsqlDataReader["part_skin"].ToString()),
                            BeretItem = int.Parse(npgsqlDataReader["beret_item_part"].ToString()),
                            AccessoryId = int.Parse(npgsqlDataReader["accesory_id"].ToString()),
                            SprayId = int.Parse(npgsqlDataReader["spray_id"].ToString()),
                            NameCardId = int.Parse(npgsqlDataReader["namecard_id"].ToString())
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerEquipmentsDb;
        }

        public static bool CreatePlayerEquipmentsDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_equipments(owner_id) VALUES(@id)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static List<CharacterModel> GetPlayerCharactersDB(long OwnerId)
        {
            List<CharacterModel> playerCharactersDb = new List<CharacterModel>();
            if (OwnerId == 0)
                return playerCharactersDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@OwnerId", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_characters WHERE owner_id=@OwnerId ORDER BY slot ASC;";
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        CharacterModel characterModel = new CharacterModel()
                        {
                            ObjectId = long.Parse(npgsqlDataReader["object_id"].ToString()),
                            Id = int.Parse(npgsqlDataReader["id"].ToString()),
                            Slot = int.Parse(npgsqlDataReader["slot"].ToString()),
                            Name = npgsqlDataReader["name"].ToString(),
                            CreateDate = uint.Parse(npgsqlDataReader["create_date"].ToString()),
                            PlayTime = uint.Parse(npgsqlDataReader["playtime"].ToString())
                        };
                        playerCharactersDb.Add(characterModel);
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerCharactersDb;
        }

        public static bool CreatePlayerCharacter(CharacterModel Chara, long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner_id", (object)OwnerId);
                    command.Parameters.AddWithValue("@id", (object)Chara.Id);
                    command.Parameters.AddWithValue("@slot", (object)Chara.Slot);
                    command.Parameters.AddWithValue("@name", (object)Chara.Name);
                    command.Parameters.AddWithValue("@createdate", (object)(long)Chara.CreateDate);
                    command.Parameters.AddWithValue("@playtime", (object)(long)Chara.PlayTime);
                    command.CommandType = CommandType.Text;
                    command.CommandText = "INSERT INTO player_characters(owner_id, id, slot, name, create_date, playtime) VALUES(@owner_id, @id, @slot, @name, @createdate, @playtime) RETURNING object_id";
                    object obj = command.ExecuteScalar();
                    Chara.ObjectId = (long)obj;
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static StatisticTotal GetPlayerStatBasicDB(long OwnerId)
        {
            StatisticTotal playerStatBasicDb = (StatisticTotal)null;
            if (OwnerId == 0)
                return playerStatBasicDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_stat_basics WHERE owner_id=@id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        playerStatBasicDb = new StatisticTotal()
                        {
                            OwnerId = OwnerId,
                            Matches = int.Parse(npgsqlDataReader["matches"].ToString()),
                            MatchWins = int.Parse(npgsqlDataReader["match_wins"].ToString()),
                            MatchLoses = int.Parse(npgsqlDataReader["match_loses"].ToString()),
                            MatchDraws = int.Parse(npgsqlDataReader["match_draws"].ToString()),
                            KillsCount = int.Parse(npgsqlDataReader["kills_count"].ToString()),
                            DeathsCount = int.Parse(npgsqlDataReader["deaths_count"].ToString()),
                            HeadshotsCount = int.Parse(npgsqlDataReader["headshots_count"].ToString()),
                            AssistsCount = int.Parse(npgsqlDataReader["assists_count"].ToString()),
                            EscapesCount = int.Parse(npgsqlDataReader["escapes_count"].ToString()),
                            MvpCount = int.Parse(npgsqlDataReader["mvp_count"].ToString()),
                            TotalMatchesCount = int.Parse(npgsqlDataReader["total_matches"].ToString()),
                            TotalKillsCount = int.Parse(npgsqlDataReader["total_kills"].ToString())
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerStatBasicDb;
        }

        public static bool CreatePlayerStatBasicDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_stat_basics(owner_id) VALUES(@id)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static StatisticSeason GetPlayerStatSeasonDB(long OwnerId)
        {
            StatisticSeason playerStatSeasonDb = (StatisticSeason)null;
            if (OwnerId == 0)
                return playerStatSeasonDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_stat_seasons WHERE owner_id=@id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        playerStatSeasonDb = new StatisticSeason()
                        {
                            OwnerId = OwnerId,
                            Matches = int.Parse(npgsqlDataReader["matches"].ToString()),
                            MatchWins = int.Parse(npgsqlDataReader["match_wins"].ToString()),
                            MatchLoses = int.Parse(npgsqlDataReader["match_loses"].ToString()),
                            MatchDraws = int.Parse(npgsqlDataReader["match_draws"].ToString()),
                            KillsCount = int.Parse(npgsqlDataReader["kills_count"].ToString()),
                            DeathsCount = int.Parse(npgsqlDataReader["deaths_count"].ToString()),
                            HeadshotsCount = int.Parse(npgsqlDataReader["headshots_count"].ToString()),
                            AssistsCount = int.Parse(npgsqlDataReader["assists_count"].ToString()),
                            EscapesCount = int.Parse(npgsqlDataReader["escapes_count"].ToString()),
                            MvpCount = int.Parse(npgsqlDataReader["mvp_count"].ToString()),
                            TotalMatchesCount = int.Parse(npgsqlDataReader["total_matches"].ToString()),
                            TotalKillsCount = int.Parse(npgsqlDataReader["total_kills"].ToString())
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerStatSeasonDb;
        }

        public static bool CreatePlayerStatSeasonDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_stat_seasons(owner_id) VALUES(@id)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static StatisticClan GetPlayerStatClanDB(long OwnerId)
        {
            StatisticClan playerStatClanDb = (StatisticClan)null;
            if (OwnerId == 0)
                return playerStatClanDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_stat_clans WHERE owner_id=@id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        playerStatClanDb = new StatisticClan()
                        {
                            OwnerId = OwnerId,
                            Matches = int.Parse(npgsqlDataReader["clan_matches"].ToString()),
                            MatchWins = int.Parse(npgsqlDataReader["clan_match_wins"].ToString()),
                            MatchLoses = int.Parse(npgsqlDataReader["clan_match_loses"].ToString())
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerStatClanDb;
        }

        public static bool CreatePlayerStatClanDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_stat_clans(owner_id) VALUES(@id)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static StatisticDaily GetPlayerStatDailiesDB(long OwnerId)
        {
            StatisticDaily playerStatDailiesDb = (StatisticDaily)null;
            if (OwnerId == 0)
                return playerStatDailiesDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_stat_dailies WHERE owner_id=@id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        playerStatDailiesDb = new StatisticDaily()
                        {
                            OwnerId = OwnerId,
                            Matches = int.Parse(npgsqlDataReader["matches"].ToString()),
                            MatchWins = int.Parse(npgsqlDataReader["match_wins"].ToString()),
                            MatchLoses = int.Parse(npgsqlDataReader["match_loses"].ToString()),
                            MatchDraws = int.Parse(npgsqlDataReader["match_draws"].ToString()),
                            KillsCount = int.Parse(npgsqlDataReader["kills_count"].ToString()),
                            DeathsCount = int.Parse(npgsqlDataReader["deaths_count"].ToString()),
                            HeadshotsCount = int.Parse(npgsqlDataReader["headshots_count"].ToString()),
                            ExpGained = int.Parse(npgsqlDataReader["exp_gained"].ToString()),
                            PointGained = int.Parse(npgsqlDataReader["point_gained"].ToString()),
                            Playtime = uint.Parse($"{npgsqlDataReader["playtime"]}")
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerStatDailiesDb;
        }

        public static bool CreatePlayerStatDailiesDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_stat_dailies(owner_id) VALUES(@id)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static StatisticWeapon GetPlayerStatWeaponsDB(long OwnerId)
        {
            StatisticWeapon playerStatWeaponsDb = (StatisticWeapon)null;
            if (OwnerId == 0)
                return playerStatWeaponsDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_stat_weapons WHERE owner_id=@id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        playerStatWeaponsDb = new StatisticWeapon()
                        {
                            OwnerId = OwnerId,
                            AssaultKills = int.Parse(npgsqlDataReader["assault_rifle_kills"].ToString()),
                            AssaultDeaths = int.Parse(npgsqlDataReader["assault_rifle_deaths"].ToString()),
                            SmgKills = int.Parse(npgsqlDataReader["sub_machine_gun_kills"].ToString()),
                            SmgDeaths = int.Parse(npgsqlDataReader["sub_machine_gun_deaths"].ToString()),
                            SniperKills = int.Parse(npgsqlDataReader["sniper_rifle_kills"].ToString()),
                            SniperDeaths = int.Parse(npgsqlDataReader["sniper_rifle_deaths"].ToString()),
                            MachinegunKills = int.Parse(npgsqlDataReader["machine_gun_kills"].ToString()),
                            MachinegunDeaths = int.Parse(npgsqlDataReader["machine_gun_deaths"].ToString()),
                            ShotgunKills = int.Parse(npgsqlDataReader["shot_gun_kills"].ToString()),
                            ShotgunDeaths = int.Parse(npgsqlDataReader["shot_gun_deaths"].ToString())
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerStatWeaponsDb;
        }

        public static bool CreatePlayerStatWeaponsDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_stat_weapons(owner_id) VALUES(@id)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static StatisticAcemode GetPlayerStatAcemodesDB(long OwnerId)
        {
            StatisticAcemode playerStatAcemodesDb = (StatisticAcemode)null;
            if (OwnerId == 0)
                return playerStatAcemodesDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_stat_acemodes WHERE owner_id=@id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        playerStatAcemodesDb = new StatisticAcemode()
                        {
                            OwnerId = OwnerId,
                            Matches = int.Parse(npgsqlDataReader["matches"].ToString()),
                            MatchWins = int.Parse(npgsqlDataReader["match_wins"].ToString()),
                            MatchLoses = int.Parse(npgsqlDataReader["match_loses"].ToString()),
                            Kills = int.Parse(npgsqlDataReader["kills_count"].ToString()),
                            Deaths = int.Parse(npgsqlDataReader["deaths_count"].ToString()),
                            Headshots = int.Parse(npgsqlDataReader["headshots_count"].ToString()),
                            Assists = int.Parse(npgsqlDataReader["assists_count"].ToString()),
                            Escapes = int.Parse(npgsqlDataReader["escapes_count"].ToString()),
                            Winstreaks = int.Parse(npgsqlDataReader["winstreaks_count"].ToString())
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerStatAcemodesDb;
        }

        public static bool CreatePlayerStatAcemodesDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_stat_acemodes(owner_id) VALUES(@id)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static StatisticBattlecup GetPlayerStatBattlecupDB(long OwnerId)
        {
            StatisticBattlecup playerStatBattlecupDb = (StatisticBattlecup)null;
            if (OwnerId == 0)
                return playerStatBattlecupDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_stat_battlecups WHERE owner_id=@id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        playerStatBattlecupDb = new StatisticBattlecup()
                        {
                            OwnerId = OwnerId,
                            Matches = int.Parse(npgsqlDataReader["matches"].ToString()),
                            MatchWins = int.Parse(npgsqlDataReader["match_wins"].ToString()),
                            MatchLoses = int.Parse(npgsqlDataReader["match_loses"].ToString()),
                            KillsCount = int.Parse(npgsqlDataReader["kills_count"].ToString()),
                            DeathsCount = int.Parse(npgsqlDataReader["deaths_count"].ToString()),
                            HeadshotsCount = int.Parse(npgsqlDataReader["headshots_count"].ToString()),
                            AssistsCount = int.Parse(npgsqlDataReader["assists_count"].ToString()),
                            EscapesCount = int.Parse(npgsqlDataReader["escapes_count"].ToString())
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerStatBattlecupDb;
        }

        public static bool CreatePlayerStatBattlecupsDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_stat_battlecups(owner_id) VALUES(@id)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static PlayerTitles GetPlayerTitlesDB(long OwnerId)
        {
            PlayerTitles playerTitlesDb = (PlayerTitles)null;
            if (OwnerId == 0)
                return playerTitlesDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_titles WHERE owner_id=@id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        playerTitlesDb = new PlayerTitles()
                        {
                            OwnerId = OwnerId,
                            Equiped1 = int.Parse(npgsqlDataReader["equip_slot1"].ToString()),
                            Equiped2 = int.Parse(npgsqlDataReader["equip_slot2"].ToString()),
                            Equiped3 = int.Parse(npgsqlDataReader["equip_slot3"].ToString()),
                            Flags = long.Parse(npgsqlDataReader["flags"].ToString()),
                            Slots = int.Parse(npgsqlDataReader["slots"].ToString())
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerTitlesDb;
        }

        public static bool CreatePlayerTitlesDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_titles(owner_id) VALUES(@id)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static PlayerBonus GetPlayerBonusDB(long OwnerId)
        {
            PlayerBonus playerBonusDb = (PlayerBonus)null;
            if (OwnerId == 0)
                return playerBonusDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_bonus WHERE owner_id=@id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        playerBonusDb = new PlayerBonus()
                        {
                            OwnerId = OwnerId,
                            Bonuses = int.Parse(npgsqlDataReader["bonuses"].ToString()),
                            CrosshairColor = int.Parse(npgsqlDataReader["crosshair_color"].ToString()),
                            FreePass = int.Parse(npgsqlDataReader["free_pass"].ToString()),
                            FakeRank = int.Parse(npgsqlDataReader["fake_rank"].ToString()),
                            FakeNick = npgsqlDataReader["fake_nick"].ToString(),
                            MuzzleColor = int.Parse(npgsqlDataReader["muzzle_color"].ToString()),
                            NickBorderColor = int.Parse(npgsqlDataReader["nick_border_color"].ToString())
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerBonusDb;
        }

        public static bool CreatePlayerBonusDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_bonus(owner_id) VALUES(@id)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static PlayerConfig GetPlayerConfigDB(long OwnerId)
        {
            PlayerConfig playerConfigDb = (PlayerConfig)null;
            if (OwnerId == 0)
                return playerConfigDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_configs WHERE owner_id=@owner";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        playerConfigDb = new PlayerConfig()
                        {
                            OwnerId = OwnerId,
                            Config = int.Parse(npgsqlDataReader["configs"].ToString()),
                            ShowBlood = int.Parse(npgsqlDataReader["show_blood"].ToString()),
                            Crosshair = int.Parse(npgsqlDataReader["crosshair"].ToString()),
                            HandPosition = int.Parse(npgsqlDataReader["hand_pos"].ToString()),
                            AudioSFX = int.Parse(npgsqlDataReader["audio_sfx"].ToString()),
                            AudioBGM = int.Parse(npgsqlDataReader["audio_bgm"].ToString()),
                            AudioEnable = int.Parse(npgsqlDataReader["audio_enable"].ToString()),
                            Sensitivity = int.Parse(npgsqlDataReader["sensitivity"].ToString()),
                            PointOfView = int.Parse(npgsqlDataReader["pov_size"].ToString()),
                            InvertMouse = int.Parse(npgsqlDataReader["invert_mouse"].ToString()),
                            EnableInviteMsg = int.Parse(npgsqlDataReader["enable_invite"].ToString()),
                            EnableWhisperMsg = int.Parse(npgsqlDataReader["enable_whisper"].ToString()),
                            Macro = int.Parse(npgsqlDataReader["macro_enable"].ToString()),
                            Macro1 = npgsqlDataReader["macro1"].ToString(),
                            Macro2 = npgsqlDataReader["macro2"].ToString(),
                            Macro3 = npgsqlDataReader["macro3"].ToString(),
                            Macro4 = npgsqlDataReader["macro4"].ToString(),
                            Macro5 = npgsqlDataReader["macro5"].ToString(),
                            Nations = int.Parse(npgsqlDataReader["nations"].ToString())
                        };
                        npgsqlDataReader.GetBytes(19, 0, playerConfigDb.KeyboardKeys, 0, 240);
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerConfigDb;
        }

        public static bool CreatePlayerConfigDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_configs(owner_id) VALUES(@owner)";
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static PlayerEvent GetPlayerEventDB(long ownerId)
        {
            if (ownerId == 0)
            {
                return null;
            }

            try
            {
                using (var connection = ConnectionSQL.GetInstance().Conn())
                using (var command = connection.CreateCommand())
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@id", ownerId);
                    command.CommandText = @"SELECT owner_id, last_visit_check_day, last_visit_seq_type, last_visit_date, last_xmas_date, last_playtime_date, last_playtime_value, last_playtime_finish, last_login_date, last_quest_date, last_quest_finish, current_playtime_event_id,  playtime_completed_levels FROM player_events WHERE owner_id = @id";

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new PlayerEvent
                            {
                                OwnerId = reader.GetInt64(0),
                                LastVisitCheckDay = reader.GetInt32(1),
                                LastVisitSeqType = reader.GetInt32(2),
                                LastVisitDate = (uint)reader.GetInt64(3), // int8 -> uint requiere cast
                                LastXmasDate = (uint)reader.GetInt64(4),
                                LastPlaytimeDate = (uint)reader.GetInt64(5),
                                LastPlaytimeValue = reader.GetInt32(6), // Es int4, no int8
                                LastPlaytimeFinish = reader.GetInt32(7),
                                LastLoginDate = (uint)reader.GetInt64(8),
                                LastQuestDate = (uint)reader.GetInt64(9),
                                LastQuestFinish = reader.GetInt32(10),
                                CurrentPlaytimeEventId = reader.GetInt32(11),
                                PlaytimeCompletedLevels = reader.GetString(12)
                            };
                        }
                    }
                }

                return null; // No existe el registro
            }
            catch (Exception ex)
            {
                CLogger.Print($"Error al obtener PlayerEvent para OwnerId {ownerId}: {ex.Message}",
                              LoggerType.Error, ex);
                return null;
            }
        }

        public static bool CreatePlayerEventDB(long OwnerId)
        {
            if (OwnerId == 0)
            {
                return false;
            }
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_events (owner_id) VALUES (@id)";
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static List<FriendModel> GetPlayerFriendsDB(long OwnerId)
        {
            List<FriendModel> playerFriendsDb = new List<FriendModel>();
            if (OwnerId == 0)
            {
                return null;
            }
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_friends WHERE owner_id=@owner ORDER BY id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        FriendModel friendModel = new FriendModel(long.Parse(npgsqlDataReader["id"].ToString()))
                        {
                            OwnerId = OwnerId,
                            ObjectId = long.Parse(npgsqlDataReader["object_id"].ToString()),
                            State = int.Parse(npgsqlDataReader["state"].ToString()),
                            Removed = bool.Parse(npgsqlDataReader["removed"].ToString())
                        };
                        playerFriendsDb.Add(friendModel);
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return playerFriendsDb;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return null;
            }
        }

        public static void UpdatePlayerBonus(long PlayerId, int Bonuses, int FreePass)
        {
            if (PlayerId == 0)
            {
                return;
            }
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@id", (object)PlayerId);
                    command.Parameters.AddWithValue("@bonuses", (object)Bonuses);
                    command.Parameters.AddWithValue("@freepass", (object)FreePass);
                    command.CommandText = "UPDATE player_bonus SET bonuses=@bonuses, free_pass=@freepass WHERE owner_id=@id";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        public static List<QuickstartModel> GetPlayerQuickstartsDB(long OwnerId)
        {
            List<QuickstartModel> playerQuickstartsDb = new List<QuickstartModel>();
            if (OwnerId == 0)
                return playerQuickstartsDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_quickstarts WHERE owner_id=@owner;";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        QuickstartModel quickstartModel1 = new QuickstartModel()
                        {
                            MapId = (int)byte.Parse(npgsqlDataReader["list0_map_id"].ToString()),
                            Rule = (int)byte.Parse(npgsqlDataReader["list0_map_rule"].ToString()),
                            StageOptions = (int)byte.Parse(npgsqlDataReader["list0_map_stage"].ToString()),
                            Type = (int)byte.Parse(npgsqlDataReader["list0_map_type"].ToString())
                        };
                        playerQuickstartsDb.Add(quickstartModel1);
                        QuickstartModel quickstartModel2 = new QuickstartModel()
                        {
                            MapId = (int)byte.Parse(npgsqlDataReader["list1_map_id"].ToString()),
                            Rule = (int)byte.Parse(npgsqlDataReader["list1_map_rule"].ToString()),
                            StageOptions = (int)byte.Parse(npgsqlDataReader["list1_map_stage"].ToString()),
                            Type = (int)byte.Parse(npgsqlDataReader["list1_map_type"].ToString())
                        };
                        playerQuickstartsDb.Add(quickstartModel2);
                        QuickstartModel quickstartModel3 = new QuickstartModel()
                        {
                            MapId = (int)byte.Parse(npgsqlDataReader["list2_map_id"].ToString()),
                            Rule = (int)byte.Parse(npgsqlDataReader["list2_map_rule"].ToString()),
                            StageOptions = (int)byte.Parse(npgsqlDataReader["list2_map_stage"].ToString()),
                            Type = (int)byte.Parse(npgsqlDataReader["list2_map_type"].ToString())
                        };
                        playerQuickstartsDb.Add(quickstartModel3);
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerQuickstartsDb;
        }

        public static bool CreatePlayerQuickstartsDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_quickstarts(owner_id) VALUES(@owner);";
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static bool IsPlayerNameExist(string Nickname)
        {
            if (string.IsNullOrEmpty(Nickname))
                return true;
            try
            {
                int num = 0;
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@name", (object)Nickname);
                    command.CommandText = "SELECT COUNT(*) FROM accounts WHERE nickname=@name";
                    num = Convert.ToInt32(command.ExecuteScalar());
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return num > 0;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static List<NHistoryModel> GetPlayerNickHistory(object Value, int Type)
        {
            List<NHistoryModel> playerNickHistory = new List<NHistoryModel>();
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    string str = Type == 0 ? "WHERE new_nick=@valor" : "WHERE owner_id=@valor";
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@valor", Value);
                    command.CommandText = $"SELECT * FROM base_nick_history {str} ORDER BY change_date LIMIT 30";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        NHistoryModel nhistoryModel = new NHistoryModel()
                        {
                            ObjectId = long.Parse(npgsqlDataReader["object_id"].ToString()),
                            OwnerId = long.Parse(npgsqlDataReader["owner_id"].ToString()),
                            OldNick = npgsqlDataReader["old_nick"].ToString(),
                            NewNick = npgsqlDataReader["new_nick"].ToString(),
                            ChangeDate = uint.Parse(npgsqlDataReader["change_date"].ToString()),
                            Motive = npgsqlDataReader["motive"].ToString()
                        };
                        playerNickHistory.Add(nhistoryModel);
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerNickHistory;
        }

        public static bool CreatePlayerNickHistory(
          long OwnerId,
          string OldNick,
          string NewNick,
          string Motive)
        {
            NHistoryModel nhistoryModel = new NHistoryModel()
            {
                OwnerId = OwnerId,
                OldNick = OldNick,
                NewNick = NewNick,
                ChangeDate = uint.Parse(DateTimeUtil.Now("yyMMddHHmm")),
                Motive = Motive
            };
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)nhistoryModel.OwnerId);
                    command.Parameters.AddWithValue("@oldnick", (object)nhistoryModel.OldNick);
                    command.Parameters.AddWithValue("@newnick", (object)nhistoryModel.NewNick);
                    command.Parameters.AddWithValue("@date", (object)(long)nhistoryModel.ChangeDate);
                    command.Parameters.AddWithValue("@motive", (object)nhistoryModel.Motive);
                    command.CommandType = CommandType.Text;
                    command.CommandText = "INSERT INTO base_nick_history(owner_id, old_nick, new_nick, change_date, motive) VALUES(@owner, @oldnick, @newnick, @date, @motive)";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static bool UpdateAccountValuable(long PlayerId, int Gold, int Cash, int Tags)
        {
            if (PlayerId == 0 || Gold == -1 && Cash == -1 && Tags == -1)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@owner", (object)PlayerId);
                    string str = "";
                    if (Gold > -1)
                    {
                        command.Parameters.AddWithValue("@gold", (object)Gold);
                        str += "gold=@gold";
                    }
                    if (Cash > -1)
                    {
                        command.Parameters.AddWithValue("@cash", (object)Cash);
                        str = $"{str}{(str != "" ? ", " : "")}cash=@cash";
                    }
                    if (Tags > -1)
                    {
                        command.Parameters.AddWithValue("@tags", (object)Tags);
                        str = $"{str}{(str != "" ? ", " : "")}tags=@tags";
                    }
                    command.CommandText = $"UPDATE accounts SET {str} WHERE player_id=@owner";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static bool UpdatePlayerKD(
          long OwnerId,
          int Kills,
          int Deaths,
          int Headshots,
          int Totals)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.Parameters.AddWithValue("@deaths", (object)Deaths);
                    command.Parameters.AddWithValue("@kills", (object)Kills);
                    command.Parameters.AddWithValue("@hs", (object)Headshots);
                    command.Parameters.AddWithValue("@total", (object)Totals);
                    command.CommandText = "UPDATE player_stat_seasons SET kills_count=@kills, deaths_count=@deaths, headshots_count=@hs, total_kills=@total WHERE owner_id=@owner";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static bool UpdatePlayerMatches(
          int Matches,
          int MatchWins,
          int MatchLoses,
          int MatchDraws,
          int Totals,
          long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.Parameters.AddWithValue("@partidas", (object)Matches);
                    command.Parameters.AddWithValue("@ganhas", (object)MatchWins);
                    command.Parameters.AddWithValue("@perdidas", (object)MatchLoses);
                    command.Parameters.AddWithValue("@empates", (object)MatchDraws);
                    command.Parameters.AddWithValue("@todaspartidas", (object)Totals);
                    command.CommandText = "UPDATE player_stat_seasons SET matches=@partidas, match_wins=@ganhas, match_loses=@perdidas, match_draws=@empates, total_matches=@todaspartidas WHERE owner_id=@owner";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static bool UpdateAccountCash(long OwnerId, int Cash)
        {
            if (OwnerId != 0)
            {
                if (Cash != -1)
                {
                    try
                    {
                        using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                        {
                            NpgsqlCommand command = npgsqlConnection.CreateCommand();
                            npgsqlConnection.Open();
                            command.CommandType = CommandType.Text;
                            command.Parameters.AddWithValue("@owner", (object)OwnerId);
                            command.Parameters.AddWithValue("@cash", (object)Cash);
                            command.CommandText = "UPDATE accounts SET cash=@cash WHERE player_id=@owner";
                            command.ExecuteNonQuery();
                            command.Dispose();
                            npgsqlConnection.Dispose();
                            npgsqlConnection.Close();
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        CLogger.Print(ex.Message, LoggerType.Error, ex);
                        return false;
                    }
                }
            }
            return false;
        }

        public static bool UpdateAccountGold(long OwnerId, int Gold)
        {
            if (OwnerId != 0)
            {
                if (Gold != -1)
                {
                    try
                    {
                        using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                        {
                            NpgsqlCommand command = npgsqlConnection.CreateCommand();
                            npgsqlConnection.Open();
                            command.CommandType = CommandType.Text;
                            command.Parameters.AddWithValue("@owner", (object)OwnerId);
                            command.Parameters.AddWithValue("@gold", (object)Gold);
                            command.CommandText = "UPDATE accounts SET gold=@gold WHERE player_id=@owner";
                            command.ExecuteNonQuery();
                            command.Dispose();
                            npgsqlConnection.Dispose();
                            npgsqlConnection.Close();
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        CLogger.Print(ex.Message, LoggerType.Error, ex);
                        return false;
                    }
                }
            }
            return false;
        }

        public static bool UpdateAccountTags(long OwnerId, int Tags)
        {
            if (OwnerId != 0)
            {
                if (Tags != -1)
                {
                    try
                    {
                        using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                        {
                            NpgsqlCommand command = npgsqlConnection.CreateCommand();
                            npgsqlConnection.Open();
                            command.CommandType = CommandType.Text;
                            command.Parameters.AddWithValue("@owner", (object)OwnerId);
                            command.Parameters.AddWithValue("@tag", (object)Tags);
                            command.CommandText = "UPDATE accounts SET tags=@tag WHERE player_id=@owner";
                            command.ExecuteNonQuery();
                            command.Dispose();
                            npgsqlConnection.Dispose();
                            npgsqlConnection.Close();
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        CLogger.Print(ex.Message, LoggerType.Error, ex);
                        return false;
                    }
                }
            }
            return false;
        }

        public static void UpdateCouponEffect(long PlayerId, CouponEffects Effects)
        {
            if (PlayerId == 0)
                return;
            ComDiv.UpdateDB("accounts", "coupon_effect", (object)(long)Effects, "player_id", (object)PlayerId);
        }

        public static int GetRequestClanId(long OwnerId)
        {
            int requestClanId = 0;
            if (OwnerId == 0)
                return requestClanId;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "SELECT clan_id FROM system_clan_invites WHERE player_id=@owner";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    if (npgsqlDataReader.Read())
                        requestClanId = int.Parse(npgsqlDataReader["clan_id"].ToString());
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return requestClanId;
        }

        public static int GetRequestClanCount(int ClanId)
        {
            int requestClanCount = 0;
            if (ClanId == 0)
                return requestClanCount;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@clan", (object)ClanId);
                    command.CommandText = "SELECT COUNT(*) FROM system_clan_invites WHERE clan_id=@clan";
                    requestClanCount = Convert.ToInt32(command.ExecuteScalar());
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return requestClanCount;
        }

        public static List<ClanInvite> GetClanRequestList(int ClanId)
        {
            List<ClanInvite> clanRequestList = new List<ClanInvite>();
            if (ClanId == 0)
                return clanRequestList;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@clan", (object)ClanId);
                    command.CommandText = "SELECT * FROM system_clan_invites WHERE clan_id=@clan";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        ClanInvite clanInvite = new ClanInvite()
                        {
                            Id = ClanId,
                            PlayerId = long.Parse(npgsqlDataReader["player_id"].ToString()),
                            InviteDate = uint.Parse(npgsqlDataReader["invite_date"].ToString()),
                            Text = npgsqlDataReader["text"].ToString()
                        };
                        clanRequestList.Add(clanInvite);
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return clanRequestList;
        }

        public static int GetPlayerMessagesCount(long OwnerId)
        {
            int playerMessagesCount = 0;
            if (OwnerId == 0)
                return playerMessagesCount;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "SELECT COUNT(*) FROM player_messages WHERE owner_id=@owner";
                    playerMessagesCount = Convert.ToInt32(command.ExecuteScalar());
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerMessagesCount;
        }

        public static bool CreatePlayerMessage(long OwnerId, MessageModel Message)
        {
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.Parameters.AddWithValue("@sendid", (object)Message.SenderId);
                    command.Parameters.AddWithValue("@clan", (object)Message.ClanId);
                    command.Parameters.AddWithValue("@sendname", (object)Message.SenderName);
                    command.Parameters.AddWithValue("@text", (object)Message.Text);
                    command.Parameters.AddWithValue("@type", (object)Message.Type);
                    command.Parameters.AddWithValue("@state", (object)Message.State);
                    command.Parameters.AddWithValue("@expire", (object)Message.ExpireDate);
                    command.Parameters.AddWithValue("@cb", (object)(int)Message.ClanNote);
                    command.CommandType = CommandType.Text;
                    command.CommandText = "INSERT INTO player_messages(owner_id, sender_id, sender_name, clan_id, clan_note, text, type, state, expire)VALUES(@owner, @sendid, @sendname, @clan, @cb, @text, @type, @state, @expire) RETURNING object_id";
                    object obj = command.ExecuteScalar();
                    Message.ObjectId = (long)obj;
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static bool DeletePlayerFriend(long friendId, long pId)
        {
            return ComDiv.DeleteDB("player_friends", "id", (object)friendId, "owner_id", (object)pId);
        }

        public static void UpdatePlayerFriendState(long ownerId, FriendModel friend)
        {
            ComDiv.UpdateDB("player_friends", "state", (object)friend.State, "owner_id", (object)ownerId, "id", (object)friend.PlayerId);
        }

        public static void UpdatePlayerFriendBlock(long OwnerId, FriendModel Friend)
        {
            ComDiv.UpdateDB("player_friends", "removed", (object)Friend.Removed, "owner_id", (object)OwnerId, "id", (object)Friend.PlayerId);
        }

        public static bool DeleteClanInviteDB(int ClanId, long PlayerId)
        {
            return PlayerId != 0 && ClanId != 0 && ComDiv.DeleteDB("system_clan_invites", "clan_id", (object)ClanId, "player_id", (object)PlayerId);
        }

        public static bool DeleteClanInviteDB(long PlayerId)
        {
            return PlayerId != 0 && ComDiv.DeleteDB("system_clan_invites", "player_id", (object)PlayerId);
        }

        public static bool CreateClanInviteInDB(ClanInvite invite)
        {
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@clan", (object)invite.Id);
                    command.Parameters.AddWithValue("@player", (object)invite.PlayerId);
                    command.Parameters.AddWithValue("@date", (object)(long)invite.InviteDate);
                    command.Parameters.AddWithValue("@text", (object)invite.Text);
                    command.CommandText = "INSERT INTO system_clan_invites(clan_id, player_id, invite_date, text)VALUES(@clan,@player,@date,@text)";
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static int GetRequestClanInviteCount(int clanId)
        {
            int requestClanInviteCount = 0;
            if (clanId == 0)
                return requestClanInviteCount;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@clan", (object)clanId);
                    command.CommandText = "SELECT COUNT(*) FROM system_clan_invites WHERE clan_id=@clan";
                    requestClanInviteCount = Convert.ToInt32(command.ExecuteScalar());
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return requestClanInviteCount;
        }

        public static string GetRequestClanInviteText(int ClanId, long PlayerId)
        {
            string requestClanInviteText = (string)null;
            if (ClanId != 0)
            {
                if (PlayerId != 0)
                {
                    try
                    {
                        using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                        {
                            NpgsqlCommand command = npgsqlConnection.CreateCommand();
                            npgsqlConnection.Open();
                            command.Parameters.AddWithValue("@clan", (object)ClanId);
                            command.Parameters.AddWithValue("@player", (object)PlayerId);
                            command.CommandText = "SELECT text FROM system_clan_invites WHERE clan_id=@clan AND player_id=@player";
                            command.CommandType = CommandType.Text;
                            NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                            if (npgsqlDataReader.Read())
                                requestClanInviteText = npgsqlDataReader["text"].ToString();
                            command.Dispose();
                            npgsqlDataReader.Close();
                            npgsqlConnection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        CLogger.Print(ex.Message, LoggerType.Error, ex);
                    }
                    return requestClanInviteText;
                }
            }
            return requestClanInviteText;
        }

        public static string GetPlayerIP4Address(long PlayerId)
        {
            string playerIp4Address = "";
            if (PlayerId == 0)
                return playerIp4Address;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@player", (object)PlayerId);
                    command.CommandText = "SELECT ip4_address FROM accounts WHERE player_id=@player";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    if (npgsqlDataReader.Read())
                        playerIp4Address = npgsqlDataReader["ip4_address"].ToString();
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerIp4Address;
        }

        public static PlayerMissions GetPlayerMissionsDB(
          long OwnerId,
          int Mission1,
          int Mission2,
          int Mission3,
          int Mission4)
        {
            PlayerMissions playerMissionsDb = (PlayerMissions)null;
            if (OwnerId == 0)
                return playerMissionsDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_missions WHERE owner_id=@owner";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        playerMissionsDb = new PlayerMissions()
                        {
                            OwnerId = OwnerId,
                            ActualMission = int.Parse(npgsqlDataReader["current_mission"].ToString()),
                            Card1 = int.Parse(npgsqlDataReader["card1"].ToString()),
                            Card2 = int.Parse(npgsqlDataReader["card2"].ToString()),
                            Card3 = int.Parse(npgsqlDataReader["card3"].ToString()),
                            Card4 = int.Parse(npgsqlDataReader["card4"].ToString()),
                            Mission1 = Mission1,
                            Mission2 = Mission2,
                            Mission3 = Mission3,
                            Mission4 = Mission4
                        };
                        npgsqlDataReader.GetBytes(6, 0, playerMissionsDb.List1, 0, 40);
                        npgsqlDataReader.GetBytes(7, 0, playerMissionsDb.List2, 0, 40);
                        npgsqlDataReader.GetBytes(8, 0, playerMissionsDb.List3, 0, 40);
                        npgsqlDataReader.GetBytes(9, 0, playerMissionsDb.List4, 0, 40);
                        playerMissionsDb.UpdateSelectedCard();
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerMissionsDb;
        }

        public static bool CreatePlayerMissionsDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_missions(owner_id) VALUES(@owner)";
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static void UpdateCurrentPlayerMissionList(long player_id, PlayerMissions mission)
        {
            byte[] currentMissionList = mission.GetCurrentMissionList();
            ComDiv.UpdateDB("player_missions", $"mission{mission.ActualMission + 1}_raw", (object)currentMissionList, "owner_id", (object)player_id);
        }

        public static bool DeletePlayerCharacter(long ObjectId, long OwnerId)
        {
            return ObjectId != 0 && OwnerId != 0 && ComDiv.DeleteDB("player_characters", "object_id", (object)ObjectId, "owner_id", (object)OwnerId);
        }

        public static bool UpdatePlayerCharacter(int Slot, long ObjectId, long OwnerId)
        {
            return ComDiv.UpdateDB("player_characters", "slot", (object)Slot, "object_id", (object)ObjectId, "owner_id", (object)OwnerId);
        }

        public static bool UpdateEquipedPlayerTitle(long player_id, int index, int titleId)
        {
            return ComDiv.UpdateDB("player_titles", $"equip_slot{index + 1}", (object)titleId, "owner_id", (object)player_id);
        }

        public static void UpdatePlayerTitlesFlags(long player_id, long flags)
        {
            ComDiv.UpdateDB("player_titles", nameof(flags), (object)flags, "owner_id", (object)player_id);
        }

        public static void UpdatePlayerTitleRequi(
          long player_id,
          int medalhas,
          int insignias,
          int ordens_azuis,
          int broche)
        {
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@pid", (object)player_id);
                    command.Parameters.AddWithValue("@broche", (object)broche);
                    command.Parameters.AddWithValue("@insignias", (object)insignias);
                    command.Parameters.AddWithValue("@medalhas", (object)medalhas);
                    command.Parameters.AddWithValue("@ordensazuis", (object)ordens_azuis);
                    command.CommandType = CommandType.Text;
                    command.CommandText = "UPDATE accounts SET ribbon=@broche, ensign=@insignias, medal=@medalhas, master_medal=@ordensazuis WHERE player_id=@pid";
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        public static bool UpdatePlayerMissionId(long player_id, int value, int index)
        {
            return ComDiv.UpdateDB("accounts", $"mission_id{index + 1}", (object)value, nameof(player_id), (object)player_id);
        }

        public static int GetUsedTicket(long OwnerId, string Token)
        {
            int usedTicket = 0;
            if (OwnerId != 0)
            {
                if (!string.IsNullOrEmpty(Token))
                {
                    try
                    {
                        using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                        {
                            NpgsqlCommand command = npgsqlConnection.CreateCommand();
                            npgsqlConnection.Open();
                            command.Parameters.AddWithValue("@player", (object)OwnerId);
                            command.Parameters.AddWithValue("@token", (object)Token);
                            command.CommandText = "SELECT used_count FROM base_redeem_history WHERE used_token=@token AND owner_id=@player";
                            command.CommandType = CommandType.Text;
                            NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                            if (npgsqlDataReader.Read())
                                usedTicket = int.Parse(npgsqlDataReader["used_count"].ToString());
                            command.Dispose();
                            npgsqlDataReader.Close();
                            npgsqlConnection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        CLogger.Print(ex.Message, LoggerType.Error, ex);
                    }
                    return usedTicket;
                }
            }
            return usedTicket;
        }

        public static bool IsTicketUsedByPlayer(long OwnerId, string Token)
        {
            bool flag = false;
            if (OwnerId == 0)
                return flag;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@player", (object)OwnerId);
                    command.Parameters.AddWithValue("@token", (object)Token);
                    command.CommandText = "SELECT * FROM base_redeem_history WHERE used_token=@token AND owner_id=@player";
                    command.CommandType = CommandType.Text;
                    flag = Convert.ToBoolean(command.ExecuteScalar());
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return flag;
        }

        public static bool CreatePlayerRedeemHistory(long OwnerId, string Token, int Used)
        {
            if (OwnerId != 0 && !string.IsNullOrEmpty(Token))
            {
                if (Used != 0)
                {
                    try
                    {
                        using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                        {
                            NpgsqlCommand command = npgsqlConnection.CreateCommand();
                            npgsqlConnection.Open();
                            command.Parameters.AddWithValue("@owner", (object)OwnerId);
                            command.Parameters.AddWithValue("@token", (object)Token);
                            command.Parameters.AddWithValue("@used", (object)Used);
                            command.CommandText = "INSERT INTO base_redeem_history(owner_id, used_token, used_count) VALUES(@owner, @token, @used)";
                            command.CommandType = CommandType.Text;
                            command.ExecuteNonQuery();
                            command.Dispose();
                            npgsqlConnection.Dispose();
                            npgsqlConnection.Close();
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        CLogger.Print(ex.Message, LoggerType.Error, ex);
                        return false;
                    }
                }
            }
            return false;
        }

        public static PlayerVip GetPlayerVIP(long OwnerId)
        {
            PlayerVip playerVip = (PlayerVip)null;
            if (OwnerId == 0)
                return playerVip;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@ownerId", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_vip WHERE owner_id=@ownerId";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    if (npgsqlDataReader.Read())
                        playerVip = new PlayerVip()
                        {
                            OwnerId = OwnerId,
                            Address = npgsqlDataReader["registered_ip"].ToString(),
                            Benefit = npgsqlDataReader["last_benefit"].ToString(),
                            Expirate = uint.Parse(npgsqlDataReader["expirate"].ToString())
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerVip;
        }

        public static PlayerReport GetPlayerReportDB(long OwnerId)
        {
            PlayerReport playerReportDb = (PlayerReport)null;
            if (OwnerId == 0)
                return playerReportDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    using (NpgsqlCommand command = npgsqlConnection.CreateCommand())
                    {
                        npgsqlConnection.Open();
                        command.Parameters.AddWithValue("@owner", (object)OwnerId);
                        command.CommandText = "SELECT * FROM player_reports WHERE owner_id=@owner";
                        command.CommandType = CommandType.Text;
                        using (NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default))
                        {
                            while (npgsqlDataReader.Read())
                                playerReportDb = new PlayerReport()
                                {
                                    OwnerId = OwnerId,
                                    TicketCount = int.Parse(npgsqlDataReader["ticket_count"].ToString()),
                                    ReportedCount = int.Parse(npgsqlDataReader["reported_count"].ToString())
                                };
                            npgsqlDataReader.Close();
                            npgsqlConnection.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerReportDb;
        }

        public static bool CreatePlayerReportDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    using (NpgsqlCommand command = npgsqlConnection.CreateCommand())
                    {
                        npgsqlConnection.Open();
                        command.Parameters.AddWithValue("@owner", (object)OwnerId);
                        command.CommandText = "INSERT INTO player_reports(owner_id) VALUES(@owner)";
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                        command.Dispose();
                        npgsqlConnection.Dispose();
                        npgsqlConnection.Close();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static bool CreatePlayerReportHistory(
          long OwnerId,
          long SenderId,
          string OwnerNick,
          string SenderNick,
          ReportType Type,
          string Message)
        {
            RHistoryModel rhistoryModel = new RHistoryModel()
            {
                OwnerId = OwnerId,
                OwnerNick = OwnerNick,
                SenderId = SenderId,
                SenderNick = SenderNick,
                Date = uint.Parse(DateTimeUtil.Now("yyMMddHHmm")),
                Type = Type,
                Message = Message
            };
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    using (NpgsqlCommand command = npgsqlConnection.CreateCommand())
                    {
                        npgsqlConnection.Open();
                        command.Parameters.AddWithValue("@OwnerId", (object)rhistoryModel.OwnerId);
                        command.Parameters.AddWithValue("@OwnerNick", (object)rhistoryModel.OwnerNick);
                        command.Parameters.AddWithValue("@SenderId", (object)rhistoryModel.SenderId);
                        command.Parameters.AddWithValue("@SenderNick", (object)rhistoryModel.SenderNick);
                        command.Parameters.AddWithValue("@Date", (object)(long)rhistoryModel.Date);
                        command.Parameters.AddWithValue("@Type", (object)(int)rhistoryModel.Type);
                        command.Parameters.AddWithValue("@Message", (object)rhistoryModel.Message);
                        command.CommandText = "INSERT INTO base_report_history(date, owner_id, owner_nick, sender_id, sender_nick, type, message) VALUES(@Date, @OwnerId, @OwnerNick, @SenderId, @SenderNick, @Type, @Message)";
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                        command.Dispose();
                        npgsqlConnection.Dispose();
                        npgsqlConnection.Close();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static PlayerBattlepass GetPlayerBattlepassDB(long OwnerId)
        {
            PlayerBattlepass Battlepass = null;
            if (OwnerId == 0)
            {
                return Battlepass;
            }
            try
            {
                using (NpgsqlConnection Connection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand Command = Connection.CreateCommand();
                    Connection.Open();
                    Command.Parameters.AddWithValue("@id", OwnerId);
                    Command.CommandText = "SELECT * FROM player_battlepass WHERE owner_id=@id";
                    Command.CommandType = CommandType.Text;
                    NpgsqlDataReader Data = Command.ExecuteReader();
                    while (Data.Read())
                    {
                        Battlepass = new PlayerBattlepass()
                        {
                            BattlepassId = Data["battlepass_id"] != DBNull.Value ? int.Parse(Data["battlepass_id"].ToString()) : 0,
                            BattlepassPremiumLevel = Data["battlepass_premium_levels"] != DBNull.Value ? int.Parse(Data["battlepass_premium_levels"].ToString()) : 0,
                            BattlepassNormalLevel = Data["battlepass_normal_levels"] != DBNull.Value ? int.Parse(Data["battlepass_normal_levels"].ToString()) : 0,
                            HavePremium = Data["battlepass_premium"] != DBNull.Value ? bool.Parse(Data["battlepass_premium"].ToString()) : false,
                            EarnedPoints = Data["earned_points"] != DBNull.Value ? int.Parse(Data["earned_points"].ToString()) : 0
                        };
                    }
                    Command.Dispose();
                    Data.Close();
                    Connection.Dispose();
                    Connection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return Battlepass;
        }

        public static PlayerCompetitive GetPlayerCompetitiveDB(long OwnerId)
        {
            PlayerCompetitive playerCompetitiveDb = (PlayerCompetitive)null;
            if (OwnerId == 0)
                return playerCompetitiveDb;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@id", (object)OwnerId);
                    command.CommandText = "SELECT * FROM player_competitive WHERE owner_id=@id";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        playerCompetitiveDb = new PlayerCompetitive()
                        {
                            OwnerId = OwnerId,
                            Level = int.Parse(npgsqlDataReader["level"].ToString()),
                            Points = int.Parse(npgsqlDataReader["points"].ToString())
                        };
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            return playerCompetitiveDb;
        }

        public static bool CreatePlayerBattlepassDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_battlepass VALUES(@owner);";
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        public static bool CreatePlayerCompetitiveDB(long OwnerId)
        {
            if (OwnerId == 0)
                return false;
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@owner", (object)OwnerId);
                    command.CommandText = "INSERT INTO player_competitive VALUES(@owner);";
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                    command.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }
    }
}