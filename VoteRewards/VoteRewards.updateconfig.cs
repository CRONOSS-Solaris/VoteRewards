using Torch;
using Torch.API.Plugins;
using VoteRewards.Config;
using VoteRewards.Nexus;

namespace VoteRewards
{
    public partial class VoteRewardsMain : TorchPluginBase, IWpfPlugin
    {
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
            _config.Data.ReferralCodeUsageTimeLimit = newConfig.ReferralCodeUsageTimeLimit;
            _config.Data.UseDatabase = newConfig.UseDatabase;
            _config.Data.DatabaseHost = newConfig.DatabaseHost;
            _config.Data.DatabasePort = newConfig.DatabasePort;
            _config.Data.DatabasePassword = newConfig.DatabasePassword;
            _config.Data.DatabaseName = newConfig.DatabaseName;
            _config.Data.DatabaseUsername = newConfig.DatabaseUsername;

            _config.Save();

            // Propaguj konfigurację do innych serwerów tylko jeśli jest to wymagane
            if (propagateToServers)
            {
                NexusManager.SendConfigToAllServers(newConfig);
            }
        }

        public void UpdateTimeSpentRewardsConfig(TimeSpentRewardsConfig newConfig)
        {
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
            if (_timeSpentRewardsConfig?.Data == null)
            {
                Log.Warn("RewardItemsConfig is not initialized.");
                return;
            }

            // Aktualizacja wartości konfiguracji
            _rewardItemsConfig.Data.RewardItems = newConfig.RewardItems;


            _rewardItemsConfig.Save();
        }

        public void UpdateRefferalCodeReward(RefferalCodeReward newConfig)
        {
            if (_refferalCodeReward?.Data == null)
            {
                Log.Warn("RewardItemsConfig is not initialized.");
                return;
            }

            // Aktualizacja wartości konfiguracji
            _refferalCodeReward.Data.RewardItems = newConfig.RewardItems;


            _refferalCodeReward.Save();
        }

        public void UpdateEventCodeReward(EventCodeReward newConfig)
        {
            if (_eventCodeReward?.Data == null)
            {
                Log.Warn("EventCodeReward is not initialized.");
                return;
            }

            // Aktualizacja wartości konfiguracji
            _eventCodeReward.Data.RewardItems = newConfig.RewardItems;


            _eventCodeReward.Save();
        }

        public void UpdateTopVotersBenefit(TopVotersBenefitConfig newConfig)
        {
            if (_topVotersBenefitConfig?.Data == null)
            {
                Log.Warn("TopVotersBenefit is not initialized.");
                return;
            }

            // Aktualizacja wartości konfiguracji
            _topVotersBenefitConfig.Data.VoteRangeRewards = newConfig.VoteRangeRewards;


            _topVotersBenefitConfig.Save();
        }
    }
}
