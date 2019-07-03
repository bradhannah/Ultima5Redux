using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;


namespace Ultima5Redux
{
    class NonPlayerCharacters
    {
        public class NonPlayerCharacter
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            protected internal unsafe struct NPC_Schedule
            {
                fixed byte AI_types[3];
                fixed byte x_coordinates[3];
                fixed byte y_coordinates[3];
                fixed byte z_coordinates[3];
                fixed byte times[4];
            };

            public enum NPCType { Guard = 0 };

            public NonPlayerCharacter (SmallMapReference.SingleMapReference location, NPC_Schedule sched, byte npcType, byte dialogNunber)
            {

            }
        }

        private List<NonPlayerCharacter> npcs = new List<NonPlayerCharacter>();

        private readonly int TOWN_OFFSET_SIZE = Marshal.SizeOf(typeof(NonPlayerCharacter.NPC_Schedule)) + 64;
        private readonly int SCHEDULE_OFFSET_SIZE = Marshal.SizeOf(typeof(NonPlayerCharacter.NPC_Schedule));
        private const int NPCS_PER_TOWN = 32;

        /*        [StructLayout(LayoutKind.Sequential, Pack = 1)]
                private unsafe struct NPC_Info
                {
                    NPC_Schedule schedule[32];
                    fixed byte type[32]; // merchant, guard, etc.
                    fixed byte dialog_number[32];
                };*/

        private void InitializeNPCs(string u5Directory, SmallMapReference.SingleMapReference.MapMasterFiles mapMaster, SmallMapReference smallMapRef)
        {
            string dataFilenameAndPath = Path.Combine(u5Directory, FileConstants.NPC_FILES[1]);

            List<byte> towne_npc = Utils.GetFileAsByteList(dataFilenameAndPath);

            for (int nTown = 0; nTown < 8; nTown++)
            {
                // fresh collections for each major loop to guarantee they are clean
                List<NonPlayerCharacter.NPC_Schedule> schedules = new List<NonPlayerCharacter.NPC_Schedule>(NPCS_PER_TOWN);
                List<byte> npcTypes = new List<byte>(NPCS_PER_TOWN);
                List<byte> npcDialogNumber = new List<byte>(NPCS_PER_TOWN);

                // bajh: I know this could be done in a single loop, but it would be so damn ugly that I honestly don't even want to both
                // read through the schedules first
                for (int nNpc = 0; nNpc < NPCS_PER_TOWN; nNpc++)
                {
                    NonPlayerCharacter.NPC_Schedule sched = (NonPlayerCharacter.NPC_Schedule)Utils.ReadStruct(towne_npc, SCHEDULE_OFFSET_SIZE, typeof(NonPlayerCharacter.NPC_Schedule));
                    schedules.Add(sched);
                }
                int count = 0;
                // now read through the type identifiers
                for (int nNpc = SCHEDULE_OFFSET_SIZE * NPCS_PER_TOWN; count < NPCS_PER_TOWN; nNpc++, count++)
                {
                    npcTypes.Add(towne_npc[nNpc]);
                }
                count = 0;
                // now read through the talk dialog numbers (added NPCS_PER_TOWN because the type identifiers are only single byte)
                for (int nNpc = (SCHEDULE_OFFSET_SIZE * NPCS_PER_TOWN) + NPCS_PER_TOWN; count < NPCS_PER_TOWN; nNpc++, count++)
                {
                    npcDialogNumber.Add(towne_npc[nNpc]);
                }

                // go over all of the NPCs, create them and add them to the collection
                for (int nNpc = 0; nNpc < NPCS_PER_TOWN; nNpc++)
                {
                    SmallMapReference.SingleMapReference singleMapRef = smallMapRef.GetSingleMapByFileAndIndex(mapMaster, nTown);
                    npcs.Add(new NonPlayerCharacter(singleMapRef, schedules[nNpc], npcTypes[nNpc], npcDialogNumber[nNpc]));
                }
            }
        }

        public NonPlayerCharacters(string u5Directory, SmallMapReference smallMapRef)
        {
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.MapMasterFiles.Towne, smallMapRef);
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.MapMasterFiles.Castle, smallMapRef);
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.MapMasterFiles.Keep, smallMapRef);
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.MapMasterFiles.Dwelling, smallMapRef);
        }



    }
}
