using Torch;

namespace VoteRewards
{
    public class VoteRewardsConfig : ViewModel
    {
        private string _serverApiKey;
        private string _votingLink;
        private string _notificationPrefix = "Reward";
        private bool _debugMode = false;
        //referral code
        private bool _isReferralCodeEnabled = false;
        private int _maxReferralCodes = 10;
        private int _commandCooldownMinutes = 60;

        public bool DebugMode 
        {
            get => _debugMode; 
            set => SetValue(ref _debugMode, value); 
        }

        public bool IsReferralCodeEnabled
        {
            get => _isReferralCodeEnabled;
            set => SetValue(ref _isReferralCodeEnabled, value);
        }

        public int MaxReferralCodes
        {
            get => _maxReferralCodes;
            set => SetValue(ref _maxReferralCodes, value);
        }
        public int CommandCooldownMinutes
        {
            get => _commandCooldownMinutes;
            set => SetValue(ref _commandCooldownMinutes, value);
        }

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
