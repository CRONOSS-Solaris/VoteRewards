using ProtoBuf;
using System.Collections.Generic;
using VoteRewards.Utils;


    [ProtoContract]
    public class TimeReward
    {
        [ProtoMember(1)]
        public string NotificationPrefix { get; set; }

        [ProtoMember(2)]
        public int RewardInterval { get; set; }

        [ProtoMember(3)]
        public bool IsNexusSynced { get; set; }

        [ProtoMember(4)]
        public List<RewardItem> RewardsList { get; set; } = new List<RewardItem>();
    }