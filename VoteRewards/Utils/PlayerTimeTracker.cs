using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Torch.API;
using VoteRewards.Nexus;
using VoteRewards.Utils;

namespace VoteRewards
{
    public class PlayerTimeTracker
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly ConcurrentDictionary<ulong, (DateTime JoinTime, string NickName, TimeSpan TotalTimeSpent)> _playerData = new ConcurrentDictionary<ulong, (DateTime, string, TimeSpan)>();
        private readonly string _dataFilePath;

        public PlayerTimeTracker()
        {
            _dataFilePath = Path.Combine(VoteRewardsMain.Instance.StoragePath, "VoteReward", "PlayerData.xml");

            InitializeDirectory();
            LoadPlayerTimes();
        }

        private void InitializeDirectory()
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(_dataFilePath) ?? string.Empty;
                Directory.CreateDirectory(directoryPath);

                if (!File.Exists(_dataFilePath))
                {
                    var newDoc = new XDocument(new XElement("Players"));
                    newDoc.Save(_dataFilePath);
                    LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, "Created new XML file for player time data.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while initializing PlayerTimeTracker directory:");
            }
        }

        public void OnPlayerJoined(IPlayer player)
        {
            LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"Player joined: {player.Name} (SteamID: {player.SteamId})");

            var isNewPlayer = !_playerData.ContainsKey(player.SteamId);
            _playerData.AddOrUpdate(player.SteamId,
                (DateTime.UtcNow, player.Name, TimeSpan.Zero),
                (key, existingValue) => (DateTime.UtcNow, player.Name, existingValue.TotalTimeSpent));

            if (isNewPlayer)
            {
                // Asynchronicznie zapisz czas gracza jako zero, jeśli to nowy gracz
                Task.Run(() => SavePlayerTimeAsync(player.SteamId, player.Name, TimeSpan.Zero));

                // Asynchroniczna synchronizacja danych z innymi serwerami
                Task.Run(() => NexusManager.SendPlayerTimeDataToAllServers(player.SteamId, player.Name, TimeSpan.Zero));
            }
        }


        public void OnPlayerLeft(IPlayer player)
        {
            LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"Player left: {player.Name} (SteamID: {player.SteamId})");
            if (_playerData.TryGetValue(player.SteamId, out var playerInfo))
            {
                // Obliczenie czasu spędzonego podczas tej sesji
                var timeSpentThisSession = DateTime.UtcNow - playerInfo.JoinTime;

                // Obliczenie całkowitego czasu spędzonego na serwerze
                var totalTimeSpent = playerInfo.TotalTimeSpent + timeSpentThisSession;

                // Aktualizacja danych gracza
                UpdatePlayerData(player.SteamId, player.Name, totalTimeSpent);

                // Logowanie czasu spędzonego podczas tej sesji
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"Player {player.Name} (SteamID: {player.SteamId}) spent {timeSpentThisSession.TotalMinutes} minutes on the server this session.");

                // Logowanie całkowitego czasu spędzonego na serwerze, wraz z wartościami, które są do siebie dodawane
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"Total time spent by {player.Name} (SteamID: {player.SteamId}) on the server: previously {playerInfo.TotalTimeSpent.TotalMinutes} minutes + {timeSpentThisSession.TotalMinutes} minutes this session = {totalTimeSpent.TotalMinutes} minutes.");

                // Asynchroniczne zapisywanie czasu gracza
                Task.Run(() => SavePlayerTimeAsync(player.SteamId, player.Name, totalTimeSpent));

                // Asynchroniczna synchronizacja danych z innymi serwerami
                Task.Run(() => NexusManager.SendPlayerTimeDataToAllServers(player.SteamId, player.Name, totalTimeSpent));
            }
        }


        public void UpdatePlayerData(ulong steamId, string nickName, TimeSpan totalTimeSpent)
        {
            // Aktualizacja danych gracza lub dodanie nowego, jeśli nie istnieje
            var playerInfo = _playerData.GetOrAdd(steamId, (DateTime.UtcNow, nickName, totalTimeSpent));
            if (playerInfo.JoinTime != DateTime.UtcNow)
            {
                // Gracz istnieje, aktualizujemy jego dane
                _playerData[steamId] = (playerInfo.JoinTime, nickName, totalTimeSpent + playerInfo.TotalTimeSpent);
            }
        }

        public async Task SaveAllPlayerTimesAsync()
        {
            Log.Info("Saving all player times asynchronously.");
            var tasks = _playerData.Select(kvp => SavePlayerTimeAsync(kvp.Key, kvp.Value.NickName, kvp.Value.TotalTimeSpent));
            await Task.WhenAll(tasks);
        }

        public async Task SavePlayerTimeAsync(ulong steamId, string nickName, TimeSpan totalTimeSpent)
        {
            try
            {
                var doc = File.Exists(_dataFilePath) ? XDocument.Load(_dataFilePath) : new XDocument(new XElement("Players"));
                var existingPlayer = doc.Root.Elements("Player").FirstOrDefault(x => x.Attribute("SteamID").Value == steamId.ToString());
                var totalMinutes = (long)totalTimeSpent.TotalMinutes;

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

                await Task.Run(() => doc.Save(_dataFilePath));
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"Saved player time data for {nickName} (SteamID: {steamId}).");
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
                        var steamId = ulong.Parse(playerElement.Attribute("SteamID").Value);
                        var nickName = playerElement.Element("NickName").Value;
                        var minutes = double.Parse(playerElement.Element("TotalTimeSpent").Value);
                        _playerData[steamId] = (DateTime.MinValue, nickName, TimeSpan.FromMinutes(minutes));
                    }
                    LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, "Loaded player times from XML file.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while loading player times from XML file:");
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
                Task.Run(() => SavePlayerTimeAsync(steamId, playerInfo.NickName, newTotalTime));
            }
            else
            {
                Log.Warn($"Player with SteamID {steamId} not found in time tracker.");
            }
        }

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
