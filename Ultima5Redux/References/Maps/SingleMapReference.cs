using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.Maps;

namespace Ultima5Redux.References.Maps
{
    public partial class SmallMapReferences
    {
        /// <summary>
        ///     Provides static details on each and every small map location
        /// </summary>
        public class SingleMapReference
        {
            [SuppressMessage("ReSharper", "IdentifierTypo")] [JsonConverter(typeof(StringEnumConverter))]
            public enum Location
            {
                Britannia_Underworld = 0x00, Moonglow = 1, Britain = 2, Jhelom = 3, Yew = 4, Minoc = 5, Trinsic = 6,
                Skara_Brae = 7, New_Magincia = 8, // Town
                Fogsbane = 9, Stormcrow = 10, Greyhaven = 11, Waveguide = 12, Iolos_Hut = 13, Suteks_Hut = 14,
                SinVraals_Hut = 15, Grendels_Hut = 16, // Dwelling
                Lord_Britishs_Castle = 17, Palace_of_Blackthorn = 18, West_Britanny = 19, North_Britanny = 20,
                East_Britanny = 21, Paws = 22, Cove = 23, // Castle
                Buccaneers_Den = 24, Ararat = 25, Bordermarch = 26, Farthing = 27, Windemere = 28, Stonegate = 29,
                Lycaeum = 30, Empath_Abbey = 31, Serpents_Hold = 32, // Keep
                Deceit = 33, Despise = 34, Destard = 35, Wrong = 36, Covetous = 37, Shame = 38, Hythloth = 39,
                Doom = 40, // Dungeons
                Combat_resting_shrine = 41
            }

            /// <summary>
            ///     Map master files. These represent .DAT, .NPC and .TLK files
            /// </summary>
            [JsonConverter(typeof(StringEnumConverter))]
            public enum SmallMapMasterFiles { Castle, Towne, Dwelling, Keep, Dungeon, None }

            /// <summary>
            ///     Total number of small map locations
            /// </summary>
            public const int TOTAL_SMALL_MAP_LOCATIONS = 32;

            /// <summary>
            ///     the floor that the single map represents
            /// </summary>
            [DataMember]
            public int Floor { get; }

            /// <summary>
            ///     the location (ie. single town like Moonglow)
            /// </summary>
            [DataMember]
            public Location MapLocation { get; }

            /// <summary>
            ///     name of the map file
            /// </summary>
            [IgnoreDataMember]
            private string MapFilename => GetFilenameFromLocation(MapLocation);

            /// <summary>
            ///     the offset of the map data in the data file
            /// </summary>
            [IgnoreDataMember]
            public int FileOffset { get; }

            /// <summary>
            ///     ID of the map location (used in saved.gam references)
            ///     Note: If things misbehave - there could be an off-by-one issue depending on how it's being referenced
            /// </summary>
            // ReSharper disable once UnusedMember.Global
            [IgnoreDataMember]
            public byte Id => (byte)MapLocation;
            //(byte)(MapLocation - 1);

            [IgnoreDataMember]
            public Map.Maps MapType
            {
                get
                {
                    switch (MapLocation)
                    {
                        case Location.Britannia_Underworld:
                            return Floor == 0 ? Map.Maps.Overworld : Map.Maps.Underworld;
                        case Location.Combat_resting_shrine:
                            return Map.Maps.Combat;
                    }

                    return MasterFile == SmallMapMasterFiles.Dungeon ? Map.Maps.Dungeon : Map.Maps.Small;
                }
            }

            /// <summary>
            ///     The master file
            /// </summary>
            // ReSharper disable once UnusedMember.Global
            [IgnoreDataMember]
            public SmallMapMasterFiles MasterFile
            {
                get
                {
                    return MapFilename switch
                    {
                        FileConstants.CASTLE_DAT => SmallMapMasterFiles.Castle,
                        FileConstants.TOWNE_DAT => SmallMapMasterFiles.Towne,
                        FileConstants.DWELLING_DAT => SmallMapMasterFiles.Dwelling,
                        FileConstants.KEEP_DAT => SmallMapMasterFiles.Keep,
                        FileConstants.DUNGEON_DAT => SmallMapMasterFiles.Dungeon,
                        _ => throw new Ultima5ReduxException("Bad MasterFile")
                    };
                }
            }

            /// <summary>
            ///     Total tiles per row
            /// </summary>
            [IgnoreDataMember]
            public int XTiles =>
                MapLocation switch
                {
                    Location.Combat_resting_shrine => 16,
                    Location.Britannia_Underworld => 256,
                    _ => SmallMap.X_TILES
                };

            /// <summary>
            ///     Total tiles per column
            /// </summary>
            [IgnoreDataMember]
            public int YTiles =>
                MapLocation switch
                {
                    Location.Combat_resting_shrine => 16,
                    Location.Britannia_Underworld => 256,
                    _ => SmallMap.Y_TILES
                };

            private readonly string _dataDirectory;

            /// <summary>
            ///     Construct a single map reference
            /// </summary>
            /// <param name="dataDirectory"></param>
            /// <param name="mapLocation">overall location (ie. Moonglow)</param>
            /// <param name="floor">the floor in the location (-1 basement, 0 main level, 1+ upstairs)</param>
            /// <param name="fileOffset">location of data offset in map file</param>
            public SingleMapReference(string dataDirectory, Location mapLocation, int floor, int fileOffset)
            {
                _dataDirectory = dataDirectory;
                MapLocation = mapLocation;
                Floor = floor;
                FileOffset = fileOffset;
            }

            /// <summary>
            ///     Loads a small map into a 2D array
            /// </summary>
            /// <param name="mapFilename">name of the file that contains the map</param>
            /// <param name="fileOffset">the file offset to begin reading the file at</param>
            /// <returns></returns>
            private byte[][] LoadSmallMapFile(string mapFilename, int fileOffset)
            {
                List<byte> mapBytes = Utils.GetFileAsByteList(mapFilename);

                byte[][] smallMap = Utils.ListTo2DArray(mapBytes, (short)XTiles, fileOffset, XTiles * YTiles);

                // have to transpose the array because the ListTo2DArray function puts the map together backwards...
                return Utils.TransposeArray(smallMap);
            }

            public static SingleMapReference GetCombatMapSingleInstance() =>
                new(GameReferences.DataOvlRef.DataDirectory, Location.Combat_resting_shrine, 0, 0);

            /// <summary>
            ///     Get the filename of the map data based on the location
            /// </summary>
            /// <param name="location">the location you are looking for</param>
            /// <returns>the filename string</returns>
            public static string GetFilenameFromLocation(Location location)
            {
                return GetMapMasterFromLocation(location) switch
                {
                    SmallMapMasterFiles.Castle => FileConstants.CASTLE_DAT,
                    SmallMapMasterFiles.Towne => FileConstants.TOWNE_DAT,
                    SmallMapMasterFiles.Dwelling => FileConstants.DWELLING_DAT,
                    SmallMapMasterFiles.Keep => FileConstants.KEEP_DAT,
                    SmallMapMasterFiles.Dungeon => FileConstants.DUNGEON_DAT,
                    _ => throw new Ultima5ReduxException("Bad _location")
                };
            }

            public static SingleMapReference GetLargeMapSingleInstance(Map.Maps map)
            {
                if (map == Map.Maps.Small)
                    throw new Ultima5ReduxException("Can't ask for a small map when you need a large one");

                return new SingleMapReference(GameReferences.DataOvlRef.DataDirectory, Location.Britannia_Underworld,
                    map == Map.Maps.Overworld ? 0 : -1, 0);
            }

            /// <summary>
            ///     Gets the master file type based on the location
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
                    //case _location.spektran
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
                    case Location.Deceit:
                    case Location.Despise:
                    case Location.Destard:
                    case Location.Wrong:
                    case Location.Covetous:
                    case Location.Shame:
                    case Location.Hythloth:
                    case Location.Doom:
                        return SmallMapMasterFiles.Dungeon;
                    case Location.Britannia_Underworld:
                    case Location.Combat_resting_shrine:
                        return SmallMapMasterFiles.None;
                    default:
                        throw new Ultima5ReduxException("EH?");
                }
            }

            /// <summary>
            ///     Gets the NPC file based on the master map file
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
                    case SmallMapMasterFiles.Dungeon:
                        break;
                }

                throw new Ultima5ReduxException("Couldn't map NPC filename");
            }

            /// <summary>
            ///     Get the name of the .TLK file based on the master map file
            /// </summary>
            /// <param name="mapMaster"></param>
            /// <returns>name of the .TLK file</returns>
            public static string GetTlkFilenameFromMasterFile(SmallMapMasterFiles mapMaster)
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
                    case SmallMapMasterFiles.Dungeon:
                        break;
                }

                throw new Ultima5ReduxException("Couldn't map NPC filename");
            }

            public override string ToString()
            {
                string mapStr = MapLocation.ToString().Replace("_", " ") + " - ";
                if (Floor == -1) mapStr += "Basement";
                else if (Floor == 0) mapStr += "Main Level";
                else mapStr += "Floor " + Floor;
                return mapStr;
            }

            public byte[][] GetDefaultMap()
            {
                return LoadSmallMapFile(Path.Combine(_dataDirectory, GetFilenameFromLocation(MapLocation)), FileOffset);
            }
        }
    }
}