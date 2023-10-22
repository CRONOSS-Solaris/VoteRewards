using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
