using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ultima5Redux.Data;

namespace Ultima5Redux.References.Dialogue
{
    public class CompressedWordReference
    {
        /// <summary>
        ///     when you must adjust the offset into the compressed word lookup, subtract this
        /// </summary>
        private const int TALK_OFFSET_ADJUST = 0x80;

        private readonly SomeStrings _compressedWords;

        private readonly Dictionary<int, byte> _compressWordLookupMap;

        /// <summary>
        ///     Construct a compressed word reference using the Data.OVL reference
        /// </summary>
        /// <param name="dataRef"></param>
        public CompressedWordReference(DataOvlReference dataRef)
        {
            _compressedWords = dataRef.GetDataChunk(DataOvlReference.DataChunkName.TALK_COMPRESSED_WORDS)
                .GetChunkAsStringList();
            //_compressedWords.PrintSomeStrings();

            // we are creating a lookup map because the indexes are not concurrent
            _compressWordLookupMap = new Dictionary<int, byte>(_compressedWords.StringList.Count);

            // this is kind of gross, but these define the index gaps in the lookup table
            // I have no idea why there are gaps, but alas, this works around them
            int i = 0;
            AddByteLookupMapping(1, 7, --i);
            AddByteLookupMapping(9, 27, --i);
            AddByteLookupMapping(29, 49, --i);
            AddByteLookupMapping(51, 64, --i);
            AddByteLookupMapping(66, 66, --i);
            AddByteLookupMapping(68, 69, --i);
            AddByteLookupMapping(71, 71, --i);
            i -= 4;
            AddByteLookupMapping(76, 129, i);
        }

        /// <summary>
        ///     Adds a byte offset lookup.
        ///     This is hard to follow - but basically it describes what should be added or subtracted from an index based on the
        ///     range it lies in.
        /// </summary>
        /// <param name="indexStart">the first index to apply the offset</param>
        /// <param name="indexStop">the last index to apply the offset to</param>
        /// <param name="offset">the offset to add or subtract (positive or negative)</param>
        private void AddByteLookupMapping(byte indexStart, byte indexStop, int offset)
        {
            Debug.Assert(indexStop <= 255);
            for (int i = indexStart; i <= indexStop; i++)
            {
                _compressWordLookupMap.Add((byte)i, (byte)(i + offset));
            }
        }

        /// <summary>
        ///     Get a compressed word with an index
        ///     The index will be automatically adjusted
        /// </summary>
        /// <param name="index">the index as it appears in the .tlk file</param>
        /// <returns></returns>
        public string GetTalkingWord(int index)
        {
            try
            {
                return _compressedWords.StringList[_compressWordLookupMap[index]];
            } catch (KeyNotFoundException)
            {
                throw new NoTalkingWordException("Couldn't find TalkWord mapping in compressed word file at index " +
                                                 index);
            } catch (ArgumentOutOfRangeException)
            {
                throw new NoTalkingWordException("Couldn't find TalkWord at index " + index);
            }
        }

        /// <summary>
        ///     Is this an expected letter or digit in the string
        /// </summary>
        /// <param name="character"></param>
        /// <returns>true if it is acceptable</returns>
        private bool IsAcceptableLettersOrDigits(char character)
        {
            return character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' ||
                   character >= '0' && character <= '9';
        }

        /// <summary>
        ///     Is it expected punctuation (not replacement characters though)
        /// </summary>
        /// <param name="character"></param>
        /// <returns>true if it is acceptable</returns>
        private bool IsAcceptablePunctuation(char character)
        {
            return character == ' ' || character == '"' || character == '!' || character == ',' || character == '\'' ||
                   character == '.' || character == '-' || character == '?' || character == '\n' || character == ';';
        }

        private bool IsReplacementCharacter(char character)
        {
            // % is gold
            // & is current piece of equipment
            // # current business (maybe with apostrophe s)
            // $ merchants name
            // @ barkeeps food/drink etc
            // * location of thing
            // ^ quantity of thing (ie. reagent)
            return character == '%' || character == '&' || character == '$' || character == '#' || character == '@' ||
                   character == '*' || character == '^';
        }

        /// <summary>
        ///     Is there a talking word at the given index?
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool IsTalkingWord(int index)
        {
            // Note: I originally wrote this just to catch the exceptions, but it was SUPER SLOW, so I hopefully smartened it up
            // is the index in the range that we can even lookup?
            if (index > _compressWordLookupMap.Keys.Max() - _compressWordLookupMap.Keys.Min()) return false;
            // if the index is in range, then does the key exist?
            return _compressWordLookupMap.ContainsKey(index);
        }

        /// <summary>
        ///     Replaces all compressed word symbols from the shopkeepers dialogue
        ///     <remarks>this leaves the variable replacement symbols in tact</remarks>
        /// </summary>
        /// <param name="rawString">raw string from shoppes.dat</param>
        /// <returns>full string with all compressed words expanded</returns>
        /// <exception cref="NoTalkingWordException"></exception>
        public string ReplaceRawMerchantStringsWithCompressedWords(string rawString)
        {
            string buildAWord = string.Empty;
            bool bUseCompressedWord = false;

            foreach (char c in rawString)
            {
                byte byteWord = (byte)c;
                byte tempByte = byteWord;

                bool bUsePhraseLookup;
                if (!(IsAcceptablePunctuation((char)byteWord) || IsAcceptableLettersOrDigits((char)byteWord) ||
                      IsReplacementCharacter((char)byteWord)))
                {
                    bUsePhraseLookup = true;
                    tempByte -= TALK_OFFSET_ADJUST;
                }
                else
                {
                    bUsePhraseLookup = false;
                }

                if (bUsePhraseLookup)
                {
                    // if we were previously writing single characters, but have moved onto lookups, then we add a space, and reset it
                    buildAWord += " ";

                    // we are going to lookup the word in the compressed word list, if we throw an exception then we know it wasn't in the list
                    if (IsTalkingWord(tempByte + 1))
                    {
                        string talkingWord = GetTalkingWord(tempByte + 1);
                        bUseCompressedWord = true;
                        buildAWord += talkingWord;
                    }
                    else
                    {
                        //Console.WriteLine("test");
                        throw new NoTalkingWordException("Shoppe keeper has uncertain instruction +" + tempByte);
                    }
                }
                else
                {
                    // if we were writing single characters then we need insert a space before we start
                    if (bUseCompressedWord)
                    {
                        buildAWord += " ";
                        // I'm writing single characters, we will keep track so that when we hit the end we can insert a space
                        bUseCompressedWord = false;
                    }

                    buildAWord += (char)tempByte;
                }
            }

            return buildAWord;
        }

        /// <summary>
        ///     Couldn't find a talking word at the indicated index
        /// </summary>
        private class NoTalkingWordException : Ultima5ReduxException //-V3164
        {
            public NoTalkingWordException(string message) : base(message)
            {
            }
        }
    }
}