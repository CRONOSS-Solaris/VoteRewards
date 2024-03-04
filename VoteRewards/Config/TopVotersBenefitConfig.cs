using ProtoBuf;
using System.Collections.Generic;

namespace VoteRewards.Config
{
    [ProtoContract]
    public class TopVotersBenefitConfig
    {
        [ProtoMember(1)]
        public List<VoteRangeReward> VoteRangeRewards { get; set; }

        public TopVotersBenefitConfig()
        {
            VoteRangeRewards = new List<VoteRangeReward>();
        }
    }

}
