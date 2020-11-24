using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.SeaFaringVessels
{
    public class Frigate : SeaFaringVessel
    {
        public Frigate(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement,
            TileReferences tileReferences, SmallMapReferences.SingleMapReference.Location location,
            DataOvlReference dataOvlReference, VirtualMap.Direction direction) :
            base(mapUnitState, null, mapUnitMovement, tileReferences, location,
                dataOvlReference, direction)
        {
        }

        /// <summary>
        ///     How many skiffs does the frigate have aboard?
        /// </summary>
        public int SkiffsAboard
        {
            get => TheMapUnitState.Depends3;
            set => TheMapUnitState.Depends3 = (byte) value;
        }

        public bool SailsHoisted { get; set; } = false;

        private static Dictionary<SmallMapReferences.SingleMapReference.Location, int> Prices { get; } =
            new Dictionary<SmallMapReferences.SingleMapReference.Location, int>
            {
                {SmallMapReferences.SingleMapReference.Location.East_Britanny, 1300},
                {SmallMapReferences.SingleMapReference.Location.Minoc, 1500},
                {SmallMapReferences.SingleMapReference.Location.Buccaneers_Den, 1400},
                {SmallMapReferences.SingleMapReference.Location.Jhelom, 1200}
            };

        private readonly Dictionary<VirtualMap.Direction, string> _sailsFurledTiles = new Dictionary<VirtualMap.Direction, string>
        {
            {VirtualMap.Direction.None, "ShipNoSailsUp"},
            {VirtualMap.Direction.Left, "ShipNoSailsLeft"},
            {VirtualMap.Direction.Down, "ShipNoSailsDown"},
            {VirtualMap.Direction.Right, "ShipNoSailsRight"},
            {VirtualMap.Direction.Up, "ShipNoSailsUp"}
        };
        
        private readonly Dictionary<VirtualMap.Direction, string> _sailsHoistedTiles = new Dictionary<VirtualMap.Direction, string>
        {
            {VirtualMap.Direction.None, "ShipSailsUp"},
            {VirtualMap.Direction.Left, "ShipSailsLeft"},
            {VirtualMap.Direction.Down, "ShipSailsDown"},
            {VirtualMap.Direction.Right, "ShipSailsRight"},
            {VirtualMap.Direction.Up, "ShipSailsUp"}
        };

        protected override Dictionary<VirtualMap.Direction, string> DirectionToTileName => _sailsFurledTiles;

        protected override Dictionary<VirtualMap.Direction, string> DirectionToTileNameBoarded => SailsHoisted ? _sailsHoistedTiles : _sailsFurledTiles;
        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Frigate;
    
        public override string BoardXitName =>
            DataOvlRef.StringReferences.GetString(DataOvlReference.SleepTransportStrings.SHIP_N).Trim();

        public int Hitpoints
        {
            get => this.TheMapUnitState.Depends1;
            set => this.TheMapUnitState.Depends1 = (byte)(value < 0 ? 0 : (value > 99 ? 99 : value));
        }

        public static int GetPrice(SmallMapReferences.SingleMapReference.Location location,
            PlayerCharacterRecords records)
        {
            return GetAdjustedPrice(records, Prices[location]);
        }
    }
}