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

        //private List<SmallMap> smallMaps = new List<SmallMap>();
        public SmallMaps AllSmallMaps { get; }

        public LargeMap OverworldMap { get; }
        public LargeMap UnderworldMap { get; }

        public TileReferences SpriteTileReferences { get; }

        private string u5Directory;
        public SmallMapReference SmallMapRef;
        private CombatMapReference combatMapRef = new CombatMapReference();
        public Look LookRef { get; }
        public Signs SignRef { get; }
        public NonPlayerCharacters NpcRef { get; }
        public DataOvlReference DataOvlRef { get; }
        public TalkScripts TalkScriptsRef { get; }
        public GameState State { get; }
        #endregion
        public LargeMapReference LargeMapRef { get; }

        public Conversation CurrentConversation { get; set; }

        public World (string ultima5Directory) : base ()
        {
            u5Directory = ultima5Directory;

            // build the overworld map
            OverworldMap = new LargeMap(u5Directory, LargeMap.Maps.Overworld);

            // build the underworld map
            UnderworldMap = new LargeMap(u5Directory, LargeMap.Maps.Underworld);

            DataOvlRef = new DataOvlReference(u5Directory);

            State = new GameState(u5Directory, DataOvlRef);

            SpriteTileReferences = new TileReferences(DataOvlRef.StringReferences);

            SmallMapRef = new SmallMapReference(DataOvlRef);

            LargeMapRef = new LargeMapReference(DataOvlRef, SmallMapRef);

            AllSmallMaps = new SmallMaps(SmallMapRef, u5Directory);

            State.GetCharacterFromParty(0);

            // build all the small maps from the Small Map reference
            //foreach (SmallMapReference.SingleMapReference mapRef in SmallMapRef.MapReferenceList)
            //{
            //    // now I can go through each and every reference
            //    SmallMap smallMap = new SmallMap(u5Directory, mapRef);
            //    smallMaps.Add(smallMap);
            //    //U5Map.PrintMapSection(smallMap.RawMap, 0, 0, 32, 32);
            //}

            // build all combat maps from the Combat Map References
            foreach (CombatMapReference.SingleCombatMapReference combatMapRef in combatMapRef.MapReferenceList)
            {
                CombatMap combatMap = new CombatMap(u5Directory, combatMapRef);
                //System.Console.WriteLine("\n");
                //System.Console.WriteLine(combatMap.Description);
                //Map.PrintMapSection(combatMap.RawMap, 0, 0, 11, 11);
            }

            // build a "look" table for all tiles
            LookRef = new Look(ultima5Directory);

            // build the sign tables
            SignRef = new Signs(ultima5Directory);

            //Signs.Sign sign = SignRef.GetSign(SmallMapReference.SingleMapReference.Location.Yew, 16, 2);
            Signs.Sign sign = SignRef.GetSign(42);

            string str = sign.SignText;
            TalkScriptsRef = new TalkScripts(u5Directory, DataOvlRef);

            // build the NPC tables
            NpcRef = new NonPlayerCharacters(ultima5Directory, SmallMapRef, TalkScriptsRef, State);

            //State.Year = 100;
            //State.Month = 13;
            //State.Day = 28;
            //State.Hour = 22;
            //State.Minute = 2;
          

            //Conversation convo = new Conversation(NpcRef.NPCs[21]); // dunkworth
            //            Conversation convo = new Conversation(NpcRef.NPCs[296], State, DataOvlRef); // Gwenno
            // 19 = Margarett
            //           NpcRef.NPCs[296].Script.PrintComprehensiveScript();

            int count = 0;
            if (false)
            {
                foreach (NonPlayerCharacters.NonPlayerCharacter npc in NpcRef.NPCs)
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
            //Conversation convo = new Conversation(NpcRef.NPCs[0xe6], State, DataOvlRef);

            // Bidney
            //Conversation convo = new Conversation(NpcRef.NPCs[0xe8], State);

            // Lord Dalgrin
            //Conversation convo = new Conversation(NpcRef.NPCs[0xea], State);

            // Geoffery
            //Conversation convo = new Conversation(NpcRef.NPCs[0xec], State, DataOvlRef);

            // Tierra 
            //Conversation convo = new Conversation(NpcRef.NPCs[0xeb], State, DataOvlRef);

//            Conversation.EnqueuedScriptItem enqueuedScriptItemDelegate = new Conversation.EnqueuedScriptItem(this.EnqueuedScriptItem);
//            convo.EnqueuedScriptItemCallback += enqueuedScriptItemDelegate;

           // convo.BeginConversation();

            //0x48 or 0x28
            //Console.WriteLine("Shutting down... Hit any key...");
            //Console.ReadKey(false);

        }

        public Conversation CreateConverationAndBegin(NonPlayerCharacters.NonPlayerCharacter npc, Conversation.EnqueuedScriptItem enqueuedScriptItem)
        {
            CurrentConversation = new Conversation(npc, State, DataOvlRef);

            //Conversation.EnqueuedScriptItem enqueuedScriptItemDelegate = new Conversation.EnqueuedScriptItem(this.EnqueuedScriptItem);
            CurrentConversation.EnqueuedScriptItemCallback += enqueuedScriptItem;

            return CurrentConversation;
        }

    

    }
}
