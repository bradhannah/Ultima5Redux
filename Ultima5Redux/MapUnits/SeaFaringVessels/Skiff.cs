using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;

namespace Ultima5Redux.MapUnits.SeaFaringVessels
{
    public sealed class Skiff : SeaFaringVessel
    {
        private static Dictionary<SmallMapReferences.SingleMapReference.Location, int> Prices { get; } =
            new Dictionary<SmallMapReferences.SingleMapReference.Location, int>
            {
                { SmallMapReferences.SingleMapReference.Location.East_Britanny, 250 },
                { SmallMapReferences.SingleMapReference.Location.Minoc, 350 },
                { SmallMapReferences.SingleMapReference.Location.Buccaneers_Den, 200 },
                { SmallMapReferences.SingleMapReference.Location.Jhelom, 400 }
            };

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Skiff;

        public override string BoardXitName => GameReferences.DataOvlRef.StringReferences
            .GetString(DataOvlReference.SleepTransportStrings.SKIFF_N).Trim();

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } =
            new Dictionary<Point2D.Direction, string>
            {
                { Point2D.Direction.None, "SkiffLeft" },
                { Point2D.Direction.Left, "SkiffLeft" },
                { Point2D.Direction.Down, "SkiffDown" },
                { Point2D.Direction.Right, "SkiffRight" },
                { Point2D.Direction.Up, "SkiffUp" }
            };

        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded => DirectionToTileName;

        public override string FriendlyName => BoardXitName;

        public override bool IsAttackable => false;

        public Skiff(MapUnitMovement mapUnitMovement, SmallMapReferences.SingleMapReference.Location location,
            Point2D.Direction direction, NonPlayerCharacterState npcState, MapUnitPosition mapUnitPosition) : base(null,
            mapUnitMovement, location, direction, npcState, mapUnitPosition)
        {
            KeyTileReference = NonBoardedTileReference;
        }

        public static int GetPrice(SmallMapReferences.SingleMapReference.Location location,
            PlayerCharacterRecords records)
        {
            return GetAdjustedPrice(records, Prices[location]);
        }

        public override bool CanBeExited(VirtualMap virtualMap) =>
            (virtualMap.IsLandNearby(Avatar.AvatarState.Regular));
    }
}