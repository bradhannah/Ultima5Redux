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

            byte[][] byteArray = Init2DByteArray(length / splitEveryN, length / splitEveryN);

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

        static public string BytesToStringNullTerm(List<byte> biteArray, int offset)
        {
            byte curCharByte;
            string str = "";
            int curOffset = offset;
            // loop until a zero byte is found indicating end of string
            while ((curCharByte = biteArray[curOffset]) != 0x00)
            {
                // cast to (char) to ensure the string understands it not a number
                str += (char)(curCharByte);
                curOffset++;
            }
            return str;
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

    }
}
