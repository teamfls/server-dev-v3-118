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
                if (this._raw != null)
                {
                    CLogger.Print(Bitwise.ToHexData("LOGIN_REQ", this._raw), LoggerType.Info);
                }

                _verificationByte1 = ReadC();
                _verificationByte2 = ReadC();
                _verificationByte3 = ReadC();
                _verificationByte4 = ReadC();
                byte[] macBytes = ReadB(6);
                _macAddress = new PhysicalAddress(macBytes);
                ReadB(20); 
                _screenResolution = $"{ReadH()}x{ReadH()}";
                ReadB(9);  
                _userFileList = ReadS(ReadC());
                ReadB(16); 
                _clientLocale = (ClientLocale)ReadC();
                _clientVersion = $"{ReadC()}.{ReadH()}";
                ReadH(); 
                _password = ReadS(ReadC());
                _username = ReadS(ReadC());
                _ipAddress = Client.GetIPAddress();

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

                lock (_lockObject)
                {
                    // Verificar si hay un intento reciente (menos de 1 segundo)
                    if (_lastLoginAttempts.ContainsKey(clientKey))
                    {
                        var timeDiff = DateTime.Now - _lastLoginAttempts[clientKey];
                        if (timeDiff.TotalMilliseconds < 1000)
                        {
                            Console.WriteLine($"Duplicate login attempt blocked for {clientKey}");
                            Client.Close(1000, false);
                            return;
                        }
                    }

                    _lastLoginAttempts[clientKey] = DateTime.Now;
                }

                // Verificar bytes de autenticación
                uint verificationKey = ComDiv.Verificate(_verificationByte1, _verificationByte2, _verificationByte3, _verificationByte4);

                if (verificationKey == 0U)
                {
                    Client.Close(1000, false);
                    return;
                }

                // Encriptar contraseña si está configurado
                if (ConfigLoader.UseCryptedPassword)
                    _password = Bitwise.HashString(_password, ConfigLoader.CryptedPasswordSalt);

                ServerConfig config = AuthXender.Client.Config;

                // Validaciones iniciales
                if (!ValidateInitialRequirements(config))
                {
                    HandleInitialValidationFailure(config);
                    return;
                }

                // Obtener cuenta de la base de datos (buscar por username y password)
                Account player = Client.Player = AccountManager.GetAccountDB(_username, _password, 2, 95);

                // Crear cuenta automáticamente si no existe y está habilitado
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

                // Validar cuenta y procesar login
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
        /// Valida los requisitos iniciales del cliente
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

            if (_clientVersion != config.ClientVersion)
                return false;

            if (config.AccessUFL && _userFileList != config.UserFileList)
                return false;

            if (_screenResolution.Equals("0x0") || ResolutionJSON.GetDisplay(_screenResolution).Equals("Invalid"))
                return false;

            return true;
        }

        /// <summary>
        /// Maneja el fallo de validación inicial
        /// </summary>
        private void HandleInitialValidationFailure(ServerConfig config)
        {
            string errorMessage = GetValidationErrorMessage(config);

            Client.SendPacket(new PROTOCOL_SERVER_MESSAGE_DISCONNECTIONSUCCESS_ACK(2147483904U, false));
            CLogger.Print(errorMessage, LoggerType.Warning);
            Client.Close(1000, true);
        }

        /// <summary>
        /// Obtiene el mensaje de error de validación apropiado
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

            if (_clientVersion != config.ClientVersion)
                return $"Version: {_clientVersion} not supported [{_username}]";

            if (config.AccessUFL && _userFileList != config.UserFileList)
                return $"UserFileList: {_userFileList} not supported [{_username}]";

            if (_screenResolution.Equals("0x0") || ResolutionJSON.GetDisplay(_screenResolution).Equals("Invalid"))
                return $"Invalid {_screenResolution} resolution [{_username}]";

            return "There is something wrong happened when trying to login " + _username;
        }

        #endregion Validation Methods

        #region Login Processing Methods

        /// <summary>
        /// Procesa el login del jugador después de validar credenciales
        /// </summary>
        private void ProcessLogin(Account player, ServerConfig config, uint verificationKey)
        {
            // ✅ PERBAIKAN 1: Cek ban aktif dari database terlebih dahulu
            BanHistory activeBan = DaoManagerSQL.GetActiveBanForPlayer(player.PlayerId);

            if (activeBan != null && activeBan.EndDate > DateTimeUtil.Now())
            {
                // Update BanObjectId jika belum sinkron
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

            // Verificar si la cuenta está baneada (backup check)
            if (player.IsBanned())
            {
                Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(EventErrorEnum.EVENT_LOG_IN_BLOCK_ACCOUNT, player, 0U));
                CLogger.Print($"Permanently banned [{player.Username}]", LoggerType.Warning);
                Client.Close(1000, false);
                return;
            }

            // Actualizar dirección MAC si cambió
            if (player.MacAddress != _macAddress)
                ComDiv.UpdateDB("accounts", "mac_address", _macAddress, "player_id", player.PlayerId);

            // Verificar bans de MAC e IP
            bool isMacBanned;
            bool isIpBanned;
            DaoManagerSQL.GetBanStatus($"{_macAddress}", _ipAddress, out isMacBanned, out isIpBanned);

            if (isMacBanned || isIpBanned)
            {
                CLogger.Print($"{(isMacBanned ? "MAC Address blocked" : "IP4 Address blocked")} [{player.Username}]", LoggerType.Warning);
                Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(
                    isIpBanned ? EventErrorEnum.LOGIN_BLOCK_IP : EventErrorEnum.EVENT_LOG_IN_BLOCK_ACCOUNT,
                    player,
                    0U
                ));
                Client.Close(1000, false);
                return;
            }

            // Verificar nivel de acceso
            bool hasAccess = (player.IsGM() && config.OnlyGM) || (player.AuthLevel() >= AccessLevel.NORMAL && !config.OnlyGM);

            if (!hasAccess)
            {
                Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(EventErrorEnum.EVENT_LOG_IN_TIME_OUT_2, player, 0U));
                CLogger.Print($"Invalid access level [{player.Username}]", LoggerType.Warning);
                Client.Close(1000, false);
                return;
            }

            // Obtener cuenta desde el manager
            Account cachedAccount = AccountManager.GetAccount(player.PlayerId, true);

            // Verificar si ya está online
            if (player.IsOnline)
            {
                HandleAlreadyOnline(player, cachedAccount);
                return;
            }

            // ✅ PERBAIKAN 2: Verificar ban temporal activo dengan null check
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

            // Login exitoso
            CompleteSuccessfulLogin(player, cachedAccount, verificationKey);
        }

        /// <summary>
        /// Maneja el caso cuando la cuenta ya está online
        /// </summary>
        private void HandleAlreadyOnline(Account player, Account cachedAccount)
        {
            Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(EventErrorEnum.EVENT_LOG_IN_ALREADY_LOGIN, player, 0U));
            CLogger.Print($"Account online [{player.Username}]", LoggerType.Warning);

            if (cachedAccount != null && cachedAccount.Connection != null)
            {
                // Desconectar la sesión anterior
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
        /// Completa el proceso de login exitoso
        /// </summary>
        private void CompleteSuccessfulLogin(Account player, Account cachedAccount, uint verificationKey)
        {
            // Update country flag jika belum ada
            FlagCountryLATAM();

            // Establecer player ID con flags
            player.SetPlayerId(player.PlayerId, 8159);

            // Enviar confirmación de login exitoso
            Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(
                EventErrorEnum.SUCCESS,
                player,
                AllUtils.ValidateKey(player.PlayerId, Client.SessionId, verificationKey)
            ));

            // Enviar información de cash/puntos
            Client.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0U, player));

            // Cargar información del clan si pertenece a uno
            if (player.ClanId > 0)
            {
                player.ClanPlayers = ClanManager.GetClanPlayers(player.ClanId, player.PlayerId);
                Client.SendPacket(new PROTOCOL_CS_MEMBER_INFO_ACK(player.ClanPlayers));
            }

            // Actualizar estado del jugador
            player.Status.SetData(uint.MaxValue, player.PlayerId);
            player.Status.UpdateServer(0);
            player.SetOnlineStatus(true);

            // Actualizar conexión en cuenta cacheada
            if (cachedAccount != null)
                cachedAccount.Connection = Client;

            // Iniciar heartbeat y refrescar
            Client.HeartBeatCounter();
            SendRefresh.RefreshAccount(player, true);
        }

        /// <summary>
        /// Maneja credenciales inválidas
        /// </summary>
        private void HandleInvalidCredentials(Account player)
        {
            string errorMessage = "";
            EventErrorEnum errorCode = EventErrorEnum.FAIL;

            if (player == null)
            {
                errorMessage = "Invalid username or password";
                errorCode = EventErrorEnum.LOGIN_ID_PASS_INCORRECT;
            }
            else
            {
                errorMessage = "Invalid password";
                errorCode = EventErrorEnum.LOGIN_ID_PASS_INCORRECT;
            }

            Client.SendPacket(new PROTOCOL_BASE_LOGIN_ACK(errorCode, player, 0U));
            CLogger.Print($"{errorMessage} [{_username}]", LoggerType.Warning);
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

                // Cek apakah sudah ada country_flags yang valid (bukan 0)
                if (p.CountryFlags > 0)
                {
                    return true; // Sudah ada flag, tidak perlu update
                }

                // Dapatkan info IP
                acs = IPK.GetIpInfo(Client.GetAddress());

                if (acs == null || string.IsNullOrEmpty(acs.country))
                {
                    CLogger.Print($"Failed to get country info for IP: {Client.GetAddress()}", LoggerType.Warning);
                    return false;
                }

                int countryFlag = 0;

                switch (acs.country)
                {
                    case "Venezuela":
                        countryFlag = 1;
                        break;
                    case "Peru":
                        countryFlag = 2;
                        break;
                    case "Chile":
                        countryFlag = 3;
                        break;
                    case "Ecuador":
                        countryFlag = 4;
                        break;
                    case "Bolivia":
                        countryFlag = 5;
                        break;
                    case "Argentina":
                        countryFlag = 6;
                        break;
                    case "Mexico":
                        countryFlag = 7;
                        break;
                    case "United States":
                        countryFlag = 8;
                        break;
                    case "Spain":
                        countryFlag = 9;
                        break;
                    case "France":
                        countryFlag = 10;
                        break;
                    case "Colombia":
                        countryFlag = 11;
                        break;
                    case "Paraguay":
                        countryFlag = 12;
                        break;
                    case "Dominican Republic":
                        countryFlag = 13;
                        break;
                    case "Puerto Rico":
                        countryFlag = 14;
                        break;
                    case "Uruguay":
                        countryFlag = 15;
                        break;
                    case "Trinidad and Tobago":
                        countryFlag = 16;
                        break;
                    case "Portugal":
                        countryFlag = 17;
                        break;
                    case "Italy":
                        countryFlag = 18;
                        break;
                    case "Canada":
                        countryFlag = 19;
                        break;
                    case "Honduras":
                        countryFlag = 20;
                        break;
                    default:
                        countryFlag = 0;
                        break;
                }

                // Update ke database dan memory
                if (ComDiv.UpdateDB("accounts", "country_flags", countryFlag, "player_id", p.PlayerId))
                {
                    p.CountryFlags = countryFlag;
                    CLogger.Print($"Country flag updated to {countryFlag} ({acs.country}) for player {p.PlayerId}", LoggerType.Info);
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