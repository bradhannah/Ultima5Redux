using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits
{
    public class Horse :  MapUnit
    {
        public override TileReference GetTileReferenceWithAvatarOnTile(VirtualMap.Direction direction)
        {
            if (direction == VirtualMap.Direction.Left)
                return TileReferences.GetTileReferenceByName("RidingHorseLeft");
            return TileReferences.GetTileReferenceByName("RidingHorseRight");
        }


        public override bool IsActive => true;
        
        public Horse(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement,
            TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location) 
            : base(null, mapUnitState, null, mapUnitMovement, null,
                null, tileReferences, location)
            
        {
            
        }
    }
}