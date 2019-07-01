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
            public SingleMapReference(Location mapLocation, int floor, int fileOffset)
            {
                MapLocation = mapLocation;
                Floor = floor;
                FileOffset = fileOffset;
            }

            //public enum MapFile {CASTLE_DAT="castle.dat", KEEP_DAT="keep.dat" };
            public enum Location { Britainnia_Underworld = 0x00, Moonglow, Britain, Jhelom, Yew, Minoc, Trinsic, Skara_Brae, New_Magincia, Fogsbane, Stormcrow, Greyhaven, Waveguide, Iolos_Hut, Suteks_Hut, SinVraals_Hut,
                Grendels_Hut, Lord_Britishs_Castle, Palace_of_Blackthorn, West_Britanny, North_Britanny, East_Britanny, Paws, Cove, Buccaneers_Den, Ararat, Bordermarch,
                Farthing, Windemere, Stonegate, Lycaeum, Empath_Abbey, Serpents_Hold }

            public byte Id
            {
                get
                {
                    return (byte)MapLocation;
                }
            }
            public string MapFilename
            {
                get
                {
                    return GetFilenameFromLocation(MapLocation);
                }
            }

            public int Floor { get; set; }
            public int FileOffset { get; set; }
            public Location MapLocation { get; set; }

            public static string GetFilenameFromLocation (Location location)
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
                        return "castle.dat";
                    case Location.Moonglow:
                    case Location.Britain:
                    case Location.Jhelom:
                    case Location.Yew:
                    case Location.Minoc:
                    case Location.Trinsic:
                    case Location.Skara_Brae:
                    case Location.New_Magincia:
                        return "towne.dat";
                    case Location.Fogsbane:
                    case Location.Stormcrow:
                    case Location.Waveguide:
                    case Location.Greyhaven:
                    case Location.Iolos_Hut:
                    //case Location.spektran
                    case Location.Suteks_Hut:
                    case Location.SinVraals_Hut:
                    case Location.Grendels_Hut:
                        return "dwelling.dat";
                    case Location.Ararat:
                    case Location.Bordermarch:
                    case Location.Farthing:
                    case Location.Windemere:
                    case Location.Stonegate:
                    case Location.Lycaeum:
                    case Location.Empath_Abbey:
                    case Location.Serpents_Hold:
                        return "keep.dat";
                }
                return "";
            }
        }

        public List<SingleMapReference> MapReferenceList
        {
            get
            {
                return mapReferences;
            }
        }

        // the master copy of the map references
        private List<SingleMapReference> mapReferences = new List<SingleMapReference>();
        // a tally of current file offsets used for auto-incrementing
        private Dictionary<string, short> roomOffsetCountDictionary = new Dictionary<string, short>();

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

        public SmallMapReference()
        {
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
            AddLocation(SingleMapReference.Location.Windemere, false, 1);
            AddLocation(SingleMapReference.Location.Stonegate, false, 1);
            AddLocation(SingleMapReference.Location.Empath_Abbey, false, 3);
            AddLocation(SingleMapReference.Location.Greyhaven, true, 3);
        }

    }
}
