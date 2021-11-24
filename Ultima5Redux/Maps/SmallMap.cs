using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.References;

namespace Ultima5Redux.Maps
{
    [DataContract] public sealed class SmallMap : RegularMap
    {
        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference =>
            GameReferences.SmallMapRef.GetSingleMapByLocation(MapLocation, MapFloor);

        public const int X_TILES = 32;
        public const int Y_TILES = 32;
        
        [IgnoreDataMember] protected override bool IsRepeatingMap => false;

        [IgnoreDataMember] public override int NumOfXTiles => CurrentSingleMapReference.XTiles;
        [IgnoreDataMember] public override int NumOfYTiles => CurrentSingleMapReference.YTiles;

        [IgnoreDataMember] public override bool ShowOuterSmallMapTiles => true;

        [IgnoreDataMember] public override byte[][] TheMap { get; protected set; }

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
            // for now combat maps don't have overrides
            //XYOverrides = GameReferences.TileOverrideRefs.GetTileXYOverrides(CurrentSingleMapReference);

            // load the map into memory
            TheMap = CurrentSingleMapReference.GetDefaultMap();

            InitializeAStarMap(WalkableType.StandardWalking);
        }

        /// <summary>
        ///     Calculates an appropriate A* weight based on the current tile as well as the surrounding tiles
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        protected override float GetAStarWeight(Point2D xy)
        {
            bool isPreferredIndex(int nSprite) =>
                nSprite == GameReferences.SpriteTileReferences.GetTileReferenceByName("BrickFloor").Index ||
                GameReferences.SpriteTileReferences.IsPath(nSprite);

            const int fDefaultDeduction = 2;

            TileReference unused = GameReferences.SpriteTileReferences.GetTileReference(TheMap[xy.X][xy.Y]);

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

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            TheMap = CurrentSingleMapReference.GetDefaultMap();

            // for now combat maps don't have overrides
            //XYOverrides = GameReferences.TileOverrideRefs.GetTileXYOverrides(CurrentSingleMapReference);

            InitializeAStarMap(WalkableType.StandardWalking);
        }
    }
}