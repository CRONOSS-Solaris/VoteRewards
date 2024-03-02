using Nexus.API;
using Sandbox.ModAPI;
using System;
using VoteRewards.Config;
using VoteRewards.Utils;

namespace VoteRewards.Nexus
{
    public static partial class NexusManager
    {
        public static void SendRewardItemsConfigUpdate(RewardItemsConfig config)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendRewardItemsConfigUpdate: No servers available to send the configuration.");
                return;
            }

            foreach (var server in servers)
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    try
                    {
                        SendRewardItemsConfigToServer(server.ServerID, config);
                        LoggerHelper.DebugLog(Log, Config, $"SendRewardItemsConfigUpdate: Configuration sent to server with ID: {server.ServerID}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"SendRewardItemsConfigUpdate: Failed to send configuration to server with ID: {server.ServerID}");
                    }
                }
                else
                {
                    LoggerHelper.DebugLog(Log, Config, "SendRewardItemsConfigUpdate: Skipped sending configuration to the source server.");
                }
            }
        }

        private static void SendRewardItemsConfigToServer(int targetServerId, RewardItemsConfig config)
        {
            byte[] configData = MyAPIGateway.Utilities.SerializeToBinary(config);
            NexusMessage message = new NexusMessage(ThisServer.ServerID, targetServerId, configData, NexusMessage.MessageType.RewardItemsConfig);
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(message);
            VoteRewardsMain.nexusAPI?.SendMessageToServer(targetServerId, data);
        }

        private static void HandleRewardItemsConfigMessage(NexusMessage message)
        {
            if (message.ConfigData != null && message.Type == NexusMessage.MessageType.RewardItemsConfig)
            {
                if (message.fromServerID == ThisServer?.ServerID)
                {
                    LoggerHelper.DebugLog(Log, Config, "Ignored RewardItemsConfig update from this server.");
                    return;
                }

                RewardItemsConfig receivedConfig = MyAPIGateway.Utilities.SerializeFromBinary<RewardItemsConfig>(message.ConfigData);
                VoteRewardsMain.Instance?.UpdateRewardItemsConfig(receivedConfig);
            }
        }
    }
}
