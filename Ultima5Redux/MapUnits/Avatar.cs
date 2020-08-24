using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    public class Avatar : MapUnit
    {
        // public Avatar(NonPlayerCharacterReference npcRef, MapUnitState mapUnitState,
        //     SmallMapCharacterState smallMapTheSmallMapCharacterState, MapUnitMovement mapUnitMovement,
        //     TimeOfDay timeOfDay, PlayerCharacterRecords playerCharacterRecords, bool bLoadedFromDisk,
        //     TileReferences tileReferences, SmallMapReferences.SingleMapReference.Location location) :
        //     base(npcRef, mapUnitState, smallMapTheSmallMapCharacterState,
        //     mapUnitMovement, timeOfDay, playerCharacterRecords, bLoadedFromDisk, tileReferences, location)
        // {
        // }

        public Avatar(TileReferences tileReferences, CharacterPosition avatarPosition, SmallMapReferences.SingleMapReference.Location location) : base()
        {
            TheMapUnitState = MapUnitState.CreateAvatar(tileReferences,
                SmallMapReferences.GetStartingXYZByLocation(location));
        }

        public override bool IsActive => true;
    }
}