using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ultima5Redux
{
    public class Signs
    {
        public class Sign
        {
            private const int CHARS_PER_LINE = 16;

            public int Offset { get; }

            /// <summary>
            /// General location of sign
            /// </summary>
            public SmallMapReference.SingleMapReference.Location Location { get; }
            /// <summary>
            /// Floor of location
            /// </summary>
            public int Floor { get; }
            /// <summary>
            /// X coordinate of sign
            /// </summary>
            public byte X { get; }
            /// <summary>
            /// Y coordinate of sign
            /// </summary>
            public byte Y { get; }
            /// <summary>
            /// Actual text of sign
            /// </summary>
            public string SignText {
                get
                {
                    return ScrubSignText(RawSignText);
                }
            }

            public string SignTextCleanedSpaces
            { 
                get
                {
                    return TrimSpareSpacesFromSign(SignText);
                }
            }


            public string RawSignText { get; }

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

            private static string TrimSpareSpacesFromSign(string signText)
            {
                char[] signTextArray = signText.ToArray();
                string trimmedStr = string.Empty;

                string[] lines = signText.Split('\n');

                foreach (string line in lines)
                {
                    trimmedStr += line.Trim() + "\n";
                }
                return trimmedStr.Trim();

            }

            private static string ScrubSignText(byte[] signBytes)
            {
                StringBuilder sb = new StringBuilder();
                foreach (byte curByte in signBytes)
                {
                    if (curByte != '\0')
                        sb.Append((char)curByte);
                }
                return sb.ToString();
            }

            private static string ScrubSignText(string signText)
            {
                char[] signTextArray = signText.ToArray();
                string scrubbedStr = string.Empty;

                // 8 = top left
                // l = horizontal line
                // m = top num
                // 9 = top right
                // g = verticle line
                // n = bottom sign post
                // : = bottom left
                // ; = bottom right
                // +)g = small sign top row
                // f)* = small sign bottom row
                // a = scrawly top left
                // b = scrawly horizontal
                // c = scrawly top right
                // d = scrawly bottom left
                // e = scrawly bottom horizontal (double line)
                // f = scrawly bottom right


                Dictionary<char, string> replacementChars = new Dictionary<char, string>();
                replacementChars.Add('@', "*"); // the actual character is a solid circle for separation //((char)0xA7).ToString());
                replacementChars.Add('[', "TH");
                replacementChars.Add('^', "EA");
                replacementChars.Add('_', "ST");


                char prevChar = '\0';
                foreach (char signChar in signTextArray)
                {
                    if (char.IsUpper(signChar) || signChar == ' ')
                    {
                        scrubbedStr += signChar;
                    }
                    else if (replacementChars.ContainsKey(signChar))
                    {
                        scrubbedStr += replacementChars[signChar];
                    }
                    // if we have two verticle bars then we move to the next line
                    else if (signChar == 'g' && prevChar == 'g')
                    {
                        // do nothing and leave it out
                        scrubbedStr += '\n';
                    }
                    prevChar = signChar;
                }
                return scrubbedStr;
            }

            public Sign(SmallMapReference.SingleMapReference.Location location, int floor, byte x, byte y, byte[] signText, int nOffset)
                : this (location, floor, x, y, ScrubSignText(signText), nOffset)
            {
                
            }


            /// <summary>
            /// Create a sign object
            /// </summary>
            /// <param name="location">the location the sign is in</param>
            /// <param name="floor">the floor the sign appears in</param>
            /// <param name="x">x coord of sign</param>
            /// <param name="y">y coord of sign</param>
            /// <param name="signText">Text of sign (may contain unpritable txt that requires fonts from ibm.ch and runes.ch</param>
            public Sign(SmallMapReference.SingleMapReference.Location location, int floor,  byte x, byte y, string signText, int nOffset)
            {
                Location = location;
                Floor = floor;
                X = x;
                Y = y;
                RawSignText = signText;
                Offset = nOffset;
                //SignText = signText;
            }
        }

        /// <summary>
        /// Total number of expected signs in file
        /// </summary>
        private const short TOTAL_SIGNS = 0x21;
        /// <summary>
        /// character used to denote end of string
        /// </summary>
        private const byte END_OF_STRING_BYTE = 0x00;

        /// <summary>
        /// Raw sign file
        /// </summary>
        private List<byte> signsByteArray = new List<byte>();
        /// <summary>
        /// List of all sign offsets in file
        /// </summary>
        private List<int> signsOffsets = new List<int>(TOTAL_SIGNS);
        /// <summary>
        /// List of all assembled signs
        /// </summary>
        private List<Sign> signList = new List<Sign>(TOTAL_SIGNS);

        public Sign GetSign(int nSign)
        {
            return signList[nSign];
        }

        public Sign GetSign(SmallMapReference.SingleMapReference.Location location, int x, int y)
        {
            foreach (Sign sign in signList)
            {
                if (sign.X == x && sign.Y == y && sign.Location == location)
                {
                    return sign;
                }
            }
            throw new Exception("You asked for a sigh that simple doesn't exist in " + location + " at X=" + x.ToString() + " Y="+y.ToString());
        }

        public static string GetEightLawsSign(SmallMapReference.SingleMapReference.Location location)
        {
            switch (location)
            {
                case SmallMapReference.SingleMapReference.Location.Moonglow:
                    //honesty
                    return "Thou shalt not lie, or thou shalt lose thy tongue.";
                case SmallMapReference.SingleMapReference.Location.Britain:
                    // compassion
                    return "Thou shalt help those in need, or thou shalt suffer the same need.";
                case SmallMapReference.SingleMapReference.Location.Jhelom:
                    // valor
                    return "Thou shalt fight to the death if challenged, or thou shalt be banished as a coward.";
                case SmallMapReference.SingleMapReference.Location.Yew:
                    // justice
                    return "Thou shalt confess to thy crime and suffer its just punishment, or thou shalt be put to death.";
                case SmallMapReference.SingleMapReference.Location.Minoc:
                    // sacrifice
                    return "Thou shalt donate half of thy income to charity, or thou shalt have no income.";
                case SmallMapReference.SingleMapReference.Location.Trinsic:
                    // honor
                    return "If thou dost lose thine own honor, thou shalt take thine own life.";
                case SmallMapReference.SingleMapReference.Location.Skara_Brae:
                    // spirituality
                    return "Thou shalt enforce the laws of virtue, or thou shalt die as a heretic.";
                case SmallMapReference.SingleMapReference.Location.New_Magincia:
                    // humility
                    return "Thou shalt humble thyself to thy superiors, or thou shalt suffer their wrath.";
                default:
                    throw new Exception("You can't get a string for the eight laws from a place that isn't one of the cities of virtue (place=" + location + ")");
            }

        }

        /// <summary>
        /// Loads the "Look" descriptions
        /// </summary>
        /// <param name="u5directory">directory of data files</param>
        public Signs(string u5directory)
        {
            signsByteArray = Utils.GetFileAsByteList(Path.Combine(u5directory, FileConstants.SIGNS_DAT));

            // add all of the offsets to a list
            // double TOTAL_LOOKS because we are using 16 bit integers, using two bytes at a time
            //for (int i = 0; i < (TOTAL_SIGNS * 2); i += 2)
            //{
            //    signsOffsets.Add((int)(signsByteArray[i] | (((uint)signsByteArray[i + 1]) << 8)));
            //}

            int nIndex = TOTAL_SIGNS * 2;
            // we are ignoring the "offsets" which are likely used to help optimize the lookup 
            // on older hardware, instead we will just be lazy and search for them by cycling
            // through the whole list
            // TODO: optimize storage to improve lookup spped
            do
            {
                string rawSignTxt = Utils.BytesToStringNullTerm(signsByteArray, nIndex + 4, 0xFF);
                //string signtxt = ScrubSignText(rawSignTxt);
                signList.Add(new Sign((SmallMapReference.SingleMapReference.Location)signsByteArray[nIndex],
                    signsByteArray[nIndex + 1],
                    signsByteArray[nIndex + 2],
                    signsByteArray[nIndex + 3],
                    rawSignTxt, nIndex) );
                nIndex += rawSignTxt.Length + 1 + 4; // we hop over the string plus it's null byte plus the four bytes for definition
            // while we don't encounter four zero bytes in a row, which is eseentially the end of the file
            } while (!(signsByteArray[nIndex] == 0 && signsByteArray[nIndex+1] == 0 && signsByteArray[nIndex+2] == 0 && signsByteArray[nIndex+3] == 0));

            List<byte> dataovlSignsByteArray = Utils.GetFileAsByteList(Path.Combine(u5directory, FileConstants.DATA_OVL));
            List<byte> shSign = DataChunk.CreateDataChunk(DataChunk.DataFormatType.ByteList, "SH Sign of Eight Laws", dataovlSignsByteArray, 0x743a, 0x66).GetAsByteList();
            signList.Add(new Sign(SmallMapReference.SingleMapReference.Location.Serpents_Hold, 0, 15, 19, shSign.ToArray(), 0x743a));
            //signList.Add();

        }


    }
}
