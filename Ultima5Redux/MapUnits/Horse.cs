using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    public sealed class Horse : MapUnit
    {
        public Horse(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement, TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location, DataOvlReference dataOvlReference,
            VirtualMap.Direction direction)
            : base(null, mapUnitState, null, mapUnitMovement, null, tileReferences,
                location, dataOvlReference, direction)
        {
            KeyTileReference = NonBoardedTileReference;
        }

        protected override Dictionary<VirtualMap.Direction, string> DirectionToTileName { get; } =
            new Dictionary<VirtualMap.Direction, string>
            {
                {VirtualMap.Direction.None, "HorseLeft"},
                {VirtualMap.Direction.Left, "HorseLeft"},
                {VirtualMap.Direction.Down, "HorseLeft"},
                {VirtualMap.Direction.Right, "HorseRight"},
                {VirtualMap.Direction.Up, "HorseRight"}
            };

        protected override Dictionary<VirtualMap.Direction, string> DirectionToTileNameBoarded { get; } =
            new Dictionary<VirtualMap.Direction, string>
            {
                {VirtualMap.Direction.None, "RidingHorseLeft"},
                {VirtualMap.Direction.Left, "RidingHorseLeft"},
                {VirtualMap.Direction.Down, "RidingHorseLeft"},
                {VirtualMap.Direction.Right, "RidingHorseRight"},
                {VirtualMap.Direction.Up, "RidingHorseRight"}
            };
        
        protected override Dictionary<VirtualMap.Direction, string> FourDirectionToTileNameBoarded  =>
            new Dictionary<VirtualMap.Direction, string>
                {
                {VirtualMap.Direction.None, "RidingHorseLeft"},
                {VirtualMap.Direction.Left, "RidingHorseLeft"},
                {VirtualMap.Direction.Down, "RidingHorseDown"},
                {VirtualMap.Direction.Right, "RidingHorseRight"},
                {VirtualMap.Direction.Up, "RidingHorseUp"}
            };


        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Horse;

        public override string BoardXitName => DataOvlRef.StringReferences
            .GetString(DataOvlReference.SleepTransportStrings.HORSE_N).Trim();

        public override bool IsActive => true;

        private static Dictionary<SmallMapReferences.SingleMapReference.Location, int> Prices { get; } =
            new Dictionary<SmallMapReferences.SingleMapReference.Location, int>
            {
                {SmallMapReferences.SingleMapReference.Location.Trinsic, 200},
                {SmallMapReferences.SingleMapReference.Location.Paws, 320},
                {SmallMapReferences.SingleMapReference.Location.Buccaneers_Den, 260}
            };

        public static int GetPrice(SmallMapReferences.SingleMapReference.Location location,
            PlayerCharacterRecords records)
        {
            return GetAdjustedPrice(records, Prices[location]);
        }

        private static int GetAdjustedPrice(PlayerCharacterRecords records, int nPrice)
        {
            const double IntelligenceFactor = 0.015;
            return (int) (nPrice - nPrice * IntelligenceFactor * records.AvatarRecord.Stats.Intelligence);
        }
    }
}