// Decompiled with JetBrains decompiler
// Type: Server.Auth.Network.ClientPacket.PROTOCOL_BASE_GAMEGUARD_REQ
// Assembly: Server.Auth, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: D2254E5E-B0BA-4DE9-9720-2DDECE3CD4EF
// Assembly location: C:\Users\home\Desktop\dll\Server.Auth-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Server.Auth.Network.ServerPacket;
using System;
using System.Runtime.CompilerServices;

namespace Server.Auth.Network.ClientPacket
{
    public class PROTOCOL_BASE_GAMEGUARD_REQ : AuthClientPacket
    {
        private byte[] Version;

        public override void Read()
        {
            this.ReadB(48 /*0x30*/);
            this.Version = this.ReadB(3);
        }
        public override void Run()
        {
            try
            {
                this.Client.SendPacket((AuthServerPacket)new PROTOCOL_BASE_GAMEGUARD_ACK(0, this.Version));
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_BASE_GAMEGUARD_REQ: " + ex.Message, LoggerType.Error, ex);
            }
        }
    }
}
