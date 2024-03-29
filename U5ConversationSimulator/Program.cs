﻿using System;
using System.Threading;
using Ultima5Redux;
using Ultima5Redux.Dialogue;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Dialogue;
using Ultima5Redux.References.Maps;

namespace U5ConversationSimulator
{
    internal class Program
    {
        private static World World;

        private static void EnqueuedScriptItem(Conversation conversation)
        {
            TalkScript.ScriptItem item = conversation.DequeueFromOutputBuffer();
            string userResponse;
            switch (item.Command)
            {
                case TalkScript.TalkCommand.PlainString:
                    Console.Write(item.StringData);
                    break;
                case TalkScript.TalkCommand.PromptUserForInput_UserInterest:
                    Console.Write(
                        Conversation.GetConversationStr(DataOvlReference.ChunkPhrasesConversation.YOUR_INTEREST));
                    userResponse = Console.ReadLine();
                    conversation.AddUserResponse(userResponse);
                    break;
                case TalkScript.TalkCommand.PromptUserForInput_NPCQuestion:
                    Console.Write(
                        Conversation.GetConversationStr(DataOvlReference.ChunkPhrasesConversation.YOU_RESPOND));
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
                    // temp breaking it - lazy
                    //World.State.CharacterRecords.AddMemberToParty(null, new TurnResults());
                    //conversation.TheNonPlayerCharacterState.NPCRef, new TurnResults());
                    break;
                case TalkScript.TalkCommand.KarmaMinusOne:
                    //
                    //World.State.ChangeKarma(-1, turnResults);
                    break;
                case TalkScript.TalkCommand.KarmaPlusOne:
                    //World.State.ChangeKarma(1, turnResults);
                    break;
                case TalkScript.TalkCommand.KeyWait:
                    Console.Write(@"...");
                    Console.ReadKey();
                    break;
                case TalkScript.TalkCommand.Pause:
                    for (int i = 0; i < 3; i++)
                    {
                        Console.Write(@".");
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

        private static void Main()
        {
            World = new World(true, "/Users/bradhannah/games/u5tests/b_carpet");

            World.State.TheVirtualMap.LoadSmallMap(
                GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Palace_of_Blackthorn,
                    0));

            //Dictionary<int, TileReference> tileReference = TileReference.Load();
            //world.OverworldMap.PrintMap();
            //world.SmallMapRef.GetLocationName(SmallMapReferences.SingleMapReference._location.Lord_Britishs_Castle);
            //world.NpcRef.GetNonPlayerCharactersByLocation(SmallMapReferences.SingleMapReference._location.Britain);

            //Conversation convo = new Conversation(_world.NpcRef.NPCs[292], _world.State, _world.DataOvlRef); // justin
            // List<NonPlayerCharacterReference> minocNpcRef =
            //     _world.NpcRefs.GetNonPlayerCharactersByLocation(SmallMapReferences.SingleMapReference.Location.Minoc);

            var location =
                SmallMapReferences.SingleMapReference.Location.Palace_of_Blackthorn;
            NonPlayerCharacterState npcState =
                World.State.TheNonPlayerCharacterStates.GetStateByLocationAndIndex(location, 5);
            var convo = new Conversation(World.State, npcState, "GenericExtortingGuard");
            //World.State, npcState); // delwyn
            

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
    }
}