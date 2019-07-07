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
        /// <summary>
        /// A single non player character (NPC)
        /// </summary>
        public class NonPlayerCharacter
        {
            public class NPCSchedule
            {
                /// <summary>
                /// TODO: Need to figure out what these AI types actually mean
                /// </summary>
                protected internal List<byte> AIType = new List<byte>();
                /// <summary>
                /// 3D Coordinates including floor number
                /// </summary>
                protected internal List<Point3D> Coords  { get;  }
                /// <summary>
                /// Times of day to move to the next scheduled item
                /// TODO: figure out why there are 4 times, but only three xyz's to go to?!
                /// </summary>
                protected internal List<byte> Times { get; }

                /// <summary>
                /// Creates an NPC Schedule object 
                /// This is easier to consume than the structure
                /// </summary>
                /// <param name="sched"></param>
                public NPCSchedule(NPC_Schedule sched)
                {
                    Coords = new List<Point3D>();
                    Times = new List<byte>();

                    unsafe
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            AIType.Add(sched.AI_types[i]);
                            Coords.Add(new Point3D(sched.x_coordinates[i], sched.y_coordinates[i], sched.z_coordinates[i]));
                        }
                        // argh, I can't get the size dynamically of the arrays
                        for (int i = 0; i < 4; i++)
                        {
                            Times.Add(sched.times[i]);
                        }
                    }
                }
            }


            /// <summary>
            /// Original structure
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            protected internal unsafe struct NPC_Schedule
            {
                public fixed byte AI_types[3];
                public fixed byte x_coordinates[3];
                public fixed byte y_coordinates[3];
                public fixed byte z_coordinates[3];
                public fixed byte times[4];
            };

            /// <summary>
            /// NPC Type, any other value is a specific character
            /// </summary>
            public enum NPCDialogTypeEnum { Custom = -1, Guard = 0, WeaponsDealer = 0x81, Barkeeper = 0x82, HorseSeller = 0x83, ShipSeller = 0x84, Healer = 0x87, InnKeeper = 0x88 };

            public NPCSchedule Schedule { get; }
            public byte DialogNumber { get; }
            public byte CharacterType { get; }
            SmallMapReference.SingleMapReference MapReference;

            public NPCDialogTypeEnum NPCType {
                get
                {
                    foreach (int npctype in Enum.GetValues(typeof(NPCDialogTypeEnum)))
                    {
                        if (npctype == DialogNumber && npctype != (int)NPCDialogTypeEnum.Custom)
                        {
                            return (NPCDialogTypeEnum)npctype;
                        }
                    }
                    return NPCDialogTypeEnum.Custom;
                }
             }


            public NonPlayerCharacter (SmallMapReference.SingleMapReference mapRef, NPC_Schedule sched, byte npcType, byte dialogNumber)
            {
                Schedule = new NPCSchedule(sched);
                MapReference = mapRef;
                CharacterType = npcType;
                DialogNumber = dialogNumber;
                // no schedule? I guess you're not real
                if (!IsEmptySched(sched))
                {
                    System.Console.WriteLine("NPC Number: " + this.DialogNumber + " in " + mapRef.MapLocation.ToString());
                }
            }

            private bool IsEmptySched(NPC_Schedule sched)
            {
                unsafe
                {
                    if (sched.times[0] == 0 && sched.times[1] == 0 && sched.times[2] == 0 && sched.times[3] == 0 && sched.times[4] == 0)
                        return true;
                }
                return false;
            }
        }

        public List<NonPlayerCharacter> NPCs
        {
            get
            {
                return (npcs);
            }
        }
        
        /// <summary>
        /// All of the NPCs
        /// </summary>
        private List<NonPlayerCharacter> npcs = new List<NonPlayerCharacter>();


        /// <summary>
        /// How many bytes does each NPC record take
        /// </summary>
        //private static readonly int NPC_CHARACTER_OFFSET_SIZE = Marshal.SizeOf(typeof(NonPlayerCharacter.NPC_Schedule)) + SIZEOF_NPC_TYPE_BLOCK + SIZEOF_NPC_DIALOG_BLOCK;

        /// <summary>
        /// How many bytes is each town offset inside the NPC file
        /// </summary>
        private static readonly int TOWN_OFFSET_SIZE = (Marshal.SizeOf(typeof(NonPlayerCharacter.NPC_Schedule)) + SIZEOF_NPC_TYPE_BLOCK + SIZEOF_NPC_DIALOG_BLOCK) * NPCS_PER_TOWN;
        
        /// <summary>
        /// How many bytes does the schedule structure use?
        /// </summary>
        private static readonly int SCHEDULE_OFFSET_SIZE = Marshal.SizeOf(typeof(NonPlayerCharacter.NPC_Schedule));
        /// <summary>
        /// How many NPC records per town?
        /// </summary>
        private const int NPCS_PER_TOWN = 32;
        private static readonly int STARTING_NPC_TYPE_TOWN_OFFSET = SCHEDULE_OFFSET_SIZE * NPCS_PER_TOWN; // starting position (within town) of NPC type
        private static readonly int STARTING_NPC_DIALOG_TOWN_OFFSET = STARTING_NPC_TYPE_TOWN_OFFSET + (SIZEOF_NPC_TYPE_BLOCK * NPCS_PER_TOWN); // starting position (within town) of NPC dialog
        private const int SIZEOF_NPC_TYPE_BLOCK = 1;
        private const int SIZEOF_NPC_DIALOG_BLOCK = 1;
        private const int TOWNS_PER_NPCFILE = 8;


        /* [StructLayout(LayoutKind.Sequential, Pack = 1)]
                private unsafe struct NPC_Info
                {
                    NPC_Schedule schedule[32];
                    fixed byte type[32]; // merchant, guard, etc.
                    fixed byte dialog_number[32];
                };*/


        /// <summary>
        /// Initialize NPCs from a particular small map master file set
        /// </summary>
        /// <param name="u5Directory">Directory with Ultima 5</param>
        /// <param name="mapMaster">The master map from which to load</param>
        /// <param name="smallMapRef">Small map reference to help link NPCs to a map</param>
        private void InitializeNPCs(string u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles mapMaster, SmallMapReference smallMapRef)
        {
            // open the appropriate NPC data file
            string dataFilenameAndPath = Path.Combine(u5Directory, SmallMapReference.SingleMapReference.GetNPCFilenameFromMasterFile(mapMaster));

            // load the file into memory
            List<byte> npcData = Utils.GetFileAsByteList(dataFilenameAndPath);

            for (int nTown = 0; nTown < TOWNS_PER_NPCFILE; nTown++)
            {
                // fresh collections for each major loop to guarantee they are clean
                List<NonPlayerCharacter.NPC_Schedule> schedules = new List<NonPlayerCharacter.NPC_Schedule>(NPCS_PER_TOWN);
                List<byte> npcTypes = new List<byte>(NPCS_PER_TOWN);
                List<byte> npcDialogNumber = new List<byte>(NPCS_PER_TOWN);

                int townOffset = (TOWN_OFFSET_SIZE * nTown);

                // bajh: I know this could be done in a single loop, but it would be so damn ugly that I honestly don't even want to both
                // read through the schedules first
                int count = 0;
                // start at the town offset, incremenet by an NPC record each time, for 32 loops
                for (int offset = townOffset; count < NPCS_PER_TOWN; offset += SCHEDULE_OFFSET_SIZE, count++)
                {
                    NonPlayerCharacter.NPC_Schedule sched = (NonPlayerCharacter.NPC_Schedule)Utils.ReadStruct(npcData, offset, typeof(NonPlayerCharacter.NPC_Schedule));
                    schedules.Add(sched);

                }
                // bajh: just shoot me if I ever have to write this again - why on earth did LB write all of his data in different formats! 
                // these are single byte, so we can capture them just by jumping to their offsets
                count = 0;
                for (int offset = townOffset ; count < NPCS_PER_TOWN; offset++, count++)
                {
                    // add NPC type
                    npcTypes.Add(npcData[offset + STARTING_NPC_TYPE_TOWN_OFFSET]);

                    // add NPC dialog #
                    npcDialogNumber.Add(npcData[offset + STARTING_NPC_DIALOG_TOWN_OFFSET]);
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
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Towne, smallMapRef);
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Castle, smallMapRef);
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Keep, smallMapRef);
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Dwelling, smallMapRef);
        }



    }
}
