using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.SeaFaringVessels
{
    [DataContract]
    public class Frigate : SeaFaringVessel
    {
        private readonly Dictionary<Point2D.Direction, string> _sailsFurledTiles =
            new()
            {
                { Point2D.Direction.None, "ShipNoSailsUp" },
                { Point2D.Direction.Left, "ShipNoSailsLeft" },
                { Point2D.Direction.Down, "ShipNoSailsDown" },
                { Point2D.Direction.Right, "ShipNoSailsRight" },
                { Point2D.Direction.Up, "ShipNoSailsUp" }
            };

        private readonly Dictionary<Point2D.Direction, string> _sailsHoistedTiles =
            new()
            {
                { Point2D.Direction.None, "ShipSailsUp" },
                { Point2D.Direction.Left, "ShipSailsLeft" },
                { Point2D.Direction.Down, "ShipSailsDown" },
                { Point2D.Direction.Right, "ShipSailsRight" },
                { Point2D.Direction.Up, "ShipSailsUp" }
            };

        private static Dictionary<SmallMapReferences.SingleMapReference.Location, int> Prices { get; } =
            new()
            {
                { SmallMapReferences.SingleMapReference.Location.East_Britanny, 1300 },
                { SmallMapReferences.SingleMapReference.Location.Minoc, 1500 },
                { SmallMapReferences.SingleMapReference.Location.Buccaneers_Den, 1400 },
                { SmallMapReferences.SingleMapReference.Location.Jhelom, 1200 }
            };

        public override bool CanStackMapUnitsOnTop => true;

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Frigate;

        public override string BoardXitName =>
            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.SleepTransportStrings.SHIP_N)
                .Trim();

        public override string FriendlyName => BoardXitName;

        public override bool IsAttackable => false;

        public int HitPoints { get; set; }

        public bool SailsHoisted { get; set; }

        /// <summary>
        ///     How many skiffs does the frigate have aboard?
        /// </summary>
        public int SkiffsAboard { get; set; }

        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded =>
            SailsHoisted ? _sailsHoistedTiles : _sailsFurledTiles;

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName => _sailsFurledTiles;

        [JsonConstructor]
        private Frigate()
        {
        }

        public Frigate(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement,
            SmallMapReferences.SingleMapReference.Location location, Point2D.Direction direction,
            NonPlayerCharacterState npcState, MapUnitPosition mapUnitPosition) : this(mapUnitMovement, location,
            direction, npcState, mapUnitPosition)
        {
            HitPoints = mapUnitState.Depends1;
            SkiffsAboard = mapUnitState.Depends3;
        }

        public Frigate(MapUnitMovement mapUnitMovement, SmallMapReferences.SingleMapReference.Location location,
            Point2D.Direction direction, NonPlayerCharacterState npcState, MapUnitPosition mapUnitPosition) : base(null,
            mapUnitMovement, location, direction, npcState, mapUnitPosition)
        {
        }

        public static int GetPrice(SmallMapReferences.SingleMapReference.Location location,
            PlayerCharacterRecords records) =>
            GetAdjustedPrice(records, Prices[location]);
    }
}