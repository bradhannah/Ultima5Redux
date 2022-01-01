using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits
{
    [DataContract] public sealed class Horse : MapUnit
    {
        [IgnoreDataMember]
        private static Dictionary<SmallMapReferences.SingleMapReference.Location, int> Prices { get; } =
            new()
            {
                { SmallMapReferences.SingleMapReference.Location.Trinsic, 200 },
                { SmallMapReferences.SingleMapReference.Location.Paws, 320 },
                { SmallMapReferences.SingleMapReference.Location.Buccaneers_Den, 260 }
            };

        [IgnoreDataMember] public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Horse;

        [IgnoreDataMember]
        public override string BoardXitName => GameReferences.DataOvlRef.StringReferences
            .GetString(DataOvlReference.SleepTransportStrings.HORSE_N).Trim();

        [IgnoreDataMember] public override string FriendlyName => BoardXitName;

        [IgnoreDataMember] public override bool IsActive => true;

        [IgnoreDataMember] public override bool IsAttackable => false;

        [IgnoreDataMember]
        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } =
            new()
            {
                { Point2D.Direction.None, "HorseLeft" },
                { Point2D.Direction.Left, "HorseLeft" },
                { Point2D.Direction.Down, "HorseLeft" },
                { Point2D.Direction.Right, "HorseRight" },
                { Point2D.Direction.Up, "HorseRight" }
            };

        [IgnoreDataMember]
        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } =
            new()
            {
                { Point2D.Direction.None, "RidingHorseLeft" },
                { Point2D.Direction.Left, "RidingHorseLeft" },
                { Point2D.Direction.Down, "RidingHorseLeft" },
                { Point2D.Direction.Right, "RidingHorseRight" },
                { Point2D.Direction.Up, "RidingHorseRight" }
            };

        [IgnoreDataMember]
        protected override Dictionary<Point2D.Direction, string> FourDirectionToTileNameBoarded =>
            new()
            {
                { Point2D.Direction.None, "RidingHorseLeft" },
                { Point2D.Direction.Left, "RidingHorseLeft" },
                { Point2D.Direction.Down, "RidingHorseDown" },
                { Point2D.Direction.Right, "RidingHorseRight" },
                { Point2D.Direction.Up, "RidingHorseUp" }
            };

        [JsonConstructor] private Horse()
        {
        }

        public Horse(MapUnitMovement mapUnitMovement, SmallMapReferences.SingleMapReference.Location location,
            Point2D.Direction direction, NonPlayerCharacterState npcState, MapUnitPosition mapUnitPosition) : base(null,
            mapUnitMovement, location, direction, npcState,
            GameReferences.SpriteTileReferences.GetTileReferenceByName("HorseLeft"), mapUnitPosition)
        {
            KeyTileReference = NonBoardedTileReference;
        }

        private static int GetAdjustedPrice(PlayerCharacterRecords records, int nPrice)
        {
            const double IntelligenceFactor = 0.015;
            return (int)(nPrice - nPrice * IntelligenceFactor * records.AvatarRecord.Stats.Intelligence);
        }

        public static int GetPrice(SmallMapReferences.SingleMapReference.Location location,
            PlayerCharacterRecords records)
        {
            return GetAdjustedPrice(records, Prices[location]);
        }
    }
}