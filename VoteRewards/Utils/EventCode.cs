using ProtoBuf;
using System.Collections.Generic;

namespace VoteRewards.Utils
{
    [ProtoContract]
    public class EventCode
    {
        [ProtoMember(1)]
        public string Code { get; set; }
        [ProtoMember(2)]
        public int? MaxUsageCount { get; set; }
        [ProtoMember(3)]
        public HashSet<ulong> RedeemedBySteamIds { get; set; } = new HashSet<ulong>();

        public EventCode() { }
        public EventCode(string code, int? maxUsageCount = null)
        {
            Code = code;
            MaxUsageCount = maxUsageCount;
        }
    }
}
