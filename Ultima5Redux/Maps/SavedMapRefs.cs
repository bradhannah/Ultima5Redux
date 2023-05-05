using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Ultima5Redux.MapUnits;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract] public class SavedMapRefs
    {
        [DataMember] public int Floor { get; private set; }
        [DataMember] public SmallMapReferences.SingleMapReference.Location Location { get; private set; }
        [DataMember] public Map.Maps MapType { get; private set; }

        // this is loaded, and only re-set before a save
        [DataMember] public MapUnitPosition MapUnitPosition { get; private set; } = new();

        internal SavedMapRefs Copy() =>
            new()
            {
                Location = Location,
                Floor = Floor,
                MapType = MapType
            };

        private void SetMapUnitPosition(Point2D playerPosition, int nFloor)
        {
            if (playerPosition == null)
            {
                MapUnitPosition.X = 0;
                MapUnitPosition.Y = 0;
            }
            else
            {
                MapUnitPosition.X = playerPosition.X;
                MapUnitPosition.Y = playerPosition.Y;
            }

            MapUnitPosition.Floor = nFloor;
            
        }

        public SmallMapReferences.SingleMapReference GetSingleMapReference() =>
            Location switch
            {
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld => 
                    GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, Floor), 
                SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine => SmallMapReferences.SingleMapReference.GetCombatMapSingleInstance(),
                _ => GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(Location, Floor)
            };

        public void SetByLargeMapType(LargeMapLocationReferences.LargeMapType largeMapType, Point2D playerPosition)
        {
            Location = SmallMapReferences.SingleMapReference.Location.Britannia_Underworld;
            Floor = largeMapType == LargeMapLocationReferences.LargeMapType.Overworld ? 0 : -1;
            MapType = largeMapType == LargeMapLocationReferences.LargeMapType.Overworld
                ? Map.Maps.Overworld
                : Map.Maps.Underworld;
            SetMapUnitPosition(playerPosition, Floor);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        public void SetBySingleCombatMapReference(SingleCombatMapReference singleCombatMapReference)
        {
            // not entirely sure we need to actually save much here since we never actually save games
            // while in combat
            Location = SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine;
            Floor = 0;
            MapType = Map.Maps.Combat;
        }

        public void SetBySingleDungeonMapFloorReference(SingleDungeonMapFloorReference singleDungeonMapFloorReference,
            Point2D playerPosition)
        {
            Location = singleDungeonMapFloorReference.DungeonLocation;
            Floor = singleDungeonMapFloorReference.DungeonFloor;
            MapType = Map.Maps.Dungeon;
            SetMapUnitPosition(playerPosition, Floor);
        }

        public void SetBySingleMapReference(SmallMapReferences.SingleMapReference singleMapReference,
            Point2D playerPosition)
        {
            Location = singleMapReference.MapLocation;
            Floor = singleMapReference.Floor;
            MapType = singleMapReference.MapType;
            SetMapUnitPosition(playerPosition ?? Point2D.Zero, Floor);
            MapUnitPosition.Floor = singleMapReference.Floor;
        }
    }
}