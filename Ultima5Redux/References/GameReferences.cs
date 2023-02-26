using System.Diagnostics.CodeAnalysis;
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

        private static GameReferences _gameReferences;

        private static string _legacyDataDirectory = "";

        public static GameReferences Instance
        {
            get
            {
                if (_gameReferences == null)
                {
                    _gameReferences = new GameReferences(_legacyDataDirectory);
                    _gameReferences.Build();
                }

                _gameReferences.IsInitialized = true;
                return _gameReferences;
            }
        }

        public CombatItemReferences CombatItemRefs { get; private set; }

        public CombatMapReferences CombatMapRefs { get; private set; }

        /// <summary>
        ///     A collection of data.ovl references
        /// </summary>
        public DataOvlReference DataOvlRef { get; private set; }

        public DungeonReferences DungeonReferences { get; private set; }

        public EnemyReferences EnemyRefs { get; private set; }

        /// <summary>
        ///     Detailed inventory information reference
        /// </summary>
        public InventoryReferences InvRef { get; private set; }

        public bool IsInitialized { get; private set; }

        /// <summary>
        ///     A large map reference
        /// </summary>
        /// <remarks>needs to be reviewed</remarks>
        public LargeMapLocationReferences LargeMapRef { get; private set; }

        /// <summary>
        ///     A collection of all Look references
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public Look LookRef { get; private set; }

        public MagicReferences MagicRefs { get; private set; }

        public MoonPhaseReferences MoonPhaseRefs { get; private set; }

        /// <summary>
        ///     A collection of all NPC references
        /// </summary>
        public NonPlayerCharacterReferences NpcRefs { get; private set; }

        public ProvisionReferences ProvisionReferences { get; private set; }

        public ReagentReferences ReagentReferences { get; private set; }

        public SearchLocationReferences SearchLocationReferences { get; private set; }

        public ShoppeKeeperDialogueReference ShoppeKeeperDialogueReference { get; private set; }
        public ShoppeKeeperReferences ShoppeKeeperRefs { get; private set; }

        /// <summary>
        ///     A collection of all Sign references
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public Signs SignRef { get; private set; }

        /// <summary>
        ///     A collection of all small map references
        /// </summary>
        public SmallMapReferences SmallMapRef { get; private set; }

        /// <summary>
        ///     A collection of all tile references
        /// </summary>
        public TileReferences SpriteTileReferences { get; private set; }

        /// <summary>
        ///     A collection of all talk script references
        /// </summary>
        public TalkScripts TalkScriptsRef { get; private set; }

        public TileOverrideReferences TileOverrideRefs { get; private set; }


        private GameReferences(string dataDirectory) => _legacyDataDirectory = dataDirectory;

        private static string GetU5Directory()
        {
            if (Directory.Exists(RELATIVE_TEST)) return RELATIVE_TEST;
            if (Directory.Exists(RELATIVE_INSTALLED)) return RELATIVE_INSTALLED;
            if (Directory.Exists(GOLD_DIR)) return GOLD_DIR; //-V3039

            throw new Ultima5ReduxException("Can't find a suitable Ultima Data Files directory.\n CWD: " +
                                            Directory.GetCurrentDirectory() + "\nDirs: " +
                                            Directory.GetDirectories(Directory.GetCurrentDirectory()));
        }

        private void Build()
        {
            LookRef = new Look(_legacyDataDirectory);
            SignRef = new Signs(_legacyDataDirectory);
            InvRef = new InventoryReferences();
            MagicRefs = new MagicReferences();
            TileOverrideRefs = new TileOverrideReferences();

            DataOvlRef = new DataOvlReference(_legacyDataDirectory);

            SmallMapRef = new SmallMapReferences(DataOvlRef);
            LargeMapRef = new LargeMapLocationReferences(DataOvlRef);
            MoonPhaseRefs = new MoonPhaseReferences(DataOvlRef);
            SpriteTileReferences = new TileReferences(DataOvlRef.StringReferences);
            CombatItemRefs = new CombatItemReferences(InvRef);
            TalkScriptsRef = new TalkScripts(_legacyDataDirectory, DataOvlRef);
            NpcRefs = new NonPlayerCharacterReferences(_legacyDataDirectory, SmallMapRef, TalkScriptsRef);
            EnemyRefs = new EnemyReferences(DataOvlRef, SpriteTileReferences);
            CombatMapRefs = new CombatMapReferences(_legacyDataDirectory, SpriteTileReferences);

            ShoppeKeeperDialogueReference = new ShoppeKeeperDialogueReference(_legacyDataDirectory, DataOvlRef);
            ShoppeKeeperRefs = new ShoppeKeeperReferences(DataOvlRef, NpcRefs);
            ReagentReferences = new ReagentReferences();
            ProvisionReferences = new ProvisionReferences();
            SearchLocationReferences = new SearchLocationReferences(DataOvlRef);
            DungeonReferences = new DungeonReferences(_legacyDataDirectory);
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

            _legacyDataDirectory = dataDirectory;
        }
    }
}