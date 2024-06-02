using Nexus.API;
using NLog;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers;
using Torch.Session;
using VoteRewards.Config;
using VoteRewards.DataBase;
using VoteRewards.Nexus;
using VoteRewards.Utils;
using VRageMath;
#nullable enable 

namespace VoteRewards
{
    public partial class VoteRewardsMain : TorchPluginBase, IWpfPlugin
    {

        // Stałe i statyczne pola
        public static IChatManagerServer ChatManager => TorchBase.Instance.CurrentSession.Managers.GetManager<IChatManagerServer>();
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly string CONFIG_FILE_NAME = "VoteRewardsConfig.cfg";
        private static readonly string REWARD_ITEMS_CONFIG_FILE_NAME = "RewardItemsConfig.cfg";
        public bool IsReloadable => true;
        public static VoteRewardsMain? Instance;

        //Nexus
        public static NexusAPI? nexusAPI { get; private set; }
        private static readonly Guid NexusGUID = new("28a12184-0422-43ba-a6e6-2e228611cca5");
        public static bool NexusInstalled { get; private set; } = false;
        public static bool NexusInited;

        // Pola prywatne i ich publiczne właściwości
        private VoteRewardsControl _control;
        private Persistent<VoteRewardsConfig> _config;
        private Persistent<TopVotersBenefitConfig> _topVotersBenefitConfig;
        private Persistent<RewardItemsConfig> _rewardItemsConfig;
        private Persistent<TimeSpentRewardsConfig> _timeSpentRewardsConfig;
        private ReferralCodeManager _referralCodeManager;
        private EventCodeManager _eventCodeManager;
        private Persistent<RefferalCodeReward> _refferalCodeReward;
        private Persistent<EventCodeReward> _eventCodeReward;
        private IMultiplayerManagerBase _multiplayerManager;
        private Dictionary<ulong, Dictionary<TimeReward, TimeSpan>> _playerTimeSpent = new Dictionary<ulong, Dictionary<TimeReward, TimeSpan>>();
        private Timer _updatePlayerTimeSpentTimer;
        public PlayerTimeTracker PlayerTimeTracker { get; private set; }

        public VoteRewardsConfig Config => _config?.Data;
        public RewardItemsConfig RewardItemsConfig => _rewardItemsConfig?.Data;
        public TopVotersBenefitConfig TopVotersBenefitConfig => _topVotersBenefitConfig?.Data;
        public TimeSpentRewardsConfig TimeSpentRewardsConfig => _timeSpentRewardsConfig?.Data;
        public ReferralCodeManager ReferralCodeManager => _referralCodeManager;
        public EventCodeManager EventCodeManager => _eventCodeManager;
        public RefferalCodeReward RefferalCodeReward => _refferalCodeReward?.Data;
        public EventCodeReward EventCodeReward => _eventCodeReward?.Data;

        // Nowe listy do przechowywania dostępnych typów i podtypów przedmiotów
        public List<string> AvailableItemTypes { get; private set; } = new List<string>();
        public Dictionary<string, List<string>> AvailableItemSubtypes { get; private set; } = new Dictionary<string, List<string>>();

        public VoteApiHelper ApiHelper { get; private set; }

        // PostgresSQL
        private PostgresDatabaseManager _databaseManager;
        public PostgresDatabaseManager DatabaseManager => _databaseManager;

        // Metody i ich implementacje
        public UserControl GetControl()
        {
            if (_control == null)
            {
                _control = new VoteRewardsControl(this);
            }
            return _control;
        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;

            _config = SetupConfig(CONFIG_FILE_NAME, new VoteRewardsConfig());
            _rewardItemsConfig = SetupConfig(REWARD_ITEMS_CONFIG_FILE_NAME, new RewardItemsConfig());
            _topVotersBenefitConfig = SetupConfig("TopVotersBenefitConfig.cfg", new TopVotersBenefitConfig());
            _timeSpentRewardsConfig = SetupConfig("TimeSpentRewardsConfig.cfg", new TimeSpentRewardsConfig());
            _refferalCodeReward = SetupConfig("ReferralCodeReward.cfg", new RefferalCodeReward());
            _eventCodeReward = SetupConfig("EventCodeReward.cfg", new EventCodeReward());

            // Tworzenie katalogu i pliku "PlayerData.xml", jeśli nie istnieje
            if (!_config.Data.UseDatabase)
            {
                InitializePlayerDataStorage();
            }

            //PostgresSQL
            if (_config.Data.UseDatabase)
            {
                // Ciąg połączenia z ustawień konfiguracji
                string connectionString = $"Host={_config.Data.DatabaseHost};Port={_config.Data.DatabasePort};Username={_config.Data.DatabaseUsername};Password={_config.Data.DatabasePassword};Database={_config.Data.DatabaseName};";
                _databaseManager = new PostgresDatabaseManager(connectionString);
                _databaseManager.InitializeDatabase();
            }

            // Tworzenie ścieżek plików tylko jeśli baza danych nie jest używana
            string referralCodeFilePath = _config.Data.UseDatabase ? null : Path.Combine(StoragePath, "VoteReward", "ReferralCodes.json");
            string eventCodeFilePath = _config.Data.UseDatabase ? null : Path.Combine(StoragePath, "VoteReward", "EventCodes.json");

            // Inicjalizacja menedżerów zależnie od konfiguracji użycia bazy danych
            _referralCodeManager = new ReferralCodeManager(referralCodeFilePath, _config.Data);
            _eventCodeManager = new EventCodeManager(eventCodeFilePath, _config.Data);
            ApiHelper = new VoteApiHelper();

            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _control = new VoteRewardsControl(this);
                    if (Application.Current.MainWindow != null)
                    {
                        Application.Current.MainWindow.Loaded += (sender, e) =>
                        {
                            _control.Loaded += Control_Loaded;
                        };
                    }
                });
            }

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            Save();
        }

        private void Control_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoggerHelper.DebugLog(Log, _config.Data, "Control loaded successfully.");
        }

        private void OnPlayerJoined(IPlayer player)
        {
            _playerTimeSpent[player.SteamId] = new Dictionary<TimeReward, TimeSpan>();
        }


        private void OnPlayerLeft(IPlayer player)
        {
            _playerTimeSpent.Remove(player.SteamId);
        }


        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    ConnectNexus();

                    _updatePlayerTimeSpentTimer = new Timer(UpdatePlayerTimeSpent, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

                    if (_config.Data.PlayerTimeTracker)
                    {
                        PlayerTimeTracker = new PlayerTimeTracker();
                    }

                    _multiplayerManager = session.Managers.GetManager<IMultiplayerManagerBase>();
                    if (_multiplayerManager == null)
                    {
                        Log.Warn("Could not get multiplayer manager.");
                    }
                    else
                    {
                        LoggerHelper.DebugLog(Log, _config.Data, "Multiplayer manager initialized.");
                        _multiplayerManager.PlayerJoined += OnPlayerJoined;
                        _multiplayerManager.PlayerLeft += OnPlayerLeft;

                        if (_config.Data.PlayerTimeTracker)
                        {
                            _multiplayerManager.PlayerJoined += PlayerTimeTracker.OnPlayerJoined;
                            _multiplayerManager.PlayerLeft += PlayerTimeTracker.OnPlayerLeft;
                        }
                    }

                    ItemLoader.LoadAvailableItemTypesAndSubtypes(AvailableItemTypes, AvailableItemSubtypes, Log, Config);

                    // Sprawdzenie systemu operacyjnego przed aktualizacją stanu przycisków
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        _control.Dispatcher.Invoke(() => _control.UpdateButtonState(true));
                    }
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");

                    // Sprawdzenie systemu operacyjnego przed aktualizacją stanu przycisków
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        _control.Dispatcher.Invoke(() => _control.UpdateButtonState(false));
                    }
                    _multiplayerManager = null;
                    break;
            }
        }


        private void UpdatePlayerTimeSpent(object state)
        {
            try
            {
                var getRandomRewardsUtils = new GetRandomRewardsUtils(this.RewardItemsConfig, this.TimeSpentRewardsConfig, this.RefferalCodeReward, this.TopVotersBenefitConfig);

                foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                {
                    var steamId = player.Id.SteamId;
                    if (!_playerTimeSpent.ContainsKey(steamId))
                    {
                        _playerTimeSpent[steamId] = new Dictionary<TimeReward, TimeSpan>();
                    }

                    foreach (var timeReward in this.TimeSpentRewardsConfig.TimeRewards)
                    {
                        if (!_playerTimeSpent[steamId].ContainsKey(timeReward))
                        {
                            _playerTimeSpent[steamId][timeReward] = TimeSpan.Zero;
                        }

                        _playerTimeSpent[steamId][timeReward] += TimeSpan.FromMinutes(1);

                        if (_playerTimeSpent[steamId][timeReward].TotalMinutes >= timeReward.RewardInterval)
                        {

                            var rewardsToAward = getRandomRewardsUtils.GetRandomRewardsFromList(timeReward.RewardsList);

                            if (rewardsToAward.Any())
                            {
                                var successfulRewards = new List<string>();

                                foreach (var rewardItem in rewardsToAward)
                                {
                                    int randomAmount = getRandomRewardsUtils.GetRandomAmount(rewardItem.AmountOne, rewardItem.AmountTwo);
                                    bool awarded = PlayerRewardManager.AwardPlayer(steamId, rewardItem, randomAmount, Log, Config);

                                    if (awarded)
                                    {
                                        successfulRewards.Add($"{randomAmount}x {rewardItem.ItemSubtypeId}");
                                    }
                                }

                                if (successfulRewards.Any())
                                {
                                    ChatManager.SendMessageAsOther(timeReward.NotificationPrefix, $"Congratulations! You have received:\n{string.Join("\n", successfulRewards)}", Color.Green, steamId);
                                }
                            }

                            // Resetowanie czasu dla tej konkretnej nagrody
                            _playerTimeSpent[steamId][timeReward] = TimeSpan.Zero;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.DebugLog(Log, _config.Data, $"An error occurred in UpdatePlayerTimeSpent: {ex.Message}");
            }
        }

        private Persistent<T> SetupConfig<T>(string fileName, T defaultConfig) where T : new()
        {
            var configFolderPath = Path.Combine(StoragePath, "VoteReward", "Config");
            Directory.CreateDirectory(configFolderPath);
            var configFilePath = Path.Combine(configFolderPath, fileName);

            Persistent<T> config;

            try
            {
                config = Persistent<T>.Load(configFilePath);
            }
            catch (Exception e)
            {
                Log.Warn(e);
                config = new Persistent<T>(configFilePath, defaultConfig);
            }

            if (config.Data == null)
            {
                Log.Info($"Creating default config for {fileName} because none was found!");

                config = new Persistent<T>(configFilePath, defaultConfig);
                config.Save();
            }

            return config;
        }

        private void ConnectNexus()
        {
            if (!NexusInited)
            {
                PluginManager? _pluginManager = Torch.Managers.GetManager<PluginManager>();
                if (_pluginManager is null)
                    return;

                if (_pluginManager.Plugins.TryGetValue(NexusGUID, out ITorchPlugin? torchPlugin))
                {
                    if (torchPlugin is null)
                        return;

                    Type? Plugin = torchPlugin.GetType();
                    Type? NexusPatcher = Plugin != null ? Plugin.Assembly.GetType("Nexus.API.PluginAPISync") : null;
                    if (NexusPatcher != null)
                    {
                        NexusPatcher.GetMethod("ApplyPatching", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[]
                        {
                            typeof(NexusAPI), "VoteRewards Plugin"
                        });
                        nexusAPI = new NexusAPI(9452);
                        MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(9452, new Action<ushort, byte[], ulong, bool>(NexusManager.HandleNexusMessage));
                        NexusInstalled = true;
                    }
                }
                NexusInited = true;
            }

            // Nowy dodany blok sprawdzający
            if (NexusInstalled && nexusAPI != null)
            {
                NexusAPI.Server thisServer = NexusAPI.GetThisServer();
                NexusManager.SetServerData(thisServer);

                if (Config!.isLobby)
                {
                    // Announce to all other servers that started before the Lobby, that this is the lobby server
                    List<NexusAPI.Server> servers = NexusAPI.GetAllServers();
                    foreach (NexusAPI.Server server in servers)
                    {
                        if (server.ServerID != thisServer.ServerID)
                        {
                            NexusMessage message = new(thisServer.ServerID, server.ServerID, false, thisServer, false, true);
                            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(message);
                            nexusAPI?.SendMessageToServer(server.ServerID, data);
                        }
                    }
                }
            }
            else
            {
                Log.Warn("Nexus API is not installed or not initialized. Skipping Nexus connection.");
            }
        }

        private void InitializePlayerDataStorage()
        {
            string playerDataDirectory = Path.Combine(StoragePath, "VoteReward");
            string playerDataFilePath = Path.Combine(playerDataDirectory, "PlayerData.xml");

            if (!Directory.Exists(playerDataDirectory))
            {
                Directory.CreateDirectory(playerDataDirectory);
            }

            if (!File.Exists(playerDataFilePath))
            {
                XDocument doc = new XDocument(new XElement("Players"));
                doc.Save(playerDataFilePath);
                Log.Info("PlayerData.xml has been created.");
            }
            else
            {
                Log.Info("PlayerData.xml already exists.");
            }
        }

        public void Save()
        {
            try
            {
                _config.Save();
                _timeSpentRewardsConfig.Save();
                _rewardItemsConfig.Save();
                _refferalCodeReward.Save();
                _eventCodeReward.Save();
                _topVotersBenefitConfig.Save();
                Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                Log.Warn("An IOException occurred while saving the configuration (It is recommended to enable debugging systems and call save again)");
                LoggerHelper.DebugLog(Log, _config.Data, $"Error message: {e.Message}");
                LoggerHelper.DebugLog(Log, _config.Data, $"Stack trace: {e.StackTrace}");

                if (e.InnerException != null)
                {
                    LoggerHelper.DebugLog(Log, _config.Data, $"Inner exception message: {e.InnerException.Message}");
                    LoggerHelper.DebugLog(Log, _config.Data, $"Inner exception stack trace: {e.InnerException.StackTrace}");
                }
            }
        }

    }
}