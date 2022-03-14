using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.External;
using Ultima5Redux.MapUnits;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract] public abstract class Map
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Maps { Small = -1, Overworld, Underworld, Combat, Dungeon }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum WalkableType { StandardWalking, CombatLand, CombatWater, CombatFlyThroughWalls, CombatLandAndWater }

        protected const int TOTAL_VISIBLE_TILES = 26;

        [DataMember(Name = "OpenDoors")] private readonly Dictionary<Point2D, int> _openDoors = new();

        [DataMember] public bool XRayMode { get; set; }

        [IgnoreDataMember] private readonly Dictionary<WalkableType, AStar> _aStarDictionary = new();

        [IgnoreDataMember] private readonly Dictionary<WalkableType, List<List<Node>>> _aStarNodes = new();

        public abstract int NumOfXTiles { get; }

        public abstract int NumOfYTiles { get; }

        // ReSharper disable once NotAccessedField.Global
        // ReSharper disable once MemberCanBePrivate.Global

        public abstract bool ShowOuterSmallMapTiles { get; }

        public abstract byte[][] TheMap { get; protected set; }

        protected abstract Dictionary<Point2D, TileOverrideReference> XYOverrides { get; }

        [JsonConstructor] protected Map()
        {
        }

        /// <summary>
        ///     Filthy little map to assign single letter to map elements
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        private static char GetMapLetter(byte tile)
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

        public AStar GetAStarByMapUnit(MapUnit mapUnit)
        {
            WalkableType walkableType = GetWalkableTypeByMapUnit(mapUnit);

            return GetAStarByWalkableType(walkableType);
        }

        public AStar GetAStarByWalkableType(WalkableType walkableType)
        {
            if (!_aStarDictionary.ContainsKey(walkableType))
            {
                throw new Ultima5ReduxException("Tried to get AStar with walkableType=" + walkableType + " in class " +
                                                GetType());
            }

            return _aStarDictionary[walkableType];
        }

        public TileOverrideReference GetTileOverride(in Point2D xy)
        {
            return XYOverrides[xy];
        }

        public TileReference GetTileReference(in Point2D xy)
        {
            if (IsXYOverride(xy))
                return GameReferences.SpriteTileReferences.GetTileReference(GetTileOverride(xy).SpriteNum);

            return GameReferences.SpriteTileReferences.GetTileReference(TheMap[xy.X][xy.Y]);
        }

        public bool IsAStarMap(WalkableType type) => _aStarDictionary.ContainsKey(type);

        public bool IsXYOverride(in Point2D xy)
        {
            return XYOverrides != null && XYOverrides.ContainsKey(xy);
        }

        public void SetWalkableTile(in Point2D xy, bool bWalkable, WalkableType walkableType)
        {
            Debug.Assert(xy.X < _aStarNodes[walkableType].Count && xy.Y < _aStarNodes[walkableType][0].Count);
            _aStarNodes[walkableType][xy.X][xy.Y].Walkable = bWalkable;
        }

        protected internal abstract WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit);

        protected internal void RecalculateWalkableTile(in Point2D xy, WalkableType walkableType)
        {
            SetWalkableTile(xy, IsTileWalkable(xy, walkableType), walkableType);
        }

        /// <summary>
        ///     Prints the map in ASCII on the console
        /// </summary>
        /// <param name="map">map object</param>
        /// <param name="xOffset">where to start the top left origin (row) </param>
        /// <param name="yOffset">where to start the top left origin (column) </param>
        /// <param name="xTilesToPrint">how many tiles to print vertically</param>
        /// <param name="yTilesToPrint">how many tiles to print horizontally</param>
        protected static void PrintMapSection(byte[][] map, int xOffset, int yOffset, int xTilesToPrint,
            int yTilesToPrint)
        {
            for (int curRow = yOffset; curRow < yTilesToPrint + yOffset; curRow++)
            {
                for (int curCol = xOffset; curCol < xTilesToPrint + xOffset; curCol++)
                {
                    if (curCol % xTilesToPrint == 0) Console.WriteLine("");
                    byte mapTile = map[curCol][curRow];
                    Console.Write(GetMapLetter(mapTile));
                }
            }
        }

        /// <summary>
        ///     Calculates an appropriate A* weight based on the current tile as well as the surrounding tiles
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        protected abstract float GetAStarWeight(in Point2D xy);

        protected virtual bool IsTileWalkable(TileReference tileReference, WalkableType walkableType)
        {
            if (walkableType == WalkableType.CombatWater)
            {
                return tileReference.IsWaterEnemyPassable;
            }

            bool bIsWalkable = tileReference.IsWalking_Passable ||
                               tileReference.Index == GameReferences.SpriteTileReferences
                                   .GetTileReferenceByName("RegularDoor").Index ||
                               tileReference.Index == GameReferences.SpriteTileReferences
                                   .GetTileReferenceByName("RegularDoorView").Index ||
                               tileReference.Index == GameReferences.SpriteTileReferences
                                   .GetTileReferenceByName("LockedDoor").Index || tileReference.Index ==
                               GameReferences.SpriteTileReferences.GetTileReferenceByName("LockedDoorView").Index;

            return bIsWalkable;
        }

        /// <summary>
        ///     Builds the A* map to be used for NPC pathfinding
        /// </summary>
        protected void InitializeAStarMap(WalkableType walkableType)
        {
            Debug.Assert(TheMap != null);
            Debug.Assert(TheMap.Length > 0);
            int nXTiles = TheMap[0].Length;
            int nYTiles = TheMap.Length;

            // load the A-Star compatible map into memory
            List<List<Node>> aStarNodesLists = Utils.Init2DList<Node>(nXTiles, nYTiles);
            _aStarNodes.Add(walkableType, aStarNodesLists);

            for (int x = 0; x < nXTiles; x++)
            {
                for (int y = 0; y < nYTiles; y++)
                {
                    TileReference currentTile = GameReferences.SpriteTileReferences.GetTileReference(TheMap[x][y]);

                    bool bIsWalkable = IsTileWalkable(currentTile, walkableType);

                    float fWeight = GetAStarWeight(new Point2D(x, y));

                    Node node = new(new Point2D(x, y), bIsWalkable, fWeight);
                    aStarNodesLists[x].Add(node);
                }
            }

            _aStarDictionary.Add(walkableType, new AStar(aStarNodesLists));
        }

        protected bool IsTileWalkable(in Point2D xy, WalkableType walkableType)
        {
            if (IsOpenDoor(xy)) return true;
            TileReference tileReference = GetTileReference(xy);
            return (IsTileWalkable(tileReference, walkableType));
        }

        #region FLOOD FILL

        // FLOOD FILL STUFF
        public bool[][] VisibleOnMap { get; protected set; }
        private readonly List<bool[][]> _testForVisibility = new();

        protected const int VISIBLE_IN_EACH_DIRECTION_OF_AVATAR = 10;
        protected Point2D AvatarXyPos;
        public bool TouchedOuterBorder { get; protected set; }

        protected abstract bool IsRepeatingMap { get; }

        internal void ClearOpenDoors()
        {
            _openDoors.Clear();
        }

        private bool SetVisibleTile(int x, int y)
        {
            if ((x < 0 || x > NumOfXTiles - 1 || (y < 0 || y > NumOfYTiles - 1)))
            {
                return false;
            }

            VisibleOnMap[x][y] = true;
            return true;
        }

        private void SetSurroundingTilesVisible(int x, int y, bool bIncludeDiagonal)
        {
            SetVisibleTile(x - 1, y);
            SetVisibleTile(x + 1, y);
            SetVisibleTile(x, y - 1);
            SetVisibleTile(x, y + 1);
            if (!bIncludeDiagonal) return;
            SetVisibleTile(x - 1, y - 1);
            SetVisibleTile(x + 1, y + 1);
            SetVisibleTile(x - 1, y + 1);
            SetVisibleTile(x + 1, y - 1);
        }

        /// <summary>
        ///     Recursive method for determining which tiles are visible and which are hidden based on the Avatar's
        ///     current position
        /// </summary>
        /// <param name="y"></param>
        /// <param name="bFirst">is this the initial call to the method?</param>
        /// <param name="nCharacterIndex"></param>
        /// <param name="overrideAvatarPos"></param>
        /// <param name="bAlwaysLookThroughWindows"></param>
        /// <param name="x"></param>
        protected void FloodFillMap(int x, int y, bool bFirst, int nCharacterIndex = 0,
            Point2D overrideAvatarPos = null, bool bAlwaysLookThroughWindows = false)
        {
            if (bFirst)
            {
                VisibleOnMap = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles);
            }

            Point2D characterPosition = overrideAvatarPos == null ? AvatarXyPos : overrideAvatarPos;

            int nAdjustedX = x, nAdjustedY = y;

            // let's check to make sure it is within bounds
            if (IsRepeatingMap)
            {
                nAdjustedX = Point2D.AdjustToMax(x, NumOfXTiles);
                nAdjustedY = Point2D.AdjustToMax(y, NumOfYTiles);
            }
            else
            {
                if (nAdjustedX < 0 || nAdjustedY < 0) return;
                if (nAdjustedX > NumOfXTiles - 1 || nAdjustedY > NumOfYTiles - 1) return;
            }

            if (_testForVisibility[nCharacterIndex][nAdjustedX][nAdjustedY]) return; // already did it
            _testForVisibility[nCharacterIndex][nAdjustedX][nAdjustedY] = true;

            // if it blocks light then we make it visible but do not make subsequent tiles visible
            TileReference tileReference =
                GameReferences.SpriteTileReferences.GetTileReference(TheMap[nAdjustedX][nAdjustedY]);

            bool bBlocksLight = tileReference.BlocksLight // if it says it blocks light AND 
                                && !bFirst // it is not the first tile (aka the one you are on) AND
                                && !(tileReference.IsWindow &&
                                     (characterPosition.IsWithinNFourDirections(nAdjustedX, nAdjustedY) ||
                                      bAlwaysLookThroughWindows))
                                && !IsOpenDoor(new Point2D(x, y)) // it's not an open door 
                ; //  you are not next to a window

            // if we are on a tile that doesn't block light then we automatically see things in every direction
            if (!bBlocksLight)
            {
                SetSurroundingTilesVisible(nAdjustedX, nAdjustedY, true);
            }

            // if we are this far then we are certain that we will make this tile visible
            SetVisibleTile(nAdjustedX, nAdjustedY);

            // if the tile blocks the light then we don't calculate the surrounding tiles
            if (bBlocksLight) return;

            int nTilesMax = NumOfXTiles - 1;

            void floodFillIfInside(int nXDiff, int nYDiff)
            {
                // if we aren't on a repeating map then we check to see if it is out of bounds
                // if we are then we note that the flood fill hit the edges
                if (!bFirst && !IsRepeatingMap &&
                    Point2D.IsOutOfRangeStatic(nAdjustedX + nXDiff, nAdjustedY + nYDiff, nTilesMax, nTilesMax))
                {
                    TouchedOuterBorder = true;
                    return;
                }

                FloodFillMap(nAdjustedX + nXDiff, nAdjustedY + nYDiff, false, nCharacterIndex, characterPosition,
                    bAlwaysLookThroughWindows);
            }

            floodFillIfInside(0, 1);
            floodFillIfInside(1, 0);
            floodFillIfInside(0, -1);
            floodFillIfInside(-1, 0);

            if (bFirst)
            {
                // we ONLY do the diagonals during the very first 
                floodFillIfInside(-1, -1);
                floodFillIfInside(1, 1);
                floodFillIfInside(-1, +1);
                floodFillIfInside(1, -1);
            }
        }

        protected virtual Point2D GetAdjustedPos(in Point2D.Direction direction, in Point2D xy)
        {
            return xy.GetAdjustedPosition(direction, NumOfXTiles - 1, NumOfYTiles - 1);
        }

        /// <summary>
        ///     Refreshes the map that tracks which tiles have been tested for visibility
        /// </summary>
        /// <param name="nCharacters"></param>
        protected void RefreshTestForVisibility(int nCharacters)
        {
            _testForVisibility.Clear();
            for (int i = 0; i < nCharacters; i++)
            {
                _testForVisibility.Add(Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles));
            }
        }

        protected void SetMaxVisibleArea(in Point2D startPos, int nVisibleTiles)
        {
            if (nVisibleTiles < 3) throw new Ultima5ReduxException("Can't set visible area if smaller than 3");
            if (startPos == null) throw new Ultima5ReduxException("Must have a proper start position");
            if (_testForVisibility.Count <= 0)
                throw new Ultima5ReduxException("You must refresh the visible area before setting the max");

            int nVisibleTilesPerSide = nVisibleTiles / 2;
            int nStartX = startPos.X, nStartY = startPos.Y;

            for (int nXDiff = 0; nXDiff < nVisibleTiles; nXDiff++)
            {
                int nX = Point2D.AdjustToMax(nStartX - nVisibleTilesPerSide + nXDiff, NumOfXTiles);
                int nTopY = Point2D.AdjustToMax(nStartY - nVisibleTilesPerSide, NumOfYTiles);
                int nBottomY = Point2D.AdjustToMax(nStartY + nVisibleTilesPerSide, NumOfYTiles);

                _testForVisibility[0][nX][nTopY] = true;
                _testForVisibility[0][nX][nBottomY] = true;
            }

            for (int nYDiff = 0; nYDiff < nVisibleTiles; nYDiff++)
            {
                int nY = Point2D.AdjustToMax(nStartY - nVisibleTilesPerSide + nYDiff, NumOfYTiles);
                int nTopX = Point2D.AdjustToMax(nStartX - nVisibleTilesPerSide, NumOfXTiles);
                int nBottomX = Point2D.AdjustToMax(nStartX + nVisibleTilesPerSide, NumOfXTiles);

                _testForVisibility[0][nTopX][nY] = true;
                _testForVisibility[0][nBottomX][nY] = true;
            }
        }

        public virtual void RecalculateVisibleTiles(in Point2D initialFloodFillPosition)
        {
            // XRay Mode makes sure you can see every tile
            if (XRayMode)
            {
                Utils.Set2DArrayAllToValue(VisibleOnMap, true);
                return;
            }

            TouchedOuterBorder = false;
            AvatarXyPos = initialFloodFillPosition;

            RefreshTestForVisibility(1);
            SetMaxVisibleArea(AvatarXyPos, TOTAL_VISIBLE_TILES);
            FloodFillMap(initialFloodFillPosition.X, initialFloodFillPosition.Y, true);
        }

        public void SetOpenDoor(in Point2D xy)
        {
            TileReference tileReference = GetTileReference(xy);
            Debug.Assert(GameReferences.SpriteTileReferences.IsDoor(tileReference.Index),
                "you tried to set an open door on a tile that is not an open door");

            _openDoors.Add(xy, 10);

            if (IsAStarMap(WalkableType.CombatLand)) SetWalkableTile(xy, true, WalkableType.CombatLand);
            if (IsAStarMap(WalkableType.StandardWalking)) SetWalkableTile(xy, true, WalkableType.StandardWalking);
        }

        public bool IsOpenDoor(in Point2D xy)
        {
            return _openDoors.ContainsKey(xy) && _openDoors[xy] > 0;
        }

        public void CloseDoor(in Point2D xy)
        {
            TileReference tileReference = GetTileReference(xy);
            Debug.Assert(GameReferences.SpriteTileReferences.IsDoor(tileReference.Index),
                "you tried to set an open door on a tile that is not an open door");
            Debug.Assert(_openDoors.ContainsKey(xy), "tried to close a door that wasn't open");

            _openDoors.Remove(xy);
        }

        #endregion
    }
}