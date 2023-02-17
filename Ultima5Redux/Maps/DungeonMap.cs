using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class DungeonMap : RegularMap
    {
        [IgnoreDataMember] private SingleDungeonMapFloorReference _singleDungeonMapFloorReference;

        [IgnoreDataMember]
        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference =>
            _currentSingleMapReference ??=
                GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(MapLocation, MapFloor);

        [IgnoreDataMember]
        // ReSharper disable once MemberCanBePrivate.Global
        public SingleDungeonMapFloorReference SingleDungeonMapFloorReference
        {
            get =>
                _singleDungeonMapFloorReference ??= GameReferences.Instance
                    .DungeonReferences.GetDungeon(MapLocation)
                    .GetSingleDungeonMapFloorReferenceByFloor(MapFloor);
            set => _singleDungeonMapFloorReference = value;
        }

        private SmallMapReferences.SingleMapReference _currentSingleMapReference;

        public override bool IsRepeatingMap => false;
        public override int NumOfXTiles => SingleDungeonMapFloorReference.N_DUNGEON_COLS_PER_ROW;
        public override int NumOfYTiles => SingleDungeonMapFloorReference.N_DUNGEON_ROWS_PER_MAP;
        public virtual bool ShowOuterSmallMapTiles => false;

        public override byte[][] TheMap { get; protected set; }

        public override Maps TheMapType => Maps.Dungeon;

        [JsonConstructor] public DungeonMap()
        {
        }

        public DungeonMap(SingleDungeonMapFloorReference singleDungeonMapFloorReference) : base(
            singleDungeonMapFloorReference.DungeonLocation, singleDungeonMapFloorReference.DungeonFloor)
        {
            SingleDungeonMapFloorReference = singleDungeonMapFloorReference;
            TheMap = SingleDungeonMapFloorReference.GetDefaultDungeonMap();
            CurrentMapUnits ??= new MapUnitCollection();
            CurrentMapUnits.Clear();

            MapUnit theAvatar = Avatar.CreateAvatar(singleDungeonMapFloorReference.DungeonLocation,
                new MapUnitMovement(0),
                new MapUnitPosition(0, 0, singleDungeonMapFloorReference.DungeonFloor),
                GameReferences.Instance.SpriteTileReferences.GetTileReference(284), UseExtendedSprites);
            CurrentMapUnits.Add(theAvatar);
        }

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            SingleDungeonMapFloorReference = GameReferences.Instance.DungeonReferences.GetDungeon(MapLocation)
                .GetSingleDungeonMapFloorReferenceByFloor(MapFloor);
            TheMap = SingleDungeonMapFloorReference.GetDefaultDungeonMap();
        }

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit)
        {
        }

        public override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit) => WalkableType.StandardWalking;

        public override void RecalculateVisibleTiles(in Point2D initialFloodFillPosition)
        {
            if (VisibleOnMap == null)
            {
                VisibleOnMap = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles, true);
                RecalculatedHash = Utils.Ran.Next();
            }

            Utils.Set2DArrayAllToValue(VisibleOnMap, true);
        }

        protected override float GetAStarWeight(in Point2D xy) => 1.0f;
    }
}