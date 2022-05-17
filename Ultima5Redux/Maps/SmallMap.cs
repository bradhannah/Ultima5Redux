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
    }
}