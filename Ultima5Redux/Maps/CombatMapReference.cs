using System.Collections.Generic;

namespace Ultima5Redux.Maps
{
    public class CombatMapReference
    {
        // the master copy of the map references

        /// <summary>
        ///     Build the combat map reference
        /// </summary>
        public CombatMapReference()
        {
            for (short i = 0; i < 16; i++)
            {
                MapReferenceList.Add(new SingleCombatMapReference(SingleCombatMapReference.Territory.Britannia, i));
            }
        }

        /// <summary>
        ///     The list of map references
        /// </summary>
        public List<SingleCombatMapReference> MapReferenceList { get; } = new List<SingleCombatMapReference>();

        public class SingleCombatMapReference
        {
            /// <summary>
            ///     The territory that the combat map is in. This matters most for determing data files.
            /// </summary>
            public enum Territory { Britannia = 0, Dungeon }

            /// <summary>
            ///     How many bytes for each combat map entry in data file
            /// </summary>
            public const int MAP_BYTE_COUNT = 0x0160;

            /// Descriptions of each combat map
            private static readonly string[] BritanniaDescriptions =
            {
                "Camp Fire", "Swamp", "Glade", "Treed", "Desert", "Clean Tree", "Mountains", "Big Bridge", "Brick",
                "Basement", "Psychodelic",
                "Boat - Ocean", "Boat - North", "Boat - South", "Boat-Boat", "Bay"
            };

            private static readonly string[] DungeonDescriptions = {"A", "B"};

            /// <summary>
            ///     Create the reference based on territory and a map number
            /// </summary>
            /// <param name="mapTerritory">Britannia or Dungeon</param>
            /// <param name="combatMapNum">map number in data file (0,1,2,3....)</param>
            public SingleCombatMapReference(Territory mapTerritory, short combatMapNum)
            {
                MapTerritory = mapTerritory;
                CombatMapNum = combatMapNum;
            }

            /// <summary>
            ///     Offset of combat map in data file
            /// </summary>
            public int FileOffset => CombatMapNum * MAP_BYTE_COUNT;

            /// <summary>
            ///     The number of the combat map (order in data file)
            /// </summary>
            public short CombatMapNum { get; }

            /// <summary>
            ///     Brief description of the combat map
            /// </summary>
            public string Description
            {
                get
                {
                    if (MapTerritory == Territory.Britannia)
                        return BritanniaDescriptions[CombatMapNum];
                    return DungeonDescriptions[CombatMapNum];
                }
            }

            /// <summary>
            ///     Territory of the combat map
            /// </summary>
            public Territory MapTerritory { get; set; }

            /// <summary>
            ///     Generated
            /// </summary>
            /// <remarks>this needs to rewritten when we understand how the data files refer to Combat Maps</remarks>
            public byte Id => (byte) MapTerritory;

            /// <summary>
            ///     Filename of the resource based on it's Territory
            /// </summary>
            public string MapFilename
            {
                get
                {
                    switch (MapTerritory)
                    {
                        case Territory.Britannia:
                            return "brit.cbt";
                        case Territory.Dungeon:
                            return "dungeon.cbt";
                    }

                    return "";
                }
            }
        }
    }
}