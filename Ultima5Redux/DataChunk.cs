using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    /// <summary>
    /// A collection of Datachunks
    /// </summary>
    public class DataChunks<T>
    {
        private Dictionary<T, DataChunk> chunkMap = new Dictionary<T, DataChunk>();
        private T unusedValue;
        private List<DataChunk> dataChunks;
        private List<byte> fileByteList;

        public List<byte> FileByteList { get { return fileByteList; } }

        public DataChunks(string chunkFile, T unusedValue)
        {
            dataChunks = new List<DataChunk>();
            this.unusedValue = unusedValue;
            fileByteList = Utils.GetFileAsByteList(chunkFile);
        }


        /// <summary>
        /// Add a chunk to the list
        /// </summary>
        /// <param name="chunk">datachunk</param>
        public void AddDataChunk (DataChunk chunk) 
        {
            //dataChunks.Add(chunk);
            AddDataChunk(chunk, unusedValue);
        }

        /// <summary>
        /// Add a DataChunk with a particular chunk name for easy retrieval
        /// </summary>
        /// <param name="chunk">Chunk to add</param>
        /// <param name="dataChunkName">Name/Description of the chunk for retrieval</param>
        public void AddDataChunk (DataChunk chunk, T dataChunkName)
        {
            // all data chunks get added to the chunk list
            //AddDataChunk(chunk);
            dataChunks.Add(chunk);

            // if the datachunk is not classified as unused then add it to the chunk map for quick reference
            if (!dataChunkName.Equals(unusedValue))
            {
                chunkMap.Add(dataChunkName, chunk);
            }
        }

        public void AddDataChunk(DataChunk.DataFormatType dataFormat, string description, int offset, int dataLength, byte addToValue, T dataChunkName)
        {
            // create the data chunk 
            DataChunk chunk = new DataChunk(dataFormat, description, FileByteList, offset, dataLength, addToValue);

            // all data chunks get added to the chunk list
            AddDataChunk(chunk);

            // if the datachunk is not classified as unused then add it to the chunk map for quick reference
            if (!dataChunkName.Equals(unusedValue))
            {
                chunkMap.Add(dataChunkName, chunk);
            }
        }

        public void AddDataChunk(DataChunk.DataFormatType dataFormat, string description, int offset, int dataLength, byte addToValue)
        {
            AddDataChunk(dataFormat, description, offset, dataLength, addToValue, unusedValue);
        }

        public void AddDataChunk(DataChunk.DataFormatType dataFormat, string description, int offset, int dataLength)
        {
            AddDataChunk(dataFormat, description, offset, dataLength, 0x00, unusedValue);
        }

        public DataChunk GetDataChunk(T dataChunkName)
        {
            return chunkMap[dataChunkName];
        }

            /// <summary>
            /// Get a data chunk
            /// </summary>
            /// <param name="index">index into chunk list</param>
            /// <returns></returns>
        public DataChunk GetDataChunk(int index)
        {
            return dataChunks[index];
        }

        /// <summary>
        /// Debug function to print it all (slow)
        /// </summary>
        public void PrintEverything()
        {
            foreach (DataChunk chunk in dataChunks)
            {
                System.Console.WriteLine("**** " + chunk.Description);
                chunk.PrintChunk();
            }
        }
    }

    /// <summary>
    /// A data chunk represents a stream of bytes that can be represented in variety of ways depending on the data type
    /// </summary>
    public class DataChunk
    {
        private const int BITS_PER_BYTE = 8;
        private const bool isDebug = false;

        /// <summary>
        /// The encoding of the data
        /// </summary>
        public enum DataFormatType { Unknown, FixedString, SimpleString, StringList, UINT16List, ByteList, Bitmap };

        /// <summary>
        /// Construct a data chunk
        /// </summary>
        /// <param name="dataFormat">what kind of encoding represents the data?</param>
        /// <param name="description">a brief description of what the data represents</param>
        /// <param name="rawData">the raw byte data</param>
        /// <param name="offset">the offset to start in the rawData that represents the chunk</param>
        /// <param name="dataLength">the length of the data from the offset that represents the chunk</param>
        /// <param name="addToValue">the value to add to each of each byte value (used occasionaly for bytes that represent offsets)</param>
        public DataChunk(DataFormatType dataFormat, string description, List<byte> rawData, int offset, int dataLength, byte addToValue)
        {
            DataFormat = dataFormat;
            Description = description;
            DataLength = dataLength;
            FileOffset = offset;
            RawData = new byte[dataLength];
            rawData.CopyTo(offset, RawData, 0, dataLength);
            ValueModifier = addToValue;
        }

        /// <summary>
        /// Construct a data chunk
        /// Note: Assumes nothing to add to the byte values
        /// </summary>
        /// <param name="dataFormat">what kind of encoding represents the data?</param>
        /// <param name="description">a brief description of what the data represents</param>
        /// <param name="rawData">the raw byte data</param>
        /// <param name="offset">the offset to start in the rawData that represents the chunk</param>
        /// <param name="dataLength">the length of the data from the offset that represents the chunk</param>
        public DataChunk(DataFormatType dataFormat, string description, List<byte> rawData, int offset, int dataLength) : 
            this(dataFormat, description, rawData, offset, dataLength, 0)
        {            
        }

        /// <summary>
        /// Debugging function that prints out the chunk
        /// </summary>
        public void PrintChunk()
        {
            switch (DataFormat)
            {
                case DataFormatType.Bitmap:
                    System.Console.WriteLine("BITMAP");
                    break;
                case DataFormatType.FixedString:
                case DataFormatType.SimpleString:
                    System.Console.WriteLine(GetChunkAsString());
                    break;
                case DataFormatType.StringList:
                    GetChunkAsStringList().PrintSomeStrings();
                    break;
                case DataFormatType.ByteList:
                    foreach (byte b in RawData)
                    {
                        System.Console.WriteLine(b.ToString("X"));
                    }
                    break;
                case DataFormatType.UINT16List:
                    foreach (UInt16 word in GetChunkAsUINT16())
                    {
                        System.Console.WriteLine("Word: " + word.ToString("X"));
                    }
                    break;
                case DataFormatType.Unknown:
                    System.Console.WriteLine("Unknown");
                    break;
            }
        }

        public List<bool> GetAsBitmapBoolList()
        {
            // this is essetially the number 1, but we use this to show that we are ANDing on the final bit
            byte shiftBit = 0b000_0001;

            List<bool> boolList = new List<bool>(this.DataLength * BITS_PER_BYTE);

            // loop through all bytes, then each of their bits, creating a list of Booleans
            for (int nByte = 0; nByte < this.DataLength; nByte++)
            {
                byte curByte = (byte)(RawData[nByte] + ValueModifier);

                for (int nBit = 0; nBit < BITS_PER_BYTE; nBit++)
                {
                    bool curBit = (((curByte >> nBit)) & shiftBit) == shiftBit;
                    boolList.Add(curBit);
                    if (isDebug) Console.WriteLine("Byte #" + nByte.ToString() + "  Bit #" + nBit.ToString() + "=" + curBit.ToString());
                }
            }

            return boolList;
        }

        /// <summary>
        /// Return the data as a byte list 
        /// Note: this will add/subtract using the addToValue
        /// </summary>
        /// <returns>a list of bytes</returns>
        public List<byte> GetAsByteList()
        {
            List<byte> data = new List<byte>(DataLength);

            for (int i = 0; i < DataLength; i++)
            {
                byte rawData = RawData[i];
                data[i] =  (byte)(rawData + ValueModifier);
            }

            return data;
        }

        /// <summary>
        /// Returns the data as a UINT16 list. It uses little endian convention.
        /// Note: this will add/subtract using the addToValue
        /// </summary>
        /// <returns>list of UINT16s</returns>
        public List<UInt16> GetChunkAsUINT16 ()
        {
            List<UInt16> data = Utils.CreateOffsetList(RawData, FileOffset, DataLength);
            for (int i = 0; i < data.Count; i++)
            {
                data[i] = (UInt16)(data[i] + ValueModifier);
            }
            return data;
        }

        /// <summary>
        /// Returns a string representation of the datachunk
        /// </summary>
        /// <returns>a single string</returns>
        public string GetChunkAsString()
        {
            if (DataFormat == DataFormatType.FixedString)
            {
                return (Utils.BytesToStringFixedWidth(RawData.ToList(), 0, DataLength));
            }
            else if (DataFormat == DataFormatType.SimpleString)
            {
                return (Utils.BytesToStringNullTerm(RawData.ToList(), 0, DataLength));
            }
            throw new Exception("String datatype doesn't match predefined list.");
        }

        /// <summary>
        /// Returns a collection of strings
        /// </summary>
        /// <returns></returns>
        public SomeStrings GetChunkAsStringList()
        {
            return (new SomeStrings(RawData.ToList(), 0, DataLength));
        }

        /// <summary>
        /// A brief description of the data chunk
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// The raw data of the data chunk
        /// </summary>
        public byte[] RawData { get; }
        /// <summary>
        /// The length of the RawData 
        /// This begins at zero in the array
        /// </summary>
        public int DataLength { get; }
        /// <summary>
        /// The offset used when getting the RawData
        /// </summary>
        public int FileOffset { get; }
        /// <summary>
        /// The adjustment value for bytes and UINT16
        /// </summary>
        public byte ValueModifier { get;  }
        /// <summary>
        /// The expected data format of the chunk
        /// </summary>
        public DataFormatType DataFormat { get; }

    }
}
