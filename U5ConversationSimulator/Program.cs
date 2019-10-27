using System;
using Ultima5Redux;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace U5ConversationSimulator
{
    class Program
    {
        static World world;
        static void Main(string[] args)
        {
            world = new World("C:\\games\\ultima_5_late\\bucden4");
            //Dictionary<int, TileReference> tileReference = TileReference.Load();
            world.OverworldMap.PrintMap();
            world.SmallMapRef.GetLocationName(SmallMapReference.SingleMapReference.Location.Lord_Britishs_Castle);
            world.NpcRef.GetNonPlayerCharactersByLocation(SmallMapReference.SingleMapReference.Location.Britain);

            //Conversation convo = new Conversation(world.NpcRef.NPCs[293], world.State, world.DataOvlRef); // eb
            Conversation convo = new Conversation(world.NpcRef.NPCs[296], world.State, world.DataOvlRef); // Gwenno
            Conversation.EnqueuedScriptItem enqueuedScriptItemDelegate = new Conversation.EnqueuedScriptItem(EnqueuedScriptItem);
            convo.EnqueuedScriptItemCallback += enqueuedScriptItemDelegate;

            convo.BeginConversation();
        }

        static private void EnqueuedScriptItem(Conversation conversation)
        {
            TalkScript.ScriptItem item = conversation.DequeueFromOutputBuffer();
            string userResponse;
            switch (item.Command)
            {
                case TalkScript.TalkCommand.PlainString:
                    Console.Write(item.Str);
                    break;
                case TalkScript.TalkCommand.PromptUserForInput_UserInterest:
                    Console.Write(conversation.GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.YOUR_INTEREST));
                    userResponse = Console.ReadLine();
                    conversation.AddUserResponse(userResponse);
                    break;
                case TalkScript.TalkCommand.PromptUserForInput_NPCQuestion:
                    Console.Write(conversation.GetConversationStr(DataOvlReference.CHUNK__PHRASES_CONVERSATION.YOU_RESPOND));
                    userResponse = Console.ReadLine();
                    conversation.AddUserResponse(userResponse);
                    break;
                case TalkScript.TalkCommand.AskName:
                    userResponse = Console.ReadLine();
                    conversation.AddUserResponse(userResponse);
                    break;
                case TalkScript.TalkCommand.CallGuards:
                    //
                    break;
                case TalkScript.TalkCommand.Change:
                    //
                    break;
                case TalkScript.TalkCommand.EndCoversation:
                    // 
                    break;
                case TalkScript.TalkCommand.Gold:
                    //
                    break;
                case TalkScript.TalkCommand.JoinParty:
                    world.State.AddMemberToParty(conversation.Npc);
                    break;
                case TalkScript.TalkCommand.KarmaMinusOne:
                    //
                    world.State.Karma -= 1;
                    break;
                case TalkScript.TalkCommand.KarmaPlusOne:
                    world.State.Karma += 1;
                    break;
                case TalkScript.TalkCommand.KeyWait:
                    Console.Write("...");
                    Console.ReadKey();
                    break;
                case TalkScript.TalkCommand.Pause:
                    for (int i = 0; i < 3; i++)
                    {
                        Console.Write(".");
                        Thread.Sleep(500);
                    }
                    Console.WriteLine("");
                    break;
                case TalkScript.TalkCommand.Rune:
                case TalkScript.TalkCommand.NewLine:
                case TalkScript.TalkCommand.DefineLabel:
                case TalkScript.TalkCommand.IfElseKnowsName:
                case TalkScript.TalkCommand.AvatarsName:
                    throw new Exception("We recieved a TalkCommand: " + item.Command.ToString() + " that we didn't expect in the World processing");
                default:
                    throw new Exception("We recieved a TalkCommand: " + item.Command.ToString() + " that we didn't expect in the World processing");
                    //Console.Write("<" + item.Command.ToString() + ">");
            }
        }
    }
}
