using System;
using System.Collections.Generic;

namespace VoteRewards.Utils
{
    public class RewardItem
    {
        public string ItemTypeId { get; set; }
        public string ItemSubtypeId { get; set; }
        public int Amount { get; set; }
        public double ChanceToDrop { get; set; }

        public List<string> AvailableSubTypeIds { get; set; } = new List<string>();

        public RewardItem()
        {
            // Domyślne wartości
            ItemTypeId = string.Empty;
            ItemSubtypeId = string.Empty;
            Amount = 1;
            ChanceToDrop = 1.0;
        }
    }
}

//koniec