using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ultima5Redux
{
    class Signs
    {
        public class Sign
        {
            private const int CHARS_PER_LINE= 16;

            public SmallMapReference.SingleMapReference.Location Location { get; }
            public int Floor { get; }
            public byte X { get; } 
            public byte Y { get;  }
            public string SignText { get; }

            /// <summary>
            /// A print function that frankly doesn't work very well...
            /// </summary>
            public void PrintSign()      
            {
                int remainder = 0;
                Math.DivRem(SignText.Length, CHARS_PER_LINE, out remainder);
                int lines = remainder == 0 ? SignText.Length / CHARS_PER_LINE : (SignText.Length / CHARS_PER_LINE) + 1;
                for (int i = 0; i < lines;  i++)
                {
                    int remainingChars = SignText.Length - (CHARS_PER_LINE * i);
                    Console.WriteLine(SignText.Substring(i*CHARS_PER_LINE, remainingChars<16?remainingChars:CHARS_PER_LINE));
                }
            }

            /// <summary>
            /// Create a sign object
            /// </summary>
            /// <param name="location">the location the sign is in</param>
            /// <param name="floor">the floor the sign appears in</param>
            /// <param name="x">x coord of sign</param>
            /// <param name="y">y coord of sign</param>
            /// <param name="signText">Text of sign (may contain unpritable txt that requires fonts from ibm.ch and runes.ch</param>
            public Sign(SmallMapReference.SingleMapReference.Location location, int floor,  byte x, byte y, string signText)
            {
                Location = location;
                Floor = floor;
                X = x;
                Y = y;
                SignText = signText;
            }
        }

        private const short TOTAL_SIGNS = 0x21;
        private const byte END_OF_STRING_BYTE = 0x00;

        private List<byte> signsByteArray = new List<byte>();
        private List<int> signsOffsets = new List<int>(TOTAL_SIGNS);
        private List<Sign> signList = new List<Sign>(TOTAL_SIGNS);


        /// <summary>
        /// Loads the "Look" descriptions
        /// </summary>
        /// <param name="u5directory">directory of data files</param>
        public Signs(string u5directory)
        {
            signsByteArray = Utils.GetFileAsByteList(Path.Combine(u5directory, FileConstants.SIGNS_DAT));

            // add all of the offsets to a list
            // double TOTAL_LOOKS because we are using 16 bit integers, using two bytes at a time
            for (int i = 0; i < (TOTAL_SIGNS * 2); i += 2)
            {
//                signsOffsets.Add((int)(signsByteArray[i] | (((uint)signsByteArray[i + 1]) << 8)));
                signsOffsets.Add((int)(signsByteArray[i] | (((uint)signsByteArray[i + 1]) << 8)));
            }

            // using the offsets, populate the sign objects
            // note: this could be on demand, but would seem like a lot of wasted cycles in exchange for a little bit more free memory
            for (int i = 0; i < TOTAL_SIGNS; i++)
            {
                int offset = signsOffsets[i];
                string signtxt = Utils.BytesToStringNullTerm(signsByteArray, offset + 4);
                signList.Add(new Sign((SmallMapReference.SingleMapReference.Location)signsByteArray[offset],
                    signsByteArray[offset + 1],
                    signsByteArray[offset + 2],
                    signsByteArray[offset + 3],
                    Utils.BytesToStringNullTerm(signsByteArray, offset+4) ) );
                signList[i].PrintSign();
            }
        }
    }
}
