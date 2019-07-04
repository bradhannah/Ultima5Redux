using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ultima5Redux
{
    public class SomeStrings
    {
        public List<string> Strs { get;  }

        public SomeStrings(List<byte> byteArray, int offset, int length)
        {
            string str=string.Empty;
            Strs = new List<string>();
            int curOffset = offset;

            while (curOffset < length)
            {
                str = Utils.BytesToStringNullTerm(byteArray, curOffset, length-curOffset);

                Strs.Add(str);
                curOffset += str.Length + 1;
                str = string.Empty;
            }
        }

        public void PrintSomeStrings()
        {
            foreach (string str in Strs)
            {
                System.Console.WriteLine(str);
            }
        }
    }

    public class RingAndAmults : SomeStrings
    {
        public RingAndAmults() : base(null, 0, 0)
        {

        }
    }

    

    class DataOvlReference
    {
        private List<byte> dataOvlByteArray;
        private DataChunks dataChunks;

        public DataOvlReference(string u5Directory)
        {
            string dataOvlFileAndPath = Path.Combine(u5Directory, FileConstants.DATA_OVL);

            dataChunks = new DataChunks();

            dataOvlByteArray = Utils.GetFileAsByteList(dataOvlFileAndPath);

            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unkown", dataOvlByteArray, 0x00, 0x18));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.FixedString, "Licence for the MS-Runtime", dataOvlByteArray, 0x18, 0x38));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Armour strings (13 of them)", dataOvlByteArray, 0x52, 0xA6));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Weapon strings (10 of them)", dataOvlByteArray, 0xF8, 0x81));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Ring and amulet strings (5 of them)", dataOvlByteArray, 0x179, 0x5a));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Character type, monster names (44 of them)", dataOvlByteArray, 0x1d3, 0x158));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Character type, monster names in capital letters (44 of them)", dataOvlByteArray, 0x32b, 0x165));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Unknown", dataOvlByteArray, 0x490, 0x33));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Item names (5 of them)", dataOvlByteArray, 0x4c3, 0x2b));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "(x where x goes from 0 to 7", dataOvlByteArray, 0x4ee, 0x18));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Shard names (3 of them)", dataOvlByteArray, 0x506, 0x29)); // changed from 0x28 to 0x29
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Additional item names (6 of them)", dataOvlByteArray, 0x52f, 0x43));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Shortened names (29 of them)", dataOvlByteArray, 0x572, 0x11a));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Potion colors (8 of them)", dataOvlByteArray, 0x68c, 0x30));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Reagents (8 of them)", dataOvlByteArray, 0x6bc, 0x4d));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Spell names", dataOvlByteArray, 0x709, 0x1bb));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Character type and names (11 of them)", dataOvlByteArray, 0x8c4, 0x54));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Health text (5 of them)", dataOvlByteArray, 0x918, 0x29));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Spell runes (26 of them)", dataOvlByteArray, 0x941, 0x64));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Unknown", dataOvlByteArray, 0x9a5, 0xa8));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "City names (in caps) (26 of them)", dataOvlByteArray, 0xa4d, 0x111));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Dungeon names (8 of them)", dataOvlByteArray, 0xb5e, 0x3a));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Virtue names (8 of them)", dataOvlByteArray, 0xb98, 0x48));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Virtue mantras (8 of them)", dataOvlByteArray, 0xbe0, 0x1e));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Store names", dataOvlByteArray, 0xbfe, 0x2fc));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Character names", dataOvlByteArray, 0xefa, 0x152));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Compressed words used in the conversation files", dataOvlByteArray, 0x104c, 0x24e));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Filenames", dataOvlByteArray, 0x129a, 0x11c));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x13b6, 0x3a6));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Weapon strings (add + 0x10)", dataOvlByteArray, 0x175c, 0xa9, 0x10));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Armor index (add + 0x10)", dataOvlByteArray, 0x1806, 0x70, 0x10));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Text index (add + 0x10)", dataOvlByteArray, 0x187a, 0x1ee, 0x10));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for TOWNE.DAT)", dataOvlByteArray, 0x1e2a, 0x8));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for DWELLING.DAT)", dataOvlByteArray, 0x1e32, 0x8));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for CASTLE.DAT)", dataOvlByteArray, 0x1e3a, 0x8));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Which Map index do we start in (for KEEP.DAT)", dataOvlByteArray, 0x1e42, 0x8));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Name of cities index (13 shorts, add 0x10)", dataOvlByteArray, 0x1e4a, 0x1a, 0x10));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.UINT16List, "Name of dwellings/castle/keeps/dungeons index (22 shorts, add 0x10))", dataOvlByteArray, 0x1e6e, 0x2c, 0x10));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "X-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1e9a, 0x28));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Y-coordinates to Towns, Dwellings, Castles, Keeps, Dungeons", dataOvlByteArray, 0x1ec2, 0x28));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Virtue and mantra index (add + 0x10)", dataOvlByteArray, 0x1f5e, 0x20, 0x10));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x1f7e, 0x33b));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Arms seller's name index", dataOvlByteArray, 0x22da, 0x12));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x22ec, 0x20c));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.ByteList, "Indexes to the dialog text (add + 0x10) (see .TLK)", dataOvlByteArray, 0x24f8, 0x13e, 0x10));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, ".DAT file names (4 files)", dataOvlByteArray, 0x2636, 0x2b));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x2661, 0x9));

            /// the following are reindexed. The file has some gunk in the middle of the strings which is indescript.
            //dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", dataOvlByteArray, 0x266a, 0x269));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", dataOvlByteArray, 0x266a, 0xE1)); // tweaked
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x2750, 0x28)); // tweaked
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings(some unknown in the middle)", dataOvlByteArray, 0x2778, 0x6F)); // tweaked
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x27E7, 0x0C)); // tweaked
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings(some unknown in the middle)", dataOvlByteArray, 0x27F3, 0xE0)); // tweaked

            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Unknown, "Unknown", dataOvlByteArray, 0x28d3, 0x83));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.StringList, "Text strings (some unknown in the middle)", dataOvlByteArray, 0x2956, 0x49e));



            dataChunks.PrintEverything();



            //dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.

        }
    }
}
