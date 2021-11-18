using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References;

namespace Ultima5Redux.MapUnits
{
    public sealed class MagicCarpet : MapUnit
    {
        private const string REGULAR_CARPET_STR = "Carpet2";

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Carpet;

        public override string BoardXitName => GameReferences.DataOvlRef.StringReferences
            .GetString(DataOvlReference.SleepTransportStrings.CARPET_N).Trim();

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } =
            new Dictionary<Point2D.Direction, string>
            {
                { Point2D.Direction.None, REGULAR_CARPET_STR },
                { Point2D.Direction.Left, REGULAR_CARPET_STR },
                { Point2D.Direction.Down, REGULAR_CARPET_STR },
                { Point2D.Direction.Right, REGULAR_CARPET_STR },
                { Point2D.Direction.Up, REGULAR_CARPET_STR }
            };

        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } =
            new Dictionary<Point2D.Direction, string>
            {
                { Point2D.Direction.None, "RidingMagicCarpetLeft" },
                { Point2D.Direction.Left, "RidingMagicCarpetLeft" },
                { Point2D.Direction.Down, "RidingMagicCarpetLeft" },
                { Point2D.Direction.Right, "RidingMagicCarpetRight" },
                { Point2D.Direction.Up, "RidingMagicCarpetRight" }
            };

        protected override Dictionary<Point2D.Direction, string> FourDirectionToTileNameBoarded =>
            new Dictionary<Point2D.Direction, string>
            {
                { Point2D.Direction.None, "RidingMagicCarpetLeft" },
                { Point2D.Direction.Left, "RidingMagicCarpetLeft" },
                { Point2D.Direction.Down, "RidingMagicCarpetDown" },
                { Point2D.Direction.Right, "RidingMagicCarpetRight" },
                { Point2D.Direction.Up, "RidingMagicCarpetUp" }
            };

        public override string FriendlyName => BoardXitName;

        public override bool IsActive => true;

        public override bool IsAttackable => false;

        public MagicCarpet(SmallMapReferences.SingleMapReference.Location location,
            Point2D.Direction direction, NonPlayerCharacterState npcState, MapUnitPosition mapUnitPosition) :
            base(null, new MapUnitMovement(0), location, direction, npcState,
                GameReferences.SpriteTileReferences.GetTileReferenceByName(REGULAR_CARPET_STR), mapUnitPosition)
        {
            KeyTileReference = NonBoardedTileReference;
        }

        public override bool CanBeExited(VirtualMap virtualMap) => (virtualMap.IsLandNearby());
    }
}