using Nexus.API;
using Sandbox.ModAPI;
using System;
using VoteRewards.Config;
using VoteRewards.Utils;

namespace VoteRewards.Nexus
{
    public static partial class NexusManager
    {
        public static void SendEventCodeRewardUpdate(EventCodeReward config)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendEventCodeRewardUpdate: No servers available to send the configuration.");
                return;
            }

            foreach (var server in servers)
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    try
                    {
                        SendEventCodeRewardToServer(server.ServerID, config);
                        LoggerHelper.DebugLog(Log, Config, $"SendEventCodeRewardUpdate: Configuration sent to server with ID: {server.ServerID}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"SendEventCodeRewardUpdate: Failed to send configuration to server with ID: {server.ServerID}");
                    }
                }
                else
                {
                    LoggerHelper.DebugLog(Log, Config, "SendEventCodeRewardUpdate: Skipped sending configuration to the source server.");
                }
            }
        }

        private static void SendEventCodeRewardToServer(int targetServerId, EventCodeReward config)
        {
            byte[] configData = MyAPIGateway.Utilities.SerializeToBinary(config);
            NexusMessage message = new NexusMessage(ThisServer.ServerID, targetServerId, configData, NexusMessage.MessageType.EventCodeReward);
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(message);
            VoteRewardsMain.nexusAPI?.SendMessageToServer(targetServerId, data);
        }

        private static void HandleEventCodeRewardMessage(NexusMessage message)
        {
            if (message.ConfigData != null && message.Type == NexusMessage.MessageType.EventCodeReward)
            {
                if (message.fromServerID == ThisServer?.ServerID)
                {
                    LoggerHelper.DebugLog(Log, Config, "Ignored EventCodeReward update from this server.");
                    return;
                }

                EventCodeReward receivedConfig = MyAPIGateway.Utilities.SerializeFromBinary<EventCodeReward>(message.ConfigData);
                VoteRewardsMain.Instance?.UpdateEventCodeReward(receivedConfig);
            }
        }
    }
}
