using System.Collections.Generic;
using VoteRewards.Utils;

namespace VoteRewards.Config
{
    public class RewardItemsConfig
    {
        public List<RewardItem> RewardItems { get; set; }

        public RewardItemsConfig()
        {
            RewardItems = new List<RewardItem>();
        }
    }
}
