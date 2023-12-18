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
        public static void SendRefferalCodeRewardUpdate(RefferalCodeReward config)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendRefferalCodeRewardUpdate: No servers available to send the configuration.");
                return;
            }

            foreach (var server in servers)
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    try
                    {
                        SendRefferalCodeRewardToServer(server.ServerID, config);
                        LoggerHelper.DebugLog(Log, Config, $"SendRefferalCodeRewardUpdate: Configuration sent to server with ID: {server.ServerID}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"SendRefferalCodeRewardUpdate: Failed to send configuration to server with ID: {server.ServerID}");
                    }
                }
                else
                {
                    LoggerHelper.DebugLog(Log, Config, "SendRefferalCodeRewardUpdate: Skipped sending configuration to the source server.");
                }
            }
        }

        private static void SendRefferalCodeRewardToServer(int targetServerId, RefferalCodeReward config)
        {
            byte[] configData = MyAPIGateway.Utilities.SerializeToBinary(config);
            NexusMessage message = new NexusMessage(ThisServer.ServerID, targetServerId, configData, NexusMessage.MessageType.RefferalCodeReward);
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(message);
            VoteRewardsMain.nexusAPI?.SendMessageToServer(targetServerId, data);
        }

        private static void HandleRefferalCodeRewardMessage(NexusMessage message)
        {
            if (message.ConfigData != null && message.Type == NexusMessage.MessageType.RefferalCodeReward)
            {
                if (message.fromServerID == ThisServer?.ServerID)
                {
                    LoggerHelper.DebugLog(Log, Config, "Ignored RefferalCodeReward update from this server.");
                    return;
                }

                RefferalCodeReward receivedConfig = MyAPIGateway.Utilities.SerializeFromBinary<RefferalCodeReward>(message.ConfigData);
                VoteRewardsMain.Instance?.UpdateRefferalCodeReward(receivedConfig);
            }
        }
    }
}
