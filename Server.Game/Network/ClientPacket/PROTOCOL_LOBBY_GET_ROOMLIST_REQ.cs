// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_LOBBY_GET_ROOMLIST_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Network;
using Plugin.Core.Utility;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_LOBBY_GET_ROOMLIST_REQ : GameClientPacket
    {
        public override void Read()
        {
        }

        
        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null || ComDiv.GetDuration(player.LastRoomList) < 1.0)
                    return;
                ChannelModel channel = player.GetChannel();
                if (channel == null)
                    return;
                player.LastRoomList = DateTimeUtil.Now();
                channel.RemoveEmptyRooms();
                List<RoomModel> rooms = channel.Rooms;
                List<Account> waitPlayers = channel.GetWaitPlayers();
                int num1 = (int)Math.Ceiling((double)rooms.Count / 10.0);
                int num2 = (int)Math.Ceiling((double)waitPlayers.Count / 8.0);
                if (player.LastRoomPage >= num1)
                    player.LastRoomPage = 0;
                if (player.LastPlayerPage >= num2)
                    player.LastPlayerPage = 0;
                int A_2_1 = 0;
                int A_2_2 = 0;
                byte[] A_7 = this.Method0(player.LastRoomPage, ref A_2_1, rooms);
                byte[] A_8 = this.Method2(player.LastPlayerPage, ref A_2_2, waitPlayers);
                this.Client.SendPacket(new PROTOCOL_LOBBY_GET_ROOMLIST_ACK(rooms.Count, waitPlayers.Count, player.LastRoomPage++, player.LastPlayerPage++, A_2_1, A_2_2, A_7, A_8));
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_LOBBY_GET_ROOMLIST_REQ: " + ex.Message, LoggerType.Error, ex);
            }
        }

        private byte[] Method0(int A_1, ref int A_2, List<RoomModel> A_3)
        {
            int num = A_1 == 0 ? 10 : 11;
            using (SyncServerPacket A_2_1 = new SyncServerPacket())
            {
                for (int index = A_1 * num; index < A_3.Count; ++index)
                {
                    this.Method1(A_3[index], A_2_1);
                    if (++A_2 == 10)
                        break;
                }
                return A_2_1.ToArray();
            }
        }

        private void Method1(RoomModel A_1, SyncServerPacket A_2)
        {
            A_2.WriteD(A_1.RoomId);
            A_2.WriteU(A_1.Name, 46);
            A_2.WriteC((byte)A_1.MapId);
            A_2.WriteC((byte)A_1.Rule);
            A_2.WriteC((byte)A_1.Stage);
            A_2.WriteC((byte)A_1.RoomType);
            A_2.WriteC((byte)A_1.State);
            A_2.WriteC((byte)A_1.GetCountPlayers());
            A_2.WriteC((byte)A_1.GetSlotCount());
            A_2.WriteC((byte)A_1.Ping);
            A_2.WriteH((ushort)A_1.WeaponsFlag);
            A_2.WriteD(A_1.GetFlag());
            A_2.WriteH((short)0);
            A_2.WriteB(A_1.LeaderAddr);
            A_2.WriteC((byte)0);
        }

        private byte[] Method2(int A_1, ref int A_2, List<Account> A_3)
        {
            int num = A_1 == 0 ? 8 : 9;
            using (SyncServerPacket A_2_1 = new SyncServerPacket())
            {
                for (int index = A_1 * num; index < A_3.Count; ++index)
                {
                    this.Method3(A_3[index], A_2_1);
                    if (++A_2 == 8)
                        break;
                }
                return A_2_1.ToArray();
            }
        }

        private void Method3(Account A_1, SyncServerPacket A_2)
        {
            ClanModel clan = ClanManager.GetClan(A_1.ClanId);
            A_2.WriteD(A_1.GetSessionId());
            A_2.WriteD(clan.Logo);
            A_2.WriteC((byte)clan.Effect);
            A_2.WriteU(clan.Name, 34);
            A_2.WriteH((short)A_1.GetRank());
            A_2.WriteU(A_1.Nickname, 66);
            A_2.WriteC((byte)A_1.NickColor);
            A_2.WriteC((byte)this.NATIONS);
            A_2.WriteD(A_1.Equipment.NameCardId);
            A_2.WriteC((byte)A_1.Bonus.NickBorderColor);
        }
    }
}