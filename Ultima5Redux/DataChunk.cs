using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ultima5Redux
{
    /// <summary>
    /// A collection of Datachunks
    /// </summary>
    public class DataChunks<T>
    {
        /// <summary>
        /// A Dictionary that maps the DataChunkName to the specific location
        /// </summary>
        private Dictionary<T, DataChunk> chunkMap = new Dictionary<T, DataChunk>();

        /// <summary>
        /// The default name to give unlabelled data chunks
        /// </summary>
        private T unusedValue;

        /// <summary>
        /// Full list of data chunks
        /// </summary>
        private List<DataChunk> dataChunks;

        /// <summary>
        /// Byte list of file contets
        /// </summary>
        public List<byte> FileByteList { get { return fileByteList; } }
        private List<byte> fileByteList;

        /// <summary>
        /// Construct a collection of DataChunks 
        /// </summary>
        /// <param name="chunkFile">The file that all chunks will be read from</param>
        /// <param name="unusedValue">The value to automatically assign all un-named data objects</param>
        public DataChunks(string chunkFile, T unusedValue)
        {
            dataChunks = new List<DataChunk>();
            this.unusedValue = unusedValue;
            fileByteList = Utils.GetFileAsByteList(chunkFile);
        }

        #region Public Methods
        /// <summary>
        /// Add a chunk to the list
        /// </summary>
        /// <param name="chunk">datachunk</param>
        public void AddDataChunk(DataChunk chunk)
        {
            //dataChunks.Add(chunk);
            AddDataChunk(chunk, unusedValue);
        }

        /// <summary>
        /// Add a DataChunk with a particular chunk name for easy retrieval
        /// </summary>
        /// <param name="chunk">Chunk to add</param>
        /// <param name="dataChunkName">Name/Description of the chunk for retrieval</param>
        public void AddDataChunk(DataChunk chunk, T dataChunkName)
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

        /// <summary>
        /// Add a generic data chunk, providing a name for easier access
        /// Additional functonality for adding/subtracting value 
        /// </summary>
        /// <param name="dataFormat">the expected format of the data</param>
        /// <param name="description">a brief written description of what the data is</param>
        /// <param name="offset">the offset to begin processing at</param>
        /// <param name="dataLength">the length of the data in bytes</param>
        /// <param name="addToValue">the byte value to add (or subtract) from each byte read</param>
        /// <param name="dataChunkName">name of the data chunk for easy access</param>
        public DataChunk AddDataChunk(DataChunk.DataFormatType dataFormat, string description, int offset, int dataLength, byte addToValue, T dataChunkName)
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
            return chunk;
        }

        /// <summary>
        /// Add a new generic data chunk (un-named)
        /// Additional functonality for adding/subtracting value 
        /// </summary>
        /// <param name="dataFormat">the expected format of the data</param>
        /// <param name="description">a brief written description of what the data is</param>
        /// <param name="offset">the offset to begin processing at</param>
        /// <param name="dataLength">the length of the data in bytes</param>
        /// <param name="addToValue">the byte value to add (or subtract) from each byte read</param>
        public DataChunk AddDataChunk(DataChunk.DataFormatType dataFormat, string description, int offset, int dataLength, byte addToValue)
        {
            return AddDataChunk(dataFormat, description, offset, dataLength, addToValue, unusedValue);
        }

        /// <summary>
        /// Add a new generic data chunk (un-named)
        /// </summary>
        /// <param name="dataFormat">the expected format of the data</param>
        /// <param name="description">a brief written description of what the data is</param>
        /// <param name="offset">the offset to begin processing at</param>
        /// <param name="dataLength">the length of the data in bytes</param>
        public DataChunk AddDataChunk(DataChunk.DataFormatType dataFormat, string description, int offset, int dataLength)
        {
            return AddDataChunk(dataFormat, description, offset, dataLength, 0x00, unusedValue);
        }

        /// <summary>
        /// Retrieve a DataChunk based on chunk name that has been assigned to it
        /// This is a handy way to label your data chunks for easier access
        /// </summary>
        /// <param name="dataChunkName">the enumeration that you will use to describe the data chunk</param>
        /// <returns>the datachunk</returns>
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
        #endregion
    }

    /// <summary>
    /// A data chunk represents a stream of bytes that can be represented in variety of ways depending on the data type
    /// </summary>
    public class DataChunk
    {
        #region Private Variables
        /// <summary>
        /// How many BITS per byte
        /// </summary>
        private const int BITS_PER_BYTE = 8;

        /// <summary>
        /// Are we running in debug mode?
        /// </summary>
        private const bool isDebug = false;

        /// <summary>
        /// A link to the actual Full RawData 
        /// used in StringListFromIndexes - but private so no one accessess it directly
        /// </summary>
        private List<byte> fullRawData;
        #endregion

        /// <summary>
        /// The encoding of the data
        /// </summary>
        public enum DataFormatType { Unknown, FixedString, SimpleString, StringList, UINT16List, UINT16, ByteList, Bitmap, Byte, StringListFromIndexes };

        #region Constructors
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
            if (dataFormat == DataFormatType.StringListFromIndexes || dataFormat == DataFormatType.UINT16List)
            {
                fullRawData = rawData;
            }
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
        /// Statically create a DataChunk
        /// Handy for quick and dirty variable extraction
        /// </summary>
        /// <param name="dataFormat">what kind of encoding represents the data?</param>
        /// <param name="description">a brief description of what the data represents</param>
        /// <param name="rawData">the raw byte data</param>
        /// <param name="offset">the offset to start in the rawData that represents the chunk</param>
        /// <param name="dataLength">the length of the data from the offset that represents the chunk</param>
        /// <returns>a new DataChunk object</returns>
        static public DataChunk CreateDataChunk(DataFormatType dataFormat, string description, List<byte> rawData, int offset, int dataLength)
        {
            DataChunk dataChunk = new DataChunk(dataFormat, description, rawData, offset, dataLength);
            return dataChunk;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Debugging function that prints out the chunk
        /// </summary>
        public void PrintChunk()
        {
            switch (DataFormat)
            {
                case DataFormatType.StringListFromIndexes:
                   
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
                case DataFormatType.Byte:
                    {
                        System.Console.WriteLine(RawData[0].ToString("X"));
                    }
                    break;
                case DataFormatType.ByteList:
                    foreach (byte b in RawData)
                    {
                        System.Console.WriteLine(b.ToString("X"));
                    }
                    break;
                case DataFormatType.UINT16:
                    {
                        UInt16 word = GetChunkAsUINT16List()[0];
                        System.Console.WriteLine("Word: " + word.ToString("X"));
                        break;
                    }
                case DataFormatType.UINT16List:
                    foreach (UInt16 word in GetChunkAsUINT16List())
                    {
                        System.Console.WriteLine("Word: " + word.ToString("X"));
                    }
                    break;
                case DataFormatType.Unknown:
                    System.Console.WriteLine("Unknown");
                    break;
            }
        }

        public List<string> GetAsStringListFromIndexes()
        {
            return DataChunk.GetAsStringListFromIndexes(GetChunkAsUINT16List(), fullRawData);
        }

        static private List<string> GetAsStringListFromIndexes(List<ushort> indexList, List<byte> rawByteList)
        {
            List<string> strList = new List<string>(indexList.Count);
            const int MAX_STR_LENGTH = 20;

            foreach (ushort index in indexList)
            {
                // we grab the strings from the fullRawData because it is the entire file
                strList.Add(DataChunk.CreateDataChunk(DataFormatType.SimpleString, String.Empty, rawByteList, index, MAX_STR_LENGTH).GetChunkAsString());
            }

            return strList;
        }

        /// <summary>
        /// Returns the data in the form of each bit represented as a boolean value
        /// </summary>
        /// <returns>A list of the extracted boolean values</returns>
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
                    // if (isDebug) Debug.WriteLine("Byte #" + nByte.ToString() + "  Bit #" + nBit.ToString() + "=" + curBit.ToString());
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
                //data[i] =  (byte)(rawData + ValueModifier);
                data.Add((byte)(rawData + ValueModifier));
            }

            return data;
        }

        public void SetChunkAsByte(byte data)
        {
            RawData[0] = data;
        }
        
        public void SetChunkAsUINT16(UInt16 data)
        {
            RawData[0] = (byte)(data & 0xFF);
            RawData[1] = (byte)((data >> 8) & 0xFF);
        }

        public byte GetChunkAsByte()
        {
            return RawData[0];
        }

        /// <summary>
        /// Returns the data as a single UINT16. It uses little endian convention.
        /// </summary>
        /// <returns>UINT16s</returns>
        public UInt16 GetChunkAsUINT16()
        {
            UInt16 data = ((UInt16)(RawData[0] | (((UInt16)RawData[1]) << 8)));
            return data;
        }

        /// <summary>
        /// Returns the data as a UINT16 list. It uses little endian convention.
        /// Note: this will add/subtract using the addToValue
        /// </summary>
        /// <returns>list of UINT16s</returns>
        public List<UInt16> GetChunkAsUINT16List()
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

        #endregion

        #region Public Properties
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
        public byte ValueModifier { get; }
        /// <summary>
        /// The expected data format of the chunk
        /// </summary>
        public DataFormatType DataFormat { get; }
        #endregion
    }
}
