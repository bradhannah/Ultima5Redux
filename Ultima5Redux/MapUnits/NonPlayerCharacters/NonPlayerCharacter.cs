using Ultima5Redux.DayNightMoon;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    public class NonPlayerCharacter : MapUnit
    {
        public NonPlayerCharacter(NonPlayerCharacterReference npcRef, MapUnitState mapUnitState,
            SmallMapCharacterState smallMapCharacterState, MapUnitMovement mapUnitMovement, TimeOfDay timeOfDay,
            PlayerCharacterRecords playerCharacterRecords, bool bLoadedFromDisk) : base(npcRef, mapUnitState,
            smallMapCharacterState, mapUnitMovement, timeOfDay, playerCharacterRecords, bLoadedFromDisk)
        {
        }
    }
}