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
        public bool IsLoginAckSent = false;
        public byte[] CMessEncryptKey = new byte[16];
        public byte[] CMessDecryptKey = new byte[16];
        public bool IsCMessReady = false;
        public object RSAPrivateKey;
        public int SessionMode => SessionId % 3;
        public int FirstPacketId;
        public bool IsFirstPacketSent = false;
        private bool _loggedFirstEncrypt = false;
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
                if (Player != null) return Player;
                CLogger.Print($"AuthClient.GetAccount: Account is null for IP: {GetIPAddress()}", LoggerType.Warning);
                return null;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return null;
            }
        }

        public void Dispose()
        {
            try { Dispose(true); GC.SuppressFinalize(this); }
            catch (Exception ex) { CLogger.Print(ex.Message, LoggerType.Error, ex); }
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (Disposed) return;
                Player = null;
                RSAPrivateKey = null;
                if (Client != null) { Client.Dispose(); Client = null; }
                if (disposing) Handle.Dispose();
                Disposed = true;
            }
            catch (Exception ex) { CLogger.Print(ex.Message, LoggerType.Error, ex); }
        }

        //public void StartSession()
        //{
        //    try
        //    {
        //        NextSessionSeed = SessionSeed;
        //        // CMessEncryptKey/DecryptKey di-set di dalam PROTOCOL_BASE_CONNECT_ACK constructor
        //        ThreadPool.QueueUserWorkItem(state => SendConnectAck());
        //        ThreadPool.QueueUserWorkItem(state => StartReceive());
        //        ThreadPool.QueueUserWorkItem(state => CheckConnectionTimeout());
        //    }
        //    catch (Exception ex)
        //    {
        //        CLogger.Print(ex.Message, LoggerType.Error, ex);
        //        Close(0, true);
        //    }
        //}

        public void StartSession()
        {
            try
            {
                SessionSeed = 0;
                NextSessionSeed = 0;

                ThreadPool.QueueUserWorkItem(state => SendConnectAck());
                ThreadPool.QueueUserWorkItem(state => StartReceive());
                ThreadPool.QueueUserWorkItem(state => CheckConnectionTimeout());
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                Close(0, true);
            }
        }

        private void CheckConnectionTimeout()
        {
            Thread.Sleep(10000);
            if (Client == null || FirstPacketId == 0)
            {
                CLogger.Print($"Conexión destruida por falta de respuestas. IP: {GetIPAddress()}", LoggerType.Warning);
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
                    CLogger.Print($"HeartBeat timeout. IP: {GetIPAddress()}", LoggerType.Warning);
                    Close(0, true);
                }
                lock (callbackState) { Tick.StopJob(); }
            });
        }

        public string GetIPAddress()
        {
            try
            {
                if (Client != null && Client.RemoteEndPoint != null)
                    return (Client.RemoteEndPoint as IPEndPoint).Address.ToString();
                return "";
            }
            catch { return ""; }
        }

        public IPAddress GetAddress()
        {
            try
            {
                if (Client != null && Client.RemoteEndPoint != null)
                    return (Client.RemoteEndPoint as IPEndPoint).Address;
                return null;
            }
            catch { return null; }
        }

        private void SendConnectAck() => SendPacket(new PROTOCOL_BASE_CONNECT_ACK(this));

        public void SendCompletePacket(byte[] Data, string PacketName)
        {
            try
            {
                if (Data.Length < 4) return;
                byte[] Result = new byte[Data.Length + 1];
                Array.Copy(Data, 0, Result, 0, Data.Length);

                if (ConfigLoader.DebugMode)
                {
                    ushort Opcode = BitConverter.ToUInt16(Result, 2);
                    CLogger.Print($"{PacketName}; Address: {Client.RemoteEndPoint}; Opcode: [{Opcode}]", LoggerType.Debug);
                }

                Client.BeginSend(Result, 0, Result.Length, SocketFlags.None,
                    new AsyncCallback(SendCallback), Client);
            }
            catch { Close(0, true); }
        }

        public void SendPacket(byte[] originalData, string PacketName)
        {
            try
            {
                if (originalData.Length < 2) return;

                byte[] encryptedData = Bitwise.Encrypt(originalData, CMessEncryptKey, SessionMode);
                if (encryptedData == null) return;

                byte[] packetSize = BitConverter.GetBytes((ushort)(encryptedData.Length + 2));
                byte[] packetData = new byte[encryptedData.Length + 2 + 1];
                Array.Copy(packetSize, 0, packetData, 0, 2);
                Array.Copy(encryptedData, 0, packetData, 2, encryptedData.Length);

                if (ConfigLoader.DebugMode)
                {
                    ushort Opcode = BitConverter.ToUInt16(originalData, 0);
                    CLogger.Print($"[AUTH] {PacketName}; Address: {Client.RemoteEndPoint}; Opcode: [{Opcode}]", LoggerType.Debug);
                }

                if (Client != null && Client.Connected)
                    Client.BeginSend(packetData, 0, packetData.Length, SocketFlags.None,
                        new AsyncCallback(SendCallback), Client);
            }
            catch { Close(0, true); }
        }

        public void SendPacket(AuthServerPacket Packet)
        {
            try
            {
                byte[] originalData = Packet.GetBytes("AuthClient.SendPacket");
                if (originalData.Length < 2) { Packet.Dispose(); return; }

                if (ConfigLoader.DebugMode)
                {
                    ushort Opcode = BitConverter.ToUInt16(originalData, 0);
                    CLogger.Print($"[AUTH] {Packet.GetType().Name}; Address: {Client.RemoteEndPoint}; Opcode: [{Opcode}]", LoggerType.Debug);
                }

                byte[] sendData;

                if (!IsFirstPacketSent)
                {
                    IsFirstPacketSent = true;
                    byte[] sz = BitConverter.GetBytes((ushort)(originalData.Length + 2));
                    sendData = new byte[originalData.Length + 2 + 5];
                    Array.Copy(sz, 0, sendData, 0, 2);
                    Array.Copy(originalData, 0, sendData, 2, originalData.Length);
                }
                else
                {
                    byte[] sz = BitConverter.GetBytes((ushort)(originalData.Length + 2));
                    sendData = new byte[originalData.Length + 2 + 1];
                    Array.Copy(sz, 0, sendData, 0, 2);
                    Array.Copy(originalData, 0, sendData, 2, originalData.Length);
                }

                if (Client != null && Client.Connected)
                    Client.BeginSend(sendData, 0, sendData.Length, SocketFlags.None,
                        new AsyncCallback(SendCallback), Client);

                Packet.Dispose();
            }
            catch (SocketException) { try { Packet?.Dispose(); } catch { } }
            catch (Exception ex)
            {
                try { Packet?.Dispose(); } catch { }
                CLogger.Print($"SendPacket error: {ex.Message}", LoggerType.Error, ex);
                Close(0, true);
            }
        }

        private void SendCallback(IAsyncResult Result)
        {
            try
            {
                Socket Handler = Result.AsyncState as Socket;
                if (Handler != null && Handler.Connected)
                    Handler.EndSend(Result);
            }
            catch { Close(0, true); }
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
                Client.BeginReceive(State.Buffer, 0, StateObject.BufferSize, SocketFlags.None,
                    new AsyncCallback(OnReceiveCallback), State);
            }
            catch { Close(0, true); }
        }

        public void Close(int TimeMS, bool DestroyConnection)
        {
            if (connectionClosed) return;
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
                            SendRefresh.RefreshAccount(Cache, false);
                        Cache.Status.ResetData(Cache.PlayerId);
                        Cache.SimpleClear();
                        Cache.UpdateCacheInfo();
                        Player = null;
                    }
                    if (Client != null) Client.Close(TimeMS);
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
            catch (Exception ex) { CLogger.Print($"AuthClient.Close: {ex.Message}", LoggerType.Error, ex); }
        }

        private void OnReceiveCallback(IAsyncResult Result)
        {
            StateObject Packet = Result.AsyncState as StateObject;
            try
            {
                int BytesCount = Packet.WorkSocket.EndReceive(Result);
                if (BytesCount > 0)
                {
                    byte[] EncryptedPacket = new byte[BytesCount];
                    Array.Copy(Packet.Buffer, 0, EncryptedPacket, 0, BytesCount);

                    CLogger.Print(Bitwise.ToHexData($"RAW RECV [{BytesCount} bytes]", EncryptedPacket), LoggerType.Debug);

                    int PacketLength = BitConverter.ToUInt16(EncryptedPacket, 0) & 0x7FFF;
                    byte[] BufferPacket = new byte[PacketLength];
                    Array.Copy(EncryptedPacket, 2, BufferPacket, 0, BufferPacket.Length);

                  
                    byte[] DecryptedPacket = Bitwise.Decrypt(BufferPacket, CMessDecryptKey, SessionMode);

                    if (DecryptedPacket == null)
                    {
                        string keyHex = BitConverter.ToString(CMessDecryptKey).Replace("-", "");
                        string pktHex = BitConverter.ToString(BufferPacket, 0, Math.Min(BufferPacket.Length, 16)).Replace("-", "");
                        CLogger.Print($"OnReceive: Decrypt failed | IP: {GetIPAddress()} | Mode: {SessionMode} | Key: {keyHex} | Pkt[0..16]: {pktHex}", LoggerType.Warning);
                        Close(0, true);
                        return;
                    }

                    ushort PacketId = BitConverter.ToUInt16(DecryptedPacket, 0);
                    ushort PacketSeed = BitConverter.ToUInt16(DecryptedPacket, 2);
                    FirstPacketCheck(PacketId);

                    if (!CheckSeed(PacketSeed, true))
                    {
                        Close(0, true);
                    }
                    else
                    {
                        if (connectionClosed) return;
                        RunPacket(PacketId, DecryptedPacket, "REQ");
                        CheckOut(EncryptedPacket, PacketLength);
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            try { StartReceive(); }
                            catch (Exception ex)
                            {
                                CLogger.Print($"Thread pool error IP: {GetIPAddress()}: {ex.Message}", LoggerType.Error, ex);
                                Close(0, true);
                            }
                        });
                    }
                }
            }
            catch { Close(0, true); }
        }

        public void CheckOut(byte[] BufferTotal, int FirstLength)
        {
            try
            {
                byte[] EncryptedPacket = new byte[BufferTotal.Length - FirstLength - 3];
                Array.Copy(BufferTotal, FirstLength + 3, EncryptedPacket, 0, EncryptedPacket.Length);
                if (EncryptedPacket.Length == 0) return;

                int PacketLength = BitConverter.ToUInt16(EncryptedPacket, 0) & 0x7FFF;
                byte[] BufferPacket = new byte[PacketLength];
                Array.Copy(EncryptedPacket, 2, BufferPacket, 0, BufferPacket.Length);

                byte[] DecryptedPacket = Bitwise.Decrypt(BufferPacket, CMessDecryptKey, SessionMode);

                if (DecryptedPacket == null)
                {
                    CLogger.Print($"CheckOut: Decrypt failed for IP: {GetIPAddress()}", LoggerType.Warning);
                    Close(0, true);
                    return;
                }

                ushort PacketId = BitConverter.ToUInt16(DecryptedPacket, 0);
                ushort PacketSeed = BitConverter.ToUInt16(DecryptedPacket, 2);

                if (!CheckSeed(PacketSeed, false)) { Close(0, true); return; }

                RunPacket(PacketId, DecryptedPacket, "REQ");
                CheckOut(EncryptedPacket, PacketLength);
            }
            catch { Close(0, true); }
        }

        private void FirstPacketCheck(ushort PacketId)
        {
            if (PacketId != 0) { this.FirstPacketId = PacketId; return; }
            if (PacketId != 1281 && PacketId != 2309)
            {
                CLogger.Print($"Unknown first packet. Opcode: {PacketId}; IP: {GetIPAddress()}", LoggerType.Warning);
                Close(0, true);
            }
            else { this.FirstPacketId = PacketId; }
        }

        //public bool CheckSeed(ushort PacketSeed, bool IsTheFirstPacket)
        //{
        //    ushort expected = GetNextSessionSeed();
        //    if (PacketSeed == expected) return true;

        //    // DEBUG: log mismatch tapi JANGAN close — bypass sementara untuk debug
        //    CLogger.Print($"[SEED-DEBUG] Got: {PacketSeed} / Expected: {expected} / Primary: {SessionSeed} / Mode: {SessionMode}", LoggerType.Warning);

        //    // TODO: kembalikan ke 'return false' setelah seed logic dikonfirmasi
        //    return true;  // BYPASS SEMENTARA
        //}

        public bool CheckSeed(ushort PacketSeed, bool IsTheFirstPacket)
        {
            ushort expected = GetNextSessionSeed();
            if (PacketSeed == expected) return true;

            CLogger.Print($"[SEED] Mismatch Got:{PacketSeed} / Expected:{expected} / Mode:{SessionMode}", LoggerType.Warning);
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
                    case 1057: packet = new PROTOCOL_AUTH_GET_POINT_CASH_REQ(); break;
                    case 1281: packet = new PROTOCOL_BASE_LOGIN_REQ(); break;
                    case 2307: packet = new PROTOCOL_BASE_LOGOUT_REQ(); break;
                    case 2309: packet = new PROTOCOL_BASE_KEEP_ALIVE_REQ(); break;
                    case 2311: packet = new PROTOCOL_BASE_GAMEGUARD_REQ(); break;
                    case 2314: packet = new PROTOCOL_BASE_GET_SYSTEM_INFO_REQ(); break;
                    case 2316: packet = new PROTOCOL_BASE_GET_USER_INFO_REQ(); break;
                    case 2318: packet = new PROTOCOL_BASE_GET_INVEN_INFO_REQ(); break;
                    case 2320: packet = new PROTOCOL_BASE_GET_OPTION_REQ(); break;
                    case 2322: packet = new PROTOCOL_BASE_OPTION_SAVE_REQ(); break;
                    case 2328: packet = new PROTOCOL_BASE_USER_LEAVE_REQ(); break;
                    case 2332: packet = new PROTOCOL_BASE_GET_CHANNELLIST_REQ(); break;
                    case 2399: packet = new PROTOCOL_BASE_GAME_SERVER_STATE_REQ(); break;
                    case 2414: packet = new PROTOCOL_BASE_DAILY_RECORD_REQ(); break;
                    case 2459: packet = new PROTOCOL_BASE_GET_MAP_INFO_REQ(); break;
                    case 2489: packet = new PROTOCOL_BASE_CHANNELTYPE_CHANGE_CONDITION_REQ(); break;
                    case 2516: packet = new PROTOCOL_BASE_MISSION_CARD_INFO_STREAM_REQ(); break;
                    case 7681: packet = new PROTOCOL_MATCH_SERVER_IDX_REQ(); break;
                    case 7699: packet = new PROTOCOL_MATCH_CLAN_SEASON_REQ(); break;
                    case 2518: packet = new PROTOCOL_BASE_TICK_REQ(); break;
                    case 8709: break; 
                    default:
                        CLogger.Print(Bitwise.ToHexData($"Opcode Not Found: [{Opcode}] | {Value}", Buffer), LoggerType.Opcode);
                        break;
                }

                if (packet != null)
                {
                    using (packet)
                    {
                        if (ConfigLoader.DebugMode)
                            CLogger.Print($"[AUTH] {packet.GetType().Name}; Address: {Client.RemoteEndPoint}; Opcode: [{Opcode}]", LoggerType.Debug);
                        packet.Makeme(this, Buffer);
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            try { packet.Run(); }
                            catch (Exception ex) { CLogger.Print(ex.Message, LoggerType.Error, ex); Close(50, true); }
                        });
                        packet.Dispose();
                        Buffer = null;
                    }
                }
            }
            catch (Exception ex) { CLogger.Print(ex.Message, LoggerType.Error, ex); }
        }
    }
}
