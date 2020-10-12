using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.SeaFaringVessels
{
    public abstract class SeaFaringVessel : MapUnit
    {
         //protected abstract Dictionary<SmallMapReferences.SingleMapReference.Location, int> Prices { get; }

         protected static int GetAdjustedPrice(PlayerCharacterRecords records, int nPrice)
         {
             return (int) (nPrice - (nPrice * 0.015 * records.AvatarRecord.Stats.Intelligence));
         }
         
         protected SeaFaringVessel(MapUnitState mapUnitState, SmallMapCharacterState smallMapTheSmallMapCharacterState,
             MapUnitMovement mapUnitMovement, TileReferences tileReferences, SmallMapReferences.SingleMapReference.Location location,
             DataOvlReference dataOvlReference, VirtualMap.Direction direction) 
             : base(null, mapUnitState, smallMapTheSmallMapCharacterState, mapUnitMovement,
             null, null, tileReferences, location, dataOvlReference, direction)
         {
         }

         public override bool IsActive => true;
    }
}
