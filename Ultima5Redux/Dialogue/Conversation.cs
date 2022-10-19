using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Dialogue;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.Dialogue
{
    /// <summary>
    ///     A conversation with an NPC.
    ///     An instantiated object of class Conversation holds and controls all aspects of a conversation.
    /// </summary>
    public class Conversation
    {
        /// <summary>
        ///     The delegate for the notification that tells the user something was added to the queue
        /// </summary>
        /// <param name="conversation"></param>
        public delegate void EnqueuedScriptItem(Conversation conversation);

        /// <summary>
        ///     Instructs the calling method how to handle proceeding TalkScript lookups
        /// </summary>
        private enum SkipInstruction { DontSkip = 0, SkipNext, SkipAfterNext, SkipToLabel }

        /// <summary>
        ///     a list of conversation indexes that refer to the particular conversationOrderScriptLines[] we are on
        /// </summary>
        private readonly List<int> _conversationOrder = new();

        /// <summary>
        ///     All of the ScriptLines that we are capable of processing for the NPC
        /// </summary>
        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<TalkScript.ScriptLine> _conversationOrderScriptLines = new();

        /// <summary>
        ///     Game State used for determining if Avatar has met the NPC
        /// </summary>
        private readonly GameState _gameState;

        /// <summary>
        ///     The output buffer storing all outputs that will be read by outer program
        /// </summary>
        private readonly Queue<TalkScript.ScriptItem> _outputBufferQueue = new();

        /// <summary>
        ///     The users responses to promptings from the outputBufferQueue
        /// </summary>
        private readonly Queue<string> _responseQueue = new();

        /// <summary>
        ///     The script of the conversation
        /// </summary>
        private readonly TalkScript _script;

        private SkipInstruction _currentSkipInstruction = SkipInstruction.DontSkip;

        /// <summary>
        ///     Are we currently in rune mode? if so, I expect that TextProcessItem will be translating text to runic
        /// </summary>
        private bool _runeMode;

        /// <summary>
        ///     Has the conversation ended?
        /// </summary>
        public bool ConversationEnded { get; set; }

        /// <summary>
        ///     The callback for the notification that tells the user something was added to the queue
        /// </summary>
        public EnqueuedScriptItem EnqueuedScriptItemCallback { get; set; }

        public NonPlayerCharacterState TheNonPlayerCharacterState { get; }

        public bool CallForGuardsAfterConversation { get; private set; }

        /// <summary>
        ///     Construction a conversation
        /// </summary>
        /// <param name="state">The games current State</param>
        /// <param name="npcState"></param>
        public Conversation(GameState state, NonPlayerCharacterState npcState)
        {
            _script = npcState.NPCRef.Script;
            _gameState = state;
            TheNonPlayerCharacterState = npcState;
        }

        public Conversation(GameState state, NonPlayerCharacterState npcState, string customDialogueId)
        {
            _script = GameReferences.TalkScriptsRef.GetCustomTalkScript(customDialogueId);
            _gameState = state;
            TheNonPlayerCharacterState = npcState;
        }

        /// <summary>
        ///     Just chill out and wait for a response, but we introduce a small sleep so we don't stress out the CPU
        /// </summary>
        /// <returns></returns>
        private async Task AwaitResponse()
        {
            while (_responseQueue.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.1));
            }
        }

        /// <summary>
        ///     Add a ScriptItem to the output buffer that will be consumed by the outside process (ie. World)
        /// </summary>
        /// <param name="output">ScripItem to add to queue</param>
        private void EnqueueToOutputBuffer(TalkScript.ScriptItem output)
        {
            lock (((ICollection)_outputBufferQueue).SyncRoot)
            {
                _outputBufferQueue.Enqueue(output);
                EnqueuedScriptItemCallback(this);
            }
        }

        /// <summary>
        ///     Loops and waits for a response (response added to responseQueue)
        /// </summary>
        /// <returns>Users resonse</returns>
        private string GetResponse()
        {
            lock (((ICollection)_responseQueue).SyncRoot)
            {
                return _responseQueue.Dequeue();
            }
        }

        /// <summary>
        ///     Process an individual line with defaults
        /// </summary>
        /// <param name="scriptLine">the script line</param>
        /// <returns>a skip instruction for the proceeding lines</returns>
        private async Task ProcessLine(TalkScript.ScriptLine scriptLine)
        {
            await ProcessLine(scriptLine, -1, -1);
        }

        /// <summary>
        ///     Process an individual line.
        ///     The method evaluates each scriptitem within the line, adding items and text to the output buffer as required.
        ///     Mega function
        /// </summary>
        /// <remarks>If you do not SPLIT the line ahead of time then it is going to act wonky...</remarks>
        /// <param name="scriptLine"></param>
        /// <param name="nTalkLineIndex"></param>
        /// <param name="nSplitLine"></param>
        /// <returns></returns>
        private async Task ProcessLine(TalkScript.ScriptLine scriptLine, int nTalkLineIndex, int nSplitLine)
        {
            // if they already know the avatar then they aren't going to ask again
            if (scriptLine.ContainsCommand(TalkScript.TalkCommand.AskName) && TheNonPlayerCharacterState.HasMetAvatar)
            {
                _currentSkipInstruction = SkipInstruction.DontSkip;
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
                    EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                        GetConversationStr(DataOvlReference.ChunkPhrasesConversation.YOU_SEE) + " "));

                switch (item.Command)
                {
                    case TalkScript.TalkCommand.IfElseKnowsName:
                        Debug.Assert(nItems == 1);
                        if (TheNonPlayerCharacterState.HasMetAvatar)
                        {
                            // we continue to the next block, but skip the one after
                            _currentSkipInstruction = SkipInstruction.SkipAfterNext;
                            return;
                        }

                        // we skip the next block because it is the line used when we actually know the Avatar
                        _currentSkipInstruction = SkipInstruction.SkipNext;
                        return;
                    case TalkScript.TalkCommand.AvatarsName:
                        // we should already know if they know the avatars name....
                        Debug.Assert(TheNonPlayerCharacterState.HasMetAvatar);
                        EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                            TextProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.AskName:
                        EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                            GetConversationStr(DataOvlReference.ChunkPhrasesConversation.WHATS_YOUR_NAME)));
                        EnqueueToOutputBuffer(item);
                        // we actually wait in the function for the user to respond
                        await AwaitResponse();
                        string avatarNameResponse = GetResponse();
                        // did they actually provide the Avatars name?
                        if (string.Equals(avatarNameResponse, _gameState.AvatarsName,
                                StringComparison.CurrentCultureIgnoreCase))
                        {
                            // i met them
                            TheNonPlayerCharacterState.HasMetAvatar = true;
                            EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                                GetConversationStr(DataOvlReference.ChunkPhrasesConversation.PLEASURE)));
                        }
                        else
                        {
                            EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                                GetConversationStr(DataOvlReference.ChunkPhrasesConversation.IF_SAY_SO)));
                        }

                        break;
                    case TalkScript.TalkCommand.CallGuards:
                        EnqueueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.Change:
                        EnqueueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.DefineLabel:
                        // if I find a goto label, then i expect I have no further conversation lines left
                        // we are going to add the GotoLabel to the script
                        _conversationOrder.Add(_script.GetScriptLineLabelIndex(item.LabelNum));
                        _conversationOrderScriptLines.Add(
                            _script.GetScriptLine(_script.GetScriptLineLabelIndex(item.LabelNum)));
                        _currentSkipInstruction = SkipInstruction.SkipToLabel;
                        return;
                    case TalkScript.TalkCommand.EndConversation:
                        EnqueueToOutputBuffer(item);
                        ConversationEnded = true;
                        break;
                    case TalkScript.TalkCommand.Gold:
                        EnqueueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.JoinParty:
                        if (_gameState.CharacterRecords.IsFullParty())
                        {
                            string noJoinResponse =
                                GetConversationStr(DataOvlReference.ChunkPhrasesConversation.CANT_JOIN_1) +
                                GetConversationStr(DataOvlReference.ChunkPhrasesConversation.CANT_JOIN_2);
                            EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                                noJoinResponse));
                        }
                        else
                        {
                            EnqueueToOutputBuffer(item);
                            ConversationEnded = true;
                        }

                        break;
                    case TalkScript.TalkCommand.KarmaMinusOne:
                        EnqueueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.KarmaPlusOne:
                        EnqueueToOutputBuffer(item);
                        break;
                    case TalkScript.TalkCommand.KeyWait:
                        EnqueueToOutputBuffer(item);
                        await AwaitResponse();
                        GetResponse();
                        break;
                    case TalkScript.TalkCommand.NewLine: // Sutek is a great example
                        EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                            TextProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.Pause:
                        EnqueueToOutputBuffer(item);
                        //await Task.Delay(TimeSpan.FromSeconds(0.5f)).ConfigureAwait(false);
                        break;
                    case TalkScript.TalkCommand.PlainString:
                        // we put it through the processor to change the text around if we are wrapped in a rune tag
                        EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                            TextProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.Rune:
                        EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                            TextProcessItem(item)));
                        break;
                    case TalkScript.TalkCommand.UserInputNotRecognized:
                        EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                            GetConversationStr(DataOvlReference.ChunkPhrasesConversation.CANNOT_HELP) + "\n"));
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
                        throw new Ultima5ReduxException("We should never see the <OR> or <A2> code in conversation");
                    case TalkScript.TalkCommand.GotoLabel:
                        break;
                    case TalkScript.TalkCommand.PromptUserForInput_NPCQuestion:
                        break;
                    case TalkScript.TalkCommand.PromptUserForInput_UserInterest:
                        break;
                    default:
                        throw new Ultima5ReduxException("Received TalkCommand I wasn't expecting during conversation");
                }

                nItem++;
                // while we still have more items in the current split line
            } while (nItem < nItems);

            _currentSkipInstruction = SkipInstruction.DontSkip;
        }

        /// <summary>
        ///     Super function - Processes a list of ScriptLines. It evaluates each line and makes logical determinations to skip
        ///     some lines if required
        /// </summary>
        /// <param name="scriptLines">the script line to process</param>
        /// <param name="nTalkLineIndex">
        ///     where we are in the conversation - it really only cares if you are on the first line of
        ///     conversation
        /// </param>
        private async Task ProcessMultipleLines(IReadOnlyList<TalkScript.SplitScriptLine> scriptLines,
            int nTalkLineIndex)
        {
            // how many items shall we skip? if == -1, then don't skip
            int skipCounter = -1;

            for (int i = 0; i < scriptLines.Count; i++)
            {
                // if we still have some counts on the skipCounter, then decrement and skip 
                if (skipCounter == 0)
                {
                    --skipCounter;
                    continue;
                }

                // if the line refers to the Avatar, but the NPC doesn't know the Avatar then just skip the line
                if (scriptLines[i].ContainsCommand(TalkScript.TalkCommand.AvatarsName) &&
                    !TheNonPlayerCharacterState.HasMetAvatar) continue;

                // If there are no script items, then just skip
                // Note: this shouldn't really happen, but it does, so maybe one day find out why they are being added
                if (scriptLines[i].NumberOfScriptItems == 0) continue;

                // process the individual line
                // it will return a skip instruction, telling us how to to handle subsequent calls
                await ProcessLine(scriptLines[i], nTalkLineIndex, i);

                SkipInstruction skipInstruction = _currentSkipInstruction;

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
        ///     Simple converter that takes in a ScriptItem and returns a string
        /// </summary>
        /// <param name="item">item to process</param>
        /// <returns>string equivalent of the ScriptItem</returns>
        private string TextProcessItem(TalkScript.ScriptItem item)
        {
            switch (item.Command)
            {
                case TalkScript.TalkCommand.AvatarsName:
                    return _gameState.AvatarsName;
                case TalkScript.TalkCommand.NewLine:
                    return "\n";
                case TalkScript.TalkCommand.PlainString:
                    return item.StringData;
                case TalkScript.TalkCommand.Rune:
                    _runeMode = !_runeMode;
                    return _runeMode ? " " : string.Empty;
                default:
                    throw new Ultima5ReduxException("Passed in an unsupported TalkCommand: " + item.Command +
                                                    " to the TextProcessItem method");
            }
        }

        /// <summary>
        ///     Add a user response to the queue
        /// </summary>
        /// <param name="response">the string response to add</param>
        public void AddUserResponse(string response)
        {
            lock (((ICollection)_responseQueue).SyncRoot)
            {
                _responseQueue.Enqueue(response);
            }
        }

        /// <summary>
        ///     Begins the conversation with the NPC.
        ///     Method will block for input if required.
        /// </summary>
        public async Task BeginConversation()
        {
            if (EnqueuedScriptItemCallback == null)
                throw new Ultima5ReduxException(
                    "Called BeginConversation without declaring a EnqueuedScriptItemCallback.");

            Console.WriteLine(@"---- PRE-CONVERSATION SCRIPT -----");
            _script.PrintComprehensiveScript();

            Console.WriteLine(@"---- STARTING CONVERSATION -----");

            // when a new section comes up, we can add it and then proceed to the next line by adding it
            // this is perpetual as long as there is more to see
            _conversationOrder.Add((int)TalkScript.TalkConstants.Description);
            _conversationOrder.Add((int)TalkScript.TalkConstants.Greeting);
            _conversationOrderScriptLines.Add(_script.GetScriptLine(TalkScript.TalkConstants.Description));
            _conversationOrderScriptLines.Add(_script.GetScriptLine(TalkScript.TalkConstants.Greeting));

            // some of these operations can be expensive, so let's call them once and store instead
            //bool npcKnowsAvatar = TheNonPlayerCharacterState.HasMetAvatar;

            // the index into conversationOrder
            int nConversationIndex = 0;

            // while there are more conversation lines to process
            while (!ConversationEnded)
            {
                // if we do not have any conversation left, then we will prompt for questions
                ///// NO DIALOG LEFT - USER RESPONDS
                ///// This will result in processable conversation, so it will just fall through
                while (nConversationIndex >= _conversationOrder.Count)
                {
                    EnqueueToOutputBuffer(
                        new TalkScript.ScriptItem(TalkScript.TalkCommand.PromptUserForInput_UserInterest));
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
                        TalkScript.ScriptLine scriptLine = new();
                        scriptLine.AddScriptItem(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                            GetConversationStr(DataOvlReference.ChunkPhrasesConversation.MY_NAME_IS) + " "));
                        await ProcessLine(scriptLine);
                    }

                    if (_script.QuestionAnswers.AnswerIsAvailable(userResponse))
                    {
                        // the user asked a question that we recognize, so let's process the answer
                        await ProcessMultipleLines(
                            _script.QuestionAnswers.GetQuestionAnswer(userResponse).Answer.SplitIntoSections(), -1);
                    }
                    else
                    {
                        // we didn't recognize the user input - we will tell them so
                        TalkScript.ScriptLine unrecognizedLine = new();
                        unrecognizedLine.AddScriptItem(
                            new TalkScript.ScriptItem(TalkScript.TalkCommand.UserInputNotRecognized));
                        await ProcessLine(unrecognizedLine);
                    }

                    // one of the ProcessLine method calls told us that we are done talking
                    // this is okay to quit anytime because we have already populated the queues with the final commands
                    if (ConversationEnded)
                        return;
                }

                // the current talk line
                int nTalkLineIndex = _conversationOrder[nConversationIndex];

                // the current ScriptLine
                TalkScript.ScriptLine currentLine = _script.GetScriptLine(_conversationOrder[nConversationIndex]);
                // Split the line into sections. This greatly simplifies the proceeding loops.
                List<TalkScript.SplitScriptLine> splitLines = currentLine.SplitIntoSections();

                // currentLine = unsplit line with all content
                // splitLines = a list of all the split up lines
                // currentSplitLine = current section of the currentLine
                Debug.Assert(splitLines.Count > 0);

                // If an AvatarsName is used in conversation, then we may need to process additional logic or ignore the line altogether
                // if it's a greeting AND her greeting includes my name AND they have NOT yet met the avatar  
                // OR if the Name line contains an IfElseKnowsName (#Eb)
                if (!TheNonPlayerCharacterState.HasMetAvatar &&
                    _conversationOrder[nConversationIndex] == (int)TalkScript.TalkConstants.Greeting &&
                    (currentLine.ContainsCommand(TalkScript.TalkCommand.AvatarsName) || _script
                        .GetScriptLine(TalkScript.TalkConstants.Name)
                        .ContainsCommand(TalkScript.TalkCommand.IfElseKnowsName)))
                    // randomly add an introduction of the Avatar since they haven't met him
                    if (Utils.OneInXOdds(2))
                        // okay, tell them who you are
                        EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                            "\nI am called " + TheNonPlayerCharacterState.NPCRef.Name));

                // if in label && next line include <AvatarName>, then skip label
                const int StartingIndexForLabel = 0;

                ///// IT'S A LABEL
                // if we have just begun a label section, then let's handle it slightly difference then the normal conversation
                if (splitLines[StartingIndexForLabel].IsLabelDefinition())
                {
                    Debug.Assert(splitLines[StartingIndexForLabel].NumberOfScriptItems == 2,
                        "If it is a label definition, then it must have only 2 items defined in it");
                    int nLabel = splitLines[StartingIndexForLabel].GetScriptItem(1).LabelNum;
                    Debug.Assert(nLabel >= 0 && nLabel <= TalkScript.TOTAL_LABELS - 1,
                        "Label number must be between 0 and 9");

                    // get the label object
                    TalkScript.ScriptTalkLabel scriptLabel = _script.TalkLabels.GetScriptLabel(nLabel);

                    // Sept 23, 2019 - if we are in a label, but don't know the Avatar's name, then we just move on
                    // if we don't then (#Eb) can expect an answer to a question that we never show the user because
                    // they don't know the Avatar
                    if (scriptLabel.InitialLine.ContainsCommand(TalkScript.TalkCommand.AvatarsName) &&
                        !TheNonPlayerCharacterState.HasMetAvatar)
                    {
                        nConversationIndex++;
                        continue;
                    }

                    // we ar going through each of the line sections, but are skipping the first one since we know it is just a label
                    // definition
                    await ProcessMultipleLines(scriptLabel.InitialLine.SplitIntoSections(), nTalkLineIndex);

                    string userResponse;
                    if (scriptLabel.ContainsQuestions())
                    {
                        int nTimes = 0;
                        do
                        {
                            // need to figure out if we are going to ask a question...
                            if (nTimes++ == 0)
                            {
                                EnqueueToOutputBuffer(
                                    new TalkScript.ScriptItem(TalkScript.TalkCommand.PromptUserForInput_NPCQuestion));
                            }
                            else
                            {
                                EnqueueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString,
                                    GetConversationStr(DataOvlReference.ChunkPhrasesConversation.WHAT_YOU_SAY)));
                                EnqueueToOutputBuffer(
                                    new TalkScript.ScriptItem(TalkScript.TalkCommand.PromptUserForInput_NPCQuestion));
                            }
                            // we wait patiently for the user to respond

                            await AwaitResponse();
                            userResponse = GetResponse();
                        } while (userResponse == string.Empty);
                    }
                    else
                    {
                        // is there an actual answer to the question?
                        // if not, then we have already processed our dialog line, let's move onto the next dialog item
                        nConversationIndex++;
                        continue;
                    }

                    // There is an answer available from the NPC
                    if (scriptLabel.QuestionAnswers.AnswerIsAvailable(userResponse))
                    {
                        // let's get the answer details including the ScriptLine that will follow
                        TalkScript.ScriptQuestionAnswer qa =
                            scriptLabel.QuestionAnswers.GetQuestionAnswer(userResponse);

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

        /// <summary>
        ///     Remove a ScriptItem from the output buffer (typically called by outside process ie. World)
        /// </summary>
        /// <returns>The next ScripItem</returns>
        public TalkScript.ScriptItem DequeueFromOutputBuffer()
        {
            lock (((ICollection)_outputBufferQueue).SyncRoot)
            {
                return _outputBufferQueue.Dequeue();
            }
        }

        /// <summary>
        ///     Quick method to return a pre-defined conversation string from Data.ovl
        /// </summary>
        /// <param name="index">index into string array (use DataOvlReference.CHUNK__PHRASES_CONVERSATION)</param>
        /// <returns>associated string</returns>
        public string GetConversationStr(DataOvlReference.ChunkPhrasesConversation index)
        {
            char[] trimChars = { '"' };
            string convStr = GameReferences.DataOvlRef
                .GetStringFromDataChunkList(DataOvlReference.DataChunkName.PHRASES_CONVERSATION, (int)index).Trim();
            convStr = convStr.Trim(trimChars);
            return convStr;
        }

        public TurnResults TheTurnResults { get; } = new();

        public static TileReference.SpriteIndex GetSpriteForChangeGiveItem(TalkScript.ScriptItem scriptItem) =>
            scriptItem.ItemAdditionalData switch
            {
                65 => // food
                    TileReference.SpriteIndex.ItemFood,
                67 => //keys
                    TileReference.SpriteIndex.ItemKey,
                75 => // skull key (serpents hold/Kristi) "skul"
                    TileReference.SpriteIndex.ItemKey,
                70 => // grapple (empath abbey/Lord Michael) "grap"
                    TileReference.SpriteIndex.Nothing,
                72 => //sextant (Lighthouse of Greyhaven/David) "sext"
                    TileReference.SpriteIndex.Nothing,
                73 => // spyglasses (Minor Keep/Seggallion) "spy" (virtue)
                    TileReference.SpriteIndex.Telescope,
                74 => // badge (Windmere/Elistaria) "oppr", "impera" 
                    TileReference.SpriteIndex.Nothing,
                8 => // jewelled sword (Minor Keep/Thrud) "jewe"
                    TileReference.SpriteIndex.ItemWeapon,
                28 => // jeweled shield (Minor Keep/Thrud) "jewe"
                    TileReference.SpriteIndex.ItemShield,
                _ => TileReference.SpriteIndex.Nothing
            };

        public static string GetNameForChangeGiveItem(TalkScript.ScriptItem scriptItem)
        {
            return scriptItem.ItemAdditionalData switch
            {
                65 => // food
                    "food",
                67 => //keys
                    "key",
                75 => // skull key (serpents hold/Kristi) "skul"
                    "skull key",
                70 => // grapple (empath abbey/Lord Michael) "grap"
                    "grappling hook",
                72 => //sextant (Lighthouse of Greyhaven/David) "sext"
                    "sextant",
                73 => // spyglasses (Minor Keep/Seggallion) "spy" (virtue)
                    "telescope",
                74 => // badge (Windmere/Elistaria) "oppr", "impera" 
                    "black badge",
                8 => // jewelled sword (Minor Keep/Thrud) "jewe"
                    "jewelled sword",
                28 => // jeweled shield (Minor Keep/Thrud) "jewe"
                    "jewelled shield",
                _ => "nothing"
            };
        }

        
        public void ProcessScriptItem(TalkScript.ScriptItem scriptItem)
        {
            switch (scriptItem.Command)
            {
                case TalkScript.TalkCommand.KarmaPlusOne:
                    _gameState.ChangeKarma(1, TheTurnResults);
                    break;
                case TalkScript.TalkCommand.KarmaMinusOne:
                    _gameState.ChangeKarma(-1, TheTurnResults);
                    break;
                case TalkScript.TalkCommand.CallGuards:
                    _gameState.TheVirtualMap.IsWantedManByThePoPo = true;
                    CallForGuardsAfterConversation = true;
                    break;
                case TalkScript.TalkCommand.Gold:
                    _gameState.PlayerInventory.TheProvisions.AddOrRemoveProvisionQuantity(
                        ProvisionReferences.SpecificProvisionType.Gold, -scriptItem.ItemAdditionalData, TheTurnResults);
                    break;
                case TalkScript.TalkCommand.JoinParty:
                    _gameState.CharacterRecords.AddMemberToParty(TheNonPlayerCharacterState.NPCRef);
                    break;
                case TalkScript.TalkCommand.Change:
                    // add inventory item to inventory
                    switch (scriptItem.ItemAdditionalData)
                    {
                        case 65: // food
                            _gameState.PlayerInventory.TheProvisions.AddOrRemoveProvisionQuantity(
                                ProvisionReferences.SpecificProvisionType.Food, 1, TheTurnResults);
                            break;
                        case 67: //keys
                            _gameState.PlayerInventory.TheProvisions.AddOrRemoveProvisionQuantity(
                                ProvisionReferences.SpecificProvisionType.Keys, 1, TheTurnResults);
                            break;
                        case 75: // skull key (serpents hold/Kristi) "skul"
                            _gameState.PlayerInventory.TheProvisions.AddOrRemoveProvisionQuantity(
                                ProvisionReferences.SpecificProvisionType.SkullKeys, 1, TheTurnResults);
                            break;
                        case 70: // grapple (empath abbey/Lord Michael) "grap"
                            _gameState.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Grapple]
                                .Quantity = 1;
                            break;
                        case 72: //sextant (Lighthouse of Greyhaven/David) "sext"
                            _gameState.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Sextant]
                                .Quantity = 1;
                            break;
                        case 73: // spyglasses (Minor Keep/Seggallion) "spy" (virtue)
                            _gameState.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Spyglass]
                                .Quantity = 1;
                            break;
                        case 74: // badge (Windmere/Elistaria) "oppr", "impera" 
                            _gameState.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.BlackBadge]
                                .Quantity = 1;
                            break;
                        case 8: // jewelled sword (Minor Keep/Thrud) "jewe"
                            _gameState.PlayerInventory.TheWeapons
                                .Items[WeaponReference.SpecificWeaponType.JeweledSword].Quantity++;
                            break;
                        case 28: // jeweled shield (Minor Keep/Thrud) "jewe"
                            _gameState.PlayerInventory.TheWeapons
                                .Items[WeaponReference.SpecificWeaponType.JewelShield].Quantity++;
                            break;
                    }

                    break;
                case TalkScript.TalkCommand.PlainString:
                case TalkScript.TalkCommand.AvatarsName:
                case TalkScript.TalkCommand.EndConversation:
                case TalkScript.TalkCommand.Pause:
                case TalkScript.TalkCommand.Or:
                case TalkScript.TalkCommand.AskName:
                case TalkScript.TalkCommand.IfElseKnowsName:
                case TalkScript.TalkCommand.NewLine:
                case TalkScript.TalkCommand.Rune:
                case TalkScript.TalkCommand.KeyWait:
                case TalkScript.TalkCommand.StartLabelDefinition:
                case TalkScript.TalkCommand.StartNewSection:
                case TalkScript.TalkCommand.Unknown_Enter:
                case TalkScript.TalkCommand.GotoLabel:
                case TalkScript.TalkCommand.DefineLabel:
                case TalkScript.TalkCommand.DoNothingSection:
                case TalkScript.TalkCommand.PromptUserForInput_NPCQuestion:
                case TalkScript.TalkCommand.PromptUserForInput_UserInterest:
                case TalkScript.TalkCommand.UserInputNotRecognized:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}