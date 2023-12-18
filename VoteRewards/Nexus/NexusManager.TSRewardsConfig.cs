using Nexus.API;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoteRewards.Config;
using VoteRewards.Utils;

namespace VoteRewards.Nexus
{
    public static partial class NexusManager
    {
        public static void SendTimeSpentRewardsConfigUpdate(TimeSpentRewardsConfig config)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendTimeSpentRewardsConfigUpdate: No servers available to send the configuration.");
                return;
            }

            foreach (var server in servers)
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    try
                    {
                        SendTimeSpentRewardsConfigToServer(server.ServerID, config);
                        LoggerHelper.DebugLog(Log, Config, $"SendTimeSpentRewardsConfigUpdate: Configuration sent to server with ID: {server.ServerID}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"SendTimeSpentRewardsConfigUpdate: Failed to send configuration to server with ID: {server.ServerID}");
                    }
                }
                else
                {
                    LoggerHelper.DebugLog(Log, Config, "SendTimeSpentRewardsConfigUpdate: Skipped sending configuration to the source server.");
                }
            }
        }

        private static void SendTimeSpentRewardsConfigToServer(int targetServerId, TimeSpentRewardsConfig config)
        {
            byte[] configData = MyAPIGateway.Utilities.SerializeToBinary(config);
            NexusMessage message = new NexusMessage(ThisServer.ServerID, targetServerId, configData, NexusMessage.MessageType.TimeSpentRewardsConfig);
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(message);
            VoteRewardsMain.nexusAPI?.SendMessageToServer(targetServerId, data);
        }

        private static void HandleTimeSpentRewardsConfigMessage(NexusMessage message)
        {
            if (message.ConfigData != null && message.Type == NexusMessage.MessageType.TimeSpentRewardsConfig)
            {
                if (message.fromServerID == ThisServer?.ServerID)
                {
                    LoggerHelper.DebugLog(Log, Config, "Ignored TimeSpentRewardsConfig update from this server.");
                    return;
                }

                TimeSpentRewardsConfig receivedConfig = MyAPIGateway.Utilities.SerializeFromBinary<TimeSpentRewardsConfig>(message.ConfigData);
                VoteRewardsMain.Instance?.UpdateTimeSpentRewardsConfig(receivedConfig);
            }
        }
    }
}
