using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public class DataChunks
    {
        public DataChunks()
        {
            dataChunks = new List<DataChunk>();
        }

        private List<DataChunk> dataChunks;

        public void AddDataChunk (DataChunk chunk) 
        {
            dataChunks.Add(chunk);
        }

        public DataChunk GetChunk(int index)
        {
            return dataChunks[index];
        }

        public void PrintEverything()
        {
            foreach (DataChunk chunk in dataChunks)
            {
                System.Console.WriteLine("**** " + chunk.Description);
                chunk.PrintChunk();
            }
        }

    }


    public class DataChunk
    {
        public enum DataFormatType { Unknown, FixedString, SimpleString, StringList, UINT16List, ByteList };

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

        public DataChunk(DataFormatType dataFormat, string description, List<byte> rawData, int offset, int dataLength) : 
            this(dataFormat, description, rawData, offset, dataLength, 0)
        {            
        }

        public void PrintChunk()
        {
            switch (DataFormat)
            {
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

        public List<UInt16> GetChunkAsUINT16 ()
        {
            List<UInt16> data = Utils.CreateOffsetList(RawData, FileOffset, DataLength);
            for (int i = 0; i < data.Count; i++)
            {
                data[i] = (UInt16)(data[i] + ValueModifier);
            }
            return data;
        }

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

        public SomeStrings GetChunkAsStringList()
        {
            return (new SomeStrings(RawData.ToList(), 0, DataLength));
        }

        public string Description { get; }
        public byte[] RawData { get; }
        public int DataLength { get; }
        public int FileOffset { get; }
        public byte ValueModifier { get;  }
        public DataFormatType DataFormat { get; }

    }
}
