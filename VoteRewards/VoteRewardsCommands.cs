﻿using NLog;
using NLog.Fluent;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using Torch;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Managers;
using VoteRewards.Utils;
using VRage.Game.ModAPI;
using VRageMath;
using VoteRewards.Utils;


namespace VoteRewards
{
    public class VoteRewardsCommands : CommandModule
    {

        public VoteRewards Plugin => (VoteRewards)Context.Plugin;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private VoteRewards _config;

        [Command("vote", "Directs the player to the voting page.")]
        [Permission(MyPromoteLevel.None)]
        public void VoteCommand()
        {
            string voteUrl = Plugin.Config.VotingLink;

            // Pobranie Steam ID gracza, który wydał komendę
            ulong steamId = Context.Player.SteamUserId;

            // Przygotuj URL z linkfilter Steam do przekierowania na stronę do głosowania
            string steamOverlayUrl = $"https://steamcommunity.com/linkfilter/?url={voteUrl}";

            // Otwarcie Steam Overlay z URL do głosowania
            MyVisualScriptLogicProvider.OpenSteamOverlay(steamOverlayUrl, Context.Player.Identity.IdentityId);

            VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", $"Please vote for us at: {voteUrl}", Color.Green, Context.Player.SteamUserId);
        }

        [Command("reward", "Allows the player to claim their vote reward.")]
        [Permission(MyPromoteLevel.None)]
        public async void ClaimRewardCommand()
        {
            var steamId = Context.Player.SteamUserId;
            var identityId = Context.Player.IdentityId;

            int voteStatus;
            try
            {
                voteStatus = await Plugin.ApiHelper.CheckVoteStatusAsync(steamId.ToString());

            }
            catch (Exception ex)
            {
                VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", "Failed to check your vote status. Please try again later.", Color.Green, Context.Player.SteamUserId);
                LoggerHelper.DebugLog(Log, Plugin.Config, "Failed to check the vote status: " + ex.Message);
                return;
            }

            List<string> messages = new List<string> { $"{Plugin.Config.NotificationPrefix}" }; // Dodaj prefix jako pierwszą wiadomość

            switch (voteStatus)
            {
                case -1:
                    messages.Add("Failed to check your vote status. Please try again later.");
                    break;
                case 0:
                    messages.Add("You have not voted yet. Please vote first.");
                    break;
                case 1:
                    // Zamiast wywoływać GetRandomReward trzy razy, wywołujemy GetRandomRewards raz, aby otrzymać wszystkie nagrody
                    List<RewardItem> rewards = Plugin.GetRandomRewards();

                    // Usuń null z listy nagród (jeśli jakiś przedmiot nie został wylosowany)
                    rewards.RemoveAll(item => item == null);

                    if (rewards.Any())
                    {
                        try
                        {
                            // Mark the vote as claimed
                            await Plugin.ApiHelper.SetVoteAsClaimedAsync(steamId);

                            // Zbierz wszystkie udane nagrody w jednym miejscu
                            var successfulRewards = new List<string>();

                            foreach (var reward in rewards)
                            {
                                // Spróbuj przyznać nagrodę graczowi
                                bool rewardGranted = Plugin.AwardPlayer(Context.Player.SteamUserId, reward);
                                if (rewardGranted)
                                {
                                    successfulRewards.Add($"{reward.Amount}x {reward.ItemSubtypeId}");
                                    LoggerHelper.DebugLog(Log, Plugin.Config, $"Player {steamId} received {reward.Amount} of {reward.ItemSubtypeId}.");
                                }
                                else
                                {
                                    LoggerHelper.DebugLog(Log, Plugin.Config, $"Player {steamId}'s inventory is full. Could not grant reward.");
                                }
                            }

                            // Sprawdź, czy jakiekolwiek nagrody zostały przyznane
                            if (successfulRewards.Any())
                            {
                                messages.Add("You received:");
                                messages.AddRange(successfulRewards.Select(reward => $"{reward}"));
                                messages.Add("Thank you for voting!");
                            }
                            else
                            {
                                messages.Add("Your inventory is full. Please make space to receive your reward.");
                            }
                        }
                        catch (Exception ex)
                        {
                            messages.Add("Failed to claim the reward. Please try again later.");
                            LoggerHelper.DebugLog(Log, Plugin.Config, "Failed to claim the reward: " + ex.Message);
                        }
                    }
                    else
                    {
                        messages.Add("No reward available at the moment. Please try again later.");
                        LoggerHelper.DebugLog(Log, Plugin.Config, "No reward item found for player " + steamId);
                    }
                    break;
                case 2:
                    messages.Add("You have already claimed your vote reward.");
                    break;
            }

            // Połącz wszystkie wiadomości w jedną i wyślij
            VoteRewards.ChatManager.SendMessageAsOther(messages.First(), string.Join("\n", messages.Skip(1)), Color.Green, Context.Player.SteamUserId);
        }

        [Command("testgetrandomreward", "Test the GetRandomRewards method.")]
        [Permission(MyPromoteLevel.Admin)]  // Ustaw na Admin, aby tylko admini mogli używać tej komendy
        public void TestGetRandomReward()
        {
            List<RewardItem> rewards = Plugin.GetRandomRewards();

            if (rewards.Any())
            {
                var rewardDescriptions = rewards.Select(reward => $"{reward.Amount} of {reward.ItemSubtypeId}").ToList();
                VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", $"Generated random rewards: {string.Join(", ", rewardDescriptions)}", Color.Green, Context.Player.SteamUserId);
            }
            else
            {
                VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", "No reward generated.", Color.Green, Context.Player.SteamUserId);
            }
        }

        [Command("testawardplayer", "Test the AwardPlayer method. Usage: /testawardplayer <ItemTypeId> <ItemSubtypeId> <Amount>")]
        [Permission(MyPromoteLevel.Admin)]  // Ustaw na Admin, aby tylko admini mogli używać tej komendy
        public void TestAwardPlayer(string itemTypeId, string itemSubtypeId, int amount)
        {
            RewardItem reward = new RewardItem
            {
                ItemTypeId = itemTypeId,
                ItemSubtypeId = itemSubtypeId,
                Amount = amount
            };

            bool rewardGranted = Plugin.AwardPlayer(Context.Player.SteamUserId, reward);
            if (rewardGranted)
            {
                VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", $"Successfully awarded {amount} of {itemSubtypeId}", Color.Green, Context.Player.SteamUserId);
            }
            else
            {
                VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", $"Failed to award {amount} of {itemSubtypeId}. It is possible that the inventory is full.", Color.Green, Context.Player.SteamUserId);
            }
        }

    }
}