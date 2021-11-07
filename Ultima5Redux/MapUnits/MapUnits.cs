using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.Serialization;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.Monsters;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    
    [DataContract] public class MapUnits
    {
        internal const int MAX_MAP_CHARACTERS = 0x20;
        private readonly bool _bUseExtendedSprites;


        /// <summary>
        ///     static references to all NPCs in the world
        /// </summary>
        [IgnoreDataMember] private readonly NonPlayerCharacterReferences _npcRefs;
        [IgnoreDataMember] private readonly DataOvlReference _dataOvlReference;
        [IgnoreDataMember] private readonly EnemyReferences _enemyReferences;
        private readonly NonPlayerCharacterStates _npcStates;
        [IgnoreDataMember] private readonly TileReferences _tileReferences;
        [IgnoreDataMember] private readonly TimeOfDay _timeOfDay;
        
        [IgnoreDataMember] private readonly Avatar _masterAvatarMapUnit;

        [IgnoreDataMember] private readonly PlayerCharacterRecords _playerCharacterRecords;

        // load the MapAnimationStates once from disk, don't worry about again until you are saving to disk
        // load the SmallMapCharacterStates once from disk, don't worry abut again until you are saving to disk

        [DataMember] public MapUnitCollection SmallMapUnitCollection { get; } = new MapUnitCollection();
        [DataMember] public MapUnitCollection UnderworldMapUnitCollection { get; } = new MapUnitCollection();
        [DataMember] public MapUnitCollection OverworldMapMapUnitCollection { get; } = new MapUnitCollection();
        [DataMember] public MapUnitCollection CombatMapMapUnitCollection { get; } = new MapUnitCollection();
        
        //[IgnoreDataMember] private readonly DataChunk _overworldDataChunk;
        [DataMember] private readonly MapUnitStates _overworldMapUnitStates;

        [DataMember] private readonly SmallMapCharacterStates _smallMapCharacterStates;

        //[IgnoreDataMember] private readonly DataChunk _underworldDataChunk;
        [DataMember] private readonly MapUnitStates _underworldMapUnitStates;
        
        [IgnoreDataMember] private MapUnitStates _combatMapUnitStates;
        [DataMember] private SmallMapReferences.SingleMapReference.Location _currentLocation;

        [IgnoreDataMember] public MapUnitCollection CurrentMapUnits => GetMapUnitCollection(_currentMapType);
            //GetMapUnits(_currentMapType);

        [DataMember] private Map.Maps _currentMapType;
        [DataMember] private MapUnitStates _smallMapUnitStates { get; set; }
        
        /// <summary>
        ///     The single source of truth for the Avatar's current position within the current map
        /// </summary>
        [IgnoreDataMember] internal MapUnitPosition CurrentAvatarPosition
        {
            get => AvatarMapUnit.MapUnitPosition;
            set => AvatarMapUnit.MapUnitPosition = value;
        }

        [IgnoreDataMember] private readonly MapUnitMovements _importedMovements;

        // ReSharper disable once UnusedMember.Local
        [IgnoreDataMember] private MapUnitStates CurrentMapUnitStates
        {
            get
            {
                switch (_currentMapType)
                {
                    case Map.Maps.Small:
                        return _smallMapUnitStates;
                    case Map.Maps.Overworld:
                        return _overworldMapUnitStates;
                    case Map.Maps.Underworld:
                        return _underworldMapUnitStates;
                    case Map.Maps.Combat:
                        return _combatMapUnitStates;
                    default:
                        throw new Ultima5ReduxException("Asked for a CurrentMapUnitStates that doesn't exist:" +
                                                        _currentMapType);
                }
            }
        }

        [IgnoreDataMember] public Avatar AvatarMapUnit => CurrentMapUnits.TheAvatar;

        /// <summary>
        ///     Constructs the collection of all Map CurrentMapUnits in overworld, underworld and current towne
        /// </summary>
        /// <param name="tileReferences">Global tile references</param>
        /// <param name="npcRefs">Global NPC references</param>
        /// <param name="timeOfDay"></param>
        /// <param name="playerCharacterRecords"></param>
        /// <param name="initialMap">
        ///     The initial map you are beginning on. It's important to know because there is only
        ///     one TheSmallMapCharacterState loaded in the save file at load time
        /// </param>
        /// <param name="bUseExtendedSprites"></param>
        /// <param name="enemyReferences"></param>
        /// <param name="importedGameState"></param>
        /// <param name="npcStates"></param>
        /// <param name="currentSmallMap">The particular map (if small map) that you are loading</param>
        /// <param name="dataOvlReference"></param>
        public MapUnits(TileReferences tileReferences, NonPlayerCharacterReferences npcRefs,
            TimeOfDay timeOfDay, PlayerCharacterRecords playerCharacterRecords,
            Map.Maps initialMap, DataOvlReference dataOvlReference, bool bUseExtendedSprites,
            EnemyReferences enemyReferences, ImportedGameState importedGameState, 
            NonPlayerCharacterStates npcStates,
            SmallMapReferences.SingleMapReference.Location currentSmallMap =
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
        {
            // let's make sure they are using the correct combination
            // Debug.Assert((initialMap == LargeMap.Maps.Small &&
            //               currentSmallMap != SmallMapReferences.SingleMapReference.Location.Britannia_Underworld));

            _dataOvlReference = dataOvlReference;
            _npcRefs = npcRefs;

            _currentMapType = initialMap;
            _bUseExtendedSprites = bUseExtendedSprites;
            _enemyReferences = enemyReferences;
            _npcStates = npcStates;
            _tileReferences = tileReferences;
            _timeOfDay = timeOfDay;
            _playerCharacterRecords = playerCharacterRecords;
            _currentLocation = currentSmallMap;

            switch (initialMap)
            {
                // if the small map is being loaded, then we pull from disk and load the over and underworld from
                // saved.ool
                case Map.Maps.Combat:
                    throw new Ultima5ReduxException("You can't initialize the MapUnits with a combat map");
                case Map.Maps.Small:
                    // small, overworld and underworld always have saved Animation states so we load them in at the beginning
                    _smallMapUnitStates = importedGameState.ActiveMapUnitStates;
                    _smallMapUnitStates.Load(MapUnitStates.MapUnitStatesFiles.SAVED_GAM, true);

                    // we always load the over and underworld from disk immediately, no need to reload as we will track it in memory 
                    // going forward
                    _overworldMapUnitStates = importedGameState.OverworldMapUnitStates; 
                    _underworldMapUnitStates = importedGameState.UnderworldMapUnitStates; 
                    break;
                case Map.Maps.Overworld:
                case Map.Maps.Underworld:
                {
                    // since it is a large map, the small map is empty because the state is lost as soon as you leave it
                    // and the selected large map is pulled directly from the active state (saved.gam @ 0x6b8)
                    _smallMapUnitStates = null;

                    if (initialMap == Map.Maps.Overworld)
                    {
                        _overworldMapUnitStates = importedGameState.ActiveMapUnitStates;
                            _underworldMapUnitStates = importedGameState.UnderworldMapUnitStates;
                    }
                    else
                    {
                        _underworldMapUnitStates = importedGameState.ActiveMapUnitStates; 
                            _overworldMapUnitStates = importedGameState.OverworldMapUnitStates;
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(initialMap), initialMap, null);
            }

            _overworldMapUnitStates.Load(MapUnitStates.MapUnitStatesFiles.BRIT_OOL, true);
            _underworldMapUnitStates.Load(MapUnitStates.MapUnitStatesFiles.UNDER_OOL, true);

            // map character states pertain to whichever map was loaded from disk
            _smallMapCharacterStates = importedGameState.SmallMapCharacterStates;

            // movements pertain to whichever map was loaded from disk
            _importedMovements = importedGameState.CharacterMovements; 

            // we only load the large maps once and they always exist on disk
            LoadLargeMap(Map.Maps.Overworld, true);
            LoadLargeMap(Map.Maps.Underworld, true);

            // if the small map is the initial map, then load it 
            // otherwise we force the correct states to either the over or underworld
            switch (initialMap)
            {
                case Map.Maps.Small:
                    LoadSmallMap(currentSmallMap, true);
                    break;
                case Map.Maps.Overworld:
                    break;
                case Map.Maps.Underworld:
                    break;
                case Map.Maps.Combat:
                    throw new Ultima5ReduxException("You can't initialize the MapUnits with a combat map");
                default:
                    throw new ArgumentOutOfRangeException(nameof(initialMap), initialMap, null);
            }

            // We will reassign each AvatarMapUnit to the active one. This will ensure that when the Avatar
            // has boarded something, it should carry between maps
            _masterAvatarMapUnit = AvatarMapUnit;
            GetMapUnitCollection(Map.Maps.Overworld).AllMapUnits[0] = _masterAvatarMapUnit;
            GetMapUnitCollection(Map.Maps.Underworld).AllMapUnits[0] = _masterAvatarMapUnit;

            SetAllExtendedSprites();

            _currentMapType = initialMap;
        }

        public void InitializeCombatMapReferences()
        {
            _combatMapUnitStates = new MapUnitStates(_tileReferences);
            CombatMapMapUnitCollection.Clear();
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                CombatMapMapUnitCollection.Add(new EmptyMapUnit());
            }
        }

        /// <summary>
        ///     Force all map units to use or not use extended sprites based on _bUseExtendedSprites field
        /// </summary>
        private void SetAllExtendedSprites()
        {
            foreach (MapUnit mapUnit in OverworldMapMapUnitCollection.AllMapUnits)
            {
                mapUnit.UseFourDirections = _bUseExtendedSprites;
            }

            foreach (MapUnit mapUnit in UnderworldMapUnitCollection.AllMapUnits)
            {
                mapUnit.UseFourDirections = _bUseExtendedSprites;
            }

            if (_smallMapUnitStates == null) return;

            foreach (MapUnit mapUnit in SmallMapUnitCollection.AllMapUnits)
            {
                mapUnit.UseFourDirections = _bUseExtendedSprites;
            }
        }

        internal MapUnitCollection GetMapUnitCollection(Map.Maps map)
        {
            return map switch
            {
                Map.Maps.Small => SmallMapUnitCollection,
                Map.Maps.Overworld => OverworldMapMapUnitCollection,
                Map.Maps.Underworld => UnderworldMapUnitCollection,
                Map.Maps.Combat => CombatMapMapUnitCollection,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        ///     Sets the current map type
        ///     Called internally to the class only since it has the bLoadFromDisk option
        /// </summary>
        /// <param name="mapRef"></param>
        /// <param name="mapType"></param>
        /// <param name="bLoadFromDisk"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetCurrentMapType(SmallMapReferences.SingleMapReference mapRef, Map.Maps mapType,
            bool bLoadFromDisk)//, bool bSkipLoadSmallMap)
        {
            _currentMapType = mapType;
            _currentLocation = mapRef.MapLocation;
            // if the current location hasn't changed then we don't reload the map info
            //bSkipLoadSmallMap |= _currentLocation == mapRef.MapLocation; 

            // I may need make an additional save of state before wiping these MapUnits out
            _playerCharacterRecords.ClearCombatStatuses();

            switch (mapType)
            {
                case Map.Maps.Small:
                    // if (!bSkipLoadSmallMap || !bLoadFromDisk) LoadSmallMap(mapRef.MapLocation, bLoadFromDisk);
                    LoadSmallMap(mapRef.MapLocation, bLoadFromDisk);
                    break;
                case Map.Maps.Combat:
                    return;
                case Map.Maps.Overworld:
                case Map.Maps.Underworld:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mapType), mapType, null);
            }

            AvatarMapUnit.MapLocation = mapRef.MapLocation;
            AvatarMapUnit.MapUnitPosition.Floor = mapRef.Floor;
        }

        /// <summary>
        ///     Sets the current map that the MapUnits represents
        /// </summary>
        /// <param name="mapRef"></param>
        /// <param name="mapType">Is it a small map, overworld or underworld</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetCurrentMapType(SmallMapReferences.SingleMapReference mapRef, Map.Maps mapType)
        {
            SetCurrentMapType(mapRef, mapType, false);
        }

        public T GetSpecificMapUnitByLocation<T>(Map.Maps map,
            Point2D xy, int nFloor, bool bCheckBaseToo = false) where T : MapUnit
        {
            foreach (T mapUnit in GetMapUnitCollection(map).GetMapUnitByType<T>())
            {
                // sometimes characters are null because they don't exist - and that is OK
                if (!mapUnit.IsActive) continue;

                if (mapUnit.MapUnitPosition.XY == xy &&
                    mapUnit.MapUnitPosition.Floor == nFloor) //&& mapUnit.MapLocation == location)
                {
                    if (bCheckBaseToo && mapUnit.GetType().BaseType == typeof(T)) return mapUnit;
                    // the map unit is at the right position AND is the correct type
                    Debug.Assert(mapUnit != null);
                    if (mapUnit.GetType() == typeof(T)) return mapUnit;
                }
            }

            return null;
        }


        /// <summary>
        ///     Gets a particular map unit on a tile in a given location
        /// </summary>
        /// <param name="map"></param>
        /// <param name="xy"></param>
        /// <param name="nFloor"></param>
        /// <returns>MapUnit or null if non exist at location</returns>
        public List<MapUnit> GetMapUnitByLocation(Map.Maps map, Point2D xy, int nFloor)
        {
            List<MapUnit> mapUnits = new List<MapUnit>();

            foreach (MapUnit mapUnit in GetMapUnitCollection(map).AllActiveMapUnits)
            {
                // sometimes characters are null because they don't exist - and that is OK
                Debug.Assert(mapUnit.IsActive);

                if (mapUnit.MapUnitPosition.XY == xy && mapUnit.MapUnitPosition.Floor == nFloor)
                    mapUnits.Add(mapUnit);
            }

            return mapUnits;
        }


        /// <summary>
        ///     Adds a new map unit to the next position available
        /// </summary>
        /// <param name="map"></param>
        /// <param name="mapUnit"></param>
        /// <returns>true if successful, false if no room was found</returns>
        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool AddNewMapUnit(Map.Maps map, MapUnit mapUnit)
        {
            int nIndex = FindNextFreeMapUnitIndex(map);
            return AddNewMapUnit(map, mapUnit, nIndex);
        }

        /// <summary>
        ///     Clear a current map unit, essentially removing it from the world
        ///     Commonly used when something is boarded, and collapses into the Avatar himself
        ///     note: the MapUnit is no longer referenced - but it will often exist within the Avatar
        ///     object if they have in fact boarded it
        /// </summary>
        /// <param name="mapUnitToClear"></param>
        /// <exception cref="Ultima5ReduxException"></exception>
        public void ClearAndSetEmptyMapUnits(MapUnit mapUnitToClear)
        {
            for (int index = 0; index < CurrentMapUnits.AllMapUnits.Count; index++)
            {
                MapUnit mapUnit = CurrentMapUnits.AllMapUnits[index];

                if (mapUnit != mapUnitToClear) continue;

                CurrentMapUnits.AllMapUnits[index] = new EmptyMapUnit();
                return;
            }

            throw new Ultima5ReduxException(
                "You provided a MapUnit to clear, but it is not in the active MapUnit list");
        }

        /// <summary>
        ///     Adds a new map unit to a given position
        /// </summary>
        /// <param name="map"></param>
        /// <param name="mapUnit"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        private bool AddNewMapUnit(Map.Maps map, MapUnit mapUnit, int nIndex)
        {
            if (nIndex == -1) return false;

            List<MapUnit> mapUnits = GetMapUnitCollection(map).AllMapUnits;
            Debug.Assert(nIndex < mapUnits.Count);
            mapUnits[nIndex] = mapUnit;
            mapUnit.UseFourDirections = _bUseExtendedSprites;
            return true;
        }


        /// <summary>
        ///     Finds the next available index in the available map unit list
        /// </summary>
        /// <param name="map"></param>
        /// <returns>>= 0 is an index, or -1 means no room found</returns>
        private int FindNextFreeMapUnitIndex(Map.Maps map)
        {
            int nIndex = 0;
            foreach (MapUnit mapUnit in GetMapUnitCollection(map).AllMapUnits)
            {
                if (mapUnit is EmptyMapUnit) return nIndex;

                nIndex++;
            }

            return -1;
        }

        /// <summary>
        ///     Will load last known state from memory (originally disk) and recalculate some values
        ///     such as movement as required.
        ///     Called only once on load - the state of the large map will persist in and out of small maps
        /// </summary>
        /// <param name="map"></param>
        /// <param name="bInitialLoad"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void LoadLargeMap(Map.Maps map, bool bInitialLoad)
        {
            List<MapUnit> mapUnits;

            // the over and underworld animation states are already loaded and can stick around
            switch (map)
            {
                case Map.Maps.Overworld:
                    mapUnits = OverworldMapMapUnitCollection.AllMapUnits;
                    break;
                case Map.Maps.Underworld:
                    mapUnits = UnderworldMapUnitCollection.AllMapUnits;
                    break;
                case Map.Maps.Combat:
                case Map.Maps.Small:
                    throw new Ultima5ReduxException("You asked for a Small map when loading a large one");
                default:
                    throw new ArgumentOutOfRangeException(nameof(map), map, null);
            }

            // populate each of the map characters individually
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                // if this is not the initial load of the map then we can trust character states and
                // movements that are already loaded into memory
                MapUnitMovement mapUnitMovement = _importedMovements.GetMovement(i);
                // always clear movements because they are not stored in the data for a LargeMap because
                // the monsters will recalculate every turn based on where the Avatar is 
                mapUnitMovement.ClearMovements();

                // we have retrieved the _currentMapUnitStates based on the map type,
                // now just get the existing animation state which persists on disk for under, over and small maps
                MapUnitState mapUnitState = CurrentMapUnitStates.GetCharacterState(i);
                //_currentMapUnitStates.GetCharacterState(i);

                // the party is always at zero
                if (i == 0)
                {
                    MapUnit theAvatar = Avatar.CreateAvatar(_tileReferences,
                        SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, mapUnitMovement,
                        mapUnitState, _dataOvlReference, _bUseExtendedSprites);

                    mapUnits.Add(theAvatar);
                    continue;
                }

                MapUnit newUnit = CreateNewMapUnit(mapUnitState, mapUnitMovement, bInitialLoad,
                    SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, null);
                // add the new character to our list of characters currently on the map
                mapUnits.Add(newUnit);
            }
        }

        /// <summary>
        ///     Resets the current map to a default state - typically no monsters and NPCs in there default positions
        /// </summary>
        private void LoadSmallMap(SmallMapReferences.SingleMapReference.Location location, bool bInitialLoad)
        {
            // wipe all existing characters since they cannot exist beyond the load
            SmallMapUnitCollection.Clear();

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

                // set a blank map unit state because it wasn't saved to disk
                _smallMapUnitStates = new MapUnitStates(_tileReferences);
            }

            // get all the NPC references for the current location
            //List<NonPlayerCharacterReference> npcCurrentMapRefs = _npcRefs.GetNonPlayerCharactersByLocation(location);
            

            // populate each of the map characters individually
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                MapUnitMovement mapUnitMovement = _importedMovements.GetMovement(i);

                // if it is the first index, then it's the Avatar - but if it's the initial load
                // then it will just load from disk, otherwise we need to create a stub
                if (i == 0 && !bInitialLoad)
                {
                    mapUnitMovement.ClearMovements();
                    // load the existing AvatarMapUnit with boarded MapUnits
                    SmallMapUnitCollection.Add(_masterAvatarMapUnit);
                    AvatarMapUnit.MapUnitPosition = SmallMapReferences.GetStartingXYZByLocation();
                    AvatarMapUnit.MapLocation = location;
                    continue;
                } 
                if (i == 0 && bInitialLoad)
                {
                    MapUnitState theAvatarMapState = _smallMapUnitStates.GetCharacterState(0);
                    MapUnit theAvatar = Avatar.CreateAvatar(_tileReferences, location, mapUnitMovement,
                        theAvatarMapState, _dataOvlReference, _bUseExtendedSprites);
                    theAvatar.MapUnitPosition.X = theAvatarMapState.X;
                    theAvatar.MapUnitPosition.Y = theAvatarMapState.Y;
                    theAvatar.MapLocation = location;
                    SmallMapUnitCollection.Add(theAvatar);
                    continue;
                }

                // get the specific NPC reference 
                NonPlayerCharacterState npcState = _npcStates.GetStateByLocationAndIndex(location, i);
                    //npcCurrentMapRefs[i];

                // we keep the object because we may be required to save this to disk - but since we are
                // leaving the map there is no need to save their movements
                mapUnitMovement.ClearMovements();

                // set a default SmallMapCharacterState based on the given NPC
                SmallMapCharacterState smallMapCharacterState =
                    new SmallMapCharacterState(_tileReferences, npcState.NPCRef, i, _timeOfDay);

                // initialize a default MapUnitState 
                MapUnitState mapUnitState = new MapUnitState(_tileReferences, npcState.NPCRef)
                {
                    X = (byte)smallMapCharacterState.TheMapUnitPosition.X,
                    Y = (byte)smallMapCharacterState.TheMapUnitPosition.Y,
                    Floor = (byte)smallMapCharacterState.TheMapUnitPosition.Floor
                };

                MapUnit mapUnit = CreateNewMapUnit(mapUnitState, mapUnitMovement, false, location, 
                    npcState, smallMapCharacterState);

                SmallMapUnitCollection.Add(mapUnit);
            }
        }

        /// <summary>
        ///     Generates a new map unit
        /// </summary>
        /// <param name="mapUnitState"></param>
        /// <param name="mapUnitMovement"></param>
        /// <param name="bInitialLoad"></param>
        /// <param name="location"></param>
        /// <param name="npcState"></param>
        /// <param name="smallMapCharacterState"></param>
        /// <returns></returns>
        private MapUnit CreateNewMapUnit(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement, bool bInitialLoad,
            SmallMapReferences.SingleMapReference.Location location, NonPlayerCharacterState npcState,
            SmallMapCharacterState smallMapCharacterState = null)
        {
            MapUnit newUnit;
            TileReference tileRef = mapUnitState.Tile1Ref;

            if (smallMapCharacterState != null && npcState != null && smallMapCharacterState.Active && npcState.NPCRef.NormalNPC)
            {
                newUnit = new NonPlayerCharacter(mapUnitState, smallMapCharacterState, mapUnitMovement,
                    _timeOfDay, _playerCharacterRecords, bInitialLoad, _tileReferences, location, _dataOvlReference, 
                    npcState);
            }
            else if (mapUnitState.Tile1Ref == null)
            {
                Debug.WriteLine("An empty map unit was created with no tile reference");
                newUnit = new EmptyMapUnit();
            }
            else if (_tileReferences.IsFrigate(mapUnitState.Tile1Ref.Index))
            {
                newUnit = new Frigate(mapUnitState, mapUnitMovement, _tileReferences, location, _dataOvlReference,
                    tileRef.GetDirection(), npcState);
            }
            else if (_tileReferences.IsSkiff(mapUnitState.Tile1Ref.Index))
            {
                newUnit = new Skiff(mapUnitState, mapUnitMovement, _tileReferences, location, _dataOvlReference,
                    tileRef.GetDirection(), npcState);
            }
            else if (_tileReferences.IsMagicCarpet(mapUnitState.Tile1Ref.Index))
            {
                newUnit = new MagicCarpet(mapUnitState, mapUnitMovement, _tileReferences, location, _dataOvlReference,
                    tileRef.GetDirection(), npcState);
            }
            else if (_tileReferences.IsHorse(mapUnitState.Tile1Ref.Index))
            {
                newUnit = new Horse(mapUnitState, mapUnitMovement, _tileReferences, location, _dataOvlReference,
                    tileRef.GetDirection(), npcState);
            }
            else if (_tileReferences.IsMonster(mapUnitState.Tile1Ref.Index))
            {
                Debug.Assert(_enemyReferences != null);
                newUnit = new Enemy(mapUnitState, mapUnitMovement, _tileReferences,
                    _enemyReferences.GetEnemyReference(tileRef), location, _dataOvlReference, _npcRefs, npcState);
            }
            // this is where we will create monsters too
            else
            {
                Debug.WriteLine("An empty map unit was created with " + mapUnitState.Tile1Ref.Name);
                newUnit = new EmptyMapUnit();
            }

            // force to use extended sprites if they exist
            newUnit.UseFourDirections = _bUseExtendedSprites;

            return newUnit;
        }

        private int AddCombatMapUnit(CombatMapUnit mapUnit)
        {
            int nIndex = FindNextFreeMapUnitIndex(Map.Maps.Combat);
            if (nIndex < 0) return -1;

            AddNewMapUnit(Map.Maps.Combat, mapUnit);

            return nIndex;
        }

        public Enemy CreateEnemy(Point2D xy, EnemyReference enemyReference, out int nIndex,
            NonPlayerCharacterReference npcRef)
        {
            Debug.Assert(_currentMapType == Map.Maps.Combat);
            nIndex = FindNextFreeMapUnitIndex(Map.Maps.Combat);
            if (nIndex == -1) return null;

            MapUnitState mapUnitState = CurrentMapUnitStates.GetCharacterState(nIndex);

            Enemy enemy = new Enemy(mapUnitState, _importedMovements.GetMovement(nIndex), _tileReferences, enemyReference,
                _currentLocation, _dataOvlReference, _npcRefs, null);
            enemy.MapUnitPosition = new MapUnitPosition(xy.X, xy.Y, 0);

            nIndex = AddCombatMapUnit(enemy);

            return enemy;
        }


        /// <summary>
        ///     Creates a new Magic Carpet and places it on the map
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Local
        internal MagicCarpet CreateMagicCarpet(Point2D xy, Point2D.Direction direction, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(_currentMapType);

            if (nIndex == -1) return null;

            MapUnitState mapUnitState = CurrentMapUnitStates.GetCharacterState(nIndex);

            MagicCarpet magicCarpet = new MagicCarpet(mapUnitState, _importedMovements.GetMovement(nIndex),
                _tileReferences, _currentLocation, _dataOvlReference, direction, null)
            {
                // set position of frigate in the world
                MapUnitPosition = new MapUnitPosition(xy.X, xy.Y, 0)
            };
            magicCarpet.KeyTileReference = magicCarpet.NonBoardedTileReference;

            AddNewMapUnit(_currentMapType, magicCarpet, nIndex);
            return magicCarpet;
        }

        public Horse CreateHorse(MapUnitPosition mapUnitPosition, Map.Maps map, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(_currentMapType);
            if (nIndex == -1) return null;

            MapUnitState mapUnitState = CurrentMapUnitStates.GetCharacterState(nIndex);
            Horse horse = new Horse(mapUnitState, _importedMovements.GetMovement(nIndex),
                _tileReferences, _currentLocation, _dataOvlReference, Point2D.Direction.Right, null)
            {
                MapUnitPosition = mapUnitPosition
            };

            // set position of frigate in the world
            AddNewMapUnit(map, horse, nIndex);
            return horse;
        }

        /// <summary>
        ///     Creates a Frigate and places it in on the map
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        private Frigate CreateFrigate(Point2D xy, Point2D.Direction direction, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(_currentMapType);

            if (nIndex == -1) return null;

            MapUnitState mapUnitState = CurrentMapUnitStates.GetCharacterState(nIndex);

            Frigate frigate = new Frigate(mapUnitState, _importedMovements.GetMovement(nIndex),
                _tileReferences, SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, _dataOvlReference,
                direction, null);

            // set position of frigate in the world
            Point2D frigateLocation = xy;
            frigate.MapUnitPosition = new MapUnitPosition(frigateLocation.X, frigateLocation.Y, 0);
            frigate.SkiffsAboard = 1;
            frigate.KeyTileReference = _tileReferences.GetTileReferenceByName("ShipNoSailsLeft");

            AddNewMapUnit(Map.Maps.Overworld, frigate, nIndex);
            return frigate;
        }

        /// <summary>
        ///     Creates a skiff and places it on the map
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        private Skiff CreateSkiff(Point2D xy, Point2D.Direction direction, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(_currentMapType);
            if (nIndex == -1) return null;

            MapUnitState mapUnitState = CurrentMapUnitStates.GetCharacterState(nIndex);
            Skiff skiff = new Skiff(mapUnitState, _importedMovements.GetMovement(nIndex),
                _tileReferences, SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, _dataOvlReference,
                direction, null);

            // set position of frigate in the world
            Point2D skiffLocation = xy;
            skiff.MapUnitPosition = new MapUnitPosition(skiffLocation.X, skiffLocation.Y, 0);
            skiff.KeyTileReference = skiff.NonBoardedTileReference;

            AddNewMapUnit(Map.Maps.Overworld, skiff, nIndex);
            return skiff;
        }

        /// <summary>
        ///     Creates a new frigate at a dock of a given location
        /// </summary>
        /// <param name="location"></param>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Frigate CreateFrigateAtDock(SmallMapReferences.SingleMapReference.Location location)
        {
            return CreateFrigate(VirtualMap.GetLocationOfDock(location, _dataOvlReference), Point2D.Direction.Right,
                out _);
        }

        /// <summary>
        ///     Creates a new skiff and places it at a given location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Skiff CreateSkiffAtDock(SmallMapReferences.SingleMapReference.Location location)
        {
            return CreateSkiff(VirtualMap.GetLocationOfDock(location, _dataOvlReference), Point2D.Direction.Right,
                out _);
        }

        /// <summary>
        ///     Makes the Avatar exit the current MapUnit they are occupying
        /// </summary>
        /// <returns>The MapUnit object they were occupying - you need to re-add it the map after</returns>
        public MapUnit XitCurrentMapUnit(VirtualMap virtualMap, out string retStr)
        {
            retStr = _dataOvlReference.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.XIT)
                .TrimEnd();

            if (!virtualMap.TheMapUnits.AvatarMapUnit.IsAvatarOnBoardedThing)
            {
                retStr += " " + _dataOvlReference.StringReferences
                    .GetString(DataOvlReference.KeypressCommandsStrings.WHAT_Q).Trim();
                return null;
            }

            if (!AvatarMapUnit.CurrentBoardedMapUnit.CanBeExited(virtualMap))
            {
                retStr += "\n" + _dataOvlReference.StringReferences
                    .GetString(DataOvlReference.SleepTransportStrings.N_NO_LAND_NEARBY_BANG_N).Trim();
                return null;
            }

            MapUnit unboardedMapUnit = AvatarMapUnit.UnboardedAvatar();
            Debug.Assert(unboardedMapUnit != null);

            // set the current positions to the equal the Avatar's as he exits the vehicle 
            unboardedMapUnit.MapLocation = _currentLocation;
            unboardedMapUnit.MapUnitPosition = CurrentAvatarPosition;
            unboardedMapUnit.Direction = AvatarMapUnit.CurrentDirection;
            unboardedMapUnit.KeyTileReference = unboardedMapUnit.NonBoardedTileReference;

            AddNewMapUnit(_currentMapType, unboardedMapUnit);
            retStr += " " + unboardedMapUnit.BoardXitName;

            // if the Avatar is on a frigate then we will check for Skiffs and exit on a skiff instead
            if (!(unboardedMapUnit is Frigate avatarFrigate)) return unboardedMapUnit;

            Debug.Assert(avatarFrigate != null, nameof(avatarFrigate) + " != null");

            // if we have skiffs, AND do not have land close by then we deploy a skiff
            if (avatarFrigate.SkiffsAboard <= 0 || virtualMap.IsLandNearby()) return unboardedMapUnit;

            MakeAndBoardSkiff();
            avatarFrigate.SkiffsAboard--;

            return unboardedMapUnit;
        }

        public Skiff MakeAndBoardSkiff()
        {
            Skiff skiff = CreateSkiff(AvatarMapUnit.MapUnitPosition.XY, AvatarMapUnit.CurrentDirection, out int nIndex);
            AvatarMapUnit.BoardMapUnit(skiff);
            ClearAndSetEmptyMapUnits(skiff);
            return skiff;
        }
    }
}