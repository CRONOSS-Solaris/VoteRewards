using Nexus.API;
using Sandbox.ModAPI;
using System;
using VoteRewards.Utils;

namespace VoteRewards.Nexus
{
    public static partial class NexusManager
    {
        //create
        public static void SendEventCodeCreateToAllServers(EventCode eventCode)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendEventCodeCreateToAllServers: No servers available to send the event codes.");
                return;
            }

            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(eventCode);
            foreach (var server in servers)
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    try
                    {
                        NexusMessage message = new NexusMessage(ThisServer.ServerID, server.ServerID, data, NexusMessage.MessageType.EventCodeCreate);
                        byte[] Data = MyAPIGateway.Utilities.SerializeToBinary(message);
                        VoteRewardsMain.nexusAPI?.SendMessageToServer(server.ServerID, Data);
                        LoggerHelper.DebugLog(Log, Config, $"Sent event code to server with ID: {server.ServerID}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to send event code to server with ID: {server.ServerID}");
                    }
                }
            }
        }

        private static void HandleEventCodeCreateMessage(NexusMessage message)
        {
            EventCode receivedCodeData = MyAPIGateway.Utilities.SerializeFromBinary<EventCode>(message.Data);

            var eventCodeManager = VoteRewardsMain.Instance?.EventCodeManager;
            if (eventCodeManager != null)
            {
                eventCodeManager.AddEventCode(receivedCodeData);
            }
            else
            {
                Log.Error("EventCodeManager instance is not available in NexusManager.HandleEventCodeCreateMessage");
            }
        }

        public static void SendRedeemEventCodeToAllServers(string code, ulong redeemerSteamId)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendRedeemEventCodeToAllServers: No servers available to send the redeemed code.");
                return;
            }

            var redeemInfo = new { Code = code, RedeemerSteamId = redeemerSteamId };
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(redeemInfo);

            foreach (var server in servers)
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    try
                    {
                        NexusMessage message = new NexusMessage(ThisServer.ServerID, server.ServerID, data, NexusMessage.MessageType.RedeemEventCode);
                        byte[] Data = MyAPIGateway.Utilities.SerializeToBinary(message);
                        VoteRewardsMain.nexusAPI?.SendMessageToServer(server.ServerID, Data);
                        LoggerHelper.DebugLog(Log, Config, $"Sent redeemed event code to server with ID: {server.ServerID}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to send redeemed event code to server with ID: {server.ServerID}");
                    }
                }
            }
        }


        private static void HandleRedeemEventCodeMessage(NexusMessage message)
        {
            var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<(string Code, ulong RedeemerSteamId)>(message.Data);
            var eventCodeManager = VoteRewardsMain.Instance?.EventCodeManager;

            if (eventCodeManager != null)
            {
                // Obsługa logiki wykorzystania kodu eventowego
                if (eventCodeManager.RedeemCode(receivedData.Code, receivedData.RedeemerSteamId))
                {
                }
            }
        }

    }
}
