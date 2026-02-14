using System;
using System.Runtime.CompilerServices;
using Plugin.Core.Utility;

namespace Server.Game.Network.ServerPacket
{
    // Token: 0x02000026 RID: 38
    public class PROTOCOL_BASE_RANDOMBOX_LIST_ACK : GameServerPacket
    {
        // Token: 0x060000AC RID: 172 RVA: 0x00007760 File Offset: 0x00005960
        public PROTOCOL_BASE_RANDOMBOX_LIST_ACK(bool A_1)
        {
            this.Field0 = A_1;
        }

        // Token: 0x060000AD RID: 173 RVA: 0x0000777C File Offset: 0x0000597C

        public override void Write()
        {
            base.WriteH(2499);
            base.WriteC((byte)(this.Field0 ? 1 : 0));
            base.WriteD(uint.Parse(DateTimeUtil.Now("yyMMddHHmm")));
        }

        // Token: 0x04000065 RID: 101
        private readonly bool Field0;
    }
}
