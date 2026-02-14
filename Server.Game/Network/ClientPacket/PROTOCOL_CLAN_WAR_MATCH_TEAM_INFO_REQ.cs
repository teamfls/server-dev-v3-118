// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_CLAN_WAR_MATCH_TEAM_INFO_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Models;
using Server.Game.Data.XML;
using Server.Game.Network.ServerPacket;
using System;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_CLAN_WAR_MATCH_TEAM_INFO_REQ : GameClientPacket
    {
        private int Field0;
        private int Field1;

        public override void Read()
        {
            this.Field0 = (int)this.ReadH();
            this.Field1 = (int)this.ReadH();
        }

        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null || player.Match == null)
                    return;
                ChannelModel channel = ChannelsXML.GetChannel(this.Field1, this.Field1 - this.Field1 / 10 * 10);
                if (channel == null)
                {
                    this.Client.SendPacket(new PROTOCOL_CLAN_WAR_MATCH_TEAM_INFO_ACK(2147483648U /*0x80000000*/));
                }
                else
                {
                    MatchModel match = channel.GetMatch(this.Field0);
                    if (match == null)
                        this.Client.SendPacket(new PROTOCOL_CLAN_WAR_MATCH_TEAM_INFO_ACK(2147483648U /*0x80000000*/));
                    else
                        this.Client.SendPacket(new PROTOCOL_CLAN_WAR_MATCH_TEAM_INFO_ACK(0U, match.Clan));
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }
    }
}