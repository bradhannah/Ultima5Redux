using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits
{
    public class Horse :  MapUnit
    {
        protected override Dictionary<VirtualMap.Direction, string> DirectionToTileName { get; } =
            new Dictionary<VirtualMap.Direction, string>()
            {
                {VirtualMap.Direction.None, "HorseLeft"},
                {VirtualMap.Direction.Left, "HorseLeft"},
                {VirtualMap.Direction.Down, "HorseLeft"},
                {VirtualMap.Direction.Right, "HorseRight"},
                {VirtualMap.Direction.Up, "HorseRight"},
            };

        protected override Dictionary<VirtualMap.Direction, string> DirectionToTileNameBoarded { get; } =
            new Dictionary<VirtualMap.Direction, string>()
            {
                {VirtualMap.Direction.None, "RidingHorseLeft"},
                {VirtualMap.Direction.Left, "RidingHorseLeft"},
                {VirtualMap.Direction.Down, "RidingHorseLeft"},
                {VirtualMap.Direction.Right, "RidingHorseRight"},
                {VirtualMap.Direction.Up, "RidingHorseRight"},
            };

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Horse;

        public override string BoardXitName => DataOvlRef.StringReferences.GetString(DataOvlReference.SleepTransportStrings.HORSE_N).Trim();

        public override bool IsActive => true;
        
        public Horse(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement, TileReferences tileReferences, 
            SmallMapReferences.SingleMapReference.Location location, DataOvlReference dataOvlReference, 
            VirtualMap.Direction direction) 
            : base(null, mapUnitState, null, mapUnitMovement, null, null, tileReferences, 
                location, dataOvlReference, direction)
        {
            
        }
    }
}