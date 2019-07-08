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


            if (false)
            {
                AddByteLookupMapping(1, 255, 0);
            }
            else if (false)
            { 
                AddByteLookupMapping(1, 7, 0);
                AddByteLookupMapping(9, 27, 1);
                AddByteLookupMapping(29, 49, 2);
                AddByteLookupMapping(51, 64, 3);
                AddByteLookupMapping(66, 66, 4);
                AddByteLookupMapping(68, 69, 5);
                AddByteLookupMapping(71, 71, 6);
                AddByteLookupMapping(76, 129, 11);
            }
            else if (true)
            {
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
            else
            {
                AddByteLookupMapping(1, 7, 0);
                AddByteLookupMapping(9, 27, -1);
                AddByteLookupMapping(29, 49, -2);
                AddByteLookupMapping(51, 64, -3);
                AddByteLookupMapping(66, 66, -4);
                AddByteLookupMapping(68, 69, -5);
                AddByteLookupMapping(71, 71, -6);
                AddByteLookupMapping(76, 129, -11);

            }
        }

        public string GetTalkingWord(int index)
        {
            if (!compressWordLookupMap.Keys.Contains(index))
            {
                return "MISSING";
            }
            return (CompressedWords.Strs[compressWordLookupMap[index]]);
        }


    }
}
