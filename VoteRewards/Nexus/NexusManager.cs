using Nexus.API;
using NLog;
using Sandbox.ModAPI;
using System;
using VoteRewards.Utils;
#nullable enable 

namespace VoteRewards.Nexus
{
    public static partial class NexusManager
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static VoteRewardsConfig? Config => VoteRewardsMain.Instance?.Config;
        private static NexusAPI.Server? LobbyServer;
        private static NexusAPI.Server? ThisServer;

        public static void SetServerData(NexusAPI.Server server)
        {
            ThisServer = server;
        }

        internal static void HandleNexusMessage(ushort handlerId, byte[] data, ulong steamID, bool fromServer)
        {
            try
            {
                LoggerHelper.DebugLog(Log, Config, $"Received raw data. Length: {data.Length} bytes.");
                string dataAsBase64 = Convert.ToBase64String(data);
                LoggerHelper.DebugLog(Log, Config, $"Received data (Base64): {dataAsBase64}");

                NexusMessage? message = MyAPIGateway.Utilities.SerializeFromBinary<NexusMessage>(data);

                if (message == null)
                {
                    LoggerHelper.DebugLog(Log, Config, "Received a null message or deserialization failed.");
                    return;
                }

                switch (message.Type)
                {
                    case NexusMessage.MessageType.BaseConfig:
                        HandleConfigurationMessage(message);
                        break;
                    case NexusMessage.MessageType.TimeSpentRewardsConfig:
                        HandleTimeSpentRewardsConfigMessage(message); 
                        break;
                    case NexusMessage.MessageType.RewardItemsConfig:
                        HandleRewardItemsConfigMessage(message);
                        break;
                    case NexusMessage.MessageType.RefferalCodeReward:
                        HandleRefferalCodeRewardMessage(message);
                        break;
                    case NexusMessage.MessageType.EventCodeReward:
                        HandleEventCodeRewardMessage(message);
                        break;
                    case NexusMessage.MessageType.RefferalCodeCreate:
                        HandleRefferalCodeCreateMessage(message);
                        break;
                    case NexusMessage.MessageType.RedeemReferralCode:
                        HandleRedeemReferralCodeMessage(message);
                        break;
                    case NexusMessage.MessageType.AwardReferralCode:
                        HandleAwardReferralCodeMessage(message);
                        break;
                    case NexusMessage.MessageType.EventCodeCreate:
                        HandleEventCodeCreateMessage(message);
                        break;
                    case NexusMessage.MessageType.RedeemEventCode:
                        HandleRedeemEventCodeMessage(message);
                        break;
                    case NexusMessage.MessageType.PlayerTimeTracker:
                        HandlePlayerTimeTrackerMessage(message);
                        break;

                    default:
                        Log.Warn($"Received an unknown type of Nexus message. Type: {message.Type}");
                        break;
                }

                if (Config!.isLobby && message.requestLobbyServer)
                {
                    GetLobbyServer();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred during the receiving or deserialization of data.");
            }
        }

        private static void GetLobbyServer()
        {
            if (ThisServer is null) return;
            NexusMessage message = new(ThisServer.ServerID, ThisServer.ServerID, false, null, true, false);
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(message);
            VoteRewardsMain.nexusAPI?.SendMessageToServer(ThisServer.ServerID, data);
        }
    }
}
