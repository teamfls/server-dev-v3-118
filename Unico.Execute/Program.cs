using Executable.Forms;
using Executable.UDP;
using Executable.UDP.Server;
using Executable.Utility;
using Plugin.Core;
using Plugin.Core.Colorful;
using Plugin.Core.Enums;
using Plugin.Core.Filters;
using Plugin.Core.JSON;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.RAW;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using Server.Auth;
using Server.Game;
using Server.Game.Data.Managers;
using Server.Game.Rcon;
using Server.Match;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Console = Plugin.Core.Colorful.Console;

namespace Executable
{
    public class Program
    {
        #region Constants

        private const string SERVER_VERSION = "V3.80";
        private const int CONSOLE_WIDTH = 160;
        private const int CONSOLE_HEIGHT = 40;
        private const int COMMUNICATION_PORT = 1909;
        private const int STARTUP_DELAY_MS = 250;
        private const int TITLE_UPDATE_INTERVAL_MS = 1000;
        private const int DAILY_RESET_CHECK_TIME = 000000;
        private const int SHUTDOWN_DELAY_MS = 1000;
        private const string ADMINISTRATOR_TITLE = "ADMINISTRATOR";
        private const string SUPERUSER_TITLE = "SUPERUSER";
        private const string MONITOR_ARGUMENT = "-supc";

        #endregion Constants

        #region Windows API

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);

        private enum CtrlType
        { CTRL_C_EVENT = 0, CTRL_BREAK_EVENT = 1, CTRL_CLOSE_EVENT = 2, CTRL_LOGOFF_EVENT = 5, CTRL_SHUTDOWN_EVENT = 6 }

        private static bool WindowsEventHandler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    CleanupFirewallRules();
                    return true;

                default:
                    return false;
            }
        }

        #endregion Windows API

        #region Fields

        private static string titleSuffix = "";
        private static Mutex applicationMutex = null;
        private static readonly FileInfo executableInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
        private static readonly int currentProcessId = Process.GetCurrentProcess().Id;
        private static readonly ServerManager serverManager = new ServerManager();

        #endregion Fields

        #region Main Entry

        [STAThread]
        protected static void Main(string[] args)
        {
            try
            {
                SetupExceptionHandlers();
                InitializeConsole();

                if (!ValidateSingleInstance()) return;

                ConfigureSystemSettings();

                bool includeMonitor = ShouldIncludeMonitorForm(args);
                if (includeMonitor) StartMonitorFormAsync();

                string fileVersion = GetApplicationVersion();
                StartServerSystems(includeMonitor, fileVersion);
            }
            catch (Exception ex)
            {
                CLogger.Print($"Critical startup error: {ex.Message}", LoggerType.Error, ex);
            }
            finally
            {
                CleanupResources();
            }
        }

        #endregion Main Entry

        #region Initialization

        private static void SetupExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Console.CancelKeyPress += OnCancelKeyPress;
        }

        private static void InitializeConsole()
        {
            try
            {
                Console.SetWindowSize(CONSOLE_WIDTH, CONSOLE_HEIGHT);
                Console.CursorVisible = false;
                Console.TreatControlCAsInput = false;
                WindowUtility.MoveWindowToCenter();

                string fileVersion = GetApplicationVersion();
                Console.Title = $"Point Blank (RUS-{SERVER_VERSION}) Server {fileVersion}";

                TerminateExistingInstances();
            }
            catch (Exception ex)
            {
                CLogger.Print($"Console initialization error: {ex.Message}", LoggerType.Warning, ex);
            }
        }

        private static bool ValidateSingleInstance()
        {
            applicationMutex = new Mutex(true, executableInfo.Name, out bool isFirstInstance);

            if (!isFirstInstance)
            {
                CLogger.Print("The server is already running! Exiting...", LoggerType.Warning);
                MessageBox.Show("The server is already running!", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Thread.Sleep(SHUTDOWN_DELAY_MS);
                WindowUtility.KillProcessAndChildren(currentProcessId);
                return false;
            }
            return true;
        }

        private static void ConfigureSystemSettings()
        {
            if (MemoryUtility.IsAdministrator())
            {
                SetConsoleCtrlHandler(new EventHandler(WindowsEventHandler), true);
                FirewallUtil.AddFirewallRule(executableInfo.FullName);
                titleSuffix = ADMINISTRATOR_TITLE;
            }
            else
            {
                titleSuffix = SUPERUSER_TITLE;
            }
        }

        private static void TerminateExistingInstances()
        {
            foreach (Process process in Process.GetProcessesByName("PointBlank"))
            {
                try { process.Kill(); }
                catch (Exception ex) { CLogger.Print($"Failed to terminate existing process: {ex.Message}", LoggerType.Warning); }
            }
        }

        private static string GetApplicationVersion() => FileVersionInfo.GetVersionInfo(executableInfo.Name).FileVersion;

        #endregion Initialization

        #region Server Startup

        private static void StartServerSystems(bool includeMonitor, string fileVersion)
        {
            serverManager.DisplayStartupBanner();
            serverManager.LoadAllComponents();

            Thread.Sleep(STARTUP_DELAY_MS);

            InitializeCommunication();

            bool serverStarted = ValidateAndStartServers();
            FinalizeStartup(serverStarted, includeMonitor, fileVersion);
        }

        private static void InitializeCommunication()
        {
            serverManager.PrintSection("Plugin Status", true);
            Communication.Start(new IPEndPoint(IPAddress.Parse(ConfigLoader.HOST[1]), COMMUNICATION_PORT));
            CLogger.Print("All Server Plugins Successfully Loaded", LoggerType.Info);
            serverManager.PrintSection("Plugin Status", false);
        }

        private static bool ValidateAndStartServers() => DatabaseValidator.ValidateAllConnections() && StartAllServerInstances();

        private static bool StartAllServerInstances()
        {
            try
            {
                return AuthServerManager.Start() && GameServerManager.Start() && BattleServerManager.Start();
            }
            catch (Exception ex)
            {
                CLogger.Print($"Server startup error: {ex.Message}", LoggerType.Error, ex);
                return false;
            }
        }

        private static void FinalizeStartup(bool serverStarted, bool includeMonitor, string fileVersion)
        {
            UpdateServerStatus(serverStarted, fileVersion);
            if (serverStarted) BeginServerOperation(includeMonitor, fileVersion);
        }

        private static void UpdateServerStatus(bool isOnline, string version)
        {
            StringUtility.ServerVersionL = version;
            StringUtility.ServerStatusL = isOnline ? "SERVER ONLINE" : "SERVER OFFLINE";

            string message = isOnline ? $"Startup Successful, Server Runtime {DateTimeUtil.Now("yyyy")}" : $"Startup Unsuccessful, Server Runtime {DateTimeUtil.Now("yyyy")}";
            LoggerType logType = isOnline ? LoggerType.Info : LoggerType.Warning;

            serverManager.PrintSection("Server Status", true);
            CLogger.Print(message, logType);
            serverManager.PrintSection("Server Status", false);
            Console.WriteLine("");
        }

        private static async void BeginServerOperation(bool includeMonitor, string version)
        {
            try { await StartTitleUpdateLoop(includeMonitor, version); }
            catch (Exception ex) { CLogger.Print($"Server operation error: {ex.Message}", LoggerType.Error, ex); }
        }

        #endregion Server Startup

        #region Monitor and Title Management

        private static bool ShouldIncludeMonitorForm(string[] args) => args.Length > 0 && args[0].Equals(MONITOR_ARGUMENT);

        private static void StartMonitorFormAsync() => new Thread(StartMonitorForm) { IsBackground = true }.Start();

        private static void StartMonitorForm()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var monitor = new MainForm(currentProcessId, new DirectoryInfo($"{executableInfo.Directory}/Logs")) { TopMost = true };
                Application.Run(monitor);
            }
            catch (Exception ex) { CLogger.Print($"Monitor form error: {ex.Message}", LoggerType.Error, ex); }
        }

        private static async Task StartTitleUpdateLoop(bool monitorMode, string version)
        {
            while (true)
            {
                try
                {
                    UpdateFormInformation();
                    UpdateConsoleTitle(monitorMode, version);
                    CheckDailyReset();
                    await Task.Delay(TITLE_UPDATE_INTERVAL_MS);
                }
                catch (Exception ex)
                {
                    CLogger.Print($"Title update error: {ex.Message}", LoggerType.Warning, ex);
                    await Task.Delay(TITLE_UPDATE_INTERVAL_MS);
                }
            }
        }

        private static void UpdateConsoleTitle(bool monitorMode, string version)
        {
            var statistics = ServerStatistics.GetCurrent();
            string titleInfo = monitorMode ? $"RAM Usage: {statistics.MemoryUsageMB:0.0} MB)" : $"Users: {statistics.TotalUsers}; Online: {statistics.OnlineUsers}; RAM Usage: {statistics.MemoryUsageMB:0.0} MB ({statistics.MemoryUsagePercent:0.0}%)";
            Console.Title = $"Point Blank (RUS-{SERVER_VERSION}) Server {version} </> {titleInfo} -{titleSuffix} </> Timeline: {DateTimeUtil.Now("dddd, MMMM dd, yyyy - HH:mm:ss")}";
        }

        #endregion Monitor and Title Management

        #region Database Management

        private static void CheckDailyReset()
        {
            try
            {
                if (int.Parse(DateTimeUtil.Now("HHmmss")) == DAILY_RESET_CHECK_TIME)
                    DatabaseManager.PerformDailyReset();
            }
            catch (Exception ex) { CLogger.Print($"Daily reset check error: {ex.Message}", LoggerType.Error, ex); }
        }

        private static void UpdateFormInformation()
        {
            var statistics = ServerStatistics.GetCurrent();
            var networkInfo = NetworkInformation.GetCurrent();
            var gameInfo = GameInformation.GetCurrent();
            StringUtilityHelper.UpdateAllStatistics(statistics, networkInfo, gameInfo);
        }

        #endregion Database Management

        #region Event Handlers

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            CLogger.Print("Server shutdown initiated by user.", LoggerType.Info);
            SendMessage.FromServer("Attention! \nThe Server Will Be Restarted!");
            e.Cancel = true;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;
            CLogger.Print($"Unhandled Exception - Sender: {sender}, Terminating: {e.IsTerminating}, Exception: {exception.Message}", LoggerType.Error, exception);
        }

        #endregion Event Handlers

        #region Cleanup

        private static void CleanupFirewallRules()
        {
            try { FirewallUtil.RemoveFirewallRule(executableInfo.FullName); }
            catch (Exception ex) { CLogger.Print($"Firewall cleanup error: {ex.Message}", LoggerType.Warning, ex); }
        }

        private static void CleanupResources()
        {
            try
            {
                applicationMutex?.ReleaseMutex();
                Process.GetCurrentProcess().WaitForExit();
            }
            catch (Exception ex) { CLogger.Print($"Resource cleanup error: {ex.Message}", LoggerType.Warning, ex); }
        }

        #endregion Cleanup
    }

    #region Server Manager - Unified Class

    public class ServerManager
    {
        private const int LINE_WIDTH = 100;

        private static readonly string[] TEAM_CREDITS = new string[]
        {
            "Pavel, Monester, Fusion, zOne62, Garry",
        };

        public void DisplayStartupBanner()
        {
            Console.WriteLine();
            PrintLine('=');
            Console.WriteLine();

            string[] logo = new string[]
            {
                @"  ██████╗  ██████╗ ██╗███╗   ██╗████████╗    ██████╗ ██╗      █████╗ ███╗   ██╗██╗  ██╗",
                @"  ██╔══██╗██╔═══██╗██║████╗  ██║╚══██╔══╝    ██╔══██╗██║     ██╔══██╗████╗  ██║██║ ██╔╝",
                @"  ██████╔╝██║   ██║██║██╔██╗ ██║   ██║       ██████╔╝██║     ███████║██╔██╗ ██║█████╔╝ ",
                @"  ██╔═══╝ ██║   ██║██║██║╚██╗██║   ██║       ██╔══██╗██║     ██╔══██║██║╚██╗██║██╔═██╗ ",
                @"  ██║     ╚██████╔╝██║██║ ╚████║   ██║       ██████╔╝███████╗██║  ██║██║ ╚████║██║  ██╗",
                @"  ╚═╝      ╚═════╝ ╚═╝╚═╝  ╚═══╝   ╚═╝       ╚═════╝ ╚══════╝╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝"
            };

            foreach (string line in logo)
                Console.WriteLine(CenterText(line));

            Console.WriteLine();
            Console.WriteLine(CenterText("RUSSIAN WARFARE SERVER"));
            Console.WriteLine();
            PrintLine('=');
            Console.WriteLine();
            Console.WriteLine(CenterText("DEVELOPMENT TEAM"));
            Console.WriteLine();

            foreach (string credit in TEAM_CREDITS)
                Console.WriteLine(CenterText(credit));

            Console.WriteLine();
            PrintLine('=');
            Console.WriteLine();
        }

        public void PrintSection(string name, bool isBegin)
        {
            if (string.IsNullOrEmpty(name)) return;

            string arrow = isBegin ? ">>>" : "<<<";
            string status = isBegin ? "[LOADING]" : "[COMPLETE]";

            string header = $"{arrow} {name} {status}";
            int padding = (LINE_WIDTH - header.Length) / 2;

            if (isBegin)
            {
                Console.WriteLine();
                Console.WriteLine(new string('-', LINE_WIDTH));
                Console.WriteLine(new string(' ', padding) + header);
            }
            else
            {
                Console.WriteLine(new string(' ', padding) + header);
                Console.WriteLine(new string('-', LINE_WIDTH));
            }
        }

        public void LoadAllComponents()
        {
            LoadConfigurations();
            LoadEventData();
            LoadPortalData();
            LoadShopData();
            LoadMissionData();
            LoadServerData();
            LoadGameModes();

            PrintSection("Rcon Status", true);
            RconCommand.Instance();
            PrintSection("Rcon Status", false);
        }

        private void LoadConfigurations()
        {
            PrintSection("Config", true);
            try
            {
                ServerConfigJSON.Load();
                CommandHelperJSON.Load();
                ResolutionJSON.Load();
                ValidateCriticalConfigurations();
            }
            catch (Exception ex)
            {
                CLogger.Print($"Configuration loading failed: {ex.Message}", LoggerType.Error, ex);
                throw;
            }
            PrintSection("Config", false);
        }

        private void ValidateCriticalConfigurations()
        {
            if (ConfigLoader.HOST == null || ConfigLoader.HOST.Length < 3)
                throw new InvalidOperationException("Host configuration is missing or incomplete");

            if (ConfigLoader.DEFAULT_PORT == null || ConfigLoader.DEFAULT_PORT.Length < 3)
                throw new InvalidOperationException("Port configuration is missing or incomplete");

            for (int i = 0; i < ConfigLoader.DEFAULT_PORT.Length; i++)
            {
                if (ConfigLoader.DEFAULT_PORT[i] <= 0)
                    throw new InvalidOperationException($"Invalid port value at index {i}: {ConfigLoader.DEFAULT_PORT[i]}");
            }

            CLogger.Print("All critical configurations validated successfully", LoggerType.Info);
        }

        private void LoadEventData()
        {
            PrintSection("Events Data", true);
            EventLoginXML.Load();
            EventBoostXML.Load();
            EventRankUpXML.Load();
            EventPlaytimeJSON.Load();
            EventQuestXML.Load();
            EventVisitXML.Load();
            EventXmasXML.Load();
            PrintSection("Events Data", false);
        }

        private void LoadShopData()
        {
            PrintSection("Shop Data", true);
            ShopManager.Load(1);
            ShopManager.Load(2);
            PrintSection("Shop Data", false);
        }

        private void LoadMissionData()
        {
            PrintSection("Mission Cards", true);
            MissionCardRAW.LoadBasicCards(1);
            MissionCardRAW.LoadBasicCards(2);
            PrintSection("Mission Cards", false);
        }

        private void LoadServerData()
        {
            PrintSection("Server Data", true);
            LoadServerXMLFiles();
            LoadServerFilters();
            PrintSection("Server Data", false);
        }

        private void LoadGameModes()
        {
            PrintSection("Classic Mode", true);
            ClassicModeManager.LoadList();
            PrintSection("Classic Mode", false);

            PrintSection("Battle Pass", true);
            BattlePassManager.Load();
            PrintSection("Battle Pass", false);

            PrintSection("Competitive", true);
            CompetitiveXML.Load();
            PrintSection("Competitive", false);
        }

        private void LoadPortalData()
        {
            PrintSection("Portal Data", true);
            PortalManager.Load();
            PrintSection("Portal Data", false);
        }

        private void LoadServerXMLFiles()
        {
            TemplatePackXML.Load();
            TitleSystemXML.Load();
            TitleAwardXML.Load();
            MissionAwardXML.Load();
            MissionConfigXML.Load();
            SChannelXML.Load();
            SynchronizeXML.Load();
            SystemMapXML.Load();
            ClanRankXML.Load();
            PlayerRankXML.Load();
            CouponEffectXML.Load();
            PermissionXML.Load();
            RandomBoxXML.Load();
            BattleBoxXML.Load();
            DirectLibraryXML.Load();
            InternetCafeXML.Load();
            RedeemCodeXML.Load();
            BattleRewardXML.Load();
        }

        private void LoadServerFilters() => NickFilter.Load();

        private void PrintLine(char character) => Console.WriteLine(new string(character, LINE_WIDTH));

        private string CenterText(string text)
        {
            int padding = (LINE_WIDTH - text.Length) / 2;
            return new string(' ', Math.Max(0, padding)) + text;
        }
    }

    #endregion Server Manager - Unified Class

    #region Support Classes

    public static class DatabaseValidator
    {
        public static bool ValidateAllConnections() => ComDiv.ValidateAllPlayersAccount();
    }

    public static class AuthServerManager
    {
        public static bool Start()
        {
            try
            {
                var manager = new ServerManager();
                manager.PrintSection("Auth Server", true);
                Server.Auth.Data.XML.ChannelsXML.Load();
                AuthXender.GetPlugin(ConfigLoader.HOST[0], ConfigLoader.DEFAULT_PORT[0]);
                manager.PrintSection("Auth Server", false);
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print($"Auth Server startup failed: {ex.Message}", LoggerType.Error, ex);
                return false;
            }
        }
    }

    public static class GameServerManager
    {
        public static bool Start()
        {
            try
            {
                var manager = new ServerManager();
                manager.PrintSection("Game Server", true);
                Server.Game.Data.XML.ChannelsXML.Load();
                Server.Game.Data.Managers.ClanManager.Load();

                foreach (SChannelModel server in SChannelXML.Servers)
                {
                    if (server.Id >= 1 && server.Port >= ConfigLoader.DEFAULT_PORT[1])
                        GameXender.GetPlugin(server.Id, ConfigLoader.HOST[0], server.Port);
                }

                manager.PrintSection("Game Server", false);
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print($"Game Server startup failed: {ex.Message}", LoggerType.Error, ex);
                return false;
            }
        }
    }

    public static class BattleServerManager
    {
        public static bool Start()
        {
            try
            {
                var manager = new ServerManager();
                manager.PrintSection("Battle Server", true);
                Server.Match.Data.XML.MapStructureXML.Load();
                Server.Match.Data.XML.CharaStructureXML.Load();
                Server.Match.Data.XML.ItemStatisticXML.Load();
                MatchXender.GetPlugin(ConfigLoader.HOST[0], ConfigLoader.DEFAULT_PORT[2]);
                manager.PrintSection("Battle Server", false);
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print($"Battle Server startup failed: {ex.Message}", LoggerType.Error, ex);
                return false;
            }
        }
    }

    public static class DatabaseManager
    {
        public static bool PerformDailyReset()
        {
            try
            {
                int playerDailies = ComDiv.CountDB("SELECT COUNT(*) FROM player_stat_dailies");
                if (playerDailies > 0)
                {
                    ComDiv.UpdateDB("player_stat_dailies",
                        new string[] { "matches", "match_wins", "match_loses", "match_draws", "kills_count", "deaths_count", "headshots_count", "exp_gained", "point_gained", "playtime" },
                        0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                }

                int playerReports = ComDiv.CountDB("SELECT COUNT(*) FROM player_reports");
                if (playerReports > 0)
                    ComDiv.UpdateDB("player_reports", new string[] { "ticket_count" }, 3);

                CLogger.Print("Daily database reset completed successfully", LoggerType.Info);
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print($"Daily reset failed: {ex.Message}", LoggerType.Error, ex);
                return false;
            }
        }
    }

    public class ServerStatistics
    {
        public double MemoryUsageMB { get; set; }
        public double MemoryUsagePercent { get; set; }
        public int TotalUsers { get; set; }
        public int OnlineUsers { get; set; }
        public int TotalClans { get; set; }
        public int VipUsers { get; set; }
        public int BannedPlayers { get; set; }

        public static ServerStatistics GetCurrent()
        {
            return new ServerStatistics
            {
                MemoryUsageMB = MemoryUtility.GetMemoryUsage(),
                MemoryUsagePercent = MemoryUtility.GetMemoryUsagePercent(),
                TotalUsers = ComDiv.CountDB("SELECT COUNT(*) FROM accounts"),
                OnlineUsers = ComDiv.CountDB($"SELECT COUNT(*) FROM accounts WHERE online = {true}"),
                TotalClans = ComDiv.CountDB("SELECT COUNT(*) FROM system_clan"),
                VipUsers = ComDiv.CountDB($"SELECT COUNT(*) FROM accounts WHERE pc_cafe = '2' OR pc_cafe = '1'"),
                BannedPlayers = ComDiv.CountDB($"SELECT COUNT(*) FROM base_auto_ban")
            };
        }
    }

    public class NetworkInformation
    {
        public string LocalAddress { get; set; }
        public string GameRegion { get; set; }

        public static NetworkInformation GetCurrent()
        {
            return new NetworkInformation
            {
                LocalAddress = GetLocalAddress(),
                GameRegion = GetGameLocale()
            };
        }

        private static string GetLocalAddress()
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress address in host.AddressList)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                        return address.ToString();
                }
            }
            catch (Exception ex) { CLogger.Print($"Failed to get local address: {ex.Message}", LoggerType.Warning, ex); }
            return "Unknown";
        }

        private static string GetGameLocale()
        {
            foreach (ClientLocale region in ConfigLoader.GameLocales)
            {
                if (region == ClientLocale.Russia)
                    return region.ToString();
            }
            return "Outside!";
        }
    }

    public class GameInformation
    {
        public string Version { get; set; }
        public string ConfigId { get; set; }
        public bool TournamentRule { get; set; }
        public bool InternetCafe { get; set; }
        public bool AutoAccount { get; set; }
        public bool AutoBan { get; set; }

        public static GameInformation GetCurrent()
        {
            return new GameInformation
            {
                Version = $"V{ServerConfigJSON.GetConfig(ConfigLoader.ConfigId)?.ClientVersion ?? "Unknown"}",
                ConfigId = ConfigLoader.ConfigId.ToString(),
                TournamentRule = ConfigLoader.TournamentRule,
                InternetCafe = ConfigLoader.ICafeSystem,
                AutoAccount = ConfigLoader.AutoAccount,
                AutoBan = ConfigLoader.AutoBan
            };
        }
    }

    public static class StringUtilityHelper
    {
        public static void UpdateAllStatistics(ServerStatistics stats, NetworkInformation network, GameInformation game)
        {
            StringUtility.MemoryValueVPB = $"{Convert.ToInt32(stats.MemoryUsageMB)}";
            StringUtility.MemoryUsageL = $"{stats.MemoryUsagePercent:0.0}%";
            StringUtility.RegisteredUserL = stats.TotalUsers.ToString();
            StringUtility.OnlineUserL = stats.OnlineUsers.ToString();
            StringUtility.TotalClansL = stats.TotalClans.ToString();
            StringUtility.VipUserL = stats.VipUsers.ToString();
            StringUtility.BannedPlayers = stats.BannedPlayers.ToString();
            StringUtility.LocalAddressL = network.LocalAddress;
            StringUtility.ForGameRegionL = network.GameRegion;
            StringUtility.ForGameVersionL = game.Version;
            StringUtility.SelectedServerConfigL = game.ConfigId;
            StringUtility.TournamentRuleL = game.TournamentRule ? "Enabled" : "Disabled";
            StringUtility.InternetCafeL = game.InternetCafe ? "Enabled" : "Disabled";
            StringUtility.EnableAutoAccountL = game.AutoAccount ? "Enabled" : "Disabled";
            StringUtility.AutoBanPlayerL = game.AutoBan ? "Enabled" : "Disabled";
            StringUtility.ServerTimelineL = DateTimeUtil.Now("yyyy");
            StringUtility.LogFileSize = CalculateLogFileSize();
            StringUtility.RegShopItems = CalculateShopItems().ToString();
            StringUtility.ShopCafeItems = CalculateCafeItems().ToString();
            StringUtility.RepairableItems = ComDiv.CountDB("SELECT COUNT(*) FROM system_shop_repair").ToString();
            StringUtility.UnknownUserL = ComDiv.CountDB("SELECT COUNT(*) FROM accounts WHERE nickname = ''").ToString();
        }

        private static string CalculateLogFileSize()
        {
            try
            {
                var executableInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
                double size = MemoryUtility.GetDirectorySize(new DirectoryInfo($"{executableInfo.Directory}/Logs"), true);
                return $"{(size / (1024 * 1024)):N2}MB";
            }
            catch { return "0.00MB"; }
        }

        private static int CalculateShopItems()
        {
            return ComDiv.CountDB("SELECT COUNT(*) FROM system_shop") +
                   ComDiv.CountDB("SELECT COUNT(*) FROM system_shop_effects") +
                   ComDiv.CountDB("SELECT COUNT(*) FROM system_shop_sets");
        }

        private static int CalculateCafeItems()
        {
            return ComDiv.CountDB($"SELECT COUNT(*) FROM system_shop WHERE item_visible = '{false}'") +
                   ComDiv.CountDB($"SELECT COUNT(*) FROM system_shop_effects WHERE coupon_visible = '{false}'");
        }
    }

    #endregion Support Classes
}