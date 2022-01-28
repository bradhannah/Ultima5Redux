using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.Properties;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux
{
    public class ImportedGameState
    {
        /// <summary>
        ///     Data chunks for each of the save game sections
        /// </summary>
        private enum DataChunkName
        {
            Unused, CHARACTER_RECORDS, NPC_ISDEAD_TABLE, NPC_ISMET_TABLE, N_PEOPLE_PARTY, FOOD_QUANTITY, GOLD_QUANTITY,
            KEYS_QUANTITY, GEMS_QUANTITY, TORCHES_QUANTITY, TORCHES_TURNS, CURRENT_YEAR, CURRENT_MONTH, CURRENT_DAY,
            CURRENT_HOUR, CURRENT_MINUTE, NPC_TYPES, NPC_MOVEMENT_LISTS, NPC_MOVEMENT_OFFSETS, NPC_SPRITE_INDEXES,
            PARTY_LOC, Z_COORD, X_COORD, Y_COORD, CHARACTER_ANIMATION_STATES, CHARACTER_STATES, MOONSTONE_X_COORDS,
            MOONSTONE_Y_COORDS, MOONSTONE_BURIED, MOONSTONE_Z_COORDS, ACTIVE_CHARACTER, GRAPPLE, SKULL_KEYS_QUANTITY,
            KARMA, TURNS_SINCE_START
        }

        private enum OverlayChunkName { Unused, CHARACTER_ANIMATION_STATES }

        // ReSharper disable once UnusedMember.Local
        [IgnoreDataMember] public MapUnitStates MapUnitStatesByInitialMap => GetMapUnitStatesByMap(InitialMap);

        private readonly DataChunks<OverlayChunkName> _overworldOverlayDataChunks;
        private readonly DataChunks<OverlayChunkName> _underworldOverlayDataChunks;

        internal byte ActivePlayerNumber => DataChunks.GetDataChunk(DataChunkName.ACTIVE_CHARACTER).GetChunkAsByte();

        internal MapUnitMovements CharacterMovements { get; private set; }

        internal PlayerCharacterRecords CharacterRecords { get; private set; }

        /// <summary>
        ///     Current floor
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        internal int Floor => DataChunks.GetDataChunk(DataChunkName.Z_COORD).GetChunkAsByte();

        /// <summary>
        ///     Players total food
        /// </summary>
        internal ushort Food => DataChunks.GetDataChunk(DataChunkName.FOOD_QUANTITY).GetChunkAsUint16();

        internal byte Gems => DataChunks.GetDataChunk(DataChunkName.GEMS_QUANTITY).GetChunkAsByte();

        /// <summary>
        ///     Players total gold
        /// </summary>
        internal ushort Gold => DataChunks.GetDataChunk(DataChunkName.GOLD_QUANTITY).GetChunkAsUint16();

        /// <summary>
        ///     Does the Avatar have a grapple?
        /// </summary>
        internal bool HasGrapple => DataChunks.GetDataChunk(DataChunkName.GRAPPLE).GetChunkAsByte() != 0x00;

        /// <summary>
        ///     Which map am I currently on?
        /// </summary>
        internal Map.Maps InitialMap =>
            Location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld
                ? Floor == 0xFF ? Map.Maps.Underworld : Map.Maps.Overworld
                : Map.Maps.Small;

        internal byte Karma => DataChunks.GetDataChunk(DataChunkName.KARMA).GetChunkAsByte();

        internal byte Keys => DataChunks.GetDataChunk(DataChunkName.KEYS_QUANTITY).GetChunkAsByte();

        /// <summary>
        ///     Current location
        /// </summary>
        internal SmallMapReferences.SingleMapReference.Location Location =>
            (SmallMapReferences.SingleMapReference.Location)DataChunks.GetDataChunk(DataChunkName.PARTY_LOC)
                .GetChunkAsByte();

        internal DataChunk NonPlayerCharacterKeySprites => DataChunks.GetDataChunk(DataChunkName.NPC_SPRITE_INDEXES);

        internal bool[][] NPCIsDeadArray { get; private set; }
        internal bool[][] NPCIsMetArray { get; private set; }

        internal byte SkullKeys => DataChunks.GetDataChunk(DataChunkName.SKULL_KEYS_QUANTITY).GetChunkAsByte();
        internal SmallMapCharacterStates SmallMapCharacterStates { get; private set; }

        internal Moongates TheMoongates => new(GetDataChunk(DataChunkName.MOONSTONE_X_COORDS),
            GetDataChunk(DataChunkName.MOONSTONE_Y_COORDS), GetDataChunk(DataChunkName.MOONSTONE_BURIED),
            GetDataChunk(DataChunkName.MOONSTONE_Z_COORDS));

        internal NonPlayerCharacterStates TheNonPlayerCharacterStates { get; private set; }

        internal int TurnsSinceStart => DataChunks.GetDataChunk(DataChunkName.TURNS_SINCE_START).GetChunkAsByte();

        internal bool IsInitialSaveFile => TurnsSinceStart == 0;

        internal TimeOfDay TheTimeOfDay => new(DataChunks.GetDataChunk(DataChunkName.CURRENT_YEAR),
            DataChunks.GetDataChunk(DataChunkName.CURRENT_MONTH), DataChunks.GetDataChunk(DataChunkName.CURRENT_DAY),
            DataChunks.GetDataChunk(DataChunkName.CURRENT_HOUR), DataChunks.GetDataChunk(DataChunkName.CURRENT_MINUTE));

        internal byte Torches => DataChunks.GetDataChunk(DataChunkName.TORCHES_QUANTITY).GetChunkAsByte();

        /// <summary>
        ///     How many turns left until your torch is burnt out?
        /// </summary>
        internal byte TorchTurnsLeft => DataChunks.GetDataChunk(DataChunkName.TORCHES_TURNS).GetChunkAsByte();

        /// <summary>
        ///     Saved X location of Avatar
        /// </summary>
        internal int X => DataChunks.GetDataChunk(DataChunkName.X_COORD).GetChunkAsByte();

        /// <summary>
        ///     Saved Y location of Avatar
        /// </summary>
        internal int Y => DataChunks.GetDataChunk(DataChunkName.Y_COORD).GetChunkAsByte();

        private MapUnitStates ActiveMapUnitStates { get; set; }

        // map overlay data chunks
        private DataChunk ActiveOverlayDataChunks =>
            DataChunks.GetDataChunk(DataChunkName.CHARACTER_ANIMATION_STATES);

        private DataChunk CharacterStatesDataChunk => DataChunks.GetDataChunk(DataChunkName.CHARACTER_STATES);

        private DataChunks<DataChunkName> DataChunks { get; }

        private List<byte> GameStateByteArray { get; }

        // DataChunk based properties (not ideal) 
        private DataChunk NonPlayerCharacterMovementLists => DataChunks.GetDataChunk(DataChunkName.NPC_MOVEMENT_LISTS);

        private DataChunk NonPlayerCharacterMovementOffsets =>
            DataChunks.GetDataChunk(DataChunkName.NPC_MOVEMENT_OFFSETS);

        private MapUnitStates OverworldMapUnitStates { get; set; }

        private DataChunk OverworldOverlayDataChunks =>
            _overworldOverlayDataChunks.GetDataChunk(OverlayChunkName.CHARACTER_ANIMATION_STATES);

        private MapUnitStates SmallMapUnitStates { get; set; }

        private MapUnitStates UnderworldMapUnitStates { get; set; }

        private DataChunk UnderworldOverlayDataChunks =>
            _underworldOverlayDataChunks.GetDataChunk(OverlayChunkName.CHARACTER_ANIMATION_STATES);

        /// <summary>
        ///     Load the default starting save game
        /// </summary>
        public ImportedGameState()
        {
            DataChunks = new DataChunks<DataChunkName>(Resources.InitGam, DataChunkName.Unused);
            GameStateByteArray = Resources.InitGam.ToList();

            // load the default overworld and underworld overlays on first load
            _overworldOverlayDataChunks = new DataChunks<OverlayChunkName>(Resources.BritOol, OverlayChunkName.Unused);
            _underworldOverlayDataChunks =
                new DataChunks<OverlayChunkName>(Resources.UnderOol, OverlayChunkName.Unused);

            _overworldOverlayDataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Character Animation States - Overworld", 0x00, 0x100, 0x00,
                OverlayChunkName.CHARACTER_ANIMATION_STATES);
            _underworldOverlayDataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Character Animation States - Underworld", 0x00, 0x100, 0x00,
                OverlayChunkName.CHARACTER_ANIMATION_STATES);

            Initialize(true);
        }

        public ImportedGameState(string u5Directory)
        {
            DataChunks = new DataChunks<DataChunkName>(u5Directory, FileConstants.SAVED_GAM, DataChunkName.Unused);
            GameStateByteArray = Utils.GetFileAsByteList(Path.Combine(u5Directory, FileConstants.SAVED_GAM));

            // load the overworld and underworld overlays
            // they are stored in the saved.ool file - but also the brit.ool and under.ool file - not quite sure why it's stored in both...
            _overworldOverlayDataChunks =
                new DataChunks<OverlayChunkName>(u5Directory, FileConstants.SAVED_OOL, OverlayChunkName.Unused);
            _underworldOverlayDataChunks =
                new DataChunks<OverlayChunkName>(u5Directory, FileConstants.SAVED_OOL, OverlayChunkName.Unused);

            _overworldOverlayDataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Character Animation States - Overworld", 0x00, 0x100, 0x00,
                OverlayChunkName.CHARACTER_ANIMATION_STATES);
            _underworldOverlayDataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Character Animation States - Underworld", 0x100, 0x100, 0x00,
                OverlayChunkName.CHARACTER_ANIMATION_STATES);

            Initialize(true);
        }

        internal int GetEquipmentQuantity(DataOvlReference.Equipment equipment)
        {
            // can't remember why I return 262..?
            if (equipment == DataOvlReference.Equipment.BareHands) return 262;
            return GetByteAsIntFromGameStateByteArray((int)equipment);
        }

        internal int GetLordBritishArtifactQuantity(LordBritishArtifact.ArtifactType artifact) =>
            GetByteAsIntFromGameStateByteArray((int)artifact);

        internal int GetPotionQuantity(Potion.PotionColor color) =>
            GetByteAsIntFromGameStateByteArray((int)color);

        internal int GetReagentQuantity(Reagent.SpecificReagentType specificReagentType) =>
            GetByteAsIntFromGameStateByteArray((int)specificReagentType);

        internal int GetScrollQuantity(MagicReference.SpellWords spellWord)
        {
            Scroll.ScrollSpells scrollSpell =
                (Scroll.ScrollSpells)Enum.Parse(typeof(Scroll.ScrollSpells), spellWord.ToString());

            int nIndex = 0x27A + (int)scrollSpell;
            return GetByteAsIntFromGameStateByteArray(nIndex);
        }

        internal int GetShadowlordShardQuantity(ShadowlordShard.ShardType shard) =>
            GetByteAsIntFromGameStateByteArray((int)shard);

        internal int GetSpecialItemQuantity(SpecialItem.SpecificItemType specialItem) =>
            GetByteAsIntFromGameStateByteArray((int)specialItem);

        internal int GetSpellQuantity(MagicReference.SpellWords spellWord) =>
            GetByteAsIntFromGameStateByteArray((int)spellWord);

        private int GetByteAsIntFromGameStateByteArray(int nOffset)
        {
            if (GameStateByteArray.Count < nOffset)
                throw new Ultima5ReduxException("Tried to access offset #" + nOffset +
                                                " but game state array is only " + GameStateByteArray.Count + " long");
            return GameStateByteArray[nOffset];
        }

        private DataChunk GetDataChunk(DataChunkName dataChunkName)
        {
            return DataChunks.GetDataChunk(dataChunkName);
        }

        private void Initialize(bool bLoadFromDisk)
        {
            // import all character records
            DataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "All Character Records (ie. name, stats)", 0x02,
                0x20 * 16, 0x00, DataChunkName.CHARACTER_RECORDS);
            DataChunk rawCharacterRecords = DataChunks.GetDataChunk(DataChunkName.CHARACTER_RECORDS);
            CharacterRecords = new PlayerCharacterRecords(rawCharacterRecords.GetAsByteList());

            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Player Karma", 0x2E2, 0x01, 0x00,
                DataChunkName.KARMA);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Number of Turns Since Start", 0x2E5, 0x01, 0x00,
                DataChunkName.TURNS_SINCE_START);

            // player location
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Party _location", 0x2ED, 0x01, 0x00,
                DataChunkName.PARTY_LOC);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Z Coordinate of Party [10]", 0x2EF, 0x01, 0x00,
                DataChunkName.Z_COORD);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "X Coordinate of Party", 0x2F0, 0x01, 0x00,
                DataChunkName.X_COORD);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Y Coordinate of Party", 0x2F1, 0x01, 0x00,
                DataChunkName.Y_COORD);

            // quantities of standard items
            DataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16, "Food Quantity", 0x202, 0x02, 0x00,
                DataChunkName.FOOD_QUANTITY);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16, "Gold Quantity", 0x204, 0x02, 0x00,
                DataChunkName.GOLD_QUANTITY);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Keys Quantity", 0x206, 0x01, 0x00,
                DataChunkName.KEYS_QUANTITY);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Gems Quantity", 0x207, 0x01, 0x00,
                DataChunkName.GEMS_QUANTITY);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Torches Quantity", 0x208, 0x01, 0x00,
                DataChunkName.TORCHES_QUANTITY);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Skull Keys Quantity", 0x20B, 0x01, 0x00,
                DataChunkName.SKULL_KEYS_QUANTITY);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Grapple", 0x209, 0x01, 0x00, DataChunkName.GRAPPLE);

            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Torches turns until it extinguishes", 0x301, 0x01,
                0x00, DataChunkName.TORCHES_TURNS);

            // moonstones and moongates
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte,
                "0-0xFF Moonstone X Coordinates (valid only if buried)", 0x28A, 0x08, 0x00,
                DataChunkName.MOONSTONE_X_COORDS);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte,
                "0-0xFF Moonstone Y Coordinates (valid only if buried)", 0x292, 0x08, 0x00,
                DataChunkName.MOONSTONE_Y_COORDS);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "0=buried,0xFF=Inventory Moonstone Flags", 0x29A,
                0x08, 0x00, DataChunkName.MOONSTONE_BURIED);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte,
                "0=Britannia,0xFF=Underworld Moonstone Z Coordinates (valid only if buried)", 0x2A2, 0x08, 0x00,
                DataChunkName.MOONSTONE_Z_COORDS);

            // misc
            DataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Number of Party Members", 0x2B5, 0x1, 0x00,
                DataChunkName.N_PEOPLE_PARTY);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Active Character - 0-5,0xFF=None", 0x2D5, 0x01,
                0x00, DataChunkName.ACTIVE_CHARACTER);

            // time and date
            DataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16, "Current Year", 0x2CE, 0x02, 0x00,
                DataChunkName.CURRENT_YEAR);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Month", 0x2D7, 0x01, 0x00,
                DataChunkName.CURRENT_MONTH);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Day", 0x2D8, 0x01, 0x00,
                DataChunkName.CURRENT_DAY);
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Hour", 0x2D9, 0x01, 0x00,
                DataChunkName.CURRENT_HOUR);
            // 0x2DA is copy of 2D9 for some reason
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Minute", 0x2DB, 0x01, 0x00,
                DataChunkName.CURRENT_MINUTE);

            // Initialize a table to determine if an NPC hsa been killed/is dead
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Bitmap, "NPC Dead Bitmap", 0x5B4, 0x80, 0x00,
                DataChunkName.NPC_ISDEAD_TABLE);
            List<bool> npcAlive = DataChunks.GetDataChunk(DataChunkName.NPC_ISDEAD_TABLE).GetAsBitmapBoolList();
            NPCIsDeadArray = Utils.ListTo2DArray(npcAlive, NonPlayerCharacterReferences.NPCS_PER_TOWN, 0x00,
                NonPlayerCharacterReferences.NPCS_PER_TOWN *
                SmallMapReferences.SingleMapReference.TOTAL_SMALL_MAP_LOCATIONS);

            // Initialize a table to determine if an NPC has been met
            DataChunks.AddDataChunk(DataChunk.DataFormatType.Bitmap, "NPC Met Bitmap", 0x634, 0x80, 0x00,
                DataChunkName.NPC_ISMET_TABLE);
            List<bool> npcMet = DataChunks.GetDataChunk(DataChunkName.NPC_ISMET_TABLE).GetAsBitmapBoolList();
            NPCIsMetArray = Utils.ListTo2DArray(npcMet, NonPlayerCharacterReferences.NPCS_PER_TOWN, 0x00,
                NonPlayerCharacterReferences.NPCS_PER_TOWN *
                SmallMapReferences.SingleMapReference.TOTAL_SMALL_MAP_LOCATIONS);

            // Currently Unused
            DataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "NPC Type InitialMap", 0x5B4, 0x20, 0x00,
                DataChunkName.NPC_TYPES);

            // get the NPCs movement list - 0x20 NPCs, with 0x10 movement commands each, consisting of 0x1 direction byte + 0x1 repetitions
            // bajh: Jan 12 2020, moved from BB8 to BBA to test a theory that it actually begins a few bytes after the original documentation indicates
            DataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "NPC Movement List", 0xBB8,
                0x20 * 0x10 * sizeof(byte) * 2, 0x00, DataChunkName.NPC_MOVEMENT_LISTS);

            // get the offsets to the current movement instructions of the NPCs
            DataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "NPC Movement Offset Lists", 0xFB8,
                0x20 * sizeof(byte) * 2, 0x00, DataChunkName.NPC_MOVEMENT_OFFSETS);

            // this stores monsters, party, objects and NPC location info and other stuff too (apparently!?)
            DataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Character Animation States - Current Environment", 0x6B4, 0x100, 0x00,
                DataChunkName.CHARACTER_ANIMATION_STATES);

            // this stores monsters, party, objects and NPC location info and other stuff too (apparently!?)
            DataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "Character States - including xyz", 0x9B8,
                0x200, 0x00, DataChunkName.CHARACTER_STATES);

            // we will need to add 0x100 for now, but cannot because it's read in as a bytelist
            DataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "NPC Sprite (by smallmap)", 0xFF8, 0x20, 0x00,
                DataChunkName.NPC_SPRITE_INDEXES);

            //// MapUnitStates
            OverworldMapUnitStates = new MapUnitStates(OverworldOverlayDataChunks);
            UnderworldMapUnitStates = new MapUnitStates(UnderworldOverlayDataChunks);
            ActiveMapUnitStates = new MapUnitStates(ActiveOverlayDataChunks);

            OverworldMapUnitStates.InitializeMapUnits(MapUnitStates.MapUnitStatesFiles.BRIT_OOL, bLoadFromDisk);
            UnderworldMapUnitStates.InitializeMapUnits(MapUnitStates.MapUnitStatesFiles.UNDER_OOL, bLoadFromDisk);
            ActiveMapUnitStates.InitializeMapUnits(MapUnitStates.MapUnitStatesFiles.SAVED_GAM, bLoadFromDisk);

            switch (InitialMap)
            {
                case Map.Maps.Small:
                    // small, overworld and underworld always have saved Animation states so we load them in at the beginning
                    SmallMapUnitStates = ActiveMapUnitStates;
                    break;
                case Map.Maps.Overworld:
                    OverworldMapUnitStates = ActiveMapUnitStates;
                    break;
                case Map.Maps.Underworld:
                    UnderworldMapUnitStates = ActiveMapUnitStates;
                    break;
                case Map.Maps.Combat:
                    throw new Ultima5ReduxException("You can't initialize the MapUnits with a combat map");
                default:
                    throw new InvalidEnumArgumentException(((int)InitialMap).ToString());
            }

            SmallMapCharacterStates = new SmallMapCharacterStates(CharacterStatesDataChunk);
            CharacterMovements = new MapUnitMovements(NonPlayerCharacterMovementLists,
                NonPlayerCharacterMovementOffsets);

            TheNonPlayerCharacterStates = new NonPlayerCharacterStates(this);
        }

        public MapUnitStates GetMapUnitStatesByMap(Map.Maps map)
        {
            return map switch
            {
                Map.Maps.Small => SmallMapUnitStates,
                Map.Maps.Overworld => OverworldMapUnitStates,
                Map.Maps.Underworld => UnderworldMapUnitStates,
                Map.Maps.Combat => throw new Ultima5ReduxException(
                    "Can't return a map state for a combat map from the imported game state"),
                _ => throw new Ultima5ReduxException("Asked for a CurrentMapUnitStates that doesn't exist:" + map)
            };
        }
    }
}