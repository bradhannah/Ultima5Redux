using System.Diagnostics;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    public class NonPlayerCharacter : MapUnit
    {
        public NonPlayerCharacter(NonPlayerCharacterReference npcRef, MapUnitState mapUnitState,
            SmallMapCharacterState smallMapTheSmallMapCharacterState, MapUnitMovement mapUnitMovement, TimeOfDay timeOfDay,
            PlayerCharacterRecords playerCharacterRecords, bool bLoadedFromDisk, TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location) : base(npcRef, mapUnitState,
            smallMapTheSmallMapCharacterState, mapUnitMovement, timeOfDay, playerCharacterRecords, bLoadedFromDisk,
            tileReferences, location)
        {
        }

        
        /// <summary>
        /// Is the map character currently an active character on the current map
        /// </summary>
        public override bool IsActive
        {
            get
            {
                // if they are in our party then we don't include them in the map 
                if (IsInParty) return false;

                // if they are in 0,0 then I am certain they are not real
                if (CurrentCharacterPosition.X == 0 && CurrentCharacterPosition.Y == 0) return false;

                // if there is a small map character state then we prefer to use it to determine if the 
                // unit is active
                Debug.Assert(TheSmallMapCharacterState != null);
                if (TheSmallMapCharacterState.MapUnitAnimationStateIndex != 0)
                {
                    return TheSmallMapCharacterState.Active;
                }
                return false;
            }
        }
    }
}