using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace Ultima5Redux
{
    class Utils
    {
        /// <summary>
        /// Simple 2D byte array initlialization to zeros
        /// </summary>
        /// <param name="numberOfRows"></param>
        /// <param name="numberOfCols"></param>
        /// <returns></returns>
        static public byte[][] Init2DByteArray(int numberOfRows, int numberOfCols)
        {
            byte[][] byteArray = new byte[numberOfRows][];
            for (int i = 0; i < numberOfRows; i++) { byteArray[i] = new byte[numberOfCols]; }

            return byteArray;
        }

        static public T[][] Init2DArray<T>(int numberOfRows, int numberOfCols)
        {
            T[][] theArray = new T[numberOfRows][];
            for (int i = 0; i < numberOfRows; i++) { theArray[i] = new T[numberOfCols]; }

            return theArray;

        }

        static public bool[][] Init2DBoolArray(int numberOfRows, int numberOfCols)
        {
            bool[][] byteArray = new bool[numberOfRows][];
            for (int i = 0; i < numberOfRows; i++) { byteArray[i] = new bool[numberOfCols]; }

            return byteArray;
        }


        static public T[][] ListTo2DArray<T>(List<T> theList, short splitEveryN, int offset, int length)
        {
            int listCount = theList.Count;

            // TODO: add safety code to make sure there is no remainer when dividing listCount/splitEveryN
            int remainder = 0;
            Math.DivRem(listCount, splitEveryN, out remainder);

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
        static public byte[][] ByteListTo2DArray(List<byte> byteList, short splitEveryN, int offset, int length)
        {
            int listCount = byteList.Count;

            // TODO: add safety code to make sure there is no remainer when dividing listCount/splitEveryN
            int remainder = 0;
            Math.DivRem(listCount, splitEveryN, out remainder);

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
        static public List<byte> GetFileAsByteList(string filename, int offset, int length)
        {
            FileStream readFile = File.OpenRead(filename);

            // find the offset in the file
            readFile.Seek(offset, SeekOrigin.Begin);

            BinaryReader mapFileReader = new BinaryReader(readFile);

            List<byte> theChunksSerial = new List<byte>();

            byte tile = (byte)0x00;

            int lengthCounter = 0;
            // read the entire file and save in serial bytes
            try
            {
                while (!(tile = mapFileReader.ReadByte()).Equals(-1))
                {
                    // if you set a length, and it has been reached, then you're done and pass it back
                    if (length != -1) { if (lengthCounter == length) { return theChunksSerial; } }
                    theChunksSerial.Add(tile);
                    lengthCounter++;
                }
            }
            catch (EndOfStreamException)
            {
                // all good, I can't get the damn peek to work properly
            }
            mapFileReader.Close();

            return theChunksSerial;
        }


        /// <summary>
        /// Gets a binary file as a stream of bytes into a list<byte>
        /// </summary>
        /// <param name="filename">filename of the binary file</param>
        /// <returns>a list of bytes</returns>
        static public List<byte> GetFileAsByteList(string filename)
        {
            return (GetFileAsByteList(filename, 0, -1));
    
        }

        /// <summary>
        /// Converts a byte[] to a readable string, assumes it ends with a NULL (0x00) byte
        /// </summary>
        /// <param name="biteArray">the array to read from</param>
        /// <param name="offset">the offset to start at</param>
        /// <returns></returns>
        static public string BytesToStringNullTerm(List<byte> byteArray, int offset, int length)
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

        static public string BytesToStringFixedWidth(List<byte> byteArray, int offset, int length)
        {
            string str = string.Empty;
            for (int i=0; i < length; i++)
            {
                str += (char)byteArray[i];
            }
            return str;
        }

        static public List<UInt16> CreateOffsetList(byte[] byteArray, int offset, int length)
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
        static public List<int> CreateOffsetList (string filename, int offset, int length)
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
        static public object ReadStruct(FileStream fs, Type t)
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
        static public object ReadStruct(List<byte> byteArray, int fileOffset, Type t)
        {
            byte[] buffer = new byte[Marshal.SizeOf(t)];
            byteArray.CopyTo(fileOffset, buffer, 0, Marshal.SizeOf(t));

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Object temp = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), t);
            handle.Free();
            return temp;
        }

        static public int LittleEndianConversion (byte a, byte b)
        {
             return ((int)(a| (((int)b) << 8)));
        }
    }
}
