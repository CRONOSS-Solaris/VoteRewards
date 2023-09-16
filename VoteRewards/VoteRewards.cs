using NLog;
using Sandbox.Definitions;  // Importujemy, aby móc używać MyPhysicalItemDefinition
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;  // Importujemy, aby móc używać list
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using VoteRewards.Config;
using VoteRewards.Utils;
using VRage;
using VRage.Game;  // Importujemy, aby móc używać MyDefinitionManager
using VRage.ObjectBuilders;
using System.Threading;
using VRageMath;

namespace VoteRewards
{
    public class VoteRewards : TorchPluginBase, IWpfPlugin
    {

        public static IChatManagerServer ChatManager => TorchBase.Instance.CurrentSession.Managers.GetManager<IChatManagerServer>();
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly string CONFIG_FILE_NAME = "VoteRewardsConfig.cfg";
        private static readonly string REWARD_ITEMS_CONFIG_FILE_NAME = "RewardItemsConfig.cfg";

        private VoteRewardsControl _control;
        public UserControl GetControl()
        {
            if (_control == null)
            {
                _control = new VoteRewardsControl(this);
            }
            return _control;
        }

        private Persistent<VoteRewardsConfig> _config;
        public VoteRewardsConfig Config => _config?.Data;

        private Persistent<RewardItemsConfig> _rewardItemsConfig;
        public RewardItemsConfig RewardItemsConfig => _rewardItemsConfig?.Data;

        private Persistent<TimeSpentRewardsConfig> _timeSpentRewardsConfig;
        public TimeSpentRewardsConfig TimeSpentRewardsConfig => _timeSpentRewardsConfig?.Data;

        private Random _random = new Random();

        // Nowe listy do przechowywania dostępnych typów i podtypów przedmiotów
        public List<string> AvailableItemTypes { get; private set; } = new List<string>();
        public Dictionary<string, List<string>> AvailableItemSubtypes { get; private set; } = new Dictionary<string, List<string>>();

        private IMultiplayerManagerBase _multiplayerManager;

        private Dictionary<ulong, TimeSpan> _playerTimeSpent = new Dictionary<ulong, TimeSpan>();
        private Timer _updatePlayerTimeSpentTimer;

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            SetupConfig();
            SetupRewardItemsConfig();
            SetupTimeSpentRewardsConfig();

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
            Log.Info("Control loaded successfully.");
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

                    _updatePlayerTimeSpentTimer = new Timer(UpdatePlayerTimeSpent, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                    // Tutaj możesz zainicjować referencję do menedżera multiplayer
                    _multiplayerManager = Torch.Managers.GetManager<IMultiplayerManagerBase>();
                    if (_multiplayerManager == null)
                    {
                        Log.Warn("Could not get multiplayer manager.");
                    }
                    else
                    {
                        Log.Info("Multiplayer manager initialized.");
                        _multiplayerManager.PlayerJoined += OnPlayerJoined;
                        _multiplayerManager.PlayerLeft += OnPlayerLeft;
                    }

                    LoadAvailableItemTypesAndSubtypes();
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");

                    // Ustawienie menedżera na null podczas rozładowywania sesji
                    _multiplayerManager = null;
                    break;
            }
        }

        private void UpdatePlayerTimeSpent(object state)
        {
            foreach (var player in MySession.Static.Players.GetOnlinePlayers())
            {
                var steamId = player.Id.SteamId;
                if (!_playerTimeSpent.ContainsKey(steamId))
                {
                    _playerTimeSpent[steamId] = TimeSpan.Zero;
                }

                _playerTimeSpent[steamId] += TimeSpan.FromMinutes(1);

                // Check if the player qualifies for a reward
                if (_playerTimeSpent[steamId].TotalMinutes >= TimeSpentRewardsConfig.RewardInterval)
                {
                    // Get a list of rewards using the updated method
                    List<RewardItem> rewards = GetRandomTimeSpentReward();

                    // Remove null rewards (if any)
                    rewards.RemoveAll(item => item == null);

                    if (rewards.Any())
                    {
                        // Collect all the successful rewards in one place
                        var successfulRewards = new List<string>();

                        foreach (var rewardItem in rewards)
                        {
                            bool awarded = AwardPlayer(steamId, rewardItem);  // Award the player the reward

                            // If the award was successful, add it to the list of successful rewards
                            if (awarded)
                            {
                                successfulRewards.Add($"{rewardItem.Amount}x {rewardItem.ItemSubtypeId}");
                            }
                        }

                        // Check if any rewards were successfully awarded
                        if (successfulRewards.Any())
                        {
                            // Send a single notification with all the rewards
                            ChatManager.SendMessageAsOther($"{TimeSpentRewardsConfig.NotificationPrefixx}", $"\nCongratulations! You have received:\n{string.Join("\n", successfulRewards)}\nfor your time spent on the server.", Color.Green, steamId);
                        }
                    }

                    // Reset the player's time spent
                    _playerTimeSpent[steamId] = TimeSpan.Zero;
                }
            }
        }

        private void SetupConfig()
        {
            var configFolderPath = Path.Combine(StoragePath, "VoteReward", "Config");
            Directory.CreateDirectory(configFolderPath);
            var configFile = Path.Combine(configFolderPath, CONFIG_FILE_NAME);

            try
            {
                _config = Persistent<VoteRewardsConfig>.Load(configFile);
            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data == null)
            {
                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<VoteRewardsConfig>(configFile, new VoteRewardsConfig());
                _config.Save();
            }
        }

        private void SetupRewardItemsConfig()
        {
            var configFolderPath = Path.Combine(StoragePath, "VoteReward", "Config");
            Directory.CreateDirectory(configFolderPath);
            var configFilePath = Path.Combine(configFolderPath, REWARD_ITEMS_CONFIG_FILE_NAME);

            try
            {
                _rewardItemsConfig = Persistent<RewardItemsConfig>.Load(configFilePath);
            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_rewardItemsConfig?.Data == null)
            {
                Log.Info("Creating default reward items config because none was found!");

                _rewardItemsConfig = new Persistent<RewardItemsConfig>(configFilePath, new RewardItemsConfig());
                _rewardItemsConfig.Save();
            }
        }

        private void SetupTimeSpentRewardsConfig()
        {
            var configFolderPath = Path.Combine(StoragePath, "VoteReward", "Config");
            Directory.CreateDirectory(configFolderPath);
            var configFilePath = Path.Combine(configFolderPath, "TimeSpentRewardsConfig.cfg");

            try
            {
                _timeSpentRewardsConfig = Persistent<TimeSpentRewardsConfig>.Load(configFilePath);
            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_timeSpentRewardsConfig?.Data == null)
            {
                Log.Info("Creating default time spent rewards config because none was found!");

                _timeSpentRewardsConfig = new Persistent<TimeSpentRewardsConfig>(configFilePath, new TimeSpentRewardsConfig());
                _timeSpentRewardsConfig.Save();
            }
        }

        public List<RewardItem> GetRandomTimeSpentReward()
        {
            List<RewardItem> rewardItems = new List<RewardItem>();

            // Dodajemy do listy przedmioty z 100% szansą na upadek
            rewardItems.AddRange(TimeSpentRewardsConfig.RewardsList.Where(item => item.ChanceToDrop == 100));

            // Dla każdego przedmiotu z szansą mniejszą niż 100%, losujemy, czy powinien zostać zwrócony
            var rewardItemsToConsider = TimeSpentRewardsConfig.RewardsList.Where(item => item.ChanceToDrop < 100);

            foreach (var item in rewardItemsToConsider)
            {
                int randomValue = _random.Next(0, 101);
                if (randomValue <= item.ChanceToDrop)
                {
                    rewardItems.Add(item);
                }
            }

            return rewardItems;
        }

        public List<RewardItem> GetRandomRewards()
        {
            List<RewardItem> rewardItems = new List<RewardItem>();

            // Dodajemy do listy przedmioty z 100% szansą na upadek
            rewardItems.AddRange(RewardItemsConfig.RewardItems.Where(item => item.ChanceToDrop == 100));

            // Dla każdego przedmiotu z szansą mniejszą niż 100%, losujemy, czy powinien zostać zwrócony
            var rewardItemsToConsider = RewardItemsConfig.RewardItems.Where(item => item.ChanceToDrop < 100);

            foreach (var item in rewardItemsToConsider)
            {
                int randomValue = _random.Next(0, 101);
                if (randomValue <= item.ChanceToDrop)
                {
                    rewardItems.Add(item);
                }
            }

            return rewardItems;
        }

        public bool AwardPlayer(ulong steamId, RewardItem rewardItem)
        {
            var player = MySession.Static.Players.TryGetPlayerBySteamId(steamId);
            if (player == null)
            {
                return false;
            }

            // Spróbuj znaleźć definicję przedmiotu na podstawie Id przedmiotu
            MyDefinitionId definitionId = new MyDefinitionId(MyObjectBuilderType.Parse(rewardItem.ItemTypeId), rewardItem.ItemSubtypeId);
            if (!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(definitionId, out var itemDefinition))
            {
                Log.Warn($"Could not find item definition for {rewardItem.ItemTypeId} {rewardItem.ItemSubtypeId}");
                return false;
            }

            // Stwórz nowy przedmiot używając definicji przedmiotu i ilości
            MyObjectBuilder_PhysicalObject physicalObject = MyObjectBuilderSerializer.CreateNewObject(definitionId) as MyObjectBuilder_PhysicalObject;

            // Get the character's inventory
            var character = player.Character;
            if (character == null)
            {
                Log.Warn($"Player {player.DisplayName} is not spawned.");
                return false;
            }
            var inventory = character.GetInventory();
            if (inventory == null)
            {
                Log.Warn($"Could not get the inventory for player {player.DisplayName}.");
                return false;
            }

            // Sprawdź dostępność miejsca w inwentarzu
            var itemVolume = (MyFixedPoint)(rewardItem.Amount * itemDefinition.Volume);
            if (inventory.CurrentVolume + itemVolume > inventory.MaxVolume)
            {
                return false; // Nie ma wystarczająco miejsca w inwentarzu
            }

            // Dodaj przedmiot do inwentarza gracza
            inventory.AddItems(rewardItem.Amount, physicalObject);
            return true; // Nagroda została przyznana
        }


        public void Save()
        {
            try
            {
                _config.Save();
                _rewardItemsConfig.Save();
                Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                Log.Warn(e, "Configuration failed to save");
            }
        }

        public async Task<int> CheckVoteStatusAsync(string steamId)
        {
            string serverKey = Config.ServerApiKey;
            string apiUrl = $"https://space-engineers.com/api/?object=votes&element=claim&key={serverKey}&steamid={steamId}";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response;

                try
                {
                    response = await client.GetAsync(apiUrl);
                }
                catch (HttpRequestException e)
                {
                    Log.Warn("Network error while contacting the API: " + e.Message);
                    return -1;
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                Log.Info($"API Response: {responseContent}");

                if (responseContent.Contains("Error"))
                {
                    Log.Warn($"API responded with an error: {responseContent}");
                    return -1;
                }

                if (!int.TryParse(responseContent, out int voteStatus))
                {
                    Log.Warn("Failed to parse API response");
                    return -1;
                }

                return voteStatus;
            }
        }

        public async Task SetVoteAsClaimedAsync(ulong steamId)
        {
            string serverKey = Config.ServerApiKey;
            string apiUrl = $"https://space-engineers.com/api/?action=post&object=votes&element=claim&key={serverKey}&steamid={steamId}";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(apiUrl);
                }
                catch (HttpRequestException e)
                {
                    Log.Warn("Network error while contacting the API: " + e.Message);
                    throw;
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                Log.Info($"API Response for setting vote as claimed: {responseContent}");

                if (responseContent.Contains("Error"))
                {
                    Log.Warn($"API responded with an error while setting vote as claimed: {responseContent}");
                    throw new Exception("API responded with an error.");
                }

                if (responseContent.Trim() != "1")
                {
                    Log.Warn("Failed to set the vote as claimed");
                    throw new Exception("Failed to set the vote as claimed");
                }
            }
        }


        // Nowa metoda do ładowania dostępnych typów i podtypów przedmiotów
        private void LoadAvailableItemTypesAndSubtypes()
        {
            foreach (var definition in MyDefinitionManager.Static.GetPhysicalItemDefinitions())
            {
                string typeId = definition.Id.TypeId.ToString();
                string subtypeId = definition.Id.SubtypeId.String;

                Log.Info($"Loading item: TypeId={typeId}, SubtypeId={subtypeId}");

                if (!AvailableItemTypes.Contains(typeId))
                {
                    AvailableItemTypes.Add(typeId);
                }

                if (!AvailableItemSubtypes.ContainsKey(typeId))
                {
                    AvailableItemSubtypes[typeId] = new List<string>();
                }

                if (!AvailableItemSubtypes[typeId].Contains(subtypeId))
                {
                    AvailableItemSubtypes[typeId].Add(subtypeId);
                }
            }

            // Upewniamy się, że każdy typ przedmiotu ma przypisaną listę, nawet jeśli jest pusta
            foreach (var itemType in AvailableItemTypes)
            {
                if (!AvailableItemSubtypes.ContainsKey(itemType))
                {
                    AvailableItemSubtypes[itemType] = new List<string>();
                }
            }

            Log.Info($"Loaded {AvailableItemTypes.Count} item types and {AvailableItemSubtypes.Count} item subtypes.");
            foreach (var kvp in AvailableItemSubtypes)
            {
                Log.Info($"Type: {kvp.Key}, Subtypes: {string.Join(", ", kvp.Value)}");
            }
        }

    }
}