using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.References;

namespace Ultima5Redux.MapUnits
{
    [DataContract] public class MapUnits
    {
        internal const int MAX_MAP_CHARACTERS = 0x20;

        [DataMember(Name = "UseExtendedSprites")] private readonly bool _bUseExtendedSprites;

        [DataMember] private Map.Maps _initialMapType;

        [DataMember] public MapUnitCollection CombatMapMapUnitCollection { get; private set; } =
            new MapUnitCollection();

        [DataMember] public SmallMapReferences.SingleMapReference.Location CurrentLocation { get; private set; }

        [DataMember] private Map.Maps CurrentMapType { get; set; }

        [DataMember] private SmallMapCharacterStates MapCharacterStates { get; set; }

        [DataMember] public MapUnitCollection OverworldMapMapUnitCollection { get; private set; } =
            new MapUnitCollection();

        // load the SmallMapCharacterStates once from disk, don't worry abut again until you are saving to disk
        [DataMember] public MapUnitCollection SmallMapUnitCollection { get; private set; } = new MapUnitCollection();

        [DataMember] public MapUnitCollection UnderworldMapUnitCollection { get; private set; } =
            new MapUnitCollection();

        [IgnoreDataMember] private readonly ImportedGameState _importedGameState;

        [IgnoreDataMember] private readonly MapUnitMovements _importedMovements;

        [IgnoreDataMember] public Avatar AvatarMapUnit => CurrentMapUnits.TheAvatar;

        [IgnoreDataMember] public MapUnitCollection CurrentMapUnits => GetMapUnitCollection(CurrentMapType);

        [IgnoreDataMember] private Avatar MasterAvatarMapUnit { get; }
        //=> GetMapUnitCollection(_initialMapType).TheAvatar;

        /// <summary>
        ///     The single source of truth for the Avatar's current position within the current map
        /// </summary>
        [IgnoreDataMember] internal MapUnitPosition CurrentAvatarPosition
        {
            get => AvatarMapUnit.MapUnitPosition;
            set => AvatarMapUnit.MapUnitPosition = value;
        }

        /// <summary>
        ///     Constructs the collection of all Map CurrentMapUnits in overworld, underworld and current towne
        ///     from a legacy save import
        /// </summary>
        /// <param name="initialMap">
        ///     The initial map you are beginning on. It's important to know because there is only
        ///     one TheSmallMapCharacterState loaded in the save file at load time
        /// </param>
        /// <param name="bUseExtendedSprites"></param>
        /// <param name="importedGameState"></param>
        /// <param name="currentSmallMap">The particular map (if small map) that you are loading</param>
        internal MapUnits(Map.Maps initialMap, bool bUseExtendedSprites, ImportedGameState importedGameState,
            SmallMapReferences.SingleMapReference.Location currentSmallMap =
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
        {
            CurrentMapType = initialMap;
            // the initial map is important because it contains the Master Avatar for duplication
            _initialMapType = initialMap;
            _bUseExtendedSprites = bUseExtendedSprites;
            _importedGameState = importedGameState;
            CurrentLocation = currentSmallMap;

            // map character states pertain to whichever map was loaded from disk
            MapCharacterStates = importedGameState.SmallMapCharacterStates;

            // movements pertain to whichever map was loaded from disk
            _importedMovements = importedGameState.CharacterMovements;

            // we only load the large maps once and they always exist on disk
            GenerateMapUnitsForLargeMap(Map.Maps.Overworld, true);
            GenerateMapUnitsForLargeMap(Map.Maps.Underworld, true);

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
            MasterAvatarMapUnit = AvatarMapUnit;
            GetMapUnitCollection(Map.Maps.Overworld).AllMapUnits[0] = AvatarMapUnit;
            GetMapUnitCollection(Map.Maps.Underworld).AllMapUnits[0] = AvatarMapUnit;

            SetAllExtendedSprites();

            CurrentMapType = initialMap;
        }

        [JsonConstructor] MapUnits()
        {
            _importedMovements = new MapUnitMovements();
        }

        private int AddCombatMapUnit(CombatMapUnit mapUnit)
        {
            int nIndex = FindNextFreeMapUnitIndex(Map.Maps.Combat);
            if (nIndex < 0) return -1;

            AddNewMapUnit(Map.Maps.Combat, mapUnit);

            return nIndex;
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

        public Enemy CreateEnemy(Point2D xy, EnemyReference enemyReference, out int nIndex)
        {
            Debug.Assert(CurrentMapType == Map.Maps.Combat);
            nIndex = FindNextFreeMapUnitIndex(Map.Maps.Combat);
            if (nIndex == -1) return null;

            Enemy enemy = new Enemy(_importedMovements.GetMovement(nIndex), enemyReference, CurrentLocation, null)
            {
                MapUnitPosition = new MapUnitPosition(xy.X, xy.Y, 0)
            };

            nIndex = AddCombatMapUnit(enemy);

            return enemy;
        }

        /// <summary>
        ///     Creates a Frigate and places it in on the map
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nIndex"></param>
        /// <param name="nSkiffsAboard"></param>
        /// <returns></returns>
        private Frigate CreateFrigate(Point2D xy, Point2D.Direction direction, out int nIndex, int nSkiffsAboard)
        {
            nIndex = FindNextFreeMapUnitIndex(CurrentMapType);

            if (nIndex == -1) return null;

            Frigate frigate = new Frigate(_importedMovements.GetMovement(nIndex),
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, direction, null,
                new MapUnitPosition(xy.X, xy.Y, 0));

            // set position of frigate in the world
            frigate.SkiffsAboard = nSkiffsAboard;
            frigate.KeyTileReference = GameReferences.SpriteTileReferences.GetTileReferenceByName("ShipNoSailsLeft");

            AddNewMapUnit(Map.Maps.Overworld, frigate, nIndex);
            return frigate;
        }

        /// <summary>
        ///     Creates a new frigate at a dock of a given location
        /// </summary>
        /// <param name="location"></param>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Frigate CreateFrigateAtDock(SmallMapReferences.SingleMapReference.Location location)
        {
            return CreateFrigate(VirtualMap.GetLocationOfDock(location), Point2D.Direction.Right, out _, 1);
        }

        public Horse CreateHorse(MapUnitPosition mapUnitPosition, Map.Maps map, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(CurrentMapType);
            if (nIndex == -1) return null;

            Horse horse = new Horse(_importedMovements.GetMovement(nIndex), CurrentLocation, Point2D.Direction.Right,
                null, mapUnitPosition)
            {
                MapUnitPosition = mapUnitPosition
            };

            // set position of frigate in the world
            AddNewMapUnit(map, horse, nIndex);
            return horse;
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
            nIndex = FindNextFreeMapUnitIndex(CurrentMapType);

            if (nIndex == -1) return null;

            MagicCarpet magicCarpet =
                new MagicCarpet(CurrentLocation, direction, null, new MapUnitPosition(xy.X, xy.Y, 0));

            AddNewMapUnit(CurrentMapType, magicCarpet, nIndex);
            return magicCarpet;
        }

        /// <summary>
        ///     Generates a new map unit
        /// </summary>
        /// <param name="mapUnitMovement"></param>
        /// <param name="bInitialLoad"></param>
        /// <param name="location"></param>
        /// <param name="npcState"></param>
        /// <param name="tileReference"></param>
        /// <param name="smallMapCharacterState"></param>
        /// <param name="mapUnitPosition"></param>
        /// <returns></returns>
        private MapUnit CreateNewMapUnit(MapUnitMovement mapUnitMovement, bool bInitialLoad,
            SmallMapReferences.SingleMapReference.Location location, NonPlayerCharacterState npcState,
            MapUnitPosition mapUnitPosition, TileReference tileReference,
            SmallMapCharacterState smallMapCharacterState = null)
        {
            MapUnit newUnit;

            if (smallMapCharacterState != null && npcState != null && smallMapCharacterState.Active &&
                npcState.NPCRef.NormalNPC)
            {
                newUnit = new NonPlayerCharacter(smallMapCharacterState, mapUnitMovement, bInitialLoad, location,
                    mapUnitPosition, npcState);
            }
            else if (tileReference == null || tileReference.Index == 256)
            {
                Debug.WriteLine("An empty map unit was created with no tile reference");
                newUnit = new EmptyMapUnit();
            }
            else if (GameReferences.SpriteTileReferences.IsFrigate(tileReference.Index))
            {
                newUnit = new Frigate(mapUnitMovement, location, tileReference.GetDirection(), npcState,
                    mapUnitPosition);
            }
            else if (GameReferences.SpriteTileReferences.IsSkiff(tileReference.Index))
            {
                newUnit = new Skiff(mapUnitMovement, location, tileReference.GetDirection(), npcState, mapUnitPosition);
            }
            else if (GameReferences.SpriteTileReferences.IsMagicCarpet(tileReference.Index))
            {
                newUnit = new MagicCarpet(location, tileReference.GetDirection(), npcState, mapUnitPosition);
            }
            else if (GameReferences.SpriteTileReferences.IsHorse(tileReference.Index))
            {
                newUnit = new Horse(mapUnitMovement, location, tileReference.GetDirection(), npcState, mapUnitPosition);
            }
            else if (GameReferences.SpriteTileReferences.IsMonster(tileReference.Index))
            {
                Debug.Assert(GameReferences.EnemyRefs != null);
                newUnit = new Enemy(mapUnitMovement, GameReferences.EnemyRefs.GetEnemyReference(tileReference),
                    location, npcState);
            }
            // this is where we will create monsters too
            else
            {
                Debug.WriteLine("An empty map unit was created with " + tileReference.Name);
                newUnit = new EmptyMapUnit();
            }

            // force to use extended sprites if they exist
            newUnit.UseFourDirections = _bUseExtendedSprites;

            return newUnit;
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
            nIndex = FindNextFreeMapUnitIndex(CurrentMapType);
            if (nIndex == -1) return null;

            Skiff skiff = new Skiff(_importedMovements.GetMovement(nIndex),
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, direction, null,
                new MapUnitPosition(xy.X, xy.Y, 0));

            AddNewMapUnit(Map.Maps.Overworld, skiff, nIndex);
            return skiff;
        }

        /// <summary>
        ///     Creates a new skiff and places it at a given location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Skiff CreateSkiffAtDock(SmallMapReferences.SingleMapReference.Location location)
        {
            return CreateSkiff(VirtualMap.GetLocationOfDock(location), Point2D.Direction.Right, out _);
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
        private void GenerateMapUnitsForLargeMap(Map.Maps map, bool bInitialLoad)
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
                MapUnitMovement mapUnitMovement =
                    bInitialLoad ? _importedMovements.GetMovement(i) : new MapUnitMovement(i);

                // always clear movements because they are not stored in the data for a LargeMap because
                // the monsters will recalculate every turn based on where the Avatar is 
                mapUnitMovement.ClearMovements();

                // if you are initial load, then grab from disk, otherwise create an empty collection
                MapUnitStates currentMapUnitStates = bInitialLoad
                    ? _importedGameState.MapUnitStatesByInitialMap
                    : new MapUnitStates();

                // we have retrieved the _currentMapUnitStates based on the map type,
                // now just get the existing animation state which persists on disk for under, over and small maps
                MapUnitState mapUnitState = currentMapUnitStates.GetCharacterState(i);

                MapUnitPosition mapUnitPosition = new MapUnitPosition(mapUnitState.X, mapUnitState.Y, 0);
                TileReference tileReference = mapUnitState.Tile1Ref;

                // the party is always at zero
                if (i == 0)
                {
                    MapUnit theAvatar = Avatar.CreateAvatar(
                        SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, mapUnitMovement,
                        mapUnitPosition, tileReference, _bUseExtendedSprites);
                    mapUnits.Add(theAvatar);
                    continue;
                }

                MapUnit newUnit = CreateNewMapUnit(mapUnitMovement, bInitialLoad,
                    SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, null, mapUnitPosition,
                    tileReference);
                // add the new character to our list of characters currently on the map
                mapUnits.Add(newUnit);
            }
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

        public T GetSpecificMapUnitByLocation<T>(Map.Maps map, Point2D xy, int nFloor, bool bCheckBaseToo = false)
            where T : MapUnit
        {
            foreach (T mapUnit in GetMapUnitCollection(map).GetMapUnitByType<T>())
            {
                if (mapUnit == null)
                    throw new Ultima5ReduxException(
                        "Getting a specific map unit by location, but the list has a null entry");
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

        public void InitializeCombatMapReferences()
        {
            CombatMapMapUnitCollection.Clear();
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                CombatMapMapUnitCollection.Add(new EmptyMapUnit());
            }
        }

        /// <summary>
        ///     Resets the current map to a default state - typically no monsters and NPCs in there default positions
        /// </summary>
        private void LoadSmallMap(SmallMapReferences.SingleMapReference.Location location, bool bInitialLoad)
        {
            if (location == SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine ||
                location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
                throw new Ultima5ReduxException("Tried to load " + location + " into a small map");

            // wipe all existing characters since they cannot exist beyond the load
            SmallMapUnitCollection.Clear();

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
                    SmallMapUnitCollection.Add(MasterAvatarMapUnit);
                    //_masterAvatarMapUnit);
                    AvatarMapUnit.MapUnitPosition = SmallMapReferences.GetStartingXYZByLocation();
                    AvatarMapUnit.MapLocation = location;
                    continue;
                }

                if (i == 0 && bInitialLoad)
                {
                    MapUnitState theAvatarMapState =
                        _importedGameState.GetMapUnitStatesByMap(Map.Maps.Small).GetCharacterState(0);
                    MapUnit theAvatar = Avatar.CreateAvatar(location, mapUnitMovement,
                        new MapUnitPosition(theAvatarMapState.X, theAvatarMapState.Y, theAvatarMapState.Floor),
                        theAvatarMapState.Tile1Ref, _bUseExtendedSprites);
                    SmallMapUnitCollection.Add(theAvatar);
                    continue;
                }

                // get the specific NPC reference 
                NonPlayerCharacterState npcState =
                    GameStateReference.State.TheNonPlayerCharacterStates.GetStateByLocationAndIndex(location, i);

                // we keep the object because we may be required to save this to disk - but since we are
                // leaving the map there is no need to save their movements
                mapUnitMovement.ClearMovements();

                // set a default SmallMapCharacterState based on the given NPC
                SmallMapCharacterState smallMapCharacterState = new SmallMapCharacterState(npcState.NPCRef, i);

                MapUnitPosition mapUnitPosition = new MapUnitPosition((byte)smallMapCharacterState.TheMapUnitPosition.X,
                    (byte)smallMapCharacterState.TheMapUnitPosition.Y,
                    (byte)smallMapCharacterState.TheMapUnitPosition.Floor);
                MapUnit mapUnit = CreateNewMapUnit(mapUnitMovement, false, location, npcState, mapUnitPosition,
                    GameReferences.SpriteTileReferences.GetTileReference(npcState.NPCRef.NPCKeySprite),
                    smallMapCharacterState);

                SmallMapUnitCollection.Add(mapUnit);
            }
        }

        public Skiff MakeAndBoardSkiff()
        {
            Skiff skiff = CreateSkiff(AvatarMapUnit.MapUnitPosition.XY, AvatarMapUnit.CurrentDirection, out int _);
            AvatarMapUnit.BoardMapUnit(skiff);
            ClearAndSetEmptyMapUnits(skiff);
            return skiff;
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

            if (SmallMapUnitCollection == null) return;

            foreach (MapUnit mapUnit in SmallMapUnitCollection.AllMapUnits)
            {
                mapUnit.UseFourDirections = _bUseExtendedSprites;
            }
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
            bool bLoadFromDisk)
        {
            CurrentMapType = mapType;
            CurrentLocation = mapRef.MapLocation;
            // if the current location hasn't changed then we don't reload the map info
            //bSkipLoadSmallMap |= _currentLocation == mapRef.MapLocation; 

            // I may need make an additional save of state before wiping these MapUnits out
            GameStateReference.State.CharacterRecords.ClearCombatStatuses();

            switch (mapType)
            {
                case Map.Maps.Small:
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

        /// <summary>
        ///     Makes the Avatar exit the current MapUnit they are occupying
        /// </summary>
        /// <returns>The MapUnit object they were occupying - you need to re-add it the map after</returns>
        public MapUnit XitCurrentMapUnit(VirtualMap virtualMap, out string retStr)
        {
            retStr = GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.XIT)
                .TrimEnd();

            if (!virtualMap.TheMapUnits.AvatarMapUnit.IsAvatarOnBoardedThing)
            {
                retStr += " " + GameReferences.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.KeypressCommandsStrings.WHAT_Q).Trim();
                return null;
            }

            if (!AvatarMapUnit.CurrentBoardedMapUnit.CanBeExited(virtualMap))
            {
                retStr += "\n" + GameReferences.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.SleepTransportStrings.N_NO_LAND_NEARBY_BANG_N).Trim();
                return null;
            }

            MapUnit unboardedMapUnit = AvatarMapUnit.UnboardedAvatar();
            Debug.Assert(unboardedMapUnit != null);

            // set the current positions to the equal the Avatar's as he exits the vehicle 
            unboardedMapUnit.MapLocation = CurrentLocation;
            unboardedMapUnit.MapUnitPosition = CurrentAvatarPosition;
            unboardedMapUnit.Direction = AvatarMapUnit.CurrentDirection;
            unboardedMapUnit.KeyTileReference = unboardedMapUnit.NonBoardedTileReference;

            AddNewMapUnit(CurrentMapType, unboardedMapUnit);
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
    }
}