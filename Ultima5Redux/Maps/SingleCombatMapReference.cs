using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Ultima5Redux.Data;

namespace Ultima5Redux.Maps
{
    public class SingleCombatMapReference
    {
        // private readonly List<List<Point2D>> _characterXyPositions;

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

        public enum EntryDirection {East = 0, West = 1, North = 2, South = 3 }
        
        /// Descriptions of each combat map
        private static readonly string[] BritanniaDescriptions =
        {
            "Camp Fire", "Swamp", "Glade", "Treed", "Desert", "Clean Tree", "Mountains", "Big Bridge", "Brick",
            "Basement", "Psychedelic", "Boat - Ocean", "Boat - North", "Boat - South", "Boat-Boat", "Bay"
        };

        private static readonly string[] DungeonDescriptions =
        {
            "Klimb Down", "Four Way with Gremlins", "Magic Barriers", "Four Ways to Nowhere", "Right Sided Hammer", 
            "Double Portcullis", "Stone Gargoyles", "Surrounded Exit Right", "Rats Surprise!",
            "Deaths Magic Door", "Underground Lake", "Daemon Bottom Right", "Daemon Bottom Left", "Daemon Top Right", 
            "Daemon Top Left", "Triggered"
        };

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
            for (int nRow = 0; nRow < XTILES; nRow++)
            {
                DataChunk rowChunk = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Tiles for row {nRow}",
                    nMapOffset + (0x20 * nRow), XTILES, 0x00, CombatMapReferences.DataChunkName.Unused);
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
        }

        public bool IsEnterable(EntryDirection entryDirection)
        {
            return _enterDirectionDictionary[entryDirection];
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

            // it's a dead body or blood spatter
            if (nEnemyRawSprite == 30 || nEnemyRawSprite == 31) return CombatMapSpriteType.Thing;

            // it's determined by the encounter 
            if (nEnemyRawSprite == 112) return CombatMapSpriteType.EncounterBased;

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