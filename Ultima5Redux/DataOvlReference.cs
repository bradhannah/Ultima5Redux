using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ultima5Redux
{

    public class RingAndAmults : SomeStrings
    {
        public RingAndAmults() : base(null, 0, 0)
        {

        }
    }

    

    class DataOvlReference
    {
        //private List<byte> dataOvlByteArray;
        private DataChunks<DataChunkName> dataChunks;

        private void PrintGaps()
        {

        }

        public enum DataChunkName {
            Unused = -1,
            TALK_COMPRESSED_WORDS,
            //LOCATION_NAME_INDEXES_1,
            //LOCATION_NAME_INDEXES_2,
            LOCATION_NAME_INDEXES,
            LOCATION_NAMES };

        //private Dictionary<DataChunkName, DataChunk> chunkMap=new Dictionary<DataChunkName, DataChunk>();

        public DataChunk GetDataChunk(DataChunk.DataFormatType dataType, string description, int offset, int length)
        {
            return new DataChunk(dataType, description, dataChunks.FileByteList, offset, length);
        }

        public DataChunk GetDataChunk(DataChunkName dataChunkName)
        {
            return dataChunks.GetDataChunk(dataChunkName);
        }


        /*  private void AddDataChunk(DataChunk.DataFormatType dataFormat, string description, List<byte> rawData, int offset, int dataLength, byte addToValue, DataChunkName dataChunkName)
          {
              // create the data chunk 
              DataChunk chunk = new DataChunk(dataFormat, description, rawData, offset, dataLength, addToValue);

              // all data chunks get added to the chunk list
              dataChunks.AddDataChunk(chunk);

              // if the datachunk is not classified as unused then add it to the chunk map for quick reference
              if (dataChunkName != DataChunkName.Unused)
              {
                  chunkMap.Add(dataChunkName, chunk);
              }
          }

          private void AddDataChunk(DataChunk.DataFormatType dataFormat, string description, List<byte> rawData, int offset, int dataLength, byte addToValue)
          {
              AddDataChunk(dataFormat, description, rawData, offset, dataLength, addToValue, DataChunkName.Unused);
          }
          */
        public DataOvlReference(string u5Directory)
        {
            string dataOvlFileAndPath = Path.Combine(u5Directory, FileConstants.DATA_OVL);

            dataChunks = new DataChunks<DataChunkName>(dataOvlFileAndPath, DataChunkName.Unused);

            //dataOvlByteArray = Utils.GetFileAsByteList(dataOvlFileAndPath);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unkown", 0x00, 0x18);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.FixedString, "Licence for the MS-Runtime", 0x18, 0x38);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Armour strings (13 of them)", 0x52, 0xA6);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Weapon strings (10 of them)", 0xF8, 0x81);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Ring and amulet strings (5 of them)", 0x179, 0x5a);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Character type, monster names (44 of them)", 0x1d3, 0x158);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Character type, monster names in capital letters (44 of them)", 0x32b, 0x165);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Unknown", 0x490, 0x33);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Item names (5 of them)", 0x4c3, 0x2b);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "(x where x goes from 0 to 7", 0x4ee, 0x18);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Shard names (3 of them)", 0x506, 0x29); // changed from 0x28 to 0x29
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Additional item names (6 of them)", 0x52f, 0x43);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Shortened names (29 of them)", 0x572, 0x11a);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Potion colors (8 of them)", 0x68c, 0x30);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Reagents (8 of them)", 0x6bc, 0x4d);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Spell names", 0x709, 0x1bb);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Character type and names (11 of them)", 0x8c4, 0x54);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Health text (5 of them)", 0x918, 0x29);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Spell runes (26 of them)", 0x941, 0x64);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Unknown", 0x9a5, 0xa8);



            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "City names (in caps) (26 of them)", 0xa4d, 0x111, 0, DataChunkName.LOCATION_NAMES);
            //dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "City names (in caps) (26 of them)", 0xa4d, 0x111);
            //dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Dungeon names (8 of them)", 0xb5e, 0x3a);

            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Virtue names (8 of them)", 0xb98, 0x48);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Virtue mantras (8 of them)", 0xbe0, 0x1e);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Store names", 0xbfe, 0x2fc);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Character names", 0xefa, 0x152);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Compressed words used in the conversation files", 0x104c, 0x24e, 0, DataChunkName.TALK_COMPRESSED_WORDS);
            //dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Compressed words used in the conversation files", 0x104c, 0x24e);

            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Filenames", 0x129a, 0x11c);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x13b6, 0x3a6);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Weapon strings (add + 0x10)", 0x175c, 0xa9, 0x10);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "Armor index (add + 0x10)", 0x1806, 0x70, 0x10);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "Text index (add + 0x10)", 0x187a, 0x1ee, 0x10);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for TOWNE.DAT)", 0x1e2a, 0x8);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for DWELLING.DAT)", 0x1e32, 0x8);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for CASTLE.DAT)", 0x1e3a, 0x8);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for KEEP.DAT)", 0x1e42, 0x8);

            //            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "Name of cities index (13 shorts, add 0x10)", 0x1e4a, 0x1a, 0x10);
            //            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "Name of dwellings/castle/keeps/dungeons index (22 shorts, add 0x10))", 0x1e6e, 0x2c, 0x10);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "Name of cities index (13+22 shorts, add 0x10)", 0x1e4a, 0x50, 0x10, DataChunkName.LOCATION_NAME_INDEXES);
            //AddDataChunk(DataChunk.DataFormatType.UINT16List, "Name of cities index (13+22 shorts, add 0x10)", 0x1e4a, 0x1a, 0x10, DataChunkName.LOCATION_NAME_INDEXES_1);
            //AddDataChunk(DataChunk.DataFormatType.UINT16List, "Name of cities index (13+22 shorts, add 0x10)", 0x1e6e, 0x2c, 0x10, DataChunkName.LOCATION_NAME_INDEXES_2);

            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "X-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", 0x1e9a, 0x28);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", 0x1ec2, 0x28);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", 0x1ec2, 0x28);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", 0x1ec2, 0x28);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", 0x1ec2, 0x28);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", 0x1ec2, 0x28);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", 0x1ec2, 0x28);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Virtue and mantra index (add + 0x10)", 0x1f5e, 0x20, 0x10);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x1f7e, 0x33b);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Arms seller's name index", 0x22da, 0x12);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x22ec, 0x20c);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Indexes to the dialog text (add + 0x10) (see .TLK)", 0x24f8, 0x13e, 0x10);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, ".DAT file names (4 files)", 0x2636, 0x2b);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x2661, 0x9);

            /// the following are reindexed. The file has some gunk in the middle of the strings which is indescript.
            //dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", 0x266a, 0x269);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", 0x266a, 0xE1); // tweaked
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x2750, 0x28); // tweaked
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Text strings(some unknown in the middle)", 0x2778, 0x6F); // tweaked
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x27E7, 0x0C); // tweaked
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Text strings(some unknown in the middle)", 0x27F3, 0xE0); // tweaked

            /// the following are reindexed. The file has some gunk in the middle of the strings which is indescript.
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x28d3, 0x83);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", 0x2956, 0x278);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x2bce, 0x9a);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", 0x2c68, 0x3c);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x2ca4, 0x9);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", 0x2cad, 0x146);

            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x2df4, 0x14d);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "Initial string", 0x2f41, 0x5b);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "STORY.DAT string", 0x2f9d, 0xa);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x3664, 0x322);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Chunking information for Britannia's map, 0xFF the chunk only consists of tile 0x1, otherwise see BRIT.DAT", 0x3886, 0x100);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Random filenames, texts and unknown", 0x3986, 0xaf);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x3a35, 0x7d);

            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "Armour accessory base prices", 0x3a92, 0x10);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Nil", 0x3aa2, 0x2);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "Armour base prices", 0x3aa4, 0xc);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "Weapon base prices", 0x3ab2, 0x2e);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Nil", 0x3ae0, 0x2);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "What weapons are sold by the merchant in cities: Britain, Jhelom, Yew, Minoc, Trinsic, British Castle, Buccaneer's Den, Border March, Serpent Hold - (9 groups of 8 bytes)	", 0x3af2, 0x48);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x3b3a, 0x38);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "Innkeeper welcome text index into SHOPPE.DAT (+0x0, 2 bytes for each index)", 0x3b72, 0x8);

            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Random texts", 0x41e4, 0x8c1);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x4aa5, 0x2f2);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Random texts", 0x4d97, 0x361);

            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Inn room description text", 0x4e7e, 0xc);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Inn bed X-coordinate", 0x4e8a, 0x5);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Inn bed Y-coordinate", 0x4e90, 0x5);


            /// extended stuff "old list"
            //flags that define the special abilities of
            //             monsters during combat; 32 bits per monster
            //             0x0020 = undead (affected by An Xen Corp)
            //             todo:
            //             - passes through walls (ghost, shadowlord)
            //             - can become invisible (wisp, ghost, shadowlord)
            //             - can teleport (wisp, shadowlord)
            //             - can't move (reaper, mimic)
            //             - able to camouflage itself
            //             - may divide when hit (slime, gargoyle)
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "flags that define the special abilities of monsters during combat", 0x154C, 0x30 * 2);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16List, "moon phases (28 byte pairs, one for each day of the month)", 0x1EEA, 0x38);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "x coordinates of shrines", 0x1F7E, 0x8);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "y coordinates of shrines", 0x1F86, 0x8);

            // this section contains information about hidden, non-regenerating objects (e.g. the magic axe in the dead tree in Jhelom); there are
            // only 0x71 such objects; the last entry in each table is 0
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "object type (tile - 0x100) (item)", 0x3E88, 0x72);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "object quality (e.g. potion type, number of gems) (item)", 0x3EFA, 0x72);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "location number (see \"Party Location\") (item)", 0x3F6C, 0x72);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "level (item)", 0x3FDE, 0x72);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "x coordinate (item)", 0x4050, 0x72);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "y coordinate (item)", 0x40C2, 0x72);

            // dock coordinates (where puchased ships/skiffs are placed)
            // 0 = Jhelom
            // 1 = Minoc
            // 2 = East Brittany
            // 3 = Buccaneer's Den
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "x coordinate (dock)", 0x4D86, 0x4);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "y coordinate (dock)", 0x4D8A, 0x4);

            // scan code translation table:
            // when the player presses a key that produces one of the scan codes in
            // the first table, the game translates it to the corresponding code in
            // the second table
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "scancodes", 0x541E, 0x8);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "internal codes", 0x5426, 0x8);



            /// begin bajh manual review
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Random texts", 0x4e96, 0x263);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x50F9, 0x377);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Random texts", 0x5470, 0x71);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x54e1, 0x83);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Random texts", 0x5564, 0x49);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Nil", 0x55Ac, 0x1470);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Unknown", 0x6a1c, 0xce);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Random texts", 0x6aea, 0x21d);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Stat lines (z-stats?)", 0x6d08, 0x43);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "Ultima IV", 0x6d48, 0xb);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Stat lines (z-stats?)", 0x6d08, 0x43);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "NPC Files", 0x6d56, 0x2e);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Responses to keystroke actions", 0x6d84, 0x179);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Victory/Lost Messages", 0x6efe, 0x1e);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Things that happened to you", 0x6f1c, 0xf4);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "MISC file names", 0x7010, 0x1a);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "Unknown String", 0x702A, 0Xa);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Things said in jail", 0x7034, 0xb4);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "BRIT.DAT", 0x70E8, 0xa);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Random texts", 0x70f2, 0xe0);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "KARMA.DAT", 0x71d2, 0xa);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Random texts (maybe power word?)", 0x71dc, 0x2c);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Strings about wishing well", 0x721c, 0x36);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "wishing for one of these keywords at a wishing well gets you a horse", 0x7252, 0x32); // in the original defintion
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Response Strings after wishing in wishing well", 0x7284, 0x27);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Fountain strings", 0x72ac, 0x54);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Time of day strings", 0x7300, 0x26);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Keep flame names", 0x7326, 0x18);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Dungeon names", 0x733e, 0x46);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Look2.dat (x2)", 0x7384, 0x14);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Signs?", 0x7398, 0x15e);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Signs.dat (x2)", 0x74f6, 0x14);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Vision strings", 0x750a, 0x22);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Things you see (dungeons I think)", 0x752c, 0x1e4);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Drinking Strings", 0x76ef, 0x71);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Level up apparition strings", 0x7760, 0x94);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Karma.dat (x2)", 0x77f4, 0x14);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Level up apparition strings", 0x7808, 0x2e);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Time of day", 0x7836, 0x1a);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Time of day", 0x7836, 0x1a);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "Shoppe.dat", 0x7850, 0xc);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Magic shop strings", 0x785c, 0x1be);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "Shoppe.dat", 0x7a1a, 0xc);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Armour/weapon shop strings", 0x7a26, 0x4e4);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "Shoppe.dat", 0x7f0a, 0xc);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Healer shop strings", 0x7f16, 0x2f8);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "end.dat", 0x820e, 0x8);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Numbers as strings (ie. twelfth)", 0x8216, 0x17c);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "End of game strings", 0x8392, 0xfe);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "miscmaps.dat", 0x8490, 0xe);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.SimpleString, "endmsg.dat", 0x849e, 0xc);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "random texts", 0x84aa, 0x74);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "finding/searching for things strings ", 0x851e, 0x442);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "wher you found something (ie. In the wall) ", 0x8960, 0xe4);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "strings about unlocking or finding doors", 0x8a44, 0x1ac);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "potion colours", 0x8bfa, 0x34);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "scroll shortfroms", 0x8c2e, 0x20);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "more found things!", 0x8c4e, 0xcc);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "getting things string!", 0x8c4e, 0x238);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "movement strings", 0x8e86, 0xed);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Mixing spells", 0x8f74, 0xbe);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "movement strings", 0x9032, 0x30);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "pay fine/bribe, merchant chat", 0x9062, 0x1b2);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, ".tlk file list", 0x9216, 0x2e);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Talking strings for ALL npcs", 0x9244, 0x1a);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Dirty words", 0x925e, 0xda);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Common talking responses", 0x9338, 0x1cc);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Directions", 0x9504, 0x3e);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "random texts", 0x9542, 0x2c);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "random texts", 0x9542, 0x2c);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "4 character shrine names (ie. hone)", 0x956e, 0x30);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "shrine strings", 0x959e, 0x6e);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "MISCMAP*.* filenames", 0x960c, 0x29);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "urn strings", 0x9635, 0x33);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "save game strings", 0x9668, 0x21);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "OOL and SAV files", 0x968a, 0x32);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Inventory and Stats strings", 0x96BC, 0x136);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Inventory warnings", 0x97f2, 0x1c8);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Battle messages", 0x99ba, 0xfe);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Buying wine dialog", 0x9ab8, 0x22c);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "4 character short form NPC questions, all quest related ", 0x9ce4, 0x9c);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Character names, perhaps resitance people (hints?)", 0x9d80, 0x220);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Strings related to intro", 0xa020, 0x5a);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Character creation related strings", 0xa07a, 0xa6);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Responses to key presses", 0xa120, 0x2a0);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "Anti piracy messages", 0xa3c0, 0x170);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Unknown, "Nil", 0xA530, 0x1820);
            //dataChunks.PrintEverything();
        }


        //public DataOvlReference(string u5Directory)
        //{
        //    string dataOvlFileAndPath = Path.Combine(u5Directory, FileConstants.DATA_OVL);

        //    dataChunks = new DataChunks<DataChunkName>(dataOvlFileAndPath, DataChunkName.Unused);

        //    //dataOvlByteArray = Utils.GetFileAsByteList(dataOvlFileAndPath);

        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unkown", dataOvlByteArray, 0x00, 0x18),DataChunkName);
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.FixedString, "Licence for the MS-Runtime", dataOvlByteArray, 0x18, 0x38));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Armour strings (13 of them)", dataOvlByteArray, 0x52, 0xA6));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Weapon strings (10 of them)", dataOvlByteArray, 0xF8, 0x81));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Ring and amulet strings (5 of them)", dataOvlByteArray, 0x179, 0x5a));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Character type, monster names (44 of them)", dataOvlByteArray, 0x1d3, 0x158));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Character type, monster names in capital letters (44 of them)", dataOvlByteArray, 0x32b, 0x165));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Unknown", dataOvlByteArray, 0x490, 0x33));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Item names (5 of them)", dataOvlByteArray, 0x4c3, 0x2b));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "(x where x goes from 0 to 7", dataOvlByteArray, 0x4ee, 0x18));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Shard names (3 of them)", dataOvlByteArray, 0x506, 0x29)); // changed from 0x28 to 0x29
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Additional item names (6 of them)", dataOvlByteArray, 0x52f, 0x43));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Shortened names (29 of them)", dataOvlByteArray, 0x572, 0x11a));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Potion colors (8 of them)", dataOvlByteArray, 0x68c, 0x30));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Reagents (8 of them)", dataOvlByteArray, 0x6bc, 0x4d));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Spell names", dataOvlByteArray, 0x709, 0x1bb));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Character type and names (11 of them)", dataOvlByteArray, 0x8c4, 0x54));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Health text (5 of them)", dataOvlByteArray, 0x918, 0x29));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Spell runes (26 of them)", dataOvlByteArray, 0x941, 0x64));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Unknown", dataOvlByteArray, 0x9a5, 0xa8));


            
        //    dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "City names (in caps) (26 of them)", 0xa4d, 0x111, 0, DataChunkName.LOCATION_NAMES);
        //    //dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "City names (in caps) (26 of them)", dataOvlByteArray, 0xa4d, 0x111));
        //    //dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Dungeon names (8 of them)", dataOvlByteArray, 0xb5e, 0x3a));

        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Virtue names (8 of them)", dataOvlByteArray, 0xb98, 0x48));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Virtue mantras (8 of them)", dataOvlByteArray, 0xbe0, 0x1e));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Store names", dataOvlByteArray, 0xbfe, 0x2fc));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Character names", dataOvlByteArray, 0xefa, 0x152));
        //    AddDataChunk(DataChunk.DataFormatType.StringList, "Compressed words used in the conversation files", dataOvlByteArray, 0x104c, 0x24e, 0, DataChunkName.TALK_COMPRESSED_WORDS);
        //    //dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Compressed words used in the conversation files", dataOvlByteArray, 0x104c, 0x24e));

        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Filenames", dataOvlByteArray, 0x129a, 0x11c));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x13b6, 0x3a6));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Weapon strings (add + 0x10)", dataOvlByteArray, 0x175c, 0xa9, 0x10));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Armor index (add + 0x10)", dataOvlByteArray, 0x1806, 0x70, 0x10));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Text index (add + 0x10)", dataOvlByteArray, 0x187a, 0x1ee, 0x10));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for TOWNE.DAT)", dataOvlByteArray, 0x1e2a, 0x8));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for DWELLING.DAT)", dataOvlByteArray, 0x1e32, 0x8));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for CASTLE.DAT)", dataOvlByteArray, 0x1e3a, 0x8));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for KEEP.DAT)", dataOvlByteArray, 0x1e42, 0x8));

        //    //            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Name of cities index (13 shorts, add 0x10)", dataOvlByteArray, 0x1e4a, 0x1a, 0x10));
        //    //            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Name of dwellings/castle/keeps/dungeons index (22 shorts, add 0x10))", dataOvlByteArray, 0x1e6e, 0x2c, 0x10));
        //    AddDataChunk(DataChunk.DataFormatType.UINT16List, "Name of cities index (13+22 shorts, add 0x10)", dataOvlByteArray, 0x1e4a, 0x50, 0x10, DataChunkName.LOCATION_NAME_INDEXES);
        //    //AddDataChunk(DataChunk.DataFormatType.UINT16List, "Name of cities index (13+22 shorts, add 0x10)", dataOvlByteArray, 0x1e4a, 0x1a, 0x10, DataChunkName.LOCATION_NAME_INDEXES_1);
        //    //AddDataChunk(DataChunk.DataFormatType.UINT16List, "Name of cities index (13+22 shorts, add 0x10)", dataOvlByteArray, 0x1e6e, 0x2c, 0x10, DataChunkName.LOCATION_NAME_INDEXES_2);

        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "X-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1e9a, 0x28));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Virtue and mantra index (add + 0x10)", dataOvlByteArray, 0x1f5e, 0x20, 0x10));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x1f7e, 0x33b));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Arms seller's name index", dataOvlByteArray, 0x22da, 0x12));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x22ec, 0x20c));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Indexes to the dialog text (add + 0x10) (see .TLK)", dataOvlByteArray, 0x24f8, 0x13e, 0x10));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, ".DAT file names (4 files)", dataOvlByteArray, 0x2636, 0x2b));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x2661, 0x9));

        //    /// the following are reindexed. The file has some gunk in the middle of the strings which is indescript.
        //    //dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", dataOvlByteArray, 0x266a, 0x269));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", dataOvlByteArray, 0x266a, 0xE1)); // tweaked
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x2750, 0x28)); // tweaked
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings(some unknown in the middle)", dataOvlByteArray, 0x2778, 0x6F)); // tweaked
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x27E7, 0x0C)); // tweaked
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings(some unknown in the middle)", dataOvlByteArray, 0x27F3, 0xE0)); // tweaked

        //    /// the following are reindexed. The file has some gunk in the middle of the strings which is indescript.
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x28d3, 0x83));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", dataOvlByteArray, 0x2956, 0x278));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x2bce, 0x9a));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", dataOvlByteArray, 0x2c68, 0x3c));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x2ca4, 0x9));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", dataOvlByteArray, 0x2cad, 0x146));

        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x2df4, 0x14d));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "Initial string", dataOvlByteArray, 0x2f41, 0x5b));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "STORY.DAT string", dataOvlByteArray, 0x2f9d, 0xa));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x3664, 0x322));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Chunking information for Britannia's map, 0xFF the chunk only consists of tile 0x1, otherwise see BRIT.DAT", dataOvlByteArray, 0x3886, 0x100));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Random filenames, texts and unknown", dataOvlByteArray, 0x3986, 0xaf));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x3a35, 0x7d));

        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Armour accessory base prices", dataOvlByteArray, 0x3a92, 0x10));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Nil", dataOvlByteArray, 0x3aa2, 0x2));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Armour base prices", dataOvlByteArray, 0x3aa4, 0xc));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Weapon base prices", dataOvlByteArray, 0x3ab2, 0x2e));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Nil", dataOvlByteArray, 0x3ae0, 0x2));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "What weapons are sold by the merchant in cities: Britain, Jhelom, Yew, Minoc, Trinsic, British Castle, Buccaneer's Den, Border March, Serpent Hold - (9 groups of 8 bytes)	", dataOvlByteArray, 0x3af2,0x48));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x3b3a, 0x38));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Innkeeper welcome text index into SHOPPE.DAT (+0x0, 2 bytes for each index)", dataOvlByteArray, 0x3b72, 0x8));

        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Random texts", dataOvlByteArray, 0x41e4, 0x8c1));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x4aa5, 0x2f2));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Random texts", dataOvlByteArray, 0x4d97, 0x361));

        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Inn room description text", dataOvlByteArray, 0x4e7e, 0xc));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Inn bed X-coordinate", dataOvlByteArray, 0x4e8a, 0x5));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Inn bed Y-coordinate", dataOvlByteArray, 0x4e90, 0x5));


        //    /// extended stuff "old list"
        //    //flags that define the special abilities of
        //    //             monsters during combat; 32 bits per monster
        //    //             0x0020 = undead (affected by An Xen Corp)
        //    //             todo:
        //    //             - passes through walls (ghost, shadowlord)
        //    //             - can become invisible (wisp, ghost, shadowlord)
        //    //             - can teleport (wisp, shadowlord)
        //    //             - can't move (reaper, mimic)
        //    //             - able to camouflage itself
        //    //             - may divide when hit (slime, gargoyle)
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "flags that define the special abilities of monsters during combat", dataOvlByteArray, 0x154C, 0x30 * 2));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "moon phases (28 byte pairs, one for each day of the month)", dataOvlByteArray, 0x1EEA, 0x38));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "x coordinates of shrines", dataOvlByteArray, 0x1F7E, 0x8));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "y coordinates of shrines", dataOvlByteArray, 0x1F86, 0x8));

        //    // this section contains information about hidden, non-regenerating objects (e.g. the magic axe in the dead tree in Jhelom); there are
        //    // only 0x71 such objects; the last entry in each table is 0
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "object type (tile - 0x100) (item)", dataOvlByteArray, 0x3E88, 0x72));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "object quality (e.g. potion type, number of gems) (item)", dataOvlByteArray, 0x3EFA, 0x72));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "location number (see \"Party Location\") (item)", dataOvlByteArray, 0x3F6C, 0x72));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "level (item)", dataOvlByteArray, 0x3FDE, 0x72));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "x coordinate (item)", dataOvlByteArray, 0x4050, 0x72));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "y coordinate (item)", dataOvlByteArray, 0x40C2, 0x72));

        //    // dock coordinates (where puchased ships/skiffs are placed)
        //    // 0 = Jhelom
        //    // 1 = Minoc
        //    // 2 = East Brittany
        //    // 3 = Buccaneer's Den
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "x coordinate (dock)", dataOvlByteArray, 0x4D86, 0x4));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "y coordinate (dock)", dataOvlByteArray, 0x4D8A, 0x4));

        //    // scan code translation table:
        //    // when the player presses a key that produces one of the scan codes in
        //    // the first table, the game translates it to the corresponding code in
        //    // the second table
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "scancodes", dataOvlByteArray, 0x541E, 0x8));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "internal codes", dataOvlByteArray, 0x5426, 0x8));



        //    /// begin bajh manual review
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Random texts", dataOvlByteArray, 0x4e96, 0x263));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x50F9, 0x377));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Random texts", dataOvlByteArray, 0x5470, 0x71));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x54e1, 0x83));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Random texts", dataOvlByteArray, 0x5564, 0x49));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Nil", dataOvlByteArray, 0x55Ac, 0x1470));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x6a1c, 0xce));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Random texts", dataOvlByteArray, 0x6aea, 0x21d));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Stat lines (z-stats?)", dataOvlByteArray, 0x6d08, 0x43));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "Ultima IV", dataOvlByteArray, 0x6d48, 0xb));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Stat lines (z-stats?)", dataOvlByteArray, 0x6d08, 0x43));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "NPC Files", dataOvlByteArray, 0x6d56, 0x2e));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Responses to keystroke actions", dataOvlByteArray, 0x6d84, 0x179));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Victory/Lost Messages", dataOvlByteArray, 0x6efe, 0x1e));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Things that happened to you", dataOvlByteArray, 0x6f1c, 0xf4));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "MISC file names", dataOvlByteArray, 0x7010, 0x1a));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "Unknown String", dataOvlByteArray, 0x702A, 0Xa));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Things said in jail", dataOvlByteArray, 0x7034, 0xb4));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "BRIT.DAT", dataOvlByteArray, 0x70E8, 0xa));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Random texts", dataOvlByteArray, 0x70f2, 0xe0));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "KARMA.DAT", dataOvlByteArray, 0x71d2, 0xa));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Random texts (maybe power word?)", dataOvlByteArray, 0x71dc, 0x2c));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Strings about wishing well", dataOvlByteArray, 0x721c, 0x36));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "wishing for one of these keywords at a wishing well gets you a horse", dataOvlByteArray, 0x7252, 0x32)); // in the original defintion
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Response Strings after wishing in wishing well", dataOvlByteArray, 0x7284, 0x27));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Fountain strings", dataOvlByteArray, 0x72ac, 0x54));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Time of day strings", dataOvlByteArray, 0x7300, 0x26));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Keep flame names", dataOvlByteArray, 0x7326, 0x18));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Dungeon names", dataOvlByteArray, 0x733e, 0x46));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Look2.dat (x2)", dataOvlByteArray, 0x7384, 0x14));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Signs?", dataOvlByteArray, 0x7398, 0x15e));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Signs.dat (x2)", dataOvlByteArray, 0x74f6, 0x14));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Vision strings", dataOvlByteArray, 0x750a, 0x22));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Things you see (dungeons I think)", dataOvlByteArray, 0x752c, 0x1e4));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Drinking Strings", dataOvlByteArray, 0x76ef, 0x71));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Level up apparition strings", dataOvlByteArray, 0x7760, 0x94));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Karma.dat (x2)", dataOvlByteArray, 0x77f4, 0x14));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Level up apparition strings", dataOvlByteArray, 0x7808, 0x2e));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Time of day", dataOvlByteArray, 0x7836, 0x1a));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Time of day", dataOvlByteArray, 0x7836, 0x1a));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "Shoppe.dat", dataOvlByteArray, 0x7850, 0xc));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Magic shop strings", dataOvlByteArray, 0x785c, 0x1be));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "Shoppe.dat", dataOvlByteArray, 0x7a1a, 0xc));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Armour/weapon shop strings", dataOvlByteArray, 0x7a26, 0x4e4));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "Shoppe.dat", dataOvlByteArray, 0x7f0a, 0xc));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Healer shop strings", dataOvlByteArray, 0x7f16, 0x2f8));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "end.dat", dataOvlByteArray, 0x820e, 0x8));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Numbers as strings (ie. twelfth)", dataOvlByteArray, 0x8216, 0x17c));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "End of game strings", dataOvlByteArray, 0x8392, 0xfe));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "miscmaps.dat", dataOvlByteArray, 0x8490, 0xe));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.SimpleString, "endmsg.dat", dataOvlByteArray, 0x849e, 0xc));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "random texts", dataOvlByteArray, 0x84aa, 0x74));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "finding/searching for things strings ", dataOvlByteArray, 0x851e, 0x442));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "wher you found something (ie. In the wall) ", dataOvlByteArray, 0x8960, 0xe4));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "strings about unlocking or finding doors", dataOvlByteArray, 0x8a44, 0x1ac));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "potion colours", dataOvlByteArray, 0x8bfa, 0x34));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "scroll shortfroms", dataOvlByteArray, 0x8c2e, 0x20));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "more found things!", dataOvlByteArray, 0x8c4e, 0xcc));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "getting things string!", dataOvlByteArray, 0x8c4e, 0x238));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "movement strings", dataOvlByteArray, 0x8e86, 0xed));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Mixing spells", dataOvlByteArray, 0x8f74, 0xbe));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "movement strings", dataOvlByteArray, 0x9032, 0x30));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "pay fine/bribe, merchant chat", dataOvlByteArray, 0x9062, 0x1b2));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, ".tlk file list", dataOvlByteArray, 0x9216, 0x2e));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Talking strings for ALL npcs", dataOvlByteArray, 0x9244, 0x1a));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Dirty words", dataOvlByteArray, 0x925e, 0xda));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Common talking responses", dataOvlByteArray, 0x9338, 0x1cc));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Directions", dataOvlByteArray, 0x9504, 0x3e));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "random texts", dataOvlByteArray, 0x9542, 0x2c));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "random texts", dataOvlByteArray, 0x9542, 0x2c));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "4 character shrine names (ie. hone)", dataOvlByteArray, 0x956e, 0x30));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "shrine strings", dataOvlByteArray, 0x959e, 0x6e));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "MISCMAP*.* filenames", dataOvlByteArray, 0x960c, 0x29));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "urn strings", dataOvlByteArray, 0x9635, 0x33));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "save game strings", dataOvlByteArray, 0x9668, 0x21));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "OOL and SAV files", dataOvlByteArray, 0x968a, 0x32));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Inventory and Stats strings", dataOvlByteArray, 0x96BC, 0x136));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Inventory warnings", dataOvlByteArray, 0x97f2, 0x1c8));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Battle messages", dataOvlByteArray, 0x99ba, 0xfe));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Buying wine dialog", dataOvlByteArray, 0x9ab8, 0x22c));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "4 character short form NPC questions, all quest related ", dataOvlByteArray, 0x9ce4, 0x9c));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Character names, perhaps resitance people (hints?)", dataOvlByteArray, 0x9d80, 0x220));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Strings related to intro", dataOvlByteArray, 0xa020, 0x5a));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Character creation related strings", dataOvlByteArray, 0xa07a, 0xa6));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Responses to key presses", dataOvlByteArray, 0xa120, 0x2a0));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Anti piracy messages", dataOvlByteArray, 0xa3c0, 0x170));
        //    dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Nil", dataOvlByteArray, 0xA530, 0x1820));
        //    //dataChunks.PrintEverything();
        //}
    }
}
