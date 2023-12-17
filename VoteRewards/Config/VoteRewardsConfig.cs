using Torch;
using VRage.Game.ModAPI;

namespace VoteRewards
{
    public class VoteRewardsConfig : ViewModel
    {
        //VoteReward
        private string _serverApiKey;
        private string _votingLink;
        private string _notificationPrefix = "Reward";
        private bool _debugMode = false;
        
        //Referral Code
        private bool _isReferralCodeEnabled = false;
        private int _maxReferralCodes = 10;
        private int _commandCooldownMinutes = 60;
        private string _referralCodePrefix = "REFERRAL CODE";

        //Event Code
        private bool _isEventCodeEnabled = false;
        private string _eventCodePrefix = "EVENT CODE";

        //VoteReward
        public bool DebugMode
        {
            get => _debugMode;
            set => SetValue(ref _debugMode, value);
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

        //Referral Code
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
        public string ReferralCodePrefix
        {
            get => _referralCodePrefix;
            set => SetValue(ref _referralCodePrefix, value);
        }

        //Event Code
        public bool IsEventCodeEnabled
        {
            get => _isEventCodeEnabled;
            set => SetValue(ref _isEventCodeEnabled, value);
        }

        public string EventCodePrefix
        {
            get => _eventCodePrefix;
            set => SetValue(ref _eventCodePrefix, value);
        }
    }

}
