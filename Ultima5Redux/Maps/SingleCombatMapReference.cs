using System;
using System.Collections.Generic;

namespace Ultima5Redux.Maps
{
    public class SingleCombatMapReference
    {
        /// <summary>
        ///     The territory that the combat map is in. This matters most for determining data files.
        /// </summary>
        public enum Territory { Britannia = 0, Dungeon }

        public const int XTILES = 11;
        public const int YTILES = 11;
        
        public byte[][] TheMap;
        
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
        public SingleCombatMapReference(Territory mapTerritory, int nCombatMapNum, IReadOnlyList<List<byte>> spriteList)
        {
            MapTerritory = mapTerritory;
            CombatMapNum = nCombatMapNum;

            // copying the array to a simpler format
            TheMap = Utils.Init2DByteArray(XTILES, YTILES);
            for (int nRow = 0; nRow < spriteList.Count; nRow++)
            {
                List<byte> rows = spriteList[nRow];
                for (int nCol = 0; nCol < rows.Count; nCol++)
                {
                    TheMap[nRow][nCol] = spriteList[nRow][nCol];
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