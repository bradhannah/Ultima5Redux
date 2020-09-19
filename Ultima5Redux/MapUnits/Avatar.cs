using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    public class Avatar : MapUnit
    {
        public Avatar(TileReferences tileReferences, MapUnitPosition avatarPosition, 
            SmallMapReferences.SingleMapReference.Location location, MapUnitMovement movement,
            MapUnitState mapUnitState = null) : base()
        {
            if (mapUnitState == null)
            {
                TheMapUnitState = MapUnitState.CreateAvatar(tileReferences, SmallMapReferences.GetStartingXYZByLocation(location));
            }
            else
            {
                TheMapUnitState = MapUnitState.CreateAvatar(tileReferences, SmallMapReferences.GetStartingXYZByLocation(location),
                    mapUnitState);
            }
            TileReferences = tileReferences;
            //TheMapUnitState = mapUnitState;
            // set the initial key tile
            // KeyTileReference = TheMapUnitState.Tile1Ref;
            // _ = KeyTileReference;
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
            SmallMapReferences.SingleMapReference.Location location, MapUnitMovement movement,
            MapUnitState mapUnitState = null)
        {
            Avatar theAvatar = new Avatar(tileReferences, SmallMapReferences.GetStartingXYZByLocation(location), 
                location, movement, mapUnitState);
            
            
            
            return theAvatar;
        }

        /// <summary>
        /// Show the Avatar that isn't boarded on top of anything
        /// </summary>
        public void SetUnboardedAvatar()
        {
            KeyTileReference = TileReferences.GetTileReferenceByName("BasicAvatar");
        }
        
        public void SetBoardedCarpet()
        {
            KeyTileReference = TileReferences.GetTileReferenceByName("RidingMagicCarpetRight");
        }

        public void SetBoardedHorse()
        {
            KeyTileReference = TileReferences.GetTileReferenceByName("RidingHorseRight");
        }

        public void SetBoardedFrigate(VirtualMap.Direction direction, bool bSailsUp = false)
        {
            KeyTileReference = TileReferences.GetTileReferenceByName("ShipNoSailsRight");
        }

        public void SetBoardedSkiff()
        {
            KeyTileReference = TileReferences.GetTileReferenceByName("SkiffRight");
        }
    }
}