using System.Collections.Generic;
using Ultima5Redux.Data;
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
        
        public override string BoardXitName => DataOvlRef.StringReferences.GetString(DataOvlReference.SleepTransportStrings.HORSE_N).Trim();

        public override bool IsActive => true;
        
        public Horse(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement, TileReferences tileReferences, 
            SmallMapReferences.SingleMapReference.Location location, DataOvlReference dataOvlReference) 
            : base(null, mapUnitState, null, mapUnitMovement, null,
                null, tileReferences, location, dataOvlReference)
            
        {
            
        }
    }
}