using System.Runtime.Serialization;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux
{
    [DataContract] public class GameStateBase
    {
        /// <summary>
        ///     All player character records
        /// </summary>
        [DataMember] public PlayerCharacterRecords CharacterRecords { get; protected set; }
        
        /// <summary>
        ///     The current time of day
        /// </summary>
        [DataMember] public TimeOfDay TheTimeOfDay { get; protected set; }

        [DataMember] public string GameDescription { get; protected set; }
    }
}