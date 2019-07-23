using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    class Conversation
    {
        private NonPlayerCharacters.NonPlayerCharacter npc;
        private TalkScript script;
        private TalkScript.ScriptLine currentLine;
        private GameState state;


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

        public TalkScript.ScriptItem Next()
        {
            return (Start());
        }

        public TalkScript.ScriptItem Next(string userResponse)
        {
            return (Start());
        }

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

        /// <summary>
        /// Simulate a conversation through a console
        /// </summary>
        public void SimulateConversation()
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


            //conversationOrder.Add((int)TalkScript.TalkConstants.Name);

            // the index into conversationOrder
            int nConversationIndex = 0;

            // while there are more conversation lines to process
            while (nConversationIndex < conversationOrder.Count)
            {
                // the current talk line
                int nTalkLineIndex = conversationOrder[nConversationIndex];

                // the current ScriptLine
                //currentLine = script.GetScriptLineByIndex(conversationOrder[nConversationIndex]);
                currentLine = conversationOrderScriptLines[nConversationIndex];

                // if it's a greeting AND her greeting includes my name AND they have NOT yet met the avatar  
                if (conversationOrder[nConversationIndex] == (int)TalkScript.TalkConstants.Greeting && currentLine.ContainsCommand(TalkScript.TalkCommand.AvatarsName)
                
                //if (cur == (int)TalkScript.TalkConstants.Greeting && currentLine.ContainsCommand(TalkScript.TalkCommand.AvatarsName)
                && !state.NpcHasMetAvatar(npc))
                {
                    // randomly add an introduction of the Avatar since they haven't met him
                    if (state.OneInXOdds(4)) conversationOrder.Add((int)TalkScript.TalkConstants.Name);
                    if (state.OneInXOdds(4)) conversationOrderScriptLines.Add(script.GetScriptLine(TalkScript.TalkConstants.Name));
                }

                // Split the line into sections
                List<TalkScript.ScriptLine> lines = currentLine.SplitIntoSections();

                // we process all ScriptLines before proceeding the next conversation item
                // todo: an emumerator would be kinda cool - i like foreach loops
                for (int i = 0; i < currentLine.GetNumberOfScriptItems(); i++)
                {
                    TalkScript.ScriptItem item = currentLine.GetScriptItem(i);

                    // script just begun
                    // - % chance that they will say "I am called XX" (perhaps when no description is present?)
                    
                    // is this a simple string command? if so, we can just print it
                    if (TalkScript.IsStringOnlyCommand(item.Command))
                    {
                        // if this is the very first position of conversation
                        if (i == 0 && nTalkLineIndex == (int)TalkScript.TalkConstants.Description)
                        {
                            System.Console.WriteLine("You see " + ProcessItem(item));
                        }
                        else
                        {
                            switch (item.Command)
                            {
                                case TalkScript.TalkCommand.AvatarsName:
                                    {
                                        if (state.NpcHasMetAvatar(npc))
                                        {
                                            System.Console.Write(ProcessItem(item));
                                        } 
/*                                        else
                                        {
                                            if (state.OneInXOdds(4))
                                            {
                                                conversationOrder.Add((int)TalkScript.TalkConstants.Name);
                                                //System.Console.Write("I am called " + script.GetScriptLine(TalkScript.TalkConstants.Name));
                                            }
                                        }*/
                                        break;
                                    }
                                default:
                                    System.Console.Write(ProcessItem(item)+" ");
                                    break;
                            }
                        }
                    }
                    else // we have a special code to process
                    {
                        if (item.Command == TalkScript.TalkCommand.DefineLabel)
                        {
                            System.Console.Write("<" + item.Command.ToString() + item.LabelNum.ToString() + ">");
                        }
                        else
                        {
                            System.Console.Write("<" + item.Command.ToString() + ">");
                        }
                    }
                }

                // we have gone through all instructions, so lets move onto the next conversation line
                nConversationIndex++;
            }

            System.Console.ReadKey();
        }
    }
}
