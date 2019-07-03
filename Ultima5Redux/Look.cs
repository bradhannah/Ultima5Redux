using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ultima5Redux
{
    /// <summary>
    /// Provides look descriptions for tiles
    /// </summary>
    class Look
    {
        /// <summary>
        /// Total number of titles that can be looked at
        /// </summary>
        private const short TOTAL_LOOKS = 0x200;
        private const byte END_OF_STRING_BYTE = 0x00;

        /// <summary>
        /// Raw file
        /// </summary>
        static private List<byte> lookByteArray = new List<byte>();
        /// <summary>
        /// List of all offsets into the lookByteArray
        /// </summary>
        static private List<int> lookOffsets = new List<int>(TOTAL_LOOKS);

        /// <summary>
        /// Loads the "Look" descriptions
        /// </summary>
        /// <param name="u5directory">directory of data files</param>
        public Look(string u5directory)
        {
            lookByteArray = Utils.GetFileAsByteList(Path.Combine(u5directory, FileConstants.LOOK2_DAT));

            // double TOTAL_LOOKS because we are using 16 bit integers, using two bytes at a time
            for (int i = 0; i < (TOTAL_LOOKS * 2); i+=2)
            {
                lookOffsets.Add((int)(lookByteArray[i] | (((uint)lookByteArray[i+1]) << 8)));
            }
        }

        /// <summary>
        /// Get the look description based on tile number 
        /// </summary>
        /// <param name="tileNumber">Tile number from 0 to 0x199</param>
        /// <returns>A very brief description of the tile</returns>
        public string GetLookDescription(int tileNumber)
        {
            string description = "";
            int lookOffset = lookOffsets[tileNumber];

            byte curCharByte;
            // loop until a zero byte is found indicating end of string
            while ( (curCharByte = lookByteArray[lookOffset]) != 0)
            {
                // cast to (char) to ensure the string understands it not a number
                description += (char)curCharByte;
                lookOffset++;
            }

            return (description);
        }
    }
}
