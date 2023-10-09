using NLog;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace VoteRewards.Utils
{
    public class VoteApiHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly string _serverApiKey;

        public VoteApiHelper(string serverApiKey)
        {
            _serverApiKey = serverApiKey;
        }

        public async Task<int> CheckVoteStatusAsync(string steamId)
        {
            string apiUrl = $"https://space-engineers.com/api/?object=votes&element=claim&key={_serverApiKey}&steamid={steamId}";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response;

                try
                {
                    response = await client.GetAsync(apiUrl);
                }
                catch (HttpRequestException e)
                {
                    Log.Warn("API(): Network error while contacting the API: " + e.Message);
                    return -1;
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                Log.Info($"API(): API Response: {responseContent}");

                if (responseContent.Contains("Error"))
                {
                    Log.Warn($"API responded with an error: {responseContent}");
                    return -1;
                }

                if (!int.TryParse(responseContent, out int voteStatus))
                {
                    Log.Warn("Failed to parse API response");
                    return -1;
                }

                return voteStatus;
            }
        }

        public async Task SetVoteAsClaimedAsync(ulong steamId)
        {
            string apiUrl = $"https://space-engineers.com/api/?action=post&object=votes&element=claim&key={_serverApiKey}&steamid={steamId}";

            using (HttpClient client = new HttpClient())
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

                string responseContent = await response.Content.ReadAsStringAsync();
                Log.Info($"API(): API Response for setting vote as claimed: {responseContent}");

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
    }
}
