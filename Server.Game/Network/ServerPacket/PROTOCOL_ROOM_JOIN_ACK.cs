using Plugin.Core.Models;
using Plugin.Core.Network;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_ROOM_JOIN_ACK : GameServerPacket
    {
        private readonly uint Error;
        private readonly RoomModel roomModel;
        private readonly int SlotId;

        public PROTOCOL_ROOM_JOIN_ACK(uint error, Account account)
        {
            Error = error;
            if (account != null)
            {
                SlotId = account.SlotId;
                roomModel = account.GetRoom();
            }
        }

        public override void Write()
        {
            WriteH((short)3586);
            WriteH((short)0);
            WriteD(Error);
            if (Error != 0U)
                return;
            lock (roomModel.Slots)
            {
                WriteB(Method0(roomModel));
                WriteB(Method1(roomModel));
                WriteC(roomModel.AiType);
                WriteC(roomModel.IsStartingMatch() ? roomModel.IngameAiLevel : roomModel.AiLevel);
                WriteC(roomModel.AiCount);
                WriteC((byte)roomModel.GetAllPlayers().Count);
                WriteC((byte)roomModel.Leader);
                WriteC((byte)roomModel.CountdownTime.GetTimeLeft());
                WriteC((byte)roomModel.Password.Length);
                WriteS(roomModel.Password, roomModel.Password.Length);
                WriteB(new byte[17]);
                WriteU(roomModel.LeaderName, 66);
                WriteD(roomModel.KillTime);
                WriteC(roomModel.Limit);
                WriteC(roomModel.WatchRuleFlag);
                WriteH((ushort)roomModel.BalanceType);
                WriteB(roomModel.RandomMaps);
                WriteC(roomModel.CountdownIG == 0 ? (byte)5 : (byte)5);
                WriteB(roomModel.LeaderAddr);
                WriteC(roomModel.KillCam);
                WriteH((short)0);
                WriteD(roomModel.RoomId);
                WriteU(roomModel.Name, 46);
                WriteC((byte)roomModel.MapId);
                WriteC((byte)roomModel.Rule);
                WriteC((byte)roomModel.Stage);
                WriteC((byte)roomModel.RoomType);
                WriteC((byte)roomModel.State);
                WriteC((byte)roomModel.GetCountPlayers());
                WriteC((byte)roomModel.GetSlotCount());
                WriteC((byte)roomModel.Ping);
                WriteH((ushort)roomModel.WeaponsFlag);
                WriteD(roomModel.GetFlag());
                WriteH((short)0);
                WriteB(new byte[4]);
                WriteC((byte)0);
                WriteC((byte)SlotId);
            }
        }

        private byte[] Method0(RoomModel A_1)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)A_1.Slots.Length);
                foreach (SlotModel slot in A_1.Slots)
                    syncServerPacket.WriteC((byte)slot.Team);
                return syncServerPacket.ToArray();
            }
        }

        private byte[] Method1(RoomModel room)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)room.Slots.Length);
                foreach (SlotModel slot in room.Slots)
                {
                    syncServerPacket.WriteC((byte)slot.State);
                    Account playerBySlot = room.GetPlayerBySlot(slot);
                    if (playerBySlot != null)
                    {
                        ClanModel clan = ClanManager.GetClan(playerBySlot.ClanId);
                        syncServerPacket.WriteC((byte)playerBySlot.GetRank());
                        syncServerPacket.WriteD(clan.Id);
                        syncServerPacket.WriteD(playerBySlot.ClanAccess);
                        syncServerPacket.WriteC((byte)clan.Rank);
                        syncServerPacket.WriteD(clan.Logo);
                        syncServerPacket.WriteC((byte)playerBySlot.CafePC);
                        syncServerPacket.WriteC((byte)playerBySlot.TourneyLevel()); //CountryFlags);
                        //syncServerPacket.WriteC((byte)playerBySlot.CountryFlags); //CountryFlags);
                        syncServerPacket.WriteQ((long)playerBySlot.Effects);
                        syncServerPacket.WriteC((byte)clan.Effect);
                        syncServerPacket.WriteC((byte)0);
                        syncServerPacket.WriteC((byte)NATIONS);
                        syncServerPacket.WriteC((byte)0);
                        syncServerPacket.WriteD(playerBySlot.Equipment.NameCardId);
                        syncServerPacket.WriteC((byte)playerBySlot.Bonus.NickBorderColor);
                        syncServerPacket.WriteC((byte)playerBySlot.AuthLevel());
                        syncServerPacket.WriteU(clan.Name, 34);
                        syncServerPacket.WriteC((byte)playerBySlot.SlotId);
                        syncServerPacket.WriteU(playerBySlot.Nickname, 66);
                        syncServerPacket.WriteC((byte)playerBySlot.NickColor);
                        syncServerPacket.WriteC((byte)playerBySlot.Bonus.MuzzleColor);
                        syncServerPacket.WriteC((byte)0);
                        syncServerPacket.WriteC(byte.MaxValue);
                        syncServerPacket.WriteC(byte.MaxValue);
                    }
                    else
                    {
                        syncServerPacket.WriteB(new byte[10]);
                        syncServerPacket.WriteD(uint.MaxValue);
                        syncServerPacket.WriteB(new byte[54]);
                        syncServerPacket.WriteC((byte)slot.Id);
                        syncServerPacket.WriteB(new byte[68]);
                        syncServerPacket.WriteC((byte)0);
                        syncServerPacket.WriteC(byte.MaxValue);
                        syncServerPacket.WriteC(byte.MaxValue);
                    }
                }
                return syncServerPacket.ToArray();
            }
        }
    }
}