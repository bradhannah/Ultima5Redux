using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.MapUnits;
using Ultima5Redux.References;

namespace Ultima5Redux.Maps
{
    public partial class SmallMapReferences
    {
        // total number of master files (towne, keep, castle and dwelling)
        private const int MASTER_FILES = 4;

        /// <summary>
        ///     Data OVL reference used for grabbing a lot of different data
        /// </summary>
        private readonly DataOvlReference _dataRef;

        // a mapping of the master file -> a list of locations, which also provides an index value for the town (implied within list)
        private readonly Dictionary<SingleMapReference.SmallMapMasterFiles, List<SingleMapReference.Location>>
            _masterFileLocationDictionary =
                new Dictionary<SingleMapReference.SmallMapMasterFiles, List<SingleMapReference.Location>>(MASTER_FILES);

        private readonly Dictionary<SingleMapReference.Location, int> _nFloorsDictionary =
            new Dictionary<SingleMapReference.Location, int>();

        // a tally of current file offsets used for auto-incrementing
        private readonly Dictionary<string, short> _roomOffsetCountDictionary = new Dictionary<string, short>();

        private readonly Dictionary<SingleMapReference.Location, bool> _smallMapBasementDictionary =
            new Dictionary<SingleMapReference.Location, bool>();

        /// <summary>
        ///     a list of all the location names
        /// </summary>
        // ReSharper disable once CollectionNeverQueried.Local
        private List<string> _locationNames;

        /// <summary>
        ///     A list of all map references
        /// </summary>
        public List<SingleMapReference> MapReferenceList { get; } = new List<SingleMapReference>();

        /// <summary>
        ///     Construct all small map references
        /// </summary>
        public SmallMapReferences(DataOvlReference dataRef)
        {
            // I wish I could do this a smarter way, but as of now I have no idea where this data is stored in any of the data files

            _dataRef = dataRef;

            InitializeLocationNames();

            // Castle.dat
            AddLocation(SingleMapReference.Location.Britannia_Underworld, true, 2);
            AddLocation(SingleMapReference.Location.Combat_resting_shrine, false, 1);

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

        /// <summary>
        ///     Gets the starting location of a small map
        /// </summary>
        /// <returns></returns>
        public static Point2D GetStartingXYByLocation()
        {
            return new Point2D(32 / 2 - 1, 30);
        }

        /// <summary>
        ///     Gets the starting position of a small map
        /// </summary>
        /// <returns></returns>
        public static MapUnitPosition GetStartingXYZByLocation()
        {
            Point2D startingXY = GetStartingXYByLocation();
            return new MapUnitPosition(startingXY.X, startingXY.Y, 0);
        }

        /// <summary>
        ///     Add a location to the reference map. Auto increments file offset, so be sure to add small maps in correct order
        /// </summary>
        /// <param name="location">the location of reference</param>
        /// <param name="hasBasement">Does it have a basement? if so, start at position -1 </param>
        /// <param name="nFloors">How many floors?</param>
        private void AddLocation(SingleMapReference.Location location, bool hasBasement, short nFloors)
        {
            // get the master map file from the location info
            SingleMapReference.SmallMapMasterFiles masterMap = SingleMapReference.GetMapMasterFromLocation(location);
            if (masterMap == SingleMapReference.SmallMapMasterFiles.None) return;

            // we are going to track the order that the maps were added 
            // if the master map hasn't been seen yet, then we need to create a new index array of locations
            if (!_masterFileLocationDictionary.ContainsKey(masterMap))
                _masterFileLocationDictionary.Add(masterMap, new List<SingleMapReference.Location>());
            _masterFileLocationDictionary[masterMap].Add(location);

            // get the filename of the location - we use it as key into a map
            string dataFilename = SingleMapReference.GetFilenameFromLocation(location);

            // create an offset counter if it doesn't already exist
            if (!_roomOffsetCountDictionary.ContainsKey(dataFilename)) _roomOffsetCountDictionary[dataFilename] = 0;

            // get the current file offset value (auto-counting...)
            short roomOffset = _roomOffsetCountDictionary[dataFilename];

            // add one or more map references 
            MapReferenceList.AddRange(GenerateSingleMapReferences(location, hasBasement ? -1 : 0, nFloors, roomOffset));

            // add the number of floors you have just added so that it can increment the file offset for subsequent calls
            _roomOffsetCountDictionary[dataFilename] += nFloors;

            _smallMapBasementDictionary.Add(location, hasBasement);
            _nFloorsDictionary.Add(location, nFloors);
        }

        /// <summary>
        ///     Cheater function to automatically create floors in a building
        /// </summary>
        /// <param name="location"></param>
        /// <param name="startFloor"></param>
        /// <param name="nFloors"></param>
        /// <param name="roomOffset"></param>
        /// <returns></returns>
        private static IEnumerable<SingleMapReference> GenerateSingleMapReferences(SingleMapReference.Location location,
            int startFloor, short nFloors, short roomOffset)
        {
            List<SingleMapReference> mapRefs = new List<SingleMapReference>();

            int fileOffset =
                roomOffset * SmallMap.X_TILES *
                SmallMap.Y_TILES; // the number of rooms offset, converted to number of bytes to skip

            for (int i = 0; i < nFloors; i++)
            {
                mapRefs.Add(new SingleMapReference(GameReferences.DataOvlRef.DataDirectory, location, startFloor + i,
                    fileOffset + i * SmallMap.X_TILES * SmallMap.Y_TILES));
            }

            return mapRefs;
        }

        /// <summary>
        ///     Get the location based on the index within the small map file
        /// </summary>
        /// <param name="smallMap">the small map file reference</param>
        /// <param name="index">the 0-7 index within the small map file</param>
        /// <returns>the location reference</returns>
        public SingleMapReference.Location GetLocationByIndex(SingleMapReference.SmallMapMasterFiles smallMap,
            int index)
        {
            SingleMapReference.Location location = _masterFileLocationDictionary[smallMap][index];
            return location;
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public string GetLocationName(SingleMapReference.Location location)
        {
            string getLocationNameStr(DataOvlReference.LocationStrings index) =>
                _dataRef.GetStringFromDataChunkList(DataOvlReference.DataChunkName.LOCATION_NAMES, (int)index);

            // filthy way to convert our more commonly used _location enum to the less used LOCATION_STRINGS
            // they didn't even bother having them all match, and then decided to leave some out
            DataOvlReference.LocationStrings newLocStrEnum =
                (DataOvlReference.LocationStrings)Enum.Parse(typeof(DataOvlReference.LocationStrings),
                    location.ToString());

            // if the DataOVL didn't provide a name, then we are forced to set our own... :(
            if ((int)newLocStrEnum >= 0) return getLocationNameStr(newLocStrEnum);

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            return newLocStrEnum switch
            {
                DataOvlReference.LocationStrings.Suteks_Hut => "SUTEK'S HUT",
                DataOvlReference.LocationStrings.SinVraals_Hut => "SIN VRAAL'S HUT",
                DataOvlReference.LocationStrings.Grendels_Hut => "GRENDAL'S HUT",
                DataOvlReference.LocationStrings.Lord_Britishs_Castle => "LORD BRITISH'S CASTLE",
                DataOvlReference.LocationStrings.Palace_of_Blackthorn => "PALACE OF BLACKTHORN",
                _ => throw new Ultima5ReduxException("Ummm asked for a location name and wasn't on the guest list.")
            };
        }

        public string GetLocationTypeStr(SingleMapReference.Location location)
        {
            // anon function for quick lookup of strings
            string getTypePlaceStr(DataOvlReference.WorldStrings index) =>
                _dataRef.GetStringFromDataChunkList(DataOvlReference.DataChunkName.WORLD, (int)index);

            switch (location)
            {
                case SingleMapReference.Location.Lord_Britishs_Castle:
                    return getTypePlaceStr(DataOvlReference.WorldStrings.to_enter_CASTLE_LB);
                case SingleMapReference.Location.Palace_of_Blackthorn:
                    return getTypePlaceStr(DataOvlReference.WorldStrings.to_enter_PALACE_B);
                case SingleMapReference.Location.East_Britanny:
                case SingleMapReference.Location.West_Britanny:
                case SingleMapReference.Location.North_Britanny:
                case SingleMapReference.Location.Paws:
                case SingleMapReference.Location.Cove:
                    return getTypePlaceStr(DataOvlReference.WorldStrings.to_enter_VILLAGE);
                case SingleMapReference.Location.Moonglow:
                case SingleMapReference.Location.Britain:
                case SingleMapReference.Location.Jhelom:
                case SingleMapReference.Location.Yew:
                case SingleMapReference.Location.Minoc:
                case SingleMapReference.Location.Trinsic:
                case SingleMapReference.Location.Skara_Brae:
                case SingleMapReference.Location.New_Magincia:
                    return getTypePlaceStr(DataOvlReference.WorldStrings.to_enter_TOWNE);
                case SingleMapReference.Location.Fogsbane:
                case SingleMapReference.Location.Stormcrow:
                case SingleMapReference.Location.Waveguide:
                case SingleMapReference.Location.Greyhaven:
                    return getTypePlaceStr(DataOvlReference.WorldStrings.to_enter_LIGHTHOUSE);
                case SingleMapReference.Location.Iolos_Hut:
                //case _location.spektran
                case SingleMapReference.Location.Suteks_Hut:
                case SingleMapReference.Location.SinVraals_Hut:
                case SingleMapReference.Location.Grendels_Hut:
                    return getTypePlaceStr(DataOvlReference.WorldStrings.to_enter_HUT);
                case SingleMapReference.Location.Ararat:
                    return getTypePlaceStr(DataOvlReference.WorldStrings.to_enter_RUINS);
                case SingleMapReference.Location.Bordermarch:
                case SingleMapReference.Location.Farthing:
                case SingleMapReference.Location.Windemere:
                case SingleMapReference.Location.Stonegate:
                case SingleMapReference.Location.Lycaeum:
                case SingleMapReference.Location.Empath_Abbey:
                case SingleMapReference.Location.Serpents_Hold:
                case SingleMapReference.Location.Buccaneers_Den:
                    return getTypePlaceStr(DataOvlReference.WorldStrings.to_enter_KEEP);
                case SingleMapReference.Location.Deceit:
                case SingleMapReference.Location.Despise:
                case SingleMapReference.Location.Destard:
                case SingleMapReference.Location.Wrong:
                case SingleMapReference.Location.Covetous:
                case SingleMapReference.Location.Shame:
                case SingleMapReference.Location.Hythloth:
                case SingleMapReference.Location.Doom:
                    return getTypePlaceStr(DataOvlReference.WorldStrings.to_enter_DUNGEON);
                case SingleMapReference.Location.Britannia_Underworld:
                    break;
                case SingleMapReference.Location.Combat_resting_shrine:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return "";
        }

        public int GetNumberOfFloors(SingleMapReference.Location location)
        {
            return _nFloorsDictionary[location];
        }

        /// <summary>
        ///     Get a map reference based on a location
        /// </summary>
        /// <param name="location">The location you are looking for</param>
        /// <param name="floor"></param>
        /// <returns>a single map reference providing details on the map itself</returns>
        public SingleMapReference GetSingleMapByLocation(SingleMapReference.Location location, int floor)
        {
            foreach (SingleMapReference mapRef in MapReferenceList.Where(mapRef =>
                mapRef.MapLocation == location && mapRef.Floor == floor))
            {
                return mapRef;
            }

            throw new Ultima5ReduxException("_location was not found!");
        }

        public bool HasBasement(SingleMapReference.Location location)
        {
            return _smallMapBasementDictionary[location];
        }

        private void InitializeLocationNames()
        {
            // get the data chunks that have the offsets to the strings in the data.ovl file, representing each location (most)
            DataChunk locationNameOffsetChunk =
                _dataRef.GetDataChunk(DataOvlReference.DataChunkName.LOCATION_NAME_INDEXES);

            // get the offsets 
            List<ushort> locationOffsets = locationNameOffsetChunk.GetChunkAsUint16List();

            // I happen to know that the underworld and overworld is [0], so let's add a placeholder
            _locationNames = new List<string>(locationOffsets.Count + 1) { "Overworld/Underworld" };

            // grab each location string
            // it isn't the most efficient way, but it gets the job done
            foreach (ushort offset in locationOffsets)
            {
                _locationNames.Add(_dataRef
                    .GetDataChunk(DataChunk.DataFormatType.SimpleString, string.Empty, offset, 20).GetChunkAsString()
                    .Replace("_", " "));
            }
        }
    }
}