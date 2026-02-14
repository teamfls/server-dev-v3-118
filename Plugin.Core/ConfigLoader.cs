using Plugin.Core.Enums;
using Plugin.Core.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Plugin.Core
{
    public static class ConfigLoader
    {
        public static string[] HOST = new string[3]
        {
        "0.0.0.0",
        "0.0.0.0",
        "0.0.0.0"
        };

        public static readonly int[] DEFAULT_PORT = new int[3]
        {
        39190,
        39191,
        40009
        };

        public static readonly int[] PROXY_PORT = new int[3]
        {
        23850,
        23851,
        24669
        };

        public static string RconIp;
        public static string RconPassword;
        public static int RconPort;

        public static bool RconEnable;
        public static bool RconInfoCommand;
        public static bool RconPrintNotValidIp;
        public static bool RconNotValidIpEnable;
        public static List<string> RconValidIps;

        public static string DatabaseName;
        public static string DatabaseHost;
        public static string DatabaseUsername;
        public static string DatabasePassword;
        public static string UdpVersion;
        public static string RandomPasswordChars;
        public static string CryptedPasswordSalt;
        public static bool IsUseProxy;
        public static bool IsTestMode;
        public static bool ShowMoreInfo;
        public static bool AutoAccount;
        public static bool DebugMode;
        public static bool WinCashPerBattle;
        public static bool ShowCashReceiveWarn;
        public static bool AutoBan;
        public static bool SendInfoToServ;
        public static bool SendFailMsg;
        public static bool EnableLog;
        public static bool UseMaxAmmoInDrop;
        public static bool UseHitMarker;
        public static bool ICafeSystem;
        public static bool IsDebugPing;
        public static bool CustomYear;
        public static bool AntiScript;
        public static bool SendPingSync;
        public static bool TournamentRule;
        public static bool RandomPassword;
        public static bool UseCryptedPassword;
        public static int DatabasePort;
        public static int ConfigId;
        public static int MaxNickSize;
        public static int MinNickSize;
        public static int MaxUserSize;
        public static int MinUserSize;
        public static int MaxPassSize;
        public static int MinPassSize;
        public static int BackLog;
        public static int MaxLatency;
        public static int MaxRepeatLatency;
        public static int MaxActiveClans;
        public static int MinRankVote;
        public static int MaxExpReward;
        public static int MaxGoldReward;
        public static int MaxCashReward;
        public static int MinCreateGold;
        public static int MinCreateRank;
        public static int InternetCafeId;
        public static int BackYear;
        public static int PingUpdateTimeSeconds;
        public static int PlayersServerUpdateTimeSeconds;
        public static int UpdateIntervalPlayersServer;
        public static int EmptyRoomRemovalInterval;
        public static int ConsoleTitleUpdateTimeSeconds;
        public static int IntervalEnterRoomAfterKickSeconds;
        public static int MaxBuyItemDays;
        public static int MaxBuyItemUnits;
        public static int BattleRewardId;
        public static int MaxConnectionPerIp;
        public static float MaxClanPoints;
        public static float PlantDuration;
        public static float DefuseDuration;
        public static ushort MaxDropWpnCount;
        public static UdpState UdpType;
        public static NationsEnum National;
        public static List<ClientLocale> GameLocales;

        static ConfigLoader()
        {
            LoadTimeline();
            Load();
            LoadRcon();
        }

        private static void Load()
        {
            ConfigEngine configEngine = new ConfigEngine("Config/Settings.ini", FileAccess.Read);
            HOST = new string[3]
            {
              configEngine.ReadS("PrivateIp4Address", "127.0.0.1", "Server"),
              configEngine.ReadS("ProxyIp4Address", "127.0.0.1", "Server"),
              configEngine.ReadS("PublicIp4Address", "127.0.0.1", "Server")
            };
            DatabaseHost = configEngine.ReadS("Host", "localhost", "Database");
            DatabaseName = configEngine.ReadS("Name", "", "Database");
            DatabaseUsername = configEngine.ReadS("User", "root", "Database");
            DatabasePassword = configEngine.ReadS("Pass", "", "Database");
            DatabasePort = configEngine.ReadD("Port", 0, "Database");
            ConfigId = configEngine.ReadD("ConfigId", 1, "Server");
            BackLog = configEngine.ReadD("BackLog", 3, "Server");
            DebugMode = configEngine.ReadX("Debug", false, "Server");
            IsTestMode = configEngine.ReadX("Test", false, "Server");
            ShowMoreInfo = configEngine.ReadX("MoreInfo", false, "Server");
            IsDebugPing = configEngine.ReadX("DebugPing", false, "Server");
            AutoBan = configEngine.ReadX("AutoBan", false, "Server");
            ICafeSystem = configEngine.ReadX("ICafeSystem", true, "Server");
            InternetCafeId = configEngine.ReadD("InternetCafeId", 1, "Server");
            IsUseProxy = configEngine.ReadX("UseProxy", true, "Server");
            AutoAccount = configEngine.ReadX("AutoAccount", false, "Essentials");
            TournamentRule = configEngine.ReadX("TournamentRule", false, "Essentials");
            RandomPassword = configEngine.ReadX("RandomPassword", false, "Essentials");
            RandomPasswordChars = configEngine.ReadS("RandomPasswordChars", "", "Essentials");
            UseCryptedPassword = configEngine.ReadX("UseCryptedPassword", true, "Essentials");
            CryptedPasswordSalt = configEngine.ReadS("CryptedPasswordSalt", "", "Essentials");
            MinRankVote = configEngine.ReadD("MinRankVote", 0, "Internal");
            WinCashPerBattle = configEngine.ReadX("WinCashPerBattle", true, "Internal");
            ShowCashReceiveWarn = configEngine.ReadX("ShowCashReceiveWarn", true, "Internal");
            MaxExpReward = configEngine.ReadD("MaxExpReward", 1000, "Internal");
            MaxGoldReward = configEngine.ReadD("MaxGoldReward", 1000, "Internal");
            MaxCashReward = configEngine.ReadD("MaxCashReward", 1000, "Internal");
            MinCreateRank = configEngine.ReadD("MinCreateRank", 15, "Internal");
            MinCreateGold = configEngine.ReadD("MinCreateGold", 7500, "Internal");
            MaxClanPoints = configEngine.ReadT("MaxClanPoints", 0.0f, "Internal");
            MaxActiveClans = configEngine.ReadD("MaxActiveClans", 0, "Internal");
            MaxLatency = configEngine.ReadD("MaxLatency", 0, "Internal");
            MaxRepeatLatency = configEngine.ReadD("MaxRepeatLatency", 0, "Internal");
            BattleRewardId = configEngine.ReadD("BattleRewardId", 1, "Internal");
            UdpType = (UdpState)configEngine.ReadC("UdpType", (byte)3, "Others");
            UdpVersion = configEngine.ReadS("UdpVersion", "1508.7", "Others");
            SendInfoToServ = configEngine.ReadX("SendInfoToServ", true, "Others");
            SendPingSync = configEngine.ReadX("SendPingSync", true, "Others");
            EnableLog = configEngine.ReadX("EnableLog", false, "Others");
            SendFailMsg = configEngine.ReadX("SendFailMsg", true, "Others");
            UseHitMarker = configEngine.ReadX("UseHitMarker", false, "Others");
            UseMaxAmmoInDrop = configEngine.ReadX("UseMaxAmmoInDrop", false, "Others");
            MaxDropWpnCount = configEngine.ReadUH("MaxDropWpnCount", (ushort)0, "Others");
            AntiScript = configEngine.ReadX("AntiScript", true, "Others");
            GameLocales = new List<ClientLocale>();
            National = (NationsEnum)Enum.Parse(typeof(NationsEnum), configEngine.ReadS("National", "Global", "Essentials"));
            string str1 = configEngine.ReadS("Region", "None", "Essentials");
            char[] chArray = new char[1] { ',' };
            foreach (string str2 in str1.Split(chArray))
            {
                ClientLocale result;
                Enum.TryParse<ClientLocale>(str2, out result);
                GameLocales.Add(result);
            }
            MinUserSize = 4;
            MaxUserSize = 16 /*0x10*/;
            MinPassSize = 4;
            MaxPassSize = 16 /*0x10*/;
            MinNickSize = 4;
            MaxNickSize = 16 /*0x10*/;
            PingUpdateTimeSeconds = 7;
            PlayersServerUpdateTimeSeconds = 7;
            UpdateIntervalPlayersServer = 2;
            EmptyRoomRemovalInterval = 2;
            ConsoleTitleUpdateTimeSeconds = 3;
            IntervalEnterRoomAfterKickSeconds = 30;
            MaxBuyItemDays = 365;
            MaxBuyItemUnits = 100000;
            PlantDuration = 5.5f;
            DefuseDuration = 7.1f;
            MaxConnectionPerIp = 2;
        }

        public static void LoadRcon()
        {
            /** Rcon Section **/
            ConfigEngine CFG = new ConfigEngine("Config/Rcon.ini", FileAccess.Read);
            RconEnable = CFG.ReadX("RconEnable", false, "Rcon");
            RconIp = CFG.ReadS("RconIp", "127.0.0.1", "Rcon");
            RconPassword = CFG.ReadS("RconPassword", "", "Rcon");
            RconPort = CFG.ReadD("RconPort", 39189, "Rcon");
            RconInfoCommand = CFG.ReadX("RconInfo", false, "Rcon");
            RconPrintNotValidIp = CFG.ReadX("RconPrintNotValidIp", false, "Rcon");
            RconNotValidIpEnable = CFG.ReadX("RconNotValidIpEnable", false, "Rcon");
            RconValidIps = new List<string>();
            string Ips = CFG.ReadS("RconValidIps", "127.0.0.1", "Rcon");
            if (Ips.Contains(";"))
            {
                RconValidIps.AddRange(Ips.Split(';'));
            }
            else RconValidIps.Add(Ips);
        }

        private static void LoadTimeline()
        {
            ConfigEngine configEngine = new ConfigEngine("Config/Timeline.ini", FileAccess.Read);
            CustomYear = configEngine.ReadX("CustomYear", false, "Addons");
            BackYear = configEngine.ReadD("BackYear", 10, "Runtime");
        }
    }
}