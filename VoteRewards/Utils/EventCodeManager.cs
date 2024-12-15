using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VoteRewards.Nexus;

namespace VoteRewards.Utils
{
    public class EventCodeManager
    {
        private List<EventCode> _eventCodes;
        private readonly string _filePath;
        private readonly VoteRewardsConfig _config;

        public EventCodeManager(string filePath, VoteRewardsConfig config)
        {
            _filePath = filePath;
            _config = config;
            LoadEventCodes();
        }

        public string CreateEventCode(int? maxUsageCount = null)
        {
            var newCode = GenerateUniqueCode();
            var newEventCode = new EventCode(newCode, maxUsageCount);
            if (!VoteRewardsMain.Instance.Config.UseDatabase)
            {
                NexusManager.SendEventCodeCreateToAllServers(newEventCode);
            }
            // Dodanie kodu do lokalnej listy
            _eventCodes.Add(newEventCode);

            // Zapisz kody zależnie od konfiguracji użycia bazy danych
            SaveEventCodes();

            return newCode;
        }


        private void LoadEventCodes()
        {
            if (_config.UseDatabase)
            {
                _eventCodes = VoteRewardsMain.Instance.DatabaseManager.LoadEventCodesAsync().Result;
            }
            else
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _eventCodes = JsonConvert.DeserializeObject<List<EventCode>>(json) ?? new List<EventCode>();
                }
                else
                {
                    _eventCodes = new List<EventCode>();
                }
            }
        }

        private void SaveEventCodes()
        {
            if (_config.UseDatabase)
            {
                // Zapisz do bazy danych
                VoteRewardsMain.Instance.DatabaseManager.SaveEventCodesAsync(_eventCodes).Wait();
            }
            else
            {
                // Zapisz do pliku, jeśli baza danych nie jest używana
                var json = JsonConvert.SerializeObject(_eventCodes, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
        }


        private string GenerateUniqueCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public bool CanBeRedeemed(string code, ulong steamId)
        {
            var eventCode = _eventCodes.FirstOrDefault(ec => ec.Code == code);

            if (eventCode != null)
            {
                if (eventCode.RedeemedBySteamIds.Contains(steamId))
                {
                    // Gracz już wykorzystał kod
                    return false;
                }

                // Jeśli MaxUsageCount jest null, kod może być używany bez ograniczeń (ale tylko raz dla danego SteamID)
                return eventCode.MaxUsageCount == null || eventCode.RedeemedBySteamIds.Count < eventCode.MaxUsageCount;
            }

            return false;
        }

        public bool RedeemCode(string code, ulong steamId)
        {
            var eventCode = _eventCodes.FirstOrDefault(ec => ec.Code == code);

            if (eventCode != null && !eventCode.RedeemedBySteamIds.Contains(steamId))
            {
                eventCode.RedeemedBySteamIds.Add(steamId);

                // Zmniejsz max_usage_count jeśli jest określone
                if (eventCode.MaxUsageCount.HasValue)
                {
                    eventCode.MaxUsageCount -= 1;
                }

                SaveEventCodes();

                // Usuwamy kod, jeśli osiągnięto limit użycia (max_usage_count == 0)
                if (eventCode.MaxUsageCount.HasValue && eventCode.MaxUsageCount.Value <= 0)
                {
                    _eventCodes.Remove(eventCode);
                    SaveEventCodes();
                }

                return true;
            }

            return false; // Kod nie może być wykorzystany
        }


        public void AddEventCode(EventCode newEventCode)
        {
            // Sprawdzenie, czy kod już istnieje
            if (!_eventCodes.Any(ec => ec.Code == newEventCode.Code))
            {
                _eventCodes.Add(newEventCode);
                SaveEventCodes();
            }
        }

    }
}
