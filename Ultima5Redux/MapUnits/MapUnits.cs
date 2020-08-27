using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers;
using Ultima5Redux.MapUnits.SeaFaringVessel;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    public class MapUnits
    {
        private const int MAX_MAP_CHARACTERS = 0x20;

        public List<MapUnit> CurrentMapUnits => GetMapUnits(_currentMapType);

        private List<MapUnit> GetMapUnits(LargeMap.Maps map)
        {
            switch (map)
            {
                case LargeMap.Maps.Small:
                    return _smallWorldMapUnits;
                case LargeMap.Maps.Overworld:
                    return _overworldMapUnits;
                case LargeMap.Maps.Underworld:
                    return _underworldMapUnits;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private readonly List<MapUnit> _smallWorldMapUnits = new List<MapUnit>(MAX_MAP_CHARACTERS);
        private readonly List<MapUnit> _overworldMapUnits = new List<MapUnit>(MAX_MAP_CHARACTERS);
        private readonly List<MapUnit> _underworldMapUnits = new List<MapUnit>(MAX_MAP_CHARACTERS);
        
        // load the MapAnimationStates once from disk, don't worry about again until you are saving to disk
        // load the SmallMapCharacterStates once from disk, don't worry abut again until you are saving to disk

        private readonly SmallMapCharacterStates _smallMapCharacterStates;
        private LargeMap.Maps _currentMapType;
        /// <summary>
        /// static references to all NPCs in the world
        /// </summary>
        private NonPlayerCharacterReferences NPCRefs { get; }
        private MapUnitMovements Movements { get; }
        private readonly TileReferences _tileRefs;
        private readonly TimeOfDay _timeOfDay;
        private readonly PlayerCharacterRecords _playerCharacterRecords;
        private readonly MapUnitStates _smallMapUnitStates;
        private readonly MapUnitStates _overworldMapUnitStates;
        private readonly MapUnitStates _underworldMapUnitStates;

        private readonly DataChunk _activeDataChunk;
        private readonly DataChunk _overworldDataChunk;
        private readonly DataChunk _underworldDataChunk;
        
        //public List<NonPlayerCharacterReference> NonPlayerCharacterReferencesList { get; private set; }
        private MapUnitStates _currentMapUnitStates;

        /// <summary>
        /// The single source of truth for the Avatar's current position within the current map
        /// </summary>
        internal MapUnitPosition CurrentAvatarPosition
        {
            get => CurrentMapUnits[0].MapUnitPosition;
            set => CurrentMapUnits[0].MapUnitPosition = value;
        }

        private MapUnitStates CurrentMapUnitStates
        {
            get
            {
                switch (_currentMapType)
                {
                    case LargeMap.Maps.Small:
                        return _smallMapUnitStates;
                    case LargeMap.Maps.Overworld:
                        return _overworldMapUnitStates;
                    case LargeMap.Maps.Underworld:
                        return _underworldMapUnitStates;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                throw new Ultima5ReduxException("Asked for a CurrentMapUnitStates that doesn't exist:" + _currentMapType);
            }
        }

        /// <summary>
        /// Constructs the collection of all Map CurrentMapUnits in overworld, underworld and current towne
        /// </summary>
        /// <param name="tileRefs">Global tile references</param>
        /// <param name="npcRefs">Global NPC references</param>
        /// <param name="activeMapUnitStatesDataChunk"></param>
        /// <param name="overworldMapUnitStatesDataChunk"></param>
        /// <param name="underworldMapUnitStatesDataChunk"></param>
        /// <param name="charStatesDataChunk"></param>
        /// <param name="nonPlayerCharacterMovementLists"></param>
        /// <param name="nonPlayerCharacterMovementOffsets"></param>
        /// <param name="timeOfDay"></param>
        /// <param name="playerCharacterRecords"></param>
        /// <param name="initialMap">The initial map you are beginning on. It's important to know because there is only
        /// one TheSmallMapCharacterState loaded in the save file at load time</param>
        /// <param name="currentSmallMap">The particular map (if small map) that you are loading</param>
        public MapUnits(TileReferences tileRefs, NonPlayerCharacterReferences npcRefs, 
            DataChunk activeMapUnitStatesDataChunk, DataChunk overworldMapUnitStatesDataChunk, 
            DataChunk underworldMapUnitStatesDataChunk, DataChunk charStatesDataChunk,
            DataChunk nonPlayerCharacterMovementLists, DataChunk nonPlayerCharacterMovementOffsets, 
            TimeOfDay timeOfDay, PlayerCharacterRecords playerCharacterRecords,
            LargeMap.Maps initialMap, SmallMapReferences.SingleMapReference.Location currentSmallMap = SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
        {
            // let's make sure they are using the correct combination
            Debug.Assert((initialMap == LargeMap.Maps.Small &&
                          currentSmallMap != SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
                         || initialMap != LargeMap.Maps.Small);

            _currentMapType = initialMap;
            _tileRefs = tileRefs;
            _timeOfDay = timeOfDay;
            _playerCharacterRecords = playerCharacterRecords;

            _activeDataChunk = activeMapUnitStatesDataChunk;
            _overworldDataChunk = overworldMapUnitStatesDataChunk;
            _underworldDataChunk = underworldMapUnitStatesDataChunk;

            // if the small map is being loaded, then we pull from disk and load the over and underworld from
            // saved.ool
            if (initialMap == LargeMap.Maps.Small)
            {
                // small, overworld and underworld always have saved Animation states so we load them in at the beginning
                _smallMapUnitStates = new MapUnitStates(_activeDataChunk, tileRefs);
                _smallMapUnitStates.Load(MapUnitStates.MapUnitStatesFiles.SAVED_GAM, true);

                // we always load the over and underworld from disk immediately, no need to reload as we will track it in memory 
                // going forward
                _overworldMapUnitStates = new MapUnitStates(_overworldDataChunk, tileRefs);

                _underworldMapUnitStates = new MapUnitStates(_underworldDataChunk, tileRefs);
            }
            else 
            {
                // since it is a large map, the small map is empty because the state is lost as soon as you leave it
                // and the selected large map is pulled directly from the active state (saved.gam @ 0x6b8)
                _smallMapUnitStates = null;

                if (initialMap == LargeMap.Maps.Overworld)
                {
                    _overworldMapUnitStates = new MapUnitStates(_activeDataChunk, tileRefs);
                    _underworldMapUnitStates = new MapUnitStates(_underworldDataChunk, tileRefs);
                }
                else
                {
                    _underworldMapUnitStates = new MapUnitStates(_activeDataChunk, tileRefs);
                    _overworldMapUnitStates = new MapUnitStates(_overworldDataChunk, tileRefs);
                }
            }
            _overworldMapUnitStates.Load(MapUnitStates.MapUnitStatesFiles.BRIT_OOL, true);
            _underworldMapUnitStates.Load(MapUnitStates.MapUnitStatesFiles.UNDER_OOL, true );

            // map character states pertain to whichever map was loaded from disk
            _smallMapCharacterStates = new SmallMapCharacterStates(charStatesDataChunk, tileRefs);
            
            // movements pertain to whichever map was loaded from disk
            Movements = new MapUnitMovements(nonPlayerCharacterMovementLists, nonPlayerCharacterMovementOffsets);
            
            NPCRefs = npcRefs;

            // we only load the large maps once and they always exist on disk
            LoadLargeMap(LargeMap.Maps.Overworld, true);
            LoadLargeMap(LargeMap.Maps.Underworld, true);

            // if the small map is the initial map, then load it 
            // otherwise we force the correct states to either the over or underworld
            switch (initialMap)
            {
                case LargeMap.Maps.Small:
                    LoadSmallMap(currentSmallMap, true);
                    break;
                case LargeMap.Maps.Overworld:
                    _currentMapUnitStates = _overworldMapUnitStates;
                    break;
                case LargeMap.Maps.Underworld:
                    _currentMapUnitStates = _underworldMapUnitStates;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(initialMap), initialMap, null);
            }

            _currentMapType = initialMap;
        }

        /// <summary>
        /// Sets the current map type
        /// Called internally to the class only since it has the bLoadFromDisk option
        /// </summary>
        /// <param name="location"></param>
        /// <param name="mapType"></param>
        /// <param name="bLoadFromDisk"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void SetCurrentMapType(SmallMapReferences.SingleMapReference.Location location, LargeMap.Maps mapType,
            bool bLoadFromDisk)
        {
            _currentMapType = mapType;

            // I may need make an additional save of state before wiping these MapUnits out
            
            switch (mapType)
            {
                case LargeMap.Maps.Small:
                    LoadSmallMap(location, bLoadFromDisk);
                    return;
                case LargeMap.Maps.Overworld:
                case LargeMap.Maps.Underworld:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mapType), mapType, null);
            }
        }

        /// <summary>
        /// Sets the current map that the MapUnits represents
        /// </summary>
        /// <param name="location"></param>
        /// <param name="mapType">Is it a small map, overworld or underworld</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetCurrentMapType(SmallMapReferences.SingleMapReference.Location location, LargeMap.Maps mapType) 
        {
            SetCurrentMapType(location, mapType, false);
        }

        public T GetSpecificMapUnitByLocation<T>(LargeMap.Maps map,
            SmallMapReferences.SingleMapReference.Location location, Point2D xy,
            int nFloor, bool bCheckBaseToo = false) where T : MapUnit
        {
            List<MapUnit> mapUnits = GetMapUnits(map);
            
            foreach (MapUnit mapUnit in mapUnits)
            {
                // sometimes characters are null because they don't exist - and that is OK
                if (!mapUnit.IsActive) continue;

                if (mapUnit.MapUnitPosition.XY == xy && 
                    mapUnit.MapUnitPosition.Floor == nFloor && mapUnit.MapLocation == location)
                {
                    if (bCheckBaseToo && mapUnit.GetType().BaseType == typeof(T))
                    {
                        return (T)mapUnit;
                    }
                    // the map unit is at the right position AND is the correct type
                    return mapUnit.GetType() == typeof(T) ? (T)mapUnit : null;
                }
            }
            return null;
        }
        
        
        public T GetSpecificMapUnitByLocation<T>(SmallMapReferences.SingleMapReference.Location location, Point2D xy, 
            int nFloor, bool bCheckBaseToo = false) where T: MapUnit
        {
            return GetSpecificMapUnitByLocation<T>(_currentMapType, location, xy, nFloor, bCheckBaseToo);
        }

        public MapUnit GetMapUnitByLocation(SmallMapReferences.SingleMapReference.Location location, Point2D xy,
            int nFloor)
        {
            foreach (MapUnit mapUnit in CurrentMapUnits)
            {
                // sometimes characters are null because they don't exist - and that is OK
                if (!mapUnit.IsActive) continue;

                if (mapUnit.MapUnitPosition.XY == xy && 
                    mapUnit.MapUnitPosition.Floor == nFloor && mapUnit.MapLocation == location)
                {
                    return mapUnit;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds a new map unit to the next position available
        /// </summary>
        /// <param name="map"></param>
        /// <param name="mapUnit"></param>
        /// <returns>true if successful, false if no room was found</returns>
        internal bool AddNewMapUnit(LargeMap.Maps map, MapUnit mapUnit)
        {
            int nIndex = FindNextFreeMapUnitIndex(map);
            return AddNewMapUnit(map, mapUnit, nIndex);
        }

        /// <summary>
        /// Adds a new map unit to a given position
        /// </summary>
        /// <param name="map"></param>
        /// <param name="mapUnit"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        internal bool AddNewMapUnit(LargeMap.Maps map, MapUnit mapUnit, int nIndex)
        {
            if (nIndex == -1) return false;
            
            List<MapUnit> mapUnits = GetMapUnits(map);
            Debug.Assert(nIndex < mapUnits.Count);
            mapUnits[nIndex] = mapUnit;
            return true;
        }


        /// <summary>
        /// Finds the next available index in the available map unit list
        /// </summary>
        /// <param name="map"></param>
        /// <returns>>= 0 is an index, or -1 means no room found</returns>
        private int FindNextFreeMapUnitIndex(LargeMap.Maps map)
        {
            int nIndex = 0;
            foreach (MapUnit mapUnit in GetMapUnits(map))
            {
                if (mapUnit.GetType() == typeof(EmptyMapUnit))
                {
                    return nIndex;
                }

                nIndex++;
            }

            return -1;
        }

        /// <summary>
        /// Will load last known state from memory (originally disk) and recalculate some values
        /// such as movement as required.
        /// Called only once on load - the state of the large map will persist in and out of small maps
        /// </summary>
        /// <param name="map"></param>
        /// <param name="bInitialLoad"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void LoadLargeMap(LargeMap.Maps map, bool bInitialLoad)
        {
            List<MapUnit> mapUnits;
            
            // the over and underworld animation states are already loaded and can stick around
            switch (map)
            {
                case LargeMap.Maps.Overworld:
                    _currentMapUnitStates = _overworldMapUnitStates;
                    mapUnits = _overworldMapUnits;
                    break;
                case LargeMap.Maps.Underworld:
                    _currentMapUnitStates = _underworldMapUnitStates;
                    mapUnits = _underworldMapUnits;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(map), map, null);
            }
            
            // populate each of the map characters individually
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                // if this is not the initial load of the map then we can trust character states and
                // movements that are already loaded into memory
                MapUnitMovement mapUnitMovement = Movements.GetMovement(i);
                // always clear movements because they are not stored in the data for a LargeMap because
                // the monsters will recalculate every turn based on where the Avatar is 
                mapUnitMovement.ClearMovements();

                // the party is always at zero
                if (i == 0)
                {
                    MapUnit theAvatar = Avatar.CreateAvatar(_tileRefs, 
                        SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, mapUnitMovement);
                    mapUnits.Add(theAvatar);
                    continue;
                }

                // we have retrieved the _currentMapUnitStates based on the map type,
                // now just get the existing animation state which persists on disk for under, over and small maps
                MapUnitState mapUnitState = _currentMapUnitStates.GetCharacterState(i);

                MapUnit newUnit;

                if (_tileRefs.IsFrigate(mapUnitState.Tile1Ref.Index))
                {
                    newUnit = new Frigate(mapUnitState, mapUnitMovement, bInitialLoad, _tileRefs,
                        SmallMapReferences.SingleMapReference.Location.Britannia_Underworld);
                }
                else if (_tileRefs.IsSkiff(mapUnitState.Tile1Ref.Index))
                {
                    newUnit = new Skiff(mapUnitState, mapUnitMovement, bInitialLoad, _tileRefs,
                        SmallMapReferences.SingleMapReference.Location.Britannia_Underworld);
                }
                else
                {
                    newUnit = new EmptyMapUnit();
                }
                
                // add the new character to our list of characters currently on the map
                mapUnits.Add(newUnit);
            }
        }

        /// <summary>
        /// Resets the current map to a default state - typically no monsters and NPCs in there default positions
        /// </summary>
        private void LoadSmallMap(SmallMapReferences.SingleMapReference.Location location, bool bInitialLoad)
        {
            // wipe all existing characters since they cannot exist beyond the load
            _smallWorldMapUnits.Clear();
            
            // are we loading from disk? This should only be done on initial game load since state is immediately 
            // lost when leaving
            if (bInitialLoad)
            {
                Debug.WriteLine("Loading character positions from disk...");
                // we are loading the small animation from disk
                // this is only done if you save the game and reload within a towne
                _smallMapUnitStates.Load(MapUnitStates.MapUnitStatesFiles.SAVED_GAM, true);
            }
            else
            {
                Debug.WriteLine("Loading default character positions...");
                // if we aren't currently on a small map then we need to reassign the current large map to the correct
                // datachunk because it would have otherwise been loaded from saved.gam the first time
                // Note: yes, this is all very confusing - I am confused, but hopefully the abstraction layers
                //       make it consumable
                if (_currentMapType != LargeMap.Maps.Small)
                {
                    if (_currentMapType == LargeMap.Maps.Overworld)
                    {
                        _overworldMapUnitStates.ReassignNewDataChunk(_overworldDataChunk);
                    }
                    else
                    {
                        _underworldMapUnitStates.ReassignNewDataChunk(_underworldDataChunk);
                    }
                }
            }

            // get all the NPC references for the current location
            List<NonPlayerCharacterReference> npcCurrentMapRefs = NPCRefs.GetNonPlayerCharactersByLocation(location);

            // populate each of the map characters individually
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                MapUnitMovement mapUnitMovement = Movements.GetMovement(i);

                // if it is the first index, then it's the Avatar - but if it's the initial load
                // then it will just load from disk, otherwise we need to create a stub
                if (i == 0 && !bInitialLoad)
                {
                    mapUnitMovement.ClearMovements();
                    MapUnit theAvatar = Avatar.CreateAvatar(_tileRefs, location, mapUnitMovement);
                    _smallWorldMapUnits.Add(theAvatar);
                    continue;
                }
                
                // get the specific NPC reference 
                NonPlayerCharacterReference npcRef = npcCurrentMapRefs[i];
                // the smallMapCharacterState contains SmallMap NPC information (not applicable for LargeMap)
                SmallMapCharacterState smallMapCharacterState;
                MapUnitState mapUnitState = null;

                if (bInitialLoad)
                {
                    // character states are only loaded when forced from disk and only on small maps
                    smallMapCharacterState = _smallMapCharacterStates.GetCharacterState(i);

                    if (CurrentMapUnitStates.HasAnyAnimationStates())
                    {
                        mapUnitState =
                            CurrentMapUnitStates.GetCharacterState(smallMapCharacterState.MapUnitAnimationStateIndex);
                    }
                }
                else
                {
                    // we keep the object because we may be required to save this to disk - but since we are
                    // leaving the map there is no need to save their movements
                    mapUnitMovement.ClearMovements();
                    // set a default SmallMapCharacterState based on the given NPC
                    smallMapCharacterState = new SmallMapCharacterState(_tileRefs, npcRef, i, _timeOfDay);
                    // initialize a default MapUnitState 
                    mapUnitState = new MapUnitState(_tileRefs, npcRef);
                }

                _smallWorldMapUnits.Add(new NonPlayerCharacter(npcRef, mapUnitState, smallMapCharacterState, mapUnitMovement, 
                    _timeOfDay, _playerCharacterRecords, bInitialLoad, _tileRefs, location));
            }
        }

        /// <summary>
        /// Creates a new frigate at a dock of a given location 
        /// </summary>
        /// <param name="location"></param>
        /// <param name="virtualMap"></param>
        /// <param name="dataOvlReference"></param>
        public void CreateFrigateAtDock(SmallMapReferences.SingleMapReference.Location location, VirtualMap virtualMap,
            DataOvlReference dataOvlReference)
        {
            int nIndex = FindNextFreeMapUnitIndex(LargeMap.Maps.Overworld);
            if (nIndex == -1) return;

            MapUnitState mapUnitState = _overworldMapUnitStates.GetCharacterState(nIndex);
            Frigate frigate = new Frigate(mapUnitState, Movements.GetMovement(nIndex), false, 
                _tileRefs, SmallMapReferences.SingleMapReference.Location.Britannia_Underworld);
            
            // set position of frigate in the world
            Point2D frigateLocation = virtualMap.GetLocationOfDock(location, dataOvlReference);
            frigate.MapUnitPosition = new MapUnitPosition(frigateLocation.X, frigateLocation.Y, 0);
            frigate.SkiffsAboard = 1;
            frigate.KeyTileReference = _tileRefs.GetTileReferenceByName("ShipNoSailsLeft");
            
            AddNewMapUnit(LargeMap.Maps.Overworld, frigate, nIndex);
        }
    }
}