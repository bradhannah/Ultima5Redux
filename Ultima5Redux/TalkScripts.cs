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
        public enum TalkCommand {AvatarsName = 0x81, EndCoversation = 0x82, Pause = 0x83, JoinParty = 0x84, Gold = 0x85, Change = 0x86, Or = 0x87, AskName = 0x88, KarmaPlusOne = 0x89,
            KarmaMinusOne = 0x8A, CallGuards = 0x8B, SetFlag = 0x8C, NewLine = 0x8D, Rune = 0x8E, KeyWait = 0x8F, DefaultMessage = 0x90, Unknown_Code = 0xA2, Unknown_Enter = 0x9F, Label = 0xFE,
            Unknown_FF = 0xFF };

        public const byte MIN_LABEL = 0x91;
        public const byte MAX_LABEL = 0x91 + 0x0A;
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

        private enum TalkConstants { Name = 0, Description, Greeting, Job, Bye }

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
                        chunkLength = npcOffsets[i + 1].fileOffset - npcOffsets[i].fileOffset;
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

            bool writingSingleCharacters = false;
            List<bool> labelsSeenList = new List<bool>(10);
            labelsSeenList.AddRange(Enumerable.Repeat(false, 10));

            foreach (byte byteWord in talkRefs[SmallMapReference.SingleMapReference.SmallMapMasterFiles.Castle][5])
            {
                // if a NULL byte is provided then you need to go the next line, resetting the writingSingleCharacters so that a space is not inserted next line
                if (byteWord == 0x00) { Console.WriteLine(""); writingSingleCharacters = false; continue; }

                byte tempByte = (byte)((int)byteWord); // this is the byte that we will manipulate, leaving the byteWord in tact
                bool usePhraseLookup = false;   // did we do a phrase lookup (else we are typing single letters)
                bool useCompressedWord = false; // did we succesfully use a compressed word?

                // if it's one of the bytes that requires a subraction of 0x80 (128)
                if (byteWord >= 165 && byteWord <= 218) { tempByte -= 128; }
                else if (byteWord >= 225 && byteWord <= 250) { tempByte -= 128; }
                else if (byteWord >= 160 && byteWord <= 161) { tempByte -= 128; }
                else
                {
                    // it didn't match which means that it's one the special phrases and we will perform a lookup
                    usePhraseLookup = true;
                }

                // it wasn't a special phrase which means that the words are being typed one word at a time
                if (!usePhraseLookup)
                {
                    // I'm writing single characters, we will keep track so that when we hit the end we can insert a space
                    writingSingleCharacters = true;
                    // this signifies the end of the printing (sample code enters a newline)
                    if ((char)tempByte == '@')
                    {
                        Console.WriteLine("");
                        continue;
                    }
                    Console.Write((char)tempByte);
                }
                else // usePhraseLookup = true      
                {
                    // We were instructed to perform a lookup, either a compressed word lookup, or a special character

                    // if we were previously writing single characters, but have moved onto lookups, then we add a space, and reset it
                    if (writingSingleCharacters) { System.Console.Write(" "); writingSingleCharacters = false; }

                    // we are going to lookup the word in the compressed word list, if we throw an exception then we know it wasn't in the list
                    try
                    {
                        string talkingWord = talkRef.GetTalkingWord((int)tempByte);
                        Console.Write(talkingWord);
                        useCompressedWord = true;
                    }
                    // this is a bit lazy, but if I ask for a string that is not captured in the lookup map, then we know it's a special case
                    catch (TalkingReferences.NoTalkingWordException)
                    {
                        if (tempByte >= TalkScript.MIN_LABEL && tempByte <= TalkScript.MAX_LABEL)
                        {
                            int offset = tempByte - TalkScript.MIN_LABEL;
                            if (labelsSeenList[offset] == true)
                            {
                                Console.Write("<LABEL " + (offset + 1).ToString() + ">");
                            }
                            else
                            {
                                Console.Write("<GOTO LABEL " + (offset + 1).ToString() + ">");
                                labelsSeenList[offset] = true;
                            }
                        }
                        else
                        {
                            System.Console.Write("<" + ((TalkScript.TalkCommand)tempByte).ToString() + ">");
                        }
                    }
                    if (useCompressedWord) { Console.Write(" "); }
                }
            }
        }
    }
}
