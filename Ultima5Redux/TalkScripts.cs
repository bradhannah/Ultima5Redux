using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Ultima5Redux
{
    class TalkScript
    {
        protected internal class ScriptTalkLabels
        {
            public List<ScriptTalkLabel> Labels { get; }

            public void AddLabel(ScriptTalkLabel talkLabel)
            {
                Labels.Add(talkLabel);
            }

            public ScriptTalkLabels()
            {
                Labels = new List<ScriptTalkLabel>();
            }
        }


        protected internal class ScriptTalkLabel
        {
            public ScriptQuestionAnswers QuestionAnswers { get;  }
            public ScriptLine InitialLine { get; set; }
            public List<ScriptLine> DefaultAnswers { get; set; }
            public int LabelNum { get;  }

            public void AddScriptQuestionAnswer(ScriptQuestionAnswer sqa)
            {
                QuestionAnswers.Add(sqa);
            }

            public ScriptTalkLabel (int labelNum, ScriptLine initialLine, List<ScriptLine> defaultAnswers, ScriptQuestionAnswers sqa)
            {
                QuestionAnswers = sqa;
                InitialLine = initialLine;
                if (defaultAnswers == null)
                {
                    DefaultAnswers = new List<ScriptLine>();
                }
                else
                {
                    DefaultAnswers = DefaultAnswers;
                }
                LabelNum = labelNum;
            }

            public ScriptTalkLabel(int labelNum, ScriptLine initialLine) : this(labelNum, initialLine, null, new ScriptQuestionAnswers())
            {
            }

            //public ScriptTalkLabels (int labelNum) : this (labelNum, null, null, new ScriptQuestionAnswers())
            //{
            //}
        }



        /// <summary>
        /// Collection of questions and answers, makes accessing them much easier
        /// </summary>
        protected internal class ScriptQuestionAnswers
        {
            public Dictionary<string, ScriptQuestionAnswer> QuestionAnswers { get; }

            public ScriptQuestionAnswer GetQuestionAnswer(string question)
            {
                return QuestionAnswers[question];
            }

            public string[] GetScriptQuestions()
            {

                return QuestionAnswers.Keys.ToArray();
            }

            public ScriptQuestionAnswers()
            {
                QuestionAnswers = new Dictionary<string, ScriptQuestionAnswer>();
            }

            public List<ScriptLine> GetAnswers()
            {
                List<ScriptLine> answers = new List<ScriptLine>();
                foreach (ScriptQuestionAnswer sqa in QuestionAnswers.Values)
                {
                    if (!answers.Contains(sqa.Answer))
                    {
                        answers.Add(sqa.Answer);
                    }
                }
                return answers;
            }

            public void Add (ScriptQuestionAnswer sqa)
            {
                if (sqa.questions == null)
                    return;

                foreach (string question in sqa.questions)
                {
                    if (!QuestionAnswers.Keys.Contains(question.Trim()))
                    {
                        QuestionAnswers.Add(question.Trim(), sqa);
                    }
                }
            }

            public void Print()
            {
                Dictionary<ScriptQuestionAnswer, bool> seenAnswers = new Dictionary<ScriptQuestionAnswer, bool>();

                foreach (ScriptQuestionAnswer sqa in QuestionAnswers.Values)
                {
                    if (seenAnswers.ContainsKey(sqa)) continue;
                    seenAnswers.Add(sqa, true);

                    bool first = true;
                    foreach (string question in sqa.questions.ToArray())
                    {
                        if (first) { first = false; Console.Write("Questions: " + question); }
                        else { Console.Write(" <OR> " + question); }
                    }
                    Console.WriteLine("");
                    Console.WriteLine("Answer: " + sqa.Answer.ToString());

                }
            }
        }


        /// <summary>
        /// A single instance of a question and answer for dialog
        /// </summary>
        protected internal class ScriptQuestionAnswer
        {
            public ScriptLine Answer { get; }
            public List<string> questions { get; }

            public ScriptQuestionAnswer(List<string> questions, ScriptLine answer)
            {
                this.questions = questions;
                Answer = answer;
            }
        }



        /// <summary>
        /// Represents a single script component
        /// </summary>
        protected internal class ScriptItem
        {
            /// <summary>
            /// command issued
            /// </summary>
            public TalkCommand Command { get; }
            /// <summary>
            /// Associated string (can be empty)
            /// </summary>
            public string Str { get { return str.Trim(); } }
            /// <summary>
            /// If there is a label, then this is a zero based index
            /// </summary>
            public int LabelNum { get; }

            private string str = string.Empty;

            static public bool IsQuestion(string str)
            {
                // if the string is:
                // 1 to 6 characters
                // AND doesn't contain spaces

                return (str.Trim().Length <= 6 && str.Trim().Length >= 1 && !str.Contains(" "));

                // there are some answers that are capitalized...
                //&& (Str.ToLower() == Str));
            }

            /// <summary>
            /// is this script item a question that the player asks an NPC?
            /// </summary>
            /// <returns></returns>
            public bool IsQuestion()
            {
                return ScriptItem.IsQuestion(Str);
            }

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
                this.str = str;
            }
        }

        /// <summary>
        /// Represents a single line of a script
        /// </summary>
        protected internal class ScriptLine
        {
            private List<ScriptItem> scriptItems = new List<ScriptItem>();

            public override string ToString()
            {
                string scriptLine = string.Empty;

                foreach (ScriptItem item in this.scriptItems)
                {
                    if (item.Command == TalkCommand.PlainString)
                    {
                        scriptLine += item.Str.Trim();
                    }
                    else
                    {
                        if (item.Command == TalkCommand.DefineLabel || item.Command == TalkCommand.GotoLabel)
                        {
                            scriptLine += ("<" + item.Command.ToString() + item.LabelNum.ToString() + ">");
                        }
                        else
                        {
                            scriptLine += ("<" + item.Command.ToString() + ">");
                        }
                    }
                }
                return scriptLine;
            }

            public bool IsQuestion()
            {
                return this.GetScriptItem(0).IsQuestion();
            }

            public bool IsEndOfLabelSection()
            {
                if (GetScriptItem(0).Command == TalkCommand.DefaultMessage && GetScriptItem(1).Command == TalkCommand.Unknown_Enter)
                {
                    return true;
                }
                return false;
            }

            public bool IsLabelDefinition()
            {
                if (GetScriptItem(0).Command == TalkCommand.DefaultMessage && GetScriptItem(1).Command == TalkCommand.DefineLabel)
                {
                    return true;
                }
                return false;
            }


            public void AddScriptItem(ScriptItem scriptItem)
            {
                scriptItems.Add(scriptItem);
            }

            public int GetNumberOfScriptItems()
            {
                return scriptItems.Count;
            }

            public ScriptItem GetScriptItem(int index)
            {
                Debug.Assert(scriptItems[index] != null);
                return scriptItems[index];
            }

            /// <summary>
            /// Determines if a particular talk command is present in a script line
            /// <remarks>This particularly helpful when looking for looking for <AvatarName></remarks>
            /// </summary>
            /// <param name="command">the command to search for</param>
            /// <returns>true if it's present, false if it isn't</returns>
            public bool ContainsCommand(TalkCommand command)
            {
                for (int nItem = 0; nItem < scriptItems.Count; nItem++)
                {
                    if (scriptItems[nItem].Command == command)
                        return true;
                }
                return false;
            }

        }

        /// <summary>
        /// Is the command only a simple or dynamic string?
        /// </summary>
        /// <param name="command">the command to evaluate</param>
        /// <returns>true if it is a string command only</returns>
        static public bool IsStringOnlyCommand (TalkCommand command)
        {
            if (command == TalkCommand.PlainString || command == TalkCommand.AvatarsName || command == TalkCommand.NewLine || command == TalkCommand.Rune)
                return true;

            return false;
        }

        /// <summary>
        /// Specific talk command
        /// </summary>
        public enum TalkCommand {PlainString = 0x00, AvatarsName = 0x81, EndCoversation = 0x82, Pause = 0x83, JoinParty = 0x84, Gold = 0x85, Change = 0x86, Or = 0x87, AskName = 0x88, KarmaPlusOne = 0x89,
            KarmaMinusOne = 0x8A, CallGuards = 0x8B, SetFlag = 0x8C, NewLine = 0x8D, Rune = 0x8E, KeyWait = 0x8F, DefaultMessage = 0x90, Unknown_CodeA2 = 0xA2, Unknown_Enter = 0x9F, GotoLabel = 0xFD, DefineLabel = 0xFE,
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

        /// <summary>
        /// Script talk labels contain all the labels, their q&a and default responses
        /// </summary>
        private ScriptTalkLabels scriptTalkLabels = new ScriptTalkLabels();
        
        /// <summary>
        /// Non label specific q&a 
        /// </summary>
        private ScriptQuestionAnswers scriptQuestionAnswers = new ScriptQuestionAnswers();

        // tracking the current script line
        private ScriptLine currentScriptLine = new ScriptLine();

        private const int endBaseIndexes = 4; // the end index for the base (TalkConstants)
        //private int endTextIndexes;

        /// <summary>
        /// The default script line offsets for the static responses
        /// </summary>
        public enum TalkConstants { Name = 0, Description, Greeting, Job, Bye }

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
        /// Move to the next line in the script (for adding new content)
        /// </summary>
        public void NextLine()
        {
            currentScriptLine = new ScriptLine();
            scriptLines.Add(currentScriptLine);
        }


        /// <summary>
        /// After adding all elements, this will process the script into a more readable format
        /// </summary>
        public void InitScript()
        {
            int nIndex = endBaseIndexes + 1;
            bool labelEncountered = false;

            string question;

            
            // repeat through the question/answer components until we hit a label - then we know to move onto the label section
            do
            {
                //ScriptQuestionAnswers sqas = new ScriptQuestionAnswers();
                

                List<string> currQuestions = new List<string>();
                ScriptLine line = scriptLines[nIndex];

                if (line.GetScriptItem(0).Command == TalkCommand.DefaultMessage)
                {
                    labelEncountered = true;
                    break;
                }

                //Debug.Assert(line.GetNumberOfScriptItems() == 1);

                // first time around we KNOW there is a first question
                question = line.GetScriptItem(0).Str;
                // dumb little thing - there are some scripts that have the same keyword multiple times
                // the game favours the one it sees first (see "Camile" in West Brittany as an example)
                if (!scriptQuestionAnswers.QuestionAnswers.ContainsKey(question))
                    currQuestions.Add(question);

                // if we peek ahead and the next command is an <or> then we will just skip it and continue to add to the questions list
                //if (scriptLines[nIndex+1].ContainsCommand(TalkCommand.Or))
                while (scriptLines[nIndex + 1].ContainsCommand(TalkCommand.Or))
                {
                    nIndex += 2;
                    line = scriptLines[nIndex];
                    question = line.GetScriptItem(0).Str;
                    // just in case they try to add the same question twice - this is kind of a bug in the data since the game just favours the first question it sees
                    if (!scriptQuestionAnswers.QuestionAnswers.ContainsKey(question))
                    {
                        currQuestions.Add(question);
                    }
                }

                ScriptLine nextLine = scriptLines[nIndex + 1];
                scriptQuestionAnswers.Add(new ScriptQuestionAnswer(currQuestions, nextLine));
                nIndex+=2;
            } while (labelEncountered == false);

            // time to process labels!! the nIndex that the previous routine left with is the beginning of the label section
            int count = 0;
            do // begin the label processing loop - pretty sure this is dumb and doesn't do anything - but everything messes up when I remove it
            {
                // this is technically a loop, but it should never actually loop. Kind of dumb, but fragile 
                Debug.Assert(count++ == 0);

                ScriptLine line = scriptLines[nIndex];
                ScriptLine nextLine;

                // if there are two script items, and those two script items identify an end of label section then let's break out
                // this should only actually occur if there are no labels at all
                if (line.GetNumberOfScriptItems() == 2 && line.IsEndOfLabelSection())
                {
                    // all done. we either had no labels or reached the end of them
                    // assert that we are on the last line of the script
                    Debug.Assert(nIndex == scriptLines.Count - 1);
                    break;
                }


                // i expect that this line will always indicate a new label is being defined
                Debug.Assert(line.GetScriptItem(0).Command == TalkCommand.DefaultMessage);

                // I don't like this, it's inelegant, but it works...
                // at this point we know:
                // This is a multi line message 
                bool nextCommandDefaultMessage = false;
               
                do // called for each label #
                {
                    // Debug code for narrowing down to a single NPC
                    //if (scriptLines[0].GetScriptItem(0).Str.ToLower().Trim() == "sutek".ToLower())
                    //if (scriptLines[0].GetScriptItem(0).Str.ToLower().Trim() == "sir arbuthnot")
                    //{
                    //        Console.WriteLine("AH");
                    //}

                    line = scriptLines[nIndex];

                    // let's make sure there are actually labels to look at
                    if (line.IsEndOfLabelSection())
                    {
                        nextCommandDefaultMessage = true;
                        break;
                    }

                    // create the shell for the label
                    ScriptTalkLabel scriptTalkLabel = new ScriptTalkLabel(line.GetScriptItem(1).LabelNum, line);

                    // save the new label to the label collection
                    scriptTalkLabels.AddLabel(scriptTalkLabel);

                    // it's a single line only, so we skip this tom foolery below
                    if (scriptLines[nIndex + 1].GetScriptItem(0).Command == TalkCommand.DefaultMessage)
                    {
                        // do nothing, the ScriptTalkLabel will simply have no DefaultAnswer indicating that only the primary label line is read

                        nIndex++;
                        continue;
                    }

                    // with a single answer below the label, we will always use the default answer
                    ScriptLine defaultAnswer = scriptLines[++nIndex];
                    scriptTalkLabel.DefaultAnswers.Add(defaultAnswer);

                    // it's a default only answer, and no additional line of dialog, then we skip this tom foolery below 
                    if (scriptLines[nIndex + 1].GetScriptItem(0).Command == TalkCommand.DefaultMessage)
                    {
                        nIndex++;
                        continue;
                    }

                    do // go through the question/answer and <or>
                    {
                        // Debug code to stop at given index
                        //if (nIndex == 22) { Console.WriteLine(""); }

                        List<string> currQuestions = new List<string>();
                        // if the next line is an <or> then process the <or> 
                        if (scriptLines[nIndex + 2].ContainsCommand(TalkCommand.Or))
                        {
                            while (scriptLines[nIndex + 2].ContainsCommand(TalkCommand.Or))
                            {
                                line = scriptLines[nIndex + 1];
                                Debug.Assert(line.IsQuestion());
                                question = line.GetScriptItem(0).Str;
                                // just in case they try to add the same question twice - this is kind of a bug in the data since the game just favours the first question it sees
                                if (!scriptQuestionAnswers.QuestionAnswers.ContainsKey(question))
                                {
                                    currQuestions.Add(question);
                                }
                                nIndex += 2;
                            }
                            line = scriptLines[++nIndex];
                            Debug.Assert(line.IsQuestion());
                            question = line.GetScriptItem(0).Str;
                            // just in case they try to add the same question twice - this is kind of a bug in the data since the game just favours the first question it sees
                            if (!scriptQuestionAnswers.QuestionAnswers.ContainsKey(question))
                            {
                                currQuestions.Add(question);
                            }
                        }
                        // is this a question that the player would ask an NPC?
                        else if (scriptLines[nIndex + 1].GetScriptItem(0).IsQuestion())
                        {
                            // get the Avater's response line
                            line = scriptLines[++nIndex];

                            question = line.GetScriptItem(0).Str;
                            Debug.Assert(ScriptItem.IsQuestion(question));
                            currQuestions.Add(question);
                        }
                        // the NPC has tricked me - this is a second line of dialog for the given 
                        /// that dasterdly LB has put an extra response line in....
                        else //if (scriptLines[nIndex + 1].GetScriptItem(0).Str.Trim().Length > 4)
                        {
                            line = scriptLines[++nIndex];
                            Debug.Assert(!line.IsQuestion());
                            scriptTalkLabel.DefaultAnswers.Add(line);
                            nIndex++;
                            // let's make double sure that we only have a single additional line of text 
                            Debug.Assert(scriptLines[nIndex].GetScriptItem(0).Command == TalkCommand.DefaultMessage);

                            nextLine = scriptLines[nIndex];
                            continue;
                        }

                        // get your answer and store it
                        ScriptLine npcResponse = scriptLines[++nIndex];
                        // we are ready to create a Q&A object and add it the label specific Q&A script
                        scriptTalkLabel.AddScriptQuestionAnswer(new ScriptQuestionAnswer(currQuestions, npcResponse));

                        // we are at the end of the label section of the file, so we are done.
                        nextLine = scriptLines[++nIndex];

                        // does the next line indicate end of all of the label sections, then let's get out of this loop
                        if (nextLine.IsEndOfLabelSection())
                        {
                            nIndex--;
                            nextCommandDefaultMessage = true;
                            break;
                        }
                        // is the next line a label definition? is so, let's exit this label and move on
                        if (!nextLine.IsLabelDefinition())
                        {
                            nIndex--;
                            continue;
                        }
       

                    } while (nextLine.GetScriptItem(0).Command != TalkCommand.DefaultMessage);

                } while (!nextCommandDefaultMessage);


                // still to do... read the labels and organize them carefully
                // still to do... save the ScriptQuestionAnswers object for future use.. THIS IS SO MUCH BETTER!
                nIndex++;
            } while (nIndex < (scriptLines.Count - 1));
        }

        /// <summary>
        /// Get the script line based on the specified Talk Constant allowing to quickly access "name", "job" etc.
        /// This is not compatible with Labels
        /// </summary>
        /// <param name="talkConst">name, job etc.</param>
        /// <returns>The corresponding single ScriptLine</returns>
        public ScriptLine GetScriptLine(TalkConstants talkConst)
        {
            return (scriptLines[(int)talkConst]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nLabel"></param>
        /// <returns></returns>
        /*public ScriptLine GetScriptLineLabel(int nLabel)
        {
            foreach (ScriptLine line in scriptLines)
            {
                ScriptItem item = line.GetScriptItem(0);
                if (item.Command == TalkCommand.DefineLabel && item.LabelNum == nLabel)
                {
                    return line;
                }
            }
            throw new Exception("You requested a script label that doesn't exist");
        }*/


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
            }
            else
            {
                currentScriptLine.AddScriptItem(new ScriptItem(talkCommand));
            }
        }

        /// <summary>
        /// Prints the script out using all of the advanced ScriptLine, ScriptLabel and ScriptQuestionAnswer(s) objects, instead of just text
        /// </summary>
        public void PrintComprehensiveScript()
        {
            Console.WriteLine("---- BEGIN NEW SCRIPT -----");
            Console.WriteLine("Name: " + this.GetScriptLine(TalkConstants.Name).ToString());
            Console.WriteLine("Description: " + this.GetScriptLine(TalkConstants.Description).ToString());
            Console.WriteLine("Greeting: " + this.GetScriptLine(TalkConstants.Greeting).ToString());
            Console.WriteLine("Job: " + this.GetScriptLine(TalkConstants.Job).ToString());
            Console.WriteLine("Bye: " + this.GetScriptLine(TalkConstants.Bye).ToString());
            Console.WriteLine("");

            scriptQuestionAnswers.Print();
            Console.WriteLine("");

            // enumerate the labels and print their scripts
            foreach (ScriptTalkLabel label in this.scriptTalkLabels.Labels)
            {
                Console.WriteLine("Label #: " + label.LabelNum.ToString());
                Console.WriteLine("Initial Line: " + label.InitialLine);
                if (label.DefaultAnswers.Count > 0)
                {
                    foreach (ScriptLine line in label.DefaultAnswers)
                    {
                        Console.WriteLine("Default Line(s): " + line);
                    }
                    label.QuestionAnswers.Print();
                }
            }
        }

        /// <summary>
        /// Print the script out to the console
        /// This is the raw print routine that uses the relatively raw script data
        /// </summary>
        public void PrintScript()
        {
            foreach (ScriptLine line in scriptLines)
            {
                for (int nItem = 0; nItem < line.GetNumberOfScriptItems(); nItem++)
                {
                    ScriptItem item = line.GetScriptItem(nItem);

                    if (item.Command == TalkCommand.PlainString)
                    {
                        System.Console.Write(item.Str);
                    }
                    else
                    {
                        if (item.Command == TalkCommand.DefineLabel || item.Command == TalkCommand.GotoLabel)
                        {
                            System.Console.Write("<" + item.Command.ToString() + item.LabelNum.ToString() + ">");
                        }
                        else
                        {
                            System.Console.Write("<" + item.Command.ToString() + ">");
                        }
                    }
                }
            }
        }
}

    /// <summary>
    /// TalkScripts represents all of the in game talking scripts for all NPCs
    /// </summary>
    class TalkScripts
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
