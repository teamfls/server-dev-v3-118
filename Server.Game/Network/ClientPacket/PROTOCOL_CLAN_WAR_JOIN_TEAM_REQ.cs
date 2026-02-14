// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ClientPacket.PROTOCOL_CLAN_WAR_JOIN_TEAM_REQ
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Models;
using Server.Game.Data.XML;
using Server.Game.Network.ServerPacket;
using System;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_CLAN_WAR_JOIN_TEAM_REQ : GameClientPacket
    {
        private int Field0;
        private int Field1;
        private int Field2;
        private uint Field3;

        public override void Read()
        {
            this.Field0 = (int)this.ReadH();
            this.Field1 = (int)this.ReadH();
            this.Field2 = (int)this.ReadC();
        }

        public override void Run()
        {
            try
            {
                Account player = this.Client.GetAccount();
                if (this.Field2 < 2 && player != null && player.Match == null && player.Room == null)
                {
                    int num = this.Field1 - this.Field1 / 10 * 10;
                    ChannelModel channel = ChannelsXML.GetChannel(this.Field1, this.Field2 == 0 ? num : player.ChannelId);
                    if (channel == null)
                        this.Client.SendPacket(new PROTOCOL_CLAN_WAR_JOIN_TEAM_ACK(2147483648U /*0x80000000*/));
                    else if (player.ClanId == 0)
                    {
                        this.Client.SendPacket(new PROTOCOL_CLAN_WAR_JOIN_TEAM_ACK(2147487835U));
                    }
                    else
                    {
                        MatchModel A_2 = this.Field2 == 1 ? channel.GetMatch(this.Field0, player.ClanId) : channel.GetMatch(this.Field0);
                        if (A_2 == null)
                            this.Client.SendPacket(new PROTOCOL_CLAN_WAR_JOIN_TEAM_ACK(2147483648U /*0x80000000*/));
                        else
                            this.Method0(player, A_2);
                    }
                }
                else
                    this.Client.SendPacket(new PROTOCOL_CLAN_WAR_JOIN_TEAM_ACK(2147483648U /*0x80000000*/));
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        private void Method0(Account A_1, MatchModel A_2)
        {
            if (!A_2.AddPlayer(A_1))
                this.Field3 = 2147483648U /*0x80000000*/;
            this.Client.SendPacket(new PROTOCOL_CLAN_WAR_JOIN_TEAM_ACK(this.Field3, A_2));
            if (this.Field3 != 0U)
                return;
            using (PROTOCOL_CLAN_WAR_REGIST_MERCENARY_ACK Packet = new PROTOCOL_CLAN_WAR_REGIST_MERCENARY_ACK(A_2))
                A_2.SendPacketToPlayers(Packet);
        }
    }
}