using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits
{
    public class MagicCarpet : MapUnit
    {
        public MagicCarpet(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement, TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location, DataOvlReference dataOvlReference,
            Point2D.Direction direction) : base(null, mapUnitState, null, 
            mapUnitMovement, null, tileReferences,
            location, dataOvlReference, direction)
        {
        }

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } =
            new Dictionary<Point2D.Direction, string>
            {
                {Point2D.Direction.None, "Carpet2"},
                {Point2D.Direction.Left, "Carpet2"},
                {Point2D.Direction.Down, "Carpet2"},
                {Point2D.Direction.Right, "Carpet2"},
                {Point2D.Direction.Up, "Carpet2"}
            };

        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } =
            new Dictionary<Point2D.Direction, string>
            {
                {Point2D.Direction.None, "RidingMagicCarpetLeft"},
                {Point2D.Direction.Left, "RidingMagicCarpetLeft"},
                {Point2D.Direction.Down, "RidingMagicCarpetLeft"},
                {Point2D.Direction.Right, "RidingMagicCarpetRight"},
                {Point2D.Direction.Up, "RidingMagicCarpetRight"}
            };
        
        protected override Dictionary<Point2D.Direction, string> FourDirectionToTileNameBoarded  =>
            new Dictionary<Point2D.Direction, string>
            {
                {Point2D.Direction.None, "RidingMagicCarpetLeft"},
                {Point2D.Direction.Left, "RidingMagicCarpetLeft"},
                {Point2D.Direction.Down, "RidingMagicCarpetDown"},
                {Point2D.Direction.Right, "RidingMagicCarpetRight"},
                {Point2D.Direction.Up, "RidingMagicCarpetUp"}
            };

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Carpet;

        public override bool CanBeExited(VirtualMap virtualMap) => (virtualMap.IsLandNearby());

        public override string BoardXitName => DataOvlRef.StringReferences
            .GetString(DataOvlReference.SleepTransportStrings.CARPET_N).Trim();

        public override bool IsActive => true;
    }
}