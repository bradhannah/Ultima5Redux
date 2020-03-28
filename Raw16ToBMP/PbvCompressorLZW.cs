using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PbvCompressor
{
    public interface ICompressorAlgorithm
    {
        bool Compress(string pIntputFileName, string pOutputFileName);
        bool Decompress(string pIntputFileName, string pOutputFileName);
    }

    //LZW Based Decompressor - basic algorithm used as described on Mark Nelson's website  http://marknelson.us
    public class PbvCompressorLzw : ICompressorAlgorithm
    {
        private const int MAX_BITS = 14; //maimxum bits allowed to read
        private const int HASH_BIT = MAX_BITS - 8; //hash bit to use with the hasing algorithm to find correct index
        private const int MAX_VALUE = (1 << MAX_BITS) - 1; //max value allowed based on max bits
        private const int MAX_CODE = MAX_VALUE - 1; //max code possible
        private const int TABLE_SIZE = 18041; //must be bigger than the maximum allowed by maxbits and prime

        private int[] _iaCodeTable = new int[TABLE_SIZE]; //code table
        private int[] _iaPrefixTable = new int[TABLE_SIZE]; //prefix table
        private int[] _iaCharTable = new int[TABLE_SIZE]; //character table

        private ulong _iBitBuffer; //bit buffer to temporarily store bytes read from the files
        private int _iBitCounter; //counter for knowing how many bits are in the bit buffer

        private void Initialize() //used to blank  out bit buffer incase this class is called to comprss and decompress from the same instance
        {
            _iBitBuffer = 0;
            _iBitCounter = 0;
        }

        public bool Compress(string pInputFileName, string pOutputFileName)
        {
            Stream reader = null;
            Stream writer = null;

            try
            {
                Initialize();
                reader = new FileStream(pInputFileName, FileMode.Open);
                writer = new FileStream(pOutputFileName, FileMode.Create);
                int iNextCode = 256;
                int iChar = 0, iString = 0, iIndex = 0;

                for (int i = 0; i < TABLE_SIZE; i++) //blank out table
                    _iaCodeTable[i] = -1;

                iString = reader.ReadByte(); //get first code, will be 0-255 ascii char

                while ((iChar = reader.ReadByte()) != -1) //read until we reach end of file
                {
                    iIndex = FindMatch(iString, iChar); //get correct index for prefix+char

                    if (_iaCodeTable[iIndex] != -1) //set string if we have something at that index
                        iString = _iaCodeTable[iIndex];
                    else //insert new entry
                    {
                        if (iNextCode <= MAX_CODE) //otherwise we insert into the tables
                        {
                            _iaCodeTable[iIndex] = iNextCode++; //insert and increment next code to use
                            _iaPrefixTable[iIndex] = iString;
                            _iaCharTable[iIndex] = (byte)iChar;
                        }

                        WriteCode(writer, iString); //output the data in the string
                        iString = iChar;
                    }
                }

                WriteCode(writer, iString); //output last code
                WriteCode(writer, MAX_VALUE); //output end of buffer
                WriteCode(writer, 0); //flush
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                if (writer != null)
                    writer.Close();
                File.Delete(pOutputFileName);
                return false;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                if (writer != null)
                    writer.Close();
            }

            return true;
        }

        //hasing function, tries to find index of prefix+char, if not found returns -1 to signify space available
        private int FindMatch(int pPrefix, int pChar)
        {
            int index = 0, offset = 0;

            index = (pChar << HASH_BIT) ^ pPrefix;

            offset = (index == 0) ? 1 : TABLE_SIZE - index;

            while (true)
            {
                if (_iaCodeTable[index] == -1)
                    return index;

                if (_iaPrefixTable[index] == pPrefix && _iaCharTable[index] == pChar)
                    return index;

                index -= offset;
                if (index < 0)
                    index += TABLE_SIZE;
            }
        }

        private void WriteCode(Stream pWriter, int pCode)
        {
            _iBitBuffer |= (ulong)pCode << (32 - MAX_BITS - _iBitCounter); //make space and insert new code in buffer
            _iBitCounter += MAX_BITS; //increment bit counter

            while (_iBitCounter >= 8) //write all the bytes we can
            {
                int temp = (byte)((_iBitBuffer >> 24) & 255);
                pWriter.WriteByte((byte)((_iBitBuffer >> 24) & 255)); //write byte from bit buffer
                _iBitBuffer <<= 8; //remove written byte from buffer
                _iBitCounter -= 8; //decrement counter
            }
        }

        public bool Decompress(string pInputFileName, string pOutputFileName)
        {
            Stream reader = null;
            Stream writer = null;

            try
            {
                Initialize();
                reader = new FileStream(pInputFileName, FileMode.Open);
                writer = new FileStream(pOutputFileName, FileMode.Create);
                int iNextCode = 256;
                int iNewCode, iOldCode;
                byte bChar;
                int iCurrentCode, iCounter;
                byte[] baDecodeStack = new byte[TABLE_SIZE];

                iOldCode = ReadCode(reader);
                bChar = (byte)iOldCode;
                writer.WriteByte((byte)iOldCode); //write first byte since it is plain ascii

                iNewCode = ReadCode(reader);

                while (iNewCode != MAX_VALUE) //read file all file
                {
                    if (iNewCode >= iNextCode)
                    { //fix for prefix+chr+prefix+char+prefx special case
                        baDecodeStack[0] = bChar;
                        iCounter = 1;
                        iCurrentCode = iOldCode;
                    }
                    else
                    {
                        iCounter = 0;
                        iCurrentCode = iNewCode;
                    }

                    while (iCurrentCode > 255) //decode string by cycling back through the prefixes
                    {
                        //lstDecodeStack.Add((byte)_iaCharTable[iCurrentCode]);
                        //iCurrentCode = _iaPrefixTable[iCurrentCode];
                        baDecodeStack[iCounter] = (byte)_iaCharTable[iCurrentCode];
                        ++iCounter;
                        if (iCounter >= MAX_CODE)
                            throw new Exception("oh crap");
                        iCurrentCode = _iaPrefixTable[iCurrentCode];
                    }

                    baDecodeStack[iCounter] = (byte)iCurrentCode;
                    bChar = baDecodeStack[iCounter]; //set last char used

                    while (iCounter >= 0) //write out decodestack
                    {
                        writer.WriteByte(baDecodeStack[iCounter]);
                        --iCounter;
                    }

                    if (iNextCode <= MAX_CODE) //insert into tables
                    {
                        _iaPrefixTable[iNextCode] = iOldCode;
                        _iaCharTable[iNextCode] = bChar;
                        ++iNextCode;
                    }

                    iOldCode = iNewCode;

                    //if (reader.PeekChar() != 0)
                    iNewCode = ReadCode(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                if (writer != null)
                    writer.Close();
                File.Delete(pOutputFileName);
                return false;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                if (writer != null)
                    writer.Close();
            }

            return true;
        }

        private int ReadCode(Stream pReader)
        {
            uint iReturnVal;

            while (_iBitCounter <= 24) //fill up buffer
            {
                _iBitBuffer |= (ulong)pReader.ReadByte() << (24 - _iBitCounter); //insert byte into buffer
                _iBitCounter += 8; //increment counter
            }

            iReturnVal = (uint)_iBitBuffer >> (32 - MAX_BITS); //get last byte from buffer so we can return it
            _iBitBuffer <<= MAX_BITS; //remove it from buffer
            _iBitCounter -= MAX_BITS; //decrement bit counter

            int temp = (int)iReturnVal;
            return temp;
        }
    }
}