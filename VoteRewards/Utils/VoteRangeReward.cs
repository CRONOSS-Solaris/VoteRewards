using ProtoBuf;
using System.Collections.Generic;
using VoteRewards.Utils;

[ProtoContract]
public class VoteRangeReward
{
    [ProtoMember(1)]
    public int MinVotes { get; set; }
    [ProtoMember(2)]
    public int MaxVotes { get; set; }
    [ProtoMember(3)]
    public List<RewardItem> Rewards { get; set; }
}
