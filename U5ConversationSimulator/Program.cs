using System;
using System.Collections.Generic;
using System.Threading;
using Ultima5Redux;
using Ultima5Redux.Data;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References;

namespace U5ConversationSimulator
{
    class Program
    {
        static World _world;

        static void Main(string[] args)
        {
            _world = new World("C:\\games\\ultima_5_late\\britain");

            _world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Minoc, 0));

            //Dictionary<int, TileReference> tileReference = TileReference.Load();
            //world.OverworldMap.PrintMap();
            //world.SmallMapRef.GetLocationName(SmallMapReferences.SingleMapReference._location.Lord_Britishs_Castle);
            //world.NpcRef.GetNonPlayerCharactersByLocation(SmallMapReferences.SingleMapReference._location.Britain);

            //Conversation convo = new Conversation(_world.NpcRef.NPCs[292], _world.State, _world.DataOvlRef); // justin
            // List<NonPlayerCharacterReference> minocNpcRef =
            //     _world.NpcRefs.GetNonPlayerCharactersByLocation(SmallMapReferences.SingleMapReference.Location.Minoc);
            
            SmallMapReferences.SingleMapReference.Location location = SmallMapReferences.SingleMapReference.Location.Minoc;
            NonPlayerCharacterState npcState =
                _world.State.TheNonPlayerCharacterStates.GetStateByLocationAndIndex(location, 9);
            Conversation convo = new Conversation(_world.State, npcState); // delwyn

            //Conversation convo = new Conversation(world.NpcRef.NPCs[293], world.State, world.DataOvlRef); // eb
            //Conversation convo = new Conversation(world.NpcRef.NPCs[296], world.State, world.DataOvlRef); // Gwenno
            Conversation.EnqueuedScriptItem enqueuedScriptItemDelegate = EnqueuedScriptItem;
            convo.EnqueuedScriptItemCallback += enqueuedScriptItemDelegate;

            convo.BeginConversation();
            //int i = 1000;
            // while (i > 0)
            // {
            //     _world.AdvanceTime(2);
            //     i--;
            // }
        }

        private static void EnqueuedScriptItem(Conversation conversation)
        {
            TalkScript.ScriptItem item = conversation.DequeueFromOutputBuffer();
            string userResponse;
            switch (item.Command)
            {
                case TalkScript.TalkCommand.PlainString:
                    Console.Write(item.Str);
                    break;
                case TalkScript.TalkCommand.PromptUserForInput_UserInterest:
                    Console.Write(
                        conversation.GetConversationStr(DataOvlReference.ChunkPhrasesConversation.YOUR_INTEREST));
                    userResponse = Console.ReadLine();
                    conversation.AddUserResponse(userResponse);
                    break;
                case TalkScript.TalkCommand.PromptUserForInput_NPCQuestion:
                    Console.Write(
                        conversation.GetConversationStr(DataOvlReference.ChunkPhrasesConversation.YOU_RESPOND));
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
                case TalkScript.TalkCommand.EndConversation:
                    // 
                    break;
                case TalkScript.TalkCommand.Gold:
                    //
                    break;
                case TalkScript.TalkCommand.JoinParty:
                    _world.State.CharacterRecords.AddMemberToParty(conversation.TheNonPlayerCharacterState.NPCRef);
                    break;
                case TalkScript.TalkCommand.KarmaMinusOne:
                    //
                    _world.State.Karma -= 1;
                    break;
                case TalkScript.TalkCommand.KarmaPlusOne:
                    _world.State.Karma += 1;
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
                    throw new Exception("We received a TalkCommand: " + item.Command +
                                        " that we didn't expect in the World processing");
                default:
                    throw new Exception("We received a TalkCommand: " + item.Command +
                                        " that we didn't expect in the World processing");
            }
        }
    }
}