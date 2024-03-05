using Nexus.API;
using Sandbox.ModAPI;
using System;
using System.Linq;
using System.Threading.Tasks;
using VoteRewards.Utils;

namespace VoteRewards.Nexus
{
    public static partial class NexusManager
    {
        public static async Task SendPlayerTimeDataToAllServers(ulong steamId, string nickName, TimeSpan totalTimeSpent)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendPlayerTimeDataToAllServers: No servers available to send the player time data.");
                return;
            }

            var playerTimeData = new { SteamId = steamId, NickName = nickName, TotalTimeSpent = totalTimeSpent.TotalMinutes };
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(playerTimeData);

            var tasks = servers.Select(server =>
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    return Task.Run(() =>
                    {
                        try
                        {
                            NexusMessage message = new NexusMessage(ThisServer.ServerID, server.ServerID, data, NexusMessage.MessageType.PlayerTimeTracker);
                            byte[] messageData = MyAPIGateway.Utilities.SerializeToBinary(message);
                            VoteRewardsMain.nexusAPI?.SendMessageToServer(server.ServerID, messageData);
                            LoggerHelper.DebugLog(Log, Config, $"Sent player time data to server with ID: {server.ServerID}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Failed to send player time data to server with ID: {server.ServerID}");
                        }
                    });
                }
                else
                {
                    return Task.Run(() => LoggerHelper.DebugLog(Log, Config, "Skipped sending data to the source server."));
                }
            }).ToList();

            await Task.WhenAll(tasks);
        }

        private static void HandlePlayerTimeTrackerMessage(NexusMessage message)
        {
            var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<(ulong SteamId, string NickName, double TotalTimeSpent)>(message.Data);
            var playerTimeTracker = VoteRewardsMain.Instance?.PlayerTimeTracker;

            if (playerTimeTracker != null)
            {
                // Bezpośrednie przekazanie otrzymanych danych do przetworzenia i zapisania
                var receivedTime = TimeSpan.FromMinutes(receivedData.TotalTimeSpent);
                playerTimeTracker.ProcessAndSaveReceivedPlayerTimeData(receivedData.SteamId, receivedData.NickName, receivedTime);

                LoggerHelper.DebugLog(Log, Config, $"Processed and saved received player time data for {receivedData.NickName} (SteamID: {receivedData.SteamId}).");
            }
            else
            {
                Log.Error("PlayerTimeTracker instance is not available in HandlePlayerTimeTrackerMessage");
            }
        }
    }
}
