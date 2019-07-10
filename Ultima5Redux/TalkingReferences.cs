using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    class TalkingReferences
    {
        private SomeStrings CompressedWords;

        private Dictionary<int, byte> compressWordLookupMap;

        private void AddByteLookupMapping(byte indexStart, byte indexStop, int offset)
        {
            Debug.Assert(indexStop <= 255);
            for (int i = indexStart; i <= indexStop; i++)
            {
                compressWordLookupMap.Add((byte)i, (byte)(i + offset));
            }
        }

        public TalkingReferences(DataOvlReference dataRef)
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

        public class NoTalkingWordException: Exception
        {
            public NoTalkingWordException(string message) :base(message)
            {
            }
        }

    }
}
