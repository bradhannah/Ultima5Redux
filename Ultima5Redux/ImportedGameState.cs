using System;
using System.Collections.Generic;
using System.IO;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux
{
    internal class ImportedGameState
    {
        /// <summary>
        ///     Data chunks for each of the save game sections
        /// </summary>
        internal enum DataChunkName
        {
            Unused, CHARACTER_RECORDS, NPC_ISALIVE_TABLE, NPC_ISMET_TABLE, N_PEOPLE_PARTY, FOOD_QUANTITY, GOLD_QUANTITY,
            KEYS_QUANTITY, GEMS_QUANTITY, TORCHES_QUANTITY, TORCHES_TURNS, CURRENT_YEAR, CURRENT_MONTH, CURRENT_DAY,
            CURRENT_HOUR, CURRENT_MINUTE, NPC_TYPES, NPC_MOVEMENT_LISTS, NPC_MOVEMENT_OFFSETS, NPC_SPRITE_INDEXES,
            PARTY_LOC, Z_COORD, X_COORD, Y_COORD, CHARACTER_ANIMATION_STATES, CHARACTER_STATES, MOONSTONE_X_COORDS,
            MOONSTONE_Y_COORDS, MOONSTONE_BURIED, MOONSTONE_Z_COORDS, ACTIVE_CHARACTER, GRAPPLE, SKULL_KEYS_QUANTITY,
            MONSTERS_AND_STUFF_TABLE
        }
        
        internal readonly PlayerCharacterRecords CharacterRecords;
        internal DataChunks<DataChunkName> DataChunks { get; }

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
        internal LargeMap.Maps InitialMap =>
            Location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld
                ? Floor == 0xFF ? LargeMap.Maps.Underworld : LargeMap.Maps.Overworld
                : LargeMap.Maps.Small;
        
        /// <summary>
        ///     Players total gold
        /// </summary>
        public ushort Gold => DataChunks.GetDataChunk(DataChunkName.GOLD_QUANTITY).GetChunkAsUint16();
        /// <summary>
        ///     Players total food
        /// </summary>
        public ushort Food => DataChunks.GetDataChunk(DataChunkName.FOOD_QUANTITY).GetChunkAsUint16();
        /// <summary>
        ///     Does the Avatar have a grapple?
        /// </summary>
        public bool HasGrapple => DataChunks.GetDataChunk(DataChunkName.GRAPPLE).GetChunkAsByte() != 0x00;
         /// <summary>
        ///     How many turns left until your torch is burnt out?
        /// </summary>
        public byte TorchTurnsLeft => DataChunks.GetDataChunk(DataChunkName.TORCHES_TURNS).GetChunkAsByte();
        public byte Keys => DataChunks.GetDataChunk(DataChunkName.KEYS_QUANTITY).GetChunkAsByte();
        public byte SkullKeys => DataChunks.GetDataChunk(DataChunkName.SKULL_KEYS_QUANTITY).GetChunkAsByte();
        public byte Gems => DataChunks.GetDataChunk(DataChunkName.GEMS_QUANTITY).GetChunkAsByte();
        public byte Torches => DataChunks.GetDataChunk(DataChunkName.TORCHES_QUANTITY).GetChunkAsByte();
        
        public ImportedGameState(string u5Directory, DataOvlReference dataOvlReference)
        {
                        string saveFileAndPath = Path.Combine(u5Directory, FileConstants.SAVED_GAM);

            DataChunks = new DataChunks<DataChunkName>(saveFileAndPath, DataChunkName.Unused);

            List<byte> gameStateByteArray = Utils.GetFileAsByteList(saveFileAndPath);

            // import all character records
            DataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "All Character Records (ie. name, stats)", 0x02,
                0x20 * 16, 0x00, DataChunkName.CHARACTER_RECORDS);
            DataChunk rawCharacterRecords = DataChunks.GetDataChunk(DataChunkName.CHARACTER_RECORDS);
            CharacterRecords = new PlayerCharacterRecords(rawCharacterRecords.GetAsByteList());

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

        }
    }
}