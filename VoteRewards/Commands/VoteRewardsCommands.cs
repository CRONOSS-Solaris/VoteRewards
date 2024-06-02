using Newtonsoft.Json;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Torch.Commands;
using Torch.Commands.Permissions;
using VoteRewards.Nexus;
using VoteRewards.Utils;
using VRage.Game.ModAPI;
using VRageMath;


namespace VoteRewards
{
    public class VoteRewardsCommands : CommandModule
    {

        public VoteRewardsMain Plugin => (VoteRewardsMain)Context.Plugin;

        [Command("votelink", "Directs the player to the voting page.")]
        [Permission(MyPromoteLevel.None)]
        public void VotelinkCommand()
        {
            string voteUrl = Plugin.Config.VotingLink;

            // Pobranie Steam ID gracza, który wydał komendę
            ulong steamId = Context.Player.SteamUserId;

            // Przygotuj URL z linkfilter Steam do przekierowania na stronę do głosowania
            string steamOverlayUrl = $"https://steamcommunity.com/linkfilter/?url={voteUrl}";

            // Otwarcie Steam Overlay z URL do głosowania
            MyVisualScriptLogicProvider.OpenSteamOverlay(steamOverlayUrl, Context.Player.Identity.IdentityId);

            VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", $"Please vote for us at: {voteUrl}", Color.Green, Context.Player.SteamUserId);
        }

        [Command("topreward", "Allows the player to claim their top voter reward.")]
        [Permission(MyPromoteLevel.None)]
        public async void TopRewardCommand()
        {
            var steamId = Context.Player.SteamUserId.ToString();
            var identityId = Context.Player.IdentityId;

            var getRandomRewardsUtils = new GetRandomRewardsUtils(Plugin.RewardItemsConfig, Plugin.TimeSpentRewardsConfig, Plugin.RefferalCodeReward, Plugin.TopVotersBenefitConfig);

            PlayerRewardTracker rewardTracker = new PlayerRewardTracker(Path.Combine(VoteRewardsMain.Instance.StoragePath, "VoteReward", "PlayerData.xml"));

            // Sprawdzanie ostatniej daty odbioru nagrody
            var lastClaimDate = await rewardTracker.GetLastRewardClaimDate(Context.Player.SteamUserId);
            if (lastClaimDate.HasValue && (DateTime.UtcNow - lastClaimDate.Value).TotalDays < 30)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", "You can only claim the top voter reward once a month. Please try again later.", Color.Green, Context.Player.SteamUserId);
                return;
            }

            // Pobieranie liczby głosów dla gracza
            string topVotersResponse = await Plugin.ApiHelper.GetTopVotersBySteamIdAsync();
            var voterResponse = JsonConvert.DeserializeObject<VoterResponse>(topVotersResponse);
            var playerVotes = voterResponse?.Voters?.Find(v => v.Nickname == Context.Player.DisplayName)?.Votes ?? 0;

            List<string> messages = new List<string> { $"{Plugin.Config.NotificationPrefix}" };

            bool rewardGrantedFlag = false;
            // Znajdowanie odpowiedniego zakresu głosów i przyznawanie nagród
            foreach (var voteRangeReward in Plugin.TopVotersBenefitConfig.VoteRangeRewards)
            {
                if (playerVotes >= voteRangeReward.MinVotes && playerVotes <= voteRangeReward.MaxVotes)
                {
                    var successfulTopRewards = new List<string>();

                    foreach (var reward in voteRangeReward.Rewards)
                    {
                        int randomAmount = getRandomRewardsUtils.GetRandomAmount(reward.AmountOne, reward.AmountTwo);
                        bool rewardGranted = PlayerRewardManager.AwardPlayer(ulong.Parse(steamId), reward, randomAmount, VoteRewardsMain.Log, Plugin.Config);
                        if (rewardGranted)
                        {
                            successfulTopRewards.Add($"{randomAmount}x {reward.ItemSubtypeId}");
                            VoteRewardsMain.Log.Info($"Player {steamId} received {randomAmount} of {reward.ItemSubtypeId}.");
                            rewardGrantedFlag = true;
                        }
                    }

                    if (successfulTopRewards.Any())
                    {
                        messages.Add("Based on your votes, you received:");
                        messages.AddRange(successfulTopRewards.Select(reward => $"{reward}"));
                        // Aktualizacja daty ostatniego odbioru nagrody
                        rewardTracker.UpdateLastRewardClaimDate(Context.Player.SteamUserId, DateTime.UtcNow);
                        
                        if (!VoteRewardsMain.Instance.Config.UseDatabase)
                        {
                            // Tylko wtedy wysyłaj dane przez NexusManager, gdy nie korzystamy z bazy danych
                            NexusManager.SendPlayerRewardTrackerToAllServers(Context.Player.SteamUserId, DateTime.UtcNow);
                        }
                    }
                    break;
                }
            }

            if (!rewardGrantedFlag)
            {
                messages.Add("You did not reach the vote threshold for a reward.");
            }

            VoteRewardsMain.ChatManager.SendMessageAsOther(messages.First(), string.Join("\n", messages.Skip(1)), Color.Green, Context.Player.SteamUserId);
        }

        [Command("reward", "Allows the player to claim their vote reward.")]
        [Permission(MyPromoteLevel.None)]
        public async void ClaimRewardCommand()
        {
            var steamId = Context.Player.SteamUserId;
            var identityId = Context.Player.IdentityId;

            var getRandomRewardsUtils = new GetRandomRewardsUtils(Plugin.RewardItemsConfig, Plugin.TimeSpentRewardsConfig, Plugin.RefferalCodeReward, Plugin.TopVotersBenefitConfig);

            try
            {
                int voteStatus = await Plugin.ApiHelper.CheckVoteStatusAsync(steamId.ToString());
                List<string> messages = new List<string> { $"{Plugin.Config.NotificationPrefix}" };

                switch (voteStatus)
                {
                    case -1:
                        messages.Add("Failed to check your vote status. Please try again later.");
                        break;
                    case 0:
                        messages.Add("You have not voted yet. Please vote first.");
                        break;
                    case 1:
                        List<RewardItem> rewards = getRandomRewardsUtils.GetRandomRewards();
                        rewards.RemoveAll(item => item == null);

                        if (rewards.Any())
                        {
                            var successfulRewards = new List<string>();

                            foreach (var reward in rewards)
                            {
                                int randomAmount = getRandomRewardsUtils.GetRandomAmount(reward.AmountOne, reward.AmountTwo);
                                bool rewardGranted = PlayerRewardManager.AwardPlayer(Context.Player.SteamUserId, reward, randomAmount, VoteRewardsMain.Log, Plugin.Config);
                                if (rewardGranted)
                                {
                                    successfulRewards.Add($"{randomAmount}x {reward.ItemSubtypeId}");
                                    VoteRewardsMain.Log.Info($"Player {steamId} received {randomAmount} of {reward.ItemSubtypeId}.");
                                }
                                else
                                {
                                    VoteRewardsMain.Log.Warn($"Player {steamId}'s inventory is full. Could not grant reward.");
                                }
                            }

                            if (successfulRewards.Any())
                            {
                                messages.Add("You received:");
                                messages.AddRange(successfulRewards);
                                messages.Add("Thank you for voting!");
                                await Plugin.ApiHelper.SetVoteAsClaimedAsync(steamId);
                            }
                            else
                            {
                                messages.Add("Your inventory is full. Please make space to receive your reward.");
                            }
                        }
                        else
                        {
                            messages.Add("No reward available at the moment. Please try again later.");
                            VoteRewardsMain.Log.Warn("No reward item found for player " + steamId);
                        }
                        break;
                    case 2:
                        messages.Add("You have already claimed your vote reward.");
                        break;
                }

                VoteRewardsMain.ChatManager.SendMessageAsOther(messages.First(), string.Join("\n", messages.Skip(1)), Color.Green, Context.Player.SteamUserId);
            }
            catch (Exception ex)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", "We encountered an error processing your request. Please try again later.", Color.Red, Context.Player.SteamUserId);
                VoteRewardsMain.Log.Warn($"Reward command failed for {steamId}: {ex.Message}");
            }
        }

        [Command("topvoters", "Shows top 10 voters of the month.")]
        [Permission(MyPromoteLevel.None)]
        public async void TopVotersCommand()
        {
            try
            {
                string responseContent = await Plugin.ApiHelper.GetTopVotersAsync();
                if (responseContent.StartsWith("Error:"))
                {
                    VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", responseContent, Color.Red, Context.Player.SteamUserId);
                    return;
                }

                var voterResponse = JsonConvert.DeserializeObject<VoterResponse>(responseContent);
                if (voterResponse?.Voters == null || voterResponse.Voters.Count == 0)
                {
                    VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", "No voters data available.", Color.Red, Context.Player.SteamUserId);
                    return;
                }

                string message = "Top 10 Voters:\n" + string.Join("\n", voterResponse.Voters.Select((voter, index) => $"{index + 1}. {voter.Nickname} - {voter.Votes} votes"));
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", message, Color.Green, Context.Player.SteamUserId);
            }
            catch (Exception ex)
            {
                VoteRewardsMain.Log.Warn("Failed to get top voters: " + ex.Message);
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", "Failed to retrieve top voters. Please try again later.", Color.Red, Context.Player.SteamUserId);
            }
        }

        [Command("subtracttime", "Subtracts time from a player's total playtime.")]
        [Permission(MyPromoteLevel.Admin)]
        public void SubtractTimeCommand(string playerIdentifier, int minutes)
        {
            ulong steamId;
            if (!ulong.TryParse(playerIdentifier, out steamId))
            {
                // Jeśli nie jest to SteamID, spróbuj znaleźć gracza po NickName
                var player = Plugin.PlayerTimeTracker.FindPlayerByNickName(playerIdentifier);
                if (player.HasValue)
                {
                    steamId = player.Value.Item1;
                }
                else
                {
                    VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", $"Player with NickName '{playerIdentifier}' not found.", Color.Red, Context.Player.SteamUserId);
                    return;
                }
            }

            TimeSpan timeToSubtract = TimeSpan.FromMinutes(minutes);
            Plugin.PlayerTimeTracker.SubtractPlayerTime(steamId, timeToSubtract);
            VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", $"Subtracted {minutes} minutes from player's (SteamID: {steamId}) playtime.", Color.Red, Context.Player.SteamUserId);
        }

        [Command("topplaytime", "Shows top 5 players with the most time spent on the server.")]
        [Permission(MyPromoteLevel.None)]
        public void ShowTopPlayersCommand()
        {
            if (!Plugin.Config.PlayerTimeTracker)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "Player Time Tracker function is disabled", Color.Red, Context.Player.SteamUserId);
                return;
            }
            var topPlayers = Plugin.PlayerTimeTracker.GetTopPlayers(5);
            if (topPlayers.Count == 0)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"Top Play Time", "No player data available.", Color.Red, Context.Player.SteamUserId);
                return;
            }

            string response = "Top 5 players by time spent on the server:\n";
            int rank = 1;
            foreach (var player in topPlayers)
            {
                var totalMinutes = (long)player.TotalTimeSpent.TotalMinutes;
                response += $"{rank}. {player.NickName}: {totalMinutes} minutes\n";
                rank++;
            }

            VoteRewardsMain.ChatManager.SendMessageAsOther($"Top Play Time", response, Color.Green, Context.Player.SteamUserId);
        }
    }
}
