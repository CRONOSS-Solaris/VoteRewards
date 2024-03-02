using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoteRewards.Utils;

namespace VoteRewards.Config
{
    [ProtoContract]
    public class TopVotersBenefitConfig
    {
        [ProtoMember(1)]
        public List<RewardItem> RewardItems { get; set; }

        public TopVotersBenefitConfig()
        {
            RewardItems = new List<RewardItem>();
        }
    }
}
