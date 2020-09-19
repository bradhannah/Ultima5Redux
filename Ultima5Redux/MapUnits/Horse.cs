using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits
{
    public class Horse :  MapUnit
    {
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