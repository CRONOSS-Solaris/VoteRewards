using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VoteRewards.Utils;

namespace VoteRewards.DataBase
{
    public partial class PostgresDatabaseManager
    {

        public async Task<List<EventCode>> LoadEventCodesAsync()
        {
            var eventCodes = new List<EventCode>();

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new NpgsqlCommand("SELECT code, max_usage_count, redeemed_by_steam_ids FROM VoteRewards_Event_Code", connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var code = reader.GetString(0);
                                int? maxUsageCount = reader.IsDBNull(1) ? null : reader.GetInt32(1);
                                var redeemedBySteamIds = reader.IsDBNull(2) ? new HashSet<ulong>() : new HashSet<ulong>(((long[])reader[2]).Select(id => (ulong)id));

                                eventCodes.Add(new EventCode(code, maxUsageCount) { RedeemedBySteamIds = redeemedBySteamIds });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred while loading event codes: {ex.Message}\n{ex.StackTrace}");
            }

            return eventCodes;
        }

        public async Task SaveEventCodesAsync(List<EventCode> eventCodes)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Najpierw aktualizujemy lub wstawiamy nowe kody
                    foreach (var eventCode in eventCodes)
                    {
                        var cmdText = @"
                        INSERT INTO VoteRewards_Event_Code (code, max_usage_count, redeemed_by_steam_ids)
                        VALUES (@Code, @MaxUsageCount, @RedeemedBySteamIds)
                        ON CONFLICT (code) DO UPDATE
                        SET max_usage_count = EXCLUDED.max_usage_count, redeemed_by_steam_ids = EXCLUDED.redeemed_by_steam_ids";

                        using (var cmd = new NpgsqlCommand(cmdText, connection))
                        {
                            cmd.Parameters.AddWithValue("@Code", eventCode.Code);
                            cmd.Parameters.AddWithValue("@MaxUsageCount", (object)eventCode.MaxUsageCount ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RedeemedBySteamIds", eventCode.RedeemedBySteamIds.Select(id => (long)id).ToArray());
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Następnie usuwamy kody z bazy danych, które osiągnęły limit użycia i zostały usunięte z lokalnej listy
                    var allCodesInDb = await LoadEventCodesAsync(); // Załaduj wszystkie kody z bazy danych
                    var codesToRemove = allCodesInDb.Where(c => !eventCodes.Any(ec => ec.Code == c.Code)).Select(c => c.Code); // Znajdź kody do usunięcia

                    foreach (var code in codesToRemove)
                    {
                        var deleteCmdText = "DELETE FROM VoteRewards_Event_Code WHERE code = @Code";
                        using (var deleteCmd = new NpgsqlCommand(deleteCmdText, connection))
                        {
                            deleteCmd.Parameters.AddWithValue("@Code", code);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred while saving event codes: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
