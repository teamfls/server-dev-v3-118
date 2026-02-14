using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Utility;
using Server.Game.Rcon;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;

namespace Server.Game.Rcon.Admin
{
    public class RconKickPlayerAdmin : RconReceive
    {
        private string Token;
        private long Id, gm_id;

        public override void Run()
        {
            if (IsJsonMode)
            {
                Token = PopString("token");
                Id = PopLong("player_id");
                gm_id = PopLong("gm");
            }

            if (!RconCommand.CheckToken(Token) || Token == "")
            {
                RconLogger.LogsPanel("An error occurred in the process, please try again later. ", 1);
                return;
            }
            Account GM = AccountManager.GetAccount(gm_id, 0);
            Account victim = AccountManager.GetAccount(Id, 0);

            ComDiv.UpdateDB("accounts", "online", false, "player_id", Id);
            victim.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), false);
            victim.Close(1000, true);

            RconLogger.LogsPanel($"[!] {victim.Nickname} has been successfully kicked from server. [{gm_id}]", 0);
        }
    }
}