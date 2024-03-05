using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using VoteRewards.Config;

namespace VoteRewards.Utils
{
    public class GetRandomRewardsUtils
    {
        // Assuming you have a static Logger for the class, otherwise you can pass it through the constructor
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Random instance for generating random numbers
        private Random _random = new Random();

        // Instances of configurations passed through the constructor
        private readonly RewardItemsConfig _rewardItemsConfig;
        private readonly TimeSpentRewardsConfig _timeSpentRewardsConfig;
        private readonly RefferalCodeReward _refferalCodeReward;
        private readonly TopVotersBenefitConfig _topVotersBenefitConfig;

        // Constructor to pass in instances of configurations
        public GetRandomRewardsUtils(RewardItemsConfig rewardItemsConfig, TimeSpentRewardsConfig timeSpentRewardsConfig, RefferalCodeReward refferalCodeReward, TopVotersBenefitConfig topVotersBenefitConfig)
        {
            _rewardItemsConfig = rewardItemsConfig ?? throw new ArgumentNullException(nameof(rewardItemsConfig));
            _timeSpentRewardsConfig = timeSpentRewardsConfig ?? throw new ArgumentNullException(nameof(timeSpentRewardsConfig));
            _refferalCodeReward = refferalCodeReward ?? throw new ArgumentNullException(nameof(refferalCodeReward));
            _topVotersBenefitConfig = topVotersBenefitConfig ?? throw new ArgumentNullException(nameof(topVotersBenefitConfig));
        }

        // Method to get random rewards from a given list of rewards
        public List<RewardItem> GetRandomRewardsFromList(List<RewardItem> rewardsList)
        {
            List<RewardItem> rewardItems = new List<RewardItem>();

            // Adding items with a 100% chance to drop
            rewardItems.AddRange(rewardsList.Where(item => item.ChanceToDrop == 100));

            // For each item with less than 100% chance, randomly determine if it should be returned
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

        // Method to get a random time spent reward
        public List<RewardItem> GetRandomTimeSpentReward()
        {
            return GetRandomRewardsFromList(_timeSpentRewardsConfig.RewardsList);
        }

        // Method to get random rewards based on the RewardItemsConfig
        public List<RewardItem> GetRandomRewards()
        {
            if (_rewardItemsConfig == null || _rewardItemsConfig.RewardItems == null)
            {
                Log.Error("Error: One or more required properties are null in RewardManager. Cannot proceed.");
                return null;
            }

            return GetRandomRewardsFromList(_rewardItemsConfig.RewardItems);
        }


        public static class ThreadSafeRandom
        {
            private static readonly Random Global = new Random();
            [ThreadStatic] private static Random _local;

            public static int Next(int min, int max)
            {
                Random inst = _local;
                if (inst == null)
                {
                    int seed;
                    lock (Global) seed = Global.Next();
                    _local = inst = new Random(seed);
                }
                return inst.Next(min, max);
            }
        }


        public int GetRandomAmount(int amountOne, int amountTwo)
        {
            return ThreadSafeRandom.Next(amountOne, amountTwo + 1);
        }

    }
}
