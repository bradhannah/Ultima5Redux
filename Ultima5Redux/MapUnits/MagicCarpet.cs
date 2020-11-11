using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits
{
    public class MagicCarpet : MapUnit
    {
        public MagicCarpet(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement, TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location, DataOvlReference dataOvlReference,
            VirtualMap.Direction direction) : base(null, mapUnitState, null, 
            mapUnitMovement, null, tileReferences,
            location, dataOvlReference, direction)
        {
        }

        protected override Dictionary<VirtualMap.Direction, string> DirectionToTileName { get; } =
            new Dictionary<VirtualMap.Direction, string>
            {
                {VirtualMap.Direction.None, "Carpet2"},
                {VirtualMap.Direction.Left, "Carpet2"},
                {VirtualMap.Direction.Down, "Carpet2"},
                {VirtualMap.Direction.Right, "Carpet2"},
                {VirtualMap.Direction.Up, "Carpet2"}
            };

        protected override Dictionary<VirtualMap.Direction, string> DirectionToTileNameBoarded { get; } =
            new Dictionary<VirtualMap.Direction, string>
            {
                {VirtualMap.Direction.None, "RidingMagicCarpetLeft"},
                {VirtualMap.Direction.Left, "RidingMagicCarpetLeft"},
                {VirtualMap.Direction.Down, "RidingMagicCarpetLeft"},
                {VirtualMap.Direction.Right, "RidingMagicCarpetRight"},
                {VirtualMap.Direction.Up, "RidingMagicCarpetRight"}
            };

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Carpet;


        public override string BoardXitName => DataOvlRef.StringReferences
            .GetString(DataOvlReference.SleepTransportStrings.CARPET_N).Trim();

        public override bool IsActive => true;
    }
}