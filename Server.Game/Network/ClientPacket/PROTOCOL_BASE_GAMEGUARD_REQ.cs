// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_BASE_GAMEGUARD_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Network.ServerPacket;
using System;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_BASE_GAMEGUARD_REQ : GameClientPacket
    {
        private byte[] Field0;

        public override void Read()
        {
            this.ReadB(48 /*0x30*/);
            this.Field0 = this.ReadB(3);
        }

        public override void Run()
        {
            try
            {
                this.Client.SendPacket(new PROTOCOL_BASE_GAMEGUARD_ACK(0, this.Field0));
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }
    }
}