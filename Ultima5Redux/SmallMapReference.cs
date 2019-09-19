using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Ultima5Redux
{
    public partial class SmallMapReference
    {

        #region Private Variables

        // the master copy of the map references
        private List<SingleMapReference> mapReferences = new List<SingleMapReference>();
        
        // total number of master files (towne, keep, castle and dwelling)
        private const int MASTER_FILES = 4;
        
        // a tally of current file offsets used for auto-incrementing
        private Dictionary<string, short> roomOffsetCountDictionary = new Dictionary<string, short>();
        
        // a mapping of the master file -> a list of locations, which also provides an index value for the town (implied within list)
        private Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, List<SingleMapReference.Location>> masterFileLocationDictionary = 
            new Dictionary<SingleMapReference.SmallMapMasterFiles, List<SingleMapReference.Location>>(MASTER_FILES);

        private Dictionary<SingleMapReference.Location, bool> smallMapBasementDictionary = new Dictionary<SingleMapReference.Location, bool>();
        private Dictionary<SingleMapReference.Location, int> nFloorsDictionary = new Dictionary<SingleMapReference.Location, int>();

        /// <summary>
        /// Data OVL reference used for grabbing a lot of different data 
        /// </summary>
        private DataOvlReference dataRef;

        /// <summary>
        /// a list of all the location names
        /// </summary>
        private List<string> locationNames;

        #endregion

        #region Private Static Methods
        /// <summary>
        /// Cheater function to automatically create floors in a building
        /// </summary>
        /// <param name="location"></param>
        /// <param name="startFloor"></param>
        /// <param name="nFloors"></param>
        /// <returns></returns>
        private List<SingleMapReference> GenerateSingleMapReferences(SingleMapReference.Location location, int startFloor, short nFloors, short roomOffset, string name)
        {
            List<SingleMapReference> mapRefs = new List<SingleMapReference>();

            int fileOffset = roomOffset * SmallMap.XTILES * SmallMap.YTILES; // the number of rooms offset, converted to number of bytes to skip

            for (int i = 0; i < nFloors; i++)
            {
                mapRefs.Add(new SingleMapReference(location, startFloor + i, fileOffset + (i * SmallMap.XTILES * SmallMap.YTILES), dataRef));
            }

            return mapRefs;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Add a location to the reference map. Auto increments file offset, so be sure to add small maps in correct order
        /// </summary>
        /// <param name="location">the location of reference</param>
        /// <param name="hasBasement">Does it have a basement? if so, start at position -1 </param>
        /// <param name="nFloors">How many floors?</param>
        private void AddLocation(SingleMapReference.Location location, bool hasBasement, short nFloors)
        {
            // get the master map file from the location info
            SingleMapReference.SmallMapMasterFiles masterMap = SingleMapReference.GetMapMasterFromLocation(location);
            // we are going to track the order that the maps were added 
            // if the master map hasn't been seen yet, then we need to create a new index array of locations
            if (!masterFileLocationDictionary.ContainsKey(masterMap))
            {
                masterFileLocationDictionary.Add(masterMap, new List<SingleMapReference.Location>());
            }
            masterFileLocationDictionary[masterMap].Add(location);

            // get the filename of the location - we use it as key into a map
            string dataFilename = SingleMapReference.GetFilenameFromLocation(location);
            
            // create an offset counter if it doesn't already exist
            if (!roomOffsetCountDictionary.ContainsKey(dataFilename))
            {
                roomOffsetCountDictionary[dataFilename] = 0;
            }

            // get the current file offset value (auto-counting...)
            short roomOffset = roomOffsetCountDictionary[dataFilename];

            // add one or more map references 
            mapReferences.AddRange(GenerateSingleMapReferences(location, hasBasement?-1:0, nFloors, roomOffset, locationNames[(int)location]));

            // add the number of floors you have just added so that it can increment the file offset for subequent calls
            roomOffsetCountDictionary[dataFilename] += nFloors;

            smallMapBasementDictionary.Add(location, hasBasement);
            nFloorsDictionary.Add(location, nFloors);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// A list of all map references
        /// </summary>
        public List<SingleMapReference> MapReferenceList
        {
            get
            {
                return mapReferences;
            }
        }
        #endregion

        #region Public Methods
        //public string GetLocationTypeStr(SingleMapReference.Location location)
        //{

        //}

        public int GetNumberOfFloors(SingleMapReference.Location location)
        {
            return nFloorsDictionary[location];
        }

        public bool HasBasement(SingleMapReference.Location location)
        {
            return smallMapBasementDictionary[location];
        }

        static public Point2D GetStartingXYByLocation(SmallMapReference.SingleMapReference.Location location)
        {
            return new Point2D(32 / 2 - 1, 30);
        }

        public string GetLocationName(SmallMapReference.SingleMapReference.Location location)
        {
            Func<DataOvlReference.LOCATION_STRINGS, string> getLocationNameStr = delegate (DataOvlReference.LOCATION_STRINGS index)
            {
                return dataRef.GetStringFromDataChunkList(DataOvlReference.DataChunkName.LOCATION_NAMES, (int)index);
            };

            // filthy way to convert our more commonly used Location enum to the less used LOCATION_STRINGS
            // they didn't even bother having them all match, and then decided to leave some out
            DataOvlReference.LOCATION_STRINGS newLocStrEnum = (DataOvlReference.LOCATION_STRINGS) Enum.Parse(typeof(DataOvlReference.LOCATION_STRINGS), location.ToString());

            // if the DataOVL didn't provide a name, then we are forced to set our own... :(
            if ((int)newLocStrEnum < 0)
            {
                switch (newLocStrEnum)
                {
                    case DataOvlReference.LOCATION_STRINGS.Suteks_Hut:
                        return "SUTEK'S HUT";
                    case DataOvlReference.LOCATION_STRINGS.SinVraals_Hut:
                        return "SIN VRAAL'S HUT";
                    case DataOvlReference.LOCATION_STRINGS.Grendels_Hut:
                        return "GRENDAL'S HUT";
                    case DataOvlReference.LOCATION_STRINGS.Lord_Britishs_Castle:
                        return "LORD BRITISH'S CASTLE";
                    case DataOvlReference.LOCATION_STRINGS.Palace_of_Blackthorn:
                        return "PALACE OF BLACKTHORN";
                    default:
                        throw new Exception("Ummm asked for a location name and wasn't on the guest list.");
                }
            }
            else
            {
                return getLocationNameStr(newLocStrEnum);
            }
    }

        public string GetLocationTypeStr(SmallMapReference.SingleMapReference.Location location)
        {
            // anon function for quick lookup of strings
            Func<DataOvlReference.WORLD_STRINGS, string> getTypePlaceStr = delegate (DataOvlReference.WORLD_STRINGS index)
            {
                return dataRef.GetStringFromDataChunkList(DataOvlReference.DataChunkName.WORLD, (int)index);
            };

            switch (location)
            {
                case SmallMapReference.SingleMapReference.Location.Lord_Britishs_Castle:
                    return getTypePlaceStr(DataOvlReference.WORLD_STRINGS.to_enter_CASTLE_LB);
                case SmallMapReference.SingleMapReference.Location.Palace_of_Blackthorn:
                    return getTypePlaceStr(DataOvlReference.WORLD_STRINGS.to_enter_PALACE_B);
                case SmallMapReference.SingleMapReference.Location.East_Britanny:
                case SmallMapReference.SingleMapReference.Location.West_Britanny:
                case SmallMapReference.SingleMapReference.Location.North_Britanny:
                case SmallMapReference.SingleMapReference.Location.Paws:
                case SmallMapReference.SingleMapReference.Location.Cove:
                    return getTypePlaceStr(DataOvlReference.WORLD_STRINGS.to_enter_VILLAGE);
                case SmallMapReference.SingleMapReference.Location.Buccaneers_Den:
                    return getTypePlaceStr(DataOvlReference.WORLD_STRINGS.to_enter_KEEP);
                case SmallMapReference.SingleMapReference.Location.Moonglow:
                case SmallMapReference.SingleMapReference.Location.Britain:
                case SmallMapReference.SingleMapReference.Location.Jhelom:
                case SmallMapReference.SingleMapReference.Location.Yew:
                case SmallMapReference.SingleMapReference.Location.Minoc:
                case SmallMapReference.SingleMapReference.Location.Trinsic:
                case SmallMapReference.SingleMapReference.Location.Skara_Brae:
                case SmallMapReference.SingleMapReference.Location.New_Magincia:
                    return getTypePlaceStr(DataOvlReference.WORLD_STRINGS.to_enter_TOWNE);
                case SmallMapReference.SingleMapReference.Location.Fogsbane:
                case SmallMapReference.SingleMapReference.Location.Stormcrow:
                case SmallMapReference.SingleMapReference.Location.Waveguide:
                case SmallMapReference.SingleMapReference.Location.Greyhaven:
                    return getTypePlaceStr(DataOvlReference.WORLD_STRINGS.to_enter_LIGHTHOUSE);
                case SmallMapReference.SingleMapReference.Location.Iolos_Hut:
                //case Location.spektran
                case SmallMapReference.SingleMapReference.Location.Suteks_Hut:
                case SmallMapReference.SingleMapReference.Location.SinVraals_Hut:
                case SmallMapReference.SingleMapReference.Location.Grendels_Hut:
                    return getTypePlaceStr(DataOvlReference.WORLD_STRINGS.to_enter_HUT);
                case SmallMapReference.SingleMapReference.Location.Ararat:
                    return getTypePlaceStr(DataOvlReference.WORLD_STRINGS.to_enter_RUINS);
                case SmallMapReference.SingleMapReference.Location.Bordermarch:
                case SmallMapReference.SingleMapReference.Location.Farthing:
                case SmallMapReference.SingleMapReference.Location.Windemere:
                case SmallMapReference.SingleMapReference.Location.Stonegate:
                case SmallMapReference.SingleMapReference.Location.Lycaeum:
                case SmallMapReference.SingleMapReference.Location.Empath_Abbey:
                case SmallMapReference.SingleMapReference.Location.Serpents_Hold:
                    return getTypePlaceStr(DataOvlReference.WORLD_STRINGS.to_enter_KEEP);
                case SmallMapReference.SingleMapReference.Location.Deceit:
                case SmallMapReference.SingleMapReference.Location.Despise:
                case SmallMapReference.SingleMapReference.Location.Destard:
                case SmallMapReference.SingleMapReference.Location.Wrong:
                case SmallMapReference.SingleMapReference.Location.Covetous:
                case SmallMapReference.SingleMapReference.Location.Shame:
                case SmallMapReference.SingleMapReference.Location.Hythloth:
                case SmallMapReference.SingleMapReference.Location.Doom:
                    return getTypePlaceStr(DataOvlReference.WORLD_STRINGS.to_enter_DUNGEON);
            }
            return "";
        }

        /// <summary>
        /// Get the location based on the index within the small map file
        /// </summary>
        /// <param name="smallMap">the small map file reference</param>
        /// <param name="index">the 0-7 index within the small map file</param>
        /// <returns>the location reference</returns>
        public SingleMapReference.Location GetLocationByIndex(SingleMapReference.SmallMapMasterFiles smallMap, int index)
        {            
            SingleMapReference.Location location = masterFileLocationDictionary[smallMap][index];
            return location;
        }

        /// <summary>
        /// Get a map reference based on a location
        /// </summary>
        /// <param name="location">The location you are looking for</param>
        /// <returns>a single map reference providing details on the map itself</returns>
        public SingleMapReference GetSingleMapByLocation (SingleMapReference.Location location, int floor)
        {
            SingleMapReference.SmallMapMasterFiles masterMap = SingleMapReference.GetMapMasterFromLocation(location);
            foreach (SingleMapReference mapRef in mapReferences)
            {
                if (mapRef.MapLocation == location && mapRef.Floor == floor)
                {
                    return mapRef; 
                }
            }
            throw new Exception("Location was not found!");
        }

        public void InitializeLocationNames ()
        {
            // get the data chunks that have the offsets to the strings in the data.ovl file, representing each location (most)
            DataChunk locationNameOffsetChunk = dataRef.GetDataChunk(DataOvlReference.DataChunkName.LOCATION_NAME_INDEXES);

            // get the offsets 
            List<ushort> locationOffsets = locationNameOffsetChunk.GetChunkAsUINT16();
            locationNames = new List<string>(locationOffsets.Count+1);

            // I happen to know that the underworld and overworld is [0], so let's add a placeholder
            locationNames.Add("Overworld/Underworld");

            // grab each location string
            // it isn't the most efficient way, but it gets the job done
            foreach (ushort offset in locationOffsets)
            {
                    locationNames.Add(dataRef.GetDataChunk(DataChunk.DataFormatType.SimpleString, string.Empty, offset, 20).GetChunkAsString());
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Construct all small map references
        /// </summary>
        public SmallMapReference(DataOvlReference dataRef)
        {
            // I wish I could do this a smarter way, but as of now I have no idea where this data is stored in any of the data files

            this.dataRef = dataRef;

            InitializeLocationNames();

            // Castle.dat
            AddLocation(SingleMapReference.Location.Lord_Britishs_Castle, true, 5);
            AddLocation(SingleMapReference.Location.Palace_of_Blackthorn, true, 5);
            AddLocation(SingleMapReference.Location.West_Britanny, false, 1);
            AddLocation(SingleMapReference.Location.North_Britanny, false, 1);
            AddLocation(SingleMapReference.Location.East_Britanny, false, 1);
            AddLocation(SingleMapReference.Location.Paws, false, 1);
            AddLocation(SingleMapReference.Location.Cove, false, 1);
            AddLocation(SingleMapReference.Location.Buccaneers_Den, false, 1);

            // Towne.dat
            AddLocation(SingleMapReference.Location.Moonglow, false, 2);
            AddLocation(SingleMapReference.Location.Britain, false, 2);
            AddLocation(SingleMapReference.Location.Jhelom, false, 2);
            AddLocation(SingleMapReference.Location.Yew, true, 2);
            AddLocation(SingleMapReference.Location.Minoc, false, 2);
            AddLocation(SingleMapReference.Location.Trinsic, false, 2);
            AddLocation(SingleMapReference.Location.Skara_Brae, false, 2);
            AddLocation(SingleMapReference.Location.New_Magincia, false, 2);

            // Dwelling.dat
            AddLocation(SingleMapReference.Location.Fogsbane, false, 3);
            AddLocation(SingleMapReference.Location.Stormcrow, false, 3);
            AddLocation(SingleMapReference.Location.Greyhaven, false, 3);
            AddLocation(SingleMapReference.Location.Waveguide, false, 3);
            AddLocation(SingleMapReference.Location.Iolos_Hut, false, 1);
            AddLocation(SingleMapReference.Location.Suteks_Hut, false, 1);
            AddLocation(SingleMapReference.Location.SinVraals_Hut, false, 1);
            AddLocation(SingleMapReference.Location.Grendels_Hut, false, 1);

            // Keep.dat
            AddLocation(SingleMapReference.Location.Ararat, false, 2);
            AddLocation(SingleMapReference.Location.Bordermarch, false, 2);
            AddLocation(SingleMapReference.Location.Farthing, false, 1);
            AddLocation(SingleMapReference.Location.Windemere, false, 1);
            AddLocation(SingleMapReference.Location.Stonegate, false, 1);
            AddLocation(SingleMapReference.Location.Lycaeum, false, 3);
            AddLocation(SingleMapReference.Location.Empath_Abbey, false, 3);
            AddLocation(SingleMapReference.Location.Serpents_Hold, true, 3);

            AddLocation(SingleMapReference.Location.Deceit, false, 0);
            AddLocation(SingleMapReference.Location.Despise, false, 0);
            AddLocation(SingleMapReference.Location.Destard, false, 0);
            AddLocation(SingleMapReference.Location.Wrong, false, 0);
            AddLocation(SingleMapReference.Location.Covetous, false, 0);
            AddLocation(SingleMapReference.Location.Shame, false, 0);
            AddLocation(SingleMapReference.Location.Hythloth, false, 0);
            AddLocation(SingleMapReference.Location.Doom, false, 0);
        }
        #endregion
    }
}
