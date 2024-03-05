using Nexus.API;
using ProtoBuf;
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

            // Upewnij się, że dane nie są null przed wysłaniem
            if (data == null)
            {
                Log.Error("SendPlayerRewardTrackerToAllServers: Data to send is null.");
                return;
            }

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
                            // Upewnij się, że messageData nie jest null
                            if (messageData == null)
                            {
                                Log.Error($"Failed to serialize message data for server with ID: {server.ServerID}");
                                return;
                            }
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
                // Sprawdź, czy message.Data nie jest null
                if (message.Data == null)
                {
                    Log.Error("HandlePlayerRewardTrackerMessage: Received data is null.");
                    return;
                }

                var playerRewardTrackerData = MyAPIGateway.Utilities.SerializeFromBinary<PlayerRewardTrackerData>(message.Data);
                // Dodatkowe sprawdzenie czy deserializacja się powiodła
                if (playerRewardTrackerData == null)
                {
                    Log.Error("HandlePlayerRewardTrackerMessage: Failed to deserialize player reward tracker data.");
                    return;
                }

                PlayerRewardTracker.HandleReceivedPlayerRewardTrackerData(playerRewardTrackerData.SteamId, playerRewardTrackerData.ClaimDate);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to handle player reward tracker message.");
            }
        }
    }

    [ProtoContract]
    public class PlayerRewardTrackerData
    {
        [ProtoMember(1)]
        public ulong SteamId { get; set; }

        [ProtoMember(2)]
        public DateTime ClaimDate { get; set; }

        public PlayerRewardTrackerData() { }
    }
}
