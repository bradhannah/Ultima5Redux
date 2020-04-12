using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace Ultima5Redux
{
    public class Utils
    {
        /// <summary>
        /// Simple 2D byte array initlialization to zeros
        /// </summary>
        /// <param name="numberOfRows"></param>
        /// <param name="numberOfCols"></param>
        /// <returns></returns>
        //static public byte[][] Init2DByteArray(int numberOfRows, int numberOfCols)
        //{
        //    byte[][] byteArray = new byte[numberOfRows][];
        //    for (int i = 0; i < numberOfRows; i++) { byteArray[i] = new byte[numberOfCols]; }

        //    return byteArray;
        //}

        public static byte[][] Init2DByteArray(int numberOfRows, int numberOfCols)
        {
            return Init2DArray<byte>(numberOfRows, numberOfCols);
        }

        public static List<List<T>> Init2DList<T>(int numberOfRows, int numberOfCols)
        {
            List<List<T>> rowList = new List<List<T>>(numberOfRows);

            for (int i = 0; i < numberOfRows; i++)
            {
                rowList.Add(new List<T>(numberOfCols));
                //for (int j = 0; j < numberOfCols; j++)
                //{

                //}
            }
            return rowList;

        }

        public static T[][] Init2DArray<T>(int numberOfRows, int numberOfCols)
        {
            T[][] theArray = new T[numberOfRows][];
            for (int i = 0; i < numberOfRows; i++)
            {
                theArray[i] = new T[numberOfCols];
            }
            return theArray;
        }

        public static T[][] Init2DArray<T>(int numberOfRows, int numberOfCols, T defaultValue)
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

        public static bool[][] Init2DBoolArray(int numberOfRows, int numberOfCols)
        {
            return Init2DArray<bool>(numberOfRows, numberOfCols);
        }

        //static public bool[][] Init2DBoolArray(int numberOfRows, int numberOfCols)
        //{
        //    bool[][] byteArray = new bool[numberOfRows][];
        //    for (int i = 0; i < numberOfRows; i++) { byteArray[i] = new bool[numberOfCols]; }

        //    return byteArray;
        //}

        public static T[][] TransposeArray<T>(T[][] ts)
        {
            // makes some assumptions, like each row has an equal number of elements
            T[][] transArray = Utils.Init2DArray<T>(ts[0].Length, ts.Length);

            for (int i = 0; i < ts[0].Length; i++)
                for (int j = 0; j < ts.Length; j++)
                    transArray[i][j] = ts[j][i];

            return transArray;            
        }
        public static T[][] ListTo2DArray<T>(List<T> theList, short splitEveryN, int offset, int length)
        {
            int listCount = theList.Count;

            // TODO: add safety code to make sure there is no remainer when dividing listCount/splitEveryN
            _ = Math.DivRem(listCount, splitEveryN, out int remainder);

            if (remainder != 0) { throw new IndexOutOfRangeException("The Remainder: " + remainder + " should be zero when loading a map"); }

            // if a problem pops up for the maps in the future, then it's because of this call... am I creating the array correctly???
            T[][] theArray = Init2DArray<T>(length / splitEveryN, splitEveryN);

            for (int listPos = offset, arrayPos = 0; listPos < offset + length; listPos++, arrayPos++)
            {
                theArray[arrayPos / splitEveryN][arrayPos % splitEveryN] = theList[listPos];
            }
            return theArray;
        }

        /// <summary>
        /// Divides a list of bytes into a two dimension byte array
        /// Ideal for searialized byte arrays from map files, into a more readable x,y
        /// </summary>
        /// <param name="byteList"></param>
        /// <param name="splitEveryN">split into a new row every N bytes</param>
        /// <param name="offset">byte position to start in list</param>
        /// <param name="length">number of bytes to copy from list to 2d array</param>
        /// <returns></returns>
        public static byte[][] ByteListTo2DArray(List<byte> byteList, short splitEveryN, int offset, int length)
        {
            int listCount = byteList.Count;

            // TODO: add safety code to make sure there is no remainer when dividing listCount/splitEveryN
            int remainder = 0;
            _ = Math.DivRem(listCount, splitEveryN, out remainder);

            if (remainder != 0) { throw new IndexOutOfRangeException("The Remainder: " + remainder + " should be zero when loading a map"); }

//            byte[][] byteArray = Init2DByteArray(length / splitEveryN, length / splitEveryN);
            byte[][] byteArray = Init2DArray<byte>(length / splitEveryN, length / splitEveryN);

            for (int listPos = offset, arrayPos = 0; listPos < offset + length; listPos++, arrayPos++)
            {
                byteArray[arrayPos / splitEveryN][arrayPos % splitEveryN] = byteList[listPos];
            }
            return byteArray;
        }

        /// <summary>
        /// Reads a list of bytes from a file.
        /// 
        /// </summary>
        /// <remarks>Could be optimized with a cache function that keeps the particular file in memory</remarks>
        /// <param name="filename">Filename to open (read only)</param>
        /// <param name="offset">byte offset to start at</param>
        /// <param name="length">number of bytes to read, use -1 to indicate to read until EOF</param>
        /// <returns></returns>
        public static List<byte> GetFileAsByteList(string filename, int offset, int length)
        {
            FileStream readFile = File.OpenRead(filename);

            byte[] fileContents = File.ReadAllBytes(filename);
            if (length == -1) return fileContents.ToList();

            List<byte> specificContents=new List<byte>(length);

            for (int i = offset ; i < offset + length; i++)
            {
                specificContents.Add(fileContents[i]);
            }
            return specificContents;
        }


        /// <summary>
        /// Gets a binary file as a stream of bytes into a list<byte>
        /// </summary>
        /// <param name="filename">filename of the binary file</param>
        /// <returns>a list of bytes</returns>
        public static List<byte> GetFileAsByteList(string filename)
        {
            return (GetFileAsByteList(filename, 0, -1));
    
        }

        /// <summary>
        /// Converts a byte[] to a readable string, assumes it ends with a NULL (0x00) byte
        /// </summary>
        /// <param name="biteArray">the array to read from</param>
        /// <param name="offset">the offset to start at</param>
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
                str += (char)(curCharByte);
                curOffset++;
                count++;
            }
            return str;
        }

        public static string BytesToStringFixedWidth(List<byte> byteArray, int offset, int length)
        {
            string str = string.Empty;
            for (int i=0; i < length; i++)
            {
                str += (char)byteArray[i];
            }
            return str;
        }

        public static List<UInt16> CreateOffsetList(byte[] byteArray, int offset, int length)
        {
            List<UInt16> offsetArray = new List<UInt16>();

            // double TOTAL_LOOKS because we are using 16 bit integers, using two bytes at a time
            for (int i = 0; i < length; i += 2)
            {
                offsetArray.Add((UInt16)(byteArray[i] | (((UInt16)byteArray[i + 1]) << 8)));
            }

            return offsetArray;
        }

        /// <summary>
        /// Creates an offset list when uint16 offsets are described in a data file
        /// </summary>
        /// <remarks>this is only midly useful due to it not passing back the byte array</remarks>
        /// <param name="filename">data filename and path</param>
        /// <param name="offset">initial offset (typically 0)</param>
        /// <param name="length">number of bytes to read</param>
        /// <returns>a list of offsets</returns>
        public static List<int> CreateOffsetList (string filename, int offset, int length)
        {
            List<byte> byteArray = Utils.GetFileAsByteList(filename);

            List<int> offsetArray = new List<int>();
            
            // double TOTAL_LOOKS because we are using 16 bit integers, using two bytes at a time
            for (int i = 0; i < length; i += 2)
            {
                offsetArray.Add((int)(byteArray[i] | (((uint)byteArray[i + 1]) << 8)));
            }

            return offsetArray;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Borrowed from: https://www.developerfusion.com/article/84519/mastering-structs-in-c </remarks>
        /// <param name="fs"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static object ReadStruct(FileStream fs, Type t)
        {
            byte[] buffer =
                new byte[Marshal.SizeOf(t)];
            fs.Read(buffer, 0,
                Marshal.SizeOf(t));
            GCHandle handle =
                GCHandle.Alloc(buffer,
                GCHandleType.Pinned);
            Object temp =
                Marshal.PtrToStructure(
                handle.AddrOfPinnedObject(),
                t);
            handle.Free();
            return temp;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Hacked from FileStream to List_byte https://www.developerfusion.com/article/84519/mastering-structs-in-c </remarks>
        /// <param name="byteArray"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static object ReadStruct(List<byte> byteArray, int fileOffset, Type t)
        {
            byte[] buffer = new byte[Marshal.SizeOf(t)];
            byteArray.CopyTo(fileOffset, buffer, 0, Marshal.SizeOf(t));

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Object temp = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), t);
            handle.Free();
            return temp;
        }

        public static int LittleEndianConversion (byte a, byte b)
        {
             return ((int)(a| (((int)b) << 8)));
        }
    }
}
