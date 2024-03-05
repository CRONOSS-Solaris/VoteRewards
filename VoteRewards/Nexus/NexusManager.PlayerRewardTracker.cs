using Nexus.API;
using Sandbox.ModAPI;
using System;
using System.Threading.Tasks;
using VoteRewards.Utils;

namespace VoteRewards.Nexus
{
    public static partial class NexusManager
    {
        public static void SendPlayerRewardTrackerToAllServers(ulong steamId, DateTime claimDate)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendPlayerRewardTrackerToAllServers: No servers available to send the player time data.");
                return;
            }

            var playerRewardTrackerData = new { SteamId = steamId, ClaimDate = claimDate };
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(playerRewardTrackerData);

            foreach (var server in servers)
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            NexusMessage message = new NexusMessage(ThisServer.ServerID, server.ServerID, data, NexusMessage.MessageType.PlayerRewardTracker);
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
                    LoggerHelper.DebugLog(Log, Config, "SendPlayerRewardTrackerToAllServers: Skipped sending data to the source server.");
                }
            }
        }

        private static void HandlePlayerRewardTrackerMessage(NexusMessage message)
        {
            try
            {
                var playerRewardTrackerData = MyAPIGateway.Utilities.SerializeFromBinary<PlayerRewardTrackerData>(message.Data);
                PlayerRewardTracker.HandleReceivedPlayerRewardTrackerData(playerRewardTrackerData.SteamId, playerRewardTrackerData.ClaimDate);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to handle player reward tracker message.");
            }
        }
    }

    public class PlayerRewardTrackerData
    {
        public ulong SteamId { get; set; }
        public DateTime ClaimDate { get; set; }
    }
}
