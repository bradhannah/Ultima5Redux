using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.Monsters
{
    public abstract class Monster : MapUnit
    {
        public Monster(NonPlayerCharacterReference npcRef, MapUnitState mapUnitState,
            SmallMapCharacterState smallMapTheSmallMapCharacterState, MapUnitMovement mapUnitMovement, TimeOfDay timeOfDay,
            PlayerCharacterRecords playerCharacterRecords, TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location, DataOvlReference dataOvlReference) : base(npcRef, mapUnitState,
            smallMapTheSmallMapCharacterState, mapUnitMovement, timeOfDay, playerCharacterRecords, tileReferences,
            location, dataOvlReference, VirtualMap.Direction.None)
        {
        }

        public override string BoardXitName => "Hostile creates don't not like to be boarded!";

        public override bool IsActive => true;
    }
}