using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly ConcurrentDictionary<ulong, (Stopwatch SessionTimer, string NickName, TimeSpan TotalTimeSpent)> _playerData = new ConcurrentDictionary<ulong, (Stopwatch, string, TimeSpan)>();
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

            var stopwatch = new Stopwatch();
            var isNewPlayer = !_playerData.ContainsKey(player.SteamId);
            _playerData.AddOrUpdate(player.SteamId,
                (stopwatch, player.Name, TimeSpan.Zero),
                (key, existingValue) => (existingValue.SessionTimer, player.Name, existingValue.TotalTimeSpent));

            stopwatch.Start();

            if (isNewPlayer)
            {
                SavePlayerTime(player.SteamId, player.Name, TimeSpan.Zero);
                if (!VoteRewardsMain.Instance.Config.UseDatabase)
                {
                    NexusManager.SendPlayerTimeDataToAllServers(player.SteamId, player.Name, TimeSpan.Zero);
                }
            }
        }

        public void OnPlayerLeft(IPlayer player)
        {
            if (_playerData.TryGetValue(player.SteamId, out var playerInfo))
            {
                playerInfo.SessionTimer.Stop();
                var timeSpentThisSession = playerInfo.SessionTimer.Elapsed;
                var totalTimeSpent = playerInfo.TotalTimeSpent + timeSpentThisSession;

                lock (_updateLock)
                {
                    _playerData[player.SteamId] = (new Stopwatch(), playerInfo.NickName, totalTimeSpent);
                }

                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"Player {player.Name} (SteamID: {player.SteamId}) spent {timeSpentThisSession.TotalMinutes} minutes on the server this session.");
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"Total time spent by {player.Name} (SteamID: {player.SteamId}) on the server: {totalTimeSpent.TotalMinutes} minutes.");

                SavePlayerTime(player.SteamId, playerInfo.NickName, totalTimeSpent);
                if (!VoteRewardsMain.Instance.Config.UseDatabase)
                {
                    NexusManager.SendPlayerTimeDataToAllServers(player.SteamId, playerInfo.NickName, totalTimeSpent);
                }
            }
        }

        public void UpdatePlayerData(ulong steamId, string nickName, TimeSpan totalTimeSpent)
        {
            if (_playerData.TryGetValue(steamId, out var playerInfo))
            {
                lock (_updateLock)
                {
                    _playerData[steamId] = (playerInfo.SessionTimer, nickName, totalTimeSpent);
                }
            }
        }

        public void SavePlayerTime(ulong steamId, string nickName, TimeSpan totalTimeSpent)
        {
            if (VoteRewardsMain.Instance.Config.UseDatabase)
            {
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, "SavePlayerTime(): Calling database communication.");
                VoteRewardsMain.Instance.DatabaseManager.SavePlayerTime((long)steamId, nickName, totalTimeSpent);
            }
            else
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

                    // Bezpośredni zapis bez Task.Run
                    storage.SavePlayerData(doc);
                    LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"Saved player time data for {nickName} (SteamID: {steamId}).");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error occurred while saving player time data for {nickName} (SteamID: {steamId}):");
                }
            }
        }

        private void LoadPlayerTimes()
        {
            if (VoteRewardsMain.Instance.Config.UseDatabase)
            {
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, "LoadPlayerTimes(): Calling database communication.");
                // Uwaga: Potrzebna jest synchroniczna wersja tej metody
                var playerTimes = VoteRewardsMain.Instance.DatabaseManager.GetAllPlayerTimes();
                foreach (var (steamId, nickName, totalTimeSpent) in playerTimes)
                {
                    _playerData[(ulong)steamId] = (new Stopwatch(), nickName, TimeSpan.FromMinutes(totalTimeSpent));
                }
            }
            else
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
                        _playerData[steamId] = (new Stopwatch(), nickName, TimeSpan.FromMinutes(minutes));
                    }
                    LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, "Loaded player times from XML file.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error occurred while loading player times from XML file:");
                }
            }
        }

        public TimeSpan GetTotalTimeSpent(ulong steamId)
        {
            if (_playerData.TryGetValue(steamId, out var playerInfo))
            {
                return playerInfo.TotalTimeSpent + (playerInfo.SessionTimer.IsRunning ? playerInfo.SessionTimer.Elapsed : TimeSpan.Zero);
            }
            return TimeSpan.Zero;
        }

        public List<(string NickName, TimeSpan TotalTimeSpent)> GetTopPlayers(int count)
        {
            return _playerData.Values
                .Select(x => (x.NickName, TotalTimeSpent: x.TotalTimeSpent + (x.SessionTimer.IsRunning ? x.SessionTimer.Elapsed : TimeSpan.Zero)))
                .OrderByDescending(p => p.TotalTimeSpent)
                .Take(count)
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

                _playerData[steamId] = (playerInfo.SessionTimer, playerInfo.NickName, newTotalTime);
                Task.Run(() => SavePlayerTime(steamId, playerInfo.NickName, newTotalTime));
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
                UpdatePlayerData(steamId, nickName, totalTimeSpent);
                Task.Run(() => SavePlayerTime(steamId, nickName, totalTimeSpent));
            }
        }
    }
}
