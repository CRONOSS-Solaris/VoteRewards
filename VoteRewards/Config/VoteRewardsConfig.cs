﻿using ProtoBuf;
using Torch;

namespace VoteRewards
{
    [ProtoContract]
    public class VoteRewardsConfig : ViewModel
    {
        //VoteReward
        private string _serverApiKey;
        private string _votingLink;
        private string _notificationPrefix = "Reward";
        private bool _debugMode = false;

        //DataBase Postgresql
        private bool _useDatabase;
        private string _databaseHost = "localhost";
        private int _databasePort = 5432;
        private string _databaseName = "mydatabase";
        private string _databaseUsername = "myuser";
        private string _databasePassword = "mypassword";

        //DataBase Postgresql
        [ProtoMember(12)]
        public bool UseDatabase { get => _useDatabase; set => SetValue(ref _useDatabase, value); }
        [ProtoMember(13)]
        public string DatabaseHost { get => _databaseHost; set => SetValue(ref _databaseHost, value); }
        [ProtoMember(14)]
        public int DatabasePort { get => _databasePort; set => SetValue(ref _databasePort, value); }
        [ProtoMember(15)]
        public string DatabaseName { get => _databaseName; set => SetValue(ref _databaseName, value); }
        [ProtoMember(16)]
        public string DatabaseUsername { get => _databaseUsername; set => SetValue(ref _databaseUsername, value); }
        [ProtoMember(17)]
        public string DatabasePassword { get => _databasePassword; set => SetValue(ref _databasePassword, value); }

        //Referral Code
        private bool _isReferralCodeEnabled = false;
        private int _maxReferralCodes = 10;
        private int _commandCooldownMinutes = 60;
        private string _referralCodePrefix = "REFERRAL CODE";
        private int _referralCodeUsageTimeLimit = 420;

        //Event Code
        private bool _isEventCodeEnabled = false;
        private string _eventCodePrefix = "EVENT CODE";

        //Nexus
        private bool _isLobby;
        public bool isLobby { get => _isLobby; set => SetValue(ref _isLobby, value); }

        //VoteReward
        [ProtoMember(1)]
        public bool DebugMode
        { get => _debugMode;  set => SetValue(ref _debugMode, value); }
        [ProtoMember(2)]
        public string ServerApiKey
        { get => _serverApiKey; set => SetValue(ref _serverApiKey, value); }

        [ProtoMember(3)]
        public string VotingLink
        {
            get => _votingLink;
            set => SetValue(ref _votingLink, value);
        }

        [ProtoMember(4)]
        public string NotificationPrefix
        {
            get => _notificationPrefix;
            set => SetValue(ref _notificationPrefix, value);
        }

        //Referral Code
        [ProtoMember(5)]
        public bool IsReferralCodeEnabled
        {
            get => _isReferralCodeEnabled;
            set => SetValue(ref _isReferralCodeEnabled, value);
        }

        [ProtoMember(6)]
        public int MaxReferralCodes
        {
            get => _maxReferralCodes;
            set => SetValue(ref _maxReferralCodes, value);
        }
        
        [ProtoMember(7)]
        public int CommandCooldownMinutes
        {
            get => _commandCooldownMinutes;
            set => SetValue(ref _commandCooldownMinutes, value);
        }
        
        [ProtoMember(8)]
        public string ReferralCodePrefix
        {
            get => _referralCodePrefix;
            set => SetValue(ref _referralCodePrefix, value);
        }
        [ProtoMember(9)]
        public int ReferralCodeUsageTimeLimit
        {
            get => _referralCodeUsageTimeLimit;
            set => SetValue(ref _referralCodeUsageTimeLimit, value);
        }

        //Event Code

        [ProtoMember(10)]
        public bool IsEventCodeEnabled
        {
            get => _isEventCodeEnabled;
            set => SetValue(ref _isEventCodeEnabled, value);
        }

        [ProtoMember(11)]
        public string EventCodePrefix
        {
            get => _eventCodePrefix;
            set => SetValue(ref _eventCodePrefix, value);
        }
    }

}
