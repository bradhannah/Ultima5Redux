using System.Collections.Generic;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits
{
    public class EmptyMapUnit : MapUnit
    {
        protected override Dictionary<VirtualMap.Direction, string> DirectionToTileName { get; }
            = new Dictionary<VirtualMap.Direction, string>();

        protected override Dictionary<VirtualMap.Direction, string> DirectionToTileNameBoarded { get; } =
            new Dictionary<VirtualMap.Direction, string>();

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;

        public override string BoardXitName => "EMPTY";

        public override bool IsActive => false;
    }
}