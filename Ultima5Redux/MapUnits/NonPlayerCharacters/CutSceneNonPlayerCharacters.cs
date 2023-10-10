using System.Collections.Generic;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    public sealed class CutSceneNonPlayerCharacter : MapUnit
    {
        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Regular;
        public override string BoardXitName => "";
        public override string FriendlyName => "";
        public override bool IsActive => _bActive;
        public override bool IsAttackable => false;
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded => new();
        protected override Dictionary<Point2D.Direction, string> DirectionToTileName => new();

        private bool _bActive;

        public void SetActive(bool bActive) {
            _bActive = bActive;
        }

        public CutSceneNonPlayerCharacter(bool bActive, TileReference tileReference) {
            SetActive(bActive);
            KeyTileReference = tileReference;
        }
    }
}