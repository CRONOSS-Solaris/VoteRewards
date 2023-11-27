using System.Collections.Generic;

namespace VoteRewards.Utils
{
    public class ReferralCode
    {
        public string PlayerName { get; set; }
        public ulong SteamId { get; set; }
        public List<string> Codes { get; set; } = new List<string>();
        public List<ulong> RedeemedBySteamIds { get; set; } = new List<ulong>();
        public int CodeUsageCount { get; set; } = 0;
    }
}
