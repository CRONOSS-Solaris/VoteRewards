using System.Collections.Generic;
using System.Xml.Serialization;

namespace VoteRewards.Utils
{
    public class RewardItem
    {
        public string ItemTypeId { get; set; }
        public string ItemSubtypeId { get; set; }
        public int AmountOne { get; set; }
        public int AmountTwo { get; set; }
        public double ChanceToDrop { get; set; }
        
        [XmlIgnore]
        public List<string> AvailableSubTypeIds { get; set; } = new List<string>();

        public RewardItem()
        {
            // Domyślne wartości
            ItemTypeId = string.Empty;
            ItemSubtypeId = string.Empty;
            AmountOne = 1;
            AmountTwo = 5;
            ChanceToDrop = 10.0;
        }
    }
}

//koniec