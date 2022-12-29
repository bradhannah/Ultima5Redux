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
        public override int NumOfXTiles => SingleDungeonMapFloorReference.N_DUNGEON_COLS_PER_ROW;
        public override int NumOfYTiles => SingleDungeonMapFloorReference.N_DUNGEON_ROWS_PER_MAP;
        public override bool ShowOuterSmallMapTiles => false;

        private SingleDungeonMapFloorReference _singleDungeonMapFloorReference;

        public SingleDungeonMapFloorReference SingleDungeonMapFloorReference
        {
            get
            {
                if (_singleDungeonMapFloorReference == null)
                    _singleDungeonMapFloorReference = GameReferences.Instance.DungeonReferences.GetDungeon(MapLocation)
                        .GetSingleDungeonMapFloorReferenceByFloor(MapFloor);
                return _singleDungeonMapFloorReference;
            }
            set => _singleDungeonMapFloorReference = value;
        }

        public override byte[][] TheMap
        {
            get;
            protected set;
            //throw new NotImplementedException();
        }

        public override void RecalculateVisibleTiles(in Point2D initialFloodFillPosition)
        {
            if (VisibleOnMap == null)
            {
                VisibleOnMap = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles, true);
                RecalculatedHash = Utils.Ran.Next();
            }
            
            Utils.Set2DArrayAllToValue(VisibleOnMap, true);
          
        }

        private SmallMapReferences.SingleMapReference _currentSingleMapReference;

        [IgnoreDataMember]
        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference
        {
            get
            {
                if (_currentSingleMapReference == null)
                    _currentSingleMapReference =
                        GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(MapLocation, MapFloor);
                return _currentSingleMapReference;
            }
        } //protected override Dictionary<Point2D, TileOverrideReference> XYOverrides => new();

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit)
        {
            //throw new System.NotImplementedException();
        }

        protected override float GetAStarWeight(in Point2D xy) => 1.0f; 

        public override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit) => WalkableType.StandardWalking;

        public override bool IsRepeatingMap => false;

        [JsonConstructor] public DungeonMap() => TheMap = SingleDungeonMapFloorReference.GetDefaultDungeonMap();

        public DungeonMap(SingleDungeonMapFloorReference singleDungeonMapFloorReference) : base(
            singleDungeonMapFloorReference.DungeonLocation, singleDungeonMapFloorReference.DungeonFloor)
        {
            SingleDungeonMapFloorReference = singleDungeonMapFloorReference;
            TheMap = SingleDungeonMapFloorReference.GetDefaultDungeonMap();
        }
    }
}