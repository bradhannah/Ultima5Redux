using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.References.Maps
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SingleCombatMapReference
    {
        public enum BritanniaCombatMaps
        {
            None = -2, BoatCalc = -1, CampFire = 0, Swamp = 1, Glade = 2, Treed = 3, Desert = 4, CleanTree = 5,
            Mountains = 6, BigBridge = 7, Brick = 8, Basement = 9, Psychedelic = 10, BoatOcean = 11, BoatNorth = 12,
            BoatSouth = 13, BoatBoat = 14, Bay = 15
        }

        public enum CombatMapSpriteType { Nothing, Thing, AutoSelected, EncounterBased }

        public enum Dungeon
        {
            Deceit = 27, Despise = 28, Destard = 29, Wrong = 30, Covetous = 31, Shame = 32, Hythloth = 33, Doom = 34
        }

        public enum EntryDirection { East = 0, West = 1, South = 2, North = 3, UpLadder, DownLadder }

        /// <summary>
        ///     The territory that the combat map is in. This matters most for determining data files.
        /// </summary>
        public enum Territory { Britannia = 0, Dungeon }

        /// <summary>
        ///     How many bytes for each combat map entry in data file
        /// </summary>
        private const int MAP_BYTE_COUNT = 0x0160;

        private const int NUM_ENTRY_DIRECTIONS = 4;
        private const int NUM_PLAYERS = 6;

        public const int NUM_MAP_UNITS = 16;

        public const int XTILES = 11;
        public const int YTILES = 11;
        private readonly CombatMapReferences.CombatMapData _combatMapData;
        private readonly List<Point2D> _mapUnitPositions = new(NUM_MAP_UNITS);
        private readonly List<byte> _mapUnitSprites = new(NUM_MAP_UNITS);

        private readonly Dictionary<int, bool> _enterDirectionDictionary = new();

        private readonly List<List<Point2D>> _playerPositionsByDirection =
            Utils.Init2DList<Point2D>(Enum.GetNames(typeof(EntryDirection)).Length, NUM_PLAYERS);

        private readonly Dictionary<EntryDirection, List<Point2D>> _entryDirectionAndPlayersDictionary = new();

        private readonly TileReferences _tileReferences;

        public readonly byte[][] TheMap;

        private int TotalDirections => Enum.GetNames(typeof(EntryDirection)).Length;

        /// <summary>
        ///     The number of the combat map (order in data file)
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public int CombatMapNum { get; }

        /// <summary>
        ///     Brief description of the combat map
        /// </summary>
        public string Description => _combatMapData.Description;

        public Dungeon DungeonLocation => Dungeon.Covetous;

        public bool HasMagicDoor =>
            DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("MagicLockDoor")) ||
            DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("MagicLockDoorWithView"));

        public bool HasRegularDoor =>
            DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("RegularDoor")) ||
            DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("LockedDoor")) ||
            DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("RegularDoorView")) ||
            DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("LockedDoorView"));

        public bool HasTriggers => true;

        public TriggerTiles TheTriggerTiles { get; set; }

        /// <summary>
        ///     Generated
        /// </summary>
        /// <remarks>this needs to rewritten when we understand how the data files refer to Combat Maps</remarks>
        public byte Id => (byte)MapTerritory;

        public int Index => CombatMapNum;
        public bool IsBroke => false;
        public bool LaddersDown => DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("LadderDown"));
        public bool LaddersUp => DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("LadderUp"));

        /// <summary>
        ///     Territory of the combat map
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public Territory MapTerritory { get; }

        public string Name => MapTerritory == Territory.Britannia ? Description : "Dungeon-" + CombatMapNum;
        public string Notes => "No Notes";
        public bool OtherStart => false;

        public bool SpecialEnemyComputation => false;

        /// <summary>
        ///     Create the reference based on territory and a map number
        /// </summary>
        /// <param name="mapTerritory">Britannia or Dungeon</param>
        /// <param name="nCombatMapNum">map number in data file (0,1,2,3....)</param>
        /// <param name="dataChunks"></param>
        /// <param name="combatMapData"></param>
        /// <param name="tileReferences"></param>
        public SingleCombatMapReference(Territory mapTerritory, int nCombatMapNum,
            DataChunks<CombatMapReferences.DataChunkName> dataChunks, CombatMapReferences.CombatMapData combatMapData,
            TileReferences tileReferences)
        {
            _combatMapData = combatMapData;
            _tileReferences = tileReferences;

            MapTerritory = mapTerritory;
            CombatMapNum = nCombatMapNum;

            // copying the array to a simpler format
            TheMap = Utils.Init2DByteArray(XTILES, YTILES);

            InitializeMap(nCombatMapNum, dataChunks);
        }

        private void InitializeMap(int nCombatMapNum, DataChunks<CombatMapReferences.DataChunkName> dataChunks)
        {
            const int nBytesPerRow = 0x20;

            List<DataChunk> fullRows = new();
            List<DataChunk> justMapRowsAndData = new();

            int nMapOffset = nCombatMapNum * MAP_BYTE_COUNT;

            for (int nRow = 0; nRow < XTILES; nRow++)
            {
                // load the entire row
                DataChunk rowChunk = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                    $"Raw row data for row: {nRow}", //-V3138
                    nMapOffset + (nBytesPerRow * nRow), nBytesPerRow, 0x00, CombatMapReferences.DataChunkName.Unused);
                fullRows.Add(rowChunk);

                // load only the data for the maps
                DataChunk justMapRowChunk = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                    $"Tiles for row {nRow}", //-V3138
                    nMapOffset + (nBytesPerRow * nRow), XTILES, 0x00, CombatMapReferences.DataChunkName.Unused);
                justMapRowsAndData.Add(justMapRowChunk);
            }

            // get and build the map sprites 
            for (int nRow = 0; nRow < XTILES; nRow++)
            {
                List<byte> mapRowByteList = justMapRowsAndData[nRow].GetAsByteList();
                for (int nCol = 0; nCol < mapRowByteList.Count; nCol++)
                {
                    byte sprite = mapRowByteList[nCol];
                    TheMap[nCol][nRow] = sprite;
                }
            }

            // get and build the start position of all the player characters
            for (int nDirection = 0; nDirection < NUM_ENTRY_DIRECTIONS; nDirection++)
            {
                for (int nPlayer = 0; nPlayer < NUM_PLAYERS; nPlayer++)
                {
                    int nDirectionRow = nDirection + 1;
                    int nPlayerColX = nPlayer + XTILES;
                    int nPlayerColY = nPlayer + XTILES + NUM_PLAYERS;

                    byte x = fullRows[nDirectionRow].GetByte(nPlayerColX);
                    byte y = fullRows[nDirectionRow].GetByte(nPlayerColY);

                    Point2D playerPosition = new(x, y);
                    _playerPositionsByDirection[nDirection].Add(playerPosition);

                    Debug.Assert(_playerPositionsByDirection[nDirection][nPlayer].X <= XTILES);
                    Debug.Assert(_playerPositionsByDirection[nDirection][nPlayer].Y <= YTILES);
                }
            }

            // Set up all the monsters and entities that can be on the map

            Dictionary<Point2D, bool> duplicateMapUnitPositionDictionary = new();
            for (int nMapUnit = 0; nMapUnit < NUM_MAP_UNITS; nMapUnit++)
            {
                int nMapUnitCol = nMapUnit + XTILES;
                const int nMapUnitTileRow = 5;
                const int nMapUnitXRow = 6;
                const int nMapUnitYRow = 7;

                Point2D mapUnitPosition = new(fullRows[nMapUnitXRow].GetByte(nMapUnitCol),
                    fullRows[nMapUnitYRow].GetByte(nMapUnitCol));
                byte enemySprite = fullRows[nMapUnitTileRow].GetByte(nMapUnitCol);

                if (!duplicateMapUnitPositionDictionary.ContainsKey(mapUnitPosition))
                {
                    _mapUnitSprites.Add(enemySprite);
                    duplicateMapUnitPositionDictionary.Add(mapUnitPosition, true);
                    _mapUnitPositions.Add(mapUnitPosition);
                }
                else
                {
                    // it's a duplicate position, so we avoid adding it again, otherwise things get screwy
                    _mapUnitSprites.Add(0);
                    _mapUnitPositions.Add(new Point2D(0, 0));
                }
            }

            // Set up all the trigger tiles
            const int nTriggerTiles = 8;

            TheTriggerTiles = new();

            for (int nTrigger = 0; nTrigger < nTriggerTiles; nTrigger++)
            {
                const int nTriggerSpriteRow = 0;
                const int nTriggerPositionRow = 8;
                const int nTriggerPosition1TileRow = 9;
                const int nTriggerPosition2TileRow = 10;

                int nTriggerSpriteCol = nTrigger + XTILES;
                int nTriggerPositionXCol = nTrigger + XTILES;
                int nTriggerPositionYCol = nTrigger + XTILES + nTriggerTiles;

                int nSprite = fullRows[nTriggerSpriteRow].GetByte(nTriggerSpriteCol);
                TileReference triggerSprite = GameReferences.SpriteTileReferences.GetTileReference(nSprite);

                Point2D triggerPosition = new(fullRows[nTriggerPositionRow].GetByte(nTriggerPositionXCol),
                    fullRows[nTriggerPositionRow].GetByte(nTriggerPositionYCol));

                Point2D triggerNewPosition1 = new(fullRows[nTriggerPosition1TileRow].GetByte(nTriggerPositionXCol),
                    fullRows[nTriggerPosition1TileRow].GetByte(nTriggerPositionYCol));
                Point2D triggerNewPosition2 = new(fullRows[nTriggerPosition2TileRow].GetByte(nTriggerPositionXCol),
                    fullRows[nTriggerPosition2TileRow].GetByte(nTriggerPositionYCol));

                if (triggerPosition.X == 0 || triggerPosition.Y == 0) continue;
                TheTriggerTiles.AddNewTrigger(triggerSprite, triggerPosition, triggerNewPosition1, triggerNewPosition2);
            }
        }

        private int GetRawEnemySprite(int nIndex)
        {
            Debug.Assert(nIndex is < NUM_MAP_UNITS and >= 0);
            return _mapUnitSprites[nIndex];
        }

        public static string GetCsvHeader() =>
            "Index, Name, DungeonLocation, DirEastLeft, DirWestRight, DirNorthUp, DirSouthDown, LaddersUp, LaddersDown, HasTriggers, Notes";

        public bool DoesTileReferenceOccurOnMap(TileReference tileReference) =>
            GetNumberOfTileReferencesOnMap(tileReference) > 0;

        public CombatMapSpriteType GetAdjustedEnemySprite(int nIndex, out int nSpriteIndex)
        {
            int nEnemyRawSprite = GetRawEnemySprite(nIndex);
            nSpriteIndex = nEnemyRawSprite + 0x100;

            switch (nEnemyRawSprite)
            {
                // enemy sprite of 0 indicates no monster
                case 0:
                    return CombatMapSpriteType.Nothing;
                // it's a chest or something like it
                case <= 15:
                    return CombatMapSpriteType.Thing;
                // it's a dead body or blood spatter
                case 30:
                case 31:
                    return CombatMapSpriteType.Thing;
                // it's determined by the encounter 
                case 112:
                    if (MapTerritory == Territory.Dungeon)
                        return CombatMapSpriteType.AutoSelected;
                    return CombatMapSpriteType.EncounterBased;
            }

            // if the sprite is lower than the crown index, then we add 4
            // OR
            // it's a Shadow Lord, but it breaks convention
            //if (nSpriteIndex is < 436 or >= 504) nSpriteIndex += 4;

            return CombatMapSpriteType.AutoSelected;
            if (nSpriteIndex is < 316 or > 511)
            {
                throw new Ultima5ReduxException(
                    $"Tried to get adjusted enemy sprite with index={nIndex} and raw sprite={nEnemyRawSprite}");
            }

            // it lies - water maps seem to pretend they are auto selected
            if (Index is 11 or 13 or 15) return CombatMapSpriteType.EncounterBased;
            // pirates
            if (Index is 12 or 14) return CombatMapSpriteType.AutoSelected;

            return CombatMapSpriteType.AutoSelected;
        }

        public string GetAsCsvLine() =>
            $"{Index}, {Name}, {DungeonLocation}, {IsValidDirection(EntryDirection.East)}, " +
            $"{IsValidDirection(EntryDirection.West)}, {IsValidDirection(EntryDirection.North)}, " +
            $"{IsValidDirection(EntryDirection.South)}, {LaddersUp}, {LaddersDown}, {HasTriggers}, {Notes}";

        public Point2D GetEnemyPosition(int nIndex)
        {
            Debug.Assert(nIndex < NUM_MAP_UNITS && nIndex >= 0);
            return _mapUnitPositions[nIndex];
        }

        public IEnumerable<EntryDirection> GetEntryDirections() =>
            Enum.GetValues(typeof(EntryDirection)).Cast<EntryDirection>().Where(IsValidDirection);

        public int GetNumberOfTileReferencesOnMap(TileReference tileReference)
        {
            int nTotal = 0;
            for (int x = 0; x < XTILES; x++)
            {
                for (int y = 0; y < YTILES; y++)
                {
                    if (TheMap[x][y] == tileReference.Index) nTotal++;
                }
            }

            return nTotal;
        }

        public List<Point2D> GetPlayerStartPositions(EntryDirection entryDirection)
        {
            Debug.Assert((int)entryDirection >= 0 && (int)entryDirection <= TotalDirections);
            return _playerPositionsByDirection[(int)entryDirection];
        }

        public bool IsEnterable(EntryDirection entryDirection)
        {
            return _enterDirectionDictionary[(int)entryDirection];
        }

        public bool IsEntryDirectionValid(EntryDirection entryDirection)
        {
            List<Point2D> points = GetPlayerStartPositions(entryDirection);
            if (points.Count == 0 || points[0].X <= 0) return false;

            List<Point2D> characterPositions = new();
            // we walk through each of the character positions and if one is repeated then we know it's not valid
            foreach (Point2D point in points)
            {
                if (characterPositions.Contains(point)) return false;
                characterPositions.Add(point);
            }

            return true;
        }

        public bool IsValidDirection(EntryDirection entryDirection) => IsEntryDirectionValid(entryDirection);

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private readonly struct PointAndTileReference
        {
            public PointAndTileReference(Point2D point, TileReference tileReference)
            {
                Point = point;
                TheTileReference = tileReference;
            }

            private Point2D Point { get; }
            private TileReference TheTileReference { get; }
        }
    }
}