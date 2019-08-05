using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace Ultima5Redux
{

    /// <summary>
    /// TalkScripts represents all of the in game talking scripts for all NPCs
    /// </summary>
    public class TalkScripts
    {
        /// <summary>
        /// do I print Debug output to the Console
        /// </summary>
        private bool isDebug = false;


        /// <summary>
        /// the mapping of NPC # to file .tlk file offset
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
        protected internal unsafe struct NPC_TalkOffset
        {
            public ushort npcIndex;
            public ushort fileOffset;
        }

        /// <summary>
        /// Dictionary that refers to the raw bytes for each NPC based on Map master file and NPC index
        /// </summary>
        private Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, Dictionary<int, byte[]>> talkRefs =
            new Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, Dictionary<int, byte[]>>(sizeof(SmallMapReference.SingleMapReference.SmallMapMasterFiles));

        /// <summary>
        /// Dictionary that refers to the fully interpreted TalkScripts for each NPC based on Master map file and NPC index
        /// </summary>
        private Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, Dictionary<int, TalkScript>> talkScriptRefs = 
            new Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, Dictionary<int, TalkScript>>();

        /// <summary>
        /// when you must adjust the offset into the compressed word lookup, subtract this
        /// </summary>
        private const int TALK_OFFSET_ADJUST = 0x80;
        /// <summary>
        /// a null byte signifies the end of the script line
        /// </summary>
        private const byte END_OF_SCRIPTLINE_BYTE = 0x00;

        // all of the compressed words that are referenced in the .tlk files
        private CompressedWordReference compressedWordRef;

        /// <summary>
        /// Build the talk scripts
        /// </summary>
        /// <param name="u5Directory">Directory with Ultima 5 data files</param>
        /// <param name="dataRef">DataOVL Reference provides compressed word details</param>
        public TalkScripts(string u5Directory, DataOvlReference dataRef)
        {
            // save the compressed words, we're gonna need them
            this.compressedWordRef = new CompressedWordReference(dataRef);

            // just a lazy array that is easier to enumerate than the enum
            SmallMapReference.SingleMapReference.SmallMapMasterFiles[] smallMapRefs =
            {
                SmallMapReference.SingleMapReference.SmallMapMasterFiles.Castle,
                SmallMapReference.SingleMapReference.SmallMapMasterFiles.Towne,
                SmallMapReference.SingleMapReference.SmallMapMasterFiles.Keep,
                SmallMapReference.SingleMapReference.SmallMapMasterFiles.Dwelling
            };

            // for each of the maps we are going to initialize
            foreach (SmallMapReference.SingleMapReference.SmallMapMasterFiles mapRef in smallMapRefs)
            {
                // initialize the raw component of the talk scripts
                InitalizeTalkScriptsRaw(u5Directory, mapRef);
                
                // initialize and allocate the appropriately sized list of TalkScript(s)
                talkScriptRefs.Add(mapRef, new Dictionary<int, TalkScript>(talkRefs[mapRef].Count));

                // for each of the NPCs in the particular map, initialize the individual NPC talk script
                foreach (int key in talkRefs[mapRef].Keys)
                {
                    talkScriptRefs[mapRef][key] = InitializeTalkScriptFromRaw(mapRef, key);
                    if (isDebug) Console.WriteLine("TalkScript in " + mapRef.ToString() + " with #" + key.ToString());
                }
            }
        }

        public TalkScript GetTalkScript(SmallMapReference.SingleMapReference.SmallMapMasterFiles smallMapRef, int nNPC)
        {
            if (NonPlayerCharacters.NonPlayerCharacter.IsSpecialDialogType((NonPlayerCharacters.NonPlayerCharacter.NPCDialogTypeEnum)nNPC))
            { return null; }
            return (talkScriptRefs[smallMapRef][nNPC]);
        }

        /// <summary>
        /// Initlializes the talk scripts into a fairly raw byte[] format
        /// </summary>
        /// <param name="u5Directory">directory of Ultima 5 data files</param>
        /// <param name="mapMaster">the small map reference (helps pick *.tlk file)</param>
        private void InitalizeTalkScriptsRaw(string u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles mapMaster)
        {
            // example NPC 1 at Castle in Lord British's castle
            // C1 EC  E9 F3 F4 E1 E9 F2 01 C2 E1 F2 E4 00
            // 65 108  69 

            string talkFilename = Path.Combine(u5Directory, SmallMapReference.SingleMapReference.GetTLKFilenameFromMasterFile(mapMaster));

            // the raw bytes of the talk file
            List<byte> talkByteList = Utils.GetFileAsByteList(talkFilename);

            // need this to make sure we don't fall of the end of the file when we read it
            FileInfo fi = new FileInfo(talkFilename);
            long talkFileSize = fi.Length;

            // keep track of the NPC to file offset mappings
            //List<NPC_TalkOffset> npcOffsets;
            Dictionary<int, NPC_TalkOffset> npcOffsets;

            // the first word in the talk file tells you how many characters are referenced in script
            int nEntries = Utils.LittleEndianConversion(talkByteList[0], talkByteList[1]);

            //talkRefs.Add(mapMaster, new List<byte[]>(nEntries));
            talkRefs.Add(mapMaster, new Dictionary<int, byte[]>(nEntries));

            // a list of all the offsets
            //npcOffsets = new List<NPC_TalkOffset>(nEntries);
            npcOffsets = new Dictionary<int, NPC_TalkOffset>(nEntries);

            unsafe
            {
                // you are in a single file right now
                for (int i = 0; i < (nEntries * sizeof(NPC_TalkOffset)); i += sizeof(NPC_TalkOffset))
                {
                    // add 2 because we know we are starting at an offset
                    unsafe {
                        NPC_TalkOffset talkOffset = (NPC_TalkOffset)Utils.ReadStruct(talkByteList, 2 + i, typeof(NPC_TalkOffset));
                        npcOffsets[talkOffset.npcIndex] = talkOffset;

                        // OMG I'm tired.. figure out why this isn't printing properly....
                        if (isDebug) Console.WriteLine("NPC #" + npcOffsets[talkOffset.npcIndex].npcIndex + " at offset " + npcOffsets[talkOffset.npcIndex].fileOffset + " in file " + talkFilename);
                    }
                }
                // you are in a single file right now
                // repeat for every single NPC in the file
                int count = 1;
                foreach (int key in npcOffsets.Keys)
                {
                    long chunkLength = 0; // didn't want a long, but the file size is long...

                    // calculate the offset size
//                    foreach (int key in npcOffsets.Keys)
                    {
                        if (count < npcOffsets.Keys.Count)
                        {
                            chunkLength = npcOffsets[key + 1].fileOffset - npcOffsets[key].fileOffset;
                        }
                        else
                        {
                            chunkLength = talkFileSize - npcOffsets[key].fileOffset;
                        }

                        count++;
                    }

                    byte[] chunk = new byte[chunkLength];

                    // copy only the bytes from the offset
                    talkByteList.CopyTo(npcOffsets[key].fileOffset, chunk, 0, (int)chunkLength);
                    // Add the raw bytes to the specific Map+NPC#
                    talkRefs[mapMaster].Add(key, chunk); // have to make an assumption that the values increase 1 at a time, this should be true though
                }
            }
        }


        /// <summary>
        /// Intializes an individual TalkingScript using the raw data created from InitalizeTalkScriptsRaw
        /// <remark>May God have mercy on my soul if I ever need to debug or troubleshoot this again.</remark>
        /// </summary>
        /// <param name="smallMapRef">the small map reference</param>
        /// <param name="index">NPC Index</param>
        private TalkScript InitializeTalkScriptFromRaw (SmallMapReference.SingleMapReference.SmallMapMasterFiles smallMapRef, int index)
        {
            TalkScript talkScript = new TalkScript(); // the script we are building and will return

            List<bool> labelsSeenList = new List<bool>(TalkScript.TOTAL_LABELS);    // keeps track of the labels we have already seen
            labelsSeenList.AddRange(Enumerable.Repeat(false, TalkScript.TOTAL_LABELS)); // creates a list of "false" bools to set the default labelsSeenList

            bool writingSingleCharacters = false;   // are we currently writing a single character at a time?
            string buildAWord = string.Empty;       // the word we are currently building if we are writingSingleCharacters=true

            foreach (byte byteWord in talkRefs[smallMapRef][index])
            {
                // if a NULL byte is provided then you need to go the next line, resetting the writingSingleCharacters so that a space is not inserted next line
                if (byteWord == END_OF_SCRIPTLINE_BYTE) {
                    buildAWord += "\n";
                    // we are done with the entire line, so lets add it the script
                    talkScript.AddTalkCommand(TalkScript.TalkCommand.PlainString, buildAWord);
                    // tells the script to move onto the next command
                    talkScript.NextLine();
                    // reset some vars
                    buildAWord = string.Empty;
                    writingSingleCharacters = false;
                    continue;
                }

                byte tempByte = (byte)((int)byteWord); // this is the byte that we will manipulate, leaving the byteWord in tact
                bool usePhraseLookup = false;   // did we do a phrase lookup (else we are typing single letters)
                bool useCompressedWord = false; // did we succesfully use a compressed word?

                // if it's one of the bytes that requires a subraction of 0x80 (128)
                if (byteWord >= 165 && byteWord <= 218) { tempByte -= TALK_OFFSET_ADJUST; }
                else if (byteWord >= 225 && byteWord <= 250) { tempByte -= TALK_OFFSET_ADJUST; }
                else if (byteWord >= 160 && byteWord <= 161) { tempByte -= TALK_OFFSET_ADJUST; }
                else
                {
                    // it didn't match which means that it's one the special phrases and we will perform a lookup
                    usePhraseLookup = true;
                }
                if (tempByte == 119) { Console.Write("");  }
                // it wasn't a special phrase which means that the words are being typed one word at a time
                if (!usePhraseLookup)
                {
                    // I'm writing single characters, we will keep track so that when we hit the end we can insert a space
                    writingSingleCharacters = true;
                    // this signifies the end of the printing (sample code enters a newline)
                    if ((char)tempByte == '@')
                    {
                        //Console.WriteLine("");
                        continue;
                    }
                    //Console.Write((char)tempByte);
                    buildAWord += (char)tempByte;
                }
                else // usePhraseLookup = true      
                {
                    // We were instructed to perform a lookup, either a compressed word lookup, or a special character

                    // if we were previously writing single characters, but have moved onto lookups, then we add a space, and reset it
                    if (writingSingleCharacters) {
                        writingSingleCharacters = false;
                        buildAWord += " "; }

                    // we are going to lookup the word in the compressed word list, if we throw an exception then we know it wasn't in the list
                    if (compressedWordRef.IsTalkingWord((int)tempByte))
                    {
                        string talkingWord = compressedWordRef.GetTalkingWord((int)tempByte);
                        useCompressedWord = true;
                        buildAWord += talkingWord;
                    }
                    // this is a bit lazy, but if I ask for a string that is not captured in the lookup map, then we know it's a special case
                    else
                    {
                        // oddly enough - we add an existing plain string that we have been building
                        // at the very last second
                        if (buildAWord != string.Empty)
                        {
                            talkScript.AddTalkCommand(TalkScript.TalkCommand.PlainString, buildAWord);
                            buildAWord = string.Empty;
                        }

                        // if the tempByte is within these boundaries then it is a label
                        // it is up to us to determine if it a Label or a GotoLabel
                        if (tempByte >= TalkScript.MIN_LABEL && tempByte <= TalkScript.MAX_LABEL)
                        {
                            // create an offset starting at 0 for label numbering
                            int offset = tempByte - TalkScript.MIN_LABEL;
                            // have I already seen a label once? Then the next label is a definition
                            if (labelsSeenList[offset] == true)
                            {
                                talkScript.AddTalkLabel(TalkScript.TalkCommand.DefineLabel, offset);
                            }
                            else
                            {
                                // the first time you see the label is a goto statement
                                talkScript.AddTalkLabel(TalkScript.TalkCommand.DefineLabel, offset);
                                //talkScript.AddTalkLabel(TalkScript.TalkCommand.GotoLabel, offset);
                                labelsSeenList[offset] = true;
                            }
                        }
                        else
                        {
                            // this is just a standard special command, so let's add it
                            talkScript.AddTalkCommand((TalkScript.TalkCommand)tempByte, string.Empty);
                        }
                    }
                    // we just used a compressed word which means we need to insert a space afterwards
                    if (useCompressedWord) {
                        buildAWord += " ";
                    }
                }
            }
            talkScript.InitScript();
            return talkScript;
        }
    }
}
