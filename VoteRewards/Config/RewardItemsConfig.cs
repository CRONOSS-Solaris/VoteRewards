using ProtoBuf;
using System.Collections.Generic;
using VoteRewards.Utils;

namespace VoteRewards.Config
{
    [ProtoContract]
    public class RewardItemsConfig
    {
        [ProtoMember(1)]
        public List<RewardItem> RewardItems { get; set; }

        public RewardItemsConfig()
        {
            RewardItems = new List<RewardItem>();
        }
    }
}
