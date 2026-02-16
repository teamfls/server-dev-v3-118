using Npgsql;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.Network;
using Plugin.Core.RAW;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Network;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Server.Game.Data.Utils
{
    /// <summary>
    /// Clase principal de utilidades del servidor del juego
    /// Contiene toda la lógica central para la gestión del juego multijugador
    /// Incluye sistemas de jugadores, batallas, misiones, anti-cheat, economía y más
    /// </summary>
    public static class AllUtils
    {
        #region PLAYER AUTHENTICATION AND ACCESS MANAGEMENT

        /// <summary>
        /// Valida y corrige el nivel de acceso del jugador
        /// Verifica que el AccessLevel sea válido y lo actualiza si es necesario
        /// </summary>
        /// <param name="player">Cuenta del jugador a validar</param>

        public static void ValidateAuthLevel(Account player)
        {
            if (Enum.IsDefined(typeof(AccessLevel), (object)player.Access))
                return;

            AccessLevel accessLevel = player.AuthLevel();
            if (!ComDiv.UpdateDB("accounts", "access_level", (object)(int)accessLevel, "player_id", (object)player.PlayerId))
                return;
            player.Access = accessLevel;
        }

        #endregion PLAYER AUTHENTICATION AND ACCESS MANAGEMENT

        #region PLAYER DATA LOADING METHODS

        /// <summary>
        /// Carga el inventario completo del jugador desde la base de datos
        /// Incluye todos los ítems que posee el jugador
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        public static void LoadPlayerInventory(Account player)
        {
            lock (player.Inventory.Items)
                player.Inventory.Items.AddRange((IEnumerable<ItemsModel>)DaoManagerSQL.GetPlayerInventoryItems(player.PlayerId));
        }

        /// <summary>
        /// Carga las misiones activas del jugador desde la base de datos
        /// Si no existen misiones, crea un nuevo registro
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void LoadPlayerMissions(Account player)
        {
            PlayerMissions playerMissionsDb = DaoManagerSQL.GetPlayerMissionsDB(player.PlayerId,
                player.Mission.Mission1, player.Mission.Mission2,
                player.Mission.Mission3, player.Mission.Mission4);

            if (playerMissionsDb != null)
            {
                player.Mission = playerMissionsDb;
            }
            else
            {
                if (DaoManagerSQL.CreatePlayerMissionsDB(player.PlayerId))
                    return;
                CLogger.Print("There was an error when creating Player Missions!", LoggerType.Warning);
            }
        }

        /// <summary>
        /// Valida y actualiza el estado del inventario del jugador
        /// Incluye validación de ítems básicos, recompensas de PC Café y efectos especiales
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void ValidatePlayerInventoryStatus(Account player)
        {
            // Cargar ítems básicos del jugador
            player.Inventory.LoadBasicItems();

            // Cargar boina especial para generales (rango 46+)
            if (player.Rank >= 46)
                player.Inventory.LoadGeneralBeret();

            // Cargar sombrero especial para Game Masters
            if (player.IsGM())
                player.Inventory.LoadHatForGM();

            // Validar personajes si el jugador tiene nickname válido
            if (!string.IsNullOrEmpty(player.Nickname))
                CreateCharactersFromInventoryItems(player);

            string validationMessage;
            if (ValidateVipAndPcCafeAccess(player, out validationMessage))
            {
                // Procesar recompensas de PC Café para usuarios VIP
                ProcessPcCafeRewards(player);
            }
            else
            {
                // Remover recompensas de PC Café si no tiene acceso válido
                RemovePcCafeRewards(player, validationMessage);
            }
        }

        /// Remueve las recompensas de PC Café del jugador
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="validationMessage">Mensaje de validación</param>
        private static void RemovePcCafeRewards(Account player, string validationMessage)
        {
            foreach (ItemsModel pcCafeReward in TemplatePackXML.GetPCCafeRewards(player.CafePC))
            {
                if (ComDiv.GetIdStatics(pcCafeReward.Id, 1) == 6 && player.Character.GetCharacter(pcCafeReward.Id) != null)
                    RemoveCharacterItem(player, pcCafeReward.Id);

                if (ComDiv.GetIdStatics(pcCafeReward.Id, 1) == 16 /*0x10*/)
                {
                    CouponFlag couponEffect = CouponEffectXML.GetCouponEffect(pcCafeReward.Id);
                    if (couponEffect != null && couponEffect.EffectFlag > (CouponEffects)0 &&
                        player.Effects.HasFlag((Enum)couponEffect.EffectFlag))
                    {
                        player.Effects -= couponEffect.EffectFlag;
                        DaoManagerSQL.UpdateCouponEffect(player.PlayerId, player.Effects);
                    }
                }
            }

            if (player.CafePC <= CafeEnum.None ||
                !ComDiv.UpdateDB("accounts", "pc_cafe", (object)0, "player_id", (object)player.PlayerId))
                return;

            player.CafePC = CafeEnum.None;
            if (!string.IsNullOrEmpty(validationMessage) &&
                ComDiv.DeleteDB("player_vip", "owner_id", (object)player.PlayerId))
                CLogger.Print($"VIP for UID: {player.PlayerId} Nick: {player.Nickname} Deleted Due To {validationMessage}", LoggerType.Info);

            CLogger.Print($"Player PC Cafe was resetted by default into '{player.CafePC}'; (UID: {player.PlayerId} Nick: {player.Nickname})", LoggerType.Info);
        }

        /// <summary>
        /// Procesa las recompensas de PC Café para el jugador
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        private static void ProcessPcCafeRewards(Account player)
        {
            List<ItemsModel> pcCafeRewards = TemplatePackXML.GetPCCafeRewards(player.CafePC);
            lock (player.Inventory.Items)
                player.Inventory.Items.AddRange((IEnumerable<ItemsModel>)pcCafeRewards);

            foreach (ItemsModel itemsModel in pcCafeRewards)
            {
                if (ComDiv.GetIdStatics(itemsModel.Id, 1) == 6 && player.Character.GetCharacter(itemsModel.Id) == null)
                    CreateCharacter(player, itemsModel);

                if (ComDiv.GetIdStatics(itemsModel.Id, 1) == 16 /*0x10*/)
                {
                    CouponFlag couponEffect = CouponEffectXML.GetCouponEffect(itemsModel.Id);
                    if (couponEffect != null && couponEffect.EffectFlag > (CouponEffects)0 &&
                        !player.Effects.HasFlag((Enum)couponEffect.EffectFlag))
                    {
                        player.Effects |= couponEffect.EffectFlag;
                        DaoManagerSQL.UpdateCouponEffect(player.PlayerId, player.Effects);
                    }
                }
            }
        }

        /// <summary>
        /// Valida el acceso VIP y de PC Café del jugador
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="message">Mensaje de validación de salida</param>
        /// <returns>True si tiene acceso válido</returns>

        private static bool ValidateVipAndPcCafeAccess(Account player, out string message)
        {
            if (player.IsGM())
            {
                message = "GM Special Access";
                return true;
            }

            PlayerVip playerVip = DaoManagerSQL.GetPlayerVIP(player.PlayerId);
            if (playerVip != null)
            {
                if (playerVip.Expirate < uint.Parse(DateTimeUtil.Now("yyMMddHHmm")))
                {
                    message = "The Time Has Expired!";
                    return false;
                }

                //if (!InternetCafeXML.IsValidAddress(DaoManagerSQL.GetPlayerIP4Address(player.PlayerId), playerVip.Address) && ConfigLoader.ICafeSystem)
                //{
                //    message = "Invalid Configuration!";
                //    return false;
                //}

                string currentBenefit = $"{player.CafePC}";
                if (!playerVip.Benefit.Equals(currentBenefit) && ComDiv.UpdateDB("player_vip", "last_benefit", (object)currentBenefit, "owner_id", (object)player.PlayerId))
                    playerVip.Benefit = currentBenefit;

                message = "Valid Access";
                return true;
            }

            message = "Database Not Found!";
            return false;
        }

        /// <summary>
        /// Carga el equipamiento del jugador desde la base de datos
        /// Si no existe, crea un nuevo registro de equipamiento
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void LoadPlayerEquipments(Account player)
        {
            PlayerEquipment playerEquipmentsDb = DaoManagerSQL.GetPlayerEquipmentsDB(player.PlayerId);
            if (playerEquipmentsDb == null)
            {
                if (DaoManagerSQL.CreatePlayerEquipmentsDB(player.PlayerId))
                    return;
                CLogger.Print("There was an error when creating Player Equipment!", LoggerType.Warning);
            }
            else
                player.Equipment = playerEquipmentsDb;
        }

        /// <summary>
        /// Carga los personajes del jugador desde la base de datos
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        public static void LoadPlayerCharacters(Account player)
        {
            List<CharacterModel> playerCharactersDb = DaoManagerSQL.GetPlayerCharactersDB(player.PlayerId);
            if (playerCharactersDb.Count <= 0)
                return;
            player.Character.Characters = playerCharactersDb;
        }

        /// <summary>
        /// Carga todas las estadísticas del jugador desde la base de datos
        /// Incluye estadísticas básicas, de temporada, de clan, diarias, de armas, etc.
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void LoadPlayerStatistic(Account player)
        {
            // Estadísticas básicas
            StatisticTotal playerStatBasicDb = DaoManagerSQL.GetPlayerStatBasicDB(player.PlayerId);
            if (playerStatBasicDb != null)
                player.Statistic.Basic = playerStatBasicDb;
            else if (!DaoManagerSQL.CreatePlayerStatBasicDB(player.PlayerId))
                CLogger.Print("There was an error when creating Player Basic Statistic!", LoggerType.Warning);

            // Estadísticas de temporada
            StatisticSeason playerStatSeasonDb = DaoManagerSQL.GetPlayerStatSeasonDB(player.PlayerId);
            if (playerStatSeasonDb != null)
                player.Statistic.Season = playerStatSeasonDb;
            else if (!DaoManagerSQL.CreatePlayerStatSeasonDB(player.PlayerId))
                CLogger.Print("There was an error when creating Player Season Statistic!", LoggerType.Warning);

            // Estadísticas de clan
            StatisticClan playerStatClanDb = DaoManagerSQL.GetPlayerStatClanDB(player.PlayerId);
            if (playerStatClanDb == null)
            {
                if (!DaoManagerSQL.CreatePlayerStatClanDB(player.PlayerId))
                    CLogger.Print("There was an error when creating Player Clan Statistic!", LoggerType.Warning);
            }
            else
                player.Statistic.Clan = playerStatClanDb;

            // Estadísticas diarias
            StatisticDaily playerStatDailiesDb = DaoManagerSQL.GetPlayerStatDailiesDB(player.PlayerId);
            if (playerStatDailiesDb == null)
            {
                if (!DaoManagerSQL.CreatePlayerStatDailiesDB(player.PlayerId))
                    CLogger.Print("There was an error when creating Player Daily Statistic!", LoggerType.Warning);
            }
            else
                player.Statistic.Daily = playerStatDailiesDb;

            // Estadísticas de armas
            StatisticWeapon playerStatWeaponsDb = DaoManagerSQL.GetPlayerStatWeaponsDB(player.PlayerId);
            if (playerStatWeaponsDb != null)
                player.Statistic.Weapon = playerStatWeaponsDb;
            else if (!DaoManagerSQL.CreatePlayerStatWeaponsDB(player.PlayerId))
                CLogger.Print("There was an error when creating Player Weapon Statistic!", LoggerType.Warning);

            // Estadísticas de modo Ace
            StatisticAcemode playerStatAcemodesDb = DaoManagerSQL.GetPlayerStatAcemodesDB(player.PlayerId);
            if (playerStatAcemodesDb == null)
            {
                if (!DaoManagerSQL.CreatePlayerStatAcemodesDB(player.PlayerId))
                    CLogger.Print("There was an error when creating Player Acemode Statistic!", LoggerType.Warning);
            }
            else
                player.Statistic.Acemode = playerStatAcemodesDb;

            // Estadísticas de Battle Cup
            StatisticBattlecup playerStatBattlecupDb = DaoManagerSQL.GetPlayerStatBattlecupDB(player.PlayerId);
            if (playerStatBattlecupDb == null)
            {
                if (DaoManagerSQL.CreatePlayerStatBattlecupsDB(player.PlayerId))
                    return;
                CLogger.Print("There was an error when creating Player Battlecup Statistic!", LoggerType.Warning);
            }
            else
                player.Statistic.Battlecup = playerStatBattlecupDb;
        }

        /// <summary>
        /// Carga los títulos del jugador desde la base de datos
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void LoadPlayerTitles(Account player)
        {
            PlayerTitles playerTitlesDb = DaoManagerSQL.GetPlayerTitlesDB(player.PlayerId);
            if (playerTitlesDb != null)
            {
                player.Title = playerTitlesDb;
            }
            else
            {
                if (DaoManagerSQL.CreatePlayerTitlesDB(player.PlayerId))
                    return;
                CLogger.Print("There was an error when creating Player Title!", LoggerType.Warning);
            }
        }

        /// <summary>
        /// Carga el Battle Pass del jugador desde la base de datos
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void LoadPlayerBattlepass(Account player)
        {
            PlayerBattlepass playerBattlepassDb = DaoManagerSQL.GetPlayerBattlepassDB(player.PlayerId);
            if (playerBattlepassDb != null)
            {
                player.Battlepass = playerBattlepassDb;
            }
            else
            {
                if (DaoManagerSQL.CreatePlayerBattlepassDB(player.PlayerId))
                    return;
                CLogger.Print("There was an error when creating Player Battlepass!", LoggerType.Warning);
            }
        }

        /// <summary>
        /// Carga los datos competitivos del jugador desde la base de datos
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void LoadPlayerCompetitive(Account player)
        {
            PlayerCompetitive playerCompetitiveDb = DaoManagerSQL.GetPlayerCompetitiveDB(player.PlayerId);
            if (playerCompetitiveDb != null)
            {
                player.Competitive = playerCompetitiveDb;
            }
            else
            {
                if (DaoManagerSQL.CreatePlayerCompetitiveDB(player.PlayerId))
                    return;
                CLogger.Print("There was an error when creating Player Competitive!", LoggerType.Warning);
            }
        }

        /// <summary>
        /// Carga los bonos del jugador desde la base de datos
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void LoadPlayerBonus(Account player)
        {
            PlayerBonus playerBonusDb = DaoManagerSQL.GetPlayerBonusDB(player.PlayerId);
            if (playerBonusDb != null)
            {
                player.Bonus = playerBonusDb;
            }
            else
            {
                if (DaoManagerSQL.CreatePlayerBonusDB(player.PlayerId))
                    return;
                CLogger.Print("There was an error when creating Player Bonus!", LoggerType.Warning);
            }
        }

        /// <summary>
        /// Carga la lista de amigos del jugador desde la base de datos
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="loadFullDatabase">Si debe cargar información completa de los amigos</param>
        public static void LoadPlayerFriend(Account player, bool loadFullDatabase)
        {
            List<FriendModel> playerFriendsDb = DaoManagerSQL.GetPlayerFriendsDB(player.PlayerId);
            if (playerFriendsDb.Count <= 0)
                return;
            player.Friend.Friends = playerFriendsDb;
            if (!loadFullDatabase)
                return;
            AccountManager.GetFriendlyAccounts(player.Friend);
        }

        /// <summary>
        /// Carga los eventos del jugador desde la base de datos
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void LoadPlayerEvent(Account player)
        {
            PlayerEvent playerEventDb = DaoManagerSQL.GetPlayerEventDB(player.PlayerId);
            if (playerEventDb != null)
            {
                player.Event = playerEventDb;
            }
            else
            {
                if (DaoManagerSQL.CreatePlayerEventDB(player.PlayerId))
                    return;
                CLogger.Print("There was an error when creating Player Event!", LoggerType.Warning);
            }
        }

        /// <summary>
        /// Carga la configuración del jugador desde la base de datos
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void LoadPlayerConfig(Account player)
        {
            PlayerConfig playerConfigDb = DaoManagerSQL.GetPlayerConfigDB(player.PlayerId);
            if (playerConfigDb != null)
            {
                player.Config = playerConfigDb;
            }
            else
            {
                if (DaoManagerSQL.CreatePlayerConfigDB(player.PlayerId))
                    return;
                CLogger.Print("There was an error when creating Player Config!", LoggerType.Warning);
            }
        }

        /// <summary>
        /// Carga los quick starts del jugador desde la base de datos
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void LoadPlayerQuickstarts(Account player)
        {
            List<QuickstartModel> playerQuickstartsDb = DaoManagerSQL.GetPlayerQuickstartsDB(player.PlayerId);
            if (playerQuickstartsDb.Count > 0)
            {
                player.Quickstart.Quickjoins = playerQuickstartsDb;
            }
            else
            {
                if (DaoManagerSQL.CreatePlayerQuickstartsDB(player.PlayerId))
                    return;
                CLogger.Print("There was an error when creating Player Quickstarts!", LoggerType.Warning);
            }
        }

        /// <summary>
        /// Carga los reportes del jugador desde la base de datos
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void LoadPlayerReport(Account player)
        {
            PlayerReport playerReportDb = DaoManagerSQL.GetPlayerReportDB(player.PlayerId);
            if (playerReportDb != null)
            {
                player.Report = playerReportDb;
            }
            else
            {
                if (DaoManagerSQL.CreatePlayerReportDB(player.PlayerId))
                    return;
                CLogger.Print("There was an error when creating Player Report!", LoggerType.Warning);
            }
        }

        #endregion PLAYER DATA LOADING METHODS

        #region KILL SCORING AND WEAPON SYSTEMS

        /// <summary>
        /// Calcula la puntuación obtenida por un tipo específico de kill
        /// </summary>
        /// <param name="killMessage">Tipo de kill realizado</param>
        /// <returns>Puntos otorgados por el kill</returns>
        public static int GetKillScore(KillingMessage killMessage)
        {
            int killScore = 0;
            switch (killMessage)
            {
                case KillingMessage.PiercingShot:
                case KillingMessage.MassKill:
                    killScore += 6;
                    goto case KillingMessage.Suicide;
                case KillingMessage.ChainStopper:
                    killScore += 8;
                    goto case KillingMessage.Suicide;
                case KillingMessage.Headshot:
                    killScore += 10;
                    goto case KillingMessage.Suicide;
                case KillingMessage.ChainHeadshot:
                    killScore += 14;
                    goto case KillingMessage.Suicide;
                case KillingMessage.ChainSlugger:
                    killScore += 6;
                    goto case KillingMessage.Suicide;
                case KillingMessage.Suicide:
                    return killScore;

                case KillingMessage.ObjectDefense:
                    killScore += 7;
                    goto case KillingMessage.Suicide;
                default:
                    killScore += 5;
                    goto case KillingMessage.Suicide;
            }
        }

        /// <summary>
        /// Convierte tipos de arma dual a su tipo base correspondiente
        /// </summary>
        /// <param name="weaponType">Tipo de arma original</param>
        /// <returns>Tipo de arma base</returns>
        private static ClassType ConvertDualWeaponType(ClassType weaponType)
        {
            switch (weaponType)
            {
                case ClassType.DualHandGun:
                    return ClassType.HandGun;

                case ClassType.DualKnife:
                case ClassType.Knuckle:
                    return ClassType.Knife;

                case ClassType.DualSMG:
                    return ClassType.SMG;

                case ClassType.DualShotgun:
                    return ClassType.Shotgun;

                default:
                    return weaponType;
            }
        }

        #endregion KILL SCORING AND WEAPON SYSTEMS

        #region BATTLE SYSTEM AND TEAM MANAGEMENT

        /// <summary>
        /// Determina el equipo ganador basado en las reglas de la sala
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <returns>Equipo ganador</returns>

        public static TeamEnum GetWinnerTeam(RoomModel room)
        {
            if (room == null)
                return TeamEnum.TEAM_DRAW;

            TeamEnum winnerTeam = TeamEnum.TEAM_DRAW;

            if (room.RoomType != RoomCondition.Bomb && room.RoomType != RoomCondition.Destroy &&
                room.RoomType != RoomCondition.Annihilation && room.RoomType != RoomCondition.Defense &&
                room.RoomType != RoomCondition.Convoy)
            {
                // Modos basados en kills
                if (room.IsDinoMode("DE"))
                {
                    // Modo Dinosaurio especial
                    if (room.CTDino != room.FRDino)
                    {
                        if (room.CTDino > room.FRDino)
                            winnerTeam = TeamEnum.CT_TEAM;
                        else if (room.CTDino < room.FRDino)
                            winnerTeam = TeamEnum.FR_TEAM;
                    }
                    else
                        winnerTeam = TeamEnum.TEAM_DRAW;
                }
                else if (room.CTKills == room.FRKills)
                    winnerTeam = TeamEnum.TEAM_DRAW;
                else if (room.CTKills <= room.FRKills)
                {
                    if (room.CTKills < room.FRKills)
                        winnerTeam = TeamEnum.FR_TEAM;
                }
                else
                    winnerTeam = TeamEnum.CT_TEAM;
            }
            else
            {
                // Modos basados en rondas
                if (room.CTRounds == room.FRRounds)
                    winnerTeam = TeamEnum.TEAM_DRAW;
                else if (room.CTRounds > room.FRRounds)
                    winnerTeam = TeamEnum.CT_TEAM;
                else if (room.CTRounds < room.FRRounds)
                    winnerTeam = TeamEnum.FR_TEAM;
            }

            return winnerTeam;
        }

        /// <summary>
        /// Determina el equipo ganador cuando hay jugadores desconectados
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="redPlayers">Número de jugadores rojos restantes</param>
        /// <param name="bluePlayers">Número de jugadores azules restantes</param>
        /// <returns>Equipo ganador</returns>
        public static TeamEnum GetWinnerTeam(RoomModel room, int redPlayers, int bluePlayers)
        {
            if (room == null)
                return TeamEnum.TEAM_DRAW;

            TeamEnum winnerTeam = TeamEnum.TEAM_DRAW;
            if (redPlayers == 0)
                winnerTeam = TeamEnum.CT_TEAM;
            else if (bluePlayers == 0)
                winnerTeam = TeamEnum.FR_TEAM;

            return winnerTeam;
        }

        /// <summary>
        /// Actualiza el contador de partidas en las estadísticas del jugador
        /// </summary>
        /// <param name="wonTheMatch">Si el jugador ganó la partida</param>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="winnerTeam">Equipo ganador (2 = empate)</param>
        /// <param name="totalQuery">Query para estadísticas totales</param>
        /// <param name="seasonQuery">Query para estadísticas de temporada</param>

        public static void UpdateMatchCount(bool wonTheMatch, Account player, int winnerTeam,
            DBQuery totalQuery, DBQuery seasonQuery)
        {
            if (winnerTeam == 2)
            {
                totalQuery.AddQuery("match_draws", (object)++player.Statistic.Basic.MatchDraws);
                seasonQuery.AddQuery("match_draws", (object)++player.Statistic.Season.MatchDraws);
            }
            else if (wonTheMatch)
            {
                totalQuery.AddQuery("match_wins", (object)++player.Statistic.Basic.MatchWins);
                seasonQuery.AddQuery("match_wins", (object)++player.Statistic.Season.MatchWins);
            }
            else
            {
                totalQuery.AddQuery("match_loses", (object)++player.Statistic.Basic.MatchLoses);
                seasonQuery.AddQuery("match_loses", (object)++player.Statistic.Season.MatchLoses);
            }

            totalQuery.AddQuery("matches", (object)++player.Statistic.Basic.Matches);
            totalQuery.AddQuery("total_matches", (object)++player.Statistic.Basic.TotalMatchesCount);
            seasonQuery.AddQuery("matches", (object)++player.Statistic.Season.Matches);
            seasonQuery.AddQuery("total_matches", (object)++player.Statistic.Season.TotalMatchesCount);
        }

        /// <summary>
        /// Actualiza las estadísticas diarias del jugador
        /// </summary>
        /// <param name="wonTheMatch">Si el jugador ganó la partida</param>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="winnerTeam">Equipo ganador</param>
        /// <param name="query">Query de base de datos</param>

        public static void UpdateDailyRecord(bool wonTheMatch, Account player, int winnerTeam, DBQuery query)
        {
            if (winnerTeam == 2)
                query.AddQuery("match_draws", (object)++player.Statistic.Daily.MatchDraws);
            else if (wonTheMatch)
                query.AddQuery("match_wins", (object)++player.Statistic.Daily.MatchWins);
            else
                query.AddQuery("match_loses", (object)++player.Statistic.Daily.MatchLoses);
            query.AddQuery("matches", (object)++player.Statistic.Daily.Matches);
        }

        /// <summary>
        /// Actualiza el contador de partidas para modo FFA (Free For All)
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="slotWin">Kills del slot ganador</param>
        /// <param name="totalQuery">Query para estadísticas totales</param>
        /// <param name="seasonQuery">Query para estadísticas de temporada</param>

        public static void UpdateMatchCountFFA(RoomModel room, Account player, int slotWin,
            DBQuery totalQuery, DBQuery seasonQuery)
        {
            int[] killCounts = new int[18];
            for (int index = 0; index < killCounts.Length; ++index)
            {
                SlotModel slot = room.Slots[index];
                killCounts[index] = slot.PlayerId != 0L ? slot.AllKills : 0;
            }

            int topPlayerIndex = 0;
            for (int index = 0; index < killCounts.Length; ++index)
            {
                if (killCounts[index] > killCounts[topPlayerIndex])
                    topPlayerIndex = index;
            }

            if (killCounts[topPlayerIndex] != slotWin)
            {
                totalQuery.AddQuery("match_loses", (object)++player.Statistic.Basic.MatchLoses);
                seasonQuery.AddQuery("match_loses", (object)++player.Statistic.Season.MatchLoses);
            }
            else
            {
                totalQuery.AddQuery("match_wins", (object)++player.Statistic.Basic.MatchWins);
                seasonQuery.AddQuery("match_wins", (object)++player.Statistic.Season.MatchWins);
            }

            totalQuery.AddQuery("matches", (object)++player.Statistic.Basic.Matches);
            totalQuery.AddQuery("total_matches", (object)++player.Statistic.Basic.TotalMatchesCount);
            seasonQuery.AddQuery("matches", (object)++player.Statistic.Season.Matches);
            seasonQuery.AddQuery("total_matches", (object)++player.Statistic.Season.TotalMatchesCount);
        }

        /// <summary>
        /// Actualiza las estadísticas diarias para modo FFA
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="slotWin">Kills del slot ganador</param>
        /// <param name="query">Query de base de datos</param>

        public static void UpdateMatchDailyRecordFFA(RoomModel room, Account player, int slotWin, DBQuery query)
        {
            int[] killCounts = new int[18];
            for (int index = 0; index < killCounts.Length; ++index)
            {
                SlotModel slot = room.Slots[index];
                killCounts[index] = slot.PlayerId != 0L ? slot.AllKills : 0;
            }

            int topPlayerIndex = 0;
            for (int index = 0; index < killCounts.Length; ++index)
            {
                if (killCounts[index] > killCounts[topPlayerIndex])
                    topPlayerIndex = index;
            }

            if (killCounts[topPlayerIndex] != slotWin)
                query.AddQuery("match_loses", (object)++player.Statistic.Daily.MatchLoses);
            else
                query.AddQuery("match_wins", (object)++player.Statistic.Daily.MatchWins);
            query.AddQuery("matches", (object)++player.Statistic.Daily.Matches);
        }

        /// <summary>
        /// Actualiza las estadísticas de armas del jugador
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="slot">Slot del jugador</param>
        /// <param name="query">Query de base de datos</param>

        public static void UpdateWeaponRecord(Account player, SlotModel slot, DBQuery query)
        {
            StatisticWeapon weapon = player.Statistic.Weapon;

            // Rifles de asalto
            if (slot.AR[0] > 0)
                query.AddQuery("assault_rifle_kills", (object)++weapon.AssaultKills);
            if (slot.AR[1] > 0)
                query.AddQuery("assault_rifle_deaths", (object)++weapon.AssaultDeaths);

            // Subfusiles
            if (slot.SMG[0] > 0)
                query.AddQuery("sub_machine_gun_kills", (object)++weapon.SmgKills);
            if (slot.SMG[1] > 0)
                query.AddQuery("sub_machine_gun_deaths", (object)++weapon.SmgDeaths);

            // Rifles de francotirador
            if (slot.SR[0] > 0)
                query.AddQuery("sniper_rifle_kills", (object)++weapon.SniperKills);
            if (slot.SR[1] > 0)
                query.AddQuery("sniper_rifle_deaths", (object)++weapon.SniperDeaths);

            // Escopetas
            if (slot.SG[0] > 0)
                query.AddQuery("shot_gun_kills", (object)++weapon.ShotgunKills);
            if (slot.SG[1] > 0)
                query.AddQuery("shot_gun_deaths", (object)++weapon.ShotgunDeaths);

            // Ametralladoras
            if (slot.MG[0] > 0)
                query.AddQuery("machine_gun_kills", (object)++weapon.MachinegunKills);
            if (slot.MG[1] > 0)
                query.AddQuery("machine_gun_deaths", (object)++weapon.MachinegunDeaths);

            // Escudos
            if (slot.SHD[0] > 0)
                query.AddQuery("shield_kills", (object)++weapon.ShieldKills);
            if (slot.SHD[1] <= 0)
                return;
            query.AddQuery("shield_deaths", (object)++weapon.ShieldDeaths);
        }

        #endregion BATTLE SYSTEM AND TEAM MANAGEMENT

        #region MISSION SYSTEM

        /// <summary>
        /// Genera recompensas por misiones completadas
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="query">Query de base de datos</param>

        public static void GenerateMissionAwards(Account player, DBQuery query)
        {
            try
            {
                PlayerMissions mission = player.Mission;
                int actualMission = mission.ActualMission;
                int currentMissionId = mission.GetCurrentMissionId();
                int currentCard = mission.GetCurrentCard();

                if (currentMissionId <= 0 || mission.SelectedCard)
                    return;

                int completedMissions = 0;
                int currentCardProgress = 0;
                byte[] currentMissionList = mission.GetCurrentMissionList();

                foreach (MissionCardModel card in MissionCardRAW.GetCards(currentMissionId, -1))
                {
                    if ((int)currentMissionList[card.ArrayIdx] >= card.MissionLimit)
                    {
                        ++completedMissions;
                        if (card.CardBasicId == currentCard)
                            ++currentCardProgress;
                    }
                }

                if (completedMissions >= 40)
                {
                    // Misión completamente terminada
                    ProcessFullMissionCompletion(player, query, mission, actualMission, currentMissionId, currentCard);
                }
                else
                {
                    if (currentCardProgress != 4 || mission.SelectedCard)
                        return;
                    // Carta individual completada
                    ProcessCardCompletion(player, query, mission, currentMissionId, currentCard);
                }
            }
            catch (Exception ex)
            {
                CLogger.Print("AllUtils.GenerateMissionAwards: " + ex.Message, LoggerType.Error, ex);
            }
        }

        /// <summary>
        /// Procesa la finalización completa de una misión
        /// </summary>
        private static void ProcessFullMissionCompletion(Account player, DBQuery query, PlayerMissions mission,
            int actualMission, int currentMissionId, int currentCard)
        {
            int masterMedal = player.MasterMedal;
            int ribbon = player.Ribbon;
            int medal = player.Medal;
            int ensign = player.Ensign;

            // Recompensas de carta
            MissionCardAwards cardAward = MissionCardRAW.GetAward(currentMissionId, currentCard);
            if (cardAward != null)
            {
                player.Ribbon += cardAward.Ribbon;
                player.Medal += cardAward.Medal;
                player.Ensign += cardAward.Ensign;
                player.Gold += cardAward.Gold;
                player.Exp += cardAward.Exp;
            }

            // Recompensas de misión
            MissionAwards missionAward = MissionAwardXML.GetAward(currentMissionId);
            if (missionAward != null)
            {
                player.MasterMedal += missionAward.MasterMedal;
                player.Exp += missionAward.Exp;
                player.Gold += missionAward.Gold;
            }

            // Items de recompensa
            List<ItemsModel> missionAwards = MissionCardRAW.GetMissionAwards(currentMissionId);
            if (missionAwards.Count > 0)
                player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, player, missionAwards));

            player.SendPacket(new PROTOCOL_BASE_QUEST_ACTIVE_IDX_CHANGE_ACK(273U, 4, player));

            // Actualizar estadísticas si cambiaron
            if (player.Ribbon != ribbon)
                query.AddQuery("ribbon", (object)player.Ribbon);
            if (player.Ensign != ensign)
                query.AddQuery("ensign", (object)player.Ensign);
            if (player.Medal != medal)
                query.AddQuery("medal", (object)player.Medal);
            if (player.MasterMedal != masterMedal)
                query.AddQuery("master_medal", (object)player.MasterMedal);

            query.AddQuery($"mission_id{actualMission + 1}", (object)0);
            ComDiv.UpdateDB("player_missions", "owner_id", (object)player.PlayerId, new string[2]
            {
                $"card{actualMission + 1}",
                $"mission{actualMission + 1}_raw"
            }, (object)0, (object)new byte[0]);

            // Resetear misión según el slot
            ResetMissionBySlot(mission, actualMission, player);
        }

        /// <summary>
        /// Procesa la finalización de una carta de misión
        /// </summary>
        private static void ProcessCardCompletion(Account player, DBQuery query, PlayerMissions mission,
            int currentMissionId, int currentCard)
        {
            MissionCardAwards award = MissionCardRAW.GetAward(currentMissionId, currentCard);
            if (award != null)
            {
                int ribbon = player.Ribbon;
                int medal = player.Medal;
                int ensign = player.Ensign;

                player.Ribbon += award.Ribbon;
                player.Medal += award.Medal;
                player.Ensign += award.Ensign;
                player.Gold += award.Gold;
                player.Exp += award.Exp;

                if (player.Ribbon != ribbon)
                    query.AddQuery("ribbon", (object)player.Ribbon);
                if (player.Ensign != ensign)
                    query.AddQuery("ensign", (object)player.Ensign);
                if (player.Medal != medal)
                    query.AddQuery("medal", (object)player.Medal);
            }

            mission.SelectedCard = true;
            player.SendPacket(new PROTOCOL_BASE_QUEST_ACTIVE_IDX_CHANGE_ACK(1U, 1, player));
        }

        /// <summary>
        /// Resetea una misión según su slot
        /// </summary>
        private static void ResetMissionBySlot(PlayerMissions mission, int actualMission, Account player)
        {
            switch (actualMission)
            {
                case 0:
                    mission.Mission1 = 0;
                    mission.Card1 = 0;
                    mission.List1 = new byte[40];
                    break;

                case 1:
                    mission.Mission2 = 0;
                    mission.Card2 = 0;
                    mission.List2 = new byte[40];
                    break;

                case 2:
                    mission.Mission3 = 0;
                    mission.Card3 = 0;
                    mission.List3 = new byte[40];
                    break;

                case 3:
                    mission.Mission4 = 0;
                    mission.Card3 = 0;
                    mission.List4 = new byte[40];
                    if (player.Event == null)
                        break;
                    player.Event.LastQuestFinish = 1;
                    ComDiv.UpdateDB("player_events", "last_quest_finish", (object)1, "owner_id", (object)player.PlayerId);
                    break;
            }
        }

        #endregion MISSION SYSTEM

        #region ROOM MANAGEMENT

        /// <summary>
        /// Resetea la información de un slot en una sala
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="slot">Slot a resetear</param>
        /// <param name="updateInfo">Si debe actualizar la información de la sala</param>
        public static void ResetSlotInfo(RoomModel room, SlotModel slot, bool updateInfo)
        {
            if (slot.State < SlotState.LOAD)
                return;
            room.ChangeSlotState(slot, SlotState.NORMAL, updateInfo);
            slot.ResetSlot();
        }

        /// <summary>
        /// Finaliza la misión al terminar una partida
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="slot">Slot del jugador</param>
        /// <param name="winnerTeam">Equipo ganador</param>
        public static void EndMatchMission(RoomModel room, Account player, SlotModel slot, TeamEnum winnerTeam)
        {
            if (winnerTeam == TeamEnum.TEAM_DRAW)
                return;
            ProcessMissionCompletion(room, player, slot, slot.Team == winnerTeam ? MissionType.WIN : MissionType.DEFEAT, 0);
        }

        /// <summary>
        /// Procesa el resultado de una votación de expulsión
        /// </summary>
        /// <param name="room">Sala de juego</param>
        public static void VotekickResult(RoomModel room)
        {
            VoteKickModel voteKick = room.VoteKick;
            if (voteKick != null)
            {
                int inGamePlayers = voteKick.GetInGamePlayers();
                if (voteKick.Accept > voteKick.Denie && voteKick.Enemies > 0 && voteKick.Allies > 0 &&
                    voteKick.Votes.Count >= inGamePlayers / 2)
                {
                    Account playerBySlot = room.GetPlayerBySlot(voteKick.VictimIdx);
                    if (playerBySlot != null)
                    {
                        playerBySlot.SendPacket(new PROTOCOL_BATTLE_NOTIFY_BE_KICKED_BY_KICKVOTE_ACK());
                        room.KickedPlayersVote.Add(playerBySlot.PlayerId);
                        room.RemovePlayer(playerBySlot, true, 2);
                    }
                }

                uint errorCode = 0;
                if (voteKick.Allies != 0)
                {
                    if (voteKick.Enemies != 0)
                    {
                        if (voteKick.Denie < voteKick.Accept || voteKick.Votes.Count < inGamePlayers / 2)
                            errorCode = 2147488000U;
                    }
                    else
                        errorCode = 2147488002U;
                }
                else
                    errorCode = 2147488001U;

                using (PROTOCOL_BATTLE_NOTIFY_KICKVOTE_RESULT_ACK packet = new PROTOCOL_BATTLE_NOTIFY_KICKVOTE_RESULT_ACK(errorCode, voteKick))
                    room.SendPacketToPlayers(packet, SlotState.BATTLE, 0);
            }
            room.VoteKick = (VoteKickModel)null;
        }

        /// <summary>
        /// Resetea toda la información de batalla de una sala
        /// </summary>
        /// <param name="room">Sala a resetear</param>
        public static void ResetBattleInfo(RoomModel room)
        {
            foreach (SlotModel slot in room.Slots)
            {
                if (slot.PlayerId > 0L && slot.State >= SlotState.LOAD)
                {
                    slot.State = SlotState.NORMAL;
                    slot.ResetSlot();
                }
                room.CheckGhostSlot(slot);
            }

            // Resetear flags de batalla
            room.PreMatchCD = false;
            room.BlockedClan = false;
            room.SwapRound = false;
            room.Rounds = 1;
            room.SpawnsCount = 0;

            // Resetear estadísticas
            room.FRKills = 0;
            room.FRAssists = 0;
            room.FRDeaths = 0;
            room.CTKills = 0;
            room.CTAssists = 0;
            room.CTDeaths = 0;
            room.FRDino = 0;
            room.CTDino = 0;
            room.FRRounds = 0;
            room.CTRounds = 0;
            room.BattleStart = new DateTime();
            room.TimeRoom = 0U;
            room.Bar1 = 0;
            room.Bar2 = 0;
            room.IngameAiLevel = (byte)0;
            room.State = RoomState.READY;
            room.UpdateRoomInfo();

            // Limpiar objetos especiales
            room.VoteKick = (VoteKickModel)null;
            room.UdpServer = (Synchronize)null;

            // Detener timers
            if (room.RoundTime.IsTimer())
                room.RoundTime.StopJob();
            if (room.VoteTime.IsTimer())
                room.VoteTime.StopJob();
            if (room.BombTime.IsTimer())
                room.BombTime.StopJob();

            room.UpdateSlotsInfo();
        }

        #endregion ROOM MANAGEMENT

        #region DINOSAUR MODE MANAGEMENT

        /// <summary>
        /// Obtiene la lista de dinosaurios para el modo Dino
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="forceNewTRex">Forzar nuevo T-Rex</param>
        /// <param name="forceRexIdx">Índice forzado para T-Rex</param>
        /// <returns>Lista de índices de dinosaurios</returns>

        public static List<int> GetDinossaurs(RoomModel room, bool forceNewTRex, int forceRexIdx)
        {
            List<int> dinosaurs = new List<int>();
            if (room.IsDinoMode())
            {
                TeamEnum team = room.Rounds == 1 ? TeamEnum.FR_TEAM : TeamEnum.CT_TEAM;
                foreach (int teamIndex in room.GetTeamArray(team))
                {
                    SlotModel slot = room.Slots[teamIndex];
                    if (slot.State == SlotState.BATTLE && !slot.SpecGM)
                        dinosaurs.Add(teamIndex);
                }

                if (((room.TRex == -1 ? 1 : (room.Slots[room.TRex].State <= SlotState.BATTLE_LOAD ? 1 : 0)) |
                    (forceNewTRex ? 1 : 0)) != 0 && dinosaurs.Count > 1 && room.IsDinoMode("DE"))
                {
                    if (forceRexIdx >= 0 && dinosaurs.Contains(forceRexIdx))
                        room.TRex = forceRexIdx;
                    else if (forceRexIdx == -2)
                        room.TRex = dinosaurs[new Random().Next(0, dinosaurs.Count)];
                }
            }
            return dinosaurs;
        }

        #endregion DINOSAUR MODE MANAGEMENT

        #region BATTLE END CONDITIONS

        /// <summary>
        /// Verifica las condiciones de final de batalla por número de jugadores
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="isBotMode">Si es modo contra bots</param>
        public static void BattleEndPlayersCount(RoomModel room, bool isBotMode)
        {
            if (room == null | isBotMode || !room.IsPreparing())
                return;

            int ctInBattle = 0;
            int frInBattle = 0;
            int ctLoading = 0;
            int frLoading = 0;

            foreach (SlotModel slot in room.Slots)
            {
                // Skip observers - they should not affect battle end conditions
                if (slot.SpecGM)
                    continue;
                    
                if (slot.State == SlotState.BATTLE)
                {
                    if (slot.Team == TeamEnum.FR_TEAM)
                        ++frInBattle;
                    else
                        ++ctInBattle;
                }
                else if (slot.State >= SlotState.LOAD)
                {
                    if (slot.Team == TeamEnum.FR_TEAM)
                        ++frLoading;
                    else
                        ++ctLoading;
                }
            }

            if ((frInBattle != 0 && ctInBattle != 0 || room.State != RoomState.BATTLE) &&
                (frLoading != 0 && ctLoading != 0 || room.State > RoomState.PRE_BATTLE))
                return;

            EndBattle(room, isBotMode);
        }

        /// <summary>
        /// Finaliza una batalla
        /// </summary>
        /// <param name="room">Sala de juego</param>
        public static void EndBattle(RoomModel room) => EndBattle(room, room.IsBotMode());

        /// <summary>
        /// Finaliza una batalla con modo específico
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="isBotMode">Si es modo contra bots</param>
        public static void EndBattle(RoomModel room, bool isBotMode)
        {
            EndBattle(room, isBotMode, GetWinnerTeam(room));
        }

        /// <summary>
        /// Finaliza una batalla sin otorgar puntos
        /// </summary>
        /// <param name="room">Sala de juego</param>
        public static void EndBattleNoPoints(RoomModel room)
        {
            List<Account> allPlayers = room.GetAllPlayers(SlotState.READY, 1);
            if (allPlayers.Count > 0)
            {
                int missionFlag;
                int slotFlag;
                byte[] data;
                GetBattleResult(room, out missionFlag, out slotFlag, out data);
                bool isBotMode = room.IsBotMode();

                foreach (Account account in allPlayers)
                {
                    account.SendPacket(new PROTOCOL_BATTLE_ENDBATTLE_ACK(account, TeamEnum.TEAM_DRAW, slotFlag, missionFlag, isBotMode, data));
                    UpdateSeasonPass(account);
                }
            }
            ResetBattleInfo(room);
        }

        /// <summary>
        /// Finaliza una batalla con equipo ganador específico
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="isBotMode">Si es modo contra bots</param>
        /// <param name="winnerTeam">Equipo ganador</param>
        public static void EndBattle(RoomModel room, bool isBotMode, TeamEnum winnerTeam)
        {
            List<Account> allPlayers = room.GetAllPlayers(SlotState.READY, 1);
            if (allPlayers.Count > 0)
            {
                room.CalculateResult(winnerTeam, isBotMode);
                int missionFlag;
                int slotFlag;
                byte[] data;
                GetBattleResult(room, out missionFlag, out slotFlag, out data);

                foreach (Account account in allPlayers)
                {
                    account.SendPacket(new PROTOCOL_BATTLE_ENDBATTLE_ACK(account, winnerTeam, slotFlag, missionFlag, isBotMode, data));
                    UpdateSeasonPass(account);
                }
            }
            ResetBattleInfo(room);
        }

        /// <summary>
        /// Finaliza una ronda de batalla
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="winner">Equipo ganador</param>
        /// <param name="forceRestart">Forzar reinicio</param>
        /// <param name="kills">Información de kills</param>
        /// <param name="killer">Slot del matador</param>
        public static void BattleEndRound(RoomModel room, TeamEnum winner, bool forceRestart, FragInfos kills, SlotModel killer)
        {
            int roundsByMask = room.GetRoundsByMask();
            if (room.FRRounds < roundsByMask && room.CTRounds < roundsByMask)
            {
                if (!(!room.ActiveC4 | forceRestart))
                    return;

                room.StopBomb();
                ++room.Rounds;
                RoundSync.SendUDPRoundSync(room);

                using (PROTOCOL_BATTLE_WINNING_CAM_ACK packet1 = new PROTOCOL_BATTLE_WINNING_CAM_ACK(kills, killer))
                {
                    using (PROTOCOL_BATTLE_MISSION_ROUND_END_ACK packet2 = new PROTOCOL_BATTLE_MISSION_ROUND_END_ACK(room, winner, RoundEndType.AllDeath))
                        room.SendPacketToPlayers(packet1, packet2, SlotState.BATTLE, 0);
                }
                room.RoundRestart();
            }
            else
            {
                room.StopBomb();
                using (PROTOCOL_BATTLE_WINNING_CAM_ACK packet1 = new PROTOCOL_BATTLE_WINNING_CAM_ACK(kills, killer))
                {
                    using (PROTOCOL_BATTLE_MISSION_ROUND_END_ACK packet2 = new PROTOCOL_BATTLE_MISSION_ROUND_END_ACK(room, winner, RoundEndType.AllDeath))
                        room.SendPacketToPlayers(packet1, packet2, SlotState.BATTLE, 0);
                }
                EndBattle(room, room.IsBotMode(), winner);
            }
        }

        /// <summary>
        /// Finaliza una ronda con motivo específico
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="winner">Equipo ganador</param>
        /// <param name="motive">Motivo del final</param>
        public static void BattleEndRound(RoomModel room, TeamEnum winner, RoundEndType motive)
        {
            using (PROTOCOL_BATTLE_MISSION_ROUND_END_ACK packet =
                new PROTOCOL_BATTLE_MISSION_ROUND_END_ACK(room, winner, motive))
                room.SendPacketToPlayers(packet, SlotState.BATTLE, 0);

            room.StopBomb();
            int roundsByMask = room.GetRoundsByMask();

            if (room.FRRounds < roundsByMask && room.CTRounds < roundsByMask)
            {
                ++room.Rounds;

                RoundSync.SendUDPRoundSync(room);

                room.RoundRestart();
            }
            else
                EndBattle(room, room.IsBotMode(), winner);
        }

        #endregion BATTLE END CONDITIONS

        #region FRIEND SYSTEM

        /// <summary>
        /// Agrega un amigo a la lista del jugador
        /// </summary>
        /// <param name="owner">Jugador que agrega</param>
        /// <param name="friend">Jugador a agregar</param>
        /// <param name="state">Estado de la amistad</param>
        /// <returns>Código de resultado</returns>

        public static int AddFriend(Account owner, Account friend, int state)
        {
            if (owner != null)
            {
                if (friend != null)
                {
                    try
                    {
                        FriendModel existingFriend = owner.Friend.GetFriend(friend.PlayerId);
                        if (existingFriend == null)
                        {
                            using (NpgsqlConnection connection = ConnectionSQL.GetInstance().Conn())
                            {
                                NpgsqlCommand command = connection.CreateCommand();
                                connection.Open();
                                command.CommandType = CommandType.Text;
                                command.Parameters.AddWithValue("@friend", (object)friend.PlayerId);
                                command.Parameters.AddWithValue("@owner", (object)owner.PlayerId);
                                command.Parameters.AddWithValue("@state", (object)state);
                                command.CommandText = "INSERT INTO player_friends (id, owner_id, state) VALUES (@friend, @owner, @state)";
                                command.ExecuteNonQuery();
                                command.Dispose();
                                connection.Dispose();
                                connection.Close();
                            }

                            lock (owner.Friend.Friends)
                            {
                                FriendModel newFriend = new FriendModel(friend.PlayerId, friend.Rank, friend.NickColor, friend.Nickname, friend.IsOnline, friend.Status)
                                {
                                    State = state,
                                    Removed = false
                                };
                                owner.Friend.Friends.Add(newFriend);
                                SendFriendInfo.Load(owner, newFriend, 0);
                            }
                            return 0;
                        }

                        if (existingFriend.Removed)
                        {
                            existingFriend.Removed = false;
                            DaoManagerSQL.UpdatePlayerFriendBlock(owner.PlayerId, existingFriend);
                            SendFriendInfo.Load(owner, existingFriend, 1);
                        }
                        return 1;
                    }
                    catch (Exception ex)
                    {
                        CLogger.Print("AllUtils.AddFriend: " + ex.Message, LoggerType.Error, ex);
                        return -1;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Sincroniza la información de un jugador con sus amigos
        /// </summary>
        /// <param name="player">Jugador a sincronizar</param>
        /// <param name="all">Si debe sincronizar con todos los amigos</param>
        public static void SyncPlayerToFriends(Account player, bool all)
        {
            if (player == null || player.Friend.Friends.Count == 0)
                return;

            PlayerInfo playerInfo = new PlayerInfo(player.PlayerId, player.Rank, player.NickColor, player.Nickname, player.IsOnline, player.Status);

            for (int index1 = 0; index1 < player.Friend.Friends.Count; ++index1)
            {
                FriendModel friend1 = player.Friend.Friends[index1];
                if (all || friend1.State == 0 && !friend1.Removed)
                {
                    Account account = AccountManager.GetAccount(friend1.PlayerId, 287);
                    if (account != null)
                    {
                        int index2 = -1;
                        FriendModel friend2 = account.Friend.GetFriend(player.PlayerId, out index2);
                        if (friend2 != null)
                        {
                            friend2.Info = playerInfo;
                            account.SendPacket(new PROTOCOL_AUTH_FRIEND_INFO_CHANGE_ACK(FriendChangeState.Update, friend2, index2), false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sincroniza la información de un jugador con los miembros de su clan
        /// </summary>
        /// <param name="player">Jugador a sincronizar</param>
        public static void SyncPlayerToClanMembers(Account player)
        {
            if (player == null || player.ClanId <= 0)
                return;

            using (PROTOCOL_CS_MEMBER_INFO_CHANGE_ACK packet = new PROTOCOL_CS_MEMBER_INFO_CHANGE_ACK(player))
                ClanManager.SendPacket(packet, player.ClanId, player.PlayerId, true, true);
        }

        #endregion FRIEND SYSTEM

        #region EQUIPMENT AND INVENTORY MANAGEMENT

        /// <summary>
        /// Actualiza el equipamiento del slot del jugador
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        public static void UpdateSlotEquips(Account player)
        {
            RoomModel room = player.Room;
            if (room == null)
                return;
            UpdateSlotEquips(player, room);
        }

        /// <summary>
        /// Actualiza el equipamiento del slot del jugador en una sala específica
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="room">Sala específica</param>
        public static void UpdateSlotEquips(Account player, RoomModel room)
        {
            SlotModel slot;
            if (room.GetSlot(player.SlotId, out slot))
                slot.Equipment = player.Equipment;
            room.UpdateSlotsInfo();
        }

        /// <summary>
        /// Obtiene las flags de slots para resultados de batalla
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="onlyNoSpectators">Solo contar no espectadores</param>
        /// <param name="missionSuccess">Si las misiones fueron exitosas</param>
        /// <returns>Flags combinadas de slots</returns>
        public static int GetSlotsFlag(RoomModel room, bool onlyNoSpectators, bool missionSuccess)
        {
            if (room == null)
                return 0;

            int slotsFlag = 0;
            foreach (SlotModel slot in room.Slots)
            {
                if (slot.State >= SlotState.LOAD &&
                    (missionSuccess && slot.MissionsCompleted ||
                     !missionSuccess && (!onlyNoSpectators || !slot.Spectator)))
                    slotsFlag += slot.Flag;
            }
            return slotsFlag;
        }

        /// <summary>
        /// Obtiene los datos de resultado de batalla
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="missionFlag">Flag de misiones (salida)</param>
        /// <param name="slotFlag">Flag de slots (salida)</param>
        /// <param name="data">Datos binarios (salida)</param>
        public static void GetBattleResult(RoomModel room, out int missionFlag, out int slotFlag, out byte[] data)
        {
            missionFlag = 0;
            slotFlag = 0;
            data = new byte[306];

            if (room == null)
                return;

            using (SyncServerPacket syncServerPacket = new SyncServerPacket())
            {
                foreach (SlotModel slot in room.Slots)
                {
                    if (slot.State >= SlotState.LOAD)
                    {
                        int flag = slot.Flag;
                        if (slot.MissionsCompleted)
                            missionFlag += flag;
                        slotFlag += flag;
                    }
                }

                // Escribir experiencia
                foreach (SlotModel slot in room.Slots)
                    syncServerPacket.WriteH((ushort)slot.Exp);

                // Escribir oro
                foreach (SlotModel slot in room.Slots)
                    syncServerPacket.WriteH((ushort)slot.Gold);

                // Escribir flags de bonus
                foreach (SlotModel slot in room.Slots)
                    syncServerPacket.WriteC((byte)slot.BonusFlags);

                // Escribir bonos de experiencia
                foreach (SlotModel slot in room.Slots)
                {
                    syncServerPacket.WriteH((ushort)slot.BonusCafeExp);
                    syncServerPacket.WriteH((ushort)slot.BonusItemExp);
                    syncServerPacket.WriteH((ushort)slot.BonusEventExp);
                }

                // Escribir bonos de puntos
                foreach (SlotModel slot in room.Slots)
                {
                    syncServerPacket.WriteH((ushort)slot.BonusCafePoint);
                    syncServerPacket.WriteH((ushort)slot.BonusItemPoint);
                    syncServerPacket.WriteH((ushort)slot.BonusEventPoint);
                }

                data = syncServerPacket.ToArray();
            }
        }

        /// <summary>
        /// Aplica desgaste a los ítems durables del jugador
        /// </summary>
        /// <param name="slot">Slot del jugador</param>
        /// <param name="player">Cuenta del jugador</param>
        /// <returns>True si se procesó correctamente</returns>

        public static bool DiscountPlayerItems(SlotModel slot, Account player)
        {
            try
            {
                bool inventoryChanged = false;
                bool effectsChanged = false;
                uint currentTime = Convert.ToUInt32(DateTimeUtil.Now("yyMMddHHmm"));
                List<ItemsModel> updatedItems = new List<ItemsModel>();
                List<object> itemsToDelete = new List<object>();

                int originalBonuses = player.Bonus != null ? player.Bonus.Bonuses : 0;
                int originalFreePass = player.Bonus != null ? player.Bonus.FreePass : 0;

                lock (player.Inventory.Items)
                {
                    for (int index = 0; index < player.Inventory.Items.Count; ++index)
                    {
                        ItemsModel item = player.Inventory.Items[index];

                        // Procesar ítems durables usados en batalla
                        if (item.Equip == ItemEquipType.Durable && slot.ItemUsages.Contains(item.Id) && !slot.SpecGM)
                        {
                            // Ítems especiales que se eliminan al expirar
                            if (item.Count <= currentTime && (item.Id == 800216 || item.Id == 2700013 || item.Id == 800169))
                                DaoManagerSQL.DeletePlayerInventoryItem(item.ObjectId, player.PlayerId);

                            if (--item.Count >= 1U)
                            {
                                updatedItems.Add(item);
                                ComDiv.UpdateDB("player_items", "count", (object)(long)item.Count, "object_id", (object)item.ObjectId, "owner_id", (object)player.PlayerId);
                            }
                            else
                            {
                                itemsToDelete.Add((object)item.ObjectId);
                                player.Inventory.Items.RemoveAt(index--);
                            }
                        }
                        // Procesar ítems temporales expirados
                        else if (item.Count <= currentTime && item.Equip == ItemEquipType.Temporary)
                        {
                            if (item.Category == ItemCategory.Coupon)
                            {
                                ProcessExpiredCoupon(player, item, inventoryChanged, effectsChanged);
                            }
                            else
                                continue;

                            itemsToDelete.Add((object)item.ObjectId);
                            player.Inventory.Items.RemoveAt(index--);
                        }
                        // Limpiar ítems con contador en 0
                        else if (item.Count == 0U)
                        {
                            itemsToDelete.Add((object)item.ObjectId);
                            player.Inventory.Items.RemoveAt(index--);
                        }
                    }
                }

                // Procesar eliminaciones
                ProcessItemDeletions(player, itemsToDelete);

                // Actualizar bonos si cambiaron
                if (player.Bonus != null && (player.Bonus.Bonuses != originalBonuses || player.Bonus.FreePass != originalFreePass))
                    DaoManagerSQL.UpdatePlayerBonus(player.PlayerId, player.Bonus.Bonuses, player.Bonus.FreePass);

                // Corregir efectos negativos
                if (player.Effects < (CouponEffects)0)
                    player.Effects = (CouponEffects)0;

                // Enviar actualizaciones al cliente
                if (updatedItems.Count > 0)
                    player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(1, player, updatedItems));

                if (effectsChanged)
                    ComDiv.UpdateDB("accounts", "coupon_effect", (object)(long)player.Effects, "player_id", (object)player.PlayerId);

                if (inventoryChanged)
                    player.SendPacket(new PROTOCOL_BASE_INV_ITEM_DATA_ACK(0, player));

                // Verificar equipamiento
                ValidateEquipmentAfterItemChanges(player, slot);

                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        /// <summary>
        /// Procesa la expiración de un cupón
        /// </summary>
        private static void ProcessExpiredCoupon(Account player, ItemsModel item, bool inventoryChanged, bool effectsChanged)
        {
            if (player.Bonus != null)
            {
                if (!player.Bonus.RemoveBonuses(item.Id))
                {
                    ProcessSpecialCouponExpiration(player, item, inventoryChanged);
                }

                CouponFlag couponEffect = CouponEffectXML.GetCouponEffect(item.Id);
                if (couponEffect != null && couponEffect.EffectFlag > (CouponEffects)0 &&
                    player.Effects.HasFlag((Enum)couponEffect.EffectFlag))
                {
                    player.Effects -= couponEffect.EffectFlag;
                    effectsChanged = true;
                }
            }
        }

        /// <summary>
        /// Procesa la expiración de cupones especiales
        /// </summary>
        private static void ProcessSpecialCouponExpiration(Account player, ItemsModel item, bool inventoryChanged)
        {
            switch (item.Id)
            {
                case 1600014: // Crosshair color
                    ComDiv.UpdateDB("player_bonus", "crosshair_color", (object)4, "owner_id", (object)player.PlayerId);
                    player.Bonus.CrosshairColor = 4;
                    inventoryChanged = true;
                    break;

                case 1600006: // Nick color
                    ComDiv.UpdateDB("accounts", "nick_color", (object)0, "player_id", (object)player.PlayerId);
                    player.NickColor = 0;
                    if (player.Room != null)
                    {
                        using (PROTOCOL_ROOM_GET_COLOR_NICK_ACK packet = new PROTOCOL_ROOM_GET_COLOR_NICK_ACK(player.SlotId, player.NickColor))
                            player.Room.SendPacketToPlayers(packet);
                        player.Room.UpdateSlotsInfo();
                    }
                    inventoryChanged = true;
                    break;

                case 1600009: // Fake rank
                    ComDiv.UpdateDB("player_bonus", "fake_rank", (object)55, "owner_id", (object)player.PlayerId);
                    player.Bonus.FakeRank = 55;
                    if (player.Room != null)
                    {
                        using (PROTOCOL_ROOM_GET_RANK_ACK packet = new PROTOCOL_ROOM_GET_RANK_ACK(player.SlotId, player.Rank))
                            player.Room.SendPacketToPlayers(packet);
                        player.Room.UpdateSlotsInfo();
                    }
                    inventoryChanged = true;
                    break;

                case 1600010: // Fake nick
                    ComDiv.UpdateDB("player_bonus", "fake_nick", (object)"", "owner_id", (object)player.PlayerId);
                    ComDiv.UpdateDB("accounts", "nickname", (object)player.Bonus.FakeNick, "player_id", (object)player.PlayerId);
                    player.Nickname = player.Bonus.FakeNick;
                    player.Bonus.FakeNick = "";
                    if (player.Room != null)
                    {
                        using (PROTOCOL_ROOM_GET_NICKNAME_ACK packet = new PROTOCOL_ROOM_GET_NICKNAME_ACK(player.SlotId, player.Nickname))
                            player.Room.SendPacketToPlayers(packet);
                        player.Room.UpdateSlotsInfo();
                    }
                    inventoryChanged = true;
                    break;

                case 1600187: // Muzzle color
                    ComDiv.UpdateDB("player_bonus", "muzzle_color", (object)0, "owner_id", (object)player.PlayerId);
                    player.Bonus.MuzzleColor = 0;
                    if (player.Room != null)
                    {
                        using (PROTOCOL_ROOM_GET_COLOR_MUZZLE_FLASH_ACK packet = new PROTOCOL_ROOM_GET_COLOR_MUZZLE_FLASH_ACK(player.SlotId, player.Bonus.MuzzleColor))
                            player.Room.SendPacketToPlayers(packet);
                    }
                    inventoryChanged = true;
                    break;

                case 1600205: // Nick border color
                    ComDiv.UpdateDB("player_bonus", "nick_border_color", (object)0, "owner_id", (object)player.PlayerId);
                    player.Bonus.NickBorderColor = 0;
                    if (player.Room != null)
                    {
                        using (PROTOCOL_ROOM_GET_NICK_OUTLINE_COLOR_ACK packet = new PROTOCOL_ROOM_GET_NICK_OUTLINE_COLOR_ACK(player.SlotId, player.Bonus.NickBorderColor))
                            player.Room.SendPacketToPlayers(packet);
                    }
                    inventoryChanged = true;
                    break;
            }
        }

        /// <summary>
        /// Procesa las eliminaciones de ítems
        /// </summary>
        private static void ProcessItemDeletions(Account player, List<object> itemsToDelete)
        {
            if (itemsToDelete.Count > 0)
            {
                for (int index = 0; index < itemsToDelete.Count; ++index)
                {
                    ItemsModel item = player.Inventory.GetItem((long)itemsToDelete[index]);
                    if (item != null && item.Category == ItemCategory.Character && ComDiv.GetIdStatics(item.Id, 1) == 6)
                        RemoveCharacterItem(player, item.Id);
                    player.SendPacket(new PROTOCOL_AUTH_SHOP_DELETE_ITEM_ACK(1U, (long)itemsToDelete[index]));
                }
                ComDiv.DeleteDB("player_items", "object_id", itemsToDelete.ToArray(), "owner_id", (object)player.PlayerId);
            }
        }

        /// <summary>
        /// Valida el equipamiento después de cambios en ítems
        /// </summary>
        private static void ValidateEquipmentAfterItemChanges(Account player, SlotModel slot)
        {
            int equipmentChanges = ComDiv.CheckEquipedItems(player.Equipment, player.Inventory.Items, false);
            if (equipmentChanges > 0)
            {
                DBQuery query = new DBQuery();
                if ((equipmentChanges & 2) == 2)
                    ComDiv.UpdateWeapons(player.Equipment, query);
                if ((equipmentChanges & 1) == 1)
                    ComDiv.UpdateChars(player.Equipment, query);
                if ((equipmentChanges & 3) == 3)
                    ComDiv.UpdateItems(player.Equipment, query);

                ComDiv.UpdateDB("player_equipments", "owner_id", (object)player.PlayerId, query.GetTables(), query.GetValues());
                player.SendPacket(new PROTOCOL_SERVER_MESSAGE_CHANGE_INVENTORY_ACK(player, slot));
                slot.Equipment = player.Equipment;
            }
        }

        /// <summary>
        /// Remueve un personaje del jugador
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="characterId">ID del personaje a remover</param>
        private static void RemoveCharacterItem(Account player, int characterId)
        {
            CharacterModel character = player.Character.GetCharacter(characterId);
            if (character == null)
                return;

            int slot = 0;
            foreach (CharacterModel otherCharacter in player.Character.Characters)
            {
                if (otherCharacter.Slot != character.Slot)
                {
                    otherCharacter.Slot = slot;
                    DaoManagerSQL.UpdatePlayerCharacter(slot, otherCharacter.ObjectId, player.PlayerId);
                    ++slot;
                }
            }

            if (!DaoManagerSQL.DeletePlayerCharacter(character.ObjectId, player.PlayerId))
                return;
            player.Character.RemoveCharacter(character);
        }

        #endregion EQUIPMENT AND INVENTORY MANAGEMENT

        #region TEAM BALANCE SYSTEM

        /// <summary>
        /// Intenta balancear un jugador entre equipos
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="player">Jugador a balancear</param>
        /// <param name="inBattle">Si está en batalla</param>
        /// <param name="mySlot">Slot del jugador (erencia)</param>
        public static void TryBalancePlayer(RoomModel room, Account player, bool inBattle, SlotModel mySlot)
        {
            SlotModel playerSlot = room.GetSlot(player.SlotId);
            if (playerSlot == null)
                return;

            TeamEnum currentTeam = playerSlot.Team;
            TeamEnum balanceTeamIdx = GetBalanceTeamIdx(room, inBattle, currentTeam);

            if (currentTeam == balanceTeamIdx || balanceTeamIdx == TeamEnum.ALL_TEAM)
                return;

            SlotModel targetSlot = null;
            foreach (int index in currentTeam == TeamEnum.CT_TEAM ? room.FR_TEAM : room.CT_TEAM)
            {
                SlotModel slot = room.Slots[index];
                if (slot.State != SlotState.CLOSE && slot.PlayerId == 0L)
                {
                    targetSlot = slot;
                    break;
                }
            }

            if (targetSlot == null)
                return;

            List<SlotModel> changedSlots = new List<SlotModel>();
            lock (room.Slots)
                room.SwitchSlots(changedSlots, targetSlot.Id, playerSlot.Id, false);

            if (changedSlots.Count <= 0)
                return;

            player.SlotId = playerSlot.Id;
            mySlot = playerSlot;

            using (PROTOCOL_ROOM_TEAM_BALANCE_ACK packet = new PROTOCOL_ROOM_TEAM_BALANCE_ACK(changedSlots, room.Leader, 1))
                room.SendPacketToPlayers(packet);
            room.UpdateSlotsInfo();
        }

        /// <summary>
        /// Obtiene el equipo que necesita ser balanceado
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="inBattle">Si está en batalla</param>
        /// <param name="playerTeamIdx">Equipo del jugador</param>
        /// <returns>Equipo objetivo para balance</returns>
        public static TeamEnum GetBalanceTeamIdx(RoomModel room, bool inBattle, TeamEnum playerTeamIdx)
        {
            int frCount = !inBattle || playerTeamIdx != TeamEnum.FR_TEAM ? 0 : 1;
            int ctCount = !inBattle || playerTeamIdx != TeamEnum.CT_TEAM ? 0 : 1;

            foreach (SlotModel slot in room.Slots)
            {
                if (slot.State == SlotState.NORMAL && !inBattle || slot.State >= SlotState.LOAD & inBattle)
                {
                    if (slot.Team == TeamEnum.FR_TEAM)
                        ++frCount;
                    else
                        ++ctCount;
                }
            }

            if (frCount + 1 < ctCount)
                return TeamEnum.FR_TEAM;
            return ctCount + 1 < frCount + 1 ? TeamEnum.CT_TEAM : TeamEnum.ALL_TEAM;
        }

        /// <summary>
        /// Obtiene el nuevo ID de slot para intercambio
        /// </summary>
        /// <param name="slotIdx">Índice del slot actual</param>
        /// <returns>Nuevo índice de slot</returns>
        public static int GetNewSlotId(int slotIdx) => slotIdx % 2 != 0 ? slotIdx - 1 : slotIdx + 1;

        #endregion TEAM BALANCE SYSTEM

        #region EVENT SYSTEM

        /// <summary>
        /// Obtiene recompensas de evento de Navidad
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void GetXmasReward(Account player)
        {
            EventXmasModel runningEvent = EventXmasXML.GetRunningEvent();
            if (runningEvent == null)
                return;

            PlayerEvent playerEvent = player.Event;
            uint currentTime = uint.Parse(DateTimeUtil.Now("yyMMddHHmm"));

            if (playerEvent == null ||
                playerEvent.LastXmasDate > runningEvent.BeginDate && playerEvent.LastXmasDate <= runningEvent.EndedDate ||
                !ComDiv.UpdateDB("player_events", "last_xmas_date", (object)(long)currentTime, "owner_id", (object)player.PlayerId))
                return;

            playerEvent.LastXmasDate = currentTime;
            GoodsItem good = ShopManager.GetGood(runningEvent.GoodId);
            if (good == null)
                return;

            if (ComDiv.GetIdStatics(good.Item.Id, 1) == 6 && player.Character.GetCharacter(good.Item.Id) == null)
                CreateCharacter(player, good.Item);
            else
                player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, player, good.Item));

            player.SendPacket(new PROTOCOL_BASE_NEW_REWARD_POPUP_ACK(player, good.Item));
        }

        /// <summary>
        /// Intercambia estadísticas de equipos si es necesario
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="playerFR">Jugadores FR (erencia)</param>
        /// <param name="playerCT">Jugadores CT (erencia)</param>
        /// <param name="deathFR">Muertes FR (erencia)</param>
        /// <param name="deathCT">Muertes CT (erencia)</param>
        private static void SwapTeamStatsIfNeeded(RoomModel room, int playerFR, int playerCT, int deathFR, int deathCT)
        {
            if (!room.IsTeamSwap() || !room.SwapRound)
                return;

            int tempPlayerFR = playerFR;
            int tempPlayerCT = playerCT;
            playerCT = tempPlayerFR;
            playerFR = tempPlayerCT;

            int tempDeathFR = deathFR;
            int tempDeathCT = deathCT;
            deathCT = tempDeathFR;
            deathFR = tempDeathCT;
        }

        #endregion EVENT SYSTEM

        #region ROUND END CONDITIONS

        /// <summary>
        /// Verifica las condiciones de final de ronda por número de jugadores
        /// </summary>
        /// <param name="room">Sala de juego</param>
        public static void BattleEndRoundPlayersCount(RoomModel room)
        {
            if (room.RoundTime.IsTimer() ||
                room.RoomType != RoomCondition.Bomb && room.RoomType != RoomCondition.Annihilation &&
                room.RoomType != RoomCondition.Convoy && room.RoomType != RoomCondition.Ace)
                return;

            int playerFR;
            int playerCT;
            int deathFR;
            int deathCT;
            room.GetPlayingPlayers(true, out playerFR, out playerCT, out deathFR, out deathCT);
            SwapTeamStatsIfNeeded(room, playerFR, playerCT, deathFR, deathCT);

            if (deathFR == playerFR)
            {
                if (!room.ActiveC4)
                {
                    if (room.IsTeamSwap() && room.SwapRound)
                        ++room.FRRounds;
                    else
                        ++room.CTRounds;
                }
                BattleEndRound(room, TeamEnum.CT_TEAM, false, null, null);
            }
            else
            {
                if (deathCT != playerCT)
                    return;

                if (room.IsTeamSwap() && room.SwapRound)
                    ++room.CTRounds;
                else
                    ++room.FRRounds;
                BattleEndRound(room, TeamEnum.FR_TEAM, true, null, null);
            }
        }

        /// <summary>
        /// Verifica las condiciones de final de batalla por kills
        /// </summary>
        /// <param name="room">Sala de juego</param>
        public static void BattleEndKills(RoomModel room)
        {
            CheckBattleEndByKills(room, room.IsBotMode());
        }

        /// <summary>
        /// Verifica las condiciones de final de batalla por kills (implementación)
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="isBotMode">Si es modo contra bots</param>

        public static void CheckBattleEndByKills(RoomModel room, bool isBotMode)
        {
            int killsByMask = room.GetKillsByMask();
            if (room.FRKills < killsByMask && room.CTKills < killsByMask)
                return;

            List<Account> allPlayers = room.GetAllPlayers(SlotState.READY, 1);
            if (allPlayers.Count > 0)
            {
                TeamEnum winnerTeam = GetWinnerTeam(room);
                room.CalculateResult(winnerTeam, isBotMode);

                int missionFlag;
                int slotFlag;
                byte[] data;
                GetBattleResult(room, out missionFlag, out slotFlag, out data);

                using (PROTOCOL_BATTLE_MISSION_ROUND_END_ACK roundEndAck = new PROTOCOL_BATTLE_MISSION_ROUND_END_ACK(room, winnerTeam, RoundEndType.TimeOut))
                {
                    byte[] completeBytes = roundEndAck.GetCompleteBytes("AllUtils.BaseEndByKills");
                    foreach (Account account in allPlayers)
                    {
                        SlotModel slot = room.GetSlot(account.SlotId);
                        if (slot != null)
                        {
                            if (slot.State == SlotState.BATTLE)
                                account.SendCompletePacket(completeBytes, roundEndAck.GetType().Name);
                            account.SendPacket(new PROTOCOL_BATTLE_ENDBATTLE_ACK(account, winnerTeam, slotFlag, missionFlag, isBotMode, data));
                            UpdateSeasonPass(account);
                        }
                    }
                }
            }
            ResetBattleInfo(room);
        }

        /// <summary>
        /// Verifica las condiciones de final de batalla por kills en modo FFA
        /// </summary>
        /// <param name="room">Sala de juego</param>

        private static void CheckBattleEndByKillsFFA(RoomModel room)
        {
            int killsByMask = room.GetKillsByMask();
            int[] killCounts = new int[18];

            for (int index = 0; index < killCounts.Length; ++index)
            {
                SlotModel slot = room.Slots[index];
                killCounts[index] = slot.PlayerId == 0L ? 0 : slot.AllKills;
            }

            int topPlayerIndex = 0;
            for (int index = 0; index < killCounts.Length; ++index)
            {
                if (killCounts[index] > killCounts[topPlayerIndex])
                    topPlayerIndex = index;
            }

            if (killCounts[topPlayerIndex] < killsByMask)
                return;

            List<Account> allPlayers = room.GetAllPlayers(SlotState.READY, 1);
            if (allPlayers.Count > 0)
            {
                room.CalculateResultFreeForAll(topPlayerIndex);
                int missionFlag;
                int slotFlag;
                byte[] data;
                GetBattleResult(room, out missionFlag, out slotFlag, out data);

                using (PROTOCOL_BATTLE_MISSION_ROUND_END_ACK roundEndAck = new PROTOCOL_BATTLE_MISSION_ROUND_END_ACK(room, topPlayerIndex, RoundEndType.FreeForAll))
                {
                    byte[] completeBytes = roundEndAck.GetCompleteBytes("AllUtils.BaseEndByKills");
                    foreach (Account account in allPlayers)
                    {
                        SlotModel slot = room.GetSlot(account.SlotId);
                        if (slot != null)
                        {
                            if (slot.State == SlotState.BATTLE)
                                account.SendCompletePacket(completeBytes, roundEndAck.GetType().Name);
                            account.SendPacket(new PROTOCOL_BATTLE_ENDBATTLE_ACK(account, topPlayerIndex, slotFlag, missionFlag, false, data));
                            UpdateSeasonPass(account);
                        }
                    }
                }
            }
            ResetBattleInfo(room);
        }

        #endregion ROUND END CONDITIONS

        #region CLAN SYSTEM

        /// <summary>
        /// Verifica las restricciones de partidas de clan
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <returns>True si está bloqueada</returns>
        public static bool CheckClanMatchRestrict(RoomModel room)
        {
            if (room.ChannelType == ChannelType.Clan)
            {
                foreach (ClanTeam clanTeam in GetClanTeams(room).Values)
                {
                    if (clanTeam.PlayersFR >= 1 && clanTeam.PlayersCT >= 1)
                    {
                        room.BlockedClan = true;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Verifica si hay 2 clanes para partida de clan
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <returns>True si hay 2 clanes</returns>
        public static bool Have2ClansToClanMatch(RoomModel room)
        {
            return GetClanTeams(room).Count == 2;
        }

        /// <summary>
        /// Verifica si hay suficientes jugadores para partida de clan
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <returns>True si hay suficientes jugadores</returns>
        public static bool HavePlayersToClanMatch(RoomModel room)
        {
            SortedList<int, ClanTeam> clanTeams = GetClanTeams(room);
            bool frTeamReady = false;
            bool ctTeamReady = false;

            foreach (ClanTeam clanTeam in clanTeams.Values)
            {
                if (clanTeam.PlayersFR >= 4)
                    frTeamReady = true;
                else if (clanTeam.PlayersCT >= 4)
                    ctTeamReady = true;
            }

            return frTeamReady & ctTeamReady;
        }

        /// <summary>
        /// Obtiene los equipos de clan de una sala
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <returns>Lista de equipos de clan</returns>
        private static SortedList<int, ClanTeam> GetClanTeams(RoomModel room)
        {
            SortedList<int, ClanTeam> clanTeams = new SortedList<int, ClanTeam>();

            for (int index = 0; index < room.GetAllPlayers().Count; ++index)
            {
                Account player = room.GetAllPlayers()[index];
                if (player.ClanId != 0)
                {
                    ClanTeam clanTeam;
                    if (clanTeams.TryGetValue(player.ClanId, out clanTeam) && clanTeam != null)
                    {
                        if (player.SlotId % 2 == 0)
                            ++clanTeam.PlayersFR;
                        else
                            ++clanTeam.PlayersCT;
                    }
                    else
                    {
                        clanTeam = new ClanTeam()
                        {
                            ClanId = player.ClanId
                        };
                        if (player.SlotId % 2 == 0)
                            ++clanTeam.PlayersFR;
                        else
                            ++clanTeam.PlayersCT;
                        clanTeams.Add(player.ClanId, clanTeam);
                    }
                }
            }

            return clanTeams;
        }

        #endregion CLAN SYSTEM

        #region EVENT SYSTEM ADVANCED

        /// <summary>
        /// Evento de tiempo de juego
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="evPlaytime">Modelo de evento de tiempo</param>
        /// <param name="isBotMode">Si es modo contra bots</param>
        /// <param name="slot">Slot del jugador</param>
        /// <param name="playedTime">Tiempo jugado</param>

        public static void PlayTimeEvent(Account player, EventPlaytimeModel evPlaytime, bool isBotMode, SlotModel slot, long playedTime)
        {
            try
            {
                RoomModel room = player.Room;
                PlayerEvent playerEvent = player.Event;
                if (room == null || playerEvent == null)
                    return;

                int minutes1 = evPlaytime.Minutes1;
                int minutes2 = evPlaytime.Minutes2;
                int minutes3 = evPlaytime.Minutes3;

                if (minutes1 == 0 && minutes2 == 0 && minutes3 == 0)
                {
                    CLogger.Print($"Event Playtime Disabled Due To: 0 Value! (Minutes1: {minutes1}; Minutes2: {minutes2}; Minutes3: {minutes3}", LoggerType.Warning);
                }
                else
                {
                    long lastPlaytimeValue = playerEvent.LastPlaytimeValue;
                    long lastPlaytimeFinish = (long)playerEvent.LastPlaytimeFinish;
                    long lastPlaytimeDate = (long)playerEvent.LastPlaytimeDate;

                    if (playerEvent.LastPlaytimeFinish >= 0 && playerEvent.LastPlaytimeFinish <= 2)
                    {
                        playerEvent.LastPlaytimeValue += (int)playedTime;
                        int requiredMinutes = playerEvent.LastPlaytimeFinish == 0 ? evPlaytime.Minutes1 :
                                            (playerEvent.LastPlaytimeFinish == 1 ? evPlaytime.Minutes2 :
                                            (playerEvent.LastPlaytimeFinish == 2 ? evPlaytime.Minutes3 : 0));

                        if (requiredMinutes == 0)
                            return;

                        int requiredSeconds = requiredMinutes * 60;
                        if (playerEvent.LastPlaytimeValue >= (long)requiredSeconds)
                        {
                            Random random = new Random();
                            List<int> rewardGoods = playerEvent.LastPlaytimeFinish == 0 ? evPlaytime.Goods1 :
                                                  (playerEvent.LastPlaytimeFinish == 1 ? evPlaytime.Goods2 :
                                                  (playerEvent.LastPlaytimeFinish == 2 ? evPlaytime.Goods3 : new List<int>()));

                            if (rewardGoods.Count > 0)
                            {
                                GoodsItem good = ShopManager.GetGood(rewardGoods[random.Next(0, rewardGoods.Count)]);
                                if (good != null)
                                {
                                    if (ComDiv.GetIdStatics(good.Item.Id, 1) == 6 && player.Character.GetCharacter(good.Item.Id) == null)
                                        CreateCharacter(player, good.Item);
                                    else
                                        player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, player, good.Item));
                                    player.SendPacket(new PROTOCOL_BASE_NEW_REWARD_POPUP_ACK(player, good.Item));
                                }
                            }

                            ++playerEvent.LastPlaytimeFinish;
                            playerEvent.LastPlaytimeValue = 0;
                        }
                        playerEvent.LastPlaytimeDate = uint.Parse(DateTimeUtil.Now("yyMMddHHmm"));
                    }

                    if (playerEvent.LastPlaytimeValue == lastPlaytimeValue &&
                        (long)playerEvent.LastPlaytimeFinish == lastPlaytimeFinish &&
                        (long)playerEvent.LastPlaytimeDate == lastPlaytimeDate)
                        return;

                    EventPlaytimeJSON.ResetPlayerEvent(player.PlayerId, playerEvent);
                }
            }
            catch (Exception ex)
            {
                CLogger.Print("[AllUtils.PlayTimeEvent] " + ex.Message, LoggerType.Error, ex);
            }
        }

        #endregion EVENT SYSTEM ADVANCED

        #region MISSION COMPLETION SYSTEM

        /// <summary>
        /// Completa una misión con información de kills
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="slot">Slot del jugador</param>
        /// <param name="kills">Información de kills</param>
        /// <param name="autoComplete">Tipo de misión a completar</param>
        /// <param name="moreInfo">Información adicional</param>

        public static void CompleteMission(RoomModel room, SlotModel slot, FragInfos kills, MissionType autoComplete, int moreInfo)
        {
            try
            {
                Account playerBySlot = room.GetPlayerBySlot(slot);
                if (playerBySlot == null)
                    return;
                ProcessMissionCompletionWithKills(room, playerBySlot, slot, kills, autoComplete, moreInfo);
            }
            catch (Exception ex)
            {
                CLogger.Print("[AllUtils.CompleteMission1] " + ex.Message, LoggerType.Error, ex);
            }
        }

        /// <summary>
        /// Completa una misión sin información de kills
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="slot">Slot del jugador</param>
        /// <param name="autoComplete">Tipo de misión a completar</param>
        /// <param name="moreInfo">Información adicional</param>

        public static void CompleteMission(RoomModel room, SlotModel slot, MissionType autoComplete, int moreInfo)
        {
            try
            {
                Account playerBySlot = room.GetPlayerBySlot(slot);
                if (playerBySlot == null)
                    return;
                ProcessMissionCompletion(room, playerBySlot, slot, autoComplete, moreInfo);
            }
            catch (Exception ex)
            {
                CLogger.Print("[AllUtils.CompleteMission2] " + ex.Message, LoggerType.Error, ex);
            }
        }

        /// <summary>
        /// Procesa la finalización de misión con información de kills
        /// </summary>
        private static void ProcessMissionCompletionWithKills(RoomModel room, Account player, SlotModel slot, FragInfos kills, MissionType autoComplete, int moreInfo)
        {
            try
            {
                PlayerMissions missions = slot.Missions;
                if (missions == null)
                    return;

                int currentMissionId = missions.GetCurrentMissionId();
                int currentCard = missions.GetCurrentCard();

                if (currentMissionId <= 0 || missions.SelectedCard)
                    return;

                List<MissionCardModel> cards = MissionCardRAW.GetCards(currentMissionId, currentCard);
                if (cards.Count == 0)
                    return;

                KillingMessage allKillFlags = kills.GetAllKillFlags();
                byte[] currentMissionList = missions.GetCurrentMissionList();
                ClassType weaponType = (ClassType)ComDiv.GetIdStatics(kills.WeaponId, 2);
                ClassType baseWeaponType = ConvertDualWeaponType(weaponType);
                int weaponId = ComDiv.GetIdStatics(kills.WeaponId, 3);

                ClassType secondaryWeaponType = moreInfo > 0 ? (ClassType)ComDiv.GetIdStatics(kills.WeaponId, 2) : ClassType.Unknown;
                ClassType baseSecondaryWeaponType = moreInfo > 0 ? ConvertDualWeaponType(secondaryWeaponType) : ClassType.Unknown;
                int secondaryWeaponId = moreInfo > 0 ? ComDiv.GetIdStatics(moreInfo, 3) : 0;

                foreach (MissionCardModel missionCard in cards)
                {
                    int progress = 0;
                    if (missionCard.MapId == 0 || (MapIdEnum)missionCard.MapId == room.MapId)
                    {
                        if (kills.Frags.Count > 0)
                        {
                            progress = CalculateMissionProgress(missionCard, slot, kills, allKillFlags, weaponType, baseWeaponType, weaponId, room);
                        }
                        else if (missionCard.MissionType == MissionType.DEATHBLOW && autoComplete == MissionType.DEATHBLOW)
                        {
                            progress = ValidateWeaponRequirements(missionCard, secondaryWeaponType, baseSecondaryWeaponType, secondaryWeaponId);
                        }
                        else if (missionCard.MissionType == autoComplete)
                        {
                            progress = 1;
                        }
                    }

                    if (progress != 0)
                    {
                        UpdateMissionProgress(player, missionCard, currentMissionList, progress);
                    }
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        /// <summary>
        /// Procesa la finalización de misión sin kills
        /// </summary>
        private static void ProcessMissionCompletion(RoomModel room, Account player, SlotModel slot, MissionType autoComplete, int moreInfo)
        {
            try
            {
                PlayerMissions missions = slot.Missions;
                if (missions == null)
                    return;

                int currentMissionId = missions.GetCurrentMissionId();
                int currentCard = missions.GetCurrentCard();

                if (currentMissionId <= 0 || missions.SelectedCard)
                    return;

                List<MissionCardModel> cards = MissionCardRAW.GetCards(currentMissionId, currentCard);
                if (cards.Count == 0)
                    return;

                byte[] currentMissionList = missions.GetCurrentMissionList();
                ClassType weaponType = moreInfo > 0 ? (ClassType)ComDiv.GetIdStatics(moreInfo, 2) : ClassType.Unknown;
                ClassType baseWeaponType = moreInfo > 0 ? ConvertDualWeaponType(weaponType) : ClassType.Unknown;
                int weaponId = moreInfo > 0 ? ComDiv.GetIdStatics(moreInfo, 3) : 0;

                foreach (MissionCardModel missionCard in cards)
                {
                    int progress = 0;
                    if (missionCard.MapId == 0 || (MapIdEnum)missionCard.MapId == room.MapId)
                    {
                        if (missionCard.MissionType != MissionType.DEATHBLOW || autoComplete != MissionType.DEATHBLOW)
                        {
                            if (missionCard.MissionType == autoComplete)
                                progress = 1;
                        }
                        else
                            progress = ValidateWeaponRequirements(missionCard, weaponType, baseWeaponType, weaponId);
                    }

                    if (progress != 0)
                    {
                        UpdateMissionProgress(player, missionCard, currentMissionList, progress);
                    }
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }

        /// <summary>
        /// Calcula el progreso de misión basado en kills
        /// </summary>
        private static int CalculateMissionProgress(MissionCardModel missionCard, SlotModel slot, FragInfos kills,
            KillingMessage allKillFlags, ClassType weaponType, ClassType baseWeaponType, int weaponId, RoomModel room)
        {
            // Verificar tipo de misión específico
            if (missionCard.MissionType == MissionType.KILL ||
                (missionCard.MissionType == MissionType.CHAINSTOPPER && allKillFlags.HasFlag(KillingMessage.ChainStopper)) ||
                (missionCard.MissionType == MissionType.CHAINSLUGGER && allKillFlags.HasFlag(KillingMessage.ChainSlugger)) ||
                (missionCard.MissionType == MissionType.CHAINKILLER && slot.KillsOnLife >= 4) ||
                (missionCard.MissionType == MissionType.TRIPLE_KILL && slot.KillsOnLife == 3) ||
                (missionCard.MissionType == MissionType.DOUBLE_KILL && slot.KillsOnLife == 2) ||
                (missionCard.MissionType == MissionType.HEADSHOT && (allKillFlags.HasFlag(KillingMessage.Headshot) || allKillFlags.HasFlag(KillingMessage.ChainHeadshot))) ||
                (missionCard.MissionType == MissionType.CHAINHEADSHOT && allKillFlags.HasFlag(KillingMessage.ChainHeadshot)) ||
                (missionCard.MissionType == MissionType.PIERCING && allKillFlags.HasFlag(KillingMessage.PiercingShot)) ||
                (missionCard.MissionType == MissionType.MASS_KILL && allKillFlags.HasFlag(KillingMessage.MassKill)) ||
                (missionCard.MissionType == MissionType.KILL_MAN && room.IsDinoMode() &&
                 ((slot.Team == TeamEnum.CT_TEAM && room.Rounds == 2) || (slot.Team == TeamEnum.FR_TEAM && room.Rounds == 1))))
            {
                return CountValidKills(missionCard, weaponType, baseWeaponType, weaponId, kills);
            }
            else if (missionCard.MissionType == MissionType.KILL_WEAPONCLASS ||
                     (missionCard.MissionType == MissionType.DOUBLE_KILL_WEAPONCLASS && slot.KillsOnLife == 2) ||
                     (missionCard.MissionType == MissionType.TRIPLE_KILL_WEAPONCLASS && slot.KillsOnLife == 3))
            {
                return CountWeaponClassKills(missionCard, kills);
            }

            return 0;
        }

        /// <summary>
        /// Cuenta kills válidos para la misión
        /// </summary>
        private static int CountValidKills(MissionCardModel missionCard, ClassType weaponType, ClassType baseWeaponType, int weaponId, FragInfos kills)
        {
            int validKills = 0;
            if ((missionCard.WeaponReqId == 0 || missionCard.WeaponReqId == weaponId) &&
                (missionCard.WeaponReq == ClassType.Unknown || missionCard.WeaponReq == weaponType || missionCard.WeaponReq == baseWeaponType))
            {
                foreach (FragModel frag in kills.Frags)
                {
                    if ((int)frag.VictimSlot % 2 != (int)kills.KillerSlot % 2)
                        ++validKills;
                }
            }
            return validKills;
        }

        /// <summary>
        /// Cuenta kills por clase de arma
        /// </summary>
        private static int CountWeaponClassKills(MissionCardModel missionCard, FragInfos kills)
        {
            int validKills = 0;
            foreach (FragModel frag in kills.Frags)
            {
                if ((int)frag.VictimSlot % 2 != (int)kills.KillerSlot % 2 &&
                    (missionCard.WeaponReq == ClassType.Unknown ||
                     missionCard.WeaponReq == (ClassType)frag.WeaponClass ||
                     missionCard.WeaponReq == ConvertDualWeaponType((ClassType)frag.WeaponClass)))
                    ++validKills;
            }
            return validKills;
        }

        /// <summary>
        /// Valida los requerimientos de arma para deathblow
        /// </summary>
        private static int ValidateWeaponRequirements(MissionCardModel missionCard, ClassType weaponType, ClassType baseWeaponType, int weaponId)
        {
            return missionCard.WeaponReqId != 0 && missionCard.WeaponReqId != weaponId ||
                   missionCard.WeaponReq != ClassType.Unknown && missionCard.WeaponReq != weaponType && missionCard.WeaponReq != baseWeaponType ? 0 : 1;
        }

        /// <summary>
        /// Actualiza el progreso de misión
        /// </summary>
        private static void UpdateMissionProgress(Account player, MissionCardModel missionCard, byte[] currentMissionList, int progress)
        {
            int arrayIdx = missionCard.ArrayIdx;
            if ((int)currentMissionList[arrayIdx] + 1 <= missionCard.MissionLimit)
            {
                player.SendPacket(new PROTOCOL_BASE_QUEST_CHANGE_ACK((int)currentMissionList[arrayIdx], missionCard));
                currentMissionList[arrayIdx] += (byte)progress;
                if ((int)currentMissionList[arrayIdx] > missionCard.MissionLimit)
                    currentMissionList[arrayIdx] = (byte)missionCard.MissionLimit;

                int currentProgress = (int)currentMissionList[arrayIdx];
                player.SendPacket(new PROTOCOL_BASE_QUEST_CHANGE_ACK(currentProgress, missionCard));
            }
        }

        /// <summary>
        /// Habilita misión de evento si está disponible
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        public static void EnableQuestMission(Account player)
        {
            PlayerEvent playerEvent = player.Event;
            if (playerEvent == null || playerEvent.LastQuestFinish != 0 || EventQuestXML.GetRunningEvent() == null)
                return;
            player.Mission.Mission4 = 13;
        }

        #endregion MISSION COMPLETION SYSTEM

        #region READY PLAYERS AND VALIDATION

        /// <summary>
        /// Obtiene el número de jugadores listos por equipo
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="frPlayers">Jugadores FR (salida)</param>
        /// <param name="ctPlayers">Jugadores CT (salida)</param>
        /// <param name="totalEnemies">Total de enemigos (salida)</param>
        public static void GetReadyPlayers(RoomModel room, ref int frPlayers, ref int ctPlayers, ref int totalEnemies)
        {
            int ffaPlayers = 0;
            for (int index = 0; index < room.Slots.Length; ++index)
            {
                SlotModel slot = room.Slots[index];
                if (slot.State == SlotState.READY)
                {
                    if (room.RoomType == RoomCondition.FreeForAll && index > 0)
                        ++ffaPlayers;
                    else if (slot.Team != TeamEnum.FR_TEAM)
                        ++ctPlayers;
                    else
                        ++frPlayers;
                }
            }

            if (room.RoomType == RoomCondition.FreeForAll)
                totalEnemies = ffaPlayers;
            else if (room.Leader % 2 == 0)
                totalEnemies = ctPlayers;
            else
                totalEnemies = frPlayers;

            if (frPlayers + ctPlayers == 2 && totalEnemies == 0)
            {
                totalEnemies = 1;
            }
        }

        /// <summary>
        /// Verifica las condiciones para partidas competitivas
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="room">Sala de juego</param>
        /// <param name="error">Código de error (salida)</param>
        /// <returns>True si hay error</returns>

        public static bool CompetitiveMatchCheck(Account player, RoomModel room, out uint error)
        {
            if (room.Competitive)
            {
                foreach (SlotModel slot in room.Slots)
                {
                    if (slot != null && slot.State != SlotState.CLOSE && slot.State < SlotState.READY)
                    {
                        player.SendPacket(new PROTOCOL_LOBBY_CHATTING_ACK(Translation.GetLabel("Competitive"), player.Session.SessionId, player.NickColor, true, Translation.GetLabel("CompetitiveFullSlot")));
                        error = 2147487858U;
                        return true;
                    }
                }
            }
            error = 0U;
            return false;
        }

        /// <summary>
        /// Verifica las condiciones para partidas de clan
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="type">Tipo de canal</param>
        /// <param name="totalEnemies">Total de enemigos</param>
        /// <param name="error">Código de error (salida)</param>
        /// <returns>True si hay error</returns>
        public static bool ClanMatchCheck(RoomModel room, ChannelType type, int totalEnemies, out uint error)
        {
            if (!ConfigLoader.IsTestMode && type == ChannelType.Clan)
            {
                if (!Have2ClansToClanMatch(room))
                {
                    error = 2147487857U;
                    return true;
                }
                if (totalEnemies > 0 && !HavePlayersToClanMatch(room))
                {
                    error = 2147487858U;
                    return true;
                }
                error = 0U;
                return false;
            }
            error = 0U;
            return false;
        }

        /// <summary>
        /// Intenta balancear los equipos automáticamente
        /// </summary>
        /// <param name="room">Sala de juego</param>
        public static void TryBalanceTeams(RoomModel room)
        {
            if (room.BalanceType != TeamBalance.Count || room.IsBotMode())
                return;

            int[] teamToBalance;
            switch (GetBalanceTeamIdx(room, false, TeamEnum.ALL_TEAM))
            {
                case TeamEnum.ALL_TEAM:
                    return;

                case TeamEnum.CT_TEAM:
                    teamToBalance = room.FR_TEAM;
                    break;

                default:
                    teamToBalance = room.CT_TEAM;
                    break;
            }

            SlotModel targetSlot = null;
            for (int index = teamToBalance.Length - 1; index >= 0; --index)
            {
                SlotModel slot = room.Slots[teamToBalance[index]];
                if (slot.State == SlotState.READY && room.Leader != slot.Id)
                {
                    targetSlot = slot;
                    break;
                }
            }

            Account player;
            if (targetSlot == null || !room.GetPlayerBySlot(targetSlot, out player))
                return;
            TryBalancePlayer(room, player, false, targetSlot);
        }

        #endregion READY PLAYERS AND VALIDATION

        #region FREEPASS AND PENALTY SYSTEM

        /// <summary>
        /// Aplica efectos de free pass o penalizaciones
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="slot">Slot del jugador</param>
        /// <param name="room">Sala de juego</param>
        /// <param name="isBotMode">Si es modo contra bots</param>

        public static void FreepassEffect(Account player, SlotModel slot, RoomModel room, bool isBotMode)
        {
            DBQuery dbQuery = new DBQuery();

            if (player.Bonus.FreePass != 0 && (player.Bonus.FreePass != 1 || room.ChannelType != ChannelType.Clan))
            {
                if (room.State != RoomState.BATTLE)
                    return;

                int experienceGain = 0;
                int goldGain = 0;

                if (!isBotMode)
                {
                    int battleTime = slot.AllKills != 0 || slot.AllDeaths != 0 ? (int)slot.InBattleTime(DateTimeUtil.Now()) : 0;

                    if (room.RoomType != RoomCondition.Bomb && room.RoomType != RoomCondition.FreeForAll && room.RoomType != RoomCondition.Convoy)
                    {
                        experienceGain = (int)((double)slot.Score + (double)battleTime / 2.5 + (double)slot.AllDeaths * 1.8 + (double)(slot.Objects * 20));
                        goldGain = (int)((double)slot.Score + (double)battleTime / 3.0 + (double)slot.AllDeaths * 1.8 + (double)(slot.Objects * 20));
                    }
                    else
                    {
                        experienceGain = (int)((double)slot.Score + (double)battleTime / 2.5 + (double)slot.AllDeaths * 2.2 + (double)(slot.Objects * 20));
                        goldGain = (int)((double)slot.Score + (double)battleTime / 3.0 + (double)slot.AllDeaths * 2.2 + (double)(slot.Objects * 20));
                    }
                }
                else
                {
                    int botLevelBonus = (int)room.IngameAiLevel * (150 + slot.AllDeaths);
                    if (botLevelBonus == 0)
                        ++botLevelBonus;
                    int baseReward = slot.Score / botLevelBonus;
                    goldGain = experienceGain = baseReward;
                }

                player.Exp += ConfigLoader.MaxExpReward < experienceGain ? ConfigLoader.MaxExpReward : experienceGain;
                player.Gold += ConfigLoader.MaxGoldReward < goldGain ? ConfigLoader.MaxGoldReward : goldGain;

                if (goldGain > 0)
                    dbQuery.AddQuery("gold", (object)player.Gold);
                if (experienceGain > 0)
                    dbQuery.AddQuery("experience", (object)player.Exp);
            }
            else
            {
                if (isBotMode || slot.State < SlotState.BATTLE_READY)
                    return;

                if (player.Gold > 0)
                {
                    player.Gold -= 200;
                    if (player.Gold < 0)
                        player.Gold = 0;
                    dbQuery.AddQuery("gold", (object)player.Gold);
                }

                ComDiv.UpdateDB("player_stat_basics", "owner_id", (object)player.PlayerId, "escapes_count", (object)++player.Statistic.Basic.EscapesCount);
                ComDiv.UpdateDB("player_stat_seasons", "owner_id", (object)player.PlayerId, "escapes_count", (object)++player.Statistic.Season.EscapesCount);
            }

            ComDiv.UpdateDB("accounts", "player_id", (object)player.PlayerId, dbQuery.GetTables(), dbQuery.GetValues());
        }

        #endregion FREEPASS AND PENALTY SYSTEM

        #region LEAVE BATTLE HANDLERS

        /// <summary>
        /// Maneja la salida del host en batalla PVE
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="player">Jugador que sale</param>

        public static void LeaveHostGiveBattlePVE(RoomModel room, Account player)
        {
            List<Account> allPlayers = room.GetAllPlayers(SlotState.READY, 1);
            if (allPlayers.Count == 0)
                return;

            int leader = room.Leader;
            room.SetNewLeader(-1, SlotState.BATTLE_READY, leader, true);

            using (PROTOCOL_BATTLE_GIVEUPBATTLE_ACK battleGiveup = new PROTOCOL_BATTLE_GIVEUPBATTLE_ACK(player, 0))
            {
                using (PROTOCOL_BATTLE_LEAVEP2PSERVER_ACK leaveP2P = new PROTOCOL_BATTLE_LEAVEP2PSERVER_ACK(room))
                {
                    byte[] giveupBytes = battleGiveup.GetCompleteBytes("PROTOCOL_BATTLE_GIVEUPBATTLE_REQ-1");
                    byte[] leaveBytes = leaveP2P.GetCompleteBytes("PROTOCOL_BATTLE_GIVEUPBATTLE_REQ-2");

                    foreach (Account account in allPlayers)
                    {
                        SlotModel slot = room.GetSlot(account.SlotId);
                        if (slot != null)
                        {
                            if (slot.State >= SlotState.PRESTART)
                                account.SendCompletePacket(leaveBytes, leaveP2P.GetType().Name);
                            account.SendCompletePacket(giveupBytes, battleGiveup.GetType().Name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finaliza batalla PVE cuando el host sale
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="player">Jugador que sale</param>

        public static void LeaveHostEndBattlePVE(RoomModel room, Account player)
        {
            List<Account> allPlayers = room.GetAllPlayers(SlotState.READY, 1);
            if (allPlayers.Count > 0)
            {
                using (PROTOCOL_BATTLE_GIVEUPBATTLE_ACK battleGiveup = new PROTOCOL_BATTLE_GIVEUPBATTLE_ACK(player, 0))
                {
                    byte[] giveupBytes = battleGiveup.GetCompleteBytes("PROTOCOL_BATTLE_GIVEUPBATTLE_REQ-3");
                    TeamEnum winnerTeam = GetWinnerTeam(room);

                    int missionFlag;
                    int slotFlag;
                    byte[] data;
                    GetBattleResult(room, out missionFlag, out slotFlag, out data);

                    foreach (Account account in allPlayers)
                    {
                        account.SendCompletePacket(giveupBytes, battleGiveup.GetType().Name);
                        account.SendPacket(new PROTOCOL_BATTLE_ENDBATTLE_ACK(account, winnerTeam, slotFlag, missionFlag, true, data));
                        UpdateSeasonPass(account);
                    }
                }
            }
            ResetBattleInfo(room);
        }

        /// <summary>
        /// Finaliza batalla PVP cuando el host sale
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="player">Jugador que sale</param>
        /// <param name="teamFR">Jugadores del equipo FR</param>
        /// <param name="teamCT">Jugadores del equipo CT</param>
        /// <param name="isFinished">Si la batalla terminó (salida)</param>

        public static void LeaveHostEndBattlePVP(RoomModel room, Account player, int teamFR, int teamCT, out bool isFinished)
        {
            isFinished = true;
            List<Account> allPlayers = room.GetAllPlayers(SlotState.READY, 1);

            if (allPlayers.Count > 0)
            {
                TeamEnum winnerTeam = GetWinnerTeam(room, teamFR, teamCT);
                if (room.State == RoomState.BATTLE)
                    room.CalculateResult(winnerTeam, false);

                using (PROTOCOL_BATTLE_GIVEUPBATTLE_ACK battleGiveup = new PROTOCOL_BATTLE_GIVEUPBATTLE_ACK(player, 0))
                {
                    byte[] giveupBytes = battleGiveup.GetCompleteBytes("PROTOCOL_BATTLE_GIVEUPBATTLE_REQ-4");

                    int missionFlag;
                    int slotFlag;
                    byte[] data;
                    GetBattleResult(room, out missionFlag, out slotFlag, out data);

                    foreach (Account account in allPlayers)
                    {
                        account.SendCompletePacket(giveupBytes, battleGiveup.GetType().Name);
                        account.SendPacket(new PROTOCOL_BATTLE_ENDBATTLE_ACK(account, winnerTeam, slotFlag, missionFlag, false, data));
                        UpdateSeasonPass(account);
                    }
                }
            }
            ResetBattleInfo(room);
        }

        /// <summary>
        /// Maneja la salida del host dando batalla PVP
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="player">Jugador que sale</param>

        public static void LeaveHostGiveBattlePVP(RoomModel room, Account player)
        {
            List<Account> allPlayers = room.GetAllPlayers(SlotState.READY, 1);
            if (allPlayers.Count == 0)
                return;

            int leader = room.Leader;
            SlotState state = room.State == RoomState.BATTLE ? SlotState.BATTLE_READY : SlotState.READY;
            room.SetNewLeader(-1, state, leader, true);

            using (PROTOCOL_BATTLE_LEAVEP2PSERVER_ACK leaveP2P = new PROTOCOL_BATTLE_LEAVEP2PSERVER_ACK(room))
            {
                using (PROTOCOL_BATTLE_GIVEUPBATTLE_ACK battleGiveup = new PROTOCOL_BATTLE_GIVEUPBATTLE_ACK(player, 0))
                {
                    byte[] leaveBytes = leaveP2P.GetCompleteBytes("PROTOCOL_BATTLE_GIVEUPBATTLE_REQ-6");
                    byte[] giveupBytes = battleGiveup.GetCompleteBytes("PROTOCOL_BATTLE_GIVEUPBATTLE_REQ-7");

                    foreach (Account account in allPlayers)
                    {
                        if (room.Slots[account.SlotId].State >= SlotState.PRESTART)
                            account.SendCompletePacket(leaveBytes, leaveP2P.GetType().Name);
                        account.SendCompletePacket(giveupBytes, battleGiveup.GetType().Name);
                    }
                }
            }
        }

        /// <summary>
        /// Finaliza batalla PVP cuando un jugador sale
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="player">Jugador que sale</param>
        /// <param name="teamFR">Jugadores del equipo FR</param>
        /// <param name="teamCT">Jugadores del equipo CT</param>
        /// <param name="isFinished">Si la batalla terminó (salida)</param>

        public static void LeavePlayerEndBattlePVP(RoomModel room, Account player, int teamFR, int teamCT, out bool isFinished)
        {
            isFinished = true;
            TeamEnum winnerTeam = GetWinnerTeam(room, teamFR, teamCT);
            List<Account> allPlayers = room.GetAllPlayers(SlotState.READY, 1);

            if (allPlayers.Count > 0)
            {
                if (room.State == RoomState.BATTLE)
                    room.CalculateResult(winnerTeam, false);

                using (PROTOCOL_BATTLE_GIVEUPBATTLE_ACK battleGiveup = new PROTOCOL_BATTLE_GIVEUPBATTLE_ACK(player, 0))
                {
                    byte[] giveupBytes = battleGiveup.GetCompleteBytes("PROTOCOL_BATTLE_GIVEUPBATTLE_REQ-8");

                    int missionFlag;
                    int slotFlag;
                    byte[] data;
                    GetBattleResult(room, out missionFlag, out slotFlag, out data);

                    foreach (Account account in allPlayers)
                    {
                        account.SendCompletePacket(giveupBytes, battleGiveup.GetType().Name);
                        account.SendPacket(new PROTOCOL_BATTLE_ENDBATTLE_ACK(account, winnerTeam, slotFlag, missionFlag, false, data));
                        UpdateSeasonPass(account);
                    }
                }
            }
            ResetBattleInfo(room);
        }

        /// <summary>
        /// Maneja cuando un jugador abandona la batalla
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="player">Jugador que abandona</param>
        public static void LeavePlayerQuitBattle(RoomModel room, Account player)
        {
            using (PROTOCOL_BATTLE_GIVEUPBATTLE_ACK packet = new PROTOCOL_BATTLE_GIVEUPBATTLE_ACK(player, 0))
                room.SendPacketToPlayers(packet, SlotState.READY, 1);
        }

        #endregion LEAVE BATTLE HANDLERS

        #region VALIDATION AND EQUIPMENT SYSTEMS

        /// <summary>
        /// Obtiene un valor de un diccionario ordenado por clave
        /// </summary>
        /// <param name="key">Clave a buscar</param>
        /// <param name="dictionary">Diccionario a consultar</param>
        /// <returns>Valor encontrado o 0</returns>
        private static int GetSortedListValue(int key, SortedList<int, int> dictionary)
        {
            int value;
            return dictionary.TryGetValue(key, out value) ? value : 0;
        }

        /// <summary>
        /// Obtiene el ID real de un ítem del inventario
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="itemId">ID del ítem</param>
        /// <returns>ID real del ítem o 0</returns>
        private static int GetRealItemId(Account player, int itemId)
        {
            ItemsModel item = player.Inventory.GetItem(itemId);
            return item == null ? 0 : item.Id;
        }

        /// <summary>
        /// Valida el equipamiento de accesorios
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="accessoryId">ID del accesorio</param>

        public static void ValidateAccesoryEquipment(Account player, int accessoryId)
        {
            if (player.Equipment.AccessoryId == accessoryId)
                return;
            player.Equipment.AccessoryId = GetRealItemId(player, accessoryId);
            ComDiv.UpdateDB("player_equipments", "accesory_id", (object)player.Equipment.AccessoryId, "owner_id", (object)player.PlayerId);
        }

        /// <summary>
        /// Valida cupones deshabilitados
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="coupons">Lista de cupones</param>
        public static void ValidateDisabledCoupon(Account player, SortedList<int, int> coupons)
        {
            for (int index = 0; index < coupons.Keys.Count; ++index)
            {
                ItemsModel item = player.Inventory.GetItem(GetSortedListValue(index, coupons));
                if (item != null)
                {
                    CouponFlag couponEffect = CouponEffectXML.GetCouponEffect(item.Id);
                    if (couponEffect != null && couponEffect.EffectFlag > (CouponEffects)0 &&
                        player.Effects.HasFlag((Enum)couponEffect.EffectFlag))
                    {
                        player.Effects -= couponEffect.EffectFlag;
                        DaoManagerSQL.UpdateCouponEffect(player.PlayerId, player.Effects);
                    }
                }
            }
        }

        /// <summary>
        /// Valida cupones habilitados
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="coupons">Lista de cupones</param>
        public static void ValidateEnabledCoupon(Account player, SortedList<int, int> coupons)
        {
            for (int index = 0; index < coupons.Keys.Count; ++index)
            {
                ItemsModel item = player.Inventory.GetItem(GetSortedListValue(index, coupons));
                if (item != null)
                {
                    int bonusResult = player.Bonus.AddBonuses(item.Id) ? 1 : 0;
                    CouponFlag couponEffect = CouponEffectXML.GetCouponEffect(item.Id);
                    if (couponEffect != null && couponEffect.EffectFlag > (CouponEffects)0 &&
                        !player.Effects.HasFlag((Enum)couponEffect.EffectFlag))
                    {
                        player.Effects |= couponEffect.EffectFlag;
                        DaoManagerSQL.UpdateCouponEffect(player.PlayerId, player.Effects);
                    }
                    if (bonusResult != 0)
                        DaoManagerSQL.UpdatePlayerBonus(player.PlayerId, player.Bonus.Bonuses, player.Bonus.FreePass);
                }
            }
        }

        /// <summary>
        /// Verifica efectos duplicados de cupones
        /// </summary>
        /// <param name="couponId">ID del cupón</param>
        /// <param name="effects">Efectos del jugador</param>
        /// <param name="couponData">Datos del cupón</param>
        /// <returns>True si hay conflicto</returns>
        private static bool CheckCouponEffectConflict(int couponId, CouponEffects effects, (int, CouponEffects, bool) couponData)
        {
            if (couponId != couponData.Item1)
                return false;
            return couponData.Item3 ? (effects & couponData.Item2) > (CouponEffects)0 : effects.HasFlag((Enum)couponData.Item2);
        }

        /// <summary>
        /// Verifica efectos duplicados de cupones
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="couponId">ID del cupón</param>
        /// <returns>True si hay efectos duplicados</returns>
        public static bool CheckDuplicateCouponEffects(Account player, int couponId)
        {
            bool hasConflict = false;

            foreach ((int, CouponEffects, bool) couponData in new List<(int, CouponEffects, bool)>()
            {
                (1600065, CouponEffects.Defense20 | CouponEffects.Defense10 | CouponEffects.Defense5, true),
                (1600079, CouponEffects.Defense90 | CouponEffects.Defense10 | CouponEffects.Defense5, true),
                (1600044, CouponEffects.Defense90 | CouponEffects.Defense20 | CouponEffects.Defense5, true),
                (1600030, CouponEffects.Defense90 | CouponEffects.Defense20 | CouponEffects.Defense10, true),
                (1600078, CouponEffects.JackHollowPoint | CouponEffects.HollowPoint | CouponEffects.FullMetalJack, true),
                (1600032, CouponEffects.HollowPointPlus | CouponEffects.JackHollowPoint | CouponEffects.FullMetalJack, true),
                (1600031, CouponEffects.HollowPointPlus | CouponEffects.JackHollowPoint | CouponEffects.HollowPoint, true),
                (1600036, CouponEffects.HollowPointPlus | CouponEffects.HollowPoint | CouponEffects.FullMetalJack, true),
                (1600028, CouponEffects.HP5, false),
                (1600040, CouponEffects.HP10, false),
                (1700208, CouponEffects.Camoflage50, false),
                (1700209, CouponEffects.Camoflage99, false)
            })
            {
                if (CheckCouponEffectConflict(couponId, player.Effects, couponData))
                {
                    hasConflict = true;
                    break;
                }
            }

            return hasConflict;
        }

        #endregion VALIDATION AND EQUIPMENT SYSTEMS

        #region CHARACTER AND EQUIPMENT VALIDATION

        /// <summary>
        /// Valida el equipamiento de personajes
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="equip">Equipamiento del jugador</param>
        /// <param name="equipmentList">Lista de equipamiento</param>
        /// <param name="characterTemps">Personajes temporales</param>
        /// <param name="characterSlots">Slots de personajes</param>

        public static void ValidateCharacterEquipment(Account player, PlayerEquipment equip, int[] equipmentList, int[] characterTemps, int[] characterSlots)
        {
            DBQuery dbQuery = new DBQuery();
            CharacterModel character = player.Character.GetCharacter(characterTemps[0]);

            if (character != null)
            {
                int itemType = ComDiv.GetIdStatics(character.Id, 1);
                int charSide = ComDiv.GetIdStatics(character.Id, 2);
                int charSpecial = ComDiv.GetIdStatics(character.Id, 5);

                if (itemType == 6 && (charSide == 1 || charSpecial == 632) && characterSlots[0] == character.Slot)
                {
                    if (equip.CharaRedId != character.Id)
                    {
                        equip.CharaRedId = character.Id;
                        dbQuery.AddQuery("chara_red_side", (object)equip.CharaRedId);
                    }
                }
                else if (itemType == 6 && (charSide == 2 || charSpecial == 664) && characterSlots[1] == character.Slot && equip.CharaBlueId != character.Id)
                {
                    equip.CharaBlueId = character.Id;
                    dbQuery.AddQuery("chara_blue_side", (object)equip.CharaBlueId);
                }
            }

            // Validar equipamiento por slots
            for (int index = 0; index < equipmentList.Length; ++index)
            {
                ItemsModel item = player.Inventory.GetItem(equipmentList[index]);
                if (item != null)
                {
                    ValidateEquipmentSlot(equip, dbQuery, index, item.Id);
                }
            }

            // Validar ítem de boina
            int beretItem = characterTemps[1];
            if (equip.BeretItem != beretItem)
            {
                equip.BeretItem = GetRealItemId(player, beretItem);
                dbQuery.AddQuery("beret_item_part", (object)equip.BeretItem);
            }

            ComDiv.UpdateDB("player_equipments", "owner_id", (object)player.PlayerId, dbQuery.GetTables(), dbQuery.GetValues());
        }

        /// <summary>
        /// Valida un slot específico de equipamiento
        /// </summary>
        private static void ValidateEquipmentSlot(PlayerEquipment equip, DBQuery dbQuery, int slotIndex, int itemId)
        {
            switch (slotIndex)
            {
                case 0: // Arma primaria
                    if (equip.WeaponPrimary != itemId)
                    {
                        equip.WeaponPrimary = itemId;
                        dbQuery.AddQuery("weapon_primary", (object)equip.WeaponPrimary);
                    }
                    break;

                case 1: // Arma secundaria
                    if (equip.WeaponSecondary != itemId)
                    {
                        equip.WeaponSecondary = itemId;
                        dbQuery.AddQuery("weapon_secondary", (object)equip.WeaponSecondary);
                    }
                    break;

                case 2: // Arma cuerpo a cuerpo
                    if (equip.WeaponMelee != itemId)
                    {
                        equip.WeaponMelee = itemId;
                        dbQuery.AddQuery("weapon_melee", (object)equip.WeaponMelee);
                    }
                    break;

                case 3: // Arma explosiva
                    if (equip.WeaponExplosive != itemId)
                    {
                        equip.WeaponExplosive = itemId;
                        dbQuery.AddQuery("weapon_explosive", (object)equip.WeaponExplosive);
                    }
                    break;

                case 4: // Arma especial
                    if (equip.WeaponSpecial != itemId)
                    {
                        equip.WeaponSpecial = itemId;
                        dbQuery.AddQuery("weapon_special", (object)equip.WeaponSpecial);
                    }
                    break;

                case 5: // Cabeza
                    if (equip.PartHead != itemId)
                    {
                        equip.PartHead = itemId;
                        dbQuery.AddQuery("part_head", (object)equip.PartHead);
                    }
                    break;

                case 6: // Cara
                    if (equip.PartFace != itemId)
                    {
                        equip.PartFace = itemId;
                        dbQuery.AddQuery("part_face", (object)equip.PartFace);
                    }
                    break;

                case 7: // Chaqueta
                    if (equip.PartJacket != itemId)
                    {
                        equip.PartJacket = itemId;
                        dbQuery.AddQuery("part_jacket", (object)equip.PartJacket);
                    }
                    break;

                case 8: // Bolsillos
                    if (equip.PartPocket != itemId)
                    {
                        equip.PartPocket = itemId;
                        dbQuery.AddQuery("part_pocket", (object)equip.PartPocket);
                    }
                    break;

                case 9: // Guantes
                    if (equip.PartGlove != itemId)
                    {
                        equip.PartGlove = itemId;
                        dbQuery.AddQuery("part_glove", (object)equip.PartGlove);
                    }
                    break;

                case 10: // Cinturón
                    if (equip.PartBelt != itemId)
                    {
                        equip.PartBelt = itemId;
                        dbQuery.AddQuery("part_belt", (object)equip.PartBelt);
                    }
                    break;

                case 11: // Funda
                    if (equip.PartHolster != itemId)
                    {
                        equip.PartHolster = itemId;
                        dbQuery.AddQuery("part_holster", (object)equip.PartHolster);
                    }
                    break;

                case 12: // Piel
                    if (equip.PartSkin != itemId)
                    {
                        equip.PartSkin = itemId;
                        dbQuery.AddQuery("part_skin", (object)equip.PartSkin);
                    }
                    break;
            }
        }

        /// <summary>
        /// Valida el equipamiento de ítems especiales
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="items">Lista de ítems</param>

        public static void ValidateItemEquipment(Account player, SortedList<int, int> items)
        {
            for (int index = 0; index < items.Keys.Count; ++index)
            {
                int itemId = GetSortedListValue(index, items);
                switch (index)
                {
                    case 0: // Ítem de dinosaurio
                        if (itemId != 0 && player.Equipment.DinoItem != itemId)
                        {
                            player.Equipment.DinoItem = GetRealItemId(player, itemId);
                            ComDiv.UpdateDB("player_equipments", "dino_item_chara", (object)player.Equipment.DinoItem, "owner_id", (object)player.PlayerId);
                        }
                        break;

                    case 1: // Spray
                        if (player.Equipment.SprayId != itemId)
                        {
                            player.Equipment.SprayId = GetRealItemId(player, itemId);
                            ComDiv.UpdateDB("player_equipments", "spray_id", (object)player.Equipment.SprayId, "owner_id", (object)player.PlayerId);
                        }
                        break;

                    case 2: // Tarjeta de nombre
                        if (player.Equipment.NameCardId != itemId)
                        {
                            player.Equipment.NameCardId = GetRealItemId(player, itemId);
                            ComDiv.UpdateDB("player_equipments", "namecard_id", (object)player.Equipment.NameCardId, "owner_id", (object)player.PlayerId);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Valida slots de personajes
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="equip">Equipamiento del jugador</param>
        /// <param name="slots">Slots de personajes</param>

        public static void ValidateCharacterSlot(Account player, PlayerEquipment equip, int[] slots)
        {
            DBQuery dbQuery = new DBQuery();

            CharacterModel redCharacter = player.Character.GetCharacterSlot(slots[0]);
            if (redCharacter != null && equip.CharaRedId != redCharacter.Id)
            {
                equip.CharaRedId = GetRealItemId(player, redCharacter.Id);
                dbQuery.AddQuery("chara_red_side", (object)equip.CharaRedId);
            }

            CharacterModel blueCharacter = player.Character.GetCharacterSlot(slots[1]);
            if (blueCharacter != null && equip.CharaBlueId != blueCharacter.Id)
            {
                equip.CharaBlueId = GetRealItemId(player, blueCharacter.Id);
                dbQuery.AddQuery("chara_blue_side", (object)equip.CharaBlueId);
            }

            ComDiv.UpdateDB("player_equipments", "owner_id", (object)player.PlayerId, dbQuery.GetTables(), dbQuery.GetValues());
        }

        /// <summary>
        /// Validasi equipment setelah karakter dihapus - dipanggil saat login
        /// </summary>
        public static void ValidateCharacterEquipmentOnLogin(Account player)
        {
            if (player == null || player.Equipment == null)
                return;

            bool needUpdate = false;
            DBQuery query = new DBQuery();

            // Cek karakter merah
            if (player.Equipment.CharaRedId != 0)
            {
                CharacterModel redChar = player.Character.GetCharacter(player.Equipment.CharaRedId);
                if (redChar == null)
                {
                    player.Equipment.CharaRedId = 601001;
                    query.AddQuery("chara_red_side", (object)player.Equipment.CharaRedId);
                    needUpdate = true;
                }
            }

            // Cek karakter biru
            if (player.Equipment.CharaBlueId != 0)
            {
                CharacterModel blueChar = player.Character.GetCharacter(player.Equipment.CharaBlueId);
                if (blueChar == null)
                {
                    player.Equipment.CharaBlueId = 602001;
                    query.AddQuery("chara_blue_side", (object)player.Equipment.CharaBlueId);
                    needUpdate = true;
                }
            }

            // Cek karakter dino
            if (player.Equipment.DinoItem != 0)
            {
                ItemsModel dinoItem = player.Inventory.GetItem(player.Equipment.DinoItem);
                if (dinoItem == null)
                {
                    player.Equipment.DinoItem = 0;
                    query.AddQuery("dino_item_chara", (object)player.Equipment.DinoItem);
                    needUpdate = true;
                }
            }

            if (needUpdate)
            {
                ComDiv.UpdateDB("player_equipments", "owner_id", (object)player.PlayerId, query.GetTables(), query.GetValues());
            }
        }

        /// <summary>
        /// Valida equipamiento para respawn
        /// </summary>
        /// <param name="slot">Slot del jugador</param>
        /// <param name="itemIds">IDs de ítems</param>
        /// <returns>Equipamiento validado</returns>
        public static PlayerEquipment ValidateRespawnEQ(SlotModel slot, int[] itemIds)
        {
            PlayerEquipment equipment = new PlayerEquipment()
            {
                WeaponPrimary = itemIds[0],
                WeaponSecondary = itemIds[1],
                WeaponMelee = itemIds[2],
                WeaponExplosive = itemIds[3],
                WeaponSpecial = itemIds[4],
                PartHead = itemIds[6],
                PartFace = itemIds[7],
                PartJacket = itemIds[8],
                PartPocket = itemIds[9],
                PartGlove = itemIds[10],
                PartBelt = itemIds[11],
                PartHolster = itemIds[12],
                PartSkin = itemIds[13],
                BeretItem = itemIds[14],
                AccessoryId = itemIds[15],
                CharaRedId = slot.Equipment.CharaRedId,
                CharaBlueId = slot.Equipment.CharaBlueId,
                DinoItem = slot.Equipment.DinoItem
            };

            int itemType = ComDiv.GetIdStatics(itemIds[5], 1);
            int charSide = ComDiv.GetIdStatics(itemIds[5], 2);
            int charSpecial = ComDiv.GetIdStatics(itemIds[5], 5);

            switch (itemType)
            {
                case 6: // Personaje
                    if (charSide == 1 || charSpecial == 632)
                        equipment.CharaRedId = itemIds[5];
                    else if (charSide == 2 || charSpecial == 664)
                        equipment.CharaBlueId = itemIds[5];
                    break;

                case 15: // Dinosaurio
                    equipment.DinoItem = itemIds[5];
                    break;
            }

            return equipment;
        }

        /// <summary>
        /// Inserta un ítem en la lista de uso del slot
        /// </summary>
        /// <param name="itemId">ID del ítem</param>
        /// <param name="slot">Slot del jugador</param>
        public static void InsertItem(int itemId, SlotModel slot)
        {
            lock (slot.ItemUsages)
            {
                if (slot.ItemUsages.Contains(itemId))
                    return;
                slot.ItemUsages.Add(itemId);
            }
        }

        #endregion CHARACTER AND EQUIPMENT VALIDATION

        #region ANTI-CHEAT AND SECURITY

        /// <summary>
        /// Valida y banea un jugador por usar cheats
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="message">Mensaje de la violación</param>

        public static void ValidateBanPlayer(Account player, string message)
        {
            if (ConfigLoader.AutoBan && DaoManagerSQL.SaveAutoBan(player.PlayerId, player.Username, player.Nickname,
                $"Cheat {message})", DateTimeUtil.Now("dd -MM-yyyy HH:mm:ss"), player.PublicIP.ToString(), "Illegal Program"))
            {
                using (PROTOCOL_LOBBY_CHATTING_ACK packet = new PROTOCOL_LOBBY_CHATTING_ACK("Server", 0, 1, false,
                    $"Permanently ban player [{player.Nickname}], {message}"))
                    GameXender.Client.SendPacketToAllClients(packet);

                player.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), false);
                player.Close(1000, true);
            }
            CLogger.Print($"Player: {player.Nickname}; Id: {player.PlayerId}; User: {player.Username}; Reason: {message}", LoggerType.Hack);
        }

        /// <summary>
        /// Procesa comandos del servidor
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="text">Texto del comando</param>
        /// <returns>True si se procesó un comando</returns>

        public static bool ServerCommands(Account player, string text)
        {
            try
            {
                int result = CommandManager.TryParse(text, player) ? 1 : 0;
                if (result != 0)
                    CLogger.Print($"Player '{player.Nickname}' (UID: {player.PlayerId}) Running Command '{text}'", LoggerType.Command);
                return result != 0;
            }
            catch
            {
                player.SendPacket(new PROTOCOL_LOBBY_CHATTING_ACK("Server", 0, 5, false, Translation.GetLabel("CommandsExceptionError")));
                return true;
            }
        }

        #endregion ANTI-CHEAT AND SECURITY

        #region COMMUNICATION AND CHAT

        /// <summary>
        /// Valida si un mensaje entre slots es válido
        /// </summary>
        /// <param name="sender">Slot del remitente</param>
        /// <param name="receiver">Slot del receptor</param>
        /// <returns>True si el mensaje es válido</returns>
        public static bool SlotValidMessage(SlotModel sender, SlotModel receiver)
        {
            if ((sender.State == SlotState.NORMAL || sender.State == SlotState.READY) &&
                (receiver.State == SlotState.NORMAL || receiver.State == SlotState.READY))
                return true;

            if (sender.State < SlotState.LOAD || receiver.State < SlotState.LOAD)
                return false;

            if (receiver.SpecGM || sender.SpecGM || sender.DeathState.HasFlag((Enum)DeadEnum.UseChat) ||
                sender.DeathState.HasFlag((Enum)DeadEnum.Dead) && receiver.DeathState.HasFlag((Enum)DeadEnum.Dead) ||
                sender.Spectator && receiver.Spectator)
                return true;

            if (!sender.DeathState.HasFlag((Enum)DeadEnum.Alive) || !receiver.DeathState.HasFlag((Enum)DeadEnum.Alive))
                return false;

            if (sender.Spectator && receiver.Spectator)
                return true;

            return !sender.Spectator && !receiver.Spectator;
        }

        /// <summary>
        /// Verifica si un jugador está en batalla
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <returns>True si está en batalla</returns>
        public static bool PlayerIsBattle(Account player)
        {
            RoomModel room = player.Room;
            SlotModel slot;
            return room != null && room.GetSlot(player.SlotId, out slot) && slot.State >= SlotState.READY;
        }

        /// <summary>
        /// Sincroniza el ping de la sala
        /// </summary>
        /// <param name="room">Sala de juego</param>
        public static void RoomPingSync(RoomModel room)
        {
            if (ComDiv.GetDuration(room.LastPingSync) < (double)ConfigLoader.PingUpdateTimeSeconds)
                return;

            byte[] pingData = new byte[18];
            for (int index = 0; index < 18; ++index)
                pingData[index] = (byte)room.Slots[index].Ping;

            using (PROTOCOL_BATTLE_SENDPING_ACK packet = new PROTOCOL_BATTLE_SENDPING_ACK(pingData))
                room.SendPacketToPlayers(packet, SlotState.BATTLE, 0);

            room.LastPingSync = DateTimeUtil.Now();
        }

        #endregion COMMUNICATION AND CHAT

        #region ITEM REPAIR SYSTEM

        /// <summary>
        /// Obtiene los ítems reparables y calcula el costo
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="objectIds">IDs de objetos a reparar</param>
        /// <param name="gold">Costo en oro (salida)</param>
        /// <param name="cash">Costo en dinero (salida)</param>
        /// <param name="error">Código de error (salida)</param>
        /// <returns>Lista de ítems reparables</returns>
        public static List<ItemsModel> RepairableItems(Account player, List<long> objectIds, out int gold, out int cash, out uint error)
        {
            gold = 0;
            cash = 0;
            error = 0U;
            List<ItemsModel> repairableItems = new List<ItemsModel>();

            if (objectIds.Count > 0)
            {
                foreach (long objectId in objectIds)
                {
                    ItemsModel item = player.Inventory.GetItem(objectId);
                    if (item == null)
                    {
                        error = 2147483920U;
                    }
                    else
                    {
                        uint[] repairCosts = CalculateRepairCost(player, item);
                        gold += (int)repairCosts[0];
                        cash += (int)repairCosts[1];
                        error = repairCosts[2];
                        repairableItems.Add(item);
                    }
                }
            }

            return repairableItems;
        }

        /// <summary>
        /// Calcula el costo de reparación de un ítem
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="item">Ítem a reparar</param>
        /// <returns>Array con [oro, dinero, error]</returns>

        private static uint[] CalculateRepairCost(Account player, ItemsModel item)
        {
            uint[] costs = new uint[3];
            ItemsRepair repairInfo = ShopManager.GetRepairItem(item.Id);

            if (repairInfo != null)
            {
                uint damagePercent = repairInfo.Quantity - item.Count;

                if (repairInfo.Point <= repairInfo.Cash)
                {
                    if (repairInfo.Cash > repairInfo.Point)
                    {
                        uint cashCost = (uint)ComDiv.Percentage(repairInfo.Cash, (int)damagePercent);
                        if ((long)player.Cash >= (long)cashCost)
                        {
                            costs[1] = cashCost;
                        }
                        else
                        {
                            costs[2] = 2147483920U;
                            return costs;
                        }
                    }
                    else
                    {
                        costs[2] = 2147483920U;
                        return costs;
                    }
                }
                else
                {
                    uint goldCost = (uint)ComDiv.Percentage(repairInfo.Point, (int)damagePercent);
                    if ((long)player.Gold >= (long)goldCost)
                    {
                        costs[0] = goldCost;
                    }
                    else
                    {
                        costs[2] = 2147483920U;
                        return costs;
                    }
                }

                item.Count = repairInfo.Quantity;
                ComDiv.UpdateDB("player_items", "count", (object)(long)item.Count, "owner_id", (object)player.PlayerId, "id", (object)item.Id);
                costs[2] = 1U;
                return costs;
            }

            costs[2] = 2147483920U;
            return costs;
        }

        #endregion ITEM REPAIR SYSTEM

        #region CHANNEL AND ACCESS VALIDATION

        /// <summary>
        /// Verifica los requerimientos de canal
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="channel">Canal a verificar</param>
        /// <returns>True si no cumple los requerimientos</returns>
        public static bool ChannelRequirementCheck(Account player, ChannelModel channel)
        {
            return !player.IsGM() &&
                   (channel.Type == ChannelType.Clan && player.ClanId == 0 ||
                    channel.Type == ChannelType.Novice && player.Statistic.GetKDRatio() > 40 && player.Statistic.GetSeasonKDRatio() > 40 ||
                    channel.Type == ChannelType.Training && player.Rank >= 4 ||
                    channel.Type == ChannelType.Special && player.Rank <= 25 ||
                    channel.Type == ChannelType.Blocked);
        }

        /// <summary>
        /// Cambia el costume del equipo
        /// </summary>
        /// <param name="slot">Slot del jugador</param>
        /// <param name="costumeTeam">Equipo del costume</param>
        /// <returns>True si se cambió</returns>
        public static bool ChangeCostume(SlotModel slot, TeamEnum costumeTeam)
        {
            if (slot.CostumeTeam != costumeTeam)
                slot.CostumeTeam = costumeTeam;
            return slot.CostumeTeam == costumeTeam;
        }

        #endregion CHANNEL AND ACCESS VALIDATION

        #region GAME RULES AND TOURNAMENT MODE

        public static bool ClassicModeCheck(Account p, RoomModel room, CouponEffects coupon)
        {
            string roomName = room.Name.ToLower();
            if (!roomName.Contains("@latam") && !roomName.Contains("@ligacuchillera") && !roomName.Contains("@fc") && !roomName.Contains("@camp") && !roomName.Contains("@evento3") && !roomName.Contains("@torneo") && !roomName.Contains("@ic"))
            {
                return false;
            }
            List<string> blocks = new List<string>();
            List<string> effects = new List<string>();
            PlayerEquipment equip = p.Equipment;

            Dictionary<int, string> equipLabels = new Dictionary<int, string>
            {
                { equip.WeaponPrimary, "Arma Primaria" },
                { equip.WeaponSecondary, "Arma Secundária" },
                { equip.WeaponMelee, "Cuchillo" },
                { equip.WeaponExplosive, "Grenade" },
                { equip.WeaponSpecial, "Special" },
                { equip.CharaRedId, "PJRojo" },
                { equip.CharaBlueId, "PJBlue" },
                { equip.PartFace, "Masck"},
                { equip.PartHead, "Casco" },
                { equip.DinoItem, "Dino" },
                { equip.BeretItem, "Boina" }
            };

            if (roomName.Contains("@latam"))
            {
                foreach (var ruleItem in ClassicModeManager.items_latam)
                {
                    if (equipLabels.TryGetValue(ruleItem.item_id, out var label))
                    {
                        blocks.Add($"{{col:255:255:0:255}}{label} - {ruleItem.item_name}{{/col}}");
                    }
                }
            }
            else if (roomName.Contains("@ligacuchillera"))
            {
                foreach (var ruleItem in ClassicModeManager.items_ligacuchillera)
                {
                    if (equipLabels.TryGetValue(ruleItem.item_id, out var label))
                    {
                        blocks.Add($"{{col:255:255:0:255}}{label} - {ruleItem.item_name}{{/col}}");
                    }
                }
            }
            else if (roomName.Contains("@fc"))
            {
                foreach (var ruleItem in ClassicModeManager.items_evento1)
                {
                    if (equipLabels.TryGetValue(ruleItem.item_id, out var label))
                    {
                        blocks.Add($"{{col:255:255:0:255}}{label} - {ruleItem.item_name}{{/col}}");
                    }
                }
            }
            else if (roomName.Contains("@camp"))
            {
                foreach (var ruleItem in ClassicModeManager.items_evento2)
                {
                    if (equipLabels.TryGetValue(ruleItem.item_id, out var label))
                    {
                        blocks.Add($"{{col:255:255:0:255}}{label} - {ruleItem.item_name}{{/col}}");
                    }
                }
            }
            else if (roomName.Contains("@evento3"))
            {
                foreach (var ruleItem in ClassicModeManager.items_evento3)
                {
                    if (equipLabels.TryGetValue(ruleItem.item_id, out var label))
                    {
                        blocks.Add($"{{col:255:255:0:255}}{label} - {ruleItem.item_name}{{/col}}");
                    }
                }
            }
            else if (roomName.Contains("@torneo"))
            {
                foreach (var ruleItem in ClassicModeManager.items_torneo)
                {
                    if (equipLabels.TryGetValue(ruleItem.item_id, out var label))
                    {
                        blocks.Add($"{{col:255:255:0:255}}{label} - {ruleItem.item_name}{{/col}}");
                    }
                }
            }
            else //@21N
            {
                foreach (var ruleItem in ClassicModeManager.items_ic)
                {
                    if (equipLabels.TryGetValue(ruleItem.item_id, out var label))
                    {
                        blocks.Add($"{{col:255:255:0:255}}{label} - {ruleItem.item_name}{{/col}}");
                    }
                }
            }
            if (blocks.Count > 0 || effects.Count > 0)
            {
                string blocksText = blocks.Count > 0 ? string.Join(", ", blocks.ToArray()) : "{col:255:255:0:255}-{/col}";
                string effectsText;
                if (effects.Count > 5)
                {
                    var firstFiveEffects = effects.Take(5).ToList();
                    effectsText = string.Join(", ", firstFiveEffects) + ",{col:255:255:0:255}etc{/col}";
                }
                else
                {
                    effectsText = effects.Count > 0 ? string.Join(", ", effects.ToArray()) : "{col:255:255:0:255}-{/col}";
                }

                string text = Translation.GetLabel("ClassicModeWarn", blocksText, effectsText);
                p.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(text));
                return true;
            }
            return false;
        }

        public static void ClassicModeCheck(string roomname, PlayerEquipment equip, Account p)
        {
            string t = "";
            if (roomname.Contains("@latam"))
            {
                foreach (var ruleItem in ClassicModeManager.items_latam)
                {
                    int id = ruleItem.item_id;
                    if (equip.WeaponPrimary != 104006 && id == equip.WeaponPrimary)
                    {
                        equip.WeaponPrimary = 104006; t = "Modo Latam";
                    }
                    if (equip.WeaponSecondary != 202003 && id == equip.WeaponSecondary)
                    {
                        equip.WeaponSecondary = 202003; t = "Modo Latam";
                    }
                    if (equip.WeaponMelee != 301001 && id == equip.WeaponMelee)
                    {
                        equip.WeaponMelee = 301001; t = "Modo Latam";
                    }
                    if (equip.WeaponExplosive != 407001 && id == equip.WeaponExplosive)
                    {
                        equip.WeaponExplosive = 407001; t = "Modo Latam";
                    }
                    if (equip.WeaponSpecial != 508001 && id == equip.WeaponSpecial)
                    {
                        equip.WeaponSpecial = 508001; t = "Modo Latam";
                    }
                    if (equip.PartHead != 700001 && id == equip.PartHead)
                    {
                        equip.PartHead = 700001; t = "Modo Latam";
                    }
                    if (equip.BeretItem != 2700001 && id == equip.BeretItem)
                    {
                        equip.BeretItem = 2700001; t = "Modo Latam";
                    }
                    if (equip.PartFace != 800001 && id == equip.PartFace)
                    {
                        equip.PartFace = 800001; t = "Modo Latam";
                    }
                }
            }
            if (roomname.Contains("@ligacuchillera"))
            {
                foreach (var ruleItem in ClassicModeManager.items_ligacuchillera)
                {
                    int id = ruleItem.item_id;
                    if (equip.WeaponPrimary != 104006 && id == equip.WeaponPrimary)
                    {
                        equip.WeaponPrimary = 104006; t = "Modo Liga cuchillera";
                    }
                    if (equip.WeaponSecondary != 202003 && id == equip.WeaponSecondary)
                    {
                        equip.WeaponSecondary = 202003; t = "Modo Liga cuchillera";
                    }
                    if (equip.WeaponMelee != 301001 && id == equip.WeaponMelee)
                    {
                        equip.WeaponMelee = 301001; t = "Modo Liga cuchillera";
                    }
                    if (equip.WeaponExplosive != 407001 && id == equip.WeaponExplosive)
                    {
                        equip.WeaponExplosive = 407001; t = "Modo Liga cuchillera";
                    }
                    if (equip.WeaponSpecial != 508001 && id == equip.WeaponSpecial)
                    {
                        equip.WeaponSpecial = 508001; t = "Modo Liga cuchillera";
                    }
                    if (equip.PartHead != 700001 && id == equip.PartHead)
                    {
                        equip.PartHead = 700001; t = "Modo Liga cuchillera";
                    }
                    if (equip.BeretItem != 2700001 && id == equip.BeretItem)
                    {
                        equip.BeretItem = 2700001; t = "Modo Liga cuchillera";
                    }
                    if (equip.PartFace != 800001 && id == equip.PartFace)
                    {
                        equip.PartFace = 800001; t = "Modo Liga cuchillera";
                    }
                }
            }
            if (roomname.Contains("@fc"))
            {
                foreach (var ruleItem in ClassicModeManager.items_evento1)
                {
                    int id = ruleItem.item_id;
                    if (equip.WeaponPrimary != 104006 && id == equip.WeaponPrimary)
                    {
                        equip.WeaponPrimary = 104006; t = "Modo evento 1";
                    }
                    if (equip.WeaponSecondary != 202003 && id == equip.WeaponSecondary)
                    {
                        equip.WeaponSecondary = 202003; t = "Modo evento 1";
                    }
                    if (equip.WeaponMelee != 301001 && id == equip.WeaponMelee)
                    {
                        equip.WeaponMelee = 301001; t = "Modo evento 1";
                    }
                    if (equip.WeaponExplosive != 407001 && id == equip.WeaponExplosive)
                    {
                        equip.WeaponExplosive = 407001; t = "Modo evento 1";
                    }
                    if (equip.WeaponSpecial != 508001 && id == equip.WeaponSpecial)
                    {
                        equip.WeaponSpecial = 508001; t = "Modo evento 1";
                    }
                    if (equip.PartHead != 700001 && id == equip.PartHead)
                    {
                        equip.PartHead = 700001; t = "Modo evento 1";
                    }
                    if (equip.BeretItem != 2700001 && id == equip.BeretItem)
                    {
                        equip.BeretItem = 2700001; t = "Modo evento 1";
                    }
                    if (equip.PartFace != 800001 && id == equip.PartFace)
                    {
                        equip.PartFace = 800001; t = "Modo evento 1";
                    }
                }
            }
            if (roomname.Contains("@camp"))
            {
                foreach (var ruleItem in ClassicModeManager.items_evento2)
                {
                    int id = ruleItem.item_id;
                    if (equip.WeaponPrimary != 104006 && id == equip.WeaponPrimary)
                    {
                        equip.WeaponPrimary = 104006; t = "Modo evento 2";
                    }
                    if (equip.WeaponSecondary != 202003 && id == equip.WeaponSecondary)
                    {
                        equip.WeaponSecondary = 202003; t = "Modo evento 3";
                    }
                    if (equip.WeaponMelee != 301001 && id == equip.WeaponMelee)
                    {
                        equip.WeaponMelee = 301001; t = "Modo evento 3";
                    }
                    if (equip.WeaponExplosive != 407001 && id == equip.WeaponExplosive)
                    {
                        equip.WeaponExplosive = 407001; t = "Modo evento 2";
                    }
                    if (equip.WeaponSpecial != 508001 && id == equip.WeaponSpecial)
                    {
                        equip.WeaponSpecial = 508001; t = "Modo evento 2";
                    }
                    if (equip.PartHead != 700001 && id == equip.PartHead)
                    {
                        equip.PartHead = 700001; t = "Modo evento 2";
                    }
                    if (equip.BeretItem != 2700001 && id == equip.BeretItem)
                    {
                        equip.BeretItem = 2700001; t = "Modo evento 2";
                    }
                    if (equip.PartFace != 800001 && id == equip.PartFace)
                    {
                        equip.PartFace = 800001; t = "Modo evento 2";
                    }
                }
            }
            if (roomname.Contains("@evento3"))
            {
                foreach (var ruleItem in ClassicModeManager.items_evento3)
                {
                    int id = ruleItem.item_id;
                    if (equip.WeaponPrimary != 104006 && id == equip.WeaponPrimary)
                    {
                        equip.WeaponPrimary = 104006; t = "Modo evento 3";
                    }
                    if (equip.WeaponSecondary != 202003 && id == equip.WeaponSecondary)
                    {
                        equip.WeaponSecondary = 202003; t = "Modo evento 3";
                    }
                    if (equip.WeaponMelee != 301001 && id == equip.WeaponMelee)
                    {
                        equip.WeaponMelee = 301001; t = "Modo evento 3";
                    }
                    if (equip.WeaponExplosive != 407001 && id == equip.WeaponExplosive)
                    {
                        equip.WeaponExplosive = 407001; t = "Modo evento 3";
                    }
                    if (equip.WeaponSpecial != 508001 && id == equip.WeaponSpecial)
                    {
                        equip.WeaponSpecial = 508001; t = "Modo evento 3";
                    }
                    if (equip.PartHead != 700001 && id == equip.PartHead)
                    {
                        equip.PartHead = 700001; t = "Modo evento 3";
                    }
                    if (equip.BeretItem != 2700001 && id == equip.BeretItem)
                    {
                        equip.BeretItem = 2700001; t = "Modo evento 3";
                    }
                    if (equip.PartFace != 800001 && id == equip.PartFace)
                    {
                        equip.PartFace = 800001; t = "Modo evento 3";
                    }
                }
            }
            else if (roomname.Contains("@torneo"))
            {
                foreach (var ruleItem in ClassicModeManager.items_torneo)
                {
                    int id = ruleItem.item_id;
                    if (equip.WeaponPrimary != 104006 && id == equip.WeaponPrimary)
                    {
                        equip.WeaponPrimary = 104006; t = "Modo Torneo";
                    }
                    if (equip.WeaponSecondary != 202003 && id == equip.WeaponSecondary)
                    {
                        equip.WeaponSecondary = 202003; t = "Modo Torneo";
                    }
                    if (equip.WeaponMelee != 301001 && id == equip.WeaponMelee)
                    {
                        equip.WeaponMelee = 301001; t = "Modo Torneo";
                    }
                    if (equip.WeaponExplosive != 407001 && id == equip.WeaponExplosive)
                    {
                        equip.WeaponExplosive = 407001; t = "Modo Torneo";
                    }
                    if (equip.WeaponSpecial != 508001 && id == equip.WeaponSpecial)
                    {
                        equip.WeaponSpecial = 508001; t = "Modo Torneo";
                    }
                    if (equip.PartHead != 700001 && id == equip.PartHead)
                    {
                        equip.PartHead = 700001; t = "Modo Torneo";
                    }
                    if (equip.BeretItem != 2700001 && id == equip.BeretItem)
                    {
                        equip.BeretItem = 2700001; t = "Modo Torneo";
                    }
                    if (equip.PartFace != 800001 && id == equip.PartFace)
                    {
                        equip.PartFace = 800001; t = "Modo Torneo";
                    }
                }
            }
            else if (roomname.Contains("@ic"))
            {
                foreach (var ruleItem in ClassicModeManager.items_ic)
                {
                    int id = ruleItem.item_id;
                    if (equip.WeaponPrimary != 104006 && id == equip.WeaponPrimary)
                    {
                        equip.WeaponPrimary = 104006; t = "Modo IC";
                    }
                    if (equip.WeaponSecondary != 202003 && id == equip.WeaponSecondary)
                    {
                        equip.WeaponSecondary = 202003; t = "Modo IC";
                    }
                    if (equip.WeaponMelee != 301001 && id == equip.WeaponMelee)
                    {
                        equip.WeaponMelee = 301001; t = "Modo IC";
                    }
                    if (equip.WeaponExplosive != 407001 && id == equip.WeaponExplosive)
                    {
                        equip.WeaponExplosive = 407001; t = "Modo IC";
                    }
                    if (equip.WeaponSpecial != 508001 && id == equip.WeaponSpecial)
                    {
                        equip.WeaponSpecial = 508001; t = "Modo IC";
                    }
                    if (equip.PartHead != 700001 && id == equip.PartHead)
                    {
                        equip.PartHead = 700001; t = "Modo IC";
                    }
                    if (equip.BeretItem != 2700001 && id == equip.BeretItem)
                    {
                        equip.BeretItem = 2700001; t = "Modo IC";
                    }
                    if (equip.PartFace != 800001 && id == equip.PartFace)
                    {
                        equip.PartFace = 800001; t = "Modo IC";
                    }
                }
            }
            if (t != "")
            {
                p.SendPacket(new PROTOCOL_LOBBY_CHATTING_ACK("Rules", 0, 2, false, "Esta arma no esta permitida en esta sala!" + t));
            }
        }

        #endregion GAME RULES AND TOURNAMENT MODE

        #region ROOM CAPACITY AND LIMITS

        /// <summary>
        /// Verifica el límite de 4vs4
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="isLeader">Si es el líder</param>
        /// <param name="playerFR">Jugadores FR (erencia)</param>
        /// <param name="playerCT">Jugadores CT (erencia)</param>
        /// <param name="totalEnemies">Total de enemigos (erencia)</param>
        /// <returns>True si se aplicó el límite</returns>
        public static bool Check4vs4(RoomModel room, bool isLeader, int playerFR, int playerCT, int totalEnemies)
        {
            if (!isLeader)
                return playerFR + playerCT >= 8;

            int totalPlayers = playerFR + playerCT + 1;
            if (totalPlayers > 8)
            {
                int playersToRemove = totalPlayers - 8;
                if (playersToRemove > 0)
                {
                    for (int index = 15; index >= 0; --index)
                    {
                        if (index != room.Leader)
                        {
                            SlotModel slot = room.GetSlot(index);
                            if (slot != null && slot.State == SlotState.READY)
                            {
                                room.ChangeSlotState(index, SlotState.NORMAL, false);
                                if (index % 2 != 0)
                                    --playerCT;
                                else
                                    --playerFR;
                                if (--playersToRemove == 0)
                                    break;
                            }
                        }
                    }
                    room.UpdateSlotsInfo();
                    totalEnemies = room.Leader % 2 == 0 ? playerCT : playerFR;
                    return true;
                }
            }
            return false;
        }

        #endregion ROOM CAPACITY AND LIMITS

        #region SEASON PASS AND BATTLE PASS

        public static void UpdateSeasonPass(Account Player)
        {
            // Verificar si hay una temporada activa
            BattlePassSeason activeSeason = BattlePassManager.GetActiveSeason();
            if (activeSeason == null)
            {
                return;
            }

            // Guardar los niveles actuales antes de actualizar
            int currentNormalLevel = Player.Battlepass.BattlepassNormalLevel;
            int currentPremiumLevel = Player.Battlepass.BattlepassPremiumLevel;

            // Enviar información actualizada al cliente
            Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_SEASON_CHANGE());
            Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(Player));

            // Obtener las cartas/niveles
            List<BattlePassCardData> cards = BattlePassManager.GetCards();
            if (cards == null || cards.Count == 0)
            {
                
                return;
            }

            // Calcular el nivel completado actual basado en la experiencia acumulada
            int completedLevel = BattlePass.GetCompletedLevel(Player.Battlepass.EarnedPoints, cards);

           

            // Comprobar si hay nuevos niveles completados para entregar recompensas
            if (completedLevel > currentNormalLevel)
            {
                //CLogger.Print($"Jugador {Player.Nickname} tiene {completedLevel - currentNormalLevel} nuevos niveles para reclamar", LoggerType.Info);

                // Entregar recompensas para cada nivel completado nuevo
                for (int level = currentNormalLevel; level < completedLevel && level < cards.Count; level++)
                //for (int level = currentNormalLevel; level < completedLevel; level++)
                {
                    if (level < cards.Count)
                    {
                        // Obtener la carta/datos del nivel actual
                        BattlePassCardData levelCard = cards[level];

                        // Entregar recompensa normal
                        if (!Player.Battlepass.HavePremium)
                        {
                            Task.Run(() =>
                            {
                                BattlepassCardReward(Player, levelCard.NormalCard, 0, 0);
                                Task.Delay(1000).Wait(); // Esperar 1 segundo entre entregas
                            });
                        }

                        //CLogger.Print($"Entregada recompensa normal del nivel {level + 1}: Item {levelCard.NormalCard}", LoggerType.Info);

                        // Aumentar nivel normal
                        Player.Battlepass.BattlepassNormalLevel++;
                        ComDiv.UpdateDB("player_battlepass", "battlepass_normal_levels", Player.Battlepass.BattlepassNormalLevel, "owner_id", Player.PlayerId);

                        // Si tiene premium, entregar recompensas premium
                        if (Player.Battlepass.HavePremium && currentPremiumLevel <= level)
                        {
                            // Entregar recompensa premium A
                            Task.Run(() =>
                            {
                                BattlepassCardReward(Player, levelCard.NormalCard, levelCard.PremiumCardA, levelCard.PremiumCardB);
                                Task.Delay(1000).Wait(); // Esperar 1 segundo entre entregas
                            });
                            //CLogger.Print($"Entregada recompensa premium A del nivel {level + 1}: Item {levelCard.PremiumCardA}", LoggerType.Info);
                            //CLogger.Print($"Entregada recompensa premium B del nivel {level + 1}: Item {levelCard.PremiumCardB}", LoggerType.Info);

                            // Aumentar nivel premium
                            Player.Battlepass.BattlepassPremiumLevel++;
                            ComDiv.UpdateDB("player_battlepass", "battlepass_premium_levels", Player.Battlepass.BattlepassPremiumLevel, "owner_id", Player.PlayerId);
                        }
                    }
                }
            }
        }

        public static void CalculateSeasonExpPoints(Account Player, PlayerBonus Bonus, SlotModel Slot, BattlePassSeason CurrentBattlepass)
        {
            PlayerBattlepass PlayerBattlepass = Player.Battlepass;
            if (CurrentBattlepass == null || PlayerBattlepass == null)
            {
                return;
            }
            int BonusSeasonPoint = 0;
            if ((Bonus.Bonuses & 512) == 512)
            {
                BonusSeasonPoint += 20;
            }
            if ((Bonus.Bonuses & 1024) == 1024)
            {
                BonusSeasonPoint += 30;
            }
            if ((Bonus.Bonuses & 2048) == 2048)
            {
                BonusSeasonPoint += 50;
            }
            if ((Bonus.Bonuses & 4096) == 4096)
            {
                BonusSeasonPoint += 80;
            }
            Slot.BonusSeasonPoint += BonusSeasonPoint;
            if (PlayerBattlepass.BattlepassId == int.Parse(CurrentBattlepass.SeasonId))
            {
                if (PlayerBattlepass.BattlepassNormalLevel >= BattlePassManager.GetCards().Count)
                {
                    Player.UpdateSeasonpass = true;
                }
                else
                {
                    //Slot.SeasonPoint += ComDiv.Percentage(Slot.Exp, 35);
                    Slot.SeasonPoint += ComDiv.Percentage(Slot.Exp, 15);
                    
                    PlayerBattlepass.EarnedPoints += Slot.SeasonPoint + ComDiv.Percentage(Slot.SeasonPoint, 15);
                    Player.UpdateSeasonpass = true;
                    
                    ComDiv.UpdateDB("player_battlepass", "earned_points", PlayerBattlepass.EarnedPoints, "owner_id", Player.PlayerId);
                }
            }
        }

        public static void ProcessBattlepassPremiumBuy(Account Player)
        {
            try
            {
                PlayerBattlepass SeasonPass = Player.Battlepass;
                if (SeasonPass != null)
                {
                    BattlePassSeason SeasonData = BattlePassManager.GetActiveSeason();
                    List<BattlePassCardData> cards = BattlePassManager.GetCards();  // Obtener las cartas por separado
                    if (SeasonData == null || cards == null || cards.Count == 0)
                    {
                        CLogger.Print($"LevelUpNormalBattlePass {SeasonData} Cards: {(cards != null ? cards.Count : 0)}", LoggerType.Warning);
                        return;
                    }
                    SeasonPass.HavePremium = true;
                    if (SeasonPass.BattlepassNormalLevel > SeasonPass.BattlepassPremiumLevel)
                    {
                        while (SeasonPass.BattlepassPremiumLevel != SeasonPass.BattlepassNormalLevel)
                        {
                            BattlePassCardData ActualCard = cards[SeasonPass.BattlepassPremiumLevel];
                            BattlepassCardReward(Player, 0, ActualCard.PremiumCardA, ActualCard.PremiumCardB);
                            SeasonPass.BattlepassPremiumLevel++;
                        }
                    }
                    ComDiv.UpdateDB("player_battlepass", "owner_id", Player.PlayerId, new string[] { "battlepass_premium_levels", "battlepass_premium" }, new object[] { SeasonPass.BattlepassPremiumLevel, SeasonPass.HavePremium });
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.ToString(), LoggerType.Error);
            }
        }

        public static void BattlepassCardReward(Account Player, int NormalCard, int PremiumCardA, int PremiumCardB)
        {
            if (Player != null)
            {
                // Process and sends the normal card if exist

                if (NormalCard != 0)
                {
                    GoodsItem GoodNormal = ShopManager.GetGood(NormalCard);
                    if (GoodNormal != null)
                    {
                        if (ComDiv.GetIdStatics(GoodNormal.Item.Id, 1) == 6 && Player.Character.GetCharacter(GoodNormal.Item.Id) == null)
                        {
                            CreateCharacter(Player, GoodNormal.Item);
                        }
                        else
                        {
                            Player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, Player, GoodNormal.Item));
                        }
                    }
                }

                // Process this PremiumCardA if exist
                if (PremiumCardA != 0)
                {
                    GoodsItem GoodPremiumA = ShopManager.GetGood(PremiumCardA);
                    if (GoodPremiumA != null)
                    {
                        if (ComDiv.GetIdStatics(GoodPremiumA.Item.Id, 1) == 6 && Player.Character.GetCharacter(GoodPremiumA.Item.Id) == null)
                        {
                            CreateCharacter(Player, GoodPremiumA.Item);
                        }
                        else
                        {
                            Player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, Player, GoodPremiumA.Item));
                        }
                    }
                }

                // Process this PremiumCardB if exist
                if (PremiumCardB != 0)
                {
                    GoodsItem GoodPremiumB = ShopManager.GetGood(PremiumCardB);
                    if (GoodPremiumB != null)
                    {
                        if (ComDiv.GetIdStatics(GoodPremiumB.Item.Id, 1) == 6 && Player.Character.GetCharacter(GoodPremiumB.Item.Id) == null)
                        {
                            CreateCharacter(Player, GoodPremiumB.Item);
                        }
                        else
                        {
                            Player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, Player, GoodPremiumB.Item));
                        }
                    }
                }

                // Enviamos el paquete con las tres recompensas
                Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_SEND_REWARD_ACK(0, new int[] { NormalCard, PremiumCardA, PremiumCardB }));
            }
        }

        #endregion SEASON PASS AND BATTLE PASS

        #region COMPETITIVE SYSTEM

        /// <summary>
        /// Envía información competitiva al jugador
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>

        public static void SendCompetitiveInfo(Account player)
        {
            try
            {
                player.SendPacket(new PROTOCOL_LOBBY_CHATTING_ACK(Translation.GetLabel("Competitive"),
                    player.Session.SessionId, player.NickColor, true,
                    Translation.GetLabel("CompetitiveRank", (object)player.Competitive.Rank().Name,
                    (object)player.Competitive.Points, (object)player.Competitive.Rank().Points)));
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.ToString(), LoggerType.Error);
            }
        }

        /// <summary>
        /// Calcula puntos competitivos al final de una partida
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="slot">Slot del jugador</param>
        /// <param name="hasWon">Si el jugador ganó</param>

        public static void CalculateCompetitive(RoomModel room, Account player, SlotModel slot, bool hasWon)
        {
            if (!room.Competitive)
                return;

            int pointsEarned = (hasWon ? 50 : -30) + 2 * slot.AllKills + slot.AllAssists - slot.AllDeaths;
            player.Competitive.Points += pointsEarned;

            if (player.Competitive.Points < 0)
                player.Competitive.Points = 0;

            UpdateCompetitiveRank(player.Competitive);

            string pointsMessage = Translation.GetLabel("CompetitivePointsEarned", (object)pointsEarned);
            string rankMessage = Translation.GetLabel("CompetitiveRank", (object)player.Competitive.Rank().Name,
                (object)player.Competitive.Points, (object)player.Competitive.Rank().Points);

            player.SendPacket(new PROTOCOL_LOBBY_CHATTING_ACK(Translation.GetLabel("Competitive"),
                player.Session.SessionId, player.NickColor, true, $"{pointsMessage}\n\r{rankMessage}"));
        }

        /// <summary>
        /// Actualiza el rango competitivo del jugador
        /// </summary>
        /// <param name="competitive">Datos competitivos del jugador</param>

        private static void UpdateCompetitiveRank(PlayerCompetitive competitive)
        {
            CompetitiveRank competitiveRank = CompetitiveXML.Ranks.FirstOrDefault<CompetitiveRank>(
                rank => competitive.Points >= rank.Points);

            int newLevel = competitiveRank == null ? competitive.Level : competitiveRank.Id;

            ComDiv.UpdateDB("player_competitive", "points", (object)competitive.Points, "owner_id", (object)competitive.OwnerId);

            if (newLevel == competitive.Level ||
                !ComDiv.UpdateDB("player_competitive", "level", (object)newLevel, "owner_id", (object)competitive.OwnerId))
                return;

            competitive.Level = newLevel;
        }

        /// <summary>
        /// Verifica si se puede abrir un slot en modo competitivo
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="openingSlot">Slot que se quiere abrir</param>
        /// <returns>True si se puede abrir</returns>
        public static bool CanOpenSlotCompetitive(RoomModel room, SlotModel openingSlot)
        {
            return room.Slots.Where(slot => slot.Team == openingSlot.Team && slot.State != SlotState.CLOSE).Count() < 5;
        }

        /// <summary>
        /// Verifica si se puede cerrar un slot en modo competitivo
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="closingSlot">Slot que se quiere cerrar</param>
        /// <returns>True si se puede cerrar</returns>
        public static bool CanCloseSlotCompetitive(RoomModel room, SlotModel closingSlot)
        {
            return room.Slots.Where(slot => slot.Team == closingSlot.Team && slot.State != SlotState.CLOSE).Count() > 3;
        }

        /// <summary>
        /// Crea personajes desde los ítems del inventario
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        private static void CreateCharactersFromInventoryItems(Account player)
        {
            List<ItemsModel> items = player.Inventory.Items;
            lock (items)
            {
                foreach (ItemsModel item in items)
                {
                    if (ComDiv.GetIdStatics(item.Id, 1) == 6 && player.Character.GetCharacter(item.Id) == null)
                        CreateCharacter(player, item);
                }
            }
        }

        /// <summary>
        /// Crea un nuevo personaje para el jugador
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="item">Ítem de personaje</param>

        public static void CreateCharacter(Account player, ItemsModel item)
        {
            int characterCount = player.Character.Characters.Count;

            CharacterModel newCharacter = new CharacterModel
            {
                Id = item.Id,
                Name = item.Name,
                Slot = characterCount,
                CreateDate = uint.Parse(DateTimeUtil.Now("yyMMddHHmm")),
                PlayTime = 0U
            };

            player.Character.AddCharacter(newCharacter);
            player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, player, item));

            if (DaoManagerSQL.CreatePlayerCharacter(newCharacter, player.PlayerId))
            {
                player.SendPacket(new PROTOCOL_CHAR_CREATE_CHARA_ACK(0U, 3, newCharacter, player));
            }
            else
            {
                player.SendPacket(new PROTOCOL_CHAR_CREATE_CHARA_ACK(0x80000000, 255, null, null));
            }
        }

        /// <summary>
        /// Envía recompensas del Battle Pass
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="rewardGoods">Array de IDs de recompensas</param>
        ///
        private static void SendBattlePassRewards(Account player, int[] rewardGoods)
        {
            foreach (int goodId in rewardGoods)
            {
                if (goodId != 0)
                {
                    GoodsItem good = ShopManager.GetGood(goodId);
                    if (good != null)
                    {
                        if (ComDiv.GetIdStatics(good.Item.Id, 1) == 6 && player.Character.GetCharacter(good.Item.Id) == null)
                        {
                            CreateCharacter(player, good.Item);
                        }
                        else
                        {
                            player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, player, good.Item));
                        }
                    }
                }
            }

            // kirim ACK reward (versi array)
            player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_SEND_REWARD_ACK(0U, rewardGoods));
        }

        /// <summary>
        /// Genera recompensas aleatorias por tipo
        /// </summary>
        /// <param name="player">Cuenta del jugador</param>
        /// <param name="rewardType">Tipo de recompensa</param>
        /// <returns>ID del ítem otorgado</returns>

        private static int GenerateRandomReward(Account player, BattleRewardType rewardType)
        {
            Random random = new Random();
            BattleRewardModel reward = BattleRewardXML.GetRewardType(rewardType);

            if (reward == null || random.Next(100) >= reward.Percentage)
                return 0;

            GoodsItem good = ShopManager.GetGood(reward.Rewards[random.Next(reward.Rewards.Length)]);
            if (good == null)
                return 0;

            player.SendPacket(new PROTOCOL_BASE_NEW_REWARD_POPUP_ACK(player, good.Item));

            if (ComDiv.GetIdStatics(good.Item.Id, 1) == 29)
            {
                int tagValue = 0;
                switch (good.Item.Id)
                {
                    case 2900001: tagValue = 1; break;
                    case 2900002: tagValue = 2; break;
                    case 2900003: tagValue = 3; break;
                    case 2900004: tagValue = 4; break;
                    case 2900005: tagValue = 5; break;
                }

                player.Tags += tagValue;
                ComDiv.UpdateDB("accounts", "tags", (object)player.Tags, "player_id", (object)player.PlayerId);
                return good.Item.Id;
            }

            if (ComDiv.GetIdStatics(good.Item.Id, 1) == 6 && player.Character.GetCharacter(good.Item.Id) == null)
                CreateCharacter(player, good.Item);
            else
                player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, player, good.Item));

            return good.Item.Id;
        }

        /// <summary>
        /// Obtiene datos de recompensas para los mejores jugadores
        /// </summary>
        /// <param name="room">Sala de juego</param>
        /// <param name="slots">Lista de slots</param>
        /// <returns>Tupla con datos de recompensas</returns>
        public static (byte[], int[]) GetRewardData(RoomModel room, List<SlotModel> slots)
        {
            byte[] playerSlots = new byte[5];
            int[] rewardItems = new int[5];
            int rewardIndex = 0;

            for (int index = 0; index < 5; ++index)
            {
                playerSlots[index] = byte.MaxValue;
                rewardItems[index] = 0;
            }

            if (!room.IsBotMode() && slots.Count > 0)
            {
                SlotModel mvpSlot = slots.Where(slot => slot.Score > 0)
                    .OrderByDescending(slot => slot.Score).FirstOrDefault();
                if (mvpSlot != null)
                    ProcessSlotReward(mvpSlot, BattleRewardType.MVP, room, rewardIndex, playerSlots, rewardItems);

                SlotModel assistKingSlot = slots.Where(slot => slot.AllAssists > 0)
                    .OrderByDescending(slot => slot.AllAssists).FirstOrDefault();
                if (assistKingSlot != null)
                    ProcessSlotReward(assistKingSlot, BattleRewardType.AssistKing, room, rewardIndex, playerSlots, rewardItems);

                SlotModel multiKillSlot = slots.Where(slot => slot.KillsOnLife > 0)
                    .OrderByDescending(slot => slot.KillsOnLife).FirstOrDefault();
                if (multiKillSlot != null)
                    ProcessSlotReward(multiKillSlot, BattleRewardType.MultiKill, room, rewardIndex, playerSlots, rewardItems);
            }

            return (playerSlots, rewardItems);
        }

        /// <summary>
        /// Procesa recompensa para un slot específico (método original StaticMethod23)
        /// </summary>
        [CompilerGenerated]
        internal static void ProcessSlotReward(SlotModel slot, BattleRewardType rewardType, RoomModel room,
             int rewardIndex, byte[] playerSlots, int[] rewardItems)
        {
            Account player;
            if (rewardIndex >= 5 || !room.GetPlayerBySlot(slot, out player))
                return;

            playerSlots[rewardIndex] = (byte)slot.Id;
            rewardItems[rewardIndex] = GenerateRandomReward(player, rewardType);
            ++rewardIndex;
        }

        /// <summary>
        /// Inicializa el rango de un bot según su nivel
        /// </summary>
        /// <param name="botLevel">Nivel del bot</param>
        /// <returns>Rango asignado</returns>
        public static int InitBotRank(int botLevel)
        {
            Random random = new Random();
            switch (botLevel)
            {
                case 1: return random.Next(0, 4);
                case 2: return random.Next(5, 9);
                case 3: return random.Next(10, 14);
                case 4: return random.Next(15, 19);
                case 5: return random.Next(20, 24);
                case 6: return random.Next(25, 29);
                case 7: return random.Next(30, 34);
                case 8: return random.Next(35, 39);
                case 9: return random.Next(40, 44);
                case 10: return random.Next(45, 49);
                default: return 52;
            }
        }

        #endregion COMPETITIVE SYSTEM
    }
}