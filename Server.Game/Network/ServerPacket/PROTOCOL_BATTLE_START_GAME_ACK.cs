// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_BATTLE_START_GAME_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Network;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using System.Runtime.CompilerServices;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_BATTLE_START_GAME_ACK : GameServerPacket
    {
        private readonly RoomModel Field0;

        public PROTOCOL_BATTLE_START_GAME_ACK(RoomModel A_1) => this.Field0 = A_1;

        public override void Write()
        {
            this.WriteH((short)5127);
            this.WriteH((short)0);
            this.WriteB(this.Method0(this.Field0));
            this.WriteB(this.Method1(this.Field0));
            this.WriteB(this.Method2(this.Field0));
            this.WriteC((byte)this.Field0.MapId);
            this.WriteC((byte)this.Field0.Rule);
            this.WriteC((byte)this.Field0.Stage);
            this.WriteC((byte)this.Field0.RoomType);
        }

        private byte[] Method0(RoomModel A_1)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)A_1.GetReadyPlayers());
                foreach (SlotModel slot in A_1.Slots)
                {
                    if (slot.State >= SlotState.READY && slot.Equipment != null)
                    {
                        Account playerBySlot = A_1.GetPlayerBySlot(slot);
                        if (playerBySlot != null && playerBySlot.SlotId == slot.Id)
                            syncServerPacket.WriteD((uint)playerBySlot.PlayerId);
                    }
                }
                return syncServerPacket.ToArray();
            }
        }

        private byte[] Method1(RoomModel A_1)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)A_1.Slots.Length);
                foreach (SlotModel slot in A_1.Slots)
                    syncServerPacket.WriteC((byte)slot.Team);
                return syncServerPacket.ToArray();
            }
        }

        
        private byte[] Method2(RoomModel A_1)
        {
            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                syncServerPacket.WriteC((byte)A_1.GetReadyPlayers());
                foreach (SlotModel slot in A_1.Slots)
                {
                    if (slot.State >= SlotState.READY && slot.Equipment != null)
                    {
                        Account playerBySlot = A_1.GetPlayerBySlot(slot);
                        if (playerBySlot != null && playerBySlot.SlotId == slot.Id)
                        {
                            syncServerPacket.WriteC((byte)slot.Id);
                            PlayerEquipment equipment = playerBySlot.Equipment;
                            PlayerTitles title = playerBySlot.Title;
                            int num = 0;
                            if (equipment != null && title != null)
                            {
                                switch (A_1.ValidateTeam(slot.Team, slot.CostumeTeam))
                                {
                                    case TeamEnum.FR_TEAM:
                                        num = equipment.CharaRedId;
                                        break;
                                    case TeamEnum.CT_TEAM:
                                        num = equipment.CharaBlueId;
                                        break;
                                }
                                syncServerPacket.WriteD(num);
                                syncServerPacket.WriteD(equipment.WeaponPrimary);
                                syncServerPacket.WriteD(equipment.WeaponSecondary);
                                syncServerPacket.WriteD(equipment.WeaponMelee);
                                syncServerPacket.WriteD(equipment.WeaponExplosive);
                                syncServerPacket.WriteD(equipment.WeaponSpecial);
                                syncServerPacket.WriteD(num);
                                syncServerPacket.WriteD(equipment.PartHead);
                                syncServerPacket.WriteD(equipment.PartFace);
                                syncServerPacket.WriteD(equipment.PartJacket);
                                syncServerPacket.WriteD(equipment.PartPocket);
                                syncServerPacket.WriteD(equipment.PartGlove);
                                syncServerPacket.WriteD(equipment.PartBelt);
                                syncServerPacket.WriteD(equipment.PartHolster);
                                syncServerPacket.WriteD(equipment.PartSkin);
                                syncServerPacket.WriteD(equipment.BeretItem);
                                syncServerPacket.WriteB(Bitwise.HexStringToByteArray("64 64 64 64 64"));
                                syncServerPacket.WriteC((byte)title.Equiped1);
                                syncServerPacket.WriteC((byte)title.Equiped2);
                                syncServerPacket.WriteC((byte)title.Equiped3);
                                syncServerPacket.WriteD(equipment.AccessoryId);
                                syncServerPacket.WriteD(equipment.SprayId);
                                syncServerPacket.WriteD(equipment.NameCardId);
                            }
                        }
                    }
                }
                return syncServerPacket.ToArray();
            }
        }
    }
}