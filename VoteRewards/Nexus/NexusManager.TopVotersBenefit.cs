using Nexus.API;
using Sandbox.ModAPI;
using System;
using VoteRewards.Config;
using VoteRewards.Utils;

namespace VoteRewards.Nexus
{
	public static partial class NexusManager
	{
		public static void SendTopVotersBenefitUpdate(TopVotersBenefitConfig config)
		{
			var servers = NexusAPI.GetAllServers();
			if (servers.Count == 0)
			{
				Log.Warn("SendTopVotersBenefitUpdate: No servers available to send the configuration.");
				return;
			}

			foreach (var server in servers)
			{
				if (server.ServerID != ThisServer?.ServerID)
				{
					try
					{
						SendTopVotersBenefitConfigToServer(server.ServerID, config);
						LoggerHelper.DebugLog(Log, Config, $"SendTopVotersBenefitUpdate: Configuration sent to server with ID: {server.ServerID}");
					}
					catch (Exception ex)
					{
						Log.Error(ex, $"SendTopVotersBenefitUpdate: Failed to send configuration to server with ID: {server.ServerID}");
					}
				}
				else
				{
					LoggerHelper.DebugLog(Log, Config, "SendTopVotersBenefitUpdate: Skipped sending configuration to the source server.");
				}
			}
		}

		private static void SendTopVotersBenefitConfigToServer(int targetServerId, TopVotersBenefitConfig config)
		{
			byte[] configData = MyAPIGateway.Utilities.SerializeToBinary(config);
			NexusMessage message = new NexusMessage(ThisServer.ServerID, targetServerId, configData, NexusMessage.MessageType.TopVotersBenefitConfig);
			byte[] data = MyAPIGateway.Utilities.SerializeToBinary(message);
			VoteRewardsMain.nexusAPI?.SendMessageToServer(targetServerId, data);
		}

		private static void HandleTopVotersBenefitConfigMessage(NexusMessage message)
		{
			if (message.ConfigData != null && message.Type == NexusMessage.MessageType.TopVotersBenefitConfig)
			{
				if (message.fromServerID == ThisServer?.ServerID)
				{
					LoggerHelper.DebugLog(Log, Config, "Ignored TopVotersBenefitConfig update from this server.");
					return;
				}

				TopVotersBenefitConfig receivedConfig = MyAPIGateway.Utilities.SerializeFromBinary<TopVotersBenefitConfig>(message.ConfigData);
				VoteRewardsMain.Instance?.UpdateTopVotersBenefit(receivedConfig);
			}
		}
	}
}
