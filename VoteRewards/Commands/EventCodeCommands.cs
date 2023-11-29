using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VoteRewards.Utils;
using VRage.Game.ModAPI;
using VRageMath;

namespace VoteRewards
{
    [Category("event")]
    public class EventCodeCommands : CommandModule
    {
        private EventCodeManager EventCodeManager => ((VoteRewards)Context.Plugin).EventCodeManager;
        private VoteRewardsConfig Config => ((VoteRewards)Context.Plugin).Config;
        public VoteRewards Plugin => (VoteRewards)Context.Plugin;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [Command("create", "Creates an Event code.")]
        [Permission(MyPromoteLevel.Owner)]
        public void CreateEventCodeCommand(int? maxUsageCount = null)
        {
            if (Context.Player == null)
            {
                Log.Error("This command can only be used by a player.");
                return;
            }

            // Sprawdzamy, czy funkcja kodów wydarzeniowych jest włączona
            if (!Config.IsEventCodeEnabled)
            {
                VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.EventCodePrefix}", "Event code function is disabled", Color.Red, Context.Player.SteamUserId);
                return;
            }

            try
            {
                string newCode = EventCodeManager.CreateEventCode(maxUsageCount);
                VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.EventCodePrefix}", $"Event code created: {newCode}", Color.Green, Context.Player.SteamUserId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while creating an event code.");
                VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.EventCodePrefix}", "Failed to create event code.", Color.Red, Context.Player.SteamUserId);
            }
        }

        [Command("redeem", "Redeems an Event code.")]
        [Permission(MyPromoteLevel.None)]
        public void RedeemEventCodeCommand(string code)
        {
            if (Context.Player == null)
            {
                Log.Error("This command can only be used by a player.");
                return;
            }

            // Sprawdzamy, czy funkcja kodów wydarzeniowych jest włączona
            if (!Config.IsEventCodeEnabled)
            {
                VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.EventCodePrefix}", "Event code function is disabled", Color.Red, Context.Player.SteamUserId);
                return;
            }

            ulong steamId = Context.Player.SteamUserId;

            try
            {
                bool result = EventCodeManager.RedeemCode(code, steamId);
                if (result)
                {
                    // Logika nagradzania gracza za wykorzystanie kodu wydarzeniowego
                    List<RewardItem> rewards = Plugin.EventCodeReward.RewardItems;
                    rewards.RemoveAll(item => item == null);

                    List<string> rewardMessages = new List<string>();

                    if (rewards.Any())
                    {
                        foreach (var reward in rewards)
                        {
                            int randomAmount = new Random().Next(reward.AmountOne, reward.AmountTwo + 1);
                            bool rewardGranted = PlayerRewardManager.AwardPlayer(Context.Player.SteamUserId, reward, randomAmount, VoteRewards.Log, Plugin.Config);
                            if (rewardGranted)
                            {
                                rewardMessages.Add($"{randomAmount}x {reward.ItemSubtypeId}");
                            }
                        }

                        if (rewardMessages.Any())
                        {
                            VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.EventCodePrefix}", "You received:\n" + string.Join("\n", rewardMessages), Color.Green, Context.Player.SteamUserId);
                        }
                        else
                        {
                            VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.EventCodePrefix}", "Your inventory is full. Please make space to receive your reward.", Color.Red, Context.Player.SteamUserId);
                        }
                    }
                    else
                    {
                        VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.EventCodePrefix}", "No reward available at the moment. Please try again later.", Color.Red, Context.Player.SteamUserId);
                    }
                }
                else
                {
                    VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.EventCodePrefix}", $"Failed to redeem event code or code already used.", Color.Red, Context.Player.SteamUserId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while redeeming an event code.");
                VoteRewards.ChatManager.SendMessageAsOther($"{Plugin.Config.EventCodePrefix}", "Error occurred while redeeming the code.", Color.Red, Context.Player.SteamUserId);
            }
        }

    }
}
