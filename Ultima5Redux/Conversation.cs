using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace Ultima5Redux
{ 
    class Conversation
    {
        private NonPlayerCharacters.NonPlayerCharacter npc;
        private TalkScript script;
        private TalkScript.ScriptLine currentLine;
        private GameState gameStateRef;
        private Queue<TalkScript.ScriptItem> outputBufferQueue = new Queue<TalkScript.ScriptItem>();
        private Queue<string> responseQueue = new Queue<string>();

        public delegate void EnqueuedScriptItem(Conversation conversation);
        public EnqueuedScriptItem EnqueuedScriptItemCallback;

        public Conversation(NonPlayerCharacters.NonPlayerCharacter npc, GameState state)
        {
            this.npc = npc;
            script = npc.Script;
            this.gameStateRef = state;
        }

        public TalkScript.ScriptItem Start()
        {
            currentLine = script.GetScriptLine(TalkScript.TalkConstants.Description);
            TalkScript.ScriptItem item = currentLine.GetScriptItem(0);

            return item;
        }

        //public TalkScript.ScriptItem Next()
        //{
        //    return (Start());
        //}

        //public TalkScript.ScriptItem Next(string userResponse)
        //{
        //    return (Start());
        //}

        private bool runeMode = false;
        List<int> conversationOrder = new List<int>();
        List<TalkScript.ScriptLine> conversationOrderScriptLines = new List<TalkScript.ScriptLine>();

        private string ProcessItem(TalkScript.ScriptItem item)
        {
            switch (item.Command)
            {
                case TalkScript.TalkCommand.AvatarsName:
                    return gameStateRef.AvatarsName;
                case TalkScript.TalkCommand.NewLine:
                    return "\n";
                case TalkScript.TalkCommand.PlainString:
                    return item.Str;
                case TalkScript.TalkCommand.Rune:
                    runeMode = !runeMode;
                    return string.Empty;
            }

            return string.Empty;
        }

        public string AwaitResponse()
        {
            while (responseQueue.Count == 0)
            {
                Thread.Sleep(10);
            }

            lock (((ICollection)responseQueue).SyncRoot)
            {
                return responseQueue.Dequeue();
            }

        }

        public void AddUserResponse(string response)
        {
            lock (((ICollection)responseQueue).SyncRoot)
            {
                responseQueue.Enqueue(response);
            }
        }

        private void EnqueToOutputBuffer(TalkScript.ScriptItem output)
        {
            lock (((ICollection)outputBufferQueue).SyncRoot)
            {
                outputBufferQueue.Enqueue(output);
                EnqueuedScriptItemCallback(this);
            }
        }

        public TalkScript.ScriptItem DequeueFromOutputBuffer()
        {
            lock (((ICollection)outputBufferQueue).SyncRoot)
            {
                return outputBufferQueue.Dequeue();
            }
        }

        public void ProcessMultipleLines(List<TalkScript.ScriptLine> scriptLines)
        {
            //List<TalkScript.ScriptLine> scriptLines = scriptLine.SplitIntoSections();
            int skipCounter = -1;
            for (int i = 0; i < scriptLines.Count; i++)
            //foreach (TalkScript.ScriptLine line in scriptLines)
            {
                if (skipCounter != -1 && --skipCounter == 0)
                {
                    continue;
                }
                SkipInstruction skipInstruction = ProcessLine(scriptLines[i]);
                switch (skipInstruction)
                {
                    case SkipInstruction.SkipAfterNext:
                        skipCounter = 2;
                        break;
                    case SkipInstruction.SkipNext:
                        // skips next instruction
                        i++;
                        Debug.Assert(i < scriptLines.Count);
                        break;
                    case SkipInstruction.DontSkip:
                        // do nothing
                        break;
                }
            }
        }

        public SkipInstruction ProcessLine(TalkScript.ScriptLine scriptLine)
        {
           return ProcessLine(scriptLine, -1, -1);
        }

        public enum SkipInstruction { DontSkip = 0, SkipNext, SkipAfterNext };

        private SkipInstruction ProcessLine(TalkScript.ScriptLine scriptLine, int nTalkLineIndex, int nSplitLine)
        {
            int nItem = 0;
            int nItems = scriptLine.GetNumberOfScriptItems();
            do
            {
                TalkScript.ScriptItem item = scriptLine.GetScriptItem(nItem);

                // script just begun
                // - % chance that they will say "I am called XX" (perhaps when no description is present?)

                // if this is the very first position of conversation
                // we must describe what we "see"
                if (nTalkLineIndex == (int)TalkScript.TalkConstants.Description && nSplitLine == 0 && nItem == 0)
                {
                    //System.Console.WriteLine("You see " + ProcessItem(item));
                    EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, "You see "));
                }

                switch (item.Command)
                {
                    case TalkScript.TalkCommand.IfElseKnowsName:
                        Debug.Assert(nItems == 1);
                        if (npc.KnowTheAvatar())
                        {
                            // we continue to the next block, but skip the one after
                            return SkipInstruction.SkipAfterNext;
                        }
                        else
                        {
                            // we skip the next block because it is the line used when we actually know the Avatar
                            return SkipInstruction.SkipNext;
                        }
                    case TalkScript.TalkCommand.AvatarsName:
                        // we should already know if they know the avatars name....
                        Debug.Assert(npc.KnowTheAvatar());
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, ProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.AskName:
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, "What is thy name?"));
                        EnqueToOutputBuffer(item);
                        string avatarNameResponse = AwaitResponse();
                        if (avatarNameResponse.ToLower() == gameStateRef.AvatarsName.ToLower())
                        {
                            // i met them
                            gameStateRef.SetMetNPC(npc);
                            EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, "A pleasure!"));
                            break;
                        }
                        else
                        {
                            EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, "If you say so..."));
                        }
                        break;
                    case TalkScript.TalkCommand.CallGuards:
                        EnqueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.Change:
                        EnqueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.DefineLabel:
                        // if I find a goto label, then i expect I have no further conversation lines left
                        //Debug.Assert(nConversationIndex == conversationOrderScriptLines.Count - 1);
                        // we are going to add the GotoLabel to the script
                        conversationOrder.Add((int)script.GetScriptLineLabelIndex(item.LabelNum));
                        conversationOrderScriptLines.Add(script.GetScriptLine(script.GetScriptLineLabelIndex(item.LabelNum)));
                        break;
                    case TalkScript.TalkCommand.EndCoversation:
                        EnqueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.Gold:
                        EnqueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.JoinParty:
                        EnqueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.KarmaMinusOne:
                        EnqueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.KarmaPlusOne:
                        EnqueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.KeyWait:
                        EnqueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.NewLine:
                        //Console.WriteLine(ProcessItem(item));
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, ProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.Pause:
                        EnqueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.PlainString:
                        // we put it through the processor to change the text around if we are wrapped in a rune tag
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, ProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.Rune:
                        //Console.WriteLine(ProcessItem(item));
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, ProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.UserInputNotRecognized:
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, "I cannot help thee with that."));
                        break;
                    case TalkScript.TalkCommand.Unknown_Enter:
                        break;
                    case TalkScript.TalkCommand.Unknown_FF:
                        break;
                    case TalkScript.TalkCommand.DefaultMessage:
                        // dirty - advance past the label that it will sink in...
                        nItem++;
                        break;
                    case TalkScript.TalkCommand.Or:
                    case TalkScript.TalkCommand.Unknown_CodeA2:
                        throw new Exception("We should never see the <OR> or <A2> code in conversation");
                    default:
                        throw new Exception("Recieved TalkCommand I wasn't expecting during conversation");

                }
                nItem++;
                // while we still have more items in the current split line
            } while (nItem < nItems);
            return SkipInstruction.DontSkip;
        }

        /// <summary>
        /// Simulate a conversation through a console
        /// </summary>
        public void BeginConversation()
        {
            System.Console.WriteLine("---- PRE-CONVERSATION SCRIPT -----");
            script.PrintComprehensiveScript();

            System.Console.WriteLine("---- STARTING CONVERSATION -----");

            TalkScript.ScriptItem startItem = Start();


            // just an idea for now - when a label comes up, we can add it and then proceed to the next line by adding it
            // this is perpetual as long as there is more to see
            conversationOrder.Add((int)TalkScript.TalkConstants.Description);
            conversationOrder.Add((int)TalkScript.TalkConstants.Greeting);

            conversationOrderScriptLines.Add(script.GetScriptLine(TalkScript.TalkConstants.Description));
            conversationOrderScriptLines.Add(script.GetScriptLine(TalkScript.TalkConstants.Greeting));

            // some of these operatoins can be expensive, so let's call them once and store instead
            bool npcKnowsAvatar = npc.KnowTheAvatar();

            // the index into conversationOrder
            int nConversationIndex = 0;

            // while there are more conversation lines to process
            while (true)
            {
                // if we do not have any conversation left, then we will prompt for questions
                ///// NO DIALOG LEFT - USER RESPONDS
                while (nConversationIndex >= conversationOrder.Count)
                {
                    EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PromptUserForInput));
                    // we wait patiently for the user to respond
                    string userResponse = AwaitResponse();

                    if (npc.Script.QuestionAnswers.AnswerIsAvailable(userResponse))
                    {
                        // the user asked a question that we recognize, so let's process the answer
                        ProcessMultipleLines(npc.Script.QuestionAnswers.GetQuestionAnswer(userResponse).Answer.SplitIntoSections());
                    }
                    else
                    {
                        // we didn't recognize the user input - we will tell them so
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.UserInputNotRecognized));
                    }
                }

                TalkScript.ScriptLine currentLine;

                // the current talk line
                int nTalkLineIndex = conversationOrder[nConversationIndex];

                // the current ScriptLine
                currentLine = script.GetScriptLine(conversationOrder[nConversationIndex]);
                //currentLine = conversationOrderScriptLines[nConversationIndex];


                // Split the line into sections. This greatly simplifies the proceeding loops.
                List<TalkScript.ScriptLine> splitLines = currentLine.SplitIntoSections();
               // int nLine = 0;

                // currentLine = unsplit line with all content
                // splitLines = a list of all the split up lines
                // curentSplitLine = current section of the currentLine
                Debug.Assert(splitLines.Count > 0);

                /// This is a logic block does needs to peek into the entire line for certain patterns
                /// For example, if an AvatarsName is used in conversation, then we may need to process additional logic or ignore the line altogether
                // if it's a greeting AND her greeting includes my name AND they have NOT yet met the avatar  
                if (conversationOrder[nConversationIndex] == (int)TalkScript.TalkConstants.Greeting && currentLine.ContainsCommand(TalkScript.TalkCommand.AvatarsName)
                && !npcKnowsAvatar)
                {
                    // randomly add an introduction of the Avatar since they haven't met him
                    if (gameStateRef.OneInXOdds(4))
                    {
                        conversationOrder.Add((int)TalkScript.TalkConstants.Name);
                        conversationOrderScriptLines.Add(script.GetScriptLine(TalkScript.TalkConstants.Name));
                    }
                }

                int nSplitLine = 0;
                int skipCounter = -1;
                // we will go over each of the split lines indivdually
                do
                {
                    int nItem = 0;

                    // if we have just begun a label section, then let's handle it slightly difference then the normal conversation
                    if (splitLines[nSplitLine].IsLabelDefinition())
                    {
                        Debug.Assert(splitLines[nSplitLine].GetNumberOfScriptItems() == 2, "If it is a label definition, then it must have only 2 items defined in it");
                        int nLabel = splitLines[nSplitLine].GetScriptItem(1).LabelNum;
                        Debug.Assert(nLabel >= 0 && nLabel <= 9, "Label number must be between 0 and 9");

                        // get the label object
                        TalkScript.ScriptTalkLabel scriptLabel = script.TalkLabels.Labels[nLabel];

                        //List<TalkScript.ScriptLine> labelScriptSplitLines = scriptLabel.InitialLine.SplitIntoSections();
                        // we ar going through each of the line sections, but are skipping the first one since we know it is just a label
                        // definition
                        //for (int i = 1; i < labelScriptSplitLines.Count; i++)
                        //{
                        //    ProcessLine(labelScriptSplitLines[i]);
                        //}
                        ProcessMultipleLines(scriptLabel.InitialLine.SplitIntoSections());

                        string userResponse = string.Empty;
                        if (scriptLabel.ContainsQuestions())
                        {
                            // need to figure out if we are going to ask a question...
                            EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PromptUserForInput));
                            // we wait patiently for the user to respond
                            userResponse = AwaitResponse();
                        }
                        else
                        {
                            // is there an actual answer to the question?
                            // if not, then we have already processed our dialog line, let's move onto the next dialog item
                            break;
                        }

                        // There is an answer available from the NPC
                        if (scriptLabel.QuestionAnswers.AnswerIsAvailable(userResponse))
                        {
                            // let's get the answer details including the ScriptLine that will follow
                            TalkScript.ScriptQuestionAnswer qa = scriptLabel.QuestionAnswers.GetQuestionAnswer(userResponse);
                            TalkScript.ScriptLine npcResponseLine = qa.Answer;

                            ProcessMultipleLines(qa.Answer.SplitIntoSections());
                        }
                        else // you have entered an answer that isn't in their dialog - so default answer
                        {
                            // Process default response
                            foreach (TalkScript.ScriptLine defaultLine in scriptLabel.DefaultAnswers)
                            {
                                ProcessMultipleLines(defaultLine.SplitIntoSections());
                            }
                        }
                        
                        // we break to get into the outer loop again... we have handled all script work for the label within this block
                        break;
                    }
                    else // it's not a label NOR is a question and answer section
                    {
                        TalkScript.ScriptLine currentSplitLine = splitLines[nSplitLine];

                        // if they are going to referece the Avatar by name, but don't know it, then we just skip the line altogether
                        if (currentSplitLine.ContainsCommand(TalkScript.TalkCommand.AvatarsName) && !npcKnowsAvatar)
                        {
                            Debug.Assert(nItem == 0);
                            nSplitLine++;
                            continue;
                        }

                        if (skipCounter != -1 && skipCounter == 0)
                        {
                            // we skip this block
                            --skipCounter;
                        }
                        else
                        {
                            if (skipCounter != -1) --skipCounter; 
                            SkipInstruction skipInstruction = ProcessLine(currentSplitLine, nTalkLineIndex, nSplitLine);
                            switch (skipInstruction)
                            {
                                case SkipInstruction.DontSkip:
                                    break;
                                case SkipInstruction.SkipAfterNext:
                                    skipCounter = 2;
                                    break;
                                case SkipInstruction.SkipNext:
                                    nSplitLine++;
                                    break;
                            }
                        }

                    }

                    // we process all ScriptItem in each split ScriptLine before proceeding the next conversation item (ie. name, job, label3)
                    // todo: an emumerator would be kinda cool - i like foreach loops
                    nSplitLine++;
                // while there are more split lines to go through
                } while (nSplitLine < splitLines.Count);

               // EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, "\n"));
                // we have gone through all instructions, so lets move onto the next conversation line
                nConversationIndex++;
            }
        }
    }
}
