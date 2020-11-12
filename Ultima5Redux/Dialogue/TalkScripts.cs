using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.Dialogue
{
    /// <summary>
    ///     TalkScripts represents all of the in game talking scripts for all NPCs
    /// </summary>
    public class TalkScripts
    {
        /// <summary>
        ///     when you must adjust the offset into the compressed word lookup, subtract this
        /// </summary>
        private const int TALK_OFFSET_ADJUST = 0x80;

        /// <summary>
        ///     a null byte signifies the end of the script line
        /// </summary>
        private const byte END_OF_SCRIPTLINE_BYTE = 0x00;

        /// <summary>
        ///     do I print Debug output to the Console
        /// </summary>
        private readonly bool _bIsDebug = false;

        // all of the compressed words that are referenced in the .tlk files
        private readonly CompressedWordReference _compressedWordRef;

        /// <summary>
        ///     Dictionary that refers to the raw bytes for each NPC based on Map master file and NPC index
        /// </summary>
        private readonly Dictionary<SmallMapReferences.SingleMapReference.SmallMapMasterFiles, Dictionary<int, byte[]>>
            _talkRefs =
                new Dictionary<SmallMapReferences.SingleMapReference.SmallMapMasterFiles, Dictionary<int, byte[]>>(
                    sizeof(SmallMapReferences.SingleMapReference.SmallMapMasterFiles));

        /// <summary>
        ///     Dictionary that refers to the fully interpreted TalkScripts for each NPC based on Master map file and NPC index
        /// </summary>
        private readonly
            Dictionary<SmallMapReferences.SingleMapReference.SmallMapMasterFiles, Dictionary<int, TalkScript>>
            _talkScriptRefs =
                new Dictionary<SmallMapReferences.SingleMapReference.SmallMapMasterFiles, Dictionary<int, TalkScript>
                >();

        /// <summary>
        ///     Build the talk scripts
        /// </summary>
        /// <param name="u5Directory">Directory with Ultima 5 data files</param>
        /// <param name="dataRef">DataOVL Reference provides compressed word details</param>
        public TalkScripts(string u5Directory, DataOvlReference dataRef)
        {
            // save the compressed words, we're gonna need them
            _compressedWordRef = new CompressedWordReference(dataRef);

            // just a lazy array that is easier to enumerate than the enum
            SmallMapReferences.SingleMapReference.SmallMapMasterFiles[] smallMapRefs =
            {
                SmallMapReferences.SingleMapReference.SmallMapMasterFiles.Castle,
                SmallMapReferences.SingleMapReference.SmallMapMasterFiles.Towne,
                SmallMapReferences.SingleMapReference.SmallMapMasterFiles.Keep,
                SmallMapReferences.SingleMapReference.SmallMapMasterFiles.Dwelling
            };

            // for each of the maps we are going to initialize
            foreach (SmallMapReferences.SingleMapReference.SmallMapMasterFiles mapRef in smallMapRefs)
            {
                // initialize the raw component of the talk scripts
                InitializeTalkScriptsRaw(u5Directory, mapRef);

                // initialize and allocate the appropriately sized list of TalkScript(s)
                _talkScriptRefs.Add(mapRef, new Dictionary<int, TalkScript>(_talkRefs[mapRef].Count));

                // for each of the NPCs in the particular map, initialize the individual NPC talk script
                foreach (int key in _talkRefs[mapRef].Keys)
                {
                    _talkScriptRefs[mapRef][key] = InitializeTalkScriptFromRaw(mapRef, key);
                    if (_bIsDebug) Console.WriteLine(@"TalkScript in " + mapRef + @" with #" + key);
                }
            }
        }

        public TalkScript GetTalkScript(SmallMapReferences.SingleMapReference.SmallMapMasterFiles smallMapRef, int nNPC)
        {
            if (NonPlayerCharacterReference.IsSpecialDialogType((NonPlayerCharacterReference.NPCDialogTypeEnum) nNPC)
            ) return null;
            return _talkScriptRefs[smallMapRef][nNPC];
        }

        /// <summary>
        ///     Initializes the talk scripts into a fairly raw byte[] format
        /// </summary>
        /// <param name="u5Directory">directory of Ultima 5 data files</param>
        /// <param name="mapMaster">the small map reference (helps pick *.tlk file)</param>
        private void InitializeTalkScriptsRaw(string u5Directory,
            SmallMapReferences.SingleMapReference.SmallMapMasterFiles mapMaster)
        {
            // example NPC 1 at Castle in Lord British's castle
            // C1 EC  E9 F3 F4 E1 E9 F2 01 C2 E1 F2 E4 00
            // 65 108  69 

            string talkFilename = Path.Combine(u5Directory,
                SmallMapReferences.SingleMapReference.GetTlkFilenameFromMasterFile(mapMaster));

            // the raw bytes of the talk file
            List<byte> talkByteList = Utils.GetFileAsByteList(talkFilename);

            // need this to make sure we don't fall of the end of the file when we read it
            FileInfo fi = new FileInfo(talkFilename);
            long talkFileSize = fi.Length;

            // keep track of the NPC to file offset mappings
            //List<NPC_TalkOffset> npcOffsets;

            // the first word in the talk file tells you how many characters are referenced in script
            int nEntries = Utils.LittleEndianConversion(talkByteList[0], talkByteList[1]);

            //talkRefs.Add(mapMaster, new List<byte[]>(nEntries));
            _talkRefs.Add(mapMaster, new Dictionary<int, byte[]>(nEntries));

            // a list of all the offsets
            //npcOffsets = new List<NPC_TalkOffset>(nEntries);
            Dictionary<int, NPCTalkOffset> npcOffsets = new Dictionary<int, NPCTalkOffset>(nEntries);

            unsafe
            {
                // you are in a single file right now
                for (int i = 0; i < nEntries * sizeof(NPCTalkOffset); i += sizeof(NPCTalkOffset))
                {
                    // add 2 because we know we are starting at an offset
                    NPCTalkOffset talkOffset =
                        (NPCTalkOffset) Utils.ReadStruct(talkByteList, 2 + i, typeof(NPCTalkOffset));
                    npcOffsets[talkOffset.npcIndex] = talkOffset;

                    // OMG I'm tired.. figure out why this isn't printing properly....
                    if (_bIsDebug)
                        Console.WriteLine(@"NPC #" + npcOffsets[talkOffset.npcIndex].npcIndex + @" at offset " +
                                          npcOffsets[talkOffset.npcIndex].fileOffset + @" in file " + talkFilename);
                }

                // you are in a single file right now
                // repeat for every single NPC in the file
                int count = 1;
                foreach (int key in npcOffsets.Keys)
                {
                    long chunkLength; // didn't want a long, but the file size is long...

                    // calculate the offset size
                    if (count < npcOffsets.Keys.Count)
                        chunkLength = npcOffsets[key + 1].fileOffset - npcOffsets[key].fileOffset;
                    else
                        chunkLength = talkFileSize - npcOffsets[key].fileOffset;

                    count++;

                    byte[] chunk = new byte[chunkLength];

                    // copy only the bytes from the offset
                    talkByteList.CopyTo(npcOffsets[key].fileOffset, chunk, 0, (int) chunkLength);
                    // Add the raw bytes to the specific Map+NPC#
                    _talkRefs[mapMaster]
                        .Add(key,
                            chunk); // have to make an assumption that the values increase 1 at a time, this should be true though
                }
            }
        }


        /// <summary>
        ///     Initializes an individual TalkingScript using the raw data created from InitializeTalkScriptsRaw
        ///     <remark>May God have mercy on my soul if I ever need to debug or troubleshoot this again.</remark>
        /// </summary>
        /// <param name="smallMapRef">the small map reference</param>
        /// <param name="index">NPC Index</param>
        private TalkScript InitializeTalkScriptFromRaw(
            SmallMapReferences.SingleMapReference.SmallMapMasterFiles smallMapRef, int index)
        {
            TalkScript talkScript = new TalkScript(); // the script we are building and will return

            List<bool>
                labelsSeenList =
                    new List<bool>(TalkScript.TOTAL_LABELS); // keeps track of the labels we have already seen
            labelsSeenList.AddRange(Enumerable.Repeat(false,
                TalkScript.TOTAL_LABELS)); // creates a list of "false" bools to set the default labelsSeenList

            bool writingSingleCharacters = false; // are we currently writing a single character at a time?
            string
                buildAWord = string.Empty; // the word we are currently building if we are writingSingleCharacters=true

            int nGoldCharsLeft = 0;

            foreach (byte byteWord in _talkRefs[smallMapRef][index])
            {
                // if a NULL byte is provided then you need to go the next line, resetting the writingSingleCharacters so that a space is not inserted next line
                // bh: Sept 21, 2019 - had to add a disgusting hack to account for what appears to be broken
                //   data or a misunderstood algorithm - they seem to have an end of script line in the middle of a response
                //   there could be a rule I simply don't understand, but for now - i hack 
                if (byteWord == END_OF_SCRIPTLINE_BYTE && !buildAWord.EndsWith("to give unto charity!"))
                {
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

                byte tempByte = byteWord; // this is the byte that we will manipulate, leaving the byteWord in tact
                bool usePhraseLookup = false; // did we do a phrase lookup (else we are typing single letters)
                bool useCompressedWord = false; // did we successfully use a compressed word?

                // if it's one of the bytes that requires a subtraction of 0x80 (128)
                if (byteWord >= 165 && byteWord <= 218)
                    tempByte -= TALK_OFFSET_ADJUST;
                else if (byteWord >= 225 && byteWord <= 250)
                    tempByte -= TALK_OFFSET_ADJUST;
                else if (byteWord >= 160 && byteWord <= 161)
                    tempByte -= TALK_OFFSET_ADJUST;
                else
                    // it didn't match which means that it's one the special phrases and we will perform a lookup
                    usePhraseLookup = true;

                // Debug code to help track down particular codes being written
                // if (tempByte == 119) { Console.Write("");  }
                // it wasn't a special phrase which means that the words are being typed one word at a time
                if (!usePhraseLookup)
                {
                    // I'm writing single characters, we will keep track so that when we hit the end we can insert a space
                    writingSingleCharacters = true;
                    // this signifies the end of the printing (sample code enters a newline)
                    if ((char) tempByte == '@')
                        //Console.WriteLine("");
                        continue;
                    //Console.Write((char)tempByte);
                    buildAWord += (char) tempByte;
                    if (nGoldCharsLeft > 0 && --nGoldCharsLeft == 0)
                    {
                        talkScript.AddTalkCommand(TalkScript.TalkCommand.PlainString, buildAWord);
                        buildAWord = string.Empty;
                    }

                    // Debug code to help track down an NPCs question or response
                    //if (buildAWord.Contains("to give unto charity")) { Console.Write(""); }
                }
                else // usePhraseLookup = true      
                {
                    // We were instructed to perform a lookup, either a compressed word lookup, or a special character

                    // if we were previously writing single characters, but have moved onto lookups, then we add a space, and reset it
                    if (writingSingleCharacters)
                    {
                        writingSingleCharacters = false;
                        buildAWord += " ";
                    }

                    // we are going to lookup the word in the compressed word list, if we throw an exception then we know it wasn't in the list
                    if (_compressedWordRef.IsTalkingWord(tempByte))
                    {
                        string talkingWord = _compressedWordRef.GetTalkingWord(tempByte);
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
                            if (labelsSeenList[offset])
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
                            talkScript.AddTalkCommand((TalkScript.TalkCommand) tempByte, string.Empty);
                            if ((TalkScript.TalkCommand) tempByte == TalkScript.TalkCommand.Gold)
                                // here are always three extra characters after the gold, but only for gold
                                // so we make sure we only capture the next 3 characters
                                nGoldCharsLeft = 3;
                        }
                    }

                    // we just used a compressed word which means we need to insert a space afterwards
                    if (useCompressedWord) buildAWord += " ";
                }
            }

            talkScript.InitScript();
            return talkScript;
        }


        /// <summary>
        ///     the mapping of NPC # to file .tlk file offset
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
        private readonly struct NPCTalkOffset
        {
            public readonly ushort npcIndex;
            public readonly ushort fileOffset;
        }
    }
}