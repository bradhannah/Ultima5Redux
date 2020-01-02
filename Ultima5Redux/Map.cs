using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Ultima5Redux
{
    abstract public class Map
    {
        #region Protected Fields
        /// <summary>
        /// The directory of the U5 data files
        /// </summary>
        protected string u5Directory;
        #endregion

        #region Internal Fields
        /// <summary>
        /// All A* nodes for the current map
        /// Accessed by [x][y]
        /// </summary>
        internal List<List<AStarSharp.Node>> aStarNodes;
        /// <summary>
        /// A* algorithm helper class
        /// </summary>
        internal AStarSharp.Astar astar;
        #endregion

        public Map(string u5Directory)
        {
            this.u5Directory = u5Directory;
        }

        protected void InitializeAStarMap(TileReferences spriteTileReferences)
        {
            Debug.Assert(TheMap != null);
            Debug.Assert(TheMap.Length > 0);
            int nXTiles = TheMap[0].Length;
            int nYTiles = TheMap.Length;

            // load the A-Star compatible map into memory
            aStarNodes = Utils.Init2DList<AStarSharp.Node>(nXTiles, nYTiles);

            for (int x = 0; x < nXTiles; x++)
            {
                for (int y = 0; y < nYTiles; y++)
                {
                    TileReference currentTile = spriteTileReferences.GetTileReference(TheMap[x][y]);
                    bool bIsWalkable = currentTile.IsWalking_Passable || currentTile.Index == 184 || currentTile.Index == 186;
                    
                    AStarSharp.Node node = new AStarSharp.Node(new System.Numerics.Vector2(x, y), bIsWalkable);
                    aStarNodes[x].Add(node);
                }
            }
            astar = new AStarSharp.Astar(aStarNodes);
        }

        /// <summary>
        /// We use this because the raw map files only store a single byte per tile (0-255), but the REAL world is made up for sprites numbered from 0-511
        /// so we make a copy for the game to use as a the live map which can contain the bigger sprite indexes
        /// </summary>
        /// <returns></returns>
        //public int[][] GetCopyOfMapAsInt()
        //{
        //    int nCols = theMap.Length;
        //    int nRows = theMap[0].Length;

        //    int[][] intMap = Utils.Init2DArray<int>(nCols, nRows, 0);

        //    for (int curRow = 0; curRow < nRows; curRow++)
        //    {
        //        for (int curCol = 0; curCol < nCols; curCol++)
        //        {
        //            // no casting required since we go from byte to int
        //            intMap[curCol][curRow] = theMap[curCol][curRow];
        //        }
        //    }
        //    return intMap;
        //}

        public byte[][] TheMap
        {
            get; protected set;
            //get
            //{
            //    return theMap;
            //}
        }

        #region Debug methods
        /// <summary>
        /// Filthy little map to assign single letter to map elements
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        static public char GetMapLetter(byte tile)
        {
            switch (tile)
            {
                case 0x03:
                case 0x02:
                case 0x01:
                    return 'W'; // water
                case 0x08:
                case 0x09:
                case 0x0A:
                    return 'T'; //  trees
                case 0x04:
                case 0x05:
                case 0x06:
                    return 'G'; // grass
                case 0x0B:
                case 0x0C:
                case 0x0D:
                case 0x0E:
                case 0x0F:
                    return 'M'; // mountains
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                case 0x14:
                case 0x15:
                case 0x16:
                case 0x17:
                case 0x18:
                    return 'X'; // towns
                case 0x19:
                case 0x1A:
                    return 'S'; //shrines
                case 0x1B:
                    return 'L'; //lighthouse
                case 0x1C:
                    return '?';
                case 0x1D:
                    return 'B'; //bridge
                case 0x1E:
                    return 'F'; //field?? --- end of tile row (30dec)
                case 0x1F:
                case 0x20:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                    return 'P'; // path --ignore next 9 (0x2E)
                case 0x2F:
                case 0x31:
                case 0x32:
                case 0x33:
                    return 'G'; // more grass
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                    return 'S'; // streams
            }
            return 'L';
        }

        /// <summary>
        /// Prints the map in ASCII on the console
        /// </summary>
        /// <param name="map">map object</param>
        /// <param name="xOffset">where to start the top left origin (row) </param>
        /// <param name="yOffset">where to start the top left origin (column) </param>
        /// <param name="xTilesToPrint">how many tiles to print vertically</param>
        /// <param name="yTilesToPrint">how many tiles to print horizontally</param>
        static public void PrintMapSection(byte[][] map, int xOffset, int yOffset, int xTilesToPrint, int yTilesToPrint)
        {
            for (int curRow = yOffset; curRow < yTilesToPrint + yOffset; curRow++)
            {
                for (int curCol = xOffset; curCol < xTilesToPrint + xOffset; curCol++)
                {
                    if (curCol % (xTilesToPrint) == 0) { System.Console.WriteLine(""); }
                    byte mapTile = map[curCol][curRow];
                    System.Console.Write(Map.GetMapLetter(mapTile));
                }
            }
        }
        #endregion


    }
}
