using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Ultima5Redux.Data;

namespace Ultima5Redux.Maps
{
    public class Signs
    {
        /// <summary>
        ///     Total number of expected signs in file
        /// </summary>
        private const short TOTAL_SIGNS = 0x21;

        /// <summary>
        ///     character used to denote end of string
        /// </summary>
        private const byte END_OF_STRING_BYTE = 0x00;

        /// <summary>
        ///     List of all assembled signs
        /// </summary>
        private readonly List<Sign> _signList = new List<Sign>(TOTAL_SIGNS);

        /// <summary>
        ///     Raw sign file
        /// </summary>
        private readonly List<byte> _signsByteArray = new List<byte>();

        /// <summary>
        ///     List of all sign offsets in file
        /// </summary>
        private List<int> _signsOffsets = new List<int>(TOTAL_SIGNS);

        /// <summary>
        ///     Loads the "Look" descriptions
        /// </summary>
        /// <param name="u5directory">directory of data files</param>
        public Signs(string u5directory)
        {
            _signsByteArray = Utils.GetFileAsByteList(Path.Combine(u5directory, FileConstants.SIGNS_DAT));

            int nIndex = TOTAL_SIGNS * 2;
            // we are ignoring the "offsets" which are likely used to help optimize the lookup 
            // on older hardware, instead we will just be lazy and search for them by cycling
            // through the whole list
            // TODO: optimize storage to improve lookup speed
            do
            {
                string rawSignTxt = Utils.BytesToStringNullTerm(_signsByteArray, nIndex + 4, 0xFF);
                int nRawSignTxtLength = rawSignTxt.Length;

                // there are often two "warning signs" in the main virtue townes. Only one of the signs text is actually 
                // populated - so if we see a "\n" as the only string, then we look ahead to the next signs text and use
                // it instead
                if (rawSignTxt.Trim() == string.Empty)
                {
                    int nNextSignAdjust = rawSignTxt.Length + 1 + 4;
                    rawSignTxt = Utils.BytesToStringNullTerm(_signsByteArray, nIndex + 4 + nNextSignAdjust, 0xFF);
                }

                _signList.Add(new Sign((SmallMapReferences.SingleMapReference.Location) _signsByteArray[nIndex],
                    _signsByteArray[nIndex + 1],
                    _signsByteArray[nIndex + 2],
                    _signsByteArray[nIndex + 3],
                    rawSignTxt, nIndex));
                nIndex += nRawSignTxtLength + 1 +
                          4; // we hop over the string plus it's null byte plus the four bytes for definition
                // while we don't encounter four zero bytes in a row, which is essentially the end of the file
            } while (!(_signsByteArray[nIndex] == 0 && _signsByteArray[nIndex + 1] == 0 &&
                       _signsByteArray[nIndex + 2] == 0 && _signsByteArray[nIndex + 3] == 0));

            // there are some signs that are not included in the signs.dat file, so we manually pont to them and add them to our sign list
            List<byte> dataovlSignsByteArray =
                Utils.GetFileAsByteList(Path.Combine(u5directory, FileConstants.DATA_OVL));
            List<byte> shSign = DataChunk.CreateDataChunk(DataChunk.DataFormatType.ByteList, "SH Sign of Eight Laws",
                dataovlSignsByteArray, 0x743a, 0x66).GetAsByteList();
            _signList.Add(new Sign(SmallMapReferences.SingleMapReference.Location.Serpents_Hold, 0, 15, 19,
                shSign.ToArray(), 0x743a));
        }

        public Sign GetSign(int nSign)
        {
            return _signList[nSign];
        }

        public Sign GetSign(SmallMapReferences.SingleMapReference.Location location, int x, int y)
        {
            foreach (Sign sign in _signList)
            {
                if (sign.X == x && sign.Y == y && sign.Location == location) return sign;
            }

            //throw new Ultima5ReduxException("You asked for a sigh that simple doesn't exist in " + location + " at X=" + x.ToString() + " Y="+y.ToString());
            return null;
        }

        /// <summary>
        ///     Gets a copy of a sign and assigns new X, Y values
        ///     Commonly used for Blackthornes Eight Laws signs
        /// </summary>
        /// <param name="location"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="newX">new X value</param>
        /// <param name="newY">new Y value</param>
        /// <returns>a new copy of the sign</returns>
        public Sign CopySign(SmallMapReferences.SingleMapReference.Location location, int x, int y, int newX, int newY)
        {
            Sign origSign = GetSign(location, x, y);
            if (origSign == null) throw new Ultima5ReduxException("Original sign was not instantiated.");
            Sign newSign = new Sign(origSign.Location, origSign.Floor, newX, newY, origSign.RawSignText,
                origSign.Offset);
            Debug.Assert(newSign != null);
            return newSign;
        }


        public int GetNumberOfSigns()
        {
            return _signList.Count;
        }

        public class Sign
        {
            private const int CHARS_PER_LINE = 16;

            public Sign(SmallMapReferences.SingleMapReference.Location location, int floor, int x, int y,
                byte[] signText, int nOffset)
                : this(location, floor, x, y, ScrubSignText(signText), nOffset)
            {
            }


            /// <summary>
            ///     Create a sign object
            /// </summary>
            /// <param name="location">the location the sign is in</param>
            /// <param name="floor">the floor the sign appears in</param>
            /// <param name="x">x coord of sign</param>
            /// <param name="y">y coord of sign</param>
            /// <param name="signText">Text of sign (may contain unpritable txt that requires fonts from ibm.ch and runes.ch</param>
            public Sign(SmallMapReferences.SingleMapReference.Location location, int floor, int x, int y,
                string signText, int nOffset)
            {
                Location = location;
                Floor = floor;
                X = x;
                Y = y;
                RawSignText = signText;
                Offset = nOffset;
                //SignText = signText;
            }

            public int Offset { get; }

            /// <summary>
            ///     General location of sign
            /// </summary>
            public SmallMapReferences.SingleMapReference.Location Location { get; }

            /// <summary>
            ///     Floor of location
            /// </summary>
            public int Floor { get; }

            /// <summary>
            ///     X coordinate of sign
            /// </summary>
            public int X { get; }

            /// <summary>
            ///     Y coordinate of sign
            /// </summary>
            public int Y { get; }

            /// <summary>
            ///     Actual text of sign
            /// </summary>
            public string SignText => ScrubSignText(RawSignText);

            public string SignTextCleanedSpaces => TrimSpareSpacesFromSign(SignText);


            public string RawSignText { get; }

            /// <summary>
            ///     A print function that frankly doesn't work very well...
            /// </summary>
            public void PrintSign()
            {
                int remainder = 0;
                _ = Math.DivRem(SignText.Length, CHARS_PER_LINE, out remainder);
                int lines = remainder == 0 ? SignText.Length / CHARS_PER_LINE : SignText.Length / CHARS_PER_LINE + 1;
                for (int i = 0; i < lines; i++)
                {
                    int remainingChars = SignText.Length - CHARS_PER_LINE * i;
                    Console.WriteLine(SignText.Substring(i * CHARS_PER_LINE,
                        remainingChars < 16 ? remainingChars : CHARS_PER_LINE));
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
                        sb.Append((char) curByte);
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
                replacementChars.Add('@',
                    " "); // the actual character is a solid circle for separation //((char)0xA7).ToString());
                replacementChars.Add('[', "TH");
                replacementChars.Add('^', "EA");
                replacementChars.Add('_', "ST");
                replacementChars.Add(']', "NG");
                replacementChars.Add('\\', "EE");
                //replacementChars.Add((char)138, "\n");

                char prevChar = '\0';
                foreach (char signChar in signTextArray)
                {
                    // do not translate lowercase because they are often used for drawing the actual signs
                    if (signChar >= 'A' && signChar <= 'Z' || signChar == ' ')
                        scrubbedStr += signChar;
                    else if (replacementChars.ContainsKey(signChar))
                        scrubbedStr += replacementChars[signChar];
                    // if we have two verticle bars then we move to the next line   
                    else if (signChar == 'g' && prevChar == 'g')
                        // do nothing and leave it out
                        scrubbedStr += '\n';
                    //else if ((signChar >= 127 + 'a' && signChar <= 127 + 'z') || (signChar >= 127 + 'A' && signChar <= 127 + 'Z'))
                    else if (signChar > 127) scrubbedStr += ((char) (signChar - 128)).ToString();
                    prevChar = signChar;
                }

                return scrubbedStr;
            }
        }
    }
}