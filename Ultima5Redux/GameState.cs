using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ultima5Redux
{
    class GameState
    {
        private Random ran = new Random();

        private DataChunks dataChunks;
        private List<byte> gameStateByteArray;

        public string AvatarsName { get { return "Fred"; } }

        public GameState(string u5Directory)
        {
            string saveFileAndPath = Path.Combine(u5Directory, FileConstants.SAVED_GAM);

            dataChunks = new DataChunks();

            gameStateByteArray = Utils.GetFileAsByteList(saveFileAndPath);

            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Bitmap, "NPC Killed Bitmap", gameStateByteArray, 0x5B4, 0x80));
            dataChunks.AddDataChunk(new DataChunk(DataChunk.DataFormatType.Bitmap, "NPC Met Bitmap", gameStateByteArray, 0x634, 0x80));

            List<bool> npcMet = dataChunks.GetChunk(1).GetAsBitmapBoolList();
            bool[][] npcMetArray = Utils.ListTo2DArray<bool>(npcMet, 0x20*8, 0x00, 0x80*8);

            //bool[][] npcMetArray = Utils.ListTo2DArray<bool>()
        }

        public bool OneInXOdds(int howMany)
        {
            // if ran%howMany is zero then we beat the odds
            int nextRan = ran.Next();
            return ((nextRan % howMany) == 0);
        }

        public bool NpcHasMetAvatar(NonPlayerCharacters.NonPlayerCharacter npc)
        {
            return false;
        }

    }
}
