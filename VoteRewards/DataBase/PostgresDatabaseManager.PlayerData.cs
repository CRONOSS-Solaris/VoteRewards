using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace VoteRewards.DataBase
{
    public partial class PostgresDatabaseManager
    {
        public void SavePlayerTime(long steamId, string nickName, TimeSpan totalTimeSpent)
        {
            try
            {
                using (var PlayerDataConnection = new NpgsqlConnection(_connectionString))
                {
                    PlayerDataConnection.Open();

                    var checkQuery = "SELECT COUNT(*) FROM VoteRewards_Player_Data WHERE steam_id = @SteamId;";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, PlayerDataConnection))
                    {
                        checkCmd.Parameters.AddWithValue("@SteamId", steamId);
                        var exists = (long)checkCmd.ExecuteScalar() > 0;

                        string commandText;
                        if (exists)
                        {
                            commandText = @"
                            UPDATE VoteRewards_Player_Data
                            SET nickname = @NickName, total_time_spent = @TotalTimeSpent
                            WHERE steam_id = @SteamId;";
                        }
                        else
                        {
                            commandText = @"
                            INSERT INTO VoteRewards_Player_Data (steam_id, nickname, total_time_spent)
                            VALUES (@SteamId, @NickName, @TotalTimeSpent);";
                        }

                        using (var cmd = new NpgsqlCommand(commandText, PlayerDataConnection))
                        {
                            cmd.Parameters.AddWithValue("@SteamId", steamId);
                            cmd.Parameters.AddWithValue("@NickName", nickName);
                            cmd.Parameters.AddWithValue("@TotalTimeSpent", (long)totalTimeSpent.TotalMinutes);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred while saving player time: {ex.Message}");
            }
        }


        public List<(long SteamId, string NickName, long TotalTimeSpent)> GetAllPlayerTimes()
        {
            var playersData = new List<(long SteamId, string NickName, long TotalTimeSpent)>();
            try
            {
                using (var PlayerDataConnection = new NpgsqlConnection(_connectionString))
                {
                    PlayerDataConnection.Open();
                    var commandText = "SELECT steam_id, nickname, total_time_spent FROM VoteRewards_Player_Data;";

                    using (var cmd = new NpgsqlCommand(commandText, PlayerDataConnection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            playersData.Add((
                                SteamId: reader.GetInt64(0),
                                NickName: reader.GetString(1),
                                TotalTimeSpent: reader.GetInt64(2)
                            ));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred while getting all player times: {ex.Message}\n{ex.StackTrace}");
            }

            return playersData;
        }


        public async Task<DateTime?> GetLastRewardClaimDateAsync(long steamId)
        {
            DateTime? lastRewardClaimDate = null;
            try
            {
                using (var PlayerDataConnection = new NpgsqlConnection(_connectionString))
                {
                    await PlayerDataConnection.OpenAsync();
                    var commandText = "SELECT last_reward_claim_date FROM VoteRewards_Player_Data WHERE steam_id = @SteamId;";

                    using (var cmd = new NpgsqlCommand(commandText, PlayerDataConnection))
                    {
                        cmd.Parameters.AddWithValue("@SteamId", steamId);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != DBNull.Value)
                        {
                            lastRewardClaimDate = (DateTime?)result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred while getting last reward claim date: {ex.Message}\n{ex.StackTrace}");
            }
            return lastRewardClaimDate;
        }

        public async Task UpdateLastRewardClaimDateAsync(long steamId, DateTime claimDate)
        {
            try
            {
                using (var PlayerDataConnection = new NpgsqlConnection(_connectionString))
                {
                    await PlayerDataConnection.OpenAsync();
                    var commandText = @"
                    UPDATE VoteRewards_Player_Data 
                    SET last_reward_claim_date = @ClaimDate
                    WHERE steam_id = @SteamId;";

                    using (var cmd = new NpgsqlCommand(commandText, PlayerDataConnection))
                    {
                        cmd.Parameters.AddWithValue("@SteamId", steamId);
                        cmd.Parameters.AddWithValue("@ClaimDate", claimDate);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred while updating last reward claim date: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public async Task<List<(string NickName, TimeSpan TotalTimeSpent)>> GetTopPlayersFromDatabase(int count)
        {
            var topPlayers = new List<(string NickName, TimeSpan TotalTimeSpent)>();
            try
            {
                using (var PlayerDataConnection = new NpgsqlConnection(_connectionString))
                {
                    await PlayerDataConnection.OpenAsync();
                    var commandText = @"
                    SELECT nickname, total_time_spent 
                    FROM VoteRewards_Player_Data 
                    ORDER BY total_time_spent DESC 
                    LIMIT @Count;";

                    using (var cmd = new NpgsqlCommand(commandText, PlayerDataConnection))
                    {
                        cmd.Parameters.AddWithValue("@Count", count);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var nickName = reader.GetString(0);
                                var totalTimeSpent = TimeSpan.FromMinutes(reader.GetInt64(1));
                                topPlayers.Add((nickName, totalTimeSpent));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred while getting top players from database: {ex.Message}\n{ex.StackTrace}");
            }

            return topPlayers;
        }
    }
}
