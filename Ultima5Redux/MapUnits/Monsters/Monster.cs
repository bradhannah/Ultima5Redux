using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.Monsters
{
    public class Monster : MapUnit
    {
        public Monster(NonPlayerCharacterReference npcRef, MapUnitState mapUnitState,
            SmallMapCharacterState smallMapTheSmallMapCharacterState, MapUnitMovement mapUnitMovement, TimeOfDay timeOfDay,
            PlayerCharacterRecords playerCharacterRecords, TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location) : base(npcRef, mapUnitState,
            smallMapTheSmallMapCharacterState, mapUnitMovement, timeOfDay, playerCharacterRecords, tileReferences,
            location)
        {
        }

        public override TileReference GetTileReferenceWithAvatarOnTile(VirtualMap.Direction direction)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsActive => true;
    }
}