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
    public class RefferalCodeReward
    {
        [ProtoMember(1)]
        public List <RewardItem> RewardItems { get; set; }

        public RefferalCodeReward()
        {
            RewardItems = new List<RewardItem>();
        }
    }
}
