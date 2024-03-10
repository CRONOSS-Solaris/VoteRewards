using NLog;
using Npgsql;
using System;
using System.Collections.Generic;

namespace VoteRewards.DataBase
{
    public partial class PostgresDatabaseManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly string _connectionString;

        public PostgresDatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void InitializeDatabase()
        {
            try
            {
                using (var PlayerDataConnection = new NpgsqlConnection(_connectionString))
                {
                    PlayerDataConnection.Open();
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = PlayerDataConnection;
                        cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS VoteRewards_Player_Data (
                            steam_id BIGINT PRIMARY KEY,
                            nickname VARCHAR(255),
                            total_time_spent BIGINT NOT NULL DEFAULT 0,
                            last_reward_claim_date TIMESTAMP WITHOUT TIME ZONE
                        )";
                        cmd.ExecuteNonQuery();
                    }
                }

                using (var ReferralCodeConnection = new NpgsqlConnection(_connectionString))
                {
                    ReferralCodeConnection.Open();
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = ReferralCodeConnection;
                        cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS VoteRewards_Referral_Code (
                        steam_id BIGINT PRIMARY KEY,
                        nickname VARCHAR(255),
                        codes TEXT[],
                        redeemed_by_steam_ids BIGINT[],
                        code_usage_count INT NOT NULL DEFAULT 0
                        )";
                        cmd.ExecuteNonQuery();
                    }
                }

                using (var EventCodeConnection = new NpgsqlConnection(_connectionString))
                {
                    EventCodeConnection.Open();
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = EventCodeConnection;
                        cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS VoteRewards_Event_Code (
                            code TEXT PRIMARY KEY,
                            max_usage_count INT,
                            redeemed_by_steam_ids BIGINT[]
                        )";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while initializing the database: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
