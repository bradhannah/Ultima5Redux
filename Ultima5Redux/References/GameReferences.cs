using System.IO;
using Ultima5Redux.Maps;
using Ultima5Redux.References.Dialogue;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.References
{
    public class GameReferences
    {
        private const string GOLD_DIR = @"C:\games\Ultima_5\Gold";
        private const string RELATIVE_INSTALLED = @"Ultima5ReduxTestDependancies/DataFiles";
        private const string RELATIVE_TEST = @"../Ultima5ReduxTestDependancies/DataFiles";

        public static CombatItemReferences CombatItemRefs { get; private set; }

        public static CombatMapReferences CombatMapRefs { get; private set; }

        /// <summary>
        ///     A collection of data.ovl references
        /// </summary>
        public static DataOvlReference DataOvlRef { get; private set; }

        public static EnemyReferences EnemyRefs { get; private set; }

        public static GameReferences Instance { get; set; }

        /// <summary>
        ///     Detailed inventory information reference
        /// </summary>
        public static InventoryReferences InvRef { get; private set; }

        public static bool IsInitialized { get; private set; }

        /// <summary>
        ///     A large map reference
        /// </summary>
        /// <remarks>needs to be reviewed</remarks>
        public static LargeMapLocationReferences LargeMapRef { get; private set; }

        /// <summary>
        ///     A collection of all Look references
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public static Look LookRef { get; private set; }

        public static MagicReferences MagicRefs { get; private set; }

        public static MoonPhaseReferences MoonPhaseRefs { get; private set; }

        /// <summary>
        ///     A collection of all NPC references
        /// </summary>
        public static NonPlayerCharacterReferences NpcRefs { get; private set; }

        public static ProvisionReferences ProvisionReferences { get; private set; }

        public static ReagentReferences ReagentReferences { get; private set; }

        public static ShoppeKeeperDialogueReference ShoppeKeeperDialogueReference { get; private set; }
        public static ShoppeKeeperReferences ShoppeKeeperRefs { get; private set; }

        /// <summary>
        ///     A collection of all Sign references
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public static Signs SignRef { get; private set; }

        /// <summary>
        ///     A collection of all small map references
        /// </summary>
        public static SmallMapReferences SmallMapRef { get; private set; }

        /// <summary>
        ///     A collection of all tile references
        /// </summary>
        public static TileReferences SpriteTileReferences { get; private set; }

        /// <summary>
        ///     A collection of all talk script references
        /// </summary>
        public static TalkScripts TalkScriptsRef { get; private set; }

        public static TileOverrideReferences TileOverrideRefs { get; private set; }

        private GameReferences(string dataDirectory)
        {
            LookRef = new Look(dataDirectory);
            SignRef = new Signs(dataDirectory);
            InvRef = new InventoryReferences();
            MagicRefs = new MagicReferences();
            TileOverrideRefs = new TileOverrideReferences();

            DataOvlRef = new DataOvlReference(dataDirectory);

            SmallMapRef = new SmallMapReferences(DataOvlRef);
            LargeMapRef = new LargeMapLocationReferences(DataOvlRef);
            MoonPhaseRefs = new MoonPhaseReferences(DataOvlRef);
            SpriteTileReferences = new TileReferences(DataOvlRef.StringReferences);
            CombatItemRefs = new CombatItemReferences(InvRef);
            TalkScriptsRef = new TalkScripts(dataDirectory, DataOvlRef);
            NpcRefs = new NonPlayerCharacterReferences(dataDirectory, SmallMapRef, TalkScriptsRef);
            EnemyRefs = new EnemyReferences(DataOvlRef, SpriteTileReferences);
            CombatMapRefs = new CombatMapReferences(dataDirectory, SpriteTileReferences);

            ShoppeKeeperDialogueReference = new ShoppeKeeperDialogueReference(dataDirectory, DataOvlRef);
            ShoppeKeeperRefs = new ShoppeKeeperReferences(DataOvlRef, NpcRefs);
            ReagentReferences = new ReagentReferences();
            ProvisionReferences = new ProvisionReferences();
        }

        private static string GetU5Directory()
        {
            if (Directory.Exists(RELATIVE_TEST)) return RELATIVE_TEST;
            if (Directory.Exists(RELATIVE_INSTALLED)) return RELATIVE_INSTALLED;
            if (Directory.Exists(GOLD_DIR)) return GOLD_DIR; //-V3039

            throw new Ultima5ReduxException("Can't find a suitable Ultima Data Files directory.\n CWD: " +
                                            Directory.GetCurrentDirectory() + "\nDirs: " +
                                            Directory.GetDirectories(Directory.GetCurrentDirectory()));
        }

        public static void Initialize(string dataDirectory = "")
        {
            if (dataDirectory == "")
            {
                dataDirectory = GetU5Directory();
            }

            if (!Directory.Exists(dataDirectory))
            {
                throw new Ultima5ReduxException("Missing the data directory: " + dataDirectory);
            }

            if (!File.Exists(
                    Utils.GetFirstFileAndPathCaseInsensitive(Path.Combine(dataDirectory, FileConstants.DATA_OVL))))
            {
                throw new Ultima5ReduxException("Missing the Data.OVL file: " +
                                                Path.Combine(dataDirectory, FileConstants.DATA_OVL));
            }

            Instance = new GameReferences(dataDirectory);
            IsInitialized = true;
        }
    }
}