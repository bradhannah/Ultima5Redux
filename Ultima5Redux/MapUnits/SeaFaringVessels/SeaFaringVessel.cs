using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.SeaFaringVessels
{
    public abstract class SeaFaringVessel : MapUnit
    {
         //protected abstract Dictionary<SmallMapReferences.SingleMapReference.Location, int> Prices { get; }

         protected static int GetAdjustedPrice(PlayerCharacterRecords records, int nPrice)
         {
             //int price = /Prices[location]; 
             return (int) (nPrice - (nPrice * 0.015 * records.AvatarRecord.Stats.Intelligence));
         }

        //  public SeaFaringVessel(MapUnitPosition position, VirtualMap.Direction direction)
        //  {
        // //     Position = position;
        //      Direction = direction;
        //  }
         
         public VirtualMap.Direction Direction { get; set; }

         protected SeaFaringVessel(MapUnitState mapUnitState, SmallMapCharacterState smallMapTheSmallMapCharacterState,
             MapUnitMovement mapUnitMovement, 
             bool bLoadedFromDisk, TileReferences tileReferences, SmallMapReferences.SingleMapReference.Location location) 
             : base(null, mapUnitState, smallMapTheSmallMapCharacterState, mapUnitMovement,
             null, null, bLoadedFromDisk, tileReferences, location)
         {
         }

         public override bool IsActive => true;
    }
}