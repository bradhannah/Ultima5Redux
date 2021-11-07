using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.SeaFaringVessels
{
    public abstract class SeaFaringVessel : MapUnit
    {
        protected SeaFaringVessel(MapUnitState mapUnitState, SmallMapCharacterState smallMapTheSmallMapCharacterState,
            MapUnitMovement mapUnitMovement, TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location,
            DataOvlReference dataOvlReference, Point2D.Direction direction, NonPlayerCharacterState npcState)
            : base(mapUnitState, smallMapTheSmallMapCharacterState, mapUnitMovement, tileReferences,
                location, dataOvlReference, direction, npcState)
        {
        }

        public override bool IsActive => true;

        protected static int GetAdjustedPrice(PlayerCharacterRecords records, int nPrice)
        {
            return (int)(nPrice - nPrice * 0.015 * records.AvatarRecord.Stats.Intelligence);
        }
    }
}