using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.JSON;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using Server.Game.Network;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server.Game
{
    public class GameManager
    {
        private readonly string Host;
        private readonly int Port;
        public readonly int ServerId;
        public ServerConfig Config;
        public Socket MainSocket;
        public bool ServerIsClosed;

        public GameManager(int ServerId, string Host, int Port)
        {
            this.Host = Host;
            this.Port = Port;
            this.ServerId = ServerId;
        }

        public bool Start()
        {
            try
            {
                Config = ServerConfigJSON.GetConfig(ConfigLoader.ConfigId);
                MainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint Local = new IPEndPoint(IPAddress.Parse(Host), Port);
                MainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                MainSocket.SetIPProtectionLevel(IPProtectionLevel.EdgeRestricted);
                MainSocket.DontFragment = false;
                MainSocket.NoDelay = true;
                MainSocket.Bind(Local);
                MainSocket.Listen(ConfigLoader.BackLog);
                CLogger.Print($"Game Serv Address {Host}:{Port}", LoggerType.Info);
                Thread OnDuty = new Thread(ReadCallback)
                {
                    Priority = ThreadPriority.Highest
                };
                OnDuty.Start();
                return true;
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
                return false;
            }
        }

        private void ReadCallback()
        {
            try
            {
                MainSocket.BeginAccept(new AsyncCallback(AcceptCallback), MainSocket);
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
        }

        private void AcceptCallback(IAsyncResult Result)
        {
            if (ServerIsClosed)
            {
                return;
            }
            Socket ClientSocket = Result.AsyncState as Socket;
            Socket Handler = null;
            try
            {
                Handler = ClientSocket.EndAccept(Result);
            }
            catch (Exception ex)
            {
                if ((DateTimeUtil.Now() - CLogger.LastGameException).Minutes >= 1)
                {
                    CLogger.Print($"Accept Callback Date: {DateTimeUtil.Now()}; Exception: {ex.Message}", LoggerType.Error);
                    CLogger.LastGameException = DateTimeUtil.Now();
                }
            }
            SessionCallBack(Handler, ClientSocket);
        }

        private void SessionCallBack(Socket Handler, Socket ClientSocket)
        {
            try
            {
                Thread.Sleep(5);
                ReadCallback();
                if (Handler != null)
                {
                    AddSession(new GameClient(ServerId, Handler));
                }
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
        }

        public void AddSession(GameClient Client)
        {
            try
            {
                if (Client == null)
                {
                    CLogger.Print("Destroyed after failed to add to list.", LoggerType.Warning);
                    return;
                }
                DateTime Connect = DateTimeUtil.Now();
                if (!GameXender.SocketConnections.ContainsKey(Client.GetIPAddress()) && GameXender.SocketConnections.TryAdd(Client.GetIPAddress(), Connect) || GameXender.SocketConnections.TryGetValue(Client.GetIPAddress(), out DateTime GetDate) && (Connect - GetDate).TotalSeconds >= 5 && GameXender.SocketConnections.TryUpdate(Client.GetIPAddress(), Connect, GetDate))
                {
                    for (int SessionId = 1; SessionId < 100000; SessionId++)
                    {
                        if (!GameXender.SocketSessions.ContainsKey(SessionId) && GameXender.SocketSessions.TryAdd(SessionId, Client))
                        {
                            Client.SessionDate = Connect;
                            Client.SessionId = SessionId;
                            Client.SessionSeed = (ushort)new Random(Connect.Millisecond).Next(SessionId, 0x7FFF);
                            Client.StartSession();
                            return;
                        }
                    }
                    CLogger.Print($"Unable to add session list. IPAddress: {Client.GetIPAddress()}; Date: {Connect}", LoggerType.Warning);
                    Client.Close(500, true);
                }
                else
                {
                    if ((Connect - CLogger.LastGameSession).Minutes >= 1)
                    {
                        CLogger.Print($"This connection is blocked for 5 seconds. IP: {Client.GetIPAddress()}; Date:{Connect}", LoggerType.Warning);
                        CLogger.LastGameSession = Connect;
                    }
                    Client.Close(500, true);
                }
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
        }

        public bool RemoveSession(GameClient Client)
        {
            try
            {
                if (Client == null || Client.SessionId == 0)
                {
                    return false;
                }
                if (GameXender.SocketSessions.ContainsKey(Client.SessionId) && GameXender.SocketSessions.TryGetValue(Client.SessionId, out Client))
                {
                    return GameXender.SocketSessions.TryRemove(Client.SessionId, out Client);
                }
                Client = null;
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
            return false;
        }

        public bool RemoveConnection(string Address)
        {
            try
            {
                if (GameXender.SocketConnections.ContainsKey(Address) && GameXender.SocketConnections.TryGetValue(Address, out DateTime Date))
                {
                    return GameXender.SocketConnections.TryRemove(Address, out Date);
                }
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
            return false;
        }

        public int SendPacketToAllClients(GameServerPacket Packet)
        {
            int Count = 0;
            if (GameXender.SocketSessions.Count == 0)
            {
                return Count;
            }
            byte[] Data = Packet.GetCompleteBytes("GameManager.SendPacketToAllClients");
            foreach (GameClient Client in GameXender.SocketSessions.Values)
            {
                Account Player = Client.GetAccount();
                if (Player != null && Player.IsOnline)
                {
                    Player.SendCompletePacket(Data, Packet.GetType().Name);
                    Count++;
                }
            }
            return Count;
        }

        public Account SearchActiveClient(long accountId)
        {
            if (GameXender.SocketSessions.Count == 0)
            {
                return null;
            }
            foreach (GameClient client in GameXender.SocketSessions.Values)
            {
                Account player = client.Player;
                if (player != null && player.PlayerId == accountId)
                {
                    return player;
                }
            }
            return null;
        }

        public Account SearchActiveClient(uint sessionId)
        {
            if (GameXender.SocketSessions.Count == 0)
            {
                return null;
            }
            foreach (GameClient client in GameXender.SocketSessions.Values)
            {
                if (client.Player != null && client.SessionId == sessionId)
                {
                    return client.Player;
                }
            }
            return null;
        }

        public int KickActiveClient(double Hours)
        {
            int count = 0;
            DateTime now = DateTimeUtil.Now();
            foreach (GameClient client in GameXender.SocketSessions.Values)
            {
                Account pl = client.Player;
                if (pl != null && pl.Room == null && pl.ChannelId > -1 && !pl.IsGM() && (now - pl.LastLobbyEnter).TotalHours >= Hours)
                {
                    count++;
                    pl.Close(5000);
                }
            }
            return count;
        }

        public int KickCountActiveClient(double Hours)
        {
            int count = 0;
            DateTime now = DateTimeUtil.Now();
            foreach (GameClient client in GameXender.SocketSessions.Values)
            {
                Account pl = client.Player;
                if (pl != null && pl.Room == null && pl.ChannelId > -1 && !pl.IsGM() && (now - pl.LastLobbyEnter).TotalHours >= Hours)
                {
                    count++;
                }
            }
            return count;
        }
    }
}