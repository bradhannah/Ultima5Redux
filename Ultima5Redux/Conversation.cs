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
        public NonPlayerCharacters.NonPlayerCharacter Npc { get; }
        private TalkScript script;
        private DataOvlReference dataOvlRef;
        //private TalkScript.ScriptLine currentLine;
        private GameState gameStateRef;
        private Queue<TalkScript.ScriptItem> outputBufferQueue = new Queue<TalkScript.ScriptItem>();
        private Queue<string> responseQueue = new Queue<string>();

        public delegate void EnqueuedScriptItem(Conversation conversation);
        public EnqueuedScriptItem EnqueuedScriptItemCallback;

        public Conversation(NonPlayerCharacters.NonPlayerCharacter npc, GameState state, DataOvlReference dataOvlRef)
        {
            this.Npc = npc;
            script = npc.Script;
            this.gameStateRef = state;
            this.dataOvlRef = dataOvlRef;
        }

        //public TalkScript.ScriptItem Start()
        //{
        //    currentLine = script.GetScriptLine(TalkScript.TalkConstants.Description);
        //    TalkScript.ScriptItem item = currentLine.GetScriptItem(0);

        //    return item;
        //}

        //public TalkScript.ScriptItem Next()
        //{
        //    return (Start());
        //}

        //public TalkScript.ScriptItem Next(string userResponse)
        //{
        //    return (Start());
        //}

        public bool ConversationEnded { get; set; } = false;
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

        private string AwaitResponse()
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

        public string GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION index)
        {
            return (dataOvlRef.GetStringFromDataChunkList(DataOvlReference.DataChunkName.PHRASES_CONVERSATION, (int)index));
        }

        private void ProcessMultipleLines(List<TalkScript.ScriptLine> scriptLines, int nTalkLineIndex)
        {
            int skipCounter = -1;
            for (int i = 0; i < scriptLines.Count; i++)
            {
                if (skipCounter != -1 && skipCounter == 0)
                {
                    --skipCounter;
                    continue;
                }

                if (scriptLines[i].ContainsCommand(TalkScript.TalkCommand.AvatarsName) && !Npc.KnowTheAvatar())
                {
                    continue;
                }

                if (scriptLines[i].GetNumberOfScriptItems()==0)
                {
                    continue;
                }

                SkipInstruction skipInstruction = ProcessLine(scriptLines[i], nTalkLineIndex, i);
                if (skipCounter != -1) --skipCounter;
                switch (skipInstruction)
                {
                    case SkipInstruction.SkipToLabel:
                        // if I get a hop to a label instruction then we don't process the rest
                        return;
                    case SkipInstruction.SkipAfterNext:
                        skipCounter = 1;
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

        private SkipInstruction ProcessLine(TalkScript.ScriptLine scriptLine)
        {
           return ProcessLine(scriptLine, -1, -1);
        }

        private enum SkipInstruction { DontSkip = 0, SkipNext, SkipAfterNext, SkipToLabel };

        private SkipInstruction ProcessLine(TalkScript.ScriptLine scriptLine, int nTalkLineIndex, int nSplitLine)
        {
            // if they already know the avatar then they aren't going to ask again
            if (scriptLine.ContainsCommand(TalkScript.TalkCommand.AskName) && Npc.KnowTheAvatar())
            {
                return SkipInstruction.DontSkip;
            }

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
                    EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.YOU_SEE)));
                        //"You see "));
                }


                switch (item.Command)
                {
                    case TalkScript.TalkCommand.IfElseKnowsName:
                        Debug.Assert(nItems == 1);
                        if (Npc.KnowTheAvatar())
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
                        Debug.Assert(Npc.KnowTheAvatar());
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, ProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.AskName:
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.WHATS_YOUR_NAME)));
                        EnqueToOutputBuffer(item);
                        string avatarNameResponse = AwaitResponse();
                        if (avatarNameResponse.ToLower() == gameStateRef.AvatarsName.ToLower())
                        {
                            // i met them
                            gameStateRef.SetMetNPC(Npc);
                            EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.PLEASURE)));
                            break;
                        }
                        else
                        {
                            EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.IF_SAY_SO)));
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
                        return SkipInstruction.SkipToLabel;
                    case TalkScript.TalkCommand.EndCoversation:
                        EnqueToOutputBuffer(item);
                        ConversationEnded = true;
                        break;
                    case TalkScript.TalkCommand.Gold:
                        EnqueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.JoinParty:
                        if (gameStateRef.IsFullParty())
                        {
                            string noJoinResponse = GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.CANT_JOIN_1) +
                                GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.CANT_JOIN_2);
                            EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, noJoinResponse));
                                //"Thou hast no room for me in thy party! Seek me again if one of thy members doth leave thee.\n"));
                        }
                        else
                        {
                            EnqueToOutputBuffer(item);
                            ConversationEnded = true;
                        }
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
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.CANNOT_HELP)+"\n"));
                        break;
                    case TalkScript.TalkCommand.Unknown_Enter:
                        break;
                    case TalkScript.TalkCommand.Unknown_FF:
                        // appears to signify an empty section
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

            // when a new section comes up, we can add it and then proceed to the next line by adding it
            // this is perpetual as long as there is more to see
            conversationOrder.Add((int)TalkScript.TalkConstants.Description);
            conversationOrder.Add((int)TalkScript.TalkConstants.Greeting);
            conversationOrderScriptLines.Add(script.GetScriptLine(TalkScript.TalkConstants.Description));
            conversationOrderScriptLines.Add(script.GetScriptLine(TalkScript.TalkConstants.Greeting));

            // some of these operatoins can be expensive, so let's call them once and store instead
            bool npcKnowsAvatar = Npc.KnowTheAvatar();

            // the index into conversationOrder
            int nConversationIndex = 0;

            // while there are more conversation lines to process
            while (!ConversationEnded)
            {
                // if we do not have any conversation left, then we will prompt for questions
                ///// NO DIALOG LEFT - USER RESPONDS
                ///// This will result in processable conversation, so it will just fall through
                while (nConversationIndex >= conversationOrder.Count && !ConversationEnded)
                {
                    EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PromptUserForInput_UserInterest));
                    // we wait patiently for the user to respond
                    string userResponse = AwaitResponse();

                    //if (userResponse == string.Empty)
                    //{
                    //    ConversationEnded = true;
                    //    return;
                    //}
                    if (userResponse == string.Empty)
                    {
                        userResponse = "bye";
                    }

                    if (Npc.Script.QuestionAnswers.AnswerIsAvailable(userResponse))
                    {
                        // the user asked a question that we recognize, so let's process the answer
                        ProcessMultipleLines(Npc.Script.QuestionAnswers.GetQuestionAnswer(userResponse).Answer.SplitIntoSections(), -1);
                    }
                    else
                    {
                        // we didn't recognize the user input - we will tell them so
                        TalkScript.ScriptLine unrecognizedLine = new TalkScript.ScriptLine();
                        unrecognizedLine.AddScriptItem(new TalkScript.ScriptItem(TalkScript.TalkCommand.UserInputNotRecognized));
                        ProcessLine(unrecognizedLine);
                    }
                    
                    // one of the ProcessLine method calls told us that we are done talking
                    // this is okay to quit anytime because we have already populated the queues with the final commands
                    if (ConversationEnded)
                        return;
                }


                // the current talk line
                int nTalkLineIndex = conversationOrder[nConversationIndex];

                // the current ScriptLine
                TalkScript.ScriptLine currentLine;
                currentLine = script.GetScriptLine(conversationOrder[nConversationIndex]);
                // Split the line into sections. This greatly simplifies the proceeding loops.
                List<TalkScript.ScriptLine> splitLines = currentLine.SplitIntoSections();

                // currentLine = unsplit line with all content
                // splitLines = a list of all the split up lines
                // curentSplitLine = current section of the currentLine
                Debug.Assert(splitLines.Count > 0);

                /// If an AvatarsName is used in conversation, then we may need to process additional logic or ignore the line altogether
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

                const int STARTING_INDEX_FOR_LABEL = 0;

                ///// IT'S A LABEL
                // if we have just begun a label section, then let's handle it slightly difference then the normal conversation
                if (splitLines[STARTING_INDEX_FOR_LABEL].IsLabelDefinition())
                {
                    Debug.Assert(splitLines[STARTING_INDEX_FOR_LABEL].GetNumberOfScriptItems() == 2, "If it is a label definition, then it must have only 2 items defined in it");
                    int nLabel = splitLines[STARTING_INDEX_FOR_LABEL].GetScriptItem(1).LabelNum;
                    Debug.Assert(nLabel >= 0 && nLabel <= TalkScript.TOTAL_LABELS-1, "Label number must be between 0 and 9");
                    
                    // get the label object
                    TalkScript.ScriptTalkLabel scriptLabel = script.TalkLabels.Labels[nLabel];

                    // we ar going through each of the line sections, but are skipping the first one since we know it is just a label
                    // definition
                    ProcessMultipleLines(scriptLabel.InitialLine.SplitIntoSections(), nTalkLineIndex);

                    string userResponse = string.Empty;
                    if (scriptLabel.ContainsQuestions())
                    {
                        // need to figure out if we are going to ask a question...
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PromptUserForInput_NPCQuestion));
                        // we wait patiently for the user to respond
                        userResponse = AwaitResponse();
                    }
                    else
                    {
                        // is there an actual answer to the question?
                        // if not, then we have already processed our dialog line, let's move onto the next dialog item
                        nConversationIndex++;
                        continue ;
                    }

                    // There is an answer available from the NPC
                    if (scriptLabel.QuestionAnswers.AnswerIsAvailable(userResponse))
                    {
                        // let's get the answer details including the ScriptLine that will follow
                        TalkScript.ScriptQuestionAnswer qa = scriptLabel.QuestionAnswers.GetQuestionAnswer(userResponse);
                        TalkScript.ScriptLine npcResponseLine = qa.Answer;

                        ProcessMultipleLines(qa.Answer.SplitIntoSections(), nTalkLineIndex);
                    }
                    else // you have entered an answer that isn't in their dialog - so default answer
                    {
                        // Process default response
                        foreach (TalkScript.ScriptLine defaultLine in scriptLabel.DefaultAnswers)
                        {
                            ProcessMultipleLines(defaultLine.SplitIntoSections(), nTalkLineIndex);
                        }
                    }
                }
                else // it's not a label NOR is a question and answer section
                // it's just a simple text section (probably from the description or greeting)
                {
                    ProcessMultipleLines(splitLines, nTalkLineIndex);
                }

                // we have gone through all instructions, so lets move onto the next conversation line
                nConversationIndex++;
            }
        }
    }
}
