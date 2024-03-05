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
            if (File.Exists(_dataFilePath))
            {
                _doc = XDocument.Load(_dataFilePath);
            }
            else
            {
                _doc = new XDocument(new XElement("Players"));
                _doc.Save(_dataFilePath);
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
            var playerElement = _doc.Root.Elements("Player").FirstOrDefault(x => ulong.Parse(x.Attribute("SteamID").Value) == steamId);
            if (playerElement != null)
            {
                var lastClaimDateElement = playerElement.Element("LastRewardClaimDate");
                if (lastClaimDateElement != null)
                {
                    lastClaimDateElement.SetValue(claimDate);
                }
                else
                {
                    playerElement.Add(new XElement("LastRewardClaimDate", claimDate));
                }
            }
            else
            {
                _doc.Root.Add(new XElement("Player",
                    new XAttribute("SteamID", steamId),
                    new XElement("LastRewardClaimDate", claimDate)));
            }
            _doc.Save(_dataFilePath);
        }

        public static void HandleReceivedPlayerRewardTrackerData(ulong steamId, DateTime claimDate)
        {
            var rewardTracker = new PlayerRewardTracker(Path.Combine(VoteRewardsMain.Instance.StoragePath, "VoteReward", "PlayerData.xml"));
            rewardTracker.UpdateLastRewardClaimDate(steamId, claimDate);
        }
    }
}
