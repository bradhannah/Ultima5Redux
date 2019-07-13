using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    class SmallMapReference
    {
        /// <summary>
        /// Provides static details on each and every small map location
        /// </summary>
        public class SingleMapReference
        {
            /// <summary>
            /// Construct a single map reference
            /// </summary>
            /// <param name="mapLocation">overall location (ie. Moonglow)</param>
            /// <param name="floor">the floor in the location (-1 basement, 0 main level, 1+ upstairs)</param>
            /// <param name="fileOffset">location of data offset in map file</param>
            public SingleMapReference(Location mapLocation, int floor, int fileOffset)
            {
                MapLocation = mapLocation;
                Floor = floor;
                FileOffset = fileOffset;
            }

            public enum Location { Britainnia_Underworld = 0x00, Moonglow, Britain, Jhelom, Yew, Minoc, Trinsic, Skara_Brae, New_Magincia, Fogsbane, Stormcrow, Greyhaven, Waveguide, Iolos_Hut, Suteks_Hut, SinVraals_Hut,
                Grendels_Hut, Lord_Britishs_Castle, Palace_of_Blackthorn, West_Britanny, North_Britanny, East_Britanny, Paws, Cove, Buccaneers_Den, Ararat, Bordermarch,
                Farthing, Windemere, Stonegate, Lycaeum, Empath_Abbey, Serpents_Hold }

            /// <summary>
            /// Map master files. These represent .DAT, .NPC and .TLK files
            /// </summary>
            public enum SmallMapMasterFiles { Castle, Towne, Dwelling, Keep };

            /// <summary>
            /// ID of the map location
            /// </summary>
            public byte Id
            {
                get
                {
                    return (byte)MapLocation;
                }
            }

            /// <summary>
            /// name of the map file
            /// </summary>
            public string MapFilename
            {
                get
                {
                    return GetFilenameFromLocation(MapLocation);
                }
            }

            /// <summary>
            /// the floor that the single map represents
            /// </summary>
            public int Floor { get; set; }
            
            /// <summary>
            /// the offset of the map data in the data file
            /// </summary>
            public int FileOffset { get; set; }

            /// <summary>
            /// the location (ie. single town like Moonglow)
            /// </summary>
            public Location MapLocation { get; set; }

            /// <summary>
            /// The master file
            /// </summary>
            public SmallMapMasterFiles MasterFile
            {
                get
                {
                    switch (MapFilename)
                    {

                        case FileConstants.CASTLE_DAT:
                            return SmallMapMasterFiles.Castle;
                        case FileConstants.TOWNE_DAT:
                            return SmallMapMasterFiles.Towne;
                        case FileConstants.DWELLING_DAT:
                            return SmallMapMasterFiles.Dwelling;
                        case FileConstants.KEEP_DAT:
                            return SmallMapMasterFiles.Keep;
                    }
                    throw (new Exception("Bad MasterFile"));
                }
            }

            /// <summary>
            /// Get the name of the .TLK file based on the master map file
            /// </summary>
            /// <param name="mapMaster"></param>
            /// <returns>name of the .TLK file</returns>
            public static string GetTLKFilenameFromMasterFile(SmallMapMasterFiles mapMaster)
            {
                switch (mapMaster)
                {
                    case SmallMapMasterFiles.Castle:
                        return FileConstants.CASTLE_TLK;
                    case SmallMapMasterFiles.Dwelling:
                        return FileConstants.DWELLING_TLK;
                    case SmallMapMasterFiles.Keep:
                        return FileConstants.KEEP_TLK;
                    case SmallMapMasterFiles.Towne:
                        return FileConstants.TOWNE_TLK;
                }
                throw (new Exception("Couldn't map NPC filename"));
            }

            /// <summary>
            /// Gets the NPC file based on the master map file
            /// </summary>
            /// <param name="mapMaster"></param>
            /// <returns>name of the .NPC file</returns>
            public static string GetNPCFilenameFromMasterFile(SmallMapMasterFiles mapMaster)
            {
                switch (mapMaster)
                {
                    case SmallMapMasterFiles.Castle:
                        return FileConstants.CASTLE_NPC;
                    case SmallMapMasterFiles.Dwelling:
                        return FileConstants.DWELLING_NPC;
                    case SmallMapMasterFiles.Keep:
                        return FileConstants.KEEP_NPC;
                    case SmallMapMasterFiles.Towne:
                        return FileConstants.TOWNE_NPC;
                }
                throw (new Exception("Couldn't map NPC filename"));
            }

            /// <summary>
            /// Gets the master file type based on the location
            /// </summary>
            /// <param name="location"></param>
            /// <returns></returns>
            public static SmallMapMasterFiles GetMapMasterFromLocation(Location location)
            {
                switch (location)
                {
                    case Location.Lord_Britishs_Castle:
                    case Location.Palace_of_Blackthorn:
                    case Location.East_Britanny:
                    case Location.West_Britanny:
                    case Location.North_Britanny:
                    case Location.Paws:
                    case Location.Cove:
                    case Location.Buccaneers_Den:
                        return SmallMapMasterFiles.Castle;
                    case Location.Moonglow:
                    case Location.Britain:
                    case Location.Jhelom:
                    case Location.Yew:
                    case Location.Minoc:
                    case Location.Trinsic:
                    case Location.Skara_Brae:
                    case Location.New_Magincia:
                        return SmallMapMasterFiles.Towne;
                    case Location.Fogsbane:
                    case Location.Stormcrow:
                    case Location.Waveguide:
                    case Location.Greyhaven:
                    case Location.Iolos_Hut:
                    //case Location.spektran
                    case Location.Suteks_Hut:
                    case Location.SinVraals_Hut:
                    case Location.Grendels_Hut:
                        return SmallMapMasterFiles.Dwelling;
                    case Location.Ararat:
                    case Location.Bordermarch:
                    case Location.Farthing:
                    case Location.Windemere:
                    case Location.Stonegate:
                    case Location.Lycaeum:
                    case Location.Empath_Abbey:
                    case Location.Serpents_Hold:
                        return SmallMapMasterFiles.Keep;
                }
                throw new Exception("EH?");
            }

            /// <summary>
            /// Get the filename of the map data based on the location
            /// </summary>
            /// <param name="location">the location you are looking for</param>
            /// <returns>the filename string</returns>
            public static string GetFilenameFromLocation (Location location)
            {
                switch (GetMapMasterFromLocation(location))
                {
                    case SmallMapMasterFiles.Castle:
                        return FileConstants.CASTLE_DAT;
                    case SmallMapMasterFiles.Towne:
                        return FileConstants.TOWNE_DAT;
                    case SmallMapMasterFiles.Dwelling:
                        return FileConstants.DWELLING_DAT;
                    case SmallMapMasterFiles.Keep:
                        return FileConstants.KEEP_DAT;
                }
                throw (new Exception("Bad Location"));
            }
        }

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

        // the master copy of the map references
        private List<SingleMapReference> mapReferences = new List<SingleMapReference>();
        
        // total number of master files (towne, keep, castle and dwelling)
        private const int MASTER_FILES = 4;
        
        // a tally of current file offsets used for auto-incrementing
        private Dictionary<string, short> roomOffsetCountDictionary = new Dictionary<string, short>();
        
        // a mapping of the master file -> a list of locations, which also provides an index value for the town (implied within list)
        private Dictionary<SmallMapReference.SingleMapReference.SmallMapMasterFiles, List<SingleMapReference.Location>> masterFileLocationDictionary = 
            new Dictionary<SingleMapReference.SmallMapMasterFiles, List<SingleMapReference.Location>>(MASTER_FILES);

        /// <summary>
        /// Cheater function to automatically create floors in a building
        /// </summary>
        /// <param name="location"></param>
        /// <param name="startFloor"></param>
        /// <param name="nFloors"></param>
        /// <returns></returns>
        private static List<SingleMapReference> GenerateSingleMapReferences(SingleMapReference.Location location, int startFloor, short nFloors, short roomOffset)
        {
            List<SingleMapReference> mapRefs = new List<SingleMapReference>();

            int fileOffset = roomOffset * SmallMap.XTILES * SmallMap.YTILES; // the number of rooms offset, converted to number of bytes to skip

            for (int i=0; i < nFloors; i++)
            {
                mapRefs.Add(new SingleMapReference(location, startFloor + i, fileOffset + (i * SmallMap.XTILES * SmallMap.YTILES)));
            }

            return mapRefs;
        }


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

            string dataFilename = SingleMapReference.GetFilenameFromLocation(location);
            // create an offset counter if it doesn't already exist
            if (!roomOffsetCountDictionary.ContainsKey(dataFilename))
            {
                roomOffsetCountDictionary[dataFilename] = 0;
            }

            // get the current file offset value (auto-counting...)
            short roomOffset = roomOffsetCountDictionary[dataFilename];

            // add one or more map references 
            mapReferences.AddRange(GenerateSingleMapReferences(location, hasBasement?-1:0, nFloors, roomOffset));

            // add the number of floors you have just added so that it can increment the file offset for subequent calls
            roomOffsetCountDictionary[dataFilename] += nFloors;
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
        public SingleMapReference GetSingleMapByLocation (SingleMapReference.Location location)
        {
            SingleMapReference.SmallMapMasterFiles masterMap = SingleMapReference.GetMapMasterFromLocation(location);
            foreach (SingleMapReference mapRef in mapReferences)
            {
                if (mapRef.MapLocation == location)
                {
                    return mapRef; 
                }
            }
            throw new Exception("Location was not found!");
        }

        /// <summary>
        /// Construct all small map references
        /// </summary>
        public SmallMapReference()
        {
            // I wish I could do this a smarter way, but as of now I have no idea where this data is stored in any of the data files

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
            AddLocation(SingleMapReference.Location.Fogsbane, false, 2);
            AddLocation(SingleMapReference.Location.Stormcrow, false, 2);
            AddLocation(SingleMapReference.Location.Greyhaven, false, 2);
            AddLocation(SingleMapReference.Location.Waveguide, false, 2);
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
        }

    }
}
