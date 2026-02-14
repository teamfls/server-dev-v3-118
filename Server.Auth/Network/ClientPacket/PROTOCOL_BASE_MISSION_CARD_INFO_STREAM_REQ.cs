using Plugin.Core;
using Plugin.Core.Enums;
using Server.Auth.Network.ServerPacket;
using System;

namespace Server.Auth.Network.ClientPacket
{
    public class PROTOCOL_BASE_MISSION_CARD_INFO_STREAM_REQ : AuthClientPacket
    {
        public override void Read()
        {
        }

        public override void Run()
        {
            try
            {
                if (this.Client.Player == null)
                    return;
                this.Client.SendPacket((AuthServerPacket)new PROTOCOL_BASE_MISSION_CARD_INFO_STREAM_ACK());
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }
    }
}