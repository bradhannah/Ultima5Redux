using Newtonsoft.Json;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.External;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.SeaFaringVessels
{
    public abstract class SeaFaringVessel : MapUnit
    {
        public override bool IsActive => true;

        [JsonConstructor] protected SeaFaringVessel()
        {
        }

        protected SeaFaringVessel(SmallMapCharacterState smallMapTheSmallMapCharacterState,
            MapUnitMovement mapUnitMovement, SmallMapReferences.SingleMapReference.Location location,
            Point2D.Direction direction, NonPlayerCharacterState npcState, MapUnitPosition mapUnitPosition) : base(
            smallMapTheSmallMapCharacterState, mapUnitMovement, location, direction, npcState,
            GameReferences.SpriteTileReferences.GetTileReferenceByName("ShipSailsDown"), mapUnitPosition)
        {
        }

        internal override void CompleteNextMove(VirtualMap virtualMap, TimeOfDay timeOfDay, AStar aStar)
        {
            // by default the thing doesn't move on it's own
        }

        protected static int GetAdjustedPrice(PlayerCharacterRecords records, int nPrice) =>
            (int)(nPrice - nPrice * 0.015 * records.AvatarRecord.Stats.Intelligence);
    }
}