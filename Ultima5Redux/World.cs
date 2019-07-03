using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ultima5Redux
{
    class World
    {
        private List<SmallMap> smallMaps = new List<SmallMap>();
        public LargeMap overworldMap;
        public LargeMap underworldMap;

        private string u5Directory;
        private SmallMapReference smallMapRef = new SmallMapReference();
        private CombatMapReference combatMapRef = new CombatMapReference();
        private Look lookRef;
        private Signs signRef;
        private NonPlayerCharacters npcRef;

        public World (string ultima5Directory) : base ()
        {
            u5Directory = ultima5Directory;

            // build the overworld map
            overworldMap = new LargeMap(u5Directory, LargeMap.Maps.Overworld);
            
            // build the underworld map
            underworldMap = new LargeMap(u5Directory, LargeMap.Maps.Underworld);

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

            // build the NPC tables
            npcRef = new NonPlayerCharacters(ultima5Directory, smallMapRef);

            foreach (NonPlayerCharacters.NonPlayerCharacter npc in npcRef.NPCs)
            {
                if (npc.NPCType != 0)
                {
                    Console.WriteLine(npc.NPCType.ToString());
                }
            }
            //NonPlayerCharacters.NonPlayerCharacter.NPCDialogTypeEnum npctype = npcRef.NPCs[100].NPCType;
            //NonPlayerCharacters.NonPlayerCharacter.NPCDialogTypeEnum npctype1 = npcRef.NPCs[80].NPCType;
            //NonPlayerCharacters.NonPlayerCharacter.NPCDialogTypeEnum npctype2 = npcRef.NPCs[202].NPCType;
        }


    }
}
