using IpPublicKnowledge;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.JSON;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Server.Auth.Data.Managers;
using Server.Auth.Data.Models;
using Server.Auth.Data.Sync.Server;
using Server.Auth.Data.Utils;
using Server.Auth.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Server.Auth.Network.ClientPacket
{
    /// <summary>
    /// Maneja la petición de login del cliente al servidor de autenticación
    /// v108: SC_VERSION layout changed, new S2MOValue<uchar> field added
    /// </summary>
    public class PROTOCOL_BASE_LOGIN_REQ : AuthClientPacket
    {
        #region Private Fields - Login Data

        private byte _verificationByte1;
        private byte _verificationByte2;
        private byte _verificationByte3;
        private byte _verificationByte4;
        private string _username;
        private string _password;
        private string _token;
        private string _clientVersion;
        private string _ipAddress;
        private string _screenResolution;
        private string _userFileList;
        private ClientLocale _clientLocale;
        private PhysicalAddress _macAddress;
        private IPI acs;

        #endregion Private Fields - Login Data

        #region Read Method

        public override void Read()
        {
            try
            {
                _verificationByte1 = ReadC();
                _verificationByte2 = ReadC();
                _verificationByte3 = ReadC();
                _verificationByte4 = ReadC();
                ReadC();
                byte[] macBytes = ReadB(6);
                _macAddress = new PhysicalAddress(macBytes);
                ReadB(20);
                _screenResolution = $"{ReadH()}x{ReadH()}";
                ReadB(10);
                _userFileList = ReadS(ReadC());
                ReadB(16);
                _clientLocale = (ClientLocale)ReadC();

               
                ushort versionMinor = (ushort)ReadH(); 
                ReadC();                                
                _clientVersion = $"{versionMinor}.0";  

                _token = ReadS(ReadH());
                _ipAddress = Client.GetIPAddress();
                ReadB(4);

                
                string rawToken = _token ?? string.Empty;
                string decoded = rawToken;
                try
                {
                    byte[] tokenBytes = Convert.FromBase64String(rawToken);
                    decoded = System.Text.Encoding.UTF8.GetString(tokenBytes);
                }
                catch { }

                int sep = decoded.IndexOf(':');
                if (sep > 0)
                {
                    _username = decoded.Substring(0, sep);
                    _password = decoded.Substring(sep + 1);
                }
                else
                {
                    
                    _username = decoded;
                    _password = rawToken;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Read(): {ex.Message}");
                throw;
            }
        }

        #endregion Read Method

        #region Run Method - Main Login Logic

        private static readonly Dictionary<string, DateTime> _lastLoginAttempts = new Dictionary<string, DateTime>();
        private static readonly object _lockObject = new object();

        public override void Run()
        {
            try
            {
                if (Client == null)
                {
                    CLogger.Print("PROTOCOL_BASE_LOGIN_REQ.Run(): Client is null", LoggerType.Error);
                    return;
                }

                string clientKey = Client.GetIPAddress();

                // --- Anti spam: blokir percobaan login < 1 detik ---
                lock (_lockObject)
                {
                    if (_lastLoginAttempts.ContainsKey(clientKey))
                    {
                        var timeDiff = DateTime.Now - _lastLoginAttempts[clientKey];
                        if (timeDiff.TotalMilliseconds < 1000)
                        {
                            CLogger.Print($"Duplicate login attempt blocked for {clientKey}", LoggerType.Warning);
                            Client.Close(1000, false);
                            return;
                        }
                    }
                    _lastLoginAttempts[clientKey] = DateTime.Now;
                }

                // --- Verifikasi bytes autentikasi ---
                uint verificationKey = ComDiv.Verificate(
                    _verificationByte1,
                    _verificationByte2,
                    _verificationByte3,
                    _verificationByte4
                );

                if (verificationKey == 0U)
                {
                    CLogger.Print($"Invalid verification bytes for IP: {clientKey}", LoggerType.Warning);
                    Client.Close(1000, false);
                    return;
                }

                // --- Enkripsi password jika dikonfigurasi ---
                if (ConfigLoader.UseCryptedPassword)
                    _password = Bitwise.HashString(_password, ConfigLoader.CryptedPasswordSalt);

                if (ConfigLoader.DebugMode)
                    CLogger.Print($"[LOGIN] username='{_username}', password='{_password}', token='{_token}', crypted={ConfigLoader.UseCryptedPassword}", LoggerType.Debug);
                ServerConfig config = AuthXender.Client.Config;

                // --- Validasi awal ---
                if (!ValidateInitialRequirements(config))
                {
                    HandleInitialValidationFailure(config);
                    return;
                }

                // --- Ambil akun dari database ---
                Account player = Client.Player = AccountManager.GetAccountDB(_username, _password, 2, 95);

                // --- Auto create account jika tidak ada ---
                if (player == null && ConfigLoader.AutoAccount)
                {
                    if (!AccountManager.CreateAccount(out player, _username, _password))
                    {
                        Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(EventErrorEnum.LOGIN_DELETE_ACCOUNT, null, 0U));
                        CLogger.Print($"Failed to create account automatically [{_username}]", LoggerType.Warning);
                        Client.Close(1000, false);
                        return;
                    }

                    Client.Player = player;
                    CLogger.Print($"Account created automatically [{_username}]", LoggerType.Info);
                }

                // --- Proses login ---
                if (player != null)
                {
                    ProcessLogin(player, config, verificationKey);
                }
                else
                {
                    HandleInvalidCredentials(null);
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        #endregion Run Method - Main Login Logic

        #region Validation Methods

        /// <summary>
        /// Validasi kebutuhan awal client sebelum proses login
        /// </summary>
        private bool ValidateInitialRequirements(ServerConfig config)
        {
            if (config == null)
                return false;

            if (!ConfigLoader.IsTestMode && !ConfigLoader.GameLocales.Contains(_clientLocale))
                return false;

            if (_username.Length < ConfigLoader.MinUserSize || _username.Length > ConfigLoader.MaxUserSize)
                return false;

            if (!ConfigLoader.IsTestMode && _password.Length < ConfigLoader.MinPassSize)
                return false;

            if (_macAddress.GetAddressBytes().SequenceEqual(new byte[6]))
                return false;

            // v108: format version adalah "108" (bukan "3.80" seperti v80)
            if (_clientVersion != config.ClientVersion)
                return false;

            if (config.AccessUFL && _userFileList != config.UserFileList)
                return false;

            if (_screenResolution.Equals("0x0") || ResolutionJSON.GetDisplay(_screenResolution).Equals("Invalid"))
                return false;

            return true;
        }

        /// <summary>
        /// Handle kegagalan validasi awal
        /// </summary>
        private void HandleInitialValidationFailure(ServerConfig config)
        {
            string errorMessage = GetValidationErrorMessage(config);
            Client.SendPacket(new PROTOCOL_SERVER_MESSAGE_DISCONNECTIONSUCCESS_ACK(2147483904U, false));
            CLogger.Print(errorMessage, LoggerType.Warning);
            Client.Close(1000, true);
        }

        /// <summary>
        /// Dapatkan pesan error validasi yang sesuai
        /// </summary>
        private string GetValidationErrorMessage(ServerConfig config)
        {
            if (config == null)
                return $"Invalid server config [{_username}]";

            if (!ConfigLoader.IsTestMode && !ConfigLoader.GameLocales.Contains(_clientLocale))
                return $"Country: {_clientLocale} of blocked client [{_username}]";

            if (_username.Length < ConfigLoader.MinUserSize)
                return $"Username too short [{_username}]";

            if (_username.Length > ConfigLoader.MaxUserSize)
                return $"Username too long [{_username}]";

            if (!ConfigLoader.IsTestMode && _password.Length < ConfigLoader.MinPassSize)
                return $"Password too short [{_username}]";

            if (!ConfigLoader.IsTestMode && _password.Length > ConfigLoader.MaxPassSize)
                return $"Password too long [{_username}]";

            if (_macAddress.GetAddressBytes().SequenceEqual(new byte[6]))
                return $"Invalid MAC Address [{_username}]";

            // v108: version format "108"
            if (_clientVersion != config.ClientVersion)
                return $"Version: {_clientVersion} not supported (expected: {config.ClientVersion}) [{_username}]";

            if (config.AccessUFL && _userFileList != config.UserFileList)
                return $"UserFileList: {_userFileList} not supported [{_username}]";

            if (_screenResolution.Equals("0x0") || ResolutionJSON.GetDisplay(_screenResolution).Equals("Invalid"))
                return $"Invalid {_screenResolution} resolution [{_username}]";

            return $"Unknown validation error [{_username}]";
        }

        #endregion Validation Methods

        #region Login Processing Methods

        /// <summary>
        /// Proses login setelah validasi kredensial berhasil
        /// </summary>
        private void ProcessLogin(Account player, ServerConfig config, uint verificationKey)
        {
            // Cek ban aktif dari database
            BanHistory activeBan = DaoManagerSQL.GetActiveBanForPlayer(player.PlayerId);

            if (activeBan != null && activeBan.EndDate > DateTimeUtil.Now())
            {
                if (player.BanObjectId != activeBan.ObjectId)
                {
                    player.BanObjectId = activeBan.ObjectId;
                    ComDiv.UpdateDB("accounts", "ban_object_id", activeBan.ObjectId, "player_id", player.PlayerId);
                }

                Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(EventErrorEnum.EVENT_LOG_IN_BLOCK_ACCOUNT, player, 0U));
                CLogger.Print($"Account is banned until {activeBan.EndDate:yyyy-MM-dd HH:mm:ss} [{player.Username}]", LoggerType.Warning);
                Client.Close(1000, false);
                return;
            }

            // Backup check: cek ban permanen
            if (player.IsBanned())
            {
                Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(EventErrorEnum.EVENT_LOG_IN_BLOCK_ACCOUNT, player, 0U));
                CLogger.Print($"Permanently banned [{player.Username}]", LoggerType.Warning);
                Client.Close(1000, false);
                return;
            }

            // Update MAC address jika berubah
            if (player.MacAddress != _macAddress)
                ComDiv.UpdateDB("accounts", "mac_address", _macAddress, "player_id", player.PlayerId);

            // Cek ban MAC dan IP
            bool isMacBanned;
            bool isIpBanned;
            DaoManagerSQL.GetBanStatus($"{_macAddress}", _ipAddress, out isMacBanned, out isIpBanned);

            if (isMacBanned || isIpBanned)
            {
                CLogger.Print(
                    $"{(isMacBanned ? "MAC Address blocked" : "IP4 Address blocked")} [{player.Username}]",
                    LoggerType.Warning
                );
                Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(
                    isIpBanned ? EventErrorEnum.LOGIN_BLOCK_IP : EventErrorEnum.EVENT_LOG_IN_BLOCK_ACCOUNT,
                    player,
                    0U
                ));
                Client.Close(1000, false);
                return;
            }

            // Cek level akses
            bool hasAccess = (player.IsGM() && config.OnlyGM) ||
                             (player.AuthLevel() >= AccessLevel.NORMAL && !config.OnlyGM);

            if (!hasAccess)
            {
                Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(EventErrorEnum.EVENT_LOG_IN_TIME_OUT_2, player, 0U));
                CLogger.Print($"Invalid access level [{player.Username}]", LoggerType.Warning);
                Client.Close(1000, false);
                return;
            }

            // Ambil cached account
            Account cachedAccount = AccountManager.GetAccount(player.PlayerId, true);

            // Cek apakah sudah online
            if (player.IsOnline)
            {
                HandleAlreadyOnline(player, cachedAccount);
                return;
            }

            // Cek ban temporal aktif
            if (player.BanObjectId > 0)
            {
                BanHistory banInfo = DaoManagerSQL.GetAccountBan(player.BanObjectId);

                if (banInfo != null && banInfo.EndDate > DateTimeUtil.Now())
                {
                    Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(EventErrorEnum.EVENT_LOG_IN_BLOCK_ACCOUNT, player, 0U));
                    CLogger.Print($"Account with ban is Active [{player.Username}]", LoggerType.Warning);
                    Client.Close(1000, false);
                    return;
                }
            }

            // Login berhasil
            CompleteSuccessfulLogin(player, cachedAccount, verificationKey);
        }

        /// <summary>
        /// Handle kasus akun sudah online
        /// </summary>
        private void HandleAlreadyOnline(Account player, Account cachedAccount)
        {
            Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(EventErrorEnum.EVENT_LOG_IN_ALREADY_LOGIN, player, 0U));
            CLogger.Print($"Account online [{player.Username}]", LoggerType.Warning);

            if (cachedAccount != null && cachedAccount.Connection != null)
            {
                cachedAccount.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(1));
                cachedAccount.SendPacket(new PROTOCOL_SERVER_MESSAGE_ERROR_ACK(2147487744U));
                cachedAccount.Close(1000);
            }
            else
            {
                AuthLogin.SendLoginKickInfo(player);
            }

            Client.Close(1000, false);
        }

        /// <summary>
        /// Selesaikan proses login yang berhasil
        /// </summary>
        private void CompleteSuccessfulLogin(Account player, Account cachedAccount, uint verificationKey)
        {
            // Update country flag
            FlagCountryLATAM();

            // Set player ID dengan flags
            player.SetPlayerId(player.PlayerId, 8159);

            // Kirim konfirmasi login berhasil
            Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(
                EventErrorEnum.SUCCESS,
                player,
                AllUtils.ValidateKey(player.PlayerId, Client.SessionId, verificationKey)
            ));

            // Kirim info cash/points
            Client.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0U, player));

            // Load info clan jika ada
            if (player.ClanId > 0)
            {
                player.ClanPlayers = ClanManager.GetClanPlayers(player.ClanId, player.PlayerId);
                Client.SendPacket(new PROTOCOL_CS_MEMBER_INFO_ACK(player.ClanPlayers));
            }

            // Update status player
            player.Status.SetData(uint.MaxValue, player.PlayerId);
            player.Status.UpdateServer(0);
            player.SetOnlineStatus(true);

            // Update koneksi di cached account
            if (cachedAccount != null)
                cachedAccount.Connection = Client;

            // Mulai heartbeat
            Client.HeartBeatCounter();
            SendRefresh.RefreshAccount(player, true);

            CLogger.Print($"Login successful [{player.Username}] IP: {_ipAddress}", LoggerType.Info);
        }

        /// <summary>
        /// Handle kredensial tidak valid
        /// </summary>
        private void HandleInvalidCredentials(Account player)
        {
            EventErrorEnum errorCode = EventErrorEnum.LOGIN_ID_PASS_INCORRECT;

            Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(errorCode, player, 0U));
            CLogger.Print(
                player == null
                    ? $"Invalid username or password [{_username}]"
                    : $"Invalid password [{_username}]",
                LoggerType.Warning
            );
            Client.Close(1000, false);
        }

        #endregion Login Processing Methods

        #region Country Flag Update

        /// <summary>
        /// Update country flag berdasarkan IP geolocation
        /// </summary>
        public bool FlagCountryLATAM()
        {
            try
            {
                Account p = Client.Player;

                if (p == null)
                {
                    CLogger.Print("FlagCountryLATAM: Player is null", LoggerType.Warning);
                    return false;
                }

                if (p.CountryFlags > 0)
                    return true;

                acs = IPK.GetIpInfo(Client.GetAddress());

                if (acs == null || string.IsNullOrEmpty(acs.country))
                {
                    CLogger.Print($"Failed to get country info for IP: {Client.GetAddress()}", LoggerType.Warning);
                    return false;
                }

                int countryFlag;
                switch (acs.country)
                {
                    case "Venezuela": countryFlag = 1; break;
                    case "Peru": countryFlag = 2; break;
                    case "Chile": countryFlag = 3; break;
                    case "Ecuador": countryFlag = 4; break;
                    case "Bolivia": countryFlag = 5; break;
                    case "Argentina": countryFlag = 6; break;
                    case "Mexico": countryFlag = 7; break;
                    case "United States": countryFlag = 8; break;
                    case "Spain": countryFlag = 9; break;
                    case "France": countryFlag = 10; break;
                    case "Colombia": countryFlag = 11; break;
                    case "Paraguay": countryFlag = 12; break;
                    case "Dominican Republic": countryFlag = 13; break;
                    case "Puerto Rico": countryFlag = 14; break;
                    case "Uruguay": countryFlag = 15; break;
                    case "Trinidad and Tobago": countryFlag = 16; break;
                    case "Portugal": countryFlag = 17; break;
                    case "Italy": countryFlag = 18; break;
                    case "Canada": countryFlag = 19; break;
                    case "Honduras": countryFlag = 20; break;
                    default: countryFlag = 0; break;
                }

                if (ComDiv.UpdateDB("accounts", "country_flags", countryFlag, "player_id", p.PlayerId))
                {
                    p.CountryFlags = countryFlag;
                    CLogger.Print(
                        $"Country flag updated to {countryFlag} ({acs.country}) for player {p.PlayerId}",
                        LoggerType.Info
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print($"Error updating country flag: {ex.Message}", LoggerType.Error, ex);
                return false;
            }
        }

        #endregion Country Flag Update
    }
}
