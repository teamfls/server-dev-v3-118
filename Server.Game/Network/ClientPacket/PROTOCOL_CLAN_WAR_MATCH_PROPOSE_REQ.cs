// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_CLAN_WAR_MATCH_PROPOSE_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Models;
using Server.Game.Data.XML;
using Server.Game.Network.ServerPacket;
using System;
using System.Runtime.CompilerServices;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_CLAN_WAR_MATCH_PROPOSE_REQ : GameClientPacket
    {
        private int Field0;
        private int Field1;
        private uint Field2;

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
                if (player == null)
                    return;
                if (player.Match != null && player.MatchSlot == player.Match.Leader && player.Match.State == MatchState.Ready)
                {
                    MatchModel match = ChannelsXML.GetChannel(this.Field1, this.Field1 - this.Field1 / 10 * 10).GetMatch(this.Field0);
                    if (match != null)
                    {
                        Account leader = match.GetLeader();
                        if (leader != null && leader.Connection != null && leader.IsOnline)
                            leader.SendPacket(new PROTOCOL_CLAN_WAR_CHANGE_MAX_PER_ACK(player.Match, player));
                        else
                            this.Field2 = 2147483648U /*0x80000000*/;
                    }
                    else
                        this.Field2 = 2147483648U /*0x80000000*/;
                }
                else
                    this.Field2 = 2147483648U /*0x80000000*/;
                this.Client.SendPacket(new PROTOCOL_CLAN_WAR_MATCH_PROPOSE_ACK(this.Field2));
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_CLAN_WAR_MATCH_PROPOSE_REQ: " + ex.Message, LoggerType.Error, ex);
            }
        }
    }
}