using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.SeaFaringVessels
{
    public class Frigate : SeaFaringVessel
    {
        private readonly Dictionary<Point2D.Direction, string> _sailsFurledTiles =
            new Dictionary<Point2D.Direction, string>
            {
                { Point2D.Direction.None, "ShipNoSailsUp" },
                { Point2D.Direction.Left, "ShipNoSailsLeft" },
                { Point2D.Direction.Down, "ShipNoSailsDown" },
                { Point2D.Direction.Right, "ShipNoSailsRight" },
                { Point2D.Direction.Up, "ShipNoSailsUp" }
            };

        private readonly Dictionary<Point2D.Direction, string> _sailsHoistedTiles =
            new Dictionary<Point2D.Direction, string>
            {
                { Point2D.Direction.None, "ShipSailsUp" },
                { Point2D.Direction.Left, "ShipSailsLeft" },
                { Point2D.Direction.Down, "ShipSailsDown" },
                { Point2D.Direction.Right, "ShipSailsRight" },
                { Point2D.Direction.Up, "ShipSailsUp" }
            };

        public Frigate(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement,
            TileReferences tileReferences, SmallMapReferences.SingleMapReference.Location location,
            DataOvlReference dataOvlReference, Point2D.Direction direction) :
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
            set => TheMapUnitState.Depends3 = (byte)value;
        }

        public bool SailsHoisted { get; set; } = false;

        private static Dictionary<SmallMapReferences.SingleMapReference.Location, int> Prices { get; } =
            new Dictionary<SmallMapReferences.SingleMapReference.Location, int>
            {
                { SmallMapReferences.SingleMapReference.Location.East_Britanny, 1300 },
                { SmallMapReferences.SingleMapReference.Location.Minoc, 1500 },
                { SmallMapReferences.SingleMapReference.Location.Buccaneers_Den, 1400 },
                { SmallMapReferences.SingleMapReference.Location.Jhelom, 1200 }
            };

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName => _sailsFurledTiles;

        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded =>
            SailsHoisted ? _sailsHoistedTiles : _sailsFurledTiles;

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Frigate;

        public override bool IsAttackable => false;
        public override string FriendlyName => BoardXitName;

        public override string BoardXitName =>
            DataOvlRef.StringReferences.GetString(DataOvlReference.SleepTransportStrings.SHIP_N).Trim();

        public int Hitpoints
        {
            get => TheMapUnitState.Depends1;
            set => TheMapUnitState.Depends1 = (byte)(value < 0 ? 0 : (value > 99 ? 99 : value));
        }

        public static int GetPrice(SmallMapReferences.SingleMapReference.Location location,
            PlayerCharacterRecords records)
        {
            return GetAdjustedPrice(records, Prices[location]);
        }
    }
}