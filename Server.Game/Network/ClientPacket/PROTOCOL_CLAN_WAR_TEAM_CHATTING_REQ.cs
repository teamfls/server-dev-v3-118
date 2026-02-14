// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_CLAN_WAR_TEAM_CHATTING_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Runtime.CompilerServices;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_CLAN_WAR_TEAM_CHATTING_REQ : GameClientPacket
    {
        private ChattingType Field0;
        private string Field1;

        public override void Read()
        {
            this.Field0 = (ChattingType)this.ReadH();
            this.Field1 = this.ReadS((int)this.ReadH());
        }

        
        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null || player.Match == null || this.Field0 != ChattingType.Match || ComDiv.GetDuration(player.LastChatting) < 1.0)
                    return;
                using (PROTOCOL_CLAN_WAR_TEAM_CHATTING_ACK Packet = new PROTOCOL_CLAN_WAR_TEAM_CHATTING_ACK(player.Nickname, this.Field1))
                    player.Match.SendPacketToPlayers(Packet);
                player.LastChatting = DateTimeUtil.Now();
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_CLAN_WAR_TEAM_CHATTING_REQ: " + ex.Message, LoggerType.Error, ex);
            }
        }
    }
}