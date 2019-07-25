using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{ 
    class Conversation
    {
        private NonPlayerCharacters.NonPlayerCharacter npc;
        private TalkScript script;
        private TalkScript.ScriptLine currentLine;
        private GameState state;
        private Queue<TalkScript.ScriptItem> outputBufferQueue = new Queue<TalkScript.ScriptItem>();
        private Queue<string> responseQueue = new Queue<string>();

        public delegate void EnqueuedScriptItem(Conversation conversation);
        public EnqueuedScriptItem EnqueuedScriptItemCallback;

        public Conversation(NonPlayerCharacters.NonPlayerCharacter npc, GameState state)
        {
            this.npc = npc;
            script = npc.Script;
            this.state = state;
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

        private string ProcessItem(TalkScript.ScriptItem item)
        {
            switch (item.Command)
            {
                case TalkScript.TalkCommand.AvatarsName:
                    return state.AvatarsName;
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

        public void AddUserResponse(string response)
        {
            responseQueue.Enqueue(response);
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

        /// <summary>
        /// Simulate a conversation through a console
        /// </summary>
        public void BeginConversation()
        {
            System.Console.WriteLine("---- PRE-CONVERSATION SCRIPT -----");
            script.PrintComprehensiveScript();

            System.Console.WriteLine("---- STARTING CONVERSATION -----");

            TalkScript.ScriptItem startItem = Start();

            List<int> conversationOrder = new List<int>();
            List<TalkScript.ScriptLine> conversationOrderScriptLines = new List<TalkScript.ScriptLine>();

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
            while (nConversationIndex < conversationOrder.Count)
            {
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
                    if (state.OneInXOdds(4))
                    {
                        conversationOrder.Add((int)TalkScript.TalkConstants.Name);
                        conversationOrderScriptLines.Add(script.GetScriptLine(TalkScript.TalkConstants.Name));
                    }
                }

                int nSplitLine = 0;
                
                // we will go over each of the split lines indivdually
                do
                {
                    int nItem = 0;

                    // if we have just begun a label section, then let's handle it slightly difference then the normal conversation
                    if (splitLines[nSplitLine].GetScriptItem(0).Command == TalkScript.TalkCommand.DefaultMessage)
                    {
                        Debug.Assert(splitLines[nSplitLine].GetNumberOfScriptItems() == 2, "If it is a label definition, then it must have only 2 items defined in it");
                        int nLabel = splitLines[nSplitLine].GetScriptItem(1).LabelNum;
                        Debug.Assert(nLabel >= 0 && nLabel <= 9, "Label number must be between 0 and 9");

                        // get the label object
                        TalkScript.ScriptTalkLabel scriptLabel = script.TalkLabels.Labels[nLabel];
                        scriptLabel.InitialLine
                        // print the initial conversation starter...

                    }

                    TalkScript.ScriptLine currentSplitLine = splitLines[nSplitLine];

                    // if they are going to referece the Avatar by name, but don't know it, then we just skip the line altogether
                    if (currentSplitLine.ContainsCommand(TalkScript.TalkCommand.AvatarsName) && !npc.KnowTheAvatar())
                    {
                        Debug.Assert(nItem == 0);
                        nSplitLine++;
                        continue;
                    }

                    // we process all ScriptItem in each split ScriptLine before proceeding the next conversation item (ie. name, job, label3)
                    // todo: an emumerator would be kinda cool - i like foreach loops
                    do
                    {
                        TalkScript.ScriptItem item = currentSplitLine.GetScriptItem(nItem);

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
                                break;
                            case TalkScript.TalkCommand.AvatarsName:
                                // we should already know if they know the avatars name....
                                Debug.Assert(npc.KnowTheAvatar());
                                EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, ProcessItem(item)));
                                break;
                            case TalkScript.TalkCommand.AskName:
                                EnqueToOutputBuffer(item);
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
                            case TalkScript.TalkCommand.Unknown_Enter:
                                break;
                            case TalkScript.TalkCommand.Unknown_FF:
                                break;
                            case TalkScript.TalkCommand.DefaultMessage:
                            case TalkScript.TalkCommand.Or:
                            case TalkScript.TalkCommand.Unknown_CodeA2:
                                throw new Exception("We should never see the <OR> or <A2> code in conversation");
                            default:
                                throw new Exception("Recieved TalkCommand I wasn't expecting during conversation");

                        }
                        nItem++;
                    // while we still have more items in the current split line
                    } while (nItem < splitLines[nSplitLine].GetNumberOfScriptItems()); ;
                    nSplitLine++;
                // while there are more split lines to go through
                } while (nSplitLine < splitLines.Count);

                EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, "\n"));
                //EnqueToOutputBuffer(new TalkScript.ScriptItem(TalkScript.TalkCommand.PlainString, "\n"));
                // we have gone through all instructions, so lets move onto the next conversation line
                nConversationIndex++;
            }

            System.Console.ReadKey();
        }
    }
}
