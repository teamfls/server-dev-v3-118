using Plugin.Core.Models;
using Server.Game.Data.Models;
using System.Collections.Generic;

namespace Server.Game.Network.ServerPacket
{

    public class PACKET_INVENTORY_MAX_UP_ACK : GameServerPacket
    {
        private readonly int resultCode;
        private readonly ushort maxCapacity;
        private readonly ushort additionalInfo;
        private readonly List<ItemsModel> items;


        public const int SUCCESS = 0;
        public const int ERROR_INSUFFICIENT_CASH = unchecked((int)0x80010080); 
        public const int ERROR_MAX_CAPACITY_REACHED = -1;
        public const int ERROR_INVALID_UPGRADE = -2;

      
        public PACKET_INVENTORY_MAX_UP_ACK(ushort newMaxCapacity, ushort cost, List<ItemsModel> updatedItems)
        {
            this.resultCode = SUCCESS;
            this.maxCapacity = newMaxCapacity;
            this.additionalInfo = cost;
            this.items = updatedItems ?? new List<ItemsModel>();
        }


        public PACKET_INVENTORY_MAX_UP_ACK(Account account, ushort newMaxCapacity, ushort cost)
        {
            this.resultCode = SUCCESS;
            this.maxCapacity = newMaxCapacity;
            this.additionalInfo = cost;
            this.items = new List<ItemsModel>();
            
            if (account?.Inventory?.Items != null)
            {
                this.items.AddRange(account.Inventory.Items);
            }
        }


        public PACKET_INVENTORY_MAX_UP_ACK(int errorCode)
        {
            this.resultCode = errorCode;
            this.maxCapacity = 0;
            this.additionalInfo = 0;
            this.items = new List<ItemsModel>();
        }


        public PACKET_INVENTORY_MAX_UP_ACK(int resultCode, ushort maxCapacity, ushort additionalInfo, List<ItemsModel> items)
        {
            this.resultCode = resultCode;
            this.maxCapacity = maxCapacity;
            this.additionalInfo = additionalInfo;
            this.items = items ?? new List<ItemsModel>();
        }

        public override void Write()
        {
            WriteH(3337); 
            WriteD(resultCode); 
            WriteH(maxCapacity); 
            WriteH(additionalInfo); 
            
            
            int itemCount = items.Count > 46 ? 46 : items.Count;
            WriteC((byte)itemCount);

            for (int i = 0; i < itemCount; i++)
            {
                var item = items[i];
               
                WriteD((uint)item.ObjectId); 
                WriteD(item.Id);             
                WriteC((byte)item.Equip);     
                WriteD(item.Count);          
            }
            
            
            int bytesWritten = itemCount * 13;
            int paddingNeeded = 608 - bytesWritten;
            for (int i = 0; i < paddingNeeded; i++)
            {
                WriteC(0);
            }
        }
    }
}
