using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Ultima5Redux
{
    public class World
    {
        #region Private Variables

        private List<SmallMap> smallMaps = new List<SmallMap>();
        public LargeMap OverworldMap { get; }
        public LargeMap UnderworldMap { get; }

        public TileReferences SpriteTileReferences { get; }

        private string u5Directory;
        public SmallMapReference SmallMapRef;
        private CombatMapReference combatMapRef = new CombatMapReference();
        private Look lookRef;
        private Signs signRef;
        private NonPlayerCharacters npcRef;
        private DataOvlReference dataOvlRef;
        private TalkScripts talkScriptsRef;
        private GameState state;
        #endregion

        public World (string ultima5Directory) : base ()
        {
            u5Directory = ultima5Directory;

            // build the overworld map
            OverworldMap = new LargeMap(u5Directory, LargeMap.Maps.Overworld);

            // build the underworld map
            UnderworldMap = new LargeMap(u5Directory, LargeMap.Maps.Underworld);

            state = new GameState(u5Directory);

            SpriteTileReferences = new TileReferences();

            dataOvlRef = new DataOvlReference(u5Directory);

            SmallMapRef = new SmallMapReference(dataOvlRef);

            // build all the small maps from the Small Map reference
            foreach (SmallMapReference.SingleMapReference mapRef in SmallMapRef.MapReferenceList)
            {
                // now I can go through each and every reference
                SmallMap smallMap = new SmallMap(u5Directory, mapRef);
                smallMaps.Add(smallMap);
                //U5Map.PrintMapSection(smallMap.RawMap, 0, 0, 32, 32);
            }

            // build all combat maps from the Combat Map References
            foreach (CombatMapReference.SingleCombatMapReference combatMapRef in combatMapRef.MapReferenceList)
            {
                CombatMap combatMap = new CombatMap(u5Directory, combatMapRef);
                //System.Console.WriteLine("\n");
                //System.Console.WriteLine(combatMap.Description);
                //Map.PrintMapSection(combatMap.RawMap, 0, 0, 11, 11);
            }

            // build a "look" table for all tiles
            lookRef = new Look(ultima5Directory);

            // build the sign tables
            signRef = new Signs(ultima5Directory);


            talkScriptsRef = new TalkScripts(u5Directory, dataOvlRef);

            // build the NPC tables
            npcRef = new NonPlayerCharacters(ultima5Directory, SmallMapRef, talkScriptsRef, state);

            //Conversation convo = new Conversation(npcRef.NPCs[21]); // dunkworth
            // 19 = Margarett


            int count = 0;
            if (false)
            {
                foreach (NonPlayerCharacters.NonPlayerCharacter npc in npcRef.NPCs)
                {
                    if (npc.NPCType != 0 && npc.Script != null)
                    {
                        Console.WriteLine("");
                        Console.WriteLine("---- SCRIPT for " + npc.Name.Trim() + " -----");
                        //Npc.Script.PrintScript();
                        npc.Script.PrintComprehensiveScript();

                        if (npc.Name.Trim() == "Geoffrey")
                        {
                            Console.WriteLine(npc.NPCType.ToString());

                        }
                    }
                    count++;
                }
            }

            // Scally
            Conversation convo = new Conversation(npcRef.NPCs[0xe6], state, dataOvlRef);

            // Bidney
            //Conversation convo = new Conversation(npcRef.NPCs[0xe8], state);

            // Lord Dalgrin
            //Conversation convo = new Conversation(npcRef.NPCs[0xea], state);

            // Geoffery
            //Conversation convo = new Conversation(npcRef.NPCs[0xec], state, dataOvlRef);

            // Tierra 
            //Conversation convo = new Conversation(npcRef.NPCs[0xeb], state, dataOvlRef);

            Conversation.EnqueuedScriptItem enqueuedScriptItemDelegate = new Conversation.EnqueuedScriptItem(this.EnqueuedScriptItem);
            convo.EnqueuedScriptItemCallback += enqueuedScriptItemDelegate;

            //convo.BeginConversation();

            //0x48 or 0x28
            //Console.WriteLine("Shutting down... Hit any key...");
            //Console.ReadKey(false);

        }


        private void EnqueuedScriptItem(Conversation conversation)
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
                    state.AddMemberToParty(conversation.Npc);
                    break;
                case TalkScript.TalkCommand.KarmaMinusOne:
                    //
                    state.Karma-=1;
                    break;
                case TalkScript.TalkCommand.KarmaPlusOne:
                    state.Karma+=1;
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
