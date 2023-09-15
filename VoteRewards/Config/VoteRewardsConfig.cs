using System;
using System.Collections.Generic;
using Torch;

namespace VoteRewards
{
    public class VoteRewardsConfig : ViewModel
    {
        private string _serverApiKey;
        public string ServerApiKey
        {
            get => _serverApiKey;
            set => SetValue(ref _serverApiKey, value);
        }

    }

}
