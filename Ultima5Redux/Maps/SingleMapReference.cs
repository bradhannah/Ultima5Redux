﻿using System;

namespace Ultima5Redux
{
    public partial class SmallMapReferences
    {
        /// <summary>
        /// Provides static details on each and every small map location
        /// </summary>
        public class SingleMapReference
        {
            #region Constructors
            /// <summary>
            /// Construct a single map reference
            /// </summary>
            /// <param name="mapLocation">overall location (ie. Moonglow)</param>
            /// <param name="floor">the floor in the location (-1 basement, 0 main level, 1+ upstairs)</param>
            /// <param name="fileOffset">location of data offset in map file</param>
            public SingleMapReference(Location mapLocation, int floor, int fileOffset, DataOvlReference dataRef)
            {
                MapLocation = mapLocation;
                Floor = floor;
                FileOffset = fileOffset;
                DataRef = dataRef;


            }

            public static SingleMapReference GetLargeMapSingleInstance(LargeMap.Maps map)
            {
                if (map == LargeMap.Maps.Small) { throw new Ultima5ReduxException("Can't ask for a small map when you need a large one");}

                return (new SingleMapReference(Location.Britannia_Underworld, map == LargeMap.Maps.Overworld ? 0 : -1,
                    0, null));
            }
            
            #endregion

            #region Constants/Enumerations
            /// <summary>
            /// Total number of small map locations
            /// </summary>
            public const int TOTAL_SMALL_MAP_LOCATIONS = 32;

            public enum Location {
                Britannia_Underworld = 0x00,
                Moonglow, Britain, Jhelom, Yew, Minoc, Trinsic, Skara_Brae, New_Magincia, // Town
                Fogsbane, Stormcrow, Greyhaven, Waveguide, Iolos_Hut, Suteks_Hut, SinVraals_Hut, Grendels_Hut, // Dwelling
                Lord_Britishs_Castle, Palace_of_Blackthorn, West_Britanny, North_Britanny, East_Britanny, Paws, Cove, // Castle
                Buccaneers_Den, Ararat, Bordermarch, Farthing, Windemere, Stonegate, Lycaeum, Empath_Abbey, Serpents_Hold, // Keep
                Deceit, Despise, Destard, Wrong, Covetous, Shame, Hythloth, Doom, // Dungeons
                Combat_resting_shrine 
            }

            /// <summary>
            /// Map master files. These represent .DAT, .NPC and .TLK files
            /// </summary>
            public enum SmallMapMasterFiles { Castle, Towne, Dwelling, Keep, Dungeon };
            #endregion

            #region Public Properties
 

            public DataOvlReference DataRef { get; }

            /// <summary>
            /// ID of the map location (used in saved.gam references)
            /// Note: If things misbehave - there could be an off-by-one issue depending on how it's being referenced
            /// </summary>
            public byte Id
            {
                get
                {
                    return (byte)(MapLocation-1);
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
            /// The official name of the location
            /// </summary>
            public string Name { get;  }

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
                    throw new Ultima5ReduxException("Bad MasterFile");
                }
            }


            #endregion

            #region Public Methods
            public override string ToString()
            {
                string mapStr = this.MapLocation.ToString().Replace("_", " ") + " - ";
                if (Floor == -1) mapStr += "Basement";
                else if (Floor == 0) mapStr += "Main Level";
                else mapStr += "Floor " + Floor.ToString();
                return mapStr;
            }
            #endregion

            #region Public Static Methods


            /// <summary>
            /// Get the name of the .TLK file based on the master map file
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
                }
                throw new Ultima5ReduxException("Couldn't map NPC filename");
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
                throw new Ultima5ReduxException("Couldn't map NPC filename");
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
                    case Location.Deceit:
                    case Location.Despise:
                    case Location.Destard:
                    case Location.Wrong:
                    case Location.Covetous:
                    case Location.Shame:
                    case Location.Hythloth:
                    case Location.Doom:
                        return SmallMapMasterFiles.Dungeon;
                }
                throw new Ultima5ReduxException("EH?");
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
                    case SmallMapMasterFiles.Dungeon:
                        return "NOFILE";
                }
                throw new Ultima5ReduxException("Bad Location");
            }
            #endregion
        }

    }
}
