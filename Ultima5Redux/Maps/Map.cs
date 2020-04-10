using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.External;

namespace Ultima5Redux.Maps
{
    public abstract class Map
    {
        public SmallMapReferences.SingleMapReference CurrentSingleMapReference { get; }
        
        #region Protected Fields
        /// <summary>
        /// The directory of the U5 data files
        /// </summary>
        protected string U5Directory;

        protected TileOverrides TileOverrides;
        #endregion

        #region Internal Fields
        /// <summary>
        /// All A* nodes for the current map
        /// Accessed by [x][y]
        /// </summary>
        protected List<List<Node>> AStarNodes;
        /// <summary>
        /// A* algorithm helper class
        /// </summary>
        internal AStar AStar;
        #endregion

        public Map(string u5Directory, TileOverrides tileOverrides, SmallMapReferences.SingleMapReference mapRef)
        {
            this.U5Directory = u5Directory;
            this.TileOverrides = tileOverrides;
            CurrentSingleMapReference = mapRef; 

            // for now combat maps don't have overrides
            if (mapRef != null)
            {
                XYOverrides = tileOverrides.GetTileXYOverridesBySingleMap(mapRef);
            }
        }

        /// <summary>
        /// Calculates an appropriate A* weight based on the current tile as well as the surrounding tiles
        /// </summary>
        /// <param name="spriteTileReferences"></param>
        /// <param name="xy"></param>
        /// <returns></returns>
        protected abstract float GetAStarWeight(TileReferences spriteTileReferences, Point2D xy);
        
        /// <summary>
        /// Builds the A* map to be used for NPC pathfinding
        /// </summary>
        /// <param name="spriteTileReferences"></param>
        protected void InitializeAStarMap(TileReferences spriteTileReferences)
        {
            Debug.Assert(TheMap != null);
            Debug.Assert(TheMap.Length > 0);
            int nXTiles = TheMap[0].Length;
            int nYTiles = TheMap.Length;

            // load the A-Star compatible map into memory
            AStarNodes = Utils.Init2DList<Node>(nXTiles, nYTiles);

            for (int x = 0; x < nXTiles; x++)
            {
                for (int y = 0; y < nYTiles; y++)
                {
                    TileReference currentTile = spriteTileReferences.GetTileReference(TheMap[x][y]);

                    bool bIsWalkable =
                        currentTile.IsWalking_Passable || currentTile.Index ==
                                                       spriteTileReferences.GetTileReferenceByName("RegularDoor").Index
                                                       || currentTile.Index == spriteTileReferences
                                                           .GetTileReferenceByName("RegularDoorView").Index
                                                       || currentTile.Index == spriteTileReferences
                                                           .GetTileReferenceByName("LockedDoor").Index
                                                       || currentTile.Index == spriteTileReferences
                                                           .GetTileReferenceByName("LockedDoorView").Index;
                    
                    float fWeight = GetAStarWeight(spriteTileReferences, new Point2D(x,y));
                    
                    Node node = new Node(new System.Numerics.Vector2(x, y), bIsWalkable, fWeight);
                    AStarNodes[x].Add(node);
                }
            }
            AStar = new AStar(AStarNodes);
        }


        public byte[][] TheMap
        {
            get; protected set;
        }

        protected Dictionary<Point2D, TileOverride> XYOverrides;

        public bool IsXYOverride(Point2D xy)
        {
            return XYOverrides != null && XYOverrides.ContainsKey(xy);
        }

        public TileOverride GetTileOverride(Point2D xy)
        {
            return XYOverrides[xy];
        }
        // public abstract TileOverride GetTileOverride(Point2D xy);
        // public abstract bool IsXYOverride(Point2D xy);

        #region Debug methods
        /// <summary>
        /// Filthy little map to assign single letter to map elements
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public static char GetMapLetter(byte tile)
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
        public static void PrintMapSection(byte[][] map, int xOffset, int yOffset, int xTilesToPrint, int yTilesToPrint)
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
