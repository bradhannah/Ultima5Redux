using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits
{
    public class MagicCarpet : MapUnit
    {
        public override TileReference GetTileReferenceWithAvatarOnTile(VirtualMap.Direction direction)
        {
            if (direction == VirtualMap.Direction.Left)
                return TileReferences.GetTileReferenceByName("RidingMagicCarpetLeft");
            return TileReferences.GetTileReferenceByName("RidingMagicCarpetRight");
        }

        public override bool IsActive => true;

        public MagicCarpet(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement,
            TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location) 
            : base(null, mapUnitState, null, mapUnitMovement, null,
                null, tileReferences, location)
            
        {
            
        }
    }
}