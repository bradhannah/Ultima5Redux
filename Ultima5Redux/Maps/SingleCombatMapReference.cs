using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;

namespace Ultima5Redux.Maps
{
    public class SingleCombatMapReference
    {
        private readonly List<List<Point2D>> _characterXyPositions;

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
        
        /// <summary>
        ///     How many bytes for each combat map entry in data file
        /// </summary>
        private const int MAP_BYTE_COUNT = 0x0160;

        private const int NUM_PLAYERS = 6;
        private const int NUM_ENEMIES = 16;
        private const int NUM_DIRECTIONS = 4;

        public enum EntryDirection {East = 0, West = 1, South = 2, North = 3 }
        
        /// Descriptions of each combat map
        private static readonly string[] BritanniaDescriptions =
        {
            "Camp Fire", "Swamp", "Glade", "Treed", "Desert", "Clean Tree", "Mountains", "Big Bridge", "Brick",
            "Basement", "Psychedelic", "Boat - Ocean", "Boat - North", "Boat - South", "Boat-Boat", "Bay"
        };

        private static readonly string[] DungeonDescriptions = {"A", "B"};

        /// <summary>
        ///     Create the reference based on territory and a map number
        /// </summary>
        /// <param name="mapTerritory">Britannia or Dungeon</param>
        /// <param name="nCombatMapNum">map number in data file (0,1,2,3....)</param>
        /// <param name="dataChunks"></param>
        public SingleCombatMapReference(Territory mapTerritory, int nCombatMapNum,
            DataChunks<CombatMapReferences.DataChunkName> dataChunks)
        {
            int nMapOffset = nCombatMapNum * MAP_BYTE_COUNT;
            
            MapTerritory = mapTerritory;
            CombatMapNum = nCombatMapNum;

            // copying the array to a simpler format
            TheMap = Utils.Init2DByteArray(XTILES, YTILES);

            // get and build the map sprites 
            for (int nRow = 0; nRow < CombatMapLegacy.XTILES; nRow++)
            {
                DataChunk rowChunk = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Tiles for row {nRow}",
                    nMapOffset + (0x20 * nRow), CombatMapLegacy.XTILES, 0x00, CombatMapReferences.DataChunkName.Unused);
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
                    nMapOffset + (0x20 * nOffsetFactor) + 0xB, 0x06).GetAsByteList();
                List<byte> yPlayerPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Player Y positions for row #" + nRow,
                    nMapOffset + (0x20 * nOffsetFactor) + 0xB + 0x06, 0x06).GetAsByteList();
                for (int i = 0; i < NUM_PLAYERS; i++)
                {
                    _playerPositionsByDirection[nRow - 1].Add(new Point2D(xPlayerPosList[i], yPlayerPosList[i]));
                }
            }

            // load the enemy positions and sprites
            int nEnemyXOffset = nMapOffset + (0x20 * 6) + 0xB;
            int nEnemyYOffset = nMapOffset + (0x20 * 7) + 0xB;
            int nEnemySpriteOffset = nMapOffset + (0x20 * 5) + 0xB;
            List<byte> xEnemyPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Enemy X positions",
                nEnemyXOffset, NUM_ENEMIES).GetAsByteList();
            List<byte> yEnemyPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Enemy Y positions",
                nEnemyYOffset, NUM_ENEMIES).GetAsByteList();
            List<byte> spriteEnemyList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Enemy sprite index" ,
                nEnemySpriteOffset, NUM_ENEMIES).GetAsByteList();

            Debug.Assert(xEnemyPosList.Count == NUM_ENEMIES);
            Debug.Assert(yEnemyPosList.Count == NUM_ENEMIES);
            Debug.Assert(spriteEnemyList.Count == NUM_ENEMIES);
            
            for (int nEnemyIndex = 0; nEnemyIndex < NUM_ENEMIES; nEnemyIndex++)
            {
                _enemyPositions.Add(new Point2D(xEnemyPosList[nEnemyIndex], yEnemyPosList[nEnemyIndex]));
                _enemySprites.Add(spriteEnemyList[nEnemyIndex]);
            }
        }

        
        public List<Point2D> GetPlayerStartPositions(EntryDirection entryDirection)
        {
            Debug.Assert((int)entryDirection >= 0 && (int)entryDirection <= NUM_DIRECTIONS);
            return _playerPositionsByDirection[(int) entryDirection];
        }
        
        /// <summary>
        ///     The number of the combat map (order in data file)
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public int CombatMapNum { get; }

        /// <summary>
        ///     Brief description of the combat map
        /// </summary>
        public string Description =>
            MapTerritory == Territory.Britannia
                ? BritanniaDescriptions[CombatMapNum]
                : DungeonDescriptions[CombatMapNum];

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