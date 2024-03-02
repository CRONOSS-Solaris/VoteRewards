using NLog;

namespace VoteRewards.Utils
{
    public static class LoggerHelper
    {
        public static void DebugLog(Logger log, VoteRewardsConfig config, string message)
        {
            if (config?.DebugMode ?? false)
            {
                log.Warn(message);
            }
        }
    }
}
