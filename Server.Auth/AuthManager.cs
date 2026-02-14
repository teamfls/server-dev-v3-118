using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.JSON;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using Server.Auth.Data.Models;
using Server.Auth.Network;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server.Auth
{
    public class AuthManager
    {
        private readonly string Host;
        private readonly int Port;
        public readonly int ServerId;
        public ServerConfig Config;
        public Socket MainSocket;
        public bool ServerIsClosed;

        public AuthManager(int ServerId, string Host, int Port)
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
                CLogger.Print($"Auth Serv Address {Host}:{Port}", LoggerType.Info);
                Thread OnDuty = new Thread(ReadCallBack)
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

        private void ReadCallBack()
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
                if ((DateTimeUtil.Now() - CLogger.LastAuthException).Minutes >= 1)
                {
                    CLogger.Print($"Accept Callback Date: {DateTimeUtil.Now()}; Exception: {ex.Message}", LoggerType.Error);
                    CLogger.LastAuthException = DateTimeUtil.Now();
                }
            }
            SessionCallback(Handler);
        }

        private void SessionCallback(Socket Handler)
        {
            try
            {
                Thread.Sleep(5);
                ReadCallBack();
                if (Handler != null)
                {
                    AddSession(new AuthClient(ServerId, Handler));
                }
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
        }

        public void AddSession(AuthClient Client)
        {
            try
            {
                if (Client == null)
                {
                    CLogger.Print("Destroyed after failed to add to list.", LoggerType.Warning);
                    return;
                }
                DateTime Connect = DateTimeUtil.Now();
                if (!AuthXender.SocketConnections.ContainsKey(Client.GetIPAddress()) && AuthXender.SocketConnections.TryAdd(Client.GetIPAddress(), Connect) || AuthXender.SocketConnections.TryGetValue(Client.GetIPAddress(), out DateTime GetDate) && (Connect - GetDate).TotalSeconds >= 5 && AuthXender.SocketConnections.TryUpdate(Client.GetIPAddress(), Connect, GetDate))
                {
                    for (int SessionId = 1; SessionId < 100000; SessionId++)
                    {
                        if (!AuthXender.SocketSessions.ContainsKey(SessionId) && AuthXender.SocketSessions.TryAdd(SessionId, Client))
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
                    if ((Connect - CLogger.LatchAuthSession).Minutes >= 1)
                    {
                        CLogger.Print($"This connection is blocked for 5 seconds. IP: {Client.GetIPAddress()}; Date:{Connect}", LoggerType.Warning);
                        CLogger.LatchAuthSession = Connect;
                    }
                    Client.Close(500, true);
                }
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
        }

        public bool RemoveSession(AuthClient Client)
        {
            try
            {
                if (Client == null || Client.SessionId == 0)
                {
                    return false;
                }
                if (AuthXender.SocketSessions.ContainsKey(Client.SessionId) && AuthXender.SocketSessions.TryGetValue(Client.SessionId, out Client))
                {
                    return AuthXender.SocketSessions.TryRemove(Client.SessionId, out Client);
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
                if (AuthXender.SocketConnections.ContainsKey(Address) && AuthXender.SocketConnections.TryGetValue(Address, out DateTime Date))
                {
                    return AuthXender.SocketConnections.TryRemove(Address, out Date);
                }
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
            return false;
        }

        public int SendPacketToAllClients(AuthServerPacket Packet)
        {
            int Count = 0;
            if (AuthXender.SocketSessions.Count == 0)
            {
                return Count;
            }
            byte[] Data = Packet.GetCompleteBytes("AuthManager.SendPacketToAllClients");
            foreach (AuthClient Client in AuthXender.SocketSessions.Values)
            {
                Account Player = Client.Player;
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
            if (AuthXender.SocketSessions.Count == 0)
            {
                return null;
            }
            foreach (AuthClient client in AuthXender.SocketSessions.Values)
            {
                Account player = client.Player;
                if (player != null && player.PlayerId == accountId)
                {
                    return player;
                }
            }
            return null;
        }
    }
}