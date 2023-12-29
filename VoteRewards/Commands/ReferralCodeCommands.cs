using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using Torch.Commands;
using Torch.Commands.Permissions;
using VoteRewards.Nexus;
using VoteRewards.Utils;
using VRage.Game.ModAPI;
using VRageMath;

namespace VoteRewards
{
    [Category("referral")]
    public class ReferralCodeCommands : CommandModule
    {
        private ReferralCodeManager ReferralCodeManager => ((VoteRewardsMain)Context.Plugin).ReferralCodeManager;
        private VoteRewardsConfig Config => ((VoteRewardsMain)Context.Plugin).Config;
        public VoteRewardsMain Plugin => (VoteRewardsMain)Context.Plugin;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [Command("create", "Creates a referral code.")]
        [Permission(MyPromoteLevel.None)]
        public void CreateReferralCodeCommand(int numberOfCodes = 1)
        {
            ulong creatorSteamId = Context.Player.SteamUserId;
            string playerName = Context.Player.DisplayName;

            if (Context.Player == null)
            {
                Log.Error("This command can only be used by a player.");
                return;
            }

            if (!Config.IsReferralCodeEnabled)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "Referral code function is disabled", Color.Red, Context.Player.SteamUserId);
                return;
            }

            var remainingCooldown = ReferralCodeManager.GetRemainingCooldown(creatorSteamId);
            if (remainingCooldown > TimeSpan.Zero)
            {
                int remainingMinutes = (int)Math.Ceiling(remainingCooldown.TotalMinutes);
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", $"You must wait {remainingMinutes} minute(s) before creating another code.", Color.Red, Context.Player.SteamUserId);
                return;
            }

            // Sprawdzenie i korekta liczby kodów, jeśli jest za duża
            numberOfCodes = Math.Min(numberOfCodes, Config.MaxReferralCodes - ReferralCodeManager.GetReferralCodesForPlayer(creatorSteamId).Count);

            List<string> generatedCodes = new List<string>();
            for (int i = 0; i < numberOfCodes; i++)
            {
                var newReferralCode = ReferralCodeManager.CreateReferralCode(creatorSteamId, playerName);
                if (newReferralCode != null)
                {
                    string lastGeneratedCode = newReferralCode.Codes.LastOrDefault();
                    if (!string.IsNullOrEmpty(lastGeneratedCode))
                    {
                        generatedCodes.Add(lastGeneratedCode);

                        // Wywołanie metody wysyłającej kody na inne serwery
                        NexusManager.SendRefferalCodeCreateToAllServers(newReferralCode);
                    }
                }
            }

            if (generatedCodes.Count > 0)
            {
                string codesMessage = string.Join(", ", generatedCodes);
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", $"Your new referral codes are: {codesMessage}", Color.Green, Context.Player.SteamUserId);

            }
            else
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", $"Cannot create new referral codes. You may have reached the limit of {Config.MaxReferralCodes} codes.", Color.Red, Context.Player.SteamUserId);
            }
        }

        [Command("redeem", "Uses referral code.")]
        [Permission(MyPromoteLevel.None)]
        public void RedeemReferralCodeCommand(string code)
        {
            ulong redeemerSteamId = Context.Player.SteamUserId;

            if (Context.Player == null)
            {
                Log.Error("This command can only be used by a player.");
                return;
            }

            if (!Config.IsReferralCodeEnabled)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "Referral code function is disabled", Color.Red, Context.Player.SteamUserId);
                return;
            }

            var result = ReferralCodeManager.RedeemReferralCode(code, redeemerSteamId);
            switch (result)
            {
                case ReferralCodeManager.RedeemCodeResult.Success:
                    List<RewardItem> rewards = Plugin.RefferalCodeReward.RewardItems;
                    rewards.RemoveAll(item => item == null);

                    List<string> rewardMessages = new List<string>();

                    if (rewards.Any())
                    {
                        foreach (var reward in rewards)
                        {
                            int randomAmount = new Random().Next(reward.AmountOne, reward.AmountTwo + 1);
                            bool rewardGranted = PlayerRewardManager.AwardPlayer(Context.Player.SteamUserId, reward, randomAmount, VoteRewardsMain.Log, Plugin.Config);
                            if (rewardGranted)
                            {
                                rewardMessages.Add($"{randomAmount}x {reward.ItemSubtypeId}");
                            }
                        }

                        if (rewardMessages.Any())
                        {
                            VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "You received:\n" + string.Join("\n", rewardMessages), Color.Green, Context.Player.SteamUserId);
                        }
                        else
                        {
                            VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "Your inventory is full. Please make space to receive your reward.", Color.Red, Context.Player.SteamUserId);
                        }
                    }
                    else
                    {
                        VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "No reward available at the moment. Please try again later.", Color.Red, Context.Player.SteamUserId);
                    }
                    break;
                case ReferralCodeManager.RedeemCodeResult.TooMuchTimeSpent:
                    VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", $"You have played more than {Plugin.Config.ReferralCodeUsageTimeLimit} minutes on the server and cannot use a referral code.", Color.Red, Context.Player.SteamUserId);
                    break;
                case ReferralCodeManager.RedeemCodeResult.CodeNotFound:
                    VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "No such code found or already used", Color.Red, Context.Player.SteamUserId);
                    break;
                case ReferralCodeManager.RedeemCodeResult.CannotUseOwnCode:
                    VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "You cannot use your own referral code", Color.Red, Context.Player.SteamUserId);
                    break;
                case ReferralCodeManager.RedeemCodeResult.AlreadyUsed:
                    VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "You have already used a referral code and cannot use another.", Color.Red, Context.Player.SteamUserId);
                    break;
            }
        }

        [Command("mycodes", "Shows your referral codes.")]
        [Permission(MyPromoteLevel.None)]
        public void MyReferralCodesCommand()
        {
            ulong steamId = Context.Player.SteamUserId;

            if (Context.Player == null)
            {
                Log.Error("This command can only be used by a player.");
                return;
            }

            var codes = ReferralCodeManager.GetReferralCodesForPlayer(steamId);
            if (codes.Count == 0)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "You have no referral codes.", Color.Red, steamId);
            }
            else
            {
                string codesMessage = string.Join(", ", codes);
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", $"Your referral codes are: {codesMessage}", Color.Green, steamId);
            }
        }


        [Command("award", "Awards rewards based on how many times your referral codes were used.")]
        [Permission(MyPromoteLevel.None)]
        public void AwardForReferralsCommand(int? numberOfAwards = null)
        {
            ulong steamId = Context.Player.SteamUserId;

            if (Context.Player == null)
            {
                Log.Error("This command can only be used by a player.");
                return;
            }

            if (!Config.IsReferralCodeEnabled)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "Referral code function is disabled", Color.Red, Context.Player.SteamUserId);
                return;
            }

            var referralCode = ReferralCodeManager.GetReferralCode(steamId);
            if (referralCode == null)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "You do not have any referral codes.", Color.Red, steamId);
                return;
            }

            int totalUses = referralCode.CodeUsageCount;
            if (totalUses == 0)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "Your referral codes have not been used.", Color.Red, steamId);
                return;
            }

            if (!numberOfAwards.HasValue)
            {
                // Pokazuje liczbę dostępnych nagród i instrukcje w osobnych liniach
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", $"Available rewards: {totalUses}.\nTo claim, type '!referral award [number]'.", Color.Green, steamId);
                return;
            }

            int awardsToClaim = Math.Min(numberOfAwards.Value, totalUses);
            List<string> rewardMessages = new List<string>();

            for (int i = 0; i < awardsToClaim; i++)
            {
                // Przyznawanie nagrody (tak samo jak w komendzie "redeemcode")
                List<RewardItem> rewards = Plugin.RefferalCodeReward.RewardItems;
                rewards.RemoveAll(item => item == null);

                foreach (var reward in rewards)
                {
                    int randomAmount = new Random().Next(reward.AmountOne, reward.AmountTwo + 1);
                    bool rewardGranted = PlayerRewardManager.AwardPlayer(Context.Player.SteamUserId, reward, randomAmount, VoteRewardsMain.Log, Plugin.Config);
                    if (rewardGranted)
                    {
                        rewardMessages.Add($"{randomAmount}x {reward.ItemSubtypeId}");
                    }
                }
            }

            if (rewardMessages.Any())
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "You received:\n" + string.Join("\n", rewardMessages), Color.Green, Context.Player.SteamUserId);
            }
            else
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "No reward available at the moment. Please try again later.", Color.Red, Context.Player.SteamUserId);
            }

            // Aktualizacja licznika użycia kodu po przyznaniu nagród
            referralCode.CodeUsageCount -= awardsToClaim;

            NexusManager.SendAwardReferralCodeToAllServers(steamId, awardsToClaim);

            ReferralCodeManager.SaveReferralCodes();
        }

        [Command("testredeem", "Tests the referral code redemption for owners.(Nexus OFF)")]
        [Permission(MyPromoteLevel.Owner)]
        public void TestRedeemReferralCodeCommand(string code)
        {
            ulong redeemerSteamId = Context.Player.SteamUserId;

            if (Context.Player == null)
            {
                Log.Error("This command can only be used by a player.");
                return;
            }

            if (!Config.IsReferralCodeEnabled)
            {
                VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "Referral code function is disabled", Color.Red, Context.Player.SteamUserId);
                return;
            }

            var result = ReferralCodeManager.TestRedeemReferralCode(code, redeemerSteamId);
            switch (result)
            {
                case ReferralCodeManager.RedeemCodeResult.Success:
                    List<RewardItem> rewards = Plugin.RefferalCodeReward.RewardItems;
                    rewards.RemoveAll(item => item == null);

                    List<string> rewardMessages = new List<string>();

                    if (rewards.Any())
                    {
                        foreach (var reward in rewards)
                        {
                            int randomAmount = new Random().Next(reward.AmountOne, reward.AmountTwo + 1);
                            bool rewardGranted = PlayerRewardManager.AwardPlayer(Context.Player.SteamUserId, reward, randomAmount, VoteRewardsMain.Log, Plugin.Config);
                            if (rewardGranted)
                            {
                                rewardMessages.Add($"{randomAmount}x {reward.ItemSubtypeId}");
                            }
                        }

                        if (rewardMessages.Any())
                        {
                            VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "You received:\n" + string.Join("\n", rewardMessages), Color.Green, Context.Player.SteamUserId);
                        }
                        else
                        {
                            VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "Your inventory is full. Please make space to receive your reward.", Color.Red, Context.Player.SteamUserId);
                        }
                    }
                    else
                    {
                        VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "No reward available at the moment. Please try again later.", Color.Red, Context.Player.SteamUserId);
                    }
                    break;
                case ReferralCodeManager.RedeemCodeResult.CodeNotFound:
                    VoteRewardsMain.ChatManager.SendMessageAsOther($"{Plugin.Config.ReferralCodePrefix}", "No such code found or already used", Color.Red, Context.Player.SteamUserId);
                    break;
            }
        }
    }
}
