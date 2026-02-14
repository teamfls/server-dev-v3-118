using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.XML;
using Server.Game.Data.Sync;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Numerics;

namespace Server.Game
{
    public class GameXender
    {
        public static GameSync Sync { get; set; }
        public static GameManager Client { get; set; }
        public static ConcurrentDictionary<int, GameClient> SocketSessions { get; set; }
        public static ConcurrentDictionary<string, DateTime> SocketConnections { get; set; }

        public static bool GetPlugin(int ServerId, string Host, int Port)
        {
            try
            {
                SocketSessions = new ConcurrentDictionary<int, GameClient>();
                SocketConnections = new ConcurrentDictionary<string, DateTime>();
                IPEndPoint EP = SynchronizeXML.GetServer(Port).Connection;
                Sync = new GameSync(EP);
                Client = new GameManager(ServerId, Host, ConfigLoader.IsUseProxy ? ConfigLoader.PROXY_PORT[1]++ : ConfigLoader.DEFAULT_PORT[1]++);
                Sync.Start();
                Client.Start();
                return true;
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
                return false;
            }
        }

        public static void UpdateEvents()
        {
            foreach (var Client in SocketSessions.Values)
            {
                if (Client != null)
                {
                    Client.SendPacket(new PROTOCOL_BASE_EVENT_PORTAL_ACK(true));
                }
            }
        }

        public static void UpdateShop()
        {
            foreach (var Client in SocketSessions.Values)
            {
                if (Client != null)
                {
                    foreach (ShopData Data in ShopManager.ShopDataItems)
                    {
                        Client.SendPacket(new PROTOCOL_AUTH_SHOP_ITEMLIST_ACK(Data, ShopManager.TotalItems));
                    }
                    foreach (ShopData Data in ShopManager.ShopDataGoods)
                    {
                        Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODSLIST_ACK(Data, ShopManager.TotalGoods));
                    }
                    foreach (ShopData Data in ShopManager.ShopDataItemRepairs)
                    {
                        Client.SendPacket(new PROTOCOL_AUTH_SHOP_REPAIRLIST_ACK(Data, ShopManager.TotalRepairs));
                    }
                    foreach (ShopData Data in BattleBoxXML.ShopDataBattleBoxes)
                    {
                        Client.SendPacket(new PROTOCOL_BATTLEBOX_GET_LIST_ACK(Data, BattleBoxXML.TotalBoxes));
                    }
                    Client.SendPacket(new PROTOCOL_SHOP_TAG_INFO_ACK());
                    Client.SendPacket(new PROTOCOL_SHOP_GET_SAILLIST_ACK(true));
                }
            }
        }

        /// <summary>
        /// Broadcasts limited sale stock sync to all connected clients
        /// </summary>
        public static void BroadcastLimitedSaleSync()
        {
            foreach (var Client in SocketSessions.Values)
            {
                if (Client != null)
                {
                    try
                    {
                        Client.SendPacket(new PROTOCOL_SHOP_LIMITED_SALE_SYNC_ACK());
                    }
                    catch { }
                }
            }
        }

    }
}