using System.Collections.Generic;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits
{
    public class EmptyMapUnit : MapUnit
    {
        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;

        public override string BoardXitName => "EMPTY";

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } = null;
        //= new Dictionary<Point2D.Direction, string>();

        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } = null;
        //new Dictionary<Point2D.Direction, string>();

        protected override Dictionary<Point2D.Direction, string> FourDirectionToTileNameBoarded { get; } = null;

        public override string FriendlyName => BoardXitName;

        public override bool IsActive => false;

        public override bool IsAttackable => false;

        public override TileReference KeyTileReference { get; set; } = null;

        public override TileReference NonBoardedTileReference { get; } = null;
    }
}