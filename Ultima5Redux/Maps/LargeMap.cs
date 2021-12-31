using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract] public sealed class LargeMap : RegularMap
    {
        //[DataMember(Name = "DataDirectory")] private readonly string _dataDirectory;
        [DataMember(Name = "MapChoice")] private readonly Maps _mapChoice;

        [DataMember(Name = "BottomRightExtent")]
        private Point2D _bottomRightExtent;

        [DataMember(Name = "TopLeftExtent")] private Point2D _topLeftExtent;

        [IgnoreDataMember] public override int NumOfXTiles => LargeMapLocationReferences.XTiles;
        [IgnoreDataMember] public override int NumOfYTiles => LargeMapLocationReferences.YTiles;

        [IgnoreDataMember] public override bool ShowOuterSmallMapTiles => false;

        [IgnoreDataMember] public override byte[][] TheMap { get; protected set; }

        [IgnoreDataMember] protected override bool IsRepeatingMap => true;

        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference =>
            SmallMapReferences.SingleMapReference.GetLargeMapSingleInstance(_mapChoice);

        [JsonConstructor] private LargeMap()
        {
            // for now combat maps don't have overrides
            //XYOverrides = GameReferences.TileOverrideRefs.GetTileXYOverrides(CurrentSingleMapReference);
        }

        /// <summary>
        ///     Build a large map. There are essentially two choices - Overworld and Underworld
        /// </summary>
        /// <param name="mapChoice"></param>
        public LargeMap(Maps mapChoice) : base(
            SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, mapChoice == Maps.Overworld ? 0 : -1)
        {
            if (mapChoice != Maps.Overworld && mapChoice != Maps.Underworld)
                throw new Ultima5ReduxException("Tried to create a large map with " + mapChoice);

            _mapChoice = mapChoice;

            // for now combat maps don't have overrides
            //XYOverrides = GameReferences.TileOverrideRefs.GetTileXYOverrides(CurrentSingleMapReference);

            BuildMap(mapChoice);
            BuildAStar();
        }

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            BuildMap(_mapChoice);
            BuildAStar();
        }

        private void BuildAStar()
        {
            InitializeAStarMap(WalkableType.StandardWalking);
            InitializeAStarMap(WalkableType.CombatWater);
        }

        private void BuildMap(Maps mapChoice)
        {
            switch (mapChoice)
            {
                case Maps.Overworld:
                case Maps.Underworld:
                    TheMap = GameReferences.LargeMapRef.GetMap(mapChoice);
                    break;
                case Maps.Small:
                    throw new Ultima5ReduxException("tried to create a LargeMap with the .Small map enum");
                default:
                    throw new ArgumentOutOfRangeException(nameof(mapChoice), mapChoice, null);
            }
        }

        public override void RecalculateVisibleTiles(Point2D initialFloodFillPosition)
        {
            if (XRayMode)
            {
                VisibleOnMap = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles, true);
                return;
            }

            NVisibleLargeMapTiles = VisibleInEachDirectionOfAvatar * 2 + 1;

            VisibleOnMap = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles);
            TestForVisibility = new List<bool[][]>();
            // reinitialize the array for all potential party members
            for (int i = 0; i < PlayerCharacterRecords.MAX_PARTY_MEMBERS; i++)
            {
                TestForVisibility.Add(Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles));
            }

            TouchedOuterBorder = false;

            AvatarXyPos = initialFloodFillPosition;

            _topLeftExtent = new Point2D(AvatarXyPos.X - VisibleInEachDirectionOfAvatar,
                AvatarXyPos.Y - VisibleInEachDirectionOfAvatar);
            _bottomRightExtent = new Point2D(AvatarXyPos.X + VisibleInEachDirectionOfAvatar,
                AvatarXyPos.Y + VisibleInEachDirectionOfAvatar);

            FloodFillMap(AvatarXyPos, true);
        }

        // ReSharper disable once UnusedMember.Global
        public void PrintMap()
        {
            PrintMapSection(TheMap, 0, 0, 160, 80);
        }

        /// <summary>
        ///     Gets a positive based Point2D for LargeMaps - it was return null if it outside of the
        ///     current extends
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="xy"></param>
        /// <returns></returns>
        protected override Point2D GetAdjustedPos(Point2D.Direction direction, Point2D xy)
        {
            int nPositiveX = xy.X + NumOfXTiles;
            int nPositiveY = xy.Y + NumOfYTiles;

            if (nPositiveX <= _topLeftExtent.X + NumOfXTiles || xy.X >= _bottomRightExtent.X)
                return null;
            if (nPositiveY <= _topLeftExtent.Y + NumOfYTiles || xy.Y >= _bottomRightExtent.Y)
                return null;

            return xy.GetAdjustedPosition(direction);
        }

        protected override float GetAStarWeight(Point2D xy)
        {
            return 1;
        }

        protected override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit)
        {
            switch (mapUnit)
            {
                case Enemy enemy:
                    return enemy.EnemyReference.IsWaterEnemy ? WalkableType.CombatWater : WalkableType.StandardWalking;
                case CombatPlayer _:
                    return WalkableType.StandardWalking;
                default:
                    return WalkableType.StandardWalking;
            }
        }
    }
}