////namespace Server.Game.Network.ServerPacket
////{
////    public class PROTOCOL_SHOP_FLASH_SALE_LIST_ACK : GameServerPacket
////    {
////        public override void Write()
////        {
////            this.WriteH((short)1111);
////            this.WriteC((byte)1);
////            this.WriteD(1);
////            this.WriteC((byte)1);
////        }
////    }
////}

//using Plugin.Core.Managers;
//using Plugin.Core.Models;
//using System.Collections.Generic;
//using System.Text;

//namespace Server.Game.Network.ServerPacket
//{
//    public class PROTOCOL_SHOP_FLASH_SALE_LIST_ACK : GameServerPacket
//    {
//        private List<FlashSaleItem> Items;

//        public PROTOCOL_SHOP_FLASH_SALE_LIST_ACK()
//        {
//            lock (ShopManager.FlashSaleItems)
//            {
//                Items = new List<FlashSaleItem>(ShopManager.FlashSaleItems);
//            }
//        }

//        public override void Write()
//        {
//            WriteH(1111);
//            WriteD(Items.Count);  
//            foreach (FlashSaleItem item in Items)
//            {
//                WriteD(item.ItemId);           
//                WriteD(item.Unknown1);         
//                WriteC(item.Flag);             
//                WriteD(item.Price);            
//                WriteD(item.Stock);            
//                WriteC(item.DiscountPercent); 
//                WriteC(item.LimitType);       

               
//                byte[] nameBytes = new byte[100];
//                if (!string.IsNullOrEmpty(item.ItemName))
//                {
//                    byte[] temp = Encoding.UTF8.GetBytes(item.ItemName);
//                    System.Buffer.BlockCopy(temp, 0, nameBytes, 0, System.Math.Min(temp.Length, 100));
//                }
//                Write(nameBytes);              

//                WriteC(item.Unknown2);        
//                WriteC(item.Unknown3);        

                
//            }
//        }
//    }
//}