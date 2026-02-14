using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.Network;
using System.Collections.Generic;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_SHOP_LIMITED_SALE_LIST_ACK : GameServerPacket
    {
        private List<ItemsLimited> Items;
        
        public PROTOCOL_SHOP_LIMITED_SALE_LIST_ACK()
        {
            lock (ShopManager.ItemLimited)
            {
                Items = new List<ItemsLimited>(ShopManager.ItemLimited);
            }
        }
        
        public override void Write()
        {
            WriteH(1096);
            WriteD(Items.Count);

            foreach (ItemsLimited item in Items)
            {
                WriteD(0);                          
                WriteD(item.GoodId);               
                WriteD((uint)item.StartDate);       
                WriteD((uint)item.EndDate);         
                WriteD(item.SaleType);              
                WriteD((int)item.Remain);           
                WriteD(0);                          
            }
        }
    }
}
