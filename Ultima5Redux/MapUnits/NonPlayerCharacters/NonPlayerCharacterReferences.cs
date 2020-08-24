using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    public class NonPlayerCharacterReferences
    {
        #region Public Properties
        /// <summary>
        /// All of the NPCs
        /// </summary>
        public List<NonPlayerCharacterReference> NPCs
        {
            get
            {
                return (_npcs);
            }
        }
        #endregion

        #region Public Methods
        public List<NonPlayerCharacterReference> GetNonPlayerCharactersByLocation(SmallMapReferences.SingleMapReference.Location location)
        {
            if (!this._locationToNPCsDictionary.ContainsKey(location)) return new List<NonPlayerCharacterReference>();

            return _locationToNPCsDictionary[location];
        }

        public NonPlayerCharacterReference GetNonPlayerCharacterByName(string name)
        {
            return _npcByNameDictionary[name];
        }

        /// <summary>
        /// Finds all NPCs at a certain location and certain type
        /// Best used for shoppe keepers
        /// </summary>
        /// <param name="location"></param>
        /// <param name="npcType"></param>
        /// <returns>NPC Reference</returns>
        public List<NonPlayerCharacterReference> GetNonPlayerCharacterByLocationAndNPCType(
            SmallMapReferences.SingleMapReference.Location location,
            NonPlayerCharacterReference.NPCDialogTypeEnum npcType)
        {
            List<NonPlayerCharacterReference> npcRefs = GetNonPlayerCharactersByLocation(location);

            return npcRefs.Where(npcRef => npcRef.NPCType == npcType).ToList();
        }
        
        //public NonPlayerCharacterReference GetNonPlayerCharacter(SmallMapReferences.SingleMapReference._location location, Point2D xy, int nFloor)
        //{
        //    foreach (NonPlayerCharacterReference npc in GetNonPlayerCharactersByLocation(location))
        //    {
        //        if (npc.CurrentMapPosition == xy && npc.CurrentFloor == nFloor)
        //        {
        //            return npc;
        //        }
        //    }
        //    return null; 
        //}
        #endregion

        #region Private Variables
        /// <summary>
        /// All of the NPCs
        /// </summary>
        private readonly List<NonPlayerCharacterReference> _npcs = new List<NonPlayerCharacterReference>();
        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, List<NonPlayerCharacterReference>> _locationToNPCsDictionary
            = new Dictionary<SmallMapReferences.SingleMapReference.Location, List<NonPlayerCharacterReference>>();

        private readonly Dictionary<string, NonPlayerCharacterReference> _npcByNameDictionary = new Dictionary<string, NonPlayerCharacterReference>();
        #endregion

        #region Constants, Enums
        /// <summary>
        /// How many bytes is each town offset inside the NPC file
        /// </summary>
        private static readonly int TownOffsetSize = (Marshal.SizeOf(typeof(NonPlayerCharacterReference.NPCSchedule)) + SIZEOF_NPC_TYPE_BLOCK + SIZEOF_NPC_DIALOG_BLOCK) * NPCS_PER_TOWN;
        
        /// <summary>
        /// How many bytes does the schedule structure use?
        /// </summary>
        private static readonly int ScheduleOffsetSize = Marshal.SizeOf(typeof(NonPlayerCharacterReference.NPCSchedule));

        /// <summary>
        /// How many NPC records per town?
        /// </summary>
        public const int NPCS_PER_TOWN = 32;

        /// <summary>
        /// starting position (within town) of NPC type
        /// </summary>
        private static readonly int StartingNPCTypeTownOffset = ScheduleOffsetSize * NPCS_PER_TOWN;
        /// <summary>
        ///  starting position (within town) of NPC dialog
        /// </summary>
        private static readonly int StartingNPCDialogTownOffset = StartingNPCTypeTownOffset + (SIZEOF_NPC_TYPE_BLOCK * NPCS_PER_TOWN);
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
        /// <param name="talkScriptsRef"></param>
        /// <param name="gameStateRef"></param>
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
                List<NonPlayerCharacterReference.NPCSchedule> schedules = new List<NonPlayerCharacterReference.NPCSchedule>(NPCS_PER_TOWN);
                List<byte> npcTypes = new List<byte>(NPCS_PER_TOWN);
                List<byte> npcDialogNumber = new List<byte>(NPCS_PER_TOWN);

                SmallMapReferences.SingleMapReference.Location location = smallMapRef.GetLocationByIndex(mapMaster, nTown);
                SmallMapReferences.SingleMapReference singleMapRef = smallMapRef.GetSingleMapByLocation(location, 0);

                //sing = SmallMapRef.GetSingleMapByLocation(SmallMapRef.GetLocationByIndex(mapMaster, nTown);

                int townOffset = (TownOffsetSize * nTown);

                // bajh: I know this could be done in a single loop, but it would be so damn ugly that I honestly don't even want to both
                // read through the schedules first
                int count = 0;
                // start at the town offset, incremenet by an NPC record each time, for 32 loops
                for (int offset = townOffset; count < NPCS_PER_TOWN; offset += ScheduleOffsetSize, count++)
                {
                    NonPlayerCharacterReference.NPCSchedule schedule = (NonPlayerCharacterReference.NPCSchedule)Utils.ReadStruct(npcData, offset, typeof(NonPlayerCharacterReference.NPCSchedule));
                    schedules.Add(schedule);
                }
                // bajh: just shoot me if I ever have to write this again - why on earth did LB write all of his data in different formats! 
                // these are single byte, so we can capture them just by jumping to their offsets
                count = 0;
                for (int offset = townOffset ; count < NPCS_PER_TOWN; offset++, count++)
                {
                    // add NPC type
                    npcTypes.Add(npcData[offset + StartingNPCTypeTownOffset]);

                    // add NPC dialog #
                    npcDialogNumber.Add(npcData[offset + StartingNPCDialogTownOffset]);
                }

                List<byte> keySpriteList = gameStateRef.NonPlayerCharacterKeySprites.GetAsByteList();

                // go over all of the NPCs, create them and add them to the collection
                for (int nNpc = 0; nNpc < NPCS_PER_TOWN; nNpc++)
                {
                    NonPlayerCharacterReference npc = new NonPlayerCharacterReference(location, gameStateRef, schedules[nNpc], npcTypes[nNpc], 
                        npcDialogNumber[nNpc], nNpc, talkScriptsRef.GetTalkScript(mapMaster, npcDialogNumber[nNpc]), (int)(keySpriteList[nNpc]+100));
                    _npcs.Add(npc);
                    // we also create a quick lookup table by location but first need to check that there is an initialized list inside
                    if (!_locationToNPCsDictionary.ContainsKey(singleMapRef.MapLocation)) 
                    { 
                        _locationToNPCsDictionary.Add(singleMapRef.MapLocation, new List<NonPlayerCharacterReference>());  
                    }

                    if (npc.Name != string.Empty && !npc.Name.StartsWith("..."))
                    {
                        // merchants do not have names recorded :(
                        _npcByNameDictionary.Add(npc.Name, npc);
                    }

                    _locationToNPCsDictionary[singleMapRef.MapLocation].Add(npc);
                }
            }
        }

        /// <summary>
        /// Construct all of the non player characters across all of the SmallMaps
        /// </summary>
        /// <param name="u5Directory">the directory with Ultima 5 data files</param>
        /// <param name="smallMapRef">The small map reference</param>
        /// <param name="talkScriptsRef"></param>
        /// <param name="gameStateRef"></param>
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
