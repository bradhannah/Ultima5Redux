using System;
using System.Collections.Generic;

namespace Ultima5Redux.Data
{
    /// <summary>
    ///     A class that simplifies importing of byte arrays into a list of strings
    /// </summary>
    public class SomeStrings
    {
        /// <summary>
        ///     List of all strings
        /// </summary>
        public List<string> StringList { get; }

        /// <summary>
        ///     Constructs a simple SomeStrings object
        /// </summary>
        /// <param name="byteArray">the array of bytes to sample from</param>
        /// <param name="offset">which byte offset to begin at</param>
        /// <param name="length">the number of bytes to consume</param>
        public SomeStrings(List<byte> byteArray, int offset, int length)
        {
            StringList = new List<string>();
            int curOffset = offset;

            while (curOffset < length)
            {
                // we will not store null byte entries. Often string have two null bytes proceeding them, 
                // this often indicates the end of a list
                if (byteArray[curOffset] == 0x00)
                {
                    curOffset++;
                    continue;
                }

                string str = Utils.BytesToStringNullTerm(byteArray, curOffset, length - curOffset);

                StringList.Add(str);
                curOffset += str.Length + 1;
                // str = string.Empty;
            }
        }

        public void PrintSomeStrings()
        {
            foreach (string str in StringList)
            {
                Console.WriteLine(str);
            }
        }
    }
}