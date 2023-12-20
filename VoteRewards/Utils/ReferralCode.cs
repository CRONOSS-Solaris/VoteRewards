using ProtoBuf;
using System.Collections.Generic;

namespace VoteRewards.Utils
{
    [ProtoContract]
    public class ReferralCode
    {
        [ProtoMember(1)]
        public string PlayerName { get; set; }
        [ProtoMember(2)]
        public ulong SteamId { get; set; }
        [ProtoMember(3)]
        public List<string> Codes { get; set; } = new List<string>();
        [ProtoMember(4)]
        public List<ulong> RedeemedBySteamIds { get; set; } = new List<ulong>();
        [ProtoMember(5)]
        public int CodeUsageCount { get; set; } = 0;
    }
}
