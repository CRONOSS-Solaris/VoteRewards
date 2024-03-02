using Nexus.API;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VoteRewards.Utils;

namespace VoteRewards.Nexus
{
    public static partial class NexusManager
    {
        //Create

        // Metoda wysyłająca informacje o nowych kodach referencyjnych do wszystkich serwerów
        public static void SendRefferalCodeCreateToAllServers(ReferralCode referralCode)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendRefferalCodeCreateToAllServers: No servers available to send the referral codes.");
                return;
            }

            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(referralCode);
            foreach (var server in servers)
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    try
                    {
                        NexusMessage message = new NexusMessage(ThisServer.ServerID, server.ServerID, data, NexusMessage.MessageType.RefferalCodeCreate);
                        byte[] Data = MyAPIGateway.Utilities.SerializeToBinary(message);
                        VoteRewardsMain.nexusAPI?.SendMessageToServer(server.ServerID, Data);
                        LoggerHelper.DebugLog(Log, Config, $"Sent referral code to server with ID: {server.ServerID}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to send referral code to server with ID: {server.ServerID}");
                    }
                }
            }
        }

        // Metoda odbierająca i przetwarzająca nowe kody referencyjne
        private static void HandleRefferalCodeCreateMessage(NexusMessage message)
        {
            ReferralCode receivedCodeData = MyAPIGateway.Utilities.SerializeFromBinary<ReferralCode>(message.Data);

            var referralCodeManager = VoteRewardsMain.Instance?.ReferralCodeManager;
            if (referralCodeManager != null)
            {
                var existingReferralCode = referralCodeManager.GetReferralCode(receivedCodeData.SteamId);
                if (existingReferralCode != null)
                {
                    // Aktualizacja istniejącego kodu
                    referralCodeManager.UpdateExistingReferralCode(receivedCodeData);
                }
                else
                {
                    // Dodanie nowego wpisu
                    referralCodeManager.AddNewReferralCode(receivedCodeData);
                }
            }
            else
            {
                Log.Error("ReferralCodeManager instance is not available in NexusManager.HandleRefferalCodeCreateMessage");
            }
        }

        //redeem
        private static void HandleRedeemReferralCodeMessage(NexusMessage message)
        {
            var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<(string Code, ulong RedeemerSteamId, List<ulong> RedeemedBySteamIds, int CodeUsageCount)>(message.Data);
            var referralCodeManager = VoteRewardsMain.Instance?.ReferralCodeManager;

            if (referralCodeManager != null)
            {
                var referralCode = referralCodeManager.GetReferralCodeByCode(receivedData.Code);
                if (referralCode != null)
                {
                    referralCode.RedeemedBySteamIds = receivedData.RedeemedBySteamIds;
                    referralCode.CodeUsageCount = receivedData.CodeUsageCount;
                    referralCode.Codes.Remove(receivedData.Code);

                    referralCodeManager.SaveReferralCodes();
                }

            }
        }

        public static void SendRedeemReferralCodeToAllServers(string code, ulong redeemerSteamId, List<ulong> redeemedBySteamIds, int codeUsageCount)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendRedeemReferralCodeToAllServers: No servers available to send the redeemed code.");
                return;
            }

            var redeemInfo = new { Code = code, RedeemerSteamId = redeemerSteamId, RedeemedBySteamIds = redeemedBySteamIds, CodeUsageCount = codeUsageCount };
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(redeemInfo);

            foreach (var server in servers)
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    try
                    {
                        NexusMessage message = new NexusMessage(ThisServer.ServerID, server.ServerID, data, NexusMessage.MessageType.RedeemReferralCode);
                        byte[] Data = MyAPIGateway.Utilities.SerializeToBinary(message);
                        VoteRewardsMain.nexusAPI?.SendMessageToServer(server.ServerID, Data);
                        LoggerHelper.DebugLog(Log, Config, $"Sent redeemed referral code to server with ID: {server.ServerID}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to send redeemed referral code to server with ID: {server.ServerID}");
                    }
                }
            }
        }

        //award

        private static void HandleAwardReferralCodeMessage(NexusMessage message)
        {
            ReferralCode awardInfo = MyAPIGateway.Utilities.SerializeFromBinary<ReferralCode>(message.Data);
            if (awardInfo != null)
            {
                var referralCodeManager = VoteRewardsMain.Instance?.ReferralCodeManager;
                if (referralCodeManager != null)
                {
                    var referralCode = referralCodeManager.GetReferralCode(awardInfo.SteamId);
                    if (referralCode != null)
                    {
                        referralCode.CodeUsageCount -= awardInfo.CodeUsageCount;
                        referralCodeManager.SaveReferralCodes();
                    }
                }
            }
        }

        public static void SendAwardReferralCodeToAllServers(ulong steamId, int awardsClaimed)
        {
            var servers = NexusAPI.GetAllServers();
            if (servers.Count == 0)
            {
                Log.Warn("SendAwardReferralCodeToAllServers: No servers available to send the award information.");
                return;
            }

            ReferralCode awardInfo = new ReferralCode
            {
                SteamId = steamId,
                CodeUsageCount = awardsClaimed
            };

            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(awardInfo);
            foreach (var server in servers)
            {
                if (server.ServerID != ThisServer?.ServerID)
                {
                    try
                    {
                        NexusMessage message = new NexusMessage(ThisServer.ServerID, server.ServerID, data, NexusMessage.MessageType.AwardReferralCode);
                        byte[] Data = MyAPIGateway.Utilities.SerializeToBinary(message);
                        VoteRewardsMain.nexusAPI?.SendMessageToServer(server.ServerID, Data);
                        LoggerHelper.DebugLog(Log, Config, $"Sent award referral code info to server with ID: {server.ServerID}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to send award referral code info to server with ID: {server.ServerID}");
                    }
                }
            }
        }

    }
}
