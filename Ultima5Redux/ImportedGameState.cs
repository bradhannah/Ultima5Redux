﻿using System.Collections.Generic;
using System.IO;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux
{
    internal class ImportedGameState
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
            KARMA
            //, MONSTERS_AND_STUFF_TABLE
        }
        
        private enum OverlayChunkName { Unused, CHARACTER_ANIMATION_STATES }
        private readonly DataChunks<OverlayChunkName> _overworldOverlayDataChunks;
        private readonly DataChunks<OverlayChunkName> _underworldOverlayDataChunks;
        
        private DataChunks<DataChunkName> DataChunks { get; }

        /// <summary>
        ///     Current location
        /// </summary>
        internal SmallMapReferences.SingleMapReference.Location Location =>
            (SmallMapReferences.SingleMapReference.Location) DataChunks.GetDataChunk(DataChunkName.PARTY_LOC)
                .GetChunkAsByte();

        /// <summary>
        ///     Current floor
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        internal int Floor => DataChunks.GetDataChunk(DataChunkName.Z_COORD).GetChunkAsByte();

        /// <summary>
        ///     Saved X location of Avatar
        /// </summary>
        internal int X => DataChunks.GetDataChunk(DataChunkName.X_COORD).GetChunkAsByte();

        /// <summary>
        ///     Saved Y location of Avatar
        /// </summary>
        internal int Y => DataChunks.GetDataChunk(DataChunkName.Y_COORD).GetChunkAsByte();

        /// <summary>
        ///     Which map am I currently on?
        /// </summary>
        internal Map.Maps InitialMap =>
            Location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld
                ? Floor == 0xFF ? Map.Maps.Underworld : Map.Maps.Overworld
                : Map.Maps.Small;
        
        /// <summary>
        ///     Players total gold
        /// </summary>
        internal ushort Gold => DataChunks.GetDataChunk(DataChunkName.GOLD_QUANTITY).GetChunkAsUint16();
        /// <summary>
        ///     Players total food
        /// </summary>
        internal ushort Food => DataChunks.GetDataChunk(DataChunkName.FOOD_QUANTITY).GetChunkAsUint16();
        /// <summary>
        ///     Does the Avatar have a grapple?
        /// </summary>
        internal bool HasGrapple => DataChunks.GetDataChunk(DataChunkName.GRAPPLE).GetChunkAsByte() != 0x00;
         /// <summary>
        ///     How many turns left until your torch is burnt out?
        /// </summary>
         internal byte TorchTurnsLeft => DataChunks.GetDataChunk(DataChunkName.TORCHES_TURNS).GetChunkAsByte();
         internal byte Keys => DataChunks.GetDataChunk(DataChunkName.KEYS_QUANTITY).GetChunkAsByte();
        internal byte SkullKeys => DataChunks.GetDataChunk(DataChunkName.SKULL_KEYS_QUANTITY).GetChunkAsByte();
        internal byte Gems => DataChunks.GetDataChunk(DataChunkName.GEMS_QUANTITY).GetChunkAsByte();
        internal byte Torches => DataChunks.GetDataChunk(DataChunkName.TORCHES_QUANTITY).GetChunkAsByte();
        internal byte ActivePlayerNumber => DataChunks.GetDataChunk(DataChunkName.ACTIVE_CHARACTER).GetChunkAsByte();
        internal byte Karma => DataChunks.GetDataChunk(DataChunkName.KARMA).GetChunkAsByte();
        internal PlayerCharacterRecords CharacterRecords { get; }
        internal TimeOfDay TheTimeOfDay => new TimeOfDay(DataChunks.GetDataChunk(DataChunkName.CURRENT_YEAR),
            DataChunks.GetDataChunk(DataChunkName.CURRENT_MONTH),
            DataChunks.GetDataChunk(DataChunkName.CURRENT_DAY), DataChunks.GetDataChunk(DataChunkName.CURRENT_HOUR),
            DataChunks.GetDataChunk(DataChunkName.CURRENT_MINUTE));
        internal Moongates TheMoongates => new Moongates(GetDataChunk(DataChunkName.MOONSTONE_X_COORDS),
                GetDataChunk(DataChunkName.MOONSTONE_Y_COORDS),
                GetDataChunk(DataChunkName.MOONSTONE_BURIED), GetDataChunk(DataChunkName.MOONSTONE_Z_COORDS));

        // DataChunk based properties (not ideal) 
        internal DataChunk NonPlayerCharacterMovementLists => DataChunks.GetDataChunk(DataChunkName.NPC_MOVEMENT_LISTS);
        internal DataChunk NonPlayerCharacterMovementOffsets => DataChunks.GetDataChunk(DataChunkName.NPC_MOVEMENT_OFFSETS);
        internal DataChunk OverworldOverlayDataChunks =>
            _overworldOverlayDataChunks.GetDataChunk(OverlayChunkName.CHARACTER_ANIMATION_STATES);
        internal DataChunk UnderworldOverlayDataChunks =>
            _underworldOverlayDataChunks.GetDataChunk(OverlayChunkName.CHARACTER_ANIMATION_STATES);
        internal DataChunk CharacterAnimationStatesDataChunk =>
            DataChunks.GetDataChunk(DataChunkName.CHARACTER_ANIMATION_STATES);
        internal DataChunk CharacterStatesDataChunk => DataChunks.GetDataChunk(DataChunkName.CHARACTER_STATES);
        internal DataChunk NonPlayerCharacterKeySprites => DataChunks.GetDataChunk(DataChunkName.NPC_SPRITE_INDEXES);
        internal List<byte> GameStateByteArray { get; }
        
        internal bool[][] NPCIsDeadArray { get; }
        internal bool[][] NPCIsMetArray { get; }
        
        public ImportedGameState(string u5Directory)
        {
            string saveFileAndPath = Path.Combine(u5Directory, FileConstants.SAVED_GAM);

            DataChunks = new DataChunks<DataChunkName>(saveFileAndPath, DataChunkName.Unused);

            GameStateByteArray = Utils.GetFileAsByteList(saveFileAndPath);

            // import all character records
            DataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "All Character Records (ie. name, stats)", 0x02,
                0x20 * 16, 0x00, DataChunkName.CHARACTER_RECORDS);
            DataChunk rawCharacterRecords = DataChunks.GetDataChunk(DataChunkName.CHARACTER_RECORDS);
            CharacterRecords = new PlayerCharacterRecords(rawCharacterRecords.GetAsByteList());

            DataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Player Karma", 0x2E2, 0x01, 0x00,
                DataChunkName.KARMA);
            
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
            
            // load the overworld and underworld overlays
            // they are stored in the saved.ool file - but also the brit.ool and under.ool file - not quite sure why it's stored in both...
            string savedOolFile = Path.Combine(u5Directory, FileConstants.SAVED_OOL);
            string overworldOverlayPath = savedOolFile;
            string underworldOverlayPath = savedOolFile;
            _overworldOverlayDataChunks =
                new DataChunks<OverlayChunkName>(overworldOverlayPath, OverlayChunkName.Unused);
            _underworldOverlayDataChunks =
                new DataChunks<OverlayChunkName>(underworldOverlayPath, OverlayChunkName.Unused);

            _overworldOverlayDataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Character Animation States - Overworld", 0x00, 0x100, 0x00,
                OverlayChunkName.CHARACTER_ANIMATION_STATES);
            _underworldOverlayDataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList,
                "Character Animation States - Underworld", 0x100, 0x100, 0x00,
                OverlayChunkName.CHARACTER_ANIMATION_STATES);
            
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
        }

        private DataChunk GetDataChunk(DataChunkName dataChunkName)
        {
            return DataChunks.GetDataChunk(dataChunkName);
        }
    }
}