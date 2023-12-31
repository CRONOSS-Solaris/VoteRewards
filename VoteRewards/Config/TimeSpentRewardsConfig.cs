﻿using System.Collections.Generic;
using Torch;
using VoteRewards.Utils;

namespace VoteRewards.Config
{
    public class TimeSpentRewardsConfig : ViewModel
    {
        private int _rewardInterval = 60;  // Domyślnie ustawiamy na 60 minut
        private List<RewardItem> _rewardsList = new List<RewardItem>();
        private string _notificationPrefixx = "TimeSpentReward";

        public int RewardInterval
        {
            get => _rewardInterval;
            set => SetValue(ref _rewardInterval, value);
        }

        public List<RewardItem> RewardsList
        {
            get => _rewardsList;
            set => SetValue(ref _rewardsList, value);
        }

        public string NotificationPrefixx
        {
            get => _notificationPrefixx;
            set => SetValue(ref _notificationPrefixx, value);
        }
    }
}
