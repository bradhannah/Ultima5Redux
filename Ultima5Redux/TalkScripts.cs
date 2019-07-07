using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace Ultima5Redux
{
    class TalkScript
    {

    }


    class TalkScripts
    {
        List<TalkScript> talkScripts = new List<TalkScript>();

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
        protected internal unsafe struct NPC_TalkOffset
        {
            public ushort npcIndex;
            public ushort fileOffset;
        }

        // map reference is key because NPC numbers can overlap
        private Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, List<byte[]>> talkRefs = 
            new Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, List<byte[]>>(sizeof(SmallMapReference.SingleMapReference.SmallMapMasterFiles));

        private enum TalkConstants { Name = 0 , Description, Greeting, Job, Bye }

        private void InitalizeTalkScripts(string u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles mapMaster)
        {
            string talkFilename = Path.Combine(u5Directory, SmallMapReference.SingleMapReference.GetTLKFilenameFromMasterFile(mapMaster));
            List<byte> talkByteList = Utils.GetFileAsByteList(talkFilename);

            // need this to make sure we don't fall of the end of the file when we read it
            FileInfo fi = new FileInfo(talkFilename);
            long talkFileSize = fi.Length;

            // keep track of the NPC to file offset mappings
            List<NPC_TalkOffset> npcOffsets;

            // the first word in the talk file tells you how many characters are referenced in script
            int nEntries = Utils.LittleEndianConversion(talkByteList[0], talkByteList[1]);

            // a list of all the offsets
            npcOffsets = new List<NPC_TalkOffset>(nEntries);

            unsafe
            {
                // you are in a single file right now
                for (int i = 0; i < (nEntries * sizeof(NPC_TalkOffset)); i += sizeof(NPC_TalkOffset))
                {
                    // add 2 because we know we are starting at an offset
                    npcOffsets.Add((NPC_TalkOffset)Utils.ReadStruct(talkByteList, 2 + i, typeof(NPC_TalkOffset)));
                    Console.WriteLine("NPC #" + npcOffsets.Last().npcIndex + " at offset " + npcOffsets.Last().fileOffset + " in file " + talkFilename);

                }
                // you are in a single file right now
                for (int i = 0; i < nEntries; i++)
                {
                    long chunkLength; // didn't want a long, but the file size is long...

                    // get the length by figuring the difference between 
                    if (i + 1 < nEntries)
                    {
                        chunkLength = npcOffsets[i+1].fileOffset - npcOffsets[i].fileOffset;
                    }
                    else
                    {
                        chunkLength = talkFileSize - npcOffsets[i].fileOffset;
                    }
                    
                    //todo: grab the bytes and save to a local byte[] array
                    //todo: 

                    //byte[] entryTalkBytes = new byte[]

                    //talkRefs[mapMaster][i] = talkByteList.ToArray()

                }
            }
        }

        // example NPC 1 at Castle in Lord British's castle
        // C1 EC E9 F3 F4 E1 E9 F2 01 C2 E1 F2 E4 00

        public TalkScripts (string u5Directory)
        {
            InitalizeTalkScripts(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Castle);
            InitalizeTalkScripts(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Towne);
            InitalizeTalkScripts(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Keep);
            InitalizeTalkScripts(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Dwelling);
        }

    }
}
