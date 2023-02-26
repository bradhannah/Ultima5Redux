using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    /// <summary>
    ///     Based on original U5 data structure
    ///     tracks the position and state of the map character
    /// </summary>
    [DataContract]
    public class SmallMapCharacterState
    {
        [DataMember] private int NpcIndex { get; set; }
        [DataMember] public bool Active { get; private set; }

        [DataMember] public MapUnitPosition TheMapUnitPosition { get; private set; } = new();

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        [IgnoreDataMember] private int MapUnitAnimationStateIndex { get; }

        /// <summary>
        ///     Build the character state based on existing conditions
        ///     This is called when entering a new map within the game - as opposed to loading from disk
        /// </summary>
        /// <param name="npcRef"></param>
        /// <param name="nMapUnitAnimationStateIndex"></param>
        public SmallMapCharacterState(NonPlayerCharacterReference npcRef, int nMapUnitAnimationStateIndex)
        {
            NpcIndex = npcRef.DialogIndex;
            MapUnitAnimationStateIndex = nMapUnitAnimationStateIndex;
            // if you are adding by hand then we can assume that the character is active
            TheMapUnitPosition =
                npcRef.Schedule.GetCharacterDefaultPositionByTime(GameStateReference.State.TheTimeOfDay);
            Active = !(TheMapUnitPosition.X == 0 && TheMapUnitPosition.Y == 0);
        }

        /// <summary>
        ///     Create a blank SmallMapCharacterState indicating no character
        /// </summary>
        [JsonConstructor]
        public SmallMapCharacterState() => Active = false;

        /// <summary>
        ///     Build the character state from data retrieved from disk
        /// </summary>
        /// <param name="stateUInts"></param>
        /// <param name="nNpcIndex"></param>
        public SmallMapCharacterState(ushort[] stateUInts, int nNpcIndex)
        {
            Debug.Assert(stateUInts.Length == 0x8);
            MapUnitAnimationStateIndex = stateUInts[6];
            NpcIndex = nNpcIndex;
            TheMapUnitPosition.X = stateUInts[1];
            TheMapUnitPosition.Y = stateUInts[2];
            TheMapUnitPosition.Floor = stateUInts[3];
            Active = !(TheMapUnitPosition.X == 0 && TheMapUnitPosition.Y == 0);
        }
    }
}