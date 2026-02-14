// Decompiled with JetBrains decompiler
// Type: Server.Game.Network.ServerPacket.PROTOCOL_BASE_EVENT_PORTAL_ACK
// Assembly: Server.Game, Version=1.1.25163.0, Culture=neutral, PublicKeyToken=null
// MVID: 2BF67F5F-ABA1-4CD4-BD5E-51B3899CA9A8
// Assembly location: C:\Users\home\Desktop\dll\Server.Game-deobfuscated-Cleaned.dll

using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.XML;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_BASE_EVENT_PORTAL_ACK : GameServerPacket
    {
        private readonly bool Field0;

        public PROTOCOL_BASE_EVENT_PORTAL_ACK(bool A_1) => this.Field0 = A_1;


        public override void Write()
        {
            this.WriteH((short)2511);
            this.WriteC(this.Field0 ? (byte)1 : (byte)0);
            this.WriteC((byte)1);
            this.WriteD(8192 /*0x2000*/);
            this.WriteC((byte)PortalManager.AllEvents.Count);
            foreach (KeyValuePair<string, PortalEvents> allEvent in PortalManager.AllEvents)
            {
                if (allEvent.Key.Contains("Boost") && allEvent.Value == PortalEvents.BoostEvent)
                {
                    EventBoostModel Boost = EventBoostXML.GetEvent(PortalManager.GetInitialId(allEvent.Key));
                    uint[] DateTime = new uint[2] { Boost.BeginDate, Boost.EndedDate };
                    string[] Info = new string[2] { Boost.Name, Boost.Description };
                    byte[] Type = new byte[2] { Boost.Period ? (byte) 0 : (byte) 1, Boost.Priority ? (byte) 1 : (byte) 0 };
                    this.WriteB(PortalManager.InitEventData(allEvent.Value, Boost.Id, DateTime, Info, Type, (ushort)0));
                    this.WriteB(PortalManager.InitBoostData(Boost));
                }
                else if (allEvent.Key.Contains("RankUp") && allEvent.Value == PortalEvents.RankUpEvent)
                {
                    EventRankUpModel RankUp = EventRankUpXML.GetEvent(PortalManager.GetInitialId(allEvent.Key));
                    uint[] DateTime = new uint[2]
                    {
          RankUp.BeginDate,
          RankUp.EndedDate
                    };
                    string[] Info = new string[2]
                    {
          RankUp.Name,
          RankUp.Description
                    };
                    byte[] Type = new byte[2]
                    {
          RankUp.Period ? (byte) 0 : (byte) 1,
          RankUp.Priority ? (byte) 1 : (byte) 0
                    };
                    this.WriteB(PortalManager.InitEventData(allEvent.Value, RankUp.Id, DateTime, Info, Type, (ushort)1));
                    this.WriteB(PortalManager.InitRankUpData(RankUp));
                }
                else if (allEvent.Key.Contains("Login") && allEvent.Value == PortalEvents.LoginEvent)
                {
                    EventLoginModel Login = EventLoginXML.GetEvent(PortalManager.GetInitialId(allEvent.Key));
                    uint[] DateTime = new uint[2]
                    {
          Login.BeginDate,
          Login.EndedDate
                    };
                    string[] Info = new string[2]
                    {
          Login.Name,
          Login.Description
                    };
                    byte[] Type = new byte[2]
                    {
          Login.Period ? (byte) 0 : (byte) 1,
          Login.Priority ? (byte) 1 : (byte) 0
                    };
                    this.WriteB(PortalManager.InitEventData(allEvent.Value, Login.Id, DateTime, Info, Type, (ushort)1));
                    this.WriteB(PortalManager.InitLoginData(Login));
                }
                else if (allEvent.Key.Contains("Playtime") && allEvent.Value == PortalEvents.PlaytimeEvent)
                {
                    EventPlaytimeModel Playtime = EventPlaytimeJSON.GetEvent(PortalManager.GetInitialId(allEvent.Key));
                    uint[] DateTime = new uint[2]
                    {
          Playtime.BeginDate,
          Playtime.EndedDate
                    };
                    string[] Info = new string[2]
                    {
          Playtime.Name,
          Playtime.Description
                    };
                    byte[] Type = new byte[2]
                    {
          Playtime.Period ? (byte) 0 : (byte) 1,
          Playtime.Priority ? (byte) 1 : (byte) 0
                    };
                    this.WriteB(PortalManager.InitEventData(allEvent.Value, Playtime.Id, DateTime, Info, Type, (ushort)0));
                    this.WriteB(PortalManager.InitPlaytimeData(Playtime));
                }
            }
        }
    }
}