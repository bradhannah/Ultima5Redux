using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits
{
    [DataContract] public sealed class MagicCarpet : MapUnit
    {
        private const string REGULAR_CARPET_STR = "Carpet2";

        [IgnoreDataMember] public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Carpet;

        [IgnoreDataMember]
        public override string BoardXitName => GameReferences.DataOvlRef.StringReferences
            .GetString(DataOvlReference.SleepTransportStrings.CARPET_N).Trim();

        [IgnoreDataMember] public override string FriendlyName => BoardXitName;

        [IgnoreDataMember] public override bool IsActive => true;

        [IgnoreDataMember] public override bool IsAttackable => false;

        [IgnoreDataMember]
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } =
            new()
            {
                { Point2D.Direction.None, REGULAR_CARPET_STR },
                { Point2D.Direction.Left, REGULAR_CARPET_STR },
                { Point2D.Direction.Down, REGULAR_CARPET_STR },
                { Point2D.Direction.Right, REGULAR_CARPET_STR },
                { Point2D.Direction.Up, REGULAR_CARPET_STR }
            };

        [IgnoreDataMember]
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } =
            new()
            {
                { Point2D.Direction.None, "RidingMagicCarpetLeft" },
                { Point2D.Direction.Left, "RidingMagicCarpetLeft" },
                { Point2D.Direction.Down, "RidingMagicCarpetLeft" },
                { Point2D.Direction.Right, "RidingMagicCarpetRight" },
                { Point2D.Direction.Up, "RidingMagicCarpetRight" }
            };

        [IgnoreDataMember]
        protected internal override Dictionary<Point2D.Direction, string> FourDirectionToTileNameBoarded =>
            new()
            {
                { Point2D.Direction.None, "RidingMagicCarpetLeft" },
                { Point2D.Direction.Left, "RidingMagicCarpetLeft" },
                { Point2D.Direction.Down, "RidingMagicCarpetDown" },
                { Point2D.Direction.Right, "RidingMagicCarpetRight" },
                { Point2D.Direction.Up, "RidingMagicCarpetUp" }
            };

        [JsonConstructor] private MagicCarpet()
        {
        }

        public MagicCarpet(SmallMapReferences.SingleMapReference.Location location, Point2D.Direction direction,
            NonPlayerCharacterState npcState, MapUnitPosition mapUnitPosition) : base(null, new MapUnitMovement(0),
            location, direction, npcState,
            GameReferences.SpriteTileReferences.GetTileReferenceByName(REGULAR_CARPET_STR), mapUnitPosition)
        {
            KeyTileReference = GetNonBoardedTileReference();
        }

        public override bool CanBeExited(VirtualMap virtualMap) => (virtualMap.IsLandNearby());
    }
}