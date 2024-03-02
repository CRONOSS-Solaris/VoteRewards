//using Nexus.API;
//using Sandbox.ModAPI;
//using System;
//using VoteRewards.Utils;

//namespace VoteRewards.Nexus
//{
//    public static partial class NexusManager
//    {
//        public static void SendPlayerTimeDataToAllServers(ulong steamId, string nickName, TimeSpan totalTimeSpent)
//        {
//            var servers = NexusAPI.GetAllServers();
//            if (servers.Count == 0)
//            {
//                Log.Warn("SendPlayerTimeDataToAllServers: No servers available to send the player time data.");
//                return;
//            }

//            var playerTimeData = new { SteamId = steamId, NickName = nickName, TotalTimeSpent = totalTimeSpent.TotalMinutes };
//            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(playerTimeData);

//            foreach (var server in servers)
//            {
//                if (server.ServerID != ThisServer?.ServerID)
//                {
//                    try
//                    {
//                        NexusMessage message = new NexusMessage(ThisServer.ServerID, server.ServerID, data, NexusMessage.MessageType.PlayerTimeTracker);
//                        byte[] messageData = MyAPIGateway.Utilities.SerializeToBinary(message);
//                        VoteRewardsMain.nexusAPI?.SendMessageToServer(server.ServerID, messageData);
//                        LoggerHelper.DebugLog(Log, Config, $"Sent player time data to server with ID: {server.ServerID}");
//                    }
//                    catch (Exception ex)
//                    {
//                        Log.Error(ex, $"Failed to send player time data to server with ID: {server.ServerID}");
//                    }
//                }
//            }
//        }

//        private static void HandlePlayerTimeTrackerMessage(NexusMessage message)
//        {
//            var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<(ulong SteamId, string NickName, double TotalTimeSpent)>(message.Data);
//            var playerTimeTracker = VoteRewardsMain.Instance?.PlayerTimeTracker;

//            if (playerTimeTracker != null)
//            {
//                var totalTimeSpent = TimeSpan.FromMinutes(receivedData.TotalTimeSpent);
//                playerTimeTracker.UpdatePlayerData(receivedData.SteamId, receivedData.NickName, totalTimeSpent, isNewPlayer: false);

//                // Zapisz czas gracza
//                playerTimeTracker.SavePlayerTime(receivedData.SteamId, receivedData.NickName, totalTimeSpent);
//            }
//            else
//            {
//                Log.Error("PlayerTimeTracker instance is not available in HandlePlayerTimeTrackerMessage");
//            }
//        }

//    }
//}
