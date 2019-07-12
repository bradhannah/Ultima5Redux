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
        /// <summary>
        /// Represents a single script component
        /// </summary>
        protected internal class ScriptItem
        {
            /// <summary>
            /// 
            /// </summary>
            public TalkCommand Command { get; }
            public string Str { get; }
            public int LabelNum { get; }

            public ScriptItem(TalkCommand command) : this(command, string.Empty)
            {

            }

            /// <summary>
            /// Creates a label 
            /// </summary>
            /// <param name="command">a GotoLabel or DefineLabel</param>
            /// <param name="nLabelNum">number of the label</param>
            public ScriptItem(TalkCommand command, int nLabelNum)
            {
                Command = command;
                LabelNum = nLabelNum;
            }

            /// <summary>
            /// A talk command with an associated string
            /// </summary>
            /// <param name="command"></param>
            /// <param name="str"></param>
            public ScriptItem(TalkCommand command, string str)
            {
                Command = command;
                Str = str;
            }
        }

        /// <summary>
        /// Represents a single line of a script
        /// </summary>
        private class ScriptLine
        {
            private List<ScriptItem> scriptItems = new List<ScriptItem>();

            public void AddScriptItem(ScriptItem scriptItem)
            {
                scriptItems.Add(scriptItem);
            }
        }

        /// <summary>
        /// The default script line offsets for the static responses
        /// </summary>
        public enum TalkConstants { Name = 0, Description, Greeting, Job, Bye }

        /// <summary>
        /// Specific talk command
        /// </summary>
        public enum TalkCommand {PlainString = 0x00, AvatarsName = 0x81, EndCoversation = 0x82, Pause = 0x83, JoinParty = 0x84, Gold = 0x85, Change = 0x86, Or = 0x87, AskName = 0x88, KarmaPlusOne = 0x89,
            KarmaMinusOne = 0x8A, CallGuards = 0x8B, SetFlag = 0x8C, NewLine = 0x8D, Rune = 0x8E, KeyWait = 0x8F, DefaultMessage = 0x90, Unknown_Code = 0xA2, Unknown_Enter = 0x9F, GotoLabel = 0xFD, DefineLabel = 0xFE,
            Unknown_FF = 0xFF };

        /// <summary>
        ///  the minimum talk code for labels (in .tlk files)
        /// </summary>
        public const byte MIN_LABEL = 0x91;

        /// <summary>
        /// the maximum talk code for labels (in .tlk files)
        /// </summary>
        public const byte MAX_LABEL = 0x91 + 0x0A;

        /// <summary>
        /// total number of labels that are allowed to be defined
        /// </summary>
        public const int TOTAL_LABELS = 0x0A;

        // All of the ScriptLines
        private List<ScriptLine> scriptLines = new List<ScriptLine>();

        // tracking the current script line
        private ScriptLine currentScriptLine = new ScriptLine();

        /// <summary>
        /// Build the initial TalkScrit
        /// </summary>
        public TalkScript()
        {
            // let's add it immediately instead of waiting for someone to commit it
            // note; this will fail if the currentScriptLine is not a reference - but I'm pretty sure it is
            scriptLines.Add(currentScriptLine);
        }

        /// <summary>
        /// Move to the next line in the script
        /// </summary>
        public void NextLine()
        {
            currentScriptLine = new ScriptLine();
            scriptLines.Add(currentScriptLine);
        }

        /// <summary>
        /// Add a talk label. 
        /// </summary>
        /// <param name="talkCommand">Either GotoLabel or DefineLabel</param>
        /// <param name="nLabel">label # (0-9)</param>
        public void AddTalkLabel(TalkCommand talkCommand, int nLabel)
        {
            if (nLabel < 0 || nLabel > TOTAL_LABELS)
            {
                throw new Exception("Label Number: " + nLabel.ToString() + " is out of range");
            }
            if (talkCommand == TalkCommand.GotoLabel || talkCommand == TalkCommand.DefineLabel)
            {
                currentScriptLine.AddScriptItem(new ScriptItem(talkCommand, nLabel));
                //System.Console.Write("<" + (talkCommand.ToString() + " " + nLabel + ">"));
            }
            else 
            {
                throw new Exception("You passed a talk command that isn't a label! ");
            }
        }

        /// <summary>
        /// Add to the current script line, but no string associated
        /// For example: STRING <NEWLINE><AVATARNAME>
        /// </summary>
        /// <param name="talkCommand"></param>
        public void AddTalkCommand(TalkCommand talkCommand)
        {
            //System.Console.Write("<" + (talkCommand.ToString() + ">"));

            currentScriptLine.AddScriptItem(new ScriptItem(talkCommand, string.Empty));
        }

        /// <summary>
        /// Add to the current script line
        /// For example: STRING <NEWLINE><AVATARNAME>
        /// </summary>
        /// <param name="talkCommand"></param>
        /// <param name="talkStr"></param>
        public void AddTalkCommand(TalkCommand talkCommand, string talkStr)
        {
            if (talkCommand == TalkCommand.PlainString)
            {
                currentScriptLine.AddScriptItem(new ScriptItem(talkCommand, talkStr));
                //System.Console.Write(talkStr);
            }
            else
            {
                currentScriptLine.AddScriptItem(new ScriptItem(talkCommand));
                //System.Console.Write("<" + (talkCommand.ToString() + ">"));
            }
            currentScriptLine.AddScriptItem(new ScriptItem(talkCommand, talkStr));
        }
}

class TalkScripts
    {
        /// <summary>
        /// the mapping of NPC # to file .tlk file offset
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
        protected internal unsafe struct NPC_TalkOffset
        {
            public ushort npcIndex;
            public ushort fileOffset;
        }

        // map reference is key because NPC numbers can overlap
//        private Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, List<byte[]>> talkRefs =
//            new Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, List<byte[]>>(sizeof(SmallMapReference.SingleMapReference.SmallMapMasterFiles));

        private Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, Dictionary<int, byte[]>> talkRefs =
            new Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, Dictionary<int, byte[]>>(sizeof(SmallMapReference.SingleMapReference.SmallMapMasterFiles));

//        private Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, List<TalkScript>> talkScriptRefs = new Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, List<TalkScript>>();
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
                InitalizeTalkScripts(u5Directory, mapRef);
                
                // initialize and allocate the appropriately sized list of TalkScript(s)
                talkScriptRefs.Add(mapRef, new Dictionary<int, TalkScript>(talkRefs[mapRef].Count));

                // for each of the NPCs in the particular map, initialize the individual NPC talk script
//                for (int i = 0; i < talkRefs[mapRef].Count; i++)
                foreach (int key in talkRefs[mapRef].Keys)
                {
                    //                    talkScriptRefs[mapRef].Add(InitializeTalkScriptFromRaw(mapRef, i));
                    //talkScriptRefs[mapRef].Add(InitializeTalkScriptFromRaw(mapRef, key));
                    talkScriptRefs[mapRef][key] = InitializeTalkScriptFromRaw(mapRef, key);
                    System.Console.WriteLine("TalkScript in " + mapRef.ToString() + " with #" + key.ToString());
                }
            }
        }

        public TalkScript GetTalkScript(SmallMapReference.SingleMapReference.SmallMapMasterFiles smallMapRef, int nNPC)
        {
            //if (talkScriptRefs[smallMapRef][nNPC].IsSpecialDialogType())
            if (NonPlayerCharacters.NonPlayerCharacter.IsSpecialDialogType((NonPlayerCharacters.NonPlayerCharacter.NPCDialogTypeEnum)nNPC))
            { return null; }
            return (talkScriptRefs[smallMapRef][nNPC]);
        }

        /// <summary>
        /// Initlializes the talk scripts into a fairly raw byte[] format
        /// </summary>
        /// <param name="u5Directory">directory of Ultima 5 data files</param>
        /// <param name="mapMaster">the small map reference (helps pick *.tlk file)</param>
        private void InitalizeTalkScripts(string u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles mapMaster)
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

                        //                  npcOffsets.Add((NPC_TalkOffset)Utils.ReadStruct(talkByteList, 2 + i, typeof(NPC_TalkOffset)));
                        //                    Console.WriteLine("NPC #" + npcOffsets.Last().npcIndex + " at offset " + npcOffsets.Last().fileOffset + " in file " + talkFilename);
                        // OMG I'm tired.. figure out why this isn't printing properly....
                        Console.WriteLine("NPC #" + npcOffsets[talkOffset.npcIndex] + " at offset " + npcOffsets[talkOffset.npcIndex].fileOffset + " in file " + talkFilename);
                    }
                }
                // you are in a single file right now
                // repeat for every single NPC in the file
                int count = 1;
                foreach (int key in npcOffsets.Keys)
//                    for (int i = 0; i < nEntries; i++)
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
                    //if (i  < nEntries)
                    //{
                    //    // if it is not the last entry, then calculate the length from the current to the next offset
                    //    chunkLength = npcOffsets[i + 1].fileOffset - npcOffsets[i].fileOffset;
                    //}
                    //else
                    //{
                    //    // else if you are on the last entry, then we use the file size 
                    //    // note: probably could have used the talkByteList.length
                    //    chunkLength = talkFileSize - npcOffsets[i].fileOffset;
                    //}

                    byte[] chunk = new byte[chunkLength];

                    // copy only the bytes from the offset
                    talkByteList.CopyTo(npcOffsets[key].fileOffset, chunk, 0, (int)chunkLength);
                    // Add the raw bytes to the specific Map+NPC#
//                    talkRefs[mapMaster].Add(chunk); // have to make an assumption that the values increase 1 at a time, this should be true though
                    talkRefs[mapMaster].Add(key, chunk); // have to make an assumption that the values increase 1 at a time, this should be true though

                }
            }
        }


        /// <summary>
        /// Intializes an individual TalkingScript using the raw data created from InitalizeTalkScripts
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
//                    try
                    if (compressedWordRef.IsTalkingWord((int)tempByte))
                    {
                        string talkingWord = compressedWordRef.GetTalkingWord((int)tempByte);
                      //  Console.Write(talkingWord);
                        useCompressedWord = true;
                        buildAWord += talkingWord;
                    }
                    // this is a bit lazy, but if I ask for a string that is not captured in the lookup map, then we know it's a special case
                    //catch (CompressedWordReference.NoTalkingWordException)
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
                                talkScript.AddTalkLabel(TalkScript.TalkCommand.GotoLabel, offset);
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
            return talkScript;
        }
    }
}
