using ProtoBuf;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VoteRewards.Config
{
    [ProtoContract]
    public class TopVotersBenefitConfig
    {
        [ProtoMember(1)]
        public ObservableCollection<VoteRangeReward> VoteRangeRewards { get; set; } = new ObservableCollection<VoteRangeReward>();

        public TopVotersBenefitConfig()
        {
            VoteRangeRewards = new ObservableCollection<VoteRangeReward>();
        }
    }

}
