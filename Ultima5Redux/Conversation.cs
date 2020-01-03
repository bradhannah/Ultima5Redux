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
    /// <summary>
    /// A conversation with an NPC.
    /// An instantiated object of class Conversation holds and controls all aspects of a conversation.
    /// </summary>
    public class Conversation
    {
        #region Public Properties
        /// <summary>
        /// The NPC with whom you are having a conversation
        /// </summary>
        public NonPlayerCharacterReference Npc { get; }

        /// <summary>
        /// Has the conversation ended?
        /// </summary>
        public bool ConversationEnded { get; set; } = false;
        #endregion

        #region Private Variables
        /// <summary>
        /// The script of the conversation
        /// </summary>
        private TalkScript script;
        /// <summary>
        /// Data.OVL reference used for grabbing predefined conversation strings
        /// </summary>
        private DataOvlReference dataOvlRef;
        /// <summary>
        /// Game State used for determining if Avatar has met the NPC
        /// </summary>
        private GameState gameStateRef;
        /// <summary>
        /// The output buffer storing all outputs that will be read by outer program
        /// </summary>
        private Queue<TalkScript.ScriptItem> outputBufferQueue = new Queue<TalkScript.ScriptItem>();
        /// <summary>
        /// The users responses to promptings from the outputBufferQueue
        /// </summary>
        private Queue<string> responseQueue = new Queue<string>();
        /// <summary>
        /// Are we currently in rune mode? if so, I expect that TextProcessItem will be translating text to runic
        /// </summary>
        private bool runeMode = false;
        /// <summary>
        /// a list of conversation indexes that refer to the particular conversationOrderScriptLines[] we are on
        /// </summary>
        private List<int> conversationOrder = new List<int>();
        /// <summary>
        /// All of the scriptlines that we are capable of processing for the NPC
        /// </summary>
        private List<TalkScript.ScriptLine> conversationOrderScriptLines = new List<TalkScript.ScriptLine>();

        private SkipInstruction currentSkipInstruction = SkipInstruction.DontSkip;
        #endregion

        #region Delegates/Callbacks
        /// <summary>
        /// The delegate for the notification that tells the user something was added to the queue
        /// </summary>
        /// <param name="conversation"></param>
        public delegate void EnqueuedScriptItem(Conversation conversation);
        /// <summary>
        /// The callback for the notification that tells the user something was added to the queue
        /// </summary>
        public EnqueuedScriptItem EnqueuedScriptItemCallback;
        #endregion

        #region Constants and Enums
        /// <summary>
        /// Instructs the calling method how to handle proceeding talkscript lookups
        /// </summary>
        private enum SkipInstruction { DontSkip = 0, SkipNext, SkipAfterNext, SkipToLabel };
        #endregion

        #region Constructors
        /// <summary>
        /// Construction a conversation
        /// </summary>
        /// <param name="npc">NPC you will have a conversation with</param>
        /// <param name="state">The games current State</param>
        /// <param name="dataOvlRef">Data.OVL for reference</param>
        public Conversation(NonPlayerCharacterReference npc, GameState state, DataOvlReference dataOvlRef)
        {
            this.Npc = npc;
            script = npc.Script;
            this.gameStateRef = state;
            this.dataOvlRef = dataOvlRef;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Simple converter that takes in a ScriptItem and returns a string
        /// </summary>
        /// <param name="item">item to process</param>
        /// <returns>string equivelant of the scriptitem</returns>
        private string TextProcessItem(TalkScript.ScriptItem item)
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
                    if (runeMode) return " ";
                    return string.Empty;
                default:
                    throw new Exception("Passed in an unsupported TalkCommand: " + item.Command.ToString() + " to the TextProcessItem method");
            }
        }

        /// <summary>
        /// Just chill out and wait for a response, but we introduce a small sleep so we don't stress out the CPU
        /// </summary>
        /// <returns></returns>
        async Task AwaitResponse()
        {
            while (responseQueue.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.1));
            }
        }

        /// <summary>
        /// Loops and waits for a response (response added to responseQueue)
        /// </summary>
        /// <returns>Users resonse</returns>
        private string GetResponse()
        {
            lock (((ICollection)responseQueue).SyncRoot)
            {
                return responseQueue.Dequeue();
            }
        }


        /// <summary>
        /// Add a ScriptItem to the output buffer that will be consumed by the outside process (ie. World)
        /// </summary>
        /// <param name="output">ScripItem to add to queue</param>
        private void EnqueToOutputBuffer(TalkScript.ScriptItem output)
        {
            lock (((ICollection)outputBufferQueue).SyncRoot)
            {
                outputBufferQueue.Enqueue(output);
                EnqueuedScriptItemCallback(this);
            }
        }

        /// <summary>
        /// Super function - Processes a list of ScriptLines. It evaluates each line and makes logical determinations to skip
        /// some lines if required
        /// </summary>
        /// <param name="scriptLines">the script line to process</param>
        /// <param name="nTalkLineIndex">where we are in the conversation - it really only cares if you are on the first line of conversation</param>
        async private Task ProcessMultipleLines(List<TalkScript.SplitScriptLine> scriptLines, int nTalkLineIndex)
        {
            // how many items shall we skip? if == -1, then don't skip
            int skipCounter = -1;

            for (int i = 0; i < scriptLines.Count; i++)
            {
                // if we still have some counts on the skipCounter, then decrement and skip 
                if (skipCounter != -1 && skipCounter == 0)
                {
                    --skipCounter;
                    continue;
                }

                // if the line refers to the Avatar, but the NPC doesn't know the Avatar then just skip the line
                if (scriptLines[i].ContainsCommand(TalkScript.TalkCommand.AvatarsName) && !Npc.KnowTheAvatar)
                {
                    continue;
                }

                // If there are no script items, then just skip
                // todo: this shouldn't really happen, but it does, so maybe one day find out why they are being added
                if (scriptLines[i].NumberOfScriptItems== 0)
                {
                    continue;
                }

                // process the individual line
                // it will return a skip instruction, telling us how to to handle subsequent calls
                await ProcessLine(scriptLines[i], nTalkLineIndex, i);
                
                SkipInstruction skipInstruction = currentSkipInstruction;

                if (skipCounter != -1) --skipCounter;
                
                // process our new skip instruction
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

        /// <summary>
        /// Process an individual line with defaults
        /// </summary>
        /// <param name="scriptLine">the script line</param>
        /// <returns>a skip instruction for the proceeding lines</returns>
        async private Task ProcessLine(TalkScript.ScriptLine scriptLine)
        {
            await ProcessLine(scriptLine, -1, -1);
        }

        /// <summary>
        /// Process an individual line. 
        /// The method evaluates each scriptitem within the line, adding items and text to the output buffer as required.
        /// Mega function
        /// </summary>
        /// <remarks>If you do not SPLIT the line ahead of time then it is going to act wonky...</remarks>
        /// <param name="scriptLine"></param>
        /// <param name="nTalkLineIndex"></param>
        /// <param name="nSplitLine"></param>
        /// <returns></returns>
        async private Task ProcessLine(TalkScript.ScriptLine scriptLine, int nTalkLineIndex, int nSplitLine)
        {
            // if they already know the avatar then they aren't going to ask again
            if (scriptLine.ContainsCommand(TalkScript.TalkCommand.AskName) && Npc.KnowTheAvatar)
            {
                currentSkipInstruction = SkipInstruction.DontSkip;
                return;
            }

            int nItem = 0;
            int nItems = scriptLine.NumberOfScriptItems;
            do
            {
                TalkScript.ScriptItem item = scriptLine.GetScriptItem(nItem);

                // if this is the very first position of conversation
                // we must describe what we "see"
                if (nTalkLineIndex == (int)TalkScript.TalkConstants.Description && nSplitLine == 0 && nItem == 0)
                {
                    EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.YOU_SEE)+" "));
                }

                switch (item.Command)
                {
                    case TalkScript.TalkCommand.IfElseKnowsName:
                        Debug.Assert(nItems == 1);
                        if (Npc.KnowTheAvatar)
                        {
                            // we continue to the next block, but skip the one after
                            currentSkipInstruction = SkipInstruction.SkipAfterNext;
                            return;
                        }
                        else
                        {
                            // we skip the next block because it is the line used when we actually know the Avatar
                            currentSkipInstruction = SkipInstruction.SkipNext;
                            return;
                        }
                    case TalkScript.TalkCommand.AvatarsName:
                        // we should already know if they know the avatars name....
                        Debug.Assert(Npc.KnowTheAvatar);
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, TextProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.AskName:
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.WHATS_YOUR_NAME)));
                        EnqueToOutputBuffer(item);
                        // we actually wait in the function for the user to respond
                        await AwaitResponse();
                        string avatarNameResponse = GetResponse();
                        // did they actually provide the Avatars name?
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
                        currentSkipInstruction = SkipInstruction.SkipToLabel;
                        return;
                        //return SkipInstruction.SkipToLabel;
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
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, TextProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.Pause:
                        EnqueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.PlainString:
                        // we put it through the processor to change the text around if we are wrapped in a rune tag
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, TextProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.Rune:
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, TextProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.UserInputNotRecognized:
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.CANNOT_HELP) + "\n"));
                        break;
                    case TalkScript.TalkCommand.Unknown_Enter:
                        break;
                    case TalkScript.TalkCommand.DoNothingSection:
                        // appears to signify an empty section
                        break;
                    case TalkScript.TalkCommand.StartLabelDefinition:
                        // dirty - advance past the label that it will sink in...
                        nItem++;
                        break;
                    case TalkScript.TalkCommand.Or:
                    case TalkScript.TalkCommand.StartNewSection:
                        throw new Exception("We should never see the <OR> or <A2> code in conversation");
                    default:
                        throw new Exception("Recieved TalkCommand I wasn't expecting during conversation");

                }
                nItem++;
                // while we still have more items in the current split line
            } while (nItem < nItems);
            currentSkipInstruction = SkipInstruction.DontSkip;
            return;
            //return SkipInstruction.DontSkip;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Add a user response to the queue
        /// </summary>
        /// <param name="response">the string response to add</param>
        public void AddUserResponse(string response)
        {
            lock (((ICollection)responseQueue).SyncRoot)
            {
                responseQueue.Enqueue(response);
            }
        }

        /// <summary>
        /// Remove a ScriptItem from the output buffer (typically called by outside process ie. World)
        /// </summary>
        /// <returns>The next ScripItem</returns>
        public TalkScript.ScriptItem DequeueFromOutputBuffer()
        {
            lock (((ICollection)outputBufferQueue).SyncRoot)
            {
                return outputBufferQueue.Dequeue();
            }
        }

        /// <summary>
        /// Quick method to return a pre-defined conversation string from Data.ovl
        /// </summary>
        /// <param name="index">index into string array (use DataOvlReference.CHUNK__PHRASES_CONVERSATION)</param>
        /// <returns>associated string</returns>
        public string GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION index)
        {
            char[] trimChars = new[] { '"' };
            string convStr = dataOvlRef.GetStringFromDataChunkList(DataOvlReference.DataChunkName.PHRASES_CONVERSATION, (int)index).Trim();
            convStr = convStr.Trim(trimChars);
            return (convStr);
        }
        #endregion

        #region Asynchronous operations 

        /// <summary>
        /// Begins the conversation with the NPC. 
        /// Method will block for input if required.
        /// </summary>
        async public Task BeginConversation()
        {
            if (this.EnqueuedScriptItemCallback == null)
            {
                throw new Exception("Called BeginConversation without declaring a EnqueuedScriptItemCallback.");
            }

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
            bool npcKnowsAvatar = Npc.KnowTheAvatar;

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

                    await AwaitResponse();
                    string userResponse = GetResponse();

                    if (userResponse == string.Empty)
                    {
                        userResponse = TalkScript.TalkConstants.Bye.ToString().ToLower();
                    }
                    // if someone has asked their name then we need to add "My name is " to beginning 
                    else if (userResponse.ToLower() == "name")
                    {
                        TalkScript.ScriptLine scriptLine = new TalkScript.ScriptLine();
                        scriptLine.AddScriptItem(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, 
                            GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.MY_NAME_IS)+" "));
                        await ProcessLine(scriptLine);
                    }

                    if (Npc.Script.QuestionAnswers.AnswerIsAvailable(userResponse))
                    {
                        // the user asked a question that we recognize, so let's process the answer
                        await ProcessMultipleLines(Npc.Script.QuestionAnswers.GetQuestionAnswer(userResponse).Answer.SplitIntoSections(), -1);
                    }
                    else
                    {
                        // we didn't recognize the user input - we will tell them so
                        TalkScript.ScriptLine unrecognizedLine = new TalkScript.ScriptLine();
                        unrecognizedLine.AddScriptItem(new TalkScript.ScriptItem(TalkScript.TalkCommand.UserInputNotRecognized));
                        await ProcessLine(unrecognizedLine);
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
                List<TalkScript.SplitScriptLine> splitLines = currentLine.SplitIntoSections();

                // currentLine = unsplit line with all content
                // splitLines = a list of all the split up lines
                // curentSplitLine = current section of the currentLine
                Debug.Assert(splitLines.Count > 0);

                /// If an AvatarsName is used in conversation, then we may need to process additional logic or ignore the line altogether
                // if it's a greeting AND her greeting includes my name AND they have NOT yet met the avatar  
                // OR if the Name line contains an IfElseKnowsName (#Eb)
                if ((!npcKnowsAvatar && conversationOrder[nConversationIndex] == (int)TalkScript.TalkConstants.Greeting) &&
                                 (currentLine.ContainsCommand(TalkScript.TalkCommand.AvatarsName) ||
                                 script.GetScriptLine(TalkScript.TalkConstants.Name).ContainsCommand(TalkScript.TalkCommand.IfElseKnowsName)))
                {
                    // randomly add an introduction of the Avatar since they haven't met him
                    if (gameStateRef.OneInXOdds(2) || true)
                    {
                        // okay, tell them who you are
                        EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, "\nI am called " + Npc.Name));
                    }
                }

                // if in label && next line include <AvatarName>, then skip label
                const int STARTING_INDEX_FOR_LABEL = 0;

                ///// IT'S A LABEL
                // if we have just begun a label section, then let's handle it slightly difference then the normal conversation
                if (splitLines[STARTING_INDEX_FOR_LABEL].IsLabelDefinition())
                {
                    Debug.Assert(splitLines[STARTING_INDEX_FOR_LABEL].NumberOfScriptItems== 2, "If it is a label definition, then it must have only 2 items defined in it");
                    int nLabel = splitLines[STARTING_INDEX_FOR_LABEL].GetScriptItem(1).LabelNum;
                    Debug.Assert(nLabel >= 0 && nLabel <= TalkScript.TOTAL_LABELS-1, "Label number must be between 0 and 9");
                    
                    // get the label object
                    TalkScript.ScriptTalkLabel scriptLabel = script.TalkLabels.GetScriptLabel(nLabel);

                    // Sept 23, 2019 - if we are in a label, but don't know the Avatar's name, then we just move on
                    // if we don't then (#Eb) can expect an answer to a question that we never show the user because
                    // they don't know the Avatar
                    if (scriptLabel.InitialLine.ContainsCommand(TalkScript.TalkCommand.AvatarsName) && !npcKnowsAvatar)
                    {
                        nConversationIndex++;
                        continue;
                    }

                    // we ar going through each of the line sections, but are skipping the first one since we know it is just a label
                    // definition
                    await ProcessMultipleLines(scriptLabel.InitialLine.SplitIntoSections(), nTalkLineIndex);

                    string userResponse = string.Empty;
                    if (scriptLabel.ContainsQuestions())
                    {
                        int nTimes = 0;
                        do
                        {
                            // need to figure out if we are going to ask a question...
                            if (nTimes++ == 0)
                            {
                                EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PromptUserForInput_NPCQuestion));
                            }
                            else
                            {
                                EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.WHAT_YOU_SAY)));//"What didst thou say?"));
                                EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PromptUserForInput_NPCQuestion));
                            }
                            // we wait patiently for the user to respond

                            await AwaitResponse();
                            userResponse = GetResponse();
                        } while (userResponse == String.Empty);
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

                        await ProcessMultipleLines(qa.Answer.SplitIntoSections(), nTalkLineIndex);
                    }
                    else // you have entered an answer that isn't in their dialog - so default answer
                    {
                        // Process default response
                        foreach (TalkScript.ScriptLine defaultLine in scriptLabel.DefaultAnswers)
                        {
                            await ProcessMultipleLines(defaultLine.SplitIntoSections(), nTalkLineIndex);
                        }
                    }
                }
                else // it's not a label NOR is a question and answer section
                // it's just a simple text section (probably from the description or greeting)
                {
                    await ProcessMultipleLines(splitLines, nTalkLineIndex);
                }

                // we have gone through all instructions, so lets move onto the next conversation line
                nConversationIndex++;
            }
        }
        #endregion
    }
}
