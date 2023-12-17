using System.Collections.Generic;

namespace VoteRewards.Utils
{
    public class EventCode
    {
        public string Code { get; set; }
        public int? MaxUsageCount { get; set; }
        public HashSet<ulong> RedeemedBySteamIds { get; set; } = new HashSet<ulong>();

        public EventCode(string code, int? maxUsageCount = null)
        {
            Code = code;
            MaxUsageCount = maxUsageCount;
        }
    }
}
