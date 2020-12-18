using System;
using System.Collections.Generic;
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
        
        /// <summary>
        ///     How many bytes for each combat map entry in data file
        /// </summary>
        public const int MAP_BYTE_COUNT = 0x0160;

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
        /// <param name="spriteList"></param>
        /// <param name="characterXyPositions"></param>
        /// <param name="dataChunks"></param>
        public SingleCombatMapReference(Territory mapTerritory, int nCombatMapNum, //IReadOnlyList<List<byte>> spriteList,
            //List<List<Point2D>> characterXyPositions,
            DataChunks<CombatMapReferences.DataChunkName> dataChunks)
        {
            int nMapOffset = nCombatMapNum * MAP_BYTE_COUNT;
            
            MapTerritory = mapTerritory;
            CombatMapNum = nCombatMapNum;

            // copying the array to a simpler format
            TheMap = Utils.Init2DByteArray(XTILES, YTILES);

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

            List<List<Point2D>> playerDirections = new List<List<Point2D>>(); 
            for (int nRow = 1; nRow <= 4; nRow++)
            {
                // 1=east,2=west,3=south,4=north
                playerDirections.Add(new List<Point2D>());
                List<byte> xPlayerPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Player X positions for row #" + nRow,
                    nMapOffset + (0x20 * nRow) + 0x11, 0x06).GetAsByteList();
                List<byte> yPlayerPosList = dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Player Y positions for row #" + nRow,
                    nMapOffset + (0x20 * nRow) + 0x11 + 0x06, 0x06).GetAsByteList();
                for (int i = 0; i < 6; i++)
                {
                    playerDirections[nRow - 1].Add(new Point2D(xPlayerPosList[i], yPlayerPosList[i]));
                }
            }
            
            
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