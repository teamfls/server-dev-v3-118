using Microsoft.Win32.SafeHandles;
using Network.ClientPacket;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Data.Utils;
using Server.Game.Network;
using Server.Game.Network.ClientPacket;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;

namespace Server.Game
{
    public class GameClient : IDisposable
    {
        public int ServerId;
        public long PlayerId;
        public Socket Client;
        public Account Player;
        public DateTime SessionDate;
        public int SessionId;
        public ushort SessionSeed;
        private ushort NextSessionSeed;

        // v108: CMessEncryptor keys replace SessionShift
        public int FirstPacketId;
        public int SessionMode;
        public byte[] SessionKey1 = new byte[8];
        public byte[] SessionKey2 = new byte[8];

        /// <summary>Combines SessionKey1 + SessionKey2 into the 16-byte CMess key expected by Bitwise.</summary>
        private byte[] GetSessionKey16()
        {
            byte[] key = new byte[16];
            Array.Copy(SessionKey1, 0, key, 0, 8);
            Array.Copy(SessionKey2, 0, key, 8, 8);
            return key;
        }

        private bool Disposed = false, connectionClosed = false;
        private readonly SafeHandle Handle = new SafeFileHandle(IntPtr.Zero, true);

        public GameClient(int ServerId, Socket Client)
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
                    CLogger.Print($"GameClient.GetAccount: Account is null for IP: {GetIPAddress()}", LoggerType.Warning);
                    localPlayer = null;
                }
                return localPlayer;
            }
            catch (Exception Ex)
            {
                CLogger.Print($"GameClient.GetAccount Exception; {Ex.Message}", LoggerType.Error, Ex);
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
                if (Disposed) return;
                Player = null;
                if (Client != null)
                {
                    Client.Dispose();
                    Client = null;
                }
                PlayerId = 0;
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

                // v108: CMessEncryptor key initialization
                SessionMode = ((SessionId + Bitwise.CRYPTO[0]) % 3);

                byte[] seed1 = BitConverter.GetBytes(SessionId ^ Bitwise.CRYPTO[1]);
                byte[] seed2 = BitConverter.GetBytes(SessionId ^ Bitwise.CRYPTO[2]);

                Array.Copy(seed1, 0, SessionKey1, 0, Math.Min(seed1.Length, 8));
                Array.Copy(seed2, 0, SessionKey2, 0, Math.Min(seed2.Length, 8));

                for (int i = seed1.Length; i < 8; i++)
                    SessionKey1[i] = (byte)(SessionId >> (i % 4) & 0xFF);
                for (int i = seed2.Length; i < 8; i++)
                    SessionKey2[i] = (byte)(SessionSeed >> (i % 4) & 0xFF);

                ThreadPool.QueueUserWorkItem(state => Connect());
                ThreadPool.QueueUserWorkItem(state => ReadPacket());
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
            if (Client == null || FirstPacketId != 0)
                return;

            CLogger.Print("Connection destroyed due to no responses. IPAddress: " + GetIPAddress(), LoggerType.Warning);
            Close(0, true);
        }

        public string GetIPAddress()
        {
            try
            {
                if (Client != null && Client.RemoteEndPoint != null)
                    return (Client.RemoteEndPoint as IPEndPoint).Address.ToString();
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
                    return (Client.RemoteEndPoint as IPEndPoint).Address;
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void Connect() => SendPacket(new PROTOCOL_BASE_CONNECT_ACK(this));

        public void SendCompletePacket(byte[] Data, string PacketName)
        {
            try
            {
                if (Data.Length < 4) return;

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
                    // v108: use CMessEncryptor
                    byte[] FinalResult = Logical.Length == 3
                        ? Bitwise.Encrypt(Result, GetSessionKey16(), SessionMode)
                        : Result;

                    if (FinalResult == null)
                    {
                        CLogger.Print($"SendCompletePacket: Encrypt returned null for {PacketName}", LoggerType.Warning);
                        Close(0, true);
                        return;
                    }

                    Client.BeginSend(FinalResult, 0, FinalResult.Length, SocketFlags.None,
                        new AsyncCallback(SendCallback), Client);
                }
                Result = null;
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
                if (originalData.Length < 2) return;

                byte[] packetSize = BitConverter.GetBytes((ushort)(originalData.Length + 2));
                byte[] newData = new byte[originalData.Length + packetSize.Length];

                Array.Copy(packetSize, 0, newData, 0, packetSize.Length);
                Array.Copy(originalData, 0, newData, 2, originalData.Length);

                byte[] packetData = new byte[newData.Length + 5]; // 5 bytes to skip encryption
                Array.Copy(newData, 0, packetData, 0, newData.Length);

                if (ConfigLoader.DebugMode)
                {
                    ushort Opcode = BitConverter.ToUInt16(packetData, 2);
                    string DebugData = Bitwise.ToByteString(packetData);
                    CLogger.Print($"{PacketName}; Address: {Client.RemoteEndPoint}; Opcode: [{Opcode}]", LoggerType.Debug);
                }

                if (Client != null && packetData.Length > 0)
                {
                    Client.BeginSend(packetData, 0, packetData.Length, SocketFlags.None,
                        new AsyncCallback(SendCallback), Client);
                }
                packetData = null;
            }
            catch
            {
                Close(0, true);
            }
        }

        public void SendPacket(GameServerPacket Packet)
        {
            try
            {
                using (Packet)
                {
                    byte[] originalData = Packet.GetBytes("GameClient.SendPacket");
                    if (originalData.Length < 2) return;

                    byte[] packetSize = BitConverter.GetBytes((ushort)(originalData.Length + 2));
                    byte[] newData = new byte[originalData.Length + packetSize.Length];

                    Array.Copy(packetSize, 0, newData, 0, packetSize.Length);
                    Array.Copy(originalData, 0, newData, 2, originalData.Length);

                    byte[] packetData = new byte[newData.Length + 5]; // 5 bytes to skip encryption
                    Array.Copy(newData, 0, packetData, 0, newData.Length);

                    if (ConfigLoader.DebugMode)
                    {
                        ushort Opcode = BitConverter.ToUInt16(packetData, 2);
                        string DebugData = Bitwise.ToByteString(packetData);
                        CLogger.Print($"[GAME] {Packet.GetType().Name}; Address: {Client.RemoteEndPoint}; Opcode: [{Opcode}]", LoggerType.Debug);
                    }

                    if (Client != null && packetData.Length > 0)
                    {
                        Client.BeginSend(packetData, 0, packetData.Length, SocketFlags.None,
                            new AsyncCallback(SendCallback), Client);
                    }
                    Packet.Dispose();
                    packetData = null;
                }
            }
            catch
            {
                Close(0, true);
            }
        }

        private void SendCallback(IAsyncResult Result)
        {
            try
            {
                // Fix CS8370: C# 7.3 compatible pattern
                Socket Handler = Result.AsyncState as Socket;
                if (Handler != null && Handler.Connected)
                {
                    Handler.EndSend(Result);
                }
            }
            catch
            {
                Close(0, true);
            }
        }

        private void ReadPacket()
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
            catch
            {
                Close(0, true);
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

                    // v108: use CMessDecryptor
                    byte[] DecryptedPacket = Bitwise.Decrypt(BufferPacket, GetSessionKey16(), SessionMode);

                    if (DecryptedPacket == null)
                    {
                        CLogger.Print($"OnReceive: Decrypt failed (invalid padding) for IP: {GetIPAddress()}", LoggerType.Warning);
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

                        ProcessPacket(PacketId, DecryptedPacket, "REQ");
                        Checkout(EcryptedPacket, PacketLength);

                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            try
                            {
                                ReadPacket();
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

        public void Checkout(byte[] BufferTotal, int FirstLength)
        {
            int BufferLength = BufferTotal.Length;
            try
            {
                byte[] EcryptedPacket = new byte[BufferLength - FirstLength - 3];
                Array.Copy(BufferTotal, FirstLength + 3, EcryptedPacket, 0, EcryptedPacket.Length);

                if (EcryptedPacket.Length == 0) return;

                int PacketLength = BitConverter.ToUInt16(EcryptedPacket, 0) & 0x7FFF;
                byte[] BufferPacket = new byte[PacketLength];
                Array.Copy(EcryptedPacket, 2, BufferPacket, 0, BufferPacket.Length);

                // v108: use CMessDecryptor
                byte[] DecryptedPacket = Bitwise.Decrypt(BufferPacket, GetSessionKey16(), SessionMode);

                if (DecryptedPacket == null)
                {
                    CLogger.Print($"Checkout: Decrypt failed (invalid padding) for IP: {GetIPAddress()}", LoggerType.Warning);
                    Close(0, true);
                    return;
                }

                ushort PacketId = BitConverter.ToUInt16(DecryptedPacket, 0);
                ushort PacketSeed = BitConverter.ToUInt16(DecryptedPacket, 2);

                if (!CheckSeed(PacketSeed, false))
                {
                    Close(0, true);
                    return;
                }

                ProcessPacket(PacketId, DecryptedPacket, "REQ");
                Checkout(EcryptedPacket, PacketLength);
            }
            catch
            {
                Close(0, true);
            }
        }

        public void Close(int TimeMS, bool DestroyConnection, bool Kicked = false)
        {
            if (connectionClosed) return;
            try
            {
                connectionClosed = true;
                GameXender.Client.RemoveSession(this);
                Account Cache = Player;
                if (DestroyConnection)
                {
                    if (PlayerId > 0 && Cache != null)
                    {
                        Cache.SetOnlineStatus(false);
                        RoomModel Room = Cache.Room;
                        if (Room != null)
                            Room.RemovePlayer(Cache, false, Kicked ? 1 : 0);

                        MatchModel Match = Cache.Match;
                        if (Match != null)
                            Match.RemovePlayer(Cache);

                        ChannelModel Channel = Cache.GetChannel();
                        if (Channel != null)
                            Channel.RemovePlayer(Cache);

                        Cache.Status.ResetData(PlayerId);
                        AllUtils.SyncPlayerToFriends(Cache, false);
                        AllUtils.SyncPlayerToClanMembers(Cache);
                        Cache.SimpleClear();
                        Cache.UpdateCacheInfo();
                        Player = null;
                    }
                    PlayerId = 0;
                    if (Client != null)
                        Client.Close(TimeMS);
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
                CLogger.Print($"GameClient.Close: {ex.Message}", LoggerType.Error, ex);
            }
        }

        private void FirstPacketCheck(ushort PacketId)
        {
            if (PacketId != 0)
            {
                this.FirstPacketId = PacketId;
                return;
            }
            if (PacketId != 2330 && PacketId != 2309)
            {
                CLogger.Print($"Conexión destruida debido a un primer paquete desconocido. Código de operación: {PacketId}; IPAddress: {GetIPAddress()}", LoggerType.Hack);
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
                return true;

            CLogger.Print($"Connection blocked. IP: {GetIPAddress()}; Date: {DateTimeUtil.Now()}; SessionId: {SessionId}; PacketSeed: {PacketSeed} / NextSessionSeed: {NextSessionSeed}; PrimarySeed: {SessionSeed}", LoggerType.Hack);

            if (IsTheFirstPacket)
                ThreadPool.QueueUserWorkItem(state => ReadPacket());

            return false;
        }

        private ushort GetNextSessionSeed()
        {
            NextSessionSeed = (ushort)((((NextSessionSeed * 214013) + 2531011) >> 16) & short.MaxValue);
            return NextSessionSeed;
        }

        private void ProcessPacket(ushort opcode, byte[] packetData, string direction)
        {
            try
            {
                GameClientPacket packet = null;

                switch (opcode)
                {
                    // Clan System packets
                    case 769: packet = new PROTOCOL_CS_CLIENT_ENTER_REQ(); break;
                    case 771: packet = new PROTOCOL_CS_CLIENT_LEAVE_REQ(); break;
                    case 800: packet = new PROTOCOL_CS_DETAIL_INFO_REQ(); break;
                    case 802: packet = new PROTOCOL_CS_MEMBER_CONTEXT_REQ(); break;
                    case 804: packet = new PROTOCOL_CS_MEMBER_LIST_REQ(); break;
                    case 806: packet = new PROTOCOL_CS_CREATE_CLAN_REQ(); break;
                    case 808: packet = new PROTOCOL_CS_CLOSE_CLAN_REQ(); break;
                    case 810: packet = new PROTOCOL_CS_CHECK_JOIN_AUTHORITY_ERQ(); break;
                    case 812: packet = new PROTOCOL_CS_JOIN_REQUEST_REQ(); break;
                    case 814: packet = new PROTOCOL_CS_CANCEL_REQUEST_REQ(); break;
                    case 816: packet = new PROTOCOL_CS_REQUEST_CONTEXT_REQ(); break;
                    case 818: packet = new PROTOCOL_CS_REQUEST_LIST_REQ(); break;
                    case 820: packet = new PROTOCOL_CS_REQUEST_INFO_REQ(); break;
                    case 822: packet = new PROTOCOL_CS_ACCEPT_REQUEST_REQ(); break;
                    case 825: packet = new PROTOCOL_CS_DENIAL_REQUEST_REQ(); break;
                    case 828: packet = new PROTOCOL_CS_SECESSION_CLAN_REQ(); break;
                    case 830: packet = new PROTOCOL_CS_DEPORTATION_REQ(); break;
                    case 833: packet = new PROTOCOL_CS_COMMISSION_MASTER_REQ(); break;
                    case 836: packet = new PROTOCOL_CS_COMMISSION_STAFF_REQ(); break;
                    case 839: packet = new PROTOCOL_CS_COMMISSION_REGULAR_REQ(); break;
                    case 854: packet = new PROTOCOL_CS_CHATTING_REQ(); break;
                    case 856: packet = new PROTOCOL_CS_CHECK_MARK_REQ(); break;
                    case 858: packet = new PROTOCOL_CS_REPLACE_NOTICE_REQ(); break;
                    case 860: packet = new PROTOCOL_CS_REPLACE_INTRO_REQ(); break;
                    case 868: packet = new PROTOCOL_CS_REPLACE_MANAGEMENT_REQ(); break;
                    case 877: packet = new PROTOCOL_CS_ROOM_INVITED_REQ(); break;
                    case 886: packet = new PROTOCOL_CS_PAGE_CHATTING_REQ(); break;
                    case 888: packet = new PROTOCOL_CS_INVITE_REQ(); break;
                    case 890: packet = new PROTOCOL_CS_INVITE_ACCEPT_REQ(); break;
                    case 892: packet = new PROTOCOL_CS_NOTE_REQ(); break;
                    case 914: packet = new PROTOCOL_CS_CREATE_CLAN_CONDITION_REQ(); break;
                    case 916: packet = new PROTOCOL_CS_CHECK_DUPLICATE_REQ(); break;
                    case 997: packet = new PROTOCOL_CS_CLAN_LIST_FILTER_REQ(); break;
                    case 999: packet = new PROTOCOL_CS_CLAN_LIST_DETAIL_INFO_REQ(); break;

                    // Shop System
                    case 1025: packet = new PROTOCOL_SHOP_ENTER_REQ(); break;
                    case 1027: packet = new PROTOCOL_SHOP_LEAVE_REQ(); break;
                    case 1029: packet = new PROTOCOL_SHOP_GET_SAILLIST_REQ(); break;
                    case 1041: packet = new PROTOCOL_AUTH_SHOP_GET_GIFTLIST_REQ(); break;
                    case 1043: packet = new PROTOCOL_AUTH_SHOP_GOODS_BUY_REQ(); break;
                    case 1045: packet = new PROTOCOL_AUTH_SHOP_GOODS_GIFT_REQ(); break;
                    case 1047: packet = new PROTOCOL_AUTH_SHOP_ITEM_AUTH_REQ(); break;
                    case 1049: packet = new PROTOCOL_INVENTORY_USE_ITEM_REQ(); break;
                    case 1053: packet = new PROTOCOL_AUTH_SHOP_AUTH_GIFT_REQ(); break;
                    case 1055: packet = new PROTOCOL_AUTH_SHOP_DELETE_ITEM_REQ(); break;
                    case 1057: packet = new PROTOCOL_AUTH_GET_POINT_CASH_REQ(); break;
                    case 1061: packet = new PROTOCOL_AUTH_USE_ITEM_CHECK_NICK_REQ(); break;
                    case 1075: packet = new PROTOCOL_AUTH_SHOP_EXTEND_REQ(); break;
                    case 1076: packet = new PROTOCOL_SHOP_REPAIR_REQ(); break;
                    case 1084: packet = new PROTOCOL_AUTH_SHOP_USE_GIFTCOUPON_REQ(); break;
                    case 1087: packet = new PROTOCOL_AUTH_SHOP_ITEM_CHANGE_DATA_REQ(); break;
                    case 1097: packet = new PROTOCOL_SHOP_LIMITED_SALE_SYNC_REQ(); break;
                    case 1112: packet = new PROTOCOL_AUTH_SHOP_NAME_CARD_NICK_OUTLINE_COLOR_REQ(); break;

                    // Friend System
                    case 1811: packet = new PROTOCOL_AUTH_FRIEND_INVITED_REQ(); break;
                    case 1816: packet = new PROTOCOL_AUTH_FRIEND_ACCEPT_REQ(); break;
                    case 1818: packet = new PROTOCOL_AUTH_FRIEND_INSERT_REQ(); break;
                    case 1820: packet = new PROTOCOL_AUTH_FRIEND_DELETE_REQ(); break;
                    case 1826: packet = new PROTOCOL_AUTH_RECV_WHISPER_REQ(); break;
                    case 1828: packet = new PROTOCOL_AUTH_SEND_WHISPER_REQ(); break;

                    // Messenger System
                    case 1921: packet = new PROTOCOL_MESSENGER_NOTE_SEND_REQ(); break;
                    case 1926: packet = new PROTOCOL_MESSENGER_NOTE_CHECK_READED_REQ(); break;
                    case 1928: packet = new PROTOCOL_MESSENGER_NOTE_DELETE_REQ(); break;
                    case 1930: packet = new PROTOCOL_MESSENGER_NOTE_RECEIVE_REQ(); break;

                    // Base System
                    case 2307: packet = new PROTOCOL_BASE_LOGOUT_REQ(); break;
                    case 2309: packet = new PROTOCOL_BASE_KEEP_ALIVE_REQ(); break;
                    case 2312: packet = new PROTOCOL_BASE_GAMEGUARD_REQ(); break;
                    case 2322: packet = new PROTOCOL_BASE_OPTION_SAVE_REQ(); break;
                    case 2326: packet = new PROTOCOL_BASE_CREATE_NICK_REQ(); break;
                    case 2328: packet = new PROTOCOL_BASE_USER_LEAVE_REQ(); break;
                    case 2330: packet = new PROTOCOL_BASE_USER_ENTER_REQ(); break;
                    case 2332: packet = new PROTOCOL_BASE_GET_CHANNELLIST_REQ(); break;
                    case 2334: packet = new PROTOCOL_BASE_SELECT_CHANNEL_REQ(); break;
                    case 2336: packet = new PROTOCOL_BASE_ATTENDANCE_REQ(); break;
                    case 2338: packet = new PROTOCOL_BASE_ATTENDANCE_CLEAR_ITEM_REQ(); break;
                    case 2350: packet = new PROTOCOL_BASE_GET_RECORD_INFO_DB_REQ(); break;
                    case 2360: packet = new PROTOCOL_BASE_QUEST_ACTIVE_IDX_CHANGE_REQ(); break;
                    case 2364: packet = new PROTOCOL_BASE_QUEST_BUY_CARD_SET_REQ(); break;
                    case 2366: packet = new PROTOCOL_BASE_QUEST_DELETE_CARD_SET_REQ(); break;
                    case 2376: packet = new PROTOCOL_BASE_USER_TITLE_CHANGE_REQ(); break;
                    case 2378: packet = new PROTOCOL_BASE_USER_TITLE_EQUIP_REQ(); break;
                    case 2380: packet = new PROTOCOL_BASE_USER_TITLE_RELEASE_REQ(); break;
                    case 2384: packet = new PROTOCOL_BASE_CHATTING_REQ(); break;
                    case 2392: packet = new PROTOCOL_2392_UNKNOWN_PACKET_REQ(); break;
                    case 2399: packet = new PROTOCOL_BASE_GAME_SERVER_STATE_REQ(); break;
                    case 2401: packet = new PROTOCOL_BASE_ENTER_PASS_REQ(); break;
                    case 2414: packet = new PROTOCOL_BASE_DAILY_RECORD_REQ(); break;
                    case 2422: packet = new PROTOCOL_BASE_GET_USER_DETAIL_INFO_REQ(); break;
                    case 2424: packet = new PROTOCOL_BASE_GET_ROOM_USER_DETAIL_INFO_REQ(); break;
                    case 2425: packet = new PROTOCOL_BASE_GET_USER_BASIC_INFO_REQ(); break;
                    case 2426: packet = new PROTOCOL_AUTH_FIND_USER_REQ(); break;
                    case 2447: packet = new PROTOCOL_BASE_GET_USER_SUBTASK_REQ(); break;
                    case 2465: packet = new PROTOCOL_BASE_URL_LIST_REQ(); break;
                    case 2489: packet = new PROTOCOL_BASE_CHANNELTYPE_CHANGE_CONDITION_REQ(); break;
                    case 2491: packet = new PROTOCOL_LOBBY_NEW_MYINFO_REQ(); break;
                    case 2498: packet = new PROTOCOL_BASE_RANDOMBOX_LIST_REQ(); break;
                    case 2508: packet = new PROTOCOL_BASE_TICKET_UPDATE_REQ(); break;
                    case 2510: packet = new PROTOCOL_BASE_EVENT_PORTAL_REQ(); break;
                    case 2518: packet = new PROTOCOL_2518_UNKNOWN_PACKET_REQ(); break;

                    // Lobby System
                    case 2561: packet = new PROTOCOL_LOBBY_LEAVE_REQ(); break;
                    case 2567: packet = new PROTOCOL_LOBBY_GET_ROOMINFOADD_REQ(); break;
                    case 2583: packet = new PROTOCOL_LOBBY_ENTER_REQ(); break;
                    case 2587: packet = new PROTOCOL_LOBBY_GET_ROOMLIST_REQ(); break;
                    case 3083: packet = new PROTOCOL_LOBBY_GET_ROOMINFOADD_REQ(); break;
                    case 3329: packet = new PROTOCOL_INVENTORY_ENTER_REQ(); break;
                    case 3331: packet = new PROTOCOL_INVENTORY_LEAVE_REQ(); break;
                    case 3333: packet = new PROTOCOL_INVENTORY_UPGRADE_REQ(); break;

                    // Room System
                    case 3585: packet = new PROTOCOL_ROOM_JOIN_REQ(); break;
                    case 3592: packet = new PROTOCOL_ROOM_CREATE_REQ(); break;
                    case 3596: packet = new PROTOCOL_ROOM_GET_PLAYERINFO_REQ(); break;
                    case 3602: packet = new PROTOCOL_ROOM_CHANGE_PASSWD_REQ(); break;
                    case 3604: packet = new PROTOCOL_ROOM_CHANGE_SLOT_REQ(); break;
                    case 3609: packet = new PROTOCOL_ROOM_PERSONAL_TEAM_CHANGE_REQ(); break;
                    case 3611: packet = new PROTOCOL_ROOM_REQUEST_MAIN_REQ(); break;
                    case 3613: packet = new RandomHostChangePacket(); break;
                    case 3615: packet = new PROTOCOL_ROOM_REQUEST_MAIN_CHANGE_WHO_REQ(); break;
                    case 3617: packet = new CheckRandomHostPacket(); break;
                    case 3619: packet = new PROTOCOL_ROOM_TOTAL_TEAM_CHANGE_REQ(); break;
                    case 3631: packet = new PROTOCOL_ROOM_GET_LOBBY_USER_LIST_REQ(); break;
                    case 3635: packet = new PROTOCOL_BASE_UNKNOWN_3635_REQ(); break;
                    case 3639: packet = new PROTOCOL_ROOM_CHANGE_ROOMINFO_REQ(); break;
                    case 3643: packet = new PROTOCOL_GM_KICK_COMMAND_REQ(); break;
                    case 3647: packet = new PROTOCOL_GM_EXIT_COMMAND_REQ(); break;
                    case 3657: packet = new PROTOCOL_ROOM_LOADING_START_REQ(); break;
                    case 3665: packet = new PROTOCOL_ROOM_GET_USER_EQUIPMENT_REQ(); break;
                    case 3671: packet = new PROTOCOL_ROOM_INFO_ENTER_REQ(); break;
                    case 3673: packet = new PROTOCOL_ROOM_INFO_LEAVE_REQ(); break;
                    case 3675: packet = new PROTOCOL_ROOM_INVITE_LOBBY_USER_LIST_REQ(); break;
                    case 3677: packet = new PROTOCOL_ROOM_CHANGE_COSTUME_REQ(); break;
                    case 3679: packet = new PROTOCOL_ROOM_SELECT_SLOT_CHANGE_REQ(); break;
                    case 3680: packet = new PROTOCOL_ROOM_GET_ACEMODE_PLAYERINFO_REQ(); break;
                    case 3682: packet = new PROTOCOL_ROOM_SELECT_SLOT_CHANGE_REQ(); break;
                    case 3686: packet = new PROTOCOL_ROOM_CHANGE_OBSERVER_SLOT_REQ(); break;
                    case 3850: packet = new PROTOCOL_COMMUNITY_USER_REPORT_REQ(); break;
                    case 3852: packet = new PROTOCOL_COMMUNITY_USER_REPORT_CONDITION_CHECK_REQ(); break;

                    // Battle System
                    case 5123: packet = new PROTOCOL_BATTLE_READYBATTLE_REQ(); break;
                    case 5129: packet = new PROTOCOL_BATTLE_PRESTARTBATTLE_REQ(); break;
                    case 5131: packet = new PROTOCOL_BATTLE_STARTBATTLE_REQ(); break;
                    case 5133: packet = new PROTOCOL_BATTLE_GIVEUPBATTLE_REQ(); break;
                    case 5135: packet = new PROTOCOL_BATTLE_DEATH_REQ(); break;
                    case 5137: packet = new PROTOCOL_BATTLE_RESPAWN_REQ(); break;
                    case 5145: break; // no-op
                    case 5146: packet = new PROTOCOL_BATTLE_SENDPING_REQ(); break;
                    case 5156: packet = new PROTOCOL_BATTLE_MISSION_BOMB_INSTALL_REQ(); break;
                    case 5158: packet = new PROTOCOL_BATTLE_MISSION_BOMB_UNINSTALL_REQ(); break;
                    case 5166: packet = new PROTOCOL_BATTLE_MISSION_GENERATOR_INFO_REQ(); break;
                    case 5168: packet = new PROTOCOL_BATTLE_TIMERSYNC_REQ(); break;
                    case 5172: packet = new PROTOCOL_BATTLE_CHANGE_DIFFICULTY_LEVEL_REQ(); break;
                    case 5174: packet = new PROTOCOL_BATTLE_RESPAWN_FOR_AI_REQ(); break;
                    case 5180: packet = new PROTOCOL_BATTLE_MISSION_DEFENCE_INFO_REQ(); break;
                    case 5182: packet = new PROTOCOL_BATTLE_MISSION_TOUCHDOWN_COUNT_REQ(); break;
                    case 5188: packet = new PROTOCOL_BATTLE_MISSION_TUTORIAL_ROUND_END_REQ(); break;
                    case 5262: packet = new PROTOCOL_BATTLE_NEW_JOIN_ROOM_SCORE_REQ(); break;
                    case 5276: packet = new PROTOCOL_BATTLE_USER_SOPETYPE_REQ(); break;
                    case 5292: packet = new PROTOCOL_BASE_UNKNOWN_PACKET_REQ(); break;
                    case 5377: packet = new PROTOCOL_LOBBY_QUICKJOIN_ROOM_REQ(); break;

                    // Character System
                    case 6145: packet = new PROTOCOL_CHAR_CREATE_CHARA_REQ(); break;
                    case 6149: packet = new PROTOCOL_CHAR_CHANGE_EQUIP_REQ(); break;
                    case 6151: packet = new PROTOCOL_CHAR_DELETE_CHARA_REQ(); break;

                    // GM Chat System
                    case 6657: packet = new PROTOCOL_GMCHAT_START_CHAT_REQ(); break;
                    case 6661: packet = new PROTOCOL_GMCHAT_END_CHAT_REQ(); break;
                    case 6663: packet = new PROTOCOL_GMCHAT_APPLY_PENALTY_REQ(); break;
                    case 6667: packet = new PROTOCOL_GMCHAT_APPLY_PENALTY_MULTI_REQ(); break;

                    case 6965: packet = new PROTOCOL_CLAN_WAR_RESULT_REQ(); break;
                    case 7429: packet = new PROTOCOL_BATTLEBOX_AUTH_REQ(); break;
                    case 7681: packet = new PROTOCOL_MATCH_SERVER_IDX_REQ(); break;
                    case 7699: packet = new PROTOCOL_MATCH_CLAN_SEASON_REQ(); break;

                    // Season System
                    case 8449: packet = new PROTOCOL_SEASON_CHALLENGE_INFO_REQ(); break;
                    case 8453: packet = new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_REQ(); break;

                    default:
                        CLogger.Print(Bitwise.ToHexData($"Opcode Not Found: [{opcode}] | {direction}", packetData), LoggerType.Opcode);
                        break;
                }

                if (packet == null) return;

                using (packet)
                {
                    if (ConfigLoader.DebugMode)
                    {
                        CLogger.Print($"[GAME] {packet.GetType().Name}; Address: {Client.RemoteEndPoint}; Opcode: [{opcode}]", LoggerType.Debug);
                    }

                    packet.Makeme(this, packetData);
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        try
                        {
                            packet.Run();
                        }
                        catch (Exception ex)
                        {
                            CLogger.Print($"Error running packet {packet.GetType().Name}: {ex.Message}", LoggerType.Error, ex);
                        }
                    });
                    packet.Dispose();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }
    }
}