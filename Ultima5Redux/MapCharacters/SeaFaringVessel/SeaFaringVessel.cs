using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapCharacters.SeaFaringVessel
{
    public abstract class SeaFaringVessel : MapUnit
    {
         //protected abstract Dictionary<SmallMapReferences.SingleMapReference.Location, int> Prices { get; }

         protected static int GetAdjustedPrice(PlayerCharacterRecords records, int nPrice)
         {
             //int price = /Prices[location]; 
             return (int) (nPrice - (nPrice * 0.015 * records.AvatarRecord.Stats.Intelligence));
         }

        //  public SeaFaringVessel(CharacterPosition position, VirtualMap.Direction direction)
        //  {
        // //     Position = position;
        //      Direction = direction;
        //  }
         
      //   public CharacterPosition Position { get; set; }
         public VirtualMap.Direction Direction { get; set; }

         protected SeaFaringVessel(MapUnitState mapUnitState, SmallMapCharacterState smallMapCharacterState,
             MapUnitMovement mapUnitMovement, 
             bool bLoadedFromDisk) : base(null, mapUnitState, smallMapCharacterState, mapUnitMovement,
             null, null, bLoadedFromDisk)
         {
         }
    }
}