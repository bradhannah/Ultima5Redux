using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// ReSharper disable InvalidXmlDocComment

namespace Ultima5Redux.Dialogue
{
    public class TalkScript
    {
        /// <summary>
        ///     Specific talk command
        /// </summary>
        public enum TalkCommand
        {
            PlainString = 0x00, AvatarsName = 0x81, EndConversation = 0x82, Pause = 0x83, JoinParty = 0x84, Gold = 0x85,
            Change = 0x86, Or = 0x87, AskName = 0x88, KarmaPlusOne = 0x89, KarmaMinusOne = 0x8A, CallGuards = 0x8B,
            IfElseKnowsName = 0x8C, NewLine = 0x8D, Rune = 0x8E, KeyWait = 0x8F, StartLabelDefinition = 0x90,
            StartNewSection = 0xA2, Unknown_Enter = 0x9F, GotoLabel = 0xFD, DefineLabel = 0xFE, DoNothingSection = 0xFF,
            PromptUserForInput_NPCQuestion = 0x80, PromptUserForInput_UserInterest = 0x7F, UserInputNotRecognized = 0x7E
        }

        /// <summary>
        ///     The default script line offsets for the static responses
        /// </summary>
        public enum TalkConstants { Name = 0, Description, Greeting, Job, Bye }


        /// <summary>
        ///     the minimum talk code for labels (in .tlk files)
        /// </summary>
        public const byte MIN_LABEL = 0x91;

        /// <summary>
        ///     the maximum talk code for labels (in .tlk files)
        /// </summary>
        public const byte MAX_LABEL = 0x91 + 0x0A;

        /// <summary>
        ///     total number of labels that are allowed to be defined
        /// </summary>
        public const int TOTAL_LABELS = 0x0A;

        /// <summary>
        ///     the end Index for the default script lines (ie. name, job etc.)
        /// </summary>
        private const int END_BASE_INDEXES = (int)TalkConstants.Bye;

        /// <summary>
        ///     All of the ScriptLines
        /// </summary>
        private readonly List<ScriptLine> _scriptLines = new List<ScriptLine>();

        /// <summary>
        ///     Non label specific Q & A
        /// </summary>
        private readonly ScriptQuestionAnswers _scriptQuestionAnswers = new ScriptQuestionAnswers();

        /// <summary>
        ///     Script talk labels contain all the labels, their Q & A and default responses
        /// </summary>
        private readonly ScriptTalkLabels _scriptTalkLabels = new ScriptTalkLabels();

        // tracking the current script line
        private ScriptLine _currentScriptLine = new ScriptLine();

        /// <summary>
        ///     Build the initial TalkScript
        /// </summary>
        public TalkScript()
        {
            // let's add it immediately instead of waiting for someone to commit it
            // note; this will fail if the currentScriptLine is not a reference - but I'm pretty sure it is
            _scriptLines.Add(_currentScriptLine);
        }

        /// <summary>
        ///     All associated labels with the Script
        /// </summary>
        protected internal ScriptTalkLabels TalkLabels => _scriptTalkLabels;

        /// <summary>
        ///     All associated questions and answers for the Script
        /// </summary>
        protected internal ScriptQuestionAnswers QuestionAnswers => _scriptQuestionAnswers;

        /// <summary>
        ///     The number of ScriptLines in the Script
        /// </summary>
        public int NumberOfScriptLines => _scriptLines.Count;

        /// <summary>
        ///     Move to the next line in the script (for adding new content)
        /// </summary>
        protected internal void NextLine()
        {
            _currentScriptLine = new ScriptLine();
            _scriptLines.Add(_currentScriptLine);
        }

        /// <summary>
        ///     After adding all elements, this will process the script into a more readable format
        /// </summary>
        public void InitScript()
        {
            // we keep track of the index into the ScriptLines all the way through the entire method
            int nIndex = END_BASE_INDEXES + 1;

            // have we encountered a label yet?

            //bool labelEncountered = false;

            string question;

            // we are going to add name, job and bye to all scripts by default. We use the QuestionAnswer objects to make it seamless
            List<string> nameQuestion = new List<string>(1) { "name" };
            _scriptQuestionAnswers.Add(new ScriptQuestionAnswer(nameQuestion, _scriptLines[(int)TalkConstants.Name]));
            List<string> jobQuestion = new List<string>(2) { "job", "work" };
            _scriptQuestionAnswers.Add(new ScriptQuestionAnswer(jobQuestion, _scriptLines[(int)TalkConstants.Job]));
            List<string> byeQuestion = new List<string>(1) { "bye" };
            _scriptQuestionAnswers.Add(new ScriptQuestionAnswer(byeQuestion, _scriptLines[(int)TalkConstants.Bye]));

            // repeat through the question/answer components until we hit a label - then we know to move onto the label section
            do
            {
                List<string> currQuestions = new List<string>();
                ScriptLine line = _scriptLines[nIndex];

                // if we just hit a label, then it's time to jump out of this loop and move onto the label reading loop
                if (line.GetScriptItem(0).Command == TalkCommand.StartLabelDefinition)
                    //labelEncountered = true;
                    break;

                // first time around we KNOW there is a first question, all NPCs have at least one question
                question = line.GetScriptItem(0).Str;

                // dumb little thing - there are some scripts that have the same keyword multiple times
                // the game favours the one it sees first (see "Camile" in West Brittany as an example)
                if (!_scriptQuestionAnswers.QuestionAnswers.ContainsKey(question))
                    currQuestions.Add(question);

                // if we peek ahead and the next command is an <or> then we will just skip it and continue to add to the questions list
                while (_scriptLines[nIndex + 1].ContainsCommand(TalkCommand.Or))
                {
                    nIndex += 2;
                    line = _scriptLines[nIndex];
                    question = line.GetScriptItem(0).Str;
                    // just in case they try to add the same question twice - this is kind of a bug in the data since the game just favours the first question it sees
                    if (!_scriptQuestionAnswers.QuestionAnswers.ContainsKey(question)) currQuestions.Add(question);
                }

                ScriptLine nextLine = _scriptLines[nIndex + 1];
                _scriptQuestionAnswers.Add(new ScriptQuestionAnswer(currQuestions, nextLine));
                nIndex += 2;
            } while (true);

            // a little hack - it's easy to end the conversation if it always ends with the end conversation tag
            _scriptLines[(int)TalkConstants.Bye].AddScriptItem(new ScriptItem(TalkCommand.EndConversation));

            // time to process labels!! the nIndex that the previous routine left with is the beginning of the label section
            int count = 0;
            do // begin the label processing loop - pretty sure this is dumb and doesn't do anything - but everything messes up when I remove it
            {
                // this is technically a loop, but it should never actually loop. Kind of dumb, but fragile 
                Debug.Assert(count++ == 0);

                ScriptLine line = _scriptLines[nIndex];
                ScriptLine nextLine;

                // if there are two script items, and those two script items identify an end of label section then let's break out
                // this should only actually occur if there are no labels at all
                if (line.NumberOfScriptItems == 2 && line.IsEndOfLabelSection)
                {
                    // all done. we either had no labels or reached the end of them
                    // assert that we are on the last line of the script
                    Debug.Assert(nIndex == _scriptLines.Count - 1);
                    break;
                }

                // i expect that this line will always indicate a new label is being defined
                Debug.Assert(line.GetScriptItem(0).Command == TalkCommand.StartLabelDefinition);

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

                    line = _scriptLines[nIndex];

                    // let's make sure there are actually labels to look at
                    if (line.IsEndOfLabelSection)
                    {
                        nextCommandDefaultMessage = true;
                        break;
                    }

                    // create the shell for the label
                    ScriptTalkLabel scriptTalkLabel = new ScriptTalkLabel(line.GetScriptItem(1).LabelNum, line);

                    // save the new label to the label collection
                    _scriptTalkLabels.AddLabel(scriptTalkLabel);

                    // it's a single line only, so we skip this tom foolery below
                    if (_scriptLines[nIndex + 1].GetScriptItem(0).Command == TalkCommand.StartLabelDefinition)
                    {
                        // do nothing, the ScriptTalkLabel will simply have no DefaultAnswer indicating that only the primary label line is read

                        nIndex++;
                        continue;
                    }

                    // with a single answer below the label, we will always use the default answer
                    ScriptLine defaultAnswer = _scriptLines[++nIndex];
                    scriptTalkLabel.DefaultAnswers.Add(defaultAnswer);

                    // it's a default only answer, and no additional line of dialog, then we skip this tom foolery below 
                    if (_scriptLines[nIndex + 1].GetScriptItem(0).Command == TalkCommand.StartLabelDefinition)
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
                        if (_scriptLines[nIndex + 2].ContainsCommand(TalkCommand.Or))
                        {
                            while (_scriptLines[nIndex + 2].ContainsCommand(TalkCommand.Or))
                            {
                                line = _scriptLines[nIndex + 1];
                                Debug.Assert(line.IsQuestion);
                                question = line.GetScriptItem(0).Str;
                                // just in case they try to add the same question twice - this is kind of a bug in the data since the game just favours the first question it sees
                                if (!_scriptQuestionAnswers.QuestionAnswers.ContainsKey(question))
                                    currQuestions.Add(question);
                                nIndex += 2;
                            }

                            line = _scriptLines[++nIndex];
                            Debug.Assert(line.IsQuestion);
                            question = line.GetScriptItem(0).Str;
                            // just in case they try to add the same question twice - this is kind of a bug in the data since the game just favours the first question it sees
                            if (!_scriptQuestionAnswers.QuestionAnswers.ContainsKey(question))
                                currQuestions.Add(question);
                        }
                        // is this a question that the player would ask an NPC?
                        else if (_scriptLines[nIndex + 1].GetScriptItem(0).IsQuestion())
                        {
                            // get the Avatar's response line
                            line = _scriptLines[++nIndex];

                            question = line.GetScriptItem(0).Str;
                            Debug.Assert(ScriptItem.IsQuestion(question));
                            currQuestions.Add(question);
                        }
                        // the NPC has tricked me - this is a second line of dialog for the given 
                        // that dastardly LB has put an extra response line in....
                        else //if (scriptLines[nIndex + 1].GetScriptItem(0).Str.Trim().Length > 4)
                        {
                            line = _scriptLines[++nIndex];
                            Debug.Assert(!line.IsQuestion);
                            scriptTalkLabel.DefaultAnswers.Add(line);
                            nIndex++;
                            // let's make double sure that we only have a single additional line of text 
                            Debug.Assert(_scriptLines[nIndex].GetScriptItem(0).Command ==
                                         TalkCommand.StartLabelDefinition);

                            nextLine = _scriptLines[nIndex];
                            continue;
                        }

                        // get your answer and store it
                        ScriptLine npcResponse = _scriptLines[++nIndex];
                        // we are ready to create a Q&A object and add it the label specific Q&A script
                        scriptTalkLabel.AddScriptQuestionAnswer(new ScriptQuestionAnswer(currQuestions, npcResponse));

                        // we are at the end of the label section of the file, so we are done.
                        nextLine = _scriptLines[++nIndex];

                        // does the next line indicate end of all of the label sections, then let's get out of this loop
                        if (nextLine.IsEndOfLabelSection)
                        {
                            nIndex--;
                            nextCommandDefaultMessage = true;
                            break;
                        }

                        // is the next line a label definition? is so, let's exit this label and move on
                        if (!nextLine.IsLabelDefinition()) nIndex--;

                        // while we know the next line is not a new label or end of label, then let's keep reading by moving to our next loop
                    } while (nextLine.GetScriptItem(0).Command != TalkCommand.StartLabelDefinition);

                    // while we haven't encountered an end of label section 
                } while (!nextCommandDefaultMessage);

                nIndex++;
                // while we haven't read every last line, then let's keep reading
            } while (nIndex < _scriptLines.Count - 1);
        }

        /// <summary>
        ///     Get the script line based on the specified Talk Constant allowing to quickly access "name", "job" etc.
        ///     This is not compatible with Labels
        /// </summary>
        /// <param name="talkConst">name, job etc.</param>
        /// <returns>The corresponding single ScriptLine</returns>
        protected internal ScriptLine GetScriptLine(TalkConstants talkConst)
        {
            return _scriptLines[(int)talkConst];
        }

        /// <summary>
        ///     Gets a script line based on its index in the overall Script
        /// </summary>
        /// <param name="index">index into Script</param>
        /// <returns>The requested ScriptLine</returns>
        protected internal ScriptLine GetScriptLine(int index)
        {
            return _scriptLines[index];
        }


        /// <summary>
        ///     Gets a scriptline based on the label index
        /// </summary>
        /// <param name="nLabel">0 based index of label</param>
        /// <returns>The corresponding script line</returns>
        protected internal ScriptLine GetScriptLineLabel(int nLabel)
        {
            return _scriptLines[GetScriptLineLabelIndex(nLabel)];
        }

        /// <summary>
        ///     Gets the index of the line that contains the given label definition
        /// </summary>
        /// <param name="nLabel">0 based label nunber</param>
        /// <returns>index into Script</returns>
        public int GetScriptLineLabelIndex(int nLabel)
        {
            int nCount = 0;
            foreach (ScriptLine line in _scriptLines)
            {
                if (line.NumberOfScriptItems <= 1)
                {
                    nCount++;
                    continue;
                }

                ScriptItem item = line.GetScriptItem(1);
                if (line.IsLabelDefinition() && item.LabelNum == nLabel)
                    return nCount;
                nCount++;
            }

            throw new Ultima5ReduxException("You requested a script label that doesn't exist");
        }

        /// <summary>
        ///     Add a talk label.
        /// </summary>
        /// <param name="talkCommand">Either GotoLabel or DefineLabel</param>
        /// <param name="nLabel">label # (0-9)</param>
        public void AddTalkLabel(TalkCommand talkCommand, int nLabel)
        {
            if (nLabel < 0 || nLabel > TOTAL_LABELS)
                throw new Ultima5ReduxException("Label Number: " + nLabel + " is out of range");

            if (talkCommand == TalkCommand.GotoLabel || talkCommand == TalkCommand.DefineLabel)
                _currentScriptLine.AddScriptItem(new ScriptItem(talkCommand, nLabel));
            //System.Console.Write("<" + (talkCommand.ToString() + " " + nLabel + ">"));
            else
                throw new Ultima5ReduxException("You passed a talk command that isn't a label! ");
        }

        /// <summary>
        ///     Add to the current script line, but no string associated
        ///     For example: STRING
        ///     <NEWLINE>
        ///         <AVATARNAME>
        /// </summary>
        /// <param name="talkCommand"></param>
        public void AddTalkCommand(TalkCommand talkCommand)
        {
            //System.Console.Write("<" + (talkCommand.ToString() + ">"));

            _currentScriptLine.AddScriptItem(new ScriptItem(talkCommand, string.Empty));
        }

        /// <summary>
        ///     Add to the current script line
        ///     For example: STRING
        ///     <NEWLINE>
        ///         <AVATARNAME>
        /// </summary>
        /// <param name="talkCommand"></param>
        /// <param name="talkStr"></param>
        public void AddTalkCommand(TalkCommand talkCommand, string talkStr)
        {
            _currentScriptLine.AddScriptItem(talkCommand == TalkCommand.PlainString
                ? new ScriptItem(talkCommand, talkStr)
                : new ScriptItem(talkCommand));
        }

        /// <summary>
        ///     Prints the script out using all of the advanced ScriptLine, ScriptLabel and ScriptQuestionAnswer(s) objects,
        ///     instead of just text
        /// </summary>
        public void PrintComprehensiveScript()
        {
            Console.WriteLine(@"---- BEGIN NEW SCRIPT -----");
            Console.WriteLine(@"Name: " + GetScriptLine(TalkConstants.Name));
            Console.WriteLine(@"Description: " + GetScriptLine(TalkConstants.Description));
            Console.WriteLine(@"Greeting: " + GetScriptLine(TalkConstants.Greeting));
            Console.WriteLine(@"Job: " + GetScriptLine(TalkConstants.Job));
            Console.WriteLine(@"Bye: " + GetScriptLine(TalkConstants.Bye));
            Console.WriteLine("");

            _scriptQuestionAnswers.Print();
            Console.WriteLine("");

            // enumerate the labels and print their scripts
            foreach (ScriptTalkLabel label in _scriptTalkLabels.Labels)
            {
                Console.WriteLine(@"Label #: " + label.LabelNum);
                Console.WriteLine(@"Initial Line: " + label.InitialLine);
                if (label.DefaultAnswers.Count > 0)
                {
                    foreach (ScriptLine line in label.DefaultAnswers)
                    {
                        Console.WriteLine(@"Default Line(s): " + line);
                    }

                    label.QuestionAnswers.Print();
                }
            }
        }

        /// <summary>
        ///     Print the script out to the console
        ///     This is the raw print routine that uses the relatively raw script data
        /// </summary>
        public void PrintScript()
        {
            foreach (ScriptLine line in _scriptLines)
            {
                for (int nItem = 0; nItem < line.NumberOfScriptItems; nItem++)
                {
                    ScriptItem item = line.GetScriptItem(nItem);

                    switch (item.Command)
                    {
                        case TalkCommand.PlainString:
                            Console.Write(item.Str);
                            break;
                        case TalkCommand.DefineLabel:
                        case TalkCommand.GotoLabel:
                            Console.Write(@"<" + item.Command + item.LabelNum + @">");
                            break;
                        default:
                            Console.Write(@"<" + item.Command + @">");
                            break;
                    }
                }
            }
        }

        /// <summary>
        ///     This is a collection of all an NPCs ScriptTalkLabel(s)
        /// </summary>
        protected internal class ScriptTalkLabels
        {
            /// <summary>
            ///     Default constructor
            /// </summary>
            public ScriptTalkLabels()
            {
                Labels = new List<ScriptTalkLabel>();
            }

            /// <summary>
            ///     A simple list of all the labels
            /// </summary>
            public List<ScriptTalkLabel> Labels { get; }

            public ScriptTalkLabel GetScriptLabel(int nLabel)
            {
                foreach (ScriptTalkLabel scriptLabel in Labels)
                {
                    if (scriptLabel.LabelNum == nLabel) return scriptLabel;
                }

                throw new Ultima5ReduxException("Asked for a label " + nLabel + " that doesn't exist...");
            }

            /// <summary>
            ///     Add a single ScriptTalkLabel
            /// </summary>
            /// <param name="talkLabel"></param>
            public void AddLabel(ScriptTalkLabel talkLabel)
            {
                Labels.Add(talkLabel);
            }
        }

        /// <summary>
        ///     A single label for an NPC
        ///     Includes all dialog components need for label
        /// </summary>
        protected internal class ScriptTalkLabel
        {
            /// <summary>
            ///     Construct the ScriptTalkLabel
            /// </summary>
            /// <param name="labelNum">label number between 0 and 9</param>
            /// <param name="initialLine">The initial line that will always be shown</param>
            /// <param name="defaultAnswers">Default answer if an expected response is not given</param>
            /// <param name="sqa">Script questions and answers</param>
            public ScriptTalkLabel(int labelNum, ScriptLine initialLine, List<ScriptLine> defaultAnswers,
                ScriptQuestionAnswers sqa)
            {
                QuestionAnswers = sqa;
                InitialLine = initialLine;
                // if one is not provided, then one will be provided for you
                if (defaultAnswers == null)
                    DefaultAnswers = new List<ScriptLine>();
                else
                    //DefaultAnswers = DefaultAnswers;
                    DefaultAnswers = defaultAnswers;
                LabelNum = labelNum;
            }

            /// <summary>
            ///     Construct the ScriptTalkLabel
            /// </summary>
            /// <param name="labelNum">label number between 0 and 9</param>
            /// <param name="initialLine">The initial line that will always be shown</param>
            public ScriptTalkLabel(int labelNum, ScriptLine initialLine) : this(labelNum, initialLine, null,
                new ScriptQuestionAnswers())
            {
            }

            /// <summary>
            ///     All the NPCs question and answers specific to the label
            /// </summary>
            public ScriptQuestionAnswers QuestionAnswers { get; }

            /// <summary>
            ///     The initial line that you will always show when jumping to the label
            /// </summary>
            public ScriptLine InitialLine { get; set; }

            /// <summary>
            ///     The default answer if non of the QuestionAnswers are satisfied
            /// </summary>
            public List<ScriptLine> DefaultAnswers { get; set; }

            /// <summary>
            ///     The label reference number
            /// </summary>
            public int LabelNum { get; }

            /// <summary>
            ///     Add an additional question and answer
            /// </summary>
            /// <param name="sqa"></param>
            public void AddScriptQuestionAnswer(ScriptQuestionAnswer sqa)
            {
                QuestionAnswers.Add(sqa);
            }

            public bool ContainsQuestions()
            {
                return DefaultAnswers.Count > 0;
            }
        }

        /// <summary>
        ///     Collection of questions and answers, makes accessing them much easier
        /// </summary>
        protected internal class ScriptQuestionAnswers
        {
            /// <summary>
            ///     Default constructor
            /// </summary>
            public ScriptQuestionAnswers()
            {
                QuestionAnswers = new Dictionary<string, ScriptQuestionAnswer>();
            }

            /// <summary>
            ///     All the NPCs question strings and associated answers
            /// </summary>
            public Dictionary<string, ScriptQuestionAnswer> QuestionAnswers { get; }

            private string GetQuestionKey(string userSuppliedQuestion)
            {
                foreach (string question in QuestionAnswers.Keys)
                {
                    if (userSuppliedQuestion.ToLower().StartsWith(question.ToLower())) return question;
                }

                return string.Empty;
            }

            public bool AnswerIsAvailable(string question)
            {
                return GetQuestionKey(question) != string.Empty;
            }

            /// <summary>
            ///     Based on a user response, return the ScriptQuestionAnswer object
            /// </summary>
            /// <param name="question">question issued by user</param>
            /// <returns>associated QuestionAnswer if one exists</returns>
            public ScriptQuestionAnswer GetQuestionAnswer(string question)
            {
                if (!AnswerIsAvailable(question))
                    throw new Ultima5ReduxException(
                        "You have requested an answer for a question that doesn't exist. Use AnswerIsAvailable to check for existence first.");

                return QuestionAnswers[GetQuestionKey(question)];
            }

            /// <summary>
            ///     Get an array of all associated questions
            /// </summary>
            /// <returns></returns>
            public string[] GetScriptQuestions()
            {
                return QuestionAnswers.Keys.ToArray();
            }

            /// <summary>
            ///     Add a ScriptQuestionAnswer to the collection
            /// </summary>
            /// <param name="sqa">ScriptQuestionAnswer object to add</param>
            public void Add(ScriptQuestionAnswer sqa)
            {
                if (sqa.Questions == null)
                    return;

                foreach (string question in sqa.Questions)
                {
                    if (!QuestionAnswers.Keys.Contains(question.Trim())) QuestionAnswers.Add(question.Trim(), sqa);
                }
            }

            /// <summary>
            ///     Print the object to the console
            /// </summary>
            public void Print()
            {
                Dictionary<ScriptQuestionAnswer, bool> seenAnswers = new Dictionary<ScriptQuestionAnswer, bool>();

                foreach (ScriptQuestionAnswer sqa in QuestionAnswers.Values)
                {
                    if (seenAnswers.ContainsKey(sqa)) continue;
                    seenAnswers.Add(sqa, true);

                    bool first = true;
                    foreach (string question in sqa.Questions.ToArray())
                    {
                        if (first)
                        {
                            first = false;
                            Console.Write(@"Questions: " + question);
                        }
                        else
                        {
                            Console.Write(@" <OR> " + question);
                        }
                    }

                    Console.WriteLine("");
                    Console.WriteLine(@"Answer: " + sqa.Answer);
                }
            }
        }

        /// <summary>
        ///     A single instance of a question and answer for dialog
        /// </summary>
        protected internal class ScriptQuestionAnswer
        {
            public ScriptQuestionAnswer(List<string> questions, ScriptLine answer)
            {
                Questions = questions;
                Answer = answer;
            }

            public ScriptLine Answer { get; }

            public List<string> Questions { get; }
        }

        /// <summary>
        ///     Represents a single script component
        /// </summary>
        public class ScriptItem
        {
            private string _str = string.Empty;

            /// <summary>
            ///     Simple constructor for basic commands
            /// </summary>
            /// <param name="command"></param>
            public ScriptItem(TalkCommand command) : this(command, string.Empty)
            {
            }

            /// <summary>
            ///     Creates a label
            /// </summary>
            /// <param name="command">a GotoLabel or DefineLabel</param>
            /// <param name="nLabelNum">number of the label</param>
            public ScriptItem(TalkCommand command, int nLabelNum)
            {
                Command = command;
                LabelNum = nLabelNum;
            }

            /// <summary>
            ///     A talk command with an associated string
            /// </summary>
            /// <param name="command"></param>
            /// <param name="str"></param>
            public ScriptItem(TalkCommand command, string str)
            {
                Command = command;
                _str = str;
            }

            /// <summary>
            ///     command issued
            /// </summary>
            public TalkCommand Command { get; }

            /// <summary>
            ///     Associated string (can be empty)
            /// </summary>
            public string Str
            {
                get
                {
                    // we trim the double quotes out since they are so hard to deal with
                    // the boys at Origin must have had some very specific rules for dealing with newlines and
                    // double quotes
                    char[] trimChars = { '"' };
                    return _str.Trim(trimChars);
                }
                set => _str = value;
            }
            //=> str;

            /// <summary>
            ///     If there is a label, then this is a zero based index
            /// </summary>
            public int LabelNum { get; }

            public int ItemAdditionalData { get; set; }

            public static bool IsQuestion(string str)
            {
                // if the string is:
                // 1 to 6 characters
                // AND doesn't contain spaces

                return str.Trim().Length <= 6 && str.Trim().Length >= 1 && !str.Contains(" ");

                // there are some answers that are capitalized...
                //&& (Str.ToLower() == Str));
            }

            /// <summary>
            ///     is this script item a question that the player asks an NPC?
            /// </summary>
            /// <returns></returns>
            public bool IsQuestion()
            {
                return IsQuestion(Str);
            }
        }

        /// <summary>
        ///     Special scriptline that identifies that it has been split in sections
        /// </summary>
        protected internal class SplitScriptLine : ScriptLine
        {
        }

        /// <summary>
        ///     Represents a single line of a script
        ///     This script line can be in a "split mode" or non-splitmode
        /// </summary>
        protected internal class ScriptLine
        {
            /// <summary>
            ///     a list of all associated ScriptItems, in a particular order
            /// </summary>
            protected List<ScriptItem> ScriptItems = new List<ScriptItem>();

            /// <summary>
            ///     Is this script line a user input based question
            /// </summary>
            /// <returns>true if it's a question</returns>
            public bool IsQuestion => GetScriptItem(0).IsQuestion();

            /// <summary>
            ///     Does this line represent the end of all Labels in the NPC talk script (end of script)
            /// </summary>
            /// <returns></returns>
            public bool IsEndOfLabelSection
            {
                get
                {
                    if (GetScriptItem(0).Command == TalkCommand.StartLabelDefinition &&
                        GetScriptItem(1).Command == TalkCommand.Unknown_Enter) return true;
                    return false;
                }
            }

            /// <summary>
            ///     Return the number of current script items
            /// </summary>
            /// <returns>the number of script items</returns>
            public int NumberOfScriptItems => ScriptItems.Count;

            /// <summary>
            ///     Creates a human readable string for the ScriptLine
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string scriptLine = string.Empty;

                foreach (ScriptItem item in ScriptItems)
                {
                    if (item.Command == TalkCommand.PlainString)
                    {
                        scriptLine += item.Str.Trim();
                    }
                    else
                    {
                        if (item.Command == TalkCommand.DefineLabel || item.Command == TalkCommand.GotoLabel)
                            scriptLine += "<" + item.Command + item.LabelNum + ">";
                        else
                            scriptLine += "<" + item.Command + ">";
                    }
                }

                return scriptLine;
            }


            /// <summary>
            ///     Does this line represent a new label definition
            /// </summary>
            /// <returns></returns>
            public bool IsLabelDefinition()
            {
                if (GetScriptItem(0).Command == TalkCommand.StartLabelDefinition &&
                    GetScriptItem(1).Command == TalkCommand.DefineLabel) return true;
                return false;
            }

            /// <summary>
            ///     Add an additional script item
            /// </summary>
            /// <param name="scriptItem"></param>
            public void AddScriptItem(ScriptItem scriptItem)
            {
                ScriptItems.Add(scriptItem);
            }

            public void InsertScriptItemAtFront(ScriptItem scriptItem)
            {
                ScriptItems.Insert(0, scriptItem);
            }

            public void EncloseInQuotes()
            {
                if (GetScriptItem(0).Command == TalkCommand.PlainString && GetScriptItem(0).Str != "\"")
                {
                    //InsertScriptItemAtFront(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, "\""));
                }

                if (GetScriptItem(NumberOfScriptItems - 1).Command == TalkCommand.PlainString &&
                    GetScriptItem(NumberOfScriptItems - 1).Str != "\"")
                {
                    //AddScriptItem(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, "\""));
                }
            }

            /// <summary>
            ///     Get a script item based on an index into the list
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public ScriptItem GetScriptItem(int index)
            {
                Debug.Assert(ScriptItems[index] != null);
                return ScriptItems[index];
            }

            /// <summary>
            ///     Splits the ScriptLine into sections and returns a special class that signifies it has been split
            /// </summary>
            /// <returns>A list of SplitScriptLines</returns>
            public List<SplitScriptLine> SplitIntoSections()
            {
                List<SplitScriptLine> lines = new List<SplitScriptLine>();
                lines.Add(new SplitScriptLine());

                int nSection = -1;
                bool first = true;
                bool forceSplitNext = false;

                for (int i = 0; i < NumberOfScriptItems; i++)
                {
                    ScriptItem item = GetScriptItem(i);

                    // Code A2 appears to denote the beginning of a new section, so we split it
                    if (item.Command == TalkCommand.StartNewSection)
                    {
                        // It's a new section, so we simply advance the section counter and add an empty SplitScriptLine
                        nSection++;
                        lines.Add(new SplitScriptLine());
                    }
                    // if there is a IfElse branch for the Avatar's name the, a DoNothingSection or a goto label, then we add a new section, save the SplitScriptLine
                    else if (item.Command == TalkCommand.IfElseKnowsName ||
                             item.Command == TalkCommand.DoNothingSection || item.Command == TalkCommand.DefineLabel)
                    {
                        // advance to next section
                        nSection++;
                        // add a stump section
                        lines.Add(new SplitScriptLine());
                        // add the item as-is to the new section
                        lines[nSection].AddScriptItem(item);
                        // we need to tell the loop to force yet another split next time around
                        // basically - these items need to part of a single item SplitScriptLine
                        forceSplitNext = true;
                    }
                    else if (item.Command == TalkCommand.Change)
                    {
                        // advance to next section
                        nSection++;
                        // add a stump section
                        lines.Add(new SplitScriptLine());

                        // this is a bit dirty - but the next item is the item number that we are being given
                        item.ItemAdditionalData = (int)GetScriptItem(i + 1).Command;
                        // add the item as-is to the new section
                        lines[nSection].AddScriptItem(item);

                        i++;
                        forceSplitNext = true;
                    }
                    else if (item.Command == TalkCommand.Gold)
                    {
                        // advance to next section
                        nSection++;

                        // the next three characters are a 3 digit string that describes how much gold we are giving the NPC
                        item.ItemAdditionalData = int.Parse(GetScriptItem(i + 1).Str.Substring(0, 3));

                        i++;
                        forceSplitNext = true;
                    }
                    // if there is a default message then it is the definition of a new label
                    else if (item.Command == TalkCommand.StartLabelDefinition)
                    {
                        // advance to next section
                        nSection++;
                        Debug.Assert(GetScriptItem(i + 1).Command ==
                                     TalkCommand
                                         .DefineLabel); // StartLabelDefinition must ALWYAYS be followed with a DefineLabel
                        // add the StartLabelDefintion to the new section
                        lines[nSection].AddScriptItem(item);

                        // add the next item - which is a DefineLabel to the section 
                        lines[nSection].AddScriptItem(GetScriptItem(i + 1));
                        // skip by the DefineLabel section since we just added
                        i++;
                        // we need to tell the loop to force yet another split next time around
                        // basically - these items need to part of a single item SplitScriptLine
                        forceSplitNext = true;
                    }
                    else // it is any other kind of TalkCommand
                    {
                        // welp - I really can't recall why I did this, but I need it.
                        if (first) nSection = 0;
                        // if we are forcing a new section from a previous run, then we increment the section number
                        if (forceSplitNext)
                        {
                            forceSplitNext = false;
                            nSection++;
                            lines.Add(new SplitScriptLine());
                        }

                        if (nSection < 0)
                            throw new Ultima5ReduxException("Section number fell below zero in conversation.");
                        lines[nSection].AddScriptItem(item);
                    }

                    first = false;
                }

                return lines;
            }

            /// <summary>
            ///     Determines if a particular talk command is present in a script line
            ///     <remarks>This particularly helpful when looking for looking for <AvatarName></remarks>
            /// </summary>
            /// <param name="command">the command to search for</param>
            /// <returns>true if it's present, false if it isn't</returns>
            public bool ContainsCommand(TalkCommand command) =>
                ScriptItems.Any(scriptItem => scriptItem.Command == command);
        }
    }
}