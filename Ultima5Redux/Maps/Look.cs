using System.Collections.Generic;
using System.IO;

namespace Ultima5Redux.Maps
{
    /// <summary>
    ///     Provides look descriptions for tiles
    /// </summary>
    public class Look
    {
        /// <summary>
        ///     Total number of titles that can be looked at
        /// </summary>
        private const short TOTAL_LOOKS = 0x200;

        /// <summary>
        ///     Raw file
        /// </summary>
        private static List<byte> LookByteArray = new List<byte>();

        /// <summary>
        ///     List of all offsets into the lookByteArray
        /// </summary>
        private static readonly List<int> LookOffsets = new List<int>(TOTAL_LOOKS);

        /// <summary>
        ///     Loads the "Look" descriptions
        /// </summary>
        /// <param name="u5directory">directory of data files</param>
        public Look(string u5directory)
        {
            LookByteArray = Utils.GetFileAsByteList(Path.Combine(u5directory, FileConstants.LOOK2_DAT));

            // double TOTAL_LOOKS because we are using 16 bit integers, using two bytes at a time
            for (int i = 0; i < TOTAL_LOOKS * 2; i += 2)
            {
                LookOffsets.Add((int)(LookByteArray[i] | ((uint)LookByteArray[i + 1] << 8)));
            }
        }

        /// <summary>
        ///     Get the look description based on tile number
        /// </summary>
        /// <param name="tileNumber">Tile number from 0 to 0x199</param>
        /// <returns>A very brief description of the tile</returns>
        public static string GetLookDescription(int tileNumber)
        {
            string description = "";
            int lookOffset = LookOffsets[tileNumber];

            byte curCharByte;
            // loop until a zero byte is found indicating end of string
            while ((curCharByte = LookByteArray[lookOffset]) != 0)
            {
                // cast to (char) to ensure the string understands it not a number
                description += (char)curCharByte;
                lookOffset++;
            }

            return description;
        }
    }
}