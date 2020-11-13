using System.Diagnostics;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    /// <summary>
    ///     Based on original U5 data structure
    ///     tracks the position and state of the map character
    /// </summary>
    public class SmallMapCharacterState
    {
        /// <summary>
        ///     Build the character state based on existing conditions
        ///     This is called when entering a new map within the game - as opposed to loading from disk
        /// </summary>
        /// <param name="tileReferences"></param>
        /// <param name="npcRef"></param>
        /// <param name="nMapUnitAnimationStateIndex"></param>
        /// <param name="timeOfDay"></param>
        public SmallMapCharacterState(TileReferences tileReferences, NonPlayerCharacterReference npcRef,
            int nMapUnitAnimationStateIndex, TimeOfDay timeOfDay)
        {
            NPCIndex = npcRef.DialogIndex;
            TileRef = tileReferences.GetTileReference(npcRef.NPCKeySprite);
            MapUnitAnimationStateIndex = nMapUnitAnimationStateIndex;
            // if you are adding by hand then we can assume that the character is active
            TheMapUnitPosition = npcRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);
            Active = !(TheMapUnitPosition.X == 0 && TheMapUnitPosition.Y == 0);
        }

        /// <summary>
        ///     Create a blank SmallMapCharacterState indicating no character
        /// </summary>
        public SmallMapCharacterState()
        {
            Active = false;
        }

        /// <summary>
        ///     Build the character state from data retrieved from disk
        /// </summary>
        /// <param name="tileReferences"></param>
        /// <param name="stateUInts"></param>
        /// <param name="nNPCIndex"></param>
        public SmallMapCharacterState(TileReferences tileReferences, ushort[] stateUInts, int nNPCIndex)
        {
            Debug.Assert(stateUInts.Length == 0x8);
            NPCIndex = nNPCIndex;
            TheMapUnitPosition.X = stateUInts[1];
            TheMapUnitPosition.Y = stateUInts[2];
            TheMapUnitPosition.Floor = stateUInts[3];
            TileRef = tileReferences.GetTileReference(stateUInts[4] + 0x100);
            MapUnitAnimationStateIndex = stateUInts[6];
            Active = stateUInts[7] > 0;
        }

        public MapUnitPosition TheMapUnitPosition { get; } = new MapUnitPosition();
        public int MapUnitAnimationStateIndex { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private int NPCIndex { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private TileReference TileRef { get; }
        public bool Active { get; }
    }
}