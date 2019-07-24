using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

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

            private GameState gameStateRef;

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
            public enum NPCDialogTypeEnum { Custom = -1, Guard = 0, WeaponsDealer = 0x81, Barkeeper = 0x82, HorseSeller = 0x83, ShipSeller = 0x84, Healer = 0x87,
                InnKeeper = 0x88, UnknownX85 = 0x85, UnknownX86 = 0x86, Unknown = 0xFF };

            /// <summary>
            /// Return true if the dialog is not part of a standard dialog tree like a guard or shopkeeper
            /// </summary>
            /// <param name="dialogType">The dialog type that yu want to compare</param>
            /// <returns></returns>
            static public bool IsSpecialDialogType(NPCDialogTypeEnum dialogType)
            {
                foreach (NPCDialogTypeEnum tempDialogType in (NPCDialogTypeEnum[])Enum.GetValues(typeof(NPCDialogTypeEnum)))
                {
                    if (dialogType == tempDialogType)
                        return true;
                }
                return false;
            }

            public string Name
            {
                get
                {
                    if (Script != null)
                    {
                        return Script.GetScriptLine(TalkScript.TalkConstants.Name).GetScriptItem(0).Str;
                    }
                    else
                    {
                        return string.Empty;
                    }
                } 
            }

            /// <summary>
            /// The daily schedule of the NPC
            /// </summary>
            public NPCSchedule Schedule { get; }
            /// <summary>
            /// The Dialog identifier
            /// </summary>
            public byte DialogNumber { get; }

            /// <summary>
            /// 0-31 index of it's position in the NPC arrays (used for saved.gam references)
            /// </summary>
            public int DialogIndex { get; }

            /// <summary>
            /// The byte representing the type of character
            /// </summary>
            private byte CharacterType { get; }

            /// <summary>
            /// The talk script the NPC will follow
            /// </summary>
            public TalkScript Script { get; }

            /// <summary>
            /// Which map is the NPC on?
            /// </summary>
            public SmallMapReference.SingleMapReference MapReference { get; }

            /// <summary>
            /// What type of NPC are they? 
            /// </summary>
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

            public bool KnowTheAvatar()
            {
                int nScriptLines = Script.GetNumberOfScriptLines();

                for (int i = 0; i < nScriptLines; i++)
                {
                    if (gameStateRef.NpcHasMetAvatar(this))
                    {
                        return true;
                    }
                    if (!Script.GetScriptLine(i).ContainsCommand(TalkScript.TalkCommand.AskName))
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Construct an NPC
            /// </summary>
            /// <param name="mapRef">Which map are they on?</param>
            /// <param name="sched">daily schedule</param>
            /// <param name="npcType">type of NPC they are</param>
            /// <param name="dialogNumber">dialog number referencing data OVL</param>
            /// <param name="dialogIndex">0-31 index of it's position in the NPC arrays (used for saved.gam references)</param>
            /// <param name="talkScript">their conversation script</param>
            public NonPlayerCharacter (SmallMapReference.SingleMapReference mapRef, GameState gameStateRef, NPC_Schedule sched, byte npcType, byte dialogNumber, int dialogIndex, TalkScript talkScript)
            {
                Schedule = new NPCSchedule(sched);
                MapReference = mapRef;
                CharacterType = npcType;
                DialogNumber = dialogNumber;
                Script = talkScript;
                DialogIndex = dialogIndex;
                this.gameStateRef = gameStateRef;

                // no schedule? I guess you're not real
                if (!IsEmptySched(sched))
                {
                    System.Console.WriteLine(mapRef.MasterFile.ToString() + "     NPC Number: " + this.DialogNumber + " in " + mapRef.MapLocation.ToString());
                }
            }

            /// <summary>
            /// Does the NPC have an empty schedule? If so, then they aren't actually an NPC
            /// </summary>
            /// <param name="sched">daily schedule</param>
            /// <returns></returns>
            static private bool IsEmptySched(NPC_Schedule sched)
            {
                unsafe
                {
                    if (sched.times[0] == 0 && sched.times[1] == 0 && sched.times[2] == 0 && sched.times[3] == 0 && sched.times[4] == 0)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// All of the NPCs
        /// </summary>
        protected internal List<NonPlayerCharacter> NPCs
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
        public const int NPCS_PER_TOWN = 32;

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
        private void InitializeNPCs(string u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles mapMaster, SmallMapReference smallMapRef, TalkScripts talkScriptsRef, GameState gameStateRef)
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

                SmallMapReference.SingleMapReference.Location location = smallMapRef.GetLocationByIndex(mapMaster, nTown);
                SmallMapReference.SingleMapReference singleMapRef = smallMapRef.GetSingleMapByLocation(location);

                //sing = smallMapRef.GetSingleMapByLocation(smallMapRef.GetLocationByIndex(mapMaster, nTown);

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
                    npcs.Add(new NonPlayerCharacter(singleMapRef, gameStateRef, schedules[nNpc], npcTypes[nNpc], npcDialogNumber[nNpc], nNpc, talkScriptsRef.GetTalkScript(mapMaster, npcDialogNumber[nNpc])));
                }
            }
        }

        /// <summary>
        /// Construct all of the non player characters across all of the SmallMaps
        /// </summary>
        /// <param name="u5Directory">the directory with Ultima 5 data files</param>
        /// <param name="smallMapRef">The small map reference</param>
        /// <param name="talkScriptsRe">Talk script references</param>
        public NonPlayerCharacters(string u5Directory, SmallMapReference smallMapRef, TalkScripts talkScriptsRef, GameState gameStateRef)
        {
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Castle, smallMapRef, talkScriptsRef, gameStateRef);
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Towne, smallMapRef, talkScriptsRef, gameStateRef);
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Keep, smallMapRef, talkScriptsRef, gameStateRef);
            InitializeNPCs(u5Directory, SmallMapReference.SingleMapReference.SmallMapMasterFiles.Dwelling, smallMapRef, talkScriptsRef, gameStateRef);
        }



    }
}
