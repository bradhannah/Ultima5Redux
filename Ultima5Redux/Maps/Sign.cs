using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ultima5Redux.Maps
{
    public class Sign
    {
        public enum SignType { SmallSign = 164, BigSign = 160, Tombstone = 138, Cross = 137, Warning = 248 }

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
        /// <param name="signText">Text of sign (may contain unprintable txt that requires fonts from ibm.ch and runes.ch</param>
        /// <param name="nOffset"></param>
        public Sign(SmallMapReferences.SingleMapReference.Location location, int floor, int x, int y,
            string signText, int nOffset)
        {
            Location = location;
            Floor = floor;
            X = x;
            Y = y;
            RawSignText = signText;
            Offset = nOffset;
        }


        private string RawSignText { get; }

        /// <summary>
        ///     Floor of location
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int Floor { get; }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int Offset { get; }

        /// <summary>
        ///     X coordinate of sign
        /// </summary>
        public int X { get; }

        /// <summary>
        ///     Y coordinate of sign
        /// </summary>
        public int Y { get; }

        /// <summary>
        ///     General location of sign
        /// </summary>
        public SmallMapReferences.SingleMapReference.Location Location { get; }

        /// <summary>
        ///     Actual text of sign
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public string SignText => ScrubSignText(RawSignText);

        // ReSharper disable once UnusedMember.Global
        public string SignTextCleanedSpaces => TrimSpareSpacesFromSign(SignText);

        /// <summary>
        ///     A print function that frankly doesn't work very well...
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public void PrintSign()
        {
            _ = Math.DivRem(SignText.Length, CHARS_PER_LINE, out int remainder);
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
            // g = vertical line
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

            Dictionary<char, string> replacementChars = new Dictionary<char, string>
            {
                { '@', " " },
                { '[', "TH" },
                { '^', "EA" },
                { '_', "ST" },
                { ']', "NG" },
                { '\\', "EE" }
            };
            // the actual character is a solid circle for separation //((char)0xA7).ToString());

            char prevChar = '\0';
            foreach (char signChar in signTextArray)
            {
                // do not translate lowercase because they are often used for drawing the actual signs
                if (signChar >= 'A' && signChar <= 'Z' || signChar == ' ')
                    scrubbedStr += signChar;
                else if (replacementChars.ContainsKey(signChar))
                    scrubbedStr += replacementChars[signChar];
                // if we have two vertical bars then we move to the next line   
                else if (signChar == 'g' && prevChar == 'g')
                    // do nothing and leave it out
                    scrubbedStr += '\n';
                else if (signChar > 127) scrubbedStr += ((char)(signChar - 128)).ToString();
                prevChar = signChar;
            }

            return scrubbedStr;
        }

        public static SignType GetSignTypeByIndex(int nIndex)
        {
            switch ((SignType)nIndex)
            {
                case SignType.SmallSign:
                case SignType.BigSign:
                case SignType.Tombstone:
                case SignType.Cross:
                case SignType.Warning:
                    return (SignType)nIndex;
                default:
                    throw new Ultima5ReduxException("Asked for Sign with index=" + nIndex +
                                                    " but that isn't a sign index");
            }
        }
    }
}