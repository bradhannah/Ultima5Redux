using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract] public sealed class LargeMap : RegularMap
    {
        [DataMember(Name = "MapChoice")] private readonly Maps _mapChoice;

        [DataMember(Name = "BottomRightExtent")]
        private Point2D _bottomRightExtent;

        [DataMember(Name = "TopLeftExtent")] private Point2D _topLeftExtent;

        [IgnoreDataMember] public override int NumOfXTiles => LargeMapLocationReferences.XTiles;
        [IgnoreDataMember] public override int NumOfYTiles => LargeMapLocationReferences.YTiles;

        [IgnoreDataMember] public override bool ShowOuterSmallMapTiles => false;

        [IgnoreDataMember] public override byte[][] TheMap { get; protected set; }

        [IgnoreDataMember] public override bool IsRepeatingMap => true;

        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference =>
            _currentSingleMapReference ??= SmallMapReferences.SingleMapReference.GetLargeMapSingleInstance(_mapChoice);

        private SmallMapReferences.SingleMapReference _currentSingleMapReference;

        [JsonConstructor] private LargeMap()
        {
            // for now combat maps don't have overrides
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

        public override void RecalculateVisibleTiles(in Point2D initialFloodFillPosition)
        {
            if (XRayMode)
            {
                Utils.Set2DArrayAllToValue(VisibleOnMap, true);
                return;
            }

            TouchedOuterBorder = false;

            AvatarXyPos = initialFloodFillPosition;

            _topLeftExtent = new Point2D(AvatarXyPos.X - VISIBLE_IN_EACH_DIRECTION_OF_AVATAR,
                AvatarXyPos.Y - VISIBLE_IN_EACH_DIRECTION_OF_AVATAR);
            _bottomRightExtent = new Point2D(AvatarXyPos.X + VISIBLE_IN_EACH_DIRECTION_OF_AVATAR,
                AvatarXyPos.Y + VISIBLE_IN_EACH_DIRECTION_OF_AVATAR);

            RefreshTestForVisibility(1);
            SetMaxVisibleArea(AvatarXyPos, TOTAL_VISIBLE_TILES);
            FloodFillMap(AvatarXyPos.X, AvatarXyPos.Y, true);
        }

        // ReSharper disable once UnusedMember.Global
        public void PrintMap()
        {
            PrintMapSection(TheMap, 0, 0, 160, 80);
        }

        protected override float GetAStarWeight(in Point2D xy)
        {
            return 1;
        }

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit)
        {
            // TBD
        }

        protected internal override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit)
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