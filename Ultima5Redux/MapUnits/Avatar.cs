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

        public Avatar(TileReferences tileReferences, MapUnitPosition avatarPosition, 
            SmallMapReferences.SingleMapReference.Location location, MapUnitMovement movement) : base()
        {
            TheMapUnitState = MapUnitState.CreateAvatar(tileReferences, SmallMapReferences.GetStartingXYZByLocation(location));
            Movement = movement;
        }

        public override bool IsActive => true;
        /// <summary>
        /// Creates an Avatar MapUnit at the default small map position
        /// Note: this should never need to be called from a LargeMap since the values persist on disk
        /// </summary>
        /// <param name="tileReferences"></param>
        /// <param name="location"></param>
        /// <param name="movement"></param>
        /// <returns></returns>
        public static MapUnit CreateAvatar(TileReferences tileReferences, 
            SmallMapReferences.SingleMapReference.Location location, MapUnitMovement movement)
        {
            Avatar theAvatar = new Avatar(tileReferences, SmallMapReferences.GetStartingXYZByLocation(location), 
                location, movement);

            return theAvatar;
        }
    }
}