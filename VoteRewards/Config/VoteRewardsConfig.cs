using System;
using System.Collections.Generic;
using Torch;

namespace VoteRewards
{
    public class VoteRewardsConfig : ViewModel
    {
        private string _serverApiKey;
        private string _votingLink;
        public string ServerApiKey
        {
            get => _serverApiKey;
            set => SetValue(ref _serverApiKey, value);
        }

        public string VotingLink
        {
            get => _votingLink;
            set => SetValue(ref _votingLink, value);
        }
    }

}
