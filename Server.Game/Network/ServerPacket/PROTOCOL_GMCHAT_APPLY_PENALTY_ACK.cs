using System;
using System.Numerics;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
//using PointBlank.Game.Rcon;
using Server.Game.Data.Models;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_GMCHAT_APPLY_PENALTY_ACK : GameServerPacket
    {
        private readonly Account Player;
        private readonly uint Error;
        private readonly int Type;
        private readonly int BanTime;
        private readonly string Reason;
        public PROTOCOL_GMCHAT_APPLY_PENALTY_ACK(uint Error, Account Player, int Type, int BanTime, string Reason)
        {
            this.Error = Error;
            this.Player = Player;
            this.Type = Type;
            this.BanTime = BanTime;
            this.Reason = Reason;
        }
        public override void Write()
        {
            WriteH(6664);
            WriteH(0);
            WriteD(Error);
            if (Error == 0)
            {
                if (Player != null)
                {
                    CLogger.Print($"PROTOCOL_GMCHAT_APPLY_PENALTY_ACK: Type={Type}, BanTime={BanTime}, Target={Player.PlayerId}", LoggerType.Info);
                    switch (Type)
                    {
                        case 0: // Mute (Client might send 0)
                        case 1: // Mute
                            if (BanTime == 0)
                            {
                                if (Player.BanObjectId > 0)
                                    ComDiv.UpdateDB("base_ban_history", "expire_date", DateTime.Now, "object_id", Player.BanObjectId);
                                else
                                    CLogger.Print($"PROTOCOL_GMCHAT_APPLY_PENALTY_ACK: Target {Player.PlayerId} has no BanObjectId to un-mute.", LoggerType.Warning);
                            }
                            else
                            {
                                int Minutes = BanTime; // Assume BanTime is in minutes from UI
                                BanHistory Ban = DaoManagerSQL.SaveBanHistory(Player.PlayerId, "MUTE", $"{Player.PlayerId}", DateTimeUtil.Now().AddMinutes(Minutes), Reason);
                                if (Ban != null)
                                {
                                    string Message = $"Player '{Player.Nickname}' has been muted for {Minutes} Minutes!";
                                    using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK Packet = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(Message))
                                    {
                                        GameXender.Client.SendPacketToAllClients(Packet);
                                    }
                                    ComDiv.UpdateDB("accounts", "ban_object_id", Ban.ObjectId, "player_id", Player.PlayerId);
                                    Player.BanObjectId = Ban.ObjectId;
                                    // Removed kick for mute
                                }
                                else
                                {
                                    CLogger.Print($"PROTOCOL_GMCHAT_APPLY_PENALTY_ACK: Failed to save mute history for {Player.PlayerId}.", LoggerType.Error);
                                }
                            }
                            break;
                        case 11: // Some clients send 1 for Ban if 0 is Mute? No, let's stick to observed patterns. 
                                 // Actually, let's just make case 1 both if it's ambiguous, but that's risky.
                                 // Usually Tab 1 = Type 1, Tab 2 = Type 2.
                                 // If Tab 1 = Type 0, Tab 2 = Type 1.
                        case 2: // Ban
                            if (BanTime == -1)
                            {
                                if (ComDiv.UpdateDB("accounts", "access_level", -1, "player_id", Player.PlayerId))
                                {
                                    // Record permanent ban in history table for visibility
                                    DaoManagerSQL.SaveBanHistory(Player.PlayerId, "PERMANENT", $"{Player.PlayerId}", DateTime.MaxValue, Reason);

                                    using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK packet = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(Translation.GetLabel("PlayerBannedWarning", Player.Nickname)))
                                    {
                                        GameXender.Client.SendPacketToAllClients(packet);
                                    }
                                    Player.Access = AccessLevel.BANNED;
                                    if (Player.Connection != null)
                                    {
                                        Player.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), false);
                                        Player.Close(1000, true);
                                    }
                                    else
                                    {
                                        CLogger.Print($"PROTOCOL_GMCHAT_APPLY_PENALTY_ACK: Target {Player.PlayerId} (Permanent) has no active connection to kick.", LoggerType.Warning);
                                    }
                                }
                                else
                                {
                                    CLogger.Print($"PROTOCOL_GMCHAT_APPLY_PENALTY_ACK: Failed to update accounts table for permanent ban of {Player.PlayerId}.", LoggerType.Error);
                                }
                            }
                            else if (BanTime == 0)
                            {
                                if (Player.Connection != null)
                                {
                                    Player.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), false);
                                    Player.Close(1000, true);
                                }
                                else
                                {
                                    CLogger.Print($"PROTOCOL_GMCHAT_APPLY_PENALTY_ACK: Target {Player.PlayerId} (Kick) has no active connection.", LoggerType.Warning);
                                }
                            }
                            else
                            {
                                // Fix: If BanTime is less than a day, use minutes
                                DateTime expiry = BanTime >= 1440 ? DateTimeUtil.Now().AddDays(BanTime / 1440) : DateTimeUtil.Now().AddMinutes(BanTime);
                                BanHistory Ban = DaoManagerSQL.SaveBanHistory(Player.PlayerId, "DURATION", $"{Player.PlayerId}", expiry, Reason);
                                if (Ban != null)
                                {
                                    string durationStr = BanTime >= 1440 ? $"{BanTime / 1440} Days" : $"{BanTime} Minutes";
                                    string Message = $"Player '{Player.Nickname}' has been banned for {durationStr}!";
                                    using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK Packet = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(Message))
                                    {
                                        GameXender.Client.SendPacketToAllClients(Packet);
                                    }
                                    ComDiv.UpdateDB("accounts", "ban_object_id", Ban.ObjectId, "player_id", Player.PlayerId);
                                    Player.BanObjectId = Ban.ObjectId;
                                    if (Player.Connection != null)
                                    {
                                        Player.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), false);
                                        Player.Close(1000, true);
                                    }
                                    else
                                    {
                                        CLogger.Print($"PROTOCOL_GMCHAT_APPLY_PENALTY_ACK: Target {Player.PlayerId} (Duration) has no active connection to kick.", LoggerType.Warning);
                                    }
                                }
                                else
                                {
                                    CLogger.Print($"PROTOCOL_GMCHAT_APPLY_PENALTY_ACK: Failed to save ban history for {Player.PlayerId}.", LoggerType.Error);
                                }
                            }
                            break;
                        default:
                            CLogger.Print($"PROTOCOL_GMCHAT_APPLY_PENALTY_ACK: Unhandled Type={Type}.", LoggerType.Warning);
                            break;
                    }
                }
            }
        }
    }
}
