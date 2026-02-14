// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_AUTH_SHOP_CAPSULE_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Models;
using System.Collections.Generic;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_AUTH_SHOP_CAPSULE_ACK : GameServerPacket
    {
        private readonly List<ItemsModel> Field0;
        private readonly int Field1;
        private readonly int Field2;

        public PROTOCOL_AUTH_SHOP_CAPSULE_ACK(ItemsModel A_1, int A_2, int A_3)
        {
            this.Field1 = A_2;
            this.Field2 = A_3;
            this.Field0 = new List<ItemsModel>();
            ItemsModel itemsModel = new ItemsModel(A_1);
            if (itemsModel == null)
                return;
            this.Field0.Add(itemsModel);
        }

        public PROTOCOL_AUTH_SHOP_CAPSULE_ACK(List<ItemsModel> A_1, int A_2, int A_3)
        {
            this.Field1 = A_2;
            this.Field2 = A_3;
            this.Field0 = new List<ItemsModel>();
            foreach (ItemsModel itemsModel1 in A_1)
            {
                ItemsModel itemsModel2 = new ItemsModel(itemsModel1);
                if (itemsModel2 != null)
                    this.Field0.Add(itemsModel2);
            }
        }

        public override void Write()
        {
            this.WriteH((short)1064);
            this.WriteH((short)0);
            this.WriteC((byte)1);
            this.WriteC((byte)this.Field2);
            this.WriteC((byte)this.Field0.Count);
            foreach (ItemsModel itemsModel in this.Field0)
                this.WriteD(itemsModel.Id);
        }
    }
}