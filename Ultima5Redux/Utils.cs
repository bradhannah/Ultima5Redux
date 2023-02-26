using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ultima5Redux
{
    public static class Utils
    {
        public static readonly TextInfo EnTextInfo = new CultureInfo("en-US", false).TextInfo;
        public static Random Ran { get; set; } = new();

        private static T[][] Init2DArray<T>(int numberOfRows, int numberOfCols)
        {
            T[][] theArray = new T[numberOfRows][];
            for (int i = 0; i < numberOfRows; i++)
            {
                theArray[i] = new T[numberOfCols];
            }

            return theArray;
        }

        private static T[][] Init2DArray<T>(int numberOfRows, int numberOfCols, T defaultValue)
        {
            T[][] theArray = new T[numberOfRows][];
            for (int i = 0; i < numberOfRows; i++)
            {
                theArray[i] = new T[numberOfCols];
                for (int j = 0; j < numberOfCols; j++)
                {
                    theArray[i][j] = defaultValue;
                }
            }

            return theArray;
        }

        public static string AddSpacesBeforeCaps(string str)
        {
            // filthy method from here: https://stackoverflow.com/questions/272633/add-spaces-before-capital-letters
            return new string(str.SelectMany((c, i) => i > 0 && char.IsUpper(c) ? new[] { ' ', c } : new[] { c })
                .ToArray());
        }

        public static string BytesToStringFixedWidth(List<byte> byteArray, int offset, int length)
        {
            string str = string.Empty;
            for (int i = 0; i < length; i++)
            {
                str += (char)byteArray[i];
            }

            return str;
        }

        /// <summary>
        ///     Converts a byte[] to a readable string, assumes it ends with a NULL (0x00) byte
        /// </summary>
        /// <param name="byteArray"></param>
        /// <param name="offset">the offset to start at</param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string BytesToStringNullTerm(List<byte> byteArray, int offset, int length)
        {
            byte curCharByte;
            string str = "";
            int curOffset = offset;
            int count = 0;
            // loop until a zero byte is found indicating end of string
            while (count < length && (curCharByte = byteArray[curOffset]) != 0x00)
            {
                // cast to (char) to ensure the string understands it not a number
                str += (char)curCharByte;
                curOffset++;
                count++;
            }

            return str;
        }

        public static List<ushort> CreateOffsetList(byte[] byteArray, int offset, int length)
        {
            List<ushort> offsetArray = new();

            // double TOTAL_LOOKS because we are using 16 bit integers, using two bytes at a time
            for (int i = 0; i < length; i += 2)
            {
                offsetArray.Add((ushort)(byteArray[i] | (byteArray[i + 1] << 8)));
            }

            return offsetArray;
        }

        public static Queue<int> CreateRandomizedIntegerQueue(int nElements)
        {
            List<int> intList = new(nElements);
            for (int i = 0; i < nElements; i++)
            {
                intList.Add(i);
            }

            Random rng = new();
            Queue<int> randomizedQueue = new(intList.OrderBy(_ => rng.Next()));
            return randomizedQueue;
        }

        /// <summary>
        ///     Reads a list of bytes from a file.
        /// </summary>
        /// <remarks>Could be optimized with a cache function that keeps the particular file in memory</remarks>
        /// <param name="fileNameAndPath">Filename to open (read only)</param>
        /// <param name="offset">byte offset to start at</param>
        /// <param name="length">number of bytes to read, use -1 to indicate to read until EOF</param>
        /// <returns></returns>
        public static List<byte> GetFileAsByteList(string fileNameAndPath, int offset = 0, int length = -1)
        {
            string fileThatExists = GetFirstFileAndPathCaseInsensitive(fileNameAndPath);

            byte[] fileContents = File.ReadAllBytes(fileThatExists);

            if (length == -1) return fileContents.ToList();

            List<byte> specificContents = new(length);

            for (int i = offset; i < offset + length; i++)
            {
                specificContents.Add(fileContents[i]);
            }

            return specificContents;
        }

        public static string GetFirstFileAndPathCaseInsensitive(string fileNameAndPath)
        {
            if (string.IsNullOrEmpty(fileNameAndPath))
                throw new Ultima5ReduxException($"{fileNameAndPath} (file) does not exist");
            string dirName = Path.GetDirectoryName(fileNameAndPath);
            if (string.IsNullOrEmpty(dirName))
                throw new Ultima5ReduxException($"{dirName} (directory) does not exist!");

            foreach (string checkFileName in Directory.GetFiles(dirName))
            {
                if (string.Compare(Path.GetFileName(checkFileName), Path.GetFileName(fileNameAndPath),
                        StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return checkFileName;
                }
            }

            throw new FileNotFoundException($"Can't find {fileNameAndPath}");
        }

        /// <summary>
        ///     Nasty catch all function that cleans up typical mixed case strings
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GetFriendlyString(string str)
        {
            string friendlyStr = str.Replace("_", " ");
            if (!friendlyStr.Contains(" ")) friendlyStr = AddSpacesBeforeCaps(friendlyStr);

            friendlyStr = EnTextInfo.ToTitleCase(friendlyStr.ToLower());
            return friendlyStr;
        }

        public static int GetNumberFromAndTo(int nMin, int nMax)
        {
            if (nMin == nMax) return nMin;
            Debug.Assert(nMin < nMax);
            int nDiff = nMax - nMin;
            return Ran.Next() % nDiff + nMin;
        }

        public static bool[][] Init2DBoolArray(int numberOfRows, int numberOfCols, bool bDefault = false) =>
            Init2DArray(numberOfRows, numberOfCols, bDefault);

        public static byte[][] Init2DByteArray(int numberOfRows, int numberOfCols) =>
            Init2DArray<byte>(numberOfRows, numberOfCols);

        public static List<List<T>> Init2DList<T>(int numberOfRows, int numberOfCols)
        {
            List<List<T>> rowList = new(numberOfRows);

            for (int i = 0; i < numberOfRows; i++)
            {
                rowList.Add(new List<T>(numberOfCols));
            }

            return rowList;
        }

        public static T[][] ListTo2DArray<T>(List<T> theList, short splitEveryN, int offset, int length)
        {
            int listCount = theList.Count;

            _ = Math.DivRem(listCount, splitEveryN, out int remainder);

            if (remainder != 0)
                throw new ArgumentOutOfRangeException($"The Remainder: {remainder} should be zero when loading a map");

            // if a problem pops up for the maps in the future, then it's because of this call... am I creating the array correctly???
            T[][] theArray = Init2DArray<T>(length / splitEveryN, splitEveryN);

            for (int listPos = offset, arrayPos = 0; listPos < offset + length; listPos++, arrayPos++)
            {
                theArray[arrayPos / splitEveryN][arrayPos % splitEveryN] = theList[listPos];
            }

            return theArray;
        }

        public static int LittleEndianConversion(byte a, byte b) => a | (b << 8);

        /// <summary>
        ///     Dirty little method to create a memory expensive weighted list for simple
        ///     weighted choices
        /// </summary>
        /// <param name="weightedDict"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> MakeWeightedList<T>(Dictionary<T, int> weightedDict)
        {
            List<T> thingList = new();
            foreach (KeyValuePair<T, int> kvp in weightedDict)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    thingList.Add(kvp.Key);
                }
            }

            return thingList;
        }

        /// <summary>
        ///     Using the random number generator, provides 1 in howMany odds of returning true
        /// </summary>
        /// <param name="howMany">1 in howMany odds of returning true</param>
        /// <returns>true if odds are beat</returns>
        public static bool OneInXOdds(int howMany)
        {
            // if ran%howMany is zero then we beat the odds
            int nextRan = Ran.Next();
            return nextRan % howMany == 0;
        }

        public static bool RandomOdds(float fLikelihoodOfTrue) => Ran.NextDouble() <= fLikelihoodOfTrue;

        /// <summary>
        /// </summary>
        /// <remarks>Hacked from FileStream to List_byte https://www.developerfusion.com/article/84519/mastering-structs-in-c </remarks>
        /// <param name="byteArray"></param>
        /// <param name="fileOffset"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static object ReadStruct(List<byte> byteArray, int fileOffset, Type t)
        {
            byte[] buffer = new byte[Marshal.SizeOf(t)];
            byteArray.CopyTo(fileOffset, buffer, 0, Marshal.SizeOf(t));

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            object temp = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), t);
            handle.Free();
            return temp;
        }

        public static void Set2DArrayAllToValue<T>(IEnumerable<T[]> twoDArray, T value)
        {
            foreach (T[] t in twoDArray)
            {
                for (int j = 0; j < t.Length; j++)
                {
                    t[j] = value;
                }
            }
        }

        public static void SwapInts(ref int a, ref int b)
        {
            int t = a;
            b = a;
            a = t;
        }

        public static T[][] TransposeArray<T>(T[][] ts)
        {
            // makes some assumptions, like each row has an equal number of elements
            T[][] transArray = Init2DArray<T>(ts[0].Length, ts.Length);

            for (int i = 0; i < ts[0].Length; i++)
            {
                for (int j = 0; j < ts.Length; j++)
                {
                    transArray[i][j] = ts[j][i];
                }
            }

            return transArray;
        }
    }
}