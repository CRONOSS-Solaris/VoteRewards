﻿using Torch;

namespace VoteRewards
{
    public class VoteRewardsConfig : ViewModel
    {
        private string _serverApiKey;
        private string _votingLink;
        private string _notificationPrefix = "Reward";
        private bool _debugMode = false;

        public bool DebugMode { get => _debugMode; set => SetValue(ref _debugMode, value); }
        public string ServerApiKey
        {
            get => _serverApiKey;
            set => SetValue(ref _serverApiKey, value);
        }

        public string VotingLink
        {
            get => _votingLink;
            set => SetValue(ref _votingLink, value);
        }

        public string NotificationPrefix
        {
            get => _notificationPrefix;
            set => SetValue(ref _notificationPrefix, value);
        }
    }


}
