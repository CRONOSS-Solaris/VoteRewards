using NLog.Fluent;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoteRewards.Config;
using VRage.Game;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage;
using Torch;
using NLog;
using Sandbox.Game.Entities;

namespace VoteRewards.Utils
{
    internal class RewardManager
    {
        private readonly VoteRewards _plugin;
        private readonly Random _random = new Random();
        private Persistent<VoteRewardsConfig> _config;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public RewardManager(VoteRewards plugin)
        {
            _plugin = plugin;;
        }

        public List<RewardItem> GetRandomRewardsFromList(List<RewardItem> rewardsList)
        {
            List<RewardItem> rewardItems = new List<RewardItem>();

            // Dodajemy do listy przedmioty z 100% szansą na upadek
            rewardItems.AddRange(rewardsList.Where(item => item.ChanceToDrop == 100));

            // Dla każdego przedmiotu z szansą mniejszą niż 100%, losujemy, czy powinien zostać zwrócony
            var rewardItemsToConsider = rewardsList.Where(item => item.ChanceToDrop < 100);
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

        public List<RewardItem> GetRandomTimeSpentReward()
        {
            return GetRandomRewardsFromList(_plugin.TimeSpentRewardsConfig.RewardsList);
        }

        public List<RewardItem> GetRandomRewards()
        {
            return GetRandomRewardsFromList(_plugin.RewardItemsConfig.RewardItems);
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
                LoggerHelper.DebugLog(Log, _config.Data, $"ITEM(): Could not find item definition for {rewardItem.ItemTypeId} {rewardItem.ItemSubtypeId}");
                return false;
            }

            // Stwórz nowy przedmiot używając definicji przedmiotu i ilości
            MyObjectBuilder_PhysicalObject physicalObject = MyObjectBuilderSerializer.CreateNewObject(definitionId) as MyObjectBuilder_PhysicalObject;

            // Get the character's inventory
            var character = player.Character;
            if (character == null)
            {
                LoggerHelper.DebugLog(Log, _config.Data, $"PLAYER(): Player {player.DisplayName} is not spawned.");
                return false;
            }
            var inventory = character.GetInventory();
            if (inventory == null)
            {
                LoggerHelper.DebugLog(Log, _config.Data, $"PLAYER(): Could not get the inventory for player {player.DisplayName}.");
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
    }
}
