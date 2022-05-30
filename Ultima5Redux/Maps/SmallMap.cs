using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract] public sealed class SmallMap : RegularMap
    {
        public const int X_TILES = 32;
        public const int Y_TILES = 32;

        [IgnoreDataMember] public override int NumOfXTiles => CurrentSingleMapReference.XTiles;
        [IgnoreDataMember] public override int NumOfYTiles => CurrentSingleMapReference.YTiles;

        [IgnoreDataMember] public override bool ShowOuterSmallMapTiles => true;

        [IgnoreDataMember] public override byte[][] TheMap { get; protected set; }

        [IgnoreDataMember] public override bool IsRepeatingMap => false;

        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference =>
            GameReferences.SmallMapRef.GetSingleMapByLocation(MapLocation, MapFloor);

        [JsonConstructor] private SmallMap()
        {
        }

        /// <summary>
        ///     Creates a small map object using a pre-defined map reference
        /// </summary>
        /// <param name="singleSmallMapReference"></param>
        public SmallMap(SmallMapReferences.SingleMapReference singleSmallMapReference) : base(
            singleSmallMapReference.MapLocation, singleSmallMapReference.Floor)
        {
            // load the map into memory
            TheMap = CurrentSingleMapReference.GetDefaultMap();

            InitializeAStarMap(WalkableType.StandardWalking);
        }

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            TheMap = CurrentSingleMapReference.GetDefaultMap();

            InitializeAStarMap(WalkableType.StandardWalking);
        }

        /// <summary>
        ///     Calculates an appropriate A* weight based on the current tile as well as the surrounding tiles
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        protected override float GetAStarWeight(in Point2D xy)
        {
            bool isPreferredIndex(int nSprite) =>
                nSprite == GameReferences.SpriteTileReferences.GetTileReferenceByName("BrickFloor").Index ||
                GameReferences.SpriteTileReferences.IsPath(nSprite);

            const int fDefaultDeduction = 2;

            float fCost = 10;

            // we reduce the weight for the A* for each adjacent brick floor or path tile
            if (xy.X - 1 >= 0) fCost -= isPreferredIndex(TheMap[xy.X - 1][xy.Y]) ? fDefaultDeduction : 0;
            if (xy.X + 1 < CurrentSingleMapReference.XTiles)
                fCost -= isPreferredIndex(TheMap[xy.X + 1][xy.Y]) ? fDefaultDeduction : 0;
            if (xy.Y - 1 >= 0) fCost -= isPreferredIndex(TheMap[xy.X][xy.Y - 1]) ? fDefaultDeduction : 0;
            if (xy.Y + 1 < CurrentSingleMapReference.YTiles)
                fCost -= isPreferredIndex(TheMap[xy.X][xy.Y + 1]) ? fDefaultDeduction : 0;

            return fCost;
        }

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit)
        {
            // TBD
        }

        protected internal override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit)
        {
            return mapUnit switch
            {
                Enemy enemy => enemy.EnemyReference.IsWaterEnemy
                    ? WalkableType.CombatWater
                    : WalkableType.StandardWalking,
                CombatPlayer _ => WalkableType.StandardWalking,
                _ => WalkableType.StandardWalking
            };
        }

        /// <summary>
        ///     Gets the appropriate out of bounds sprite based on the map
        /// </summary>
        /// <returns></returns>
        public int GetOutOfBoundsSprite(Point2D position)
        {
            return 5;
        }

        /// <summary>
        ///     Checks if a tile is in bounds of the actual map and not a border tile
        /// </summary>
        /// <param name="position">position within the virtual map</param>
        /// <returns></returns>
        public bool IsInBounds(Point2D position)
        {
            // determine if the x or y coordinates are in bounds, if they are out of bounds and the map does not repeat
            // then we are going to draw a default texture on the outside areas.
            bool xInBounds = position.X is >= 0 and < X_TILES;
            bool yInBounds = position.Y is >= 0 and < Y_TILES;

            // fill outside of the bounds with a default tile
            return (xInBounds && yInBounds);
        }
    }
}