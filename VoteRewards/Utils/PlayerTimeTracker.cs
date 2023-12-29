using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Torch.API;
using VoteRewards.Nexus;
using VoteRewards.Utils;

namespace VoteRewards
{
    public class PlayerTimeTracker
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly VoteRewardsConfig _config;
        private readonly Dictionary<ulong, (DateTime JoinTime, string NickName, TimeSpan TotalTimeSpent)> _playerData = new Dictionary<ulong, (DateTime, string, TimeSpan)>();
        private readonly string _dataFilePath;

        public PlayerTimeTracker(VoteRewardsConfig config)
        {
            if (config == null)
            {
                Log.Error("PlayerTimeTracker initialization failed: config is null.");
                return;
            }

            _config = config;
            _dataFilePath = Path.Combine(VoteRewardsMain.Instance.StoragePath, "VoteReward", "PlayerTimeData.xml");

            InitializeDirectory();
            LoadPlayerTimes();
        }

        private void InitializeDirectory()
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(_dataFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath ?? string.Empty);
                    LoggerHelper.DebugLog(Log, _config, "Created directory for player time data.");
                }

                if (!File.Exists(_dataFilePath))
                {
                    XDocument newDoc = new XDocument(new XElement("Players"));
                    newDoc.Save(_dataFilePath);
                    LoggerHelper.DebugLog(Log, _config, "Created new XML file for player time data.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while initializing PlayerTimeTracker directory:");
            }
        }

        public void OnPlayerJoined(IPlayer player)
        {
            LoggerHelper.DebugLog(Log, _config, $"Player joined: {player.Name} (SteamID: {player.SteamId})");

            // Sprawdź, czy gracz ma już zapisany czas
            if (_playerData.TryGetValue(player.SteamId, out var existingPlayerInfo))
            {
                // Ustawienie aktualnego czasu dołączenia, zachowując wcześniej zapisany całkowity czas spędzony
                _playerData[player.SteamId] = (DateTime.UtcNow, player.Name, existingPlayerInfo.TotalTimeSpent);
            }
            else
            {
                // Nowy gracz, zacznij od zera
                _playerData[player.SteamId] = (DateTime.UtcNow, player.Name, TimeSpan.Zero);
            }
        }

        public void OnPlayerLeft(IPlayer player)
        {
            LoggerHelper.DebugLog(Log, _config, $"Player left: {player.Name} (SteamID: {player.SteamId})");
            if (_playerData.TryGetValue(player.SteamId, out var playerInfo))
            {
                var timeSpent = DateTime.UtcNow - playerInfo.JoinTime;
                var totalTimeSpent = playerInfo.TotalTimeSpent + timeSpent;
                UpdatePlayerData(player.SteamId, player.Name, totalTimeSpent);

                // Zapisz zmiany do pliku XML
                SavePlayerTime(player.SteamId, player.Name, totalTimeSpent);

                // Synchronizuj dane z innymi serwerami
                NexusManager.SendPlayerTimeDataToAllServers(player.SteamId, player.Name, totalTimeSpent);
            }
        }

        public void UpdatePlayerData(ulong steamId, string nickName, TimeSpan totalTimeSpent, bool isNewPlayer = false)
        {
            if (isNewPlayer || !_playerData.ContainsKey(steamId))
            {
                _playerData[steamId] = (DateTime.UtcNow, nickName, totalTimeSpent);
            }
            else
            {
                _playerData[steamId] = (_playerData[steamId].JoinTime, nickName, totalTimeSpent);
            }
        }

        public void SaveAllPlayerTimes()
        {
            Log.Info("Saving all player times.");
            foreach (var (steamId, playerInfo) in _playerData)
            {
                SavePlayerTime(steamId, playerInfo.NickName, playerInfo.TotalTimeSpent);
                NexusManager.SendPlayerTimeDataToAllServers(steamId, playerInfo.NickName, playerInfo.TotalTimeSpent);
            }
        }

        public void SavePlayerTime(ulong steamId, string nickName, TimeSpan totalTimeSpent)
        {
            try
            {
                XDocument doc = File.Exists(_dataFilePath) ? XDocument.Load(_dataFilePath) : new XDocument(new XElement("Players"));
                var existingPlayer = doc.Root.Elements("Player").FirstOrDefault(x => x.Attribute("SteamID").Value == steamId.ToString());
                int totalMinutes = (int)totalTimeSpent.TotalMinutes; // Konwersja na liczbę całkowitą

                if (existingPlayer != null)
                {
                    existingPlayer.SetElementValue("TotalTimeSpent", totalMinutes);
                }
                else
                {
                    doc.Root.Add(new XElement("Player",
                        new XAttribute("SteamID", steamId),
                        new XElement("NickName", nickName),
                        new XElement("TotalTimeSpent", totalMinutes)));
                }

                doc.Save(_dataFilePath);
                LoggerHelper.DebugLog(Log, _config, $"Saved player time data for {nickName} (SteamID: {steamId}).");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error occurred while saving player time data for {nickName} (SteamID: {steamId}):");
            }
        }

        private void LoadPlayerTimes()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    var doc = XDocument.Load(_dataFilePath);
                    foreach (var playerElement in doc.Root.Elements("Player"))
                    {
                        ulong steamId = ulong.Parse(playerElement.Attribute("SteamID").Value);
                        string nickName = playerElement.Element("NickName").Value;
                        double minutes = double.Parse(playerElement.Element("TotalTimeSpent").Value);
                        _playerData[steamId] = (DateTime.MinValue, nickName, TimeSpan.FromMinutes(minutes));
                    }
                    LoggerHelper.DebugLog(Log, _config, "Loaded player times from XML file.");
                }
            }
            catch (Exception ex)
            {
                Log.Error (ex, "Error occurred while loading player times from XML file:");
            }
        }

        public TimeSpan GetTotalTimeSpent(ulong steamId)
        {
            if (_playerData.TryGetValue(steamId, out var playerInfo))
            {
                return playerInfo.TotalTimeSpent;
            }
            return TimeSpan.Zero;
        }

        public List<(string NickName, TimeSpan TotalTimeSpent)> GetTopPlayers(int count)
        {
            return _playerData.Values
                .OrderByDescending(p => p.TotalTimeSpent)
                .Take(count)
                .Select(p => (p.NickName, p.TotalTimeSpent))
                .ToList();
        }

        public void SubtractPlayerTime(ulong steamId, TimeSpan timeToSubtract)
        {
            if (_playerData.TryGetValue(steamId, out var playerInfo))
            {
                var newTotalTime = playerInfo.TotalTimeSpent - timeToSubtract;
                if (newTotalTime < TimeSpan.Zero)
                {
                    newTotalTime = TimeSpan.Zero;
                }

                _playerData[steamId] = (playerInfo.JoinTime, playerInfo.NickName, newTotalTime);
                SavePlayerTime(steamId, playerInfo.NickName, newTotalTime);
            }
            else
            {
                Log.Warn($"Player with SteamID {steamId} not found in time tracker.");
            }
        }

        // Metoda pomocnicza do wyszukiwania gracza po NickName
        public (ulong, string)? FindPlayerByNickName(string nickName)
        {
            foreach (var kvp in _playerData)
            {
                if (kvp.Value.NickName.Equals(nickName, StringComparison.OrdinalIgnoreCase))
                {
                    return (kvp.Key, kvp.Value.NickName);
                }
            }
            return null;
        }
    }
}
