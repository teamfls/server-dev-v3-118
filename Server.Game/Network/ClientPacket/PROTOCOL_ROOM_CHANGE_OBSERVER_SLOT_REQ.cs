using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_ROOM_CHANGE_OBSERVER_SLOT_REQ : GameClientPacket
    {
        private ViewerType ViewerType;

        public override void Read() => ViewerType = (ViewerType)(int)ReadC();

        public override void Run()
        {
            try
            {
                Account player = Client.GetAccount();
                if (player != null)
                {
                    RoomModel room = player.GetRoom();
                    if (room != null && player.IsGM())
                    {
                        SlotModel slot = room.GetSlot(player.SlotId);
                        if (slot != null)
                        {
                            slot.ViewType = ViewerType;
                            if (slot.ViewType == ViewerType.SpecGM)
                            {
                                slot.SpecGM = true;
                            }
                            Client.SendPacket(new PROTOCOL_ROOM_CHANGE_OBSERVER_SLOT_ACK(slot.Id));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CLogger.Print($"{(this).GetType().Name}; {ex.Message}", LoggerType.Error, ex);
            }
        }
    }
}