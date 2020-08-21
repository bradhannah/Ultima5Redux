using System;
using System.Diagnostics;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapCharacters
{
    /// <summary>
    /// Based on original U5 data structure
    /// tracks the position and state of the map character
    /// </summary>
    public class MapCharacterState
    {
        #region Public Properties
        public CharacterPosition TheCharacterPosition { get; } = new CharacterPosition();
        public int CharacterAnimationStateIndex { get; }
        public int NPCIndex { get; }
        TileReference TileRef { get; }
        public bool Active { get; }
        #endregion

        #region Constructors
        /// <summary>
        /// Build the character state based on existing conditions
        /// This is called when entering a new map within the game - as opposed to loading from disk
        /// </summary>
        /// <param name="tileReferences"></param>
        /// <param name="npcRef"></param>
        /// <param name="nCharacterAnimationStateIndex"></param>
        /// <param name="timeOfDay"></param>
        public MapCharacterState(TileReferences tileReferences, NonPlayerCharacterReference npcRef, int nCharacterAnimationStateIndex, TimeOfDay timeOfDay)
        {
            NPCIndex = npcRef.DialogIndex;
            TileRef = tileReferences.GetTileReference(npcRef.NPCKeySprite);
            CharacterAnimationStateIndex = nCharacterAnimationStateIndex;
            // if you are adding by hand then we can assume that the character is active
            TheCharacterPosition = npcRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);
            Active = true;
        }

        /// <summary>
        /// Build the character state from data retrieved from disk
        /// </summary>
        /// <param name="tileReferences"></param>
        /// <param name="stateUInts"></param>
        /// <param name="nNPCIndex"></param>
        public MapCharacterState(TileReferences tileReferences, UInt16[] stateUInts, int nNPCIndex)
        {
            Debug.Assert(stateUInts.Length == 0x8);
            NPCIndex = nNPCIndex;
            TheCharacterPosition.X = stateUInts[1];
            TheCharacterPosition.Y = stateUInts[2];
            TheCharacterPosition.Floor = stateUInts[3];
            TileRef = tileReferences.GetTileReference(stateUInts[4]+0x100);
            CharacterAnimationStateIndex = stateUInts[6];
            Active = stateUInts[7]>0;
        }
        #endregion
    }
}
