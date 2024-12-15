using Npgsql;
using static VoteRewards.Utils.ReferralCodeManager;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using VoteRewards.Utils;
using System.Linq;

namespace VoteRewards.DataBase
{
    public partial class PostgresDatabaseManager
    {
        public async Task<List<ReferralCode>> LoadReferralCodesAsync()
        {
            var referralCodes = new List<ReferralCode>();
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new NpgsqlCommand("SELECT steam_id, nickname, codes, redeemed_by_steam_ids, code_usage_count FROM VoteRewards_Referral_Code", connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var steamId = (long)reader.GetInt64(0);
                                var nickname = reader.GetString(1);
                                var codes = ((string[])reader["codes"]).ToList();
                                var redeemedBySteamIds = ((long[])reader["redeemed_by_steam_ids"]).Select(id => (long)id).ToHashSet();
                                var codeUsageCount = reader.GetInt32(4);

                                referralCodes.Add(new ReferralCode
                                {
                                    SteamId = (ulong)steamId,
                                    PlayerName = nickname,
                                    Codes = codes,
                                    RedeemedBySteamIds = redeemedBySteamIds.Select(id => (ulong)id).ToList(),
                                    CodeUsageCount = codeUsageCount
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred while loading referral codes: {ex.Message}\n{ex.StackTrace}");
            }

            return referralCodes;
        }

        public async Task SaveReferralCodesAsync(List<ReferralCode> referralCodes)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    foreach (var code in referralCodes)
                    {
                        var commandText = @"
                        INSERT INTO VoteRewards_Referral_Code (steam_id, nickname, codes, redeemed_by_steam_ids, code_usage_count) 
                        VALUES (@SteamId, @Nickname, @Codes, @RedeemedBySteamIds, @CodeUsageCount)
                        ON CONFLICT (steam_id) DO UPDATE 
                        SET nickname = EXCLUDED.nickname, codes = EXCLUDED.codes, redeemed_by_steam_ids = EXCLUDED.redeemed_by_steam_ids, code_usage_count = EXCLUDED.code_usage_count";

                        using (var cmd = new NpgsqlCommand(commandText, connection))
                        {
                            // Konwersja ulong na long
                            cmd.Parameters.AddWithValue("@SteamId", (long)code.SteamId);
                            cmd.Parameters.AddWithValue("@Nickname", code.PlayerName);
                            cmd.Parameters.AddWithValue("@Codes", code.Codes.ToArray());
                            cmd.Parameters.AddWithValue("@RedeemedBySteamIds", code.RedeemedBySteamIds.Select(id => (long)id).ToArray());
                            cmd.Parameters.AddWithValue("@CodeUsageCount", code.CodeUsageCount);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred while saving referral codes: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public async Task<RedeemCodeResult> RedeemCodeInDatabaseAsync(string code, long redeemerSteamId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Sprawdź, czy kod istnieje i nie został wykorzystany przez tego samego gracza
                    var cmdCheckCode = new NpgsqlCommand(@"SELECT steam_id, codes, redeemed_by_steam_ids FROM VoteRewards_Referral_Code WHERE codes @> ARRAY[@Code]::TEXT[] LIMIT 1", connection);
                    cmdCheckCode.Parameters.AddWithValue("@Code", code);

                    RedeemCodeResult result = RedeemCodeResult.CodeNotFound;

                    using (var reader = await cmdCheckCode.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var steamId = (long)reader.GetInt64(0);
                            var redeemedBySteamIds = ((long[])reader["redeemed_by_steam_ids"]).Select(id => (ulong)id).ToList();

                            if (steamId == redeemerSteamId)
                            {
                                result = RedeemCodeResult.CannotUseOwnCode;
                            }
                            else if (redeemedBySteamIds.Contains((ulong)redeemerSteamId))
                            {
                                result = RedeemCodeResult.AlreadyUsed;
                            }
                            else
                            {
                                redeemedBySteamIds.Add((ulong)redeemerSteamId);
                                result = RedeemCodeResult.Success;
                            }
                        }
                    }

                    if (result == RedeemCodeResult.Success)
                    {
                        // Aktualizuj informacje o wykorzystaniu kodu
                        var cmdUpdate = new NpgsqlCommand(@"UPDATE VoteRewards_Referral_Code SET redeemed_by_steam_ids = array_append(redeemed_by_steam_ids, @RedeemerSteamId) WHERE codes @> ARRAY[@Code]::TEXT[]", connection);
                        cmdUpdate.Parameters.AddWithValue("@RedeemerSteamId", (long)redeemerSteamId);
                        cmdUpdate.Parameters.AddWithValue("@Code", code);

                        await cmdUpdate.ExecuteNonQueryAsync();
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while redeeming a referral code: {ex.Message}\n{ex.StackTrace}");
                return RedeemCodeResult.CodeNotFound;
            }
        }
    }
}
