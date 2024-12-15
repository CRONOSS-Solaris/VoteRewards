using ProtoBuf;
using System.Collections.Generic;
using Torch;
using VoteRewards.Utils;

namespace VoteRewards.Config
{
    [ProtoContract]
    public class TimeSpentRewardsConfig : ViewModel
    {
        private List<TimeReward> _timeRewards = new List<TimeReward>();

        [ProtoMember(1)]
        public List<TimeReward> TimeRewards
        {
            get => _timeRewards;
            set => SetValue(ref _timeRewards, value);
        }
    }
}
