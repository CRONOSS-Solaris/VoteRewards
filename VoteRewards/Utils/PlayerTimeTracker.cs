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
        private readonly object _updateLock = new object();
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
                var timeSpentThisSession = DateTime.UtcNow - playerInfo.JoinTime;
                var totalTimeSpent = playerInfo.TotalTimeSpent + timeSpentThisSession;

                lock (_updateLock)
                {
                    UpdatePlayerData(player.SteamId, player.Name, totalTimeSpent);
                }

                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"Player {player.Name} (SteamID: {player.SteamId}) spent {timeSpentThisSession.TotalMinutes} minutes on the server this session.");
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"Total time spent by {player.Name} (SteamID: {player.SteamId}) on the server: {totalTimeSpent.TotalMinutes} minutes.");

                Task.Run(() => SavePlayerTimeAsync(player.SteamId, player.Name, totalTimeSpent));
                Task.Run(() => NexusManager.SendPlayerTimeDataToAllServers(player.SteamId, player.Name, totalTimeSpent));
            }
        }

        public void UpdatePlayerData(ulong steamId, string nickName, TimeSpan totalTimeSpent)
        {
            // Bezpośrednia aktualizacja, zakładając że totalTimeSpent jest już skumulowanym czasem
            _playerData.AddOrUpdate(steamId,
                // Jeśli klucz nie istnieje, dodaj nowy wpis
                (DateTime.UtcNow, nickName, totalTimeSpent),
                // Jeśli klucz istnieje, zaktualizuj wpis
                (key, existingValue) => {
                    var updatedTimeSpent = totalTimeSpent > existingValue.TotalTimeSpent ? totalTimeSpent : existingValue.TotalTimeSpent;
                    return (DateTime.UtcNow, nickName, updatedTimeSpent);
                });
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
                PlayerDataStorage storage = PlayerDataStorage.GetInstance(_dataFilePath);
                var doc = storage.LoadPlayerData();
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

                await Task.Run(() => storage.SavePlayerData(doc));
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
                PlayerDataStorage storage = PlayerDataStorage.GetInstance(_dataFilePath);
                var doc = storage.LoadPlayerData();
                foreach (var playerElement in doc.Root.Elements("Player"))
                {
                    var steamId = ulong.Parse(playerElement.Attribute("SteamID").Value);
                    var nickName = playerElement.Element("NickName").Value;
                    var minutes = double.Parse(playerElement.Element("TotalTimeSpent").Value);
                    _playerData[steamId] = (DateTime.MinValue, nickName, TimeSpan.FromMinutes(minutes));
                }
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, "Loaded player times from XML file.");
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

        public void ProcessAndSaveReceivedPlayerTimeData(ulong steamId, string nickName, TimeSpan totalTimeSpent)
        {
            lock (_updateLock)
            {
                // Aktualizuj dane gracza lub dodaj nowego gracza
                UpdatePlayerData(steamId, nickName, totalTimeSpent);

                // Asynchronicznie zapisz aktualizacje
                Task.Run(() => SavePlayerTimeAsync(steamId, nickName, totalTimeSpent));
            }
        }

    }
}
