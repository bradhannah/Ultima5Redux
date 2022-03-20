using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits
{
    [DataContract] public sealed class EmptyMapUnit : MapUnit
    {
        [IgnoreDataMember] public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;

        [IgnoreDataMember] public override string BoardXitName => "EMPTY";

        [IgnoreDataMember] public override string FriendlyName => BoardXitName;

        [IgnoreDataMember] public override bool IsActive => false;

        [IgnoreDataMember] public override bool IsAttackable => false;

        [IgnoreDataMember] public override TileReference NonBoardedTileReference => null;

        [IgnoreDataMember] public override TileReference KeyTileReference { get; set; } = null;

        [IgnoreDataMember]
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileName => null;

        [IgnoreDataMember] protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded => null;

        [IgnoreDataMember]
        protected override Dictionary<Point2D.Direction, string> FourDirectionToTileNameBoarded =>
            null;
    }
}