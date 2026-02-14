using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;

namespace Server.Game.Network.ClientPacket
{

    public class PROTOCOL_INVENTORY_UPGRADE_REQ : GameClientPacket
    {
        
        private const ushort BASE_CAPACITY = 500;
        private const ushort MAX_CAPACITY = 1000;
        private const ushort CAPACITY_INCREMENT = 100;
        private const int UPGRADE_COST = 10000; 

        public override void Read()
        {
            
        }

        public override void Run()
        {
            try
            {
                Account account = this.Client.Player;
                if (account == null)
                    return;

                // Get current capacity
                int currentCapacity = account.InventoryPlus;
                
                // If capacity is 0, we assume base capacity is handled elsewhere or start from 0
                // Based on UI, if they click +, they probably have the base amount already.

                // Check if already at max capacity
                if (currentCapacity >= 500) // Assuming +500 is max additional
                {
                    this.Client.SendPacket(new PACKET_INVENTORY_MAX_UP_ACK(
                        PACKET_INVENTORY_MAX_UP_ACK.ERROR_MAX_CAPACITY_REACHED));
                    return;
                }

                // Check if player has enough cash
                if (account.Cash < UPGRADE_COST)
                {
                    this.Client.SendPacket(new PACKET_INVENTORY_MAX_UP_ACK(
                        PACKET_INVENTORY_MAX_UP_ACK.ERROR_INSUFFICIENT_CASH));
                    return;
                }

                int newPlus = currentCapacity + CAPACITY_INCREMENT;
                if (newPlus > 500) newPlus = 500;

                // Process upgrade
                if (DaoManagerSQL.UpdateAccountValuable(account.PlayerId, account.Gold, account.Cash - UPGRADE_COST, account.Tags))
                {
                    if (ComDiv.UpdateDB("accounts", "inventory_plus", newPlus, "player_id", account.PlayerId))
                    {
                        account.Cash -= UPGRADE_COST;
                        account.InventoryPlus = newPlus;

                        // Send success response
                        this.Client.SendPacket(new PACKET_INVENTORY_MAX_UP_ACK(account, (ushort)(600 + newPlus), (ushort)UPGRADE_COST));
                        
                        CLogger.Print($"[Inventory] Player {account.PlayerId} upgraded inventory plus: {currentCapacity} -> {newPlus} (cost: {UPGRADE_COST} cash)", LoggerType.Info);
                    }
                    else
                    {
                         // Rollback cash if DB update fails? usually not needed if simple UpdateDB
                         this.Client.SendPacket(new PACKET_INVENTORY_MAX_UP_ACK(-1));
                    }
                }
                else
                {
                    this.Client.SendPacket(new PACKET_INVENTORY_MAX_UP_ACK(PACKET_INVENTORY_MAX_UP_ACK.ERROR_INSUFFICIENT_CASH));
                }
            }
            catch (Exception ex)
            {
                CLogger.Print($"[PROTOCOL_INVENTORY_UPGRADE_REQ] Error: {ex.Message}", LoggerType.Error, ex);
            }
        }
    }
}
