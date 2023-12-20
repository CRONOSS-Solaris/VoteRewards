using Nexus.API;
using Sandbox.Engine.Utils;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoteRewards.Utils;
#nullable enable 

namespace VoteRewards.Nexus
{
    public static partial class NexusManager
    {
        private static void HandleConfigurationMessage(NexusMessage message)
        {
            if (message.ConfigData != null)
            {
                VoteRewardsConfig receivedConfig = MyAPIGateway.Utilities.SerializeFromBinary<VoteRewardsConfig>(message.ConfigData);
                UpdateConfiguration(receivedConfig);
            }
        }

        public static void SendConfigToAllServers(VoteRewardsConfig config)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendConfigToAllServers: No servers available to send the configuration.");
                return;
            }

            foreach (var server in servers)
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    try
                    {
                        SendConfigToServer(server.ServerID, config);
                        LoggerHelper.DebugLog(Log, Config, $"SendConfigToAllServers: Configuration sent to server with ID: {server.ServerID}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"SendConfigToAllServers: Failed to send configuration to server with ID: {server.ServerID}");
                    }
                }
                else
                {
                    LoggerHelper.DebugLog(Log, Config, "SendConfigToAllServers: Skipped sending configuration to the source server.");
                }
            }
        }

        public static void SendConfigToServer(int targetServerId, VoteRewardsConfig config)
        {
            byte[] configData = MyAPIGateway.Utilities.SerializeToBinary(config);
            NexusMessage message = new NexusMessage(ThisServer.ServerID, targetServerId, configData, NexusMessage.MessageType.BaseConfig);
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(message);
            VoteRewardsMain.nexusAPI?.SendMessageToServer(targetServerId, data);
        }

        private static void UpdateConfiguration(VoteRewardsConfig newConfig)
        {
            VoteRewardsMain.Instance?.UpdateConfig(newConfig, false);
        }
    }
}
