using System.IO;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.References
{
    public static class GameReferences
    {
        public static CombatItemReferences CombatItemRefs { get; }

        public static CombatMapReferences CombatMapRefs { get; }

        /// <summary>
        ///     A collection of data.ovl references
        /// </summary>
        public static DataOvlReference DataOvlRef { get; }

        public static EnemyReferences EnemyRefs { get; }

        /// <summary>
        ///     Detailed inventory information reference
        /// </summary>
        public static InventoryReferences InvRef { get; }

        /// <summary>
        ///     A large map reference
        /// </summary>
        /// <remarks>needs to be reviewed</remarks>
        public static LargeMapLocationReferences LargeMapRef { get; }

        /// <summary>
        ///     A collection of all Look references
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public static Look LookRef { get; }

        public static MagicReferences MagicRefs { get; }

        public static MoonPhaseReferences MoonPhaseRefs { get; }

        /// <summary>
        ///     A collection of all NPC references
        /// </summary>
        public static NonPlayerCharacterReferences NpcRefs { get; }

        public static ShoppeKeeperDialogueReference ShoppeKeeperDialogueReference { get; }
        public static ShoppeKeeperReferences ShoppeKeeperRefs { get; }

        /// <summary>
        ///     A collection of all Sign references
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public static Signs SignRef { get; }

        /// <summary>
        ///     A collection of all small map references
        /// </summary>
        public static SmallMapReferences SmallMapRef { get; }

        /// <summary>
        ///     A collection of all tile references
        /// </summary>
        public static TileReferences SpriteTileReferences { get; }

        /// <summary>
        ///     A collection of all talk script references
        /// </summary>
        public static TalkScripts TalkScriptsRef { get; }

        public static TileOverrideReferences TileOverrideRefs { get; }

        private static string U5Directory
        {
            get
            {
                if (Directory.Exists(RELATIVE_TEST)) return RELATIVE_TEST;
                if (Directory.Exists(RELATIVE_INSTALLED)) return RELATIVE_INSTALLED;
                if (Directory.Exists(GOLD_DIR)) return GOLD_DIR;
                throw new Ultima5ReduxException("Can't find a suitable Ultima Data Files directory.\n CWD: " +
                                                Directory.GetCurrentDirectory() + "\nDirs: " +
                                                Directory.GetDirectories(Directory.GetCurrentDirectory()));
            }
        }

        private const string GOLD_DIR = @"C:\games\Ultima_5\Gold";
        private const string RELATIVE_TEST = @"../Ultima5ReduxTestDependancies/DataFiles";
        private const string RELATIVE_INSTALLED = @"Ultima5ReduxTestDependancies/DataFiles";

        //=> ;

        static GameReferences()
        {
            LookRef = new Look(U5Directory);
            SignRef = new Signs(U5Directory);
            InvRef = new InventoryReferences();
            MagicRefs = new MagicReferences();

            DataOvlRef = new DataOvlReference(U5Directory);

            SmallMapRef = new SmallMapReferences(DataOvlRef);
            LargeMapRef = new LargeMapLocationReferences(DataOvlRef);
            MoonPhaseRefs = new MoonPhaseReferences(DataOvlRef);
            SpriteTileReferences = new TileReferences(DataOvlRef.StringReferences);
            CombatItemRefs = new CombatItemReferences(InvRef);
            TalkScriptsRef = new TalkScripts(U5Directory, DataOvlRef);
            NpcRefs = new NonPlayerCharacterReferences(U5Directory, SmallMapRef, TalkScriptsRef);
            EnemyRefs = new EnemyReferences(DataOvlRef, SpriteTileReferences);
            CombatMapRefs = new CombatMapReferences(U5Directory, SpriteTileReferences);

            ShoppeKeeperDialogueReference = new ShoppeKeeperDialogueReference(U5Directory, DataOvlRef);
            ShoppeKeeperRefs = new ShoppeKeeperReferences(DataOvlRef, NpcRefs);
            TileOverrideRefs = new TileOverrideReferences();
        }
    }
}