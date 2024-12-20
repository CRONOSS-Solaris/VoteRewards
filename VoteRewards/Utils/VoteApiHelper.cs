using NLog;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace VoteRewards.Utils
{
    public class VoteApiHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private string ServerApiKey => VoteRewardsMain.Instance.Config.ServerApiKey;

        public VoteApiHelper()
        {
        }

        public async Task<int> CheckVoteStatusAsync(string steamId)
        {
            string apiUrl = $"https://space-engineers.com/api/?object=votes&element=claim&key={ServerApiKey}&steamid={steamId}";

            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
            {
                HttpResponseMessage response;

                try
                {
                    response = await client.GetAsync(apiUrl);
                }
                catch (HttpRequestException e)
                {
                    Log.Warn("API(): Network error while contacting the API: " + e.Message);
                    return -1; // Network issue
                }
                catch (Exception e)
                {
                    Log.Error("API(): Unexpected error while contacting the API: " + e.Message);
                    return -1; // General API error
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"API(): API Response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Log.Warn($"API(): Unsuccessful API response: {response.StatusCode}");
                    return -1; // Server returned an error
                }

                if (responseContent.Contains("Error"))
                {
                    Log.Warn($"API(): API responded with an error: {responseContent}");
                    return -1; // API-specific error
                }

                if (!int.TryParse(responseContent, out int voteStatus))
                {
                    Log.Warn("API(): Failed to parse API response");
                    return -1; // Parsing issue
                }

                return voteStatus;
            }
        }

        public async Task SetVoteAsClaimedAsync(ulong steamId)
        {
            string apiUrl = $"https://space-engineers.com/api/?action=post&object=votes&element=claim&key={ServerApiKey}&steamid={steamId}";

            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
            {
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(apiUrl);
                }
                catch (HttpRequestException e)
                {
                    Log.Warn("API(): Network error while contacting the API: " + e.Message);
                    throw;
                }
                catch (Exception e)
                {
                    Log.Error("API(): Unexpected error while setting vote as claimed: " + e.Message);
                    throw;
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"API(): API Response for setting vote as claimed: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Log.Warn($"API(): Unsuccessful API response: {response.StatusCode}");
                    throw new Exception("API response was not successful.");
                }

                if (responseContent.Contains("Error"))
                {
                    Log.Warn($"API(): API responded with an error while setting vote as claimed: {responseContent}");
                    throw new Exception("API responded with an error.");
                }

                if (responseContent.Trim() != "1")
                {
                    Log.Warn("API(): Failed to set the vote as claimed");
                    throw new Exception("Failed to set the vote as claimed");
                }
            }
        }

        public async Task<string> GetTopVotersAsync(string period = "current", int limit = 10, string rankBy = "nickname")
        {
            string apiUrl = $"https://space-engineers.com/api/?object=servers&element=voters&key={ServerApiKey}&month={period}&format=json&limit={limit}&rank={rankBy}";

            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
            {
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(apiUrl);
                }
                catch (HttpRequestException e)
                {
                    Log.Warn("API(): Network error while contacting the API: " + e.Message);
                    return "Error: Network error.";
                }
                catch (Exception e)
                {
                    Log.Error("API(): Unexpected error while retrieving top voters: " + e.Message);
                    return "Error: Unexpected error.";
                }

                if (!response.IsSuccessStatusCode)
                {
                    return "Error: API response was not successful.";
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"API(): API Response: {responseContent}");

                return responseContent;
            }
        }

        public async Task<string> GetTopVotersBySteamIdAsync(string period = "previous", int limit = 10000, string rankBy = "steamid")
        {
            string apiUrl = $"https://space-engineers.com/api/?object=servers&element=voters&key={ServerApiKey}&month={period}&format=json&limit={limit}&rank={rankBy}";

            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
            {
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(apiUrl);
                }
                catch (HttpRequestException e)
                {
                    Log.Warn("API(): Network error while contacting the API: " + e.Message);
                    return "Error: Network error.";
                }
                catch (Exception e)
                {
                Log.Error("API(): Unexpected error while retrieving voters by Steam ID: " + e.Message);
                    return "Error: Unexpected error.";
                }

                if (!response.IsSuccessStatusCode)
                {
                    return "Error: API response was not successful.";
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                LoggerHelper.DebugLog(Log, VoteRewardsMain.Instance.Config, $"API(): API Response: {responseContent}");

                return responseContent;
            }
        }
    }
}
