﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ultima5Redux.Data
{
    /// <summary>
    ///     A collection of Datachunks
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")] public class DataChunks<T>
    {
        /// <summary>
        ///     A Dictionary that maps the DataChunkName to the specific location
        /// </summary>
        private readonly Dictionary<T, DataChunk> _chunkMap = new Dictionary<T, DataChunk>();

        /// <summary>
        ///     Full list of data chunks
        /// </summary>
        private readonly List<DataChunk> _dataChunks;

        /// <summary>
        ///     The default name to give unlabelled data chunks
        /// </summary>
        private readonly T _unusedValue;

        /// <summary>
        ///     Construct a collection of DataChunks
        /// </summary>
        /// <param name="chunkFile">The file that all chunks will be read from</param>
        /// <param name="unusedValue">The value to automatically assign all un-named data objects</param>
        public DataChunks(string chunkFile, T unusedValue)
        {
            _dataChunks = new List<DataChunk>();
            _unusedValue = unusedValue;
            FileByteList = Utils.GetFileAsByteList(chunkFile);
        }

        /// <summary>
        ///     Byte list of file contents
        /// </summary>
        public List<byte> FileByteList { get; }

        /// <summary>
        ///     Add a chunk to the list
        /// </summary>
        /// <param name="chunk">datachunk</param>
        private void AddDataChunk(DataChunk chunk)
        {
            AddDataChunk(chunk, _unusedValue);
        }

        /// <summary>
        ///     Add a DataChunk with a particular chunk name for easy retrieval
        /// </summary>
        /// <param name="chunk">Chunk to add</param>
        /// <param name="dataChunkName">Name/Description of the chunk for retrieval</param>
        private void AddDataChunk(DataChunk chunk, T dataChunkName)
        {
            // all data chunks get added to the chunk list
            _dataChunks.Add(chunk);

            // if the datachunk is not classified as unused then add it to the chunk map for quick reference
            if (!dataChunkName.Equals(_unusedValue)) _chunkMap.Add(dataChunkName, chunk);
        }

        /// <summary>
        ///     Add a generic data chunk, providing a name for easier access
        ///     Additional functionality for adding/subtracting value
        /// </summary>
        /// <param name="dataFormat">the expected format of the data</param>
        /// <param name="description">a brief written description of what the data is</param>
        /// <param name="offset">the offset to begin processing at</param>
        /// <param name="dataLength">the length of the data in bytes</param>
        /// <param name="addToValue">the byte value to add (or subtract) from each byte read</param>
        /// <param name="dataChunkName">name of the data chunk for easy access</param>
        public DataChunk AddDataChunk(DataChunk.DataFormatType dataFormat, string description, int offset,
            int dataLength, byte addToValue, T dataChunkName)
        {
            // create the data chunk 
            DataChunk chunk = new DataChunk(dataFormat, description, FileByteList, offset, dataLength, addToValue);

            // all data chunks get added to the chunk list
            AddDataChunk(chunk);

            // if the datachunk is not classified as unused then add it to the chunk map for quick reference
            if (!dataChunkName.Equals(_unusedValue)) _chunkMap.Add(dataChunkName, chunk);
            return chunk;
        }

        /// <summary>
        ///     Add a new generic data chunk (un-named)
        ///     Additional functionality for adding/subtracting value
        /// </summary>
        /// <param name="dataFormat">the expected format of the data</param>
        /// <param name="description">a brief written description of what the data is</param>
        /// <param name="offset">the offset to begin processing at</param>
        /// <param name="dataLength">the length of the data in bytes</param>
        /// <param name="addToValue">the byte value to add (or subtract) from each byte read</param>
        public DataChunk AddDataChunk(DataChunk.DataFormatType dataFormat, string description, int offset,
            int dataLength, byte addToValue)
        {
            return AddDataChunk(dataFormat, description, offset, dataLength, addToValue, _unusedValue);
        }

        /// <summary>
        ///     Add a new generic data chunk (un-named)
        /// </summary>
        /// <param name="dataFormat">the expected format of the data</param>
        /// <param name="description">a brief written description of what the data is</param>
        /// <param name="offset">the offset to begin processing at</param>
        /// <param name="dataLength">the length of the data in bytes</param>
        public DataChunk AddDataChunk(DataChunk.DataFormatType dataFormat, string description, int offset,
            int dataLength)
        {
            return AddDataChunk(dataFormat, description, offset, dataLength, 0x00, _unusedValue);
        }

        /// <summary>
        ///     Retrieve a DataChunk based on chunk name that has been assigned to it
        ///     This is a handy way to label your data chunks for easier access
        /// </summary>
        /// <param name="dataChunkName">the enumeration that you will use to describe the data chunk</param>
        /// <returns>the datachunk</returns>
        public DataChunk GetDataChunk(T dataChunkName)
        {
            return _chunkMap[dataChunkName];
        }

        /// <summary>
        ///     Get a data chunk
        /// </summary>
        /// <param name="index">index into chunk list</param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public DataChunk GetDataChunk(int index) => _dataChunks[index];

        /// <summary>
        ///     Debug function to print it all (slow)
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public void PrintEverything()
        {
            foreach (DataChunk chunk in _dataChunks)
            {
                Console.WriteLine(@"**** " + chunk.Description);
                chunk.PrintChunk();
            }
        }
    }

    /// <summary>
    ///     A data chunk represents a stream of bytes that can be represented in variety of ways depending on the data type
    /// </summary>
    public class DataChunk
    {
        /// <summary>
        ///     The encoding of the data
        /// </summary>
        public enum DataFormatType
        {
            Unknown, FixedString, SimpleString, StringList, UINT16List, UINT16, ByteList, Bitmap, Byte,
            StringListFromIndexes
        }

        /// <summary>
        ///     How many BITS per byte
        /// </summary>
        private const int BITS_PER_BYTE = 8;

        /// <summary>
        ///     A link to the actual Full RawData
        ///     used in StringListFromIndexes - but private so no one accesses it directly
        /// </summary>
        private readonly List<byte> _fullRawData;

        /// <summary>
        ///     Construct a data chunk
        /// </summary>
        /// <param name="dataFormat">what kind of encoding represents the data?</param>
        /// <param name="description">a brief description of what the data represents</param>
        /// <param name="rawData">the raw byte data</param>
        /// <param name="offset">the offset to start in the rawData that represents the chunk</param>
        /// <param name="dataLength">the length of the data from the offset that represents the chunk</param>
        /// <param name="addToValue">
        ///     the value to add to each of each byte value (used occasionally for bytes that represent
        ///     offsets)
        /// </param>
        public DataChunk(DataFormatType dataFormat, string description, List<byte> rawData, int offset, int dataLength,
            byte addToValue = 0)
        {
            DataFormat = dataFormat;
            Description = description;
            DataLength = dataLength;
            FileOffset = offset;
            RawData = new byte[dataLength];
            rawData.CopyTo(offset, RawData, 0, dataLength);
            ValueModifier = addToValue;
            if (dataFormat == DataFormatType.StringListFromIndexes || dataFormat == DataFormatType.UINT16List)
                _fullRawData = rawData;
        }

        /// <summary>
        ///     A brief description of the data chunk
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     The raw data of the data chunk
        /// </summary>
        private byte[] RawData { get; }

        /// <summary>
        ///     The length of the RawData
        ///     This begins at zero in the array
        /// </summary>
        private int DataLength { get; }

        /// <summary>
        ///     The offset used when getting the RawData
        /// </summary>
        private int FileOffset { get; }

        /// <summary>
        ///     The adjustment value for bytes and UINT16
        /// </summary>
        private byte ValueModifier { get; }

        /// <summary>
        ///     The expected data format of the chunk
        /// </summary>
        private DataFormatType DataFormat { get; }

        /// <summary>
        ///     Statically create a DataChunk
        ///     Handy for quick and dirty variable extraction
        /// </summary>
        /// <param name="dataFormat">what kind of encoding represents the data?</param>
        /// <param name="description">a brief description of what the data represents</param>
        /// <param name="rawData">the raw byte data</param>
        /// <param name="offset">the offset to start in the rawData that represents the chunk</param>
        /// <param name="dataLength">the length of the data from the offset that represents the chunk</param>
        /// <returns>a new DataChunk object</returns>
        public static DataChunk CreateDataChunk(DataFormatType dataFormat, string description, List<byte> rawData,
            int offset, int dataLength)
        {
            DataChunk dataChunk = new DataChunk(dataFormat, description, rawData, offset, dataLength);
            return dataChunk;
        }

        /// <summary>
        ///     Debugging function that prints out the chunk
        /// </summary>
        public void PrintChunk()
        {
            switch (DataFormat)
            {
                case DataFormatType.StringListFromIndexes:

                case DataFormatType.Bitmap:
                    Console.WriteLine(@"BITMAP");
                    break;
                case DataFormatType.FixedString:
                case DataFormatType.SimpleString:
                    Console.WriteLine(GetChunkAsString());
                    break;
                case DataFormatType.StringList:
                    GetChunkAsStringList().PrintSomeStrings();
                    break;
                case DataFormatType.Byte:
                {
                    Console.WriteLine(RawData[0].ToString("X"));
                }
                    break;
                case DataFormatType.ByteList:
                    foreach (byte b in RawData)
                    {
                        Console.WriteLine(b.ToString("X"));
                    }

                    break;
                case DataFormatType.UINT16:
                {
                    ushort word = GetChunkAsUint16List()[0];
                    Console.WriteLine(@"Word: " + word.ToString("X"));
                    break;
                }
                case DataFormatType.UINT16List:
                    foreach (ushort word in GetChunkAsUint16List())
                    {
                        Console.WriteLine(@"Word: " + word.ToString("X"));
                    }

                    break;
                case DataFormatType.Unknown:
                    Console.WriteLine(@"Unknown");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public List<string> GetAsStringListFromIndexes()
        {
            return GetAsStringListFromIndexes(GetChunkAsUint16List(), _fullRawData);
        }

        private static List<string> GetAsStringListFromIndexes(IReadOnlyCollection<ushort> indexList,
            List<byte> rawByteList)
        {
            List<string> strList = new List<string>(indexList.Count);
            const int MaxStrLength = 20;

            foreach (ushort index in indexList)
            {
                // we grab the strings from the fullRawData because it is the entire file
                strList.Add(CreateDataChunk(DataFormatType.SimpleString, string.Empty, rawByteList, index, MaxStrLength)
                    .GetChunkAsString());
            }

            return strList;
        }

        public List<bool> GetAsBitmapBoolList(int nStart, int nLength)
        {
            List<bool> boolList = new List<bool>(nLength);

            // loop through all bytes, then each of their bits, creating a list of Booleans
            for (int nByte = nStart; nByte < nStart + nLength; nByte++)
            {
                byte compareByte = 0x80;
                byte curByte = (byte) (RawData[nByte] + ValueModifier); 

                for (int nBit = BITS_PER_BYTE - 1; nBit >= 0; nBit--)
                {
                    bool curBit = (compareByte & curByte) > 0;
                    compareByte >>= 1;
                    boolList.Add(curBit);
                    // if (isDebug) Debug.WriteLine("Byte #" + nByte.ToString() + "  Bit #" + nBit.ToString() + "=" + curBit.ToString());
                }
            }

            return boolList;
        }
        
        /// <summary>
        ///     Returns the data in the form of each bit represented as a boolean value
        /// </summary>
        /// <returns>A list of the extracted boolean values</returns>
        public List<bool> GetAsBitmapBoolList()
        {
            return GetAsBitmapBoolList(0, DataLength);
        }

        /// <summary>
        ///     Return the data as a byte list
        ///     Note: this will add/subtract using the addToValue
        /// </summary>
        /// <returns>a list of bytes</returns>
        public List<byte> GetAsByteList()
        {
            List<byte> data = new List<byte>(DataLength);

            for (int i = 0; i < DataLength; i++)
            {
                byte rawData = RawData[i];
                data.Add((byte) (rawData + ValueModifier));
            }

            return data;
        }

        public byte GetByte(int nIndex)
        {
            Debug.Assert(nIndex < RawData.Length && nIndex >= 0);
            return RawData[nIndex];
        }
        

        public void SetChunkAsByte(byte data)
        {
            RawData[0] = data;
        }

        public void SetChunkAsUint16(ushort data)
        {
            RawData[0] = (byte) (data & 0xFF);
            RawData[1] = (byte) ((data >> 8) & 0xFF);
        }

        public byte GetChunkAsByte()
        {
            return RawData[0];
        }

        /// <summary>
        ///     Returns the data as a single UINT16. It uses little endian convention.
        /// </summary>
        /// <returns>UINT16s</returns>
        public ushort GetChunkAsUint16()
        {
            ushort data = (ushort) (RawData[0] | (RawData[1] << 8));
            return data;
        }

        /// <summary>
        ///     Returns the data as a UINT16 list. It uses little endian convention.
        ///     Note: this will add/subtract using the addToValue
        /// </summary>
        /// <returns>list of UINT16s</returns>
        public List<ushort> GetChunkAsUint16List()
        {
            List<ushort> data = Utils.CreateOffsetList(RawData, FileOffset, DataLength);
            for (int i = 0; i < data.Count; i++)
            {
                data[i] = (ushort) (data[i] + ValueModifier);
            }

            return data;
        }

        /// <summary>
        ///     Returns a string representation of the datachunk
        /// </summary>
        /// <returns>a single string</returns>
        public string GetChunkAsString()
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (DataFormat)
            {
                case DataFormatType.FixedString:
                    return Utils.BytesToStringFixedWidth(RawData.ToList(), 0, DataLength);
                case DataFormatType.SimpleString:
                    return Utils.BytesToStringNullTerm(RawData.ToList(), 0, DataLength);
                default:
                    throw new Ultima5ReduxException("String datatype doesn't match predefined list.");
            }
        }

        /// <summary>
        ///     Returns a collection of strings
        /// </summary>
        /// <returns></returns>
        public SomeStrings GetChunkAsStringList()
        {
            return new SomeStrings(RawData.ToList(), 0, DataLength);
        }
    }
}