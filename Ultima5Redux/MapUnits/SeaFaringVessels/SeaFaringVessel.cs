using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;

namespace Ultima5Redux.MapUnits.SeaFaringVessels
{
    public abstract class SeaFaringVessel : MapUnit
    {
        protected SeaFaringVessel(SmallMapCharacterState smallMapTheSmallMapCharacterState,
            MapUnitMovement mapUnitMovement, SmallMapReferences.SingleMapReference.Location location, 
            Point2D.Direction direction, NonPlayerCharacterState npcState, MapUnitPosition mapUnitPosition)
            : base(smallMapTheSmallMapCharacterState, mapUnitMovement, location, direction, npcState, 
                GameReferences.SpriteTileReferences.GetTileReferenceByName("ShipSailsDown"), mapUnitPosition)
        {
        }

        public override bool IsActive => true;

        protected static int GetAdjustedPrice(PlayerCharacterRecords records, int nPrice)
        {
            return (int)(nPrice - nPrice * 0.015 * records.AvatarRecord.Stats.Intelligence);
        }
    }
}