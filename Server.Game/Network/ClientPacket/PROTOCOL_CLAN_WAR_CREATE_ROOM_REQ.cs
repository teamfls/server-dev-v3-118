// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_CLAN_WAR_CREATE_ROOM_REQ
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
    public class PROTOCOL_CLAN_WAR_CREATE_ROOM_REQ : GameClientPacket
    {
        private int Field0 = -1;
        private int Field1;
        private int Field2;
        private int Field3;
        private string Field4;
        private StageOptions Field5;
        private MapIdEnum Field6;
        private MapRules Field7;
        private RoomCondition Field8;
        private RoomWeaponsFlag Field9;
        private RoomStageFlag Field10;

        public override void Read()
        {
            this.Field3 = (int)this.ReadH();
            this.ReadD();
            this.ReadD();
            int num1 = (int)this.ReadH();
            this.Field4 = this.ReadU(46);
            this.Field6 = (MapIdEnum)this.ReadC();
            this.Field7 = (MapRules)this.ReadC();
            this.Field5 = (StageOptions)this.ReadC();
            this.Field8 = (RoomCondition)this.ReadC();
            int num2 = (int)this.ReadC();
            int num3 = (int)this.ReadC();
            this.Field1 = (int)this.ReadC();
            this.Field2 = (int)this.ReadC();
            this.Field9 = (RoomWeaponsFlag)this.ReadH();
            this.Field10 = (RoomStageFlag)this.ReadD();
        }

        
        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null || player.ClanId == 0)
                    return;
                ChannelModel channel = player.GetChannel();
                MatchModel match1 = player.Match;
                if (channel == null || match1 == null)
                    return;
                MatchModel match2 = channel.GetMatch(this.Field3);
                if (match2 == null)
                    return;
                lock (channel.Rooms)
                {
                    for (int index = 0; index < channel.MaxRooms; ++index)
                    {
                        if (channel.GetRoom(index) == null)
                        {
                            RoomModel roomModel = new RoomModel(index, channel)
                            {
                                Name = this.Field4,
                                MapId = this.Field6,
                                Rule = this.Field7,
                                Stage = this.Field5,
                                RoomType = this.Field8
                            };
                            roomModel.SetSlotCount(this.Field1, true, false);
                            roomModel.Ping = this.Field2;
                            roomModel.WeaponsFlag = this.Field9;
                            roomModel.Flag = this.Field10;
                            roomModel.Password = "";
                            roomModel.KillTime = 3;
                            if (roomModel.AddPlayer(player) >= 0)
                            {
                                channel.AddRoom(roomModel);
                                this.Client.SendPacket(new PROTOCOL_ROOM_CREATE_ACK(0U, roomModel));
                                this.Field0 = index;
                                return;
                            }
                        }
                    }
                }
                using (PROTOCOL_CLAN_WAR_ENEMY_INFO_ACK clanWarEnemyInfoAck = new PROTOCOL_CLAN_WAR_ENEMY_INFO_ACK(match2))
                {
                    using (PROTOCOL_CLAN_WAR_JOIN_ROOM_ACK clanWarJoinRoomAck = new PROTOCOL_CLAN_WAR_JOIN_ROOM_ACK(match2, this.Field0, 0))
                    {
                        byte[] completeBytes1 = clanWarEnemyInfoAck.GetCompleteBytes("PROTOCOL_CLAN_WAR_CREATE_ROOM_REQ-1");
                        byte[] completeBytes2 = clanWarJoinRoomAck.GetCompleteBytes("PROTOCOL_CLAN_WAR_CREATE_ROOM_REQ-2");
                        foreach (Account allPlayer in match1.GetAllPlayers(match1.Leader))
                        {
                            allPlayer.SendCompletePacket(completeBytes1, clanWarEnemyInfoAck.GetType().Name);
                            allPlayer.SendCompletePacket(completeBytes2, clanWarJoinRoomAck.GetType().Name);
                            if (allPlayer.Match != null)
                                match1.Slots[allPlayer.MatchSlot].State = SlotMatchState.Ready;
                        }
                    }
                }
                using (PROTOCOL_CLAN_WAR_ENEMY_INFO_ACK clanWarEnemyInfoAck = new PROTOCOL_CLAN_WAR_ENEMY_INFO_ACK(match1))
                {
                    using (PROTOCOL_CLAN_WAR_JOIN_ROOM_ACK clanWarJoinRoomAck = new PROTOCOL_CLAN_WAR_JOIN_ROOM_ACK(match1, this.Field0, 1))
                    {
                        byte[] completeBytes3 = clanWarEnemyInfoAck.GetCompleteBytes("PROTOCOL_CLAN_WAR_CREATE_ROOM_REQ-3");
                        byte[] completeBytes4 = clanWarJoinRoomAck.GetCompleteBytes("PROTOCOL_CLAN_WAR_CREATE_ROOM_REQ-4");
                        foreach (Account allPlayer in match2.GetAllPlayers())
                        {
                            allPlayer.SendCompletePacket(completeBytes3, clanWarEnemyInfoAck.GetType().Name);
                            allPlayer.SendCompletePacket(completeBytes4, clanWarJoinRoomAck.GetType().Name);
                            if (allPlayer.Match != null)
                                match1.Slots[allPlayer.MatchSlot].State = SlotMatchState.Ready;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }
    }
}