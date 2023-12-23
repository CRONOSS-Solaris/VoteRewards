using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using VoteRewards.Nexus;

namespace VoteRewards.Utils
{
    public class ReferralCodeManager
    {
        private List<ReferralCode> _referralCodes;
        private readonly string _filePath;
        private readonly VoteRewardsConfig _config;
        private Dictionary<ulong, DateTime> _lastCommandUsage;
        private readonly TimeSpan _commandCooldown;
        private HashSet<ulong> _playersWhoUsedCode;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public ReferralCodeManager(string filePath, VoteRewardsConfig config)
        {
            _filePath = filePath;
            _config = config;
            LoadReferralCodes();
            _lastCommandUsage = new Dictionary<ulong, DateTime>();
            _commandCooldown = TimeSpan.FromMinutes(config.CommandCooldownMinutes);
            _playersWhoUsedCode = new HashSet<ulong>();

            Log.Info($"Configuration: MaxReferralCodes = {_config.MaxReferralCodes}, CommandCooldownMinutes = {_config.CommandCooldownMinutes}");
        }


        private void LoadReferralCodes()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _referralCodes = JsonConvert.DeserializeObject<List<ReferralCode>>(json) ?? new List<ReferralCode>();
                Log.Info($"Loaded {_referralCodes.Count} referral codes from file.");
            }
            else
            {
                _referralCodes = new List<ReferralCode>();
                Log.Info("No referral codes file found, starting with an empty list.");
            }
        }


        public ReferralCode CreateReferralCode(ulong steamId, string playerName)
        {
            // Sprawdzenie, czy istnieje już kod dla tego gracza
            var existingReferralCode = _referralCodes.FirstOrDefault(rc => rc.SteamId == steamId);

            if (existingReferralCode != null)
            {
                if (existingReferralCode.Codes.Count >= _config.MaxReferralCodes)
                {
                    Log.Info($"Player {steamId} has reached the max limit of referral codes.");
                    return null;
                }

                var newCode = GenerateCode();
                existingReferralCode.Codes.Add(newCode);
                SaveReferralCodes();
                return existingReferralCode;
            }
            else
            {
                if (!CanCreateReferralCode(steamId))
                {
                    Log.Info($"Player {steamId} cannot create a new referral code due to cooldown.");
                    return null;
                }

                var newReferralCode = new ReferralCode
                {
                    SteamId = steamId,
                    PlayerName = playerName,
                    Codes = new List<string> { GenerateCode() }
                };
                _referralCodes.Add(newReferralCode);
                SaveReferralCodes();
                return newReferralCode;
            }
        }


        public bool CanCreateReferralCode(ulong steamId)
        {
            if (_lastCommandUsage.TryGetValue(steamId, out var lastUsed))
            {
                if (DateTime.UtcNow - lastUsed < _commandCooldown)
                {
                    Log.Info($"Player {steamId} is on cooldown. Time remaining: {(_commandCooldown - (DateTime.UtcNow - lastUsed)).TotalMinutes} minutes.");
                    return false;
                }
            }

            _lastCommandUsage[steamId] = DateTime.UtcNow;
            Log.Info($"Player {steamId} can create a new referral code. Updating last command usage time.");
            return true;
        }


        public TimeSpan GetRemainingCooldown(ulong steamId)
        {
            if (_lastCommandUsage.TryGetValue(steamId, out var lastUsed))
            {
                var timeElapsed = DateTime.UtcNow - lastUsed;
                if (timeElapsed < _commandCooldown)
                {
                    return _commandCooldown - timeElapsed;
                }
            }
            return TimeSpan.Zero;
        }

        public enum RedeemCodeResult
        {
            Success,
            CodeNotFound,
            CannotUseOwnCode,
            AlreadyUsed
        }


        public RedeemCodeResult RedeemReferralCode(string code, ulong redeemerSteamId)
        {
            // Sprawdzanie, czy gracz już wykorzystał jakiś kod
            if (HasPlayerUsedAnyCode(redeemerSteamId))
            {
                return RedeemCodeResult.AlreadyUsed;
            }

            var referralCode = _referralCodes.FirstOrDefault(rc => rc.Codes.Contains(code));
            if (referralCode == null)
            {
                return RedeemCodeResult.CodeNotFound;
            }

            if (referralCode.SteamId == redeemerSteamId)
            {
                return RedeemCodeResult.CannotUseOwnCode;
            }

            // Oznaczenie kodu jako wykorzystanego i aktualizacja CodeUsageCount
            referralCode.RedeemedBySteamIds.Add(redeemerSteamId);
            referralCode.CodeUsageCount++;

            NexusManager.SendRedeemReferralCodeToAllServers(code, redeemerSteamId, referralCode.RedeemedBySteamIds, referralCode.CodeUsageCount);

            // Usunięcie wykorzystanego kodu, ponieważ jest jednorazowy
            referralCode.Codes.Remove(code);

            SaveReferralCodes();
            return RedeemCodeResult.Success;
        }

        public bool HasPlayerUsedAnyCode(ulong steamId)
        {
            return _referralCodes.Any(rc => rc.RedeemedBySteamIds.Contains(steamId));
        }

        public ReferralCodeManager.RedeemCodeResult TestRedeemReferralCode(string code, ulong redeemerSteamId)
        {

            var referralCode = _referralCodes.FirstOrDefault(rc => rc.Codes.Contains(code));
            if (referralCode == null)
            {
                return ReferralCodeManager.RedeemCodeResult.CodeNotFound;
            }

            // Pominąć ten warunek, aby zezwolić na używanie własnych kodów
            // if (referralCode.SteamId == redeemerSteamId)
            // {
            //     return ReferralCodeManager.RedeemCodeResult.CannotUseOwnCode;
            // }

            // Oznaczenie kodu jako wykorzystanego (jeśli to potrzebne w testach)
            referralCode.RedeemedBySteamIds.Add(redeemerSteamId);
            referralCode.CodeUsageCount++;


            referralCode.Codes.Remove(code); // Opcjonalnie: usunięcie wykorzystanego kodu
            SaveReferralCodes();
            return ReferralCodeManager.RedeemCodeResult.Success;
        }


        public void SaveReferralCodes()
        {
            var json = JsonConvert.SerializeObject(_referralCodes, Formatting.Indented);
            File.WriteAllText(_filePath, json);
            Log.Info($"Saved {_referralCodes.Count} referral codes to file.");
        }


        private string GenerateCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public List<string> GetReferralCodesForPlayer(ulong steamId)
        {
            var referralCode = _referralCodes.FirstOrDefault(rc => rc.SteamId == steamId);
            return referralCode?.Codes ?? new List<string>();
        }

        public ReferralCode GetReferralCode(ulong steamId)
        {
            return _referralCodes.FirstOrDefault(rc => rc.SteamId == steamId);
        }

        public ReferralCode GetReferralCodeByCode(string code)
        {
            return _referralCodes.FirstOrDefault(rc => rc.Codes.Contains(code));
        }

        public void AddNewReferralCode(ReferralCode newReferralCode)
        {
            // Dodanie nowego wpisu
            _referralCodes.Add(newReferralCode);
            SaveReferralCodes();
        }

        public void UpdateExistingReferralCode(ReferralCode updatedReferralCode)
        {
            var existingReferralCode = _referralCodes.FirstOrDefault(rc => rc.SteamId == updatedReferralCode.SteamId);
            if (existingReferralCode != null)
            {
                // Aktualizacja istniejącego kodu
                existingReferralCode.Codes = updatedReferralCode.Codes;
                existingReferralCode.RedeemedBySteamIds = updatedReferralCode.RedeemedBySteamIds;
                existingReferralCode.CodeUsageCount = updatedReferralCode.CodeUsageCount;
                SaveReferralCodes();
            }
        }

    }
}