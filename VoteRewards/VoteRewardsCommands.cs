using NLog.Fluent;
using Sandbox.Game;
using Sandbox.Game.World;
using System;
using System.Linq;
using Torch;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Managers;
using VoteRewards.Utils;
using VRage.Game.ModAPI;

namespace VoteRewards
{
    public class VoteRewardsCommands : CommandModule
    {

        public VoteRewards Plugin => (VoteRewards)Context.Plugin;

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

            Context.Respond($"Please vote for us at: {voteUrl}");
        }

        [Command("claimreward", "Allows the player to claim their vote reward.")]
        [Permission(MyPromoteLevel.None)]
        public async void ClaimRewardCommand()
        {
            var steamId = Context.Player.SteamUserId;
            var identityId = Context.Player.IdentityId;

            int voteStatus;
            try
            {
                voteStatus = await Plugin.CheckVoteStatusAsync(steamId.ToString());
            }
            catch (Exception ex)
            {
                Context.Respond("Failed to check the vote status. Please try again later.");
                VoteRewards.Log.Warn("Failed to check the vote status: " + ex.Message);
                return;
            }

            switch (voteStatus)
            {
                case -1:
                    Context.Respond("Failed to check your vote status. Please try again later.");
                    break;
                case 0:
                    Context.Respond("You have not voted yet. Please vote first.");
                    break;
                case 1:
                    RewardItem reward = Plugin.GetRandomReward();
                    if (reward != null)
                    {
                        try
                        {
                            // Mark the vote as claimed
                            await Plugin.SetVoteAsClaimedAsync(steamId);

                            // Award the reward to the player
                            bool rewardGranted = Plugin.AwardPlayer(steamId, reward);
                            if (rewardGranted)
                            {
                                Context.Respond($"You received {reward.Amount} of {reward.ItemSubtypeId}. Thank you for voting!");
                                VoteRewards.Log.Info($"Player {steamId} received {reward.Amount} of {reward.ItemSubtypeId}.");
                            }
                            else
                            {
                                Context.Respond("Your inventory is full. Please make space to receive your reward.");
                                VoteRewards.Log.Warn($"Player {steamId}'s inventory is full. Could not grant reward.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Context.Respond("Failed to claim the reward. Please try again later.");
                            VoteRewards.Log.Warn("Failed to claim the reward: " + ex.Message);
                        }
                    }
                    else
                    {
                        Context.Respond("No reward available at the moment. Please try again later.");
                        VoteRewards.Log.Warn("No reward item found for player " + steamId);
                    }
                    break;
                case 2:
                    Context.Respond("You have already claimed your vote reward.");
                    break;
            }
        }


        [Command("testgetrandomreward", "Test the GetRandomReward method.")]
        [Permission(MyPromoteLevel.Admin)]  // Ustaw na Admin, aby tylko admini mogli używać tej komendy
        public void TestGetRandomReward()
        {
            RewardItem reward = Plugin.GetRandomReward();
            if (reward != null)
            {
                Context.Respond($"Generated random reward: {reward.Amount} of {reward.ItemSubtypeId}");
            }
            else
            {
                Context.Respond("No reward generated.");
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
                Context.Respond($"Successfully awarded {amount} of {itemSubtypeId}");
            }
            else
            {
                Context.Respond($"Failed to award {amount} of {itemSubtypeId}. It is possible that the inventory is full.");
            }
        }

    }
}

//koniec