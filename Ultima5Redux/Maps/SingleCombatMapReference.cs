﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Ultima5Redux.Data;

namespace Ultima5Redux.Maps
{
    public class SingleCombatMapReference
    {
        private readonly CombatMapReferences.CombatMapData _combatMapData;

        /// <summary>
        ///     The territory that the combat map is in. This matters most for determining data files.
        /// </summary>
        public enum Territory { Britannia = 0, Dungeon }

        public const int XTILES = 11;
        public const int YTILES = 11;
        
        public readonly byte[][] TheMap;
        
        private readonly List<List<Point2D>> _playerPositionsByDirection = Utils.Init2DList<Point2D>(4, 6);
        private readonly List<Point2D> _enemyPositions = new List<Point2D>(NUM_ENEMIES);
        private readonly List<byte> _enemySprites = new List<byte>(NUM_ENEMIES);

        private readonly Dictionary<EntryDirection, bool> _enterDirectionDictionary =
            new Dictionary<EntryDirection, bool>();
        
        /// <summary>
        ///     How many bytes for each combat map entry in data file
        /// </summary>
        private const int MAP_BYTE_COUNT = 0x0160;

        public const int NUM_ENEMIES = 16;
        private const int NUM_PLAYERS = 6;
        private const int NUM_DIRECTIONS = 4;

        public enum EntryDirection {East = 0, West = 1, South = 2, North = 3 }
        
        public enum BritanniaCombatMaps 
        {
            None = -2, BoatCalc = -1, CampFire = 0, Swamp = 1, Glade = 2, Treed = 3, Desert = 4, CleanTree = 5, Mountains = 6, 
            BigBridge = 7, Brick = 8, Basement = 9, Psychedelic = 10, BoatOcean = 11, BoatNorth = 12, BoatSouth = 13, 
            BoatBoat = 14, Bay = 15
        };

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
            int nMapOffset = nCombatMapNum * MAP_BYTE_COUNT;
            
            MapTerritory = mapTerritory;
            CombatMapNum = nCombatMapNum;
            const int nBytesPerRow = 0x20;

            // copying the array to a simpler format
            TheMap = Utils.Init2DByteArray(XTILES, YTILES);

            // get and build the map sprites 
            for (int nRow = 0; nRow < XTILES; nRow++)
            {
                DataChunk rowChunk = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Tiles for row {nRow}",
                    nMapOffset + (nBytesPerRow * nRow), XTILES, 0x00, CombatMapReferences.DataChunkName.Unused);
                List<byte> list = rowChunk.GetAsByteList();
                for (int nCol = 0; nCol < list.Count; nCol++)
                {
                    byte sprite = list[nCol];
                    TheMap[nCol][nRow] = sprite; 
                }
            }

            // build the maps of all four directions and the respective player positions
            for (int nRow = 1, nOffsetFactor = 0; nRow <= NUM_DIRECTIONS; nRow++, nOffsetFactor++)
            {
                // 1=east,2=west,3=south,4=north
                List<byte> xPlayerPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Player X positions for row #" + nRow,
                    nMapOffset + (nBytesPerRow * nOffsetFactor) + 0xB, 0x06).GetAsByteList();
                List<byte> yPlayerPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Player Y positions for row #" + nRow,
                    nMapOffset + (nBytesPerRow * nOffsetFactor) + 0xB + 0x06, 0x06).GetAsByteList();
                for (int nPlayer = 0; nPlayer < NUM_PLAYERS; nPlayer++)
                {
                    // if the X or Y value is above the number of tiles then it indicates a sprite number
                    // which I believe is additional trigger tiles
                    bool bIsEnterable = yPlayerPosList[nPlayer] < YTILES && xPlayerPosList[nPlayer] < XTILES;
                    _enterDirectionDictionary[(EntryDirection) nRow - 1] = bIsEnterable;
                    _playerPositionsByDirection[nRow - 1].Add(bIsEnterable
                        ? new Point2D(xPlayerPosList[nPlayer], yPlayerPosList[nPlayer])
                        : new Point2D(0, 0));

                    Debug.Assert(_playerPositionsByDirection[nRow - 1][nPlayer].X <= XTILES);
                    Debug.Assert(_playerPositionsByDirection[nRow - 1][nPlayer].Y <= YTILES);
                }
            }

            // load the enemy positions and sprites
            int nEnemyXOffset = nMapOffset + (nBytesPerRow * 6) + 0xB;
            int nEnemyYOffset = nMapOffset + (nBytesPerRow * 7) + 0xB;
            int nEnemySpriteOffset = nMapOffset + (nBytesPerRow * 5) + 0xB;
            List<byte> xEnemyPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Enemy X positions",
                nEnemyXOffset, NUM_ENEMIES).GetAsByteList();
            List<byte> yEnemyPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Enemy Y positions",
                nEnemyYOffset, NUM_ENEMIES).GetAsByteList();
            List<byte> spriteEnemyList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Enemy sprite index" ,
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
                    _enemyPositions.Add(new Point2D(0,0));
                }
                else
                {
                    _enemySprites.Add(spriteEnemyList[nEnemyIndex]);
                    duplicatePositionDictionary.Add(enemyPosition, true);
                    _enemyPositions.Add(enemyPosition);
                }
            }
            
            List<Point2D> _triggerPositions = new List<Point2D>(XTILES);
            List<TileReference> _triggerTileReferences = new List<TileReference>(XTILES);
            Dictionary<Point2D, List<PointAndTileReference>> _triggerPointToTileReferences =
                new Dictionary<Point2D, List<PointAndTileReference>>();

            // load the trigger positions
            // these are the Points that a player character hits and causes a triggering event
            const int nTriggerPositions = 8;
            List<byte> triggerXPositions = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Trigger X positions",
                nMapOffset + nBytesPerRow * 9 + XTILES, nTriggerPositions).GetAsByteList();
            List<byte> triggerYPositions = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Trigger Y positions",
                nMapOffset + nBytesPerRow * 9 + YTILES, nTriggerPositions).GetAsByteList();
            for (int i = 0; i < nTriggerPositions; i++)
            {
                _triggerPositions.Add(new Point2D(triggerXPositions[i], triggerYPositions[i]));
            }
            
            // Gather all replacement tile references when trigger occurs
            List<byte> triggerTileIndexes = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Trigger tile indexes",
                nMapOffset + XTILES, nTriggerPositions).GetAsByteList();
            foreach (byte nIndex in triggerTileIndexes)
            {
                _triggerTileReferences.Add(tileReferences.GetTileReference(nIndex + 0xFF));
            }
            
            // Gather all positions that change as a result of a trigger
            // the results are split into two different sections
            List<byte> triggerResultXPositions = new List<byte>();
            List<byte> triggerResultYPositions = new List<byte>();
            triggerResultXPositions.AddRange(dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Trigger Result X position #1",
                nMapOffset + nBytesPerRow * 9 + XTILES, nTriggerPositions).GetAsByteList());
            triggerResultYPositions.AddRange(dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Trigger Result Y position #1",
                nMapOffset + nBytesPerRow * 9 + YTILES, nTriggerPositions).GetAsByteList());
            triggerResultXPositions.AddRange(dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Trigger Result X position #2",
                nMapOffset + nBytesPerRow * 10 + XTILES, nTriggerPositions).GetAsByteList());
            triggerResultYPositions.AddRange(dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Trigger Result Y position #2",
                nMapOffset + nBytesPerRow * 10 + YTILES, nTriggerPositions).GetAsByteList());
            for (int i = 0; i < nTriggerPositions; i++)
            {
                TileReference newTriggeredTileReference = _triggerTileReferences[i];
                // if the index is 255 (was 0) then we know it is indicating it shouldn't used
                if (newTriggeredTileReference.Index == 0xFF) continue;

                // grab the two Points that will change as a result of landing on the trigger tile
                Point2D pos1 = new Point2D(triggerResultXPositions[i], triggerResultYPositions[i]);
                Point2D pos2 = new Point2D(triggerResultXPositions[i + nTriggerPositions], triggerResultYPositions[i + nTriggerPositions]);
                Point2D triggeredPosition = _triggerPositions[i];

                
                // if the trigger position has not been recorded yet, then we initialize the list
                // we use a List because every tile has a minimum of 2 changes, but can result in a lot more
                if (!_triggerPointToTileReferences.ContainsKey(triggeredPosition))
                    _triggerPointToTileReferences.Add(triggeredPosition, new List<PointAndTileReference>());
                
                _triggerPointToTileReferences[triggeredPosition].Add(new PointAndTileReference(pos1, _triggerTileReferences[i]));
                _triggerPointToTileReferences[triggeredPosition].Add(new PointAndTileReference(pos2, _triggerTileReferences[i]));
            }

            ((Action)(() => { }))();

        }

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
        //
        // public bool IsEnterable(EntryDirection entryDirection)
        // {
        //     return _enterDirectionDictionary[entryDirection];
        // }
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
            if (points[0].X <= 0) return false;
            return true;
        }
        
        public List<Point2D> GetPlayerStartPositions(EntryDirection entryDirection)
        {
            Debug.Assert((int)entryDirection >= 0 && (int)entryDirection <= NUM_DIRECTIONS);
            return _playerPositionsByDirection[(int) entryDirection];
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

        public enum CombatMapSpriteType { Nothing, Thing, AutoSelected, EncounterBased}
        public CombatMapSpriteType GetAdjustedEnemySprite(int nIndex, out int nSpriteIndex)
        {
            int nEnemyRawSprite = GetRawEnemySprite(nIndex);
            nSpriteIndex = nEnemyRawSprite + 0xFF;
            
            // enemy sprite of 0 indicates no monster
            if (nEnemyRawSprite == 0) return CombatMapSpriteType.Nothing;
                
            // it's a chest or something like it
            if (nEnemyRawSprite >= 1 && nEnemyRawSprite <= 15) return CombatMapSpriteType.Thing;

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

            if (nSpriteIndex < 320 || nSpriteIndex > 511)
            {
                throw new Ultima5ReduxException(
                    $"Tried to get adjusted enemy sprite with index={nIndex} and raw sprite={nEnemyRawSprite}");
            }

            return CombatMapSpriteType.AutoSelected;
        }
        
        
        /// <summary>
        ///     The number of the combat map (order in data file)
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public int CombatMapNum { get; }

        /// <summary>
        ///     Brief description of the combat map
        /// </summary>
        public string Description => _combatMapData.Description;

        /// <summary>
        ///     Territory of the combat map
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public Territory MapTerritory { get; }

        /// <summary>
        ///     Generated
        /// </summary>
        /// <remarks>this needs to rewritten when we understand how the data files refer to Combat Maps</remarks>
        public byte Id => (byte) MapTerritory;

   
    }
}