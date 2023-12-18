using ProtoBuf;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Torch;
using VoteRewards.Nexus;
using VoteRewards.Utils;

namespace VoteRewards.Config
{
    [ProtoContract]
    public class TimeSpentRewardsConfig : ViewModel
    {
        private int _rewardInterval = 60;  // Domyślnie ustawiamy na 60 minut
        private List<RewardItem> _rewardsList = new List<RewardItem>();
        private string _notificationPrefixx = "TimeSpentReward";

        [ProtoMember(1)]
        public int RewardInterval
        {
            get => _rewardInterval;
            set => SetValue(ref _rewardInterval, value);
        }

        [ProtoMember(2)]
        public List<RewardItem> RewardsList
        {
            get => _rewardsList;
            set => SetValue(ref _rewardsList, value);
        }

        [ProtoMember(3)]
        public string NotificationPrefixx
        {
            get => _notificationPrefixx;
            set => SetValue(ref _notificationPrefixx, value);
        }

    }
}
