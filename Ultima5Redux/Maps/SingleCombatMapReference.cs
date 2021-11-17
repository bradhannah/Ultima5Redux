using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Ultima5Redux.Data;

namespace Ultima5Redux.Maps
{
    [JsonObject(MemberSerialization.OptIn)] public class SingleCombatMapReference
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

        public enum EntryDirection
        {
            Direction0 = 0, Direction1 = 1, Direction2 = 2, Direction3 = 3, UpLadder, DownLadder
        }

        /// <summary>
        ///     The territory that the combat map is in. This matters most for determining data files.
        /// </summary>
        public enum Territory { Britannia = 0, Dungeon }

        public const int XTILES = 11;
        public const int YTILES = 11;

        /// <summary>
        ///     How many bytes for each combat map entry in data file
        /// </summary>
        private const int MAP_BYTE_COUNT = 0x0160;

        public const int NUM_ENEMIES = 16;
        private const int NUM_PLAYERS = 6;
        private const int NUM_HARD_CODED_DIRECTIONS = 4;
        private readonly CombatMapReferences.CombatMapData _combatMapData;
        private readonly List<Point2D> _enemyPositions = new List<Point2D>(NUM_ENEMIES);
        private readonly List<byte> _enemySprites = new List<byte>(NUM_ENEMIES);

        private readonly Dictionary<EntryDirection, bool> _enterDirectionDictionary =
            new Dictionary<EntryDirection, bool>();

        private readonly List<List<Point2D>> _playerPositionsByDirection =
            Utils.Init2DList<Point2D>(Enum.GetNames(typeof(EntryDirection)).Length, NUM_PLAYERS);

        private readonly TileReferences _tileReferences;

        public readonly byte[][] TheMap;


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
            int nMapOffset = nCombatMapNum * MAP_BYTE_COUNT;

            MapTerritory = mapTerritory;
            CombatMapNum = nCombatMapNum;
            const int nBytesPerRow = 0x20;

            // copying the array to a simpler format
            TheMap = Utils.Init2DByteArray(XTILES, YTILES);

            Dictionary<EntryDirection, Point2D> ladders = new Dictionary<EntryDirection, Point2D>();

            // get and build the map sprites 
            for (int nRow = 0; nRow < XTILES; nRow++)
            {
                DataChunk rowChunk = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, $"Tiles for row {nRow}", //-V3138
                    nMapOffset + (nBytesPerRow * nRow), XTILES, 0x00, CombatMapReferences.DataChunkName.Unused);
                List<byte> list = rowChunk.GetAsByteList();
                for (int nCol = 0; nCol < list.Count; nCol++)
                {
                    byte sprite = list[nCol];
                    TheMap[nCol][nRow] = sprite;

                    if (_tileReferences.IsLadderUp(sprite))
                        ladders.Add(EntryDirection.UpLadder, new Point2D(nCol, nRow));
                    if (_tileReferences.IsLadderDown(sprite))
                        ladders.Add(EntryDirection.DownLadder, new Point2D(nCol, nRow));
                }
            }

            // build the maps of all four directions and the respective player positions
            for (int nRow = 1, nOffsetFactor = 0; nRow <= NUM_HARD_CODED_DIRECTIONS; nRow++, nOffsetFactor++)
            {
                // Supposed to be 1=east,2=west,3=south,4=north
                // but in reality they are kind of all over!
                List<byte> xPlayerPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                    "Player X positions for row #" + nRow,
                    nMapOffset + (nBytesPerRow * nOffsetFactor) + 0xB, 0x06).GetAsByteList();
                List<byte> yPlayerPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                    "Player Y positions for row #" + nRow,
                    nMapOffset + (nBytesPerRow * nOffsetFactor) + 0xB + 0x06, 0x06).GetAsByteList();
                for (int nPlayer = 0; nPlayer < NUM_PLAYERS; nPlayer++)
                {
                    // if the X or Y value is above the number of tiles then it indicates a sprite number
                    // which I believe is additional trigger tiles
                    bool bIsEnterable = yPlayerPosList[nPlayer] < YTILES && xPlayerPosList[nPlayer] < XTILES;
                    _enterDirectionDictionary[(EntryDirection)nRow - 1] = bIsEnterable;
                    _playerPositionsByDirection[nRow - 1].Add(bIsEnterable
                        ? new Point2D(xPlayerPosList[nPlayer], yPlayerPosList[nPlayer])
                        : new Point2D(0, 0));

                    Debug.Assert(_playerPositionsByDirection[nRow - 1][nPlayer].X <= XTILES);
                    Debug.Assert(_playerPositionsByDirection[nRow - 1][nPlayer].Y <= YTILES);
                }
            }

            // we also calculate any up or down ladder positions 
            foreach (KeyValuePair<EntryDirection, Point2D> entry in ladders)
            {
                Point2D ladderPoint = entry.Value;
                int nKey = (int)entry.Key;

                _enterDirectionDictionary[entry.Key] = true;
                _playerPositionsByDirection[nKey].Add(ladderPoint.GetAdjustedPosition(0, 1));
                _playerPositionsByDirection[nKey].Add(ladderPoint.GetAdjustedPosition(-1, 0));
                _playerPositionsByDirection[nKey].Add(ladderPoint.GetAdjustedPosition(1, 0));
                _playerPositionsByDirection[nKey].Add(ladderPoint.GetAdjustedPosition(0, -1));
                _playerPositionsByDirection[nKey].Add(ladderPoint.GetAdjustedPosition(-1, -1));
                _playerPositionsByDirection[nKey].Add(ladderPoint.GetAdjustedPosition(1, -1));
            }

            // load the enemy positions and sprites
            int nEnemyXOffset = nMapOffset + (nBytesPerRow * 6) + 0xB;
            int nEnemyYOffset = nMapOffset + (nBytesPerRow * 7) + 0xB;
            int nEnemySpriteOffset = nMapOffset + (nBytesPerRow * 5) + 0xB;
            List<byte> xEnemyPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Enemy X positions",
                nEnemyXOffset, NUM_ENEMIES).GetAsByteList();
            List<byte> yEnemyPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Enemy Y positions",
                nEnemyYOffset, NUM_ENEMIES).GetAsByteList();
            List<byte> spriteEnemyList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Enemy sprite index",
                nEnemySpriteOffset, NUM_ENEMIES).GetAsByteList();

            Debug.Assert(xEnemyPosList.Count == NUM_ENEMIES);
            Debug.Assert(yEnemyPosList.Count == NUM_ENEMIES);
            Debug.Assert(spriteEnemyList.Count == NUM_ENEMIES);

            Dictionary<Point2D, bool> duplicatePositionDictionary = new Dictionary<Point2D, bool>();

            for (int nEnemyIndex = 0; nEnemyIndex < NUM_ENEMIES; nEnemyIndex++)
            {
                Point2D enemyPosition = new Point2D(xEnemyPosList[nEnemyIndex], yEnemyPosList[nEnemyIndex]);

                if (duplicatePositionDictionary.ContainsKey(enemyPosition))
                {
                    // it's a duplicate position, so we avoid adding it again, otherwise things get screwy
                    _enemySprites.Add(0);
                    _enemyPositions.Add(new Point2D(0, 0));
                }
                else
                {
                    _enemySprites.Add(spriteEnemyList[nEnemyIndex]);
                    duplicatePositionDictionary.Add(enemyPosition, true);
                    _enemyPositions.Add(enemyPosition);
                }
            }

            List<Point2D> triggerPositions = new List<Point2D>(XTILES);
            List<TileReference> triggerTileReferences = new List<TileReference>(XTILES);
            Dictionary<Point2D, List<PointAndTileReference>> triggerPointToTileReferences =
                new Dictionary<Point2D, List<PointAndTileReference>>();

            // load the trigger positions
            // these are the Points that a player character hits and causes a triggering event
            const int nTriggerPositions = 8;
            List<byte> triggerXPositions = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Trigger X positions",
                nMapOffset + nBytesPerRow * 9 + XTILES, nTriggerPositions).GetAsByteList();
            List<byte> triggerYPositions = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Trigger Y positions",
                nMapOffset + nBytesPerRow * 9 + YTILES, nTriggerPositions).GetAsByteList();
            for (int i = 0; i < nTriggerPositions; i++)
            {
                triggerPositions.Add(new Point2D(triggerXPositions[i], triggerYPositions[i]));
            }

            // Gather all replacement tile references when trigger occurs
            List<byte> triggerTileIndexes = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Trigger tile indexes",
                nMapOffset + XTILES, nTriggerPositions).GetAsByteList();
            foreach (byte nIndex in triggerTileIndexes)
            {
                triggerTileReferences.Add(tileReferences.GetTileReference(nIndex
                ));
                //+ 0xFF));
            }

            // Gather all positions that change as a result of a trigger
            // the results are split into two different sections
            List<byte> triggerResultXPositions = new List<byte>();
            List<byte> triggerResultYPositions = new List<byte>();
            triggerResultXPositions.AddRange(dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Trigger Result X position #1",
                nMapOffset + nBytesPerRow * 9 + XTILES, nTriggerPositions).GetAsByteList());
            triggerResultYPositions.AddRange(dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Trigger Result Y position #1",
                nMapOffset + nBytesPerRow * 9 + (XTILES * 2), nTriggerPositions).GetAsByteList());
            triggerResultXPositions.AddRange(dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Trigger Result X position #2",
                nMapOffset + nBytesPerRow * 10 + XTILES, nTriggerPositions).GetAsByteList());
            triggerResultYPositions.AddRange(dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Trigger Result Y position #2",
                nMapOffset + nBytesPerRow * 10 + (XTILES * 2), nTriggerPositions).GetAsByteList());
            for (int i = 0; i < nTriggerPositions; i++)
            {
                TileReference newTriggeredTileReference = triggerTileReferences[i];
                // if the index is 255 (was 0) then we know it is indicating it shouldn't used
                if (newTriggeredTileReference.Index == 0x0) continue;

                // grab the two Points that will change as a result of landing on the trigger tile
                Point2D pos1 = new Point2D(triggerResultXPositions[i], triggerResultYPositions[i]);
                Point2D pos2 = new Point2D(triggerResultXPositions[i + nTriggerPositions],
                    triggerResultYPositions[i + nTriggerPositions]);
                Point2D triggeredPosition = triggerPositions[i];

                // if the trigger position has not been recorded yet, then we initialize the list
                // we use a List because every tile has a minimum of 2 changes, but can result in a lot more
                ///// NOTE!!!!! Check this again - it may be putting duplicates in
                if (!triggerPointToTileReferences.ContainsKey(triggeredPosition))
                    triggerPointToTileReferences.Add(triggeredPosition, new List<PointAndTileReference>());

                triggerPointToTileReferences[triggeredPosition]
                    .Add(new PointAndTileReference(pos1, triggerTileReferences[i]));
                if (pos1 != pos2)
                    triggerPointToTileReferences[triggeredPosition]
                        .Add(new PointAndTileReference(pos2, triggerTileReferences[i]));
            }
        }

        private int TotalDirections => Enum.GetNames(typeof(EntryDirection)).Length;

        public bool HasMagicDoor => DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("MagicLockDoor"))
                                    || DoesTileReferenceOccurOnMap(
                                        _tileReferences.GetTileReferenceByName("MagicLockDoorWithView"));

        public bool HasRegularDoor => DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("RegularDoor"))
                                      || DoesTileReferenceOccurOnMap(
                                          _tileReferences.GetTileReferenceByName("LockedDoor"))
                                      || DoesTileReferenceOccurOnMap(
                                          _tileReferences.GetTileReferenceByName("RegularDoorView"))
                                      || DoesTileReferenceOccurOnMap(
                                          _tileReferences.GetTileReferenceByName("LockedDoorView"));

        public bool HasTriggers => true;
        public bool IsBroke => false;
        public bool LaddersDown => DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("LadderDown"));
        public bool LaddersUp => DoesTileReferenceOccurOnMap(_tileReferences.GetTileReferenceByName("LadderUp"));
        public bool OtherStart => false;

        public bool SpecialEnemyComputation => false;

        /// <summary>
        ///     Generated
        /// </summary>
        /// <remarks>this needs to rewritten when we understand how the data files refer to Combat Maps</remarks>
        public byte Id => (byte)MapTerritory;

        public Dungeon DungeonLocation => Dungeon.Covetous;


        /// <summary>
        ///     The number of the combat map (order in data file)
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public int CombatMapNum { get; }

        public int Index => CombatMapNum;

        /// <summary>
        ///     Brief description of the combat map
        /// </summary>
        public string Description => _combatMapData.Description;

        public string Name => MapTerritory == Territory.Britannia ? Description : "Dungeon-" + CombatMapNum;
        public string Notes => "No Notes";

        /// <summary>
        ///     Territory of the combat map
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public Territory MapTerritory { get; }

        public string GetAsCsvLine() =>
            $"{Index}, {Name}, {DungeonLocation}, {IsValidDirection(EntryDirection.Direction0)}, " +
            $"{IsValidDirection(EntryDirection.Direction1)}, {IsValidDirection(EntryDirection.Direction3)}, " +
            $"{IsValidDirection(EntryDirection.Direction2)}, {LaddersUp}, {LaddersDown}, {HasTriggers}, {Notes}";

        public static string GetCsvHeader() =>
            "Index, Name, DungeonLocation, DirEastLeft, DirWestRight, DirNorthUp, DirSouthDown, LaddersUp, LaddersDown, HasTriggers, Notes";

        public bool IsEnterable(EntryDirection entryDirection)
        {
            return _enterDirectionDictionary[entryDirection];
        }

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

        public bool DoesTileReferenceOccurOnMap(TileReference tileReference) =>
            GetNumberOfTileReferencesOnMap(tileReference) > 0;

        public bool IsEntryDirectionValid(EntryDirection entryDirection)
        {
            List<Point2D> points = GetPlayerStartPositions(entryDirection);
            if (points.Count == 0 || points[0].X <= 0) return false;

            List<Point2D> characterPositions = new List<Point2D>();
            // we walk through each of the character positions and if one is repeated then we know it's not valid
            foreach (Point2D point in points)
            {
                if (characterPositions.Contains(point)) return false;
                characterPositions.Add(point);
            }

            return true;
        }

        public List<Point2D> GetPlayerStartPositions(EntryDirection entryDirection)
        {
            Debug.Assert((int)entryDirection >= 0 && (int)entryDirection <= TotalDirections);
            return _playerPositionsByDirection[(int)entryDirection];
        }

        public Point2D GetEnemyPosition(int nIndex)
        {
            Debug.Assert(nIndex < NUM_ENEMIES && nIndex >= 0);
            return _enemyPositions[nIndex];
        }

        private int GetRawEnemySprite(int nIndex)
        {
            Debug.Assert(nIndex < NUM_ENEMIES && nIndex >= 0);
            return _enemySprites[nIndex];
        }

        public CombatMapSpriteType GetAdjustedEnemySprite(int nIndex, out int nSpriteIndex)
        {
            int nEnemyRawSprite = GetRawEnemySprite(nIndex);
            nSpriteIndex = nEnemyRawSprite + 0xFF;

            // enemy sprite of 0 indicates no monster
            if (nEnemyRawSprite == 0) return CombatMapSpriteType.Nothing;

            // it's a chest or something like it
            if (nEnemyRawSprite <= 15) return CombatMapSpriteType.Thing;

            switch (nEnemyRawSprite)
            {
                // it's a dead body or blood spatter
                case 30:
                case 31:
                    return CombatMapSpriteType.Thing;
                // it's determined by the encounter 
                case 112:
                    return CombatMapSpriteType.EncounterBased;
            }

            // if the sprite is lower than the crown index, then we add 4
            // OR
            // it's a Shadow Lord, but it breaks convention
            if (nSpriteIndex < 436 || nSpriteIndex >= 504) nSpriteIndex += 4;

            if (nSpriteIndex < 316 || nSpriteIndex > 511)
            {
                throw new Ultima5ReduxException(
                    $"Tried to get adjusted enemy sprite with index={nIndex} and raw sprite={nEnemyRawSprite}");
            }

            return CombatMapSpriteType.AutoSelected;
        }

        public List<EntryDirection> GetEntryDirections()
        {
            List<EntryDirection> validEntryDirections = new List<EntryDirection>();
            foreach (EntryDirection entryDirection in Enum.GetValues(typeof(EntryDirection)))
            {
                if (IsValidDirection(entryDirection)) validEntryDirections.Add(entryDirection);
            }

            return validEntryDirections;
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