using Microsoft.Win32.SafeHandles;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Utility;
using Server.Auth.Data.Models;
using Server.Auth.Data.Sync.Server;
using Server.Auth.Network;
using Server.Auth.Network.ClientPacket;
using Server.Auth.Network.ServerPacket;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;

namespace Server.Auth
{
    public class AuthClient : IDisposable
    {
        public int ServerId;
        public Socket Client;
        public Account Player;
        public DateTime SessionDate;
        public int SessionId;
        public ushort SessionSeed;
        private ushort NextSessionSeed;
        public int SessionShift, FirstPacketId;
        private bool Disposed = false, connectionClosed = false;
        private readonly SafeHandle Handle = new SafeFileHandle(IntPtr.Zero, true);

        public AuthClient(int ServerId, Socket Client)
        {
            this.ServerId = ServerId;
            this.Client = Client;
        }

        public Account GetAccount()
        {
            try
            {
                Account localPlayer = new Account();

                if (Player != null)
                {
                    localPlayer = Player;
                }
                else
                {
                    CLogger.Print($"AuthClient.GetAccount: Account is null for IP: {GetIPAddress()}", LoggerType.Warning);
                    localPlayer = null;
                }
                return localPlayer;
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
                return null;
            }
        }

        public void Dispose()
        {
            try
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (Disposed)
                {
                    return;
                }
                Player = null;
                if (Client != null)
                {
                    Client.Dispose();
                    Client = null;
                }
                if (disposing)
                {
                    Handle.Dispose();
                }
                Disposed = true;
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
        }

        public void StartSession()
        {
            try
            {
                NextSessionSeed = SessionSeed;
                SessionShift = ((SessionId + Bitwise.CRYPTO[0]) % 7 + 1);

                ThreadPool.QueueUserWorkItem(state => SendConnectAck());
                ThreadPool.QueueUserWorkItem(state => StartReceive());
                ThreadPool.QueueUserWorkItem(state => CheckConnectionTimeout());
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
                Close(0, true);
            }
        }

        private void CheckConnectionTimeout()
        {
            Thread.Sleep(10000);
            if (Client == null || FirstPacketId == 0)
            {
                CLogger.Print($"Conexión destruida por falta de respuestas. Dirección IP: {GetIPAddress()}", LoggerType.Warning);
                Close(0, true);
            }
        }

        public void HeartBeatCounter()
        {
            TimerState Tick = new TimerState();
            Tick.StartTimer(TimeSpan.FromMinutes(20), (callbackState) =>
            {
                if (!connectionClosed)
                {
                    CLogger.Print($"Conexión destruida debido a falta de respuesta durante 20 minutos (HeartBeat). IPAddress: {GetIPAddress()}", LoggerType.Warning);
                    Close(0, true);
                }
                lock (callbackState)
                {
                    Tick.StopJob();
                }
            });
        }

        public string GetIPAddress()
        {
            try
            {
                if (Client != null && Client.RemoteEndPoint != null)
                {
                    return (Client.RemoteEndPoint as IPEndPoint).Address.ToString();
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        public IPAddress GetAddress()
        {
            try
            {
                if (Client != null && Client.RemoteEndPoint != null)
                {
                    return (Client.RemoteEndPoint as IPEndPoint).Address;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void SendConnectAck() => SendPacket(new PROTOCOL_BASE_CONNECT_ACK(this));

        public void SendCompletePacket(byte[] Data, string PacketName)
        {
            try
            {
                if (Data.Length < 4)
                {
                    return;
                }

                byte[] Logical = new byte[5];
                byte[] Result = new byte[Data.Length + 1 + Logical.Length];
                Array.Copy(Data, 0, Result, 0, Data.Length);
                Array.Clear(Result, Data.Length, 1 + Logical.Length);
                if (ConfigLoader.DebugMode)
                {
                    ushort Opcode = BitConverter.ToUInt16(Result, 2);
                    string DebugData = Bitwise.ToByteString(Result);
                    CLogger.Print($"{PacketName}; Address: {Client.RemoteEndPoint}; Opcode: [{Opcode}]", LoggerType.Debug);
                }
                if (Result.Length > 0)
                {
                    byte[] FinalResult = Logical.Length == 3 ? Bitwise.Encrypt(Result, SessionShift) : Result;
                    Client.BeginSend(FinalResult, 0, FinalResult.Length, SocketFlags.None, new AsyncCallback(SendCallback), Client);
                }
            }
            catch
            {
                Close(0, true);
            }
        }

        public void SendPacket(byte[] originalData, string PacketName)
        {
            try
            {
                if (originalData.Length < 2)
                {
                    return;
                }

                byte[] packetSize = BitConverter.GetBytes((ushort)(originalData.Length + 2));
                byte[] newData = new byte[originalData.Length + packetSize.Length];

                Array.Copy(packetSize, 0, newData, 0, packetSize.Length);
                Array.Copy(originalData, 0, newData, 2, originalData.Length);

                byte[] packetData = new byte[newData.Length + 5]; //5 bytes para não encriptar
                Array.Copy(newData, 0, packetData, 0, newData.Length);

                if (ConfigLoader.DebugMode)
                {
                    ushort Opcode = BitConverter.ToUInt16(packetData, 2);  // Changed from Result to packetData
                    string DebugData = Bitwise.ToByteString(packetData);   // Changed from Result to packetData
                    CLogger.Print($"[AUTH] {PacketName}; Address: {Client.RemoteEndPoint}; Opcode: [{Opcode}]", LoggerType.Debug);
                }

                if (Client != null && packetData.Length > 0)
                {
                    Client.BeginSend(packetData, 0, packetData.Length, SocketFlags.None, new AsyncCallback(SendCallback), Client);
                }
                // Removed Packet.Dispose() since there's no Packet variable here
                packetData = null;
            }
            catch
            {
                Close(0, true);
            }
        }

        public void SendPacket(AuthServerPacket Packet)
        {
            try
            {
                // Obtener los datos primero
                byte[] originalData = Packet.GetBytes("AuthClient.SendPacket");
                if (originalData.Length < 2)
                {
                    Packet.Dispose();  // Disponer manualmente si hay una salida temprana
                    return;
                }

                byte[] packetSize = BitConverter.GetBytes((ushort)(originalData.Length + 2));
                byte[] newData = new byte[originalData.Length + packetSize.Length];

                Array.Copy(packetSize, 0, newData, 0, packetSize.Length);
                Array.Copy(originalData, 0, newData, 2, originalData.Length);

                byte[] packetData = new byte[newData.Length + 5]; //5 bytes para não encriptar
                Array.Copy(newData, 0, packetData, 0, newData.Length);

                if (ConfigLoader.DebugMode)
                {
                    ushort Opcode = BitConverter.ToUInt16(packetData, 2);
                    string DebugData = Bitwise.ToByteString(packetData);
                    CLogger.Print($"[AUTH] {Packet.GetType().Name}; Address: {Client.RemoteEndPoint}; Opcode: [{Opcode}]", LoggerType.Debug);
                }

                if (Client != null && Client.Connected && packetData.Length > 0)
                {
                    Client.BeginSend(packetData, 0, packetData.Length, SocketFlags.None, new AsyncCallback(SendCallback), Client);
                }

                // Disponer el paquete al final
                Packet.Dispose();
                packetData = null;
            }
            catch (SocketException)
            {
                // En caso de error de socket, disponer el paquete sin cerrar la conexión
                try { Packet?.Dispose(); } catch { }
                // No llamar Close aquí para evitar loops infinitos
            }
            catch (Exception ex)
            {
                // En caso de error, asegurarse de disponer el paquete
                try { Packet?.Dispose(); } catch { }
                CLogger.Print($"SendPacket error: {ex.Message}", LoggerType.Error, ex);
                Close(0, true);
            }
        }

        private void SendCallback(IAsyncResult Result)
        {
            try
            {
                if (Result.AsyncState is Socket Handler && Handler.Connected)
                {
                    Handler.EndSend(Result);
                }
            }
            catch
            {
                Close(0, true);
            }
        }

        private void StartReceive()
        {
            try
            {
                StateObject State = new StateObject()
                {
                    WorkSocket = Client,
                    Buffer = new byte[StateObject.BufferSize] 
                };
                Client.BeginReceive(State.Buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(OnReceiveCallback), State);
            }
            catch
            {
                Close(0, true);
            }
        }

        public void Close(int TimeMS, bool DestroyConnection)
        {
            if (connectionClosed)
            {
                return;
            }
            try
            {
                connectionClosed = true;
                AuthXender.Client.RemoveSession(this);
                Account Cache = Player;
                if (DestroyConnection)
                {
                    if (Cache != null)
                    {
                        Cache.SetOnlineStatus(false);
                        if (Cache.Status.ServerId == 0)
                        {
                            SendRefresh.RefreshAccount(Cache, false);
                        }
                        Cache.Status.ResetData(Cache.PlayerId);
                        Cache.SimpleClear();
                        Cache.UpdateCacheInfo();
                        Player = null;
                    }
                    if (Client != null)
                    {
                        Client.Close(TimeMS);
                    }
                    Thread.Sleep(TimeMS);
                    Dispose();
                }
                else if (Cache != null)
                {
                    Cache.SimpleClear();
                    Cache.UpdateCacheInfo();
                    Player = null;
                }
                UpdateServer.RefreshSChannel(ServerId);
            }
            catch (Exception ex)
            {
                CLogger.Print($"AuthClient.Close: {ex.Message}", LoggerType.Error, ex);
            }
        }

        private void OnReceiveCallback(IAsyncResult Result)
        {
            StateObject Packet = Result.AsyncState as StateObject;
            try
            {
                int BytesCount = Packet.WorkSocket.EndReceive(Result);
                if (BytesCount > 0)
                {
                    byte[] EcryptedPacket = new byte[BytesCount];
                    Array.Copy(Packet.Buffer, 0, EcryptedPacket, 0, BytesCount);
                    int PacketLength = BitConverter.ToUInt16(EcryptedPacket, 0) & 0x7FFF;
                    byte[] BufferPacket = new byte[PacketLength];
                    Array.Copy(EcryptedPacket, 2, BufferPacket, 0, BufferPacket.Length);
                    byte[] DecryptedPacket = Bitwise.Decrypt(BufferPacket, SessionShift);
                    ushort PacketId = BitConverter.ToUInt16(DecryptedPacket, 0);
                    ushort PacketSeed = BitConverter.ToUInt16(DecryptedPacket, 2);
                    FirstPacketCheck(PacketId);

                    if (!CheckSeed(PacketSeed, true))
                    {
                        Close(0, true);
                    }
                    else
                    {
                        if (connectionClosed)
                            return;

                        RunPacket(PacketId, DecryptedPacket, "REQ");
                        CheckOut(EcryptedPacket, PacketLength);

                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            try
                            {
                                StartReceive();
                            }
                            catch (Exception ex)
                            {
                                CLogger.Print($"Error processing packet in thread pool for IP: {GetIPAddress()}: {ex.Message}", LoggerType.Error, ex);
                                Close(0, true);
                            }
                        });
                    }
                }
            }
            catch
            {
                Close(0, true);
            }
        }

        public void CheckOut(byte[] BufferTotal, int FirstLength)
        {
            int BufferLength = BufferTotal.Length;
            try
            {
                byte[] EcryptedPacket = new byte[BufferLength - FirstLength - 3];
                Array.Copy(BufferTotal, FirstLength + 3, EcryptedPacket, 0, EcryptedPacket.Length);
                if (EcryptedPacket.Length == 0)
                {
                    return;
                }
                int PacketLength = BitConverter.ToUInt16(EcryptedPacket, 0) & 0x7FFF;
                byte[] BufferPacket = new byte[PacketLength];
                Array.Copy(EcryptedPacket, 2, BufferPacket, 0, BufferPacket.Length);
                byte[] DecryptedPacket = Bitwise.Decrypt(BufferPacket, SessionShift);
                ushort PacketId = BitConverter.ToUInt16(DecryptedPacket, 0);
                ushort PacketSeed = BitConverter.ToUInt16(DecryptedPacket, 2);
                if (!CheckSeed(PacketSeed, false))
                {
                    Close(0, true);
                    return;
                }
                RunPacket(PacketId, DecryptedPacket, "REQ");
                CheckOut(EcryptedPacket, PacketLength);
            }
            catch
            {
                Close(0, true);
            }
        }

        private void FirstPacketCheck(ushort PacketId)
        {
            if (PacketId != 0)
            {
                this.FirstPacketId = PacketId; // O simplemente this.FirstPacketId = 1; si solo es una bandera.
                return;
            }
            if (PacketId != 1281 && PacketId != 2309)
            {
                CLogger.Print($"Connection destroyed due to unknown first packet. Opcode: {PacketId}; IPAddress: {GetIPAddress()}", LoggerType.Warning);
                Close(0, true);
            }
            else
            {
                this.FirstPacketId = PacketId;
            }
        }

        public bool CheckSeed(ushort PacketSeed, bool IsTheFirstPacket)
        {
            if (PacketSeed == GetNextSessionSeed())
            {
                return true;
            }
            CLogger.Print($"Connection blocked. IP: {GetIPAddress()}; Date: {DateTimeUtil.Now()}; SessionId: {SessionId}; PacketSeed: {PacketSeed} / NextSessionSeed: {NextSessionSeed}; PrimarySeed: {SessionSeed}", LoggerType.Warning);
            if (IsTheFirstPacket)
            {
                ThreadPool.QueueUserWorkItem(state => StartReceive());
            }
            return false;
        }

        private ushort GetNextSessionSeed()
        {
            NextSessionSeed = (ushort)((((NextSessionSeed * 214013) + 2531011) >> 16) & short.MaxValue);
            return NextSessionSeed;
        }

        private void RunPacket(ushort Opcode, byte[] Buffer, string Value)
        {
            try
            {
                AuthClientPacket packet = null;
                switch (Opcode)
                {
                    case 1057:
                        packet = new PROTOCOL_AUTH_GET_POINT_CASH_REQ();
                        break;

                    case 1281:
                        packet = new PROTOCOL_BASE_LOGIN_REQ();
                        break;

                    case 2307:
                        packet = new PROTOCOL_BASE_LOGOUT_REQ();
                        break;

                    case 2309:
                        packet = new PROTOCOL_BASE_KEEP_ALIVE_REQ();
                        break;

                    case 2311:
                        packet = new PROTOCOL_BASE_GAMEGUARD_REQ();
                        break;

                    case 2314:
                        packet = new PROTOCOL_BASE_GET_SYSTEM_INFO_REQ();
                        break;

                    case 2316:
                        packet = new PROTOCOL_BASE_GET_USER_INFO_REQ();
                        break;

                    case 2318:
                        packet = new PROTOCOL_BASE_GET_INVEN_INFO_REQ();
                        break;

                    case 2320:
                        packet = new PROTOCOL_BASE_GET_OPTION_REQ();
                        break;

                    case 2322:
                        packet = new PROTOCOL_BASE_OPTION_SAVE_REQ();
                        break;

                    case 2328:
                        packet = new PROTOCOL_BASE_USER_LEAVE_REQ();
                        break;

                    case 2332:
                        packet = new PROTOCOL_BASE_GET_CHANNELLIST_REQ();
                        break;

                    case 2399:
                        packet = new PROTOCOL_BASE_GAME_SERVER_STATE_REQ();
                        break;

                    case 2414:
                        packet = new PROTOCOL_BASE_DAILY_RECORD_REQ();
                        break;

                    case 2459:
                        packet = new PROTOCOL_BASE_GET_MAP_INFO_REQ();
                        break;

                    case 2489:
                        packet = new PROTOCOL_BASE_CHANNELTYPE_CHANGE_CONDITION_REQ();
                        break;

                    case 2516:
                        packet = new PROTOCOL_BASE_MISSION_CARD_INFO_STREAM_REQ();
                        break;

                    case 7681:
                        packet = new PROTOCOL_MATCH_SERVER_IDX_REQ();
                        break;

                    case 7699:
                        packet = new PROTOCOL_MATCH_CLAN_SEASON_REQ();
                        break;

                    default:
                        CLogger.Print(Bitwise.ToHexData($"Opcode Not Found: [{Opcode}] | {Value}", Buffer), LoggerType.Opcode);
                        break;
                }
                if (packet != null)
                {
                    using (packet)
                    {
                        if (ConfigLoader.DebugMode)
                        {
                            CLogger.Print($"[AUTH] {packet.GetType().Name}; Address: {Client.RemoteEndPoint}; Opcode: [{Opcode}]", LoggerType.Debug);
                        }
                        packet.Makeme(this, Buffer);
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            try
                            {
                                packet.Run();
                            }
                            catch (Exception Ex)
                            {
                                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
                                Close(50, true);
                            }
                        });
                        packet.Dispose();
                        Buffer = null;
                    }
                }
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
        }
    }
}