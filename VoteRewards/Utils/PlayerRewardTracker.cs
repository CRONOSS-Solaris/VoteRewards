using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Nexus.API;
using VoteRewards.Nexus;

namespace VoteRewards.Utils
{
    public class PlayerRewardTracker
    {
        private readonly string _dataFilePath;
        private XDocument _doc;

        public PlayerRewardTracker(string dataFilePath)
        {
            _dataFilePath = dataFilePath;
            LoadOrCreateDocument();
        }

        private void LoadOrCreateDocument()
        {
            PlayerDataStorage storage = PlayerDataStorage.GetInstance(_dataFilePath);
            _doc = storage.LoadPlayerData();
            if (_doc.Root == null)
            {
                _doc.Add(new XElement("Players"));
                storage.SavePlayerData(_doc);
            }
        }

        public DateTime? GetLastRewardClaimDate(ulong steamId)
        {
            var playerElement = _doc.Root.Elements("Player").FirstOrDefault(x => ulong.Parse(x.Attribute("SteamID").Value) == steamId);
            if (playerElement != null && DateTime.TryParse(playerElement.Element("LastRewardClaimDate")?.Value, out var lastClaimDate))
            {
                return lastClaimDate;
            }
            return null;
        }

        public void UpdateLastRewardClaimDate(ulong steamId, DateTime claimDate)
        {
            PlayerDataStorage storage = PlayerDataStorage.GetInstance(_dataFilePath);
            var playerElement = _doc.Root.Elements("Player").FirstOrDefault(x => ulong.Parse(x.Attribute("SteamID").Value) == steamId);
            if (playerElement != null)
            {
                playerElement.SetElementValue("LastRewardClaimDate", claimDate);
            }
            else
            {
                _doc.Root.Add(new XElement("Player",
                    new XAttribute("SteamID", steamId),
                    new XElement("LastRewardClaimDate", claimDate)));
            }
            storage.SavePlayerData(_doc);
        }

        public static void HandleReceivedPlayerRewardTrackerData(ulong steamId, DateTime claimDate)
        {
            var rewardTracker = new PlayerRewardTracker(Path.Combine(VoteRewardsMain.Instance.StoragePath, "VoteReward", "PlayerData.xml"));
            rewardTracker.UpdateLastRewardClaimDate(steamId, claimDate);
        }
    }
}
