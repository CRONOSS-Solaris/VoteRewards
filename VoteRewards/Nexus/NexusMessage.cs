using Nexus.API;
using ProtoBuf;
#nullable enable

namespace VoteRewards.Nexus
{
    [ProtoContract]
    public class NexusMessage
    {
        public enum MessageType
        {
            BaseConfig,
            TimeSpentRewardsConfig,
            RewardItemsConfig,
            RefferalCodeReward,
            RefferalCodeCreate,
            RedeemReferralCode,
            AwardReferralCode,
            EventCodeReward,
            EventCodeCreate,
            RedeemEventCode,
            PlayerTimeTracker,
            TopVotersBenefitConfig,
            PlayerRewardTracker
            // ...
        }

        [ProtoMember(1)]
        public byte[]? ConfigData { get; set; }

        [ProtoMember(2)]
        public MessageType Type { get; set; }

        [ProtoMember(3)]
        public byte[]? Data { get; set; }

        public readonly int fromServerID;
        public readonly int toServerID;
        public readonly bool isTestAnnouncement;
        public readonly bool requestLobbyServer;
        public readonly NexusAPI.Server? lobbyServerData;
        public readonly bool isLobbyReply;

        public NexusMessage()
        {
            // Konstruktor domyślny może być potrzebny dla serializacji.
        }

        public NexusMessage(int _fromServerId, int _toServerId, byte[] data, MessageType messageType)
        {
            fromServerID = _fromServerId;
            toServerID = _toServerId;
            isTestAnnouncement = false;
            requestLobbyServer = false;
            isLobbyReply = false;
            lobbyServerData = null;


            if (messageType == MessageType.BaseConfig)
            {
                ConfigData = data;
                Data = new byte[0];
                //PlayerOffers = new byte[0];

            }
            else if (messageType == MessageType.TimeSpentRewardsConfig)
            {
                ConfigData = data;
                Data = new byte[0];
            }
            else if (messageType == MessageType.RewardItemsConfig)
            {
                ConfigData = data;
                Data = new byte[0];
            }
            else if (messageType == MessageType.RefferalCodeReward)
            {
                ConfigData = data;
                Data = new byte[0];
            }
            else if (messageType == MessageType.EventCodeReward)
            {
                ConfigData = data;
                Data = new byte[0];
            }
            else if (messageType == MessageType.RefferalCodeCreate)
            {
                Data = data;
                ConfigData = new byte[0];
            }
            else if (messageType == MessageType.RedeemReferralCode)
            {
                Data = data;
                ConfigData = new byte[0];
            }
            else if (messageType == MessageType.AwardReferralCode)
            {
                Data = data;
                ConfigData = new byte[0];
            }
            else if (messageType == MessageType.EventCodeCreate)
            {
                Data = data;
                ConfigData = new byte[0];
            }
            else if (messageType == MessageType.RedeemEventCode)
            {
                Data = data;
                ConfigData = new byte[0];
            }
            else if (messageType == MessageType.PlayerTimeTracker)
            {
                Data = data;
                ConfigData = new byte[0];
            }
            else if (messageType == MessageType.TopVotersBenefitConfig)
            {
                Data = data;
                ConfigData = new byte[0];
            }
            else if (messageType == MessageType.PlayerTimeTracker)
            {
                Data = data;
                ConfigData = new byte[0];
            }

            Type = messageType;
        }

        public NexusMessage(int _fromServerId, int _toServerId, bool _isTestAnnouncement, NexusAPI.Server? _lobbyServerData, bool _requestLobbyServer, bool _isLobbyReply)
        {
            fromServerID = _fromServerId;
            toServerID = _toServerId;
            isTestAnnouncement = _isTestAnnouncement;
            requestLobbyServer = _requestLobbyServer;
            isLobbyReply = _isLobbyReply;
            lobbyServerData = _lobbyServerData;

            // Ponieważ ten konstruktor nie dotyczy wiadomości konfiguracyjnych, ustawiamy odpowiednie wartości domyślne.
            ConfigData = new byte[0];  // lub jakiekolwiek inne domyślne wartości, które są odpowiednie w kontekście
            Type = MessageType.BaseConfig; // lub inny domyślny typ wiadomości,
        }
    }
}
