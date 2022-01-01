using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.External;
using Ultima5Redux.MapUnits;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract] public abstract class Map
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Maps { Small = -1, Overworld, Underworld, Combat }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum WalkableType { StandardWalking, CombatLand, CombatWater, CombatFlyThroughWalls, CombatLandAndWater }

        [DataMember(Name = "OpenDoors")] private readonly Dictionary<Point2D, int> _openDoors = new();

        [DataMember] public bool XRayMode { get; set; }

        [IgnoreDataMember] //(Name = "AStarDictionary")]
        private readonly Dictionary<WalkableType, AStar> _aStarDictionary = new();

        [IgnoreDataMember] //[DataMember(Name = "AStarNodes")]
        private readonly Dictionary<WalkableType, List<List<Node>>> _aStarNodes = new();

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

        protected abstract WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit);

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

                    Node node = new(new Point2D(x, y),
                        //new Vector2(x, y),
                        bIsWalkable, fWeight);
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

        protected void RecalculateWalkableTile(in Point2D xy, WalkableType walkableType)
        {
            SetWalkableTile(xy, IsTileWalkable(xy, walkableType), walkableType);
        }

        #region FLOOD FILL

        // FLOOD FILL STUFF
        public bool[][] VisibleOnMap { get; protected set; } //=Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles);
        protected readonly List<bool[][]> TestForVisibility = new();
        protected readonly int VisibleInEachDirectionOfAvatar = 10;
        protected int NVisibleLargeMapTiles;
        protected Point2D AvatarXyPos;
        protected bool TouchedOuterBorder;

        protected abstract bool IsRepeatingMap { get; }

        internal void ClearOpenDoors()
        {
            _openDoors.Clear();
        }

        /// <summary>
        ///     Attempts to set the visible tile flag
        /// </summary>
        /// <param name="visibleTilePos"></param>
        /// <returns>true if the coordinate is out of bounds</returns>
        private bool SetVisibleTile(in Point2D visibleTilePos)
        {
            if (visibleTilePos == null) return true;
            if (!visibleTilePos.IsOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1))
                VisibleOnMap[visibleTilePos.X][visibleTilePos.Y] = true;
            //else
            //  VisibleOnMap[visibleTilePos.X][visibleTilePos.Y] = false;
            return false;
        }

        private void SetSurroundingTilesVisible(in Point2D xy, bool bIncludeDiagonal)
        {
            SetVisibleTile(new Point2D(xy.X - 1, xy.Y).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1));
            SetVisibleTile(new Point2D(xy.X + 1, xy.Y).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1));
            SetVisibleTile(new Point2D(xy.X, xy.Y - 1).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1));
            SetVisibleTile(new Point2D(xy.X, xy.Y + 1).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1));
            if (!bIncludeDiagonal) return;
            SetVisibleTile(
                new Point2D(xy.X - 1, xy.Y - 1).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1));
            SetVisibleTile(
                new Point2D(xy.X + 1, xy.Y + 1).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1));
            SetVisibleTile(
                new Point2D(xy.X - 1, xy.Y + 1).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1));
            SetVisibleTile(
                new Point2D(xy.X + 1, xy.Y - 1).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1));
        }

        /// <summary>
        ///     Recursive method for determining which tiles are visible and which are hidden based on the Avatar's
        ///     current position
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="bFirst">is this the initial call to the method?</param>
        /// <param name="nCharacterIndex"></param>
        /// <param name="overrideAvatarPos"></param>
        /// <param name="bAlwaysLookThroughWindows"></param>
        protected void FloodFillMap(in Point2D xy, bool bFirst, int nCharacterIndex = 0,
            Point2D overrideAvatarPos = null,
            bool bAlwaysLookThroughWindows = false)
        {
            if (xy == null)
            {
                TouchedOuterBorder = true;
                return; // out of bounds
            }

            if (bFirst)
            {
                Utils.Set2DArrayAllToValue(VisibleOnMap, true);
                Utils.Set2DArrayAllToValue(TestForVisibility[nCharacterIndex], false);
            }

            Point2D characterPosition = overrideAvatarPos == null ? AvatarXyPos : overrideAvatarPos;

            Point2D adjustedXy = xy.Copy();

            // let's check to make sure it is within bounds
            if (IsRepeatingMap)
                adjustedXy.AdjustXAndYToMax(NumOfXTiles);

            if (TestForVisibility[nCharacterIndex][adjustedXy.X][adjustedXy.Y]) return; // already did it
            TestForVisibility[nCharacterIndex][adjustedXy.X][adjustedXy.Y] = true;

            // if it blocks light then we make it visible but do not make subsequent tiles visible
            TileReference tileReference =
                GameReferences.SpriteTileReferences.GetTileReference(TheMap[adjustedXy.X][adjustedXy.Y]);

            bool bBlocksLight = tileReference.BlocksLight // if it says it blocks light AND 
                                && !bFirst // it is not the first tile (aka the one you are on) AND
                                && !IsOpenDoor(xy) // it's not an open door 
                                && !(tileReference.IsWindow && (characterPosition.IsWithinNFourDirections(adjustedXy) ||
                                                                bAlwaysLookThroughWindows)); //  you are not next to a window

            // if we are on a tile that doesn't block light then we automatically see things in every direction
            if (!bBlocksLight)
            {
                SetSurroundingTilesVisible(adjustedXy, true);
            }

            // if we are this far then we are certain that we will make this tile visible
            TouchedOuterBorder |= SetVisibleTile(xy);

            // if the tile blocks the light then we don't calculate the surrounding tiles
            if (bBlocksLight) return;

            FloodFillMap(GetAdjustedPos(Point2D.Direction.Up, xy), false, nCharacterIndex, characterPosition,
                bAlwaysLookThroughWindows);
            FloodFillMap(GetAdjustedPos(Point2D.Direction.Down, xy), false, nCharacterIndex, characterPosition,
                bAlwaysLookThroughWindows);
            FloodFillMap(GetAdjustedPos(Point2D.Direction.Left, xy), false, nCharacterIndex, characterPosition,
                bAlwaysLookThroughWindows);
            FloodFillMap(GetAdjustedPos(Point2D.Direction.Right, xy), false, nCharacterIndex, characterPosition,
                bAlwaysLookThroughWindows);

            if (!bFirst) return;

            // if it is the first call (avatar tile) then we always check the diagonals as well 
            FloodFillMap(new Point2D(xy.X - 1, xy.Y - 1).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1),
                false, nCharacterIndex, characterPosition, bAlwaysLookThroughWindows);
            FloodFillMap(new Point2D(xy.X + 1, xy.Y + 1).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1),
                false, nCharacterIndex, characterPosition, bAlwaysLookThroughWindows);
            FloodFillMap(new Point2D(xy.X - 1, xy.Y + 1).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1),
                false, nCharacterIndex, characterPosition, bAlwaysLookThroughWindows);
            FloodFillMap(new Point2D(xy.X + 1, xy.Y - 1).GetPoint2DOrNullOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1),
                false, nCharacterIndex, characterPosition, bAlwaysLookThroughWindows);
        }

        protected virtual Point2D GetAdjustedPos(Point2D.Direction direction, Point2D xy)
        {
            return xy.GetAdjustedPosition(direction, NumOfXTiles - 1, NumOfYTiles - 1);
        }

        public virtual void RecalculateVisibleTiles(in Point2D initialFloodFillPosition)
        {
            VisibleOnMap ??= Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles);

            // XRay Mode makes sure you can see every tile
            if (XRayMode)
            {
                Utils.Set2DArrayAllToValue(VisibleOnMap, true);
                //VisibleOnMap = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles, true);
                return;
            }

            //VisibleOnMap = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles);
            //TestForVisibility = new List<bool[][]>();
            // reinitialize the array for all potential party members
            if (TestForVisibility.Count <= 0)
                for (int i = 0; i < PlayerCharacterRecords.MAX_PARTY_MEMBERS; i++)
                {
                    TestForVisibility.Add(Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles));
                }

            TouchedOuterBorder = false;
            AvatarXyPos = initialFloodFillPosition;

            FloodFillMap(initialFloodFillPosition, true);
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