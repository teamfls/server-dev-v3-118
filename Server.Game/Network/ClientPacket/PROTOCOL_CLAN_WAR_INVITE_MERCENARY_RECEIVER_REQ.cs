// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_CLAN_WAR_INVITE_MERCENARY_RECEIVER_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_CLAN_WAR_INVITE_MERCENARY_RECEIVER_REQ : GameClientPacket
    {
        private int Field0;

        public override void Read() => this.Field0 = (int)this.ReadC();

        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null)
                    return;
                MatchModel match = player.Match;
                if (match != null && player.MatchSlot == match.Leader)
                {
                    match.Training = this.Field0;
                    using (PROTOCOL_CLAN_WAR_INVITE_MERCENARY_RECEIVER_ACK Packet = new PROTOCOL_CLAN_WAR_INVITE_MERCENARY_RECEIVER_ACK(0U, this.Field0))
                        match.SendPacketToPlayers(Packet);
                }
                else
                    this.Client.SendPacket(new PROTOCOL_CLAN_WAR_INVITE_MERCENARY_RECEIVER_ACK(2147483648U /*0x80000000*/));
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }
    }
}