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
        protected internal class ScriptItem
        {
            public TalkCommand Command { get; }
            public string Str { get; }
            public int LabelNum { get; }

            public ScriptItem(TalkCommand command, int nLabelNum)
            {
                Command = command;
                LabelNum = nLabelNum;
            }

            public ScriptItem(TalkCommand command, string str)
            {
                Command = command;
                Str = str;
            }
        }

        private class ScriptLine
        {
            private List<ScriptItem> scriptItems = new List<ScriptItem>();

            public void AddScriptItem(ScriptItem scriptItem)
            {
                scriptItems.Add(scriptItem);
            }
        }

        public enum TalkCommand {PlainString = 0x00, AvatarsName = 0x81, EndCoversation = 0x82, Pause = 0x83, JoinParty = 0x84, Gold = 0x85, Change = 0x86, Or = 0x87, AskName = 0x88, KarmaPlusOne = 0x89,
            KarmaMinusOne = 0x8A, CallGuards = 0x8B, SetFlag = 0x8C, NewLine = 0x8D, Rune = 0x8E, KeyWait = 0x8F, DefaultMessage = 0x90, Unknown_Code = 0xA2, Unknown_Enter = 0x9F, GotoLabel = 0xFD, DefineLabel = 0xFE,
            Unknown_FF = 0xFF };

        public const byte MIN_LABEL = 0x91;
        public const byte MAX_LABEL = 0x91 + 0x0A;
        public const int TOTAL_LABELS = 0x0A;

        //private TalkScript talkScript = new TalkScript();
        private List<ScriptLine> scriptLines = new List<ScriptLine>();

        private ScriptLine currentScriptLine = new ScriptLine();

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

        public void AddTalkLabel(TalkCommand talkCommand, int nLabel)
        {
            if (talkCommand == TalkCommand.GotoLabel || talkCommand == TalkCommand.DefineLabel)
            {
                currentScriptLine.AddScriptItem(new ScriptItem(talkCommand, nLabel));
                System.Console.Write("<" + (talkCommand.ToString() + " " + nLabel + ">"));
            }
            else 
            {
                throw new Exception("You passed a talk command that isn't a label! ");
            }
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
                System.Console.Write(talkStr);
            }
            else
            {
                System.Console.Write("<" + (talkCommand.ToString() + ">"));
            }
            currentScriptLine.AddScriptItem(new ScriptItem(talkCommand, talkStr));
        }
}

class TalkScripts
    {
        //List<TalkScript> talkScripts = new List<TalkScript>();

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
        private const int TALK_OFFSET_ADJUST = 0x80;
        private const byte END_OF_SCRIPTLINE_BYTE = 0x00;

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
            SmallMapReference.SingleMapReference.SmallMapMasterFiles[] smallMapRefs =
            {
                SmallMapReference.SingleMapReference.SmallMapMasterFiles.Castle,
                SmallMapReference.SingleMapReference.SmallMapMasterFiles.Towne,
                SmallMapReference.SingleMapReference.SmallMapMasterFiles.Keep,
                SmallMapReference.SingleMapReference.SmallMapMasterFiles.Dwelling
            };

            foreach (SmallMapReference.SingleMapReference.SmallMapMasterFiles mapRef in smallMapRefs)
            {
                InitalizeTalkScripts(u5Directory, mapRef);
                for (int i = 0; i < talkRefs[mapRef].Count; i++)
                {
                    InitializeTalkScriptsFromRaw(mapRef, i);
                }
            }
        }

        public void InitializeTalkScriptsFromRaw (SmallMapReference.SingleMapReference.SmallMapMasterFiles smallMapRef, int index)
        {
            bool writingSingleCharacters = false;
            List<bool> labelsSeenList = new List<bool>(TalkScript.TOTAL_LABELS);
            labelsSeenList.AddRange(Enumerable.Repeat(false, TalkScript.TOTAL_LABELS));

            TalkScript talkScript = new TalkScript();
            string buildAWord = string.Empty;

            foreach (byte byteWord in talkRefs[smallMapRef][index])
            {
                // if a NULL byte is provided then you need to go the next line, resetting the writingSingleCharacters so that a space is not inserted next line
                if (byteWord == END_OF_SCRIPTLINE_BYTE) {
                    buildAWord += "\n";
                    talkScript.AddTalkCommand(TalkScript.TalkCommand.PlainString, buildAWord);
                    buildAWord = string.Empty;
                    talkScript.NextLine();
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
                    try
                    {
                        string talkingWord = talkRef.GetTalkingWord((int)tempByte);
                      //  Console.Write(talkingWord);
                        useCompressedWord = true;
                        buildAWord += talkingWord;
                    }
                    // this is a bit lazy, but if I ask for a string that is not captured in the lookup map, then we know it's a special case
                    catch (TalkingReferences.NoTalkingWordException)
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
                            talkScript.AddTalkCommand((TalkScript.TalkCommand)tempByte, string.Empty);
                        }
                    }
                    // we just used a compressed word which means we need to insert a space afterwards
                    if (useCompressedWord) {
                        buildAWord += " ";
                    }
                }
            }
        }
    }
}
