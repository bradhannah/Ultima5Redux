using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.MapUnits;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux
{
    [DataContract] public class GameSummary
    {

        [DataMember] public string GameDescription
        {
            get
            {
                string description =
                    $"Last Saved: {TheExtraSaveData.LastWrite.ToLongDateString()} {TheExtraSaveData.LastWrite.ToShortTimeString()}\n\n";
                description += $"{TheTimeOfDay.FormattedDate} {TheTimeOfDay.FormattedTime}\n\n";
                description += $"{TheExtraSaveData.FriendlyLocationName}\n";
                if (TheExtraSaveData.CurrentMapPosition != null)
                    description += $"{TheExtraSaveData.CurrentMapPosition.FriendlyString}\n\n";

                return description;
            }
        }

        [DataMember] public PlayerCharacterRecords CharacterRecords { get; protected internal set; }

        [DataMember] public ExtraSaveData TheExtraSaveData { get; protected internal set; } = new();

        [DataMember] public TimeOfDay TheTimeOfDay { get; protected internal set; }

        public string SerializeGameSummary()
        {
            JsonSerializerSettings jss = new()
            {
                Formatting = Formatting.Indented,
                // PreserveReferencesHandling = PreserveReferencesHandling.All,
                // TypeNameHandling = TypeNameHandling.All
            };
            string stateJson = JsonConvert.SerializeObject(this, jss);
            return stateJson;
        }

        public static GameSummary DeserializeGameSummary(string gameSummaryJson)
        {
            GameSummary gameStateSummary = JsonConvert.DeserializeObject<GameSummary>(gameSummaryJson);
            return gameStateSummary;
        }

        [DataContract] public class ExtraSaveData
        {
            [DataMember] public MapUnitPosition CurrentMapPosition { get; internal set; }
            [DataMember] public string FriendlyLocationName { get; internal set; }
            [DataMember] public DateTime LastWrite { get; internal set; }
            [DataMember] public string SavedDirectory { get; protected internal set; }
        }
    }
}