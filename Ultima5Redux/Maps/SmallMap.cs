using System.Collections.Generic;
using System.IO;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.Monsters;

namespace Ultima5Redux.Maps
{
    public class SmallMap : RegularMap
    {
        private readonly SmallMapReferences.SingleMapReference _singleSmallMapReference;

        /// <summary>
        ///     Creates a small map object using a pre-defined map reference
        /// </summary>
        /// <param name="u5Directory"></param>
        /// <param name="singleSmallMapReference"></param>
        /// <param name="spriteTileReferences"></param>
        /// <param name="tileOverrides"></param>
        public SmallMap(string u5Directory, SmallMapReferences.SingleMapReference singleSmallMapReference,
            TileReferences spriteTileReferences, TileOverrides tileOverrides) : base(tileOverrides,
            singleSmallMapReference,
            spriteTileReferences)
        {
            _singleSmallMapReference = singleSmallMapReference;

            // load the map into memory
            TheMap = LoadSmallMapFile(Path.Combine(u5Directory, singleSmallMapReference.MapFilename),
                singleSmallMapReference.FileOffset);

            InitializeAStarMap(WalkableType.StandardWalking);
        }

        /// <summary>
        ///     Total tiles per row
        /// </summary>
        public static int XTILES => 32;

        /// <summary>
        ///     Total tiles per column
        /// </summary>
        public static int YTILES => 32;

        public override byte[][] TheMap { get; protected set; }

        public override int NumOfXTiles => XTILES;
        public override int NumOfYTiles => YTILES;

        public override bool ShowOuterSmallMapTiles => true;

        protected override bool IsRepeatingMap => false;

        public SmallMapReferences.SingleMapReference.Location MapLocation => _singleSmallMapReference.MapLocation;
        public int MapFloor => _singleSmallMapReference.Floor;

        /// <summary>
        ///     Loads a small map into a 2D array
        /// </summary>
        /// <param name="mapFilename">name of the file that contains the map</param>
        /// <param name="fileOffset">the file offset to begin reading the file at</param>
        /// <returns></returns>
        private static byte[][] LoadSmallMapFile(string mapFilename, int fileOffset)
        {
            List<byte> mapBytes = Utils.GetFileAsByteList(mapFilename);

            byte[][] smallMap = Utils.ListTo2DArray(mapBytes, (short)XTILES, fileOffset, XTILES * YTILES);

            // have to transpose the array because the ListTo2DArray function puts the map together backwards...
            return Utils.TransposeArray(smallMap);
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


        /// <summary>
        ///     Calculates an appropriate A* weight based on the current tile as well as the surrounding tiles
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        protected override float GetAStarWeight(Point2D xy)
        {
            bool isPreferredIndex(int nSprite) =>
                nSprite == SpriteTileReferences.GetTileReferenceByName("BrickFloor").Index ||
                SpriteTileReferences.IsPath(nSprite);

            const int fDefaultDeduction = 2;

            TileReference unused = SpriteTileReferences.GetTileReference(TheMap[xy.X][xy.Y]);

            float fCost = 10;

            // we reduce the weight for the A* for each adjacent brick floor or path tile
            if (xy.X - 1 >= 0) fCost -= isPreferredIndex(TheMap[xy.X - 1][xy.Y]) ? fDefaultDeduction : 0;
            if (xy.X + 1 < XTILES) fCost -= isPreferredIndex(TheMap[xy.X + 1][xy.Y]) ? fDefaultDeduction : 0;
            if (xy.Y - 1 >= 0) fCost -= isPreferredIndex(TheMap[xy.X][xy.Y - 1]) ? fDefaultDeduction : 0;
            if (xy.Y + 1 < YTILES) fCost -= isPreferredIndex(TheMap[xy.X][xy.Y + 1]) ? fDefaultDeduction : 0;

            return fCost;
        }
    }
}