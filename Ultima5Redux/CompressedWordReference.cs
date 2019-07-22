using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    class CompressedWordReference
    {
        private SomeStrings CompressedWords;

        private Dictionary<int, byte> compressWordLookupMap;

        /// <summary>
        /// Adds a byte offset lookup.
        /// This is hard to follow - but basically it describes what should be added or subtracted from an index based on the range it lies in.
        /// </summary>
        /// <param name="indexStart">the first index to apply the offset</param>
        /// <param name="indexStop">the last index to apply the offset to</param>
        /// <param name="offset">the offset to add or subtract (positive or negative)</param>
        private void AddByteLookupMapping(byte indexStart, byte indexStop, int offset)
        {
            Debug.Assert(indexStop <= 255);
            for (int i = indexStart; i <= indexStop; i++)
            {
                compressWordLookupMap.Add((byte)i, (byte)(i + offset));
            }
        }

        /// <summary>
        /// Construct a compressed word reference using the Data.OVL reference
        /// </summary>
        /// <param name="dataRef"></param>
        public CompressedWordReference(DataOvlReference dataRef)
        {
            CompressedWords = dataRef.GetDataChunk(DataOvlReference.DataChunkName.TALK_COMPRESSED_WORDS).GetChunkAsStringList();
            CompressedWords.PrintSomeStrings();

            // we are creating a lookup map because the indexes are not concurrent
            compressWordLookupMap = new Dictionary<int, byte>(CompressedWords.Strs.Count);

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

        public bool IsTalkingWord(int index)
        {
            // Note: I originally wrote this just to catch the exceptions, but it was SUPER SLOW, so I hopefully smartened it up
            // is the index in the range that we can even lookup?
            if (index > (compressWordLookupMap.Keys.Max() - compressWordLookupMap.Keys.Min()))
            {
                return false;
            }
            // if the index is in range, then does the key exist?
            if (!compressWordLookupMap.ContainsKey(index))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a compressed word with an index
        /// The index will be automatically adjusted
        /// </summary>
        /// <param name="index">the index as it appears in the .tlk file</param>
        /// <returns></returns>
        public string GetTalkingWord(int index)
        {
            try
            {
                return (CompressedWords.Strs[compressWordLookupMap[index]]);
            } catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new NoTalkingWordException("Couldn't find TalkWord mapping in compressed word file at index " + index);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                throw new NoTalkingWordException("Couldn't find TalkWord at index " + index);
            }
        }

        /// <summary>
        /// Couldn't find a talking word at the indicated index
        /// </summary>
        public class NoTalkingWordException: Exception
        {
            public NoTalkingWordException(string message) :base(message)
            {
            }
        }

    }
}
