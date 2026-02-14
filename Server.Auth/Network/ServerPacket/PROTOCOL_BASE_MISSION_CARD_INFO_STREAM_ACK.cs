// Decompiled with JetBrains decompiler
// Type: Server.Auth.Network.ServerPacket.PROTOCOL_BASE_MISSION_CARD_INFO_STREAM_ACK
// Assembly: Server.Auth, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: D2254E5E-B0BA-4DE9-9720-2DDECE3CD4EF
// Assembly location: C:\Users\home\Desktop\dll\Server.Auth-deobfuscated-Cleaned.dll

using System.Runtime.CompilerServices;


namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_BASE_MISSION_CARD_INFO_STREAM_ACK : AuthServerPacket
    {
        protected readonly string[] MISSION_NAME;
        protected readonly string[] MISSION_DESCRIPTION;


        public PROTOCOL_BASE_MISSION_CARD_INFO_STREAM_ACK()
        {
            this.MISSION_NAME = new string[5]
            {
      " Mission #1",
      " Mission #2",
      " Mission #3",
      " Mission #4",
      " Mission #5"
            };
            this.MISSION_DESCRIPTION = new string[5]
            {
      " Description #1",
      " Description #2",
      " Description #3",
      " Description #4",
      " Description #5"
            };
        }


        public override void Write()
        {
            this.WriteH((short)2517);
            this.WriteH((short)16 /*0x10*/);
            this.WriteC((byte)4);
            this.WriteC((byte)1);
            this.WriteC((byte)this.MISSION_NAME[0].Length);
            this.WriteN(this.MISSION_NAME[0], this.MISSION_NAME[0].Length, "UTF-16LE");
            this.WriteC((byte)this.MISSION_DESCRIPTION[0].Length);
            this.WriteN(this.MISSION_DESCRIPTION[0], this.MISSION_DESCRIPTION[0].Length, "UTF-16LE");
            for (int index = 0; index < 10; ++index)
            {
                this.WriteH((short)2);
                this.WriteH((short)11);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)16 /*0x10*/);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)15);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)27);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
            }
            this.WriteD(0);
            for (int index = 0; index < 10; ++index)
            {
                this.WriteD(1000);
                this.WriteB(new byte[48 /*0x30*/]);
            }
            this.WriteD(90000);
            this.WriteD(0);
            this.WriteD(3400016);
            this.WriteD(86400);
            this.WriteB(new byte[32 /*0x20*/]);
            this.WriteC((byte)5);
            this.WriteC((byte)this.MISSION_NAME[1].Length);
            this.WriteN(this.MISSION_NAME[1], this.MISSION_NAME[1].Length, "UTF-16LE");
            this.WriteC((byte)this.MISSION_DESCRIPTION[1].Length);
            this.WriteN(this.MISSION_DESCRIPTION[1], this.MISSION_DESCRIPTION[1].Length, "UTF-16LE");
            for (int index = 0; index < 10; ++index)
            {
                this.WriteH((short)2);
                this.WriteH((short)11);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)16 /*0x10*/);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)15);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)27);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
            }
            this.WriteD(0);
            for (int index = 0; index < 10; ++index)
            {
                this.WriteD(1000);
                this.WriteB(new byte[48 /*0x30*/]);
            }
            this.WriteD(20000);
            this.WriteD(0);
            this.WriteD(106199);
            this.WriteD(2592000);
            this.WriteB(new byte[32 /*0x20*/]);
            this.WriteC((byte)6);
            this.WriteC((byte)this.MISSION_NAME[2].Length);
            this.WriteN(this.MISSION_NAME[2], this.MISSION_NAME[2].Length, "UTF-16LE");
            this.WriteC((byte)this.MISSION_DESCRIPTION[2].Length);
            this.WriteN(this.MISSION_DESCRIPTION[2], this.MISSION_DESCRIPTION[1].Length, "UTF-16LE");
            for (int index = 0; index < 10; ++index)
            {
                this.WriteH((short)2);
                this.WriteH((short)11);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)16 /*0x10*/);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)15);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)27);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
            }
            this.WriteD(0);
            for (int index = 0; index < 10; ++index)
            {
                this.WriteD(1000);
                this.WriteB(new byte[48 /*0x30*/]);
            }
            this.WriteD(20000);
            this.WriteD(0);
            this.WriteD(106199);
            this.WriteD(2592000);
            this.WriteB(new byte[32 /*0x20*/]);
            this.WriteC((byte)7);
            this.WriteC((byte)this.MISSION_NAME[3].Length);
            this.WriteN(this.MISSION_NAME[3], this.MISSION_NAME[3].Length, "UTF-16LE");
            this.WriteC((byte)this.MISSION_DESCRIPTION[3].Length);
            this.WriteN(this.MISSION_DESCRIPTION[3], this.MISSION_DESCRIPTION[3].Length, "UTF-16LE");
            for (int index = 0; index < 10; ++index)
            {
                this.WriteH((short)2);
                this.WriteH((short)11);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)16 /*0x10*/);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)15);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)27);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
            }
            this.WriteD(0);
            for (int index = 0; index < 10; ++index)
            {
                this.WriteD(1000);
                this.WriteB(new byte[48 /*0x30*/]);
            }
            this.WriteD(20000);
            this.WriteD(0);
            this.WriteD(106199);
            this.WriteD(2592000);
            this.WriteB(new byte[32 /*0x20*/]);
            this.WriteC((byte)8);
            this.WriteC((byte)this.MISSION_NAME[4].Length);
            this.WriteN(this.MISSION_NAME[4], this.MISSION_NAME[4].Length, "UTF-16LE");
            this.WriteC((byte)this.MISSION_DESCRIPTION[4].Length);
            this.WriteN(this.MISSION_DESCRIPTION[4], this.MISSION_DESCRIPTION[4].Length, "UTF-16LE");
            for (int index = 0; index < 10; ++index)
            {
                this.WriteH((short)2);
                this.WriteH((short)11);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)16 /*0x10*/);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)15);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
                this.WriteH((short)2);
                this.WriteH((short)27);
                this.WriteC((byte)10);
                this.WriteC((byte)0);
                this.WriteB(new byte[17]);
            }
            this.WriteD(0);
            for (int index = 0; index < 10; ++index)
            {
                this.WriteD(1000);
                this.WriteB(new byte[48 /*0x30*/]);
            }
            this.WriteD(20000);
            this.WriteD(0);
            this.WriteD(106199);
            this.WriteD(2592000);
            this.WriteB(new byte[32 /*0x20*/]);
        }
    }
}