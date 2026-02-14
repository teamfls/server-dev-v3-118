// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_CS_MEDAL_INFO_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Utility;
using System.Runtime.CompilerServices;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_CS_MEDAL_INFO_ACK : GameServerPacket
    {

        public override void Write()
        {
            this.WriteH((short)989);
            this.WriteD(0);
            this.WriteC((byte)0);
            this.WriteD(1);
            this.WriteD(30);
            this.WriteD(500);
            this.WriteH((short)2);
            this.WriteH((short)1);
            this.WriteD(1045220557);
            this.WriteD(700);
            this.WriteH((short)3);
            this.WriteH((short)1);
            this.WriteD(1028443341);
            this.WriteD(1000);
            this.WriteH((short)5);
            this.WriteH((short)1);
            this.WriteD(1045220557);
            this.WriteD(1300);
            this.WriteH((short)8);
            this.WriteH((short)1);
            this.WriteD(1028443341);
            this.WriteD(1700);
            this.WriteH((short)2);
            this.WriteH((short)2);
            this.WriteD(1056964608 /*0x3F000000*/);
            this.WriteD(2000);
            this.WriteH((short)3);
            this.WriteH((short)2);
            this.WriteD(1036831949);
            this.WriteD(2500);
            this.WriteH((short)5);
            this.WriteH((short)2);
            this.WriteD(1050253722);
            this.WriteD(3000);
            this.WriteH((short)8);
            this.WriteH((short)2);
            this.WriteD(1034147594);
            this.WriteD(4000);
            this.WriteH((short)4);
            this.WriteH((short)1);
            this.WriteD(1073741824 /*0x40000000*/);
            this.WriteD(5000);
            this.WriteH((short)7);
            this.WriteH((short)1);
            this.WriteD(1050253722);
            this.WriteD(6000);
            this.WriteH((short)2);
            this.WriteH((short)3);
            this.WriteD(1061158912 /*0x3F400000*/);
            this.WriteD(7000);
            this.WriteH((short)3);
            this.WriteH((short)3);
            this.WriteD(1041865114);
            this.WriteD(8000);
            this.WriteH((short)5);
            this.WriteH((short)3);
            this.WriteD(1056964608 /*0x3F000000*/);
            this.WriteD(10000);
            this.WriteH((short)8);
            this.WriteH((short)3);
            this.WriteD(1039516303);
            this.WriteD(12000);
            this.WriteH((short)4);
            this.WriteH((short)2);
            this.WriteD(1077936128 /*0x40400000*/);
            this.WriteD(14000);
            this.WriteH((short)7);
            this.WriteH((short)2);
            this.WriteD(1056964608 /*0x3F000000*/);
            this.WriteD(16000);
            this.WriteH((short)1);
            this.WriteH((short)1);
            this.WriteD(1045220557);
            this.WriteD(18000);
            this.WriteH((short)2);
            this.WriteH((short)4);
            this.WriteD(1065353216 /*0x3F800000*/);
            this.WriteD(20000);
            this.WriteH((short)3);
            this.WriteH((short)4);
            this.WriteD(1045220557);
            this.WriteD(23000);
            this.WriteH((short)5);
            this.WriteH((short)4);
            this.WriteD(1060320051);
            this.WriteD(25000);
            this.WriteH((short)8);
            this.WriteH((short)4);
            this.WriteD(1041865114);
            this.WriteD(28000);
            this.WriteH((short)4);
            this.WriteH((short)3);
            this.WriteD(1082130432 /*0x40800000*/);
            this.WriteD(30000);
            this.WriteH((short)7);
            this.WriteH((short)3);
            this.WriteD(1060320051);
            this.WriteD(32000);
            this.WriteH((short)1);
            this.WriteH((short)2);
            this.WriteD(1056964608 /*0x3F000000*/);
            this.WriteD(35000);
            this.WriteH((short)4);
            this.WriteH((short)4);
            this.WriteD(1084227584 /*0x40A00000*/);
            this.WriteD(37000);
            this.WriteH((short)7);
            this.WriteH((short)4);
            this.WriteD(1063675494);
            this.WriteD(40000);
            this.WriteH((short)1);
            this.WriteH((short)3);
            this.WriteD(1061158912 /*0x3F400000*/);
            this.WriteD(43000);
            this.WriteH((short)6);
            this.WriteH((short)1);
            this.WriteD(1028443341);
            this.WriteD(45000);
            this.WriteH((short)1);
            this.WriteH((short)4);
            this.WriteD(1065353216 /*0x3F800000*/);
            this.WriteD(50000);
            this.WriteH((short)6);
            this.WriteH((short)2);
            this.WriteB(Bitwise.HexStringToByteArray("CD CC CC 3D 02 00 00 00 1E 00 00 00 F4 01 00 0002 00 01 00 CD CC 4C 3E BC 02 00 00 03 00 01 00CD CC 4C 3D E8 03 00 00 05 00 01 00 CD CC 4C 3E14 05 00 00 08 00 01 00 CD CC 4C 3D A4 06 00 0002 00 02 00 00 00 00 3F D0 07 00 00 03 00 02 00CD CC CC 3D C4 09 00 00 05 00 02 00 9A 99 99 3EB8 0B 00 00 08 00 02 00 0A D7 A3 3D A0 0F 00 0004 00 01 00 00 00 00 40 88 13 00 00 07 00 01 009A 99 99 3E 70 17 00 00 02 00 03 00 00 00 40 3F58 1B 00 00 03 00 03 00 9A 99 19 3E 40 1F 00 0005 00 03 00 00 00 00 3F 10 27 00 00 08 00 03 008F C2 F5 3D E0 2E 00 00 04 00 02 00 00 00 40 40B0 36 00 00 07 00 02 00 00 00 00 3F 80 3E 00 0001 00 01 00 CD CC 4C 3E 50 46 00 00 02 00 04 0000 00 80 3F 20 4E 00 00 03 00 04 00 CD CC 4C 3ED8 59 00 00 05 00 04 00 33 33 33 3F A8 61 00 0008 00 04 00 9A 99 19 3E 60 6D 00 00 04 00 03 0000 00 80 40 30 75 00 00 07 00 03 00 33 33 33 3F00 7D 00 00 01 00 02 00 00 00 00 3F B8 88 00 0004 00 04 00 00 00 A0 40 88 90 00 00 07 00 04 0066 66 66 3F 40 9C 00 00 01 00 03 00 00 00 40 3FF8 A7 00 00 06 00 01 00 CD CC 4C 3D C8 AF 00 0001 00 04 00 00 00 80 3F 50 C3 00 00 06 00 02 00"));
        }
    }
}