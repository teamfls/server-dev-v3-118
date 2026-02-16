// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_BATTLE_RESPAWN_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Data.Utils;
using Server.Game.Network.ServerPacket;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_BATTLE_RESPAWN_REQ : GameClientPacket
    {
        private int[] Field0;
        private int Field1;

        public override void Read()
        {
            this.Field0 = new int[16 /*0x10*/];
            this.Field0[0] = this.ReadD();
            int num1 = (int)this.ReadUD();
            this.Field0[1] = this.ReadD();
            int num2 = (int)this.ReadUD();
            this.Field0[2] = this.ReadD();
            int num3 = (int)this.ReadUD();
            this.Field0[3] = this.ReadD();
            int num4 = (int)this.ReadUD();
            this.Field0[4] = this.ReadD();
            int num5 = (int)this.ReadUD();
            this.Field0[5] = this.ReadD();
            int num6 = (int)this.ReadUD();
            this.Field0[6] = this.ReadD();
            int num7 = (int)this.ReadUD();
            this.Field0[7] = this.ReadD();
            int num8 = (int)this.ReadUD();
            this.Field0[8] = this.ReadD();
            int num9 = (int)this.ReadUD();
            this.Field0[9] = this.ReadD();
            int num10 = (int)this.ReadUD();
            this.Field0[10] = this.ReadD();
            int num11 = (int)this.ReadUD();
            this.Field0[11] = this.ReadD();
            int num12 = (int)this.ReadUD();
            this.Field0[12] = this.ReadD();
            int num13 = (int)this.ReadUD();
            this.Field0[13] = this.ReadD();
            int num14 = (int)this.ReadUD();
            this.Field0[14] = this.ReadD();
            int num15 = (int)this.ReadUD();
            this.Field1 = (int)this.ReadH();
            this.Field0[15] = this.ReadD();
            int num16 = (int)this.ReadUD();
        }

        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (player == null)
                    return;
                RoomModel room = player.Room;
                if (room == null || room.State != RoomState.BATTLE)
                    return;
                SlotModel slot = room.GetSlot(player.SlotId);
                
                // DEBUG LOGGING - Log all observer requests
                if (slot != null && slot.SpecGM)
                {
                    CLogger.Print($"[RESPAWN DEBUG] Observer {player.Nickname} requested. FirstRespawn: {slot.FirstRespawn}, State: {slot.State}", LoggerType.Debug);
                }

                if (slot == null || slot.State != SlotState.BATTLE)
                    return;

                // Clear DeathState for normal players, but NOT for observers who are Dead (watching kill cam)
                if (slot.DeathState.HasFlag((Enum)DeadEnum.Dead))
                {
                    if (slot.SpecGM)
                    {
                        // Observer is watching kill cam - do NOT clear state and do NOT send RESPAWN_ACK
                        CLogger.Print($"[RESPAWN DEBUG] Ignoring RESPAWN_REQ for Observer {player.Nickname} (watching kill cam, DeathState: Dead)", LoggerType.Debug);
                        return;
                    }
                    else
                    {
                        // Normal player can respawn
                        slot.DeathState = DeadEnum.Alive;
                    }
                }
                else if (slot.DeathState.HasFlag((Enum)DeadEnum.UseChat))
                {
                    slot.DeathState = DeadEnum.Alive;
                }
                PlayerEquipment Equip = AllUtils.ValidateRespawnEQ(slot, this.Field0);
                if (Equip != null)
                {
                    ComDiv.CheckEquipedItems(Equip, player.Inventory.Items, true);
                    string RoomName = room.Name.ToLower();
                    if (RoomName.Contains("@latam") || RoomName.Contains("@ligacuchillera") || RoomName.Contains("@fc") || RoomName.Contains("@camp") || RoomName.Contains("@evento3") || RoomName.Contains("@torneo") || RoomName.Contains("@ic"))
                    {
                        AllUtils.ClassicModeCheck(RoomName, Equip, player);
                    }
                    slot.Equipment = Equip;
                    if ((this.Field1 & 8) > 0)
                        AllUtils.InsertItem(Equip.WeaponPrimary, slot);
                    if ((this.Field1 & 4) > 0)
                        AllUtils.InsertItem(Equip.WeaponSecondary, slot);
                    if ((this.Field1 & 2) > 0)
                        AllUtils.InsertItem(Equip.WeaponMelee, slot);
                    if ((this.Field1 & 1) > 0)
                        AllUtils.InsertItem(Equip.WeaponExplosive, slot);
                    AllUtils.InsertItem(Equip.WeaponSpecial, slot);
                    AllUtils.InsertItem(Equip.PartHead, slot);
                    AllUtils.InsertItem(Equip.PartFace, slot);
                    AllUtils.InsertItem(Equip.BeretItem, slot);
                    AllUtils.InsertItem(Equip.AccessoryId, slot);
                    int idStatics1 = ComDiv.GetIdStatics(this.Field0[5], 1);
                    int idStatics2 = ComDiv.GetIdStatics(this.Field0[5], 2);
                    int idStatics3 = ComDiv.GetIdStatics(this.Field0[5], 5);
                    switch (idStatics1)
                    {
                        case 6:
                            if (idStatics2 != 1 && idStatics3 != 632)
                            {
                                if (idStatics2 == 2 || idStatics3 == 664)
                                {
                                    AllUtils.InsertItem(Equip.CharaBlueId, slot);
                                    break;
                                }
                                break;
                            }
                            AllUtils.InsertItem(Equip.CharaRedId, slot);
                            break;

                        case 15:
                            AllUtils.InsertItem(Equip.DinoItem, slot);
                            break;
                    }
                }
                
                using (PROTOCOL_BATTLE_RESPAWN_ACK Packet = new PROTOCOL_BATTLE_RESPAWN_ACK(room, slot))
                {
                    if (slot.SpecGM)
                    {
                        // Send RESPAWN_ACK only for the first join to complete loading
                        // Subsequent respawns: skip RESPAWN_ACK to prevent observer from seeing their own body
                        if (slot.FirstRespawn)
                        {
                            CLogger.Print($"[RESPAWN DEBUG] Sending initial RESPAWN_ACK to Observer {player.Nickname}", LoggerType.Debug);
                            player.SendPacket(Packet);
                        }
                        else
                        {
                            CLogger.Print($"[RESPAWN DEBUG] Skipping RESPAWN_ACK for Observer {player.Nickname} (prevents local body spawn)", LoggerType.Debug);
                        }
                    }
                    else
                    {
                        room.SendPacketToPlayers(Packet, SlotState.BATTLE, 0);
                    }
                }

                if (slot.FirstRespawn)
                {
                    slot.FirstRespawn = false;
                    if (!slot.SpecGM)
                    {
                        // Only send EquipmentSync for normal players, never for observers
                        EquipmentSync.SendUDPPlayerSync(room, slot, player.Effects, 0);
                        CLogger.Print($"[RESPAWN DEBUG] Sent EquipmentSync (Type 0) for {player.Nickname}", LoggerType.Debug);
                    }
                    else
                    {
                        CLogger.Print($"[RESPAWN DEBUG] Skipped EquipmentSync for Observer {player.Nickname} (prevents body visibility)", LoggerType.Debug);
                    }
                }
                else if (!slot.SpecGM)
                {
                    EquipmentSync.SendUDPPlayerSync(room, slot, player.Effects, 2);
                }
                
                if (!slot.SpecGM && room.WeaponsFlag != (RoomWeaponsFlag)this.Field1)
                {
                    CLogger.Print($"Player '{player.Nickname}' Weapon Flags Doesn't Match! (Room: {(int)room.WeaponsFlag}; Player: {this.Field1})", LoggerType.Warning);
                }
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_BATTLE_RESPAWN_REQ: " + ex.Message, LoggerType.Error, ex);
            }
        }
    }
}