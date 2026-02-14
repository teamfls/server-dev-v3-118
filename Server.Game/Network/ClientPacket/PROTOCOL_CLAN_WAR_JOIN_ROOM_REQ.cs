// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_CLAN_WAR_JOIN_ROOM_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Runtime.CompilerServices;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_CLAN_WAR_JOIN_ROOM_REQ : GameClientPacket
    {
        private int Field0;
        private int Field1;
        private TeamEnum Field2;

        public override void Read()
        {
            this.Field0 = this.ReadD();
            this.Field2 = (TeamEnum)this.ReadH();
            this.Field1 = (int)this.ReadH();
        }

        
        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null || player.ClanId == 0 || player.Match == null)
                    return;
                ChannelModel Channel;
                if (player != null && player.Nickname.Length > 0 && player.Room == null && player.GetChannel(out Channel))
                {
                    RoomModel room = Channel.GetRoom(this.Field0);
                    if (room != null && room.GetLeader() != null)
                    {
                        if (room.Password.Length > 0 && !player.IsGM())
                            this.Client.SendPacket(new PROTOCOL_ROOM_JOIN_ACK(2147487749U /*0x80001005*/, (Account)null));
                        else if (room.Limit == (byte)1 && room.State >= RoomState.COUNTDOWN)
                            this.Client.SendPacket(new PROTOCOL_ROOM_JOIN_ACK(2147487763U, (Account)null));
                        else if (!room.KickedPlayersVote.Contains(player.PlayerId))
                        {
                            if (room.AddPlayer(player, this.Field2) >= 0)
                            {
                                using (PROTOCOL_ROOM_GET_SLOTONEINFO_ACK Packet = new PROTOCOL_ROOM_GET_SLOTONEINFO_ACK(player))
                                    room.SendPacketToPlayers(Packet, player.PlayerId);
                                room.UpdateSlotsInfo();
                                this.Client.SendPacket(new PROTOCOL_ROOM_JOIN_ACK(0U, player));
                            }
                            else
                                this.Client.SendPacket(new PROTOCOL_ROOM_JOIN_ACK(2147487747U /*0x80001003*/, (Account)null));
                        }
                        else
                            this.Client.SendPacket(new PROTOCOL_ROOM_JOIN_ACK(2147487756U /*0x8000100C*/, (Account)null));
                    }
                    else
                        this.Client.SendPacket(new PROTOCOL_ROOM_JOIN_ACK(2147487748U /*0x80001004*/, (Account)null));
                }
                else
                    this.Client.SendPacket(new PROTOCOL_ROOM_JOIN_ACK(2147487748U /*0x80001004*/, (Account)null));
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_CLAN_WAR_JOIN_ROOM_REQ: " + ex.Message, LoggerType.Error, ex);
            }
        }
    }
}