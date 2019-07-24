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
    class World
    {
        private List<SmallMap> smallMaps = new List<SmallMap>();
        public LargeMap overworldMap;
        public LargeMap underworldMap;

        private string u5Directory;
        private SmallMapReference smallMapRef;
        private CombatMapReference combatMapRef = new CombatMapReference();
        private Look lookRef;
        private Signs signRef;
        private NonPlayerCharacters npcRef;
        private DataOvlReference dataOvlRef;
        private TalkScripts talkScriptsRef;
        private GameState state;

        public World (string ultima5Directory) : base ()
        {
            u5Directory = ultima5Directory;

            dataOvlRef = new DataOvlReference(u5Directory);

            state = new GameState(u5Directory);

            // build the overworld map
            overworldMap = new LargeMap(u5Directory, LargeMap.Maps.Overworld);
            
            // build the underworld map
            underworldMap = new LargeMap(u5Directory, LargeMap.Maps.Underworld);

            smallMapRef = new SmallMapReference(dataOvlRef);

            // build all the small maps from the Small Map reference
            foreach (SmallMapReference.SingleMapReference mapRef in smallMapRef.MapReferenceList)
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
            npcRef = new NonPlayerCharacters(ultima5Directory, smallMapRef, talkScriptsRef, state);

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
                        //npc.Script.PrintScript();
                        npc.Script.PrintComprehensiveScript();

                        if (npc.Name.Trim() == "Geoffrey")
                        {
                            Console.WriteLine(npc.NPCType.ToString());

                        }
                    }
                    count++;
                }
            }

//            Conversation convo = new Conversation(npcRef.NPCs[0xea], state);
//            Conversation convo = new Conversation(npcRef.NPCs[0xec], state);
            Conversation convo = new Conversation(npcRef.NPCs[0xeb], state);
            convo.BeginConversation();
            currentlyConversating = true;

            Conversation.EnqueuedScriptItem enqueuedScriptItemDelegate = new Conversation.EnqueuedScriptItem(this.EnqueuedScriptItem);
            convo.EnqueuedScriptItemCallback += enqueuedScriptItemDelegate;


            while (currentlyConversating)
            {
                if (wantUserInput)
                {
                    Console.Write("You respond: ");
                    string response = Console.ReadLine();
                    convo.AddUserResponse(response);
                }
                else
                {
                    // nothing for me to do...
                    Thread.Sleep(50);
                }
            }

            //0x48 or 0x28
            Console.ReadKey();

        }

        private bool currentlyConversating = false;
        private bool wantUserInput = false;

        private void EnqueuedScriptItem(Conversation conversation)
        {

        }

    }
}
