using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Utility;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_AUTH_FIND_USER_REQ : GameClientPacket
    {
        private string Nickname;
        private uint Error;

        public override void Read() => this.Nickname = ReadU(33);

        public override void Run()
        {
            try
            {
                Account player = Client.Player;
                if (player == null || ComDiv.GetDuration(player.LastFindUser) < 1.0)
                {
                    return;
                }
                Account account = AccountManager.GetAccount(player.FindPlayer, 1, 31);
                if (account != null && player.Nickname.Length > 0 && player.Nickname != Nickname)
                {
                    if (player.Nickname != account.Nickname)
                    {
                        player.FindPlayer = account.Nickname;
                    }
                    Client.SendPacket(new PROTOCOL_BASE_GET_USER_DETAIL_INFO_ACK(Error, account, int.MaxValue));
                }
                else
                {
                    Error = 0x80001803;
                }
                Client.SendPacket(new PROTOCOL_AUTH_FIND_USER_ACK(Error));
                player.LastFindUser = DateTimeUtil.Now();
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }
    }
}