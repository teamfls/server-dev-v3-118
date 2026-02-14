// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_CLAN_WAR_INVITE_ACCEPT_REQ
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
    public class PROTOCOL_CLAN_WAR_INVITE_ACCEPT_REQ : GameClientPacket
    {
        private int Field0;
        private int Field1;
        private int Field2;
        private uint Field3;

        public override void Read()
        {
            this.ReadD();
            this.Field0 = (int)this.ReadH();
            this.Field1 = (int)this.ReadH();
            this.Field2 = (int)this.ReadC();
        }

        
        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null)
                    return;
                MatchModel match1 = player.Match;
                MatchModel match2 = ChannelsXML.GetChannel(this.Field1, this.Field1 - this.Field1 / 10 * 10).GetMatch(this.Field0);
                if (match1 != null && match2 != null && player.MatchSlot == match1.Leader)
                {
                    if (this.Field2 != 1)
                    {
                        Account leader = match2.GetLeader();
                        if (leader != null && leader.Match != null)
                            leader.SendPacket(new PROTOCOL_CLAN_WAR_INVITE_ACCEPT_ACK(2147487891U));
                    }
                    else if (match1.Training == match2.Training)
                    {
                        if (match2.GetCountPlayers() == match1.Training && match1.GetCountPlayers() == match1.Training)
                        {
                            if (match2.State != MatchState.Play && match1.State != MatchState.Play)
                            {
                                match1.State = MatchState.Play;
                                Account leader = match2.GetLeader();
                                if (leader != null && leader.Match != null)
                                {
                                    leader.SendPacket(new PROTOCOL_CLAN_WAR_ENEMY_INFO_ACK(match1));
                                    leader.SendPacket(new PROTOCOL_CLAN_WAR_CREATE_ROOM_ACK(match1));
                                    match2.Slots[leader.MatchSlot].State = SlotMatchState.Ready;
                                }
                                match2.State = MatchState.Play;
                            }
                            else
                                this.Field3 = 2147487888U /*0x80001090*/;
                        }
                        else
                            this.Field3 = 2147487889U;
                    }
                    else
                        this.Field3 = 2147487890U;
                }
                else
                    this.Field3 = 2147487892U;
                this.Client.SendPacket(new PROTOCOL_CLAN_WAR_ACCEPT_BATTLE_ACK(this.Field3));
            }
            catch (Exception ex)
            {
                CLogger.Print("CLAN_WAR_ACCEPT_BATTLE_REC: " + ex.Message, LoggerType.Error, ex);
            }
        }
    }
}