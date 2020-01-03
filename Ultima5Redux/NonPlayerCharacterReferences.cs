using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace Ultima5Redux
{
    public partial class NonPlayerCharacterReferences
    {
        #region Public Properties
        /// <summary>
        /// All of the NPCs
        /// </summary>
        public List<NonPlayerCharacterReference> NPCs
        {
            get
            {
                return (npcs);
            }
        }
        #endregion

        #region Public Methods
        public List<NonPlayerCharacterReference> GetNonPlayerCharactersByLocation(SmallMapReferences.SingleMapReference.Location location)
        {
            if (!this.locationToNPCsDictionary.ContainsKey(location)) return new List<NonPlayerCharacterReference>();

            return locationToNPCsDictionary[location];
        }

        public NonPlayerCharacterReference GetNonPlayerCharacter(SmallMapReferences.SingleMapReference.Location location, Point2D xy, int nFloor)
        {
            foreach (NonPlayerCharacterReference npc in GetNonPlayerCharactersByLocation(location))
            {
                if (npc.CurrentMapPosition == xy && npc.CurrentFloor == nFloor)
                {
                    return npc;
                }
            }
            return null; 
        }
        #endregion

        #region Private Variables
        /// <summary>
        /// All of the NPCs
        /// </summary>
        private List<NonPlayerCharacterReference> npcs = new List<NonPlayerCharacterReference>();
        private Dictionary<SmallMapReferences.SingleMapReference.Location, List<NonPlayerCharacterReference>> locationToNPCsDictionary
            = new Dictionary<SmallMapReferences.SingleMapReference.Location, List<NonPlayerCharacterReference>>();
        #endregion

        #region Constants, Enums
        /// <summary>
        /// How many bytes is each town offset inside the NPC file
        /// </summary>
        private static readonly int TOWN_OFFSET_SIZE = (Marshal.SizeOf(typeof(NonPlayerCharacterReference.NPC_Schedule)) + SIZEOF_NPC_TYPE_BLOCK + SIZEOF_NPC_DIALOG_BLOCK) * NPCS_PER_TOWN;
        
        /// <summary>
        /// How many bytes does the schedule structure use?
        /// </summary>
        private static readonly int SCHEDULE_OFFSET_SIZE = Marshal.SizeOf(typeof(NonPlayerCharacterReference.NPC_Schedule));

        /// <summary>
        /// How many NPC records per town?
        /// </summary>
        public const int NPCS_PER_TOWN = 32;

        /// <summary>
        /// starting position (within town) of NPC type
        /// </summary>
        private static readonly int STARTING_NPC_TYPE_TOWN_OFFSET = SCHEDULE_OFFSET_SIZE * NPCS_PER_TOWN;
        /// <summary>
        ///  starting position (within town) of NPC dialog
        /// </summary>
        private static readonly int STARTING_NPC_DIALOG_TOWN_OFFSET = STARTING_NPC_TYPE_TOWN_OFFSET + (SIZEOF_NPC_TYPE_BLOCK * NPCS_PER_TOWN);
        /// <summary>
        /// Sizeof(bytes) a single NPC type number in file
        /// </summary>
        private const int SIZEOF_NPC_TYPE_BLOCK = 1;
        /// <summary>
        /// Sizeof(bytes) a single NPC dialog number in file
        /// </summary>
        private const int SIZEOF_NPC_DIALOG_BLOCK = 1;
        /// <summary>
        /// Number of townes stores in each .NPC file
        /// </summary>
        private const int TOWNS_PER_NPCFILE = 8;
        // left over structure
        /* [StructLayout(LayoutKind.Sequential, Pack = 1)]
                private unsafe struct NPC_Info
                {
                    NPC_Schedule schedule[32];
                    fixed byte type[32]; // merchant, guard, etc.
                    fixed byte dialog_number[32];
                };*/
        #endregion

        #region Initialization and Constructor routines
        /// <summary>
        /// Initialize NPCs from a particular small map master file set
        /// </summary>
        /// <param name="u5Directory">Directory with Ultima 5</param>
        /// <param name="mapMaster">The master map from which to load</param>
        /// <param name="smallMapRef">Small map reference to help link NPCs to a map</param>
        private void InitializeNPCs(string u5Directory, SmallMapReferences.SingleMapReference.SmallMapMasterFiles mapMaster, SmallMapReferences smallMapRef, 
            TalkScripts talkScriptsRef, GameState gameStateRef)
        {
            // open the appropriate NPC data file
            string dataFilenameAndPath = Path.Combine(u5Directory, SmallMapReferences.SingleMapReference.GetNPCFilenameFromMasterFile(mapMaster));

            // load the file into memory
            List<byte> npcData = Utils.GetFileAsByteList(dataFilenameAndPath);

            for (int nTown = 0; nTown < TOWNS_PER_NPCFILE; nTown++)
            {
                // fresh collections for each major loop to guarantee they are clean
                List<NonPlayerCharacterReference.NPC_Schedule> schedules = new List<NonPlayerCharacterReference.NPC_Schedule>(NPCS_PER_TOWN);
                List<byte> npcTypes = new List<byte>(NPCS_PER_TOWN);
                List<byte> npcDialogNumber = new List<byte>(NPCS_PER_TOWN);

                SmallMapReferences.SingleMapReference.Location location = smallMapRef.GetLocationByIndex(mapMaster, nTown);
                SmallMapReferences.SingleMapReference singleMapRef = smallMapRef.GetSingleMapByLocation(location, 0);

                //sing = SmallMapRef.GetSingleMapByLocation(SmallMapRef.GetLocationByIndex(mapMaster, nTown);

                int townOffset = (TOWN_OFFSET_SIZE * nTown);

                // bajh: I know this could be done in a single loop, but it would be so damn ugly that I honestly don't even want to both
                // read through the schedules first
                int count = 0;
                // start at the town offset, incremenet by an NPC record each time, for 32 loops
                for (int offset = townOffset; count < NPCS_PER_TOWN; offset += SCHEDULE_OFFSET_SIZE, count++)
                {
                    NonPlayerCharacterReference.NPC_Schedule sched = (NonPlayerCharacterReference.NPC_Schedule)Utils.ReadStruct(npcData, offset, typeof(NonPlayerCharacterReference.NPC_Schedule));
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

                List<byte> keySpriteList = gameStateRef.NonPlayerCharacterKeySprites.GetAsByteList();

                // go over all of the NPCs, create them and add them to the collection
                for (int nNpc = 0; nNpc < NPCS_PER_TOWN; nNpc++)
                {
                    NonPlayerCharacterReference npc = new NonPlayerCharacterReference(location, gameStateRef, schedules[nNpc], npcTypes[nNpc], 
                        npcDialogNumber[nNpc], nNpc, talkScriptsRef.GetTalkScript(mapMaster, npcDialogNumber[nNpc]), (int)(keySpriteList[nNpc]+100));
                    npcs.Add(npc);
                    // we also create a quick lookup table by location but first need to check that there is an initialized list inside
                    if (!locationToNPCsDictionary.ContainsKey(singleMapRef.MapLocation)) 
                    { 
                        locationToNPCsDictionary.Add(singleMapRef.MapLocation, new List<NonPlayerCharacterReference>());  
                    }
                    locationToNPCsDictionary[singleMapRef.MapLocation].Add(npc);
                }
            }
        }

        /// <summary>
        /// Construct all of the non player characters across all of the SmallMaps
        /// </summary>
        /// <param name="u5Directory">the directory with Ultima 5 data files</param>
        /// <param name="smallMapRef">The small map reference</param>
        /// <param name="talkScriptsRe">Talk script references</param>
        public NonPlayerCharacterReferences(string u5Directory, SmallMapReferences smallMapRef, TalkScripts talkScriptsRef, GameState gameStateRef)
        {
            InitializeNPCs(u5Directory, SmallMapReferences.SingleMapReference.SmallMapMasterFiles.Castle, smallMapRef, talkScriptsRef, gameStateRef);
            InitializeNPCs(u5Directory, SmallMapReferences.SingleMapReference.SmallMapMasterFiles.Towne, smallMapRef, talkScriptsRef, gameStateRef);
            InitializeNPCs(u5Directory, SmallMapReferences.SingleMapReference.SmallMapMasterFiles.Keep, smallMapRef, talkScriptsRef, gameStateRef);
            InitializeNPCs(u5Directory, SmallMapReferences.SingleMapReference.SmallMapMasterFiles.Dwelling, smallMapRef, talkScriptsRef, gameStateRef);
        }
        #endregion    
    }
}
