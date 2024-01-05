using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using Torch.Commands;
using Torch.Commands.Permissions;
using VoteRewards.Utils;
using VRage.Game.ModAPI;
using VRageMath;


namespace VoteRewards
{
    public class VoteRewardsCommands : CommandModule
    {

        public VoteRewardsMain Plugin => (VoteRewardsMain)Context.Plugin;

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

            VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", $"Please vote for us at: {voteUrl}", Color.Green, Context.Player.SteamUserId);
        }

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

        [Command("reward", "Allows the player to claim their vote reward.")]
        [Permission(MyPromoteLevel.None)]
        public async void ClaimRewardCommand()
        {
            var steamId = Context.Player.SteamUserId;
            var identityId = Context.Player.IdentityId;

            var getRandomRewardsUtils = new GetRandomRewardsUtils(Plugin.RewardItemsConfig, Plugin.TimeSpentRewardsConfig, Plugin.RefferalCodeReward);

            int voteStatus;
            try
            {
                voteStatus = await Plugin.ApiHelper.CheckVoteStatusAsync(steamId.ToString());
            }
            catch (Exception ex)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.NotificationPrefix}", "Failed to check your vote status. Please try again later.", Color.Green, Context.Player.SteamUserId);
                VoteRewardsMain.Log.Warn("Failed to check the vote status: " + ex.Message);
                return;
            }

            List<string> messages = new List<string> { $"{Plugin.Config.NotificationPrefix}" }; // Add prefix as the first message

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
                        try
                        {
                            await Plugin.ApiHelper.SetVoteAsClaimedAsync(steamId);

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
                            VoteRewardsMain.Log.Warn("Failed to claim the reward: " + ex.Message);
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
                    Context.Respond($"Player with NickName '{playerIdentifier}' not found.");
                    return;
                }
            }

            TimeSpan timeToSubtract = TimeSpan.FromMinutes(minutes);
            Plugin.PlayerTimeTracker.SubtractPlayerTime(steamId, timeToSubtract);
            Context.Respond($"Subtracted {minutes} minutes from player's (SteamID: {steamId}) playtime.");
        }

        [Command("topplaytime", "Shows top 5 players with the most time spent on the server.")]
        [Permission(MyPromoteLevel.None)]
        public void ShowTopPlayersCommand()
        {
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
