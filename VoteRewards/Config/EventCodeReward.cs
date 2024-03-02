using ProtoBuf;
using System.Collections.Generic;
using VoteRewards.Utils;

namespace VoteRewards.Config
{
    [ProtoContract]
    public class EventCodeReward
    {
        [ProtoMember(1)]
        public List <RewardItem> RewardItems { get; set; }

        public EventCodeReward()
        {
            RewardItems = new List<RewardItem>();
        }
    }
}
