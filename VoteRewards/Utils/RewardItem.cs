using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace VoteRewards.Utils
{
    [ProtoContract]
    public class RewardItem
    {
        [ProtoMember(1)]
        public string ItemTypeId { get; set; }

        [ProtoMember(2)]
        public string ItemSubtypeId { get; set; }

        [ProtoMember(3)]
        public int AmountOne { get; set; }

        [ProtoMember(4)]
        public int AmountTwo { get; set; }

        [ProtoMember(5)]
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