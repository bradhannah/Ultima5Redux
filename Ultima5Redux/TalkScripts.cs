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

        private TalkingReferences talkRef;

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

            talkRefs.Add(mapMaster, new List<byte[]>(nEntries));

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

                    byte[] chunk = new byte[chunkLength];

                    // copy only the bytes from the offset
                    talkByteList.CopyTo(npcOffsets[i].fileOffset, chunk, 0, (int)chunkLength);
                    // Add the raw bytes to the specific Map+NPC#
                    talkRefs[mapMaster].Add(chunk); // have to make an assumption that the values increase 1 at a time, this should be true though

                    

                    //talkRefs[mapMaster][i] = talkByteList.ToArray()

                }
            }
        }

        // example NPC 1 at Castle in Lord British's castle
        // C1 EC  E9 F3 F4 E1 E9 F2 01 C2 E1 F2 E4 00
        // 65 108  69 
        public TalkScripts(string u5Directory, TalkingReferences talkRef)
        {
            this.talkRef = talkRef;

            InitalizeTalkScripts(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Castle);
            InitalizeTalkScripts(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Towne);
            InitalizeTalkScripts(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Keep);
            InitalizeTalkScripts(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Dwelling);

        }

        public void PrintSomeTalking()
        {
            Console.WriteLine("Printsometalking.....");
            bool letterAtATime = false;
            foreach (byte byteWord in talkRefs[SmallMapReference.SingleMapReference.SmallMapMasterFiles.Castle][0])
            {
                if (byteWord == 0x00) { Console.WriteLine("");  letterAtATime = false;  continue;  }
                byte tempByte = (byte)((int)byteWord);
                bool phrase = false;
                if (byteWord >= 165 && byteWord <= 218) { tempByte -= 128;  }
                else if (byteWord >= 225 && byteWord <= 250) { tempByte -= 128;  }
                else if (byteWord >= 160 && byteWord <= 161) { tempByte -= 128;  }
                else
                {
                    phrase = true;
                }
                if (!phrase)
                {
                    letterAtATime = true;
                    if ((char)tempByte=='@')
                    {
                        letterAtATime = false;
                        Console.Write(" ");
                        continue; 
                    }
                    Console.Write((char)tempByte);
                }
                else if (phrase)
                {
                        string talkingWord = talkRef.GetTalkingWord((int)tempByte);
                        Console.Write(talkingWord+" ");
                }
                else
                {
                    switch (tempByte)
                    {
                        case 130:
                            Console.Write("<End Conversation>");
                            break;
                        case 131:
                            Console.Write("<Pause>");
                            break;
                        case 132:
                            Console.Write("<Join Party>");
                            break;
                        case 133:
                            Console.Write("<Gold - ");
                            break;
                        case 134:
                            Console.Write("<Change>");
                            break;
                        case 135:
                            Console.Write("");
                            break;
                        case 136:
                            Console.Write("<Or>");
                            break;
                        case 137:
                            Console.Write("<Karma + 1>");
                            break;
                        case 138:
                            Console.Write("<Karma - 1>");
                            break;
                        case 139:
                            Console.Write("<Call Guards>");
                            break;
                        case 140:
                            Console.Write("<Set Flag>");
                            break;
                        case 141:
                            Console.Write("<New Line>");
                            break;
                        case 142:
                            Console.Write("<Rune>");
                            break;
                        case 143:
                            Console.Write("<Key Wait>");
                            break;
                        case 144:
                            Console.Write("<DEFAULT ANSWER>");
                            break;
                    }

                    if (tempByte >= 145 && tempByte <= 155)
                    {
                        Console.Write("<LABEL " + (tempByte - 145).ToString() + ">");
                    }

                }
            }
        }
    }
}
