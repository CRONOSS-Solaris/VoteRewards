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
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers;
using Torch.Session;
using VoteRewards.Config;
using VoteRewards.Nexus;
using VoteRewards.Utils;
using VRageMath;

namespace VoteRewards
{
    public class VoteRewardsMain : TorchPluginBase, IWpfPlugin
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
        private Persistent<RewardItemsConfig> _rewardItemsConfig;
        private Persistent<TimeSpentRewardsConfig> _timeSpentRewardsConfig;
        private ReferralCodeManager _referralCodeManager;
        private EventCodeManager _eventCodeManager;
        private Persistent<RefferalCodeReward> _refferalCodeReward;
        private Persistent<EventCodeReward> _eventCodeReward;
        private IMultiplayerManagerBase _multiplayerManager;
        private Dictionary<ulong, TimeSpan> _playerTimeSpent = new Dictionary<ulong, TimeSpan>();
        private Timer _updatePlayerTimeSpentTimer;

        public VoteRewardsConfig Config => _config?.Data;
        public RewardItemsConfig RewardItemsConfig => _rewardItemsConfig?.Data;
        public TimeSpentRewardsConfig TimeSpentRewardsConfig => _timeSpentRewardsConfig?.Data;
        public ReferralCodeManager ReferralCodeManager => _referralCodeManager;
        public EventCodeManager EventCodeManager => _eventCodeManager;
        public RefferalCodeReward RefferalCodeReward => _refferalCodeReward?.Data;
        public EventCodeReward EventCodeReward => _eventCodeReward?.Data;

        // Nowe listy do przechowywania dostępnych typów i podtypów przedmiotów
        public List<string> AvailableItemTypes { get; private set; } = new List<string>();
        public Dictionary<string, List<string>> AvailableItemSubtypes { get; private set; } = new Dictionary<string, List<string>>();

        public VoteApiHelper ApiHelper { get; private set; }

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
            _timeSpentRewardsConfig = SetupConfig("TimeSpentRewardsConfig.cfg", new TimeSpentRewardsConfig());
            _refferalCodeReward = SetupConfig("ReferralCodeReward.cfg", new RefferalCodeReward());
            _eventCodeReward = SetupConfig("EventCodeReward.cfg", new EventCodeReward());
            string referralCodeFilePath = Path.Combine(StoragePath, "VoteReward", "ReferralCodes.json");
            _referralCodeManager = new ReferralCodeManager(referralCodeFilePath, _config.Data);
            string EventCodeFilePath = Path.Combine(StoragePath, "VoteReward", "EventCodes.json");
            _eventCodeManager = new EventCodeManager(EventCodeFilePath, _config.Data);
            ApiHelper = new VoteApiHelper(Config.ServerApiKey);


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
            _playerTimeSpent[player.SteamId] = TimeSpan.Zero;
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

                    _multiplayerManager = Torch.Managers.GetManager<IMultiplayerManagerBase>();
                    if (_multiplayerManager == null)
                    {
                        Log.Warn("Could not get multiplayer manager.");
                    }
                    else
                    {
                        LoggerHelper.DebugLog(Log, _config.Data, "Multiplayer manager initialized.");
                        _multiplayerManager.PlayerJoined += OnPlayerJoined;
                        _multiplayerManager.PlayerLeft += OnPlayerLeft;
                    }

                    ItemLoader.LoadAvailableItemTypesAndSubtypes(AvailableItemTypes, AvailableItemSubtypes, Log, Config);
                    _control.Dispatcher.Invoke(() => _control.UpdateButtonState(true));
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");

                    // Ustawienie menedżera na null podczas rozładowywania sesji
                    _multiplayerManager = null;
                    _control.Dispatcher.Invoke(() => _control.UpdateButtonState(false));
                    break;
            }
        }

        private void UpdatePlayerTimeSpent(object state)
        {
            try
            {
                var getRandomRewardsUtils = new GetRandomRewardsUtils(this.RewardItemsConfig, this.TimeSpentRewardsConfig, this.RefferalCodeReward);

                foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                {
                    var steamId = player.Id.SteamId;
                    if (!_playerTimeSpent.ContainsKey(steamId))
                    {
                        _playerTimeSpent[steamId] = TimeSpan.Zero;
                    }

                    _playerTimeSpent[steamId] += TimeSpan.FromMinutes(1);

                    if (_playerTimeSpent[steamId].TotalMinutes >= this.TimeSpentRewardsConfig.RewardInterval)
                    {
                        List<RewardItem> rewards = getRandomRewardsUtils.GetRandomTimeSpentReward();
                        rewards.RemoveAll(item => item == null);

                        if (rewards.Any())
                        {
                            var successfulRewards = new List<string>();

                            foreach (var rewardItem in rewards)
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
                                ChatManager.SendMessageAsOther(this.TimeSpentRewardsConfig.NotificationPrefixx, $"Congratulations! You have received:\n{string.Join("\n", successfulRewards)}", Color.Green, steamId);
                            }
                        }

                        _playerTimeSpent[steamId] = TimeSpan.Zero;
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
                    Type? NexusPatcher = Plugin != null! ? Plugin.Assembly.GetType("Nexus.API.PluginAPISync") : null;
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
        }

        public void UpdateConfig(VoteRewardsConfig newConfig, bool propagateToServers = true)
        {
            if (_config?.Data == null)
            {
                Log.Warn("Config is not initialized.");
                return;
            }

            // aktualizacja wartości konfiguracji...
            _config.Data.DebugMode = newConfig.DebugMode;
            _config.Data.ServerApiKey = newConfig.ServerApiKey;
            _config.Data.VotingLink = newConfig.VotingLink;
            _config.Data.NotificationPrefix = newConfig.NotificationPrefix;
            _config.Data.IsReferralCodeEnabled = newConfig.IsReferralCodeEnabled;
            _config.Data.MaxReferralCodes = newConfig.MaxReferralCodes;
            _config.Data.CommandCooldownMinutes = newConfig.CommandCooldownMinutes;
            _config.Data.ReferralCodePrefix = newConfig.ReferralCodePrefix;
            _config.Data.IsEventCodeEnabled = newConfig.IsEventCodeEnabled;
            _config.Data.EventCodePrefix = newConfig.EventCodePrefix;

            _config.Save();

            // Propaguj konfigurację do innych serwerów tylko jeśli jest to wymagane
            if (propagateToServers)
            {
                NexusManager.SendConfigToAllServers(newConfig);
            }
        }

        public void UpdateTimeSpentRewardsConfig(TimeSpentRewardsConfig newConfig)
        {
            // Sprawdzanie, czy _timeSpentRewardsConfig nie jest nullem i czy jego Data jest dostępna
            if (_timeSpentRewardsConfig?.Data == null)
            {
                Log.Warn("TimeSpentRewardsConfig is not initialized.");
                return;
            }

            // Aktualizacja wartości konfiguracji
            _timeSpentRewardsConfig.Data.RewardInterval = newConfig.RewardInterval;
            _timeSpentRewardsConfig.Data.RewardsList = newConfig.RewardsList;
            _timeSpentRewardsConfig.Data.NotificationPrefixx = newConfig.NotificationPrefixx;


            _timeSpentRewardsConfig.Save();
        }

        public void UpdateRewardItemsConfig(RewardItemsConfig newConfig)
        {
            // Sprawdzanie, czy _timeSpentRewardsConfig nie jest nullem i czy jego Data jest dostępna
            if (_timeSpentRewardsConfig?.Data == null)
            {
                Log.Warn("RewardItemsConfig is not initialized.");
                return;
            }

            // Aktualizacja wartości konfiguracji
            _rewardItemsConfig.Data.RewardItems = newConfig.RewardItems;


            _rewardItemsConfig.Save();
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