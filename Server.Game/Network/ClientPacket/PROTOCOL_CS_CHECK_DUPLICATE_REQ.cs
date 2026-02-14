// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_CS_CHECK_DUPLICATE_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Managers;
using Server.Game.Network.ServerPacket;
using System;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_CS_CHECK_DUPLICATE_REQ : GameClientPacket
    {
        private string Field0;

        public override void Read() => this.Field0 = this.ReadU((int)this.ReadC() * 2);

        public override void Run()
        {
            try
            {
                this.Client.SendPacket(new PROTOCOL_CS_CHECK_DUPLICATE_ACK(!ClanManager.IsClanNameExist(this.Field0) ? 0U : 2147483648U /*0x80000000*/));
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }
    }
}